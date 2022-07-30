using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Reflection;

namespace TFTV
{
    internal class TFTVThirdAct
    {
        //private static readonly DefRepository Repo = TFTVMain.Repo;

        [HarmonyPatch(typeof(GeoBehemothActor), "StartTravel")]
        public static class GeoBehemothActor_ThirdAct_Patch
        {
            public static void Postfix(GeoBehemothActor __instance)
            {
                try
                {
                    TFTVLogger.Always("Behemoth started travelling method called");
                    if (__instance.GeoLevel.EventSystem.GetVariable("CorruptedLairDestroyed") == 1 && __instance.GeoLevel.EventSystem.GetVariable("ThirdActStarted") == 0)
                    {
                        TFTVLogger.Always("Behemoth rumpus has begun! Let the Third Act roll!");
                        __instance.GeoLevel.EventSystem.SetVariable("ThirdActStarted", 1);
                        SetBehemothOnRampageMod(__instance.GeoLevel);
                        MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(GeoBehemothActor), "CalculateDisruptionThreshhold");
                        method_GenerateTargetData.Invoke(__instance, null);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        public static void SetBehemothOnRampageMod(GeoLevelController geoLevel)

        {
            try
            {
                /* FesteringSkiesSettingsDef festeringSkiesSettingsDef = 
                    Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
                 festeringSkiesSettingsDef.NumOfHavensToDestroyBeforeSubmerge = 30;
                 festeringSkiesSettingsDef.DisruptionThreshholdBaseValue = 100;*/
                if (geoLevel.EventSystem.GetVariable("ThirdActStarted") == 1)
                {
                    geoLevel.CurrentDifficultyLevel.DestroyHavenOutcomeChance = 100;
                    geoLevel.FesteringSkiesSettings.NumOfHavensToDestroyBeforeSubmerge = 30;
                    geoLevel.FesteringSkiesSettings.DisruptionThreshholdBaseValue = 100;
                    geoLevel.CurrentDifficultyLevel.DamageHavenOutcomeChance = 0;
                }

                else
                {
                    geoLevel.CurrentDifficultyLevel.DestroyHavenOutcomeChance = 0;
                    geoLevel.CurrentDifficultyLevel.DamageHavenOutcomeChance = 100;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
