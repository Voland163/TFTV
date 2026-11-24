using Base.Core;
using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVHarshAttrition
    {
        /// <summary>
        /// Mind Control, Induce Panic and the other abilities that only work on a target with fewer Will Points than the
        /// caster leave such targets out, which tells the player the target's Will Points. Under Harsh Attrition, when the
        /// player doesn't know them for real (vivisection and study, <see cref="PandoranVariance.Insight"/>), the target can
        /// be picked anyway. Cast on a target whose Will Points are too high, the ability is resisted: the caster spends the
        /// Action Points, the Will Points are kept, nothing else happens.
        /// The vanilla checks are the def flags MindControlAbilityDef.CheckAgainstTargetWillPoints (fails when the target
        /// has more Will Points than the caster) and ApplyStatusAbilityDef.CheckAgainstTargetWillpower (fails unless the
        /// target has fewer), read in each ability's TargetFilterPredicate.
        /// </summary>
        internal static class WillpowerResistance
        {
            private const string ResistedKey = "TFTV_WILL_RESISTED";

            /// <summary>The ability being cast on a target that resists it, while its Activate runs.</summary>
            private static TacticalAbility _resisting;
            private static TacticalActor _resistingTarget;

            private static bool ChecksWillpower(TacticalAbility ability)
            {
                switch (ability)
                {
                    case MindControlAbility mindControl:
                        return mindControl.MindControlAbilityDef != null && mindControl.MindControlAbilityDef.CheckAgainstTargetWillPoints;
                    case ApplyStatusAbility applyStatus:
                        return applyStatus.ApplyStatusAbilityDef != null && applyStatus.ApplyStatusAbilityDef.CheckAgainstTargetWillpower;
                    default:
                        return false;
                }
            }

            private static void SetWillpowerCheck(TacticalAbility ability, bool check)
            {
                switch (ability)
                {
                    case MindControlAbility mindControl:
                        mindControl.MindControlAbilityDef.CheckAgainstTargetWillPoints = check;
                        break;
                    case ApplyStatusAbility applyStatus:
                        applyStatus.ApplyStatusAbilityDef.CheckAgainstTargetWillpower = check;
                        break;
                }
            }

            /// <summary>A player unit casting on a creature whose real Will Points the player doesn't know.</summary>
            private static bool WillpowerHidden(TacticalAbility ability, TacticalActorBase target)
            {
                if (!IsEnabled || ability == null || !(target is TacticalActor))
                {
                    return false;
                }

                TacticalActor caster = ability.TacticalActor;
                if (caster == null || !caster.IsControlledByPlayer)
                {
                    return false;
                }

                PandoranVariance.Insight insight = PandoranVariance.GetInsight(target);
                return !(insight.Stats && insight.Studied);
            }

            /// <summary>The vanilla comparison each ability makes, worked out the same way.</summary>
            private static bool Resists(TacticalAbility ability, TacticalActor target, out float targetWill, out float casterWill)
            {
                targetWill = target.CharacterStats.WillPoints;
                casterWill = ability.TacticalActor.CharacterStats.WillPoints;

                if (ability is MindControlAbility)
                {
                    return targetWill > casterWill;
                }

                return !Base.Utils.Maths.Utl.LesserThan(targetWill, casterWill, 1E-05f);
            }

            /// <summary>Turns the def's Will Points check off while this target is filtered, then back on.</summary>
            private static void FilterPrefix(TacticalAbility ability, TacticalActorBase target, out bool __state)
            {
                __state = false;

                try
                {
                    if (ChecksWillpower(ability) && WillpowerHidden(ability, target))
                    {
                        SetWillpowerCheck(ability, false);
                        __state = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static Exception FilterFinalizer(TacticalAbility ability, bool __state, Exception __exception)
            {
                if (__state)
                {
                    SetWillpowerCheck(ability, true);
                }

                return __exception;
            }

            [HarmonyPatch(typeof(MindControlAbility), "TargetFilterPredicate")]
            internal static class MindControlAbility_TargetFilterPredicate_WillpowerResistance_Patch
            {
                private static void Prefix(MindControlAbility __instance, TacticalActorBase __3, out bool __state)
                {
                    FilterPrefix(__instance, __3, out __state);
                }

                private static Exception Finalizer(MindControlAbility __instance, bool __state, Exception __exception)
                {
                    return FilterFinalizer(__instance, __state, __exception);
                }
            }

            [HarmonyPatch(typeof(ApplyStatusAbility), "TargetFilterPredicate")]
            internal static class ApplyStatusAbility_TargetFilterPredicate_WillpowerResistance_Patch
            {
                private static void Prefix(ApplyStatusAbility __instance, TacticalActorBase __3, out bool __state)
                {
                    FilterPrefix(__instance, __3, out __state);
                }

                private static Exception Finalizer(ApplyStatusAbility __instance, bool __state, Exception __exception)
                {
                    return FilterFinalizer(__instance, __state, __exception);
                }
            }

            /// <summary>Notes a cast the target will resist, for ApplyCosts and PlayAction further down the same Activate.</summary>
            private static void ActivatePrefix(TacticalAbility ability, object parameter)
            {
                try
                {
                    _resisting = null;
                    _resistingTarget = null;

                    if (!(parameter is TacticalAbilityTarget abilityTarget) || !(abilityTarget.Actor is TacticalActor target)
                        || !ChecksWillpower(ability) || !WillpowerHidden(ability, target)
                        || !Resists(ability, target, out float targetWill, out float casterWill))
                    {
                        return;
                    }

                    _resisting = ability;
                    _resistingTarget = target;

                    TFTVLogger.Always($"[HarshAttrition] {ability.TacticalActor.DisplayName}'s {ability.TacticalAbilityDef.name} on {target.DisplayName} is resisted: target Will Points {targetWill} against the caster's {casterWill}; Action Points spent, Will Points kept");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static Exception ActivateFinalizer(Exception __exception)
            {
                _resisting = null;
                _resistingTarget = null;
                return __exception;
            }

            [HarmonyPatch(typeof(MindControlAbility), nameof(MindControlAbility.Activate))]
            internal static class MindControlAbility_Activate_WillpowerResistance_Patch
            {
                private static void Prefix(MindControlAbility __instance, object parameter)
                {
                    ActivatePrefix(__instance, parameter);
                }

                private static Exception Finalizer(Exception __exception)
                {
                    return ActivateFinalizer(__exception);
                }
            }

            [HarmonyPatch(typeof(ApplyStatusAbility), nameof(ApplyStatusAbility.Activate))]
            internal static class ApplyStatusAbility_Activate_WillpowerResistance_Patch
            {
                private static void Prefix(ApplyStatusAbility __instance, object parameter)
                {
                    ActivatePrefix(__instance, parameter);
                }

                private static Exception Finalizer(Exception __exception)
                {
                    return ActivateFinalizer(__exception);
                }
            }

            /// <summary>A resisted cast pays its Action Points only.</summary>
            [HarmonyPatch(typeof(TacticalAbility), "ApplyCosts")]
            internal static class TacticalAbility_ApplyCosts_WillpowerResistance_Patch
            {
                private static bool Prefix(TacticalAbility __instance, object parameter)
                {
                    try
                    {
                        if (_resisting == null || __instance != _resisting)
                        {
                            return true;
                        }

                        if (__instance.ShouldApplyCosts(parameter) && __instance.UsesActionPoints)
                        {
                            __instance.TacticalActor.CharacterStats.ActionPoints.Subtract(__instance.ActionPointCost);
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            /// <summary>A resisted cast plays nothing: "RESISTED" over the target instead of the ability's own action.</summary>
            [HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.PlayAction))]
            internal static class TacticalAbility_PlayAction_WillpowerResistance_Patch
            {
                private static void Prefix(TacticalAbility __instance, ref Func<PlayingAction, IEnumerator<NextUpdate>> action)
                {
                    try
                    {
                        if (_resisting == null || __instance != _resisting)
                        {
                            return;
                        }

                        TacticalActor target = _resistingTarget;
                        action = playing => Resisted(target);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static IEnumerator<NextUpdate> Resisted(TacticalActor target)
            {
                try
                {
                    if (target != null)
                    {
                        TFTVTauntsAndQuips.FloatingTextManager.ShowFloatingText(target, TFTVCommonMethods.ConvertKeyToString(ResistedKey), Color.white);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                yield break;
            }
        }
    }
}
