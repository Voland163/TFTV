using Base.Defs;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Statuses;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels.Mist;
using UnityEngine;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Common.Core;
using Base.Entities.Effects.ApplicationConditions;
using Base.UI;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using Epic.OnlineServices.Sessions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Common.ContextHelp;

namespace TFTV
{
    internal class TFTVDefsInjectedOnlyOnce
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void InjectDefsInjectedOnlyOnce() 
        {
            AlistairRoadsEvent();
            AugmentationEventsDefs();
            ChangesAmbushMissions();
            CreateHints();
            CreateIntro();
            Create_VoidOmen_Events();
            ChangeInfestationDefs();
            ChangesToMedbay();
            ChangeUmbra();
            InjectAlistairAhsbyLines();
            InjectOlenaKimLines();
            MistOnAllMissions();
            ModifyAirCombatDefs();      
            ModifyDefsForPassengerModules();
            ModifyMissionDefsToReplaceNeutralWithBandit();
            ModifyPandoranProgress();
            RemoveCorruptionDamageBuff();
            TFTVChangesToDLC1andDLC2Events.ChangesToDLC1andDLC2Defs();
            TFTVChangesToDLC3Events.ChangesToDLC3Defs();
            TFTVChangesToDLC4Events.ChangesToDLC4Defs();
            TFTVChangesToDLC5Events.ChangesToDLC5Defs();
            TFTVDefsWithConfigDependency.HarderDiplomacy();

        }

        public static void CreateHints()

        {
            try
            {
                TFTVTutorialAndStory.CreateNewTacticalHint("UmbraSighted", HintTrigger.ActorSeen, "Oilcrab_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true);
                TFTVTutorialAndStory.CreateNewTacticalHint("RevenantSighted", HintTrigger.ActorSeen, "RevenantTier_1_GameTagDef", "REVENANT_SIGHTED_TITLE", "REVENANT_SIGHTED_TEXT", 1, true);


                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMission("InfestationMissionIntro");
                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMission("InfestationMissionIntro2");
                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMissionEnd("InfestationMissionEnd");

                ContextHelpHintDef infestationIntro2 = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("InfestationMissionIntro2"));
                ContextHelpHintDef infestationIntro = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("InfestationMissionIntro"));
                infestationIntro.NextHint = infestationIntro2;


                //  CreateNewTacticalHint("LeaderSighted", HintTrigger.ActorSeen, "HumanEnemy_GameTagDef", "Should not appear", "Should not appear", 1, false);
                //   ContextHelpHintDef leaderSightedHint = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("LeaderSighted"));
                //   leaderSightedHint.AnyCondition = false;
                //  leaderSightedHint.Conditions[0] = ActorIsOfFactionCreateNewConditionForTacticalHint("AN_FallenOnes_TacticalFactionDef");
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("Anu_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NEU_Bandits_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NJ_Purists_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NewJericho_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("Synedrion_TacticalFactionDef"));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChangeInfestationDefs()
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

        public static void ModifyMissionDefsToReplaceNeutralWithBandit()
        {
            try
            {
                PPFactionDef banditFaction = Repo.GetAllDefs<PPFactionDef>().FirstOrDefault(p => p.name.Equals("NEU_Bandits_FactionDef"));

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    // TFTVLogger.Always("The first foreach went ok");


                    foreach (MutualParticipantsRelations relations in missionTypeDef.ParticipantsRelations)
                    {
                        // TFTVLogger.Always("The second foreach went ok");
                        if (relations.FirstParticipant == TacMissionParticipant.Player && relations.MutualRelation == FactionRelation.Enemy)
                        {
                            //   TFTVLogger.Always("The if inside the second foreach went ok");

                            if (missionTypeDef.ParticipantsData != null)
                            {
                                foreach (TacMissionTypeParticipantData data in missionTypeDef.ParticipantsData)
                                {
                                    //TFTVLogger.Always("The third foreach went Ok");

                                    if (data.ParticipantKind == relations.SecondParticipant)
                                    {
                                        // TFTVLogger.Always("The if inside the third foreach went ok");
                                        if (data.FactionDef != null)
                                        {
                                            if (missionTypeDef.name == "StoryAN1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryNJ_Chain1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryPX13_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN0_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN4_CustomMissionTypeDef")
                                            {
                                                data.FactionDef = banditFaction;
                                                TFTVLogger.Always("In mission " + missionTypeDef.name + " the enemy faction is " + data.FactionDef.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ChangesAmbushMissions()
        {
            try
            {
                //Changing ambush missions so that all of them have crates
                CustomMissionTypeDef AmbushALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAlien_CustomMissionTypeDef"));
                CustomMissionTypeDef SourceScavCratesALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("ScavCratesALN_CustomMissionTypeDef"));
                var pickResourceCratesObjective = SourceScavCratesALN.CustomObjectives[2];
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushALN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushALN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushALN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushALN.CratesDeploymentPointsRange.Min = 30;
                AmbushALN.CratesDeploymentPointsRange.Max = 50;
                AmbushALN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushAN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAN_CustomMissionTypeDef"));
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushAN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushAN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushAN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushAN.CratesDeploymentPointsRange.Min = 30;
                AmbushAN.CratesDeploymentPointsRange.Max = 50;
                AmbushAN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushBandits = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushBandits_CustomMissionTypeDef"));
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushBandits.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushBandits.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushBandits.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushBandits.CratesDeploymentPointsRange.Min = 30;
                AmbushBandits.CratesDeploymentPointsRange.Max = 50;
                AmbushBandits.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushFallen = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushFallen_CustomMissionTypeDef"));
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushFallen.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushFallen.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushFallen.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushFallen.CratesDeploymentPointsRange.Min = 30;
                AmbushFallen.CratesDeploymentPointsRange.Max = 50;
                AmbushFallen.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushNJ = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushNJ_CustomMissionTypeDef"));
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushNJ.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushNJ.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushNJ.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushNJ.CratesDeploymentPointsRange.Min = 30;
                AmbushNJ.CratesDeploymentPointsRange.Max = 50;
                AmbushNJ.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushPure = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushPure_CustomMissionTypeDef"));
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushPure.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushPure.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushPure.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushPure.CratesDeploymentPointsRange.Min = 30;
                AmbushPure.CratesDeploymentPointsRange.Max = 50;
                AmbushPure.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushSY = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushSY_CustomMissionTypeDef"));
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushSY.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushSY.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushSY.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushSY.CratesDeploymentPointsRange.Min = 30;
                AmbushSY.CratesDeploymentPointsRange.Max = 50;
                AmbushSY.CustomObjectives[2] = pickResourceCratesObjective;

                //Reduce XP for Ambush mission
                SurviveTurnsFactionObjectiveDef surviveAmbush_CustomMissionObjective = Repo.GetAllDefs<SurviveTurnsFactionObjectiveDef>().FirstOrDefault(ged => ged.name.Equals("SurviveAmbush_CustomMissionObjective"));
                surviveAmbush_CustomMissionObjective.MissionObjectiveData.ExperienceReward = 100;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ModifyDefsForPassengerModules()
        {

            try
            {
                //ID all the factions for later
                GeoFactionDef PhoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef NewJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef Anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef Synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                //ID all craft for later
                GeoVehicleDef manticore = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_Manticore_Def"));
                GeoVehicleDef helios = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("SYN_Helios_Def"));
                GeoVehicleDef thunderbird = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Thunderbird_Def"));
                GeoVehicleDef blimp = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("ANU_Blimp_Def"));
                GeoVehicleDef manticoreMasked = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_MaskedManticore_Def"));

                //Reduce all craft seating (except blimp) by 4 and create clones with previous seating

                GeoVehicleDef manticoreNew = Helper.CreateDefFromClone(manticore, "83A7FD03-DB85-4CEE-BAED-251F5415B82B", "PP_Manticore_Def_6_Slots");
                manticore.BaseStats.SpaceForUnits = 2;
                GeoVehicleDef heliosNew = Helper.CreateDefFromClone(helios, "4F9026CB-EF42-44B8-B9C3-21181EC4E2AB", "SYN_Helios_Def_5_Slots");
                helios.BaseStats.SpaceForUnits = 1;
                GeoVehicleDef thunderbirdNew = Helper.CreateDefFromClone(thunderbird, "FDE7F0C2-8BA7-4046-92EB-F3462F204B2B", "NJ_Thunderbird_Def_7_Slots");
                thunderbird.BaseStats.SpaceForUnits = 3;
                GeoVehicleDef blimpNew = Helper.CreateDefFromClone(blimp, "B857B76D-BDDB-4CA9-A1CA-895A540B17C8", "ANU_Blimp_Def_12_Slots");
                blimpNew.BaseStats.SpaceForUnits = 12;
                GeoVehicleDef manticoreMaskedNew = Helper.CreateDefFromClone(manticoreMasked, "19B82FD8-67EE-4277-B982-F352A53ADE72", "PP_ManticoreMasked_Def_8_Slots");
                manticoreMasked.BaseStats.SpaceForUnits = 4;

                //Change Hibernation module
                GeoVehicleModuleDef hibernationmodule = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(ged => ged.name.Equals("SY_HibernationPods_GeoVehicleModuleDef"));
                //Increase cost to 50% of Vanilla Manti
                hibernationmodule.ManufactureMaterials = 600;
                hibernationmodule.ManufactureTech = 75;
                hibernationmodule.ManufacturePointsCost = 505;
                //Change Cruise Control module
                GeoVehicleModuleDef cruisecontrolmodule = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(ged => ged.name.Equals("NJ_CruiseControl_GeoVehicleModuleDef"));
                //Increase cost to 50% of Vanilla Manti
                cruisecontrolmodule.ManufactureMaterials = 600;
                cruisecontrolmodule.ManufactureTech = 75;
                cruisecontrolmodule.ManufacturePointsCost = 505;
                //Change Fuel Tank module
                GeoVehicleModuleDef fueltankmodule = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(ged => ged.name.Equals("NJ_FuelTanks_GeoVehicleModuleDef"));
                //Increase cost to 50% of Vanilla Manti
                fueltankmodule.ManufactureMaterials = 600;
                fueltankmodule.ManufactureTech = 75;
                fueltankmodule.ManufacturePointsCost = 505;


                //Make Hibernation module available for manufacture from start of game - doesn't work because HM is not an ItemDef
                //GeoPhoenixFactionDef phoenixFactionDef = Repo.GetAllDefs<GeoPhoenixFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                //EntitlementDef festeringSkiesEntitlementDef = Repo.GetAllDefs<EntitlementDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesEntitlementDef"));
                // phoenixFactionDef.AdditionalDLCItems.Add(new GeoFactionDef.DLCStartItems { DLC = festeringSkiesEntitlementDef, StartingManufacturableItems = hibernationmodule };               
                //Change cost of Manti to 50% of Vanilla
                VehicleItemDef mantiVehicle = Repo.GetAllDefs<VehicleItemDef>().FirstOrDefault(ged => ged.name.Equals("PP_Manticore_VehicleItemDef"));
                mantiVehicle.ManufactureMaterials = 600;
                mantiVehicle.ManufactureTech = 75;
                mantiVehicle.ManufacturePointsCost = 505;
                //Change cost of Helios to Vanilla minus cost of passenger module
                VehicleItemDef heliosVehicle = Repo.GetAllDefs<VehicleItemDef>().FirstOrDefault(ged => ged.name.Equals("SYN_Helios_VehicleItemDef"));
                heliosVehicle.ManufactureMaterials = 555;
                heliosVehicle.ManufactureTech = 173;
                heliosVehicle.ManufacturePointsCost = 510;
                //Change cost of Thunderbird to Vanilla minus cost of passenger module
                VehicleItemDef thunderbirdVehicle = Repo.GetAllDefs<VehicleItemDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Thunderbird_VehicleItemDef"));
                thunderbirdVehicle.ManufactureMaterials = 900;
                thunderbirdVehicle.ManufactureTech = 113;
                thunderbirdVehicle.ManufacturePointsCost = 660;

                //Make HM research for PX, available after completing Phoenix Archives
                ResearchDef hibernationModuleResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_Aircraft_HybernationPods_ResearchDef"));
                ResearchDef sourcePX_SDI_ResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_SDI_ResearchDef"));
                hibernationModuleResearch.Faction = PhoenixPoint;
                hibernationModuleResearch.RevealRequirements = sourcePX_SDI_ResearchDef.RevealRequirements;
                hibernationModuleResearch.ResearchCost = 100;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ModifyPandoranProgress()
        {

            try
            {
              
                    // All sources of evolution due to scaling removed, leaving only evolution per day
                    // Additional source of evolution will be number of surviving Pandoran colonies, modulated by difficulty level
                    GameDifficultyLevelDef veryhard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("VeryHard_GameDifficultyLevelDef"));
                    //Hero
                    GameDifficultyLevelDef hard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Hard_GameDifficultyLevelDef"));
                    //Standard
                    GameDifficultyLevelDef standard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Standard_GameDifficultyLevelDef"));
                    //Easy
                    GameDifficultyLevelDef easy = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Easy_GameDifficultyLevelDef"));

                    veryhard.NestLimitations.MaxNumber = 3; //vanilla 6
                    veryhard.NestLimitations.HoursBuildTime = 90; //vanilla 45
                    veryhard.LairLimitations.MaxNumber = 3; // vanilla 5
                    veryhard.LairLimitations.MaxConcurrent = 3; //vanilla 4
                    veryhard.LairLimitations.HoursBuildTime = 100; //vanilla 50
                    veryhard.CitadelLimitations.HoursBuildTime = 180; //vanilla 60
                    veryhard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                    veryhard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                    veryhard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                    veryhard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                    veryhard.ApplyInfestationOutcomeChange = 0;
                    veryhard.ApplyDamageHavenOutcomeChange = 0;
                    veryhard.StartingSquadTemplate[0] = hard.TutorialStartingSquadTemplate[1];
                    veryhard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[2];




                    // PX_Jacob_Tutorial2_TacCharacterDef replace [3], with hard starting squad [1]
                    // PX_Sophia_Tutorial2_TacCharacterDef replace [1], with hard starting squad [2]

                    //reducing evolution per day because there other sources of evolution points now
                    veryhard.EvolutionProgressPerDay = 70; //vanilla 100



                    hard.NestLimitations.MaxNumber = 3; //vanilla 5
                    hard.NestLimitations.HoursBuildTime = 90; //vanilla 50
                    hard.LairLimitations.MaxNumber = 3; // vanilla 4
                    hard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                    hard.LairLimitations.HoursBuildTime = 100; //vanilla 80
                    hard.CitadelLimitations.HoursBuildTime = 180; //vanilla 100
                    hard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                    hard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                    hard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                    hard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                    hard.ApplyInfestationOutcomeChange = 0;
                    hard.ApplyDamageHavenOutcomeChange = 0;
                    hard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                    hard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];


                    //reducing evolution per day because there other sources of evolution points now
                    hard.EvolutionProgressPerDay = 60; //vanilla 70


                    standard.NestLimitations.MaxNumber = 3; //vanilla 4
                    standard.NestLimitations.HoursBuildTime = 90; //vanilla 55
                    standard.LairLimitations.MaxNumber = 3; // vanilla 3
                    standard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                    standard.LairLimitations.HoursBuildTime = 100; //vanilla 120
                    standard.CitadelLimitations.HoursBuildTime = 180; //vanilla 145
                    standard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                    standard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                    standard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                    standard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                    standard.ApplyDamageHavenOutcomeChange = 0;
                    standard.ApplyInfestationOutcomeChange = 0;
                    standard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                    standard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                    //reducing evolution per day because there other sources of evolution points now
                    standard.EvolutionProgressPerDay = 40; //vanilla 55

                    easy.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                    easy.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                    easy.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                    easy.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                    easy.ApplyInfestationOutcomeChange = 0;
                    easy.ApplyDamageHavenOutcomeChange = 0;
                    easy.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                    easy.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                    //keeping evolution per day because low enough already
                    easy.EvolutionProgressPerDay = 35; //vanilla 35

                    //Remove faction diplo penalties for not destroying revealed PCs and increase rewards for haven leader
                    GeoAlienBaseTypeDef nestType = Repo.GetAllDefs<GeoAlienBaseTypeDef>().FirstOrDefault(a => a.name.Equals("Nest_GeoAlienBaseTypeDef"));
                    GeoAlienBaseTypeDef lairType = Repo.GetAllDefs<GeoAlienBaseTypeDef>().FirstOrDefault(a => a.name.Equals("Lair_GeoAlienBaseTypeDef"));
                    GeoAlienBaseTypeDef citadelType = Repo.GetAllDefs<GeoAlienBaseTypeDef>().FirstOrDefault(a => a.name.Equals("Citadel_GeoAlienBaseTypeDef"));
                    GeoAlienBaseTypeDef palaceType = Repo.GetAllDefs<GeoAlienBaseTypeDef>().FirstOrDefault(a => a.name.Equals("Palace_GeoAlienBaseTypeDef"));

                    nestType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                    nestType.HavenLeaderDiplomacyReward = 12; //vanilla 8 
                    lairType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                    lairType.HavenLeaderDiplomacyReward = 16; //vanilla 12 
                    citadelType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                    citadelType.HavenLeaderDiplomacyReward = 20; //vanilla 16 
                 
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void InjectAlistairAhsbyLines()
        {
            try
            {
                //Alistair speaks about Symes after completing Symes Retreat
                GeoscapeEventDef alistairOnSymes1 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes1", "PROG_PX10_WIN_TITLE", "KEY_ALISTAIRONSYMES_1_DESCRIPTION", null);
                alistairOnSymes1.GeoscapeEventData.Flavour = "IntroducingSymes";

                //Alistair speaks about Barnabas after Barnabas asks for help
                GeoscapeEventDef alistairOnBarnabas = TFTVCommonMethods.CreateNewEvent("AlistairOnBarnabas", "PROG_CH0_TITLE", "KEY_ALISTAIRONBARNABAS_DESCRIPTION", null);
                alistairOnBarnabas.GeoscapeEventData.Flavour = "DLC4_Generic_NJ";

                //Alistair speaks about Symes after Antarctica discovery
                GeoscapeEventDef alistairOnSymes2 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes2", "PROG_PX1_WIN_TITLE", "KEY_ALISTAIRONSYMES_2_DESCRIPTION", null);
                alistairOnSymes2.GeoscapeEventData.Flavour = "AntarcticSite_Victory";


                AlistairRoadsEvent();
                CreateEventMessagesFromTheVoid();
                InjectOlenaKimLines();
                CreateBehemothPattern();
                CreateTrappedInMist();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void InjectOlenaKimLines()

        {
            try
            {
                //Helena reveal about Olena
                GeoscapeEventDef helenaOnOlena = TFTVCommonMethods.CreateNewEvent("HelenaOnOlena", "PROG_LE0_WIN_TITLE", "KEY_OLENA_HELENA_DESCRIPTION", null);
                //Olena about West
                GeoscapeEventDef olenaOnWest = TFTVCommonMethods.CreateNewEvent("OlenaOnWest", "PROG_NJ1_WIN_TITLE", "KEY_OLENAONWEST_DESCRIPTION", null);
                //Olena about Synod
                GeoscapeEventDef olenaOnSynod = TFTVCommonMethods.CreateNewEvent("OlenaOnSynod", "PROG_AN6_WIN2_TITLE", "KEY_OLENAONSYNOD_DESCRIPTION", null);
                //Olena about the Ancients
                GeoscapeEventDef olenaOnAncients = TFTVCommonMethods.CreateNewEvent("OlenaOnAncients", "KEY_OLENAONANCIENTS_TITLE", "KEY_OLENAONANCIENTS_DESCRIPTION", null);
                //Olena about the Behemeoth
                GeoscapeEventDef olenaOnBehemoth = TFTVCommonMethods.CreateNewEvent("OlenaOnBehemoth", "PROG_FS1_WIN_TITLE", "KEY_OLENAONBEHEMOTH_DESCRIPTION", null);
                //Olena about Alistair - missing an event hook!!
                GeoscapeEventDef olenaOnAlistair = TFTVCommonMethods.CreateNewEvent("OlenaOnAlistair", "", "KEY_OLENAONALISTAIR_DESCRIPTION", null);
                //Olena about Symes
                GeoscapeEventDef olenaOnSymes = TFTVCommonMethods.CreateNewEvent("OlenaOnSymes", "PROG_PX1_WIN_TITLE", "KEY_OLENAONSYMES_DESCRIPTION", null);
                //Olena about ending 
                GeoscapeEventDef olenaOnEnding = TFTVCommonMethods.CreateNewEvent("OlenaOnEnding", "KEY_ALISTAIR_ROADS_TITLE", "KEY_OLENAONENDING_DESCRIPTION", null);
                //Olena about Bionics Lab sabotage
                GeoscapeEventDef olenaOnBionicsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnBionicsLabSabotage", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_CHOICE_0_OUTCOME", null);
                //Olena about Mutations Lab sabotage
                GeoscapeEventDef olenaOnMutationsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnMutationsLabSabotage", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0_OUTCOME", null);
                CreateEventFirstFlyer();
                CreateEventFirstHavenTarget();
                CreateEventFirstHavenAttack();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AlistairRoadsEvent()
        {
            try
            {
                string title = "KEY_ALISTAIR_ROADS_TITLE";
                string description = "KEY_ALISTAIR_ROADS_DESCRIPTION";
                string passToOlena = "OlenaOnEnding";

                string startingEvent = "AlistairRoads";
                string afterWest = "AlistairRoadsNoWest";
                string afterSynedrion = "AlistairRoadsNoSynedrion";
                string afterAnu = "AlistairRoadsNoAnu";
                string afterVirophage = "AlistairRoadsNoVirophage";

                string questionAboutWest = "KEY_ALISTAIRONWEST_CHOICE";
                string questionAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_CHOICE";
                string questionAboutAnu = "KEY_ALISTAIRONANU_CHOICE";
                string questionAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_CHOICE";
                //   string questionAboutHelena = "KEY_ALISTAIRONHELENA_CHOICE";
                string noMoreQuestions = "KEY_ALISTAIR_ROADS_ALLDONE";

                string answerAboutWest = "KEY_ALISTAIRONWEST_DESCRIPTION";
                string answerAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_DESCRIPTION";
                string answerAboutAnu = "KEY_ALISTAIRONANU_DESCRIPTION";
                string answerAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_DESCRIPTION";
                //   string answerAboutHelena = "KEY_ALISTAIRONHELENA_DESCRIPTION";
                string promptMoreQuestions = "KEY_ALISTAIR_ROADS_DESCRIPTION_2";

                GeoscapeEventDef alistairRoads = TFTVCommonMethods.CreateNewEvent(startingEvent, title, description, null);
                GeoscapeEventDef alistairRoadsAfterWest = TFTVCommonMethods.CreateNewEvent(afterWest, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterSynedrion = TFTVCommonMethods.CreateNewEvent(afterSynedrion, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterAnu = TFTVCommonMethods.CreateNewEvent(afterAnu, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterVirophage = TFTVCommonMethods.CreateNewEvent(afterVirophage, title, promptMoreQuestions, null);

                alistairRoads.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoads.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutAnu, answerAboutAnu);

                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutAnu, answerAboutAnu);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutAnu, answerAboutAnu);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutAnu, answerAboutAnu);


                alistairRoads.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoads.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoads.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;

                alistairRoadsAfterWest.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterAnu.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateIntro()
        {
            try
            {
                string introEvent_0 = "IntroBetterGeo_0";
                string introEvent_1 = "IntroBetterGeo_1";
                string introEvent_2 = "IntroBetterGeo_2";
                GeoscapeEventDef intro0 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_0, "BG_INTRO_0_TITLE", "BG_INTRO_0_DESCRIPTION", null);
                GeoscapeEventDef intro1 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_1, "BG_INTRO_1_TITLE", "BG_INTRO_1_DESCRIPTION", null);
                intro1.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO1_CHOICE_1";
                GeoscapeEventDef intro2 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_2, "BG_INTRO_2_TITLE", "BG_INTRO_2_DESCRIPTION", null);
                intro2.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO_2_CHOICE_0";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
   
        public static void ModifyAirCombatDefs()
        {
            try
            {
                //implementing Belial's proposal: 

                // ALN_VoidChamber_VehicleWeaponDef  Fire rate increased 20s-> 10s, Damage decreased 400-> 200
                // ALN_Spikes_VehicleWeaponDef	Changed to Psychic Guidance (from Visual Guidance)
                // ALN_Ram_VehicleWeaponDef Changed to Psychic Guidance(from Visual Guidance), HP 250-> 350

                // PX_Afterburner_GeoVehicleModuleDef Charges 5-> 3
                // PX_Flares_GeoVehicleModuleDef 5-> 3
                //  AN_ECMJammer_GeoVehicleModuleDef Charges 5-> 3

                //PX_ElectrolaserThunderboltHC9_VehicleWeaponDef Accuracy 95 % -> 85 %
                // PX_BasicMissileNomadAAM_VehicleWeaponDef 80 % -> 70 %
                // NJ_RailgunMaradeurAC4_VehicleWeaponDef 80 % -> 70 %
                //SY_LaserGunArtemisMkI_VehicleWeaponDef Artemis Accuracy 95 % -> 85 %


                GeoVehicleWeaponDef voidChamberWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_VoidChamber_VehicleWeaponDef"));
                GeoVehicleWeaponDef spikesWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Spikes_VehicleWeaponDef"));
                GeoVehicleWeaponDef ramWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Ram_VehicleWeaponDef"));
                GeoVehicleWeaponDef thunderboltWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_ElectrolaserThunderboltHC9_VehicleWeaponDef"));
                GeoVehicleWeaponDef nomadWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_BasicMissileNomadAAM_VehicleWeaponDef"));
                GeoVehicleWeaponDef railGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("NJ_RailgunMaradeurAC4_VehicleWeaponDef"));
                GeoVehicleWeaponDef laserGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("SY_LaserGunArtemisMkI_VehicleWeaponDef"));

                //Design decision
                GeoVehicleModuleDef afterburnerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Afterburner_GeoVehicleModuleDef"));
                GeoVehicleModuleDef flaresMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Flares_GeoVehicleModuleDef"));
                //   GeoVehicleModuleDef jammerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("AN_ECMJammer_GeoVehicleModuleDef"));

                voidChamberWDef.ChargeTime = 10.0f;
                var voidDamagePayload = voidChamberWDef.DamagePayloads[0].Damage;
                voidChamberWDef.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = voidDamagePayload, Amount = 200 };

                spikesWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                // ramWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.HitPoints = 350;
                thunderboltWDef.Accuracy = 85;
                nomadWDef.Accuracy = 70;
                railGunWDef.Accuracy = 70;
                laserGunWDef.Accuracy = 85;

                afterburnerMDef.HitPoints = 250;
                flaresMDef.HitPoints = 250;
                //flaresMDef.AmmoCount = 3;
                //jammerMDef.AmmoCount = 3;

                ResearchDbDef ppResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("pp_ResearchDB"));
                ResearchDbDef anuResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("anu_ResearchDB"));
                ResearchDbDef njResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("nj_ResearchDB"));
                ResearchDbDef synResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("syn_ResearchDB"));

                //This is testing Belial's suggestions, unlocking flares via PX Aerial Warfare, etc.
                AddItemToManufacturingReward("PX_Aircraft_Flares_ResearchDef_ManufactureResearchRewardDef_0",
                    "PX_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Flares_ResearchDef");

                ManufactureResearchRewardDef fenrirReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef virophageWeaponsReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsVirophage = virophageWeaponsReward.Items.ToList();
                rewardsVirophage.Add(fenrirReward.Items[0]);
                virophageWeaponsReward.Items = rewardsVirophage.ToArray();
                ResearchDef fenrirResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef"));
                ppResearchDB.Researches.Remove(fenrirResearch);
              

                ManufactureResearchRewardDef thunderboltReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedLasersReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedLaserTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedLasers = advancedLasersReward.Items.ToList();
                rewardsAdvancedLasers.Add(thunderboltReward.Items[0]);
                advancedLasersReward.Items = rewardsAdvancedLasers.ToArray();
                ResearchDef electroLaserResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef"));
                ppResearchDB.Researches.Remove(electroLaserResearch);

                ManufactureResearchRewardDef handOfTyrReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedShreddingReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedShreddingTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedShredding = advancedShreddingReward.Items.ToList();
                rewardsAdvancedShredding.Add(handOfTyrReward.Items[0]);
                advancedShreddingReward.Items = rewardsAdvancedShredding.ToArray();
                ResearchDef handOfTyrResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef"));
                ppResearchDB.Researches.Remove(handOfTyrResearch);

                AddItemToManufacturingReward("NJ_Aircraft_TacticalNuke_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_GuidanceTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_TacticalNuke_ResearchDef");
                ResearchDef tacticalNukeResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("NJ_Aircraft_TacticalNuke_ResearchDef"));
                ResearchDef njGuidanceResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("NJ_GuidanceTech_ResearchDef"));
                List <ResearchRewardDef> guidanceUnlocks = njGuidanceResearch.Unlocks.ToList();
                guidanceUnlocks.Add(tacticalNukeResearch.Unlocks[1]);
                njGuidanceResearch.Unlocks=guidanceUnlocks.ToArray();
                

                AddItemToManufacturingReward("NJ_Aircraft_FuelTank_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_FuelTank_ResearchDef");

                AddItemToManufacturingReward("NJ_Aircraft_CruiseControl_ResearchDef_ManufactureResearchRewardDef_0",
                    "SYN_Rover_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_CruiseControl_ResearchDef");

                ManufactureResearchRewardDef medusaAAM = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef synAirCombat = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewards = synAirCombat.Items.ToList();
                rewards.Add(medusaAAM.Items[0]);
                synAirCombat.Items = rewards.ToArray();

                ResearchDef nanotechResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_NanoTech_ResearchDef"));
                ResearchDef medusaAAMResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef"));
                synResearchDB.Researches.Remove(medusaAAMResearch);
                if (ppResearchDB.Researches.Contains(medusaAAMResearch))
                {
                    ppResearchDB.Researches.Remove(medusaAAMResearch);
                }
                List<ResearchRewardDef> nanotechUnlocks = nanotechResearch.Unlocks.ToList();
                nanotechUnlocks.Add(medusaAAMResearch.Unlocks[1]);
                nanotechResearch.Unlocks = nanotechUnlocks.ToArray();

                //This one is the source of the gamebreaking bug:
                /* AddItemToManufacturingReward("SY_EMPMissileMedusaAAM_VehicleWeaponDef",
                         "SYN_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_EMPMissile_ResearchDef");*/
                AddItemToManufacturingReward("ANU_Aircraft_Oracle_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_Oracle_ResearchDef");

                ResearchDef anuAWResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ANU_AerialWarfare_ResearchDef"));
                ResearchDef oracleResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ANU_Aircraft_Oracle_ResearchDef"));
              
                List<ResearchRewardDef> anuAWUnlocks = anuAWResearch.Unlocks.ToList();
                anuAWUnlocks.Add(oracleResearch.Unlocks[1]);
                anuAWResearch.Unlocks = anuAWUnlocks.ToArray();


                CreateManufacturingReward("ANU_Aircraft_MutogCatapult_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_Aircraft_ECMJammer_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_ECMJammer_ResearchDef", "ANU_Aircraft_MutogCatapult_ResearchDef",
                    "ANU_AdvancedBlimp_ResearchDef");
               
                ResearchDef advancedBlimpResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ANU_AdvancedBlimp_ResearchDef"));
                ResearchDef ecmResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ANU_Aircraft_ECMJammer_ResearchDef"));
                ResearchDef mutogCatapultResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ANU_Aircraft_MutogCatapult_ResearchDef"));

                List<ResearchRewardDef> advancedBlimpUnlocks = advancedBlimpResearch.Unlocks.ToList();
                advancedBlimpUnlocks.Add(ecmResearch.Unlocks[1]);
                advancedBlimpUnlocks.Add(mutogCatapultResearch.Unlocks[1]);
                advancedBlimpResearch.Unlocks = advancedBlimpUnlocks.ToArray();

                CreateManufacturingReward("PX_Aircraft_Autocannon_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_SecurityStation_ResearchDef_ManufactureResearchRewardDef_0",
                      "SYN_Aircraft_SecurityStation_ResearchDef", "PX_Aircraft_Autocannon_ResearchDef",
                      "PX_Alien_Spawnery_ResearchDef");
            
                EncounterVariableResearchRequirementDef charunEncounterVariableResearchRequirement = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Small_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                charunEncounterVariableResearchRequirement.VariableName = "CharunAreComing";

                //Changing ALN Berith research req so that they only appear after certain ODI event
                EncounterVariableResearchRequirementDef berithEncounterVariable = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Medium_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                berithEncounterVariable.VariableName = "BerithResearchVariable";

                //Changing ALN Abbadon research so they appear only in Third Act, or After ODI reaches apex
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));

                //Creating new Research Requirements, each requiring a variable to be triggered  
                EncounterVariableResearchRequirementDef variableResReqAbbadon = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqDef");
                variableResReqAbbadon.VariableName = "AbbadonResearchVariable";
              //  EncounterVariableResearchRequirementDef variableResReqAbbadonAlt = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqAltDef");
              //  variableResReqAbbadonAlt.VariableName = "ODI_Complete";
                //Altering researchDef, requiring Third Act to have started and adding an alternative way of revealing research if ODI is completed 
                ResearchDef aLN_Large_Flyer_ResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef"));
              //  aLN_Large_Flyer_ResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ANY;
              
                ReseachRequirementDefOpContainer[] reseachRequirementDefOpContainers = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                researchRequirementDefs[0] = variableResReqAbbadon;

                reseachRequirementDefOpContainers[0].Requirements = researchRequirementDefs;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container = reseachRequirementDefOpContainers;

                //Changes to FesteringSkies settings
                FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(gvw => gvw.name.Equals("FesteringSkiesSettingsDef"));
                festeringSkiesSettingsDef.SpawnInfestedAircraftChance = 0;
                festeringSkiesSettingsDef.InfestedAircraftChance = 0;
                festeringSkiesSettingsDef.InfestedAircrafts.Clear();
                festeringSkiesSettingsDef.InfestedAircraftRebuildHours = 100000;

                InterceptionGameDataDef interceptionGameDataDef = Repo.GetAllDefs<InterceptionGameDataDef>().FirstOrDefault(gvw => gvw.name.Equals("InterceptionGameDataDef"));
                interceptionGameDataDef.DisengageDuration = 3;

                RemoveHardFlyersTemplates();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void RemoveHardFlyersTemplates()
        {
            try
            {
                GeoVehicleWeaponDef acidSpit = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_AcidSpit_VehicleWeaponDef"));
                GeoVehicleWeaponDef spikes = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Spikes_VehicleWeaponDef"));
                GeoVehicleWeaponDef napalmBreath = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_NapalmBreath_VehicleWeaponDef"));
                GeoVehicleWeaponDef ram = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Ram_VehicleWeaponDef"));
                GeoVehicleWeaponDef tick = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Tick_VehicleWeaponDef"));
                GeoVehicleWeaponDef voidChamber = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_VoidChamber_VehicleWeaponDef"));

                /* GeoVehicleWeaponDamageDef shredDamage = Repo.GetAllDefs<GeoVehicleWeaponDamageDef>().FirstOrDefault(gvw => gvw.name.Equals("Shred_GeoVehicleWeaponDamageDef")); 
                 GeoVehicleWeaponDamageDef regularDamage= Repo.GetAllDefs<GeoVehicleWeaponDamageDef>().FirstOrDefault(gvw => gvw.name.Equals("Regular_GeoVehicleWeaponDamageDef"));

                 tick.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = shredDamage, Amount = 20 };
                 tick.DamagePayloads.Add(new GeoWeaponDamagePayload { Damage = regularDamage, Amount = 60 });*/


                GeoVehicleLoadoutDef charun2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Small2_VehicleLoadout"));
                GeoVehicleLoadoutDef charun4 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Small4_VehicleLoadout"));
                GeoVehicleLoadoutDef berith1 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium1_VehicleLoadout"));
                GeoVehicleLoadoutDef berith2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium2_VehicleLoadout"));
                GeoVehicleLoadoutDef berith3 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium3_VehicleLoadout"));
                GeoVehicleLoadoutDef berith4 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium4_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon1 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large1_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large2_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon3 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large3_VehicleLoadout"));

                charun2.EquippedItems[0] = napalmBreath;
                charun2.EquippedItems[1] = ram;

                charun4.EquippedItems[0] = voidChamber;
                charun4.EquippedItems[1] = spikes;

                berith1.EquippedItems[0] = acidSpit;
                berith1.EquippedItems[1] = acidSpit;
                berith1.EquippedItems[2] = spikes;
                berith1.EquippedItems[3] = ram;

                berith2.EquippedItems[0] = tick;
                berith2.EquippedItems[1] = ram;
                berith2.EquippedItems[2] = ram;
                berith2.EquippedItems[3] = spikes;

                berith3.EquippedItems[0] = napalmBreath;
                berith3.EquippedItems[1] = spikes;
                berith3.EquippedItems[2] = spikes;
                berith3.EquippedItems[3] = ram;

                berith4.EquippedItems[0] = voidChamber;
                berith4.EquippedItems[1] = napalmBreath;
                berith4.EquippedItems[2] = ram;
                berith4.EquippedItems[3] = ram;

                abbadon1.EquippedItems[0] = acidSpit;
                abbadon1.EquippedItems[1] = acidSpit;
                abbadon1.EquippedItems[2] = acidSpit;
                abbadon1.EquippedItems[3] = spikes;
                abbadon1.EquippedItems[4] = spikes;
                abbadon1.EquippedItems[5] = spikes;

                abbadon2.EquippedItems[0] = voidChamber;
                abbadon2.EquippedItems[1] = napalmBreath;
                abbadon2.EquippedItems[2] = ram;
                abbadon2.EquippedItems[3] = ram;
                abbadon2.EquippedItems[4] = ram;
                abbadon2.EquippedItems[5] = ram;

                abbadon3.EquippedItems[0] = voidChamber;
                abbadon3.EquippedItems[1] = voidChamber;
                abbadon3.EquippedItems[2] = ram;
                abbadon3.EquippedItems[3] = ram;
                abbadon3.EquippedItems[4] = spikes;
                abbadon3.EquippedItems[5] = spikes;



                /* Info about Vanilla loadouts:
               AlienFlyerResearchRewardDef aLN_Small_FlyerLoadouts= Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Small_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Small1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Small2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef
                AL_Small3_VehicleLoadout: ALN_Ram_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef

                AlienFlyerResearchRewardDef aLN_Medium_FlyerLoadouts = Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Medium_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Medium1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef
                AL_Medium2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                AL_Medium3_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                AL_Small4_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef

                AlienFlyerResearchRewardDef aLN_Large_FlyerLoadouts = Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Large1_VehicleLoadout: ALN_VoidChamber_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef
                AL_Large2_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Large3_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Small5_VehicleLoadout: ALN_Ram_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef
                AL_Medium4_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef

                */


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddItemToManufacturingReward(string researchReward, string reward, string research)
        {

            try
            {

                ResearchDbDef ppResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("pp_ResearchDB"));
                ResearchDbDef anuResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("anu_ResearchDB"));
                ResearchDbDef njResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("nj_ResearchDB"));
                ResearchDbDef synResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("syn_ResearchDB"));

                ManufactureResearchRewardDef researchRewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward));
                ManufactureResearchRewardDef rewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(reward));

                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                List<ItemDef> rewards = rewardDef.Items.ToList();
                rewards.Add(researchRewardDef.Items[0]);
                rewardDef.Items = rewards.ToArray();
                if (ppResearchDB.Researches.Contains(researchDef)) 
                {
                    ppResearchDB.Researches.Remove(researchDef);
                }
                if (anuResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (njResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (synResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateManufacturingReward(string researchReward1, string researchReward2, string research, string research2, string newResearch)
        {

            try
            {
                ResearchDbDef ppResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("pp_ResearchDB"));
                ResearchDbDef anuResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("anu_ResearchDB"));
                ResearchDbDef njResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("nj_ResearchDB"));
                ResearchDbDef synResearchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(gvw => gvw.name.Equals("syn_ResearchDB"));

                ManufactureResearchRewardDef researchReward1Def = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward1));
                ManufactureResearchRewardDef researchReward2Def = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward2));
                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                ResearchDef research2Def = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research2));
                ResearchDef newResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(newResearch));
                List<ItemDef> rewards = researchReward2Def.Items.ToList();
                rewards.Add(researchReward1Def.Items[0]);
                researchReward2Def.Items = rewards.ToArray();
                newResearchDef.Unlocks = researchDef.Unlocks;
                newResearchDef.Unlocks[0] = researchReward2Def;

                if (ppResearchDB.Researches.Contains(researchDef))
                {
                    ppResearchDB.Researches.Remove(researchDef);
                }
                if (anuResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (njResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (synResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (ppResearchDB.Researches.Contains(research2Def))
                {
                    ppResearchDB.Researches.Remove(research2Def);
                }
                if (anuResearchDB.Researches.Contains(research2Def))
                {
                    anuResearchDB.Researches.Remove(research2Def);
                }
                if (njResearchDB.Researches.Contains(research2Def))
                {
                    anuResearchDB.Researches.Remove(research2Def);
                }
                if (synResearchDB.Researches.Contains(research2Def))
                {
                    anuResearchDB.Researches.Remove(research2Def);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void RemoveCorruptionDamageBuff()
        {
            try
            {
                CorruptionStatusDef corruption_StatusDef = Repo.GetAllDefs<CorruptionStatusDef>().FirstOrDefault(ged => ged.name.Equals("Corruption_StatusDef"));
                corruption_StatusDef.Multiplier = 0.0f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
 
        public static void ChangesToMedbay()
        {
            try
            {
                HealFacilityComponentDef e_HealMedicalBay_PhoenixFacilityDe = Repo.GetAllDefs<HealFacilityComponentDef>().FirstOrDefault(ged => ged.name.Equals("E_Heal [MedicalBay_PhoenixFacilityDef]"));
                e_HealMedicalBay_PhoenixFacilityDe.BaseHeal = 16;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void MistOnAllMissions()
        {
            try
            {
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    missionTypeDef.SpawnMistAtLevelStart = true;
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        
        public static void ChangeUmbra()

        {
            try
            {
                RandomValueEffectConditionDef randomValueFishUmbra = Repo.GetAllDefs<RandomValueEffectConditionDef>().FirstOrDefault(ged => ged.name.Equals("E_RandomValue [UmbralFishmen_FactionEffectDef]"));
                RandomValueEffectConditionDef randomValueCrabUmbra = Repo.GetAllDefs<RandomValueEffectConditionDef>().FirstOrDefault(ged => ged.name.Equals("E_RandomValue [UmbralCrabmen_FactionEffectDef]"));
                randomValueCrabUmbra.ThresholdValue=0;
                randomValueFishUmbra.ThresholdValue = 0;
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                //Changing Umbra Crab and Triton to appear after SDI event 3;
                ResearchDef umbraCrabResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_CrabmanUmbra_ResearchDef"));

                //Creating new Research Requirement, requiring a variable to be triggered  
                string variableUmbraALNResReq = "Umbra_Encounter_Variable";
                EncounterVariableResearchRequirementDef variableResReqUmbra = Helper.CreateDefFromClone(sourceVarResReq, "0CCC30E0-4DB1-44CD-9A60-C1C8F6588C8A", "UmbraResReqDef");
                variableResReqUmbra.VariableName = variableUmbraALNResReq;
                // This changes the Umbra reserach so that 2 conditions have to be fulfilled: 1) a) nest has to be researched, or b) exotic material has to be found
                // (because 1)a) is fufilled at start of the game, b)) is redundant but harmless), and 2) a special variable has to be triggered, assigned to event sdi3
                umbraCrabResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraCrabResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraCrabResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Now same thing for Triton Umbra, but it will use same variable because we want them to appear at the same time
                ResearchDef umbraFishResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_FishmanUmbra_ResearchDef"));
                umbraFishResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraFishResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraFishResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Because Triton research has 2 requirements in the second container, we set them to any
                umbraFishResearch.RevealRequirements.Container[1].Operation = ResearchContainerOperation.ANY;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void Create_VoidOmen_Events()

        {
            TFTVCommonMethods.CreateNewEvent("VoidOmen", "", "", null);
            TFTVCommonMethods.CreateNewEvent("VoidOmenIntro", "", "", null);

        }

        public static void AugmentationEventsDefs()
        {
            try
            {
                //ID all the factions for later
                GeoFactionDef phoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef newJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                //Anu pissed at player for doing Bionics
                GeoscapeEventDef anuPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed1", "ANU_PISSED_BIONICS_TITLE", "ANU_PISSED_BIONICS_TEXT_GENERAL_0", "ANU_PISSED_BIONICS_CHOICE_0_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_PISSED_BIONICS_CHOICE_0";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_1", "ANU_PISSED_BIONICS_CHOICE_1_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_2", "ANU_PISSED_BIONICS_CHOICE_2_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_Anu_Pissed_Made_Promise", 1, true));


                //Anu really pissed at player for doing Bionics
                GeoscapeEventDef anuReallyPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed2", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_TEXT_GENERAL_0", null);
                anuReallyPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_REALLY_PISSED_BIONICS_CHOICE_0";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -6));

                //NJ pissed at player for doing Mutations
                GeoscapeEventDef nJPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed1", "NJ_PISSED_MUTATIONS_TITLE", "NJ_PISSED_MUTATIONS_TEXT_GENERAL_0", "NJ_PISSED_MUTATIONS_CHOICE_0_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_PISSED_MUTATIONS_CHOICE_0";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -5));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_1", "NJ_PISSED_MUTATIONS_CHOICE_1_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -8));
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_2", "NJ_PISSED_MUTATIONS_CHOICE_2_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_NJ_Pissed_Made_Promise", 1, true));

                //NJ really pissed at player for doing Mutations
                GeoscapeEventDef nJReallyPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed2", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_TEXT_GENERAL_0", null);
                nJReallyPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -6));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Story events:

        public static void CreateEventFirstFlyer()
        {
            try
            {
                string eventID = "OlenaOnFirstFlyer";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_FLYER_TITLE", "OLENA_ON_FIRST_FLYER_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenTarget()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenTarget";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_HAVEN_TARGET_TITLE", "OLENA_ON_FIRST_HAVEN_TARGET_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenAttack()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenAttack";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "FIRST_HAVEN_ATTACK_TITLE", "FIRST_HAVEN_ATTACK_TEXT", "FIRST_HAVEN_ATTACK_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventMessagesFromTheVoid()
        {
            try
            {
                string eventID = "AlistairOnMessagesFromTheVoid";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "AFTER_YE_SIGNAL_TITLE", "AFTER_YE_SIGNAL_TEXT", "AFTER_YE_SIGNAL_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;
                newEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "AFTER_YE_SIGNAL_CHOICE";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateBehemothPattern()
        {
            try
            {
                string eventID = "OlenaOnBehemothPattern";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "BEHEMOTH_PATTERN_TITLE", "BEHEMOTH_PATTERN_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateTrappedInMist()
        {
            try
            {
                string eventID = "OlenaOnHavenInfested";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_HAVEN_INFESTED_TITLE", "OLENA_ON_HAVEN_INFESTED_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


     

    }

}
