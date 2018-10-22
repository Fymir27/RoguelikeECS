using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

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
        SpriteFont defaultFont;

        InputSystem inputSystem;
        MovementSystem movementSystem;
        RenderSystem renderSystem;
        CollisionSystem collisionSystem;
        HealthSystem healthSystem;
        CombatSystem combatSystem;
        NPCBehaviourSystem npcBehaviourSystem;
        InteractionSystem interactionSystem;

        public static Random Random { get; } = new Random();

               
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

            // TODO: Add your initialization logic here
            //Floor test = new Floor(10, 10);
            Floor test = new Floor(AppDomain.CurrentDomain.BaseDirectory + "/map.txt");
            Util.CurrentFloor = test;

            var playerHealthComponent = EntityManager.GetComponentOfEntity<HealthComponent>(Util.PlayerID);

            int lowerFloorBorder = Util.TileSize * 10;
            int UI = EntityManager.CreateEntity(new List<IComponent>()
            {
                new DescriptionComponent() { Name = "UI", Description = "Displays stuff you probably want to know!"},
                new RenderableTextComponent() { Position = new Vector2(10, lowerFloorBorder + 10), Text = "Player HP: " },
                new RenderableTextComponent() { Position = new Vector2(90, lowerFloorBorder + 10), GetTextFrom = playerHealthComponent.GetString }
            });

            Log.Data(DescriptionSystem.GetDebugInfoEntity(Util.PlayerID));
            //int entity = EntityManager.createEntity();
            //EntityManager.addComponentToEntity(entity, new ColliderComponent() { Solid = true });
            //EntityManager.addComponentToEntity(entity, new TransformComponent());

            //System.Console.WriteLine(test);

            //EntityManager.Dump();

            // instantiate all the systems
            inputSystem = new InputSystem();
            movementSystem = new MovementSystem();
            renderSystem = new RenderSystem();
            collisionSystem = new CollisionSystem();
            healthSystem = new HealthSystem();
            combatSystem = new CombatSystem();
            npcBehaviourSystem = new NPCBehaviourSystem();
            interactionSystem = new InteractionSystem();

            // hook up all events with their handlers
            inputSystem.MovementEvent += movementSystem.HandleMovementEvent;

            npcBehaviourSystem.EnemyMovedEvent += movementSystem.HandleMovementEvent;

            movementSystem.CollisionEvent += collisionSystem.HandleCollision;
            movementSystem.BasicAttackEvent += combatSystem.HandleBasicAttack;
            movementSystem.PlayerTurnOverEvent += healthSystem.RegneratePlayer;
            movementSystem.PlayerTurnOverEvent += npcBehaviourSystem.EnemyTurn;
            

            combatSystem.HealthLostEvent += healthSystem.HandleLostHealth;
            combatSystem.PlayerTurnOverEvent += healthSystem.RegneratePlayer;
            combatSystem.PlayerTurnOverEvent += npcBehaviourSystem.EnemyTurn;


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

            string[] textures = { "player", "enemy", "wall", "door" };
            TextureManager.Init(Content);
            TextureManager.LoadTextures(textures);
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
            //spriteBatch.DrawString(Util.DefaultFont, "Player HP: (30|30)", new Vector2(10, Util.TileSize * 10 + 10), Color.Black);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
