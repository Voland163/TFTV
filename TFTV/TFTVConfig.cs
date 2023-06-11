using PhoenixPoint.Modding;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

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
        //Default settings
        [ConfigField(text: "DEFAULT TFTV SETTINGS",
            description: "Sets all settings to default, to provide the Terror from the Void experience as envisioned by its creators.")]
        public bool defaultSettings = false;

        [ConfigField(text: "ANIMATIONS CONTINUE DURING SHOOTING (FLINCHING)",
            description: "The characters will continue to animate during shooting sequences and targets that are hit may flinch, " +
            "causing subsequent shots in a burst to miss when shooting in freeaim mode.")]
        public bool AnimateWhileShooting = false;



        //BetterEnemies
        [ConfigField(text: "MAKE PANDORANS STRONGER",
       description: "Applies the changes from Dtony BetterEnemies that make Pandorans more of a challenge.")]
        public bool BetterEnemiesOn = false;

        [ConfigField(text: "OVERRIDE ROOKIE DIFFICULTY SETTINGS",
          description: "Certain config settings are set by default to a certain level for Rookie (see each config option for details). If you want to override them, check this box.")]
        public bool OverrideRookieDifficultySettings = false;

        [ConfigField(text: "EASY TACTICAL",
          description: "All enemies gain a special trait increasing damage done to them by 50%, Pandorans never have more than 20 armor, Scylla and Node have less HP. " +
            "All Phoenix operatives gain a special trait increasing their damage resistance by 50%. Set to true on Rookie by default.")]
        public bool EasyTactical = false;

        [ConfigField(text: "EASY GEOSCAPE",
          description: "All diplo rewards, resource rewards from missions, research output are doubled, and all diplo penalties are halved. " +
            " Set to true on Rookie by default.")]
        public bool EasyGeoscape = false;

        [ConfigField(text: "I AM ETERMES",
                  description: "YOU ARE ETERMES... and everything is always too easy for you")]
        public bool EtermesMode = false;

        [ConfigField(text: "PLAY WITH MORE MIST VOID OMEN",
            description: "If you are playing on a Low-end system and experience lag with this Void Omen, you can turn it off here. This will prevent it from rolling" +
            " and if already rolled, will prevent it from having any effect.")]
        public bool MoreMistVO = true;

        [ConfigField(text: "SKIP MOVIES",
             description: "Choose whether to skip Logos on game launch, Intro and Landing cinematics. Adapted from Mad's Assorted Adjustments.")]
        public bool SkipMovies = false;

        //New LOTA settings
        [ConfigField(text: "AMOUNT OF EXOTIC RESOURCES",
         description: "Choose the amount of Exotic Resources you want to have in your game per playthrough. Each unit provides enough resources to manufacture one set of Impossible Weapons. " +
          "So, if you want to have two full sets, set this number to 2, and so on. By default, this is set by the difficulty level: 2.5 on Rookie, 2 on Veteran, 1.5 on Hero, 1 on Legend. " +
          "Need to restart the game for the changes to take effect.")]
        public float amountOfExoticResources = 1f;

        [ConfigField(text: "IMPOSSIBLE WEAPONS ADJUSTMENTS", description: "In TFTV, Ancient Weapons are replaced by the Impossible Weapons (IW) " +
            "counterparts. They have different functionality (read: they are nerfed) " +
            "and some of them require additional faction research.  " +
            "Check this option off to keep Impossible Weapons with the same stats and functionality as Ancient Weapons in Vanilla and without requiring additional faction research. Set to false by default on Rookie.")]
        public bool impossibleWeaponsAdjustments = true;

        //Starting squad

        public enum StartingSquadFaction
        {
            PHOENIX, ANU, NJ, SYNEDRION
        }
        [ConfigField(text: "Starting squad",
         description: "You can choose a different starting squad. If you do, one of your Assaults and your starting Heavy on Legend and Hero, " +
            "Assault on Veteran, or Sniper on Rookie will be replaced by an operative of the elite class of the Faction of your choice. " +
            "You will also get the corresponding faction technology once the faction researches it.")]
        public StartingSquadFaction startingSquad = StartingSquadFaction.PHOENIX;

 
        public enum StartingBaseLocation
        {
            [Description("Vanilla selection")]
            Vanilla,

            [Description("Random")]
            Random,

            [Description("North Africa (Algeria)")]
            Algeria,

            [Description("Eastern Europe (Ukraine)")]
            Ukraine,

            [Description("Central America (Mexico)")]
            Mexico,

            [Description("South America (Bolivia)")]
            Bolivia,

            [Description("East Africa (Ethiopia)")]
            Ethiopia,

            [Description("Asia (China)")]
            China,

            [Description("Northern Asia (Siberia)")]
            Siberia,

            [Description("Middle East (Afghanistan)")]
            Afghanistan,

            [Description("Antarctica")]
            Antarctica,

            [Description("South America (Tierra de Fuego)")]
            Argentina,

            [Description("Australia")]
            Australia,

            [Description("Southeast Asia (Cambodia)")]
            Cambodia,

            [Description("South Africa (Zimbabwe)")]
            Zimbabwe,

            [Description("West Africa (Ghana)")]
            Ghana,

            [Description("Central America (Honduras)")]
            Honduras,

            [Description("North America (Quebec)")]
            Quebec,

            [Description("North America (Alaska)")]
            Alaska,

            [Description("Greenland")]
            Greenland
        }


         [ConfigField(text: "Starting base",
          description: "You can choose a specific location to start from. Please note that some locations are harder to start from than others!")]
         public StartingBaseLocation startingBaseLocation = StartingBaseLocation.Vanilla;
        

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



        public enum StartingSquadCharacters
        {
            UNBUFFED, BUFFED, RANDOM
        }
        [ConfigField(text: "Tutorial characters in your starting squad",
   description: "You can choose to get a completely random squad (as in Vanilla without doing the tutorial), " +
            "the Vanilla tutorial starting squad (with higher stats), " +
            "or a squad that will include Sophia Brown and Jacob with unbuffed stats (default on TFTV). " +
            "Note that Jacob is a sniper, as in the title screen :)")]

        public StartingSquadCharacters tutorialCharacters = StartingSquadCharacters.UNBUFFED;
       
        // These settings determine amount of resources player can acquire:
        [ConfigField(text: "Number of scavenging sites",
            description: "Total number of scavenging sites generated on game start, not counting overgrown sites\n" +
            "(Vanilla: 16, TFTV default 8, because Ambushes generate additional resources).\n" +
            "Will not have any effect on a game in progress.")]
        public int InitialScavSites = 8; // 16 on Vanilla

        public enum ScavengingWeight
        {
            None, Low, Medium, High
        }

        [ConfigField(text: "Chances of sites with resource crates",
           description: "Of the total number of scavenging sites, choose the relative chances of a resource scavenging site being generated.\n" +
            "Choose none to have 0 scavenging sites of this type (Vanilla and TFTV default: High)\n" +
            "Will not have any effect on a game in progress.")]
        public ScavengingWeight ChancesScavCrates = ScavengingWeight.High;
        [ConfigField(text: "Chances of sites with recruits",
           description: "Of the total number of scavenging sites, choose the relative chances of a recruits rescue site being generated.\n" +
            "Choose none to have 0 scavenging sites of this type (Vanilla and TFTV default: low)\n" +
            "Will not have any effect on a game in progress.")]
        public ScavengingWeight ChancesScavSoldiers = ScavengingWeight.Low;
        [ConfigField(text: "Chances of sites with vehicles",
           description: "Of the total number of scavenging sites, choose the relative chances of a vehile rescue site being generated.\n" +
            "Choose none to have 0 scavenging sites of this type (Vanilla and TFTV default: low)\n" +
            "Will not have any effect on a game in progress.")]
        public ScavengingWeight ChancesScavGroundVehicleRescue = ScavengingWeight.Low;

        // Determines amount of resources gained in Events. 1f = 100% if Azazoth level.
        // 0.8f = 80% of Azazoth = Pre-Azazoth level (default on BetterGeoscape).
        // For example, to double amount of resources from current Vanilla (Azazoth level), change to 2f 
        // Can be applied to game in progress
        [ConfigField(text: "Amount of resources gained in events",
           description: "For current (post Azazoth patch) Vanilla, set to 1. For default TFTV and Vanilla pre-Azazoth patch, set to 0.8.\n" +
            "Can be applied to a game in progress.\n"+"Set to 1.2 on Rookie by default")] //done
        public float ResourceMultiplier = 0.8f;

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

        // If set to false, applying a mutation will not set character's Stamina to zero
        [ConfigField(text: "Stamina drained on mutation",
          description: "The stamina of any operative that undergoes a mutation will be set to zero.\n" +
           "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
        public bool StaminaPenaltyFromMutation = true;

        // If set to false, adding a bionic will not set character's Stamina to zero
        [ConfigField(text: "Stamina drained on bionic augmentation",
          description: "The stamina of any operative that undergoes a bionic augmentation will be set to zero.\n" +
           "Can be applied to a game in progress.\n" + "Set to false on Rookie by default")] //done
        public bool StaminaPenaltyFromBionics = true;

        // If set to false, ambushes will happen as rarely as in Vanilla, and will not have crates in them
        [ConfigField(text: "New ambush",
          description: "Ambushes will happen more often and will be harder. Regardless of this setting, all ambushes will have crates in them.\n" + "Set to false on Rookie by default")] //done
        public bool MoreAmbushes = true;

        // Changing the settings below will make the game harder:

        // If set to true, the passenger module FAR-M will no longer regenerate Stamina in flight
        // For players who prefer having to come back to base more often
        [ConfigField(text: "Stamina recuperation from FAR-M",
         description: "The starting type of passenger module, FAR-M, will slowly recuperate the stamina of the operatives on board.\n" +
            "Switch off if you prefer to have to return to base more often")]
        public bool ActivateStaminaRecuperatonModule = true;

        // If set to true reversing engineering an item allows to research the faction technology that allows manufacturing the item 
        [ConfigField(text: "Enhanced Reverse Engineering",
        description: "Reversing engineering an item allows to research the faction technology that allows manufacturing the item. IF YOU CHANGE THIS SETTING, QUIT THE GAME TO DESKTOP AND RESTART FOR CHANGES TO TAKE EFFECT")]
        public bool ActivateReverseEngineeringResearch = true;

        // Below are advanced settings. The mod was designed to be played with all of them turned on

        //If set to true, modifes stats of some weapons and modules, and adds random chance that weapon will be lost when disengaging
        [ConfigField(text: "Changes to air combat",
       description: "Modifes stats of some weapons and modules.")]
        public bool ActivateAirCombatChanges = true;

        // If set to true activates DLC5 Kaos Engines story rework (in progress)
        [ConfigField(text: "Changes to DLC5 Marketplace (in progress)",
       description: "Removes cutscenes and missions, all items available at lowest prices 24 hours after discovering Marketplace.")]
        public bool ActivateKERework = true;

        // If set to true, unrevealed havens will be revealed when attacked
        [ConfigField(text: "Havens under attack revealed",
       description: "Havens under attack will send an SOS, revealing their location to the player.")]
        public bool HavenSOS = true;

        //If set to 1, shows when any error ocurrs. Do not change unless you know what you are doing.
        [ConfigField(text: "Debug log & messages",
       description: "Shows when any error ocurrs. Please, do not change unless you know what you are doing.")]
        public bool Debug = true;

        [ConfigField(text: "Learn the first personal skill",
           description: "If enabled, the first personal skill (level 1) is set right after a character is created (starting soldiers, new recruits on haven, rewards ect)")]
        public bool LearnFirstPersonalSkill = true;

        // Deactivate auto standy in tactical missions
        [ConfigField(text: "Deactivate tactical auto standby",
            description: "Disables or enables the vanilla behavior of automatically putting an actor into standby mode and thus switching to the next actor when all AP are used.\n"
                         + "ATTENTION: This function is WIP, i.e. currently still experimental!")]
        public bool DeactivateTacticalAutoStandby = false;

        // Infiltrator Crossbow Ammo changes
        [ConfigField(text: "Eros ammo",
            description: "Set the amount of bolts for the magazine of the base crossbows (Eros CRB III)")]
        public int BaseCrossbow_Ammo = 3;
        [ConfigField(text: "Psyche ammo",
            description: "Set the amount of bolts for the magazine of the advanced crossbow (Psyche CRB IV)")]
        public int VenomCrossbow_Ammo = 3;

        // Always show helmets
      /*  [ConfigField(text: "Operatives will appear without helmets in the personnel screen",
            description: "Turn off if you don't like to see the faces of the individuals you are sending to be clawed and devoured by horrifying monstrosities")]
        public bool ShowFaces = true;*/


    }
}
