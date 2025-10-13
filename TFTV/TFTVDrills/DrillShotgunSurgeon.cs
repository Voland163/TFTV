using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal class DrillShotgunSurgeon
    {
        [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
        public abstract class WeaponSpreadModifierStatus : TacStatus
        {
            public virtual float GetSpreadMultiplier(Weapon weapon, TacticalActor shooter, AttackType attackType, float targetDistance)
            {
                return 1f;
            }

            public override void OnApply(StatusComponent statusComponent)
            {
                base.OnApply(statusComponent);
                InvalidateSituationCache();
            }

            public override void OnUnapply()
            {
                base.OnUnapply();
                InvalidateSituationCache();
            }

            protected void InvalidateSituationCache()
            {
                TacticalLevel?.SituationCache?.Invalidate();
            }
        }

        [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
        [DefTarget(typeof(ShotgunSurgeonStatus))]
        public class ShotgunSurgeonStatusDef : TacStatusDef
        {
            public float RangeInTiles = 5f;

            public float SpreadMultiplier = 0.5f;

            public GameTagDef WeaponTag;

            public int MinimumProjectilesPerShot = 2;
        }

        [SerializeType(InheritCustomCreateFrom = typeof(WeaponSpreadModifierStatus))]
        public class ShotgunSurgeonStatus : WeaponSpreadModifierStatus
        {     
            public ShotgunSurgeonStatusDef ShotgunSurgeonStatusDef
            {
                get { return Def as ShotgunSurgeonStatusDef; }
            }

            public override float GetSpreadMultiplier(Weapon weapon, TacticalActor shooter, AttackType attackType, float targetDistance)
            {
                ShotgunSurgeonStatusDef statusDef = ShotgunSurgeonStatusDef;
                if (statusDef == null || weapon == null || shooter == null)
                {
                    return 1f;
                }

                if (statusDef.MinimumProjectilesPerShot > 0 && weapon.ProjectilesPerShot < statusDef.MinimumProjectilesPerShot)
                {
                    return 1f;
                }

                if (statusDef.WeaponTag != null)
                {
                    GameTagsList<GameTagDef> weaponTags = weapon.WeaponDef?.Tags;
                    if (weaponTags == null || !weaponTags.Contains(statusDef.WeaponTag))
                    {
                        return 1f;
                    }
                }

                if (targetDistance <= 0f)
                {
                    return 1f;
                }

                float rangeMeters = Mathf.Max(0f, statusDef.RangeInTiles) * TacticalMap.TileSize;
                float allowedDistance = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(rangeMeters, shooter);
                if (targetDistance > allowedDistance + 1E-05f)
                {
                    return 1f;
                }

                float multiplier = Mathf.Max(0f, statusDef.SpreadMultiplier);
                if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
                {
                    return 1f;
                }

                return multiplier;
            }
        }
    
    }
}
