using Base.Core;
using Base.Entities.Effects;
using Base.Levels;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.Effects.DamageEffect;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class ParalysisDamageTacticalVanillaFixes
    {
        private static Dictionary<TacticalActor, float> _actorsWithAppliedParalysisDamage = new Dictionary<TacticalActor, float>();

        [HarmonyPatch(typeof(PanicAbility), "Move")]
        internal static class PanicAbilityMovePatch
        {
            private static bool Prefix(PanicAbility __instance, ref IEnumerator<NextUpdate> __result)
            {
                if (!__instance.TacticalActor.Status.HasStatus<ParalysedStatus>())
                {
                    return true;
                }

                PanicStatus panicStatus = __instance.PanicStatus;
                if (panicStatus != null)
                {
                    // Match the state produced by a completed panic move. This lets the
                    // normal panic flow recover the actor instead of retrying every turn.
                    panicStatus.State = PanicStatus.PanicState.Moved;
                    panicStatus.DoNotRecoverThisTurn = true;
                }

                __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
                return false;
            }
        }

        public static void ClearDataActorsParalysisDamage()
        {
            try
            {
                //  TFTVLogger.Always($"Clearing paralysis damage");
                _actorsWithAppliedParalysisDamage.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckActorsWithAppliedParalysisDict(TacticalActor actor, float apLost)
        {
            try
            {

                //   TFTVLogger.Always($"actor: {actor.DisplayName} ap value: {actor.CharacterStats.ActionPoints.Value} max value: {actor.CharacterStats.ActionPoints.IntMax}");

                if (_actorsWithAppliedParalysisDamage.ContainsKey(actor) && _actorsWithAppliedParalysisDamage[actor] >= apLost)
                {
                    //  TFTVLogger.Always($"{actor.DisplayName} already lost {apLost} from PD application this turn");
                    return true;
                }

                if (_actorsWithAppliedParalysisDamage.ContainsKey(actor))
                {
                    _actorsWithAppliedParalysisDamage[actor] = apLost;
                }
                else
                {
                    _actorsWithAppliedParalysisDamage.Add(actor, apLost);
                }

                return false;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        /// <summary>
        /// Fixes inconsistency in paralysis damage application
        /// </summary>
        [HarmonyPatch(typeof(ParalysisDamageEffect), nameof(ParalysisDamageEffect.AddTarget))]
        public static class TFTV_ParalysisDamageEffect_AddTarget_Patch
        {

            public static bool Prefix(ParalysisDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {
                    TacticalActor tacticalActor = (__instance.IsSimulation(target) ? (target.GetParam<Params>().Predictor.GetPredictingReceiver(recv.GetActor()) as TacticalActor) : (recv.GetActor() as TacticalActor));
                    if (tacticalActor == null)
                    {
                        return false;
                    }

                    //added
                    bool attackNotSoT = false; //flag to check that the PD application is coming from an attack, not the SoT effect
                                               //added

                    DamageOverTimeStatus status = tacticalActor.Status.GetStatus<DamageOverTimeStatus>(__instance.ParalysisDamageEffectDef.ParalysisStacksStatus);
                    bool flag = false;
                    float num = accum.Amount;

                    //   TFTVLogger.Always($"{tacticalActor.DisplayName} num: {num}; fullDamageValue: {status?.FullDamageValue}");

                    if (status != null && status != __instance.Source) //this triggers only if the PD is added as an attack. The other case would be if status==null
                    {
                        flag = true;
                        num += status.FullDamageValue;
                        //  TFTVLogger.Always($"it's an attack, not SoT effect! status.FullDamageValue: {status.FullDamageValue}, so num {num}");
                    }

                    //added
                    if (flag || status == null || status != null && status.FullDamageValue == 0)
                    {
                        attackNotSoT = true;
                    }

                    float currentPD = (float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString());

                    if (!attackNotSoT)
                    {
                        num -= 1; //As this SoT effect, but the application is carried before  
                                  // TFTVLogger.Always($"As this SoT effect, num reduced by 1 to: {num}");
                    }
                    //added

                    float a = num / (float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString());


                    //  TFTVLogger.Always($"{tacticalActor.DisplayName}, STR: {(float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString())}, num: {num}, a: {a}"); 

                    if (Utl.GreaterThanOrEqualTo(a, 1f))
                    {
                        //    TFTVLogger.Always($"1 or more");

                        tacticalActor.CharacterStats.ActionPoints.Subtract(tacticalActor.CharacterStats.ActionPoints.Max);
                        // if (flag || Utl.GreaterThan(a, 1f))
                        // {
                        //   TFTVLogger.Always($"greater than 1");

                        TacticalActorBase sourceTacticalActorBase = TacUtil.GetSourceTacticalActorBase(status?.Source ?? __instance.Source);
                        tacticalActor.Status.ApplyStatus(__instance.ParalysisDamageEffectDef.ParalysedStatus, sourceTacticalActorBase);
                        //  }
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.75f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.75f))
                    {
                        //  TFTVLogger.Always($"0.75 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.75f * (float)tacticalActor.CharacterStats.ActionPoints.Max);

                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.5f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.5f))
                    {
                        //   TFTVLogger.Always($"0.5 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.5f * (float)tacticalActor.CharacterStats.ActionPoints.Max);
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.25f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.25f))
                    {
                        //   TFTVLogger.Always($"0.25 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.25f * (float)tacticalActor.CharacterStats.ActionPoints.Max);
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }
    }
}
