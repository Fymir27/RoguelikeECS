using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    // should return true if interaction was successful
    public delegate bool InteractionHandler(int actor, int other);

    class InteractionSystem
    {
        public bool HandleInteraction(int actor, int other)
        {          
            var interactable = EntityManager.GetComponent<InteractableComponent>(other);

            if(interactable == null)
            {
                Log.Error("HandleInteraction called on non-interactable object!");
                Log.Data(DescriptionSystem.GetDebugInfoEntity(other));
                return false;
            }

            Log.Message("Interaction between " + DescriptionSystem.GetNameWithID(actor) + " and " + DescriptionSystem.GetNameWithID(other));
            Log.Data(DescriptionSystem.GetDebugInfoEntity(other));

            if(interactable.ChangeSolidity)
            {
                var collider = EntityManager.GetComponent<ColliderComponent>(other);

                if(collider == null)
                {
                    Log.Warning("Interactable has ChangeSolidity set but has no collider attached! " + DescriptionSystem.GetNameWithID(other));
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(other));
                }
                else if(collider.Solid == false)
                {
                    //TODO: make it solid again? ( ._.)?
                }
                else
                {
                    collider.Solid = false;
                }
            }

            if(interactable.ChangeTexture)
            {
                var renderable = EntityManager.GetComponent<RenderableSpriteComponent>(other);

                if(renderable == null)
                {
                    Log.Error("Interactable has ChangeTexture set but does not have RenderableSprite attached! " + DescriptionSystem.GetNameWithID(other));
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(other));
                    return false;
                }

                if(interactable.AlternateTexture == "")
                {
                    Log.Warning("Interactable has ChangeTexture set but does not define AlternateTexture! " + DescriptionSystem.GetNameWithID(other));
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(other));
                    renderable.Texture = "square"; // placholder; something's not right
                }
                else
                {
                    renderable.Texture = interactable.AlternateTexture;
                }
            }

            if(interactable.GrantsItems)
            {
                if(interactable.Items == null || interactable.Items.Count == 0)
                {
                    Log.Warning("Interactable has GrantItems set but does not define Items! " + DescriptionSystem.GetNameWithID(other));
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(other));
                }
                else
                {
                    //TODO: pick up item(s)
                }
            }                 

            Util.TurnOver(actor);
            return true;
        }
    }
}
