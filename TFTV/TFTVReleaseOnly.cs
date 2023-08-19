using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;

namespace TFTV
{
    internal class TFTVReleaseOnly
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //   public static bool NewDifficultiesImplemented = false;

        public static void ConvertDifficulty(GeoLevelController geoController, TacticalLevelController tacticalController)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (config.EtermesMode)
                {
                    if (geoController != null)
                    {
                        geoController.CurrentDifficultyLevel = GetDifficultyFromOrder(6);
                        TFTVLogger.Always($"I AM ETERMES config option detected; switching to ETERMES difficulty");
                    }

                    if (tacticalController != null) 
                    {
                        geoController.CurrentDifficultyLevel = GetDifficultyFromOrder(6);
                        TFTVLogger.Always($"I AM ETERMES config option detected; switching to ETERMES difficulty");

                    }
                }

                if(config.EasyGeoscape) 
                {
                    if (geoController != null)
                    {
                        geoController.CurrentDifficultyLevel = GetDifficultyFromOrder(1);
                        TFTVLogger.Always($"Easy Geoscape config option detected; switching to STORY difficulty");
                    }

                    if (tacticalController != null)
                    {
                        geoController.CurrentDifficultyLevel = GetDifficultyFromOrder(1);
                        TFTVLogger.Always($"Easy Geoscape config option detected; switching to STORY difficulty");
                    }
                }

                TFTVNewGameOptions.SetInternalConfigOptions(geoController, tacticalController);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static void OnReleasePrototypeDefs()
        {
            try
            {
                CreateETERMESDifficultyLevel();
                CreateStoryModeDifficultyLevel();
                ModifyVanillaDifficultiesOrder();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(GeoMission), "ApplyTacticalMissionResult")]
        public class ApplyTacticalMissionResult
        {
            static bool Prefix(GeoMission __instance, out int __state)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.Level;
                    __state = controller.CurrentDifficultyLevel.Order;

                    if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                    {
                        TFTVLogger.Always($"difficulty order level: {__state}");
                        controller.CurrentDifficultyLevel = GetTacticalDifficulty();
                        TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
                    }

                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            static void Postfix(GeoMission __instance, int __state)

            {
                try
                {
                    GeoLevelController controller = __instance.Level;
                    controller.CurrentDifficultyLevel = GetDifficultyFromOrder(__state);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }




        }

        [HarmonyPatch(typeof(GeoMission), "PrepareTacticalGame")]
        public class PrepareTacticalGame
        {
            static bool Prefix(GeoMission __instance, out int __state)
            {

                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.Level;
                    __state = controller.CurrentDifficultyLevel.Order;

                    if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                    {
                        TFTVLogger.Always($"difficulty order level: {__state}");
                        controller.CurrentDifficultyLevel = GetTacticalDifficulty();
                        TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
                    }

                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            static void Postfix(GeoMission __instance, int __state)

            {
                try
                {
                    GeoLevelController controller = __instance.Level;
                    controller.CurrentDifficultyLevel = GetDifficultyFromOrder(__state);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static GameDifficultyLevelDef GetDifficultyFromOrder(int order)
        {
            try
            {
                switch (order)
                {
                    case 1:

                        return DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");

                    case 2:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                    case 3:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");

                    case 4:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");

                    case 5:

                        return DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

                    case 6:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");

                }

                return null;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static GameDifficultyLevelDef GetTacticalDifficulty()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                {
                    TFTVLogger.Always($"Difficulty level on Tactical is {config.difficultyOnTactical}");

                    switch (config.difficultyOnTactical)
                    {
                        case TFTVConfig.DifficultyOnTactical.STORY:

                            return DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.ROOKIE:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.VETERAN:

                            return DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.HERO:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.LEGEND:
                            return DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.ETERMES:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");

                    }
                }

                return null;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }


        public static int DifficultyOrderConverter(int order)
        {
            try
            {
               
                    TFTVConfig config = TFTVMain.Main.Config;

                    TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                    if (tacticalLevelController != null && config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                    {
                        switch (config.difficultyOnTactical)
                        {
                            case TFTVConfig.DifficultyOnTactical.STORY:
                                return 1;

                            case TFTVConfig.DifficultyOnTactical.ROOKIE:
                                return 1;

                            case TFTVConfig.DifficultyOnTactical.VETERAN:
                                return 2;

                            case TFTVConfig.DifficultyOnTactical.HERO:
                                return 3;

                            case TFTVConfig.DifficultyOnTactical.LEGEND:
                                return 4;

                            case TFTVConfig.DifficultyOnTactical.ETERMES:
                                return 5;

                        }
                    }

                    if (order >= 2)
                    {
                        return order - 1;
                    }

               
                return order;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }




        private static void ModifyVanillaDifficultiesOrder()
        {
            try
            {
                DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef").Order = 2;
                DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef").Order = 3;
                DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef").Order = 4;
                DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef").Order = 5;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void CreateStoryModeDifficultyLevel()
        {
            try
            {
                GameDifficultyLevelDef sourceDef = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");
                GameDifficultyLevelDef newDifficulty = Helper.CreateDefFromClone(sourceDef, "{B10E3C8C-1398-4398-B1A6-A93DB0C48781}", "StoryMode_DifficultyLevelDef");
                newDifficulty.Order = 1;
                newDifficulty.Name.LocalizationKey = "TFTV_DIFFICULTY_ROOKIE_TITLE";
                newDifficulty.Description.LocalizationKey = "TFTV_DIFFICULTY_ROOKIE_DESCRIPTION";

                List<GameDifficultyLevelDef> difficultyLevelDefs = new List<GameDifficultyLevelDef>(Shared.DifficultyLevels);
                difficultyLevelDefs.Insert(0, newDifficulty);

                Shared.DifficultyLevels = difficultyLevelDefs.ToArray();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateETERMESDifficultyLevel()
        {
            try
            {
                GameDifficultyLevelDef sourceDef = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                GameDifficultyLevelDef newDifficulty = Helper.CreateDefFromClone(sourceDef, "{F713C90F-5D7D-4F95-B71A-CE094A7DA6AE}", "Etermes_DifficultyLevelDef");
                newDifficulty.Order = 6;
                newDifficulty.Name.LocalizationKey = "TFTV_DIFFICULTY_ETERMES_TITLE";
                newDifficulty.Description.LocalizationKey = "TFTV_DIFFICULTY_ETERMES_DESCRIPTION";

                newDifficulty.RecruitCostPerLevelMultiplier = 0.5f;
                newDifficulty.RecruitmentPriceModifier = 1.3f;
                newDifficulty.NestLimitations.MaxNumber = 4;
                newDifficulty.NestLimitations.HoursBuildTime = 73;
                newDifficulty.LairLimitations.MaxNumber = 4; 
                newDifficulty.LairLimitations.MaxConcurrent = 4; 
                newDifficulty.LairLimitations.HoursBuildTime = 80; 
                newDifficulty.CitadelLimitations.HoursBuildTime = 144;

                newDifficulty.InitialDeploymentPoints = 812;
                newDifficulty.FinalDeploymentPoints = 3125;
                newDifficulty.DaysToReachFinalDeployment = 72;

                List<GameDifficultyLevelDef> difficultyLevelDefs = new List<GameDifficultyLevelDef>(Shared.DifficultyLevels) { newDifficulty };

                Shared.DifficultyLevels = difficultyLevelDefs.ToArray();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
