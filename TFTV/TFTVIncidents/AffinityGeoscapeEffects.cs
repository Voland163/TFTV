using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    internal static class AffinityGeoscapeEffects
    {
        private const string DiagTag = "[Incidents][AffinityEffects]";
        private const string BiotechHavenAttitudeRankSiteTagPrefix = "TFTV_AFFINITY_BIOTECH_HAVEN_ATTITUDE_RANK_";

        private static readonly MissionTagDef HavenDefenseTag = TFTVMain.Main.DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");

        internal class PsychoSociologyGeoscapeBenefits


        {


            [HarmonyPatch(typeof(ResourceMissionOutcomeDef), nameof(ResourceMissionOutcomeDef.ApplyOutcome))]
            private static class ResourceMissionOutcomeDef_ApplyOutcome_AffinityHavenDefenseReward_Patch
            {
                private static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription)
                {
                    try
                    {
                        if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                        {
                            return;
                        }


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
        }

        internal class ExplorationGeoscapeBenefits
        {
            
            private static GeoSite _pendingExplorationSite;
            private static int _pendingExplorationRank;

           /* [HarmonyPatch(typeof(UIModuleSiteContextualMenu))]
            internal static class UIModuleSiteContextualMenu_ExploreSiteTimePatch
            {
                private const string TimeSeparator = " ~ ";
                private const string TimeFormat = "{0}H";

                [HarmonyPatch("SetMenuItems")]
                [HarmonyPostfix]
                private static void SetMenuItems_Postfix(UIModuleSiteContextualMenu __instance)
                {
                    if (__instance == null || __instance.ButtonsHolder == null)
                    {
                        return;
                    }
                    foreach (object obj in __instance.ButtonsHolder.transform)
                    {
                        SiteContextualMenuItem siteContextualMenuItem = ((Transform)obj).GetComponent<SiteContextualMenuItem>();
                        if (siteContextualMenuItem == null || !siteContextualMenuItem.gameObject.activeSelf)
                        {
                            continue;
                        }
                        GeoAbility ability = siteContextualMenuItem.Ability;
                        if (!(ability is ExploreSiteAbility))
                        {
                            continue;
                        }
                        if (!TryGetExplorationHours(ability, __instance.SelectedSite, out int num))
                        {
                            continue;
                        }
                        string text = ability.View.ViewElementDef.DisplayName1.Localize(null).ToUpperInvariant();
                        siteContextualMenuItem.ItemText.text = text + TimeSeparator + string.Format(TimeFormat, num);
                    }
                }

                [HarmonyPatch("OnAbilityHover")]
                [HarmonyPostfix]
                private static void OnAbilityHover_Postfix(UIModuleSiteContextualMenu __instance, bool isHovered, SiteContextualMenuItem menuItem)
                {
                    if (!isHovered || __instance?.DescriptionBox == null || menuItem == null || !(menuItem.Ability is ExploreSiteAbility))
                    {
                        return;
                    }
                    if (!TryGetExplorationHours(menuItem.Ability, __instance.SelectedSite, out int num))
                    {
                        return;
                    }
                    string text = __instance.DescriptionBox.DescriptionText.text;
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }
                    string text2 = TimeSeparator.Trim();
                    if (text.Contains(text2))
                    {
                        return;
                    }
                    __instance.DescriptionBox.DescriptionText.text = string.Format("{0}\n\nExploration time: {1}", text, string.Format(TimeFormat, num));
                }

                private static bool TryGetExplorationHours(GeoAbility ability, GeoSite selectedSite, out int hours)
                {
                    hours = 0;
                    GeoVehicle geoVehicle = ability?.GeoActor as GeoVehicle;
                    GeoSite geoSite = geoVehicle?.CurrentSite ?? selectedSite;
                    if (geoSite == null)
                    {
                        return false;
                    }
                    double totalHours = geoSite.ExplorationTime.TimeSpan.TotalHours;
                    if (totalHours <= 0.0)
                    {
                        return false;
                    }
                    hours = Mathf.CeilToInt((float)totalHours);
                    return true;
                }
            }*/

            [HarmonyPatch(typeof(GeoVehicle), "StartExploringCurrentSite")]
            private static class GeoVehicle_StartExploringCurrentSite_AffinityExplorationBonus_Patch
            {
                private static void Prefix(GeoVehicle __instance)
                {
                    try
                    {
                      
                        BeginExplorationAdjustment(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void Postfix(GeoVehicle __instance)
                {
                    try
                    {
                     
                        EndExplorationAdjustment();
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
                public static void Postfix(GeoSite __instance, ref TimeUnit __result)
                {
                    try
                    {
                        double baseHours = __result.TimeSpan.TotalHours;

                        bool shouldApply = ShouldApplyExplorationAdjustment(__instance);

            
                        if (!shouldApply)
                        {
                            return;
                        }

                        double reducedHours = Math.Max(1d, baseHours - _pendingExplorationRank);
                        if (reducedHours >= baseHours)
                        {
                          
                            return;
                        }

                        __result = TimeUnit.FromHours((float)reducedHours);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void BeginExplorationAdjustment(GeoVehicle vehicle)
            {
                _pendingExplorationSite = null;
                _pendingExplorationRank = 0;

                if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                {
                 
                    return;
                }

                if (vehicle == null)
                {

                    return;
                }

                if (vehicle.CurrentSite == null)
                {

                    return;
                }

                List<GeoCharacter> soldiers = vehicle.Soldiers != null
                    ? vehicle.Soldiers.ToList()
                    : new List<GeoCharacter>();

                int chosenOption = Affinities.AffinityBenefitsChoices.GetGeoscapeBenefitChoice(
                    vehicle.GeoLevel,
                    LeaderSelection.AffinityApproach.Exploration);

             

                int rank = GetActiveGeoscapeRank(
                    vehicle.GeoLevel,
                    soldiers,
                    LeaderSelection.AffinityApproach.Exploration,
                    requiredOption: 1);


                if (rank <= 0)
                {
                    
                    return;
                }

                _pendingExplorationSite = vehicle.CurrentSite;
                _pendingExplorationRank = rank;

            }

            private static void EndExplorationAdjustment()
            {
               

                _pendingExplorationSite = null;
                _pendingExplorationRank = 0;
            }

            private static bool ShouldApplyExplorationAdjustment(GeoSite site)
            {
                if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                {

                    return false;
                }

                if (site == null)
                {

                    return false;
                }
      
                if (_pendingExplorationSite == null)
                {

                    return false;
                }

                if (_pendingExplorationRank <= 0)
                {

                    return false;
                }

                if (!ReferenceEquals(site, _pendingExplorationSite))
                {
              
                    return false;
                }

                return true;
            }

          
        }
        internal static int GetOccultAttackWarningLeadHours(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction?.Soldiers == null)
                {
                    return 0;
                }

                int rank = GetActiveGeoscapeRank(level, level.PhoenixFaction.Soldiers.ToList(), LeaderSelection.AffinityApproach.Occult, requiredOption: 1);
                return rank <= 0 ? 0 : 24 * rank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        internal static float GetAircraftSpeedMultiplier(GeoVehicle geoVehicle)
        {
            int rank = GetActiveGeoscapeRank(geoVehicle.GeoLevel, geoVehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Compute, requiredOption: 1);
            return rank <= 0 ? 1f : 1f + (0.10f * rank);
        }


        internal static int GetComputeHavenAttackWarningLeadHours(GeoLevelController level)
        {
            try
            {
               
                if(Affinities.AffinityBenefitsChoices.GetGeoscapeBenefitChoice(
                    level, LeaderSelection.AffinityApproach.Compute)!=2) 
                { 
                return 0;
                }

                List<GeoCharacter> soldiers = level.PhoenixFaction.Soldiers.ToList();
                List<string> rankedOperatives = new List<string>();
                int bestRosterRank = 0;

                foreach (GeoCharacter soldier in soldiers)
                {
                    int rank = GetAffinityRankForApproach(soldier, LeaderSelection.AffinityApproach.Compute);
                    if (rank <= 0)
                    {
                        continue;
                    }

                    if (rank > bestRosterRank)
                    {
                        bestRosterRank = rank;
                    }
                }

                int leadHours = bestRosterRank*4;

               
                return leadHours;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        internal static float GetMistPenaltyMultiplier(GeoVehicle geoVehicle)
        {
            int rank = GetActiveGeoscapeRank(geoVehicle.GeoLevel, geoVehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Occult, requiredOption: 2);
            return rank <= 0 ? 1f : Mathf.Max(0f, 1f - (0.15f * rank));
        }

        internal static float GetMistBuffMultiplier(GeoVehicle geoVehicle)
        {
            int rank = GetActiveGeoscapeRank(geoVehicle.GeoLevel, geoVehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Occult, requiredOption: 2);
            return rank <= 0 ? 1f : 1f + (0.15f * rank);
        }

        internal static float GetAircraftMaintenanceLossMultiplier(GeoVehicle geoVehicle)
        {
            int rank = GetActiveGeoscapeRank(geoVehicle.GeoLevel, geoVehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Machinery, requiredOption: 2);
            return rank <= 0 ? 1f : Mathf.Max(0f, 1f - (0.25f * rank));
        }

        internal static void ApplyPostMissionRecovery(GeoMission mission, TacMissionResult result, GeoSquad squad)
        {
            try
            {
               

                GeoLevelController level = mission.Level;

                TFTVLogger.Always($"{DiagTag} Checking for post-mission recovery benefits... " +
                    $"level null? {level==null}, squad null: {squad==null}, phoenix won? {DidPhoenixWin(result, level)}");

                if (level == null || squad == null || !DidPhoenixWin(result, level))
                {
                    return;
                }

                List<GeoCharacter> squadSoldiers = squad.Soldiers?.ToList() ?? new List<GeoCharacter>();

                int biotechRank = GetActiveGeoscapeRank(level, squadSoldiers, LeaderSelection.AffinityApproach.Biotech, requiredOption: 1);

            TFTVLogger.Always($"{DiagTag} Biotech post-mission recovery rank: {biotechRank}.");

                if (biotechRank > 0 && squad.Soldiers != null)
                {
                    float healAmount = 30f * biotechRank;
                    foreach (GeoCharacter soldier in squad.Soldiers)
                    {
                        TFTVLogger.Always($"{DiagTag} Healing {soldier.DisplayName} for {healAmount} due to Biotech affinity.");
                        soldier.Heal(healAmount);
                    }
                }

                int explorationRank = GetActiveGeoscapeRank(level, squadSoldiers, LeaderSelection.AffinityApproach.Exploration, requiredOption: 2);

                    TFTVLogger.Always($"{DiagTag} Exploration post-mission recovery rank: {explorationRank}.");

                if (explorationRank > 0 && squad.Soldiers != null)
                {
                    float staminaAmount = 2f * explorationRank;
                    foreach (GeoCharacter soldier in squad.Soldiers)
                    {
                        soldier.Fatigue?.Stamina?.AddRestrictedToMax(staminaAmount);
                    }
                }

                int machineryRank = GetActiveGeoscapeRank(level, squadSoldiers, LeaderSelection.AffinityApproach.Machinery, requiredOption: 1);

                    TFTVLogger.Always($"{DiagTag} Machinery post-mission recovery rank: {machineryRank}.");

                if (machineryRank > 0 && squad.Vehicles != null)
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

            FactionResult phoenixResult = result.FactionResults.FirstOrDefault(r => r?.FactionDef == level.PhoenixFaction.PPFactionDef);
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

        internal static void ApplyVisitedSiteBenefits(PhoenixPoint.Geoscape.Levels.Factions.GeoPhoenixFaction phoenixFaction, GeoVehicle vehicle)
        {
            try
            {
                if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled || phoenixFaction == null || vehicle?.CurrentSite == null)
                {
                    return;
                }

                GeoSite site = vehicle.CurrentSite;
                if (site.Type != GeoSiteType.Haven)
                {
                    return;
                }

                ApplyBiotechHavenVisitAttitudeBonus(phoenixFaction, vehicle, site);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyBiotechHavenVisitAttitudeBonus(
            PhoenixPoint.Geoscape.Levels.Factions.GeoPhoenixFaction phoenixFaction,
            GeoVehicle vehicle,
            GeoSite site)
        {
            try
            {
                if (phoenixFaction == null || vehicle?.Soldiers == null || site == null)
                {
                    return;
                }

                GeoHaven haven = site.GetComponent<GeoHaven>();
                if (haven?.Leader == null)
                {
                    return;
                }

                int visitingRank = GetActiveGeoscapeRank(site.GeoLevel, vehicle.Soldiers.ToList(), LeaderSelection.AffinityApproach.Biotech, requiredOption: 2);
                if (visitingRank <= 0)
                {
                    return;
                }

                int storedRank = GetStoredBiotechHavenAttitudeRank(site);
                if (visitingRank <= storedRank)
                {
                    return;
                }

                int deltaRank = visitingRank - storedRank;
                int attitudeIncrease = 15 * deltaRank;

                if (!TryIncreaseLeaderAttitudeToPhoenix(haven, phoenixFaction, attitudeIncrease))
                {
                    TFTVLogger.Always($"{DiagTag} Failed to apply Biotech haven leader attitude bonus at {site.LocalizedSiteName}.");
                    return;
                }

                SetStoredBiotechHavenAttitudeRank(site, visitingRank);

                // TODO: Show the stored Biotech haven attitude bonus rank in Haven info next to leader attitude.
                TFTVLogger.Always($"{DiagTag} Biotech haven leader attitude at {site.LocalizedSiteName} increased by {attitudeIncrease} (rank {storedRank} -> {visitingRank}).");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static int GetStoredBiotechHavenAttitudeRank(GeoSite site)
        {
            try
            {
                if (site?.SiteTags == null)
                {
                    return 0;
                }

                for (int rank = 3; rank >= 1; rank--)
                {
                    if (site.SiteTags.Contains(GetBiotechHavenAttitudeRankSiteTag(rank)))
                    {
                        return rank;
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        private static void SetStoredBiotechHavenAttitudeRank(GeoSite site, int rank)
        {
            try
            {
                if (site?.SiteTags == null)
                {
                    return;
                }

                for (int existingRank = 1; existingRank <= 3; existingRank++)
                {
                    string tag = GetBiotechHavenAttitudeRankSiteTag(existingRank);
                    if (site.SiteTags.Contains(tag))
                    {
                        site.SiteTags.Remove(tag);
                    }
                }

                if (rank >= 1 && rank <= 3)
                {
                    string newTag = GetBiotechHavenAttitudeRankSiteTag(rank);
                    if (!site.SiteTags.Contains(newTag))
                    {
                        site.SiteTags.Add(newTag);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string GetBiotechHavenAttitudeRankSiteTag(int rank)
        {
            return $"{BiotechHavenAttitudeRankSiteTagPrefix}{rank}";
        }

        private static bool TryIncreaseLeaderAttitudeToPhoenix(
            GeoHaven haven,
            PhoenixPoint.Geoscape.Levels.Factions.GeoPhoenixFaction phoenixFaction,
            int delta)
        {
            try
            {
                if (haven?.Leader?.Diplomacy == null || phoenixFaction == null || delta <= 0)
                {
                    return false;
                }

                object diplomacy = haven.Leader.Diplomacy;
                int currentValue = haven.Leader.Diplomacy.GetDiplomacy(phoenixFaction);
                int targetValue = Mathf.Clamp(currentValue + delta, 0, 100);

                if (TryInvokeDiplomacyMethod(diplomacy, "SetDiplomacy", phoenixFaction, targetValue))
                {
                    return true;
                }

                if (TryInvokeDiplomacyMethod(diplomacy, "ChangeDiplomacy", phoenixFaction, delta))
                {
                    return true;
                }

                if (TryInvokeDiplomacyMethod(diplomacy, "ModifyDiplomacy", phoenixFaction, delta))
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static bool TryInvokeDiplomacyMethod(
            object target,
            string methodName,
            GeoFaction faction,
            int value)
        {
            try
            {
                if (target == null || faction == null || string.IsNullOrEmpty(methodName))
                {
                    return false;
                }

                IEnumerable<System.Reflection.MethodInfo> methods =
                    AccessTools.GetDeclaredMethods(target.GetType())
                    .Concat(target.GetType().GetMethods())
                    .Where(m => m.Name == methodName)
                    .Distinct();

                foreach (System.Reflection.MethodInfo method in methods)
                {
                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 2)
                    {
                        continue;
                    }

                    if (!parameters[0].ParameterType.IsInstanceOfType(faction))
                    {
                        continue;
                    }

                    object convertedValue;
                    Type secondParameterType = parameters[1].ParameterType;

                    if (secondParameterType == typeof(int))
                    {
                        convertedValue = value;
                    }
                    else if (secondParameterType == typeof(float))
                    {
                        convertedValue = (float)value;
                    }
                    else if (secondParameterType == typeof(double))
                    {
                        convertedValue = (double)value;
                    }
                    else
                    {
                        continue;
                    }

                    method.Invoke(target, new object[] { faction, convertedValue });
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        internal static int GetPsychoSociologyDeliriumRecoveryBonusPercent(
            GeoLevelController level,
            List<GeoCharacter> operativesInvolved)
        {
            try
            {
                int rank = GetActiveGeoscapeRank(
                    level,
                    operativesInvolved,
                    LeaderSelection.AffinityApproach.PsychoSociology,
                    requiredOption: 2);

                return rank <= 0 ? 0 : 15 * rank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }
    }
}
