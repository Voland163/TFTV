﻿using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.ContextHelp.HintConditions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVTutorialAndStory
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
      // public static string hintShow = "";

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

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

       

        [HarmonyPatch(typeof(UIModuleContextHelp), "Show")]
        public static class UIModuleContextHelp_Show_Hints_Patch
        {
            
            public static void Postfix(LocalizedTextBind title, UIModuleContextHelp __instance)
            {
                try
                {
                    TFTVLogger.Always("Show hint method invoked");
                   ContextHelpHintDef leaderSightedHint = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("LeaderSighted"));
                    TFTVLogger.Always(leaderSightedHint.Title.LocalizeEnglish());
                    TFTVLogger.Always(title.LocalizeEnglish());

                    if (title.LocalizationKey == "UMBRA_SIGHTED_TITLE")
                    {
                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Umbra_hint.jpg");
                    }

                    if (title.LocalizationKey == "REVENANT_SIGHTED_TITLE")
                    {
                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Umbra_hint.jpg");
                    }

                    foreach (string name in TFTVHumanEnemiesNames.nouns) 
                    {
                        if (title.LocalizeEnglish().Contains(name)) 
                        {

                       
                            TFTVLogger.Always("leaderSightedHint if check passed");
                         //   image = Helper.CreateSpriteFromImageFile("Umbra_family.png");
                            __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile(TFTVHumanEnemies.FileNameSquadPic);
                           leaderSightedHint.AnyCondition = false;

                        }
                    
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void CreateHints()

        {
            try
            {
                CreateNewTacticalHint("UmbraSighted", HintTrigger.ActorSeen, "Oilcrab_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT",0, true);
                CreateNewTacticalHint("RevenantSighted", HintTrigger.ActorSeen, "RevenantTier_1_GameTagDef", "REVENANT_SIGHTED_TITLE", "REVENANT_SIGHTED_TEXT", 1, true);
                CreateNewTacticalHint("LeaderSighted", HintTrigger.ActorSeen, "HumanEnemyTier_1_GameTagDef", "Should not appear", "Should not appear", 1, false);
               // ContextHelpHintDef leaderSightedHint = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("LeaderSighted"));
               // leaderSightedHint.AnyCondition = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static ActorHasTemplateHintConditionDef ActorHasTemplateCreateNewConditionForTacticalHint(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();
                ActorHasTemplateHintConditionDef sourceActorHasTemplateHintConditionDef = Repo.GetAllDefs<ActorHasTemplateHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("ActorHasTemplate_Fishman2_Sneaker_AlienMutationVariationDef_HintConditionDef"));
                ActorHasTemplateHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTemplateHintConditionDef, gUID, "ActorHasTemplate_" + name + "_HintConditionDef");
                TacCharacterDef tacCharacterDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(ged => ged.name.Equals(name));
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

                
                ActorHasTagHintConditionDef sourceActorHasTemplateHintConditionDef = Repo.GetAllDefs<ActorHasTagHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef"));
                ActorHasTagHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTemplateHintConditionDef, gUID, "ActorHasTag_" + name + "_HintConditionDef");
                GameTagDef gameTagDef = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(ged => ged.name.Contains(name));
                newActorHasTemplateHintConditionDef.GameTagDef = gameTagDef;
               
                return newActorHasTemplateHintConditionDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw new InvalidOperationException();
            }
        }




        public static void CreateNewTacticalHint(string name, HintTrigger trigger, string conditionName, string title, string text, int typeHint, bool oneTime)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();
                ContextHelpHintDef sourceContextHelpHintDef = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("TUT_DLC3_MissionStartStory_HintDef"));
                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = trigger;
                if (typeHint == 0)
                {
                    newContextHelpHintDef.Conditions[0] = ActorHasTemplateCreateNewConditionForTacticalHint(conditionName);
                }
                else 
                {
                    newContextHelpHintDef.Conditions[0] = ActorHasTagCreateNewConditionForTacticalHint(conditionName);
                }
                newContextHelpHintDef.Title.LocalizationKey = title;
                newContextHelpHintDef.Text.LocalizationKey = text;
                //  newContextHelpHintDef.AnyCondition = false;
                string gUID2 = Guid.NewGuid().ToString();
                HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = Repo.GetAllDefs<HasSeenHintHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef"));
                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                if (oneTime) 
                {
                       newContextHelpHintDef.AnyCondition = false;   
                }


                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = Repo.GetAllDefs<ContextHelpHintDbDef>().FirstOrDefault(ged => ged.name.Equals("AlwaysDisplayedTacticalHintsDbDef"));
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

    }
}
