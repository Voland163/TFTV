﻿using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.ContextHelp.HintConditions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    //for future hints: TUT4_BodyPartDisabled_HintDef can be used to teach about Stamina
    //disable hint diplomacy effect of Pandoran bases
    internal class TFTVTutorialAndStory
    {

        // public static List<string> TacticalHintsToShow = new List<string>();
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
        // public static string hintShow = "";


        private static readonly ActorHasTemplateHintConditionDef sourceActorHasTemplateHintConditionDef = DefCache.GetDef<ActorHasTemplateHintConditionDef>("ActorHasTemplate_Fishman2_Sneaker_AlienMutationVariationDef_HintConditionDef");
        private static readonly ActorHasTagHintConditionDef sourceActorHasTagHintConditionDef = DefCache.GetDef<ActorHasTagHintConditionDef>("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef");
        private static readonly ActorHasStatusHintConditionDef sourceActorHasStatusHintConditionDef = DefCache.GetDef<ActorHasStatusHintConditionDef>("ActorHasStatus_CorruptionAttack_StatusDef_HintConditionDef");

        private static readonly ContextHelpHintDef sourceContextHelpHintDef = DefCache.GetDef<ContextHelpHintDef>("TUT_DLC3_MissionStartStory_HintDef");
        private static readonly HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef");
        private static readonly LevelHasTagHintConditionDef sourceInfestationMission = DefCache.GetDef<LevelHasTagHintConditionDef>("LevelHasTag_MissionTypeBaseInfestation_MissionTagDef_HintConditionDef");
        private static readonly MissionTypeTagDef infestationMissionTagDef = DefCache.GetDef<MissionTypeTagDef>("HavenInfestation_MissionTypeTagDef");
        // private static readonly IsDefHintConditionDef sourceIsDefHintConditionDef = DefCache.GetDef<IsDefHintConditionDef>("IsDef_Strained_StatusDef_HintConditionDef");

        /*  public static void RemoveAlreadyShownTacticalHints()
          {
              try
              {
                  if (TacticalHintsToShow.Count > 0)
                  {
                      foreach (string hintDefName in TacticalHintsToShow)
                      {
                          ContextHelpHintDef hint = DefCache.GetDef<ContextHelpHintDef>(hintDefName);
                          TFTVLogger.Always("Hint already shown is " + hintDefName);

                          if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hint))
                          {
                              alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hint);
                          }
                      }
                  }
              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }
          }*/



        /* [HarmonyPatch(typeof(UIStateGeoCutscene), "ExitState")] //, new Type[] { typeof(GeoscapeTutorialStepType), typeof(int)})]
         public static class GeoscapeTutorial_Finished_Patch
         {
             public static void Postfix(UIStateGeoCutscene __instance)
             {
                 try
                 {

                     TFTVLogger.Always("ExitState");

                         GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));
                         TFTVLogger.Always("Intro variable is " + controller.EventSystem.GetVariable("BG_Intro_Played"));
                         //controller.EventSystem.SetVariable("BG_Intro_Played", 0);
                         TFTVNewPXCharacters.PlayIntro(controller);
                         TFTVLogger.Always("Intro should have triggered");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }

             }
         }
        */





        [HarmonyPatch(typeof(UIModuleTutorialModal), "SetTutorialStep")]
        public static class UIModuleTutorialModal_SetTutorialStep_Hints_Patch
        {

            public static void Postfix(UIModuleTutorialModal __instance, GeoscapeTutorialStep step)
            {
                try
                {
                    if (step.StepType == GeoscapeTutorialStepType.CorruptionActivated && step.Title.LocalizationKey == "KEY_GEO_HINT_ENEMY_SPECIAL_CORRUPTION_NAME")
                    {
                        __instance.Image.sprite = Helper.CreateSpriteFromImageFile("BG_Hint_Delirium.png");
                    }
                    else if (step.StepType == GeoscapeTutorialStepType.Customization && step.Title.LocalizationKey == "KEY_GEO_HINT_CUSTOMIZE_TITLE")
                    {
                        __instance.Image.sprite = Helper.CreateSpriteFromImageFile("Hint_DeliriumUI.png");
                    }
                    else if (step.StepType == GeoscapeTutorialStepType.AlienBaseDiplomacyPenalty && step.Title.LocalizationKey == "KEY_GEO_HINT_PANDORAN_BASE_DIPLOMACY_EFFECTS_TITLE")
                    {
                        __instance.Image.sprite = Helper.CreateSpriteFromImageFile("Hint_PandoranEvolution.png");
                    }
                    else if (step.StepType == GeoscapeTutorialStepType.HarvestingSiteCaptured || step.StepType == GeoscapeTutorialStepType.RefineryCaptured)
                    {
                        __instance.Image.sprite = Helper.CreateSpriteFromImageFile("background_ancients_hint.jpg");
                    }


                    /*  else if (step.StepType == GeoscapeTutorialStepType.HarvestingSiteCaptured) 
                      {
                          __instance.CancelInvoke();      
                      }
                      else if (step.StepType == GeoscapeTutorialStepType.HarvestingSiteCaptured)
                              {
                                  __instance.CancelInvoke();
                              }*/

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleContextHelp), "ShowPanel")]
        public static class UIModuleContextHelp_Show_Hints_Patch
        {
            public static void Postfix(UIModuleContextHelp __instance, object ____context)
            {
                try
                {
                    ContextHelpHintDef hintDef = ____context as ContextHelpHintDef;

                    if (hintDef != null)
                    {


                        bool tacticsHintWasShown = false;
                        TFTVLogger.Always("Show hint method invoked, the hint is " + hintDef.name);
                        //  TFTVLogger.Always("There are " + TFTVHumanEnemies.TacticsHint.Count + " hints in the human tactics list");

                        if (hintDef.name.Contains("InfestationMissionIntro"))// && !TacticalHintsToShow.Contains(hintDef.name))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("px_squad.jpg");
                            hintDef.Trigger = HintTrigger.Manual;
                            // TacticalHintsToShow.Add(hintDef.name);
                        }

                        else if (hintDef.name.Contains("InfestationMissionEnd"))// && !TacticalHintsToShow.Contains(hintDef.name))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("px_squad.jpg");
                            hintDef.Trigger = HintTrigger.Manual;
                            // TacticalHintsToShow.Add(hintDef.name);
                        }

                        else if (hintDef.Title.LocalizationKey == "UMBRA_SIGHTED_TITLE")// && !TacticalHintsToShow.Contains(hintDef.name))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("VO_15.jpg");
                            // TacticalHintsToShow.Add(hintDef.name);
                        }

                        else if (hintDef.Title.LocalizationKey == "REVENANT_SIGHTED_TITLE")// && !TacticalHintsToShow.Contains(hintDef.name))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Hint_Revenant.png");
                            // TacticalHintsToShow.Add(hintDef.name);
                        }

                        else if (hintDef.name.Contains("TFTV_StaminaHintDef"))// && !TacticalHintsToShow.Contains(hintDef.name))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("broken_limb_stamina.png");
                            // TacticalHintsToShow.Add(hintDef.name);
                        }

                        else if (hintDef.name.Contains("RevenantResistanceSighted"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Hint_Revenant.png");
                            alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintDef);
                        }

                        else if (hintDef.name.Contains("VoidTouchedSighted"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Hint_TBTV.png");

                        }
                        else if (hintDef.name.Contains("VoidTouchedOnAttack"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Hint_TBTV_MfD.png");
                        }
                        else if (hintDef.name.Contains("VoidTouchedOnTurnEnd"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Hint_TBTV_EoT.png");
                        }
                        else if (hintDef.name.Contains("TUT_DLC4_Acheron_HintDef"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Acheron.png");
                        }
                        else if (hintDef.name.Contains("AcheronPrime"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("AcheronPrime.png"); //Prime.png");
                        }
                        else if (hintDef.name.Equals("AcheronAchlys"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("AcheronAchlys.png");
                        }
                        else if (hintDef.name.Equals("AcheronAchlysChampion"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("AcheronAchlysChampion.png");
                        }
                        else if (hintDef.name.Equals("AcheronAsclepius"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("AcheronAsclepius.png");
                        }
                        else if (hintDef.name.Equals("AcheronAsclepiusChampion"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("AcheronAsclepiusChampion.png");
                        }
                        else if (hintDef.name.Equals("VoidBlight"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("acheron_void_blight.png");
                        }
                        else if (hintDef.name.Equals("TFTV_Tutorial1"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("alistair.png");
                        }
                        else if (hintDef.name.Equals("TFTV_Tutorial2"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Olena.png");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_STORY1"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_Story_1.jpg");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_CYCLOPS"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_CYCLOPSDEFENSE"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_HOPLITS"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_Hoplites.jpg");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_HOPLITSREPAIR"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg");
                        }
                        else if (hintDef.name.Equals("ANCIENTS_HOPLITSMAXPOWER"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg");
                        }
                        else if (hintDef.name.Equals("HostileDefenders"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("TFTV_Hint_HostileDefenders.jpg");
                        }
                        else if (hintDef.name.Equals("FIRE_QUENCHER"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("hint_firequencher.png");
                        }
                        else if (hintDef.name.Equals("BaseDefenseUmbraStrat") 
                            || hintDef.name.Equals("BaseDefenseWormsStrat") 
                            || hintDef.name.Equals("BaseDefenseForce2Strat")
                            || hintDef.name.Equals("BaseDefenseVenting"))
                        {
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Olena_static.jpg");
                        }
                        else if (hintDef.name.Equals("TFTVBaseDefense"))
                        {
                            if (TFTVBaseDefenseTactical.AttackProgress < 0.3)
                            {
                                __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("base_defense_hint.jpg");


                            }
                            else if (TFTVBaseDefenseTactical.AttackProgress >= 0.3 && TFTVBaseDefenseTactical.AttackProgress < 0.8)
                            {
                                __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("base_defense_hint_nesting.jpg");

                            }
                            else
                            {
                                __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("base_defense_hint_infestation.jpg");
                            }

                        }
                        else
                        {
                            __instance.Image.overrideSprite = null;//Helper.CreateSpriteFromImageFile("missing_hint_pic.jpg");
                        }



                        foreach (ContextHelpHintDef tacticsHint in TFTVHumanEnemies.TacticsHint)
                        {
                            if (tacticsHint.name == hintDef.name && !hintDef.Title.LocalizationKey.Contains("Should not appear"))  //hintDef.Text.LocalizeEnglish().Contains("Their leader is"))
                            {
                                //  TFTVLogger.Always("leaderSightedHint if check passed");

                                if (hintDef.Text.LocalizeEnglish().Contains("Synedrion"))
                                {
                                    __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("syn_squad.jpg");
                                }
                                else if (hintDef.Text.LocalizeEnglish().Contains("a pack of Forsaken"))
                                {
                                    __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("fo_squad.png");
                                }
                                else if (hintDef.Text.LocalizeEnglish().Contains("New Jericho"))
                                {
                                    __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("nj_squad.jpg");
                                }
                                else if (hintDef.Text.LocalizeEnglish().Contains("Disciples of Anu"))
                                {
                                    __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("anu_squad.jpg");
                                }
                                else if (hintDef.Text.LocalizeEnglish().Contains("an array of the Pure"))
                                {
                                    if (!hintDef.Text.LocalizeEnglish().Contains("You are finally facing Subject 24"))
                                    {
                                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("pu_squad.jpg");
                                    }
                                    else
                                    {
                                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("subject24_squad.png");
                                    }
                                }
                                else if (hintDef.Text.LocalizeEnglish().Contains("a gang"))
                                {
                                    __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("ban_squad.png");
                                }

                                tacticsHintWasShown = true;
                                alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintDef);
                            }

                        }
                        if (tacticsHintWasShown)
                        {
                            TFTVHumanEnemies.TacticsHint.Remove(hintDef);
                        }
                    }
                    else
                    {
                        __instance.Image.overrideSprite = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        public static ActorHasTemplateHintConditionDef ActorHasTemplateCreateNewConditionForTacticalHint(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ActorHasTemplateHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTemplateHintConditionDef, gUID, "ActorHasTemplate_" + name + "_HintConditionDef");
                TacCharacterDef tacCharacterDef = DefCache.GetDef<TacCharacterDef>(name);
                newActorHasTemplateHintConditionDef.TacActorDef = tacCharacterDef;
                return newActorHasTemplateHintConditionDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw new InvalidOperationException();
            }
        }

        public static ActorHasTagHintConditionDef ActorHasTagCreateNewConditionForTacticalHint(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ActorHasTagHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTagHintConditionDef, gUID, "ActorHasTag_" + name + "_HintConditionDef");
                GameTagDef gameTagDef = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(ged => ged.name.Equals(name));
                newActorHasTemplateHintConditionDef.GameTagDef = gameTagDef;

                return newActorHasTemplateHintConditionDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw new InvalidOperationException();
            }
        }

        public static ActorHasStatusHintConditionDef ActorHasStatusHintConditionDefCreateNewConditionForTacticalHint(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ActorHasStatusHintConditionDef newActorHasStatusHintConditionDef = Helper.CreateDefFromClone(sourceActorHasStatusHintConditionDef, gUID, "ActorHasStatus_" + name + "_HintConditionDef");
                StatusDef statusDef = DefCache.GetDef<StatusDef>(name);
                newActorHasStatusHintConditionDef.StatusDef = statusDef;

                return newActorHasStatusHintConditionDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw new InvalidOperationException();
            }
        }
        public static LevelHasTagHintConditionDef LevelHasTagHintConditionForTacticalHint(string name)
        {
            try
            {

                string gUID = Guid.NewGuid().ToString();

                LevelHasTagHintConditionDef newLevelTagCondition = Helper.CreateDefFromClone(sourceInfestationMission, gUID, name + "_HintConditionDef");

                newLevelTagCondition.GameTagDef = DefCache.GetDef<MissionTypeTagDef>(name);

                return newLevelTagCondition;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw new InvalidOperationException();
            }
        }




        public static void CreateNewTacticalHint(string name, HintTrigger trigger, string conditionName, string title, string text, int typeHint, bool oneTime, string gUID)
        {
            try
            {



                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);

                newContextHelpHintDef.Trigger = trigger;
                newContextHelpHintDef.Conditions = new List<HintConditionDef>() { };



                if (typeHint == 0)
                {
                    newContextHelpHintDef.Conditions.Add(ActorHasTemplateCreateNewConditionForTacticalHint(conditionName));
                }
                else if (typeHint == 1)
                {
                    newContextHelpHintDef.Conditions.Add(ActorHasTagCreateNewConditionForTacticalHint(conditionName));

                }
                else if (typeHint == 2)
                {
                    newContextHelpHintDef.Conditions.Add(ActorHasStatusHintConditionDefCreateNewConditionForTacticalHint(conditionName));
                }
                else if (typeHint == 3)
                {
                    newContextHelpHintDef.Conditions.Add(LevelHasTagHintConditionForTacticalHint(conditionName));

                }


                newContextHelpHintDef.Title.LocalizationKey = title;



                newContextHelpHintDef.Text.LocalizationKey = text;

                newContextHelpHintDef.AnyCondition = false;


                if (oneTime)
                {
                    string gUID2 = Guid.NewGuid().ToString();

                    HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                    newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                    newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);
                    newContextHelpHintDef.AnyCondition = false;
                }



                alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);



                //newContextHelpHintDef.NextHint = null;


                /*
                                    string m_Name = "TUT_DLC3_MissionStartStory_HintDef"
                    string Guid = "cacf2bbc-50c9-9144-6922-39ac7712e719"
                    string ResourcePath = "Defs/Gameplay/ContextHelp/TUT_DLC3_MissionStartStory_HintDef"
                    SInt32 Trigger = 40
                    bool AnyCondition = True
                    List`1 Conditions
                        Array Array
                        int size = 1
                            [0]
                            PPtr<HintConditionDef> data
                                int m_FileID = 0
                                SInt64 m_PathID = 37133
                    LocalizedTextBind Title
                        string LocalizationKey = "KEY_DLC3_TAC_HINT_MANTICORE_RUN_STORY_NAME"
                    LocalizedTextBind Text
                        string LocalizationKey = "KEY_DLC3_TAC_HINT_MANTICORE_RUN_STORY_DESCRIPTION"
                    LocalizedTextBind ControllerText
                        string LocalizationKey = ""
                    AssetReferenceSprite Image
                        string m_AssetGUID = "3c3d998d6c26dfc4990f5a1ce67007f5"
                        string m_SubObjectName = ""
                        string m_SubObjectType = ""
                    PPtr<VideoPlaybackSourceDef> VideoDef
                        int m_FileID = 0
                        SInt64 m_PathID = 0
                    bool DisplayAsModal = False
                    bool KeepUiInBackground = False
                    bool DelayUntilActorSelected = False
                    bool DiscardIfContextDies = False
                    bool IsTutorialHint = False
                    PPtr<BaseEventDef> EventusEventDef
                        int m_FileID = 0
                        SInt64 m_PathID = 0
                    PPtr<ContextHelpHintDef> NextHint
                        int m_FileID = 0
                        SInt64 m_PathID = 37247*/




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateNewManualTacticalHint(string name, string gUID, string titleKey, string textKey)
        {
            try
            {
                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = HintTrigger.Manual;
                newContextHelpHintDef.Conditions.Clear();
                newContextHelpHintDef.AnyCondition = true;
                newContextHelpHintDef.Title.LocalizationKey = titleKey;
                newContextHelpHintDef.Text.LocalizationKey = textKey;
                newContextHelpHintDef.IsTutorialHint = true;
                ContextHelpHintDbDef tacticalHintsDB = DefCache.GetDef<ContextHelpHintDbDef>("TacticalHintsDbDef");
                tacticalHintsDB.Hints.Add(newContextHelpHintDef);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateNewTacticalHintInfestationMission(string name, string gUID, string gUID2, string gUID3)
        {
            try

            {
                //  string gUID = Guid.NewGuid().ToString();

                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = HintTrigger.Manual;

                //  string gUID2 = Guid.NewGuid().ToString();
                LevelHasTagHintConditionDef infestedHavenMissionTagCondition = Helper.CreateDefFromClone(sourceInfestationMission, gUID2, name + "_HintConditionDef");
                infestedHavenMissionTagCondition.GameTagDef = infestationMissionTagDef;
                newContextHelpHintDef.Conditions[0] = infestedHavenMissionTagCondition;


                //  string gUID3 = Guid.NewGuid().ToString();
                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID3, name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                newContextHelpHintDef.AnyCondition = false;


                alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateNewTacticalHintInfestationMissionEnd(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = HintTrigger.Manual;

                string gUID2 = Guid.NewGuid().ToString();
                LevelHasTagHintConditionDef infestedHavenMissionTagCondition = Helper.CreateDefFromClone(sourceInfestationMission, gUID2, name + "_HintConditionDef");
                infestedHavenMissionTagCondition.GameTagDef = infestationMissionTagDef;
                newContextHelpHintDef.Conditions[0] = infestedHavenMissionTagCondition;


                string gUID3 = Guid.NewGuid().ToString();
                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID3, name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                newContextHelpHintDef.AnyCondition = false;

                alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateNewTacticalHintForRevenantResistance(string name, HintTrigger trigger, string conditionName, string title, string text)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = trigger;

                newContextHelpHintDef.Conditions[0] = ActorHasTagCreateNewConditionForTacticalHint(conditionName);

                newContextHelpHintDef.Conditions.Add(ActorHasTagCreateNewConditionForTacticalHint("RevenantResistance_GameTagDef"));

                string gUID2 = Guid.NewGuid().ToString();

                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                newContextHelpHintDef.Title = new LocalizedTextBind(title, true);
                newContextHelpHintDef.Text = new LocalizedTextBind(text, true);


                newContextHelpHintDef.AnyCondition = false;


                alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateNewTacticalHintForHumanEnemies(string name, HintTrigger trigger, string conditionName, string title, string text)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();

                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = trigger;

                newContextHelpHintDef.Conditions[0] = ActorHasTagCreateNewConditionForTacticalHint(conditionName);

                newContextHelpHintDef.Conditions.Add(ActorHasTagCreateNewConditionForTacticalHint("HumanEnemy_GameTagDef"));

                string gUID2 = Guid.NewGuid().ToString();

                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                newContextHelpHintDef.Title = new LocalizedTextBind(title, true);
                newContextHelpHintDef.Text = new LocalizedTextBind(text, true);


                newContextHelpHintDef.AnyCondition = false;


                alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
