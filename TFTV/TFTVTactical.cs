using Base;
using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
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
        public List<int> charactersWithBrokenLimbs; // = TFTVStamina.charactersWithBrokenLimbs;
        public bool UmbraResearched;// = TFTVUmbra.UmbraResearched;
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
        public bool [] VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck;
        // public bool PhoenixWonInfestationMissionTacticalSaveData = TFTVInfestation.PhoenixWon;
    }

    /// <summary>
    /// Represents a mod instance specific for Tactical game.
    /// Each time Tactical level is loaded, new mod's ModTactical is created. 
    /// </summary>
    public class TFTVTactical : ModTactical
    {
        /// <summary>
        /// Called when Tactical starts.
        /// </summary>
        public override void OnTacticalStart()
        {

            /// Tactical level controller is accessible at any time.
            TacticalLevelController tacController = Controller;
            //  TFTVDefsRequiringReinjection.InjectDefsRequiringReinjection();
            /// ModMain is accesible at any time
            // TFTVMain main = (TFTVMain)Main;
            //TFTV give Dtony's Delirium Perks
            //  TFTVDelirium.DeliriumPerksOnTactical(tacController);
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
          //  TFTVLogger.Always("# of already shown tactical hints is " + TFTVTutorialAndStory.TacticalHintsToShow.Count);
           // TFTVTutorialAndStory.RemoveAlreadyShownTacticalHints();
            TFTVHumanEnemies.RollCount = 0;
            TFTVRevenant.ModifyRevenantResistanceAbility(Controller);
            TFTVRevenant.CheckForNotDeadSoldiers(tacController);
            TFTVRevenant.RevenantCheckAndSpawn(Controller);
            TFTVRevenant.ImplementVO19(Controller);
            TFTVLogger.Always("Tactical start completed");
            TFTVLogger.Always("Difficulty level is " + tacController.Difficulty.Order);
        }

        /// <summary>
        /// Called when Tactical ends.
        /// </summary>
        public override void OnTacticalEnd()
        {
            //   TFTVRevenant.CheckForNotDeadSoldiers(Controller);
              TFTVLogger.Always("OnTacticalEnd check");
            TFTVRevenant.revenantCanSpawn=false;
          //  TFTVInfestation.CheckIfPhoenixLost(Controller);
            TFTVRevenantResearch.CheckRevenantCapturedOrKilled(Controller);
         //   TFTVVoidOmens.GameOverMethodInvoked = false;
            

            base.OnTacticalEnd();

          /*  if (TFTVRevenant.revenantSpawned)
            {
                TFTVLogger.Always("revenant Spawned is true and " + TFTVRevenant.timeLastRevenantSpawned.DateTime);
                TFTVRevenant.timeLastRevenantSpawned = TFTVRevenant.timeOfMissionStart;
                TFTVLogger.Always("revenant Spawned is true and " + TFTVRevenant.timeLastRevenantSpawned.DateTime);
            }*/
            /*  if (TFTVRevenant.DeadSoldiersDelirium.Count != 0 && TFTVRevenant.DeadSoldiersDelirium.Count > TFTVRevenant.GeoDeadSoldiersDelirium.Count)
              {
                  TFTVRevenant.DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
              }*/

            
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
                TFTVStamina.charactersWithBrokenLimbs = data.charactersWithBrokenLimbs;
                TFTVVoidOmens.VoidOmensCheck = data.VoidOmensCheck;
                TFTVUmbra.UmbraResearched = data.UmbraResearched;
                TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;
                //  TFTVRevenant.timeRevenantLastSeen = data.timeRevenantLastSeenSaveData;
                // TFTVRevenant.timeLastRevenantSpawned = data.timeLastRevenantSpawned;
                TFTVRevenant.revenantSpawned = data.revenantSpawned;
                TFTVRevenant.revenantSpecialResistance = data.revenantSpecialResistance;
                TFTVRevenant.revenantCanSpawn = data.revenantCanSpawnSaveDate;
                TFTVRevenantResearch.ProjectOsirisStats = data.ProjectOsirisStatsTacticalSaveData;
                TFTVRevenantResearch.ProjectOsiris = data.ProjectOrisisCompletedSaveData;
               // TFTVHumanEnemies.difficultyLevel = data.difficultyLevelForTacticalSaveData;
                TFTVHumanEnemies.HumanEnemiesAndTactics = data.humanEnemiesLeaderTacticsSaveData;
                TFTVInfestationStory.HavenPopulation = data.infestedHavenPopulationSaveData;
                TFTVInfestationStory.OriginalOwner = data.infestedHavenOriginalOwnerSaveData;
                TFTVRevenant.revenantID = data.RevenantId;
             //   TFTVTutorialAndStory.TacticalHintsToShow = data.TacticalHintsToShow;

                // TFTVInfestation.PhoenixWon = data.PhoenixWonInfestationMissionTacticalSaveData;
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
                charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs,
                VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck,
                UmbraResearched = TFTVUmbra.UmbraResearched,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
                //   timeRevenantLastSeenSaveData = TFTVRevenant.timeRevenantLastSeen,
                revenantSpawned = TFTVRevenant.revenantSpawned,
                revenantSpecialResistance = TFTVRevenant.revenantSpecialResistance,
                revenantCanSpawnSaveDate = TFTVRevenant.revenantCanSpawn,
                ProjectOsirisStatsTacticalSaveData = TFTVRevenantResearch.ProjectOsirisStats,
                ProjectOrisisCompletedSaveData = TFTVRevenantResearch.ProjectOsiris,
                humanEnemiesLeaderTacticsSaveData = TFTVHumanEnemies.HumanEnemiesAndTactics,
                infestedHavenPopulationSaveData = TFTVInfestationStory.HavenPopulation,
                infestedHavenOriginalOwnerSaveData = TFTVInfestationStory.OriginalOwner,
                RevenantId = TFTVRevenant.revenantID,
            
            };
            
        }
        /// <summary>
        /// Called when new turn starts in tactical. At this point all factions must play in their order.
        /// </summary>
        /// <param name="turnNumber">Current turn number</param>
        public override void OnNewTurn(int turnNumber)
        {
            TFTVRevenant.revenantSpecialResistance.Clear();
            TFTVUmbra.SpawnUmbra(Controller);
            if (turnNumber == 0)
            { TFTVHumanEnemies.AssignHumanEnemiesTags(Controller); }
            TFTVHumanEnemies.ChampRecoverWPAura(Controller);           
            TFTVHumanEnemies.ApplyTactic(Controller);
           


        }
    }
}