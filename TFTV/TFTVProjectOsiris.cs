using Base;
using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
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
using System.Diagnostics;
using System.Linq;

namespace TFTV
{
    internal class TFTVProjectOsiris
    {
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly BCSettings bCSettings = TFTVMain.Main.Settings;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static readonly string RobocopEvent = "RoboCopDeliveryEvent";
        public static readonly string ProjectOsirisEvent = "ProjectOsirisEvent";
        public static readonly string FullMutantEvent = "FullMutantEvent";
        public static readonly string RoboCopDeliveryEvent = "DeliveryEvent";
        public static readonly string ScoutDeliveryEvent = "ScoutDeliveryEvent";
        public static readonly string ShinobiDeliveryEvent = "ShinobiDeliveryEvent";
        public static readonly string HeavyMutantDeliveryEvent = "HeavyMutantDeliveryEvent";
        public static readonly string WatcherMutantDeliveryEvent = "WatcherMutantDeliveryEvent";
        public static readonly string ShooterMutantDeliveryEvent = "ShooterMutantDeliveryEvent";
        public static readonly List<string> ProjectOsirisDeliveryEvents = new List<string>()
        {RobocopEvent, ProjectOsirisEvent, FullMutantEvent, RoboCopDeliveryEvent, ScoutDeliveryEvent, HeavyMutantDeliveryEvent, ShinobiDeliveryEvent,
            WatcherMutantDeliveryEvent, ShinobiDeliveryEvent};


        private static readonly TacticalItemDef juggHead = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef juggLegs = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef juggTorso = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");
        private static readonly TacticalItemDef exoHead = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef exoLegs = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef exoTorso = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Torso_BodyPartDef");
        private static readonly TacticalItemDef shinobiHead = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Helmet_BodyPartDef");
        private static readonly TacticalItemDef shinobiLegs = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Legs_ItemDef");
        private static readonly TacticalItemDef shinobiTorso = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Torso_BodyPartDef");

        private static readonly TacticalItemDef heavyHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Helmet_BodyPartDef");
        private static readonly TacticalItemDef heavyLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Legs_ItemDef");
        private static readonly TacticalItemDef heavyTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Torso_BodyPartDef");
        private static readonly TacticalItemDef watcherHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Helmet_BodyPartDef");
        private static readonly TacticalItemDef watcherLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Legs_ItemDef");
        private static readonly TacticalItemDef watcherTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Torso_BodyPartDef");
        private static readonly TacticalItemDef shooterHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Helmet_BodyPartDef");
        private static readonly TacticalItemDef shooterLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Legs_ItemDef");
        private static readonly TacticalItemDef shooterTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Torso_BodyPartDef");

        public static TacCharacterData SaveTemplateData = new TacCharacterData();
        public static GeoTacUnitId IdProjectOsirisCandidate = new GeoTacUnitId();

        public static void CreateProjectOsirisDefs()
        {

            CreateProjectOsirisEvents();


        }


        public static void CreateProjectOsirisEvents()
        {
            try
            {
                GeoscapeEventDef projectOsirisEvent = TFTVCommonMethods.CreateNewEvent(ProjectOsirisEvent, "PROJECT_OSIRIS_TITLE", "PROJECT_OSIRIS_TEXT", null);
                TFTVCommonMethods.GenerateGeoEventChoice(projectOsirisEvent, "PROJECT_OSIRIS_CHOOSE_MUTANT", null);
                projectOsirisEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "PROJECT_OSIRIS_CHOOSE_ROBOCOP";

                TFTVCommonMethods.CreateNewEvent(RobocopEvent, "ROBOCOP_DELIVERY_TITLE", "ROBOCOP_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(FullMutantEvent, "FULL_MUTANT_TITLE", "FULL_MUTANT_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(RoboCopDeliveryEvent, "ROBOCOP_DELIVERY_TITLE", "HEAVY_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(ScoutDeliveryEvent, "ROBOCOP_DELIVERY_TITLE", "SCOUT_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(ShinobiDeliveryEvent, "ROBOCOP_DELIVERY_TITLE", "SHINOBI_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(HeavyMutantDeliveryEvent, "FULL_MUTANT_TITLE", "HEAVY_MUTANT_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(WatcherMutantDeliveryEvent, "FULL_MUTANT_TITLE", "WATCHER_MUTANT_DELIVERY_TEXT", null);
                TFTVCommonMethods.CreateNewEvent(ShooterMutantDeliveryEvent, "FULL_MUTANT_TITLE", "SHOOTER_MUTANT_DELIVERY_TEXT", null);

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

                if (TFTVAugmentations.CheckForFacility(controller, "KEY_BASE_FACILITY_BIONICSLAB_NAME"))
                {

                    labTypes[0] = true;

                };

                if (TFTVAugmentations.CheckForFacility(controller, "KEY_BASE_FACILITY_MUTATION_LAB_NAME"))
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

        public static void CheckCompletedBionicsAndMutationResearches(GeoLevelController controller)
        {
            try
            {
                GeoscapeEventDef roboCopEvent = controller.EventSystem.GetEventByID(RobocopEvent);
                GeoscapeEventDef fullMutantEvent = controller.EventSystem.GetEventByID(FullMutantEvent);

                roboCopEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = RoboCopDeliveryEvent;
                roboCopEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "HEAVY_DELIVERY_CHOICE";

                fullMutantEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = HeavyMutantDeliveryEvent;
                fullMutantEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "HEAVY_MUTANT_DELIVERY_CHOICE";

                if (controller.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef") && roboCopEvent.GeoscapeEventData.Choices.Count == 1)
                {
                    TFTVCommonMethods.GenerateGeoEventChoice(roboCopEvent, "SCOUT_DELIVERY_CHOICE", null);
                    roboCopEvent.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = ScoutDeliveryEvent;
                }

                if (controller.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef") && roboCopEvent.GeoscapeEventData.Choices.Count == 2)
                {
                    TFTVCommonMethods.GenerateGeoEventChoice(roboCopEvent, "SHINOBI_DELIVERY_CHOICE", null);
                    roboCopEvent.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = ShinobiDeliveryEvent;
                }

                if (controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech2_ResearchDef") && fullMutantEvent.GeoscapeEventData.Choices.Count == 1)
                {
                    TFTVCommonMethods.GenerateGeoEventChoice(fullMutantEvent, "WATCHER_MUTANT_DELIVERY_CHOICE", null);
                    fullMutantEvent.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = WatcherMutantDeliveryEvent;

                }
                if (controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech3_ResearchDef") && fullMutantEvent.GeoscapeEventData.Choices.Count == 2)
                {
                    TFTVCommonMethods.GenerateGeoEventChoice(fullMutantEvent, "SHOOTER_MUTANT_DELIVERY_CHOICE", null);
                    fullMutantEvent.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = ShooterMutantDeliveryEvent;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static string CreateDescriptionForEvent(GeoLevelController controller, GeoUnitDescriptor deadSoldierDescriptor)
        {
            try
            {

                string name = deadSoldierDescriptor.Identity.Name;
                string pronoun = "";
                string possesivePronoun = "";
                if (deadSoldierDescriptor.Identity.Sex == GeoCharacterSex.Male)
                {
                    pronoun = "He";
                    possesivePronoun = "him";
                }
                else if (deadSoldierDescriptor.Identity.Sex == GeoCharacterSex.Female)
                {
                    pronoun = "She";
                    possesivePronoun = "her";
                }
                else
                {
                    pronoun = "They";
                    pronoun = "them";
                }

                string typeOfBodyAvailable = "";
                string buildAdditionalLab = "";
                string and1 = "";
                string and2 = "";

                if (CheckLabs(controller)[0] && CheckLabs(controller)[1])
                {
                    typeOfBodyAvailable = "made of titanium or of mutagen flesh";
                }
                else if (CheckLabs(controller)[0] && !CheckLabs(controller)[1])
                {
                    typeOfBodyAvailable = "made of titanium";
                    buildAdditionalLab = " build a mutation lab ";
                }
                else if (!CheckLabs(controller)[0] && CheckLabs(controller)[1])
                {
                    typeOfBodyAvailable = "made of mutagen flesh";
                    buildAdditionalLab = " build a bionics lab ";
                }

                string increaseOptions = "";

                string researchAdditionalTech1 = "";
                string researchAdditionalTech2 = "";
                string anyAdditionalResearch = "";
                string anyAdditionalResearch2 = "";

                if (!controller.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef") || !controller.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef"))
                {
                    researchAdditionalTech1 = "new bionic";
                }

                if (!controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech2_ResearchDef") || !controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech3_ResearchDef"))
                {
                    researchAdditionalTech2 = "new mutation";
                }

                if (buildAdditionalLab != "" || researchAdditionalTech1 != "" || researchAdditionalTech2 != "")
                {
                    increaseOptions = "\nTo enhance Project Osiris and increase our options we should ";
                }

                if ((researchAdditionalTech1 != "" || researchAdditionalTech2 != "") && buildAdditionalLab != "")
                {
                    and1 = "and";

                }
                if (researchAdditionalTech1 != "" && researchAdditionalTech2 != "")
                {
                    and2 = " and ";
                }
                if (researchAdditionalTech1 != "" || researchAdditionalTech2 != "")
                {
                    anyAdditionalResearch = " acquire ";
                    anyAdditionalResearch2 = " research.";
                }

                string modularEventText = "<i>-''I'm... a mess.''\n-''They will fix you. They fix everything.''</i>\n\nWe have recovered " + name + "(" + deadSoldierDescriptor.GetClassViewElementDefs().First().Name + ") from the battlefield. "
                    + pronoun + " is clinically dead, but with the Project Osiris now operational we can bring " + possesivePronoun + " back with a new body " + typeOfBodyAvailable + ". " +
                    increaseOptions + buildAdditionalLab + and1 + anyAdditionalResearch + researchAdditionalTech1 + and2 + researchAdditionalTech2 + anyAdditionalResearch2;

                return modularEventText;

            }



            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static void CompleteProjectOsiris(GeoTacUnitId idProjectOsirisCandidate, GeoLevelController controller, GeoscapeEventDef geoscapeEventDef)
        {
            try
            {
                GeoUnitDescriptor deadSoldierDescriptor = controller.DeadSoldiers[idProjectOsirisCandidate];
                string name = deadSoldierDescriptor.Identity.Name;
                GeoCharacter geoCharacterCloneFromDead = controller.DeadSoldiers[idProjectOsirisCandidate].SpawnAsCharacter();
                TacCharacterDef deadTemplateDef = geoCharacterCloneFromDead.TemplateDef;
                SaveTemplateData = deadTemplateDef.Data.Clone();
                deadTemplateDef.Data.EquipmentItems = new ItemDef[] { };
                deadTemplateDef.Data.InventoryItems = new ItemDef[] { };
                deadTemplateDef.Data.LocalizeName = false;
                deadTemplateDef.Data.Name = name;

                TFTVLogger.Always($"Candidate name is {name} and it is level {deadSoldierDescriptor.Level}");

                bCSettings.SpecialCharacterPersonalSkills.Add(name, new Dictionary<int, string>()
                        {
                            {0,deadSoldierDescriptor.Progression.PersonalAbilities[0].name},
                            {1,deadSoldierDescriptor.Progression.PersonalAbilities[1].name},
                            {2,deadSoldierDescriptor.Progression.PersonalAbilities[2].name},
                            {3,deadSoldierDescriptor.Progression.PersonalAbilities[3].name},
                            {4,deadSoldierDescriptor.Progression.PersonalAbilities[4].name},
                            {5,deadSoldierDescriptor.Progression.PersonalAbilities[5].name},
                            {6,deadSoldierDescriptor.Progression.PersonalAbilities[6].name}
                        });

                TFTVLogger.Always($"{name} added to BC special list is {bCSettings.SpecialCharacterPersonalSkills.Keys.Contains(name)}");

                List<GameTagDef> deadSoldiersTags = new List<GameTagDef>();
                List<ClassTagDef> deadSoldierClassTags = new List<ClassTagDef>();

                deadSoldiersTags.AddRange(deadSoldierDescriptor.GetGameTags());
                GameTagDef[] gameTagDefs = deadSoldiersTags.ToArray();
                deadTemplateDef.Data.GameTags = gameTagDefs;

                List<TacticalAbilityDef> abilityDefs = deadSoldierDescriptor.GetTacticalAbilities().ToList();
                TacticalAbilityDef[] tacticalAbilities = abilityDefs.ToArray();
                deadTemplateDef.Data.Abilites = tacticalAbilities;


                //  deadTemplateDef.Data.LevelProgression.SetLevel(level);

                if (geoscapeEventDef == controller.EventSystem.GetEventByID(ScoutDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { exoHead, exoTorso, exoLegs };
                }
                else if (geoscapeEventDef == controller.EventSystem.GetEventByID(ShinobiDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { shinobiHead, shinobiTorso, shinobiLegs };
                }
                else if (geoscapeEventDef == controller.EventSystem.GetEventByID(RoboCopDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { juggHead, juggTorso, juggLegs };
                }
                else if (geoscapeEventDef == controller.EventSystem.GetEventByID(HeavyMutantDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { heavyHead, heavyTorso, heavyLegs };
                }
                else if (geoscapeEventDef == controller.EventSystem.GetEventByID(WatcherMutantDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { watcherHead, watcherTorso, watcherLegs };
                }
                else if (geoscapeEventDef == controller.EventSystem.GetEventByID(ShooterMutantDeliveryEvent))
                {
                    deadTemplateDef.Data.BodypartItems = new ItemDef[] { shooterHead, shooterTorso, shooterLegs };
                }

                LocalizedTextBind projectOsirisDescription = new LocalizedTextBind(CreateDescriptionForEvent(controller, deadSoldierDescriptor) + "\n\nProject Osiris has given " + name + " a new body.", true);

                GeoscapeEventDef deliveryEvent = controller.EventSystem.GetEventByID(RoboCopDeliveryEvent);
                GeoscapeEventDef scoutDeliveryEvent = controller.EventSystem.GetEventByID(ScoutDeliveryEvent);
                GeoscapeEventDef shinobiDeliveryEvent = controller.EventSystem.GetEventByID(ShinobiDeliveryEvent);
                GeoscapeEventDef heavyMutantDeliveryEvent = controller.EventSystem.GetEventByID(HeavyMutantDeliveryEvent);
                GeoscapeEventDef watcherMutantDeliveryEvent = controller.EventSystem.GetEventByID(WatcherMutantDeliveryEvent);
                GeoscapeEventDef shooterMutantDeliveryEvent = controller.EventSystem.GetEventByID(ShooterMutantDeliveryEvent);

                List<GeoscapeEventDef> allDeliveryEvents = new List<GeoscapeEventDef> {deliveryEvent, scoutDeliveryEvent, shinobiDeliveryEvent,
                    heavyMutantDeliveryEvent, watcherMutantDeliveryEvent, shooterMutantDeliveryEvent };

                foreach (GeoscapeEventDef eventDef in allDeliveryEvents)
                {
                    eventDef.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                    eventDef.GeoscapeEventData.Description[0].General = projectOsirisDescription;

                }

                OsirisEventTemplate = deadTemplateDef;
                /* deliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                 scoutDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                 shinobiDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                 heavyMutantDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                 watcherMutantDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);
                 shooterMutantDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(deadTemplateDef);*/
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static TacCharacterDef OsirisEventTemplate = null;

        public static void RunProjectOsiris(GeoLevelController controller)
        {
            try
            {
                if (TFTVRevenantResearch.ProjectOsirisStats.Count > 0 && (CheckLabs(controller)[0] || CheckLabs(controller)[1]))
                {
                    TFTVLogger.Always("ProjectOsirisStats has people in it and player has a bionic or a mutation lab");
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
                                if (numMissions > 3)
                                {
                                    baseScore = 25;
                                }
                                int score = baseScore + (numMissions + enemiesKilled + soldierLevel);
                                TFTVLogger.Always("#of Missions: " + numMissions + ". #enemies killed: " + enemiesKilled + ". level: " + soldierLevel + ". The score is " + score);
                                allProjectOsirisCandidates.Add(geoTacUnitId, score);
                            }
                        }
                    }

                    if (allProjectOsirisCandidates.Count > 0) //it can happen that this list is empty because savescumming
                    {
                        List<int> orderedList = allProjectOsirisCandidates.Values.OrderByDescending(x => x).ToList();

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(0, 100);

                        int rollTo = (orderedList.Count - 1) * 10 + orderedList[0];
                        if (rollTo > 90)
                        {
                            rollTo = 90;
                        }

                        TFTVLogger.Always("The roll is " + roll + " and the rollTo is " + rollTo);

                        if (roll <= rollTo)
                        {
                            foreach (GeoTacUnitId id in allProjectOsirisCandidates.Keys)
                            {
                                if (allProjectOsirisCandidates[id] == orderedList.First())
                                {
                                    IdProjectOsirisCandidate = id;

                                }
                            }
                            GeoCharacter geoCharacterCloneFromDead = controller.DeadSoldiers[IdProjectOsirisCandidate].SpawnAsCharacter();
                            TacCharacterDef deadTemplateDef = geoCharacterCloneFromDead.TemplateDef;
                            string name = geoCharacterCloneFromDead.DisplayName;
                            LocalizedTextBind projectOsirisDescription = new LocalizedTextBind(CreateDescriptionForEvent(controller, controller.DeadSoldiers[IdProjectOsirisCandidate]), true);

                            GeoscapeEventDef deliveryEvent = controller.EventSystem.GetEventByID(RoboCopDeliveryEvent);

                            deadTemplateDef.Data.Strength = TFTVRevenantResearch.ProjectOsirisStats[IdProjectOsirisCandidate][0];
                            deadTemplateDef.Data.Will = TFTVRevenantResearch.ProjectOsirisStats[IdProjectOsirisCandidate][1];
                            deadTemplateDef.Data.Speed = TFTVRevenantResearch.ProjectOsirisStats[IdProjectOsirisCandidate][2];


                            GeoSite phoenixBase = controller.PhoenixFaction.Bases.First().Site;
                            GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, controller.PhoenixFaction);
                            GeoscapeEventDef projectOsirisEvent = controller.EventSystem.GetEventByID(ProjectOsirisEvent);
                            projectOsirisEvent.GeoscapeEventData.Description[0].General = projectOsirisDescription;
                            GeoscapeEventDef roboCopEvent = controller.EventSystem.GetEventByID(RobocopEvent);
                            roboCopEvent.GeoscapeEventData.Description[0].General = projectOsirisDescription;
                            GeoscapeEventDef fullMutantEvent = controller.EventSystem.GetEventByID(FullMutantEvent);
                            fullMutantEvent.GeoscapeEventData.Description[0].General = projectOsirisDescription;

                            CheckCompletedBionicsAndMutationResearches(controller);

                            if (CheckLabs(controller)[0] && CheckLabs(controller)[1])
                            {
                                TFTVLogger.Always("Player has both labs");

                                if (controller.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef")
                                || controller.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef"))
                                {
                                    TFTVLogger.Always("Player has researched additional Bionic tech");
                                    projectOsirisEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = RobocopEvent;

                                }
                                else
                                {
                                    TFTVLogger.Always("Player has only basic Bionic tech");
                                    projectOsirisEvent.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = RoboCopDeliveryEvent;
                                }
                                if (controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech2_ResearchDef")
                                || controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech3_ResearchDef"))
                                {
                                    TFTVLogger.Always("Player has researched additional Mutation tech");
                                    projectOsirisEvent.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = FullMutantEvent;

                                }
                                else
                                {
                                    TFTVLogger.Always("Player has only basic Mutation tech");
                                    projectOsirisEvent.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = HeavyMutantDeliveryEvent;
                                }

                                controller.EventSystem.TriggerGeoscapeEvent(ProjectOsirisEvent, context);
                            }
                            else if (CheckLabs(controller)[0] && !CheckLabs(controller)[1])
                            {
                                TFTVLogger.Always("Player has only Bionics lab");
                                if (controller.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef")
                                || controller.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef"))
                                {
                                    TFTVLogger.Always("Player has researched additional Bionic tech");
                                    controller.EventSystem.TriggerGeoscapeEvent(RobocopEvent, context);
                                }
                                else
                                {
                                    TFTVLogger.Always("Player has only basic Bionic tech");
                                    controller.EventSystem.TriggerGeoscapeEvent(RoboCopDeliveryEvent, context);
                                }
                            }
                            else if (!CheckLabs(controller)[0] && CheckLabs(controller)[1])
                            {
                                TFTVLogger.Always("Player has only Mutation lab");
                                if (controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech2_ResearchDef")
                                    || controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech3_ResearchDef"))
                                {
                                    TFTVLogger.Always("Player has researched additional Mutation tech");
                                    controller.EventSystem.TriggerGeoscapeEvent(FullMutantEvent, context);
                                }
                                else
                                {
                                    TFTVLogger.Always("Player has only basic Mutation tech");
                                    controller.EventSystem.TriggerGeoscapeEvent(HeavyMutantDeliveryEvent, context);
                                }
                            }
                        }
                        else//this is in case the roll is not made, so that list is cleared for when Project Osiris is run again 
                        {
                            TFTVRevenantResearch.ProjectOsirisStats.Clear();
                        }
                    }
                    else //this is in case a GeoTacUnitId is present in the stats list, but is not actually dead, because savescumming 
                    {
                        TFTVRevenantResearch.ProjectOsirisStats.Clear();
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        [HarmonyPatch(typeof(GeoscapeEventSystem), "TriggerGeoscapeEvent")]

        public static class GeoscapeEventSystem_TriggerGeoscapeEvent_ProjectOsiris_patch
        {
            public static void Prefix(string eventId, GeoscapeEventSystem __instance)
            {
                try
                {
                    TFTVLogger.Always("TriggerGeoscapeEvent prefix triggered for event " + eventId);

                    if (eventId == RoboCopDeliveryEvent || eventId == ScoutDeliveryEvent || eventId == ShinobiDeliveryEvent ||
                        eventId == HeavyMutantDeliveryEvent || eventId == WatcherMutantDeliveryEvent || eventId == ShooterMutantDeliveryEvent)
                    {

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        CompleteProjectOsiris(IdProjectOsirisCandidate, controller, __instance.GetEventByID(eventId));

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            public static void Postfix(string eventId)
            {
                try
                {

                    if (eventId == RoboCopDeliveryEvent || eventId == ScoutDeliveryEvent || eventId == ShinobiDeliveryEvent ||
                        eventId == HeavyMutantDeliveryEvent || eventId == WatcherMutantDeliveryEvent || eventId == ShooterMutantDeliveryEvent)
                    {

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));
                        GeoCharacter geoCharacterCloneFromDead = controller.DeadSoldiers[IdProjectOsirisCandidate].SpawnAsCharacter();
                        TacCharacterDef deadTemplateDef = geoCharacterCloneFromDead.TemplateDef;


                        GeoTacUnitId geoTacUnitNewCharacter = NewBodyGeoID;
                        TFTVLogger.Always($"geoCharacterCloneFromDead.Id is {geoCharacterCloneFromDead.Id}, compared to last living soldier id: {statisticsManager.CurrentGameStats.LivingSoldiers.Last().Key}");
                        // statisticsManager.CurrentGameStats.LivingSoldiers.Last().Key;


                        if (statisticsManager.CurrentGameStats.LivingSoldiers.ContainsKey(geoTacUnitNewCharacter))
                        {
                          //  TFTVLogger.Always("tis true");
                            statisticsManager.CurrentGameStats.LivingSoldiers[geoTacUnitNewCharacter] = statisticsManager.CurrentGameStats.DeadSoldiers[IdProjectOsirisCandidate];
                        }
                        else
                        {
                            statisticsManager.CurrentGameStats.LivingSoldiers.Add(geoTacUnitNewCharacter, statisticsManager.CurrentGameStats.DeadSoldiers[IdProjectOsirisCandidate]);
                        }

                        if (controller.DeadSoldiers.ContainsKey(IdProjectOsirisCandidate))
                        {
                            controller.DeadSoldiers.Remove(IdProjectOsirisCandidate);

                            statisticsManager.CurrentGameStats.DeadSoldiers.Remove(IdProjectOsirisCandidate);
                        }


                        if (TFTVRevenant.DeadSoldiersDelirium.Keys.Contains(IdProjectOsirisCandidate))
                        {
                            TFTVRevenant.DeadSoldiersDelirium.Remove(IdProjectOsirisCandidate);
                        }




                        int level = (int)(geoCharacterCloneFromDead.Progression?.LevelProgression?.Level);


                        GeoCharacter returned = controller.PhoenixFaction.Soldiers.FirstOrDefault(s => s.Id.Equals(NewBodyGeoID));

                        TFTVLogger.Always($"The returned is {returned.DisplayName}");

                        returned.Identity.CopyFrom(geoCharacterCloneFromDead.Identity, PhoenixPoint.Common.Entities.Characters.CharacterIdentity.EmptyReplaceOperation.Default);
                        returned.LevelProgression.SetLevel(level);
                        returned.Progression.SkillPoints = 0;
                        returned.Fatigue.Stamina.SetToMin();

                        TFTVLogger.Always($"The clone is level {geoCharacterCloneFromDead.LevelProgression.Level}");
                      //  geoCharacterCloneFromDead.LevelProgression.SetLevel(geoCharacterCloneFromDead.LevelProgression.Level);
                        // TFTVLogger.Always("The clone has a secondary class " + geoCharacterCloneFromDead.Progression.SecondarySpecDef.name);

                        if (geoCharacterCloneFromDead.Progression.SecondarySpecDef != null)
                        {
                            returned.Progression.AddSecondaryClass(geoCharacterCloneFromDead.Progression.SecondarySpecDef);
                        }

                        deadTemplateDef.Data = SaveTemplateData;
                        bCSettings.SpecialCharacterPersonalSkills.Clear();
                        IdProjectOsirisCandidate = new GeoTacUnitId();
                        SaveTemplateData = new TacCharacterData();
                        TFTVRevenantResearch.ProjectOsirisStats.Clear();
                        NewBodyGeoID = new GeoTacUnitId();

                        GeoscapeEventDef deliveryEvent = controller.EventSystem.GetEventByID(RoboCopDeliveryEvent);
                        GeoscapeEventDef scoutDeliveryEvent = controller.EventSystem.GetEventByID(ScoutDeliveryEvent);
                        GeoscapeEventDef shinobiDeliveryEvent = controller.EventSystem.GetEventByID(ShinobiDeliveryEvent);
                        GeoscapeEventDef heavyMutantDeliveryEvent = controller.EventSystem.GetEventByID(HeavyMutantDeliveryEvent);
                        GeoscapeEventDef watcherMutantDeliveryEvent = controller.EventSystem.GetEventByID(WatcherMutantDeliveryEvent);
                        GeoscapeEventDef shooterMutantDeliveryEvent = controller.EventSystem.GetEventByID(ShooterMutantDeliveryEvent);

                        List<GeoscapeEventDef> allDeliveryEvents = new List<GeoscapeEventDef> {deliveryEvent, scoutDeliveryEvent, shinobiDeliveryEvent,
                    heavyMutantDeliveryEvent, watcherMutantDeliveryEvent, shooterMutantDeliveryEvent };

                        foreach (GeoscapeEventDef eventDef in allDeliveryEvents)
                        {
                            eventDef.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Remove(deadTemplateDef);

                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static GeoTacUnitId NewBodyGeoID = new GeoTacUnitId();

        [HarmonyPatch(typeof(GeoLevelController), "CreateCharacterFromDescriptor")]

        public static class GeoLevelController_CreateCharacterFromDescriptor_ProjectOsiris_patch
        {
            public static void Postfix(GeoLevelController __instance, GeoUnitDescriptor unit, GeoCharacter __result)
            {
                try
                {
                    if (OsirisEventTemplate != null)
                    {
                        TFTVLogger.Always($"{unit.Identity.Name} {OsirisEventTemplate.Data.Name}");

                        if (unit.Identity.Name == OsirisEventTemplate.Data.Name)
                        {
                            NewBodyGeoID = __result.Id;
                            OsirisEventTemplate = null;
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
}
