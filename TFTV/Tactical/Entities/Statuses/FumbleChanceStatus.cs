using Base.Entities.Statuses;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System.Linq;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class FumbleChanceStatus : TacStatus
    {
        public FumbleChanceStatusDef FumbleChanceStatusDef => BaseDef as FumbleChanceStatusDef;

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);
            bool actorHasRestrictedWeapon = false;
            if (TacticalActor != null && FumbleChanceStatusDef.RestrictedDeliveryType != default)
            {
                actorHasRestrictedWeapon = TacticalActor.Equipments.GetWeapons().Any(weapon => weapon.WeaponDef.DamagePayload.DamageDeliveryType == FumbleChanceStatusDef.RestrictedDeliveryType
                                                                                               && !weapon.WeaponDef.Tags.Any(gt => FumbleChanceStatusDef.WeaponTagCullFilter.Contains(gt)));
            }
            if (TacticalActor == null || !actorHasRestrictedWeapon)
            {
                RequestUnapply(statusComponent);
                return;
            }
            TacticalLevel.AbilityActivatingEvent += OnAbilityActivating;
        }

        public override void OnUnapply()
        {
            base.OnUnapply();
            TacticalLevel.AbilityActivatingEvent -= OnAbilityActivating;
        }


        private void OnAbilityActivating(TacticalAbility ability, object parameter)
        {
            if (ability.TacticalActor != TacticalActor || !ability.TacticalActor.HasStatus(FumbleChanceStatusDef) || ability.FumbledAction)
            {
                return; //Early exit to do nothing, ability is not activated by this actor, actor does not have the FumbleChanceStatus applied or ability will already fumble
            }

            if (ShouldFumble(ability))
            {
                FumbleChanceStatus fumbleChanceStatus = ability.TacticalActor.Status.GetStatus<FumbleChanceStatus>(FumbleChanceStatusDef);

                TacticalActor statusSourceActor = fumbleChanceStatus.Source as TacticalActor;

                TFTVLogger.Always($"");

                //Voland's messy drills thing
                PassiveModifierAbilityDef causticJammingDef = (PassiveModifierAbilityDef)Repo.GetDef("8d4e5f60-9192-122b-d4e5-f60718293a4b");
                DamageOverTimeStatusDef poisonStatusDef = (DamageOverTimeStatusDef)Repo.GetDef("ea972820-342d-76a4-c9a3-1018e06666b9");

                TFTVLogger.Always($"FumbleChanceStatus.Source: {statusSourceActor?.DisplayName} jammed actor is poisoned? {TacticalActor.Status.HasStatus(poisonStatusDef)}");


                if (causticJammingDef != null && statusSourceActor!=null 
                    && statusSourceActor.GetAbilityWithDef<PassiveModifierAbility>(causticJammingDef) != null && TacticalActor.Status.HasStatus(poisonStatusDef))
                {
                    AccessTools.Property(typeof(TacticalAbility), "FumbledAction").SetValue(ability, true);
                    TFTVLogger.Always($"Caustic Jamming + Poison combo detected, forcing fumble for {ability} used by {TacticalActor}");
                    return;

                }

                int triggerRoll = UnityEngine.Random.Range(0, 100);
                TFTVLogger.Always($"{FumbleChanceStatusDef.name} for actor {TacticalActor} using {ability}: random roll (0-99) to trigger a fumble is {triggerRoll}, preset FumbleChancePerc is {FumbleChanceStatusDef.FumbleChancePerc}");
                if (triggerRoll < FumbleChanceStatusDef.FumbleChancePerc)
                {
                    // Using reflection to set the readonly FumbledAction property (the setter is private)
                    AccessTools.Property(typeof(TacticalAbility), "FumbledAction").SetValue(ability, true);
                }
            }
        }

        private bool ShouldFumble(TacticalAbility ability)
        {
            if (FumbleChanceStatusDef.RestrictedDeliveryType != default)
            {
                return ability.Source is Weapon weapon
                       && weapon.WeaponDef.DamagePayload.DamageDeliveryType == FumbleChanceStatusDef.RestrictedDeliveryType
                       && !weapon.WeaponDef.Tags.Any(gt => FumbleChanceStatusDef.WeaponTagCullFilter.Contains(gt));
            }
            if (FumbleChanceStatusDef.AbilitiesToFumble != null && FumbleChanceStatusDef.AbilitiesToFumble.Length > 0)
            {
                return FumbleChanceStatusDef.AbilitiesToFumble.Contains(ability.TacticalAbilityDef);
            }
            return ability.Source is Equipment;
        }
    }
}
