using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{

    /// <summary>
    /// Mod's custom save data for geoscape.
    /// </summary>
    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]

    public class TFTVGSInstanceData
    {
        public List<int> charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs;
        public List<int> targetsForBehemoth = TFTVAirCombat.targetsForBehemoth;
        public Dictionary<int, List<int>> flyersAndHavens = TFTVAirCombat.flyersAndHavens;
        public bool checkHammerfall = TFTVAirCombat.checkHammerfall;
        public Dictionary<int, int> DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
        public List<int> behemothScenicRoute = TFTVAirCombat.behemothScenicRoute;
        public int behemothTarget = TFTVAirCombat.behemothTarget;
        public int behemothWaitHours = TFTVAirCombat.behemothWaitHours;
        public int timeRevenantLasteSeenSaveData = TFTVRevenant.daysRevenantLastSeen;
        public int infestedHavenPopulationSaveData = TFTVInfestationStory.HavenPopulation;
        public string infestedHavenOriginalOwnerSaveData = TFTVInfestationStory.OriginalOwner;
        public Dictionary<int, int[]> ProjectOsirisStatsSaveData = TFTVRevenantResearch.ProjectOsirisStats;
        public bool[] VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck;



     //   public string PhoenixBaseUnderAttack = TFTVExperimental.PhoenixBaseUnderAttack;
    //    public PhoenixBaseAttacker baseAttacker = TFTVExperimental.phoenixBaseAttacker;
      //  public PPFactionDef factionAttackingPheonixBase = TFTVExperimental.FactionAttackingPhoenixBase;
        // public List<string> TacticalHintsToShow = TFTVTutorialAndStory.TacticalHintsToShow;
    }

    /// <summary>
    /// Represents a mod instance specific for Geoscape game.
    /// Each time Geoscape level is loaded, new mod's ModGeoscape is created.
    /// </summary>
    public class TFTVGeoscape : ModGeoscape
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        /// <summary>
        /// Called when Geoscape starts.
        /// </summary>
        public override void OnGeoscapeStart()
        {

            /// Geoscape level controller is accessible at any time.
            GeoLevelController gsController = Controller;
            /// ModMain is accesible at any time
            DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [PsychicWard_AbilityDef]").Origin.Range = 10; //Fix Dtony thing
           // TFTVBetaSaveGamesFixes.SpecialFixForTesting(gsController);
            TFTVBetaSaveGamesFixes.CheckSaveGameEventChoices(gsController);
            TFTVBetaSaveGamesFixes.CheckUmbraResearchVariable(gsController);
            TFTVCommonMethods.CheckGeoUIfunctionality(gsController); 
            TFTVNewPXCharacters.PlayIntro(gsController);
            TFTVVoidOmens.CheckVoidOmensBeforeImplementing(gsController);
            TFTVVoidOmens.ImplementVoidOmens(gsController);
            TFTVUmbra.CheckForUmbraResearch(gsController);
            TFTVUmbra.SetUmbraEvolution(gsController);
            TFTVThirdAct.SetBehemothOnRampageMod(gsController);
            TFTVStamina.CheckBrokenLimbs(gsController.PhoenixFaction.Soldiers.ToList());
            TFTVRevenant.UpdateRevenantTimer(gsController);
            if (TFTVRevenant.revenantID != 0 && TFTVRevenant.DeadSoldiersDelirium.ContainsKey(TFTVRevenant.revenantID))
            {
                TFTVRevenant.DeadSoldiersDelirium[TFTVRevenant.revenantID] += 1;
            }

            TFTVRevenantResearch.CheckRevenantResearchRequirements(Controller);
            TFTVProjectOsiris.RunProjectOsiris(gsController);  
            Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            TFTVLogger.Always("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            TFTVBetaSaveGamesFixes.CheckNewLOTA(gsController);
            TFTVAncients.AncientsOnGeoscapeStartChecks(gsController);
            


            // TFTVExperimental.ClearAndCreateBaseDefenseMission(gsController);
            //  TFTVLogger.Always("Revenants destroyed " + gsController.EventSystem.GetVariable("RevenantsDestroyed"));
            //  TFTVLogger.Always("Revenant captured " + gsController.EventSystem.GetVariable("RevenantCapturedVariable"));
        }
        /// <summary>
        /// Called when Geoscape ends.
        /// </summary>
        public override void OnGeoscapeEnd()
        {
            GeoLevelController gsController = Controller;

            TFTVUmbra.CheckForUmbraResearch(gsController);
            TFTVUmbra.SetUmbraEvolution(gsController);
            TFTVVoidOmens.CheckForVoidOmensRequiringTacticalPatching(gsController);
            TFTVUI.hookToCharacter = null;
            TFTVRevenant.CheckRevenantTime(gsController);
            TFTVRevenantResearch.CheckProjectOsiris(gsController);
            TFTVDiplomacyPenalties.VoidOmensImplemented = false;
            TFTVAncients.CheckResearchStateOnGeoscapeEnd(gsController);

        }

        /// <summary>
        /// Called when Geoscape save is going to be generated, giving mod option for custom save data.
        /// </summary>
        /// <returns>Object to serialize or null if not used.</returns>
        public override object RecordGeoscapeInstanceData()
        {
            TFTVLogger.Always("Geoscape data will be saved");
            TFTVRevenant.UpdateRevenantTimer(Controller);
            return new TFTVGSInstanceData()
            {
                charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs,
                targetsForBehemoth = TFTVAirCombat.targetsForBehemoth,
                flyersAndHavens = TFTVAirCombat.flyersAndHavens,
                checkHammerfall = TFTVAirCombat.checkHammerfall,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
                behemothScenicRoute = TFTVAirCombat.behemothScenicRoute,
                behemothTarget = TFTVAirCombat.behemothTarget,
                behemothWaitHours = TFTVAirCombat.behemothWaitHours,
                timeRevenantLasteSeenSaveData = TFTVRevenant.daysRevenantLastSeen,
                infestedHavenOriginalOwnerSaveData = TFTVInfestationStory.OriginalOwner,
                infestedHavenPopulationSaveData = TFTVInfestationStory.HavenPopulation,
                ProjectOsirisStatsSaveData = TFTVRevenantResearch.ProjectOsirisStats,
                VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck,
           
                //  PhoenixBaseUnderAttack = TFTVExperimental.PhoenixBaseUnderAttack,
               // baseAttacker = TFTVExperimental.phoenixBaseAttacker
              //  factionAttackingPheonixBase = TFTVExperimental.FactionAttackingPhoenixBase
            };

        }
        /// <summary>
        /// Called when Geoscape save is being process. At this point level is already created, but GeoscapeStart is not called.
        /// </summary>
        /// <param name="instanceData">Instance data serialized for this mod. Cannot be null.</param>
        public override void ProcessGeoscapeInstanceData(object instanceData)
        {

            TFTVCommonMethods.ClearInternalVariables();
            TFTVGSInstanceData data = (TFTVGSInstanceData)instanceData;
            TFTVStamina.charactersWithBrokenLimbs = data.charactersWithBrokenLimbs;
            TFTVAirCombat.targetsForBehemoth = data.targetsForBehemoth;
            TFTVAirCombat.flyersAndHavens = data.flyersAndHavens;
            TFTVAirCombat.checkHammerfall = data.checkHammerfall;
            TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;
            TFTVRevenant.daysRevenantLastSeen = data.timeRevenantLasteSeenSaveData;
            TFTVAirCombat.behemothScenicRoute = data.behemothScenicRoute;
            TFTVAirCombat.behemothTarget = data.behemothTarget;
            TFTVAirCombat.behemothWaitHours = data.behemothWaitHours;
            TFTVInfestationStory.HavenPopulation = data.infestedHavenPopulationSaveData;
            TFTVInfestationStory.OriginalOwner = data.infestedHavenOriginalOwnerSaveData;
            TFTVRevenantResearch.ProjectOsirisStats = data.ProjectOsirisStatsSaveData;
            TFTVVoidOmens.VoidOmensCheck = data.VoidOmensCheck;
      
            //TFTVExperimental.FactionAttackingPhoenixBase = data.factionAttackingPheonixBase;
            //TFTVExperimental.CheckIfFactionAttackingPhoenixBase();

            //  TFTVTutorialAndStory.TacticalHintsToShow = data.TacticalHintsToShow;

            //  Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            Main.Logger.LogInfo("# Characters with broken limbs: " + TFTVStamina.charactersWithBrokenLimbs.Count);
            Main.Logger.LogInfo("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            //    Main.Logger.LogInfo("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            Main.Logger.LogInfo("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            Main.Logger.LogInfo("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            Main.Logger.LogInfo("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
            Main.Logger.LogInfo("# sites on Behemoth scenic route " + TFTVAirCombat.behemothScenicRoute.Count);
            Main.Logger.LogInfo("Behemoth target id number is " + TFTVAirCombat.behemothTarget);
            Main.Logger.LogInfo("Behemoth will wait for another  " + TFTVAirCombat.behemothWaitHours + " before moving");
            Main.Logger.LogInfo("Last time a Revenant was seen was on day " + TFTVRevenant.daysRevenantLastSeen + ", and now it is day " + Controller.Timing.Now.TimeSpan.Days);
            Main.Logger.LogInfo("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
         //   Main.Logger.LogInfo("Faction attacking Phoenix Base is " + TFTVExperimental.FactionAttackingPhoenixBase.name);
            //  Main.Logger.LogInfo("Shown hints count " + TFTVTutorialAndStory.TacticalHintsToShow.Count);


            //  
            TFTVLogger.Always("# Characters with broken limbs: " + TFTVStamina.charactersWithBrokenLimbs.Count);
            TFTVLogger.Always("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            //   TFTVLogger.Always("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            TFTVLogger.Always("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            TFTVLogger.Always("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            TFTVLogger.Always("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
            TFTVLogger.Always("# sites on Behemoth scenic route " + TFTVAirCombat.behemothScenicRoute.Count);
            TFTVLogger.Always("Behemoth target id number is " + TFTVAirCombat.behemothTarget);
            TFTVLogger.Always("Behemoth will wait for another  " + TFTVAirCombat.behemothWaitHours + " before moving");
            TFTVLogger.Always("Last time a Revenant was seen was on day " + TFTVRevenant.daysRevenantLastSeen + ", and now it is day " + Controller.Timing.Now.TimeSpan.Days);
            TFTVLogger.Always("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
           // TFTVLogger.Always("Faction attacking Phoenix Base is " + TFTVExperimental.FactionAttackingPhoenixBase.name);
            // TFTVLogger.Always("Shown hints count " + TFTVTutorialAndStory.TacticalHintsToShow.Count);

        }


        /// <summary>
        /// Called when new Geoscape world is generating. This only happens on new game.
        /// Useful to modify initial spawned sites.
        /// </summary>
        /// <param name="setup">Main geoscape setup object.</param>
        /// <param name="worldSites">Sites to spawn and start simulating.</param>
        public override void OnGeoscapeNewWorldInit(GeoInitialWorldSetup setup, IList<GeoSiteSceneDef.SiteInfo> worldSites)
        {
            TFTVMain main = (TFTVMain)Main;
            GeoLevelController gsController = Controller;
            TFTVCommonMethods.ClearInternalVariables();

            try
            {

                setup.InitialScavengingSiteCount = (uint)main.Config.InitialScavSites;

                // Generate only one LOTA site of each kind
                foreach (GeoInitialWorldSetup.ArcheologyHasvestingConfiguration archeologyHasvestingConfiguration in setup.ArcheologyHasvestingSitesDistribution)
                {
                    archeologyHasvestingConfiguration.AmountToGenerate = 1;
                }

                // ScavengingSitesDistribution is an array with the weights for scav, rescue soldier and vehicle
                foreach (GeoInitialWorldSetup.ScavengingSiteConfiguration scavSiteConf in setup.ScavengingSitesDistribution)
                {
                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_ResourceCrates_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueSoldier_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }


                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        /// <summary>
        /// Called when generated Geoscape world will pass through simulation step. This only happens on new game.
        /// Useful to modify game startup setup before simulation.
        /// </summary>
        /// <param name="setup">Main geoscape setup object.</param>
        /// <param name="context">Context for game setup.</param>
        public override void OnGeoscapeNewWorldSimulationStart(GeoInitialWorldSetup setup, GeoInitialWorldSetup.SimContext context)
        {

            try
            {

                TFTVMain main = (TFTVMain)Main;
                GeoLevelController gsController = Controller;



                setup.InitialScavengingSiteCount = (uint)main.Config.InitialScavSites;

                // Generate only one LOTA site of each kind
                foreach (GeoInitialWorldSetup.ArcheologyHasvestingConfiguration archeologyHasvestingConfiguration in setup.ArcheologyHasvestingSitesDistribution) 
                {

                    archeologyHasvestingConfiguration.AmountToGenerate = 1;
                
                
                }

                
                // ScavengingSitesDistribution is an array with the weights for scav, rescue soldier and vehicle
                foreach (GeoInitialWorldSetup.ScavengingSiteConfiguration scavSiteConf in setup.ScavengingSitesDistribution)
                {
                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_ResourceCrates_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueSoldier_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavSoldiers == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }


                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
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
}