using Base;
using Base.AI.Defs;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDefsRequiringReinjection
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static Sprite VoidIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");

        public static void InjectDefsInjectedOnlyOnceBatch2()
        {
            try
            {
                CreateNewDefsForTFTVStart();
                CreateDeliriumPerks();
                CreateRevenantDefs();
                CreateHumanEnemiesDefs();
                CreateUmbraDefs();
                CreateNewFleeConsiderationForAI();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void CreateNewFleeConsiderationForAI()
        {
            try
            {

                /*  MultiStatusDef sourceMultiStatus = DefCache.GetDef<MultiStatusDef>("CanBeRecruited_FactionBundledStatus_StatusDef");
                  string nameMultiStatus = "MultiStatusNotFlee";
                  string gUIDMultiStatus = "{0D63DFAF-96F7-4057-990A-096E701A82D7}";
                  MultiStatusDef newMultiStatus = Helper.CreateDefFromClone(sourceMultiStatus, gUIDMultiStatus, nameMultiStatus);*/

                StatusDef onAttackTBTV = DefCache.GetDef<StatusDef>("TBTV_OnAttack_StatusDef");
                StatusDef onTurnEndTBTV = DefCache.GetDef<StatusDef>("TBTV_OnTurnEnd_StatusDef");

                DamageMultiplierStatusDef RoboticSelfRepairStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RoboticSelfRepair_AddAbilityStatusDef");

                StatusDef oilCrabStatusDef = DefCache.GetDef<StatusDef>("OilCrab_AddAbilityStatusDef");
                StatusDef oilFishStatusDef = DefCache.GetDef<StatusDef>("OilFish_AddAbilityStatusDef");

                //  newMultiStatus.Statuses = new StatusDef[] {RoboticSelfRepairStatus};

                //   TFTVLogger.Always($"{newMultiStatus.Statuses[2].name}");

                AIStatusConsiderationDef sourceStatusConsideration = DefCache.GetDef<AIStatusConsiderationDef>("NoCanBeRecruitedStatus_AIConsiderationDef");

                string nameAIStatusConsiderationSelfRepair = "AIConsiderationNoFleeSelfRepair";
                string gUIDAIStatusConsiderationSelfRepair = "{3892ECC8-EDE9-4A31-8C32-8F094EED9170}";

                AIStatusConsiderationDef newAIStatusConsiderationSelfRepair = Helper.CreateDefFromClone(sourceStatusConsideration, gUIDAIStatusConsiderationSelfRepair, nameAIStatusConsiderationSelfRepair);

                newAIStatusConsiderationSelfRepair.StatusDef = RoboticSelfRepairStatus;

                string nameAIStatusConsiderationTBTV_OnTurnEnd = "AIConsiderationNoFleeTBTV";
                string gUIDAIStatusConsiderationTBTV_OnTurnEnd = "{CD765A6A-C2DA-4EE8-B109-D5C01AB59B32}";

                AIStatusConsiderationDef newAIStatusConsiderationTBTV = Helper.CreateDefFromClone(sourceStatusConsideration, gUIDAIStatusConsiderationTBTV_OnTurnEnd, nameAIStatusConsiderationTBTV_OnTurnEnd);

                newAIStatusConsiderationTBTV.StatusDef = onTurnEndTBTV;

                //counter-productive, if it has no weapons, player won't attack it
             /*   string nameAIStatusConsiderationTBTV_OnAttack = "AIConsiderationNoFleeOnAttack";
                string gUIDAIStatusConsiderationTBTV_OnAttack = "{908A99C2-AC90-4BC6-A812-9DC3953DAE00}";

                AIStatusConsiderationDef newAIStatusConsiderationTBTVOnAttack = Helper.CreateDefFromClone(sourceStatusConsideration, gUIDAIStatusConsiderationTBTV_OnAttack, nameAIStatusConsiderationTBTV_OnAttack);

                newAIStatusConsiderationTBTVOnAttack.StatusDef = onAttackTBTV;*/


                string nameAIStatusConsiderationOilCrab = "AIConsiderationNoFleeOilCrab";
                string gUIDAIStatusConsiderationOilCrab = "{7CE39E7C-C691-4CC1-93EA-EB36EEAF1985}";

                AIStatusConsiderationDef newAIStatusConsiderationOilCrab = Helper.CreateDefFromClone(sourceStatusConsideration, gUIDAIStatusConsiderationOilCrab, nameAIStatusConsiderationOilCrab);

                newAIStatusConsiderationOilCrab.StatusDef = oilCrabStatusDef;


                string nameAIStatusConsiderationOilFish = "AIConsiderationNoFleeOilFish";
                string gUIDAIStatusConsiderationOilFish = "{9DA59FA0-646F-4C38-952E-2CA530F9B158}";

                AIStatusConsiderationDef newAIStatusConsiderationOilFish = Helper.CreateDefFromClone(sourceStatusConsideration, gUIDAIStatusConsiderationOilFish, nameAIStatusConsiderationOilFish);

                newAIStatusConsiderationOilFish.StatusDef = oilFishStatusDef;



                //first early exit consideration uses NoCanBeRecruitedStatus_AIConsiderationDef
                AIActionMoveToPositionDef moveToRandomWP = DefCache.GetDef<AIActionMoveToPositionDef>("MoveToRandomWaypoint_AIActionDef");

                AIActionMoveAndEscapeDef fleeHumanoidsAIAction = DefCache.GetDef<AIActionMoveAndEscapeDef>("Flee_AIActionDef");
                AIActionMoveAndEscapeDef fleeCrabmenAIAction = DefCache.GetDef<AIActionMoveAndEscapeDef>("Crabman_Flee_AIActionDef");
                AIActionMoveAndEscapeDef fleeFishmenAIAction = DefCache.GetDef<AIActionMoveAndEscapeDef>("Fishman_Flee_AIActionDef");

                AIAdjustedConsideration aIAdjustedConsiderationSelfRepair = new AIAdjustedConsideration() { Consideration = newAIStatusConsiderationSelfRepair, ScoreCurve = moveToRandomWP.EarlyExitConsiderations[0].ScoreCurve };
                AIAdjustedConsideration aIAdjustedConsiderationTBTV = new AIAdjustedConsideration() { Consideration = newAIStatusConsiderationTBTV, ScoreCurve = moveToRandomWP.EarlyExitConsiderations[0].ScoreCurve };
               // AIAdjustedConsideration aIAdjustedConsiderationTBTVonAttack = new AIAdjustedConsideration() { Consideration = newAIStatusConsiderationTBTVOnAttack, ScoreCurve = moveToRandomWP.EarlyExitConsiderations[0].ScoreCurve };
                AIAdjustedConsideration aIAdjustedConsiderationOilCrab = new AIAdjustedConsideration() { Consideration = newAIStatusConsiderationOilCrab, ScoreCurve = moveToRandomWP.EarlyExitConsiderations[0].ScoreCurve };
                AIAdjustedConsideration aIAdjustedConsiderationOilFish = new AIAdjustedConsideration() { Consideration = newAIStatusConsiderationOilFish, ScoreCurve = moveToRandomWP.EarlyExitConsiderations[0].ScoreCurve };



                List<AIAdjustedConsideration> aIAdjustedConsiderationsHumanoidsFlee = new List<AIAdjustedConsideration>()
                {
                  aIAdjustedConsiderationSelfRepair, aIAdjustedConsiderationTBTV, aIAdjustedConsiderationTBTV, aIAdjustedConsiderationOilCrab, aIAdjustedConsiderationOilFish
                };

                /*   List<AIAdjustedConsideration> aIAdjustedConsiderationsCrabmenFlee = new List<AIAdjustedConsideration>()
                   {
                      aIAdjustedConsiderationTBTV
                   };

                   List<AIAdjustedConsideration> aIAdjustedConsiderationsFishmenFlee = new List<AIAdjustedConsideration>()
                   {
                      aIAdjustedConsiderationTBTV
                   };*/

                aIAdjustedConsiderationsHumanoidsFlee.AddRange(fleeHumanoidsAIAction.EarlyExitConsiderations);
                //  aIAdjustedConsiderationsCrabmenFlee.AddRange(fleeCrabmenAIAction.EarlyExitConsiderations);
                //  aIAdjustedConsiderationsFishmenFlee.AddRange(fleeFishmenAIAction.EarlyExitConsiderations);

                fleeHumanoidsAIAction.EarlyExitConsiderations = aIAdjustedConsiderationsHumanoidsFlee.ToArray();
                fleeCrabmenAIAction.EarlyExitConsiderations = aIAdjustedConsiderationsHumanoidsFlee.ToArray(); //aIAdjustedConsiderationsCrabmenFlee.ToArray();
                fleeFishmenAIAction.EarlyExitConsiderations = aIAdjustedConsiderationsHumanoidsFlee.ToArray(); //aIAdjustedConsiderationsFishmenFlee.ToArray();

                ApplyStatusAbilityDef quickaim = DefCache.GetDef<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef");
                DefCache.GetDef<AIAbilityDisabledStateConsiderationDef>("QuickAimAbilityEnabled_AIConsiderationDef").Ability = quickaim;

                AIActionMoveAndExecuteAbilityDef moveAndQA = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndQuickAim_AIActionDef");
                moveAndQA.AbilityToExecute = quickaim;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        public static void CreateHumanEnemiesDefs()
        {
            CreateAmbushAbility();
            CreateHumanEnemiesTags();
        }

        public static void CreateDeliriumPerks()
        {
            Clone_GameTag();
            AddAnimation();
            Create_InnerSight();
            Create_Terror();
            Create_FasterSynapses();
            Create_Anxiety();
         //   Create_OneOfThem();
            Create_OneOfThemPassive();
            Create_Bloodthirsty();
            //Clone_Inspire();
            Create_Wolverine();
            Create_WolverinePassive();
            //Create_Derealization();
            Create_DerealizationIgnorePain();
            Create_Feral();
            Create_Hyperalgesia();
            //Clone_ArmorBuffStatus();
            Create_AnxietyStatus();
            Create_NewFeralPerk();
            Create_New_Anxiety();
            Create_New_Derealization();
            Create_WolverineCured();
        }


        //FERAL Skills cost +1 WP. You gain +8 STR
        internal static void Create_NewFeralPerk()
        {
            try
            {
                string skillName = "FeralNew_AbilityDef";
                string skillGUID = "{7BFA5655-4CC3-4C46-98BA-896622EE9BBC}";
                string progressionGUID = "{B496D54C-B0F3-45D7-AA42-C672D01DA26B}";
                string viewElementGUID = "{F3588757-0C6B-484B-8A30-F54104CA3A27}";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                    skillGUID,
                    skillName);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    progressionGUID,
                    skillName);
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    skillName);
                newAbility.StatModifications = new ItemStatModification[]
                {
                new ItemStatModification()
                    {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = 8
                    },

               
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = 8
                },
                };

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FERAL_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_NEW_FERAL_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Feral.png");
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AddAnimation()
        {
            try
            {
                ApplyStatusAbilityDef devour = DefCache.GetDef<ApplyStatusAbilityDef>("Mutog_Devour_AbilityDef");
                PlayActionAnimationAbilityDef devourAnim = DefCache.GetDef<PlayActionAnimationAbilityDef>("Mutog_PlayDevourAnimation_AbilityDef");

                //OnActorDeathEffectStatusDef devourStatus = (OnActorDeathEffectStatusDef)devour.StatusDef;
                //devourStatus.Range = 99;
                //devourStatus.RequiredDyingActorTags = null;


                foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                {
                    if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(devour))
                    {
                        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(devour).ToArray();
                    }
                }

                foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                {
                    if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(devourAnim))
                    {
                        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(devourAnim).ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Clone_GameTag()
        {
            try
            {
                string skillName = "OneOfUsMistResistance_GameTagDef";
                GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                GameTagDef Takashi = Helper.CreateDefFromClone(
                    source,
                    "F9FF0EF9-4800-4355-B6F4-5543994C129F",
                    skillName);

                TacticalVoxelMatrixDataDef tVMDD = DefCache.GetDef<TacticalVoxelMatrixDataDef>("TacticalVoxelMatrixDataDef");
                tVMDD.MistImmunityTags = new GameTagsList()
            {
                Takashi,
            };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_InnerSight()
        {
            try
            {
                string skillName = "InnerSight_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef shutEye = Helper.CreateDefFromClone(
                    source,
                    "95431c82-a525-4975-a8da-9add9799a340",
                    skillName);
                shutEye.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "69bbcec5-d491-4e7e-85a2-1063716f4532",
                    skillName);
                shutEye.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "28d440ee-c254-427a-b0a9-fe62a25faeac",
                    skillName);
                shutEye.StatModifications = new ItemStatModification[]
                  {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Perception,
                    Modification = StatModificationType.Add,
                    Value = -10
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.HearingRange,
                    Modification = StatModificationType.Add,
                    Value = 10
                },
                  };
                shutEye.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                shutEye.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_INNER_SIGHT_NAME";
                shutEye.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_INNER_SIGHT_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Inner_Sight.png");
                shutEye.ViewElementDef.LargeIcon = icon;
                shutEye.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_Anxiety()
        {
            try
            {
                string skillName = "AnxietyAbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "5d3421cb-9e22-4cdf-bcac-3beac61b2713",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "92560850-084c-4d43-8c57-a4f5773e4a26",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "b8c58fc2-c56e-4577-a187-c0922cba8468",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[0];
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ANXIETY_NAME";
                hallucinating.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ANXIETY_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Anxiety.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void Create_New_Anxiety()
        {
            try
            {
                string skillName = "NewAnxietyAbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MindControlImmunity_AbilityDef");
                ApplyStatusAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "{33568B0C-45A3-4FBC-BE5A-5292FE928C35}",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "{66590C72-BD59-47E6-A922-F7E78B841BEF}",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "{4D9194A0-EE49-4D5F-8A51-735CC6FD5CC3}",
                    skillName);
           
                hallucinating.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ANXIETY_NAME";
                hallucinating.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_NEW_ANXIETY_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Anxiety.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
       
        public static void Create_Bloodthirsty()
        {
            try
            {
                string skillName = "Bloodthirsty_AbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("Inspire_AbilityDef");
                ApplyStatusAbilityDef bloodthirsty = Helper.CreateDefFromClone(
                    source,
                    "0319cf53-65d2-4964-98d2-08c1acb54b24",
                    skillName);
                bloodthirsty.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "b101c95b-cd35-4649-9983-2662a454e40f",
                    skillName);
                bloodthirsty.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "ed164c5a-2927-422a-a086-8762137d4c5d",
                    skillName);

                OnActorDeathEffectStatusDef bloodthirstyStatus = Helper.CreateDefFromClone(
                    bloodthirsty.StatusDef as OnActorDeathEffectStatusDef,
                    "ac7195f9-c382-4f79-a956-55d5eb3b6371",
                    "E_KillListenerStatus [" + skillName + "]");

                FactionMembersEffectDef bloodthirstyEffectDef2 = Helper.CreateDefFromClone(
                    bloodthirstyStatus.EffectDef as FactionMembersEffectDef,
                    "8bd34f58-d452-4f38-975e-4f32b33d283d",
                    "E_Effect [" + skillName + "]");

                StatsModifyEffectDef bloodthirstySingleEffectDef2 = Helper.CreateDefFromClone(
                    bloodthirstyEffectDef2.SingleTargetEffect as StatsModifyEffectDef,
                    "ad0891cf-fe7a-443f-acb9-575c3cf23432",
                    "E_SingleTargetEffect [" + skillName + "]");



                bloodthirsty.StatusDef = bloodthirstyStatus;
                bloodthirstyStatus.EffectDef = bloodthirstyEffectDef2;
                bloodthirstyEffectDef2.SingleTargetEffect = bloodthirstySingleEffectDef2;
                // This is working:
                bloodthirstySingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "WillPoints",
                    Value = -1,
                }
            };

                bloodthirsty.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_NAME";
                bloodthirsty.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_DESCRIPTION";

                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Bloodthirsty.png");
                bloodthirsty.ViewElementDef.LargeIcon = icon;
                bloodthirsty.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_FasterSynapses()
        {
            try
            {
                string skillName = "FasterSynapses_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef angerIssues = Helper.CreateDefFromClone(
                    source,
                    "c1a545b3-eb5d-47f0-bf59-82710415d559",
                    skillName);
                angerIssues.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "561c23c1-ce46-4862-b49f-0fd3656cdefc",
                    skillName);
                angerIssues.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "da704d9c-354c-4e2b-a61d-af3b23f47522",
                    skillName);
                angerIssues.StatModifications = new ItemStatModification[]
                  {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = -0.25f
                },
                  };
                angerIssues.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                angerIssues.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FASTER_SYNAPSES_NAME";
                angerIssues.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_FASTER_SYNAPSES_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Faster_Synapses.png");
                angerIssues.ViewElementDef.LargeIcon = icon;
                angerIssues.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_Terror()
        {
            try
            {
                string skillName = "Terror_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef photophobia = Helper.CreateDefFromClone(
                    source,
                    "42399bdf-b43b-40f4-a471-89d082a31fde",
                    skillName);
                photophobia.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "7e8fff90-a757-4794-81a9-a90cb97cb325",
                    skillName);
                photophobia.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "2e4f7cec-80de-423c-914d-865700949a93",
                    skillName);
                photophobia.StatModifications = new ItemStatModification[]
                  {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Speed,
                    Modification = StatModificationType.Add,
                    Value = -2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = 0.25f
                },
                  };
                photophobia.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                photophobia.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_TERROR_NAME";
                photophobia.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_TERROR_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Terror.png");
                photophobia.ViewElementDef.LargeIcon = icon;
                photophobia.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_WolverinePassive()
        {
            try
            {
                string skillName = "WolverinePassive_StatusDef";
                StatMultiplierStatusDef source = DefCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef");
                StatMultiplierStatusDef wolverinePassiveStatus = Helper.CreateDefFromClone(
                    source,
                    "b3185867-ca87-4e59-af6d-012267a7bd25",
                    skillName);
               
                wolverinePassiveStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "3f170800-b819-4237-80a3-c9b9daa9dab4",
                    skillName);
                wolverinePassiveStatus.StatsMultipliers[0].Multiplier = 0.8f;
                wolverinePassiveStatus.ExpireOnEndOfTurn = false;
                wolverinePassiveStatus.DurationTurns = -1;
                wolverinePassiveStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                wolverinePassiveStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList;
                wolverinePassiveStatus.VisibleOnPassiveBar = false;

                

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_Wolverine()
        {
            try
            {
                string skillName = "Wolverine_AbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("Mutoid_Adapt_RightArm_Slasher_AbilityDef");
                ApplyStatusAbilityDef nails = Helper.CreateDefFromClone(
                    source,
                    "bb65ab9c-94ae-4878-b999-e04946f720aa",
                    skillName);
                nails.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "c050760d-1fb7-4b25-9295-00d98aedad19",
                    skillName);
                nails.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "e9bd7acb-6955-414b-a2de-7544c38b7b6e",
                    skillName);

                nails.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_NAME";
                nails.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Wolverine.png");
                nails.ViewElementDef.LargeIcon = icon;
                nails.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void Create_WolverineCured()
        {
            try
            {
                string skillName = "WolverineCured_AbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("Mutoid_Adapt_RightArm_Slasher_AbilityDef");
                ApplyStatusAbilityDef nails = Helper.CreateDefFromClone(
                    source,
                    "{51145E82-44FF-45D7-9DDF-F2751D9EDFD8}",
                    skillName);
                nails.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "{785D5A01-2F15-4E77-9B10-6F250AA1AD28}",
                    skillName);
                nails.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "{BD0DDD5E-8B12-4DD4-BF3D-843AFB2D82B3}",
                    skillName);

                nails.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_NAME";
                nails.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_CURED_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Wolverine.png");
                nails.ViewElementDef.LargeIcon = icon;
                nails.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void Create_OneOfThem()
        {
            try
            {
                string skillName = "OneOfThem_AbilityDef";
                DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("VirusResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef oneOfUs = Helper.CreateDefFromClone(
                    source,
                    "d4f5f9f2-43b6-4c3e-a5db-78a7a9cccd3e",
                    skillName);
                oneOfUs.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "569a8f7b-41bf-4a0c-93ce-d96006f4ed27",
                    skillName);
                oneOfUs.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3cc4d8c8-739c-403b-92c9-7a6f5c54abb5",
                    skillName);

                oneOfUs.DamageTypeDef = DefCache.GetDef<DamageTypeBaseEffectDef>("Mist_SpawnVoxelDamageTypeEffectDef");
                oneOfUs.Multiplier = 0;

                oneOfUs.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
                oneOfUs.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_One_Of_Them.png");
                oneOfUs.ViewElementDef.LargeIcon = icon;
                oneOfUs.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_OneOfThemPassive()
        {
            try
            {
                string skillName = "OneOfThemPassive_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef oneOfThemPassive = Helper.CreateDefFromClone(
                    source,
                    "ff35f9ef-ad67-42ff-9dcd-0288dba4d636",
                    skillName);
                oneOfThemPassive.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "61e44215-fc05-4383-b9e4-17f384e3d003",
                    skillName);
                oneOfThemPassive.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "aaead24e-9dba-4ef7-ba2d-8df142cb9105",
                    skillName);

                oneOfThemPassive.StatModifications = new ItemStatModification[] { };
               /*   {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = -2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = -2
                },
                  };*/

                DamageMultiplierStatusDef mistResistance = DefCache.GetDef<DamageMultiplierStatusDef>("MistResistance_StatusDef");
                mistResistance.Multiplier = 0.0f;
                oneOfThemPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                oneOfThemPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
                oneOfThemPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_One_Of_Them.png");
                oneOfThemPassive.ViewElementDef.LargeIcon = icon;
                oneOfThemPassive.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void Create_Feral()
        {
            try
            {
                string skillName = "Feral_AbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("RapidClearance_AbilityDef");
                ProcessDeathReportEffectDef sourceEffect = DefCache.GetDef<ProcessDeathReportEffectDef>("E_Effect [RapidClearance_AbilityDef]");
                ApplyStatusAbilityDef feral = Helper.CreateDefFromClone(
                    source,
                    "34612505-8512-4eb3-8429-ef087c07c764",
                    skillName);
                feral.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "75660746-2f27-41d1-97e3-f0d6340e96b7",
                    skillName);
                feral.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "1135128c-a10d-4285-9d03-d93a4afd6733",
                    skillName);
                OnActorDeathEffectStatusDef feralStatusDef = Helper.CreateDefFromClone(
                    source.StatusDef as OnActorDeathEffectStatusDef,
                    "9510c7e3-bef7-4b89-b20a-3bb57a7e664b",
                    "E_FeralStatus [Feral_AbilityDef]");
                ProcessDeathReportEffectDef feralEffectDef = Helper.CreateDefFromClone(
                    sourceEffect,
                    "d0f71701-4255-4b57-a387-0f3c936ed29e",
                    "E_Effect [Feral_AbilityDef]");

                feral.StatusApplicationTrigger = StatusApplicationTrigger.AbilityAdded;
                feral.Active = false;
                feral.WillPointCost = 0;
                feral.StatusDef = feralStatusDef;
                feralStatusDef.EffectDef = feralEffectDef;
                feralStatusDef.ExpireOnEndOfTurn = false;
                feralStatusDef.Duration = -1;
                feralStatusDef.DurationTurns = -1;
                feralEffectDef.RestoreActionPointsFraction = 0;

                feral.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FERAL_NAME";
                feral.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_FERAL_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Feral.png");
                feral.ViewElementDef.LargeIcon = icon;
                feral.ViewElementDef.SmallIcon = icon;
                feralStatusDef.ExpireOnEndOfTurn = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void Create_Hyperalgesia()
        {
            string skillName = "Hyperalgesia_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
            PassiveModifierAbilityDef solipsism = Helper.CreateDefFromClone(
                source,
                "ccd66e53-6258-4fa6-a185-66ba0f5bc4b7",
                skillName);
            solipsism.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "1aef5152-c6d6-435f-959e-0ac368dcf248",
                skillName);
            solipsism.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "ff72f143-8f3e-4988-a5fd-566faa5cb281",
                skillName);


            solipsism.StatModifications = new ItemStatModification[0];
            solipsism.ItemTagStatModifications = new EquipmentItemTagStatModification[0];

            solipsism.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_HYPERALGESIA_NAME";
            solipsism.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_HYPERALGESIA_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Hyperalgesia.png");
            solipsism.ViewElementDef.LargeIcon = icon;
            solipsism.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_AnxietyStatus()
        {
            MindControlAbilityDef priestMindControlAbility = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");

            string skillName = "Anxiety_StatusDef";
            SilencedStatusDef source = DefCache.GetDef<SilencedStatusDef>("ActorSilenced_StatusDef");
            SilencedStatusDef hallucinatingStatus = Helper.CreateDefFromClone(
                source,
                "2d5ed7eb-f4f3-42bf-8589-1d50ec99fa8b",
                skillName);

            hallucinatingStatus.DurationTurns = 2;

            priestMindControlAbility.DisablingStatuses = priestMindControlAbility.DisablingStatuses.AddToArray(hallucinatingStatus);
        }


        internal static void Create_New_Derealization()
        {
            try
            {
                string skillName = "Derealization_AbilityDef";
                string skillGUID = "{2DF689FD-EF69-48F4-B2BF-BD95B6510C7D}";
                string progressionGUID = "{C661D352-502C-4B16-BBEE-21860539341D}";
                string viewElementGUID = "{68B7F31A-59C7-4A9A-BC70-45DC26040FCE}";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                    skillGUID,
                    skillName);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    progressionGUID,
                    skillName);
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    skillName);
                newAbility.StatModifications = new ItemStatModification[]
                {
                new ItemStatModification()
                    {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = -5
                    },


                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = -5
                },
                };

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Derealization.png");
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void Create_DerealizationIgnorePain()
        {
            try
            {
                string statusName = "IgnoreDisabledLimbs_StatusDef";

                FreezeAspectStatsStatusDef sourceStatus = DefCache.GetDef<FreezeAspectStatsStatusDef>("IgnorePain_StatusDef");
                FreezeAspectStatsStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, "{B86D5C6C-C644-4B77-92DD-18E8C85D1F15}", statusName);


                string skillName = "DerealizationIgnorePain_AbilityDef";
                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("IgnorePain_AbilityDef");
                ApplyStatusAbilityDef derealizationIgnorePain = Helper.CreateDefFromClone(
                    source,
                    "eea26659-d54f-48d8-8025-cb7ca53c1749",
                    skillName);
                derealizationIgnorePain.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "d99c2d2f-0cff-412c-ad99-218b39158c88",
                    skillName);
                derealizationIgnorePain.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3f8b13e1-70ff-4964-923d-1e2c73f66f4f",
                    skillName);

                derealizationIgnorePain.StatusDef = newStatus;

                derealizationIgnorePain.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
                derealizationIgnorePain.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_DeliriumPerks_Derealization.png");
                derealizationIgnorePain.ViewElementDef.LargeIcon = icon;
                derealizationIgnorePain.ViewElementDef.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

       

        public static void CreateRevenantDefs()
        {
            try
            {
                CreateRevenantAbility();
                CreateRevenantStatusEffect();
                CreateRevenantGameTags();
                CreateRevenantAbilityForAssault();
                CreateRevenantAbilityForBerserker();
                CreateRevenantAbilityForHeavy();
                CreateRevenantAbilityForInfiltrator();
                CreateRevenantAbilityForPriest();
                CreateRevenantAbilityForSniper();
                CreateRevenantAbilityForTechnician();
                CreateRevenantResistanceStatus();
                //  CreateRevenantClassStatusEffects();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateUmbraDefs()
        {
            try
            {
                CreateTouchedByTheVoidAbilities();
                CreateTBTVStatuses();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        //Need the tag for the Hint
        

        public static void CreateTBTVStatuses()
        {
            try
            {
                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                string onAttackStatusDefName = "TBTV_OnAttack_StatusDef";
                DamageMultiplierStatusDef onAttackStatus = Helper.CreateDefFromClone(
                    source,
                    "59FCD1FC-1A61-4AA6-B585-7DDD71739F40",
                    onAttackStatusDefName);
                onAttackStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "8553E163-8559-4551-870C-AD67D48EECE8",
                    onAttackStatusDefName);

                onAttackStatus.EffectName = "OnAttackTBTV";
                onAttackStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                onAttackStatus.VisibleOnPassiveBar = true;
                onAttackStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                onAttackStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[1];
                onAttackStatus.Visuals.DisplayName1.LocalizationKey = "TBTV_ON_ATTACK_TITLE";
                onAttackStatus.Visuals.Description.LocalizationKey = "TBTV_ON_ATTACK_TEXT";
                onAttackStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TBTV_MarkForDeath.png");
                onAttackStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TBTV_MarkForDeath.png");

                string onTurnEndStatusDefName = "TBTV_OnTurnEnd_StatusDef";
                DamageMultiplierStatusDef onTurnEndStatus = Helper.CreateDefFromClone(
                    source,
                    "77DF22A4-164A-497F-9239-A783F8DDB3AB",
                    onTurnEndStatusDefName);
                onTurnEndStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "34F6B557-B7DD-47A8-A8B3-9F945A156F18",
                    onTurnEndStatusDefName);
                onTurnEndStatus.EffectName = "OnTurnEndTBTV";
                onTurnEndStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                onTurnEndStatus.VisibleOnPassiveBar = true;
                onTurnEndStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                onTurnEndStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[1];

                onTurnEndStatus.Visuals.DisplayName1.LocalizationKey = "TBTV_ON_TURN_END_TITLE";
                onTurnEndStatus.Visuals.Description.LocalizationKey = "TBTV_ON_TURN_END_TEXT";
                onTurnEndStatus.Visuals.LargeIcon = DefCache.GetDef<CallReinforcementsAbilityDef>("Acheron_CallReinforcements_AbilityDef").ViewElementDef.LargeIcon;
                onTurnEndStatus.Visuals.SmallIcon = DefCache.GetDef<CallReinforcementsAbilityDef>("Acheron_CallReinforcements_AbilityDef").ViewElementDef.SmallIcon;


                //need to clone belcher abilities so that they can be covertly applied in case the character gets one-shot killed
                AddAbilityStatusDef oilCrabStatusDef = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
                //  string hiddenOilCrabStatusDefName = "HiddenOilCrabStatusDef";
                //   AddAbilityStatusDef hiddenOilCrabStatusDef = Helper.CreateDefFromClone(oilCrabStatusDef, "5A7ABACE-21DA-4A4B-AFB5-5D178226D9D1", hiddenOilCrabStatusDefName);



                oilCrabStatusDef.EffectName = "OilCrabOnDeath";
                oilCrabStatusDef.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "0015079C-C250-404C-B912-679303BD5B2C",
                    oilCrabStatusDef.EffectName);
                oilCrabStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                oilCrabStatusDef.VisibleOnPassiveBar = true;
                oilCrabStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                oilCrabStatusDef.Visuals.DisplayName1.LocalizationKey = "KEY_AC_DEATHBELCH_NAME";
                oilCrabStatusDef.Visuals.Description.LocalizationKey = "KEY_AC_DEATHBELCH_DESCRIPTION";
                oilCrabStatusDef.Visuals.LargeIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.LargeIcon;
                oilCrabStatusDef.Visuals.SmallIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.SmallIcon;

                AddAbilityStatusDef oilFishStatusDef = DefCache.GetDef<AddAbilityStatusDef>("OilFish_AddAbilityStatusDef");
                oilFishStatusDef.EffectName = "OilFishOnDeath";
                oilFishStatusDef.Visuals = Helper.CreateDefFromClone(
                   source.Visuals,
                   "44BA7178-2307-491C-9593-2B8FDD312328",
                   oilCrabStatusDef.EffectName);

                oilFishStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                oilFishStatusDef.VisibleOnPassiveBar = true;
                oilFishStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                oilFishStatusDef.Visuals.DisplayName1.LocalizationKey = "KEY_AC_DEATHBELCH_NAME";
                oilFishStatusDef.Visuals.Description.LocalizationKey = "KEY_AC_DEATHBELCH_DESCRIPTION";
                oilFishStatusDef.Visuals.LargeIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.LargeIcon;
                oilFishStatusDef.Visuals.SmallIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.SmallIcon;

                DeathBelcherAbilityDef oilCrabDie = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef");
                oilCrabDie.ViewElementDef.ShowInStatusScreen = false;

                DeathBelcherAbilityDef oilFishDie = DefCache.GetDef<DeathBelcherAbilityDef>("Oilfish_Die_DeathBelcher_AbilityDef");
                oilFishDie.ViewElementDef.ShowInStatusScreen = false;



                /*   string onDeathStatusDefName = "TBTV_OnDeath_StatusDef";
                   DamageMultiplierStatusDef onDeathStatus = Helper.CreateDefFromClone(
                       source,
                       "77DF22A4-164A-497F-9239-A783F8DDB3AB",
                       onTurnEndStatusDefName);
                   onDeathStatus.Visuals = Helper.CreateDefFromClone(
                       source.Visuals,
                       "34F6B557-B7DD-47A8-A8B3-9F945A156F18",
                       onTurnEndStatusDefName);
                   onDeathStatus.EffectName = "OnTurnEndTBTV";
                   onDeathStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                   onDeathStatus.VisibleOnPassiveBar = true;
                   onDeathStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                   onDeathStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[1];

                   onDeathStatus.Visuals.DisplayName1.LocalizationKey = "KEY_AC_DEATHBELCH_NAME";
                   onDeathStatus.Visuals.Description.LocalizationKey = "KEY_AC_DEATHBELCH_DESCRIPTION";
                   onDeathStatus.Visuals.LargeIcon = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef").Visuals.LargeIcon;
                   onDeathStatus.Visuals.SmallIcon = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef").ViewElementDef.SmallIcon;*/


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void CreateTouchedByTheVoidAbilities()
        {
            try
            {

                string hiddenAbilityName = "TBTV_Hidden_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef hiddenTBTVAbility = Helper.CreateDefFromClone(
                    source,
                    "08B0682C-AAA0-42EC-9F96-794B1D4D7C7C",
                    hiddenAbilityName);
                hiddenTBTVAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "83A0DB53-E4EE-4EFC-A344-856BEE621D27",
                    hiddenAbilityName);
                hiddenTBTVAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "215E4A23-AF21-415D-B8FD-DA90E916542E",
                    hiddenAbilityName);
                hiddenTBTVAbility.StatModifications = new ItemStatModification[0];
                hiddenTBTVAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hiddenTBTVAbility.ViewElementDef.DisplayName1.LocalizationKey = "TBTV_HIDDEN_ABILITY_NAME";
                hiddenTBTVAbility.ViewElementDef.Description.LocalizationKey = "TBTV_HIDDEN_ABILITY_DESCRIPTION";
                hiddenTBTVAbility.ViewElementDef.LargeIcon = VoidIcon;
                hiddenTBTVAbility.ViewElementDef.SmallIcon = VoidIcon;

                AddAbilityStatusDef sourceAbilityStatusDef = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");

                string statusHiddenAbilityTBTVName = "TBTV_Hidden_AddAbilityStatusDef";
                AddAbilityStatusDef statusHiddenTBTVDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "23BD57AF-418A-49C7-B658-D05465248578", statusHiddenAbilityTBTVName);
                statusHiddenTBTVDef.AbilityDef = hiddenTBTVAbility;
                statusHiddenTBTVDef.ApplicationConditions = new EffectConditionDef[] { };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffects()
        {
            try
            {
                CreateRevenantClassStatusEffectAssault();
                CreateRevenantClassStatusEffectBerserker();
                CreateRevenantClassStatusEffectHeavy();
                CreateRevenantClassStatusEffectInfiltrator();
                CreateRevenantClassStatusEffectPriest();
                CreateRevenantClassStatusEffectSniper();
                CreateRevenantClassStatusEffectTechnician();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void CreateRevenantClassStatusEffectAssault()
        {
            try
            {
                string name = "RevenantAssaultStatus";
                string gUID1 = "1c8e77d9-2f5b-487f-92c3-7cd3d04a5a51";
                string gUID2 = "5d5dc5e5-5f20-4c6f-824b-a3ddd41b5f5c";
                string title = "REVENANT_STATUS_TITLE_ASSAULT";
                string description = "REVENANT_STATUS_DESCRIPTION_ASSAULT";
                string iconFile = "UI_ClassIcon_Assault_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectBerserker()
        {
            try
            {
                string name = "RevenantBerserkerStatus";
                string gUID1 = "7a28e9c0-6b7a-4686-8f26-cdc37bfe88b0";
                string gUID2 = "9dc081e6-4f33-4086-a21d-7f6329c08a86";
                string title = "REVENANT_STATUS_TITLE_BERSERKER";
                string description = "REVENANT_STATUS_DESCRIPTION_BERSERKER";
                string iconFile = "UI_ClassIcon_Berserker_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectHeavy()
        {
            try
            {
                string name = "RevenantHeavyStatus";
                string gUID1 = "a1dfd0b7-69a9-45a7-a89b-52f7d23b1c80";
                string gUID2 = "b2f70d58-9c3d-4dd7-aabf-a7a62c0e04a3";
                string title = "REVENANT_STATUS_TITLE_HEAVY";
                string description = "REVENANT_STATUS_DESCRIPTION_HEAVY";
                string iconFile = "UI_ClassIcon_Heavy_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectInfiltrator()
        {
            try
            {
                string name = "RevenantInfiltratorStatus";
                string gUID1 = "c3a74e7c-87f2-4e18-98d6-f91c874087e8";
                string gUID2 = "d4b95f1d-c87b-4f7d-abf6-12fd6b05a2d4";
                string title = "REVENANT_STATUS_TITLE_INFILTRATOR";
                string description = "REVENANT_STATUS_DESCRIPTION_INFILTRATOR";
                string iconFile = "UI_ClassIcon_Infiltrator_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectPriest()
        {
            try
            {
                string name = "RevenantPriestStatus";
                string gUID1 = "e5c06eba-05e4-4234-8b3e-5f5b0c5d5f5e";
                string gUID2 = "f6d17f5b-3e0f-4ddc-b5e5-5f5b5f5b5f5f";
                string title = "REVENANT_STATUS_TITLE_PRIEST";
                string description = "REVENANT_STATUS_DESCRIPTION_PRIEST";
                string iconFile = "UI_ClassIcon_Priest_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectSniper()
        {
            try
            {
                string name = "RevenantSniperStatus";
                string gUID1 = "076e8e7c-6d0b-4ddb-aabf-a7a6a7a6a7a6";
                string gUID2 = "186f9f1d-c87b-4f7d-abf6-12fd12fd12fd";
                string title = "REVENANT_STATUS_TITLE_SNIPER";
                string description = "REVENANT_STATUS_DESCRIPTION_SNIPER";
                string iconFile = "UI_ClassIcon_Sniper_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantClassStatusEffectTechnician()
        {
            try
            {
                string name = "RevenantTechnicianStatus";
                string gUID1 = "296e0eba-05e4-4234-8b3e-5f5b0c0c0c0c";
                string gUID2 = "396f1f5b-3e0f-4ddc-b5e5-5f5b5b5b5b5b";
                string title = "REVENANT_STATUS_TITLE_TECHNICIAN";
                string description = "REVENANT_STATUS_DESCRIPTION_TECHNICIAN";
                string iconFile = "UI_ClassIcon_Technician_noOutlines.png";

                TFTVCommonMethods.CreateNewDescriptiveTacticalStatus(name, gUID1, gUID2, title, description, iconFile);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateRevenantStatusEffect()
        {
            try
            {


                AddAbilityStatusDef sourceAbilityStatusDef = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
                PassiveModifierAbilityDef Revenant_Ability = DefCache.GetDef<PassiveModifierAbilityDef>("Revenant_AbilityDef");

                AddAbilityStatusDef newAbilityStatusDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "68EE5958-D977-4BD4-9018-CAE03C5A6579", "Revenant_StatusEffectDef");
                newAbilityStatusDef.AbilityDef = Revenant_Ability;
                newAbilityStatusDef.ApplicationConditions = new EffectConditionDef[] { };


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbility()
        {
            try
            {

                string skillName = "Revenant_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef revenantAbility = Helper.CreateDefFromClone(
                    source,
                    "8A62302E-9C2D-4AFA-AFF3-2F526BF82252",
                    skillName);
                revenantAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "FECD4DD8-5E1A-4A0F-BC3A-C2F0AA30E41F",
                    skillName);
                revenantAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "75B1017A-0455-4B44-91F0-3E1446899B42",
                    skillName);
                revenantAbility.StatModifications = new ItemStatModification[0];
                revenantAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_ABILITY_REVENANT";
                revenantAbility.ViewElementDef.Description.LocalizationKey = "KEY_ABILITY_REVENANT_DESCRIPTION";
                revenantAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ODI_Skull.png");
                revenantAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ODI_Skull.png");
                // revenantAbility.ViewElementDef.ShowInStatusScreen = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForAssault()
        {
            try
            {

                string skillName = "RevenantAssault_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef revenantAssault = Helper.CreateDefFromClone(
                    source,
                    "1045EB8D-1916-428F-92EF-A15FD2807818",
                    skillName);
                revenantAssault.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "7FF5A3CF-6BBD-4E4F-9E80-2DB7BDB29112",
                    skillName);
                revenantAssault.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "47BE3577-1D68-4FB2-BFA3-0A158FC710D9",
                    skillName);
                revenantAssault.StatModifications = new ItemStatModification[]
                {
                    new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.05f},
                    new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 2},
                    new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 2},
                };
                revenantAssault.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantAssault.ViewElementDef.DisplayName1 = new LocalizedTextBind("Assault Revenant", true);
                revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+5% Damage", true);

                revenantAssault.ViewElementDef.LargeIcon = VoidIcon;
                revenantAssault.ViewElementDef.SmallIcon = VoidIcon;
                revenantAssault.ViewElementDef.ShowInStatusScreen = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForBerserker()
        {
            try
            {

                string skillName = "RevenantBerserker_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef revenantBerserker = Helper.CreateDefFromClone(
                    source,
                    "FD3FE516-25BA-44F2-9770-3AA4AD1DCB91",
                    skillName);
                revenantBerserker.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "E2707CBD-3D99-4EA4-A48D-B8E6E14EFDFD",
                    skillName);
                revenantBerserker.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3F74FAF1-1A87-4E2A-AEC2-CBB0BA5A14E0",
                    skillName);
                revenantBerserker.StatModifications = new ItemStatModification[]
                {
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                };
                revenantBerserker.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantBerserker.ViewElementDef.DisplayName1 = new LocalizedTextBind("Berserker Revenant", true);
                revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+4 Speed", true);

                revenantBerserker.ViewElementDef.LargeIcon = VoidIcon;
                revenantBerserker.ViewElementDef.SmallIcon = VoidIcon;
                revenantBerserker.ViewElementDef.ShowInStatusScreen = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForHeavy()
        {
            try
            {

                string skillName = "RevenantHeavy_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef heavy = Helper.CreateDefFromClone(
                    source,
                    "A8603522-3472-4A95-9ADF-F27E8B287D15",
                    skillName);
                heavy.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "AA5F572B-D86B-4C00-B8B9-4D86EE5F7F4D",
                    skillName);
                heavy.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "F8781E78-D106-44B3-A0E6-855BCAEB0A2F",
                    skillName);
                heavy.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 100},
                };
                heavy.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                heavy.ViewElementDef.DisplayName1 = new LocalizedTextBind("Heavy Revenant", true);
                heavy.ViewElementDef.Description = new LocalizedTextBind("+5 Strength", true);

                heavy.ViewElementDef.LargeIcon = VoidIcon;
                heavy.ViewElementDef.SmallIcon = VoidIcon;
                heavy.ViewElementDef.ShowInStatusScreen = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForInfiltrator()
        {
            try
            {

                string skillName = "RevenantInfiltrator_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef infiltrator = Helper.CreateDefFromClone(
                    source,
                    "6C56E0F9-56BB-41D2-AFB1-08C8A49F69FA",
                    skillName);
                infiltrator.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1F8B6D09-A2C5-4B3F-BBED-F59675301ABB",
                    skillName);
                infiltrator.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6CAFD922-60C6-449E-A652-C2BD94386BE5",
                    skillName);
                infiltrator.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.15f},
                };
                infiltrator.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                infiltrator.ViewElementDef.DisplayName1 = new LocalizedTextBind("Infiltrator Revenant", true);
                infiltrator.ViewElementDef.Description = new LocalizedTextBind("+15% Stealth", true);

                infiltrator.ViewElementDef.LargeIcon = VoidIcon;
                infiltrator.ViewElementDef.SmallIcon = VoidIcon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForPriest()
        {
            try
            {

                string skillName = "RevenantPriest_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef priest = Helper.CreateDefFromClone(
                    source,
                    "0816E671-D396-4212-910F-87B5DEC6ADE2",
                    skillName);
                priest.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C1C7FBEA-2C0B-4930-A73C-15BF3A987784",
                    skillName);
                priest.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "460AAE12-0541-40AB-A4EE-E3E206A96FB4",
                    skillName);
                priest.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10},
                };
                priest.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                priest.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                priest.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);

                priest.ViewElementDef.LargeIcon = VoidIcon;
                priest.ViewElementDef.SmallIcon = VoidIcon;
                priest.ViewElementDef.ShowInStatusScreen = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForSniper()
        {
            try
            {

                string skillName = "RevenantSniper_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef sniper = Helper.CreateDefFromClone(
                    source,
                    "4A2C53A3-D9DB-456A-8B88-AB2D90BE1DB5",
                    skillName);
                sniper.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0D811905-8C70-4D46-9CF2-1A31C5E98ED1",
                    skillName);
                sniper.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "7DCCCAAA-7245-4245-9033-F6320CCDA2AB",
                    skillName);
                sniper.ViewElementDef.ShowInStatusScreen = false;
                sniper.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 10},
                };
                sniper.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                sniper.ViewElementDef.DisplayName1 = new LocalizedTextBind("Sniper Revenant", true);
                sniper.ViewElementDef.Description = new LocalizedTextBind("+10 Perception", true);

                sniper.ViewElementDef.LargeIcon = VoidIcon;
                sniper.ViewElementDef.SmallIcon = VoidIcon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantAbilityForTechnician()
        {
            try
            {

                string skillName = "RevenantTechnician_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef technician = Helper.CreateDefFromClone(
                    source,
                    "04A284AC-545A-455F-8843-54056D68022E",
                    skillName);
                technician.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1A995634-EE80-4E72-A10F-F8389E8AEB50",
                    skillName);
                technician.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "19B35512-5C23-4046-B10D-2052CDEFB769",
                    skillName);
                technician.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 5},
                 new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 5}
                };
                technician.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                technician.ViewElementDef.DisplayName1 = new LocalizedTextBind("Technician Revenant", true);
                technician.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +5 Willpower", true);

                technician.ViewElementDef.LargeIcon = VoidIcon;
                technician.ViewElementDef.SmallIcon = VoidIcon;
                technician.ViewElementDef.ShowInStatusScreen = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantResistanceStatus()
        {
            try
            {
                string skillName = "RevenantResistance_StatusDef";
                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef revenantResistance = Helper.CreateDefFromClone(
                    source,
                    "A7F8113B-B281-4ECD-99FE-3125FCE029C4",
                    skillName);
                revenantResistance.EffectName = "RevenantResistance";
                revenantResistance.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                revenantResistance.VisibleOnPassiveBar = true;
                revenantResistance.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;

                //  revenantResistance.CharacterProgressionData = Helper.CreateDefFromClone(
                //      source.CharacterProgressionData,
                //      "C298F900-A7D5-4EEC-96E1-50D017614396",
                //     skillName);
                revenantResistance.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "B737C223-52D0-413B-B48F-978AD5D5BB33",
                    skillName);
                revenantResistance.DamageTypeDefs = new DamageTypeBaseEffectDef[1];

                revenantResistance.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_RevenantResistance.png");
                revenantResistance.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_RevenantResistance.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateRevenantGameTags()
        {
            string skillName = "RevenantTier";
            GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
            Helper.CreateDefFromClone(
                source,
                "1677F9F4-5B45-47FA-A119-83A76EF0EC70",
                skillName + "_1_" + "GameTagDef");
            Helper.CreateDefFromClone(
                source,
                "9A807A62-D51D-404E-ADCF-ABB4A888202E",
                skillName + "_2_" + "GameTagDef");
            Helper.CreateDefFromClone(
                source,
                "B4BD3091-8522-4F3C-8A0F-9EE522E0E6B4",
                skillName + "_3_" + "GameTagDef");
            Helper.CreateDefFromClone(
                source,
                "D2904A22-FE23-45B3-8879-9236E389C9E4",
                "Any_Revenant_TagDef");
            string tagName = "RevenantResistance";
            Helper.CreateDefFromClone(
                source,
                "D424B077-6731-40AD-BFA8-7020BD3A9F9A",
                tagName + "_GameTagDef");
        }
        public static void CreateNewDefsForTFTVStart()
        {
            try
            {
                CreateNewSophiaAndJacob();
                CreateInitialInfiltrator();
                CreateInitialPriest();
                CreateInitialTechnician();
                CreateStartingTemplatesBuffed();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void CreateStartingTemplatesBuffed()
        {
            try
            {
                TacCharacterDef Jacob2 = DefCache.GetDef<TacCharacterDef>("PX_Jacob_Tutorial2_TacCharacterDef");
                TacCharacterDef Sophia2 = DefCache.GetDef<TacCharacterDef>("PX_Sophia_Tutorial2_TacCharacterDef");
                TacCharacterDef Omar3 = DefCache.GetDef<TacCharacterDef>("PX_Omar_Tutorial3_TacCharacterDef");
                TacCharacterDef Takeshi3 = DefCache.GetDef<TacCharacterDef>("PX_Takeshi_Tutorial3_TacCharacterDef");
                TacCharacterDef Irina3 = DefCache.GetDef<TacCharacterDef>("PX_Irina_Tutorial3_TacCharacterDef");


                Helper.CreateDefFromClone(Jacob2, "B1968124-ABDD-4A2C-9CBC-33DBC0EE3EE5", "PX_JacobBuffed_TFTV_TacCharacterDef");
                Helper.CreateDefFromClone(Sophia2, "B3EA411B-DE35-4B63-874A-553D816C06BC", "PX_SophiaBuffed_TFTV_TacCharacterDef");
                Helper.CreateDefFromClone(Omar3, "024AB8C6-A2CD-4B81-A927-C8713A008EF2", "PX_OmarBuffed_TFTV_TacCharacterDef");
                Helper.CreateDefFromClone(Takeshi3, "4230D9B4-6D88-4545-8680-BBCAE463356B", "PX_TakeshiBuffed_TFTV_TacCharacterDef");
                Helper.CreateDefFromClone(Irina3, "8E57C25C-7289-4F7C-9D0C-5F8E55601B49", "PX_IrinaBuffed_TFTV_TacCharacterDef");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateNewSophiaAndJacob()
        {
            try
            {
                TacCharacterDef Jacob2 = DefCache.GetDef<TacCharacterDef>("PX_Jacob_Tutorial2_TacCharacterDef");
                TacCharacterDef Sophia2 = DefCache.GetDef<TacCharacterDef>("PX_Sophia_Tutorial2_TacCharacterDef");
                TacCharacterDef newJacob = Helper.CreateDefFromClone(Jacob2, "DDA13436-40BE-4096-9C69-19A3BF6658E6", "PX_Jacob_TFTV_TacCharacterDef");
                TacCharacterDef newSophia = Helper.CreateDefFromClone(Sophia2, "D9EC7144-6EB5-451C-9015-3E67F194AB1B", "PX_Sophia_TFTV_TacCharacterDef");

                //  TFTVLogger.Always("TacCharacterDefs found");

                ViewElementDef pX_SniperViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [PX_Sniper_ActorViewDef]");
                //   TFTVLogger.Always("ViewElement Found");
                if (newJacob.Data != null)
                {
                    newJacob.Data.ViewElementDef = pX_SniperViewElementDef;

                    GameTagDef Sniper_CTD = DefCache.GetDef<GameTagDef>("Sniper_ClassTagDef");
                    //   TFTVLogger.Always("SniperCTD");
                    for (int i = 0; i < newJacob.Data.GameTags.Length; i++)
                    {
                        if (newJacob.Data.GameTags[i].GetType() == Sniper_CTD.GetType())
                        {
                            newJacob.Data.GameTags[i] = Sniper_CTD;
                        }
                    }

                    newJacob.Data.Abilites = new TacticalAbilityDef[] // abilities -> Class proficiency
                    {
                DefCache.GetDef<ClassProficiencyAbilityDef>("Sniper_ClassProficiency_AbilityDef")

                    };
                    //   TFTVLogger.Always("got passed abilities thingy");

                    newJacob.Data.BodypartItems = new ItemDef[] // Armour
                    {
                DefCache.GetDef<TacticalItemDef>("PX_Sniper_Helmet_BodyPartDef"),
                DefCache.GetDef<TacticalItemDef>("PX_Sniper_Torso_BodyPartDef"),
                DefCache.GetDef<TacticalItemDef>("PX_Sniper_Legs_ItemDef")
                    };
                    //     TFTVLogger.Always("got passed abilities thingy");
                    newJacob.Data.EquipmentItems = new ItemDef[] // Ready slots
                   { DefCache.GetDef<WeaponDef>("PX_SniperRifle_WeaponDef"),
                    DefCache.GetDef<WeaponDef>("PX_Pistol_WeaponDef"),
                DefCache.GetDef<TacticalItemDef>("Medkit_EquipmentDef")
                   };
                    newJacob.Data.InventoryItems = new ItemDef[] // Backpack
                    {
                newJacob.Data.EquipmentItems[0].CompatibleAmmunition[0],
                newJacob.Data.EquipmentItems[1].CompatibleAmmunition[0]
                    };

                    newJacob.Data.Strength = 0;
                    newJacob.Data.Will = 0;
                    newJacob.Data.Speed = 0;
                    newJacob.Data.CurrentHealth = -1;

                    newSophia.Data.Strength = 0;
                    newSophia.Data.Will = 0;
                    newSophia.Data.Speed = 0;
                    newSophia.Data.CurrentHealth = -1;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialInfiltrator()
        {
            try
            {
                TacCharacterDef sourceInfiltrator = DefCache.GetDef<TacCharacterDef>("S_SY_Infiltrator_TacCharacterDef");
                TacCharacterDef startingInfiltrator = Helper.CreateDefFromClone(sourceInfiltrator, "8835621B-CFCA-41EF-B480-241D506BD742", "PX_Starting_Infiltrator_TacCharacterDef");
                startingInfiltrator.Data.Strength = 0;
                startingInfiltrator.Data.Will = 0;

                /*   startingInfiltrator.Data.BodypartItems = new ItemDef[] // Armour
                   {
                   DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef")),
                   DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef")),
                   DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef"))
                   };           */
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialPriest()
        {
            try
            {
                TacCharacterDef sourcePriest = DefCache.GetDef<TacCharacterDef>("S_AN_Priest_TacCharacterDef");
                TacCharacterDef startingPriest = Helper.CreateDefFromClone(sourcePriest, "B1C9385B-05D1-453D-8665-4102CCBA77BE", "PX_Starting_Priest_TacCharacterDef");
                startingPriest.Data.Strength = 0;
                startingPriest.Data.Will = 0;

                startingPriest.Data.BodypartItems = new ItemDef[] // Armour
                {
              //  DefCache.GetDef<TacticalItemDef>("AN_Priest_Head02_BodyPartDef")),
                DefCache.GetDef<TacticalItemDef>("AN_Priest_Torso_BodyPartDef"),
                DefCache.GetDef<TacticalItemDef>("AN_Priest_Legs_ItemDef")
                };
                //    TFTVLogger.Always(startingPriest.Data.EquipmentItems.Count().ToString());

                /*  ItemDef[] inventoryList = new ItemDef[]

                  { 
                  startingPriest.Data.EquipmentItems[0].CompatibleAmmunition[0],
                  startingPriest.Data.EquipmentItems[1].CompatibleAmmunition[0]
                  };*/

                startingPriest.Data.EquipmentItems = new ItemDef[] // Ready slots
                { DefCache.GetDef<WeaponDef>("AN_Redemptor_WeaponDef"),
                  //  DefCache.GetDef<WeaponDef>("Medkit_EquipmentDef"))
                };
                // startingPriest.Data.InventoryItems = inventoryList;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateInitialTechnician()
        {
            try
            {
                TacCharacterDef sourceTechnician = DefCache.GetDef<TacCharacterDef>("NJ_Technician_TacCharacterDef");
                TacCharacterDef startingTechnician = Helper.CreateDefFromClone(sourceTechnician, "1D0463F9-6684-4CE1-82CA-386FC2CE18E3", "PX_Starting_Technician_TacCharacterDef");
                startingTechnician.Data.Strength = 0;
                startingTechnician.Data.Will = 0;
                startingTechnician.Data.LevelProgression.Experience = 0;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateHumanEnemiesTags()
        {
            try
            {


                string tagName = "HumanEnemy";
                string anu = "anu";
                string bandit = "ban";
                string newJericho = "nj";
                string synedrion = "syn";
                string forsaken = "FallenOnes";
                string pure = "Purists";

                GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                GameTagDef tier1GameTag = Helper.CreateDefFromClone(
                    source,
                    "11F227E3-A45A-44EE-8B93-94E59D8C7B53",
                    tagName + "Tier_1_" + "GameTagDef");
                GameTagDef tier2GameTag = Helper.CreateDefFromClone(
                    source,
                    "CE88CFDB-B010-40A7-A86A-C842DF5F35CF",
                    tagName + "Tier_2_" + "GameTagDef");
                GameTagDef tier3GameTag = Helper.CreateDefFromClone(
                    source,
                    "D4E764C5-3978-40C3-8CED-AFAF81B40BF8",
                    tagName + "Tier_3_" + "GameTagDef");
                GameTagDef tier4GameTag = Helper.CreateDefFromClone(
                    source,
                    "21D065AC-432F-4D29-92AF-5355EF972E38",
                    tagName + "Tier_4_" + "GameTagDef");
                GameTagDef anuGameTag = Helper.CreateDefFromClone(
                    source,
                    "1C8EC6EF-CE51-4AC5-B799-128FDE6ABF14",
                    tagName + "Faction_" + anu + "_GameTagDef");
                GameTagDef banditGameTag = Helper.CreateDefFromClone(
                    source,
                    "78993F15-9233-4C49-B8C3-13144156E438",
                    tagName + "Faction_" + bandit + "_GameTagDef");
                GameTagDef newJerichoGameTag = Helper.CreateDefFromClone(
                    source,
                    "62980A28-8E7A-4F0D-A01C-B58C4D085677",
                    tagName + "Faction_" + newJericho + "_GameTagDef");
                GameTagDef SynedrionGameTag = Helper.CreateDefFromClone(
                    source,
                    "B29CEA3A-6C24-4872-9773-02E2FC21F645",
                    tagName + "Faction_" + synedrion + "_GameTagDef");
                GameTagDef forsakenGameTag = Helper.CreateDefFromClone(
                    source,
                    "133FA2A8-C93D-43A9-BEFB-E5FAAAC43AFF",
                    tagName + "Faction_" + forsaken + "_GameTagDef");
                GameTagDef pureGameTag = Helper.CreateDefFromClone(
                    source,
                    "DDDAB7AC-1317-4B37-AB18-1E57F8D30147",
                    tagName + "Faction_" + pure + "_GameTagDef");
                GameTagDef humanEnemyTag = Helper.CreateDefFromClone(
                    source,
                    "BF6F6546-AE38-47E0-B581-FDB8F8F5171D",
                    tagName + "_GameTagDef");


                //    TFTVLogger.Always("Human Enemy Tags created");

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void CreateAmbushAbility()
        {
            try
            {

                string skillName = "HumanEnemiesTacticsAmbush_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef ambushAbility = Helper.CreateDefFromClone(
                    source,
                    "31785839-0687-4065-ACFB-255C1A1CE63D",
                    skillName);
                ambushAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "136290BA-D672-4EEF-822E-F3B8FF27496C",
                    skillName);
                ambushAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6D47E347-35DE-4E8E-B6FF-9B9DF0598175",
                    skillName);
                ambushAbility.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                };
                ambushAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                ambushAbility.ViewElementDef.DisplayName1.LocalizationKey = "HUMAN_ENEMIES_KEY_AMBUSH";
                ambushAbility.ViewElementDef.Description.LocalizationKey = "HUMAN_ENEMIES_KEY_AMBUSH_DESCRIPTION";
                   
                Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_TacticalAnalyst.png");
                ambushAbility.ViewElementDef.LargeIcon = icon;
                ambushAbility.ViewElementDef.SmallIcon = icon;

                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
