using Base.Core;
using Base.Defs;
using HarmonyLib;
using Mono.Cecil;
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
                      //  TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
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
                    TFTVPureAndForsaken.CheckMissionVSPure(__instance);

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
                      //  TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
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
                  //  TFTVLogger.Always($"Difficulty level on Tactical is {config.difficultyOnTactical}");

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
       
    }
}
