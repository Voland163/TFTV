using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class PanicTacticalVanillaFixes
    {
        /// <summary>
        /// A unit could lose two whole turns to one panic.
        ///
        /// Vanilla gives Panic one of two states when it is applied. Panicked during another faction's
        /// turn: on its next turn the unit flees and recovers, one turn lost. Panicked during its own
        /// faction's turn: it is expected to flee in what is left of that turn, and recovering (which
        /// ends a turn) is held over to the next. But nothing makes it flee then if it has already
        /// finished acting - e.g. a heavy whose Will dropped while the rest of its faction moved. So the
        /// flight slipped to its next turn, still under the "own turn" rule, and the recovery to the
        /// turn after: two turns lost.
        ///
        /// If a unit starts its turn with Panic still in the "own turn" state from an earlier turn, that
        /// turn passed without it fleeing, so it is treated as panicked during another faction's turn:
        /// it flees and recovers now. Panic applied at the start of the unit's own turn, before this runs,
        /// keeps the vanilla behaviour.
        /// </summary>
        [HarmonyPatch(typeof(PanicStatus), nameof(PanicStatus.StartTurn))]
        internal static class PanicStatus_StartTurn_Patch
        {
            private static void Postfix(PanicStatus __instance)
            {
                try
                {
                    // TurnApplied is stamped from this same TurnNumber when the status is applied.
                    if (__instance?.State != PanicStatus.PanicState.PanickedDuringOwnTurn
                        || __instance.TurnApplied >= __instance.TurnNumber)
                    {
                        return;
                    }

                    __instance.State = PanicStatus.PanicState.PanickedDuringOtherTurn;

                    TFTVLogger.Always($"[Panic] {__instance.TacticalActor?.DisplayName} panicked during its own turn {__instance.TurnApplied} " +
                        $"without fleeing; it flees and recovers this turn ({__instance.TurnNumber}).");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
