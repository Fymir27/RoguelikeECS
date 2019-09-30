using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    using static TheAlchemist.Components.CraftingMaterialComponent;

    //public enum CraftingType
    //{
    //    Alchemy,
    //    Engineering
    //}

    //public enum Craftable
    //{
    //    Potion,
    //    Weapon
    //    //...
    //}

    public delegate void AddCraftingIngredientHandler(int item);
    public delegate bool CraftItemHandler();
    public delegate void ResetCraftingHandler();

    //struct Ingredient
    //{
    //    public int Min;
    //    public int Max;
    //    public MaterialType Type;
    //    public MaterialType[] Types;

    //    public Ingredient(int min, int max, MaterialType type, MaterialType[] types = null)
    //    {
    //        Min = min;
    //        Max = max;
    //        Type = type;
    //        Types = types;
    //    }
    //}

    //struct CraftingData
    //{

    //}

    //struct CraftingRecipe
    //{
    //    MaterialType[] coreIngredients;
    //    MaterialType[] optionalIngredients;
    //}

    class CraftingSystem : Singleton<CraftingSystem>
    {
        List<int> items;
        //List<CraftingMaterialComponent> ingredients;
        List<SubstanceComponent> substances;
        int maxSlots;

        //Dictionary<Craftable, List<Ingredient>> recipes = new Dictionary<Craftable, List<Ingredient>>()
        //{
        //    {
        //        Craftable.Potion, new List<Ingredient>()
        //        {
        //            new Ingredient(1, 0, MaterialType.Potion),
        //            new Ingredient(1, 0, MaterialType.Potion, new MaterialType[] { }),
        //            new Ingredient() { Types = new MaterialType[] { MaterialType.Plant, MaterialType.Mineral } }
        //        }
        //    }
        //};

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

            //ingredients.Add(craftingMaterial);

            //var craftingMaterial = EntityManager.GetComponent<CraftingMaterialComponent>(item);

            //if (craftingMaterial == null)
            //{
            //    UISystem.Message("This item can't be used for crafting!");
            //    return;
            //}

            //// remember item and remove it from player inventory
            //items.Add(item);
            //var inv = Util.GetPlayerInventory();
            //inv.Items.Remove(item);

            //ingredients.Add(craftingMaterial);
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
            var description = EntityManager.GetComponent<DescriptionComponent>(newItem);
            description.Name = "Crafted " + description.Name;
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

            return CreatePotion(accumulatedProperties);

            //MaterialType[] possibleIngredients =
            //{
            //    MaterialType.Potion,
            //    MaterialType.Plant,
            //    MaterialType.Mineral
            //};

            //foreach (var ing in ingredients)
            //{
            //    if (!possibleIngredients.Contains(ing.Type))
            //    {
            //        UISystem.Message("You can't use that ingredient: " + DescriptionSystem.GetName(ing.EntityID));
            //        return 0;
            //    }
            //}

            //List<UsableItemComponent.ItemEffect> resultEffects = new List<UsableItemComponent.ItemEffect>();
            //List<UsableItemComponent.ItemEffect> ingredientEffects = new List<UsableItemComponent.ItemEffect>();

            //// extract all the item effects form ingredients
            //foreach (var ing in ingredients)
            //{
            //    foreach (var effect in ing.Effects)
            //    {
            //        ingredientEffects.Add(effect);
            //    }
            //}

            //// mix all effects together
            //while (ingredientEffects.Any())
            //{
            //    var curEffectType = ingredientEffects[0].Type;

            //    var effectsOfSameType = ingredientEffects.Where(e => e.Type == curEffectType).ToArray();

            //    int combinedPotency = 0;

            //    foreach (var effect in effectsOfSameType)
            //    {
            //        // how much each individual ingredient changes the potency
            //        float individualMultiplier = 1.0f;

            //        // add individual potency if effect is beneficial
            //        // subtract individual potency if effect is harmful
            //        combinedPotency += (int)Math.Round(effect.Potency * Util.Sign(!effect.Harmful) * individualMultiplier);
            //    }

            //    // remove effects because they are now already accounted for
            //    ingredientEffects.RemoveAll(e => e.Type == curEffectType);

            //    UsableItemComponent.ItemEffect combinedEffect = new UsableItemComponent.ItemEffect()
            //    {
            //        Type = curEffectType,
            //        Harmful = combinedPotency < 0,
            //        Potency = Math.Abs(combinedPotency)
            //    };

            //    resultEffects.Add(combinedEffect);
            //}

            //// TODO: craft other alchemy items?
            //return CreatePotion(resultEffects);
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

        //public int CreatePotion(IEnumerable<UsableItemComponent.ItemEffect> effects)
        //{
        //    int potion = GameData.Instance.CreateItem("templatePotion");
        //    var usableComponent = EntityManager.GetComponent<UsableItemComponent>(potion);
        //    usableComponent.Effects.AddRange(effects);
        //    var craftingComponent = EntityManager.GetComponent<CraftingMaterialComponent>(potion);
        //    craftingComponent.Effects.AddRange(effects);
        //    return potion;
        //}

        public int CreatePotion(Dictionary<Property, int> properties)
        {
            int potion = GameData.Instance.CreateItem("templatePotion");
            var substance = EntityManager.GetComponent<SubstanceComponent>(potion);
            substance.Properties = properties;

            // to suppress warning of rendersystem (default is true)
            EntityManager.GetComponent<RenderableSpriteComponent>(potion).Visible = false;

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
