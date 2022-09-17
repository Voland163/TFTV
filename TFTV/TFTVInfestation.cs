using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVInfestation
    {
        public static SharedData sharedData = GameUtl.GameComponent<SharedData>();
        private static readonly DefRepository Repo = TFTVMain.Repo;

        internal static GeoHavenDefenseMission DefenseMission = null;
        internal static GeoSite GeoSiteForInfestation = null;
        internal static GeoSite GeoSiteForScavenging = null;
        public static string InfestedHavensVariable = "Number_of_Infested_Havens";
        public static string LivingWeaponsAcquired = "Living_Weapons_Acquired";
        public static void Apply_Infestation_Changes()
        {
            try
            {
                AlienRaidsSetupDef raidsSetup = Repo.GetAllDefs<AlienRaidsSetupDef>().FirstOrDefault(ged => ged.name.Equals("_AlienRaidsSetupDef"));
                raidsSetup.RaidBands[0].RollResultMax = 60;
                raidsSetup.RaidBands[1].RollResultMax = 80;
                raidsSetup.RaidBands[2].RollResultMax = 100;
                raidsSetup.RaidBands[3].RollResultMax = 130;
                raidsSetup.RaidBands[4].RollResultMax = 9999;
                raidsSetup.RaidBands[4].AircraftTypesAllowed = 0;

                CustomMissionTypeDef Anu_Infestation = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("HavenInfestationAN_CustomMissionTypeDef"));
                CustomMissionTypeDef NewJericho_Infestation = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("HavenInfestationSY_CustomMissionTypeDef"));
                CustomMissionTypeDef Synderion_Infestation = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("HavenInfestationNJ_CustomMissionTypeDef"));

                ResourceMissionOutcomeDef sourceMissonResourceReward = Repo.GetAllDefs<ResourceMissionOutcomeDef>().FirstOrDefault(ged => ged.name.Equals("HavenDefAN_ResourceMissionOutcomeDef"));
                ResourceMissionOutcomeDef mutagenRewardInfestation = Helper.CreateDefFromClone(sourceMissonResourceReward, "2E579AB8-3744-4994-8036-B5018B5E2E15", "InfestationReward");
                mutagenRewardInfestation.Resources.Values.Clear();
                mutagenRewardInfestation.Resources.Values.Add(new ResourceUnit { Type = ResourceType.Mutagen, Value = 800 });

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    if (missionTypeDef.name.Contains("Haven") && missionTypeDef.name.Contains("Infestation"))
                    {
                        missionTypeDef.Outcomes[0].DestroySite = true;
                        missionTypeDef.Outcomes[0].Outcomes[2] = mutagenRewardInfestation;
                        missionTypeDef.Outcomes[0].BriefingModalBind.Title.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_VICTORY_NAME";
                        missionTypeDef.Outcomes[0].BriefingModalBind.Description.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_VICTORY_DESCRIPTION";
                        missionTypeDef.BriefingModalBind.Title.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_NAME";
                        missionTypeDef.BriefingModalBind.Description.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_DESCRIPTION";
                    }
                }

               // GeoscapeEventDef rewardEvent = TFTVCommonMethods.CreateNewEvent("InfestationReward", "KEY_INFESTATION_REWARD_TITLE", "KEY_INFESTATION_REWARD_DESCRIPTION", null);
                //Muting Living Weapons
                GeoscapeEventDef lwstartingEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LW1_GeoscapeEventDef"));
                lwstartingEvent.GeoscapeEventData.Mute = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        public static List<ItemUnit> InfestationRewardGenerator(int num)
        {
            try 
            {
                GeoscapeEventDef LW1Miss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LW1_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LW2Miss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LW2_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LW3Miss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LW3_WIN_GeoscapeEventDef"));

                if (num == 1) 
                {
                    List<ItemUnit> reward = LW1Miss.GeoscapeEventData.Choices[0].Outcome.Items;
                    return reward;
                }
                else if (num == 2) 
                {
                    List<ItemUnit> reward = LW2Miss.GeoscapeEventData.Choices[0].Outcome.Items;
                    return reward;
                }
                else if (num == 3)
                {
                    List<ItemUnit> reward = LW3Miss.GeoscapeEventData.Choices[0].Outcome.Items;
                    return reward;
                }
                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        } 
        

        // Copied and adapted from Mad´s Assorted Adjustments
        

        // Store mission for other patches
        [HarmonyPatch(typeof(GeoHavenDefenseMission), "UpdateGeoscapeMissionState")]
        public static class GeoHavenDefenseMission_UpdateGeoscapeMissionState_StoreMission_Patch
        {
            public static void Prefix(GeoHavenDefenseMission __instance)
            {
                DefenseMission = __instance;
            }

            public static void Postfix()
            {
                DefenseMission = null;
                                         
            }
        }

        [HarmonyPatch(typeof(GeoSite), "DestroySite")]
        public static class GeoSite_DestroySite_Patch_ConvertDestructionToInfestation
        {
            public static bool Prefix(GeoSite __instance)
            {
                try
                {
                    if(__instance.Type == GeoSiteType.Haven)
                    {
                   // TFTVLogger.Always("DestroySite method called");
                    TFTVLogger.Always("infestation variable is " + __instance.GeoLevel.EventSystem.GetVariable("Infestation_Encounter_Variable"));
                    string faction = __instance.Owner.GetPPName();
                  //  TFTVLogger.Always(faction);
                    if (DefenseMission == null)
                    {

                        return true;
                    }

                    IGeoFactionMissionParticipant attacker = DefenseMission.GetEnemyFaction();

                    int roll = UnityEngine.Random.Range(0, 6 + __instance.GeoLevel.CurrentDifficultyLevel.Order);
                    TFTVLogger.Always("Infestation roll is " + roll);
                    int[] rolledVoidOmens = TFTVVoidOmens.CheckFordVoidOmensInPlay(__instance.GeoLevel);
                    if (attacker.PPFactionDef == sharedData.AlienFactionDef && __instance.IsInMist && __instance.GeoLevel.EventSystem.GetVariable("Infestation_Encounter_Variable") > 0
                     && (roll >= 6 || rolledVoidOmens.Contains(17)))
                    {
                        GeoSiteForInfestation = __instance;
                        __instance.ActiveMission = null;
                        __instance.ActiveMission?.Cancel();

                        TFTVLogger.Always("We got to here, defense mission should be successful and haven should look infested");
                        __instance.GeoLevel.EventSystem.SetVariable("Number_of_Infested_Havens", __instance.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable) + 1);

                        __instance.RefreshVisuals();
                        return false;
                    }
                    int roll2 = UnityEngine.Random.Range(0, 10);
                 /*   if (!__instance.IsInMist && rolledVoidOmens.Contains(12) && roll2 > 5)
                    {
                        GeoSiteForScavenging = __instance;
                        TFTVLogger.Always(__instance.SiteName.LocalizeEnglish() + "ready to spawn a scavenging site");
                    }*/

                    TFTVLogger.Always("Defense mission is not null, the conditions for infestation were not fulfilled, so return true");
                    return true;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }


        [HarmonyPatch(typeof(GeoscapeLog), "AddEntry")]
        public static class GeoscapeLog_AddEntry_Patch_ConvertDestructionToInfestation
        {
            public static void Prefix(GeoscapeLogEntry entry)
            {
                try
                {
                    //TFTVLogger.Always("AddEntry method invoked");
                    


                    if (GeoSiteForInfestation != null && GeoSiteForInfestation.SiteName != null && entry != null && entry.Parameters!=null && entry.Parameters.Contains(GeoSiteForInfestation.SiteName))
                    {
                      //  TFTVLogger.Always("Attempting to add infestation entry to Log");
                        if (entry == null)
                        {
                        //    TFTVLogger.Always("Failed because entry is null");
                        }
                        else 
                        { 
                            if(entry.Text == null) 
                            {
                            //    TFTVLogger.Always("Failed because entry.text is null");

                            }
                            else
                            {
                               // TFTVLogger.Always("Entry.text is not null");
                                entry.Text = new LocalizedTextBind(GeoSiteForInfestation.Owner + " " + DefenseMission.Haven.Site.Name + " has succumbed to Pandoran infestation!", true);
                               // TFTVLogger.Always("The following entry to Log was added" + GeoSiteForInfestation.Owner + " " + DefenseMission.Haven.Site.Name + " has succumbed to Pandoran infestation!");

                            }
                        
                        
                        }           
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoscapeLog), "Map_SiteMissionEnded")]
        public static class GeoscapeLog_Map_SiteMissionEnded_Patch_ConvertDestructionToInfestation
        {

            public static void Postfix(GeoSite site, GeoMission mission)
            {
                try
                {

                    if (GeoSiteForInfestation != null && site == GeoSiteForInfestation && mission is GeoHavenDefenseMission)
                    {
                        TFTVLogger.Always("GeoSiteForInfestation is " + GeoSiteForInfestation.name);
                        site.GeoLevel.AlienFaction.InfestHaven(GeoSiteForInfestation);
                        TFTVLogger.Always("We got to here, haven should be infested!");
                        GeoSiteForInfestation = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        

        [HarmonyPatch(typeof(InfestedHavenOutcomeDataBind), "ModalShowHandler")]

        public static class InfestedHavenOutcomeDataBind_Patch_ConvertDestructionToInfestation
        {
            public static bool Prefix(InfestedHavenOutcomeDataBind __instance, UIModal modal, bool ____shown, UIModal ____modal)
            {

                try
                {
                   
                    if (!____shown)
                    {
                        ____shown = true;
                        if (____modal == null)
                        {
                            ____modal = modal;
                            ____modal.OnModalHide += __instance.ModalHideHandler;
                        }

                        Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                        description.GetComponent<I2.Loc.Localize>().enabled = false;
                        description.text = "We destroyed the monstrosity that had taken over the remaining inhabitants of the haven, delivering them from a fate worse than death";
                        Text title = __instance.TopBar.Title;
                        title.GetComponent<I2.Loc.Localize>().enabled = false;
                        title.text = "NODE DESTOYED";

                        GeoInfestationCleanseMission geoInfestationCleanseMission = (GeoInfestationCleanseMission)modal.Data;
                        GeoSite site = geoInfestationCleanseMission.Site;
                        __instance.Background.sprite = Helper.CreateSpriteFromImageFile("NodeAlt.jpg");
                        Sprite icon = __instance.CommonResources.GetFactionInfo(site.Owner).Icon;
                        __instance.TopBar.Icon.sprite = icon;
                        __instance.TopBar.Subtitle.text = site.LocalizedSiteName;
                        GeoFactionRewardApplyResult applyResult = geoInfestationCleanseMission.Reward.ApplyResult;
                        __instance.AttitudeChange.SetAttitudes(applyResult.Diplomacy);
                        __instance.Rewards.SetReward(geoInfestationCleanseMission.Reward);
                        TFTVLogger.Always("InfestedHavensVariable before method is " + site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable));
                        site.GeoLevel.EventSystem.SetVariable(InfestedHavensVariable, site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable) - 1);
                        TFTVLogger.Always("InfestedHavensVariable is " + site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable));
                        site.GeoLevel.EventSystem.SetVariable(LivingWeaponsAcquired, site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) + 1);
                        GeoscapeEventDef reward = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("InfestationReward"));
                        GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(site.GeoLevel.PhoenixFaction, site.GeoLevel.ViewerFaction);
                       /* if (site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) > 0 && site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) < 4) 
                        {
                            reward.GeoscapeEventData.Choices[0].Outcome.Items = InfestationRewardGenerator(site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired));
                            site.GeoLevel.EventSystem.TriggerGeoscapeEvent("InfestationReward", geoscapeEventContext);
                        }*/

                    }

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }

            }
        }


    }


}

