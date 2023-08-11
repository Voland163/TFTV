using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Modding;
using System;

namespace TFTV
{
    /// <summary>
    /// ModConfig is mod settings that players can change from within the game.
    /// Config is only editable from players in main menu.
    /// Only one config can exist per mod assembly.
    /// Config is serialized on disk as json.
    /// </summary>

    public class TFTVConfig : ModConfig
    {



        [HarmonyPatch(typeof(ModSettingController), "ApplyModification")]
        public static class ModSettingController_ApplyModification_Patch
        {
            private static void Postfix(ModSettingController __instance)
            {
                try
                {

                    if ((TFTVDefsWithConfigDependency.ChangesToCapturingPandoransImplemented && __instance.Label.text == "CAPTURING PANDORANS IS LIMITED")
                        || (TFTVBetterEnemies.StrongerPandoransImplemented && __instance.Label.text == "MAKE PANDORANS STRONGER") ||
                       (TFTVDefsWithConfigDependency.ChangesToFoodAndMutagenGenerationImplemented && __instance.Label.text == "LIMITS ON RENDERING PANDORANS FOR FOOD OR MUTAGENS"))
                    {
                        string warning = $"Previous setting for {__instance.Label.text} has already been implemetend on starting or a loading a game! PLEASE QUIT TO DESKTOP BEFORE STARTING OR LOADING A GAME";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                    }


                    if (__instance.Label.text == "DISABLE SAVING ON TACTICAL")
                    {
                        string warning = $"Saving and loading on Tactical can result in odd behavior and bugs (Vanilla issues). It is recommended to save only on Geoscape (and use several saves, in case one of them gets corrupted). And, you know what... losing soldiers in TFTV is fun :)";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                    }



                    /*   if (__instance.Label.text== "MANUALLY OVERRIDE SETTINGS CHOSEN ON GAME START")
                       {
                           string warning = $"";

                           GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                       }*/


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(ModSettingController), "Init")]
        public static class ModSettingController_Init_Patch
        {
            private static void Postfix(ModSettingController __instance, string label)
            {
                try
                {

                    if (label == "OVERRIDE ROOKIE DIFFICULTY SETTINGS" ||
                        label == "I AM ETERMES" ||
                        label == "EASY GEOSCAPE" ||
                        label == "New ambush" ||
                        label == "Stamina drained on injury" ||
                        label == "Higher diplomatic penalties" ||
                        label == "Amount of resources gained in events" ||
                        label == "IMPOSSIBLE WEAPONS ADJUSTMENTS" ||
                        label == "AMOUNT OF EXOTIC RESOURCES" ||
                        label == "MAKE PANDORANS STRONGER" ||
                        label == "CAPTURING PANDORANS IS LIMITED" ||
                        label == "LIMITS ON RENDERING PANDORANS FOR FOOD OR MUTAGENS" ||
                        label == "Eros ammo" ||
                        label == "Psyche ammo")
                    {
                        __instance.TextField.gameObject.SetActive(value: false);
                        __instance.ToggleField.gameObject.SetActive(value: false);
                        __instance.ListField.gameObject.SetActive(value: false);
                        __instance.SliderFormatter.gameObject.SetActive(value: false);
                        __instance.Label.gameObject.SetActive(false);
                        __instance.gameObject.SetActive(false);

                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        //Default settings
        /*   [ConfigField(text: "MANUALLY OVERRIDE SETTINGS CHOSEN ON GAME START",
             description: "You can customize any and all settings when you start a new game, on the new game screen. You can override most of these settings here (except those that only concern game start). ")]
           public bool overrideStartSettings = false;*/

        [ConfigField(text: "SKIP MOVIES",
            description: "Choose whether to skip Logos on game launch, Intro and Landing cinematics. Adapted from Mad's Assorted Adjustments.")]
        public bool SkipMovies = false;


        [ConfigField(text: "NO PROFIT FROM TRADING",
    description: "Trade is always 1 tech for 5 food or 5 materials, so no profit can be made from trading. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public bool EqualizeTrade = true;

        [ConfigField(text: "LIMITED RAIDING",
description: "After a raid, all faction havens are immediately set to highest alert and may not be raided in the next 7 days. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public bool LimitedRaiding = true;


        [ConfigField(text: "DISABLE SAVING ON TACTICAL",
        description: "You can still restart the mission though.")]
        public bool disableSavingOnTactical = true;

        public enum DifficultyOnTactical
        {
            GEOSCAPE, STORY, ROOKIE, VETERAN, HERO, LEGEND, ETERMES
        }

        [ConfigField(text: "DIFFICULTY ON TACTICAL",
        description: "You can choose a different difficulty setting for the tactical portion of the game at any time. Some changes will not take effect during a mission in progress. AFTER CHANGING THIS SETTING, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public DifficultyOnTactical difficultyOnTactical = DifficultyOnTactical.GEOSCAPE;

        [ConfigField(text: "ANIMATIONS CONTINUE DURING SHOOTING (FLINCHING)",
         description: "The characters will continue to animate during shooting sequences and targets that are hit may flinch, " +
         "causing subsequent shots in a burst to miss when shooting in freeaim mode.")]
        public bool AnimateWhileShooting = false;

        [ConfigField(text: "NO ITEM DROPS FROM REINFORCEMENTS",
description: "Enemy reinforcements do not drop items on death; disallows farming for weapons on missions with infinite reinforcements.  IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public bool ReinforcementsNoDrops = true;


        [ConfigField(text: "PLAY WITH MORE MIST VOID OMEN",
           description: "If you are playing on a Low-end system and experience lag with this Void Omen, you can turn it off here. This will prevent it from rolling" +
           " and if already rolled, will prevent it from having any effect.")]
        public bool MoreMistVO = true;

        [ConfigField(text: "Stamina recuperation from FAR-M",
         description: "The starting type of passenger module, FAR-M, will slowly recuperate the stamina of the operatives on board.\n" +
            "Switch off if you prefer to have to return to base more often")]
        public bool ActivateStaminaRecuperatonModule = true;


        [ConfigField(text: "Havens under attack revealed",
     description: "Havens under attack will send an SOS, revealing their location to the player.")]
        public bool HavenSOS = true;

     

        [ConfigField(text: "Learn the first personal skill",
           description: "If enabled, the first personal skill (level 1) is set right after a character is created (starting soldiers, new recruits on haven, rewards ect)")]
        public bool LearnFirstPersonalSkill = true;

        // Deactivate auto standy in tactical missions
        [ConfigField(text: "Deactivate tactical auto standby",
            description: "Disables or enables the vanilla behavior of automatically putting an actor into standby mode and thus switching to the next actor when all AP are used.\n"
                         + "ATTENTION: This function is WIP, i.e. currently still experimental!")]
        public bool DeactivateTacticalAutoStandby = false;


        //If set to 1, shows when any error ocurrs. Do not change unless you know what you are doing.
        [ConfigField(text: "Debug log & messages",
       description: "Shows when any error ocurrs. Please, do not change unless you know what you are doing.")]
        public bool Debug = true;

        //Default settings
        /*     [ConfigField(text: "DEFAULT TFTV SETTINGS",
                 description: "Sets all settings to default, to provide the Terror from the Void experience as envisioned by its creators.")]
             public bool defaultSettings = false;*/


        [ConfigField(text: "CAPTURING PANDORANS IS LIMITED",
  description: "There is a limit to how many Pandorans you can capture per mission. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public bool LimitedCapture = true;

        [ConfigField(text: "LIMITS ON RENDERING PANDORANS FOR FOOD OR MUTAGENS",
 description: "New mechanics make obtaining food or mutagens from captured Pandorans harder. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
        public bool LimitedHarvesting = true;

     

      
       

     

        //BetterEnemies
        [ConfigField(text: "MAKE PANDORANS STRONGER",
       description: "Applies the changes from Dtony BetterEnemies that make Pandorans more of a challenge.")]
        public bool StrongerPandorans = false;


       

       

        //New LOTA settings
        [ConfigField(text: "AMOUNT OF EXOTIC RESOURCES",
         description: "Choose the amount of Exotic Resources you want to have in your game per playthrough. Each unit provides enough resources to manufacture one set of Impossible Weapons. " +
          "So, if you want to have two full sets, set this number to 2, and so on. By default, this is set by the difficulty level: 2.5 on Story Mode and on Rookie, 2 on Veteran, 1.5 on Hero, 1 on Legend and 0.5 on Etermes. " +
          "Need to restart the game for the changes to take effect.")]
        public float amountOfExoticResources = 1f;


        [ConfigField(text: "IMPOSSIBLE WEAPONS ADJUSTMENTS", description: "In TFTV, Ancient Weapons are replaced by the Impossible Weapons (IW) " +
            "counterparts. They have different functionality (read: they are nerfed) " +
            "and some of them require additional faction research.  " +
            "Check this option off to keep Impossible Weapons with the same stats and functionality as Ancient Weapons in Vanilla and without requiring additional faction research. Set to false by default on Rookie.")]
        public bool impossibleWeaponsAdjustments = true;


        // Determines amount of resources gained in Events. 1f = 100% if Azazoth level.
        // 0.8f = 80% of Azazoth = Pre-Azazoth level (default on BetterGeoscape).
        // For example, to double amount of resources from current Vanilla (Azazoth level), change to 2f 
        // Can be applied to game in progress
        [ConfigField(text: "Amount of resources gained in events",
           description: "TFTV adjusts the amount of resources gained in Events by difficulty level. Please be aware that TFTV 100% refers to Vanilla Pre-Azazoth patch levels (so it's actually 80% of current Vanilla amount).")] //done
        public float ResourceMultiplier = 1f;

        // Changing the settings below will make the game easier:

        // Determines if diplomatic penalties are applied when cozying up to one of the factions by the two other factions
        // Can be applied to game in progress
        [ConfigField(text: "Higher diplomatic penalties",
           description: "Diplomatic penalties from choices in events are doubled and revealing diplomatic missions for one faction gives a diplomatic penalty with the other factions.\n" +
                        "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
        public bool DiplomaticPenalties = true;


        // If set to false, a disabled limb in tactical will not set character's Stamina to zero in geo
        [ConfigField(text: "Stamina drained on injury",
           description: "The stamina of any operative that sustains an injury in combat that results in a disabled body part will be set to zero after the mission.\n" +
            "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
        public bool StaminaPenaltyFromInjury = true;

        /*   // If set to false, applying a mutation will not set character's Stamina to zero
           [ConfigField(text: "Stamina drained on mutation",
             description: "The stamina of any operative that undergoes a mutation will be set to zero.\n" +
              "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
           public bool StaminaPenaltyFromMutation = true;

           // If set to false, adding a bionic will not set character's Stamina to zero
           [ConfigField(text: "Stamina drained on bionic augmentation",
             description: "The stamina of any operative that undergoes a bionic augmentation will be set to zero.\n" +
              "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
           public bool StaminaPenaltyFromBionics = true;*/

        // If set to false, ambushes will happen as rarely as in Vanilla, and will not have crates in them
        [ConfigField(text: "New ambush",
          description: "Ambushes will happen more often and will be harder. Regardless of this setting, all ambushes will have crates in them.\n" + "Set to false on Rookie by default")] //done
        public bool MoreAmbushes = true;

        // Changing the settings below will make the game harder:

        // If set to true, the passenger module FAR-M will no longer regenerate Stamina in flight
        // For players who prefer having to come back to base more often
        

        /* // If set to true reversing engineering an item allows to research the faction technology that allows manufacturing the item 
         [ConfigField(text: "Enhanced Reverse Engineering",
         description: "Reversing engineering an item allows to research the faction technology that allows manufacturing the item. IF YOU CHANGE THIS SETTING, QUIT THE GAME TO DESKTOP AND RESTART FOR CHANGES TO TAKE EFFECT")]
         public bool ActivateReverseEngineeringResearch = true;*/

        // Below are advanced settings. The mod was designed to be played with all of them turned on

        /*     //If set to true, modifes stats of some weapons and modules, and adds random chance that weapon will be lost when disengaging
             [ConfigField(text: "Changes to air combat",
            description: "Modifes stats of some weapons and modules.")]
             public bool ActivateAirCombatChanges = true;

             // If set to true activates DLC5 Kaos Engines story rework (in progress)
             [ConfigField(text: "Changes to DLC5 Marketplace (in progress)",
            description: "Removes cutscenes and missions, all items available at lowest prices 24 hours after discovering Marketplace.")]
             public bool ActivateKERework = true;*/

        // If set to true, unrevealed havens will be revealed when attacked
      

        // Infiltrator Crossbow Ammo changes
        [ConfigField(text: "Eros ammo",
            description: "Set the amount of bolts for the magazine of the base crossbows (Eros CRB III)")]
        public int BaseCrossbow_Ammo = 3;
        [ConfigField(text: "Psyche ammo",
            description: "Set the amount of bolts for the magazine of the advanced crossbow (Psyche CRB IV)")]
        public int VenomCrossbow_Ammo = 3;

        [ConfigField(text: "EASY GEOSCAPE",
        description: "All diplo rewards, resource rewards from missions, research output are doubled, and all diplo penalties are halved. " +
          " Set to true on Rookie by default.")]
        public bool EasyGeoscape = false;

        [ConfigField(text: "I AM ETERMES",
                  description: "YOU ARE ETERMES... and everything is always too easy for you")]
        public bool EtermesMode = false;

        [ConfigField(text: "OVERRIDE ROOKIE DIFFICULTY SETTINGS",
          description: "Certain config settings are set by default to a certain level for Rookie (see each config option for details). If you want to override them, check this box.")]
        public bool OverrideRookieDifficultySettings = false;

        // Always show helmets
        /*  [ConfigField(text: "Operatives will appear without helmets in the personnel screen",
              description: "Turn off if you don't like to see the faces of the individuals you are sending to be clawed and devoured by horrifying monstrosities")]
          public bool ShowFaces = true;*/


    }
}
