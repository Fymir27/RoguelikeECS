﻿using Microsoft.Xna.Framework;
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

        RenderSystem renderSystem;
        UISystem uiSystem;

        public static Random Random { get; } = new Random();

               
        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Util.ScreenWidth;
            graphics.PreferredBackBufferHeight = Util.ScreenHeight;
            //graphics.IsFullScreen = true;

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
            Util.ContentPath = Content.RootDirectory;

            Log.Init(AppDomain.CurrentDomain.BaseDirectory + "/log.html");

            // TODO: Add your initialization logic here
            //Floor test = new Floor(10, 10);          
            Floor test = new Floor(Content.RootDirectory + "/map.txt");
            Util.CurrentFloor = test;
            test.CalculateTileVisibility();

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

            // hook up all events with their handlers
            Log.Message("Registering Event Handlers...");
            //inputSystem.MovementEvent += movementSystem.HandleMovementEvent;
            //inputSystem.MovementEvent += uiSystem.HandleInventoryCursorMoved;
            //inputSystem.UsedItemEvent += itemSystem.UseItem;
            //inputSystem.InteractionEvent += interactionSystem.HandleInteraction;
            //inputSystem.PickupItemEvent += itemSystem.PickUpItem;
            //inputSystem.InventoryToggledEvent += uiSystem.HandleInventoryToggled;
            //inputSystem.UpdateTargetLineEvent += () => Util.UpdateTargetLine(init: true);

            // new input manager
            input.MovementEvent += movementSystem.HandleMovementEvent;
            input.InventoryToggledEvent += uiSystem.HandleInventoryToggled;
            input.InventoryCursorMovedEvent += uiSystem.HandleInventoryCursorMoved;
            input.PickupItemEvent += itemSystem.PickUpItem;
            input.ItemUsedEvent += itemSystem.UseItem;
            input.UpdateTargetLineEvent += () => Util.UpdateTargetLine(init: true);

            itemSystem.PlayerPromptEvent += inputSystem.HandlePlayerPrompt;
            itemSystem.TargetModeToggledEvent += input.ToggleTargetMode;
            itemSystem.WaitForConfirmationEvent += inputSystem.RegisterConfirmationCallback;
            itemSystem.InventoryToggledEvent += uiSystem.HandleInventoryToggled;
            itemSystem.HealthGainedEvent += healthSystem.HandleGainedHealth;
            itemSystem.HealthLostEvent += healthSystem.HandleLostHealth;

            npcBehaviourSystem.EnemyMovedEvent += movementSystem.HandleMovementEvent;

            movementSystem.CollisionEvent += collisionSystem.HandleCollision;
            movementSystem.BasicAttackEvent += combatSystem.HandleBasicAttack;
            movementSystem.InteractionEvent += interactionSystem.HandleInteraction;
            movementSystem.UpdateTargetLineEvent += () => Util.UpdateTargetLine();

            Util.TurnOverEvent += healthSystem.RegenerateEntity;

            combatSystem.HealthLostEvent += healthSystem.HandleLostHealth;

            Log.Message("Loading Keybindings...");
            string keybindings = File.ReadAllText(Util.ContentPath + "/keybindings.json");          
            input.LoadKeyBindings(keybindings);
            input.EnterDomain(InputManager.CommandDomain.Exploring); // start out with exploring as bottom level command domain
            input.ControlledEntity = Util.PlayerID;

            base.Initialize();
            Log.Message("Initialization completed!");
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
            renderedWorld = new RenderTarget2D(GraphicsDevice, Util.WorldWidth, Util.WorldHeight);

            // TODO: use this.Content to load your game content here
            Util.DefaultFont = Content.Load<SpriteFont>("default");
            Util.SmallFont = Content.Load<SpriteFont>("small");
            Util.BigFont = Content.Load<SpriteFont>("big");

            string[] textures = 
            {
                "player", "box", "wall", "targetIndicator",
                "doorOpen", "doorClosed", "square",
                // enemies:
                "rat", "spider",
                "bat", "enemy",
                //items
                "gold", "potion",
                // UI:
                "inventory", "inventoryOpen",
                "messageLogBox", "tooltip"
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
                inputSystem.Run(gameTime);
                InputManager.Instance.CheckInput(gameTime);
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
            GraphicsDevice.Clear(Color.White);

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
