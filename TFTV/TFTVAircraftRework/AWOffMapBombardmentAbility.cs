using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Levels;
using Base.Serialization.General;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public float PreImpactDelaySeconds = 0f;

        [Tooltip("Delay between consecutive strikes in the pattern.")]
        public float DelayBetweenStrikesSeconds = 0.5f;

        [Tooltip("Optional icons used to represent the bombardment level in tactical UI.")]
        public Sprite[] LevelIcons = System.Array.Empty<Sprite>();
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
                ApplyExplosion(impactPositions[Mathf.Clamp(i, 0, impactPositions.Count - 1)], explosion);

                if (i < explosionCount - 1 && def.DelayBetweenStrikesSeconds > 0f)
                {
                    yield return NextUpdate.Seconds(def.DelayBetweenStrikesSeconds);
                }
            }

            TFTVAircraftReworkMain.Modules.Tactical.GroundAttackWeapon.RemoveGroundAttackWeaponModuleAbility(TacticalActor.TacticalLevel);
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

