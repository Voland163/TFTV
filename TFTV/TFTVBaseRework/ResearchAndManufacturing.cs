using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVIncidents;
using UnityEngine;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV.TFTVBaseRework
{
    internal static class ResearchAndManufacturing
    {
        internal const float RegularWorkerOutputPerSlot = 2.0f;
        internal const float AffinityWorkerOutputPerSlot = 4.0f;
        internal const float SpecialistWorkerOutputPerSlot = 6.0f;

        private static readonly MethodInfo OnIncomeChangedMethod =
            AccessTools.Method(typeof(GeoFaction), "OnIncomeChanged");

        internal static void ApplyProductionAdjustments(GeoFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            GeoPhoenixFaction phoenixFaction = faction as GeoPhoenixFaction;
            if (phoenixFaction?.ResourceIncome == null)
            {
                return;
            }

            // Recompute the raw site production the same way GeoFaction.UpdateProduction does, rather
            // than reading back whatever is currently stored. Reading the stored value would fold our
            // own bonus back in every time this runs outside the UpdateProduction postfix.
            ResourcePack sitePack = GetSiteProduction(phoenixFaction);

            float baseResearch = sitePack.ByResourceType(ResourceType.Research).Value;
            float baseProduction = sitePack.ByResourceType(ResourceType.Production).Value;

            GetOutputBonuses(phoenixFaction, out float researchBonus, out float productionBonus);

            float newResearch = Mathf.Max(0f, baseResearch + researchBonus);
            float newProduction = Mathf.Max(0f, baseProduction + productionBonus);

            // Keep every other resource the bases generate (food, mutagen, ...) exactly as the sites
            // reported it. SetOutput replaces the whole pack, so anything omitted here is lost income.
            List<ResourceUnit> units = sitePack.Values
                .Where(unit => unit.Type != ResourceType.Research && unit.Type != ResourceType.Production)
                .ToList();

            units.Add(new ResourceUnit(ResourceType.Research, newResearch));
            units.Add(new ResourceUnit(ResourceType.Production, newProduction));

            float previousResearch = phoenixFaction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value;
            float previousProduction = phoenixFaction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value;

            phoenixFaction.ResourceIncome.SetOutput(OperationReason.Production, new ResourcePack(units));

            // UpdateProduction raises IncomeChanged before this postfix runs, so the info bar would
            // otherwise keep showing the unadjusted figure until something else refreshes it.
            if (!Mathf.Approximately(previousResearch, newResearch) || !Mathf.Approximately(previousProduction, newProduction))
            {
                try
                {
                    OnIncomeChangedMethod?.Invoke(phoenixFaction, null);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static ResourcePack GetSiteProduction(GeoPhoenixFaction faction)
        {
            try
            {
                return new ResourcePack(faction.Sites
                    .Select(site => site.SiteProduction)
                    .Where(production => production != null)
                    .SelectMany(production => production.Values));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new ResourcePack();
            }
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

            researchBonus = GetAssignedBonus(faction, PersonnelAssignment.Research, snapshot.ResearchCapacity, ResourceType.Research)
                + GetIdleSlotBonus(faction, snapshot.ResearchCapacity, snapshot.ResearchAssigned, ResourceType.Research);

            if (TFTVVoidOmens.VoidOmensCheck[6])
            {
                researchBonus *= 1.5f;
            }

            productionBonus = GetAssignedBonus(faction, PersonnelAssignment.Manufacturing, snapshot.ManufacturingCapacity, ResourceType.Production)
                + GetIdleSlotBonus(faction, snapshot.ManufacturingCapacity, snapshot.ManufacturingAssigned, ResourceType.Production);
        }

        /// <summary>
        /// Capacity comes from the facilities, the assigned count from the personnel records. The
        /// running slot counters are display state and drift from the records mid-assignment, so they
        /// must not feed the income.
        /// </summary>
        private static PoolAssignmentSnapshot BuildPoolAssignmentSnapshot(GeoPhoenixFaction faction)
        {
            Workers.FacilitySlotPools pools = Workers.ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            int researchCapacity = pools.Research.ProvidedSlots;
            int manufacturingCapacity = pools.Manufacturing.ProvidedSlots;

            return new PoolAssignmentSnapshot
            {
                ResearchCapacity = researchCapacity,
                ResearchAssigned = Math.Min(researchCapacity, CountAssigned(faction, PersonnelAssignment.Research)),
                ManufacturingCapacity = manufacturingCapacity,
                ManufacturingAssigned = Math.Min(manufacturingCapacity, CountAssigned(faction, PersonnelAssignment.Manufacturing))
            };
        }

        /// <summary>
        /// Slots actually being worked: the personnel assigned to this duty, capped at the slots the
        /// facilities provide. Same source the income uses, so displays cannot disagree with it.
        /// </summary>
        internal static int GetOccupiedSlots(GeoPhoenixFaction faction, PersonnelAssignment assignment)
        {
            if (faction == null)
            {
                return 0;
            }

            Workers.FacilitySlotPools pools = Workers.ResearchManufacturingSlotsManager.GetOrCreatePools(faction);

            int capacity = assignment == PersonnelAssignment.Research
                ? pools.Research.ProvidedSlots
                : pools.Manufacturing.ProvidedSlots;

            return Math.Min(capacity, CountAssigned(faction, assignment));
        }

        private static int CountAssigned(GeoPhoenixFaction faction, PersonnelAssignment assignment)
        {
            return PersonnelData.Assignments.Values
                .Count(person => person?.Character != null
                    && person.Character.Faction == faction
                    && person.Assignment == assignment);
        }

        /// <summary>
        /// Output of the personnel working this assignment, capped at the number of slots the
        /// facilities provide. When more are assigned than there are slots, the most productive ones
        /// take the slots.
        /// </summary>
        private static float GetAssignedBonus(GeoPhoenixFaction faction, PersonnelAssignment assignment, int capacity, ResourceType resourceType)
        {
            if (capacity <= 0)
            {
                return 0f;
            }

            return PersonnelData.Assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction && person.Assignment == assignment)
                .OrderByDescending(person => GetWorkerOutput(person.Character, resourceType))
                .ThenBy(person => person.Id)
                .Take(capacity)
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

        /// <summary>
        /// Gives idle slots the facility upgrades that Workers.GeoFactionFacilityBuffCollection_GetValue_Patch
        /// strips from every research and manufacturing facility. Without this, researched upgrades would do
        /// nothing at all; an occupied slot does not get them because the personnel in it is the upgrade.
        /// </summary>
        private static float GetIdleSlotBonus(GeoPhoenixFaction faction, int capacity, int assigned, ResourceType resourceType)
        {
            int idleSlots = Math.Max(0, capacity - assigned);
            if (idleSlots <= 0 || capacity <= 0)
            {
                return 0f;
            }

            float strippedTotal = GetStrippedBuffTotal(faction, resourceType);
            if (strippedTotal <= 0f)
            {
                return 0f;
            }

            // strippedTotal covers every providing facility; hand out only the idle share of it.
            return strippedTotal * idleSlots / capacity;
        }

        /// <summary>
        /// Sum, over every working research/manufacturing facility, of the buff value our GetValue patch
        /// discards: vanilla would return baseValue * (multiplier + buffs + global) + added, the patch
        /// returns baseValue * multiplier + added, so the difference is what researched upgrades are worth.
        /// </summary>
        private static float GetStrippedBuffTotal(GeoPhoenixFaction faction, ResourceType resourceType)
        {
            GeoFactionFacilityBuffCollection buffs = faction?.FacilityBuffs;
            if (buffs?.FacilityBuffs == null || faction.Bases == null)
            {
                return 0f;
            }

            float total = 0f;

            foreach (GeoPhoenixBase geoBase in faction.Bases)
            {
                if (geoBase?.Layout?.Facilities == null)
                {
                    continue;
                }

                // Outposts provide no slots, so they get no make-up bonus either.
                if (geoBase.Site != null && geoBase.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                {
                    continue;
                }

                foreach (GeoPhoenixFacility facility in geoBase.Layout.Facilities)
                {
                    if (facility == null || !facility.IsWorking || facility.Def?.GeoFacilityComponentDefs == null)
                    {
                        continue;
                    }

                    foreach (GeoFacilityComponentDef component in facility.Def.GeoFacilityComponentDefs)
                    {
                        if (!(component is ResourceGeneratorFacilityComponentDef generator))
                        {
                            continue;
                        }

                        float baseValue = generator.BaseResourcesOutput.ByResourceType(resourceType).Value;
                        if (baseValue <= 0f)
                        {
                            continue;
                        }

                        total += GetStrippedBuffValue(buffs, facility.Def, baseValue);
                    }
                }
            }

            return total;
        }

        private static float GetStrippedBuffValue(GeoFactionFacilityBuffCollection buffs, PhoenixFacilityDef facilityDef, float baseValue)
        {
            List<GeoFactionFacilityBuff> forFacility = buffs.FacilityBuffs
                .Where(buff => buff != null && buff.FacilityDef == facilityDef)
                .ToList();

            // A Set buff overrides everything in vanilla, including the base value.
            GeoFactionFacilityBuff set = forFacility
                .FirstOrDefault(buff => buff.ModificationType == GeoFactionFacilityBuff.FacilityComponentModificationType.Set);
            if (set != null)
            {
                return Mathf.Max(0f, set.Amount - baseValue);
            }

            float added = 0f;
            float multiplier = buffs.GlobalProductionMultiplier;

            foreach (GeoFactionFacilityBuff buff in forFacility)
            {
                switch (buff.ModificationType)
                {
                    case GeoFactionFacilityBuff.FacilityComponentModificationType.Add:
                        added += buff.Amount;
                        break;
                    case GeoFactionFacilityBuff.FacilityComponentModificationType.Multiply:
                        multiplier += buff.Amount;
                        break;
                }
            }

            return Mathf.Max(0f, baseValue * multiplier + added);
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
