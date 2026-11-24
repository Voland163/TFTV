using Base.Core;
using Base.Entities;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// A human enemy picking a weapon or a magazine off the floor, with the bend-down-and-grab animation of a Technician
    /// retrieving a turret. Only the AI uses it (TFTV_PickUpGear_AIActionDef, see TFTVAICombatAbilities.Scavenging):
    /// the ability is given to a unit when it stands next to something it needs and taken away again once it doesn't.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
    public class PickUpGearAbilityDef : TacticalAbilityDef
    {
    }

    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbility))]
    public class PickUpGearAbility : TacticalAbility
    {
        public override bool HasValidTargets => TFTVAICombatAbilities.Scavenging.DropAtHand(TacticalActor).Key != null;

        public override void Activate(object parameter = null)
        {
            base.Activate(parameter);
            PlayAction(new Func<PlayingAction, IEnumerator<NextUpdate>>(PickUpCrt), parameter, null);
        }

        private IEnumerator<NextUpdate> PickUpCrt(PlayingAction action)
        {
            TacticalActor actor = TacticalActor;
            KeyValuePair<ItemContainer, TacticalItem> drop = TFTVAICombatAbilities.Scavenging.DropAtHand(actor);

            if (drop.Key == null)
            {
                yield break;
            }

            Vector3 direction = drop.Key.Pos - actor.Pos;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                yield return Timing.Call(actor.TacticalNav.Face(direction.normalized, false), null);
            }

            // The ability has no animation of its own: the default action clip plays, swapped for the turret pick-up
            // for this action only (the way FreeReloadAbility borrows a clip).
            AnimationClip defaultClip = actor.ActorAnimActions?.TacActorAnimActionsDef?.DefaultActionClip;
            AnimationClip pickUpClip = TFTVAICombatAbilities.Scavenging.PickUpClip;
            AnimationClip previous = null;
            bool overridden = false;

            if (defaultClip != null && pickUpClip != null && actor.AnimatorOverrides != null)
            {
                previous = actor.AnimatorOverrides[defaultClip];
                actor.AnimatorOverrides[defaultClip] = pickUpClip;
                actor.AnimatorOverrides.ApplyOverrides();
                overridden = true;
            }

            try
            {
                yield return Timing.Call(DoActionAnimation(true), null);

                TFTVAICombatAbilities.Scavenging.TakeDrop(actor, drop, false);

                yield return Timing.Call(WaitForActionAnimationEnd(), null);
            }
            finally
            {
                if (overridden)
                {
                    actor.AnimatorOverrides[defaultClip] = previous != null ? previous : defaultClip;
                    actor.AnimatorOverrides.ApplyOverrides();
                }
            }
        }
    }
}
