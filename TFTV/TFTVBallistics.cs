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
        public static Dictionary<Projectile, HashSet<ActorSlotKey>> ProjectileSlotName = new Dictionary<Projectile, HashSet<ActorSlotKey>>();

        // Prediction-time de-dupe keyed by DamageAccumulation when available
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DamageAccumulation, HashSet<ActorSlotKey>> PredictorScorpionSlots
            = new System.Runtime.CompilerServices.ConditionalWeakTable<DamageAccumulation, HashSet<ActorSlotKey>>();

        // Fallback prediction-time de-dupe when ____damageAccum is null: scope by the current ProjectileLogic
        private static readonly Dictionary<ProjectileLogic, HashSet<ActorSlotKey>> PredictorScorpionSlotsByLogic
            = new Dictionary<ProjectileLogic, HashSet<ActorSlotKey>>();

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

        public readonly struct ActorSlotKey : IEquatable<ActorSlotKey>
        {
            private readonly int _actorId;
            private readonly int _slotHash;

            public ActorSlotKey(TacticalActor actor, string slotName)
            {
                _actorId = actor != null ? actor.GetHashCode() : 0;
                _slotHash = !string.IsNullOrEmpty(slotName) ? slotName.GetHashCode() : 0;
            }

            public bool Equals(ActorSlotKey other)
            {
                return _actorId == other._actorId && _slotHash == other._slotHash;
            }

            public override bool Equals(object obj)
            {
                return obj is ActorSlotKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_actorId * 397) ^ _slotHash;
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "Die")] //VERIFIED
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

        [HarmonyPatch(typeof(ProjectileLogic), "OnProjectileHit")] //VERIFIED
        public static class ProjectileLogic_OnProjectileHit_Umbra_Patch
        {
            private static readonly MethodInfo AffectTargetMethod =
                typeof(ProjectileLogic).GetMethod("AffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);

            private static readonly WeaponDef ScorpionDef = DefCache.GetDef<WeaponDef>("AC_Scorpion_WeaponDef");
            private static readonly ClassTagDef UmbraClassTag = DefCache.GetDef<ClassTagDef>("Umbra_ClassTagDef");
            private static readonly SpawnedActorTagDef DecoyTag = DefCache.GetDef<SpawnedActorTagDef>("Decoy_SpawnedActorTagDef");

            [ThreadStatic]
            private static object[] _affectTargetArgs;

            private static void InvokeAffectTarget(ProjectileLogic instance, CastHit hit, Vector3 dir)
            {
                object[] args = _affectTargetArgs;
                if (args == null)
                {
                    args = new object[2];
                    _affectTargetArgs = args;
                }

                args[0] = hit;
                args[1] = dir;
                AffectTargetMethod.Invoke(instance, args);
            }

            public static bool Prefix(ProjectileLogic __instance, ref bool __result, DamageAccumulation ____damageAccum, CastHit hit, Vector3 dir)
            {
                try
                {
                    if (hit.Collider == null || hit.Collider.gameObject == null)
                    {
                        return true;
                    }

                    Vector3 pos = hit.Point;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    IDamageReceiver receiver = GetDamageReceiver(__instance.Predictor, hit.Collider.gameObject, pos, rot);

                    TacticalActor hitActor = receiver?.GetActor() as TacticalActor ?? receiver?.GetParent() as TacticalActor;
                    string slotName = receiver?.GetSlotName();

                    if (hitActor != null)
                    {
                        if (__instance.Projectile != null && (hitActor.HasGameTag(UmbraClassTag) || hitActor.HasGameTag(DecoyTag)))
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
                                InvokeAffectTarget(__instance, hit, dir);
                                ____damageAccum?.ResetToInitalAmount();

                                if (ProjectileActor.TryGetValue(__instance.Projectile, out var existing) && existing != null)
                                {
                                    existing.Add(hitActor);
                                }
                                else
                                {
                                    ProjectileActor[__instance.Projectile] = new List<TacticalActor> { hitActor };
                                }
                            }

                            return false;
                        }

                        bool isScorpionRuntime = __instance.Projectile != null
                            && __instance.Projectile.TryGetWeapon().WeaponDef == ScorpionDef;

                        bool isScorpionPredict = __instance.Predictor != null && !string.IsNullOrEmpty(slotName);

                        if ((isScorpionRuntime || isScorpionPredict) && !string.IsNullOrEmpty(slotName))
                        {
                            __result = false;
                            ActorSlotKey key = new ActorSlotKey(hitActor, slotName);

                            if (isScorpionRuntime)
                            {
                                if (!ProjectileSlotName.TryGetValue(__instance.Projectile, out var set) || set == null)
                                {
                                    set = new HashSet<ActorSlotKey>();
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
                                    InvokeAffectTarget(__instance, hit, dir);
                                    ____damageAccum?.ResetToInitalAmount();
                                    set.Add(key);
                                }
                            }
                            else
                            {
                                HashSet<ActorSlotKey> set = null;

                                if (____damageAccum != null)
                                {
                                    if (!PredictorScorpionSlots.TryGetValue(____damageAccum, out set))
                                    {
                                        set = new HashSet<ActorSlotKey>();
                                        PredictorScorpionSlots.Add(____damageAccum, set);
                                    }
                                }
                                else
                                {
                                    if (!PredictorScorpionSlotsByLogic.TryGetValue(__instance, out set) || set == null)
                                    {
                                        set = new HashSet<ActorSlotKey>();
                                        PredictorScorpionSlotsByLogic[__instance] = set;
                                    }
                                }

                                if (set.Contains(key))
                                {
                                    ____damageAccum?.ResetToInitalAmount();
                                }
                                else
                                {
                                    InvokeAffectTarget(__instance, hit, dir);
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

        [HarmonyPatch(typeof(DieAbility), nameof(DieAbility.Activate))]
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

                    if (damageDealer.GetDamagePayload() == null || actor == null)
                    {
                        return;
                    }

                    if (damageDealer.GetDamagePayload().DamageKeywords.Count() == 0)
                    {
                        return;
                    }

                    if (actor.IsAlive)
                    {
                        if (actor.TacticalActorDef == dcoy && damageDealer != null && damageDealer != null && !damageDealer.GetDamagePayload().DamageKeywords.First().DamageKeywordDef.Equals(Shared.SharedDamageKeywords.ShockKeyword) && damageDealer.GetTacticalActorBase() != null)
                        {
                            TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                            TacticalActor attacker = attackerBase as TacticalActor;

                            if (!attacker.IsControlledByPlayer)
                            {
                                if (attacker.GameTags.Contains(sirenTag) || attacker.GameTags.Contains(queenTag)
                                    || attacker.GameTags.Contains(hopliteTag) || attacker.GameTags.Contains(cyclopsTag))
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    TFTVHints.TacticalHints.ShowStoryPanel(actor.TacticalLevel, "HintDecoyDiscovered");
                                }
                                else if ((attacker.GameTags.Contains(tritonTag)
                                    || attacker.GameTags.Contains(humanTag)
                                    || attacker.GameTags.Contains(acheronTag))
                                    && (actor.Pos - attacker.Pos).magnitude <= 5)
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
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
