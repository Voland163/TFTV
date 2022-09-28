using Base.Core;
using Base.Defs;
using Base.Levels;
using HarmonyLib;
using Newtonsoft.Json;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Entities;
using PRMBetterClasses;
using PRMBetterClasses.SkillModifications;
using PRMBetterClasses.VariousAdjustments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// This is the main mod class. Only one can exist per assembly.
    /// If no ModMain is detected in assembly, then no other classes/callbacks will be called.
    /// </summary>
    public class TFTVMain : ModMain
    {
      

        /// Old Modnix configuration now used to hold all settings
        public BCSettings Settings = new BCSettings();

        /// Name of config file for all settings
        public const string PRMBC_ConfigFileName = "PRM_BC_Config.json";

      
        /// Config is accessible at any time, if any is declared.
        public new TFTVConfig Config => (TFTVConfig)base.Config;

        public static TFTVMain Main { get; private set; }

     //   public static List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>();

        //TFTV Adding references to DefRepo and SharedData
        internal static readonly DefRepository Repo = GameUtl.GameComponent<DefRepository>();
        internal static readonly SharedData Shared = GameUtl.GameComponent<SharedData>();

        //TFTV We want at least the LogPath, but maybe other directories too...
        internal static string LogPath;
        internal static string ModDirectory;
        internal static string LocalizationDirectory;
        internal static string TexturesDirectory;

        // internal static bool injectionComplete = false;

        /// This property indicates if mod can be Safely Disabled from the game.
        /// Safely sisabled mods can be reenabled again. Unsafely disabled mods will need game restart ot take effect.
        /// Unsafely disabled mods usually cannot revert thier changes in OnModDisabled
        public override bool CanSafelyDisable => false;

        /// <summary>
        /// Callback for when mod is enabled. Called even on game starup.
        /// </summary>
        public override void OnModEnabled()
        {
            Main = this;
            /// All mod dependencies are accessible and always loaded.
            int c = Dependencies.Count();
            /// Metadata is whatever is written in meta.json
            string v = MetaData.Version.ToString();
            /// Game creates Harmony object for each mod. Accessible if needed.
            HarmonyLib.Harmony harmony = (HarmonyLib.Harmony)HarmonyInstance;
            /// Mod instance is mod's runtime representation in game.
            string id = Instance.ID;
            /// Game creates Game Object for each mod. 
            GameObject go = ModGO;
            /// PhoenixGame is accessible at any time.
            PhoenixGame game = GetGame();

            Logger.LogInfo("TFTV September 28 release #1");

            //BC stuff
            ApplyInGameConfig();
            ApplyDefChanges();
            //TFTV Stuff that needs to happen ASAP
            TFTVDefsCreatedOnLevelChanged.Create_VoidOmen_Events();
            //Medbay
            TFTVDefsCreatedOnLevelChanged.ChangesToMedbay();

            //Load changes to Defs, on the assumption that they will not degrade over time
            TFTVDefsCreatedOnLevelChanged.ModifyAmountResourcesEvents(Config.ResourceMultiplier);
            //Check if player chose to have more ambushes and crates, and if Void Omen changing Ambushes is not in play
            if (Config.MoreAmbushes)
            {
                TFTVAmbushes.Apply_Changes_Ambush_Missions();
            }
            //Creates events where factions upset because of augmentations
            TFTVAugmentations.ApplyChanges();
            //Changes to DLC events
            TFTVChangesToDLC1andDLC2Events.Apply_Changes();
            TFTVChangesToDLC3Events.ApplyChanges();
            TFTVChangesToDLC3Events.ModifyMaskedManticoreResearch();
            TFTVChangesToDLC4Events.Apply_Changes();
            if (Config.ActivateKERework)
            {
                TFTVChangesToDLC5Events.Apply_Changes();
            }
            //Sets bonus to damage from Delirium to 0
            TFTVDefsCreatedOnLevelChanged.RemoveCorruptionDamageBuff();
            //Modifies weapons/modules stats as per Belial's doc
            if (Config.ActivateAirCombatChanges)
            {
                TFTVDefsCreatedOnLevelChanged.ModifyAirCombatDefs();
            }
            //Doubles penalties from factions
            if (Config.DiplomaticPenalties)
            {
                TFTVDiplomacyPenalties.Apply_Changes();
            }
            //Applies changes to infestation mission/rewards
            TFTVInfestation.Apply_Infestation_Changes();
            //Pending config, modifes evo points per day depending on difficulty level, etc
            TFTVDefsCreatedOnLevelChanged.ModifyPandoranProgress();
            //Modifies air vehicles 
            TFTVDefsCreatedOnLevelChanged.ModifyDefsForPassengerModules();
            //HybernationModuleStaminaRecuperation, adjusts if selected in config       
            TFTVDefsCreatedOnLevelChanged.HibernationModuleStaminaRecuperation();
            //Makes reverse engineering grant access to underlying research. Pending unifying research cost of RE items.
            if (Config.ActivateReverseEngineeringResearch)
            {
                TFTVReverseEngineering.ModifyReverseEngineering();
            }
            //Changes when Umbra will appear
            TFTVDefsCreatedOnLevelChanged.ChangeUmbra();
            //Create Dtony's delirium perks
            TFTVDefsCreatedOnLevelChanged.CreateDeliriumPerks();
            //Modify Defs to introduce Alistair's events
            TFTVDefsCreatedOnLevelChanged.InjectAlistairAhsbyLines();
            //Create Revenant defs
            TFTVDefsCreatedOnLevelChanged.CreateRevenantDefs();
            //This creates the intro events when a new game is started
            TFTVDefsCreatedOnLevelChanged.CreateIntro();
            //Run all harmony patches; some patches have config flags


            TFTVTutorialAndStory.CreateHints();
            TFTVDefsCreatedOnLevelChanged.MistOnAllMissions();
            TFTVHumanEnemiesDefs.CreateHumanEnemiesTags();
            TFTVHumanEnemiesDefs.ModifyMissionDefsToReplaceNeutralWithBandit();
            TFTVHumanEnemiesDefs.CreateAmbushAbility();
            TFTVHumanEnemiesNames.CreateNamesDictionary();
            TFTVDefsCreatedOnLevelChanged.CreateNewDefsForTFTVStart();


            //TFTV 
            ModDirectory = Instance.Entry.Directory;
            //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //Path to localization CSVs
            LocalizationDirectory = Path.Combine(ModDirectory, "Assets", "Localization");
            //Texture Directory (for Dtony's DeliriumPerks)
            TexturesDirectory = Path.Combine(ModDirectory, "Assets", "Textures");
            // Initialize Logger
            LogPath = Path.Combine(ModDirectory, "TFTV.log");
            TFTVLogger.Initialize(LogPath, Config.Debug, ModDirectory, nameof(TFTV));
            PRMLogger.Initialize(LogPath, Settings.Debug, ModDirectory, nameof(PRMBetterClasses));

            TFTVLogger.Always("TFTV September 28 release #1");
            
            PRMBetterClasses.Helper.Initialize();
            // Initialize Helper
            Helper.Initialize();
            //This creates the Void Omen events
            
            harmony.PatchAll();

            // if (!injectionComplete)
            // {


            //       injectionComplete = true;
            //  }

        }

        /// Apply any general game modifications.


        /// <summary>
        /// Callback for when mod is disabled. This will be called even if mod cannot be safely disabled.
        /// Guaranteed to have OnModEnabled before.
        /// </summary>
        public override void OnModDisabled()
        {
            Main = null;

            /// Undo any game modifications if possible. Else "CanSafelyDisable" must be set to false.
            /// ModGO will be destroyed after OnModDisabled.
        }

        /// <summary>
        /// Callback for when any property from mod's config is changed.
        /// </summary>
        public override void OnConfigChanged()
        {
            ApplyInGameConfig();
            WeaponModifications.Change_Crossbows();

            /*
            if (Config.defaultSettings)
            {
                Config.InitialScavSites = 8;
                Config.ChancesScavCrates = TFTVConfig.ScavengingWeight.High;
                Config.ChancesScavSoldiers = TFTVConfig.ScavengingWeight.Low;
                Config.ChancesScavGroundVehicleRescue = TFTVConfig.ScavengingWeight.Low;
                Config.ResourceMultiplier = 0.8f;
                Config.DiplomaticPenalties = true;
                Config.StaminaPenaltyFromInjury = true;
                Config.StaminaPenaltyFromMutation = true;
                Config.StaminaPenaltyFromBionics = true;
                Config.MoreAmbushes = true;
                Config.ActivateStaminaRecuperatonModule = true;
                Config.ActivateReverseEngineeringResearch = true;
                Config.ActivateAirCombatChanges = true;
                Config.ActivateKERework = true;
                Config.HavenSOS = true;
                Config.Debug = true;

            }
            if (Config.InitialScavSites != 8 ||
               Config.ChancesScavCrates != TFTVConfig.ScavengingWeight.High ||
               Config.ChancesScavSoldiers != TFTVConfig.ScavengingWeight.Low ||
               Config.ChancesScavGroundVehicleRescue != TFTVConfig.ScavengingWeight.Low ||
            Config.ResourceMultiplier != 0.8f ||
            Config.DiplomaticPenalties != true ||
            Config.StaminaPenaltyFromInjury != true ||
            Config.StaminaPenaltyFromMutation != true ||
            Config.StaminaPenaltyFromBionics != true ||
            Config.MoreAmbushes != true ||
            Config.ActivateStaminaRecuperatonModule != true ||
            Config.ActivateReverseEngineeringResearch != true ||
            Config.ActivateAirCombatChanges != true ||
            Config.ActivateKERework != true ||
            Config.HavenSOS != true ||
            Config.Debug != true)
            {

                Config.defaultSettings = false;

            }
            */
            ApplyDefChanges();
            TFTVDefsCreatedOnLevelChanged.CreateNewDefsForTFTVStart();
          /*  if (Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.UNBUFFED) 
            {
               startingTemplates = TFTVStarts.SetInitialSquadUnbuffed();     
            }
            else if(Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.BUFFED) 
            {
                startingTemplates = TFTVStarts.SetInitialSquadBuffed();          
            }
            else if(Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.RANDOM) 
            {
                startingTemplates = TFTVStarts.SetInitialSquadRandom();          *
            }

            
            Harmony harmony = (Harmony)HarmonyInstance;
            //  injectionComplete = false;
            harmony.UnpatchAll();
            harmony.PatchAll();
            /*  
              UIModuleModManager uIModuleModManager = (UIModuleModManager)UnityEngine.Object.FindObjectOfType(typeof(UIModuleModManager));
              PhoenixGeneralButton activeModTab = uIModuleModManager.ModSettingsSections.First(pgb => pgb.IsSelected);
              MethodInfo SelectModSettings_Info = AccessTools.Method(typeof(UIModuleModManager), "SelectModSettings", new Type[] { typeof(PhoenixGeneralButton) });
              SelectModSettings_Info.Invoke(uIModuleModManager, new object[] { activeModTab });
            */
            /// Config is accessible at any time.
        }


        /// <summary>
        /// In Phoenix Point there can be only one active level at a time. 
        /// Levels go through different states (loading, unloaded, start, etc.).
        /// General puprose level state change callback.
        /// </summary>
        /// <param name="level">Level being changed.</param>
        /// <param name="prevState">Old state of the level.</param>
        /// <param name="state">New state of the level.</param>
        public override void OnLevelStateChanged(Level level, Level.State prevState, Level.State state)
        {
            //Level l = GetLevel();
                ApplyDefChanges();
                TFTVDefsCreatedOnLevelChanged.CreateNewDefsForTFTVStart();
            /*
                if (Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.UNBUFFED)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadUnbuffed();
                }
                else if (Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.BUFFED)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadBuffed();
                }
                else if (Config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.RANDOM)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadRandom();
                }*/

                // TFTVRevenantResearch.CreateDefs();
            
           
            /// Alternative way to access current level at any time.


        }

        /// <summary>
        /// Useful callback for when level is loaded, ready, and starts.
        /// Usually game setup is executed.
        /// </summary>
        /// <param name="level">Level that starts.</param>
        public override void OnLevelStart(Level level)
        {
            //Reinject Dtony's delirium perks, because assuming degradation will happen based on BetterClasses experience
            ApplyDefChanges();
            TFTVDefsCreatedOnLevelChanged.CreateDeliriumPerks();
            TFTVHumanEnemiesDefs.CreateAmbushAbility();
            TFTVDefsCreatedOnLevelChanged.CreateNewDefsForTFTVStart();
            
        }

        /// <summary>
        /// Useful callback for when level is ending, before unloading.
        /// Usually game cleanup is executed.
        /// </summary>
        /// <param name="level">Level that ends.</param>
        public override void OnLevelEnd(Level level)
        {
        }

        private void ApplyInGameConfig()
        {
            Settings.LearnFirstPersonalSkill = Config.LearnFirstPersonalSkill;
            Settings.DeactivateTacticalAutoStandby = Config.DeactivateTacticalAutoStandby;
            Settings.BaseCrossbow_Ammo = Config.BaseCrossbow_Ammo;
            Settings.VenomCrossbow_Ammo = Config.VenomCrossbow_Ammo;
        }

        /// <summary>
        /// Loads a config file from mod directory or creates a new default one if none exists
        /// </summary>
        /// <param name="modDirectory">Path to the config file</param>
        private void LoadSavedConfig(string modDirectory)
        {
            try
            {
                string configFilePath = Path.Combine(modDirectory, PRMBC_ConfigFileName);
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    Settings = JsonConvert.DeserializeObject<BCSettings>(json);
                }
                else
                {
                    string jsonString = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                    File.WriteAllText(configFilePath, jsonString);
                }
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
                Logger.LogError($"{MethodBase.GetCurrentMethod().Name} ERROR: ", e);
            }
        }

        /// <summary>
        /// Applies all changes to Definables (Defs), uses RunTimeDefs and for this needs to be refreshed
        /// </summary>
        public void ApplyDefChanges()
        {
            // Apply skill modifications
            SkillModsMain.ApplyChanges();

            // Generate the main specialization as configured
            MainSpecModification.GenerateMainSpec();

            // Apply story rework changes (Voland)
            //if (Config.ActivateStoryRework)
            //{
            //	StoryReworkMain.ApplyChanges();
            //}

            // Apply various adjustments
            VariousAdjustmentsMain.ApplyChanges();
        }

    }
}