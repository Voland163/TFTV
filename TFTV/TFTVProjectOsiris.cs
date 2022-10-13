using Base;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVProjectOsiris
    {
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly BCSettings bCSettings = TFTVMain.Main.Settings;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly string RobocopEvent = "RoboCopDeliveryEvent";
        private static readonly string ProjectOsirisEvent = "ProjectOsirisEvent";
        private static readonly string FullMutantEvent = "FullMutantEvent";
        private static readonly string DeliveryEvent = "DeliveryEvent";


        private static readonly TacCharacterDef characterDef = DefCache.GetDef<TacCharacterDef>("NJ_Jugg_TacCharacterDef");

        private static readonly TacticalItemDef juggHead = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef juggLegs = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef juggTorso = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");
        private static readonly TacticalItemDef exoHead = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef exoLegs = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef exoTorso = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Torso_BodyPartDef");
        private static readonly TacticalItemDef shinobiHead = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef shinobiLegs = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef shinobiTorso = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Torso_BodyPartDef");


        public static void CreateProjectOsirisDefs()
        {
            // CreateRoboCopDef();
            CreateProjectOsirisEvents();



        }


        public static void CreateRoboCopDef()
        {
            try
            {

                TacCharacterDef roboCopDef = Helper.CreateDefFromClone(characterDef, "36B01740-2E05-4D5A-9C2E-24D506C6F584", "RoboCop");
                //   roboCopDef.name = "RoboCop";
                //   roboCopDef.SpawnCommandId = "DeadOrAliveYouAreComingWithMe";
                roboCopDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopDef.Data.InventoryItems = new ItemDef[] { };
                roboCopDef.Data.Abilites = new TacticalAbilityDef[] { };
                roboCopDef.Data.GameTags = new GameTagDef[] { };
                //  roboCopDef.Data.BodypartItems = new ItemDef[] { juggHead, juggTorso, juggLegs };

                TacCharacterDef roboCopScoutDef = Helper.CreateDefFromClone(characterDef, "BF7FDA63-18BA-4B1B-9AD6-A17643471530", "RoboCopScoutDef");
                roboCopScoutDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopScoutDef.Data.InventoryItems = new ItemDef[] { };
                roboCopScoutDef.Data.Abilites = new TacticalAbilityDef[] { };
                roboCopScoutDef.Data.GameTags = new GameTagDef[] { };
                roboCopScoutDef.Data.BodypartItems = new ItemDef[] { exoHead, exoTorso, exoLegs };

                TacCharacterDef roboCopShinobiDef = Helper.CreateDefFromClone(characterDef, "90C338B9-BA40-44AE-8CAA-2F988D300E08", "RoboCopShinobiDef");
                roboCopShinobiDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopShinobiDef.Data.InventoryItems = new ItemDef[] { };
                roboCopShinobiDef.Data.Abilites = new TacticalAbilityDef[] { };
                roboCopShinobiDef.Data.GameTags = new GameTagDef[] { };
                roboCopShinobiDef.Data.BodypartItems = new ItemDef[] { shinobiHead, shinobiTorso, shinobiLegs };

                TFTVLogger.Always("RoboCop Defs created");


                //need to change name, and class (roboCopDef.Data.Abilites and roboCopDef.Data.GameTags)
                //to add different equipment sets roboCopDef.Data.BodypartItems



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateProjectOsirisEvents()
        {
            try
            {
                // TFTVLogger.Always("Robocop def real " + DefCache.GetDef<TacCharacterDef>("RoboCop").name);



                // TFTVLogger.Always("Check that Murphy is in the list " + bCSettings.SpecialCharacterPersonalSkills.Keys.Contains("Murphy"));
                //NJ_Exo_BIO_Helmet_BodyPartDef
                //NJ_Exo_BIO_Legs_ItemDef
                //NJ_Exo_BIO_Torso_BodyPartDef


                //SY_Shinobi_BIO_Helmet_BodyPartDef
                //SY_Shinobi_BIO_Legs_ItemDef
                //SY_Shinobi_BIO_Torso_BodyPartDef

                GeoscapeEventDef projectOsirisEvent = TFTVCommonMethods.CreateNewEvent(ProjectOsirisEvent, "PROJECT_OSIRIS_TITLE", "PROJECT_OSIRIS_TEXT", "PROJECT_OSIRIS_OUTCOME");
                TFTVCommonMethods.GenerateGeoEventChoice(projectOsirisEvent, "PROJECT_OSIRIS_CHOOSE_MUTANT", null);
                projectOsirisEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "PROJECT_OSIRIS_CHOOSE_ROBOCOP";
                projectOsirisEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = RobocopEvent;
                projectOsirisEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = FullMutantEvent;

                TFTVCommonMethods.CreateNewEvent(RobocopEvent, "ROBOCOP_DELIVERY_TITLE", "ROBOCOP_DELIVERY_TEXT", "ROBOCOP_DELIVERY_OUTCOME");
                TFTVCommonMethods.CreateNewEvent(FullMutantEvent, "FULL_MUTANT_TITLE", "FULL_MUTANT_TEXT", "FULL_MUTANT_OUTCOME");
                


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static bool[] CheckLabs(GeoLevelController controller)
        {
            try
            {
                bool[] labTypes = new bool[2];
                labTypes[0] = false;
                labTypes[1] = false;

                if (TFTVAugmentations.CheckForFacility(controller, "BionicsLab_PhoenixFacilityDef"))
                {

                    labTypes[0] = true;

                };

                if (TFTVAugmentations.CheckForFacility(controller, "MutationLab_PhoenixFacilityDef"))
                {

                    labTypes[1] = true;

                };
                return labTypes;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }


        public static void RunProjectOsiris(GeoLevelController controller)
        {
            try
            {
                if (TFTVRevenantResearch.ProjectOsirisStats.Count > 0 && (CheckLabs(controller)[0] || CheckLabs(controller)[1]))
                {
                    PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));

                    Dictionary<GeoTacUnitId, int> allProjectOsirisCandidates = new Dictionary<GeoTacUnitId, int>();

                    foreach (GeoTacUnitId geoTacUnitId in statisticsManager.CurrentGameStats.DeadSoldiers.Keys)
                    {

                        foreach (int id in TFTVRevenantResearch.ProjectOsirisStats.Keys)
                        {
                            if (geoTacUnitId == id)
                            {
                                SoldierStats deadSoldierStats = statisticsManager.CurrentGameStats.DeadSoldiers[geoTacUnitId];
                                int numMissions = deadSoldierStats.MissionsParticipated;
                                int enemiesKilled = deadSoldierStats.EnemiesKilled.Count;
                                int soldierLevel = deadSoldierStats.Level;
                                int baseScore = 0;
                                if (numMissions >= 3)
                                {
                                    baseScore = 25;
                                }
                                int score = baseScore + (numMissions + enemiesKilled + soldierLevel);
                                TFTVLogger.Always("#of Missions: " + numMissions + ". #enemies killed: " + enemiesKilled + ". level: " + soldierLevel + ". The score is " + score);
                                allProjectOsirisCandidates.Add(geoTacUnitId, score);
                            }
                        }
                    }

                    List<int> orderedList = allProjectOsirisCandidates.Values.OrderBy(x => x).ToList();

                    GeoTacUnitId idProjectOsirisCandidate = new GeoTacUnitId();

                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                    int roll = UnityEngine.Random.Range(0, 100);
                    int rollTo = orderedList.Count * 10 + orderedList[0];
                    if (rollTo > 90)
                    {
                        rollTo = 90;
                    }

                    if (roll <= 100) //testing, real rollTo instead of 100
                    {
                        foreach (GeoTacUnitId id in allProjectOsirisCandidates.Keys)
                        {
                            if (allProjectOsirisCandidates[id] == allProjectOsirisCandidates.First().Value)
                            {
                                idProjectOsirisCandidate = id;

                            }
                        }

                        GeoUnitDescriptor deadSoldierDescriptor = controller.DeadSoldiers[idProjectOsirisCandidate];
                        string name = deadSoldierDescriptor.Identity.Name;
                        GeoCharacter geoCharacterCloneFromDead = controller.DeadSoldiers[idProjectOsirisCandidate].SpawnAsCharacter();
                        TacCharacterDef deadTemplateDef = geoCharacterCloneFromDead.TemplateDef;
                        TacCharacterData saveTemplateData = deadTemplateDef.Data.Clone();
                        deadTemplateDef.Data.EquipmentItems = new ItemDef[] { };
                        deadTemplateDef.Data.InventoryItems = new ItemDef[] { };
                      //  templateDef.Data.BodypartItems = new ItemDef[] { juggHead, juggTorso, juggLegs };
                        deadTemplateDef.Data.LocalizeName = false;
                        deadTemplateDef.Data.Name = name;

                        //  templateDef.Data.LevelProgression.Experience = geoCharacter.Progression.LevelProgression.Experience;


                        TFTVLogger.Always("deadSoldierDescriptor.Progression.PersonalAbilities[0].name is " + deadSoldierDescriptor.Progression.PersonalAbilities[0].name +
                            " and deadSoldierDescriptor.Progression.PersonalAbilities[4].name is " + deadSoldierDescriptor.Progression.PersonalAbilities[4].name);

                        bCSettings.SpecialCharacterPersonalSkills.Add(name, new Dictionary<int, string>()
                        {
                            {0,deadSoldierDescriptor.Progression.PersonalAbilities[0].name},
                            {4,deadSoldierDescriptor.Progression.PersonalAbilities[4].name}
                        });

                        TFTVLogger.Always(name + " added to BC special list is " + bCSettings.SpecialCharacterPersonalSkills.Keys.Contains(name));

                        List<GameTagDef> deadSoldiersTags = new List<GameTagDef>();

                        deadSoldiersTags.AddRange(deadSoldierDescriptor.GetGameTags());
                        GameTagDef[] gameTagDefs = deadSoldiersTags.ToArray();
                        deadTemplateDef.Data.GameTags = gameTagDefs;

                        List<TacticalAbilityDef> abilityDefs = deadSoldierDescriptor.GetTacticalAbilities().ToList();
                        TacticalAbilityDef[] tacticalAbilities = abilityDefs.ToArray();
                        deadTemplateDef.Data.Abilites = tacticalAbilities;
                        deadTemplateDef.Data.LevelProgression.SetLevel(geoCharacterCloneFromDead.LevelProgression.Level);
                        //  templateDef.Data.LevelProgression.CurrentLevelExperience = geoCharacter.Progression.LevelProgression.CurrentLevelExperience;
                     
                        GeoSite phoenixBase = controller.PhoenixFaction.Bases.First().Site;
                        GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, controller.PhoenixFaction);

                        if (CheckLabs(controller)[0] && CheckLabs(controller)[1]) 
                        {
                            controller.EventSystem.TriggerGeoscapeEvent(ProjectOsirisEvent, context);

                        }

                        GeoscapeEventDef roboCopDeliveryEvent = controller.EventSystem.GetEventByID(RobocopEvent);


                        roboCopDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                        TFTVLogger.Always("RobocopDef added as customCharacter reward");


                        if (controller.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef"))
                        {
                            TFTVCommonMethods.GenerateGeoEventChoice(roboCopDeliveryEvent, "ROBOCOP_DELIVERY_SCOUT", "ROBOCOP_DELIVERY_OUTCOME");
                            // TacCharacterDef scout = templateDef;
                            //  scout.Data.BodypartItems = new ItemDef[] { exoHead, exoTorso, exoLegs };
                            // roboCopDeliveryEvent.GeoscapeEventData.Choices[1].Outcome.CustomCharacters.Add(templateDef);
                            roboCopDeliveryEvent.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("ProjectOsirisBodyChoice", 1, true));
                        }
                        if (controller.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef"))
                        {
                            TFTVCommonMethods.GenerateGeoEventChoice(roboCopDeliveryEvent, "ROBOCOP_DELIVERY_SHINOBI", "ROBOCOP_DELIVERY_OUTCOME");
                            //  roboCopDeliveryEvent.GeoscapeEventData.Choices[2].Outcome.CustomCharacters.Add(templateDef);
                            roboCopDeliveryEvent.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("ProjectOsirisBodyChoice", 2, true));
                            //  TacCharacterDef shinobi = templateDef;
                            //  shinobi.Data.BodypartItems = new ItemDef[] { shinobiHead, shinobiTorso, shinobiLegs };
                            // roboCopDeliveryEvent.GeoscapeEventData.Choices[2].Outcome.CustomCharacters.Add(shinobi);
                        }

                        TFTVLogger.Always(RobocopEvent + " options added");
                        
                       
                        TFTVLogger.Always("Event will be triggered");
                        controller.EventSystem.TriggerGeoscapeEvent(RobocopEvent, context);

                        GeoTacUnitId geoTacUnitNewCharacter = statisticsManager.CurrentGameStats.LivingSoldiers.Last().Key;
                        statisticsManager.CurrentGameStats.LivingSoldiers.Remove(geoTacUnitNewCharacter);
                        statisticsManager.CurrentGameStats.LivingSoldiers.Add(geoTacUnitNewCharacter, statisticsManager.CurrentGameStats.DeadSoldiers[idProjectOsirisCandidate]);
                        controller.DeadSoldiers.Remove(idProjectOsirisCandidate);
                        statisticsManager.CurrentGameStats.DeadSoldiers.Remove(idProjectOsirisCandidate);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Endurance.Set(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][0]);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Endurance.SetMax(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][0]);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Willpower.Set(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][1]);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Willpower.SetMax(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][1]);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Speed.Set(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][2]);
                        controller.PhoenixFaction.Soldiers.Last().CharacterStats.Speed.SetMax(TFTVRevenantResearch.ProjectOsirisStats[idProjectOsirisCandidate][2]);
                        controller.PhoenixFaction.Soldiers.Last().Identity.CopyFrom(geoCharacterCloneFromDead.Identity, PhoenixPoint.Common.Entities.Characters.CharacterIdentity.EmptyReplaceOperation.Default);
                        controller.PhoenixFaction.Soldiers.Last().Progression.SkillPoints = 0;
                        deadTemplateDef.Data = saveTemplateData;
                    }


                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




    }
}
