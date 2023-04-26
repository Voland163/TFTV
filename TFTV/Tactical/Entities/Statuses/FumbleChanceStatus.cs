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
                actorHasRestrictedWeapon = TacticalActor.Equipments.GetWeapons().Any(weapon => weapon.WeaponDef.DamagePayload.DamageDeliveryType == FumbleChanceStatusDef.RestrictedDeliveryType);
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
            if (ability.FumbledAction || !ability.TacticalActor.HasStatus(FumbleChanceStatusDef))
            {
                return; //Early exit to do nothing, ability will already fumble or actor does not have the FumbleChanceStatus applied
            }

            if (ShouldFumble(ability) && UnityEngine.Random.Range(0, 100) < FumbleChanceStatusDef.FumbleChancePerc)
            {
                // Using reflection to set the readonly FumbledAction property (the setter is private)
                AccessTools.Property(typeof(TacticalAbility), "FumbledAction").SetValue(ability, true);
            }
        }

        //public bool AbilityFumbleCheck(TacticalAbility tacticalAbility)
        //{
        //    return ShouldFumble(tacticalAbility) && UnityEngine.Random.Range(0, 100) < FumbleChanceStatusDef.FumbleChancePerc;
        //}

        private bool ShouldFumble(TacticalAbility ability)
        {
            if (FumbleChanceStatusDef.RestrictedDeliveryType != default)
            {
                return ability.Source is Weapon weapon && weapon.WeaponDef.DamagePayload.DamageDeliveryType == FumbleChanceStatusDef.RestrictedDeliveryType;
            }
            if (FumbleChanceStatusDef.AbilitiesToFumble != null && FumbleChanceStatusDef.AbilitiesToFumble.Length > 0)
            {
                return FumbleChanceStatusDef.AbilitiesToFumble.Contains(ability.TacticalAbilityDef);
            }
            return ability.Source is Equipment;
        }
    }

    //[HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.PlayAction))]
    //public static class TacticalAbility_PlayAction_Patch
    //{
    //    public static void Prefix(TacticalAbility __instance)
    //    {
    //        TFTVLogger.Always("PlayAction for " + __instance + " is called ...");
    //    }
    //}

    //[HarmonyPatch]
    //public static class FumbleChanceStatus_Ability_Activate_Fixes
    //{
    //    /// <summary>
    //    /// Reverse patch to call the base TacticalAbility.Activate from the patch on the overridden ShootAbility.Activate.
    //    /// Reflection does not work here, it will always call the overridden Activate method.
    //    /// See also: https://harmony.pardeike.net/articles/patching-edgecases.html#calling-base-methods
    //    /// </summary>
    //    /// <param name="instance">The instance of the class that overrides the base TacticalAbility.Activate method</param>
    //    /// <param name="parameter">Pass through parameter from the overidden Activate method</param>
    //    [HarmonyReversePatch]
    //    [HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.Activate))]
    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    private static void TacticalAbility_Activate(ShootAbility instance, object parameter)
    //    {
    //        TFTVLogger.Always($"HarmonyReversePatch to TacticalAbility.Activate() called from derived {instance} with paramter {parameter}");
    //    }
    //
    //    [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.Activate))]
    //    public static bool Prefix(ShootAbility __instance, object parameter)
    //    {
    //        try
    //        {
    //            TFTVLogger.Always($"ShootAbility.Activate() called, executing Prefix patch ...");
    //            if (!__instance.Weapon.IsAttackSilent(__instance.TacticalActor, null))
    //            {
    //                TacticalFactionVision.IncrementKnownCounterToAll(__instance.TacticalActor, KnownState.Located, 1, true);
    //                foreach (TacticalFaction tacticalFaction in __instance.TacticalActor.TacticalLevel.Factions)
    //                {
    //                    if (tacticalFaction != __instance.TacticalActor.TacticalFaction)
    //                    {
    //                        tacticalFaction.Vision.UpdateVisibilityOfAllTowardsActor(__instance.TacticalActor, float.PositiveInfinity, true);
    //                    }
    //                }
    //            }
    //            TFTVLogger.Always($"Before TacticalAbility.Activate() called from derived {__instance} with paramter {parameter}, FumbledAction is '{__instance.FumbledAction}'");
    //            TacticalAbility_Activate(__instance, parameter);
    //            TFTVLogger.Always($"After TacticalAbility.Activate() called from derived {__instance} with paramter {parameter}, FumbledAction is '{__instance.FumbledAction}'");
    //            TacticalAbilityTarget tacticalAbilityTarget = (TacticalAbilityTarget)parameter;
    //            Func<PlayingAction, IEnumerator<NextUpdate>> action = AccessTools.MethodDelegate<Func<PlayingAction, IEnumerator<NextUpdate>>>(AccessTools.Method("ShootAbility.Shoot"), __instance);
    //            if (tacticalAbilityTarget.AttackType == AttackType.ReturnFire
    //                || tacticalAbilityTarget.AttackType == AttackType.Overwatch
    //                || __instance.TacticalActor.FPSMode
    //                || __instance.OriginTargetData.Range <= 1.5f
    //                || __instance.TacticalActor.TacticalLevel.AnyAIEvaluationAbilityExecuting)
    //            {
    //                TFTVLogger.Always($"Calling TacticalAbility.PlayAction() ...");
    //                __instance.PlayAction(action, parameter, null);
    //                return false;
    //            }
    //            TFTVLogger.Always($"Calling TacticalAbility.EnqueueAction() ...");
    //            if (__instance.FumbledAction)
    //            {
    //                action = AccessTools.MethodDelegate<Func<PlayingAction, IEnumerator<NextUpdate>>>(AccessTools.Method("TacticalAbility.PlayFumbleAction"), __instance);
    //            }
    //            __instance.EnqueueAction(action, parameter, true);
    //            return false;
    //        }
    //        catch (Exception e)
    //        {
    //            TFTVLogger.Error(e);
    //            return true;
    //        }
    //    }
    //}
}
