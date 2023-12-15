using Base.Core;
using Base.Serialization.General;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;

namespace TFTV
{

    /// <summary>
    /// Mod's custom save data for tactical.
    /// </summary>
    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public class TFTVTacInstanceData
    {
        // public int ExampleData;
        // Dictionary to transfer the characters geoscape stamina to tactical level by actor ID
        public Dictionary<int, List<string>> charactersWithBrokenLimbs = TFTVStamina.charactersWithDisabledBodyParts;
        public int TBTVVariable;
        public bool UmbraResearched;
        public Dictionary<int, int> DeadSoldiersDelirium;// = TFTVRevenant.DeadSoldiersDelirium;
                                                         //    public TimeUnit timeRevenantLastSeenSaveData = TFTVRevenant.timeRevenantLastSeen;
                                                         //    public TimeSpan timeLastRevenantSpawned = TFTVRevenant.timeLastRevenantSpawned;
        public List<string> revenantSpecialResistance;// = TFTVRevenant.revenantSpecialResistance;
        public bool revenantSpawned;// = TFTVRevenant.revenantSpawned;
        public bool revenantCanSpawnSaveDate;// = TFTVRevenant.revenantCanSpawn;
        public Dictionary<string, int> humanEnemiesLeaderTacticsSaveData;// = TFTVHumanEnemies.HumanEnemiesAndTactics;
        public int difficultyLevelForTacticalSaveData;// = TFTVHumanEnemies.difficultyLevel;
        public int infestedHavenPopulationSaveData;// = TFTVInfestationStory.HavenPopulation;
        public string infestedHavenOriginalOwnerSaveData;// = TFTVInfestationStory.OriginalOwner;
        public Dictionary<int, int[]> ProjectOsirisStatsTacticalSaveData;// = TFTVRevenantResearch.ProjectOsirisStats;
        public bool ProjectOrisisCompletedSaveData;// = TFTVRevenantResearch.ProjectOsiris;
        public int RevenantId;
        public bool[] VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck;
        public bool LOTAReworkActiveInTactical;
        public bool TurnZeroMethodsExecuted;
        public bool[] BaseDefenseConsole;
        public float BaseDefenseAttackProgress;
        public int BaseDefenseStratToBeAnnounced;
        public int BaseDefenseStratToBeImplemented;
        public bool[] StratsAlreadyImplementedAtBD;
        public bool AutomataResearched;
        public List<string> HopliteKillList;
        public Dictionary<float, float> ConsolePositionsInBaseDefense;
        public Dictionary<int, int> CyclopsMolecularTargeting;
        public int DeployedAircraftCaptureCapacity;
        public bool ContainmentFacilityPresent;
        public bool ScyllaCaptureModule;
        public int AvailableContainment;
        public bool Update35TacticalCheck;
        public bool StrongerPandoransTactical;
        public bool NerfAncientsWeaponsTactical;

    }

    /// <summary>
    /// Represents a mod instance specific for Tactical game.
    /// Each time Tactical level is loaded, new mod's ModTactical is created. 
    /// </summary>
    public class TFTVTactical : ModTactical
    {
       // private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static void ImplementSpecialMissions(TacticalLevelController controller)
        {
            try
            {
                TFTVBaseDefenseTactical.Map.Consoles.SpawnConsoles.PlaceObjectives(controller);
                TFTVRescueVIPMissions.CheckAndImplementVIPRescueMIssions(controller);
                TFTVPalaceMission.CheckPalaceMission();
                TFTVAncients.CyclopsAbilities.CyclopsResistance.CheckCyclopsDefense();
                TFTVAncientsGeo.AncientsResearch.CheckResearchStateOnGeoscapeEndAndOnTacticalStart(null);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static void ImplementConfigOptions(TacticalLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (config.AnimateWhileShooting)
                {
                    TFTVLogger.Always($"Flinching should be on");

                    controller.FireTargetTimeScale = 1f;
                    //  Controller.FirstPersonShootingTimeScale = 0.2f;
                }
                else
                {
                    controller.FireTargetTimeScale = 0.1f;
                    //  Controller.FirstPersonShootingTimeScale = 0.1f;

                }


                if (config.disableSavingOnTactical)
                {
                    GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().GameController.SaveManager.IsSaveEnabled = false;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void RunChecksForAllMissions(TacticalLevelController controller)
        {
            try
            {
                TFTVHumanEnemies.RollCount = 0;
                TFTVSpecialDifficulties.DefModifying.CheckForSpecialDifficulties();
                TFTVRevenant.Resistance.CheckIfRevenantPresent(controller);
                TFTVUITactical.RemoveDamagePredictionBar();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        private static void RunBetaTestChecks()
        {
            try
            {
                TFTVBetaSaveGamesFixes.CorrrectPhoenixSaveManagerDifficulty();
               // TFTVNewGameOptions.Change_Crossbows();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }



        public static bool TurnZeroMethodsExecuted = false;
        /// <summary>
        /// Called when Tactical starts.
        /// </summary>
        public override void OnTacticalStart()
        {

            /// Tactical level controller is accessible at any time.
            TacticalLevelController tacController = Controller;

            /// ModMain is accesible at any time

            TFTVLogger.Always("OnTacticalStarted");
            ImplementSpecialMissions(tacController);
            ImplementConfigOptions(tacController);
            RunChecksForAllMissions(tacController);
            RunBetaTestChecks();
          //  TFTVTacticalUtils.RevealAllSpawns(tacController);

            TFTVLogger.Always("The count of Human tactics in play is " + TFTVHumanEnemies.HumanEnemiesAndTactics.Count);
            TFTVLogger.Always("VO3 Active " + TFTVVoidOmens.VoidOmensCheck[3]);
            TFTVLogger.Always("VO5 Active " + TFTVVoidOmens.VoidOmensCheck[5]);
            TFTVLogger.Always("VO7 Active " + TFTVVoidOmens.VoidOmensCheck[7]);
            TFTVLogger.Always("VO10 Active " + TFTVVoidOmens.VoidOmensCheck[10]);
            TFTVLogger.Always("VO15 Active " + TFTVVoidOmens.VoidOmensCheck[15]);
            TFTVLogger.Always("VO16 Active " + TFTVVoidOmens.VoidOmensCheck[16]);
            TFTVLogger.Always("VO19 Active " + TFTVVoidOmens.VoidOmensCheck[19]);
            TFTVLogger.Always("Project Osiris researched " + TFTVRevenantResearch.ProjectOsiris);

            TFTVLogger.Always($"Deployed Aircraft capture capacity is {TFTVCapturePandorans.AircraftCaptureCapacity}");
            TFTVLogger.Always($"Available containment is {TFTVCapturePandorans.ContainmentSpaceAvailable}");
            TFTVLogger.Always($"Containment facility is present {TFTVCapturePandorans.ContainmentFacilityPresent}");
            TFTVLogger.Always($"Scylla Capture Module is present {TFTVCapturePandorans.ScyllaCaptureModulePresent}");
            TFTVLogger.Always($"Stronger Pandorans {TFTVNewGameOptions.StrongerPandoransSetting}");
            TFTVLogger.Always($"Nerf ancients weapons {TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting}");
            TFTVLogger.Always($"Mission: {Controller.TacMission.MissionData.MissionType.name}");
            TFTVLogger.Always($"Difficulty level is {tacController.Difficulty.name} and treated as {TFTVReleaseOnly.DifficultyOrderConverter(tacController.Difficulty.Order)} after TFTV conversion.");
            TFTVLogger.Always("Tactical start completed");
        }

        /// <summary>
        /// Called when Tactical ends.
        /// </summary>
        public override void OnTacticalEnd()
        {

            TFTVLogger.Always("OnTacticalEnd check");
            TFTVRevenant.revenantCanSpawn = false;
            TFTVRevenantResearch.CheckRevenantCapturedOrKilled(Controller);

            base.OnTacticalEnd();

        }

        /// <summary>
        /// Called when Tactical save is being process. At this point level is already created, but TacticalStart is not called.
        /// </summary>
        /// <param name="data">Instance data serialized for this mod. Cannot be null.</param>
        public override void ProcessTacticalInstanceData(object instanceData)
        {
            try
            {

                TFTVLogger.Always("Tactical save is being processed");

                TFTVCommonMethods.ClearInternalVariables();
                TFTVDefsWithConfigDependency.StrongerPandorans.ImplementStrongerPandorans();
                TFTVTacInstanceData data = (TFTVTacInstanceData)instanceData;
                TFTVStamina.charactersWithDisabledBodyParts = data.charactersWithBrokenLimbs;
                TFTVVoidOmens.VoidOmensCheck = data.VoidOmensCheck;
                TFTVTouchedByTheVoid.TBTVVariable = data.TBTVVariable;
                TFTVTouchedByTheVoid.UmbraResearched = data.UmbraResearched;
                TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;

                TFTVRevenant.revenantSpawned = data.revenantSpawned;
                TFTVRevenant.revenantSpecialResistance = data.revenantSpecialResistance;
                TFTVRevenant.revenantCanSpawn = data.revenantCanSpawnSaveDate;
                TFTVRevenantResearch.ProjectOsirisStats = data.ProjectOsirisStatsTacticalSaveData;
                TFTVRevenantResearch.ProjectOsiris = data.ProjectOrisisCompletedSaveData;

                TFTVHumanEnemies.HumanEnemiesAndTactics = data.humanEnemiesLeaderTacticsSaveData;
                TFTVInfestation.HavenPopulation = data.infestedHavenPopulationSaveData;
                TFTVInfestation.OriginalOwner = data.infestedHavenOriginalOwnerSaveData;
                TFTVRevenant.revenantID = data.RevenantId;
                TFTVBaseDefenseTactical.AttackProgress = data.BaseDefenseAttackProgress;
                TFTVBaseDefenseTactical.StratToBeAnnounced = data.BaseDefenseStratToBeAnnounced;
                TFTVBaseDefenseTactical.StratToBeImplemented = data.BaseDefenseStratToBeImplemented;
                TFTVBaseDefenseTactical.UsedStrats = data.StratsAlreadyImplementedAtBD;
                TFTVBaseDefenseTactical.ConsolePositions = data.ConsolePositionsInBaseDefense;
                TFTVAncients.CyclopsMolecularDamageBuff = data.CyclopsMolecularTargeting;
                TFTVCapturePandorans.AircraftCaptureCapacity = data.DeployedAircraftCaptureCapacity;
                TFTVCapturePandorans.ContainmentFacilityPresent = data.ContainmentFacilityPresent;
                TFTVCapturePandorans.ScyllaCaptureModulePresent = data.ScyllaCaptureModule;

                TFTVVoidOmens.ModifyVoidOmenTacticalObjectives(Controller.TacMission.MissionData.MissionType);
                TFTVCapturePandorans.ModifyCapturePandoransTacticalObjectives(Controller.TacMission.MissionData.MissionType);
                TFTVBaseDefenseTactical.Objectives.ModifyBaseDefenseTacticalObjectives(Controller.TacMission.MissionData.MissionType);
                TFTVVoidOmens.ImplementHavenDefendersAlwaysHostile(Controller);
                TFTVAncients.AutomataResearched = data.AutomataResearched;
                TFTVAncients.AlertedHoplites = data.HopliteKillList;
                TFTVCapturePandorans.ContainmentSpaceAvailable = data.AvailableContainment;
                TFTVNewGameOptions.Update35Check = data.Update35TacticalCheck;
                TFTVNewGameOptions.StrongerPandoransSetting = data.StrongerPandoransTactical;
                TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = data.NerfAncientsWeaponsTactical;

                TFTVLogger.Always($"Config settings:" +                  
                    $"\nStrongerPandoransSetting {TFTVNewGameOptions.StrongerPandoransSetting}\nImpossibleWeaponsAdjustmentsSetting: {TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting}");

                TFTVDefsWithConfigDependency.ImplementConfigChoices();

                TurnZeroMethodsExecuted = data.TurnZeroMethodsExecuted;





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        /// <summary>
        /// Called when Tactical save is going to be generated, giving mod option for custom save data.
        /// </summary>
        /// <returns>Object to serialize or null if not used.</returns>
        public override object RecordTacticalInstanceData()
        {
            TFTVLogger.Always("Tactical data will be saved");
            return new TFTVTacInstanceData()
            {
                charactersWithBrokenLimbs = TFTVStamina.charactersWithDisabledBodyParts,
                VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck,
                TBTVVariable = TFTVTouchedByTheVoid.TBTVVariable,
                UmbraResearched = TFTVTouchedByTheVoid.UmbraResearched,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
                revenantSpawned = TFTVRevenant.revenantSpawned,
                revenantSpecialResistance = TFTVRevenant.revenantSpecialResistance,
                revenantCanSpawnSaveDate = TFTVRevenant.revenantCanSpawn,
                ProjectOsirisStatsTacticalSaveData = TFTVRevenantResearch.ProjectOsirisStats,
                ProjectOrisisCompletedSaveData = TFTVRevenantResearch.ProjectOsiris,
                humanEnemiesLeaderTacticsSaveData = TFTVHumanEnemies.HumanEnemiesAndTactics,
                infestedHavenPopulationSaveData = TFTVInfestation.HavenPopulation,
                infestedHavenOriginalOwnerSaveData = TFTVInfestation.OriginalOwner,
                RevenantId = TFTVRevenant.revenantID,
                BaseDefenseAttackProgress = TFTVBaseDefenseTactical.AttackProgress,
                BaseDefenseStratToBeImplemented = TFTVBaseDefenseTactical.StratToBeImplemented,
                BaseDefenseStratToBeAnnounced = TFTVBaseDefenseTactical.StratToBeAnnounced,
                StratsAlreadyImplementedAtBD = TFTVBaseDefenseTactical.UsedStrats,
                ConsolePositionsInBaseDefense = TFTVBaseDefenseTactical.ConsolePositions,
                AutomataResearched = TFTVAncients.AutomataResearched,
                CyclopsMolecularTargeting = TFTVAncients.CyclopsMolecularDamageBuff,
                HopliteKillList = TFTVAncients.AlertedHoplites,
                DeployedAircraftCaptureCapacity = TFTVCapturePandorans.AircraftCaptureCapacity,
                ContainmentFacilityPresent = TFTVCapturePandorans.ContainmentFacilityPresent,
                ScyllaCaptureModule = TFTVCapturePandorans.ScyllaCaptureModulePresent,
                AvailableContainment = TFTVCapturePandorans.ContainmentSpaceAvailable,
                Update35TacticalCheck = TFTVNewGameOptions.Update35Check,
                NerfAncientsWeaponsTactical = TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting,
                StrongerPandoransTactical = TFTVNewGameOptions.StrongerPandoransSetting,

                TurnZeroMethodsExecuted = TurnZeroMethodsExecuted

            };
        }
        /// <summary>
        /// Called when new turn starts in tactical. At this point all factions must play in their order.
        /// </summary>
        /// <param name="turnNumber">Current turn number</param>
        public override void OnNewTurn(int turnNumber)
        {
            try
            {
                TFTVLogger.Always("The turn is " + turnNumber);

                if (!Controller.TacMission.MissionData.MissionType.name.Contains("Tutorial"))
                {
                    //This is to ensure correct functionality of parapsychosis; otherwise wild faction turns out of sync for status effects
                    if (Controller.CurrentFaction == Controller.GetTacticalFaction(Controller.TacticalLevelControllerDef.WildBeastFaction)
                        && turnNumber < Controller.GetTacticalFaction(TacMissionParticipant.Player).TurnNumber)
                    {
                        turnNumber = Controller.GetTacticalFaction(TacMissionParticipant.Player).TurnNumber;
                        Controller.CurrentFaction.TurnNumber = turnNumber;
                    }

                    if (turnNumber == 0 && TFTVHumanEnemies.HumanEnemiesAndTactics.Count == 0)
                    {
                        TFTVHumanEnemies.CheckMissionType(Controller);
                    }

                    if (turnNumber == 0 && !TurnZeroMethodsExecuted)
                    {
                        TFTVLogger.Always("Turn 0 check");
                        if (TFTVAncients.CheckIfAncientsPresent(Controller))
                        {
                            TFTVAncients.AncientDeployment.AdjustAncientsOnDeployment(Controller);
                        }

                        TFTVRevenant.Resistance.ModifyRevenantResistanceAbility(Controller);
                        TFTVRevenant.PrespawnChecks.CheckForNotDeadSoldiers(Controller);
                        TFTVRevenant.Spawning.RevenantCheckAndSpawn(Controller);
                        TFTVRevenant.Resistance.ImplementVO19(Controller);
                        TFTVVoidOmens.VO5TurnHostileCivviesFriendly(Controller);
                        TFTVBaseDefenseTactical.Map.Consoles.GetConsoles();

                        //  TFTVBaseDefenseTactical.ModifyObjectives(Controller.TacMission.MissionData.MissionType);
                        TurnZeroMethodsExecuted = true;
                    }

                    TFTVRevenant.revenantSpecialResistance.Clear();
                    TFTVTouchedByTheVoid.Umbra.UmbraTactical.SpawnUmbra(Controller);
                    TFTVHumanEnemies.ChampRecoverWPAura(Controller);
                    TFTVSpecialDifficulties.CounterSpawned = 0;


                }
                else
                {
                    TFTVLogger.Always($"Playing tutorial mission");


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }
    }
}