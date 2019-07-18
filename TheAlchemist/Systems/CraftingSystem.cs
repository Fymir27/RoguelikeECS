using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    using static TheAlchemist.Components.CraftingMaterialComponent;

    public enum CraftingType
    {
        Alchemy,
        Engineering
    }

    public enum Craftable
    {
        Potion,
        Weapon
        //...
    }

    public delegate void AddCraftingIngredientHandler(int item);
    public delegate void CraftItemHandler();
    public delegate void ResetCraftingHandler();

    struct Ingredient
    {
        public int Min;
        public int Max;
        public MaterialType Type;
        public MaterialType[] Types;

        public Ingredient(int min, int max, MaterialType type, MaterialType[] types = null)
        {
            Min = min;
            Max = max;
            Type = type;
            Types = types;
        }
    }

    struct CraftingData
    {

    }

    struct CraftingRecipe
    {
        MaterialType[] coreIngredients;
        MaterialType[] optionalIngredients;
    }

    class CraftingSystem : Singleton<CraftingSystem>
    {
        List<int> items;
        List<CraftingMaterialComponent> ingredients;
        int maxSlots;

        Dictionary<Craftable, List<Ingredient>> recipes = new Dictionary<Craftable, List<Ingredient>>()
        {
            {
                Craftable.Potion, new List<Ingredient>()
                {
                    new Ingredient(1, 0, MaterialType.Potion),
                    new Ingredient(1, 0, MaterialType.Potion, new MaterialType[] { }),
                    new Ingredient() { Types = new MaterialType[] { MaterialType.Plant, MaterialType.Mineral } }
                }
            }
        };

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
        }

        public void CraftItem()
        {
            if (ingredients.Count < 2)
            {
                UISystem.Message("You need to put in at least two items!");
                return;
            }

            // TODO: other crafting than alchemy
            int newItem = Alchemy();

            if (newItem == 0)
            {
                // problems should be displayed 
                // the crafting functions
                return;
            }

            UISystem.Message("You just crafted something!");

            items.Clear();
            ingredients.Clear();
            var description = EntityManager.GetComponent<DescriptionComponent>(newItem);
            description.Name = "Crafted " + description.Name;
            Util.GetPlayerInventory().Items.Add(newItem);

            string info = DescriptionSystem.GetDebugInfoEntity(newItem);
            Log.Message("Crafting successful:");
            Log.Data(info);
        }

        /// <summary>
        /// try to craft something using alchemy
        /// </summary>
        /// <returns> ID of newly crafted item (or 0 on fail) </returns>
        public int Alchemy()
        {
            if (!ingredients.Any(i => i.Type == MaterialType.Potion))
            {
                UISystem.Message("You need at least a potion (or water)!");
                return 0;
            }

            MaterialType[] possibleIngredients =
            {
                MaterialType.Potion,
                MaterialType.Plant,
                MaterialType.Mineral
            };

            foreach (var ing in ingredients)
            {
                if (!possibleIngredients.Contains(ing.Type))
                {
                    UISystem.Message("You can't use that ingredient: " + DescriptionSystem.GetName(ing.EntityID));
                    return 0;
                }
            }

            List<UsableItemComponent.ItemEffect> resultEffects = new List<UsableItemComponent.ItemEffect>();
            List<UsableItemComponent.ItemEffect> ingredientEffects = new List<UsableItemComponent.ItemEffect>();

            // extract all the item effects form ingredients
            foreach (var ing in ingredients)
            {
                foreach (var effect in ing.Effects)
                {
                    ingredientEffects.Add(effect);
                }
            }

            // mix all effects together
            while (ingredientEffects.Any())
            {
                var curEffectType = ingredientEffects[0].Type;

                var effectsOfSameType = ingredientEffects.Where(e => e.Type == curEffectType).ToArray();

                int combinedPotency = 0;

                foreach (var effect in effectsOfSameType)
                {
                    // how much each individual ingredient changes the potency
                    float individualMultiplier = 1.0f;

                    // add individual potency if effect is beneficial
                    // subtract individual potency if effect is harmful
                    combinedPotency += (int)Math.Round(effect.Potency * Util.Sign(!effect.Harmful) * individualMultiplier);
                }

                // remove effects because they are now already accounted for
                ingredientEffects.RemoveAll(e => e.Type == curEffectType);

                UsableItemComponent.ItemEffect combinedEffect = new UsableItemComponent.ItemEffect()
                {
                    Type = curEffectType,
                    Harmful = combinedPotency < 0,
                    Potency = Math.Abs(combinedPotency)
                };

                resultEffects.Add(combinedEffect);
            }

            // TODO: craft other alchemy items?
            return CreatePotion(resultEffects);
        }

        public int CreatePotion(IEnumerable<UsableItemComponent.ItemEffect> effects)
        {
            int potion = GameData.Instance.CreateItem("templatePotion");
            var usableComponent = EntityManager.GetComponent<UsableItemComponent>(potion);
            usableComponent.Effects.AddRange(effects);
            var craftingComponent = EntityManager.GetComponent<CraftingMaterialComponent>(potion);
            craftingComponent.Effects.AddRange(effects);
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
