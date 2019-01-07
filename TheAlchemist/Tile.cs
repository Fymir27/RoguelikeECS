using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    class Tile
    {
        public int Terrain;
        public int Character;
        public List<int> Items;
        public bool Discovered;

        public Tile(int terrain = 0, int character = 0, List<int> items = null)
        {
            Terrain = terrain;
            Character = character;
            Items = items;
            Discovered = false;
        }
    }

    /*
    class Tile
    {
        public enum Type
        {
            Floor,
            Wall,
            Stair,
            Door
        }

        Type type;
        //Item[] items;
        
        public Tile()
        {
            type = (Type)Game.Random.Next(0, 3);
        }

        public Tile(char c)
        {
            switch (c)
            {
                case '#':
                    type = Type.Wall;
                    break;

                case ' ':
                    type = Type.Floor;
                    break;

                default:
                    Console.WriteLine("Tile::ctor Unknown character!");
                    type = (Type)Game.Random.Next(0, 3);
                    break;
            }
        }

        public override string ToString()
        {
            return ((int)type).ToString();
        }
    }
    */
}
