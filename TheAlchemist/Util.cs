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

    public struct Array1D<T>
    {
        private T[] data;
        private int width;
        private int height;

        public Array1D(T[] data, int width, int height)
        {
            if(data.Length != width * height)
            {
                throw new ArgumentOutOfRangeException("data", "Data array needs to be of length width * height!");
            }

            this.data = data;
            this.width = width;
            this.height = height;
        }

        public Array1D(int width, int height)
        {
            data = new T[width * height];
            this.width = width;
            this.height = height;
        }

        public T this[int x, int y]
        {
            get => data[y * width + x];
            set => data[y * width + x] = value;
        }
            

        public T this[int i]
        {
            get => data[i];
            set => data[i] = value;
        }

        public T[] GetData()
        {
            return data;
        }
    }

    // marks a position in the game world
    [JsonObject]
    public struct Position
    {
        public int X;
        public int Y;

        public Position(int x, int y) { X = x; Y = y; }
        public Position(Position other) { X = other.X; Y = other.Y; }

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        public static readonly Position Zero = new Position(0, 0);
        public static readonly Position One = new Position(1, 1);

        public static readonly Position Up = new Position(0, -1);
        public static readonly Position Right = new Position(1, 0);
        public static readonly Position Down = new Position(0, 1);
        public static readonly Position Left = new Position(-1, 0);

        public Position[] GetNeighbours()
        {
            Position[] result = new Position[4];
            result[0] = this + Up;
            result[1] = this + Right;
            result[2] = this + Down;
            result[3] = this + Left;
            return result;
        }

        public static Position[] HexDirections =
        {
            Up,
            Up + Right,
            Right,
            Down,
            Down + Left,
            Left
        };

        public Position(Point point)
        {
            (X, Y) = point;
        }

        public Position[] GetNeighboursHexPointyTop()
        {
            Position[] result = new Position[6];
            result[0] = this + Up;
            result[1] = this + Up + Right;
            result[2] = this + Right;
            result[3] = this + Down;
            result[4] = this + Down + Left;
            result[5] = this + Left;
            return result;
        }

        public Position[] HexCircle(Position direction, int circumference)
        {

            // TODO: start from existing line + (counter)clockwise

            var result = new Position[circumference];
            int stepDirectionIndex = Array.IndexOf(HexDirections, direction);

            Position step = direction;
            int maxPool = circumference;
            int pool = maxPool;

            if (circumference % 6 == 5)
                pool -= 2;

            Position cur = this;
            for (int i = 0; i < circumference; i++)            
            {
                result[i] = cur;
                cur += step;
                pool -= 6;
                while(pool <= 0)
                {
                    stepDirectionIndex = (stepDirectionIndex + 1) % 6;
                    step = HexDirections[stepDirectionIndex];
                    pool += maxPool;
                }
            }
            return result;
        }

        public Position[] HexCircle(Position[] alreadyPlaced, int circumference, int clockwiseIn = 0)
        {
            int prevIndex = 0;
            int maxPool = circumference;
            int pool = maxPool;
            int clockwise = clockwiseIn == 0 ? 1 : clockwiseIn;
            Position step;
            for (int i = 0; i < alreadyPlaced.Length - 1; i++)
            {
                step = alreadyPlaced[i + 1] - alreadyPlaced[i];
                pool -= 6;
                if(i == 0)
                {
                    prevIndex = Array.IndexOf(HexDirections, step);
                }
                else
                {
                    int neighbourIndex = Array.IndexOf(HexDirections, step);
                    if(neighbourIndex - prevIndex > 2)
                    {
                        clockwise = -1;
                    }
                    int rotations = Math.Abs(prevIndex - (clockwise > 0 ? neighbourIndex : 6 - neighbourIndex));
                    pool += rotations * maxPool;
                    prevIndex = neighbourIndex;
                }
            }
            List<Position> result = new List<Position>(alreadyPlaced);
            Position cur = alreadyPlaced.Length > 0 ? alreadyPlaced[alreadyPlaced.Length - 1] : this;
            int stepDirectionIndex = prevIndex;
            step = HexDirections[prevIndex];
            for (int i = alreadyPlaced.Length; i < circumference; i++)
            {           
                while (pool <= 0)
                {
                    stepDirectionIndex = (stepDirectionIndex + (clockwise > 0 ? 1 : 5)) % 6;
                    step = HexDirections[stepDirectionIndex];
                    pool += maxPool;
                }
                cur += step;
                result.Add(cur);
                pool -= 6;          
            }
            return result.ToArray();
        }

        public static implicit operator Position(Vector2 other) { return new Position((int)other.X, (int)other.Y); }

        public static Position operator +(Position first, Position second)
        {
            return new Position(first.X + second.X, first.Y + second.Y);
        }

        public static Position operator -(Position first, Position second)
        {
            return new Position(first.X - second.X, first.Y - second.Y);
        }

        public static Position operator *(Position pos, int factor)
        {
            return new Position(pos.X * factor, pos.Y * factor);
        }

        public static Position operator *(int factor, Position pos)
        {
            return new Position(pos.X * factor, pos.Y * factor);
        }
        public static Position operator *(Position first, Position second)
        {
            return new Position(first.X * second.X, first.Y * second.Y);
        }

        public static bool operator ==(Position first, Position second)
        {
            return first.X == second.X && first.Y == second.Y;
        }

        public static bool operator !=(Position first, Position second)
        {
            return first.X != second.X || first.Y != second.Y;
        }

        public override string ToString()
        {
            return "(" + X + "|" + Y + ")";
        }

        public Direction ToDirection()
        {
            if (this == Up)
                return Direction.North;
            if (this == Right)
                return Direction.East;
            if (this == Down)
                return Direction.South;
            if (this == Left)
                return Direction.West;

            Log.Error("Could not transform Position to Direction: " + this.ToString());
            return Direction.North;
        }
    }

    public delegate void TurnOverHandler(int entity);
    public delegate void UpdateTargetLineHandler();

    static class Util
    {
        public static event TurnOverHandler TurnOverEvent;

        public static int TurnCount = 0;

        public static bool ErrorOccured = false;
        // ignores errors when on
        public static bool BrutalModeOn = false;

        public static SpriteFont DefaultFont { get; set; } = null;
        public static SpriteFont SmallFont { get; set; } = null;
        public static SpriteFont BigFont { get; set; } = null;
        public static SpriteFont MonospaceFont { get; set; } = null;
        public static string ContentPath { get; set; } = "";

        // size of virtual screen (disregarding resizing)
        public static int ScreenWidth { get; } = 1280;
        public static int ScreenHeight { get; } = 720;

        // virtual size of world (or rather view that is displayed)
        public static int TileSize { get; } = 20;
        public static int WorldViewPixelWidth { get; } = 820;
        public static int WorldViewPixelHeight { get; } = 500;

        // size of world view in tiles
        public static int WorldViewTileWidth { get; } = 41;
        public static int WorldViewTileHeight { get; } = 25;

        public static int PlayerID { get; set; } = 0;
        public static Floor CurrentFloor { get; set; } = null;
        public static bool PlayerTurnOver { get; set; } = false;

        public static int TargetIndicatorID { get; set; } = 0;

        public static FOV FOV = FOV.Medium;

        public static Color[] Colors =
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Yellow,
            Color.Violet,
            Color.Orange,
            Color.Beige,
            Color.Turquoise
        };

        public static GraphicsDevice GraphicsDevice = null;

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

        public static string GetStringFromEnumerable<T>(IEnumerable<T> list)
        {
            string result = "[";
            foreach (var elem in list)
            {
                result += elem.ToString() + ",";
            }
            return result.Substring(0, result.Length - 1) + "]";
        }

        public static string GetStringFromCollection(System.Collections.ICollection collection)
        {
            string result = "[";
            foreach (var elem in collection)
            {
                result += elem.ToString() + ",";
            }
            return result.Substring(0, result.Length - 1) + "]";
        }

        public static T PickRandomElementWeighted<T>(SortedList<int, T> list)
        {
            if(list == null)
            {
                string message = "Util.GetRandomElementWeighted: Input list is null!";
                Log.Error(message);
                throw new ArgumentNullException(message);
            }

            if(list.Count == 0)
            {
                string message = "Util.GetRandomElementWeighted: Empty input list!";
                Log.Error(message);
                throw new ArgumentException(message);
            }

            int max = list.Last().Key;
            int r = Game.Random.Next(max);
            foreach (var pair in list)
            {
                if(r < pair.Key)
                {
                    return pair.Value;
                }
            }
            throw new ArgumentException("W T F did you do? This SortedList is not sorted!!!");
        }

        // transforms world position to screen position based on tile size
        public static Vector2 WorldToScreenPosition(Position worldPos)
        {
            return new Vector2(worldPos.X * TileSize, worldPos.Y * TileSize);
        }

        public static Position GetPlayerPos()
        {
            return EntityManager.GetComponent<TransformComponent>(PlayerID).Position;
        }

        // returns Direction 180 degrees from param direction
        public static Direction GetOppositeDirection(Direction direction)
        {
            int nrDirections = 8;
            return (Direction)(((int)direction + nrDirections / 2) % nrDirections);
        }

        // returns a "unit" vector in Direction dir
        public static Position GetUnitVectorInDirection(Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return new Position(0, -1);

                case Direction.East:
                    return new Position(1, 0);

                case Direction.South:
                    return new Position(0, 1);

                case Direction.West:
                    return new Position(-1, 0);

                default:
                    Log.Error("No vector known for " + dir);
                    return new Position(0, 0);
            }
        }

        public static void TurnOver(int entity)
        {
            //Log.Message("Turn over for " + DescriptionSystem.GetNameWithID(entity));
            var health = EntityManager.GetComponent<HealthComponent>(entity);

            // entity dead, don't do anything
            if (health.Amount <= 0)
            {
                return;
            }

            TurnOverEvent?.Invoke(entity);

            if (entity == PlayerID)
            {
                CurrentFloor.CalculateTileVisibility();
                LocationSystem.UpdateDistanceMap(PlayerID);
                PlayerTurnOver = true;
                TurnCount++;
            }
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
            try
            {
                return JsonConvert.DeserializeObject<T>(obj, settings);
            }
            catch (JsonException e)
            {
                Log.Error("Deserialization went wrong! " + typeof(T), false);
                Log.Error(e.Message);
                return default;
            }
        }

        // set init to true if targeting mode has just been entered
        // otherwise it will produce a warning at runtime because 
        // this function tries to remove old sprites (which there are none of)
        /*public static void UpdateTargetLine(bool init = false)
        {
            var targetPos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;
            // calculate line to target indicator
            var line = CurrentFloor.GetLine(
                EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position,
                targetPos);

            if (line.Count > 1)
            {
                line.RemoveAt(0); // don't draw square on player unless targeting self
            }

            // remove any old sprites
            if (!init)
            {
                EntityManager.RemoveAllComponentsOfType(Util.TargetIndicatorID, RenderableSpriteComponent.TypeID);
            }

            List<IComponent> updatedSprites = new List<IComponent>();

            foreach (var pos in line)
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

        }*/

        public static int Sign(bool b)
        {
            return b ? 1 : -1;
        }

        public static int Lerp(int x, int x0, int y0, int x1, int y1)
        {
            //int deltaY = y1 - y0;
            //int deltaX = x1 - x0;
            //float slope = (float)deltaY / deltaX;
            //int distance = x - x0;
            //int res = (int)(y0 + slope * distance);

            //Log.Message("Lerp: " + x + "|? --> " + new Position(x0, y0) + " ~~ " + new Position(x1, y1));
            //Log.Message("Slope: " + deltaY + "/" + deltaX + " = " + slope);
            //Log.Message("Result: " + x0 + " + " + slope + "*" + distance + " = " + res);
            //return res;

            return y0 + (int)Math.Round(((float)(y1 - y0) / (x1 - x0)) * (x - x0));
        }

        public static T PickRandomElement<T>(List<T> list)
        {
            return list[Game.Random.Next(0, list.Count)];
        }

        public static T PickRandomElement<T>(T[] arr)
        {
            return arr[Game.Random.Next(0, arr.Length)];
        }

        // based on Fisher-Yates shuffle
        public static List<T> Shuffle<T>(IEnumerable<T> enumerable)
        {          
            List<T> list = enumerable.ToList();
            for (int n = list.Count() - 1; n >= 0; n--)
            {
                int r = Game.Random.Next(n + 1);
                T value = list[r];
                list[r] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static InventoryComponent GetPlayerInventory()
        {
            return EntityManager.GetComponent<InventoryComponent>(PlayerID);
        }

        public static int GetCurrentItem()
        {
            UI.SyncInventoryCursor();
            int cursorPos = UI.InventoryCursorPosition;
            var inventory = EntityManager.GetComponent<InventoryComponent>(InputManager.Instance.ControlledEntity);

            if (inventory.Items.Count == 0)
            {
                //UISystem.Message("Your inventory is empty!");
                return 0;
            }

            try
            {
                return inventory.Items[cursorPos - 1];
            }
            catch (ArgumentOutOfRangeException)
            {
                Log.Error("Inventory cursor position corrupted!" + DescriptionSystem.GetNameWithID(InputManager.Instance.ControlledEntity));
                return 0;
            }
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(max) > 0)
                return max;
            if (value.CompareTo(min) < 0)
                return min;
            return value;
        }

        public static void FillArray2D<T>(T[,] array, T value)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    array[x, y] = value;
                }
            }
        }

        /// <summary>
        /// Tries to add a new key/value pair to the dictionary
        /// if the key already exists, the new value is added to the existing one 
        /// </summary>
        /// <typeparam name="K"> Key type </typeparam>
        /// <param name="dict"> dictionary to modify </param>
        /// <param name="key"> key where value should be added</param>
        /// <param name="newVal"> new value that should be added </param>
        public static void AddOrIncrease<K>(this Dictionary<K, int> dict, K key, int newVal)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += newVal;
            }
            else
            {
                dict[key] = newVal;
            }
        }

        public static int ChangeValueByPercentage(int value, int percent)
        {
            if (percent == 0)
            {
                return value;
            }

            float ratio = 1f + percent / 100f;

            return (int)Math.Round(value * ratio);
        }

        public static T[,] FlipHorizontally<T>(this T[,] matrix)
        {
            int width = matrix.GetLength(0);
            int height = matrix.GetLength(1);

            T[,] result = new T[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int mirroredIndex = width - 1 - x;
                    result[x, y] = matrix[mirroredIndex, y];
                }
            }

            return result;
        }

        public static T[,] Make2D<T>(this T[] source, int width, int height)
        {
            T[,] result = new T[width, height];

            if(source.Length != width * height)
            {
                throw new ArgumentOutOfRangeException("source", "Source array needs to be of length width * height!");
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = y * width + x;
                    result[x, y] = source[sourceIndex];
                }
            }

            return result;
        }

        public static T[] Make1D<T>(this T[,] source)
        {
            int width = source.GetLength(0);
            int height = source.GetLength(1);

            T[] result = new T[width * height];          

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int resultIndex = y * width + x;
                    result[resultIndex] = source[x, y];
                }
            }

            return result;
        }
    }
}
