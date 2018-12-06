﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TheAlchemist
{
    using Systems;
    using Components;
    
    public enum Direction
    {
        North,   
        NorthEast,  
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public enum FOV
    {
        Permissive = 1,
        Medium = 2,
        Restricted = 3
    }

    public struct Vector2i
    {
        public int X;
        public int Y;

        public Vector2i(int x, int y) { X = x; Y = y; }
        public Vector2i(Vector2i other) { X = other.X; Y = other.Y; }

        public static implicit operator Vector2i(Vector2 other) { return new Vector2i((int)other.X, (int)other.Y); }

        public static Vector2i operator +(Vector2i first, Vector2i second)
        {
            return new Vector2i(first.X + second.X, first.Y + second.Y);
        }

        public static Vector2i operator -(Vector2i first, Vector2i second)
        {
            return new Vector2i(first.X - second.X, first.Y - second.Y);
        }

        public static bool operator ==(Vector2i first, Vector2i second)
        {
            return first.X == second.X && first.Y == second.Y;
        }

        public static bool operator !=(Vector2i first, Vector2i second)
        {
            return first.X != second.X || first.Y != second.Y;
        }

        public override string ToString()
        {
            return "(" + X + "|" + Y + ")";
        }
    }

    public delegate void TurnOverHandler(int entity);
    public delegate void UpdateTargetLineHandler();

    static class Util
    {
        public static event TurnOverHandler TurnOverEvent;
 
        public static SpriteFont DefaultFont { get; set; } = null;
        public static SpriteFont SmallFont { get; set; } = null;
        public static SpriteFont BigFont { get; set; } = null;
        public static string ContentPath { get; set; } = "";

        // size of virtual screen (disregarding resizing)
        public static int ScreenWidth { get; } = 1280;
        public static int ScreenHeight { get; } = 720;

        // virtual size of world (or rather view that is displayed)
        public static int TileSize { get; } = 20;
        public static int WorldWidth { get; } = 800;
        public static int WorldHeight { get; } = 500;

        public static int PlayerID { get; set; } = 0;
        public static Floor CurrentFloor { get; set; } = null;
        public static bool PlayerTurnOver { get; set; } = false;

        public static int TargetIndicatorID { get; set; } = 0;

        public static FOV FOV = FOV.Medium;   
    
        // gets called for every new type of component/entity/...
        public static class TypeID<T>
        {
            static int counter = 0;
            public static int Get()
            {             
                return ++counter;
            }
        }

        // gets called for every new instance of component/entity/... respectively
        public static class UniqueID<T>
        {
            static int counter = 0;
            public static int Get()
            {
                return ++counter;
            }
        }

        // transforms world position to screen position based on tile size
        public static Vector2 WorldToScreenPosition(Vector2 worldPos)
        {
            return new Vector2(worldPos.X * TileSize, worldPos.Y * TileSize);
        }

        // returns Direction 180 degrees from param direction
        public static Direction getOppositeDirection(Direction direction)
        {
            int nrDirections = 8;
            return (Direction)(((int)direction + nrDirections / 2) % nrDirections);
        }

        // returns a "unit" vector in Direction dir
        public static Vector2 GetUnitVectorInDirection(Direction dir)
        {
            switch(dir)
            {
                case Direction.North:
                    return new Vector2(0, -1);

                case Direction.East:
                    return new Vector2(1, 0);

                case Direction.South:
                    return new Vector2(0, 1);

                case Direction.West:
                    return new Vector2(-1, 0);

                default:
                    Console.WriteLine("No vector known for " + dir);
                    return new Vector2(0, 0);
            }
        }

        public static void TurnOver(int entity)
        {
            //Log.Message("Turn over for " + DescriptionSystem.GetNameWithID(entity));

            CurrentFloor.CalculateTileVisibility();

            TurnOverEvent?.Invoke(entity);

            if (entity == PlayerID)
                PlayerTurnOver = true;
        }

        public static string SerializeObject(object obj, bool indented = false, bool saveTypeNames = true)
        {
            var settings = new JsonSerializerSettings();
            if (indented) settings.Formatting = Formatting.Indented;
            if (saveTypeNames) settings.TypeNameHandling = TypeNameHandling.Auto;
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T DeserializeObject<T>(string obj)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            return JsonConvert.DeserializeObject<T>(obj, settings);
        }

        public static void UpdateTargetLine()
        {
            var targetPos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;
            // calculate line to target indicator
            var line = CurrentFloor.GetLine(
                EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position,
                targetPos);

            if(line.Count > 1)
            {
                line.RemoveAt(0); // don't draw square on player unless targeting self
            }

            // remove any old sprites
            EntityManager.RemoveAllComponentsOfType(Util.TargetIndicatorID, RenderableSpriteComponent.TypeID);

            List<IComponent> updatedSprites = new List<IComponent>();

            foreach(var pos in line)
            {
                updatedSprites.Add(new RenderableSpriteComponent()
                {
                    Texture = "square",
                    Tint = new Color(Color.Gold, 0.7f),
                    Position = Util.WorldToScreenPosition(pos)
                });
            }

            // add "crosshair"
            updatedSprites.Add(new RenderableSpriteComponent()
            {
                Texture = "targetIndicator",
                Position = Util.WorldToScreenPosition(targetPos)
            });

            // add updated sprites to entity
            EntityManager.AddComponents(Util.TargetIndicatorID, updatedSprites);
         
        }
    }
}
