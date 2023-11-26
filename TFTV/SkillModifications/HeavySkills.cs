using Base.Cameras.ExecutionNodes;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Cameras.Filters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.Statuses.ItemSlotStatsModifyStatusDef;

namespace PRMBetterClasses.SkillModifications
{
    internal class HeavySkills
    {
        // Get config, definition repository and shared data
        //private static readonly Settings Config = BetterClassesMain.Config;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //private static readonly SharedData Shared = BetterClassesMain.Shared;

        //private static readonly bool doNotLocalize = BetterClassesMain.doNotLocalize;

        public static void ApplyChanges()
        {
            // Return Fire: Fix to work on all classes
            Change_ReturnFire();

            // Hunker Down: -25% incoming damage for 2 AP and 2 WP
            Create_HunkerDown();

            // Skimisher: If you take damage during enemy turn your attacks deal 25% more damage until end of turn
            Create_Skirmisher();

            // Shred Resistance: 50% shred resistance
            Create_ShredResistance();

            // Rage Burst: Increase accuracy and cone angle
            Change_RageBurst();

            // Jetpack Control: 2 AP jump, 12 tiles range
            Create_JetpackControl();

            // Boom Blast: -30% range instead of +50%
            Change_BoomBlast();

            // War Cry: -1 AP and -10% damage, doubled if WP of target < WP of caster (see Harmony patch below)
            //Change_WarCry();
        }

        private static void Change_ReturnFire()
        {
            TacticalAbilityDef returnFire = DefCache.GetDef<TacticalAbilityDef>("ReturnFire_AbilityDef"); // Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("ReturnFire_AbilityDef"));
            returnFire.ActorTags = new GameTagDef[0]; // Deletes all given tags => no restriction for any class
        }
        private static void Create_HunkerDown()
        {
            string skillName = "HunkerDown_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");
            ApplyStatusAbilityDef hunkerDown = Helper.CreateDefFromClone(
                source,
                "a3d841c5-b3dd-440b-ae4e-629dcabd14df",
                skillName);
            hunkerDown.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "64add472-da6f-4584-b5e9-f204b7d3c735",
                skillName);
            hunkerDown.TargetingDataDef = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef").TargetingDataDef;
            hunkerDown.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "c0b8b645-b1b7-4f4e-87ea-3f6bacc2dc4f",
                skillName);

            ItemSlotStatsModifyStatusDef hunkerDownArmourBuffStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]"),
                "adc38c08-1878-422f-a37c-a859aa67ceed",
                "E_ArmourModifier [HunkerDown_AbilityDef]");
            hunkerDownArmourBuffStatus.Visuals = Helper.CreateDefFromClone(
                hunkerDown.ViewElementDef,
                "C14AA324-3F07-4607-A1B9-75AFED9E2143",
                "E_Visuals_ArmourModifier [HunkerDown_AbilityDef]");
            hunkerDownArmourBuffStatus.Visuals.DisplayName1.LocalizationKey = "PR_BC_HUNKER_DOWN_ARMOR_STATUS";
            hunkerDownArmourBuffStatus.Visuals.Description.LocalizationKey = "PR_BC_HUNKER_DOWN_ARMOR_STATUS_DESC";
            hunkerDownArmourBuffStatus.StatsModifications = new ItemSlotModification[]
            {
                new ItemSlotModification()
                {
                    Type = StatType.Armour,
                    ModificationType = StatModificationType.AddMax,
                    Value = 10f,
                    ShowsNotification = false,
                    NotifyOnce = false
                },
                new ItemSlotModification()
                {
                    Type = StatType.Armour,
                    ModificationType = StatModificationType.AddRestrictedToBounds,
                    Value = 10f,
                    ShowsNotification = true,
                    NotifyOnce = true
                }
            };

            ChangeAbilitiesCostStatusDef hunkerDownApCostModifier = Helper.CreateDefFromClone(
                DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_AbilityCostModifier [QuickAim_AbilityDef]"),
                "8D495936-8AEB-42B2-94A2-04CB7604D545",
                "E_AbilityCostModifier [HunkerDown_AbilityDef]");
            hunkerDownApCostModifier.DurationTurns = 1;
            hunkerDownApCostModifier.ExpireOnEndOfTurn = true;
            hunkerDownApCostModifier.SingleInstance = false;
            //hunkerDownApCostModifier.AbilityCostModification.ActionPointModType = TacticalAbilityModificationType.Multiply;
            //hunkerDownApCostModifier.AbilityCostModification.ActionPointMod = 2f / 3;

            AddAttackBoostStatusDef hunkerDownAddAttackBoostStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<AddAttackBoostStatusDef>("E_Status [QuickAim_AbilityDef]"),
                "B75DFE69-6E49-4277-9312-DAEC9A8220B4",
                "E_AddAttackBoostStatus [HunkerDown_AbilityDef]");
            hunkerDownAddAttackBoostStatus.DurationTurns = 1;
            hunkerDownAddAttackBoostStatus.ExpireOnEndOfTurn = true;
            hunkerDownAddAttackBoostStatus.ShowNotification = true;
            hunkerDownAddAttackBoostStatus.Visuals = Helper.CreateDefFromClone(
                hunkerDown.ViewElementDef,
                "ABD59E47-EF36-4FB8-8ED6-ACDB2C317C37",
                "E_Visuals_AddAttackBoostStatus [HunkerDown_AbilityDef]");
            hunkerDownAddAttackBoostStatus.Visuals.DisplayName1.LocalizationKey = "PR_BC_HUNKER_DOWN_AP_STATUS";
            hunkerDownAddAttackBoostStatus.Visuals.Description.LocalizationKey = "PR_BC_HUNKER_DOWN_AP_STATUS_DESC";
            hunkerDownAddAttackBoostStatus.AdditionalStatusesToApply = new TacStatusDef[] { hunkerDownArmourBuffStatus, hunkerDownApCostModifier };

            hunkerDown.StatusDef = hunkerDownAddAttackBoostStatus;

            hunkerDown.Active = true;
            hunkerDown.EndsTurn = true;
            hunkerDown.ActionPointCost = 0.25f;
            hunkerDown.WillPointCost = 2.0f;
            hunkerDown.DisablingStatuses = new StatusDef[0];// { hunkerDown.StatusDef };
            hunkerDown.TraitsRequired = new string[] { "start", "ability", "move" };
            hunkerDown.TraitsToApply = new string[] { "ability" };
            hunkerDown.ShowNotificationOnUse = true;
            hunkerDown.StatusApplicationTrigger = StatusApplicationTrigger.ActivateAbility;
            hunkerDown.CharacterProgressionData.RequiredStrength = 0;
            hunkerDown.CharacterProgressionData.RequiredWill = 0;
            hunkerDown.CharacterProgressionData.RequiredSpeed = 0;
            hunkerDown.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_HUNKER_DOWN";
            hunkerDown.ViewElementDef.Description.LocalizationKey = "PR_BC_HUNKER_DOWN_DESC";
            Sprite hunkerDownIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
            hunkerDown.ViewElementDef.LargeIcon = hunkerDownIcon;
            hunkerDown.ViewElementDef.SmallIcon = hunkerDownIcon;

            // Animation related stuff
            AbilityDef animationSearchDef = DefCache.GetDef<AbilityDef>("QuickAim_AbilityDef");
            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animationSearchDef) && !animActionDef.AbilityDefs.Contains(hunkerDown))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(hunkerDown).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
        // Harmony patch for HunkerDown to prevent counting return fire for the attack boost
        // => don't check abilities that are executed in the enemy turn
        [HarmonyPatch(typeof(AddAttackBoostStatus), "AbilityExecutedHandler")]
        internal static class HunkerDown_AddAttackBoostStatusDef_AbilityExecutedHandler_patch
        {
            public static bool Prefix(AddAttackBoostStatus __instance)
            {
                TacStatusDef hdBaseStatus = DefCache.GetDef<TacStatusDef>("E_AddAttackBoostStatus [HunkerDown_AbilityDef]");
                // If it is not in the turn of the status holder then don't monitor this ability
                return __instance.TacStatusDef != hdBaseStatus || __instance.TacticalActor.DuringOwnTurn;
            }
        }

        private static void Create_Skirmisher()
        {
            float damageMod = 1.25f;
            string skillName = "Skirmisher_AbilityDef";

            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_DynamicResistance_AbilityDef");
            ApplyStatusAbilityDef skirmisher = Helper.CreateDefFromClone(
                source,
                "d6d9041b-9763-4673-a057-2bbefd96aa67",
                skillName);
            skirmisher.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData, //Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("MasterMarksman_AbilityDef")).CharacterProgressionData,
                "657f3e2b-08c0-4234-b16f-3f6d57d049e1",
                skillName);
            skirmisher.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "5ef3fb17-03d0-4e33-b76a-d74cbeefc509",
                skillName);
            skirmisher.StatusDef = Helper.CreateDefFromClone(
                source.StatusDef,
                "2bafd8da-f84a-4fd7-ae41-8ba0f9e7aba6",
                skillName);
            skirmisher.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SKIRMISHER"; // new LocalizedTextBind("SKIRMISHER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            skirmisher.ViewElementDef.Description.LocalizationKey = "PR_BC_SKIRMISHER_DESC"; // new LocalizedTextBind($"If you take damage during enemy turn your attacks deal {(damageMod * 100) - 100}% more damage until end of turn.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //skirmisher.ViewElementDef.Color = new Color(0, 0, 0, 0); // Color.yellow
            Sprite skirmisherIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Gifted.png");
            skirmisher.ViewElementDef.LargeIcon = skirmisherIcon;
            skirmisher.ViewElementDef.SmallIcon = skirmisherIcon;

            StanceStatusDef skirmisherDamageModification = Helper.CreateDefFromClone(
                DefCache.GetDef<StanceStatusDef>("E_SneakAttackStatus [SneakAttack_AbilityDef]"),
                "728f321f-3a9d-4e63-a160-660c2a2c4664",
                $"E_DamageModificationStatus [{skillName}]");

            skirmisherDamageModification.DurationTurns = 1;
            skirmisherDamageModification.SingleInstance = true;
            skirmisherDamageModification.Visuals = skirmisher.ViewElementDef;
            skirmisherDamageModification.EquipmentsStatModifications = new EquipmentItemTagStatModification[0];
            skirmisherDamageModification.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.BonusAttackDamage,
                    Modification = StatModificationType.Multiply,
                    Value = damageMod
                }
            };
            skirmisherReactionStatus = (DynamicResistanceStatusDef)skirmisher.StatusDef;
            skirmisherReactionStatus.ResistanceStatuses = new DynamicResistanceStatusDef.ResistancePerDamageType[]
            {
                new DynamicResistanceStatusDef.ResistancePerDamageType()
                {
                    DamageTypeBaseEffectDef = DefCache.GetDef<DamageTypeBaseEffectDef>("Projectile_StandardDamageTypeEffectDef"), //Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Projectile_StandardDamageTypeEffectDef")),
                    ResistanceStatusDef = skirmisherDamageModification
                },
                new DynamicResistanceStatusDef.ResistancePerDamageType()
                {
                    DamageTypeBaseEffectDef = DefCache.GetDef<DamageTypeBaseEffectDef>("Bash_StandardDamageTypeEffectDef"), //Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Bash_StandardDamageTypeEffectDef")),
                    ResistanceStatusDef = skirmisherDamageModification
                },
                new DynamicResistanceStatusDef.ResistancePerDamageType()
                {
                    DamageTypeBaseEffectDef = DefCache.GetDef < DamageTypeBaseEffectDef >("MeleeBash_StandardDamageTypeEffectDef"),
                    ResistanceStatusDef = skirmisherDamageModification
                },
                new DynamicResistanceStatusDef.ResistancePerDamageType()
                {
                    DamageTypeBaseEffectDef = DefCache.GetDef < DamageTypeBaseEffectDef >("Blast_StandardDamageTypeEffectDef"),
                    ResistanceStatusDef = skirmisherDamageModification
                }
            };
        }
        public static DynamicResistanceStatusDef skirmisherReactionStatus;
        // Harmony patch for Skirmisher where it checks if the ability should be monitored
        [HarmonyPatch(typeof(DynamicResistanceStatus), "IsAbilityMonitored")]
        internal static class Skirmisher_DynamicResistanceStatus_IsAbilityMonitored_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(DynamicResistanceStatus __instance, ref bool __result)
            {
                // If it is in the turn of the status holder (__instance.TacticalActor.DuringOwnTurn) then don't monitor this ability
                if (__instance.TacStatusDef == skirmisherReactionStatus && __instance.TacticalActor.DuringOwnTurn)
                {
                    __result = false;
                }
            }
        }

        /*// Harmony patch for Skirmisher in the moment an ability is activated
        [HarmonyPatch(typeof(DynamicResistanceStatus), "OnAbilityActivating")]
        internal static class Skirmisher_DynamicResistanceStatus_OnAbilityActivating_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(DynamicResistanceStatus __instance)
            {
                // Don't execute original method (return false) when current instance is called from Skirmisher ability
                // AND current turn is same turn of ability owner (e.g. self damaging in player turn, no cheese patch)
                return !(__instance.TacStatusDef == skirmisherReactionStatus
                    && __instance.TacticalActorBase.TacticalLevel.CurrentFaction == __instance.TacticalActorBase.TacticalFaction);
            }
        }
        // Harmony patch for Skirmisher in the moment damage is appilied
        [HarmonyPatch(typeof(DynamicResistanceStatus), "OnDamageApplied")]
        internal static class Skirmisher_DynamicResistanceStatus_OnDamageApplied_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(DynamicResistanceStatus __instance)
            {
                // Don't execute original method (return false) when current instance is called from Skirmisher ability
                // AND current turn is same turn of ability owner (e.g. self damaging in player turn, no cheese patch)
                return !(__instance.TacStatusDef == skirmisherReactionStatus
                    && __instance.TacticalActorBase.TacticalLevel.CurrentFaction == __instance.TacticalActorBase.TacticalFaction);
            }
        }*/

        private static void Create_ShredResistance()
        {
            string skillName = "ShredResistant_DamageMultiplierAbilityDef";
            DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("PoisonResistant_DamageMultiplierAbilityDef");
            DamageMultiplierAbilityDef shredRes = Helper.CreateDefFromClone(
                source,
                "da32f3c3-74d4-440c-9197-8fcccaf66da8",
                skillName);
            shredRes.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "18a2c7e2-9266-4f8f-acf9-8242c5b529c3",
                skillName);
            shredRes.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "487acaef-7908-436b-b458-b0a670382663",
                skillName);
            shredRes.DamageTypeDef = DefCache.GetDef<DamageTypeBaseEffectDef>("Shred_StandardDamageTypeEffectDef"); //Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Shred_StandardDamageTypeEffectDef"));
            shredRes.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SHRED_RESISTANCE"; // new LocalizedTextBind("SHRED RESISTANCE", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            shredRes.ViewElementDef.Description.LocalizationKey = "PR_BC_SHRED_RESISTANCE_DESC"; // new LocalizedTextBind("Shred Resistance", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            TacticalAbilityViewElementDef pr_ViewElement = (TacticalAbilityViewElementDef)Repo.GetDef("00431749-6f3f-d7e3-41a1-56e07706bd5a");
            if (pr_ViewElement != null)
            {
                shredRes.ViewElementDef.LargeIcon = pr_ViewElement.LargeIcon;
                shredRes.ViewElementDef.SmallIcon = pr_ViewElement.LargeIcon;
            }
        }
        // Harmony Patch to calcualte shred resistance, vanilla has no implementation for this
      /*  [HarmonyPatch(typeof(ShreddingDamageKeywordData), "ProcessKeywordDataInternal")]
        internal static class BC_ShreddingDamageKeywordData_ProcessKeywordDataInternal_ShredResistant_patch
        {
            //public static DamageMultiplierAbilityDef shredResistanceAbilityDef = DefCache.GetDef<DamageMultiplierAbilityDef>("ShredResistant_DamageMultiplierAbilityDef");
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                TacticalActorBase actor = data.Target.GetActor();
                DamageMultiplierAbilityDef shredResistanceAbilityDef = DefCache.GetDef<DamageMultiplierAbilityDef>("ShredResistant_DamageMultiplierAbilityDef");
                if (actor != null && actor.GetAbilityWithDef<DamageMultiplierAbility>(shredResistanceAbilityDef) != null)
                {
                    data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * shredResistanceAbilityDef.Multiplier);
                }
            }
        }*/ //added to TFTVRevenant.cs for revenants

        private static void Change_RageBurst()
        {
            RageBurstInConeAbilityDef rageBurst = DefCache.GetDef<RageBurstInConeAbilityDef>("RageBurst_RageBurstInConeAbilityDef");
            rageBurst.ProjectileSpreadMultiplier = 0.4f; // acc buff calculation: 1 / value - 100 = +acc%, 1 / 0.4 - 100 = +150%
            rageBurst.ConeSpread = 15.0f;
            rageBurst.ViewElementDef.Description.LocalizationKey = "PR_BC_RAGE_BURST_DESC"; // new LocalizedTextBind("Shoot 5 times across a wide arc with increased accuracy", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            rageBurst.DisablingStatuses = new StatusDef[]
            {
                DefCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef").StatusDef,
            };
        }
        private static void Create_JetpackControl()
        {
            string skillName = "JetpackControl_AbilityDef";
            float jetpackControlAPCost = 0.5f;
            float jetpackControlWPCost = 3f;
            float jetpackControlRange = 14f;
            JetJumpAbilityDef source = DefCache.GetDef<JetJumpAbilityDef>("JetJump_AbilityDef");
            // Change Jet Jump abilities restricted to 1 use per turn
            source.UsesPerTurn = 1;
            JetJumpAbilityDef jetpackControl = Helper.CreateDefFromClone(
                source,
                "ddbb58e8-9ea4-417c-bddb-8ed62837bb10",
                skillName);
            jetpackControl.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "f330ce45-361a-4444-bd69-04b3e6350a0e",
                skillName);
            jetpackControl.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "629a8d02-1dfe-48bb-9ae5-4ef8d789b5eb",
                skillName);
            jetpackControl.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "c97fda50-4e29-443d-a043-cf852fa0ec12",
                skillName);
            jetpackControl.CharacterProgressionData.RequiredStrength = 0;
            jetpackControl.CharacterProgressionData.RequiredWill = 0;
            jetpackControl.CharacterProgressionData.RequiredSpeed = 0;
            jetpackControl.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_JETPACK_CONTROL"; // new LocalizedTextBind("JETPACK CONTROL", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //string description = $"Jet jump to a location within {jetpackControlRange} tiles";
            jetpackControl.ViewElementDef.Description.LocalizationKey = "PR_BC_JETPACK_CONTROL_DESC"; // new LocalizedTextBind(description, TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite jetpackControlIcon = DefCache.GetDef<ClassProficiencyAbilityDef>("UseAttachedEquipment_AbilityDef").ViewElementDef.LargeIcon; //Repo.GetAllDefs<ClassProficiencyAbilityDef>().FirstOrDefault(cp => cp.name.Equals("UseAttachedEquipment_AbilityDef")).ViewElementDef.LargeIcon;
            jetpackControl.ViewElementDef.LargeIcon = jetpackControlIcon;
            jetpackControl.ViewElementDef.SmallIcon = jetpackControlIcon;
            jetpackControl.ActionPointCost = jetpackControlAPCost;
            jetpackControl.WillPointCost = jetpackControlWPCost;
            jetpackControl.AbilitiesRequired = new TacticalAbilityDef[] { source };
            jetpackControl.TargetingDataDef.Origin.Range = jetpackControlRange;

            // Animation related stuff
            TacCameraAbilityFilterDef tacCameraAbilityFilter1 = DefCache.GetDef<TacCameraAbilityFilterDef>("E_JetJumpAbilityFilter");
            FirstMatchExecutionDef cameraAbility1 = Helper.CreateDefFromClone(
                Repo.GetAllDefs<FirstMatchExecutionDef>().FirstOrDefault(fme => fme.FilterDef == tacCameraAbilityFilter1),
                "1f6b1ee0-0e11-4985-ab69-4266f40c9117",
                "E_JetpackControl_CameraAbility1 [NoDieCamerasTacticalCameraDirectorDef]");
            cameraAbility1.FilterDef = Helper.CreateDefFromClone(
                tacCameraAbilityFilter1,
                "ce4947e2-527f-4815-bdfc-8973d8ef7802",
                "E_JetpackControl_CameraAbilityFilter1 [NoDieCamerasTacticalCameraDirectorDef]");
            (cameraAbility1.FilterDef as TacCameraAbilityFilterDef).TacticalAbilityDef = jetpackControl;

            TacCameraAbilityFilterDef tacCameraAbilityFilter2 = DefCache.GetDef<TacCameraAbilityFilterDef>("E_JetJumpAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]");
            FirstMatchExecutionDef cameraAbility2 = Helper.CreateDefFromClone(
                Repo.GetAllDefs<FirstMatchExecutionDef>().FirstOrDefault(fme => fme.FilterDef == tacCameraAbilityFilter2),
                "34095bd0-ccf5-48cb-ba73-7689a5d45e7e",
                "E_JetpackControl_CameraAbility2 [NoDieCamerasTacticalCameraDirectorDef]");
            cameraAbility2.FilterDef = Helper.CreateDefFromClone(
                tacCameraAbilityFilter2,
                "5fa204df-5048-428f-b701-722ea9e15cc7",
                "E_JetpackControl_CameraAbilityFilter2 [NoDieCamerasTacticalCameraDirectorDef]");
            (cameraAbility2.FilterDef as TacCameraAbilityFilterDef).TacticalAbilityDef = jetpackControl;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(jetpackControl))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(jetpackControl).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
        private static void Change_BoomBlast()
        {
            float wpCost = 4.0f;
            bool setNewStats = false;
            ApplyStatusAbilityDef boomBlast = DefCache.GetDef<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
            boomBlast.ViewElementDef.Description.LocalizationKey = "PR_BC_BOOM_BLAST_DESC"; // new LocalizedTextBind($"Until end of turn your explosives gain +50% range, in addtion Rocket and Grenade Launchers cost - 1AP.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            boomBlast.WillPointCost = wpCost;

            // Convert additional statuses to a List for easier access
            List<TacStatusDef> bbAdditionalStatusesToApply = (boomBlast.StatusDef as AddAttackBoostStatusDef).AdditionalStatusesToApply.ToList();

            // Fix AP cost to only affect grenade launcher, incl set the right tag for them (not used and set in vanilla)
            GameTagDef glTag = DefCache.GetDef<GameTagDef>("GrenadeLauncherItem_TagDef");
            foreach (WeaponDef wd in Repo.GetAllDefs<WeaponDef>())
            {
                if ((wd.name.Equals("PX_GrenadeLauncher_WeaponDef")
                    || wd.name.Equals("AC_Rebuke_WeaponDef")
                    || wd.name.Equals("FS_AssaultGrenadeLauncher_WeaponDef")
                    || wd.name.Equals("NJ_HeavyRocketLauncher_WeaponDef"))
                    && !wd.Tags.Contains(glTag))
                {
                    wd.Tags.Add(glTag);
                }
            }
            ChangeAbilitiesCostStatusDef reduceApCostStatus = (ChangeAbilitiesCostStatusDef)bbAdditionalStatusesToApply.FirstOrDefault(a => a.name.Equals("E_ReduceExplosiveAbilitiesCost [BigBooms_AbilityDef]"));
            if (reduceApCostStatus != null && reduceApCostStatus.AbilityCostModification.EquipmentTagDef != glTag)
            {
                reduceApCostStatus.AbilityCostModification.EquipmentTagDef = glTag;
                //bbAdditionalStatusesToApply.Remove(reduceApCostStatus);
            }

            // Set new detailed stats if configured
            if (setNewStats)
            {
                float bbDamageMod = 0.33f;
                float bbRangeMod = 1.4f;
                float bbAccuracyMod = 0.5f;
                float bbProjectileMod = 7.0f;
                boomBlast.ViewElementDef.Description = new LocalizedTextBind($"Until end of turn your explosives get {(bbDamageMod * 100) - 100}% damage, {(bbRangeMod * 100) - 100}% range, {(bbAccuracyMod * 100) - 100}% accuracy. Launcher with multiple explosives per magazine gain +{bbProjectileMod} projectiles per shot.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

                StatMultiplierStatusDef bbAccModStatus = DefCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef");
                bbAccModStatus.EffectName = "";
                bbAccModStatus.ShowNotification = false;
                bbAccModStatus.VisibleOnHealthbar = 0;
                bbAccModStatus.VisibleOnStatusScreen = 0;
                bbAccModStatus.Visuals = null;
                bbAccModStatus.StatsMultipliers[0].StatName = "Accuracy";
                bbAccModStatus.StatsMultipliers[0].Multiplier = bbAccuracyMod;
                GameTagDef explosiveWeaponTag = DefCache.GetDef<GameTagDef>("ExplosiveWeapon_TagDef");
                EquipmentItemTagStatModification[] bbMods = new EquipmentItemTagStatModification[]
                {
                    new EquipmentItemTagStatModification()
                    {
                        ItemTag = explosiveWeaponTag,
                        EquipmentStatModification = new ItemStatModification()
                        {
                            TargetStat = StatModificationTarget.BonusAttackDamage,
                            Modification = StatModificationType.Multiply,
                            Value = bbDamageMod
                        }
                    },
                    new EquipmentItemTagStatModification()
                    {
                        ItemTag = explosiveWeaponTag,
                        EquipmentStatModification = new ItemStatModification()
                        {
                            TargetStat = StatModificationTarget.BonusAttackRange,
                            Modification = StatModificationType.Multiply,
                            Value = bbRangeMod
                        }
                    },
                    new EquipmentItemTagStatModification()
                    {
                        ItemTag = explosiveWeaponTag,
                        EquipmentStatModification = new ItemStatModification()
                        {
                            TargetStat = StatModificationTarget.BonusProjectilesPerShot,
                            Modification = StatModificationType.AddRestrictedToBounds,
                            Value = bbProjectileMod
                        }
                    }
                };
                bbAdditionalStatusesToApply.OfType<StanceStatusDef>().First().EquipmentsStatModifications = bbMods;
                if (!bbAdditionalStatusesToApply.Contains(bbAccModStatus))
                {
                    bbAdditionalStatusesToApply.Add(bbAccModStatus);
                }
            }

            // Convert changed list with additional statuses back to array
            (boomBlast.StatusDef as AddAttackBoostStatusDef).AdditionalStatusesToApply = bbAdditionalStatusesToApply.ToArray();
        }
    }
}
