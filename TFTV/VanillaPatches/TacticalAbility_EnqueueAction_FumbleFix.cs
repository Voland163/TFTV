using Base.Core;
using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;

namespace TFTV.VanillaPatches
{
    /// <summary>
    /// Fix:
    /// The method TacticalAbility.EnqueueAction() is an alternative to TacticalAbility.PlayAction().
    /// The assumption is that this method will play an action synchronously with a currently running action, as opposed to playing it directly as with PlayAction().
    /// This method appears to be called only by ShootAbility.Activate().
    /// In contrast to PlayAction(), however, a possible fumble (property FumbledAction = true) is not checked with this method and therefore no fumble will happen.
    /// This patch fixes the problem and queues the FumbleAction() to play when the FumbledAction property is set (= true).
    /// </summary>
    [HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.EnqueueAction))]
    public static class TacticalAbility_EnqueueAction_FumbleFix
    {
        public static bool Prefix(TacticalAbility __instance,
                                  ref PlayingAction ____nextAction,
                                  Func<PlayingAction, IEnumerator<NextUpdate>> action,
                                  object parameter,
                                  bool soloAfterCurrent = false)
        {
            try
            {
                // Change action to play FumbleAction dependent on the property FumbledAction
                if (__instance.FumbledAction)
                {
                    action = GetDelegate<Func<PlayingAction, IEnumerator<NextUpdate>>>(__instance, "FumbleAction");
                    TFTVLogger.Always($"TacticalAbility_EnqueueAction_FumbleFixPatch: FumbledAction for {__instance} is true, changed action to play to TacticalAbility.FumbleAction().");
                }

                // Get delegate for CreateWaitingForCameraBlendingAction method
                Func<Func<PlayingAction, IEnumerator<NextUpdate>>, Func<PlayingAction, IEnumerator<NextUpdate>>> instance_CreateWaitingForCameraBlendingAction =
                    GetDelegate<Func<Func<PlayingAction, IEnumerator<NextUpdate>>, Func<PlayingAction, IEnumerator<NextUpdate>>>>(__instance, "CreateWaitingForCameraBlendingAction");
                // Go on with original code
                ____nextAction = __instance.ActionComponent.PlayActionAfterCurrent(ActionChannel.ActorActions,
                                                                                   soloAfterCurrent,
                                                                                   parameter,
                                                                                   instance_CreateWaitingForCameraBlendingAction(action),
                                                                                   GetDelegate<Action<PlayingAction>>(__instance, "StartPlayingAction"),
                                                                                   GetDelegate<Action<PlayingAction>>(__instance, "ClearPlayingAction"));

                __instance.TacticalActorBase.OnAbilityEnqueued(__instance);
                return false; // Don't execute (skip) original method, this is a fix for the original one in the end ;-)
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return true;  // Execute original method
            }
        }

        private static DelegateType GetDelegate<DelegateType>(object instance, string name) where DelegateType : Delegate
        {
            return AccessTools.MethodDelegate<DelegateType>(AccessTools.Method(instance.GetType(), name), instance); ;
        }
    }
}
