using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal static class ResearchAndManufacturing
    {
        internal const float WorkerOutputPerSlot = 4.0f;

        private const int UnoccupiedResearchPerSlot = 1;
        private const int UnoccupiedProductionPerSlot = 2;
        private const string CentralizedAIResearchId = "NJ_CentralizedAI_ResearchDef";

        internal static void ApplyProductionAdjustments(GeoFaction faction)
        {
            if (!BaseReworkUtils.BaseReworkEnabled)
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

            GetOutputBonuses(phoenixFaction, out float researchBonus, out float productionBonus);

            phoenixFaction.ResourceIncome.SetOutput(OperationReason.Production, new ResourcePack(new ResourceUnit[]
            {
                new ResourceUnit(ResourceType.Research, Mathf.Max(0f, baseResearch + researchBonus)),
                new ResourceUnit(ResourceType.Production, Mathf.Max(0f, baseProduction + productionBonus))
            }));
        }

        internal static void GetOutputBonuses(GeoPhoenixFaction faction, out float researchBonus, out float productionBonus)
        {
            researchBonus = 0f;
            productionBonus = 0f;

            if (!BaseReworkUtils.BaseReworkEnabled || faction == null)
            {
                return;
            }

            PoolAssignmentSnapshot snapshot = BuildPoolAssignmentSnapshot(faction);
            researchBonus = GetAssignedBonus(snapshot.ResearchAssigned) + GetIdleSlotBonus(faction, snapshot.ResearchCapacity, snapshot.ResearchAssigned, ResourceType.Research, UnoccupiedResearchPerSlot);
            productionBonus = GetAssignedBonus(snapshot.ManufacturingAssigned) + GetIdleSlotBonus(faction, snapshot.ManufacturingCapacity, snapshot.ManufacturingAssigned, ResourceType.Production, UnoccupiedProductionPerSlot);
        }

        private static ResourcePack GetProductionReasonOutput(GeoPhoenixFaction faction)
        {
            if (faction?.ResourceIncome == null)
            {
                return new ResourcePack();
            }

            try
            {
                MethodInfo getOutputMethod = AccessTools.Method(faction.ResourceIncome.GetType(), "GetOutput", new[] { typeof(OperationReason) });
                if (getOutputMethod != null)
                {
                    object output = getOutputMethod.Invoke(faction.ResourceIncome, new object[] { OperationReason.Production });
                    if (output is ResourcePack productionOutput)
                    {
                        return productionOutput;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return new ResourcePack(new ResourceUnit[]
            {
                new ResourceUnit(ResourceType.Research, faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value),
                new ResourceUnit(ResourceType.Production, faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value)
            });
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

        private static float GetAssignedBonus(int assignedSlots)
        {
            return assignedSlots * WorkerOutputPerSlot;
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
