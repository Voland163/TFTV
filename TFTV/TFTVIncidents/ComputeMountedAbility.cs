using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class ComputeMountedAbility
    {
        [CreateAssetMenu(fileName = "MountedDriverPassiveAbilityDef", menuName = "Defs/Abilities/Tactical/MountedDriverPassive")]
        [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
        public class MountedDriverPassiveAbilityDef : TacticalAbilityDef
        {
            public ItemStatModification[] StatModifications;
        }

        public class MountedDriverPassiveAbility : TacticalAbility
        {
            public MountedDriverPassiveAbilityDef MountedDriverPassiveAbilityDef
            {
                get
                {
                    return (MountedDriverPassiveAbilityDef)base.BaseDef;
                }
            }

            public override void AbilityAdded()
            {
                base.AbilityAdded();
                base.TacticalActor.Status.OnStatusApplied += this.OnStatusChanged;
                base.TacticalActor.Status.OnStatusUnapplied += this.OnStatusChanged;
                this.RefreshMountedBuff();
            }

            public override void AbilityRemovingStart()
            {
                base.AbilityRemovingStart();
                base.TacticalActor.Status.OnStatusApplied -= this.OnStatusChanged;
                base.TacticalActor.Status.OnStatusUnapplied -= this.OnStatusChanged;
                this.RemoveMountedBuff();
            }

            private void OnStatusChanged(Status status)
            {
                if (status is MountedStatus)
                {
                    this.RefreshMountedBuff();
                }
            }

            private void RefreshMountedBuff()
            {
                TacticalActor mountedVehicleActor = this.GetMountedVehicleActor();
                if (this._buffedVehicleActor == mountedVehicleActor)
                {
                    return;
                }
                this.RemoveMountedBuff();
                if (mountedVehicleActor != null)
                {
                    this.ApplyMountedBuff(mountedVehicleActor);
                }
            }

            private TacticalActor GetMountedVehicleActor()
            {
                MountedStatus status = base.TacticalActor.Status.GetStatus<MountedStatus>();
                if (status == null)
                {
                    return null;
                }
                return status.VehicleActorBase as TacticalActor;
            }

            private void ApplyMountedBuff(TacticalActor vehicleActor)
            {
                bool flag = false;
                foreach (ItemStatModification itemStatModification in this.MountedDriverPassiveAbilityDef.StatModifications)
                {
                    BaseStat baseStat = vehicleActor.CharacterStats.TryGetStat(itemStatModification.TargetStat);
                    if (baseStat != null)
                    {
                        baseStat.AddStatModification(itemStatModification.GetStatModification(this), true);
                        flag |= this.ShouldUpdateStats(itemStatModification.TargetStat);
                    }
                }
                if (flag)
                {
                    vehicleActor.UpdateStats();
                }
                this._buffedVehicleActor = vehicleActor;
            }

            private void RemoveMountedBuff()
            {
                if (this._buffedVehicleActor == null)
                {
                    return;
                }
                bool flag = false;
                foreach (ItemStatModification itemStatModification in this.MountedDriverPassiveAbilityDef.StatModifications)
                {
                    BaseStat baseStat = this._buffedVehicleActor.CharacterStats.TryGetStat(itemStatModification.TargetStat);
                    if (baseStat != null)
                    {
                        baseStat.RemoveStatModificationsWithSource(this, true);
                        flag |= this.ShouldUpdateStats(itemStatModification.TargetStat);
                    }
                }
                if (flag)
                {
                    this._buffedVehicleActor.UpdateStats();
                }
                this._buffedVehicleActor = null;
            }

            private bool ShouldUpdateStats(StatModificationTarget statTarget)
            {
                return (statTarget & (StatModificationTarget.Speed | StatModificationTarget.Accuracy)) > StatModificationTarget.Endurance;
            }

            private TacticalActor _buffedVehicleActor;
        }

    }
}
