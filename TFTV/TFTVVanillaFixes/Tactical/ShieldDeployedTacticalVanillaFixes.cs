using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class ShieldDeployedTacticalVanillaFixes
    {
        /// <summary>
        /// A deployed shield retracting itself when a tactical save is loaded, leaving the soldier free to
        /// move with the shield in hand.
        ///
        /// ShieldDeployedStatus retracts on two signals: its shield being detached (OnDetached) and its
        /// carrier moving (OnActorMoved; it ignores exactly one move, the one it expects while being
        /// restored). Loading a save fires them while the status is being restored - the shield is
        /// re-attached to its shield point ("attaching ... for a second time") - so the status was
        /// unapplied in the same instant it was applied. With it gone nothing roots the soldier: the
        /// abilities bar, which with a shield deployed only offers Retrieve Shield and Stand By, is back
        /// to normal and the soldier can walk off.
        ///
        /// Neither signal is the player doing anything while a save is loading, so both are ignored then.
        /// </summary>
        private static bool IsLoading(ShieldDeployedStatus status)
        {
            return status?.TacticalActor?.TacticalLevel != null
                && status.TacticalActor.TacticalLevel.IsLoadingSavedGame;
        }

        private static void LogKept(ShieldDeployedStatus status, string signal)
        {
            TFTVLogger.Always($"[ShieldDeployed] Kept {status?.TacticalActor?.DisplayName}'s shield deployed: ignored '{signal}' while loading a save.");
        }

        [HarmonyPatch(typeof(ShieldDeployedStatus), "OnDetached")]
        internal static class ShieldDeployedStatus_OnDetached_Patch
        {
            private static bool Prefix(ShieldDeployedStatus __instance)
            {
                try
                {
                    if (!IsLoading(__instance))
                    {
                        return true;
                    }

                    LogKept(__instance, "shield detached");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(ShieldDeployedStatus), "OnActorMoved")]
        internal static class ShieldDeployedStatus_OnActorMoved_Patch
        {
            private static bool Prefix(ShieldDeployedStatus __instance, TacticalActorBase actor)
            {
                try
                {
                    if (actor == null || actor != __instance?.TacticalActor || !IsLoading(__instance))
                    {
                        return true;
                    }

                    LogKept(__instance, "carrier moved");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
