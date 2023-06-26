using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class BerserkerSkills
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            // Dash: Move up to 13 tiles. Limited to 1 use per turn
            Change_Dash();

            // Ignore Pain: Remove MC immunity
            Change_IgnorePain();

            // Bloodlust: reduce buffs to max 25%
            Change_Bloodlust();

            // Adrenaline Rush: 1 AP for one handed weapons and skills, no WP restriction
            Change_AdrenalineRush();

            // Gun Kata 0AP 2WP Shoot your handgun for free. Limited to 2 uses per turn.
            Create_GunKata();

            // Exertion: 0AP 2WP Recover 1AP. Next turn you have -1 AP. Limited to 1 use per turn.
            Create_Exertion();

            // Killer Instinct: At the beginning of your turn, if there is an enemy within 5 tiles your next attack costs -2AP.
            Create_KillerInstinct();
        }

        private static void Change_Bloodlust()
        {
            float maxBoost = 0.25f;
            ViewElementDef blView = DefCache.GetDef<ViewElementDef>("E_ViewElement [BloodLust_AbilityDef]");
            BloodLustStatusDef blStatus = DefCache.GetDef<BloodLustStatusDef>("E_Status [BloodLust_AbilityDef]");
            blStatus.MaxBoost = maxBoost;
            blStatus.HealthLowBound = 0.5f;
            blView.Description.LocalizationKey = "PR_BC_BLOODLUST_DESC";
        }

        [HarmonyPatch(typeof(BloodLustStatus), "ApplyModification")]
        internal static class BloodLustStatus_ApplyModification_Patch
        {
            public static bool Prefix(BloodLustStatus __instance, StatusStat healthStat)
            {
                if (__instance.BloodLustStatusDef.HealthLowBound >= 1)
                {
                    return true;
                }
                float healthLowBound = healthStat.Max * __instance.BloodLustStatusDef.HealthLowBound;
                healthLowBound = Mathf.Clamp(healthLowBound, 1, healthStat.Max);
                float num = (healthStat.Value - healthLowBound) / (healthStat.Max - healthLowBound);
                num = Mathf.Clamp01(num);
                float num2 = 1f + (1f - num) * __instance.BloodLustStatusDef.MaxBoost;
                num2 = Mathf.Max(num2, 1f);
                foreach (StatModificationTarget targetStat in __instance.BloodLustStatusDef.StatModificationTargets)
                {
                    BaseStat baseStat = __instance.TacticalActor.CharacterStats.TryGetStat(targetStat);
                    baseStat.RemoveStatModificationsWithSource(__instance.BloodLustStatusDef, true);
                    if (baseStat is StatusStat)
                    {
                        baseStat.AddStatModification(new StatModification(StatModificationType.MultiplyMax, targetStat.ToString(), num2, __instance.BloodLustStatusDef, num2), true);
                    }
                    baseStat.AddStatModification(new StatModification(StatModificationType.MultiplyRestrictedToBounds, targetStat.ToString(), num2, __instance.BloodLustStatusDef, num2), true);
                    baseStat.ReapplyModifications();
                }
                return false;
            }
        }

        private static void Change_Dash()
        {
            //float dashRange = 13f;
            //int dashUsesPerTurn = 1;
            //string dashDescription = $"Move up to {(int)dashRange} tiles. Limited to {dashUsesPerTurn} use per turn";
            Sprite dashIcon = DefCache.GetDef<TacticalAbilityViewElementDef>("E_View [BodySlam_AbilityDef]").LargeIcon;
            //
            RepositionAbilityDef dash = DefCache.GetDef<RepositionAbilityDef>("Dash_AbilityDef");
            //dash.TargetingDataDef.Origin.Range = dashRange;
            //dash.ViewElementDef.Description = new LocalizedTextBind(dashDescription, doNotLocalize);
            dash.ViewElementDef.LargeIcon = dashIcon;
            dash.ViewElementDef.SmallIcon = dashIcon;
            //dash.UsesPerTurn = dashUsesPerTurn;
            //dash.AmountOfMovementToUseAsRange = -1.0f;
        }

        private static void Change_IgnorePain()
        {
            // Remove Ignore Pain from mind control application conditions
            MindControlStatusDef mcStatus = DefCache.GetDef<MindControlStatusDef>("MindControl_StatusDef");
            EffectConditionDef actorHasIgnorePain = DefCache.GetDef<EffectConditionDef>("NoIgnorePainStatus_ApplicationCondition");
            List<EffectConditionDef> mcApplicationConditions = mcStatus.ApplicationConditions.ToList();
            if (mcApplicationConditions.Remove(actorHasIgnorePain))
            {
                mcStatus.ApplicationConditions = mcApplicationConditions.ToArray();
            }
        }

        private static void Change_AdrenalineRush()
        {
            ApplyStatusAbilityDef adrenalineRush = DefCache.GetDef<ApplyStatusAbilityDef>("AdrenalineRush_AbilityDef");
            adrenalineRush.StatusDef = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_SetAbilitiesTo1AP [AdrenalineRush_AbilityDef]");
            adrenalineRush.ViewElementDef.Description.LocalizationKey = "PR_BC_ADRENALINE_DESC"; // new LocalizedTextBind("Until end of turn one-handed weapon and all non-weapon skills cost 1AP, except Recover.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        // Adrenaline Rush: Patching AbilityQualifies of ARs TacticalAbilityCostModification to determine which ability should get modified
        [HarmonyPatch(typeof(TacticalAbilityCostModification), "AbilityQualifies")]
        internal static class AR_AbilityQualifies_patch
        {
            internal static List<string> arExcludeList = new List<string>()
            {
                "RecoverWill_AbilityDef",
                "Overwatch_AbilityDef"
            };
            //internal static SkillTagDef attackAbility_Tag = DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef");
            //internal static ApplyStatusAbilityDef adrenalineRush = DefCache.GetDef<ApplyStatusAbilityDef>("AdrenalineRush_AbilityDef");
            //internal static ChangeAbilitiesCostStatusDef arStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_SetAbilitiesTo1AP [AdrenalineRush_AbilityDef]");

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(TacticalAbilityCostModification __instance, ref bool __result, TacticalAbility ability)
            {
                try
                {
                    ChangeAbilitiesCostStatusDef arStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_SetAbilitiesTo1AP [AdrenalineRush_AbilityDef]");
                    if (ability.TacticalActor.Status.HasStatus(arStatus) && __instance == arStatus.AbilityCostModification)
                    {
                        SkillTagDef attackAbility_Tag = DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef");
                        __result = ability.TacticalAbilityDef.SkillTags.Contains(attackAbility_Tag)
                            ? ability.Equipment == null || ability.Equipment.HandsToUse == 1 || ability.AbilityDef.name.Equals("ElectricTentacleAttack_AbilityDef")
                            : !arExcludeList.Contains(ability.TacticalAbilityDef.name);
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        private static void Create_GunKata()
        {
            int usesPerTurn = 2;
            float wpCost = 2.0f;
            float accMod = 1.0f;
            bool useFPC = true;
            string skillName = "GunKata_AbilityDef";
            //LocalizedTextBind name = new LocalizedTextBind("GUN KATA", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Shoot your handgun for free. Limited to 2 uses per turn.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            // Get some basics from repo
            ShootAbilityDef source = DefCache.GetDef<ShootAbilityDef>("Gunslinger_AbilityDef");
            GameTagDef handgunWeaponTag = DefCache.GetDef<GameTagDef>("HandgunItem_TagDef");

            ShootAbilityDef GunKata = Helper.CreateDefFromClone(
                source,
                "f7d997ce-1272-4337-a55e-97ecab56d58e",
                skillName);
            GunKata.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "fe360ad7-fd39-432b-97c9-8354f1823dbd",
                skillName);
            GunKata.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "cf65b3b6-ec33-48ab-a08f-71a3cb44567a",
                skillName);
            GunKata.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_GUN_KATA"; // name;
            GunKata.ViewElementDef.Description.LocalizationKey = "PR_BC_GUN_KATA_DESC"; // description;
            GunKata.EquipmentTags = new GameTagDef[] { handgunWeaponTag };
            GunKata.UsesPerTurn = usesPerTurn;
            GunKata.WillPointCost = wpCost;
            GunKata.CanUseFirstPersonCam = useFPC;
            GunKata.ProjectileSpreadMultiplier = accMod;
        }

        private static void Create_Exertion()
        {
            int usesPerTurn = 1;
            float wpCost = 5.0f;
            float apMod = 50.0f;
            string skillName = "Exertion_AbilityDef";
            //LocalizedTextBind name = new LocalizedTextBind("EXERTION", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Recover 1AP. Limited to 1 use per turn.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            // Get some basics from repo
            ExtraMoveAbilityDef source = DefCache.GetDef<ExtraMoveAbilityDef>("ExtraMove_AbilityDef");
            //ExtraMoveAbilityDef Exertion = Repo.GetAllDefs<ExtraMoveAbilityDef>().FirstOrDefault(asa => asa.name.Equals("ExtraMove_AbilityDef"));

            ExtraMoveAbilityDef Exertion = Helper.CreateDefFromClone(
                source,
                "790233f5-5aa5-4769-931c-c2f740271836",
                skillName);
            Exertion.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "f0bdbe30-2947-49f6-a1e7-276c6245861b",
                skillName);
            Exertion.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "d20f2149-a24b-4419-8a7f-b86bb7837a4d",
                skillName);
            Exertion.UsesPerTurn = usesPerTurn;
            Exertion.WillPointCost = wpCost;
            Exertion.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_EXERTION"; // name;
            Exertion.ViewElementDef.Description.LocalizationKey = "PR_BC_EXERTION_DESC"; // = description;
            Exertion.ActionPointsReturnedPerc = apMod;

            //AbilityDef animSource = Repo.GetAllDefs<AbilityDef>().FirstOrDefault(ad => ad.name.Equals("Priest_InstilFrenzy_AbilityDef"));
            //foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            //{
            //    if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(Exertion))
            //    {
            //        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(Exertion).ToArray();
            //        PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
            //        foreach (AbilityDef ad in animActionDef.AbilityDefs)
            //        {
            //            PRMLogger.Debug("  " + ad.name);
            //        }
            //        PRMLogger.Debug("----------------------------------------------------", false);
            //    }
            //}
        }

        private static void Create_KillerInstinct()
        {
            string skillname = "KillerInstinct_AbiltyDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
            ApplyStatusAbilityDef killerInstinct = Helper.CreateDefFromClone(
                source,
                "ADEAFE3F-7592-43A2-9C57-EE2BBA4932D8",
                skillname);

            killerInstinct.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "C1527003-4FC9-4167-B6BC-BD35CEF4E9D1",
                skillname);

            killerInstinct.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "65B21ADD-A05E-463B-8A66-C0A8377C5FE6",
                skillname);
            killerInstinct.TargetingDataDef.Origin.Range = 5f;

            killerInstinct.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "9BDBE22B-624F-472F-8002-27BBD5657700",
                skillname);
            Sprite icon = Helper.CreateSpriteFromImageFile("meat-cleaver.png");
            killerInstinct.ViewElementDef.LargeIcon = icon;
            killerInstinct.ViewElementDef.SmallIcon = icon;
            killerInstinct.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_KILLER_INSTINCT"; // name;
            killerInstinct.ViewElementDef.Description.LocalizationKey = "PR_BC_KILLER_INSTINCT_DESC"; // description;

            VisibleActorsInRangeEffectConditionDef visibleActorsInRange = Helper.CreateDefFromClone(
                DefCache.GetDef<VisibleActorsInRangeEffectConditionDef>("E_VisibleActorsInRange [MasterMarksman_AbilityDef]"),
                "621C3DBA-1DC6-400B-B6C1-B3FB8C648241",
                skillname);
            visibleActorsInRange.TargetingData = killerInstinct.TargetingDataDef;
            visibleActorsInRange.ActorsInRange = true;

            ChangeAbilitiesCostStatusDef changeAbilitiesCostStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_AbilityCostModifier [QuickAim_AbilityDef]"),
                "47272759-6EAE-4F13-B399-2FD582FD01AF",
                skillname);
            changeAbilitiesCostStatus.ApplicationConditions = new EffectConditionDef[] { visibleActorsInRange };
            changeAbilitiesCostStatus.AbilityCostModification.SkillTagCullFilter = new SkillTagDef[0];
            changeAbilitiesCostStatus.AbilityCostModification.ActionPointMod = -0.5f;

            AddAttackBoostStatusDef addAttackBoostStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<AddAttackBoostStatusDef>("E_Status [QuickAim_AbilityDef]"),
                "6B341F7B-A12B-4833-9F85-AC2AA66AF288",
                skillname);
            addAttackBoostStatus.ApplicationConditions = new EffectConditionDef[] { visibleActorsInRange };
            addAttackBoostStatus.ShowNotification = true;
            addAttackBoostStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            addAttackBoostStatus.Visuals = Helper.CreateDefFromClone(
                killerInstinct.ViewElementDef,
                "9FBE01B5-E8B0-4FDC-93FE-41DA4283888C",
                "E_AbilityCostModifier_Visuals [KillerInstinct_AbiltyDef]");
            addAttackBoostStatus.Visuals.Color = Color.red;
            addAttackBoostStatus.Visuals.DisplayName1.LocalizationKey = "PR_BC_KILLER_INSTINCT_STATUS";
            addAttackBoostStatus.Visuals.Description.LocalizationKey = "PR_BC_KILLER_INSTINCT_STATUS_DESC";
            addAttackBoostStatus.SkillTagCullFilter = new SkillTagDef[0];
            addAttackBoostStatus.AdditionalStatusesToApply = new TacStatusDef[] { changeAbilitiesCostStatus };

            killerInstinct.StatusDef = addAttackBoostStatus;

            killerInstinct.StatusApplicationTrigger = StatusApplicationTrigger.StartTurn;
        }
    }
}
