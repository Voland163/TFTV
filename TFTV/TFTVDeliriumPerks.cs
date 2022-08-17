using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDeliriumPerks
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        internal static bool doNotLocalize = false;

        public static void Main()
        {
            Clone_GameTag();
            AddAnimation();
            Create_ShutEye();
            Create_Photophobia();
            Create_AngerIssues();
            Create_Hallucinating();
            Create_OneOfUs();
            Create_OneOfUsPassive();
            Create_FleshEater();
            Clone_Inspire();
            Create_Nails();
            Create_NailsPassive();
            Create_Immortality();
            Create_Feral();
            Create_Solipsism();
            Clone_ArmorBuffStatus();
            Create_HallucinatingStatus();
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

        public static void Create_ShutEye()
        {
            string skillName = "ShutEye_AbilityDef";
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
        public static void Create_Hallucinating()
        {
            string skillName = "Hallucinating_AbilityDef";
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
            string skillName = "FleshEaterHP_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
            ApplyStatusAbilityDef fleshEater = Helper.CreateDefFromClone(
                source,
                "FF52ACBE-FFB2-4A96-8DC2-0B8072036669",
                skillName);
            fleshEater.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "B5D6B88F-5F0A-4B3B-9F53-3E14276F4533",
                skillName);
            fleshEater.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "0078B9D3-8DFF-40C6-A009-8B572EFCF87A",
                skillName);

            OnActorDeathEffectStatusDef fleshEaterStatus = Helper.CreateDefFromClone(
                fleshEater.StatusDef as OnActorDeathEffectStatusDef,
                "42600C75-8E8A-4AC9-B192-49960957CAAA",
                "E_KillListenerStatus [" + skillName + "]");

            FactionMembersEffectDef fleshEaterEffectDef2 = Helper.CreateDefFromClone(
                fleshEaterStatus.EffectDef as FactionMembersEffectDef,
                "452133A6-BB2E-4DE7-B561-073CCBE48D49",
                "E_Effect [" + skillName + "]");

            StatsModifyEffectDef fleshEaterSingleEffectDef2 = Helper.CreateDefFromClone(
                fleshEaterEffectDef2.SingleTargetEffect as StatsModifyEffectDef,
                "9F39B97B-9DB7-4076-96AE-4AAD317E1A6D",
                "E_SingleTargetEffect [" + skillName + "]");

            fleshEater.ApplyStatusToAllTargets = false;
            //(fleshEater.StatusDef as OnActorDeathEffectStatusDef).EffectDef = fleshEaterEffectDef;
            //fleshEaterEffectDef.SingleTargetEffect = fleshEaterSingleTargetEffectDef;
            //fleshEaterSingleTargetEffectDef.StatModifications[0].StatName = "";
            fleshEater.StatusDef = fleshEaterStatus;
            fleshEaterStatus.EffectDef = fleshEaterEffectDef2;
            fleshEaterEffectDef2.SingleTargetEffect = fleshEaterSingleEffectDef2;
            fleshEaterEffectDef2.IgnoreTargetActor = false;

            fleshEaterSingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "Health",
                    Value = 80,
                }
            };
        }


        public static void Create_FleshEater()
        {
            string skillName = "FleshEater_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("Inspire_AbilityDef"));
            ApplyStatusAbilityDef fleshEater = Helper.CreateDefFromClone(
                source,
                "0319cf53-65d2-4964-98d2-08c1acb54b24",
                skillName);
            fleshEater.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "b101c95b-cd35-4649-9983-2662a454e40f",
                skillName);
            fleshEater.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "ed164c5a-2927-422a-a086-8762137d4c5d",
                skillName);

            OnActorDeathEffectStatusDef fleshEaterStatus = Helper.CreateDefFromClone(
                fleshEater.StatusDef as OnActorDeathEffectStatusDef,
                "ac7195f9-c382-4f79-a956-55d5eb3b6371",
                "E_KillListenerStatus [" + skillName + "]");

            FactionMembersEffectDef fleshEaterEffectDef2 = Helper.CreateDefFromClone(
                fleshEaterStatus.EffectDef as FactionMembersEffectDef,
                "8bd34f58-d452-4f38-975e-4f32b33d283d",
                "E_Effect [" + skillName + "]");

            StatsModifyEffectDef fleshEaterSingleEffectDef2 = Helper.CreateDefFromClone(
                fleshEaterEffectDef2.SingleTargetEffect as StatsModifyEffectDef,
                "ad0891cf-fe7a-443f-acb9-575c3cf23432",
                "E_SingleTargetEffect [" + skillName + "]");


            //(fleshEater.StatusDef as OnActorDeathEffectStatusDef).EffectDef = fleshEaterEffectDef;
            //fleshEaterEffectDef.SingleTargetEffect = fleshEaterSingleTargetEffectDef;
            //fleshEaterSingleTargetEffectDef.StatModifications[0].StatName = "";
            fleshEater.StatusDef = fleshEaterStatus;
            fleshEaterStatus.EffectDef = fleshEaterEffectDef2;
            fleshEaterEffectDef2.SingleTargetEffect = fleshEaterSingleEffectDef2;

            fleshEaterSingleEffectDef2.StatModifications = new List<StatModification>
            {
                new StatModification()
                {
                    Modification = StatModificationType.AddRestrictedToBounds,
                    StatName = "WillPoints",
                    Value = -2,
                }
            };

            fleshEater.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_NAME";
            fleshEater.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_BLOODTHIRSTY_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_Devour_AbilityDef]")).LargeIcon;
            fleshEater.ViewElementDef.LargeIcon = icon;
            fleshEater.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_AngerIssues()
        {
            string skillName = "AngerIssues_AbilityDef";
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
        public static void Create_Photophobia()
        {
            string skillName = "Photophobia_AbilityDef";
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
        public static void Create_NailsPassive()
        {
            string skillName = "NailsPassive_AbilityDef";
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
        public static void Create_Nails()
        {
            string skillName = "Nails_AbilityDef";
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
        public static void Create_OneOfUs()
        {
            string skillName = "OneOfUs_AbilityDef";
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
        public static void Create_OneOfUsPassive()
        {
            string skillName = "OneOfUsPassive_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef ofuPassive = Helper.CreateDefFromClone(
                source,
                "ff35f9ef-ad67-42ff-9dcd-0288dba4d636",
                skillName);
            ofuPassive.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "61e44215-fc05-4383-b9e4-17f384e3d003",
                skillName);
            ofuPassive.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "aaead24e-9dba-4ef7-ba2d-8df142cb9105",
                skillName);

            ofuPassive.StatModifications = new ItemStatModification[]
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
            ofuPassive.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            ofuPassive.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_NAME";
            ofuPassive.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_ONE_OF_THEM_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-2.png");
            ofuPassive.ViewElementDef.LargeIcon = icon;
            ofuPassive.ViewElementDef.SmallIcon = icon;
        }
        public static void Create_Immortality()
        {
            string skillName = "Immortality_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Thief_AbilityDef"));
            PassiveModifierAbilityDef immortality = Helper.CreateDefFromClone(
                source,
                "51ddff8e-49d0-4cca-8f4f-53aa39fcbce9",
                skillName);
            immortality.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3efc6f6b-8c57-405b-afe4-f20491336bd5",
                skillName);
            immortality.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "604181c6-fd18-46be-a3af-0b756a8200f1",
                skillName);
            immortality.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = -4,
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = -4,
                },
              };
            immortality.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            immortality.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_NAME";
            immortality.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_DEREALIZATION_DESCRIPTION";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Vampire.png");
            immortality.ViewElementDef.LargeIcon = icon;
            immortality.ViewElementDef.SmallIcon = icon;
        }

        public static void Create_Feral()
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
            feralEffectDef.RestoreActionPointsFraction = 0.25f;

            feral.ViewElementDef.DisplayName1.LocalizationKey = "DELIRIUM_PERK_FERAL_NAME";
            feral.ViewElementDef.Description.LocalizationKey = "DELIRIUM_PERK_FERAL_DESCRIPTION";
            Sprite icon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tav => tav.name.Equals("E_ViewElement [Mutog_PrimalInstinct_AbilityDef]")).LargeIcon;
            feral.ViewElementDef.LargeIcon = icon;
            feral.ViewElementDef.SmallIcon = icon;
            feralStatusDef.ExpireOnEndOfTurn = false;
        }
        public static void Create_Solipsism()
        {
            string skillName = "Solipsism_AbilityDef";
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

        public static void Create_HallucinatingStatus()
        {
            string skillName = "Hallucinating_StatusDef";
            SilencedStatusDef source = Repo.GetAllDefs<SilencedStatusDef>().FirstOrDefault(p => p.name.Equals("ActorSilenced_StatusDef"));
            SilencedStatusDef hallucinatingStatus = Helper.CreateDefFromClone(
                source,
                "2d5ed7eb-f4f3-42bf-8589-1d50ec99fa8b",
                skillName);

            hallucinatingStatus.DurationTurns = 2;
        }

        public static void Clone_ArmorBuffStatus()
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
    }
}

