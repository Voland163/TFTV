using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class ComputeMountedAbility
    {
        private const string DiagTag = "[Incidents][ComputeMountedAbility]";
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        internal static class Defs
        {
            private static readonly string[] AbilityGuids =
            {
                "f6db2db2-d7d4-4ef7-86a8-4fa52a877b01",
                "35c6f1c4-6f65-40e7-bb8d-b16b87dfce02",
                "8d2ec7eb-b77a-4fe3-a65e-5d3f2df72d03"
            };

            private static readonly string[] ViewGuids =
            {
                "2147d03e-13ef-4f31-95d3-8c72aab7d111",
                "1a0be9d8-ae87-42f7-8d35-d1d95f4fd112",
                "4c977e27-74fe-4123-8ee7-8f1a5e74ad13"
            };

            private static readonly string[] ProgressionGuids =
            {
                "8f2441f1-9f22-4448-a1fd-7b0271c0e221",
                "23c03d9e-2387-4c7d-8cf8-4dc3a479e222",
                "d5321f20-2a17-4ec2-86d0-c0b7cb127223"
            };

            private static readonly MountedDriverPassiveAbilityDef[] MountedDriverPassiveAbilities =
            {
                null,
                null,
                null
            };

            internal static void CreateDefs()
            {
                for (int rank = 1; rank <= 3; rank++)
                {
                    GetMountedDriverPassiveAbilityDef(rank);
                }
            }

            internal static MountedDriverPassiveAbilityDef GetMountedDriverPassiveAbilityDef(int rank)
            {
                if (rank < 1 || rank > 3)
                {
                    return null;
                }

                int index = rank - 1;

                if (MountedDriverPassiveAbilities[index] == null)
                {
                    MountedDriverPassiveAbilities[index] = CreateMountedDriverPassiveAbilityDef(index);
                }

                return MountedDriverPassiveAbilities[index];
            }

            private static MountedDriverPassiveAbilityDef CreateMountedDriverPassiveAbilityDef(int index)
            {
                try
                {
                    int rank = index + 1;
                    string skillName = $"ComputeMountedDriverPassive_{rank}_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

                    MountedDriverPassiveAbilityDef mountedDriverPassiveAbility =
                        Repo.GetDef(AbilityGuids[index]) as MountedDriverPassiveAbilityDef;

                    if (mountedDriverPassiveAbility == null)
                    {
                        mountedDriverPassiveAbility = Repo.CreateDef<MountedDriverPassiveAbilityDef>(AbilityGuids[index]);
                        Helper.CopyFieldsByReflection(source, mountedDriverPassiveAbility);
                        mountedDriverPassiveAbility.name = skillName;
                        DefCache.AddDef(mountedDriverPassiveAbility.name, mountedDriverPassiveAbility.Guid);
                    }

                    mountedDriverPassiveAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        ProgressionGuids[index],
                        skillName);

                    mountedDriverPassiveAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        ViewGuids[index],
                        skillName);

                    mountedDriverPassiveAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind("Mounted Driver", true);
                    mountedDriverPassiveAbility.ViewElementDef.Description = new LocalizedTextBind(
                        string.Format(
                            "Ground vehicles with this character inside gain +{0} Speed and +{1}% accuracy.",
                            3 * rank,
                            10 * rank),
                        true);

                    mountedDriverPassiveAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_AffinityIcon_COMPUTE_0.png");
                    mountedDriverPassiveAbility.ViewElementDef.SmallIcon = mountedDriverPassiveAbility.ViewElementDef.LargeIcon;

                    mountedDriverPassiveAbility.Rank = rank;
                    mountedDriverPassiveAbility.StatModifications = new ItemStatModification[]
                    {
                    new ItemStatModification
                    {
                        TargetStat = StatModificationTarget.Speed,
                        Modification = StatModificationType.Add,
                        Value = 3f * rank
                    },
                    new ItemStatModification
                    {
                        TargetStat = StatModificationTarget.Accuracy,
                        Modification = StatModificationType.Add,
                        Value = 0.10f * rank
                    }
                    };

                    return mountedDriverPassiveAbility;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }
        }

        [CreateAssetMenu(fileName = "MountedDriverPassiveAbilityDef", menuName = "Defs/Abilities/Tactical/MountedDriverPassive")]
        [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
        public class MountedDriverPassiveAbilityDef : TacticalAbilityDef
        {
            public int Rank;
            public ItemStatModification[] StatModifications;
        }

        [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbility))]
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

                if (base.TacticalActor?.Status == null)
                {
                    return;
                }

                base.TacticalActor.Status.OnStatusApplied += this.OnStatusChanged;
                base.TacticalActor.Status.OnStatusUnapplied += this.OnStatusChanged;

                if (base.TacticalActor.TacticalLevel != null)
                {
                    base.TacticalActor.TacticalLevel.AbilityExecutedEvent += this.OnAbilityExecuted;
                }

                this.RefreshMountedBuffState();
            }

            public override void AbilityRemovingStart()
            {
                TacticalActor previousVehicle = this._buffedVehicleActor ?? this.GetMountedVehicleActor();

                if (base.TacticalActor?.Status != null)
                {
                    base.TacticalActor.Status.OnStatusApplied -= this.OnStatusChanged;
                    base.TacticalActor.Status.OnStatusUnapplied -= this.OnStatusChanged;
                }

                if (base.TacticalActor?.TacticalLevel != null)
                {
                    base.TacticalActor.TacticalLevel.AbilityExecutedEvent -= this.OnAbilityExecuted;
                }

                this.RemoveMountedBuff();
                this.RefreshMountedBuffsForVehicle(previousVehicle);

                base.AbilityRemovingStart();
            }

            private void OnStatusChanged(Status status)
            {
                if (!(status is MountedStatus))
                {
                    return;
                }

                this.RefreshMountedBuffState();
            }

            private void OnAbilityExecuted(TacticalAbility ability, object parameter)
            {
                if (ability == null || ability.TacticalActor != base.TacticalActor)
                {
                    return;
                }

                if (!(ability is EnterVehicleAbility) && !(ability is ExitVehicleAbility))
                {
                    return;
                }

                this.RefreshMountedBuffState();
            }

            private void RefreshMountedBuffState()
            {
                TacticalActor previousVehicle = this._buffedVehicleActor;
                TacticalActor currentVehicle = this.GetMountedVehicleActor();

                this.RefreshMountedBuff();

                if (previousVehicle != null)
                {
                    this.RefreshMountedBuffsForVehicle(previousVehicle);
                }

                if (currentVehicle != null && currentVehicle != previousVehicle)
                {
                    this.RefreshMountedBuffsForVehicle(currentVehicle);
                }
            }

            private void RefreshMountedBuff()
            {
                TacticalActor mountedVehicleActor = this.GetMountedVehicleActor();

                if (mountedVehicleActor == null)
                {
                    this.RemoveMountedBuff();
                    return;
                }

                if (!this.ShouldProvideMountedBuff(mountedVehicleActor))
                {
                    this.RemoveMountedBuff();
                    return;
                }

                if (this._buffedVehicleActor == mountedVehicleActor)
                {
                    return;
                }

                this.RemoveMountedBuff();
                this.ApplyMountedBuff(mountedVehicleActor);
            }

            private TacticalActor GetMountedVehicleActor()
            {
                MountedStatus status = base.TacticalActor?.Status?.GetStatus<MountedStatus>();

                if (status == null)
                {
                    return null;
                }

                return status.VehicleActorBase as TacticalActor;
            }

            private bool ShouldProvideMountedBuff(TacticalActor vehicleActor)
            {
                MountedDriverPassiveAbility bestAbility = GetBestMountedAbilityForVehicle(vehicleActor);
                return bestAbility == this;
            }

            private MountedDriverPassiveAbility GetBestMountedAbilityForVehicle(TacticalActor vehicleActor)
            {
                if (vehicleActor?.TacticalLevel == null)
                {
                    return null;
                }

                TacticalFaction phoenixFaction = vehicleActor.TacticalLevel.GetFactionByCommandName("PX");

                if (phoenixFaction?.Actors == null)
                {
                    return null;
                }

                MountedDriverPassiveAbility bestAbility = null;
                int bestRank = -1;
                string bestActorName = null;

                foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                {
                    TacticalActor actor = actorBase as TacticalActor;
                    if (actor == null)
                    {
                        continue;
                    }

                    foreach (MountedDriverPassiveAbility ability in actor.GetAbilities<MountedDriverPassiveAbility>())
                    {
                        if (ability == null || ability.GetMountedVehicleActor() != vehicleActor)
                        {
                            continue;
                        }

                        int rank = ability.MountedDriverPassiveAbilityDef != null
                            ? ability.MountedDriverPassiveAbilityDef.Rank
                            : 0;

                        string actorName = actor.DisplayName ?? actor.name ?? string.Empty;

                        if (bestAbility == null
                            || rank > bestRank
                            || (rank == bestRank && string.CompareOrdinal(actorName, bestActorName ?? string.Empty) < 0))
                        {
                            bestAbility = ability;
                            bestRank = rank;
                            bestActorName = actorName;
                        }
                    }
                }

                return bestAbility;
            }

            private void RefreshMountedBuffsForVehicle(TacticalActor vehicleActor)
            {
                if (vehicleActor?.TacticalLevel == null)
                {
                    return;
                }

                TacticalFaction phoenixFaction = vehicleActor.TacticalLevel.GetFactionByCommandName("PX");

                if (phoenixFaction?.Actors == null)
                {
                    return;
                }

                foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                {
                    TacticalActor actor = actorBase as TacticalActor;
                    if (actor == null)
                    {
                        continue;
                    }

                    foreach (MountedDriverPassiveAbility ability in actor.GetAbilities<MountedDriverPassiveAbility>())
                    {
                        if (ability == null)
                        {
                            continue;
                        }

                        TacticalActor mountedVehicle = ability.GetMountedVehicleActor();

                        if (mountedVehicle == vehicleActor || ability._buffedVehicleActor == vehicleActor)
                        {
                            ability.RefreshMountedBuff();
                        }
                    }
                }
            }

            private void ApplyMountedBuff(TacticalActor vehicleActor)
            {
                bool updateStats = false;

                foreach (ItemStatModification itemStatModification in this.MountedDriverPassiveAbilityDef.StatModifications)
                {
                    BaseStat baseStat = vehicleActor.CharacterStats.TryGetStat(itemStatModification.TargetStat);
                    if (baseStat != null)
                    {
                        baseStat.AddStatModification(itemStatModification.GetStatModification(this), true);
                        updateStats |= this.ShouldUpdateStats(itemStatModification.TargetStat);
                    }
                }

                if (updateStats)
                {
                    vehicleActor.UpdateStats();
                }

                this._buffedVehicleActor = vehicleActor;

                TFTVLogger.Always(
                    $"{DiagTag} Applied mounted vehicle buff from {base.TacticalActor?.DisplayName ?? base.TacticalActor?.name} to {vehicleActor.name} at rank {this.MountedDriverPassiveAbilityDef.Rank}.");
            }

            private void RemoveMountedBuff()
            {
                if (this._buffedVehicleActor == null)
                {
                    return;
                }

                bool updateStats = false;

                foreach (ItemStatModification itemStatModification in this.MountedDriverPassiveAbilityDef.StatModifications)
                {
                    BaseStat baseStat = this._buffedVehicleActor.CharacterStats.TryGetStat(itemStatModification.TargetStat);
                    if (baseStat != null)
                    {
                        baseStat.RemoveStatModificationsWithSource(this, true);
                        updateStats |= this.ShouldUpdateStats(itemStatModification.TargetStat);
                    }
                }

                if (updateStats)
                {
                    this._buffedVehicleActor.UpdateStats();
                }

                this._buffedVehicleActor = null;
            }

            private bool ShouldUpdateStats(StatModificationTarget statTarget)
            {
                return (statTarget & (StatModificationTarget.Speed | StatModificationTarget.Accuracy)) != 0;
            }

            private TacticalActor _buffedVehicleActor;
        }
    }
}
