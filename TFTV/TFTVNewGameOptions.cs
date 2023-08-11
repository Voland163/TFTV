using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;

namespace TFTV
{
    internal class TFTVNewGameOptions
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
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

        public static int initialScavSites = 8; // 16 on Vanilla

        public enum ScavengingWeight
        {
            High, Medium, Low, None
        }

        public static ScavengingWeight chancesScavCrates = ScavengingWeight.High;

        public static ScavengingWeight chancesScavSoldiers = ScavengingWeight.Low;

        public static ScavengingWeight chancesScavGroundVehicleRescue = ScavengingWeight.Low;


        public static void SetInternalConfigOptions(GeoLevelController geoLevelController, TacticalLevelController tacticalLevelController)
        {
            if (!ConfigImplemented)
            {
                try
                {
                    TFTVLogger.Always($"This game started before update #35; adjusting config");

                    TFTVConfig config = TFTVMain.Main.Config;

                    int difficultyOrder = 0;

                    if (geoLevelController != null)
                    {
                        difficultyOrder = geoLevelController.CurrentDifficultyLevel.Order;
                    }
                    else if (tacticalLevelController != null)
                    {
                        difficultyOrder = tacticalLevelController.Difficulty.Order;
                    }

                    float scaling_factor = 1f / 0.8f;

                    if (difficultyOrder == 1 && !config.OverrideRookieDifficultySettings)
                    {
                        AmountOfExoticResourcesSetting = 2.5f;
                        ResourceMultiplierSetting = 2f;
                        DiplomaticPenaltiesSetting = false;
                        StaminaPenaltyFromInjurySetting = false;
                        MoreAmbushesSetting = false;
                        StrongerPandoransSetting = false;
                        ImpossibleWeaponsAdjustmentsSetting = false;
                    }
                    else
                    {
                        AmountOfExoticResourcesSetting = config.amountOfExoticResources;
                        TFTVLogger.Always($"config.amountOfExoticResources:  {config.amountOfExoticResources}");
                        ResourceMultiplierSetting = config.ResourceMultiplier * scaling_factor;
                        TFTVLogger.Always($"config.ResourceMultiplier * scaling_factor:  {config.ResourceMultiplier * scaling_factor}");
                        DiplomaticPenaltiesSetting = config.DiplomaticPenalties;
                        TFTVLogger.Always($"config.DiplomaticPenalties:  {config.DiplomaticPenalties}");
                        StaminaPenaltyFromInjurySetting = config.StaminaPenaltyFromInjury;
                        TFTVLogger.Always($"config.StaminaPenaltyFromInjury:  {config.StaminaPenaltyFromInjury}");
                        MoreAmbushesSetting = config.MoreAmbushes;
                        TFTVLogger.Always($"config.MoreAmbushes:  {config.MoreAmbushes}");
                        LimitedCaptureSetting = config.LimitedCapture;
                        TFTVLogger.Always($"config.LimitedCapture:  {config.LimitedCapture}");
                        LimitedHarvestingSetting = config.LimitedHarvesting;
                        TFTVLogger.Always($"config.LimitedHarvesting:  {config.LimitedHarvesting}");
                        StrongerPandoransSetting = config.StrongerPandorans;
                        TFTVLogger.Always($"config.StrongerPandorans:  {config.StrongerPandorans}");
                        ImpossibleWeaponsAdjustmentsSetting = config.impossibleWeaponsAdjustments;
                        TFTVLogger.Always($"config.impossibleWeaponsAdjustments:  {config.impossibleWeaponsAdjustments}");
                    }

                    if (difficultyOrder == 6)
                    {
                        ResourceMultiplierSetting = 0.5f;
                    }

                    ConfigImplemented = true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void Change_Crossbows()
        {
            try
            {
                if (Update35Check)
                {
                    TFTVLogger.Always($"Game started after Update 35 check passed; applying changes to crossbows");
                    WeaponDef ErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_WeaponDef");
                    WeaponDef BonusErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
                    ItemDef ErosCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Crossbow_AmmoClip_ItemDef");
                    WeaponDef PsycheCrb = DefCache.GetDef<WeaponDef>("SY_Venombolt_WeaponDef");
                    ItemDef PsycheCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Venombolt_AmmoClip_ItemDef");
                    ErosCrb.ChargesMax = 5;
                    BonusErosCrb.ChargesMax = 5;
                    ErosCrb_Ammo.ChargesMax = 5;
                    PsycheCrb.ChargesMax = 4;
                    PsycheCrb_Ammo.ChargesMax = 4;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        /*  public static void SetInternalConfigOptions(GeoLevelController geoLevelController, TacticalLevelController tacticalLevelController) 
          {
              if (!NewConfigUsed) 
              {
              float[] amountMultiplierResources = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f, 4f };

                  amountOfExoticResources = amountMultiplierResources[ConvertDifficultyToIndexExoticResources(geoLevelController, tacticalLevelController)];
                  ResourceMultiplier = amountMultiplierResources[ConvertDifficultyToIndexEventsResources(geoLevelController, tacticalLevelController)];





              } 
          }*/
    }
}
