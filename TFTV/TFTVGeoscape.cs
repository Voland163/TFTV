using Base.Serialization.General;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
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
        public Dictionary<int, List<string>> charactersWithDisabledBodyParts = TFTVStamina.charactersWithDisabledBodyParts;
        public List<int> targetsForBehemoth = TFTVBehemothAndRaids.targetsForBehemoth;
        public Dictionary<int, List<int>> flyersAndHavens = TFTVBehemothAndRaids.flyersAndHavens;
        public bool checkHammerfall = TFTVBehemothAndRaids.checkHammerfall;
        public Dictionary<int, int> DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
        public List<int> behemothScenicRoute = TFTVBehemothAndRaids.behemothScenicRoute;
        public int behemothTarget = TFTVBehemothAndRaids.behemothTarget;
        public int behemothWaitHours = TFTVBehemothAndRaids.behemothWaitHours;
        public int timeRevenantLasteSeenSaveData = TFTVRevenant.daysRevenantLastSeen;
        public int infestedHavenPopulationSaveData = TFTVInfestation.HavenPopulation;
        public string infestedHavenOriginalOwnerSaveData = TFTVInfestation.OriginalOwner;
        public Dictionary<int, int[]> ProjectOsirisStatsSaveData = TFTVRevenantResearch.ProjectOsirisStats;
        public Dictionary<int, Dictionary<string, double>> PhoenixBasesUnderAttack = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack;
        public Dictionary<int, int> PhoenixBasesContainmentBreach = TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach;
        public List<int> InfestedPhoenixBases = new List<int>();
        public int SpawnedScyllas = new int();
        public Dictionary<int, Dictionary<string, List<string>>> CharacterLoadouts;
        public Dictionary<int, int> CharactersDeliriumPerksAndMissions;
        public float SuppliesFromProcessedPandas;
        public float ToxinsInFood;

        public int DifficultySetting;
        public bool NewConfigUsedInstance;
        public float AmountOfExoticResourcesSettingInstance;
        public float ResourceMultiplierSettingInstance;
        public bool DiplomaticPenaltiesSettingInstance;
        public bool StaminaPenaltyFromInjurySettingInstance;
        public bool MoreAmbushesSettingInstance;
        public bool LimitedCaptureSettingInstance;
        public bool LimitedHarvestingSettingInstance;
        public bool StrongerPandoransSettingInstance;
        public bool ImpossibleWeaponsAdjustmentsSettingInstance;
        public bool NoSecondChances;

       // public bool Update35GeoscapeCheck;

        public List<int> PU_Hotspots;
        public List<int> FO_Hotspots;
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
            TFTVLogger.Always($"OnGeoscapeStart");


            /// Geoscape level controller is accessible at any time.
            GeoLevelController gsController = Controller;


            /// ModMain is accesible at any time

            TFTVBetaSaveGamesFixes.OpenBetaSaveGameFixes(gsController);
            TFTVLogger.Always($"Difficulty level on Geoscape is {Controller.CurrentDifficultyLevel.name}");
            TFTVBetaSaveGamesFixes.CorrrectPhoenixSaveManagerDifficulty();
            TFTVCommonMethods.CheckGeoUIfunctionality(gsController);
            TFTVBackgroundsAndCharacters.PlayIntro(gsController);
            TFTVVoidOmens.ImplementVoidOmens(gsController);
            TFTVTouchedByTheVoid.Umbra.UmbraGeoscape.CheckForUmbraResearch(gsController);
            TFTVTouchedByTheVoid.Umbra.UmbraGeoscape.SetUmbraEvolution(gsController);
            TFTVBehemothAndRaids.SetBehemothOnRampageMod(gsController);
            TFTVStamina.CheckBrokenLimbs(gsController.PhoenixFaction.Soldiers.ToList(), gsController);
            TFTVRevenant.RecordUpkeep.UpdateRevenantTimer(gsController);
            if (TFTVRevenant.revenantID != 0 && TFTVRevenant.DeadSoldiersDelirium.ContainsKey(TFTVRevenant.revenantID))
            {
                TFTVRevenant.DeadSoldiersDelirium[TFTVRevenant.revenantID] += 1;
            }

            TFTVRevenantResearch.CheckRevenantResearchRequirements(Controller);
            TFTVProjectOsiris.RunProjectOsiris(gsController);
            Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVTouchedByTheVoid.TBTVVariableName));
            TFTVLogger.Always("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVTouchedByTheVoid.TBTVVariableName));
            TFTVAncientsGeo.AncientsResearch.AncientsCheckResearchState(gsController);
            TFTVAncientsGeo.ImpossibleWeapons.CheckImpossibleWeaponsAdditionalRequirements(gsController);
            TFTVAncientsGeo.ExoticResources.EnsureNoHarvesting(gsController);
            TFTVVoxels.TFTVFire.CheckForFireQuenchers(gsController);
            TFTVSpecialDifficulties.DefModifying.CheckForSpecialDifficulties();
            TFTVPandoranProgress.ScyllaCount = 0;
            TFTVODIandVoidOmenRoll.Calculate_ODI_Level(Controller);
            TFTVBetaSaveGamesFixes.CheckResearches(Controller);
            TFTVPassengerModules.ImplementFarMConfig(Controller);
          //  TFTVNewGameOptions.Change_Crossbows();
            TFTVBetaSaveGamesFixes.RemoveBadSlug(Controller);
           // TFTVDefsInjectedOnlyOnce.Print();
           // TFTVAmbushes.GetAllPureAndForsakenHotspots(Controller);
           
       /*  foreach(ResearchElement element in Controller.NewJerichoFaction.Research.Researchable) 
            {
                TFTVLogger.Always($"{element.ResearchID}");
            }*/
            
            // TFTVDefsInjectedOnlyOnce.Print();
         //  TFTVBetaSaveGamesFixes.SpecialFixForNarvi();
        }
        /// <summary>
        /// Called when Geoscape ends.
        /// </summary>
        public override void OnGeoscapeEnd()
        {
            TFTVLogger.Always($"OnGeoscapeEnd");
            GeoLevelController gsController = Controller;

            TFTVTouchedByTheVoid.Umbra.UmbraGeoscape.CheckForUmbraResearch(gsController);
            TFTVTouchedByTheVoid.Umbra.UmbraGeoscape.SetUmbraEvolution(gsController);
            TFTVVoidOmens.CheckForVoidOmensRequiringTacticalPatching(gsController);

            TFTVRevenant.PrespawnChecks.CheckRevenantTime(gsController);
            TFTVRevenantResearch.CheckProjectOsiris(gsController);
            TFTVDiplomacyPenalties.VoidOmensImplemented = false;
            TFTVAncientsGeo.AncientsResearch.CheckResearchStateOnGeoscapeEndAndOnTacticalStart(gsController);

        }

        /// <summary>
        /// Called when Geoscape save is going to be generated, giving mod option for custom save data.
        /// </summary>
        /// <returns>Object to serialize or null if not used.</returns>
        public override object RecordGeoscapeInstanceData()
        {
            TFTVLogger.Always("Geoscape data will be saved");

            /*  foreach (int i in TFTVBaseDefenseGeoscape.PhoenixBasesInfested)
              {
                  TFTVLogger.Always($"On RecordInstance: infested base in temporary variable is {i}");

              }*/


            //  TFTVLogger.Always($"Items currently available in Aircraft inventory {TFTVUI.CurrentlyAvailableInv.Values.Count}");
            //  TFTVLogger.Always($"Items currently hidden in Aircraft inventory {TFTVUI.CurrentlyHiddenInv.Values.Count}");
            TFTVRevenant.RecordUpkeep.UpdateRevenantTimer(Controller);
            return new TFTVGSInstanceData()
            {

                charactersWithDisabledBodyParts = TFTVStamina.charactersWithDisabledBodyParts,
                targetsForBehemoth = TFTVBehemothAndRaids.targetsForBehemoth,
                flyersAndHavens = TFTVBehemothAndRaids.flyersAndHavens,
                checkHammerfall = TFTVBehemothAndRaids.checkHammerfall,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
                behemothScenicRoute = TFTVBehemothAndRaids.behemothScenicRoute,
                behemothTarget = TFTVBehemothAndRaids.behemothTarget,
                behemothWaitHours = TFTVBehemothAndRaids.behemothWaitHours,
                timeRevenantLasteSeenSaveData = TFTVRevenant.daysRevenantLastSeen,
                infestedHavenOriginalOwnerSaveData = TFTVInfestation.OriginalOwner,
                infestedHavenPopulationSaveData = TFTVInfestation.HavenPopulation,
                ProjectOsirisStatsSaveData = TFTVRevenantResearch.ProjectOsirisStats,
                PhoenixBasesUnderAttack = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack,
                PhoenixBasesContainmentBreach = TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach,
                InfestedPhoenixBases = TFTVBaseDefenseGeoscape.PhoenixBasesInfested,
                SpawnedScyllas = TFTVPandoranProgress.ScyllaCount,
                CharacterLoadouts = TFTVUI.EditScreen.LoadoutsAndHelmetToggle.CharacterLoadouts,
                CharactersDeliriumPerksAndMissions = TFTVDelirium.CharactersDeliriumPerksAndMissions,
                SuppliesFromProcessedPandas = TFTVCapturePandoransGeoscape.PandasForFoodProcessing,
                ToxinsInFood = TFTVCapturePandoransGeoscape.ToxinsInCirculation,
                NewConfigUsedInstance = TFTVNewGameOptions.ConfigImplemented,
                DifficultySetting = TFTVNewGameOptions.InternalDifficultyCheck,
                AmountOfExoticResourcesSettingInstance = TFTVNewGameOptions.AmountOfExoticResourcesSetting,
                ResourceMultiplierSettingInstance = TFTVNewGameOptions.ResourceMultiplierSetting,
                DiplomaticPenaltiesSettingInstance = TFTVNewGameOptions.DiplomaticPenaltiesSetting,
                StaminaPenaltyFromInjurySettingInstance = TFTVNewGameOptions.StaminaPenaltyFromInjurySetting,
                MoreAmbushesSettingInstance = TFTVNewGameOptions.MoreAmbushesSetting,
                LimitedCaptureSettingInstance = TFTVNewGameOptions.LimitedCaptureSetting,
                LimitedHarvestingSettingInstance = TFTVNewGameOptions.LimitedHarvestingSetting,
                StrongerPandoransSettingInstance = TFTVNewGameOptions.StrongerPandoransSetting,
                ImpossibleWeaponsAdjustmentsSettingInstance = TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting,
                NoSecondChances = TFTVNewGameOptions.NoSecondChances,
                PU_Hotspots = TFTVAmbushes.NJ_Purists_Hotspots,
                FO_Hotspots = TFTVAmbushes.AN_FallenOnes_Hotspots,
             //   Update35GeoscapeCheck = TFTVNewGameOptions.Update35Check,

            };

        }
        /// <summary>
        /// Called when Geoscape save is being process. At this point level is already created, but GeoscapeStart is not called.
        /// </summary>
        /// <param name="instanceData">Instance data serialized for this mod. Cannot be null.</param>
        public override void ProcessGeoscapeInstanceData(object instanceData)
        {
            try
            {
                DateTime myDate = new DateTime(1, 1, 1);

                TFTVLogger.Always("Geoscape data will be processed");

                TFTVGSInstanceData data = (TFTVGSInstanceData)instanceData;

                TFTVCommonMethods.ClearInternalVariables();

                TFTVStamina.charactersWithDisabledBodyParts = data.charactersWithDisabledBodyParts;
                TFTVBehemothAndRaids.targetsForBehemoth = data.targetsForBehemoth;
                TFTVBehemothAndRaids.flyersAndHavens = data.flyersAndHavens;
                TFTVBehemothAndRaids.checkHammerfall = data.checkHammerfall;
                TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;
                TFTVRevenant.daysRevenantLastSeen = data.timeRevenantLasteSeenSaveData;
                TFTVBehemothAndRaids.behemothScenicRoute = data.behemothScenicRoute;
                TFTVBehemothAndRaids.behemothTarget = data.behemothTarget;
                TFTVBehemothAndRaids.behemothWaitHours = data.behemothWaitHours;
                TFTVInfestation.HavenPopulation = data.infestedHavenPopulationSaveData;
                TFTVInfestation.OriginalOwner = data.infestedHavenOriginalOwnerSaveData;
                TFTVRevenantResearch.ProjectOsirisStats = data.ProjectOsirisStatsSaveData;
                TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack = data.PhoenixBasesUnderAttack;
                TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach = data.PhoenixBasesContainmentBreach;
                TFTVBaseDefenseGeoscape.PhoenixBasesInfested = data.InfestedPhoenixBases;
                TFTVPandoranProgress.ScyllaCount = data.SpawnedScyllas;
                TFTVUI.EditScreen.LoadoutsAndHelmetToggle.CharacterLoadouts = data.CharacterLoadouts;
                TFTVDelirium.CharactersDeliriumPerksAndMissions = data.CharactersDeliriumPerksAndMissions;
                TFTVCapturePandoransGeoscape.PandasForFoodProcessing = data.SuppliesFromProcessedPandas;
                TFTVCapturePandoransGeoscape.ToxinsInCirculation = data.ToxinsInFood;
                TFTVNewGameOptions.ConfigImplemented = data.NewConfigUsedInstance;
                TFTVAmbushes.AN_FallenOnes_Hotspots = data.FO_Hotspots;
                TFTVAmbushes.NJ_Purists_Hotspots = data.PU_Hotspots;

                TFTVLogger.Always($"ConfigImplemented? {TFTVNewGameOptions.ConfigImplemented}");

                if (TFTVNewGameOptions.ConfigImplemented)
                {
                    TFTVNewGameOptions.InternalDifficultyCheck = data.DifficultySetting;
                    TFTVNewGameOptions.AmountOfExoticResourcesSetting = data.AmountOfExoticResourcesSettingInstance;
                    TFTVNewGameOptions.ResourceMultiplierSetting = data.ResourceMultiplierSettingInstance;
                    TFTVNewGameOptions.DiplomaticPenaltiesSetting = data.DiplomaticPenaltiesSettingInstance;
                    TFTVNewGameOptions.StaminaPenaltyFromInjurySetting = data.StaminaPenaltyFromInjurySettingInstance;
                    TFTVNewGameOptions.MoreAmbushesSetting = data.MoreAmbushesSettingInstance;
                    TFTVNewGameOptions.LimitedCaptureSetting = data.LimitedCaptureSettingInstance;
                    TFTVNewGameOptions.LimitedHarvestingSetting = data.LimitedHarvestingSettingInstance;
                    TFTVNewGameOptions.StrongerPandoransSetting = data.StrongerPandoransSettingInstance;
                    TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = data.ImpossibleWeaponsAdjustmentsSettingInstance;
                    TFTVNewGameOptions.NoSecondChances = data.NoSecondChances;
                }
                else 
                {
                    TFTVNewGameOptions.SetInternalConfigOptions(Controller); 
                }

                TFTVLogger.Always($"Config settings:" +
                    $"\nAmountOfExoticResourcesSetting: {TFTVNewGameOptions.AmountOfExoticResourcesSetting}\nResourceMultiplierSetting: {TFTVNewGameOptions.ResourceMultiplierSetting}" +
                    $"\nDiplomaticPenaltiesSetting: {TFTVNewGameOptions.DiplomaticPenaltiesSetting}\nStaminaPenaltyFromInjurySetting: {TFTVNewGameOptions.StaminaPenaltyFromInjurySetting}" +
                    $"\nMoreAmbushesSetting: {TFTVNewGameOptions.MoreAmbushesSetting}\nLimitedCaptureSetting: {TFTVNewGameOptions.LimitedCaptureSetting}\nLimitedHarvestingSetting: {TFTVNewGameOptions.LimitedHarvestingSetting}" +
                    $"\nStrongerPandoransSetting {TFTVNewGameOptions.StrongerPandoransSetting}\nImpossibleWeaponsAdjustmentsSetting: {TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting}" +
                    $"\nNoSecondChances: {TFTVNewGameOptions.NoSecondChances}");

                TFTVDefsWithConfigDependency.ImplementConfigChoices();

               // TFTVNewGameOptions.Update35Check = data.Update35GeoscapeCheck;

                //  Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                Main.Logger.LogInfo("# Characters with broken limbs: " + TFTVStamina.charactersWithDisabledBodyParts.Count);
                Main.Logger.LogInfo("# Behemoth targets for this emergence: " + TFTVBehemothAndRaids.targetsForBehemoth.Count);
                //    Main.Logger.LogInfo("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
                Main.Logger.LogInfo("# Pandoran flyers that have visited havens on this emergence:  " + TFTVBehemothAndRaids.flyersAndHavens.Count);
                Main.Logger.LogInfo("Hammerfall: " + TFTVBehemothAndRaids.checkHammerfall);
                Main.Logger.LogInfo("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
                Main.Logger.LogInfo("# sites on Behemoth scenic route " + TFTVBehemothAndRaids.behemothScenicRoute.Count);
                Main.Logger.LogInfo("Behemoth target id number is " + TFTVBehemothAndRaids.behemothTarget);
                Main.Logger.LogInfo("Behemoth will wait for " + TFTVBehemothAndRaids.behemothWaitHours + " hours before moving");
                Main.Logger.LogInfo("Last time a Revenant was seen was on  " + myDate.Add(new TimeSpan(TFTVRevenant.daysRevenantLastSeen, 0, 0, 0)) + ", and now it is day " + myDate.Add(new TimeSpan(Controller.Timing.Now.TimeSpan.Ticks)));
                Main.Logger.LogInfo("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
                //  Main.Logger.LogInfo("LOTAGlobalReworkCheck is " + TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck);
                Main.Logger.LogInfo($"Bases under attack count {TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Count}");
                Main.Logger.LogInfo($"Infested Phoenix bases {TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Count}");
                Main.Logger.LogInfo($"Supplies from Pandas pending processing {TFTVCapturePandoransGeoscape.PandasForFoodProcessing}");
                Main.Logger.LogInfo($"Toxins in food {TFTVCapturePandoransGeoscape.ToxinsInCirculation}");
                Main.Logger.LogInfo($"Scylla count {TFTVPandoranProgress.ScyllaCount}");
                Main.Logger.LogInfo($"infested haven population save data {TFTVInfestation.HavenPopulation}");
                Main.Logger.LogInfo($"aircraft capacity {TFTVCapturePandorans.AircraftCaptureCapacity}");

                // Main.Logger.LogInfo($"New Difficulties implemented {TFTVReleaseOnly.NewDifficultiesImplemented}");
                //    Main.Logger.LogInfo($"Items currently available in Aircraft inventory {TFTVUI.CurrentlyAvailableInv.Values.Count}");
                //   Main.Logger.LogInfo($"Items currently hidden in Aircraft inventory {TFTVUI.CurrentlyAvailableInv.Values.Count}");
                //  
                TFTVLogger.Always("# Characters with broken limbs: " + TFTVStamina.charactersWithDisabledBodyParts.Count);
                TFTVLogger.Always("# Behemoth targets for this emergence: " + TFTVBehemothAndRaids.targetsForBehemoth.Count);
                //   TFTVLogger.Always("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
                TFTVLogger.Always("# Pandoran flyers that have visited havens on this emergence:  " + TFTVBehemothAndRaids.flyersAndHavens.Count);
                TFTVLogger.Always("Hammerfall: " + TFTVBehemothAndRaids.checkHammerfall);
                TFTVLogger.Always("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
                TFTVLogger.Always("# sites on Behemoth scenic route " + TFTVBehemothAndRaids.behemothScenicRoute.Count);
                TFTVLogger.Always("Behemoth target id number is " + TFTVBehemothAndRaids.behemothTarget);
                TFTVLogger.Always("Behemoth will wait for another  " + TFTVBehemothAndRaids.behemothWaitHours + " before moving");
                TFTVLogger.Always("Last time a Revenant was seen was on  " + myDate.Add(new TimeSpan(TFTVRevenant.daysRevenantLastSeen, 0, 0, 0)) + ", and now it is day " + myDate.Add(new TimeSpan(Controller.Timing.Now.TimeSpan.Ticks)));
                TFTVLogger.Always("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
                //   TFTVLogger.Always("LOTAGlobalReworkCheck is " + TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck);
                TFTVLogger.Always($"Bases under attack count {TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Count}");
                TFTVLogger.Always($"Infested Phoenix bases {TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Count}");
                TFTVLogger.Always($"Supplies from Pandas pending processing {TFTVCapturePandoransGeoscape.PandasForFoodProcessing}");
                TFTVLogger.Always($"Toxins in food {TFTVCapturePandoransGeoscape.ToxinsInCirculation}");
                TFTVLogger.Always($"Scylla count {TFTVPandoranProgress.ScyllaCount}");
                TFTVLogger.Always($"Internal difficulty check {TFTVNewGameOptions.InternalDifficultyCheck}");

              //  TFTVLogger.Always($"Pure hotspots count {TFTVAmbushes.NJ_Purists_Hotspots.Count()>0}");
              //  TFTVLogger.Always($"Forsaken hotspots count {TFTVAmbushes.AN_FallenOnes_Hotspots.Count()>0}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
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
            TFTVConfig config = TFTVMain.Main.Config;


            List<int> locations = new List<int>() {
                0, // "Vanilla Random"
                1, //"Random (ALL bases included)"
                584, //"Antarctica"
                170, //"Asia (China)"
                191, //"Australia"
                186, //"Central America (Honduras)"                
                169, //"East Africa (Ethiopia)"
                166, //"Eastern Europe (Ukraine)"
                187, //"Greenland"
                172, //"Middle East (Afghanistan)"
                165, //"North Africa (Algeria)"
                192, //"North America (Alaska)"
                167, //"North America (Mexico)"
                185, //"North America (Quebec)"
                171, //"Northern Asia (Siberia)"
                189, //"South Africa (Zimbabwe)"
                168, // "South America (Bolivia)"
                193, //"South America (Tierra de Fuego)"
                190, //"Southeast Asia (Cambodia)"
                188  // "West Africa (Ghana)
            };

            List<int> phoenixBases = new List<int>(){
                584, //"Antarctica"
                170, //"Asia (China)"
                191, //"Australia"
                186, //"Central America (Honduras)"
                169, //"East Africa (Ethiopia)"
                166, //"Eastern Europe (Ukraine)"
                187, //"Greenland"
                172, //"Middle East (Afghanistan)"
                165, //"North Africa (Algeria)"
                192, //"North America (Alaska)"
                167, //"North America (Mexico)"
                185, //"North America (Quebec)"
                171, //"Northern Asia (Siberia)"
                189, //"South Africa (Zimbabwe)"
                168, // "South America (Bolivia)"
                193, //"South America (Tierra de Fuego)"
                190, //"Southeast Asia (Cambodia)"
                188  // "West Africa (Ghana)
            };

            /*
            (-0.4f, 3.7f, 5.2f),  North Africa (Algeria) 165 
            (-1.9f, 4.8f, 3.8f), Eastern Europe (Ukraine) 166
            (5.3f, 3.1f, -1.7f), Central America (Mexico) 167
            (5.5f, -2.0f, 2.7f), South America (Bolivia) 168
            (-4.2f, 1.1f, 4.7f), East Africa (Ethiopia) 169
            (-5.1f, 3.8f, -0.8f), Asia (China)  170
            (-1.9f, 6.0f, -1.2f), Northern Asia (Siberia) 171
            (-4.8f, 3.7f, 2.2f) Middle East (Afghanistan) 172
            (0.0f, -6.4f, 0.1f) Antarctica 584
            (3.5f, -5.2f, 1.3f)  South America (Tierra de Fuego) 193
            (-4.5f, -1.5f, -4.3f) Australia 191
            (-6.0f, 1.3f, -1.9f) Southeast Asia (Cambodia) 190
            (-2.7f, -2.4f, 5.3f) South Africa (Zimbabwe) 189
            (0.5f, 0.8f, 6.3f) West Africa (Ghana) 188
            (6.2f, 1.6f, 0.4f) Central America (Honduras) 186
            (4.0f, 4.9f, 1.0f) North America (Quebec) 185 
            (1.3f, 5.7f, -2.6f) North America (Alaska) 192
            (0.7f, 6.2f, 1.5f) Greenland 187 
            */

            try
            {
                if (TFTVNewGameOptions.startingBaseLocation == TFTVNewGameOptions.StartingBaseLocation.Vanilla && TFTVSpecialDifficulties.CheckGeoscapeSpecialDifficultySettings(gsController) == 0)
                {

                }
                else
                {
                    foreach (GeoSiteSceneDef.SiteInfo siteInfo in worldSites.Where(ws => phoenixBases.Any(id => id.Equals(ws.SiteId))))
                    {
                        int index = (int)TFTVNewGameOptions.startingBaseLocation;
                        TFTVLogger.Always($"chosen base is {locations[index]}");

                        if (TFTVNewGameOptions.startingBaseLocation == TFTVNewGameOptions.StartingBaseLocation.Vanilla)
                        {
                            if (TFTVSpecialDifficulties.CheckGeoscapeSpecialDifficultySettings(gsController) == 1)
                            {
                                List<int> forbiddenBases = new List<int> { 167, 168, 584, 193, 191, 186, 185, 192, 187 };

                                if (forbiddenBases.Contains(siteInfo.SiteId))
                                {
                                    if (siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                                    {
                                        siteInfo.SiteTags.Remove("StartingPhoenixBase");
                                    }
                                }
                            }
                            else
                            {
                                List<int> approvedBases = new List<int> { 584, 191 };

                                if (approvedBases.Contains(siteInfo.SiteId))
                                {
                                    if (!siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                                    {
                                        siteInfo.SiteTags.Add("StartingPhoenixBase");
                                    }
                                }
                                else
                                {
                                    if (siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                                    {
                                        siteInfo.SiteTags.Remove("StartingPhoenixBase");
                                    }
                                }
                            }

                        }
                        else if (TFTVNewGameOptions.startingBaseLocation == TFTVNewGameOptions.StartingBaseLocation.Random)
                        {
                            if (!siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                            {
                                siteInfo.SiteTags.Add("StartingPhoenixBase");
                                // TFTVLogger.Always($"Found site {siteInfo.SiteId}");
                            }

                        }
                        else
                        {
                            TFTVLogger.Always($"checking {siteInfo.SiteId}");
                            if (siteInfo.SiteId == locations[index])   //SiteDescription.LocalizationKey== "KEY_OBJECTIVE_PHOENIXBASE_NAME_18") 
                            {
                                if (!siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                                {
                                    siteInfo.SiteTags.Add("StartingPhoenixBase");
                                    TFTVLogger.Always($"Found site {siteInfo.SiteId}");
                                }
                            }
                            else
                            {
                                if (siteInfo.SiteTags.Contains("StartingPhoenixBase"))
                                {
                                    siteInfo.SiteTags.Remove("StartingPhoenixBase");
                                    TFTVLogger.Always($"{siteInfo.SiteId} has site tag removed");
                                    // GeoSitesMapper

                                }

                            }

                        }
                    }
                    //   TFTVLogger.Always($"{siteInfo.SiteDescription.Localize()} world position is {siteInfo.WorldPosition}");

                }



                setup.InitialScavengingSiteCount = (uint)TFTVNewGameOptions.initialScavSites;

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
                        if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueSoldier_MissionTagDef")))
                    {
                        if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }


                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }
                }

                TFTVDefsWithConfigDependency.ImplementConfigChoices();
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



                setup.InitialScavengingSiteCount = (uint)TFTVNewGameOptions.initialScavSites;

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
                        if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavCrates == TFTVNewGameOptions.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }
                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueSoldier_MissionTagDef")))
                    {
                        if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavSoldiers == TFTVNewGameOptions.ScavengingWeight.None)
                        {
                            scavSiteConf.Weight = 0;
                        }


                    }

                    if (scavSiteConf.MissionTags.Any(mt => mt.name.Equals("Contains_RescueVehicle_MissionTagDef")))
                    {
                        if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.High)
                        {
                            scavSiteConf.Weight = 6;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.Medium)
                        {
                            scavSiteConf.Weight = 4;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.Low)
                        {
                            scavSiteConf.Weight = 1;
                        }
                        else if (TFTVNewGameOptions.chancesScavGroundVehicleRescue == TFTVNewGameOptions.ScavengingWeight.None)
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