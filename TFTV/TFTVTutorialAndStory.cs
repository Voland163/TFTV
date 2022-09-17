using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.ContextHelp.HintConditions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVTutorialAndStory
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = Repo.GetAllDefs<ContextHelpHintDbDef>().FirstOrDefault(ged => ged.name.Equals("AlwaysDisplayedTacticalHintsDbDef"));
        // public static string hintShow = "";


        private static readonly ActorHasTemplateHintConditionDef sourceActorHasTemplateHintConditionDef = Repo.GetAllDefs<ActorHasTemplateHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("ActorHasTemplate_Fishman2_Sneaker_AlienMutationVariationDef_HintConditionDef"));
        private static readonly ActorHasTagHintConditionDef sourceActorHasTagHintConditionDef = Repo.GetAllDefs<ActorHasTagHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef"));
        private static readonly ContextHelpHintDef sourceContextHelpHintDef = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("TUT_DLC3_MissionStartStory_HintDef"));
        private static readonly HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = Repo.GetAllDefs<HasSeenHintHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef"));


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

            public static void Postfix(LocalizedTextBind title, UIModuleContextHelp __instance, object context)
            {
                try
                {
                    

                    ContextHelpHintDef hintDef = context as ContextHelpHintDef;
                    bool tacticsHintWasShown = false;
                    TFTVLogger.Always("Show hint method invoked, the hint is " + hintDef.name);
                    TFTVLogger.Always("There are " + TFTVHumanEnemies.TacticsHint.Count + " hints in the human tactics list");

                    for (int i = 0; i < TFTVHumanEnemies.TacticsHint.Count; i++) 
                    {
                        TFTVLogger.Always("The hint # " + (i+1) + " is " + TFTVHumanEnemies.TacticsHint[i].name);

                    }
                    
                    if (title.LocalizationKey == "UMBRA_SIGHTED_TITLE")
                    {
                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Umbra_hint.jpg");
                    }

                    if (title.LocalizationKey == "REVENANT_SIGHTED_TITLE")
                    {
                        __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("Umbra_hint.jpg");
                    }

                    foreach(ContextHelpHintDef tacticsHint in TFTVHumanEnemies.TacticsHint) 
                    {
                        if (tacticsHint.name==hintDef.name)  //hintDef.Text.LocalizeEnglish().Contains("Their leader is"))
                        {
                            TFTVLogger.Always("leaderSightedHint if check passed");

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
                                __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("pu_squad.jpg");
                            }
                            else if (hintDef.Text.LocalizeEnglish().Contains("a gang"))
                            {
                                __instance.Image.overrideSprite = Helper.CreateSpriteFromImageFile("ban_squad.png");
                            }
                            tacticsHintWasShown = true;
                        }
                    }
                    if (tacticsHintWasShown)
                    {
                       
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintDef);
                        TFTVHumanEnemies.TacticsHint.Remove(hintDef);
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
                CreateNewTacticalHint("UmbraSighted", HintTrigger.ActorSeen, "Oilcrab_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true);
                CreateNewTacticalHint("RevenantSighted", HintTrigger.ActorSeen, "RevenantTier_1_GameTagDef", "REVENANT_SIGHTED_TITLE", "REVENANT_SIGHTED_TEXT", 1, true);
                //  CreateNewTacticalHint("LeaderSighted", HintTrigger.ActorSeen, "HumanEnemy_GameTagDef", "Should not appear", "Should not appear", 1, false);
                //   ContextHelpHintDef leaderSightedHint = Repo.GetAllDefs<ContextHelpHintDef>().FirstOrDefault(ged => ged.name.Equals("LeaderSighted"));
                //   leaderSightedHint.AnyCondition = false;
                //  leaderSightedHint.Conditions[0] = ActorIsOfFactionCreateNewConditionForTacticalHint("AN_FallenOnes_TacticalFactionDef");
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("Anu_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NEU_Bandits_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NJ_Purists_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("NewJericho_TacticalFactionDef"));
                //  leaderSightedHint.Conditions.Add(ActorIsOfFactionCreateNewConditionForTacticalHint("Synedrion_TacticalFactionDef"));
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

        public static ActorIsOfFactionHintConditionDef ActorIsOfFactionCreateNewConditionForTacticalHint(string name)
        {
            try

            {
                string gUID = Guid.NewGuid().ToString();


                ActorIsOfFactionHintConditionDef sourceActorIsOfFactionConditionDef = Repo.GetAllDefs<ActorIsOfFactionHintConditionDef>().FirstOrDefault(ged => ged.name.Equals("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef"));
                ActorIsOfFactionHintConditionDef newActorIsOfFactionHintConditionDef = Helper.CreateDefFromClone(sourceActorIsOfFactionConditionDef, gUID, "ActorIsOfFaction_" + name + "_HintConditionDef");
                TacticalFactionDef tacticalFactionDef = Repo.GetAllDefs<TacticalFactionDef>().FirstOrDefault(ged => ged.name.Contains(name));
                newActorIsOfFactionHintConditionDef.TacticalFactionDef = tacticalFactionDef;

                return newActorIsOfFactionHintConditionDef;
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
                
                ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                newContextHelpHintDef.Trigger = trigger;
                if (typeHint == 0)
                {
                    newContextHelpHintDef.Conditions[0] = ActorHasTemplateCreateNewConditionForTacticalHint(conditionName);
                }
                else if (typeHint == 1)
                {
                    newContextHelpHintDef.Conditions[0] = ActorHasTagCreateNewConditionForTacticalHint(conditionName);
                }
                else
                {

                }
                newContextHelpHintDef.Title.LocalizationKey = title;
                newContextHelpHintDef.Text.LocalizationKey = text;
                // newContextHelpHintDef.AnyCondition = false;

                if (oneTime)
                {
                    string gUID2 = Guid.NewGuid().ToString();
                    
                    HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                    newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                    newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);
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

                newContextHelpHintDef.Title.LocalizationKey = title;
                newContextHelpHintDef.Text.LocalizationKey = text;


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
