using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TheAlchemist
{
    using Systems;
    using Components;

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        bool gameOver = false;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D virtualScreen;
        RenderTarget2D renderedWorld;

        InputSystem inputSystem;

        MovementSystem movementSystem;
        CollisionSystem collisionSystem;
        HealthSystem healthSystem;
        CombatSystem combatSystem;
        NPCBehaviourSystem npcBehaviourSystem;
        InteractionSystem interactionSystem;
        ItemSystem itemSystem;
        StatSystem statSystem;
        CraftingSystem craftingSystem;

        RenderSystem renderSystem;
        UISystem uiSystem;

        // broken seeds:
        // 75782, 79176, 80725, 85376
        public static int Seed = 63958; //(int)DateTime.Now.TimeOfDay.TotalSeconds; 
        public static Random Random { get; } = new Random(Seed);


        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            //graphics.IsFullScreen = true;

            Util.GraphicsDevice = GraphicsDevice;

            Window.AllowUserResizing = true;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = Util.ScreenWidth;
            graphics.PreferredBackBufferHeight = Util.ScreenHeight;
            graphics.ApplyChanges();
            
            Util.ContentPath = Content.RootDirectory;

            Log.Init(AppDomain.CurrentDomain.BaseDirectory);
            Log.Message("Seed: " + Seed);

            var gameData = GameData.Instance = new GameData();

            var dataPath = Util.ContentPath + "/GameData/";
            gameData.LoadEntities(EntityType.Terrain, dataPath + "Entities/Terrains");
            gameData.LoadEntities(EntityType.Structure, dataPath + "Entities/Structures");
            gameData.LoadEntities(EntityType.Character, dataPath + "Entities/Characters");
            gameData.LoadEntities(EntityType.Item, dataPath + "Entities/Items");

            var herb = gameData.CreateItem("randomHerb");
            var herbSubstance = EntityManager.GetComponent<SubstanceComponent>(herb);
            
            gameData.LoadTilesets(dataPath + "tilesets.json");
            gameData.LoadRoomTemplates(dataPath);

            //Floor test = new Floor(Content.RootDirectory + "/map.txt");
            Floor testFloor = new Floor(500, 500);
            //testFloor.GenerateSimple();
            testFloor.GenerateGraphBased();

            //EntityManager.Dump();

            Util.CurrentFloor = testFloor;
            Util.CurrentFloor.CalculateTileVisibility();

            // Delete entities that were removed during generation
            EntityManager.CleanUpEntities();

            //Log.Data(DescriptionSystem.GetDebugInfoEntity(Util.PlayerID));

            InputManager input = InputManager.Instance;

            // instantiate all the systems
            Log.Message("Loading Systems...");
            inputSystem = new InputSystem();
            movementSystem = new MovementSystem();
            renderSystem = new RenderSystem(GraphicsDevice);
            collisionSystem = new CollisionSystem();
            healthSystem = new HealthSystem();
            combatSystem = new CombatSystem();
            npcBehaviourSystem = new NPCBehaviourSystem();
            interactionSystem = new InteractionSystem();
            itemSystem = new ItemSystem();
            uiSystem = new UISystem();
            statSystem = new StatSystem();
            craftingSystem = new CraftingSystem();

            // toggle FOW
            renderSystem.FogOfWarEnabled = true;

            // hook up all events with their handlers
            Log.Message("Registering Event Handlers...");

            input.MovementEvent += movementSystem.HandleMovementEvent;
            input.InventoryToggledEvent += uiSystem.HandleInventoryToggled;
            input.InventoryCursorMovedEvent += uiSystem.HandleInventoryCursorMoved;
            input.PickupItemEvent += itemSystem.PickUpItem;
            input.InteractionEvent += interactionSystem.HandleInteraction;
            input.ItemUsedEvent += itemSystem.UseItem;
            input.ItemConsumedEvent += itemSystem.ConsumeItem;

            // crafting
            input.AddItemAsIngredientEvent += craftingSystem.AddIngredient;
            input.CraftItemEvent += craftingSystem.CraftItem;
            input.ResetCraftingEvent += craftingSystem.ResetCrafting;

            interactionSystem.ItemAddedEvent += itemSystem.AddItem;

            itemSystem.HealthGainedEvent += healthSystem.HandleGainedHealth;
            itemSystem.HealthLostEvent += healthSystem.HandleLostHealth;
            itemSystem.StatChangedEvent += statSystem.ChangeStat;

            npcBehaviourSystem.EnemyMovedEvent += movementSystem.HandleMovementEvent;

            movementSystem.CollisionEvent += collisionSystem.HandleCollision;
            movementSystem.BasicAttackEvent += combatSystem.HandleBasicAttack;
            movementSystem.InteractionEvent += interactionSystem.HandleInteraction;

            craftingSystem.ItemAddedEvent += itemSystem.AddItem;

            Util.TurnOverEvent += healthSystem.RegenerateEntity;
            Util.TurnOverEvent += statSystem.TurnOver;

            combatSystem.HealthLostEvent += healthSystem.HandleLostHealth;

            Log.Message("Loading Keybindings...");
            string keybindings = File.ReadAllText(Util.ContentPath + "/GameData/keybindings.json");
            input.LoadKeyBindings(keybindings);
            input.EnterDomain(InputManager.CommandDomain.Exploring); // start out with exploring as bottom level command domain
            input.ControlledEntity = Util.PlayerID;        

            base.Initialize();
            Log.Message("Initialization completed!");

            //CraftableComponent.foo();

            int testRock = EntityManager.CreateEntity(new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Rock", Description = "You can't stop the rock!"},
                new ItemComponent() { MaxCount = 5, Count = 3, Value = 1, Weight = 10 }
            }, EntityType.Item);

            // fill inventory with test items
            itemSystem.AddItems(Util.PlayerID, new int[] 
            {
                //testRock,
                GameData.Instance.CreateItem("healthPotion"),
                GameData.Instance.CreateItem("healthPotion"),
                GameData.Instance.CreateItem("healthPotion"),
                GameData.Instance.CreateItem("poisonPotion"),
                GameData.Instance.CreateItem("poisonPotion"),
                GameData.Instance.CreateItem("elementalPotion")
            });


            //EntityManager.Dump();
            //EntityManager.RemoveEntity(testBush);
            //EntityManager.CleanUpEntities();
            //EntityManager.Dump();

            //Log.Data(DescriptionSystem.GetDebugInfoEntity(Util.GetPlayerInventory().Items[0]));

            LocationSystem.UpdateDistanceMap(Util.PlayerID);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            virtualScreen = new RenderTarget2D(GraphicsDevice, Util.ScreenWidth, Util.ScreenHeight);
            renderedWorld = new RenderTarget2D(GraphicsDevice, Util.WorldViewPixelWidth, Util.WorldViewPixelHeight);

            Util.DefaultFont = Content.Load<SpriteFont>("default");
            Util.SmallFont = Content.Load<SpriteFont>("small");
            Util.BigFont = Content.Load<SpriteFont>("big");
            Util.MonospaceFont = Content.Load<SpriteFont>("monospace");

            /*
            string[] textures =
            {
                "player", "box", "wall", "targetIndicator",
                "doorOpen", "doorClosed", "square", "floor",
                // enemies:
                "rat", "spider",
                "bat", "enemy",
                //items
                "gold", "potion",
                // UI:
                "inventory", "inventoryOpen",
                "messageLogBox", "tooltip",
                // other stuff:
                "bush", "Sprites/bushHarvested"
            };
            */

            TextureManager.Init(Content);
            //TextureManager.LoadTextures(textures);
            TextureManager.LoadAllTextures("Sprites");
            TextureManager.LoadAllTextures(@"Sprites\Mechs");

            Log.Message("Loading UI...");
            UI.Init(graphics.GraphicsDevice);

            Util.CurrentFloor.GenerateImage("./floor.png", GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            Log.SaveUIMessage("---", true);
            Log.End();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Util.ErrorOccured)
            {
                if (!Util.BrutalModeOn)
                {
                    UISystem.Message("A CRITICAL ERROR HAS OCCURED!");                   
                    Exit();
                }
            }
           
            if(gameOver)
            {
                if (InputManager.Instance.CheckKey(Keys.Escape))
                {
                    Exit();
                }

                // ignore all other input
                base.Update(gameTime);
                return;
            }

            // check for input
            if(InputManager.Instance.CheckInput(gameTime))
            {
                InputManager.Instance.ExecuteCurrentCommand();

                if (Util.PlayerTurnOver)
                {
                    npcBehaviourSystem.EnemyTurn();
                    Util.PlayerTurnOver = false;
                }
              
                float playerHealth = EntityManager.GetComponent<HealthComponent>(Util.PlayerID).Amount;
                if (playerHealth <= 0)
                {
                    UISystem.Message("---");
                    UISystem.Message("You died.");
                    UISystem.Message("");
                    UISystem.Message("Press ESC to exit...");

                    gameOver = true;
                }

                // remove all entities that died this turn
                EntityManager.CleanUpEntities();
            }          

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // render world onto a texture
            renderSystem.RenderWorld(renderedWorld);

            // render everything to virtual screen (also a texture)
            GraphicsDevice.SetRenderTarget(virtualScreen);

            spriteBatch.Begin(); //:::::::::::::::::::::::::::::::::::::::::::::::::::
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Draw(renderedWorld, Vector2.Zero, Color.White);
            UI.Render(spriteBatch);

            spriteBatch.End(); //:::::::::::::::::::::::::::::::::::::::::::::::::::::


            // then draw virtual screen onto actual window
            GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(); //:::::::::::::::::::::::::::::::::::::::::::::::::::
            GraphicsDevice.Clear(Color.Black);

            // fit texture to screen for resizing
            Rectangle destRect = new Rectangle(Point.Zero, new Point(Window.ClientBounds.Width, Window.ClientBounds.Height));
            spriteBatch.Draw(virtualScreen, destRect, Color.White);

            spriteBatch.End(); //:::::::::::::::::::::::::::::::::::::::::::::::::::::

            base.Draw(gameTime);
        }
    }
}
