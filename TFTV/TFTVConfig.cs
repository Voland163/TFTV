using PhoenixPoint.Modding;
using System.Collections.Generic;
using System.Linq;

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
      /*  public readonly Dictionary<string, StartingSquadFaction> StartingSquad= new Dictionary<string, StartingSquadFaction>();

        public override List<ModConfigField> GetConfigFields()
        {
            return StartingSquad.Select(s => new ModConfigField(s.Key, s.Value.GetType())
            {
                GetValue = () => s.Value,
                SetValue = (StartingSquadFaction) => StartingSquad[s.Key] = (StartingSquadFaction)StartingSquadFaction,
                GetDescription = () => "<<custom description>>"
            }).ToList();
        }
      */

        //Default settings
        [ConfigField(text: "DEFAULT TFTV SETTINGS",
            description: "Sets all settings to default, to provide the Terror from the Void experience as envisioned by its creators")]
        public bool defaultSettings = true;

        //Starting squad
      
       public enum StartingSquadFaction
        {
            PHOENIX, ANU, NJ, SYNEDRION
        }
        [ConfigField(text: "Starting squad",
         description: "You can choose a different starting squad. If you do, one of your Assaults and your starting Heavy on Legend and Hero, " +
            "Assault on Veteran, or Sniper on Rookie will be replaced by a Faction class of your choice. " +
            "You will also get the corresponding faction technology once the faction researches it.")]
        public StartingSquadFaction startingSquad = StartingSquadFaction.PHOENIX;

        
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
            "(Vanilla: 16, TFTV default 8, because Ambushes generate additional resources)\n" +
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
            "Can be applied to a game in progress.")]
        public float ResourceMultiplier = 0.8f;

        // Changing the settings below will make the game easier:

        // Determines if diplomatic penalties are applied when cozying up to one of the factions by the two other factions
        // Can be applied to game in progress
        [ConfigField(text: "Higher diplomatic penalties",
           description: "Diplomatic penalties from choices in events are doubled and revealing diplomatic missions for one faction gives a diplomatic penalty with the other factions.\n" +
                        "Can be applied to a game in progress.")]
        public bool DiplomaticPenalties = true;


        // If set to false, a disabled limb in tactical will not set character's Stamina to zero in geo
        [ConfigField(text: "Stamina drained on injury",
           description: "The stamina of any operative that sustains an injury in combat that results in a disabled body part will be set to zero after the mission.\n" +
            "Can be applied to a game in progress.")]
        public bool StaminaPenaltyFromInjury = true;

        // If set to false, applying a mutation will not set character's Stamina to zero
        [ConfigField(text: "Stamina drained on mutation",
          description: "The stamina of any operative that undergoes a mutation will be set to zero.\n" +
           "Can be applied to a game in progress.")]
        public bool StaminaPenaltyFromMutation = true;

        // If set to false, adding a bionic will not set character's Stamina to zero
        [ConfigField(text: "Stamina drained on bionic augmentation",
          description: "The stamina of any operative that undergoes a bionic augmentation will be set to zero.\n" +
           "Can be applied to a game in progress.")]
        public bool StaminaPenaltyFromBionics = true;

        // If set to false, ambushes will happen as rarely as in Vanilla, and will not have crates in them
        [ConfigField(text: "New ambush",
          description: "Ambushes will happen more often, will be harder and will have crates in them.")]
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
        description: "Reversing engineering an item allows to research the faction technology that allows manufacturing the item.")]
        public bool ActivateReverseEngineeringResearch = true;

        // Below are advanced settings. The mod was designed to be played with all of them turned on

        //If set to true, modifes stats of some weapons and modules, and adds random chance that weapon will be lost when disengaging
        [ConfigField(text: "Changes to air combat",
       description: "Modifes stats of some weapons and modules, and adds random chance that weapon will be lost when disengaging.")]
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




    }
}
