using Base.Entities;
using Base.Entities.Statuses;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TFTV
{
    internal class TFTVSuppression
    {
        private static DefCache DefCache = TFTVMain.Main.DefCache;

        /// <summary>
        /// Indicates the intensity of suppression accumulated by a tactical actor.
        /// </summary>
        public enum SuppressionLevel
        {
            None = 0,
            Light,
            Moderate,
            Heavy
        }

        /// <summary>
        /// Represents the action point penalty that should be applied at the start of an actor's turn.
        /// </summary>
        public readonly struct SuppressionPenalty
        {
            public static readonly SuppressionPenalty None = new SuppressionPenalty(SuppressionLevel.None, 0f);

            public SuppressionPenalty(SuppressionLevel level, float penaltyFraction)
            {
                Level = level;
                PenaltyFraction = penaltyFraction;
            }

            /// <summary>
            /// Gets the suppression level associated with the penalty.
            /// </summary>
            public SuppressionLevel Level { get; }

            /// <summary>
            /// Gets the fraction of the actor's action points that should be removed.
            /// </summary>
            public float PenaltyFraction { get; }

            /// <summary>
            /// True when the penalty actually reduces action points.
            /// </summary>
            public bool HasPenalty => Level != SuppressionLevel.None && PenaltyFraction > 0f;
        }

        /// <summary>
        /// Provides runtime helpers and Harmony patches for the suppression system so that the base game code can remain untouched.
        /// </summary>
        public static class SuppressionRuntime
        {
            private sealed class ActorSuppressionState
            {
                public readonly SuppressionTracker Tracker = new SuppressionTracker();
                public SuppressionLevel CurrentLevel = SuppressionLevel.None;
                public SuppressionLevel DisplayedLevel = SuppressionLevel.None;
                public CoverPose? ForcedPose;
            }

            private static readonly ConditionalWeakTable<TacticalActor, ActorSuppressionState> ActorStates = new ConditionalWeakTable<TacticalActor, ActorSuppressionState>();

            private static readonly object StatusSource = new object();

            private static bool IsSuppressionEnabled => TFTVMain.Main?.Config?.TFTVSuppression ?? true;

            private static ActorSuppressionState GetActorState(TacticalActor actor)
            {
                return ActorStates.GetValue(actor, _ => new ActorSuppressionState());
            }

            /// <summary>
            /// Registers a suppression event for the supplied actor and weapon combination.
            /// </summary>
            public static void RegisterSuppressionEvent(TacticalActor actor, Weapon weapon, float weight = 1f)
            {
                if (actor == null || !actor.IsAlive || weapon == null || weapon.WeaponDef == null|| !weapon.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("GunWeapon_TagDef")))
                {
                    return;
                }

                ActorSuppressionState state = GetActorState(actor);
                state.Tracker.RegisterEvent(actor, weapon, weight);

                if (ShouldApplyImmediatePenalty(actor))
                {
                    SuppressionPenalty pendingPenalty = SuppressionSettings.GetPenaltyFor(state.Tracker.PendingSuppression, actor);
                    if (pendingPenalty.HasPenalty)
                    {
                        SuppressionLevel desiredLevel = (SuppressionLevel)Mathf.Max((int)state.CurrentLevel, (int)pendingPenalty.Level);
                        float previousFraction = SuppressionSettings.GetPenaltyFraction(state.CurrentLevel);
                        float desiredFraction = SuppressionSettings.GetPenaltyFraction(desiredLevel);
                        float fractionDelta = desiredFraction - previousFraction;

                        state.CurrentLevel = desiredLevel;

                        if (fractionDelta > 0f && actor.IsAlive)
                        {
                            float apToRemove = actor.CharacterStats.ActionPoints.Max * fractionDelta;
                            if (apToRemove > 0f)
                            {
                                actor.CharacterStats.ActionPoints.Subtract(apToRemove);
                            }
                        }
                    }
                }

                UpdateSuppressionStatus(actor, state);
            }

            /// <summary>
            /// Applies any pending suppression penalty to the actor's action points and returns the consumed penalty information.
            /// </summary>
            public static SuppressionPenalty ApplySuppressionPenalty(TacticalActor actor)
            {
                if (actor == null)
                {
                    return SuppressionPenalty.None;
                }

                ActorSuppressionState state = GetActorState(actor);
                SuppressionPenalty penalty = state.Tracker.ConsumePendingPenalty(actor);
                float previousFraction = SuppressionSettings.GetPenaltyFraction(state.CurrentLevel);
                state.CurrentLevel = penalty.Level;
                float desiredFraction = SuppressionSettings.GetPenaltyFraction(state.CurrentLevel);
                float fractionDelta = desiredFraction - previousFraction;

                if (actor.IsAlive && penalty.HasPenalty && fractionDelta > 0f)
                {
                    float apToRemove = actor.CharacterStats.ActionPoints.Max * fractionDelta;
                    if (apToRemove > 0f)
                    {
                        actor.CharacterStats.ActionPoints.Subtract(apToRemove);
                    }
                }

                UpdateSuppressionStatus(actor, state);
                return penalty;
            }

            /// <summary>
            /// Returns the current suppression level recorded for the actor.
            /// </summary>
            public static SuppressionLevel GetCurrentSuppressionLevel(TacticalActor actor)
            {
                if (actor == null)
                {
                    return SuppressionLevel.None;
                }

                ActorSuppressionState state;
                if (ActorStates.TryGetValue(actor, out state))
                {
                    return state.CurrentLevel;
                }

                return SuppressionLevel.None;
            }

            /// <summary>
            /// Returns the suppression tracker associated with the actor, creating one if necessary.
            /// </summary>
            public static SuppressionTracker GetTracker(TacticalActor actor)
            {
                if (actor == null)
                {
                    return null;
                }

                return GetActorState(actor).Tracker;
            }

            /// <summary>
            /// Clears the accumulated suppression for the specified actor.
            /// </summary>
            public static void ClearSuppression(TacticalActor actor)
            {
                if (actor == null)
                {
                    return;
                }

                if (!ActorStates.TryGetValue(actor, out ActorSuppressionState state))
                {
                    return;
                }

                state.Tracker.Clear();
                state.CurrentLevel = SuppressionLevel.None;
                UpdateSuppressionStatus(actor, state);
            }

            /// <summary>
            /// Clears suppression for all actors in the supplied faction.
            /// </summary>
            public static void ClearSuppressionForFaction(TacticalFaction faction)
            {
                if (faction == null)
                {
                    return;
                }

                foreach (TacticalActor tacticalActor in faction.TacticalActors)
                {
                    ClearSuppression(tacticalActor);
                }
            }

            internal static void TryRegisterSuppressionNearMiss(ProjectileLogic logic, CastHit hit)
            {
                if (logic == null || logic.IsSimulation)
                {
                    return;
                }

                TacticalActor shooter = logic.TacticalActor;
                if (shooter == null || !shooter.IsAlive || hit.Collider == null)
                {
                    return;
                }

                if (hit.Collider.GetComponentInParent<TacticalActorBase>() != null)
                {
                    return;
                }

                TacticalMap map = shooter.TacticalLevel.Map;
                if (map == null)
                {
                    return;
                }

                Vector3 impactPoint = hit.Point;
                float radius = SuppressionSettings.NearMissRadius;
                float sqrRadius = radius * radius;

                foreach (TacticalActor nearbyActor in map.GetActors((TacticalActor candidate) => candidate.IsAlive))
                {
                    if (nearbyActor == shooter)
                    {
                        continue;
                    }

                    Vector3 offset = nearbyActor.Pos - impactPoint;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= sqrRadius)
                    {
                        RegisterSuppressionEvent(nearbyActor, logic.Weapon, 1f);
                    }
                }
            }

            private static bool ShouldApplyImmediatePenalty(TacticalActor actor)
            {
                TacticalLevelController level = actor?.TacticalLevel;
                if (level == null)
                {
                    return false;
                }

                TacticalFaction currentFaction = level.CurrentFaction;
                if (currentFaction == null || currentFaction != actor.TacticalFaction)
                {
                    return false;
                }

                if (!level.TurnIsPlaying || !currentFaction.IsPlayingTurn)
                {
                    return false;
                }

                return true;
            }

            private static void UpdateSuppressionStatus(TacticalActor actor, ActorSuppressionState state)
            {
                if (actor == null)
                {
                    return;
                }

                SuppressionLevel pendingLevel = SuppressionSettings.GetPenaltyFor(state.Tracker.PendingSuppression, actor).Level;
                SuppressionLevel desiredLevel = (SuppressionLevel)Mathf.Max((int)pendingLevel, (int)state.CurrentLevel);

                MaintainSuppressionPose(actor, state, desiredLevel);

                if (state.DisplayedLevel == desiredLevel)
                {
                    return;
                }

                StatusComponent statusComponent = actor.Status;
                if (statusComponent == null)
                {
                    state.DisplayedLevel = SuppressionLevel.None;
                    return;
                }

                if (state.DisplayedLevel != SuppressionLevel.None)
                {
                    TacStatusDef previousDef = SuppressionStatuses.GetStatusDef(state.DisplayedLevel);
                    if (previousDef != null)
                    {
                        TacStatus previousStatus = statusComponent.GetStatus<TacStatus>(previousDef);
                        if (previousStatus != null)
                        {
                            statusComponent.UnapplyStatus(previousStatus);
                        }
                    }
                }

                if (desiredLevel != SuppressionLevel.None)
                {
                    TacStatusDef desiredDef = SuppressionStatuses.GetStatusDef(desiredLevel);
                    if (desiredDef != null)
                    {
                        statusComponent.ApplyStatus(desiredDef, StatusSource);
                    }
                }

                state.DisplayedLevel = desiredLevel;
            }

            private static void MaintainSuppressionPose(TacticalActor actor, ActorSuppressionState state, SuppressionLevel desiredLevel)
            {
                if (actor == null || !actor.IsAlive)
                {
                    state.ForcedPose = null;
                    return;
                }

                IdleAbility idleAbility = actor.IdleAbility;
                if (idleAbility == null)
                {
                    state.ForcedPose = null;
                    return;
                }

                bool shouldKneel = desiredLevel != SuppressionLevel.None;
                if (shouldKneel)
                {
                    CoverPose pose;
                    if (!state.ForcedPose.HasValue || NeedsPoseRefresh(actor, state.ForcedPose.Value))
                    {
                        pose = CreateSuppressionPose(actor);
                        state.ForcedPose = pose;
                    }
                    else
                    {
                        pose = state.ForcedPose.Value;
                    }

                    idleAbility.ForceRefresh(false, pose);
                }
                else if (state.ForcedPose.HasValue)
                {
                    state.ForcedPose = null;
                    idleAbility.ForceRefresh(false, null);
                }
            }

            private static bool NeedsPoseRefresh(TacticalActor actor, CoverPose pose)
            {
                Vector3 actorPos = actor.Pos;
                Vector3 diff = actorPos - pose.GridPos;
                diff.y = 0f;
                if (diff.sqrMagnitude > 0.01f)
                {
                    return true;
                }

                Vector3 currentForward = GetActorForward(actor);
                if (currentForward.sqrMagnitude < 0.0001f)
                {
                    return false;
                }

                return Vector3.Dot(currentForward, pose.FaceDir) < 0.99f;
            }

            private static CoverPose CreateSuppressionPose(TacticalActor actor)
            {
                Vector3 forward = GetActorForward(actor);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector3.forward;
                }
                forward.Normalize();

                CoverInfo coverInfo = CoverInfo.NoCover;
                coverInfo.CoverType = CoverType.Low;
                coverInfo.DirectionFromSource = -forward;

                return new CoverPose
                {
                    CoverInfo = coverInfo,
                    FaceDir = forward,
                    GridPos = actor.Pos,
                    LookAtTarget = null,
                    LookAtUsedWeapon = null
                };
            }

            private static Vector3 GetActorForward(TacticalActor actor)
            {
                Vector3 forward = Vector3.forward;
                Transform transform = actor?.transform;
                if (transform != null)
                {
                    forward = transform.forward;
                }

                forward.y = 0f;
                if (forward.sqrMagnitude < 1E-06f)
                {
                    forward = Vector3.forward;
                }

                return forward.normalized;
            }

            private static void RegisterDirectHitSuppression(TacticalActor actor, DamageResult damageResult)
            {
                Weapon weapon = TacUtil.GetSourceOfType<Weapon>(damageResult.Source);
                RegisterSuppressionEvent(actor, weapon, 1f);
            }

            [HarmonyPatch]
            private static class Patches
            {
                [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.StartTurn))]
                private static class TacticalActor_StartTurn_Patch
                {
                    private static void Postfix(TacticalActor __instance)
                    {
                        if (!IsSuppressionEnabled) return;
                        ApplySuppressionPenalty(__instance);
                    }
                }

                [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.ForceRestartTurn))]
                private static class TacticalActor_ForceRestartTurn_Patch
                {
                    private static void Postfix(TacticalActor __instance)
                    {
                        if (!IsSuppressionEnabled) return;
                        ApplySuppressionPenalty(__instance);
                    }
                }

                [HarmonyPatch(typeof(TacticalActor), "ApplyDamageInternal")]
                private static class TacticalActor_ApplyDamageInternal_Patch
                {
                    private static void Postfix(TacticalActor __instance, DamageResult damageResult)
                    {
                        if (!IsSuppressionEnabled) return;
                        RegisterDirectHitSuppression(__instance, damageResult);
                    }
                }

                [HarmonyPatch(typeof(ProjectileLogic), "AffectTarget")]
                private static class ProjectileLogic_AffectTarget_Patch
                {
                    private static void Postfix(ProjectileLogic __instance, CastHit hit)
                    {
                        if (!IsSuppressionEnabled) return; 
                      

                        TryRegisterSuppressionNearMiss(__instance, hit);
                    }
                }

                [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.EndTurn))]
                private static class TacticalFaction_EndTurn_Patch
                {
                    private static void Postfix(TacticalFaction __instance)
                    {
                        if (!IsSuppressionEnabled) return;
                        ClearSuppressionForFaction(__instance);
                    }
                }
            }
        }

        /// <summary>
        /// Static configuration used by the suppression system.
        /// </summary>
        public static class SuppressionSettings
        {
            public const float LightSuppressionThreshold = 5f;
            public const float ModerateSuppressionThreshold = 10f;
            public const float HeavySuppressionThreshold = 15f;

            public const float LightSuppressionPenalty = 0.125f;
            public const float ModerateSuppressionPenalty = 0.25f;
            public const float HeavySuppressionPenalty = 0.375f;

            public const float MaxSuppressionPoints = HeavySuppressionThreshold;

            /// <summary>
            /// Radius in world units that counts as a near miss when a projectile hits non-actor geometry.
            /// </summary>
            public const float NearMissRadius = 1.5f;

            /// <summary>
            /// Calculates the penalty that should be applied for the provided suppression amount.
            /// </summary>
            public static SuppressionPenalty GetPenaltyFor(float suppressionPoints, TacticalActor actor = null)
            {
                float multiplier = GetThresholdMultiplier(actor);
                float heavyThreshold = HeavySuppressionThreshold * multiplier;
                float moderateThreshold = ModerateSuppressionThreshold * multiplier;
                float lightThreshold = LightSuppressionThreshold * multiplier;

                if (suppressionPoints >= heavyThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Heavy, HeavySuppressionPenalty);
                }

                if (suppressionPoints >= moderateThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Moderate, ModerateSuppressionPenalty);
                }

                if (suppressionPoints >= lightThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Light, LightSuppressionPenalty);
                }

                return SuppressionPenalty.None;
            }

            /// <summary>
            /// Returns the penalty fraction associated with the specified level.
            /// </summary>
            public static float GetPenaltyFraction(SuppressionLevel level)
            {
                switch (level)
                {
                    case SuppressionLevel.Light:
                        return LightSuppressionPenalty;
                    case SuppressionLevel.Moderate:
                        return ModerateSuppressionPenalty;
                    case SuppressionLevel.Heavy:
                        return HeavySuppressionPenalty;
                    default:
                        return 0f;
                }
            }

            /// <summary>
            /// Returns the maximum suppression that should be stored for the supplied actor.
            /// </summary>
            public static float GetMaxSuppressionFor(TacticalActor actor)
            {
                return HeavySuppressionThreshold * GetThresholdMultiplier(actor);
            }

            /// <summary>
            /// Clamps the incoming value to the maximum allowed suppression.
            /// </summary>
            public static float ClampSuppression(float value, TacticalActor actor = null)
            {
                float maxSuppression = GetMaxSuppressionFor(actor);
                return Mathf.Clamp(value, 0f, maxSuppression);
            }

            private static float GetThresholdMultiplier(TacticalActor actor)
            {
                NavMeshNavigationComponent navMeshNavigationComponent = actor?.NavigationComponent as NavMeshNavigationComponent;
                if (navMeshNavigationComponent != null)
                {
                    float agentRadius = navMeshNavigationComponent.AgentNavSettings.AgentRadius;
                    if (agentRadius > 2f)
                    {
                        return 3f;
                    }

                    if (agentRadius > 1f)
                    {
                        return 2f;
                    }
                }

                return 1f;
            }
        }

        /// <summary>
        /// Provides runtime status definitions that mirror the three suppression tiers.
        /// </summary>
        public static class SuppressionStatuses
        {
            /// <summary>
            /// Tac status shown when an actor is lightly suppressed.
            /// </summary>
            public static TacStatusDef LightSuppressionStatus { get; } = CreateStatus(0,
                "{8214A4CE-DDA3-4059-9EEB-FE3BD22DE960}", "{0C8B14A3-2F02-4DAC-BE47-4A80A39366BC}");

            /// <summary>
            /// Tac status shown when an actor is moderately suppressed.
            /// </summary>
            public static TacStatusDef ModerateSuppressionStatus { get; } = CreateStatus(1,
                "{86BC2CB1-FA02-42B5-A987-7F754AEEC0EB}", "{F49EEACA-D08D-4BFF-ABBC-513F75D860B0}");

            /// <summary>
            /// Tac status shown when an actor is heavily suppressed.
            /// </summary>
            public static TacStatusDef HeavySuppressionStatus { get; } = CreateStatus(2,
                "{A7AF311C-22C6-44D8-95D7-6A9DFFF78C10}", "{C4D48E1F-7519-4EB8-87F4-13FAB6826EE3}");

            /// <summary>
            /// Returns the status definition that corresponds to the provided suppression level.
            /// </summary>
            public static TacStatusDef GetStatusDef(SuppressionLevel level)
            {
                switch (level)
                {
                    case SuppressionLevel.Light:
                        return LightSuppressionStatus;
                    case SuppressionLevel.Moderate:
                        return ModerateSuppressionStatus;
                    case SuppressionLevel.Heavy:
                        return HeavySuppressionStatus;
                    default:
                        return null;
                }
            }

            private static TacStatusDef CreateStatus(int level, string guid0, string guid1)
            {
                

                string name = $"TFTV_SUPPRESSION_STATUS";
                string locKeyName = $"{name}_{level}_NAME";
                string locKeyDesc = $"{name}_{level}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Suppression_{level}.png");

                DamageMultiplierStatusDef sourceStatus = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);
                newStatus.Visuals = Helper.CreateDefFromClone(sourceStatus.Visuals, guid1, name);
                newStatus.Visuals.DisplayName1.LocalizationKey = locKeyName;
                newStatus.Visuals.Description.LocalizationKey = locKeyDesc;
                newStatus.Visuals.LargeIcon = icon;
                newStatus.Visuals.SmallIcon = icon;
                newStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                newStatus.Multiplier = 1;
                newStatus.DurationTurns = 1;
                newStatus.EffectName = name;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;

                return newStatus;
            }
        }

        /// <summary>
        /// Keeps track of suppression events accumulated against a specific actor.
        /// </summary>
        [Serializable]
        public class SuppressionTracker
        {
            private float _pendingSuppression;

            /// <summary>
            /// Registers a suppression event caused by the provided weapon.
            /// </summary>
            public void RegisterEvent(TacticalActor actor, Weapon weapon, float weight)
            {
                if (weight <= 0f)
                {
                    return;
                }

                float contribution = weight;

              //  TFTVLogger.Always($"{actor?.name} {weapon?.DisplayName} {weight}");

                if (weapon != null)
                {
                  

                    WeaponDef weaponDef = weapon.WeaponDef;
                    if (weaponDef != null && weaponDef.DamagePayload.ProjectilesPerShot > 1)
                    {
                        contribution = Mathf.Min(contribution/ weaponDef.DamagePayload.ProjectilesPerShot, 1f);
                      //  TFTVLogger.Always($"{actor?.name} {weapon?.DisplayName} {contribution}");
                    }
                }

                if (contribution <= 0f)
                {
                    return;
                }

                //TFTVLogger.Always($"_pendingSuppression: {_pendingSuppression} contribution: {contribution}");
                float newValue = _pendingSuppression + contribution;

                _pendingSuppression = SuppressionSettings.ClampSuppression(newValue, actor);
            }

            /// <summary>
            /// Consumes the currently accumulated suppression, returning the penalty to apply.
            /// </summary>
            public SuppressionPenalty ConsumePendingPenalty(TacticalActor actor)
            {
                if (_pendingSuppression <= 0f)
                {
                    return SuppressionPenalty.None;
                }

                SuppressionPenalty penalty = SuppressionSettings.GetPenaltyFor(_pendingSuppression, actor);
                _pendingSuppression = 0f;

                return penalty;
            }

            /// <summary>
            /// Clears all accumulated suppression without applying any penalty.
            /// </summary>
            public void Clear()
            {
                _pendingSuppression = 0f;
            }

            /// <summary>
            /// Returns the current suppression before it is consumed.
            /// </summary>
            public float PendingSuppression => _pendingSuppression;
        }
    }
}