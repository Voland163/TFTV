using Base.Core;
using Base.Defs;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Modding;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// This is the main mod class. Only one can exist per assembly.
    /// If no ModMain is detected in assembly, then no other classes/callbacks will be called.
    /// </summary>
    public class TFTVMain : ModMain
    {
        /// Config is accessible at any time, if any is declared.
        public new TFTVConfig Config => (TFTVConfig)base.Config;

        public static TFTVMain Main { get; private set; }


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

            Logger.LogInfo("TFTV September 14 midnight release #1");


            //TFTV 
            ModDirectory = Instance.Entry.Directory;
            //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //Path to localization CSVs
            LocalizationDirectory = Path.Combine(ModDirectory); //, "Assets", "Localization"
            //Texture Directory (for Dtony's DeliriumPerks)
            TexturesDirectory = Path.Combine(ModDirectory); //, "Assets", "Textures");
            // Initialize Logger
            LogPath = Path.Combine(ModDirectory, "TFTV.log");
            TFTVLogger.Initialize(LogPath, Config.Debug, ModDirectory, nameof(TFTV));
            TFTVLogger.Always("TFTV September 14 midnight release #1");
            // Initialize Helper
            Helper.Initialize();



            // if (!injectionComplete)
            // {

            //TFTV Stuff that needs to happen ASAP

            //This creates the Void Omen events
            TFTVVoidOmens.Create_VoidOmen_Events();

            //Medbay
            TFTVSmallChanges.ChangesToMedbay();

            //Load changes to Defs, on the assumption that they will not degrade over time
            TFTVResources.Apply_Changes(Config.ResourceMultiplier);
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
            TFTVChangesToDLC4Events.Apply_Changes();
            if (Config.ActivateKERework)
            {
                TFTVChangesToDLC5Events.Apply_Changes();
            }
            //Sets bonus to damage from Delirium to 0
            TFTVDelirium.ApplyChanges();
            //Modifies weapons/modules stats as per Belial's doc
            if (Config.ActivateAirCombatChanges)
            {
                TFTVAirCombat.ModifyAirCombatDefs();
            }
            //Doubles penalties from factions
            if (Config.DiplomaticPenalties)
            {
                TFTVDiplomacyPenalties.Apply_Changes();
            }
            //Applies changes to infestation mission/rewards
            TFTVInfestation.Apply_Infestation_Changes();
            //Pending config, modifes evo points per day depending on difficulty level, etc
            TFTVPandoranProgress.Apply_Changes();
            //Modifies air vehicles 
            TFTVPassengerModules.Apply_Changes();
            //HybernationModuleStaminaRecuperation, adjusts if selected in config       
            TFTVPassengerModules.HibernationModuleStaminaRecuperation();
            //Makes reverse engineering grant access to underlying research. Pending unifying research cost of RE items.
            if (Config.ActivateReverseEngineeringResearch)
            {
                TFTVReverseEngineering.Apply_Changes();
            }
            //Changes when Umbra will appear
            TFTVUmbra.ChangeUmbra();
            //Create Dtony's delirium perks
            TFTVDeliriumPerks.Main();
            //Modify Defs to introduce Alistair's events
            TFTVNewPXCharacters.InjectAlistairAhsbyLines();
            //Create Revenant defs
            TFTVRevenant.CreateRevenantDefs();
            //This creates the intro events when a new game is started
            TFTVNewPXCharacters.CreateIntro();
            //Run all harmony patches; some patches have config flags

            TFTVStarts.CreateNewDefsForTFTVStart();
            TFTVTutorialAndStory.CreateHints();
            TFTVSmallChanges.MistOnAllMissions();
            TFTVHumanEnemiesDefs.CreateHumanEnemiesTags();
            TFTVHumanEnemiesDefs.ModifyMissionDefsToReplaceNeutralWithBandit();
            TFTVHumanEnemiesDefs.CreateAmbushAbility();
            TFTVHumanEnemiesNames.CreateNamesDictionary();
            
             
            // TFTVRevenantResearch.CreateDefs();

            harmony.PatchAll();
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
           

            /// Alternative way to access current level at any time.
            Level l = GetLevel();

        }

        /// <summary>
        /// Useful callback for when level is loaded, ready, and starts.
        /// Usually game setup is executed.
        /// </summary>
        /// <param name="level">Level that starts.</param>
        public override void OnLevelStart(Level level)
        {
            //Reinject Dtony's delirium perks, because assuming degradation will happen based on BetterClasses experience
            TFTVDeliriumPerks.Main();
            TFTVHumanEnemiesDefs.CreateAmbushAbility();


        }

        /// <summary>
        /// Useful callback for when level is ending, before unloading.
        /// Usually game cleanup is executed.
        /// </summary>
        /// <param name="level">Level that ends.</param>
        public override void OnLevelEnd(Level level)
        {
        }
    }
}