using PhoenixPoint.Modding;

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
        // For testing purposes
        public int evolutionPointsLegend = 70;
        // These settings determine amount of resources player can acquire:

        // Determines amount of scavenging missions available and type of mission (crates, vehicles, or soldiers)
        // Is setup at start of new game, so game in progress will not be affected by change in settings
        public int InitialScavSites = 8; // 16 on Vanilla
        public int ChancesScavCrates = 4; // 4 on Vanilla
        public int ChancesScavSoldiers = 1; // 1 on Vanilla
        public int ChancesScavGroundVehicleRescue = 1; // 1 on Vanilla 

        // Determines amount of resources gained in Events. 1f = 100% if Azazoth level.
        // 0.8f = 80% of Azazoth = Pre-Azazoth level (default on BetterGeoscape).
        // For example, to double amount of resources from current Vanilla (Azazoth level), change to 2f 
        // Can be applied to game in progress
        public float ResourceMultiplier = 0.8f;

        // Changing the settings below will make the game easier:

        // Determines if diplomatic penalties are applied when cozying up to one of the factions by the two other factions
        // Can be applied to game in progress
        public bool DiplomaticPenalties = true;

        // If set to false, a disabled limb in tactical will not set character's Stamina to zero in geo
        public bool StaminaPenaltyFromInjury = true;

        // If set to false, applying a mutation will not set character's Stamina to zero
        public bool StaminaPenaltyFromMutation = true;

        // If set to false, adding a bionic will not set character's Stamina to zero
        public bool StaminaPenaltyFromBionics = true;

        // If set to false, ambushes will happen as rarely as in Vanilla, and will not have crates in them
        public bool MoreAmbushes = true;

        // Changing the settings below will make the game harder:

        // If set to true, the passenger module FAR-M will no longer regenerate Stamina in flight
        // For players who prefer having to come back to base more often
        public bool ActivateStaminaRecuperatonModule = true;

        // If set to true reversing engineering an item allows to research the faction technology that allows manufacturing the item 
        public bool ActivateReverseEngineeringResearch = true;

        // Below are advanced settings. The mod was designed to be played with all of them turned on

        //If set to true, modifes stats of some weapons and modules, and adds random chance that weapon will be lost when disengaging
        public bool ActivateAirCombatChanges = true;

        // If set to true activates DLC5 Kaos Engines story rework (in progress)
        public bool ActivateKERework = true;

        //If set to 1, shows when any error ocurrs. Do not change unless you know what you are doing.
        public int Debug = 1;

    }
}
