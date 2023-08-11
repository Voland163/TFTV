using Base.Core;
using Base.Serialization.General;
using EnviroSamples;
using Epic.OnlineServices;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using UnityEngine;

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

    }

    /// <summary>
    /// Represents a mod instance specific for Tactical game.
    /// Each time Tactical level is loaded, new mod's ModTactical is created. 
    /// </summary>
    public class TFTVTactical : ModTactical
    {
        public static bool TurnZeroMethodsExecuted = false;
        /// <summary>
        /// Called when Tactical starts.
        /// </summary>
        public override void OnTacticalStart()
        {

            /// Tactical level controller is accessible at any time.
            TacticalLevelController tacController = Controller;
            TFTVConfig config = TFTVMain.Main.Config;
            TFTVReleaseOnly.ConvertDifficulty(null, tacController);
            if (config.AnimateWhileShooting)
            {
                TFTVLogger.Always($"Flinching should be on");

                Controller.FireTargetTimeScale = 1f;
              //  Controller.FirstPersonShootingTimeScale = 0.2f;
            }
            else 
            {
                Controller.FireTargetTimeScale = 0.1f;
              //  Controller.FirstPersonShootingTimeScale = 0.1f;

            }
            /// ModMain is accesible at any time

            TFTVLogger.Always("Tactical Started");
            TFTVLogger.Always("The count of tactics in play is " + TFTVHumanEnemies.HumanEnemiesAndTactics.Count);
            TFTVLogger.Always("VO3 Active " + TFTVVoidOmens.VoidOmensCheck[3]);
            TFTVLogger.Always("VO5 Active " + TFTVVoidOmens.VoidOmensCheck[5]);
            TFTVLogger.Always("VO7 Active " + TFTVVoidOmens.VoidOmensCheck[7]);
            TFTVLogger.Always("VO10 Active " + TFTVVoidOmens.VoidOmensCheck[10]);
            TFTVLogger.Always("VO15 Active " + TFTVVoidOmens.VoidOmensCheck[15]);
            TFTVLogger.Always("VO16 Active " + TFTVVoidOmens.VoidOmensCheck[16]);
            TFTVLogger.Always("VO19 Active " + TFTVVoidOmens.VoidOmensCheck[19]);
            TFTVLogger.Always("Project Osiris researched " + TFTVRevenantResearch.ProjectOsiris);

            TFTVHumanEnemies.RollCount = 0;
            TFTVAncients.CheckCyclopsDefense();
            TFTVLogger.Always("Tactical start completed");
            TFTVLogger.Always($"Difficulty level is {tacController.Difficulty.Order} and treated as {TFTVReleaseOnly.DifficultyOrderConverter(tacController.Difficulty.Order)}");
         //   TFTVLogger.Always("LOTA rework active in tactical is " + TFTVAncients.LOTAReworkActive);
            TFTVLogger.Always($"Deployed Aircraft capture capacity is {TFTVCapturePandorans.AircraftCaptureCapacity}");
            TFTVLogger.Always($"Available containment is {TFTVCapturePandorans.ContainmentSpaceAvailable}");
            TFTVLogger.Always($"Containment facility is present {TFTVCapturePandorans.ContainmentFacilityPresent}");
            TFTVLogger.Always($"Scylla Capture Module is present {TFTVCapturePandorans.ScyllaCaptureModulePresent}");
            TFTVAncientsGeo.CheckResearchStateOnGeoscapeEndAndOnTacticalStart(null);

            TFTVBaseDefenseTactical.CheckConsoleSituation(Controller);
            //  TFTVBaseDefenseTactical.InteractionPointPlacement();
            //   TFTVBaseDefenseTactical.CheckIfConsoleActivated(Controller);
            TFTVSpecialDifficulties.CheckForSpecialDifficulties();
          //  TFTVBetterEnemies.ImplementBetterEnemies();
            TFTVRevenant.CheckIfRevenantPresent(Controller);

            if (config.disableSavingOnTactical)
            {
                GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().GameController.SaveManager.IsSaveEnabled = false;
            }

            TFTVNewGameOptions.Change_Crossbows();

            //   TFTVBaseDefenseTactical.OjectivesDebbuger(Controller);
            // TFTVBaseDefenseTactical.RevealAllSpawns(Controller);

            // TFTVBaseDefenseTactical.CheckingPortrait(Controller);

            //  TFTVBaseDefenseTactical.ModifyObjectives(Controller);

        }

        /// <summary>
        /// Called when Tactical ends.
        /// </summary>
        public override void OnTacticalEnd()
        {

            TFTVLogger.Always("OnTacticalEnd check");
            TFTVRevenant.revenantCanSpawn = false;

            TFTVRevenantResearch.CheckRevenantCapturedOrKilled(Controller);
           // TFTVBaseDefenseTactical.OjectivesDebbuger(Controller);
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
                TFTVTacInstanceData data = (TFTVTacInstanceData)instanceData;
                TFTVStamina.charactersWithDisabledBodyParts = data.charactersWithBrokenLimbs;
                TFTVVoidOmens.VoidOmensCheck = data.VoidOmensCheck;
                TFTVUmbra.TBTVVariable = data.TBTVVariable;
                TFTVUmbra.UmbraResearched = data.UmbraResearched;
                TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;

                TFTVRevenant.revenantSpawned = data.revenantSpawned;
                TFTVRevenant.revenantSpecialResistance = data.revenantSpecialResistance;
                TFTVRevenant.revenantCanSpawn = data.revenantCanSpawnSaveDate;
                TFTVRevenantResearch.ProjectOsirisStats = data.ProjectOsirisStatsTacticalSaveData;
                TFTVRevenantResearch.ProjectOsiris = data.ProjectOrisisCompletedSaveData;

                TFTVHumanEnemies.HumanEnemiesAndTactics = data.humanEnemiesLeaderTacticsSaveData;
                TFTVInfestationStory.HavenPopulation = data.infestedHavenPopulationSaveData;
                TFTVInfestationStory.OriginalOwner = data.infestedHavenOriginalOwnerSaveData;
                TFTVRevenant.revenantID = data.RevenantId;
              //  TFTVAncients.LOTAReworkActive = data.LOTAReworkActiveInTactical;
                TFTVBaseDefenseTactical.AttackProgress = data.BaseDefenseAttackProgress;
                TFTVBaseDefenseTactical.StratToBeAnnounced = data.BaseDefenseStratToBeAnnounced;
                TFTVBaseDefenseTactical.StratToBeImplemented = data.BaseDefenseStratToBeImplemented;  
                TFTVBaseDefenseTactical.UsedStrats = data.StratsAlreadyImplementedAtBD;
                TFTVBaseDefenseTactical.ConsolePositions = data.ConsolePositionsInBaseDefense;
                TFTVAncients.CyclopsMolecularDamageBuff = data.CyclopsMolecularTargeting;
                TFTVCapturePandorans.AircraftCaptureCapacity = data.DeployedAircraftCaptureCapacity;
                TFTVCapturePandorans.ContainmentFacilityPresent = data.ContainmentFacilityPresent;
                TFTVCapturePandorans.ScyllaCaptureModulePresent = data.ScyllaCaptureModule;
                TFTVBaseDefenseTactical.ModifyObjectives(Controller.TacMission.MissionData.MissionType);
                TFTVAncients.AutomataResearched = data.AutomataResearched;
                TFTVAncients.AlertedHoplites = data.HopliteKillList;
                TFTVCapturePandorans.ContainmentSpaceAvailable = data.AvailableContainment;
                TFTVNewGameOptions.Update35Check = data.Update35TacticalCheck;
                //Deprecated
              //  TFTVBetaSaveGamesFixes.CheckNewLOTASavegame();

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
                TBTVVariable = TFTVUmbra.TBTVVariable,
                UmbraResearched = TFTVUmbra.UmbraResearched,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
                revenantSpawned = TFTVRevenant.revenantSpawned,
                revenantSpecialResistance = TFTVRevenant.revenantSpecialResistance,
                revenantCanSpawnSaveDate = TFTVRevenant.revenantCanSpawn,
                ProjectOsirisStatsTacticalSaveData = TFTVRevenantResearch.ProjectOsirisStats,
                ProjectOrisisCompletedSaveData = TFTVRevenantResearch.ProjectOsiris,
                humanEnemiesLeaderTacticsSaveData = TFTVHumanEnemies.HumanEnemiesAndTactics,
                infestedHavenPopulationSaveData = TFTVInfestationStory.HavenPopulation,
                infestedHavenOriginalOwnerSaveData = TFTVInfestationStory.OriginalOwner,
                RevenantId = TFTVRevenant.revenantID,

             //   LOTAReworkActiveInTactical = TFTVAncients.LOTAReworkActive,
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

                //   TFTVLogger.Always("AI Actions influenced by Safety consideration during previous turn " + TFTVExperimental.CounterAIActionsInfluencedBySafetyConsideration);
                //   TFTVExperimental.CounterAIActionsInfluencedBySafetyConsideration = 0;


                // MissionDeployment.PlaceInteractionPoint_EXAMPLE(new Vector3(-34.5f, 0.0f, 2.5f));

                if (!Controller.TacMission.MissionData.MissionType.name.Contains("Tutorial"))
                {
                  
                    if (turnNumber == 0 && TFTVHumanEnemies.HumanEnemiesAndTactics.Count == 0)
                    {
                        TFTVHumanEnemies.CheckMissionType(Controller);
                    }

                    if (turnNumber == 0 && TFTVAncients.CheckIfAncientsPresent(Controller))
                    {
                        TFTVAncients.AdjustAncientsOnDeployment(Controller);
                    }

                    if (turnNumber == 0 && !TurnZeroMethodsExecuted)
                    {
                        TFTVLogger.Always("Turn 0 check");
                        TFTVRevenant.ModifyRevenantResistanceAbility(Controller);
                        TFTVRevenant.CheckForNotDeadSoldiers(Controller);
                        TFTVRevenant.RevenantCheckAndSpawn(Controller);
                        TFTVRevenant.ImplementVO19(Controller);
                        TFTVVoidOmens.VO5TurnHostileCivviesFriendly(Controller);
                        TFTVBaseDefenseTactical.GetConsoles();
                        //  TFTVBaseDefenseTactical.ModifyObjectives(Controller.TacMission.MissionData.MissionType);
                        TurnZeroMethodsExecuted = true;
                    }

                    TFTVRevenant.revenantSpecialResistance.Clear();
                    TFTVUmbra.SpawnUmbra(Controller);
                    TFTVHumanEnemies.ChampRecoverWPAura(Controller);
                   // TFTVHumanEnemies.ApplyTactic(Controller);


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }
    }
}