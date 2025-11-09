using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVBallistics
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static IDamageReceiver GetDamageReceiver(DamagePredictor predictor, GameObject gameObject, Vector3 pos, Quaternion rot)
        {
            IDamageable damageableObject = gameObject.GetComponentInParent<IDamageable>();
            if (damageableObject == null)
            {
                return null;
            }

            IDamageReceiver recv = damageableObject.GetDamageReceiverForHit(pos, rot * Vector3.forward);
            if (predictor != null)
            {
                recv = predictor.GetPredictingReceiver(recv);
            }

            return recv;
        }

        public static Dictionary<Projectile, List<TacticalActor>> ProjectileActor = new Dictionary<Projectile, List<TacticalActor>>();

        // Use HashSet for fast lookup
        public static Dictionary<Projectile, HashSet<string>> ProjectileSlotName = new Dictionary<Projectile, HashSet<string>>();

        // Prediction-time de-dupe keyed by DamageAccumulation when available
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DamageAccumulation, HashSet<string>> PredictorScorpionSlots
            = new System.Runtime.CompilerServices.ConditionalWeakTable<DamageAccumulation, HashSet<string>>();

        // Fallback prediction-time de-dupe when ____damageAccum is null: scope by the current ProjectileLogic
        private static readonly Dictionary<ProjectileLogic, HashSet<string>> PredictorScorpionSlotsByLogic
            = new Dictionary<ProjectileLogic, HashSet<string>>();

        internal static void ClearPredictionDedup()
        {
            PredictorScorpionSlotsByLogic.Clear();
            // CWT entries will be GC’d with their keys; no explicit clear needed
        }

        public static void ClearBallisticInfoOnAbilityExecuteFinished()
        {
            try
            {
                ProjectileActor.Clear();
                ProjectileSlotName.Clear();
                ClearPredictionDedup();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "Die")]
        public static class TacticalActor_Die_patch
        {
            public static bool Prefix(TacticalActor __instance)
            {
                try
                {
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");

                    if (!__instance.IsObject() && __instance.ActorDef == dcoy)
                    {
                        __instance.gameObject.SetActive(false);
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(ProjectileLogic), "OnProjectileHit")]
        public static class ProjectileLogic_OnProjectileHit_Umbra_Patch
        {
            private static readonly MethodInfo AffectTargetMethod =
                typeof(ProjectileLogic).GetMethod("AffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);

            private static readonly WeaponDef ScorpionDef = DefCache.GetDef<WeaponDef>("AC_Scorpion_WeaponDef");

            public static bool Prefix(ProjectileLogic __instance, ref bool __result, DamageAccumulation ____damageAccum, CastHit hit, Vector3 dir)
            {
                try
                {
                    Vector3 pos = hit.Point;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    IDamageReceiver receiver = GetDamageReceiver(__instance.Predictor, hit.Collider.gameObject, pos, rot);

                    ClassTagDef umbraClassTag = DefCache.GetDef<ClassTagDef>("Umbra_ClassTagDef");
                    SpawnedActorTagDef decoy = DefCache.GetDef<SpawnedActorTagDef>("Decoy_SpawnedActorTagDef");

                    if (__instance.Predictor != null)
                    {
                        receiver = __instance.Predictor.GetPredictingReceiver(receiver);
                    }

                    TacticalActor hitActor = receiver?.GetActor() as TacticalActor ?? receiver?.GetParent() as TacticalActor;
                    string slotName = receiver?.GetSlotName();

                    // Normalize the key to be per-actor and per-slot
                    string SlotKey(TacticalActor actor, string slot)
                    {
                        // fallbacks to avoid null/empty mismatches
                        string actorKey = actor != null ? actor.GetHashCode().ToString() : "null_actor";
                        string sKey = string.IsNullOrEmpty(slot) ? "no_slot" : slot;
                        return actorKey + ":" + sKey;
                    }

                    if (hitActor != null)
                    {
                        // Umbra/Decoy passthrough as before
                        if (__instance.Projectile != null && (hitActor.HasGameTag(umbraClassTag) || hitActor.HasGameTag(decoy)))
                        {
                            __result = false;

                            if (ProjectileActor.TryGetValue(__instance.Projectile, out var list) && list != null && list.Contains(hitActor))
                            {
                                __instance.Projectile.OnProjectileHit(hit);
                                ____damageAccum?.ResetToInitalAmount();
                            }
                            else
                            {
                                __instance.Projectile.OnProjectileHit(hit);
                                AffectTargetMethod.Invoke(__instance, new object[] { hit, dir });
                                ____damageAccum?.ResetToInitalAmount();

                                if (ProjectileActor.ContainsKey(__instance.Projectile))
                                {
                                    ProjectileActor[__instance.Projectile].Add(hitActor);
                                }
                                else
                                {
                                    ProjectileActor.Add(__instance.Projectile, new List<TacticalActor> { hitActor });
                                }
                            }
                            return false;
                        }

                        // Scorpion: prevent multiple hits to the same limb
                        bool isScorpionRuntime = __instance.Projectile != null
                            && __instance.Projectile.TryGetWeapon().WeaponDef == ScorpionDef;

                        bool isScorpionPredict = __instance.Predictor != null && !string.IsNullOrEmpty(slotName);

                        if ((isScorpionRuntime || isScorpionPredict) && !string.IsNullOrEmpty(slotName))
                        {
                            __result = false;
                            string key = SlotKey(hitActor, slotName);

                            if (isScorpionRuntime)
                            {
                                if (!ProjectileSlotName.TryGetValue(__instance.Projectile, out var set) || set == null)
                                {
                                    set = new HashSet<string>();
                                    ProjectileSlotName[__instance.Projectile] = set;
                                }

                                if (set.Contains(key))
                                {
                                    __instance.Projectile.OnProjectileHit(hit);
                                    ____damageAccum?.ResetToInitalAmount();
                                }
                                else
                                {
                                    __instance.Projectile.OnProjectileHit(hit);
                                    AffectTargetMethod.Invoke(__instance, new object[] { hit, dir });
                                    ____damageAccum?.ResetToInitalAmount();
                                    set.Add(key);
                                }
                            }
                            else
                            {
                                // Prediction path: prefer DamageAccumulation if available, else fall back to per-logic set
                                HashSet<string> set = null;

                                if (____damageAccum != null)
                                {
                                    if (!PredictorScorpionSlots.TryGetValue(____damageAccum, out set))
                                    {
                                        set = new HashSet<string>();
                                        PredictorScorpionSlots.Add(____damageAccum, set);
                                    }
                                }
                                else
                                {
                                    if (!PredictorScorpionSlotsByLogic.TryGetValue(__instance, out set) || set == null)
                                    {
                                        set = new HashSet<string>();
                                        PredictorScorpionSlotsByLogic[__instance] = set;
                                    }
                                }

                                if (set.Contains(key))
                                {
                                    // Already counted for preview
                                    ____damageAccum?.ResetToInitalAmount();
                                }
                                else
                                {
                                    AffectTargetMethod.Invoke(__instance, new object[] { hit, dir });
                                    ____damageAccum?.ResetToInitalAmount();
                                    set.Add(key);
                                }
                            }

                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(DieAbility), "Activate")]
        public static class DieAbility_Activate_Decoy_Patch
        {
            public static bool Prefix(DieAbility __instance)
            {
                try
                {
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");

                    if (!__instance.TacticalActorBase.IsObject() && __instance.TacticalActorBase.ActorDef == dcoy)
                    {
                        __instance.TacticalActor.gameObject.SetActive(false);
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //D-Coy patch to remove if attacked by "smart" enemy
        public static void RemoveDCoy(TacticalActor actor, IDamageDealer damageDealer)
        {

            try
            {
                if (damageDealer != null)
                {

                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");
                    ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                    ClassTagDef tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                    ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                    GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");

                    /* TFTVLogger.Always($"actor is null? {actor == null}");
                     TFTVLogger.Always($"actor is {actor.name}");
                     TFTVLogger.Always($"damagedealer null? {damageDealer == null}");
                     TFTVLogger.Always($"damagePayload null? {damageDealer.GetDamagePayload() == null}");*/

                    if (damageDealer.GetDamagePayload() == null || actor == null) //|| damageDealer.GetDamagePayload().DamageKeywords.Count==0) 
                    {
                        return;
                    }

                    if (damageDealer.GetDamagePayload().DamageKeywords.Count() == 0)
                    {
                        return;

                    }

                    //TFTVLogger.Always($"running ActorDamageDealt damage: {damageDealer.GetDamagePayload()?.DamageKeywords?.First()?.DamageKeywordDef.name}");

                    if (actor.IsAlive)
                    {
                        if (actor.TacticalActorDef == dcoy && damageDealer != null && damageDealer != null && !damageDealer.GetDamagePayload().DamageKeywords.First().DamageKeywordDef.Equals(Shared.SharedDamageKeywords.ShockKeyword) && damageDealer.GetTacticalActorBase() != null)
                        {
                            TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                            TacticalActor attacker = attackerBase as TacticalActor;

                            if (!attacker.IsControlledByPlayer)
                            {
                                //Decoy despawned if attacked by Siren or Scylla
                                if (attacker.GameTags.Contains(sirenTag) || attacker.GameTags.Contains(queenTag)
                                    || attacker.GameTags.Contains(hopliteTag) || attacker.GameTags.Contains(cyclopsTag))
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                    TFTVHints.TacticalHints.ShowStoryPanel(actor.TacticalLevel, "HintDecoyDiscovered");
                                }
                                //Decoy despawned if attacked within 5 tiles by human, triton or acheron
                                else if ((attacker.GameTags.Contains(tritonTag)
                                    || attacker.GameTags.Contains(humanTag)
                                    || attacker.GameTags.Contains(acheronTag))
                                    && (actor.Pos - attacker.Pos).magnitude <= 5)
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
