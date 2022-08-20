using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDeliriumPerks
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        internal static bool doNotLocalize = false;
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

        public static void Main()
        {
            Clone_GameTag();
            AddAnimation();
            Create_InnerSight();
            Create_Terror();
            Create_FasterSynapses();
            Create_Anxiety();
            Create_OneOfThem();
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
        }

        public static void AddAnimation()
        {
            ApplyStatusAbilityDef devour = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutog_Devour_AbilityDef"));
            PlayActionAnimationAbilityDef devourAnim = Repo.GetAllDefs<PlayActionAnimationAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutog_PlayDevourAnimation_AbilityDef"));

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
        public static void Clone_GameTag()
        {
            string skillName = "Takashi_GameTagDef";
            GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
            GameTagDef Takashi = Helper.CreateDefFromClone(
                source,
                "F9FF0EF9-4800-4355-B6F4-5543994C129F",
                skillName);

            TacticalVoxelMatrixDataDef tVMDD = Repo.GetAllDefs<TacticalVoxelMatrixDataDef>().FirstOrDefault(dtb => dtb.name.Equals("TacticalVoxelMatrixDataDef"));
            tVMDD.MistImmunityTags = new GameTagsList()
            {
                Takashi,
            };
        }

        public static void Create_InnerSight()
        {
            string skillName = "InnerSight_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
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
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Volunteered_1-2.png");
            shutEye.ViewElementDef.LargeIcon = icon;
            shutEye.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Anxiety()
        {
            string skillName = "AnxietyAbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
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
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Paranoid_2-1.png");
            hallucinating.ViewElementDef.LargeIcon = icon;
            hallucinating.ViewElementDef.SmallIcon = icon;
        }

        public static void Clone_Inspire()
        {
            string skillName = "BloodthirstyHP_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
            ApplyStatusAbilityDef bloodthirsty = Helper.CreateDefFromClone(
                source,
                "FF52ACBE-FFB2-4A96-8DC2-0B8072036669",
                skillName);
            bloodthirsty.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "B5D6B88F-5F0A-4B3B-9F53-3E14276F4533",
                skillName);
            bloodthirsty.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "0078B9D3-8DFF-40C6-A009-8B572EFCF87A",
                skillName);

            OnActorDeathEffectStatusDef bloodthirstyStatus = Helper.CreateDefFromClone(
                bloodthirsty.StatusDef as OnActorDeathEffectStatusDef,
                "42600C75-8E8A-4AC9-B192-49960957CAAA",
                "E_KillListenerStatus [" + skillName + "]");

            FactionMembersEffectDef bloodthirstyStatus2 = Helper.CreateDefFromClone(
                bloodthirstyStatus.EffectDef as FactionMembersEffectDef,
                "452133A6-BB2E-4DE7-B561-073CCBE48D49",
                "E_Effect [" + skillName + "]");

            StatsModifyEffectDef bloodthirstySingleEffectDef2 = Helper.CreateDefFromClone(
                bloodthirstyStatus2.SingleTargetEffect as StatsModifyEffectDef,
                "9F39B97B-9DB7-4076-96AE-4AAD317E1A6D",
                "E_SingleTargetEffect [" + skillName + "]");

            bloodthirsty.ApplyStatusToAllTargets = false;
            //(fleshEater.StatusDef as OnActorDeathEffectStatusDef).EffectDef = fleshEaterEffectDef;
            //fleshEaterEffectDef.SingleTargetEffect = fleshEaterSingleTargetEffectDef;
            //fleshEaterSingleTargetEffectDef.StatModifications[0].StatName = "";
            bloodthirsty.StatusDef = bloodthirstyStatus;
            bloodthirstyStatus.EffectDef = bloodthirstyStatus2;
            bloodthirstyStatus2.SingleTargetEffect = bloodthirstySingleEffectDef2;
            bloodthirstyStatus2.IgnoreTargetActor = false;

            bloodthirstySingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "Health",
                    Value = 80,
                }
            };
        }


        public static void Create_Bloodthirsty()
        {
            string skillName = "Bloodthirsty_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
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
                    Value = -2,
                }
            };

            bloodthirsty.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_NAME";
            bloodthirsty.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_Devour_AbilityDef]")).LargeIcon;
            bloodthirsty.ViewElementDef.LargeIcon = icon;
            bloodthirsty.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_FasterSynapses()
        {
            string skillName = "FasterSynapses_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
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
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_View [WarCry_AbilityDef]")).LargeIcon;
            angerIssues.ViewElementDef.LargeIcon = icon;
            angerIssues.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Terror()
        {
            string skillName = "Terror_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
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
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_NightOwl.png");
            photophobia.ViewElementDef.LargeIcon = icon;
            photophobia.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_WolverinePassive()
        {
            string skillName = "WolverinePassive_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Cautious_AbilityDef"));
            PassiveModifierAbilityDef nailsPassive = Helper.CreateDefFromClone(
                source,
                "b3185867-ca87-4e59-af6d-012267a7bd25",
                skillName);
            nailsPassive.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3e57b19b-11e1-42b9-81f4-c9cc9fffc42d",
                skillName);
            nailsPassive.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "3f170800-b819-4237-80a3-c9b9daa9dab4",
                skillName);
            nailsPassive.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = -0.2f
                },
              };
            nailsPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            nailsPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_NAME";
            nailsPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_WOLVERINE_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutoid_SlashingStrike_AbilityDef]")).SmallIcon;
            nailsPassive.ViewElementDef.LargeIcon = icon;
            nailsPassive.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Wolverine()
        {
            string skillName = "Wolverine_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Mutoid_Adapt_RightArm_Slasher_AbilityDef"));
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
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutoid_SlashingStrike_AbilityDef]")).SmallIcon;
            nails.ViewElementDef.LargeIcon = icon;
            nails.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_OneOfThem()
        {
            string skillName = "OneOfThem_AbilityDef";
            DamageMultiplierAbilityDef source = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("VirusResistant_DamageMultiplierAbilityDef"));
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

            oneOfUs.DamageTypeDef = Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Mist_SpawnVoxelDamageTypeEffectDef"));
            oneOfUs.Multiplier = 0;

            oneOfUs.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
            oneOfUs.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-2.png");
            oneOfUs.ViewElementDef.LargeIcon = icon;
            oneOfUs.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_OneOfThemPassive()
        {
            string skillName = "OneOfThemPassive_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
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

            oneOfThemPassive.StatModifications = new ItemStatModification[]
              {
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
              };

            DamageMultiplierStatusDef mistResistance = Repo.GetAllDefs<DamageMultiplierStatusDef>().FirstOrDefault(a => a.name.Contains("MistResistance_StatusDef"));
            mistResistance.Multiplier = 0.0f;
            oneOfThemPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            oneOfThemPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
            oneOfThemPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-2.png");
            oneOfThemPassive.ViewElementDef.LargeIcon = icon;
            oneOfThemPassive.ViewElementDef.SmallIcon = icon;
        }
        

        public static void Create_Feral()
        {
            try
            {
                string skillName = "Feral_AbilityDef";
                ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("RapidClearance_AbilityDef"));
                ProcessDeathReportEffectDef sourceEffect = Repo.GetAllDefs<ProcessDeathReportEffectDef>().FirstOrDefault(p => p.name.Equals("E_Effect [RapidClearance_AbilityDef]"));
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
                Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_PrimalInstinct_AbilityDef]")).LargeIcon;
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
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
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
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Privileged_1-2.png");
            solipsism.ViewElementDef.LargeIcon = icon;
            solipsism.ViewElementDef.SmallIcon = icon;
        }

        public static void Create_AnxietyStatus()
        {
            string skillName = "Anxiety_StatusDef";
            SilencedStatusDef source = Repo.GetAllDefs<SilencedStatusDef>().FirstOrDefault(p => p.name.Equals("ActorSilenced_StatusDef"));
            SilencedStatusDef hallucinatingStatus = Helper.CreateDefFromClone(
                source,
                "2d5ed7eb-f4f3-42bf-8589-1d50ec99fa8b",
                skillName);

            hallucinatingStatus.DurationTurns = 1;
        }

        public static void Create_Derealization()
        {
            string skillName = "Derealization_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef derealization = Helper.CreateDefFromClone(
                source,
                "51ddff8e-49d0-4cca-8f4f-53aa39fcbce9",
                skillName);
            derealization.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3efc6f6b-8c57-405b-afe4-f20491336bd5",
                skillName);
            derealization.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "604181c6-fd18-46be-a3af-0b756a8200f1",
                skillName);
            derealization.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = -5,
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = -5,
                },
              };
            derealization.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            derealization.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
            derealization.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Vampire.png");
            derealization.ViewElementDef.LargeIcon = icon;
            derealization.ViewElementDef.SmallIcon = icon;
        }

        public static void Create_DerealizationIgnorePain()
        {
            string skillName = "DerealizationIgnorePain_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("IgnorePain_AbilityDef"));
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

            derealizationIgnorePain.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
            derealizationIgnorePain.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Vampire.png");
            derealizationIgnorePain.ViewElementDef.LargeIcon = icon;
            derealizationIgnorePain.ViewElementDef.SmallIcon = icon;
        }

        public static void Clone_ArmorBuffStatus()
        {
            try
            {

                string skillName = "ArmorBuffStatus_StatusDef";
                ItemSlotStatsModifyStatusDef source = Repo.GetAllDefs<ItemSlotStatsModifyStatusDef>().FirstOrDefault(p => p.name.Equals("E_Status [Acheron_RestorePandoranArmor_AbilityDef]"));
                ItemSlotStatsModifyStatusDef armorBuffStatus = Helper.CreateDefFromClone(
                    source,
                    "D2B46847-FC47-436D-A940-19CDEF472ED1",
                    skillName);

                armorBuffStatus.StatsModifications = new ItemSlotStatsModifyStatusDef.ItemSlotModification[]
                {
                new ItemSlotStatsModifyStatusDef.ItemSlotModification()
                {
                    Type = ItemSlotStatsModifyStatusDef.StatType.Armour,
                    ModificationType = StatModificationType.Add,
                    Value = 10,
                },
                new ItemSlotStatsModifyStatusDef.ItemSlotModification()
                {
                    Type = ItemSlotStatsModifyStatusDef.StatType.Armour,
                    ModificationType = StatModificationType.AddMax,
                    Value = 10,
                },
                };



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_DeliriumPerks_Patch
        {
            public static void Postfix(TacticalActorBase actor)
            {
                try
                {

                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.PhoenixFactionDef)
                    {
                        TacticalActor tacticalActor = actor as TacticalActor;

                        TacticalAbilityDef fasterSynapsesDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("FasterSynapses_AbilityDef"));
                        TacticalAbilityDef anxietyDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("AnxietyAbilityDef"));
                        TacticalAbilityDef bloodthirstyDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Bloodthirsty_AbilityDef"));
                        TacticalAbilityDef oneOfThemDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("OneOfThemPassive_AbilityDef"));
                        TacticalAbilityDef wolverineDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Wolverine_AbilityDef"));
                        TacticalAbilityDef derealizationDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("DerealizationIgnorePain_AbilityDef"));

                        if (actor.GetAbilityWithDef<Ability>(fasterSynapsesDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Frenzy_StatusDef")));
                            TFTVLogger.Always(actor.DisplayName + " with " + fasterSynapsesDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(anxietyDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Anxiety_StatusDef")));
                            TFTVLogger.Always(actor.DisplayName + " with " + anxietyDef.name);
                        }

                    /*    if (actor.GetAbilityWithDef<Ability>(bloodthirstyDef) != null)
                        {
                            tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("BloodthirstyHP_AbilityDef")), actor);
                            TFTVLogger.Always(actor.DisplayName + " with " + bloodthirstyDef.name);
                        }*/

                        if (actor.GetAbilityWithDef<Ability>(oneOfThemDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("MistResistance_StatusDef")));
                            tacticalActor.GameTags.Add(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(sd => sd.name.Equals("Takashi_GameTagDef")), GameTagAddMode.ReplaceExistingExclusive);
                            TFTVLogger.Always(actor.DisplayName + " with " + oneOfThemDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(wolverineDef) != null)
                        {
                            tacticalActor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("WolverinePassive_AbilityDef")), actor);
                            TFTVLogger.Always(actor.DisplayName + " with " + wolverineDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(derealizationDef) != null)
                        {
                            
                            tacticalActor.CharacterStats.Endurance.Value.ModificationValue -= 5;
                            tacticalActor.CharacterStats.Endurance.Max.ModificationValue -= 5;
                            tacticalActor.UpdateStats();


                            // tacticalActor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(sd => sd.name.Equals("IgnorePain_AbilityDef")), actor);

                            TFTVLogger.Always(actor.DisplayName + " with " + derealizationDef.name);

                        }

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(TacticalAbility), "FumbleActionCheck")]
        public static class TacticalAbility_FumbleActionCheck_Patch
        {
            public static void Postfix(TacticalAbility __instance, ref bool __result)
            {
                DefRepository Repo = GameUtl.GameComponent<DefRepository>();

                try
                {
                    TacticalAbilityDef abilityDef9 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Feral_AbilityDef"));
                    if (__instance.TacticalActor.GetAbilityWithDef<TacticalAbility>(abilityDef9) != null && __instance.Source is Equipment)
                    {
                        __result = UnityEngine.Random.Range(0, 100) < 10;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        //Dtony's Delirium perks patch
        [HarmonyPatch(typeof(RecruitsListElementController), "SetRecruitElement")]
        public static class RecruitsListElementController_SetRecruitElement_Patch
        {
            public static bool Prefix(RecruitsListElementController __instance, RecruitsListEntryData entryData, List<RowIconTextController> ____abilityIcons)
            {
                try
                {
                    if (____abilityIcons == null)
                    {
                        ____abilityIcons = new List<RowIconTextController>();
                        if (__instance.PersonalTrackRoot.transform.childCount < entryData.PersonalTrackAbilities.Count())
                        {
                            RectTransform parent = __instance.PersonalTrackRoot.GetComponent<RectTransform>();
                            RowIconTextController source = parent.GetComponentInChildren<RowIconTextController>();
                            parent.DetachChildren();
                            source.Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);
                            for (int i = 0; i < entryData.PersonalTrackAbilities.Count(); i++)
                            {
                                RowIconTextController entry = UnityEngine.Object.Instantiate(source, parent, true);
                            }
                        }
                        UIUtil.GetComponentsFromContainer(__instance.PersonalTrackRoot.transform, ____abilityIcons);
                    }
                    __instance.RecruitData = entryData;
                    __instance.RecruitName.SetSoldierData(entryData.Recruit);
                    BC_SetAbilityIcons(entryData.PersonalTrackAbilities.ToList(), ____abilityIcons);
                    if (entryData.SuppliesCost != null && __instance.CostText != null && __instance.CostColorController != null)
                    {
                        __instance.CostText.text = entryData.SuppliesCost.ByResourceType(ResourceType.Supplies).RoundedValue.ToString();
                        __instance.CostColorController.SetWarningActive(!entryData.IsAffordable, true);
                    }
                    __instance.NavHolder.RefreshNavigation();
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }


            private static void BC_SetAbilityIcons(List<TacticalAbilityViewElementDef> abilities, List<RowIconTextController> abilityIcons)
            {
                foreach (RowIconTextController rowIconTextController in abilityIcons)
                {
                    rowIconTextController.gameObject.SetActive(false);
                }
                for (int i = 0; i < abilities.Count; i++)
                {
                    abilityIcons[i].gameObject.SetActive(true);
                    abilityIcons[i].SetController(abilities[i].LargeIcon, abilities[i].DisplayName1, abilities[i].Description);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_Patch
        {
            public static void Postfix(TacticalActor __instance, DeathReport death)
            {
                DefRepository Repo = GameUtl.GameComponent<DefRepository>();
                try
                {
                    TacticalAbilityDef hyperalgesiaAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Hyperalgesia_AbilityDef"));
                    TacticalAbilityDef feralAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Feral_AbilityDef"));
                    TacticalAbilityDef bloodthirstyAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Bloodthirsty_AbilityDef"));

                    TacticalAbility hyperAlgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                    TacticalAbility feral = __instance.GetAbilityWithDef<TacticalAbility>(feralAbilityDef);
                    TacticalAbility bloodthirsty = __instance.GetAbilityWithDef<TacticalAbility>(bloodthirstyAbilityDef);

                    if (hyperAlgesia != null)
                    {
                        TacticalFaction tacticalFaction = death.Actor.TacticalFaction;
                        int num = (int)__instance.RelationTo(tacticalFaction);
                        int willPointWorth = death.Actor.TacticalActorBaseDef.WillPointWorth;
                        if (death.Actor.TacticalFaction == __instance.TacticalFaction)
                        {
                            __instance.CharacterStats.WillPoints.Add(willPointWorth);
                        }
                    }
                    if (feral != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.ActionPoints.Add(__instance.CharacterStats.ActionPoints.Max/4);
                    }
                    if (bloodthirsty != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.Health.AddRestrictedToMax(death.Actor.Health.Max/2);
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static void Postfix(TacticalActor __instance, DamageResult damageResult)
            {
                DefRepository Repo = GameUtl.GameComponent<DefRepository>();
                try
                {
                    TacticalAbilityDef hyperalgesiaDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Hyperalgesia_AbilityDef"));
                 //   TacticalAbilityDef derealizationDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Derealization_AbilityDef"));

                    TacticalAbility hyperalgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaDef);
                  //  TacticalAbility derealization = __instance.GetAbilityWithDef<TacticalAbility>(derealizationDef);
                    if (__instance.IsAlive && hyperalgesia != null)
                    {
                        __instance.CharacterStats.WillPoints.Subtract(1);
                    }
                 /*   if (__instance.IsAlive && derealization != null)
                    {
                        
                        typeof(DamageResult).GetMethod("get_HealthDamage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(damageResult, new object[] {   });
                    }*/

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


    }
}

