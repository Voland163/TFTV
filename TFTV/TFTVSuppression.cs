using Base.Entities.Statuses;
using Base.Levels;
using HarmonyLib;
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

            private sealed class WeaponSuppressionState
            {
                public float ContributionMultiplier = 1f;
            }

            private static readonly ConditionalWeakTable<TacticalActor, ActorSuppressionState> ActorStates = new ConditionalWeakTable<TacticalActor, ActorSuppressionState>();
            private static readonly ConditionalWeakTable<WeaponDef, WeaponSuppressionState> WeaponStates = new ConditionalWeakTable<WeaponDef, WeaponSuppressionState>();
            private static readonly object StatusSource = new object();

            private static ActorSuppressionState GetActorState(TacticalActor actor)
            {
                return ActorStates.GetValue(actor, _ => new ActorSuppressionState());
            }

            internal static float GetWeaponContributionMultiplier(Weapon weapon)
            {
                WeaponDef weaponDef = weapon?.WeaponDef;
                if (weaponDef == null)
                {
                    return 1f;
                }

                WeaponSuppressionState state = WeaponStates.GetValue(weaponDef, _ => new WeaponSuppressionState());
                return Mathf.Max(state.ContributionMultiplier, 0f);
            }

            /// <summary>
            /// Sets the suppression multiplier for the provided weapon definition. Values below 1 treat projectile clusters as a single point of suppression.
            /// </summary>
            public static void SetWeaponContributionMultiplier(WeaponDef weaponDef, float multiplier)
            {
                if (weaponDef == null)
                {
                    return;
                }

                WeaponStates.GetValue(weaponDef, _ => new WeaponSuppressionState()).ContributionMultiplier = Mathf.Max(multiplier, 0f);
            }

            /// <summary>
            /// Returns the multiplier previously configured for the weapon definition, defaulting to 1 when unset.
            /// </summary>
            public static float GetWeaponContributionMultiplier(WeaponDef weaponDef)
            {
                if (weaponDef == null)
                {
                    return 1f;
                }

                WeaponSuppressionState state;
                if (WeaponStates.TryGetValue(weaponDef, out state))
                {
                    return Mathf.Max(state.ContributionMultiplier, 0f);
                }

                return 1f;
            }

            /// <summary>
            /// Registers a suppression event for the supplied actor and weapon combination.
            /// </summary>
            public static void RegisterSuppressionEvent(TacticalActor actor, Weapon weapon, float weight = 1f)
            {
                if (actor == null || !actor.IsAlive)
                {
                    return;
                }

                ActorSuppressionState state = GetActorState(actor);
                state.Tracker.RegisterEvent(weapon, weight);
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
                SuppressionPenalty penalty = state.Tracker.ConsumePendingPenalty();
                state.CurrentLevel = penalty.Level;
                if (actor.IsAlive && penalty.HasPenalty)
                {
                    float apToRemove = actor.CharacterStats.ActionPoints.Max * penalty.PenaltyFraction;
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

                foreach (TacticalActor actor in map.GetActors((TacticalActor candidate) => candidate.IsAlive))
                {
                    if (actor == shooter)
                    {
                        continue;
                    }

                    Vector3 offset = actor.Pos - impactPoint;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= sqrRadius)
                    {
                        RegisterSuppressionEvent(actor, logic.Weapon, 1f);
                    }
                }
            }

            private static void UpdateSuppressionStatus(TacticalActor actor, ActorSuppressionState state)
            {
                if (actor == null)
                {
                    return;
                }

                SuppressionLevel pendingLevel = SuppressionSettings.GetPenaltyFor(state.Tracker.PendingSuppression).Level;
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
                        ApplySuppressionPenalty(__instance);
                    }
                }

                [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.ForceRestartTurn))]
                private static class TacticalActor_ForceRestartTurn_Patch
                {
                    private static void Postfix(TacticalActor __instance)
                    {
                        ApplySuppressionPenalty(__instance);
                    }
                }

                [HarmonyPatch(typeof(TacticalActor), "ApplyDamageInternal")]
                private static class TacticalActor_ApplyDamageInternal_Patch
                {
                    private static void Postfix(TacticalActor __instance, DamageResult damageResult)
                    {
                        RegisterDirectHitSuppression(__instance, damageResult);
                    }
                }

                [HarmonyPatch(typeof(ProjectileLogic), "AffectTarget")]
                private static class ProjectileLogic_AffectTarget_Patch
                {
                    private static void Postfix(ProjectileLogic __instance, CastHit hit)
                    {
                        TryRegisterSuppressionNearMiss(__instance, hit);
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
            public static SuppressionPenalty GetPenaltyFor(float suppressionPoints)
            {
                if (suppressionPoints >= HeavySuppressionThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Heavy, HeavySuppressionPenalty);
                }

                if (suppressionPoints >= ModerateSuppressionThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Moderate, ModerateSuppressionPenalty);
                }

                if (suppressionPoints >= LightSuppressionThreshold)
                {
                    return new SuppressionPenalty(SuppressionLevel.Light, LightSuppressionPenalty);
                }

                return SuppressionPenalty.None;
            }

            /// <summary>
            /// Clamps the incoming value to the maximum allowed suppression.
            /// </summary>
            public static float ClampSuppression(float value)
            {
                return Mathf.Clamp(value, 0f, MaxSuppressionPoints);
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
                DefCache DefCache = TFTVMain.Main.DefCache;

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
            private readonly Dictionary<WeaponDef, float> _weaponContributions = new Dictionary<WeaponDef, float>();

            /// <summary>
            /// Registers a suppression event caused by the provided weapon.
            /// </summary>
            public void RegisterEvent(Weapon weapon, float weight)
            {
                if (weight <= 0f)
                {
                    return;
                }

                WeaponDef weaponDef = null;
                float modifier = 1f;
                if (weapon != null)
                {
                    weaponDef = weapon.WeaponDef;
                    modifier = SuppressionRuntime.GetWeaponContributionMultiplier(weapon);
                }

                float contribution = weight * modifier;
                if (contribution <= 0f)
                {
                    return;
                }

                _pendingSuppression = SuppressionSettings.ClampSuppression(_pendingSuppression + contribution);

                if (weaponDef != null)
                {
                    float total;
                    if (_weaponContributions.TryGetValue(weaponDef, out total))
                    {
                        _weaponContributions[weaponDef] = total + contribution;
                    }
                    else
                    {
                        _weaponContributions.Add(weaponDef, contribution);
                    }
                }
            }

            /// <summary>
            /// Consumes the currently accumulated suppression, returning the penalty to apply.
            /// </summary>
            public SuppressionPenalty ConsumePendingPenalty()
            {
                if (_pendingSuppression <= 0f)
                {
                    return SuppressionPenalty.None;
                }

                SuppressionPenalty penalty = SuppressionSettings.GetPenaltyFor(_pendingSuppression);
                _pendingSuppression = 0f;
                _weaponContributions.Clear();
                return penalty;
            }

            /// <summary>
            /// Returns the current suppression before it is consumed.
            /// </summary>
            public float PendingSuppression => _pendingSuppression;

            /// <summary>
            /// Returns the suppression contributions grouped by weapon definition.
            /// </summary>
            public IReadOnlyDictionary<WeaponDef, float> WeaponContributions => _weaponContributions;
        }
    }
}