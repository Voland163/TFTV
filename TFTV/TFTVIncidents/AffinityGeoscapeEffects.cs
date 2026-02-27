using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVIncidents
{
    internal static class AffinityGeoscapeEffects
    {
        private const string DiagTag = "[Incidents][AffinityEffects]";

        private static readonly MissionTagDef HavenDefenseTag = TFTVMain.Main.DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");

        [HarmonyPatch(typeof(ResourceMissionOutcomeDef), nameof(ResourceMissionOutcomeDef.ApplyOutcome))]
        private static class ResourceMissionOutcomeDef_ApplyOutcome_AffinityHavenDefenseReward_Patch
        {
            private static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription)
            {
                try
                {
                    GeoLevelController level = mission?.Level;
                    if (level == null)
                    {
                        return;
                    }

                    if (mission?.MissionDef?.Tags == null || HavenDefenseTag == null || !mission.MissionDef.Tags.Contains(HavenDefenseTag))
                    {
                        return;
                    }

                    int rank = GetActiveGeoscapeRank(level, mission.Squad.Soldiers.ToList(), LeaderSelection.AffinityApproach.PsychoSociology, requiredOption: 1);
                    if (rank <= 0 || rewardDescription.Resources == null || rewardDescription.Resources.Count == 0)
                    {
                        return;
                    }

                    float multiplier = 1f + (0.15f * rank);
                    ResourcePack boosted = new ResourcePack(rewardDescription.Resources);

                    for (int i = 0; i < boosted.Count; i++)
                    {
                        ResourceUnit resource = boosted[i];
                        boosted[i] = new ResourceUnit(resource.Type, resource.Value * multiplier);
                    }

                    rewardDescription.Resources.Clear();
                    rewardDescription.Resources.AddRange(boosted);

                    TFTVLogger.Always($"{DiagTag} Haven defense rewards boosted by {Math.Round((multiplier - 1f) * 100f)}% (rank {rank}).");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static Dictionary<GeoSite, int> _vehicleStartingExploration = new Dictionary<GeoSite, int>();

        [HarmonyPatch(typeof(GeoVehicle), "StartExploringCurrentSite")]
        private static class GeoSite_StartExploringCurrentSite_AffinityExplorationBonus_Patch
        {
            private static void Prefix(GeoVehicle __instance)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        return;
                    }

                    GeoLevelController level = __instance.GeoLevel;
                    int rank = GetActiveGeoscapeRank(level, __instance.Soldiers.ToList(), LeaderSelection.AffinityApproach.Exploration, requiredOption: 1);
                    if (rank <= 0)
                    {
                        return;
                    }
                    _vehicleStartingExploration.Add(__instance.CurrentSite, rank);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoSite), "get_ExplorationTime")]
        private static class GeoSite_get_ExplorationTime_AffinityExplorationBonus_Patch
        {
            private static void Postfix(GeoSite __instance, ref TimeUnit __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn || _vehicleStartingExploration.ContainsKey(__instance))
                    {
                        return;
                    }


                    if (__instance?.Type != GeoSiteType.Exploration)
                    {
                        return;
                    }

                    GeoLevelController level = __instance.GeoLevel;
                    int rank = _vehicleStartingExploration[__instance];

                    double baseHours = __result.TimeSpan.TotalHours;
                    double reducedHours = Math.Max(1d, baseHours - rank);
                    __result = TimeUnit.FromHours((float)reducedHours);

                    _vehicleStartingExploration.Remove(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //This doesn't work, we want a warning before the attack starts.
        /*[HarmonyPatch(typeof(TFTVBaseDefenseGeoscape), "AddToTFTVAttackSchedule")]
        private static class TFTVBaseDefenseGeoscape_AddToTFTVAttackSchedule_AffinityWarningBonus_Patch
        {
            private static void Prefix(GeoLevelController controller, ref int hours)
            {
                try
                {
                    int rank = GetActiveGeoscapeRank(controller, LeaderSelection.AffinityApproach.Occult, requiredOption: 1);
                    if (rank <= 0)
                    {
                        return;
                    }

                    hours += (6 * rank);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/

        internal static float GetAircraftSpeedMultiplier(GeoVehicle geoVehicle)
        {
            int rank = GetActiveGeoscapeRank(geoVehicle.GeoLevel, geoVehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Compute, requiredOption: 1);
            return rank <= 0 ? 1f : 1f + (0.10f * rank);
        }

        internal static void ApplyPostMissionRecovery(GeoMission mission, TacMissionResult result, GeoSquad squad)
        {
            try
            {
                GeoLevelController level = mission?.Site?.GeoLevel ?? mission?.Level;
                if (level == null || !DidPhoenixWin(result, level))
                {
                    return;
                }

                int biotechRank = GetActiveGeoscapeRank(level, squad.Soldiers.ToList(), LeaderSelection.AffinityApproach.Biotech, requiredOption: 1);
                if (biotechRank > 0 && squad?.Soldiers != null)
                {
                    float healAmount = 30f * biotechRank;
                    foreach (GeoCharacter soldier in squad.Soldiers)
                    {
                        soldier.Heal(healAmount);
                    }
                }

                int machineryRank = GetActiveGeoscapeRank(level, squad.Soldiers.ToList(), LeaderSelection.AffinityApproach.Machinery, requiredOption: 1);
                if (machineryRank > 0)
                {
                    int repairAmount = 150 * machineryRank;
                    foreach (GeoCharacter vehicle in squad.Vehicles)
                    {
                        vehicle.Heal(repairAmount);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool DidPhoenixWin(TacMissionResult result, GeoLevelController level)
        {
            if (result?.FactionResults == null || level?.PhoenixFaction?.FactionDef == null)
            {
                return false;
            }

            FactionResult phoenixResult = result.FactionResults.FirstOrDefault(r => r?.FactionDef == level.PhoenixFaction.FactionDef);
            return phoenixResult != null && phoenixResult.State == TacFactionState.Won;
        }

        private static int GetActiveGeoscapeRank(
            GeoLevelController level,
            List<GeoCharacter> operativesInvolved,
            LeaderSelection.AffinityApproach approach,
            int requiredOption)
        {
            try
            {
                int chosenOption = Affinities.AffinityBenefitsChoices.GetGeoscapeBenefitChoice(level, approach);
                if (chosenOption != requiredOption)
                {
                    return 0;
                }

                int bestRank = 0;
                foreach (GeoCharacter soldier in operativesInvolved)
                {
                    int rank = GetAffinityRankForApproach(soldier, approach);
                    if (rank > bestRank)
                    {
                        bestRank = rank;
                    }
                }

                return bestRank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        private static int GetAffinityRankForApproach(GeoCharacter character, LeaderSelection.AffinityApproach approach)
        {
            if (character?.Progression?.Abilities == null)
            {
                return 0;
            }

            PassiveModifierAbilityDef[] abilities = GetApproachAbilities(approach);
            if (abilities == null || abilities.Length < 3)
            {
                return 0;
            }

            if (character.Progression.Abilities.Contains(abilities[2]))
            {
                return 3;
            }

            if (character.Progression.Abilities.Contains(abilities[1]))
            {
                return 2;
            }

            if (character.Progression.Abilities.Contains(abilities[0]))
            {
                return 1;
            }

            return 0;
        }

        private static PassiveModifierAbilityDef[] GetApproachAbilities(LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.PsychoSociology:
                    return Affinities.PsychoSociology;
                case LeaderSelection.AffinityApproach.Exploration:
                    return Affinities.Exploration;
                case LeaderSelection.AffinityApproach.Occult:
                    return Affinities.Occult;
                case LeaderSelection.AffinityApproach.Biotech:
                    return Affinities.Biotech;
                case LeaderSelection.AffinityApproach.Machinery:
                    return Affinities.Machinery;
                case LeaderSelection.AffinityApproach.Compute:
                    return Affinities.Compute;
                default:
                    return null;
            }
        }
    }
}
