using Base.Core;
using Base.Defs;
using Base.Levels;
using Base.Platforms;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.RedeemableCodes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Levels;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;



        //You will need to edit scene hierarchy to add new objects under GameSettingsModule, it has a UIModuleGameSettings script
        //Class UIStateNewGeoscapeGameSettings is responsible for accepting selected settings and start the game, so you'll have to dig inside
        //for changing behaviour.
        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "BindSecondaryOptions")]
        internal static class UIStateNewGeoscapeGameSettings_BindSecondaryOptions_patch
        {
            private static void Postfix(UIStateNewGeoscapeGameSettings __instance, GameOptionViewController element)
            {
                try
                {
                    TFTVLogger.Always("Element is " + element.OptionText.text);
                    if (element.OptionText.text == "PLAY PROLOGUE AND TUTORIAL")
                    {
                        element.OptionText.text = "START WITH VANILLA TUTORIAL SQUAD";                      
                    }
                    else
                    {
                        element.CheckedToggle.isOn = false;

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


        }

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "CreateSceneBinding")]
        public static class UIStateNewGeoscapeGameSettings_GameSettings_OnConfirm_patch
        {
            public static void Prefix(GeoscapeGameParams gameParams)
            {
                try
                {
                    if (gameParams.TutorialEnabled)
                    {
                        gameParams.TutorialEnabled = false;
                        TFTVConfig config = TFTVMain.Main.Config;
                        config.tutorialCharacters = TFTVConfig.StartingSquadCharacters.BUFFED;
                    }
                    UIModuleGameSettings gameSettings = (UIModuleGameSettings)UnityEngine.Object.FindObjectOfType(typeof(UIModuleGameSettings));
                    List<EntitlementDef> entitlementDefs = new List<EntitlementDef>();
                    entitlementDefs.AddRange(gameSettings.GetActivatedEntitlements());
                    entitlementDefs.AddRange(gameSettings.GetDeactivatedEntitlements());
                    gameParams.EnabledDlc = entitlementDefs.ToArray(); 
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        /*  [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "GameSettings_OnConfirm")]
          public static class UIStateNewGeoscapeGameSettings_GameSettings_OnConfirm_patch
          {
              public static void Postfix(UIStateNewGeoscapeGameSettings __instance, GeoscapeGameModeDef ____gameModeDef)
              {
                  try
                  {
                      TFTVLogger.Always("Check " + ____gameModeDef.Description.LocalizeEnglish());

                      UIModuleGameSettings gameSettings = (UIModuleGameSettings)UnityEngine.Object.FindObjectOfType(typeof(UIModuleGameSettings));
                      TFTVLogger.Always("Check " + gameSettings.name);

                      GeoscapeGameParams geoscapeGameParams = ((GeoscapeGameParams)____gameModeDef.LevelSceneBinding.LevelParams).Clone();
                      geoscapeGameParams.TutorialEnabled = false;
                      OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                      if (gameSettings.SecondaryOptions.Elements[0].IsSelected)
                      {
                          IEnumerable<RedeemableCodeDef> enumerable = optionsManager.AllRedeemableKeys();
                          IEnumerable<string> source = optionsManager.ActivatedRedeemCodes.Distinct();
                          foreach (RedeemableCodeDef code in enumerable)
                          {
                              if (code.AutoRedeem)
                              {
                                  geoscapeGameParams.RedeemableCodes.Add(code);
                              }
                              else if (code.AutoRedeemOnCompleteEdition)
                              {
                                  geoscapeGameParams.RedeemableCodes.Add(code);
                              }
                              else if (!string.IsNullOrWhiteSpace(source.FirstOrDefault((string r) => r == code.RedeemableCode)))
                              {
                                  geoscapeGameParams.RedeemableCodes.Add(code);
                              }
                          }
                      }
                      LevelSceneBinding levelSceneBinding = ____gameModeDef.LevelSceneBinding.CloneAndAddLevelParams(geoscapeGameParams);
                      GameUtl.GameComponent<PhoenixGame>().FinishLevel(new PlayNewGameResult
                      {
                          LevelSceneBinding = levelSceneBinding
                      });

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);

                  }

              }


          }

          */

    }
}
