using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheAlchemist
{
    using Systems;
    using Components;
    
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        InputSystem inputSystem;

        MovementSystem movementSystem;
        CollisionSystem collisionSystem;
        HealthSystem healthSystem;
        CombatSystem combatSystem;
        NPCBehaviourSystem npcBehaviourSystem;
        InteractionSystem interactionSystem;
        ItemSystem itemSystem;

        RenderSystem renderSystem;
        UISystem uiSystem;

        public static Random Random { get; } = new Random();

               
        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            //graphics.IsFullScreen = true;

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
            Util.ContentPath = Content.RootDirectory;

            Log.Init(AppDomain.CurrentDomain.BaseDirectory + "/log.html");

            Log.Message("Initializing...");

            // TODO: Add your initialization logic here
            //Floor test = new Floor(10, 10);
            Floor test = new Floor(Content.RootDirectory + "/map.txt");
            Util.CurrentFloor = test;
            test.CalculateTileVisibility();

            

            

            Log.Data(DescriptionSystem.GetDebugInfoEntity(Util.PlayerID));

            // instantiate all the systems
            inputSystem = new InputSystem();
            movementSystem = new MovementSystem();
            renderSystem = new RenderSystem();
            collisionSystem = new CollisionSystem();
            healthSystem = new HealthSystem();
            combatSystem = new CombatSystem();
            npcBehaviourSystem = new NPCBehaviourSystem();
            interactionSystem = new InteractionSystem();
            itemSystem = new ItemSystem();
            uiSystem = new UISystem();

            // hook up all events with their handlers
            inputSystem.MovementEvent += movementSystem.HandleMovementEvent;
            inputSystem.InteractionEvent += interactionSystem.HandleInteraction;
            inputSystem.PickupItemEvent += itemSystem.PickUpItem;
            inputSystem.InventoryToggledEvent += uiSystem.HandleInventoryToggled;

            npcBehaviourSystem.EnemyMovedEvent += movementSystem.HandleMovementEvent;

            movementSystem.CollisionEvent += collisionSystem.HandleCollision;
            movementSystem.BasicAttackEvent += combatSystem.HandleBasicAttack;
            movementSystem.InteractionEvent += interactionSystem.HandleInteraction;

            Util.TurnOverEvent += healthSystem.RegenerateEntity;

            combatSystem.HealthLostEvent += healthSystem.HandleLostHealth;
           

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            Util.DefaultFont = Content.Load<SpriteFont>("default");
            Util.SmallFont = Content.Load<SpriteFont>("small");

            string[] textures = 
            {
                "player", "box",
                "gold", "wall",
                "doorOpen", "doorClosed", "square",
                // enemies:
                "rat", "spider",
                "bat", "enemy",
                // UI:
                "inventory", "inventoryOpen"
            };

            TextureManager.Init(Content);
            TextureManager.LoadTextures(textures);

            // try to create custom texture
            Texture2D tex = new Texture2D(GraphicsDevice, 150, 115, false, SurfaceFormat.Color);
            Color[] colors = new Color[150 * 115];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.Aqua;
            }
            tex.SetData<Color>(colors);

            tex.Name = "test";

            TextureManager.AddTexture(tex);

            UI.Init();       
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            Log.End();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //Exit();

            // TODO: Add your update logic here
            if (Util.PlayerTurnOver)
            {
                npcBehaviourSystem.EnemyTurn();
                Util.PlayerTurnOver = false;
            }
            else
            {
                inputSystem.Run();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            // TODO: Add your drawing code here
            spriteBatch.Begin();
            //spriteBatch.Draw(textureSquare, new Vector2(0, 0), Color.Red);
            renderSystem.Run(spriteBatch);
            //spriteBatch.DrawString(Util.DefaultFont, "Player HP: (30|30)", new Vector2(10, Util.TileSize * 10 + 10), Color.Black);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
