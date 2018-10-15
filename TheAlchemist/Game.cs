using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

namespace TheAlchemist
{
    using Systems;
    using Components;
    using Newtonsoft.Json;

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        InputSystem inputSystem;
        MovementSystem movementSystem;
        RenderSystem renderSystem;
        CollisionSystem collisionSystem;

        public static Random Random { get; } = new Random();

        Texture2D textureSquare;
        //Texture2D texturePlayer;

        //int player;
        
        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
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
            Log.Init(AppDomain.CurrentDomain.BaseDirectory + "/log.html");
            Log.Message("Initializing...");
            Log.Warning("Warning!");
            Log.Error("Error!");

            // TODO: Add your initialization logic here
            Floor test = new Floor(10, 10);
            Util.CurrentFloor = test;


            //int entity = EntityManager.createEntity();
            //EntityManager.addComponentToEntity(entity, new ColliderComponent() { Solid = true });
            //EntityManager.addComponentToEntity(entity, new TransformComponent());

            //string serializedEM = EntityManager.ToJson();
            //Log.Message(serializedEM);
            //EntityManager.InitFromJson(serializedEM);
            //Log.Message(EntityManager.ToJson());

            //System.Console.WriteLine(test);

            //EntityManager.Dump();

            // instantiate all the systems
            inputSystem = new InputSystem();
            movementSystem = new MovementSystem();
            renderSystem = new RenderSystem();
            collisionSystem = new CollisionSystem();

            // hook up all events with their handlers
            inputSystem.MovementEvent += movementSystem.HandleMovementEvent;
            movementSystem.CollisionEvent += collisionSystem.HandleCollision;
         

            EntityManager.Dump();



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
            textureSquare = Content.Load<Texture2D>("square");

            TextureManager.AddTexture(Content.Load<Texture2D>("player"));
            TextureManager.AddTexture(Content.Load<Texture2D>("wall"));
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
            inputSystem.Run();

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
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
