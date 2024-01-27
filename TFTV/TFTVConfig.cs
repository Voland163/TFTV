using Base.Core;
using Base.Platforms;
using Base.UI.MessageBox;
using HarmonyLib;
using Newtonsoft.Json;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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


        public bool SkipMovies = true;
        public bool EqualizeTrade = true;
        public bool LimitedRaiding = true;
        public bool disableSavingOnTactical = true;
        public enum DifficultyOnTactical
        {
            GEOSCAPE, STORY, ROOKIE, VETERAN, HERO, LEGEND, ETERMES
        }
        public DifficultyOnTactical difficultyOnTactical = DifficultyOnTactical.GEOSCAPE;

        public bool AnimateWhileShooting = false;
        public bool ReinforcementsNoDrops = true;
        public bool MoreMistVO = true;
        public bool LimitedDeploymentVO = true;
        public bool ActivateStaminaRecuperatonModule = true;
        public bool HavenSOS = true;
        public bool LearnFirstPersonalSkill = true;
        public bool DeactivateTacticalAutoStandby = true;
        public bool Debug = true;
       // public bool LimitedCapture = true;
      //  public bool LimitedHarvesting = true;
        public bool NoBarks = false;
        public int Difficulty = 2;



        internal List<ModConfigField> modConfigFields = new List<ModConfigField>();
        public void PopulateConfigFields()
        {
            try
            {
                foreach (var fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).ToList())
                {
                    modConfigFields.Add(new ModConfigField(fieldInfo.Name, fieldInfo.GetValue(this).GetType())
                    {
                        GetValue = () => fieldInfo.GetValue(this),
                        SetValue = (o) => fieldInfo.SetValue(this, o),
                        GetText = () => TFTVCommonMethods.ConvertKeyToString($"KEY_{fieldInfo.Name}"),
                        GetDescription = () => TFTVCommonMethods.ConvertKeyToString($"KEY_{fieldInfo.Name}_DESCRIPTION"),
                    });
                }

                string filePathRoot = GameUtl.GameComponent<PlatformComponent>().Platform.GetPlatformData().GetFilePathRoot();

                string path = Path.Combine(filePathRoot, ConfigFileName);
                if (File.Exists(path))
                {

                    Dictionary<string, ModRawConfig> modConfigs = JsonConvert.DeserializeObject<Dictionary<string, ModRawConfig>>(File.ReadAllText(path));

                    if (modConfigs != null && modConfigs.ContainsKey("phoenixrising.tftv"))
                    {
                        ModRawConfig rawTFTVConfig = modConfigs["phoenixrising.tftv"];

                       
                        if (rawTFTVConfig != null && rawTFTVConfig.Count == modConfigFields.Count)
                        {
                            TFTVLogger.Always($"Found the raw config! Count: {rawTFTVConfig.Count}");
                            LoadFromRawConfig(rawTFTVConfig);

                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public override List<ModConfigField> GetConfigFields()
        {
            return modConfigFields;

        }



        [HarmonyPatch(typeof(ModSettingController), "ApplyModification")]
        public static class ModSettingController_ApplyModification_Patch
        {
            private static void Postfix(ModSettingController __instance)
            {
                try
                {

                /*    if ((TFTVDefsWithConfigDependency.ChangesToCapturingPandoransImplemented && __instance.Label.text == "CAPTURING PANDORANS IS LIMITED")
                        || (TFTVDefsWithConfigDependency.StrongerPandorans.StrongerPandoransImplemented && __instance.Label.text == "MAKE PANDORANS STRONGER") ||
                       (TFTVDefsWithConfigDependency.ChangesToFoodAndMutagenGenerationImplemented && __instance.Label.text == "LIMITS ON RENDERING PANDORANS FOR FOOD OR MUTAGENS"))
                    {
                        string warning = $"{TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING0")} {__instance.Label.text} {TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING0")}";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    }*/

                    if (__instance.Label.text == "DISABLE SAVING ON TACTICAL")
                    {
                        string warning = TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_TACTICAL_SAVING_WARNING");

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    }
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
            private static void Postfix(ModSettingController __instance, string label, Type type)
            {
                try
                {

                    if (type== typeof(int))
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

        /*  public void RetrieveConfigOptions()
          {
              try
              {
                  SkipMovies = (bool)Fields[$"SkipMovies"];
                 // TFTVLogger.Always($"SkipMovies is {SkipMovies}");
                  disableSavingOnTactical = (bool)Fields[$"disableSavingOnTactical"];
               //  TFTVLogger.Always($"disableSavingOnTactical is {disableSavingOnTactical}");
                  difficultyOnTactical = (DifficultyOnTactical)Fields[$"difficultyOnTactical"];
                  EqualizeTrade = (bool)Fields[$"EqualizeTrade"];
                  LimitedRaiding = (bool)Fields[$"LimitedRaiding"];
                  LimitedCapture = (bool)Fields[$"LimitedCapture"];
                  LimitedHarvesting = (bool)Fields[$"LimitedHarvesting"];
                  ReinforcementsNoDrops = (bool)Fields[$"ReinforcementsNoDrops"];
                  MoreMistVO = (bool)Fields[$"MoreMistVO"];
                  LimitedDeploymentVO = (bool)Fields[$"LimitedDeploymentVO"];
                  AnimateWhileShooting = (bool)Fields[$"AnimateWhileShooting"];
                  ActivateStaminaRecuperatonModule = (bool)Fields[$"ActivateStaminaRecuperatonModule"];
                  HavenSOS = (bool)Fields[$"HavenSOS"];
                  LearnFirstPersonalSkill = (bool)Fields[$"LearnFirstPersonalSkill"];
                  NoBarks = (bool)Fields[$"NoBarks"];
                  DeactivateTacticalAutoStandby = (bool)Fields[$"DeactivateTacticalAutoStandby"];
                  Debug = (bool)Fields[$"Debug"];

              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
                  throw;
              }
          }*/

    }
}
