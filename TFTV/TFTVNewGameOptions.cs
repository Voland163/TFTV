using Base.Core;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.DifficultySystem;
using PhoenixPoint.Geoscape.Levels;
using System;
using UnityEngine;

namespace TFTV
{
    internal class TFTVNewGameOptions
    {

        public static bool IsReworkEnabled()
        {
            return TFTVAircraftReworkMain.AircraftReworkOn;
        }

        [HarmonyPatch(typeof(DynamicDifficultySystem), nameof(DynamicDifficultySystem.GetCalculatedDeployment))]
        public static class DynamicDifficultySystem_GetCalculatedDeployment_Patch
        {
            public static bool Prefix(DynamicDifficultySystem __instance, GeoMission mission, ref int __result)
            {
                try
                {
                    if (!TFTVNewGameOptions.ConfigImplemented)
                    {
                        return true;
                    }

                    GameThreatLevelDef threatLevelDef = __instance.GetThreatLevelDef(mission.ThreatLevel);
                    GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    int days = (level.Timing.Now - level.Timing.StartTime).TimeSpan.Days;

                    int startingForce = TFTVNewGameOptions.StartingEnemyForce;
                    int maximumForce = TFTVNewGameOptions.MaximumEnemyForce;
                    int escalationDays = TFTVNewGameOptions.EnemyEscalationSpeed;

                    // Same formula as vanilla, substituting our configurable values for the
                    // difficulty-def fields (InitialDeploymentPoints, FinalDeploymentPoints,
                    // DaysToReachFinalDeployment).
                    float ratePerDay = (float)(maximumForce - startingForce) / (float)escalationDays;
                    float raw = ((float)startingForce + (float)days * ratePerDay) * threatLevelDef.ThreatLevelModifier;

                    __result = Mathf.CeilToInt(Mathf.Clamp(raw, (float)startingForce, (float)maximumForce));

                    TFTVLogger.Always($"[EnemyForce] day={days} start={startingForce} max={maximumForce} escalDays={escalationDays} " +
                        $"threat={mission.ThreatLevel} threatMod={threatLevelDef.ThreatLevelModifier:F2} result={__result}");

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }


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
            Ethiopia,
            Ukraine,
            Greenland,
            Afghanistan,
            Algeria,
            Alaska,
            Mexico,
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
        public static int EtermesResistanceAndVulnerability;

        public static float RansackResourcesMultiplier = 1f;  // 0.0 – 2.0
        public static int InitialLootLevel = 2;               // 0=+4 Story, 1=Story, 2=Rookie, 3=Veteran, 4=Hero, 5=Legend, 6=Eldritch, 7=None
        public static int PersonnelInfluxLevel = 2;           // 0=+2 Story, 1=Story, 2=Rookie, 3=Veteran, 4=Hero, 5=None
        public static int InitialManticoreLimit = 1;          // 0–4; max Manticores to award across all base visits
        public static int InitialScarabLimit = 1;             // 0–4; max Scarabs to award across all base visits

        public static bool NewTrainingFacilities = false;
        public static bool BaseRework = false;
        public static bool NewPowerManagement = false;

        public static int initialScavSites = 8; // 16 on Vanilla

        public static int StartingEnemyForce = 500;   // index 1 = Standard
        public static int MaximumEnemyForce = 1650;   // index 1 = Moderate
        public static int EnemyEscalationSpeed = 145; // index 1 = Measured
        public enum ScavengingWeight
        {
            High, Medium, Low, None
        }

        public static ScavengingWeight chancesScavCrates = ScavengingWeight.High;

        public static ScavengingWeight chancesScavSoldiers = ScavengingWeight.Low;

        public static ScavengingWeight chancesScavGroundVehicleRescue = ScavengingWeight.Low;

        public static void SetBaseReworkOptionDefaults(int difficulty)
        {
            try
            {
                switch (difficulty)
                {
                    case 1: // Story
                        RansackResourcesMultiplier = 2f;
                        InitialLootLevel = 7;
                        PersonnelInfluxLevel = 3;
                        InitialManticoreLimit = 2;
                        InitialScarabLimit = 2;
                        break;
                    case 2: // Rookie
                        RansackResourcesMultiplier = 1.5f;
                        InitialLootLevel = 6;
                        PersonnelInfluxLevel = 3;
                        InitialManticoreLimit = 1;
                        InitialScarabLimit = 2;
                        break;
                    case 3: // Veteran
                        RansackResourcesMultiplier = 1f;
                        InitialLootLevel = 5;
                        PersonnelInfluxLevel = 2;
                        InitialManticoreLimit = 1;
                        InitialScarabLimit = 1;
                        break;
                    case 4: // Hero
                        RansackResourcesMultiplier = 0.75f;
                        InitialLootLevel = 4;
                        PersonnelInfluxLevel = 1;
                        InitialManticoreLimit = 0;
                        InitialScarabLimit = 1;
                        break;
                    case 5: // Legend
                        RansackResourcesMultiplier = 0.5f;
                        InitialLootLevel = 3;
                        PersonnelInfluxLevel = 0;
                        InitialManticoreLimit = 0;
                        InitialScarabLimit = 0;
                        break;
                    default: // Eldritch+
                        RansackResourcesMultiplier = 0.25f;
                        InitialLootLevel = 2;
                        PersonnelInfluxLevel = 0;
                        InitialManticoreLimit = 0;
                        InitialScarabLimit = 0;
                        break;
                }

                TFTVLogger.Always($"[BaseRework] SetBaseReworkOptionDefaults for difficulty {difficulty}: " +
                    $"Ransack={RansackResourcesMultiplier} Loot={InitialLootLevel} Personnel={PersonnelInfluxLevel} " +
                    $"ManticoreLimit={InitialManticoreLimit} ScarabLimit={InitialScarabLimit}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void SetEnemyForceOptionDefaults(int difficulty)
        {
            try
            {
                // Story and Rookie share index 0; each step up advances by one index.
                // Index maps: 0=Light/Limited/Gradual, 1=Standard/Moderate/Measured, ...
                switch (difficulty)
                {
                    case 1: // Story
                        StartingEnemyForce = 450;
                        MaximumEnemyForce = 1450;
                        EnemyEscalationSpeed = 170;
                        break;
                    case 2: // Rookie
                        StartingEnemyForce = 450;
                        MaximumEnemyForce = 1450;
                        EnemyEscalationSpeed = 170;
                        break;
                    case 3: // Veteran
                        StartingEnemyForce = 500;
                        MaximumEnemyForce = 1650;
                        EnemyEscalationSpeed = 145;
                        break;
                    case 4: // Hero
                        StartingEnemyForce = 575;
                        MaximumEnemyForce = 1900;
                        EnemyEscalationSpeed = 120;
                        break;
                    case 5: // Legend
                        StartingEnemyForce = 650;
                        MaximumEnemyForce = 2200;
                        EnemyEscalationSpeed = 90;
                        break;
                    default: // Eldritch+
                        StartingEnemyForce = 812;
                        MaximumEnemyForce = 3125;
                        EnemyEscalationSpeed = 72;
                        break;
                }

                TFTVLogger.Always($"[EnemyForce] SetEnemyForceOptionDefaults for difficulty {difficulty}: " +
                    $"StartingEnemyForce={StartingEnemyForce} MaximumEnemyForce={MaximumEnemyForce} EnemyEscalationSpeed={EnemyEscalationSpeed}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

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

                SetBaseReworkOptionDefaults(difficulty);

                if (difficulty > 5)
                {
                    DiplomaticPenaltiesSetting = true;
                    StaminaPenaltyFromInjurySetting = true;
                    MoreAmbushesSetting = true;
                    StrongerPandoransSetting = true;
                    ImpossibleWeaponsAdjustmentsSetting = true;
                    LimitedCaptureSetting = true;
                    LimitedHarvestingSetting = true;
                    NoSecondChances = true;
                    EtermesResistanceAndVulnerability = 0;
                }
                else if (difficulty > 4)
                {
                    DiplomaticPenaltiesSetting = true;
                    StaminaPenaltyFromInjurySetting = true;
                    MoreAmbushesSetting = true;
                    StrongerPandoransSetting = true;
                    ImpossibleWeaponsAdjustmentsSetting = true;
                    LimitedCaptureSetting = true;
                    LimitedHarvestingSetting = true;             
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

                SetEnemyForceOptionDefaults(difficulty);

                ConfigImplemented = true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
