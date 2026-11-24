using Base.Core;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class PanicTacticalVanillaFixes
    {
        /// <summary>
        /// A panicked unit could show AP it can't use. Vanilla PanicAbility.Move halves the unit's AP and
        /// then runs it to the safest spot it finds; whatever the run doesn't spend (all of it when there is
        /// nowhere to run) stays on the unit, shown in its AP bar, though Panic lets it do nothing more that
        /// turn. When the run is over and the unit is left panicked, its AP goes to 0.
        /// </summary>
        [HarmonyPatch(typeof(PanicAbility), "Move")]
        internal static class PanicAbility_Move_SpendLeftoverAP_Patch
        {
            private static void Postfix(PanicAbility __instance, ref IEnumerator<NextUpdate> __result)
            {
                __result = ThenSpendLeftoverAP(__result, __instance?.TacticalActor);
            }

            private static IEnumerator<NextUpdate> ThenSpendLeftoverAP(IEnumerator<NextUpdate> move, TacticalActor actor)
            {
                while (move.MoveNext())
                {
                    yield return move.Current;
                }

                SpendLeftoverAP(actor);
            }

            private static void SpendLeftoverAP(TacticalActor actor)
            {
                try
                {
                    PanicStatus panic = actor?.Status?.GetStatus<PanicStatus>();

                    if (actor == null || !actor.IsAlive || panic == null || panic.State != PanicStatus.PanicState.Moved)
                    {
                        return;
                    }

                    float left = (float)actor.CharacterStats.ActionPoints;

                    if (left > 0f)
                    {
                        actor.CharacterStats.ActionPoints.Set(0f);
                        TFTVLogger.Always($"[Panic] {actor.DisplayName} ran in panic with {left:0.#} AP to spare; AP set to 0.");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

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
