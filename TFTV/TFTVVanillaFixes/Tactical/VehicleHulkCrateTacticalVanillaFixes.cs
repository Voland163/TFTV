using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    /// <summary>
    /// Destroyed vehicles leave a hulk that doubles as a lootable crate, but the hulk's
    /// Animator has no "open" bool parameter. CrateComponent.Open() therefore never gets
    /// its OnCrateOpenAnim() animation event and IsOpen() keeps returning false, with two
    /// consequences:
    ///
    ///  * OpenCrateAbility spins on "while (crate.IsOpening)" until CrateOpenTimeout expires,
    ///    holding the game in UIStateWaiting for a full 10 seconds. The UI is dead and the
    ///    actor stands frozen in its idle animation for the duration.
    ///  * Because the hulk never counts as open, every subsequent move next to it re-fires
    ///    the ability, re-awarding the crate's Will Points bonus and popping the inventory.
    ///
    /// Both follow from the animator refusing the flag, so both are fixed by noticing that
    /// Open() failed to take and recording the open state on the mod's side instead.
    /// </summary>
    internal static class VehicleHulkCrateTacticalVanillaFixes
    {
        private static readonly ConditionalWeakTable<CrateComponent, object> OpenedWithoutAnimator =
            new ConditionalWeakTable<CrateComponent, object>();

        private static readonly MethodInfo IsOpeningSetter =
            AccessTools.PropertySetter(typeof(CrateComponent), nameof(CrateComponent.IsOpening));

        [HarmonyPatch(typeof(CrateComponent), nameof(CrateComponent.Open))]
        internal static class CrateComponent_Open_NoOpenAnimation_Patch
        {
            private static void Postfix(CrateComponent __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return;
                    }

                    object marker;
                    if (!OpenedWithoutAnimator.TryGetValue(__instance, out marker))
                    {
                        // A crate whose animator accepted the flag reports itself open right away.
                        // Ask before the crate is marked, so IsOpen() still reflects the animator.
                        if (__instance.IsOpen())
                        {
                            return;
                        }

                        OpenedWithoutAnimator.Add(__instance, null);
                    }

                    // There is no open animation to wait for, so stop waiting on it.
                    IsOpeningSetter?.Invoke(__instance, new object[] { false });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(CrateComponent), nameof(CrateComponent.IsOpen))]
        internal static class CrateComponent_IsOpen_NoOpenAnimation_Patch
        {
            private static void Postfix(CrateComponent __instance, ref bool __result)
            {
                try
                {
                    object marker;
                    if (!__result && __instance != null && OpenedWithoutAnimator.TryGetValue(__instance, out marker))
                    {
                        __result = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
