using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities.Abilities; 
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTVVehicleRework.Abilities 
{

    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbility))]   
    public class FreeReloadAbility : TacticalAbility
    {
        public FreeReloadAbilityDef FreeReloadAbilityDef
        {
            get
            {
                return this.Def<FreeReloadAbilityDef>();
            }
        }

        public override bool HasValidTargets
        {
            get
            {
				return ( (base.SelectedEquipment != null) && (base.SelectedEquipment.CommonItemData.CurrentCharges != base.SelectedEquipment.ChargesMax) );
            }
        }

        public override bool ShouldFlash
		{
			get
			{
				return base.ShouldFlash || this.MustReload;
			}
		}

        public bool MustReload
		{
			get
			{
				Equipment equipment = base.TacticalActor.Equipments.SelectedEquipment as Weapon;
				return equipment != null && !(base.GetDisabledState(null) != AbilityDisabledState.NotDisabled) && !equipment.InfiniteCharges && equipment.CommonItemData.CurrentCharges == 0;
			}
		}

        protected override AbilityDisabledState GetDisabledStateInternal(IgnoredAbilityDisabledStatesFilter filter)
		{
			IgnoredAbilityDisabledStatesFilter filter2 = new IgnoredAbilityDisabledStatesFilter(filter, new AbilityDisabledState[]
			{
				AbilityDisabledState.EquipmentIsDisabled
			});
			AbilityDisabledState disabledStateInternal = base.GetDisabledStateInternal(filter2);
			if (disabledStateInternal == AbilityDisabledState.NoValidTarget)
			{
				return AbilityDisabledState.EquipmentIsFullyLoaded;
			}
			return disabledStateInternal;
		}

        public override void Activate(object parameter = null)
		{
			base.Activate(parameter);
			base.PlayAction(new Func<PlayingAction, IEnumerator<NextUpdate>>(this.ReloadCrt), parameter, null);
		}

		private IEnumerator<NextUpdate> ReloadCrt(PlayingAction action)
		{
			TacticalAbilityTarget abilityTarget = action.Param as TacticalAbilityTarget;
			Equipment equipment = base.SelectedEquipment;
			if (base.TacticalActor.Equipments.Equipments.Contains(equipment))
			{
				TacActorShootAnimActionDef shootcontext = base.TacticalActor.ActorAnimActions.GetAnimAction<TacActorShootAnimActionDef>(TacActorShootAnimActionDef.MakeContext(
				base.TacticalActor, equipment, null, default(Vector3), null));
				
				// AnimationClip SpinUp = shootcontext.FireStart;
				// AnimationClip SpinDown = shootcontext.FireEnd;

				// AnimationClip[] reloadclip = new AnimationClip[]
				// {
				// 	SpinUp,
				// 	SpinDown
				// };
				// AnimationClip reload = base.TacticalActor.ActorAnimActions.GetAnimAction<TacActorShootAnimActionDef>(TacActorShootAnimActionDef.MakeContext(
				// base.TacticalActor, equipment, null, default(Vector3), null)).FireStart; //SpinUp from shooting Animation - works but causes a micro-freeze
			
				AnimationClip defaultActionClip = base.TacticalActor.ActorAnimActions.TacActorAnimActionsDef.DefaultActionClip;
				base.TacticalActor.AnimatorOverrides[defaultActionClip] = shootcontext.Reload; //Empty animation, but doesn't freeze the game.
				base.TacticalActor.AnimatorOverrides.ApplyOverrides();
				yield return base.Timing.Call(base.DoActionAnimation(false), null);
			}
			this.Reload(equipment);
			yield break;
		}
		
		private void Reload(Equipment equipment)
		{
			if (equipment.CommonItemData.Ammo != null)
			{
				int amount = equipment.ItemDef.ChargesMax - equipment.CommonItemData.CurrentCharges;
				equipment.CommonItemData.Ammo.ReloadCharges(amount, true);
			}
		}
    }
}