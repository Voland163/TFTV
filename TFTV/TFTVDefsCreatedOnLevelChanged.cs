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

namespace TFTV
{
    internal class TFTVDefsCreatedOnLevelChanged
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static bool ApplyChangeReduceResources = true;
        public static void ModifyAmountResourcesEvents(float resourceMultiplier)
        {
            try
            {
                if (ApplyChangeReduceResources)
                {

                    foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                    {
                        foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                        {
                            if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                            {
                                choice.Outcome.Resources *= resourceMultiplier;
                            }
                        }
                    }
                    ApplyChangeReduceResources = false;
                }

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


        public static void HibernationModuleStaminaRecuperation()
        {

            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
                GeoVehicleModuleDef hibernationmodule = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(ged => ged.name.Equals("SY_HibernationPods_GeoVehicleModuleDef"));

                if (config.ActivateStaminaRecuperatonModule)
                {
                    hibernationmodule.GeoVehicleModuleBonusValue = 0.35f;

                }
                else
                {
                    hibernationmodule.GeoVehicleModuleBonusValue = 0;
                }

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
                InjectOlenaKimLines();


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
                GeoscapeEventDef intro2 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_2, "BG_INTRO_2_TITLE", "BG_INTRO_2_DESCRIPTION", null);


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

                //This is testing Belial's suggestions, unlocking flares via PX Aerial Warfare, etc.
                AddItemToManufacturingReward("PX_Aircraft_Flares_ResearchDef_ManufactureResearchRewardDef_0",
                    "PX_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Flares_ResearchDef");

                ManufactureResearchRewardDef fenrirReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef virophageWeaponsReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsVirophage = virophageWeaponsReward.Items.ToList();
                rewardsVirophage.Add(fenrirReward.Items[0]);
                virophageWeaponsReward.Items = rewardsVirophage.ToArray();
                ResearchDef fenrirResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef"));
                fenrirResearch.HideInUI = true;

                ManufactureResearchRewardDef thunderboltReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedLasersReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedLaserTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedLasers = advancedLasersReward.Items.ToList();
                rewardsAdvancedLasers.Add(thunderboltReward.Items[0]);
                advancedLasersReward.Items = rewardsAdvancedLasers.ToArray();
                ResearchDef electroLaserResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef"));
                electroLaserResearch.HideInUI = true;

                ManufactureResearchRewardDef handOfTyrReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedShreddingReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedShreddingTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedShredding = advancedShreddingReward.Items.ToList();
                rewardsAdvancedShredding.Add(handOfTyrReward.Items[0]);
                advancedShreddingReward.Items = rewardsAdvancedShredding.ToArray();
                ResearchDef handOfTyrResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef"));
                handOfTyrResearch.HideInUI = true;

                AddItemToManufacturingReward("NJ_Aircraft_TacticalNuke_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_GuidanceTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_TacticalNuke_ResearchDef");
                AddItemToManufacturingReward("NJ_Aircraft_FuelTank_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_FuelTank_ResearchDef");
                AddItemToManufacturingReward("NJ_Aircraft_CruiseControl_ResearchDef_ManufactureResearchRewardDef_0",
                    "SYN_Rover_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_CruiseControl_ResearchDef");

                ManufactureResearchRewardDef medusaAAM = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef synAirCombat = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewards = synAirCombat.Items.ToList();
                rewards.Add(medusaAAM.Items[0]);
                synAirCombat.Items = rewards.ToArray();
                ResearchDef medusaAAMResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef"));
                medusaAAMResearch.HideInUI = true;

                //This one is the source of the gamebreaking bug:
                /* AddItemToManufacturingReward("SY_EMPMissileMedusaAAM_VehicleWeaponDef",
                         "SYN_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_EMPMissile_ResearchDef");*/
                AddItemToManufacturingReward("ANU_Aircraft_Oracle_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_Oracle_ResearchDef");
                CreateManufacturingReward("ANU_Aircraft_MutogCatapult_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_Aircraft_ECMJammer_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_ECMJammer_ResearchDef", "ANU_Aircraft_MutogCatapult_ResearchDef",
                    "ANU_AdvancedBlimp_ResearchDef");
                CreateManufacturingReward("PX_Aircraft_Autocannon_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_SecurityStation_ResearchDef_ManufactureResearchRewardDef_0",
                      "SYN_Aircraft_SecurityStation_ResearchDef", "PX_Aircraft_Autocannon_ResearchDef",
                      "PX_Alien_Spawnery_ResearchDef");


                EncounterVariableResearchRequirementDef charunEncounterVariableResearchRequirement = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Small_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                charunEncounterVariableResearchRequirement.VariableName = "CharunAreComing";

                //Changing ALN Berith research req so that they only appear after certain ODI event
                EncounterVariableResearchRequirementDef berithEncounterVariable = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Medium_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                berithEncounterVariable.VariableName = "BerithAreComing";

                //Changing ALN Abbadon research so they appear only in Third Act, or After ODI reaches apex
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));

                //Creating new Research Requirements, each requiring a variable to be triggered  
                EncounterVariableResearchRequirementDef variableResReqAbbadon = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqDef");
                variableResReqAbbadon.VariableName = "ThirdActStarted";
                EncounterVariableResearchRequirementDef variableResReqAbbadonAlt = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqAltDef");
                variableResReqAbbadonAlt.VariableName = "ODI_Complete";
                //Altering researchDef, requiring Third Act to have started and adding an alternative way of revealing research if ODI is completed 
                ResearchDef aLN_Large_Flyer_ResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef"));
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ANY;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container[0].Requirements.AddItem(variableResReqAbbadon);
                ReseachRequirementDefOpContainer[] reseachRequirementDefOpContainers = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                researchRequirementDefs[0] = variableResReqAbbadonAlt;

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

                ManufactureResearchRewardDef researchRewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward));
                ManufactureResearchRewardDef rewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(reward));

                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                List<ItemDef> rewards = rewardDef.Items.ToList();
                rewards.Add(researchRewardDef.Items[0]);
                rewardDef.Items = rewards.ToArray();
                researchDef.HideInUI = true;

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
                researchDef.HideInUI = true;
                research2Def.HideInUI = true;

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

        public static void CreateDeliriumPerks()
        {
            Clone_GameTag();
            AddAnimation();
            Create_InnerSight();
            Create_Terror();
            Create_FasterSynapses();
            Create_Anxiety();
            Create_OneOfThem();
            Create_OneOfThemPassive();
            Create_Bloodthirsty();
            //Clone_Inspire();
            Create_Wolverine();
            Create_WolverinePassive();
            //Create_Derealization();
            Create_DerealizationIgnorePain();
            Create_Feral();
            Create_Hyperalgesia();
            //Clone_ArmorBuffStatus();
            Create_AnxietyStatus();
        }

        public static void AddAnimation()
        {
            ApplyStatusAbilityDef devour = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutog_Devour_AbilityDef"));
            PlayActionAnimationAbilityDef devourAnim = Repo.GetAllDefs<PlayActionAnimationAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutog_PlayDevourAnimation_AbilityDef"));

            //OnActorDeathEffectStatusDef devourStatus = (OnActorDeathEffectStatusDef)devour.StatusDef;
            //devourStatus.Range = 99;
            //devourStatus.RequiredDyingActorTags = null;


            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(devour))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(devour).ToArray();
                }
            }

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(devourAnim))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(devourAnim).ToArray();
                }
            }
        }
        public static void Clone_GameTag()
        {
            string skillName = "OneOfUsMistResistance_GameTagDef";
            GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
            GameTagDef Takashi = Helper.CreateDefFromClone(
                source,
                "F9FF0EF9-4800-4355-B6F4-5543994C129F",
                skillName);

            TacticalVoxelMatrixDataDef tVMDD = Repo.GetAllDefs<TacticalVoxelMatrixDataDef>().FirstOrDefault(dtb => dtb.name.Equals("TacticalVoxelMatrixDataDef"));
            tVMDD.MistImmunityTags = new GameTagsList()
            {
                Takashi,
            };
        }
        public static void Create_InnerSight()
        {
            string skillName = "InnerSight_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
            PassiveModifierAbilityDef shutEye = Helper.CreateDefFromClone(
                source,
                "95431c82-a525-4975-a8da-9add9799a340",
                skillName);
            shutEye.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "69bbcec5-d491-4e7e-85a2-1063716f4532",
                skillName);
            shutEye.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "28d440ee-c254-427a-b0a9-fe62a25faeac",
                skillName);
            shutEye.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Perception,
                    Modification = StatModificationType.Add,
                    Value = -10
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.HearingRange,
                    Modification = StatModificationType.Add,
                    Value = 10
                },
              };
            shutEye.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            shutEye.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_INNER_SIGHT_NAME";
            shutEye.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_INNER_SIGHT_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Volunteered_1-2.png");
            shutEye.ViewElementDef.LargeIcon = icon;
            shutEye.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Anxiety()
        {
            string skillName = "AnxietyAbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
            PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                source,
                "5d3421cb-9e22-4cdf-bcac-3beac61b2713",
                skillName);
            hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "92560850-084c-4d43-8c57-a4f5773e4a26",
                skillName);
            hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "b8c58fc2-c56e-4577-a187-c0922cba8468",
                skillName);
            hallucinating.StatModifications = new ItemStatModification[0];
            hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            hallucinating.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ANXIETY_NAME";
            hallucinating.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ANXIETY_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Paranoid_2-1.png");
            hallucinating.ViewElementDef.LargeIcon = icon;
            hallucinating.ViewElementDef.SmallIcon = icon;
        }
        public static void Clone_Inspire()
        {
            string skillName = "BloodthirstyHP_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
            ApplyStatusAbilityDef bloodthirsty = Helper.CreateDefFromClone(
                source,
                "FF52ACBE-FFB2-4A96-8DC2-0B8072036669",
                skillName);
            bloodthirsty.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "B5D6B88F-5F0A-4B3B-9F53-3E14276F4533",
                skillName);
            bloodthirsty.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "0078B9D3-8DFF-40C6-A009-8B572EFCF87A",
                skillName);

            OnActorDeathEffectStatusDef bloodthirstyStatus = Helper.CreateDefFromClone(
                bloodthirsty.StatusDef as OnActorDeathEffectStatusDef,
                "42600C75-8E8A-4AC9-B192-49960957CAAA",
                "E_KillListenerStatus [" + skillName + "]");

            FactionMembersEffectDef bloodthirstyStatus2 = Helper.CreateDefFromClone(
                bloodthirstyStatus.EffectDef as FactionMembersEffectDef,
                "452133A6-BB2E-4DE7-B561-073CCBE48D49",
                "E_Effect [" + skillName + "]");

            StatsModifyEffectDef bloodthirstySingleEffectDef2 = Helper.CreateDefFromClone(
                bloodthirstyStatus2.SingleTargetEffect as StatsModifyEffectDef,
                "9F39B97B-9DB7-4076-96AE-4AAD317E1A6D",
                "E_SingleTargetEffect [" + skillName + "]");

            bloodthirsty.ApplyStatusToAllTargets = false;
            //(fleshEater.StatusDef as OnActorDeathEffectStatusDef).EffectDef = fleshEaterEffectDef;
            //fleshEaterEffectDef.SingleTargetEffect = fleshEaterSingleTargetEffectDef;
            //fleshEaterSingleTargetEffectDef.StatModifications[0].StatName = "";
            bloodthirsty.StatusDef = bloodthirstyStatus;
            bloodthirstyStatus.EffectDef = bloodthirstyStatus2;
            bloodthirstyStatus2.SingleTargetEffect = bloodthirstySingleEffectDef2;
            bloodthirstyStatus2.IgnoreTargetActor = false;

            bloodthirstySingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "Health",
                    Value = 80,
                }
            };
        }
        public static void Create_Bloodthirsty()
        {
            string skillName = "Bloodthirsty_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
            ApplyStatusAbilityDef bloodthirsty = Helper.CreateDefFromClone(
                source,
                "0319cf53-65d2-4964-98d2-08c1acb54b24",
                skillName);
            bloodthirsty.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "b101c95b-cd35-4649-9983-2662a454e40f",
                skillName);
            bloodthirsty.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "ed164c5a-2927-422a-a086-8762137d4c5d",
                skillName);

            OnActorDeathEffectStatusDef bloodthirstyStatus = Helper.CreateDefFromClone(
                bloodthirsty.StatusDef as OnActorDeathEffectStatusDef,
                "ac7195f9-c382-4f79-a956-55d5eb3b6371",
                "E_KillListenerStatus [" + skillName + "]");

            FactionMembersEffectDef bloodthirstyEffectDef2 = Helper.CreateDefFromClone(
                bloodthirstyStatus.EffectDef as FactionMembersEffectDef,
                "8bd34f58-d452-4f38-975e-4f32b33d283d",
                "E_Effect [" + skillName + "]");

            StatsModifyEffectDef bloodthirstySingleEffectDef2 = Helper.CreateDefFromClone(
                bloodthirstyEffectDef2.SingleTargetEffect as StatsModifyEffectDef,
                "ad0891cf-fe7a-443f-acb9-575c3cf23432",
                "E_SingleTargetEffect [" + skillName + "]");



            bloodthirsty.StatusDef = bloodthirstyStatus;
            bloodthirstyStatus.EffectDef = bloodthirstyEffectDef2;
            bloodthirstyEffectDef2.SingleTargetEffect = bloodthirstySingleEffectDef2;
            // This is working:
            bloodthirstySingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "WillPoints",
                    Value = -2,
                }
            };

            bloodthirsty.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_NAME";
            bloodthirsty.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_Devour_AbilityDef]")).LargeIcon;
            bloodthirsty.ViewElementDef.LargeIcon = icon;
            bloodthirsty.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_FasterSynapses()
        {
            string skillName = "FasterSynapses_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef angerIssues = Helper.CreateDefFromClone(
                source,
                "c1a545b3-eb5d-47f0-bf59-82710415d559",
                skillName);
            angerIssues.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "561c23c1-ce46-4862-b49f-0fd3656cdefc",
                skillName);
            angerIssues.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "da704d9c-354c-4e2b-a61d-af3b23f47522",
                skillName);
            angerIssues.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = -0.25f
                },
              };
            angerIssues.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            angerIssues.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FASTER_SYNAPSES_NAME";
            angerIssues.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_FASTER_SYNAPSES_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_View [WarCry_AbilityDef]")).LargeIcon;
            angerIssues.ViewElementDef.LargeIcon = icon;
            angerIssues.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Terror()
        {
            string skillName = "Terror_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef photophobia = Helper.CreateDefFromClone(
                source,
                "42399bdf-b43b-40f4-a471-89d082a31fde",
                skillName);
            photophobia.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "7e8fff90-a757-4794-81a9-a90cb97cb325",
                skillName);
            photophobia.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "2e4f7cec-80de-423c-914d-865700949a93",
                skillName);
            photophobia.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Speed,
                    Modification = StatModificationType.Add,
                    Value = -2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = 0.25f
                },
              };
            photophobia.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            photophobia.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_TERROR_NAME";
            photophobia.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_TERROR_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_NightOwl.png");
            photophobia.ViewElementDef.LargeIcon = icon;
            photophobia.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_WolverinePassive()
        {
            string skillName = "WolverinePassive_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Cautious_AbilityDef"));
            PassiveModifierAbilityDef nailsPassive = Helper.CreateDefFromClone(
                source,
                "b3185867-ca87-4e59-af6d-012267a7bd25",
                skillName);
            nailsPassive.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3e57b19b-11e1-42b9-81f4-c9cc9fffc42d",
                skillName);
            nailsPassive.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "3f170800-b819-4237-80a3-c9b9daa9dab4",
                skillName);
            nailsPassive.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = -0.2f
                },
              };
            nailsPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            nailsPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_NAME";
            nailsPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutoid_SlashingStrike_AbilityDef]")).SmallIcon;
            nailsPassive.ViewElementDef.LargeIcon = icon;
            nailsPassive.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Wolverine()
        {
            string skillName = "Wolverine_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutoid_Adapt_RightArm_Slasher_AbilityDef"));
            ApplyStatusAbilityDef nails = Helper.CreateDefFromClone(
                source,
                "bb65ab9c-94ae-4878-b999-e04946f720aa",
                skillName);
            nails.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "c050760d-1fb7-4b25-9295-00d98aedad19",
                skillName);
            nails.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "e9bd7acb-6955-414b-a2de-7544c38b7b6e",
                skillName);

            nails.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_NAME";
            nails.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutoid_SlashingStrike_AbilityDef]")).SmallIcon;
            nails.ViewElementDef.LargeIcon = icon;
            nails.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_OneOfThem()
        {
            string skillName = "OneOfThem_AbilityDef";
            DamageMultiplierAbilityDef source = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("VirusResistant_DamageMultiplierAbilityDef"));
            DamageMultiplierAbilityDef oneOfUs = Helper.CreateDefFromClone(
                source,
                "d4f5f9f2-43b6-4c3e-a5db-78a7a9cccd3e",
                skillName);
            oneOfUs.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "569a8f7b-41bf-4a0c-93ce-d96006f4ed27",
                skillName);
            oneOfUs.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "3cc4d8c8-739c-403b-92c9-7a6f5c54abb5",
                skillName);

            oneOfUs.DamageTypeDef = Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Mist_SpawnVoxelDamageTypeEffectDef"));
            oneOfUs.Multiplier = 0;

            oneOfUs.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
            oneOfUs.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-2.png");
            oneOfUs.ViewElementDef.LargeIcon = icon;
            oneOfUs.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_OneOfThemPassive()
        {
            string skillName = "OneOfThemPassive_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef oneOfThemPassive = Helper.CreateDefFromClone(
                source,
                "ff35f9ef-ad67-42ff-9dcd-0288dba4d636",
                skillName);
            oneOfThemPassive.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "61e44215-fc05-4383-b9e4-17f384e3d003",
                skillName);
            oneOfThemPassive.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "aaead24e-9dba-4ef7-ba2d-8df142cb9105",
                skillName);

            oneOfThemPassive.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = -2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = -2
                },
              };

            DamageMultiplierStatusDef mistResistance = Repo.GetAllDefs<DamageMultiplierStatusDef>().FirstOrDefault(a => a.name.Contains("MistResistance_StatusDef"));
            mistResistance.Multiplier = 0.0f;
            oneOfThemPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            oneOfThemPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
            oneOfThemPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-2.png");
            oneOfThemPassive.ViewElementDef.LargeIcon = icon;
            oneOfThemPassive.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Feral()
        {
            try
            {
                string skillName = "Feral_AbilityDef";
                ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("RapidClearance_AbilityDef"));
                ProcessDeathReportEffectDef sourceEffect = Repo.GetAllDefs<ProcessDeathReportEffectDef>().FirstOrDefault(p => p.name.Equals("E_Effect [RapidClearance_AbilityDef]"));
                ApplyStatusAbilityDef feral = Helper.CreateDefFromClone(
                    source,
                    "34612505-8512-4eb3-8429-ef087c07c764",
                    skillName);
                feral.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "75660746-2f27-41d1-97e3-f0d6340e96b7",
                    skillName);
                feral.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "1135128c-a10d-4285-9d03-d93a4afd6733",
                    skillName);
                OnActorDeathEffectStatusDef feralStatusDef = Helper.CreateDefFromClone(
                    source.StatusDef as OnActorDeathEffectStatusDef,
                    "9510c7e3-bef7-4b89-b20a-3bb57a7e664b",
                    "E_FeralStatus [Feral_AbilityDef]");
                ProcessDeathReportEffectDef feralEffectDef = Helper.CreateDefFromClone(
                    sourceEffect,
                    "d0f71701-4255-4b57-a387-0f3c936ed29e",
                    "E_Effect [Feral_AbilityDef]");

                feral.StatusApplicationTrigger = StatusApplicationTrigger.AbilityAdded;
                feral.Active = false;
                feral.WillPointCost = 0;
                feral.StatusDef = feralStatusDef;
                feralStatusDef.EffectDef = feralEffectDef;
                feralStatusDef.ExpireOnEndOfTurn = false;
                feralStatusDef.Duration = -1;
                feralStatusDef.DurationTurns = -1;
                feralEffectDef.RestoreActionPointsFraction = 0;

                feral.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FERAL_NAME";
                feral.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_FERAL_DESCRIPTION";
                Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_PrimalInstinct_AbilityDef]")).LargeIcon;
                feral.ViewElementDef.LargeIcon = icon;
                feral.ViewElementDef.SmallIcon = icon;
                feralStatusDef.ExpireOnEndOfTurn = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void Create_Hyperalgesia()
        {
            string skillName = "Hyperalgesia_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef solipsism = Helper.CreateDefFromClone(
                source,
                "ccd66e53-6258-4fa6-a185-66ba0f5bc4b7",
                skillName);
            solipsism.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "1aef5152-c6d6-435f-959e-0ac368dcf248",
                skillName);
            solipsism.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "ff72f143-8f3e-4988-a5fd-566faa5cb281",
                skillName);


            solipsism.StatModifications = new ItemStatModification[0];
            solipsism.ItemTagStatModifications = new EquipmentItemTagStatModification[0];

            solipsism.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_HYPERALGESIA_NAME";
            solipsism.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_HYPERALGESIA_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Privileged_1-2.png");
            solipsism.ViewElementDef.LargeIcon = icon;
            solipsism.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_AnxietyStatus()
        {
            string skillName = "Anxiety_StatusDef";
            SilencedStatusDef source = Repo.GetAllDefs<SilencedStatusDef>().FirstOrDefault(p => p.name.Equals("ActorSilenced_StatusDef"));
            SilencedStatusDef hallucinatingStatus = Helper.CreateDefFromClone(
                source,
                "2d5ed7eb-f4f3-42bf-8589-1d50ec99fa8b",
                skillName);

            hallucinatingStatus.DurationTurns = 2;
        }
        public static void Create_Derealization()
        {
            string skillName = "Derealization_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef derealization = Helper.CreateDefFromClone(
                source,
                "51ddff8e-49d0-4cca-8f4f-53aa39fcbce9",
                skillName);
            derealization.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3efc6f6b-8c57-405b-afe4-f20491336bd5",
                skillName);
            derealization.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "604181c6-fd18-46be-a3af-0b756a8200f1",
                skillName);
            derealization.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = -5,
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = -5,
                },
              };
            derealization.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            derealization.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
            derealization.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Vampire.png");
            derealization.ViewElementDef.LargeIcon = icon;
            derealization.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_DerealizationIgnorePain()
        {
            string skillName = "DerealizationIgnorePain_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("IgnorePain_AbilityDef"));
            ApplyStatusAbilityDef derealizationIgnorePain = Helper.CreateDefFromClone(
                source,
                "eea26659-d54f-48d8-8025-cb7ca53c1749",
                skillName);
            derealizationIgnorePain.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "d99c2d2f-0cff-412c-ad99-218b39158c88",
                skillName);
            derealizationIgnorePain.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "3f8b13e1-70ff-4964-923d-1e2c73f66f4f",
                skillName);

            derealizationIgnorePain.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
            derealizationIgnorePain.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Vampire.png");
            derealizationIgnorePain.ViewElementDef.LargeIcon = icon;
            derealizationIgnorePain.ViewElementDef.SmallIcon = icon;
        }
        public static void Clone_ArmorBuffStatus()
        {
            try
            {

                string skillName = "ArmorBuffStatus_StatusDef";
                ItemSlotStatsModifyStatusDef source = Repo.GetAllDefs<ItemSlotStatsModifyStatusDef>().FirstOrDefault(p => p.name.Equals("E_Status [Acheron_RestorePandoranArmor_AbilityDef]"));
                ItemSlotStatsModifyStatusDef armorBuffStatus = Helper.CreateDefFromClone(
                    source,
                    "D2B46847-FC47-436D-A940-19CDEF472ED1",
                    skillName);

                armorBuffStatus.StatsModifications = new ItemSlotStatsModifyStatusDef.ItemSlotModification[]
                {
                new ItemSlotStatsModifyStatusDef.ItemSlotModification()
                {
                    Type = ItemSlotStatsModifyStatusDef.StatType.Armour,
                    ModificationType = StatModificationType.Add,
                    Value = 10,
                },
                new ItemSlotStatsModifyStatusDef.ItemSlotModification()
                {
                    Type = ItemSlotStatsModifyStatusDef.StatType.Armour,
                    ModificationType = StatModificationType.AddMax,
                    Value = 10,
                },
                };



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantDefs()
        {
            try
            {
                CreateRevenantAbility();
                CreateRevenantStatusEffect();
                CreateRevenantGameTags();
                CreateRevenantAbilityForAssault();
                CreateRevenantAbilityForBerserker();
                CreateRevenantAbilityForHeavy();
                CreateRevenantAbilityForInfiltrator();
                CreateRevenantAbilityForPriest();
                CreateRevenantAbilityForSniper();
                CreateRevenantAbilityForTechnician();
                CreateRevenantResistanceAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateRevenantStatusEffect()
        {
            try
            {
                AddAbilityStatusDef sourceAbilityStatusDef =
                     Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                     (ged => ged.name.Equals("OilCrab_AddAbilityStatusDef"));
                PassiveModifierAbilityDef Revenant_Ability =
                    Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Revenant_AbilityDef"));
                AddAbilityStatusDef newAbilityStatusDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "68EE5958-D977-4BD4-9018-CAE03C5A6579", "Revenant_StatusEffectDef");
                newAbilityStatusDef.AbilityDef = Revenant_Ability;
                newAbilityStatusDef.ApplicationConditions = new EffectConditionDef[] { };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbility()
        {
            try
            {

                string skillName = "Revenant_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef revenantAbility = Helper.CreateDefFromClone(
                    source,
                    "8A62302E-9C2D-4AFA-AFF3-2F526BF82252",
                    skillName);
                revenantAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "FECD4DD8-5E1A-4A0F-BC3A-C2F0AA30E41F",
                    skillName);
                revenantAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "75B1017A-0455-4B44-91F0-3E1446899B42",
                    skillName);
                revenantAbility.StatModifications = new ItemStatModification[0];
                revenantAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant", true);
                revenantAbility.ViewElementDef.Description = new LocalizedTextBind("Nothing because fail", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantAbility.ViewElementDef.LargeIcon = icon;
                revenantAbility.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForAssault()
        {
            try
            {

                string skillName = "RevenantAssault_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef revenantAssault = Helper.CreateDefFromClone(
                    source,
                    "1045EB8D-1916-428F-92EF-A15FD2807818",
                    skillName);
                revenantAssault.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "7FF5A3CF-6BBD-4E4F-9E80-2DB7BDB29112",
                    skillName);
                revenantAssault.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "47BE3577-1D68-4FB2-BFA3-0A158FC710D9",
                    skillName);
                revenantAssault.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                };
                revenantAssault.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantAssault.ViewElementDef.DisplayName1 = new LocalizedTextBind("Assault Revenant", true);
                revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+10% Damage", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantAssault.ViewElementDef.LargeIcon = icon;
                revenantAssault.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForBerserker()
        {
            try
            {

                string skillName = "RevenantBerserker_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef revenantBerserker = Helper.CreateDefFromClone(
                    source,
                    "FD3FE516-25BA-44F2-9770-3AA4AD1DCB91",
                    skillName);
                revenantBerserker.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "E2707CBD-3D99-4EA4-A48D-B8E6E14EFDFD",
                    skillName);
                revenantBerserker.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3F74FAF1-1A87-4E2A-AEC2-CBB0BA5A14E0",
                    skillName);
                revenantBerserker.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                };
                revenantBerserker.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantBerserker.ViewElementDef.DisplayName1 = new LocalizedTextBind("Berserker Revenant", true);
                revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+4 Speed", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantBerserker.ViewElementDef.LargeIcon = icon;
                revenantBerserker.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForHeavy()
        {
            try
            {

                string skillName = "RevenantHeavy_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef heavy = Helper.CreateDefFromClone(
                    source,
                    "A8603522-3472-4A95-9ADF-F27E8B287D15",
                    skillName);
                heavy.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "AA5F572B-D86B-4C00-B8B9-4D86EE5F7F4D",
                    skillName);
                heavy.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "F8781E78-D106-44B3-A0E6-855BCAEB0A2F",
                    skillName);
                heavy.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 100},
                };
                heavy.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                heavy.ViewElementDef.DisplayName1 = new LocalizedTextBind("Heavy Revenant", true);
                heavy.ViewElementDef.Description = new LocalizedTextBind("+10 Strength", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                heavy.ViewElementDef.LargeIcon = icon;
                heavy.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForInfiltrator()
        {
            try
            {

                string skillName = "RevenantInfiltrator_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef infiltrator = Helper.CreateDefFromClone(
                    source,
                    "6C56E0F9-56BB-41D2-AFB1-08C8A49F69FA",
                    skillName);
                infiltrator.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1F8B6D09-A2C5-4B3F-BBED-F59675301ABB",
                    skillName);
                infiltrator.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6CAFD922-60C6-449E-A652-C2BD94386BE5",
                    skillName);
                infiltrator.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.MultiplyMax, Value = 1.15f},
                };
                infiltrator.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                infiltrator.ViewElementDef.DisplayName1 = new LocalizedTextBind("Infiltrator Revenant", true);
                infiltrator.ViewElementDef.Description = new LocalizedTextBind("+15% Stealth", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                infiltrator.ViewElementDef.LargeIcon = icon;
                infiltrator.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForPriest()
        {
            try
            {

                string skillName = "RevenantPriest_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef priest = Helper.CreateDefFromClone(
                    source,
                    "0816E671-D396-4212-910F-87B5DEC6ADE2",
                    skillName);
                priest.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C1C7FBEA-2C0B-4930-A73C-15BF3A987784",
                    skillName);
                priest.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "460AAE12-0541-40AB-A4EE-E3E206A96FB4",
                    skillName);
                priest.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10},
                };
                priest.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                priest.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                priest.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                priest.ViewElementDef.LargeIcon = icon;
                priest.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForSniper()
        {
            try
            {

                string skillName = "RevenantSniper_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef sniper = Helper.CreateDefFromClone(
                    source,
                    "4A2C53A3-D9DB-456A-8B88-AB2D90BE1DB5",
                    skillName);
                sniper.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0D811905-8C70-4D46-9CF2-1A31C5E98ED1",
                    skillName);
                sniper.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "7DCCCAAA-7245-4245-9033-F6320CCDA2AB",
                    skillName);
                sniper.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 10},
                };
                sniper.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                sniper.ViewElementDef.DisplayName1 = new LocalizedTextBind("Sniper Revenant", true);
                sniper.ViewElementDef.Description = new LocalizedTextBind("+10 Perception", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                sniper.ViewElementDef.LargeIcon = icon;
                sniper.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForTechnician()
        {
            try
            {

                string skillName = "RevenantTechnician_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "04A284AC-545A-455F-8843-54056D68022E",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1A995634-EE80-4E72-A10F-F8389E8AEB50",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "19B35512-5C23-4046-B10D-2052CDEFB769",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 5},
                 new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 5}
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Technician Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +5 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantResistanceAbility()
        {
            try
            {
                string skillName = "RevenantResistance_AbilityDef";
                DamageMultiplierAbilityDef source = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("FireResistant_DamageMultiplierAbilityDef"));
                DamageMultiplierAbilityDef revenantResistance = Helper.CreateDefFromClone(
                    source,
                    "A7F8113B-B281-4ECD-99FE-3125FCE029C4",
                    skillName);
                revenantResistance.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C298F900-A7D5-4EEC-96E1-50D017614396",
                    skillName);
                revenantResistance.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "B737C223-52D0-413B-B48F-978AD5D5BB33",
                    skillName);

                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantResistance.ViewElementDef.LargeIcon = icon;
                revenantResistance.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantGameTags()
        {
            string skillName = "RevenantTier";
            GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
            GameTagDef revenantTier1GameTag = Helper.CreateDefFromClone(
                source,
                "1677F9F4-5B45-47FA-A119-83A76EF0EC70",
                skillName + "_1_" + "GameTagDef");
            GameTagDef revenantTier2GameTag = Helper.CreateDefFromClone(
                source,
                "9A807A62-D51D-404E-ADCF-ABB4A888202E",
                skillName + "_2_" + "GameTagDef");
            GameTagDef revenantTier3GameTag = Helper.CreateDefFromClone(
                source,
                "B4BD3091-8522-4F3C-8A0F-9EE522E0E6B4",
                skillName + "_3_" + "GameTagDef");
            GameTagDef anyRevenantGameTag = Helper.CreateDefFromClone(
                source,
                "D2904A22-FE23-45B3-8879-9236E389C9E4",
                "Any_Revenant_TagDef");
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

        public static void CreateNewDefsForTFTVStart()
        {
            try
            {
                CreateNewSophiaAndJacob();
                CreateInitialInfiltrator();
                CreateInitialPriest();
                CreateInitialTechnician();
                CreateStartingTemplatesBuffed();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void CreateStartingTemplatesBuffed()
        {
            TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_Tutorial2_TacCharacterDef"));
            TacCharacterDef Sophia2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Sophia_Tutorial2_TacCharacterDef"));
            TacCharacterDef Omar3 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Omar_Tutorial3_TacCharacterDef"));
            TacCharacterDef Takeshi3 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Takeshi_Tutorial3_TacCharacterDef"));
            TacCharacterDef Irina3 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Irina_Tutorial3_TacCharacterDef"));


            Helper.CreateDefFromClone(Jacob2, "B1968124-ABDD-4A2C-9CBC-33DBC0EE3EE5", "PX_JacobBuffed_TFTV_TacCharacterDef");
            Helper.CreateDefFromClone(Sophia2, "B3EA411B-DE35-4B63-874A-553D816C06BC", "PX_SophiaBuffed_TFTV_TacCharacterDef");
            Helper.CreateDefFromClone(Omar3, "024AB8C6-A2CD-4B81-A927-C8713A008EF2", "PX_OmarBuffed_TFTV_TacCharacterDef");
            Helper.CreateDefFromClone(Takeshi3, "4230D9B4-6D88-4545-8680-BBCAE463356B", "PX_TakeshiBuffed_TFTV_TacCharacterDef");
            Helper.CreateDefFromClone(Irina3, "8E57C25C-7289-4F7C-9D0C-5F8E55601B49", "PX_IrinaBuffed_TFTV_TacCharacterDef");
        }
        public static void CreateNewSophiaAndJacob()
        {
            try
            {
                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_Tutorial2_TacCharacterDef"));
                TacCharacterDef Sophia2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Sophia_Tutorial2_TacCharacterDef"));
                TacCharacterDef newJacob = Helper.CreateDefFromClone(Jacob2, "DDA13436-40BE-4096-9C69-19A3BF6658E6", "PX_Jacob_TFTV_TacCharacterDef");
                TacCharacterDef newSophia = Helper.CreateDefFromClone(Sophia2, "D9EC7144-6EB5-451C-9015-3E67F194AB1B", "PX_Sophia_TFTV_TacCharacterDef");

                newJacob.Data.ViewElementDef = Repo.GetAllDefs<ViewElementDef>().First(ved => ved.name.Equals("E_View [PX_Sniper_ActorViewDef]"));
                GameTagDef Sniper_CTD = Repo.GetAllDefs<GameTagDef>().First(gtd => gtd.name.Equals("Sniper_ClassTagDef"));
                for (int i = 0; i < newJacob.Data.GameTags.Length; i++)
                {
                    if (newJacob.Data.GameTags[i].GetType() == Sniper_CTD.GetType())
                    {
                        newJacob.Data.GameTags[i] = Sniper_CTD;
                    }
                }

                newJacob.Data.Abilites = new TacticalAbilityDef[] // abilities -> Class proficiency
                {
                Repo.GetAllDefs<ClassProficiencyAbilityDef>().First(cpad => cpad.name.Equals("Sniper_ClassProficiency_AbilityDef"))

                };
                newJacob.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Helmet_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Legs_ItemDef"))
                };

                newJacob.Data.EquipmentItems = new ItemDef[] // Ready slots
               { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_SniperRifle_WeaponDef")),
                    Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_Pistol_WeaponDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("Medkit_EquipmentDef"))
               };
                newJacob.Data.InventoryItems = new ItemDef[] // Backpack
                {
                newJacob.Data.EquipmentItems[0].CompatibleAmmunition[0],
                newJacob.Data.EquipmentItems[1].CompatibleAmmunition[0]
                };

                newJacob.Data.Strength = 0;
                newJacob.Data.Will = 0;
                newJacob.Data.Speed = 0;
                newJacob.Data.CurrentHealth = -1;

                newSophia.Data.Strength = 0;
                newSophia.Data.Will = 0;
                newSophia.Data.Speed = 0;
                newSophia.Data.CurrentHealth = -1;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialInfiltrator()
        {
            try
            {
                TacCharacterDef sourceInfiltrator = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("S_SY_Infiltrator_TacCharacterDef"));
                TacCharacterDef startingInfiltrator = Helper.CreateDefFromClone(sourceInfiltrator, "8835621B-CFCA-41EF-B480-241D506BD742", "PX_Starting_Infiltrator_TacCharacterDef");
                startingInfiltrator.Data.Strength = 0;
                startingInfiltrator.Data.Will = 0;

                /*   startingInfiltrator.Data.BodypartItems = new ItemDef[] // Armour
                   {
                   Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Helmet_BodyPartDef")),
                   Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Torso_BodyPartDef")),
                   Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Legs_ItemDef"))
                   };           */
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialPriest()
        {
            try
            {
                TacCharacterDef sourcePriest = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("S_AN_Priest_TacCharacterDef"));
                TacCharacterDef startingPriest = Helper.CreateDefFromClone(sourcePriest, "B1C9385B-05D1-453D-8665-4102CCBA77BE", "PX_Starting_Priest_TacCharacterDef");
                startingPriest.Data.Strength = 0;
                startingPriest.Data.Will = 0;

                startingPriest.Data.BodypartItems = new ItemDef[] // Armour
                {
              //  Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Head02_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Legs_ItemDef"))
                };
                //    TFTVLogger.Always(startingPriest.Data.EquipmentItems.Count().ToString());

                /*  ItemDef[] inventoryList = new ItemDef[]

                  { 
                  startingPriest.Data.EquipmentItems[0].CompatibleAmmunition[0],
                  startingPriest.Data.EquipmentItems[1].CompatibleAmmunition[0]
                  };*/

                startingPriest.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("AN_Redemptor_WeaponDef")),
                  //  Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("Medkit_EquipmentDef"))
                };
                // startingPriest.Data.InventoryItems = inventoryList;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialTechnician()
        {
            try
            {
                TacCharacterDef sourceTechnician = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("NJ_Technician_TacCharacterDef"));
                TacCharacterDef startingTechnician = Helper.CreateDefFromClone(sourceTechnician, "1D0463F9-6684-4CE1-82CA-386FC2CE18E3", "PX_Starting_Technician_TacCharacterDef");
                startingTechnician.Data.Strength = 0;
                startingTechnician.Data.Will = 0;
                startingTechnician.Data.LevelProgression.Experience = 0;

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

    }

}
