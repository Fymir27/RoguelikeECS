using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{  
    abstract class CraftableComponent : Component<CraftableComponent>
    {
        private static int materialIDCounter = 1;

        public static int GetNewMaterialID()
        {
            return materialIDCounter++;
        }

        public static void foo()
        {
            //List<List<>
            List<IMaterial> ingredients = new List<IMaterial>()
            {
                new Liquid(),
                new Plant(),
                new Liquid(),
                new Metal()
            };

            foreach (var ingredient in ingredients)
            {
                string name = "";
                switch(ingredient)
                {
                    case Plant p:
                        name = "plant";
                        break;

                    case Liquid l:
                        name = "liquid";
                        break;

                    default:
                        name = "unknown";
                        break;
                }
                Console.WriteLine("This ingredient is of type " + name);
            }
        }
    }

    interface IMaterial
    {
        int GetID();
    }

    abstract class Material<T> : IMaterial
    {
        private static int materialTypeID = CraftableComponent.GetNewMaterialID();

        public int GetID()
        {
            return materialTypeID;
        }

        protected Material()
        {
            Console.WriteLine(GetType() + "-> ID: " + materialTypeID);
        }
    }

    class Liquid : Material<Liquid>
    {
        public int PH { get; set; }
    }

    class Metal : Material<Metal>
    {
        public int Hardness { get; set; }
        public int Conductivity { get; set; }
        public bool Magnetic { get; set; }
        public bool Noble { get; set; }
    }

    class Plant : Material<Plant>
    {
        public bool Poisonous;
    }


}
