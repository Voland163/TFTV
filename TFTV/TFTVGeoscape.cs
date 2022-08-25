using Base.Serialization.General;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Modding;
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
        public List<int> targetsVisitedByBehemoth = TFTVAirCombat.targetsVisitedByBehemoth;
        public Dictionary<int, List<int>> flyersAndHavens = TFTVAirCombat.flyersAndHavens;
        public bool checkHammerfall = TFTVAirCombat.checkHammerfall;
        public Dictionary<string, int> DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
    }

    /// <summary>
    /// Represents a mod instance specific for Geoscape game.
    /// Each time Geoscape level is loaded, new mod's ModGeoscape is created.
    /// </summary>
    public class TFTVGeoscape : ModGeoscape
    {

        /// <summary>
        /// Called when Geoscape starts.
        /// </summary>
        public override void OnGeoscapeStart()
        {
           
            /// Geoscape level controller is accessible at any time.
            GeoLevelController gsController = Controller;
            /// ModMain is accesible at any time

            TFTVMain main = (TFTVMain)Main;
            TFTVNewPXCharacters.PlayIntro(gsController);
            TFTVVoidOmens.ImplementVoidOmens(gsController);
            TFTVUmbra.CheckForUmbraResearch(gsController);
            TFTVUmbra.SetUmbraEvolution(gsController);
            TFTVThirdAct.SetBehemothOnRampageMod(gsController);
            TFTVChangesToDLC3Events.ChangeHavenDeploymentDefense(gsController);
            TFTVStamina.CheckBrokenLimbs(gsController.PhoenixFaction.Soldiers.ToList());
            
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
            if (TFTVVoidOmens.VoidOmen16Active && TFTVVoidOmens.VoidOmen15Active)
            {
                TFTVUmbra.SetUmbraRandomValue(0.32f);
            }
            if (TFTVVoidOmens.VoidOmen16Active && !TFTVVoidOmens.VoidOmen15Active)
            {
                TFTVUmbra.SetUmbraRandomValue(0.16f);
            }
            TFTVUI.hookToCharacter = null;

            TFTVRevenant.timeOfMissionStart = Controller.Timing.Now;
            TFTVLogger.Always("The time is " + TFTVRevenant.timeOfMissionStart.DateTime);
            
            
        }

        /// <summary>
        /// Called when Geoscape save is going to be generated, giving mod option for custom save data.
        /// </summary>
        /// <returns>Object to serialize or null if not used.</returns>
        public override object RecordGeoscapeInstanceData()
        {
            return new TFTVGSInstanceData()
            {
                charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs,
                targetsForBehemoth = TFTVAirCombat.targetsForBehemoth,
                targetsVisitedByBehemoth = TFTVAirCombat.targetsForBehemoth,
                flyersAndHavens = TFTVAirCombat.flyersAndHavens,
                checkHammerfall = TFTVAirCombat.checkHammerfall,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
            };

        }
        /// <summary>
        /// Called when Geoscape save is being process. At this point level is already created, but GeoscapeStart is not called.
        /// </summary>
        /// <param name="instanceData">Instance data serialized for this mod. Cannot be null.</param>
        public override void ProcessGeoscapeInstanceData(object instanceData)
        {
            TFTVGSInstanceData data = (TFTVGSInstanceData)instanceData;

            Main.Logger.LogInfo("# Characters with broken limbs in the save: " + data.charactersWithBrokenLimbs.Count);
            Main.Logger.LogInfo("# Behemoth targets for this emergence in the save: " + data.targetsForBehemoth.Count);
            Main.Logger.LogInfo("# Targets already hit by Behemoth on this emergence in the save: " + data.targetsVisitedByBehemoth.Count);
            Main.Logger.LogInfo("# Pandoran flyers that have visited havens on this emergence in the save:  " + data.flyersAndHavens.Count);
            Main.Logger.LogInfo("Hammerfall in the save: " + data.checkHammerfall);
            Main.Logger.LogInfo("# Lost operatives in the save: " + data.DeadSoldiersDelirium.Count);
            TFTVLogger.Always("# Characters with broken limbs in the save: " + data.charactersWithBrokenLimbs.Count);
            TFTVLogger.Always("# Behemoth targets for this emergence in the save: " + data.targetsForBehemoth.Count);
            TFTVLogger.Always("# Targets already hit by Behemoth on this emergence in the save: " + data.targetsVisitedByBehemoth.Count);
            TFTVLogger.Always("# Pandoran flyers that have visited havens on this emergence in the save:  " + data.flyersAndHavens.Count);
            TFTVLogger.Always("Hammerfall in the save: " + data.checkHammerfall);
            TFTVLogger.Always("# Lost operatives in the save: " + data.DeadSoldiersDelirium.Count);

             TFTVStamina.charactersWithBrokenLimbs = data.charactersWithBrokenLimbs;
             TFTVAirCombat.targetsForBehemoth = data.targetsForBehemoth;
             TFTVAirCombat.targetsVisitedByBehemoth = data.targetsVisitedByBehemoth;
             TFTVAirCombat.flyersAndHavens = data.flyersAndHavens;
             TFTVAirCombat.checkHammerfall = data.checkHammerfall;
             TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;

           /* TFTVAirCombat.targetsForBehemoth = new List<int>();
            TFTVAirCombat.targetsVisitedByBehemoth = new List<int>();
            TFTVAirCombat.flyersAndHavens = new Dictionary<int, List<int>>();
            TFTVAirCombat.checkHammerfall = true;
            TFTVRevenant.DeadSoldiersDelirium = new Dictionary<string, int>();*/

            
            Main.Logger.LogInfo("# Characters with broken limbs: " + TFTVStamina.charactersWithBrokenLimbs.Count);
            Main.Logger.LogInfo("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            Main.Logger.LogInfo("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            Main.Logger.LogInfo("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            Main.Logger.LogInfo("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            Main.Logger.LogInfo("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
            TFTVLogger.Always("# Characters with broken limbs: " + TFTVStamina.charactersWithBrokenLimbs.Count);
            TFTVLogger.Always("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            TFTVLogger.Always("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            TFTVLogger.Always("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            TFTVLogger.Always("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            TFTVLogger.Always("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
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
            TFTVAirCombat.targetsForBehemoth = new List<int>();
            TFTVAirCombat.targetsVisitedByBehemoth = new List<int>();
            TFTVAirCombat.flyersAndHavens = new Dictionary<int, List<int>>();
            TFTVAirCombat.checkHammerfall = false;
            TFTVRevenant.DeadSoldiersDelirium = new Dictionary<string, int>();


            try
            {
                                
                setup.InitialScavengingSiteCount = (uint)main.Config.InitialScavSites;

                // ScavengingSitesDistribution is an array with the weights for scav, rescue soldier and vehicle
                foreach (GeoInitialWorldSetup.ScavengingSiteConfiguration scavSiteConf in setup.ScavengingSitesDistribution)
                {
                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_ResourceCrates_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 4;
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
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 4;
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
                
                /*	if (main.Config.MoreAmbushes)
                    {
                        TFTVAmbushes.Apply_Changes_Ambush_Chances(gsController.EventSystem);
                    }*/


                setup.InitialScavengingSiteCount = (uint)main.Config.InitialScavSites;

                // ScavengingSitesDistribution is an array with the weights for scav, rescue soldier and vehicle
                foreach (GeoInitialWorldSetup.ScavengingSiteConfiguration scavSiteConf in setup.ScavengingSitesDistribution)
                {
                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_ResourceCrates_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavCrates == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 4;
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
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (main.Config.ChancesScavGroundVehicleRescue == TFTVConfig.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 4;
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