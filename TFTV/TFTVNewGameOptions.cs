using PhoenixPoint.Geoscape.Levels;
using System;

namespace TFTV
{
    internal class TFTVNewGameOptions
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        public static int InternalDifficultyCheck = 0;
        public static int InternalDifficultyCheckTactical = 0;

        public enum StartingSquadFaction
        {
            PHOENIX, ANU, NJ, SYNEDRION
        }

        public static StartingSquadFaction startingSquad = StartingSquadFaction.PHOENIX;

        public enum StartingBaseLocation
        {

            Vanilla,
            Random,
            Antarctica,
            China,
            Australia,
            Honduras,
            Mexico,
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
            Ghana
        }

        public static StartingBaseLocation startingBaseLocation = StartingBaseLocation.Vanilla;

        public enum StartingSquadCharacters
        {
            UNBUFFED, BUFFED, RANDOM
        }

        public static StartingSquadCharacters startingSquadCharacters = StartingSquadCharacters.UNBUFFED;

        public static bool ConfigImplemented = false;
        public static bool Update35Check = false;
        public static float AmountOfExoticResourcesSetting;
        public static float ResourceMultiplierSetting;
        public static bool DiplomaticPenaltiesSetting;
        public static bool StaminaPenaltyFromInjurySetting;
        public static bool MoreAmbushesSetting;
        public static bool LimitedCaptureSetting;
        public static bool LimitedHarvestingSetting;
        public static bool StrongerPandoransSetting;
        public static bool ImpossibleWeaponsAdjustmentsSetting;
        public static bool NoSecondChances;

        public static int initialScavSites = 8; // 16 on Vanilla

        public enum ScavengingWeight
        {
            High, Medium, Low, None
        }

        public static ScavengingWeight chancesScavCrates = ScavengingWeight.High;

        public static ScavengingWeight chancesScavSoldiers = ScavengingWeight.Low;

        public static ScavengingWeight chancesScavGroundVehicleRescue = ScavengingWeight.Low;


        public static void SetInternalConfigOptions(GeoLevelController geoLevelController)
        {
            try
            {
                TFTVLogger.Always($"This game lost its config; probably saved, quit and loaded during Tutotial. Restoring default values for difficulty");
                TFTVConfig config = TFTVMain.Main.Config;

                if (geoLevelController == null)
                {
                    return;
                }

                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;

                switch (difficulty)
                {
                    case 1:
                        AmountOfExoticResourcesSetting = 2.5f;
                        ResourceMultiplierSetting = 2f;
                        break;
                    case 2:
                        AmountOfExoticResourcesSetting = 2.5f;
                        ResourceMultiplierSetting = 1.5f;
                        break;
                    case 3:
                        AmountOfExoticResourcesSetting = 2f;
                        ResourceMultiplierSetting = 1.25f;
                        break;
                    case 4:
                        AmountOfExoticResourcesSetting = 1.5f;
                        ResourceMultiplierSetting = 1f;
                        break;
                    case 5:
                        AmountOfExoticResourcesSetting = 1f;
                        ResourceMultiplierSetting = 1f;
                        break;
                    case 6:
                        AmountOfExoticResourcesSetting = 0.5f;
                        ResourceMultiplierSetting = 0.75f;
                        break;
                }


                if (difficulty > 4)
                {
                    DiplomaticPenaltiesSetting = true;
                    StaminaPenaltyFromInjurySetting = true;
                    MoreAmbushesSetting = true;
                    StrongerPandoransSetting = true;
                    ImpossibleWeaponsAdjustmentsSetting = true;
                    LimitedCaptureSetting = true;
                    LimitedHarvestingSetting = true;
                    NoSecondChances = true;
                }
                else if(difficulty > 2) 
                {
                    DiplomaticPenaltiesSetting = true;
                    StaminaPenaltyFromInjurySetting = true;
                    MoreAmbushesSetting = true; 
                    ImpossibleWeaponsAdjustmentsSetting = true;
                    LimitedCaptureSetting = true;
                    LimitedHarvestingSetting = true;
                }
                else 
                {
                    DiplomaticPenaltiesSetting = false;
                    StaminaPenaltyFromInjurySetting = false;
                    MoreAmbushesSetting = false;
                    StrongerPandoransSetting = false;
                    ImpossibleWeaponsAdjustmentsSetting = false;
                    LimitedCaptureSetting = false;
                    LimitedHarvestingSetting = false;
                    NoSecondChances = false;
                }

                ConfigImplemented = true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
