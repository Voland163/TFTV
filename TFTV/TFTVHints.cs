using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.ContextHelp.HintConditions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.ContextHelp.HintConditions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

namespace TFTV
{

    internal class TFTVHints
    {
        // public static List<string> TacticalHintsToShow = new List<string>();
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
        // public static string hintShow = "";
        internal static Dictionary<ContextHelpHintDef, string> _hintDefSpriteFileNameDictionary = new Dictionary<ContextHelpHintDef, string>();

        internal class HintDefs
        {
            private static readonly ActorHasTemplateHintConditionDef sourceActorHasTemplateHintConditionDef = DefCache.GetDef<ActorHasTemplateHintConditionDef>("ActorHasTemplate_Fishman2_Sneaker_AlienMutationVariationDef_HintConditionDef");
            private static readonly ActorHasTagHintConditionDef sourceActorHasTagHintConditionDef = DefCache.GetDef<ActorHasTagHintConditionDef>("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef");
            private static readonly ActorHasStatusHintConditionDef sourceActorHasStatusHintConditionDef = DefCache.GetDef<ActorHasStatusHintConditionDef>("ActorHasStatus_CorruptionAttack_StatusDef_HintConditionDef");

            private static readonly ContextHelpHintDef sourceContextHelpHintDef = DefCache.GetDef<ContextHelpHintDef>("TUT_DLC3_MissionStartStory_HintDef");
            private static readonly HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef");
            private static readonly LevelHasTagHintConditionDef sourceInfestationMission = DefCache.GetDef<LevelHasTagHintConditionDef>("LevelHasTag_MissionTypeBaseInfestation_MissionTagDef_HintConditionDef");
            private static readonly MissionTypeTagDef infestationMissionTagDef = DefCache.GetDef<MissionTypeTagDef>("HavenInfestation_MissionTypeTagDef");

            public static void CreateHints()
            {
                try
                {
                    CreateMainHints();
                    CreatePalaceMissionHints();
                    CreateHintsForBaseDefense();
                    CreateFireQuencherHint();
                    CreateLOTAHints();
                    CreateTBTVHints();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw new InvalidOperationException();
                }
            }

            internal class Constructors
            {
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
                public static IsDefHintConditionDef IsDefConditionForTacticalHint(string name)
                {
                    try
                    {
                        string gUID = Guid.NewGuid().ToString();

                        IsDefHintConditionDef source = DefCache.GetDef<IsDefHintConditionDef>("IsDef_Overwatch_AbilityDef_HintConditionDef");

                        IsDefHintConditionDef newIsDefCondition = Helper.CreateDefFromClone(source, gUID, name + "_HintConditionDef");

                        return newIsDefCondition;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw new InvalidOperationException();
                    }
                }

            }

            internal class DynamicallyCreatedHints
            {
                public static ContextHelpHintDef CreateNewTacticalHintForHumanEnemies(string name, HintTrigger trigger, string conditionName, string title, string text, string spriteFileName)
                {
                    try
                    {
                        string gUID = Guid.NewGuid().ToString();

                        ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                        newContextHelpHintDef.Trigger = trigger;

                        newContextHelpHintDef.Conditions[0] = Constructors.ActorHasTagCreateNewConditionForTacticalHint(conditionName);

                        newContextHelpHintDef.Conditions.Add(Constructors.ActorHasTagCreateNewConditionForTacticalHint("HumanEnemy_GameTagDef"));

                        string gUID2 = Guid.NewGuid().ToString();

                        HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                        newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                        newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                        newContextHelpHintDef.Title = new LocalizedTextBind(title, true);
                        newContextHelpHintDef.Text = new LocalizedTextBind(text, true);

                        newContextHelpHintDef.AnyCondition = false;

                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);
                        _hintDefSpriteFileNameDictionary.Add(newContextHelpHintDef, spriteFileName);

                        return newContextHelpHintDef;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static void CreateNewTacticalHintForRevenantResistance(string name, HintTrigger trigger, string conditionName, string title, string text)
                {
                    try
                    {
                        string gUID = Guid.NewGuid().ToString();

                        ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                        newContextHelpHintDef.Trigger = trigger;

                        newContextHelpHintDef.Conditions[0] = Constructors.ActorHasTagCreateNewConditionForTacticalHint(conditionName);

                        newContextHelpHintDef.Conditions.Add(Constructors.ActorHasTagCreateNewConditionForTacticalHint("RevenantResistance_GameTagDef"));

                        string gUID2 = Guid.NewGuid().ToString();

                        HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID2, name + "HasSeenHintConditionDef");
                        newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                        newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                        newContextHelpHintDef.Title = new LocalizedTextBind(title, true);
                        newContextHelpHintDef.Text = new LocalizedTextBind(text, true);

                        newContextHelpHintDef.AnyCondition = false;

                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);

                        if (!_hintDefSpriteFileNameDictionary.ContainsKey(newContextHelpHintDef))
                        {
                            _hintDefSpriteFileNameDictionary.Add(newContextHelpHintDef, "Hint_Revenant.jpg");
                        }

                        TFTVRevenant.revenantResistanceHintGUID = gUID;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            public static ContextHelpHintDef CreateNewTacticalHint(string name, HintTrigger trigger, string conditionName, string title, string text, int typeHint, bool oneTime, string gUID, string spriteFileName)
            {
                try
                {
                    ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);

                    newContextHelpHintDef.Trigger = trigger;
                    newContextHelpHintDef.Conditions = new List<HintConditionDef>() { };

                    if (typeHint == 0)
                    {
                        newContextHelpHintDef.Conditions.Add(Constructors.ActorHasTemplateCreateNewConditionForTacticalHint(conditionName));
                    }
                    else if (typeHint == 1)
                    {
                        newContextHelpHintDef.Conditions.Add(Constructors.ActorHasTagCreateNewConditionForTacticalHint(conditionName));
                    }
                    else if (typeHint == 2)
                    {
                        newContextHelpHintDef.Conditions.Add(Constructors.ActorHasStatusHintConditionDefCreateNewConditionForTacticalHint(conditionName));
                    }
                    else if (typeHint == 3)
                    {
                        newContextHelpHintDef.Conditions.Add(Constructors.LevelHasTagHintConditionForTacticalHint(conditionName));
                    }
                    else if (typeHint == 4)
                    {
                        newContextHelpHintDef.Conditions.Add(Constructors.IsDefConditionForTacticalHint(conditionName));
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

                    _hintDefSpriteFileNameDictionary.Add(newContextHelpHintDef, spriteFileName);

                    return newContextHelpHintDef;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static ContextHelpHintDef CreateNewManualTacticalHint(string name, string gUID, string titleKey, string textKey, string spriteFileName)
            {
                try
                {
                    ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                    newContextHelpHintDef.Trigger = HintTrigger.Manual;
                    newContextHelpHintDef.Conditions.Clear();
                    newContextHelpHintDef.AnyCondition = false;
                    newContextHelpHintDef.Title.LocalizationKey = titleKey;
                    newContextHelpHintDef.Text.LocalizationKey = textKey;
                    newContextHelpHintDef.IsTutorialHint = false;
                    ContextHelpHintDbDef tacticalHintsDB = DefCache.GetDef<ContextHelpHintDbDef>("TacticalHintsDbDef");
                    tacticalHintsDB.Hints.Add(newContextHelpHintDef);
                    // alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef );

                    _hintDefSpriteFileNameDictionary.Add(newContextHelpHintDef, spriteFileName);

                    return newContextHelpHintDef;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void CreateNewTacticalHintInfestationMission(string name, string gUID, string gUID2, string gUID3)
            {
                try
                {
                    ContextHelpHintDef newContextHelpHintDef = Helper.CreateDefFromClone(sourceContextHelpHintDef, gUID, name);
                    newContextHelpHintDef.Trigger = HintTrigger.Manual;

                    LevelHasTagHintConditionDef infestedHavenMissionTagCondition = Helper.CreateDefFromClone(sourceInfestationMission, gUID2, name + "_HintConditionDef");
                    infestedHavenMissionTagCondition.GameTagDef = infestationMissionTagDef;
                    newContextHelpHintDef.Conditions[0] = infestedHavenMissionTagCondition;

                    HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, gUID3, name + "HasSeenHintConditionDef");
                    newHasSeenHintConditionDef.HintDef = newContextHelpHintDef;
                    newContextHelpHintDef.Conditions.Add(newHasSeenHintConditionDef);

                    newContextHelpHintDef.AnyCondition = false;

                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(newContextHelpHintDef);
                    _hintDefSpriteFileNameDictionary.Add(newContextHelpHintDef, "px_squad.jpg");
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


            public static void CreateMainHints()
            {
                try
                {
                    ContextHelpHintDef oilCrabHint = CreateNewTacticalHint("UmbraSighted", HintTrigger.ActorSeen, "Oilcrab_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true, "C63F5953-9D29-4245-8FCD-1B8B875C007D", "VO_15.jpg");
                    ContextHelpHintDef oilFishHint = CreateNewTacticalHint("UmbraSightedTriton", HintTrigger.ActorSeen, "Oilfish_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true, "7F85AF7F-D7F0-41F3-B6EF-839509FCCF00", "VO_15.jpg");
                    CreateNewTacticalHint("AcheronPrime", HintTrigger.ActorSeen, "AcheronPrime_TacCharacterDef", "HINT_ACHERON_PRIME_TITLE", "HINT_ACHERON_PRIME_DESCRIPTION", 0, true, "0266C7C5-B5A4-41B8-9987-653248113CC5", "AcheronPrime.jpg");
                    CreateNewTacticalHint("AcheronAsclepius", HintTrigger.ActorSeen, "AcheronAsclepius_TacCharacterDef", "HINT_ACHERON_ASCLEPIUS_TITLE", "HINT_ACHERON_ASCLEPIUS_DESCRIPTION", 0, true, "F34ED218-BF6D-44CD-B653-9EC8C7AB0D84", "AcheronAsclepius.jpg");
                    CreateNewTacticalHint("AcheronAsclepiusChampion", HintTrigger.ActorSeen, "AcheronAsclepiusChampion_TacCharacterDef", "HINT_ACHERON_ASCLEPIUS_CHAMPION_TITLE", "HINT_ACHERON_ASCLEPIUS_CHAMPION_DESCRIPTION", 0, true, "2FA6F938-0928-4C3A-A514-91F3BD90E048", "AcheronAsclepiusChampion.jpg");
                    CreateNewTacticalHint("AcheronAchlys", HintTrigger.ActorSeen, "AcheronAchlys_TacCharacterDef", "HINT_ACHERON_ACHLYS_TITLE", "HINT_ACHERON_ACHLYS_DESCRIPTION", 0, true, "06EEEA6B-1264-4616-AC78-1A2A56911E72", "AcheronAchlys.jpg");
                    CreateNewTacticalHint("AcheronAchlysChampion", HintTrigger.ActorSeen, "AcheronAchlysChampion_TacCharacterDef", "HINT_ACHERON_ACHLYS_CHAMPION_TITLE", "HINT_ACHERON_ACHLYS_CHAMPION_DESCRIPTION", 0, true, "760FDBB6-1556-4B1D-AFE0-59C906672A5D", "AcheronAchlysChampion.jpg");
                    CreateNewTacticalHint("RevenantSighted", HintTrigger.ActorSeen, TFTVRevenant.AnyRevenantGameTag.name, "REVENANT_SIGHTED_TITLE", "REVENANT_SIGHTED_TEXT", 1, true, "194317EC-67DF-4775-BAFD-98499F82C2D7", "Hint_Revenant.jpg");

                    _hintDefSpriteFileNameDictionary.Add(DefCache.GetDef<ContextHelpHintDef>("TUT_DLC4_Acheron_HintDef"), "Acheron.jpg");

                    CreateNewTacticalHintInfestationMission("InfestationMissionIntro", "BBC5CAD0-42FF-4BBB-8E13-7611DC5695A6", "1ED63949-4375-4A9D-A017-07CF483F05D5", "2A01E924-A26B-44FB-AD67-B1B590B4E1D5");
                    CreateNewTacticalHintInfestationMission("InfestationMissionIntro2", "164A4170-F7DC-4350-90C0-D5C1A0284E0D", "CA236EF2-6E6B-4CE4-89E9-17157930F91A", "422A7D39-0110-4F5B-98BB-66B1B5F616DD");
                    ContextHelpHintDef tutorialTFTV1 = CreateNewManualTacticalHint("TFTV_Tutorial1", "0D36F3D5-9A39-4A5C-B6A4-85B5A3007655", "KEY_TUT3_TFTV1_TITLE", "KEY_TUT3_TFTV1_DESCRIPTION", "alistair.jpg");
                    ContextHelpHintDef tutorialTFTV2 = CreateNewManualTacticalHint("TFTV_Tutorial2", "EA319607-D2F3-4293-AECE-91AC26C9BD5E", "KEY_TUT3_TFTV2_TITLE", "KEY_TUT3_TFTV2_DESCRIPTION", "Olena.jpg");

                    ContextHelpHintDef tutorial3MissionEnd = DefCache.GetDef<ContextHelpHintDef>("TUT3_MissionSuccess_HintDef");
                    tutorial3MissionEnd.NextHint = tutorialTFTV1;
                    tutorialTFTV1.NextHint = tutorialTFTV2;
                    tutorialTFTV1.Conditions = tutorial3MissionEnd.Conditions;
                    tutorialTFTV2.Conditions = tutorial3MissionEnd.Conditions;
                    tutorialTFTV1.IsTutorialHint = true;
                    tutorialTFTV2.IsTutorialHint = true;

                    HasSeenHintHintConditionDef seenOilCrabConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("UmbraSightedHasSeenHintConditionDef");
                    HasSeenHintHintConditionDef seenFishCrabConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("UmbraSightedTritonHasSeenHintConditionDef");

                    oilCrabHint.Conditions.Add(seenFishCrabConditionDef);
                    oilFishHint.Conditions.Add(seenOilCrabConditionDef);

                    CreateNewTacticalHintInfestationMissionEnd("InfestationMissionEnd");
                    CreateStaminaHint();
                    CreateUIDeliriumHint();

                    ContextHelpHintDef hostileDefenders = CreateNewTacticalHint("HostileDefenders", HintTrigger.MissionStart, "MissionTypeHavenDefense_MissionTagDef", "HINT_HOSTILE_DEFENDERS_TITLE", "HINT_HOSTILE_DEFENDERS_TEXT", 3, true, "F2F5E5B1-5B9B-4F5B-8F5C-9B5E5B5F5B5F", "TFTV_Hint_HostileDefenders.jpg");
                    alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hostileDefenders);

                    ContextHelpHintDef hatchingHint1 = DefCache.GetDef<ContextHelpHintDef>("TUT_DLC3_MissionStartStory_HintDef"); //KEY_DLC3_TAC_HINT_MANTICORE_RUN_STORY_NAME CHANGE OF PLANS
                    ContextHelpHintDef hatchingHint2 = DefCache.GetDef<ContextHelpHintDef>("TUT_DLC3_MissionStart_HintDef"); //KEY_DLC3_TAC_HINT_MANTICORE_RUN_START_NAME  AURORA

                    //For some reason the hint is played anyway, but without breaking the chain, it is played in the wrong order.
                    hatchingHint1.NextHint = null;
                    
              
                   

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreatePalaceIntroHint(MissionTypeTagDef missionTag, string hintName, string titleKey, string textKey, string textKey2, string gUID1, string gUID2, string spriteFileName)
            {
                try
                {
                    ContextHelpHintDef palaceStart0Hint = CreateNewTacticalHint(hintName + "0", HintTrigger.MissionStart, missionTag.name, titleKey, textKey, 3, false, gUID1, $"{spriteFileName}0.jpg");
                    ContextHelpHintDef palaceStart1Hint = CreateNewManualTacticalHint(hintName + "1", gUID2, titleKey, textKey2, $"{spriteFileName}1.jpg");

                    palaceStart0Hint.AnyCondition = true;
                    // palaceStart1Hint.IsTutorialHint = false;
                    palaceStart1Hint.Conditions = new List<HintConditionDef>() { Constructors.LevelHasTagHintConditionForTacticalHint(missionTag.name) };
                    palaceStart1Hint.AnyCondition = true;
                    palaceStart0Hint.NextHint = palaceStart1Hint;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreatePalaceMissionHints()
            {
                try
                {
                    CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("PXPalace"),
                        "TFTVPXPalaceStart",
                        "PX_VICTORY_MISSION_START_TITLE", "PX_VICTORY_MISSION_START0", "PX_VICTORY_MISSION_START1",
                        "{71C7DB4D-1C0D-4AF0-BE4E-2BB90E96CF61}",
                        "{A5AE9410-69F7-4DDF-9517-A85B8ADA118A}", "PX_VICTORY_START");

                    CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("NJPalace"),
                       "TFTVNJPalaceStart",
                       "NJ_VICTORY_MISSION_START_TITLE", "NJ_VICTORY_MISSION_START0", "NJ_VICTORY_MISSION_START1",
                       "{C38C52CA-8CFA-4F4D-867F-024ED8BB1FFA}",
                       "{67041692-4508-4D90-AEFA-9E145DA5E830}", "NJ_VICTORY_START");

                    CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("ANPalace"),
                      "TFTVANPalaceStart",
                      "AN_VICTORY_MISSION_START_TITLE", "AN_VICTORY_MISSION_START0", "AN_VICTORY_MISSION_START1",
                      "{8C41089E-BD4A-4D99-A066-17C10570F10B}",
                      "{87256303-4DD6-4EDC-B907-F8C02F8CFD02}", "AN_VICTORY_START");

                    CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("SYPolyPalace"),
                     "TFTVSYPolyPalaceStart",
                     "SY_POLY_VICTORY_MISSION_START_TITLE", "SY_POLY_VICTORY_MISSION_START0", "SY_POLY_VICTORY_MISSION_START1",
                     "{BEDF6DAD-9DF4-41C6-9A81-5913B0B8253A}",
                     "{D6C6CC71-A471-45CB-A59D-6EB52C3075EE}", "SY_POLY_VICTORY_START");

                    CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("SYTerraPalace"),
                     "TFTVSYTerraPalaceStart",
                     "SY_TERRA_VICTORY_MISSION_START_TITLE", "SY_TERRA_VICTORY_MISSION_START0", "SY_TERRA_VICTORY_MISSION_START1",
                     "{634FF698-80B8-4859-8ACF-956B16BD5B90}",
                     "{CBE4D317-A0A4-49D0-963D-9EE646D601B8}", "SY_Terra_VICTORY_START");

                    string nameGateHint0 = "ReceptacleGateHint0";
                    string nameGateHint1 = "ReceptacleGateHint1";
                    ContextHelpHintDef palaceGateHint0 = CreateNewManualTacticalHint(nameGateHint0, "{589E3AA7-07AB-4F36-9C22-05937FE77486}", "VICTORY_MISSION_GATES_TITLE", "VICTORY_MISSION_GATES0", "VICTORY_GATE.jpg");
                    ContextHelpHintDef palaceGateHint1 = CreateNewManualTacticalHint(nameGateHint1, "{8861E55F-486A-4A53-991C-E94F9917CFF1}", "VICTORY_MISSION_GATES_TITLE", "VICTORY_MISSION_GATES1", "VICTORY_GATE.jpg");

                    string nameRevenantHint0 = "PalaceRevenantHint0";
                    string nameRevenantHint1 = "PalaceRevenantHint1";

                    ContextHelpHintDef palaceRevenantHint0 = CreateNewManualTacticalHint(nameRevenantHint0, "{7D5440F0-DF8B-44E2-BB67-A02F72FB1628}", "VICTORY_MISSION_REVENANT_TO_PX_TITLE", "VICTORY_MISSION_REVENANT_TO_PX", "VICTORY_REVENANT_TO_PX.jpg");
                    ContextHelpHintDef palaceRevenantHint1 = CreateNewManualTacticalHint(nameRevenantHint1, "{8B9B2ACE-7790-4F1A-A5F4-4835FB16F972}", "VICTORY_MISSION_REVENANT_TO_YR_TITLE", "VICTORY_MISSION_REVENANT_TO_YR", "Hint_Revenant.jpg");

                    string nameHisMinionsHint = "PalaceHisMinionsHint";
                    ContextHelpHintDef palaceHisMinionsHint = CreateNewManualTacticalHint(nameHisMinionsHint, "{9EB02D9C-CC19-4D2F-920F-32A8227B685C}", "VICTORY_MISSION_HIS_MINIONS_TITLE", "VICTORY_MISSION_HIS_MINIONS", "VICTORY_MINIONS.jpg");

                    string nameEyesHint = "PalaceEyesHint";
                    string nameTag = "Yuggothian_ClassTagDef";

                    CreateNewTacticalHint(nameEyesHint, HintTrigger.ActorSeen, nameTag, "VICTORY_MISSION_FOR_THE_EYES_TITLE", "VICTORY_MISSION_FOR_THE_EYES_TEXT", 1, true, "{FF77A9F0-EB84-4CBE-AD78-298399B33956}", "VICTORY_EYES.jpg");

                    string nameSacrificeHint = "SacrificeHint";
                    ContextHelpHintDef sacrificeHint = CreateNewManualTacticalHint(nameSacrificeHint, "{B99AAD83-9D04-4B65-9E12-AB9423713973}", "VICTORY_MISSION_SACRIFICE_TITLE", "VICTORY_MISSION_SACRIFICE_TEXT", "VICTORY_GATE_SACRIFICE.jpg");

                   
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void CreateHintsForBaseDefense()
            {
                try
                {
                    MissionTypeTagDef baseDefenseMissionTag = DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");

                    CreateNewManualTacticalHint("BaseDefenseUmbraStrat", "{B6B31D16-82B6-462D-A220-388447F2C9D8}", "BASEDEFENSE_UMBRASTRAT_TITLE", "BASEDEFENSE_UMBRASTRAT_TEXT", "Olena_static.jpg");
                    CreateNewManualTacticalHint("BaseDefenseWormsStrat", "{1CA6F9FB-BD41-430B-A4BF-04867245BEBF}", "BASEDEFENSE_WORMSSTRAT_TITLE", "BASEDEFENSE_WORMSSTRAT_TEXT", "Olena_static.jpg");
                    CreateNewManualTacticalHint("BaseDefenseForce2Strat", "{22DF1F91-2D1A-4F34-AD9A-E9881E60CCD5}", "BASEDEFENSE_FORCE2STRAT_TITLE", "BASEDEFENSE_FORCE2STRAT_TEXT", "Olena_static.jpg");

                    ContextHelpHintDef sourceBaseDefenseHint = DefCache.GetDef<ContextHelpHintDef>("TUT_BaseDefense_HintDef");
                   // ContextHelpHintDef sourceBaseDefenseHint2 = DefCache.GetDef<ContextHelpHintDef>("TUT_DLC3_MissionStartStory_HintDef");
                    string name = "TFTVBaseDefense";
                    ContextHelpHintDef newBaseDefenseHint = Helper.CreateDefFromClone(sourceBaseDefenseHint, "{61AA33F7-0B37-48C9-9C57-4B38AF024CCF}", name);
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(newBaseDefenseHint);

                 //   CreateNewTacticalHint("TFTVBaseDefense", HintTrigger.MissionStart, baseDefenseMissionTag.name, "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE", "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION", 3, false, "{DB7CF4DE-D59F-4990-90AE-4C0B43550468}", "base_defense_hint.jpg");



                  //  ContextHelpHintDef baseDefenseStartHint = DefCache.GetDef<ContextHelpHintDef>("TFTVBaseDefense");
                  //  baseDefenseStartHint.AnyCondition = true;

                 //   DefCache.GetDef<ContextHelpHintDbDef>("TacticalHintsDbDef").Hints.Add(baseDefenseStartHint);

                    CreateNewManualTacticalHint("BaseDefenseVenting", "{AE6CE201-816F-4363-A80E-5CD07D8263CF}", "BASEDEFENSE_VENTING_TITLE", "BASEDEFENSE_VENTING_TEXT", "Olena_static.jpg");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            public static void CreateFireQuencherHint()
            {
                try
                {
                    DamageMultiplierStatusDef status = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");



                    string hintName = "FIRE_QUENCHER";
                    string hintTitle = "HINT_FIRE_QUENCHER_TITLE";
                    string hintText = "HINT_FIRE_QUENCHER_TEXT";


                    CreateNewTacticalHint(hintName, HintTrigger.ActorSeen, status.name, hintTitle, hintText, 2, true, "5F24B699-455E-44E5-831D-1CA79B9E3EED", "hint_firequencher.jpg");



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }

            public static void CreateLOTAHints()
            {
                try
                {
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    MissionTypeTagDef ancientMissionTag = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteAttack_MissionTagDef");

                    DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                    DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                    string hintStory1 = "ANCIENTS_STORY1";
                    string story1Title = "HINT_ANCIENTS_STORY1_TITLE";
                    string story1Text = "HINT_ANCIENTS_STORY1_TEXT";
                    string hintCyclops = "ANCIENTS_CYCLOPS";
                    string cyclopsTitle = "HINT_ANCIENTS_CYCLOPS_TITLE";
                    string cyclopsText = "HINT_ANCIENTS_CYCLOPS_TEXT";
                    string hintCyclopsDefense = "ANCIENTS_CYCLOPSDEFENSE";
                    string cyclopsDefenseTitle = "HINT_ANCIENTS_CYCLOPSDEFENSE_TITLE";
                    string cyclopsDefenseText = "HINT_ANCIENTS_CYCLOPSDEFENSE_TEXT";
                    string hintHoplites = "ANCIENTS_HOPLITS";
                    string hoplitesTitle = "HINT_ANCIENTS_HOPLITS_TITLE";
                    string hoplitesText = "HINT_ANCIENTS_HOPLITS_TEXT";
                    string hintHopliteRepair = "ANCIENTS_HOPLITSREPAIR";
                    string hoplitesRepairTitle = "HINT_ANCIENTS_HOPLITSREPAIR_TITLE";
                    string hoplitesRepairText = "HINT_ANCIENTS_HOPLITSREPAIR_TEXT";
                    string hintHopliteMaxPower = "ANCIENTS_HOPLITSMAXPOWER";
                    string hopliteMaxPowerTitle = "HINT_ANCIENTS_HOPLITSMAXPOWER_TITLE";
                    string hopliteMaxPowerText = "HINT_ANCIENTS_HOPLITSMAXPOWER_TEXT";

                    CreateNewTacticalHint(hintCyclops, HintTrigger.ActorSeen, cyclopsTag.name, cyclopsTitle, cyclopsText, 1, true, "41B73D60-433A-4F75-9E8B-CA30FBE45622", "HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg");
                    CreateNewTacticalHint(hintHoplites, HintTrigger.ActorSeen, hopliteTag.name, hoplitesTitle, hoplitesText, 1, true, "2DC1BC66-F42F-4E84-9680-826A57C28E48", "HINT_TFTV_Ancients_Tactical_Hoplites.jpg");
                    CreateNewTacticalHint(hintCyclopsDefense, HintTrigger.ActorHurt, cyclopsTag.name, cyclopsDefenseTitle, cyclopsDefenseText, 1, true, "E4A4FB8B-10ED-49CF-870A-6ED9497F6895", "HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg");
                    CreateNewTacticalHint(hintStory1, HintTrigger.MissionStart, ancientMissionTag.name, story1Title, story1Text, 3, true, "24C57D44-3CBA-4310-AB09-AE9444822C91", "HINT_TFTV_Ancients_Tactical_Story_1.jpg");
                    ContextHelpHintDef hoplitesHint = DefCache.GetDef<ContextHelpHintDef>(hintHoplites);
                    hoplitesHint.Conditions.Add(Constructors.ActorHasStatusHintConditionDefCreateNewConditionForTacticalHint("Alerted_StatusDef"));
                    CreateNewTacticalHint(hintHopliteRepair, HintTrigger.ActorSeen, AddAutoRepairStatusAbility.name, hoplitesRepairTitle, hoplitesRepairText, 2, true, "B25F1794-5641-40D3-88B5-0AA104FC75A1", "HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg");
                    CreateNewTacticalHint(hintHopliteMaxPower, HintTrigger.ActorSeen, ancientsPowerUpStatus.name, hopliteMaxPowerTitle, hopliteMaxPowerText, 2, true, "0DC75121-325A-406E-AC37-5F1AAB4E7778", "HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CreateTBTVHints()
            {
                try
                {

                    string tagTBTVName = "VoidTouched";
                    GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                    Helper.CreateDefFromClone(
                        source,
                        "D7E21666-5953-4773-A0EE-D8646D278FE5",
                        tagTBTVName + "_" + "GameTagDef");

                    CreateNewTacticalHint("VoidTouchedSighted", HintTrigger.ActorSeen, "VoidTouched_GameTagDef", "VOID_TOUCHED_TITLE", "VOID_TOUCHED_TEXT", 1, true, "D3FC85FA-465C-4085-8A40-84B960DB5D25", "Hint_TBTV.jpg");

                    string tagTBTVOnAttackName = "VoidTouchedOnAttack";

                    Helper.CreateDefFromClone(
                        source,
                        "B715978B-0ABF-48C2-BEC5-1B72C5AC4389",
                        tagTBTVOnAttackName + "_" + "GameTagDef");

                    CreateNewTacticalHint(tagTBTVOnAttackName + "_Hint", HintTrigger.ActorHurt, "VoidTouchedOnAttack_GameTagDef", "TBTV_ON_ATTACK_TITLE_HINT", "TBTV_ON_ATTACK_TEXT_HINT", 1, true, "6B34678C-6C8F-4462-B1DB-ED6A4B236B3D", "Hint_TBTV_MfD.jpg");

                    string tagTBTVOnTurnEndName = "VoidTouchedOnTurnEnd";

                    Helper.CreateDefFromClone(
                        source,
                        "6620CBB3-D199-4A25-A10E-46F29359174F",
                        tagTBTVOnTurnEndName + "_" + "GameTagDef");


                    CreateNewTacticalHint(tagTBTVOnTurnEndName + "_Hint", HintTrigger.ActorHurt, "VoidTouchedOnTurnEnd_GameTagDef", "TBTV_ON_TURN_END_TITLE_HINT", "TBTV_ON_TURN_END_TEXT_HINT", 1, true, "E7365C33-7222-44E3-B397-77DA892E6D9F", "Hint_TBTV_EoT.jpg");

                    string tagVoidBlightName = "VoidBlight";

                    Helper.CreateDefFromClone(
                        source,
                        "D3276B4D-4A50-48AF-B21D-EB831287811B",
                        tagVoidBlightName + "_GameTagDef");

                    CreateNewTacticalHint(tagVoidBlightName, HintTrigger.StatusApplied, "TBTV_Target", "VOID_BLIGHT_NAME_HINT", "VOID_BLIGHT_DESCRIPTION_HINT", 2, true, "24D1EE1C-90A2-47FC-A999-FC0A4B63997C", "acheron_void_blight.jpg");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CreateUIDeliriumHint()
            {
                try
                {
                    GeoTimeElapsedGeoHintConditionDef geoTimeElapsedGeoHintConditionDef = DefCache.GetDef<GeoTimeElapsedGeoHintConditionDef>("E_MinTimeElapsedForCustomizationHint [GeoscapeHintsManagerDef]");
                    geoTimeElapsedGeoHintConditionDef.TimeRangeInDays.Min = 14.0f;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            public static void CreateStaminaHint()
            {
                try
                {
                    string name = "TFTV_StaminaHintDef";
                    ContextHelpHintDef sourceHint = DefCache.GetDef<ContextHelpHintDef>("TUT4_BodyPartDisabled_HintDef");
                    ContextHelpHintDef staminaHint = Helper.CreateDefFromClone(sourceHint, "DE4949BA-D178-4036-9827-00A0E1C9BE5E", name);

                    staminaHint.IsTutorialHint = false;
                    HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef");

                    HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, "DC1E6A07-F1DA-47F4-875B-CA18144F56C4", name + "HasSeenHintConditionDef");
                    newHasSeenHintConditionDef.HintDef = staminaHint;
                    staminaHint.Conditions[1] = newHasSeenHintConditionDef;
                    staminaHint.AnyCondition = false;
                    staminaHint.Text.LocalizationKey = "TFTV_STAMINAHINT_TEXT";
                    staminaHint.Title.LocalizationKey = "TFTV_STAMINAHINT_TITLE";

                    ActorHasTagHintConditionDef sourceActorHasTagHintConditionDef = DefCache.GetDef<ActorHasTagHintConditionDef>("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef");

                    ActorHasTagHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTagHintConditionDef, "3DC53C38-BB43-4F2B-9165-475F7CE2D237", "ActorHasTag_" + name + "_HintConditionDef");
                    GameTagDef gameTagDef = DefCache.GetDef<GameTagDef>("PhoenixPoint_UniformTagDef");
                    newActorHasTemplateHintConditionDef.GameTagDef = gameTagDef;
                    staminaHint.Conditions.Add(newActorHasTemplateHintConditionDef);

                    ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(staminaHint);

                    _hintDefSpriteFileNameDictionary.Add(staminaHint, "broken_limb_stamina.jpg");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }


        internal class GeoscapeHints
        {
            internal static Sprite CustomGeoHintImage;

            public static void TriggerBaseDefenseHint(GeoLevelController controller) 
            {
                try
                {               
                    CreateCustomGeoHint("KEY_HINT_BASE_MISSION_TITLE", "KEY_HINT_BASE_MISSION_DESCRIPTION", "base_defense_geo_hint.JPG");
                    PlayCustomGeoHint(controller);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void TriggerBehemothDeployHint(GeoLevelController controller)
            {
                try 
                {
                    CreateCustomGeoHint("KEY_HINT_BEHEMOTH_DEPLOY_TITLE", "KEY_HINT_BEHEMOTH_DEPLOY_DESCRIPTION", "HINT_GEO_BEHEMOTH_DEPLOY.JPG");
                    PlayCustomGeoHint(controller);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void TriggerBehemothMissionHint(GeoLevelController controller)
            {
                try
                {
                    CreateCustomGeoHint("KEY_HINT_BEHEMOTH_MISSION_TITLE", "KEY_HINT_BEHEMOTH_MISSION_DESCRIPTION", "HINT_GEO_BEHEMOTH_MISSION.JPG");
                    PlayCustomGeoHint(controller);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateCustomGeoHint(string title, string description, string imageFile) 
            {
                try 
                {
                    GeoscapeTutorialStepsDef geoscapeTutorialStepsDef = DefCache.GetDef<GeoscapeTutorialStepsDef>("GeoscapeTutorialStepsDef");

                    geoscapeTutorialStepsDef.Hints[27].Title.LocalizationKey = title; //27
                    geoscapeTutorialStepsDef.Hints[27].Description.LocalizationKey = description; //27

                    CustomGeoHintImage = Helper.CreateSpriteFromImageFile(imageFile);
                
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void PlayCustomGeoHint(GeoLevelController controller) 
            {
                try
                {
                    GeoscapeTutorialStepType stepType = GeoscapeTutorialStepType.AlienReconRaid;

                    GeoscapeTutorial geoscapeTutorial = controller.Tutorial;

                    FieldInfo _shownStepsFieldInfo = typeof(GeoscapeTutorial).GetField("_shownSteps", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo _completedStepsFieldInfo = typeof(GeoscapeTutorial).GetField("_completedSteps", BindingFlags.NonPublic | BindingFlags.Instance);

                    HashSet<GeoscapeTutorialStepType> shownSteps = (HashSet<GeoscapeTutorialStepType>)_shownStepsFieldInfo.GetValue(geoscapeTutorial);
                    HashSet<GeoscapeTutorialStepType> completedSteps = (HashSet<GeoscapeTutorialStepType>)_completedStepsFieldInfo.GetValue(geoscapeTutorial);

                    if(shownSteps.Contains(stepType)) 
                    {
                        shownSteps.Remove(stepType);
                        _shownStepsFieldInfo.SetValue(geoscapeTutorial, shownSteps);                
                    }

                    if (completedSteps.Contains(stepType))
                    {
                        completedSteps.Remove(stepType);
                        _completedStepsFieldInfo.SetValue(geoscapeTutorial, completedSteps);
                    }

                    geoscapeTutorial.ShowTutorialStep(stepType);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            


          /*  [HarmonyPatch(typeof(GeoscapeTutorial), "ShowTutorialStep", typeof(GeoscapeTutorialStepType), typeof(int))]
            public static class TFTV_GeoscapeTutorial_ShowTutorialStep_Hints_Patch
            {
                public static void Postfix(GeoscapeTutorial __instance, GeoscapeTutorialStepType stepType, bool __result)
                {
                    try
                    {
                        TFTVLogger.Always($"setting geo tutorial step {stepType}, result is {__result}");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }*/




            [HarmonyPatch(typeof(UIModuleTutorialModal), "SetTutorialStep")]
            public static class UIModuleTutorialModal_SetTutorialStep_Hints_Patch
            {
                public static void Postfix(UIModuleTutorialModal __instance, GeoscapeTutorialStep step)
                {
                    try
                    {
                       // TFTVLogger.Always($"setting geo tutorial step {step.StepType}");

                        if (step.StepType == GeoscapeTutorialStepType.CorruptionActivated && step.Title.LocalizationKey == "KEY_GEO_HINT_ENEMY_SPECIAL_CORRUPTION_NAME")
                        {
                            __instance.Image.sprite = Helper.CreateSpriteFromImageFile("BG_Hint_Delirium.jpg");
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.Customization && step.Title.LocalizationKey == "KEY_GEO_HINT_CUSTOMIZE_TITLE")
                        {
                            __instance.Image.sprite = Helper.CreateSpriteFromImageFile("Hint_DeliriumUI.jpg");
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.AlienBaseDiplomacyPenalty && step.Title.LocalizationKey == "KEY_GEO_HINT_PANDORAN_BASE_DIPLOMACY_EFFECTS_TITLE")
                        {
                            __instance.Image.sprite = Helper.CreateSpriteFromImageFile("Hint_PandoranEvolution.jpg");
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.HarvestingSiteCaptured || step.StepType == GeoscapeTutorialStepType.RefineryCaptured)
                        {
                            __instance.Image.sprite = Helper.CreateSpriteFromImageFile("background_ancients_hint.jpg");
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.AlienInfestHavenRaid) 
                        {  
                        __instance.Image.sprite = Helper.CreateSpriteFromImageFile("MP_Choices_All.jpg");
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.AlienReconRaid) 
                        {
                            __instance.Image.sprite = CustomGeoHintImage;
                        }
                        else if (step.StepType == GeoscapeTutorialStepType.Geoscape) 
                        {
                            TFTVLogger.Always($"Geoscape tutorial step triggered adding Alistair and Olena lore entries");
                            GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            geoLevelController.Phoenixpedia.AddEntryFromDef(Repo.GetDef("B955090F-62E0-41F2-9036-3548A1DC5F46"));
                            geoLevelController.Phoenixpedia.AddEntryFromDef(Repo.GetDef("38ACBF41-7D2D-479F-981E-10FED4FC6800"));

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }

        internal class TacticalHints
        {
            private static void InfestationStoryTest(ContextHelpHintDef hintDef, UIModuleContextHelp contextHelpModule)
            {
                try
                {
                    if (hintDef.name.Equals("InfestationMissionIntro"))
                    {
                        TFTVLogger.Always($"InfestationMissionIntro Hint check passed");

                        contextHelpModule.Image.overrideSprite = Helper.CreateSpriteFromImageFile("UI_Portrait_Grunt.png");
                        //GetCharacterPortrait();

                        RectTransform rectTransform = contextHelpModule.Image.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(750f, 300f); // Set width and height

                        rectTransform.anchorMin = new Vector2(0f, 0f);
                        rectTransform.anchorMax = new Vector2(0f, 0f);

                        rectTransform.pivot = new Vector2(0f, 0f);

                        rectTransform.anchoredPosition = Vector2.zero;
                        //contextHelpModule.ImageContainer.transform.localScale = new Vector2(900f, 1000f);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /*   private static Sprite GetCharacterPortrait()
               {
                   try 
                   {
                       TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                       //Dictionary<TacticalActor, PortraitSprites> _soldierPortraits

                       SquadMemberScrollerController squadMemberScrollerController = controller.View.TacticalModules.SquadManagementModule.SquadMemberScroller;


                       Type type = typeof(SquadMemberScrollerController);
                       BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                       FieldInfo fieldInfo = type.GetField("_soldierPortraits", flags);

                       Dictionary<TacticalActor, PortraitSprites> soldierPortraits = (Dictionary<TacticalActor, PortraitSprites>)fieldInfo.GetValue(squadMemberScrollerController);

                       TFTVLogger.Always($"soldierPortraits count: {soldierPortraits.Count}");
                     //  TFTVLogger.Always($"{TFTVInfestation.StoryFirstInfestedHaven._nameOfTopCharacter}");

                       foreach (TacticalActor tacticalActor in soldierPortraits.Keys) 
                       {
                           TFTVLogger.Always($"{tacticalActor.DisplayName}");


                       }

                    //   PortraitSprites portrait = soldierPortraits[soldierPortraits.Keys.FirstOrDefault(ta => ta.DisplayName.Contains(TFTVInfestation.StoryFirstInfestedHaven._nameOfTopCharacter))];

                       TFTVLogger.Always($"portrait null? {portrait==null}; rendered portrait null? {portrait.RenderedPortrait==null}");

                       if(portrait.RenderedPortrait == null) 
                       {
                           MethodInfo recapturePortraitsMethod = type.GetMethod("ForceRecapturePortraits", flags);

                           recapturePortraitsMethod.Invoke(squadMemberScrollerController, null);

                       }


                       Sprite sprite = Sprite.Create(portrait.RenderedPortrait, new Rect(0, 0, portrait.RenderedPortrait.width, portrait.RenderedPortrait.height), new Vector2(0.0f, 0.0f));

                       TFTVLogger.Always($"sprite null? {sprite==null}");

                       return sprite;

                      // portrait.RenderedPortrait; //but this is a Texture2D!

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }*/


            /* private static readonly Dictionary<string, string> hintImageDictionary = new Dictionary<string, string>
 {
     {"InfestationMissionIntro", "px_squad.jpg"},
     {"InfestationMissionEnd", "px_squad.jpg"},
     {"UmbraSighted", "VO_15.jpg"},
     {"RevenantSighted", "Hint_Revenant.png"},
     {"TFTV_StaminaHintDef", "broken_limb_stamina.png"},
     {"RevenantResistanceSighted", "Hint_Revenant.png"},
     {"VoidTouchedSighted", "Hint_TBTV.png"},
     {"VoidTouchedOnAttack", "Hint_TBTV_MfD.png"},
     {"VoidTouchedOnTurnEnd", "Hint_TBTV_EoT.png"},
     {"TUT_DLC4_Acheron_HintDef", "Acheron.png"},
     {"AcheronPrime", "AcheronPrime.png"},
     {"AcheronAchlys", "AcheronAchlys.png"},
     {"AcheronAchlysChampion", "AcheronAchlysChampion.png"},
     {"AcheronAsclepius", "AcheronAsclepius.png"},
     {"AcheronAsclepiusChampion", "AcheronAsclepiusChampion.png"},
     {"VoidBlight", "acheron_void_blight.png"},
     {"TFTV_Tutorial1", "alistair.png"},
     {"TFTV_Tutorial2", "Olena.png"},
     {"ANCIENTS_STORY1", "HINT_TFTV_Ancients_Tactical_Story_1.jpg"},
     {"ANCIENTS_CYCLOPS", "HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg"},
     {"ANCIENTS_CYCLOPSDEFENSE", "HINT_TFTV_Ancients_Tactical_CyclopsDefense.jpg"},
     {"ANCIENTS_HOPLITS", "HINT_TFTV_Ancients_Tactical_Hoplites.jpg"},
     {"ANCIENTS_HOPLITSREPAIR", "HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg"},
     {"ANCIENTS_HOPLITSMAXPOWER", "HINT_TFTV_Ancients_Tactical_Hoplites_Overpower.jpg"},
     {"HostileDefenders", "TFTV_Hint_HostileDefenders.jpg"},
     {"FIRE_QUENCHER", "hint_firequencher.png"},
     {"HintDecoyPlaced", "decoy_hint.jpg"},
     {"HintDecoyDiscovered", "decoy_removed_hint.jpg"},
     {"BaseDefenseUmbraStrat", "Olena_static.jpg"},
     {"BaseDefenseWormsStrat", "Olena_static.jpg"},
     {"BaseDefenseForce2Strat", "Olena_static.jpg"},
     {"BaseDefenseVenting", "Olena_static.jpg"},
     {"TFTVPXPalaceStart0", "PX_VICTORY_START0.jpg"},
     {"TFTVNJPalaceStart0", "NJ_VICTORY_START0.jpg"},
     {"TFTVANPalaceStart0", "AN_VICTORY_START0.jpg"},
     {"TFTVNJPalaceStart1", "NJ_VICTORY_START1.jpg"},
     {"TFTVPXPalaceStart1", "PX_VICTORY_START1.jpg"},
     {"TFTVANPalaceStart1", "AN_VICTORY_START1.jpg"},
     {"TFTVSYPolyPalaceStart0", "SY_POLY_VICTORY_START0.jpg"},
     {"TFTVSYPolyPalaceStart1", "SY_POLY_VICTORY_START1.jpg"},
     {"TFTVSYTerraPalaceStart0", "SY_Terra_VICTORY_START0.jpg"},
     {"TFTVSYTerraPalaceStart1", "SY_TERRA_VICTORY_START1.jpg"},
     {"ReceptacleGateHint0", "VICTORY_GATE.jpg"},
     {"ReceptacleGateHint1", "VICTORY_GATE.jpg"},
     {"PalaceRevenantHint0", "VICTORY_REVENANT_TO_PX.jpg"},
     {"PalaceRevenantHint1", "Hint_Revenant.png"},
     {"PalaceHisMinionsHint", "VICTORY_MINIONS.jpg"},
     {"PalaceEyesHint", "VICTORY_EYES.jpg"},
 };
            */
            private static void AddHintToDisplayedHints(ContextHelpManager contextHelpManager, ContextHelpHintDef contextHelpHintDef)
            {
                try
                {
                    Type type = typeof(ContextHelpManager);
                    BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    FieldInfo fieldInfo = type.GetField("_shownHints", flags);

                    if (fieldInfo != null)
                    {
                        HashSet<ContextHelpHintDef> currentHints = (HashSet<ContextHelpHintDef>)fieldInfo.GetValue(contextHelpManager);

                        if (!currentHints.Contains(contextHelpHintDef))
                        {
                            currentHints.Add(contextHelpHintDef);
                        }
                        else
                        {
                            TFTVLogger.Always($"{contextHelpHintDef.name} was not added to _shownHints because already in the list.");
                        }
                    }
                    else
                    {
                        TFTVLogger.Always("_shownHints field not found.");
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            public static void ShowStoryPanel(TacticalLevelController controller, string nameFirstHint, string nameSecondHint = null)
            {
                try
                {
                    if (GameUtl.CurrentLevel() == null || GameUtl.CurrentLevel().GetComponent<TacContextHelpManager>() == null)
                    {
                        TFTVLogger.Always("no level/hint manager found!");
                        return;
                    }

                    TacContextHelpManager hintManager = GameUtl.CurrentLevel().GetComponent<TacContextHelpManager>();
                    ContextHelpHintDef firstHint = DefCache.GetDef<ContextHelpHintDef>(nameFirstHint);
                    FieldInfo hintsPendingDisplayField = typeof(ContextHelpManager).GetField("_hintsPendingDisplay", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (!hintManager.WasHintShown(firstHint))
                    {
                        if (!hintManager.RegisterContextHelpHint(firstHint, isMandatory: true, null))
                        {

                            ContextHelpHint item = new ContextHelpHint(firstHint, isMandatory: true, null);
                            List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);
                            hintsPendingDisplay.Add(item);
                            hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                        }

                        MethodInfo startLoadingHintAssetsMethod = typeof(TacContextHelpManager).GetMethod("StartLoadingHintAssets", BindingFlags.NonPublic | BindingFlags.Instance);

                        object[] args = new object[] { firstHint };

                        // Invoke the StartLoadingHintAssets method using reflection
                        startLoadingHintAssetsMethod.Invoke(hintManager, args);

                        controller.View.TryShowContextHint();

                        AddHintToDisplayedHints(hintManager, firstHint);

                        if (nameSecondHint != null)
                        {
                            ContextHelpHintDef secondHint = DefCache.GetDef<ContextHelpHintDef>(nameSecondHint);

                            if (!hintManager.RegisterContextHelpHint(secondHint, isMandatory: true, null))
                            {

                                ContextHelpHint item = new ContextHelpHint(secondHint, isMandatory: true, null);

                                // Get the current value of _hintsPendingDisplay
                                List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);

                                // Add the new hint to _hintsPendingDisplay
                                hintsPendingDisplay.Add(item);

                                // Set the modified _hintsPendingDisplay value back to the hintManager instance
                                hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                            }

                            args = new object[] { secondHint }; // Replace hintDef with your desired argument value

                            // Invoke the StartLoadingHintAssets method using reflection
                            startLoadingHintAssetsMethod.Invoke(hintManager, args);

                            controller.View.TryShowContextHint();
                            AddHintToDisplayedHints(hintManager, secondHint);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ChangeHintBackground(UIModuleContextHelp contextHelpModule, ContextHelpHintDef hintDef)
            {
                try
                {
                    if (_hintDefSpriteFileNameDictionary.ContainsKey(hintDef))
                    {
                        contextHelpModule.Image.overrideSprite = Helper.CreateSpriteFromImageFile(_hintDefSpriteFileNameDictionary[hintDef]);
                    }
                    else
                    {
                        contextHelpModule.Image.overrideSprite = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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
                            TFTVLogger.Always($"Show hint method invoked, the hint is {hintDef.name}");
                            ChangeHintBackground(__instance, hintDef);

                            foreach (ContextHelpHintDef tacticsHint in TFTVHumanEnemies.TacticsHint)
                            {
                                if (tacticsHint.name == hintDef.name)  //hintDef.Text.LocalizeEnglish().Contains("Their leader is"))
                                {
                                    alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintDef);
                                    _hintDefSpriteFileNameDictionary.Remove(hintDef);
                                    TFTVHumanEnemies.TacticsHint.Remove(hintDef);
                                    break;
                                }
                            }

                            if (TFTVRevenant.revenantResistanceHintGUID != null)
                            {
                                ContextHelpHintDef revenantResistanceHint = (ContextHelpHintDef)Repo.GetDef(TFTVRevenant.revenantResistanceHintGUID);

                                // DefCache.GetDef<ContextHelpHintDef>("RevenantResistanceSighted");
                                if (revenantResistanceHint != null && alwaysDisplayedTacticalHintsDbDef.Hints.Contains(revenantResistanceHint))
                                {
                                    alwaysDisplayedTacticalHintsDbDef.Hints.Remove(revenantResistanceHint);
                                    TFTVLogger.Always("Revenant resistance hint removed");
                                    TFTVRevenant.revenantResistanceHintGUID = null;
                                }
                            }


                            //  InfestationStoryTest(hintDef, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }
    }
}
