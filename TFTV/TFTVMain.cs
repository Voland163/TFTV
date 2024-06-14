using Base.Build;
using Base.Core;
using Base.Defs;
using Base.Levels;
using HarmonyLib;
using Microsoft.CSharp;
using Newtonsoft.Json;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Modding;
using PRMBetterClasses;
using PRMBetterClasses.SkillModifications;
using PRMBetterClasses.VariousAdjustments;
using System;
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

        public DefCache DefCache = new DefCache();
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

        internal static bool ReleaseStuffCreated = false;

        //TFTV We want at least the LogPath, but maybe other directories too...
        internal static string LogPath;
        internal static string ModDirectory;
        internal static string LocalizationDirectory;
        internal static string TexturesDirectory;

        internal static string TFTVversion;

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
            try
            {
                Main = this;
                /// All mod dependencies are accessible and always loaded.
                int c = Dependencies.Count();
                /// Metadata is whatever is written in meta.json
                string v = MetaData.Version.ToString();
                /// Game creates Harmony object for each mod. Accessible if needed.
                Harmony harmony = (Harmony)HarmonyInstance;
                /// Mod instance is mod's runtime representation in game.
                string id = Instance.ID;
                /// Game creates Game Object for each mod. 
                GameObject go = ModGO;
                /// PhoenixGame is accessible at any time.
                PhoenixGame game = GetGame();

                string version = $"TFTV 1.0, hotfix 1 patch 6 20240614 release #2 v{MetaData.Version}";

                TFTVversion = version;

                Logger.LogInfo(version);

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
                // DefCache.Initialize();
                TFTVLogger.Always(version);

                PRMBetterClasses.Helper.Initialize();
                // Initialize Helper
                Helper.Initialize();
                //This creates the Void Omen events

                //BC stuff
                Logger.LogInfo("BC stuff loading");
                //  BCApplyInGameConfig();
                BCApplyDefChanges();
                Logger.LogInfo("BC stuff loaded");
                //TFTV 
                Logger.LogInfo("TFTV stuff loading");
                TFTVDefsInjectedOnlyOnce.InjectDefsInjectedOnlyOnceBatch1();
                Logger.LogInfo("First batch of Defs injected");
                TFTVDefsRequiringReinjection.InjectDefsInjectedOnlyOnceBatch2();
                Logger.LogInfo("Second batch of Defs injected");

                TFTVHumanEnemiesNames.CreateNamesDictionary();
                Logger.LogInfo("Names for human enemies created");
                TFTVHumanEnemiesNames.CreateRanksDictionary();
                Logger.LogInfo("Ranks for human enemies created");
                TFTVHumanEnemiesNames.CreateTierDescriptions();
                TFTVAAAgendaTracker.ExtendedAgendaTracker.LocalizeExtendedAgendaUI();

                Logger.LogInfo("Loading Vehicle Rework");
                TFTVVehicleRework.VehiclesMain.ReworkVehicles(this);
                Logger.LogInfo("Vehicle Rework Loaded");


                TFTVRevenantResearch.CreateRevenantRewardsDefs();
                TFTVProjectOsiris.Defs.CreateProjectOsirisDefs();
                //  NoSecondChances.ImplementNoSecondChances();
                //  TFTVAncients.CheckResearchesRequiringThings();

                Config.PopulateConfigFields();
                //  Config.RetrieveConfigOptions();
                harmony.PatchAll();
                TFTVVanillaFixes.FixSurveillanceAbilityGroundMarker(harmony);
                

                if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<HomeScreenView>() != null)
                {
                    TFTVLogger.Always($"enabling TFTV for the first time!");
                    HomeScreenView homeScreenView = GameUtl.CurrentLevel().GetComponent<HomeScreenView>();

                    homeScreenView.EditionVisualsController.SwitchToVanillaVisuals();
                    UIModuleGameplayOptionsPanel uIModuleGameplayOptionsPanel = homeScreenView.CommonModules.PauseScreenModule.OptionsSubmenuModule.GameplayOptionsPanel;
                    OptionsManager optionsManager = uIModuleGameplayOptionsPanel.OptionsManager;

                    if (!optionsManager.CurrentGameplayOptions.EnableContextHelpHints)
                    {
                        TFTVLogger.Always($"Context hints were off! Setting context hints on!");
                        FieldInfo enableContextHelpHints = typeof(UIModuleGameplayOptionsPanel).GetField("_currentOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                        optionsManager.OptionsComponent.Options.Set(optionsManager.OptionsManagerDef.EnableContextHelpHintsKey, true);
                        OptionsManager.GameplayOptions gameplayOptions = (OptionsManager.GameplayOptions)enableContextHelpHints.GetValue(uIModuleGameplayOptionsPanel);
                        gameplayOptions.EnableContextHelpHints = true;
                        enableContextHelpHints.SetValue(uIModuleGameplayOptionsPanel, gameplayOptions);
                    }
                    TFTVNewGameMenu.TitleScreen.SetTFTVLogo(homeScreenView);
                }


              

                    /*     Type renderingEnvironmentType = typeof(RenderingEnvironment);

                         // Get all public constructors
                         ConstructorInfo[] constructors = renderingEnvironmentType.GetConstructors();

                         // Print the names of constructors
                         foreach (ConstructorInfo constructor in constructors)
                         {
                             TFTVLogger.Always("Constructor Name: " + constructor.FullDescription());
                         }*/



                    /*  if(GetLevel()!=null && GetLevel().name.Contains("HomeScreenLevel")) 
                      {
                          TFTVLogger.Always($"TFTV is enabled!");
                          string warning = $"Terror from the Void is now enabled! PLEASE QUIT TO DESKTOP BEFORE STARTING OR LOADING A GAME";

                          GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);


                      }*/
                    // if (!injectionComplete)
                    // {


                    //       injectionComplete = true;
                    //  }
                }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

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


            // Config.RetrieveConfigOptions();
            //  TFTVLogger.Always($"Config changed. Skip movies is now {Config.SkipMovies}");



            //  BCApplyInGameConfig();
            //    BCApplyDefChanges();
            //  WeaponModifications.Change_Crossbows();




            /*   if (Config.defaultSettings)
               {

                 //  Config.OverrideRookieDifficultySettings = false;
                 //  Config.EasyTactical = false;
                   Config.EasyGeoscape = false;
                   Config.EtermesMode = false;
                   Config.MoreMistVO = true;
                   Config.SkipMovies = false;
                //   Config.amountOfExoticResources = 1f;
                //   Config.impossibleWeaponsAdjustments = true;
                 //  Config.startingSquad = StartingSquadFaction.PHOENIX;
                 //  Config.startingBaseLocation = StartingBaseLocation.Vanilla;
                 //  Config.tutorialCharacters = StartingSquadCharacters.UNBUFFED;
                 //  Config.InitialScavSites = 8;
                 //  Config.ChancesScavCrates = TFTVConfig.ScavengingWeight.High;
                 //  Config.ChancesScavSoldiers = TFTVConfig.ScavengingWeight.Low;
                 //  Config.ChancesScavGroundVehicleRescue = TFTVConfig.ScavengingWeight.Low;
                //   Config.ResourceMultiplier = 1f;
                 //  Config.DiplomaticPenalties = true;
                 //  Config.StaminaPenaltyFromInjury = true;
                  // Config.StaminaPenaltyFromMutation = true;
                 //  Config.StaminaPenaltyFromBionics = true;
               //    Config.MoreAmbushes = true;
                   Config.ActivateStaminaRecuperatonModule = true;
                 //  Config.ActivateReverseEngineeringResearch = true;
                   Config.HavenSOS = true;
                   Config.Debug = true;
                   Config.EqualizeTrade = true;
               //    Config.LimitedCapture = true;
               //    Config.LimitedHarvesting = true;
                   Config.LimitedRaiding = true;
                   Config.ReinforcementsNoDrops = true;
           // Config.ShowFaces = true;

       }
               if (
               Config.EasyGeoscape != false||
               Config.EtermesMode != false ||
               Config.MoreMistVO != true ||
               Config.SkipMovies != false ||
              // Config.amountOfExoticResources != 1f ||
             //  Config.impossibleWeaponsAdjustments != true ||             
            //   Config.ResourceMultiplier != 1f ||
            //   Config.DiplomaticPenalties != true ||
            //   Config.StaminaPenaltyFromInjury != true ||          
           //    Config.MoreAmbushes != true ||
               Config.ActivateStaminaRecuperatonModule != true ||
             //  Config.ActivateReverseEngineeringResearch != true ||
               Config.HavenSOS != true ||
               Config.Debug != true ||
                   Config.EqualizeTrade != true ||

               Config.LimitedRaiding != true ||
               Config.ReinforcementsNoDrops != true)
               //   Config.ShowFaces!=true)
               {

                   Config.defaultSettings = false;

               }
            */
            /*   Harmony harmony = (Harmony)HarmonyInstance;
               //  injectionComplete = false;
               harmony.UnpatchAll();
               harmony.PatchAll();*/
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


            // TFTVLogger.Always($"level {level.name} loading");


            // Logger.LogInfo($"{MethodBase.GetCurrentMethod().Name} called for level '{level}' with old state '{prevState}' and new state '{state}'");
            /*  if (!ConfigImplemented && (level.name.Contains("GeoscapeLevel") || level.name.Contains("TacticalLevel")) && state == Level.State.Loading)
              {
                  TFTVLogger.Always($"level {level.name} loading");

                  TFTVDefsWithConfigDependency.ImplementConfigChoices();
                  ConfigImplemented = true;
              }*/




            /// Alternative way to access current level at any time.
            //Level l = GetLevel();

            /// Alternative way to access current level at any time.
        }

        /// <summary>
        /// Useful callback for when level is loaded, ready, and starts.
        /// Usually game setup is executed.
        /// </summary>
        /// <param name="level">Level that starts.</param>
        public override void OnLevelStart(Level level)
        {
            // TFTVLogger.Always($"level is {level.name}");

        }

        /// <summary>
        /// Useful callback for when level is ending, before unloading.
        /// Usually game cleanup is executed.
        /// </summary>
        /// <param name="level">Level that ends.</param>
        public override void OnLevelEnd(Level level)
        {
            if (level.name.Contains("HomeScreen"))
            {
                Logger.LogInfo($"{MethodBase.GetCurrentMethod().Name} called for level '{level}'; harmony re-patching everything in case config changed");

                Harmony harmony = (Harmony)HarmonyInstance;
                harmony.UnpatchAll(harmony.Id);
                harmony.PatchAll();
                TFTVVanillaFixes.FixSurveillanceAbilityGroundMarker(harmony);
            }
        }

        private void BCApplyInGameConfig()
        {
            // Settings.LearnFirstPersonalSkill = Config.LearnFirstPersonalSkill;
            //  Settings.DeactivateTacticalAutoStandby = Config.DeactivateTacticalAutoStandby;
            //  Settings.BaseCrossbow_Ammo = Config.BaseCrossbow_Ammo;
            //  Settings.VenomCrossbow_Ammo = Config.VenomCrossbow_Ammo;
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
        public void BCApplyDefChanges()
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



        [HarmonyPatch(typeof(UIModuleBuildRevision), "SetRevisionNumber")]
        internal static class UIModuleBuildRevision_SetRevisionNumber
        {
            private static void Postfix(UIModuleBuildRevision __instance)
            {
                __instance.BuildRevisionNumber.text = $"{RuntimeBuildInfo.UserVersion} w/{TFTVMain.TFTVversion} ";
            }
        }
    }
}