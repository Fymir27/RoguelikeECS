using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    using static TheAlchemist.Components.CraftingMaterialComponent;

    public delegate void AddCraftingIngredientHandler(int item);
    public delegate bool CraftItemHandler();
    public delegate void ResetCraftingHandler();

    class CraftingSystem : Singleton<CraftingSystem>
    {
        List<int> items;
        //List<CraftingMaterialComponent> ingredients;
        List<SubstanceComponent> substances;
        int maxSlots;

        public CraftingSystem(int slots = 5)
        {
            //ingredients = new List<CraftingMaterialComponent>();
            substances = new List<SubstanceComponent>();
            items = new List<int>();
            maxSlots = slots;
        }

        public void ResetCrafting()
        {
            Util.GetPlayerInventory().Items.AddRange(items);
            items.Clear();
            substances.Clear();
        }

        public void AddIngredient(int item)
        {
            if (items.Count >= maxSlots)
            {
                UISystem.Message("You can't use that many ingredients at once! (max. " + maxSlots + ")");
                return;
            }

            var substance = EntityManager.GetComponent<SubstanceComponent>(item);

            if (substance == null)
            {
                UISystem.Message("This item can not be used for crafting!");
                return;
            }

            // remember item and remove it from player inventory
            items.Add(item);
            var inv = Util.GetPlayerInventory();
            inv.Items.Remove(item);
        }

        /// <summary>
        /// Crafts an item from the added ingredients
        /// </summary>
        /// <returns>true if item was succesfully created</returns>
        public bool CraftItem()
        {
            if (items.Count < 2)
            {
                UISystem.Message("You need to put in at least two items!");
                ResetCrafting();
                return false;
            }

            ExtractSubstances();

            // TODO: other crafting than alchemy
            int newItem = Alchemy();

            if (newItem == 0)
            {
                // something went wrong before and 
                // should have already been handled
                ResetCrafting();
                return false;
            }

            UISystem.Message("You just crafted something!");

            foreach (var item in items)
            {
                EntityManager.RemoveEntity(item);
            }
            items.Clear();
            substances.Clear();
            //var description = EntityManager.GetComponent<DescriptionComponent>(newItem);
            //description.Name = "Crafted " + description.Name;
            Util.GetPlayerInventory().Items.Add(newItem);

            string info = DescriptionSystem.GetDebugInfoEntity(newItem);
            Log.Message("Crafting successful:");
            Log.Data(info);

            return true;
        }

        /// <summary>
        /// try to craft something using alchemy
        /// assumes all the substances are already extracted
        /// </summary>
        /// <returns> ID of newly crafted item (or 0 on fail) </returns>
        public int Alchemy()
        {
            // TODO: check if there's at least some liquid in the recipe or something

            Dictionary<Property, int> accumulatedProperties = new Dictionary<Property, int>();

            foreach (var substance in substances)
            {
                foreach (var prop in substance.Properties)
                {
                    if (accumulatedProperties.ContainsKey(prop.Key))
                    {
                        accumulatedProperties[prop.Key] += prop.Value; // TODO: reduce weight of ingredients?
                    }
                    else
                    {
                        accumulatedProperties[prop.Key] = prop.Value;
                    }
                }
            }

            int potion = CreatePotion(accumulatedProperties);

            // let the player learn about the created potion's properties
            var knowledge = EntityManager.GetComponent<SubstanceKnowledgeComponent>(Util.PlayerID);

            foreach (var prop in accumulatedProperties.Keys)
            {
                knowledge.PropertyKnowledge[prop] += 10;
                knowledge.TypeKnowledge[prop.GetPropType()] += 5;
            }

            // make sure the properties of the created potion aren't displayed by default
            EntityManager.GetComponent<SubstanceComponent>(potion).PropertiesKnown = false;

            return potion;          
        }

        /// <summary>
        /// extracts substances from already added ingredient items
        /// and saves them into this.substances
        /// </summary>
        void ExtractSubstances()
        {
            substances.Clear();
            foreach (var item in items)
            {
                substances.Add(EntityManager.GetComponent<SubstanceComponent>(item));
            }
        }

        public int CreatePotion(Dictionary<Property, int> properties)
        {
            int potion = GameData.Instance.CreateItem("water");
            var substance = EntityManager.GetComponent<SubstanceComponent>(potion);
            substance.Properties = properties;

            // to suppress warning of rendersystem (default is true)
            EntityManager.GetComponent<RenderableSpriteComponent>(potion).Visible = false;

            var descriptionC = EntityManager.GetComponent<DescriptionComponent>(potion);

            descriptionC.Name = "Crafted Potion";
            descriptionC.Description = "What have you brewed up this time?";

            return potion;
        }

        public List<string> GetIngredientNames()
        {
            List<string> names = new List<string>();
            foreach (var item in items)
            {
                names.Add(DescriptionSystem.GetName(item));
            }
            return names;
        }
    }
}
