using Base.Serialization.General;
using Epic.OnlineServices;
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
        public Dictionary<int, List<string>> charactersWithDisabledBodyParts = TFTVStamina.charactersWithDisabledBodyParts;
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
        //   public bool[] VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck;
        //   public bool GlobalLOTAReworkCheck = TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck;
        public Dictionary<int, Dictionary<string, double>> PhoenixBasesUnderAttack = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack;
        public Dictionary<int, int> PhoenixBasesContainmentBreach = TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach;
        public List<int> InfestedPhoenixBases = new List<int>();
        public int SpawnedScyllas = new int();
        public Dictionary<int, Dictionary<string, List<string>>> CharacterLoadouts;
        public Dictionary<int, int> CharactersDeliriumPerksAndMissions;
     //   public int AircraftCaptureCapacity = TFTVCapturePandorans.AircraftCaptureCapacity;
        public float SuppliesFromProcessedPandas;
        public float ToxinsInFood;

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

        public bool Update35GeoscapeCheck;

        //   public bool NewDifficulties;
        //  public Dictionary<int, List<string>> HiddenInventories; //TFTVUI.CurrentlyHiddenInv;
        //  public Dictionary<int, List<string>> AvailableInventories; //= TFTVUI.CurrentlyAvailableInv;
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


        //   internal static bool ConfigImplemented = false;
        /// <summary>
        /// Called when Geoscape starts.
        /// </summary>
        public override void OnGeoscapeStart()
        {
            TFTVLogger.Always($"OnGeoscapeStart");


            /// Geoscape level controller is accessible at any time.
            GeoLevelController gsController = Controller;


            /// ModMain is accesible at any time
            DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [PsychicWard_AbilityDef]").Origin.Range = 10; //Fix Dtony thing
                                                                                                                     //  TFTVBetaSaveGamesFixes.FixInfestedBase(gsController);
                                                                                                                     //  TFTVBetaSaveGamesFixes.CheckSaveGameEventChoices(gsController);
                                                                                                                     //  TFTVBetaSaveGamesFixes.CheckUmbraResearchVariable(gsController);
            TFTVReleaseOnly.ConvertDifficulty(gsController, null);
            TFTVBetaSaveGamesFixes.OpenBetaSaveGameFixes(gsController);

            TFTVCommonMethods.CheckGeoUIfunctionality(gsController);
            TFTVNewPXCharacters.PlayIntro(gsController);
            //  TFTVVoidOmens.CheckVoidOmensBeforeImplementing(gsController);
            TFTVVoidOmens.ImplementVoidOmens(gsController);
            TFTVUmbra.CheckForUmbraResearch(gsController);
            TFTVUmbra.SetUmbraEvolution(gsController);
            TFTVAirCombat.SetBehemothOnRampageMod(gsController);
            TFTVStamina.CheckBrokenLimbs(gsController.PhoenixFaction.Soldiers.ToList(), gsController);
            TFTVRevenant.UpdateRevenantTimer(gsController);
            if (TFTVRevenant.revenantID != 0 && TFTVRevenant.DeadSoldiersDelirium.ContainsKey(TFTVRevenant.revenantID))
            {
                TFTVRevenant.DeadSoldiersDelirium[TFTVRevenant.revenantID] += 1;
            }

            TFTVRevenantResearch.CheckRevenantResearchRequirements(Controller);
            TFTVProjectOsiris.RunProjectOsiris(gsController);
            Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            TFTVLogger.Always("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            // TFTVBetaSaveGamesFixes.CheckNewLOTA(gsController);
            TFTVAncientsGeo.AncientsCheckResearchState(gsController);
            TFTVAncientsGeo.CheckImpossibleWeaponsAdditionalRequirements(gsController);
            TFTVAncientsGeo.EnsureNoHarvesting(gsController);
            TFTVFire.CheckForFireQuenchers(gsController);
            TFTVSpecialDifficulties.CheckForSpecialDifficulties();
            //    TFTVBetterEnemies.ImplementBetterEnemies();
            TFTVPandoranProgress.ScyllaCount = 0;
            TFTVSDIandVoidOmenRoll.Calculate_ODI_Level(Controller);
            TFTVBetaSaveGamesFixes.CheckResearches(Controller);
            TFTVBetaSaveGamesFixes.CheckScyllaCaptureTechResearch(Controller);
            TFTVPassengerModules.ImplementFarMConfig(Controller);
            TFTVNewGameOptions.Change_Crossbows();

            TFTVLogger.Always($"Difficulty level on Geoscape is {Controller.CurrentDifficultyLevel.name}");

            //  TFTVCapturePandorans.SetMutagenOutput(Controller);
            //  TFTVDeliriumPerks.RemoveDeliriumPerks(Controller);

            // TFTVAirCombat.behemothScenicRoute.Clear();
        }
        /// <summary>
        /// Called when Geoscape ends.
        /// </summary>
        public override void OnGeoscapeEnd()
        {
            TFTVLogger.Always($"OnGeoscapeEnd");
            GeoLevelController gsController = Controller;

            TFTVUmbra.CheckForUmbraResearch(gsController);
            TFTVUmbra.SetUmbraEvolution(gsController);
            TFTVVoidOmens.CheckForVoidOmensRequiringTacticalPatching(gsController);
            //  TFTVUI.hookToCharacter = null;
            TFTVRevenant.CheckRevenantTime(gsController);
            TFTVRevenantResearch.CheckProjectOsiris(gsController);
            TFTVDiplomacyPenalties.VoidOmensImplemented = false;
            TFTVAncientsGeo.CheckResearchStateOnGeoscapeEndAndOnTacticalStart(gsController);

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
            TFTVRevenant.UpdateRevenantTimer(Controller);
            return new TFTVGSInstanceData()
            {
                // HiddenInventories = TFTVUI.CurrentlyHiddenInv,
                //  AvailableInventories = TFTVUI.CurrentlyAvailableInv,
                charactersWithDisabledBodyParts = TFTVStamina.charactersWithDisabledBodyParts,
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
                //  VoidOmensCheck = TFTVVoidOmens.VoidOmensCheck,
                //   GlobalLOTAReworkCheck = TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck,
                PhoenixBasesUnderAttack = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack,
                PhoenixBasesContainmentBreach = TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach,
                InfestedPhoenixBases = TFTVBaseDefenseGeoscape.PhoenixBasesInfested,
                SpawnedScyllas = TFTVPandoranProgress.ScyllaCount,
                CharacterLoadouts = TFTVUI.CharacterLoadouts,
                CharactersDeliriumPerksAndMissions = TFTVDelirium.CharactersDeliriumPerksAndMissions,
                SuppliesFromProcessedPandas = TFTVCapturePandoransGeoscape.PandasForFoodProcessing,
                ToxinsInFood = TFTVCapturePandoransGeoscape.ToxinsInCirculation,
                //AircraftCaptureCapacity = TFTVCapturePandorans.AircraftCaptureCapacity,
                NewConfigUsedInstance = TFTVNewGameOptions.ConfigImplemented,
                AmountOfExoticResourcesSettingInstance = TFTVNewGameOptions.AmountOfExoticResourcesSetting,
                ResourceMultiplierSettingInstance = TFTVNewGameOptions.ResourceMultiplierSetting,
                DiplomaticPenaltiesSettingInstance = TFTVNewGameOptions.DiplomaticPenaltiesSetting,
                StaminaPenaltyFromInjurySettingInstance = TFTVNewGameOptions.StaminaPenaltyFromInjurySetting,
                MoreAmbushesSettingInstance = TFTVNewGameOptions.MoreAmbushesSetting,
                LimitedCaptureSettingInstance = TFTVNewGameOptions.LimitedCaptureSetting,
                LimitedHarvestingSettingInstance = TFTVNewGameOptions.LimitedHarvestingSetting,
                StrongerPandoransSettingInstance = TFTVNewGameOptions.StrongerPandoransSetting,
                ImpossibleWeaponsAdjustmentsSettingInstance = TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting,

                Update35GeoscapeCheck = TFTVNewGameOptions.Update35Check,

                //   NewDifficulties = TFTVReleaseOnly.NewDifficultiesImplemented



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

            DateTime myDate = new DateTime(1, 1, 1);

            TFTVLogger.Always("Geoscape data will be processed");


            TFTVGSInstanceData data = (TFTVGSInstanceData)instanceData;
            //  TFTVLogger.Always($"currently infested bases {data.InfestedPhoenixBases.Count}");
            TFTVCommonMethods.ClearInternalVariables();
            //  TFTVLogger.Always($"currently infested bases {data.InfestedPhoenixBases.Count}");
            // TFTVLogger.Always($"Items currently hidden in Aircraft inventory {data.HiddenInventories.Values.Count}");
            //  TFTVUI.CurrentlyAvailableInv = data.AvailableInventories;
            //  TFTVUI.CurrentlyHiddenInv = data.HiddenInventories;
            TFTVStamina.charactersWithDisabledBodyParts = data.charactersWithDisabledBodyParts;
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
          //  TFTVCapturePandorans.AircraftCaptureCapacity = data.AircraftCaptureCapacity;
           
            //  TFTVVoidOmens.VoidOmensCheck = data.VoidOmensCheck;
            //   TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck = data.GlobalLOTAReworkCheck;
            TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack = data.PhoenixBasesUnderAttack;
            TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach = data.PhoenixBasesContainmentBreach;
            TFTVBaseDefenseGeoscape.PhoenixBasesInfested = data.InfestedPhoenixBases;
            TFTVPandoranProgress.ScyllaCount = data.SpawnedScyllas;
            TFTVUI.CharacterLoadouts = data.CharacterLoadouts;
            TFTVDelirium.CharactersDeliriumPerksAndMissions = data.CharactersDeliriumPerksAndMissions;
            TFTVCapturePandoransGeoscape.PandasForFoodProcessing = data.SuppliesFromProcessedPandas;
            TFTVCapturePandoransGeoscape.ToxinsInCirculation = data.ToxinsInFood;
            TFTVNewGameOptions.ConfigImplemented = data.NewConfigUsedInstance;
            if (TFTVNewGameOptions.ConfigImplemented)
            {
                TFTVNewGameOptions.AmountOfExoticResourcesSetting = data.AmountOfExoticResourcesSettingInstance;
                TFTVNewGameOptions.ResourceMultiplierSetting = data.ResourceMultiplierSettingInstance;
                TFTVNewGameOptions.DiplomaticPenaltiesSetting = data.DiplomaticPenaltiesSettingInstance;
                TFTVNewGameOptions.StaminaPenaltyFromInjurySetting = data.StaminaPenaltyFromInjurySettingInstance;
                TFTVNewGameOptions.MoreAmbushesSetting = data.MoreAmbushesSettingInstance;
                TFTVNewGameOptions.LimitedCaptureSetting = data.LimitedCaptureSettingInstance;
                TFTVNewGameOptions.LimitedHarvestingSetting = data.LimitedHarvestingSettingInstance;
                TFTVNewGameOptions.StrongerPandoransSetting = data.StrongerPandoransSettingInstance;
                TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = data.ImpossibleWeaponsAdjustmentsSettingInstance;
            }
            TFTVNewGameOptions.Update35Check = data.Update35GeoscapeCheck;

            //  if (!ConfigImplemented)
            //  {
            TFTVDefsWithConfigDependency.ImplementConfigChoices();
            //     ConfigImplemented = true;
            //  }


            //  TFTVReleaseOnly.NewDifficultiesImplemented = data.NewDifficulties;

            //  TFTVBetaSaveGamesFixes.CheckNewLOTASavegame();
            //TFTVExperimental.FactionAttackingPhoenixBase = data.factionAttackingPheonixBase;
            //TFTVExperimental.CheckIfFactionAttackingPhoenixBase();

            //  TFTVTutorialAndStory.TacticalHintsToShow = data.TacticalHintsToShow;

            //  Main.Logger.LogInfo("UmbraEvolution variable is " + Controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
            Main.Logger.LogInfo("# Characters with broken limbs: " + TFTVStamina.charactersWithDisabledBodyParts.Count);
            Main.Logger.LogInfo("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            //    Main.Logger.LogInfo("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            Main.Logger.LogInfo("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            Main.Logger.LogInfo("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            Main.Logger.LogInfo("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
            Main.Logger.LogInfo("# sites on Behemoth scenic route " + TFTVAirCombat.behemothScenicRoute.Count);
            Main.Logger.LogInfo("Behemoth target id number is " + TFTVAirCombat.behemothTarget);
            Main.Logger.LogInfo("Behemoth will wait for " + TFTVAirCombat.behemothWaitHours + " hours before moving");
            Main.Logger.LogInfo("Last time a Revenant was seen was on  " + myDate.Add(new TimeSpan(TFTVRevenant.daysRevenantLastSeen, 0, 0, 0)) + ", and now it is day " + myDate.Add(new TimeSpan(Controller.Timing.Now.TimeSpan.Ticks)));
            Main.Logger.LogInfo("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
            //  Main.Logger.LogInfo("LOTAGlobalReworkCheck is " + TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck);
            Main.Logger.LogInfo($"Bases under attack count {TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Count}");
            Main.Logger.LogInfo($"Infested Phoenix bases {TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Count}");
            Main.Logger.LogInfo($"Supplies from Pandas pending processing {TFTVCapturePandoransGeoscape.PandasForFoodProcessing}");
            Main.Logger.LogInfo($"Toxins in food {TFTVCapturePandoransGeoscape.ToxinsInCirculation}");
            Main.Logger.LogInfo($"Scylla count {TFTVPandoranProgress.ScyllaCount}");
            Main.Logger.LogInfo($"infested haven population save data {TFTVInfestationStory.HavenPopulation}");
            Main.Logger.LogInfo($"aircraft capacity {TFTVCapturePandorans.AircraftCaptureCapacity}");

            // Main.Logger.LogInfo($"New Difficulties implemented {TFTVReleaseOnly.NewDifficultiesImplemented}");
            //    Main.Logger.LogInfo($"Items currently available in Aircraft inventory {TFTVUI.CurrentlyAvailableInv.Values.Count}");
            //   Main.Logger.LogInfo($"Items currently hidden in Aircraft inventory {TFTVUI.CurrentlyAvailableInv.Values.Count}");
            //  
            TFTVLogger.Always("# Characters with broken limbs: " + TFTVStamina.charactersWithDisabledBodyParts.Count);
            TFTVLogger.Always("# Behemoth targets for this emergence: " + TFTVAirCombat.targetsForBehemoth.Count);
            //   TFTVLogger.Always("# Targets already hit by Behemoth on this emergence: " + TFTVAirCombat.targetsVisitedByBehemoth.Count);
            TFTVLogger.Always("# Pandoran flyers that have visited havens on this emergence:  " + TFTVAirCombat.flyersAndHavens.Count);
            TFTVLogger.Always("Hammerfall: " + TFTVAirCombat.checkHammerfall);
            TFTVLogger.Always("# Lost operatives: " + TFTVRevenant.DeadSoldiersDelirium.Count);
            TFTVLogger.Always("# sites on Behemoth scenic route " + TFTVAirCombat.behemothScenicRoute.Count);
            TFTVLogger.Always("Behemoth target id number is " + TFTVAirCombat.behemothTarget);
            TFTVLogger.Always("Behemoth will wait for another  " + TFTVAirCombat.behemothWaitHours + " before moving");
            TFTVLogger.Always("Last time a Revenant was seen was on  " + myDate.Add(new TimeSpan(TFTVRevenant.daysRevenantLastSeen, 0, 0, 0)) + ", and now it is day " + myDate.Add(new TimeSpan(Controller.Timing.Now.TimeSpan.Ticks)));
            TFTVLogger.Always("Project Osiris stats count " + TFTVRevenantResearch.ProjectOsirisStats.Count);
            //   TFTVLogger.Always("LOTAGlobalReworkCheck is " + TFTVBetaSaveGamesFixes.LOTAReworkGlobalCheck);
            TFTVLogger.Always($"Bases under attack count {TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Count}");
            TFTVLogger.Always($"Infested Phoenix bases {TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Count}");
            TFTVLogger.Always($"Supplies from Pandas pending processing {TFTVCapturePandoransGeoscape.PandasForFoodProcessing}");
            TFTVLogger.Always($"Toxins in food {TFTVCapturePandoransGeoscape.ToxinsInCirculation}");
            TFTVLogger.Always($"Scylla count {TFTVPandoranProgress.ScyllaCount}");
            //  TFTVLogger.Always($"New Difficulties implemented {TFTVReleaseOnly.NewDifficultiesImplemented}");

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
           

            List<int> locations = new List<int>() { 0, 1, 584, 170, 191, 167, 169, 166, 187, 172, 165, 192, 185, 171, 189, 168, 193, 190, 188 };

            /*
        Vanilla,
        Random, 
        Antarctica, 
        China,
        Australia,
        Honduras,
        Ethiopia,
        Ukraine,
        Greenland,
        Afghanistan,
        Algeria,
        Alaska,
        Quebec,
        Siberia,
        Zimbabwe,
        Bolivia,
        Argentina,
        Cambodia,
        Ghana*/


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
                    foreach (GeoSiteSceneDef.SiteInfo siteInfo in worldSites.Where(ws => ws.SiteTags.Any(t => t.Contains("PhoenixBase"))))
                    {
                        int index = (int)TFTVNewGameOptions.startingBaseLocation;
                      //  TFTVLogger.Always($"index is {index}");

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
                            TFTVLogger.Always($"{siteInfo.SiteId} world position is {siteInfo.WorldPosition}");
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
                                    //  TFTVLogger.Always($"{siteInfo.SiteId} has site tag removed");
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