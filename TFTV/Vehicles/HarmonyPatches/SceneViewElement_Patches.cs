using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.UI;
using System;
using TFTVVehicleRework.Abilities;

namespace TFTVVehicleRework.HarmonyPatches
{
    /// <summary>
    /// Lets the boarding marker survive the gate that runs before any tile is looked at.
    ///
    /// SceneViewElement.GetPositionMarkers() opens with "if (validMoves == null || !IsValid())
    /// return false", and IsValid() rejects an ability that
    /// Ability.IsEnabled(IgnoreNoValidTargetsFilter) reports as disabled. That filter ignores
    /// NoValidTarget but not NotEnoughActionPoints, and the entry discount is only registered on
    /// the operative while they stand where it applies. So an operative with less than the full
    /// entry cost, standing anywhere other than the entry tile itself, failed the gate and got no
    /// boarding marker at all - the per-tile pricing never even ran, because it is evaluated
    /// lazily inside a call that had already returned.
    ///
    /// Relaxing the gate is safe because nothing downstream trusts it. The per-tile filter still
    /// prices each candidate tile against exactly the discount that tile grants, and
    /// GetPositionMarkers() still only emits a marker where GetTargetsAt(tile) finds a vehicle to
    /// board. All this does is let those two run.
    /// </summary>
    [HarmonyPatch(typeof(SceneViewElement), "IsValid")]
    internal static class SceneViewElement_IsValid_EnterVehicleMarkers_Patch
    {
        /// <summary>
        /// Action points, and having nothing to board from where the operative happens to stand,
        /// are the only two objections worth overriding here. Every other one is real.
        ///
        /// Reading GetDisabledState() and comparing it against NotEnoughActionPoints would not be
        /// enough: GetDisabledStateDefaults() returns the *first* failing check, and
        /// NotEnoughActionPoints is evaluated ahead of RequirementsNotMet, OffMap, ActorStunned
        /// and BlockedByStatus. An operative who is stunned or status-blocked *and* short on AP
        /// reports NotEnoughActionPoints, and would slip through. Asking whether the ability is
        /// enabled once both are ignored is order-independent: anything else still says no.
        /// </summary>
        private static readonly IgnoredAbilityDisabledStatesFilter IgnoreNoTargetAndActionPoints =
            new IgnoredAbilityDisabledStatesFilter(
                IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter,
                AbilityDisabledState.NotEnoughActionPoints);

        private static void Postfix(SceneViewElement __instance, ref bool __result)
        {
            try
            {
                // Def == null is the other reason IsValid() says no, and GetPositionMarkers()
                // dereferences Def straight away, so that one has to keep saying no.
                if (__result || __instance == null || __instance.Def == null)
                {
                    return;
                }

                ExtendedEnterVehicleAbility enterVehicle = __instance.Ability as ExtendedEnterVehicleAbility;

                if (enterVehicle == null)
                {
                    return;
                }

                // Disabled for a reason that has nothing to do with action points - the operative
                // is stunned, blocked by a status, off map, missing a requirement. Vanilla means
                // it, so leave the gate shut.
                if (!enterVehicle.IsEnabled(IgnoreNoTargetAndActionPoints))
                {
                    return;
                }

                if (!enterVehicle.HasEntryDiscountSomewhere())
                {
                    return;
                }

                __result = true;
            }
            catch (Exception e)
            {
                TFTV.TFTVLogger.Error(e);
            }
        }
    }
}
