using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;
using TFTV.TFTVIncidents;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal static class ResearchAndManufacturing
    {
        internal const float RegularWorkerOutputPerSlot = 2.0f;
        internal const float AffinityWorkerOutputPerSlot = 4.0f;
        internal const float SpecialistWorkerOutputPerSlot = 6.0f;

        private const int UnoccupiedResearchPerSlot = 1;
        private const int UnoccupiedProductionPerSlot = 2;
        private const string CentralizedAIResearchId = "NJ_CentralizedAI_ResearchDef";

        internal static void ApplyProductionAdjustments(GeoFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            GeoPhoenixFaction phoenixFaction = faction as GeoPhoenixFaction;
            if (phoenixFaction == null)
            {
                return;
            }

            ResourcePack baseProductionOutput = GetProductionReasonOutput(phoenixFaction);

            float baseResearch = baseProductionOutput.ByResourceType(ResourceType.Research).Value;
            float baseProduction = baseProductionOutput.ByResourceType(ResourceType.Production).Value;
            //   float baseSupplies = baseProductionOutput.ByResourceType(ResourceType.Supplies).Value;
            //   float baseMutagen = baseProductionOutput.ByResourceType(ResourceType.Mutagen).Value;

            // TFTVLogger.Always($"Base production output - Research: {baseResearch}, Production: {baseProduction}, Supplies: {baseSupplies}, Mutagen: {baseMutagen}");

            GetOutputBonuses(phoenixFaction, out float researchBonus, out float productionBonus);

            phoenixFaction.ResourceIncome.SetOutput(OperationReason.Production, new ResourcePack(new ResourceUnit[]
            {
                new ResourceUnit(ResourceType.Research, Mathf.Max(0f, baseResearch + researchBonus)),
                new ResourceUnit(ResourceType.Production, Mathf.Max(0f, baseProduction + productionBonus)),
               // new ResourceUnit(ResourceType.Supplies, baseSupplies),
               // new ResourceUnit(ResourceType.Mutagen, baseMutagen)
            }));
        }

        internal static void GetOutputBonuses(GeoPhoenixFaction faction, out float researchBonus, out float productionBonus)
        {
            researchBonus = 0f;
            productionBonus = 0f;

            if (faction == null)
            {
                return;
            }

            PoolAssignmentSnapshot snapshot = BuildPoolAssignmentSnapshot(faction);
            researchBonus = GetAssignedBonus(faction, PersonnelAssignment.Research, snapshot.ResearchAssigned, ResourceType.Research)
                + GetIdleSlotBonus(faction, snapshot.ResearchCapacity, snapshot.ResearchAssigned, ResourceType.Research, UnoccupiedResearchPerSlot);

            if (TFTVVoidOmens.VoidOmensCheck[6])
            {
                researchBonus *= 1.5f;
            }

            productionBonus = GetAssignedBonus(faction, PersonnelAssignment.Manufacturing, snapshot.ManufacturingAssigned, ResourceType.Production)
                + GetIdleSlotBonus(faction, snapshot.ManufacturingCapacity, snapshot.ManufacturingAssigned, ResourceType.Production, UnoccupiedProductionPerSlot);
        }

        private static ResourcePack GetProductionReasonOutput(GeoPhoenixFaction faction)
        {
            if (faction?.ResourceIncome == null)
            {
                return new ResourcePack();
            }

            try
            {

                return new ResourcePack(new ResourceUnit[]
                {
                new ResourceUnit(ResourceType.Research, faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value),
                new ResourceUnit(ResourceType.Production, faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value),
                    //  new ResourceUnit(ResourceType.Supplies, faction.ResourceIncome.GetTotalResouce(ResourceType.Supplies).Value),
                    //  new ResourceUnit(ResourceType.Mutagen, faction.ResourceIncome.GetTotalResouce(ResourceType.Mutagen).Value)

                });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static PoolAssignmentSnapshot BuildPoolAssignmentSnapshot(GeoPhoenixFaction faction)
        {
            Workers.FacilitySlotPools pools = Workers.ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            return new PoolAssignmentSnapshot
            {
                ResearchAssigned = pools.Research.UsedSlots,
                ResearchCapacity = pools.Research.ProvidedSlots,
                ManufacturingAssigned = pools.Manufacturing.UsedSlots,
                ManufacturingCapacity = pools.Manufacturing.ProvidedSlots
            };
        }

        private static float GetAssignedBonus(GeoPhoenixFaction faction, PersonnelAssignment assignment, int usedSlots, ResourceType resourceType)
        {
            if (usedSlots <= 0)
            {
                return 0f;
            }

            return PersonnelData.Assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction && person.Assignment == assignment)
                .OrderBy(person => person.Id)
                .Take(usedSlots)
                .Sum(person => GetWorkerOutput(person.Character, resourceType));
        }

        internal static float GetWorkerOutput(GeoCharacter character, ResourceType resourceType)
        {
            if (character == null
                || !LeaderSelection.TryGetCurrentAffinity(character, out LeaderSelection.AffinityApproach approach, out _))
            {
                return RegularWorkerOutputPerSlot;
            }

            switch (approach)
            {
                case LeaderSelection.AffinityApproach.Biotech:
                    return resourceType == ResourceType.Research ? SpecialistWorkerOutputPerSlot : RegularWorkerOutputPerSlot;
                case LeaderSelection.AffinityApproach.Machinery:
                    return resourceType == ResourceType.Production ? SpecialistWorkerOutputPerSlot : RegularWorkerOutputPerSlot;
                case LeaderSelection.AffinityApproach.Compute:
                case LeaderSelection.AffinityApproach.Occult:
                case LeaderSelection.AffinityApproach.PsychoSociology:
                    return AffinityWorkerOutputPerSlot;
                default:
                    return RegularWorkerOutputPerSlot;
            }
        }

        private static float GetIdleSlotBonus(GeoPhoenixFaction faction, int capacity, int assigned, ResourceType resourceType, int basePerSlot)
        {
            int idleSlots = Math.Max(0, capacity - assigned);
            if (idleSlots <= 0 || !HasFacilityBuff(faction, resourceType))
            {
                return 0f;
            }

            int perSlot = basePerSlot;
            if (HasCentralizedAI(faction))
            {
                perSlot++;
            }

            return idleSlots * perSlot;
        }

        private static bool HasCentralizedAI(GeoPhoenixFaction faction)
        {
            return faction?.Research != null && faction.Research.HasCompleted(CentralizedAIResearchId);
        }

        private static bool HasFacilityBuff(GeoPhoenixFaction faction, ResourceType resourceType)
        {
            return faction?.FacilityBuffs?.FacilityBuffs != null
                && faction.FacilityBuffs.FacilityBuffs.Any(buff => buff?.FacilityDef != null && FacilityProvidesOutput(buff.FacilityDef, resourceType));
        }

        private static bool FacilityProvidesOutput(PhoenixFacilityDef facilityDef, ResourceType resourceType)
        {
            if (facilityDef?.GeoFacilityComponentDefs == null)
            {
                return false;
            }

            foreach (GeoFacilityComponentDef component in facilityDef.GeoFacilityComponentDefs)
            {
                ResourceGeneratorFacilityComponentDef generator = component as ResourceGeneratorFacilityComponentDef;
                if (generator != null && generator.BaseResourcesOutput.ByResourceType(resourceType).Value > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private struct PoolAssignmentSnapshot
        {
            public int ResearchAssigned;
            public int ResearchCapacity;
            public int ManufacturingAssigned;
            public int ManufacturingCapacity;
        }
    }
}
