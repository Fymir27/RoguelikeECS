using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public enum Craftable
    {
        Potion,
        Weapon
        //...
    }

    public delegate void AddCraftingIngredientHandler(int item);
    public delegate void CraftItemHandler();
    public delegate void ResetCraftingHandler();

    class CraftingSystem : Singleton<CraftingSystem>
    {
        List<int> items;
        List<CraftingMaterialComponent> ingredients;
        int maxSlots;

        public CraftingSystem(int slots = 5)
        {
            ingredients = new List<CraftingMaterialComponent>();
            items = new List<int>();
            maxSlots = slots;
        }

        public void ResetCrafting()
        {
            Util.GetPlayerInventory().Items.AddRange(items);
            items.Clear();
            ingredients.Clear();
        }

        public void AddIngredient(int item)
        {
            if (items.Count >= maxSlots)
            {
                UISystem.Message("You can't use that many ingredients at once! (max. " + maxSlots + ")");
                return;
            }

            var craftingMaterial = EntityManager.GetComponent<CraftingMaterialComponent>(item);

            if (craftingMaterial == null)
            {
                UISystem.Message("This item can't be used for crafting!");
                return;
            }

            // remember item and remove it from player inventory
            items.Add(item);
            var inv = Util.GetPlayerInventory();
            inv.Items.Remove(item);

            ingredients.Add(craftingMaterial);
            Console.Out.WriteLine("Ingredient added!" + Util.GetStringFromEnumerable(ingredients));
        }

        public void CraftItem()
        {
            // check if anything is craftable
            UISystem.Message("You just crafted something!");
            int newItem = GameData.Instance.CreateItem("healthPotion");
            items.Clear();
            ingredients.Clear();
            var description = EntityManager.GetComponent<DescriptionComponent>(newItem);
            description.Name = "Crafted " + description.Name;
            Util.GetPlayerInventory().Items.Add(newItem);
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
