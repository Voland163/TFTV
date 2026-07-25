using Base.Core;
using Base.Entities;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class MadSkunkyFixes
    {
        //Madskunky's replacement of a trig function to reduce AI processing time 
        [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetDamageModifierForDistance))]
        public static class Weapon_GetDamageModifierForDistance_patch
        {

            public static bool Prefix(Weapon __instance, TacticalActorBase targetActor, ref float __result, float distance)
            {
                try
                {
                    float num = (float)Math.PI / 180f * __instance.WeaponDef.SpreadDegrees;
                    if (num < Mathf.Epsilon)
                    {
                        __result = 1f;
                        return false;
                    }

                    TacticalPerceptionBase tacticalPerceptionBase = targetActor.TacticalPerceptionBase;
                    float num2 = tacticalPerceptionBase.Height / 2f;
                    float r = tacticalPerceptionBase.GetCapsuleLocal().r;
                    float time = Mathf.Clamp(((num2 + r) / 2f / distance) / num, 0f, 1f);
                    __result = time < 0.5 ? 0f : time;
                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        //MadSkunky's OW, fumble and Throwing range fix copied over from BC

        /// <summary> 
        /// Fix:
        /// The UI icon element of overwatch abilities does not take AP modifications for the default shooting ability into account.
        /// </summary>
        [HarmonyPatch(typeof(TacticalAbility), "FractActionPointCost", MethodType.Getter)] //VERIFIED
        internal class TacticalAbility_get_FractActionPointCost_OWFix
        {
            public static bool Prefix(TacticalAbility __instance, ref float __result)
            {
                try
                {
                    if (!(__instance is OverwatchAbility overwatchAbility))
                    {
                        // do nothing and call the patched method if this is not an overwatch ability
                        return true;
                    }

                    // copy-pasted from the original get_FractActionPointCost()
                    float baseFract;
                    if (overwatchAbility.TacticalAbilityDef.ActionPointCost >= 0f)
                    {
                        baseFract = overwatchAbility.TacticalAbilityDef.ActionPointCost;
                    }
                    else
                    {
                        Equipment equipment = overwatchAbility.Equipment;
                        baseFract = ((equipment != null) ? equipment.FractApToUse : 0f);
                    }
                    // copy-pasted from the original and private method GetActionPointFractCost(baseFract);
                    if (!baseFract.IsZero(1E-05f) && overwatchAbility.IsTacticalActor)
                    {
                        baseFract = overwatchAbility.TacticalActor.CalcFractActionPointCost(baseFract, overwatchAbility);
                    }

                    // adding comparison with default shooting ability and take the minimum of both
                    __result = Mathf.Min(baseFract, overwatchAbility.GetDefaultShootAbility().FractActionPointCost);

                    // skip the patched method
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

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

        /// <summary>
        /// Harmony patch that fixes the vanilla throw range calculation.
        /// The attenuation tag allows Harmony to find the targeted class/object method and apply the patch from the following class.
        /// </summary>
        [HarmonyPatch(typeof(Weapon), "GetThrowingRange")] //VERIFIED
        internal class Weapon_GetThrowingRange_Fix
        {
            /// Using Postfix patch to be guaranteed to get executed.
            public static void Postfix(ref float __result, Weapon __instance, float rangeMultiplier)
            {
                try
                {
                    float num = __instance.TacticalActor.CharacterStats.Endurance * __instance.TacticalActor.TacticalActorDef.EnduranceToThrowMultiplier;
                    float num2 = __instance.TacticalActor.CharacterStats.BonusAttackRange.CalcModValueBasedOn(num);
                    // MadSkunky: Extension of calculation with range multiplier divided by 12 for normalization and multiplier from configuration.
                    num *= __instance.GetDamagePayload().Range / 12f;
                    float multiplier = 1f; // (Main.Config as GrenadeThrowRangeFixConfig).ThrowRangeMultiplier / 100f;
                    __result = ((num / __instance.Weight * rangeMultiplier) + num2) * multiplier;
                    // End of changes
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}
