using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Eventus;
using Base.Serialization.General;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Eventus.Contexts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVAircraftRework
{


    [DefTarget(typeof(GroundAttackWeaponAbility))]
    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
    public class GroundAttackWeaponAbilityDef : TacticalAbilityDef
    {
        [Header("Ground Attack Weapon")]
        public List<DelayedEffectDef> ExplosionDefs = new List<DelayedEffectDef>();

        [Tooltip("Offsets applied to the selected target position when generating the bombardment pattern.")]
        public List<Vector3> ImpactOffsets = new List<Vector3>();

        [Tooltip("Maximum distance from the selected target position that an offset can be considered valid.")]
        public float PatternRadius = 4f;

        [Tooltip("Delay before the first strike is executed.")]
        public float PreImpactDelaySeconds = 0.2f;

        [Tooltip("Delay between consecutive strikes in the pattern.")]
        public float DelayBetweenStrikesSeconds = 0.5f;

        [Tooltip("Optional icons used to represent the bombardment level in tactical UI.")]
        public Sprite[] LevelIcons = System.Array.Empty<Sprite>();

        [Header("Projectile Settings")]
        public ProjectileDef ProjectileDef = null;

        [Tooltip("Audio event played when the projectile is spawned and travelling toward the target.")]
        public TacticalEventDef ProjectileIncomingAudioEvent = null;

        [Tooltip("Height above the impact point where the projectile is spawned.")]
        public float ProjectileSpawnHeight = 100f;

        [Tooltip("Speed at which the projectile travels toward the target.")]
        public float ProjectileSpeed = 30f;

        [Tooltip("Additional distance to check below the spawn point when raycasting for the ground.")]
        public float ProjectileMaxRaycastDistanceBuffer = 5f;

        [Tooltip("Layers considered valid ground for projectile raycasts.")]
        public LayerMask ProjectileRaycastMask = UnityLayers.BlockingAll;
    }

    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbility))]
    public class GroundAttackWeaponAbility : TacticalAbility
    {
        private int _volleyCount = 1;

        private GroundAttackWeaponAbilityDef GroundAttackWeaponAbilityDef => this.Def<GroundAttackWeaponAbilityDef>();

        public override bool HasValidTargets => TacticalActorBase != null && GetTargets().Any();

        public void ConfigureForLevel(int level)
        {
            _volleyCount = Mathf.Clamp(level, 1, Mathf.Max(1, GroundAttackWeaponAbilityDef.ExplosionDefs.Count));
            UpdateAbilityIcons(level);
        }

        public override void Activate(object parameter = null)
        {
            base.Activate(parameter);
            base.PlayAction(ExecuteBombardment, parameter, null);
        }

        public override IEnumerable<TacticalAbilityTarget> GetTargets(TacticalTargetData targetData, TacticalActorBase sourceActor, Vector3 sourcePosition)
        {
            if (targetData == GroundAttackWeaponAbilityDef.TargetingDataDef.Origin)
            {
                return GetBombardmentTargets(sourceActor, sourcePosition);
            }

            return base.GetTargets(targetData, sourceActor, sourcePosition);
        }

        private IEnumerator<NextUpdate> ExecuteBombardment(PlayingAction action)
        {
            TacticalAbilityTarget abilityTarget = action.Param as TacticalAbilityTarget;
            if (abilityTarget == null)
            {
                yield break;
            }

            Vector3 impactCenter = abilityTarget.PositionToApply;
            List<Vector3> impactPositions = GenerateImpactPattern(impactCenter);
            if (impactPositions.Count == 0)
            {
                yield break;
            }

            ProvideCameraHint(impactCenter);

            GroundAttackWeaponAbilityDef def = GroundAttackWeaponAbilityDef;

            if (def.PreImpactDelaySeconds > 0f)
            {
                yield return NextUpdate.Seconds(def.PreImpactDelaySeconds);
            }

            int explosionCount = Mathf.Min(_volleyCount, def.ExplosionDefs.Count);
            for (int i = 0; i < explosionCount; i++)
            {
                DelayedEffectDef explosion = def.ExplosionDefs[Mathf.Clamp(i, 0, def.ExplosionDefs.Count - 1)];
                Vector3 impactPosition = impactPositions[Mathf.Clamp(i, 0, impactPositions.Count - 1)];

                foreach (NextUpdate update in ResolveStrike(impactPosition, explosion))
                {
                    yield return update;
                }

                if (i < explosionCount - 1 && def.DelayBetweenStrikesSeconds > 0f)
                {
                    yield return NextUpdate.Seconds(def.DelayBetweenStrikesSeconds);
                }
            }

            TFTVAircraftReworkMain.Modules.Tactical.GroundAttackWeapon.RemoveGroundAttackWeaponModuleAbility();
        }

        private List<Vector3> GenerateImpactPattern(Vector3 center)
        {
            GroundAttackWeaponAbilityDef def = GroundAttackWeaponAbilityDef;
            int requiredCount = Mathf.Min(_volleyCount, Mathf.Max(1, def.ExplosionDefs.Count));
            List<Vector3> results = new List<Vector3>(requiredCount);

            foreach (Vector3 offset in def.ImpactOffsets)
            {
                Vector3 candidate = center + offset;
                if (Vector3.Distance(candidate, center) > def.PatternRadius)
                {
                    continue;
                }

                if (IsValidExplosionTile(candidate))
                {
                    results.Add(candidate);
                    if (results.Count >= requiredCount)
                    {
                        break;
                    }
                }
            }

            if (results.Count == 0 && IsValidExplosionTile(center))
            {
                results.Add(center);
            }

            while (results.Count < requiredCount)
            {
                results.Add(results.Last());
            }

            return results;
        }

        private IEnumerable<TacticalAbilityTarget> GetBombardmentTargets(TacticalActorBase sourceActor, Vector3 sourcePosition)
        {
            if (sourceActor == null)
            {
                return Enumerable.Empty<TacticalAbilityTarget>();
            }

            TacticalTargetData targetData = GroundAttackWeaponAbilityDef.TargetingDataDef.Target;
            if (targetData == null)
            {
                return Enumerable.Empty<TacticalAbilityTarget>();
            }

            return base.GetTargets(targetData, sourceActor, sourcePosition);
        }

        private bool IsValidExplosionTile(Vector3 candidatePos)
        {
            Vector3 rayStart = candidatePos + Vector3.up * 5f;
            Ray ray = new Ray(rayStart, Vector3.down);
            float maxDistance = 10f;
            return Physics.Raycast(ray, out _, maxDistance, UnityLayers.FloorAllMask);
        }

        private IEnumerable<NextUpdate> ResolveStrike(Vector3 position, DelayedEffectDef explosion)
        {
            GroundAttackWeaponAbilityDef def = GroundAttackWeaponAbilityDef;

            Vector3 spawnPoint = position + Vector3.up * Mathf.Max(0f, def.ProjectileSpawnHeight);
            float rayLength = def.ProjectileSpawnHeight + Mathf.Max(0f, def.ProjectileMaxRaycastDistanceBuffer);

            if (def.ProjectileDef == null || def.ProjectileSpawnHeight <= 0f)
            {
                PlayProjectileIncomingAudio(null, spawnPoint, position);
                ApplyExplosion(position, explosion);
                yield break;
            }

            if (Physics.Raycast(spawnPoint, Vector3.down, out RaycastHit hit, rayLength, def.ProjectileRaycastMask))
            {
                foreach (NextUpdate update in SpawnProjectileAndResolve(spawnPoint, hit.point, explosion))
                {
                    yield return update;
                }

                yield break;
            }

            PlayProjectileIncomingAudio(null, spawnPoint, position);
            ApplyExplosion(position, explosion);
        }

        private IEnumerable<NextUpdate> SpawnProjectileAndResolve(Vector3 spawnPoint, Vector3 impactPoint, DelayedEffectDef explosion)
        {
            Projectile projectileInstance = null;
            GroundAttackWeaponAbilityDef def = GroundAttackWeaponAbilityDef;

            if (def.ProjectileDef != null)
            {
                Vector3 direction = (impactPoint - spawnPoint).normalized;
                if (direction == Vector3.zero)
                {
                    direction = Vector3.down;
                }

                projectileInstance = base.Repo.Instantiate<Projectile>(
                    def.ProjectileDef,
                    null,
                    new Vector3?(spawnPoint),
                    new Quaternion?(Quaternion.LookRotation(direction)),
                    false);

                if (projectileInstance != null)
                {
                    projectileInstance.Setup(TacticalActor, this, TacticalActor?.Equipments?.SelectedWeapon);
                    projectileInstance.OnProjectileStart();
                }
            }

            Transform projectileTransform = projectileInstance != null ? projectileInstance.transform : null;

            PlayProjectileIncomingAudio(projectileTransform, spawnPoint, impactPoint);

            foreach (NextUpdate update in AnimateProjectile(projectileTransform, spawnPoint, impactPoint))
            {
                yield return update;
            }

            ApplyExplosion(impactPoint, explosion);

            if (projectileInstance != null)
            {
                if (projectileTransform != null)
                {
                    projectileTransform.position = impactPoint;
                }

                projectileInstance.OnTrajectoryEnd();
            }
        }

        private void PlayProjectileIncomingAudio(Transform projectileTransform, Vector3 spawnPoint, Vector3 impactPoint)
        {
            GroundAttackWeaponAbilityDef def = GroundAttackWeaponAbilityDef;
            TacticalEventDef audioEvent = def.ProjectileIncomingAudioEvent;
            if (audioEvent == null)
            {
                return;
            }

            EventusManager eventusManager = GameUtl.GameComponent<EventusManager>();
            if (eventusManager == null)
            {
                return;
            }

            TacticalActor tacticalActor = TacticalActor;
            BaseEventContext context;
            if (projectileTransform != null && tacticalActor != null)
            {
                TFTVLogger.Always($"context, first option");
                context = new TacActorEventContext(tacticalActor, tacticalActor, projectileTransform, null);
            }
            else
            {
                TFTVLogger.Always($"context, second option");
                Vector3 targetPosition = projectileTransform != null ? projectileTransform.position : spawnPoint;
                Quaternion? orientation = null;
                Vector3 travelVector = impactPoint - spawnPoint;
                if (travelVector.sqrMagnitude > 0.001f)
                {
                    orientation = Quaternion.LookRotation(travelVector.normalized);
                }

                object sender = (object)tacticalActor ?? this;
                context = new SimpleWorldEventContext(sender, projectileTransform, targetPosition, null, orientation);
            }

            TFTVLogger.Always($"playing event");

            eventusManager.PlayEventDirect(audioEvent, context);
        }

        private IEnumerable<NextUpdate> AnimateProjectile(Transform projectileTransform, Vector3 start, Vector3 end)
        {
            float distance = Vector3.Distance(start, end);
            float travelTime = distance / Mathf.Max(1f, GroundAttackWeaponAbilityDef.ProjectileSpeed);
            float elapsed = 0f;

            if (projectileTransform != null)
            {
                Vector3 direction = (end - start).normalized;
                if (direction == Vector3.zero)
                {
                    direction = Vector3.down;
                }

                projectileTransform.rotation = Quaternion.LookRotation(direction);
            }

            while (elapsed < travelTime)
            {
                elapsed += Timing.Delta;
                float t = Mathf.Clamp01(elapsed / travelTime);

                if (projectileTransform != null)
                {
                    projectileTransform.position = Vector3.Lerp(start, end, t);
                }

                yield return NextUpdate.NextFrame;
            }

            if (projectileTransform != null)
            {
                projectileTransform.position = end;
            }
        }

        private void ApplyExplosion(Vector3 position, DelayedEffectDef explosion)
        {
            Effect.Apply(base.Repo, explosion, new EffectTarget
            {
                Position = position
            }, null);
        }

        private void ProvideCameraHint(Vector3 focusPosition)
        {
            CameraDirector director = TacticalActor?.CameraDirector;
            if (director == null)
            {
                return;
            }

            director.Hint(CameraHint.ChaseTarget, new CameraChaseParams
            {
                ChaseVector = focusPosition,
                ChaseTransform = null,
                ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                LockCameraMovement = false,
                Instant = true,
                ChaseOnlyOutsideFrame = false,
                SnapToFloorHeight = true
            });
        }

        private void UpdateAbilityIcons(int level)
        {
            Sprite icon = GroundAttackWeaponAbilityDef.LevelIcons != null && level - 1 < GroundAttackWeaponAbilityDef.LevelIcons.Length
                ? GroundAttackWeaponAbilityDef.LevelIcons[Mathf.Max(level - 1, 0)]
                : null;

            if (icon != null)
            {
                GroundAttackWeaponAbilityDef.ViewElementDef.SmallIcon = icon;
                GroundAttackWeaponAbilityDef.ViewElementDef.LargeIcon = icon;
            }
        }
    }
}

