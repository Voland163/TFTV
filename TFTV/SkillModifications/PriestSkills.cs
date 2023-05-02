using Base.Cameras.ExecutionNodes;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Levels;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Cameras.Filters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PRMBetterClasses.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class PriestSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            // Fix balance issues with psychic abilities
            FixPsychicAbilitiesIssues();

            // Psychic Ward - fix and description to : Allies within 10 tiles are immune to panic and psychic scream damage
            Change_PsychicWard();

            // Biochemist: Paralysis, Poison and Viral damage increased 25%
            Create_BC_Biochemist();

            // Lay Waste: 1 AP, 3 WP, If your current Willpower score is higher than target's deal 30 damage for each point of WP difference
            Create_LayWaste();
        }

        private static void FixPsychicAbilitiesIssues()
        {
            string[] psychicAbilityDefNames = {
                "Priest_MindControl_AbilityDef",
                "Exalted_MindControl_AbilityDef",
                "InducePanic_AbilityDef",
                "Exalted_InducePanic_AbilityDef",
                "Priest_PsychicScream_AbilityDef",
                "Siren_PsychicScream_AbilityDef",
                "MindCrush_AbilityDef",
                "Exalted_MindCrush_AbilityDef"
            };
            SkillTagDef attackSkillTag = DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef");
            GameTagDef metallicSubstanceTag = DefCache.GetDef<GameTagDef>("Metallic_SubstanceTypeTagDef");
            foreach (string defName in psychicAbilityDefNames)
            {
                TacticalAbilityDef abilityDef = DefCache.GetDef<TacticalAbilityDef>(defName);
                if (!abilityDef.SkillTags.Contains(attackSkillTag))
                {
                    abilityDef.SkillTags = abilityDef.SkillTags.AddItem(attackSkillTag).ToArray();
                }
                if (abilityDef.TargetingDataDef.Origin.TargetTags.Contains(metallicSubstanceTag))
                {
                    abilityDef.TargetingDataDef.Origin.TargetTags.Remove(metallicSubstanceTag);
                }
            }
        }

        public static void Change_PsychicWard()
        {
            ApplyStatusAbilityDef psychicWard = DefCache.GetDef<ApplyStatusAbilityDef>("PsychicWard_AbilityDef");
            psychicWard.ViewElementDef.Description.LocalizationKey = "PR_BC_PSYCHIC_WARD_DESC";
        }

        private static void Create_BC_Biochemist()
        {
            float damageMod = 0.25f;
            string skillName = "BC_Biochemist_AbilityDef";

            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BodypartDamageMultiplier_AbilityDef");
            Sprite icon = DefCache.GetDef<TacticalAbilityDef>("BioChemist_AbilityDef").ViewElementDef.LargeIcon;

            ApplyStatusAbilityDef Biochemist = Helper.CreateDefFromClone(
                source,
                "87d0f9a4-0d26-4c2a-badb-cef90ae746a5",
                skillName);
            Biochemist.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "1845e667-c394-4248-bf21-10fa661aeb6f",
                skillName);
            Biochemist.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "683abf9c-46ea-4406-a943-ae3864fd1ce4",
                skillName);
            Biochemist.StatusDef = Helper.CreateDefFromClone<AddDependentDamageKeywordsStatusDef>(
                null,
                "1bfd9c34-b5c5-4c6e-abaf-aefccaea2a3c",
                $"E_Status [{skillName}]");

            Biochemist.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_BIOCHEMIST";
            Biochemist.ViewElementDef.Description.LocalizationKey = "PR_BC_BIOCHEMIST_DESC";
            Biochemist.ViewElementDef.LargeIcon = icon;
            Biochemist.ViewElementDef.SmallIcon = icon;

            AddDependentDamageKeywordsStatusDef statusDef = (AddDependentDamageKeywordsStatusDef)Biochemist.StatusDef;
            statusDef.EffectName = "BC_Biochemist";
            statusDef.ApplicationConditions = new EffectConditionDef[0];
            statusDef.DurationTurns = -1;
            statusDef.DisablesActor = false;
            statusDef.SingleInstance = true;
            statusDef.ShowNotification = true;
            statusDef.VisibleOnPassiveBar = false;
            statusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            statusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList | TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList;
            statusDef.HealthbarPriority = 0;
            statusDef.StackMultipleStatusesAsSingleIcon = false;
            statusDef.Visuals = Biochemist.ViewElementDef;
            statusDef.ParticleEffectPrefab = null;
            statusDef.DontRaiseOnApplyOnLoad = false;
            statusDef.EventOnApply = null;
            statusDef.EventOnUnapply = null;
            statusDef.DamageKeywordDefs = new DamageKeywordDef[]
            {
                SkillModsMain.sharedSoloDamageKeywords.SoloViralKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloAcidKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloPoisonousKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloParalysingKeyword
            };
            statusDef.BonusDamagePerc = damageMod;
        }

        internal static float wpCost = 5.0f;
        private static void Create_LayWaste()
        {
            float apCost = 0.25f;
            float baseDamage = 10;
            float range = 25.0f;

            string skillName = "LayWaste_AbilityDef";
            Sprite icon = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Mutoid_PoisonExplosion_ApplyStatusAbilityDef]").LargeIcon;

            ApplyEffectAbilityDef source = DefCache.GetDef<ApplyEffectAbilityDef>("MindCrush_AbilityDef");

            ApplyEffectAbilityDef LayWaste = Helper.CreateDefFromClone(
                source,
                "887e8c79-21d1-460f-b528-b6377ab6baf5",
                skillName);
            LayWaste.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "dc252b8d-2fca-46b8-bd31-68e125e7d0ef",
                skillName);
            LayWaste.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "787d2f98-69d4-4e87-90a6-ad8d68395ce0",
                skillName);
            LayWaste.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "d8fc6992-fd47-474a-a071-150162b8aebb",
                skillName);
            LayWaste.EffectDef = Helper.CreateDefFromClone(
                source.EffectDef,
                "1160e6e7-8e03-4823-a986-58f0130f21a6",
                skillName);

            LayWaste.TargetingDataDef.Origin.LineOfSight = LineOfSightType.InSight;
            LayWaste.TargetingDataDef.Origin.FactionVisibility = LineOfSightType.InSight;
            LayWaste.TargetingDataDef.Origin.CanPeekFromEdge = true;
            LayWaste.TargetingDataDef.Origin.Range = range;
            LayWaste.TargetingDataDef.Target.TargetEnemies = true;
            LayWaste.TargetingDataDef.Target.TargetResult = TargetResult.Actor;
            LayWaste.TargetingDataDef.Target.TargetTags = new GameTagsList
            {
                DefCache.GetDef < SubstanceTypeTagDef >("Organic_SubstanceTypeTagDef"),
            };

            LayWaste.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_LAY_WASTE";
            LayWaste.ViewElementDef.Description.LocalizationKey = "PR_BC_LAY_WASTE_DESC";
            LayWaste.ViewElementDef.LargeIcon = icon;
            LayWaste.ViewElementDef.SmallIcon = icon;

            LayWaste.TrackWithCamera = true;
            LayWaste.ShownModeToTrack = KnownState.Hidden;
            LayWaste.ActionPointCost = apCost;
            LayWaste.WillPointCost = wpCost;
            LayWaste.ApplyToAllTargets = false;
            LayWaste.MultipleTargetSimulation = false;

            DamageEffectDef damageEffect = LayWaste.EffectDef as DamageEffectDef;
            damageEffect.MinimumDamage = baseDamage;
            damageEffect.MaximumDamage = baseDamage;

            // Animation related stuff
            FirstMatchExecutionDef cameraAbility = Helper.CreateDefFromClone(
                DefCache.GetDef<FirstMatchExecutionDef>("E_MindControlAbility [NoDieCamerasTacticalCameraDirectorDef]"),
                "51f0a0d0-7cab-4a6a-b511-51ba91e699fd",
                "E_LayWaste_CameraAbility [NoDieCamerasTacticalCameraDirectorDef]");
            cameraAbility.FilterDef = Helper.CreateDefFromClone(
                DefCache.GetDef<TacCameraAbilityFilterDef>("E_MindControlFilter [NoDieCamerasTacticalCameraDirectorDef]"),
                "77f7e07a-a0b2-40b1-90c7-b8e86b70a5fd",
                "E_LayWaste_CameraAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]");
            (cameraAbility.FilterDef as TacCameraAbilityFilterDef).TacticalAbilityDef = LayWaste;

            TacticalAbilityDef animSource = DefCache.GetDef<TacticalAbilityDef>("Priest_MindControl_AbilityDef");
            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(LayWaste))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(LayWaste).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
        // Harmony patch for LayWaste to inject check against willpower
        [HarmonyPatch(typeof(ApplyEffectAbility), "TargetFilterPredicate")]
        internal static class LayWaste_ApplyEffectAbility_TargetFilterPredicate_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ApplyEffectAbility __instance, ref bool __result, TacticalActorBase targetActor)
            {
                try
                {
                    if (__instance.AbilityDef.name.Equals("LayWaste_AbilityDef") && __result && targetActor != null)
                    {
                        PRMLogger.Debug("----------------------------------------------------", false);
                        PRMLogger.Debug($"POSTFIX ApplyEffectAbility.TargetFilterPredicate(..) with 'LayWaste_AbilityDef' detected and __result is true ...");
                        BaseStat targetWP = targetActor.Status.GetStat(StatModificationTarget.WillPoints.ToString(), null);
                        StatusStat sourceWP = __instance.TacticalActor.CharacterStats.WillPoints; //((TacticalActor)AccessTools.Property(typeof(TacticalAbility), "TacticalActor").GetValue(__instance, null)).CharacterStats.WillPoints;
                        __result = targetWP != null && Utl.LesserThan(targetWP, sourceWP, 1E-05f);
                        PRMLogger.Debug($"Target actor WP {targetWP} vs source WP {sourceWP}, result after WP check: {__result}");
                        PRMLogger.Debug("----------------------------------------------------", false);
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }
        // Harmony patch for LayWaste to inject the calculation for damage by WP difference
        [HarmonyPatch(typeof(DamageEffect), "OnApply")]
        internal static class LayWaste_DamageEffect_OnApply_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(DamageEffect __instance, EffectTarget target)
            {
                try
                {
                    if (__instance.EffectDef.name.Equals("E_Effect [LayWaste_AbilityDef]"))
                    {
                        PRMLogger.Debug("----------------------------------------------------", false);
                        PRMLogger.Debug($"POSTFIX DamageEffect.OnApply(..) from '{__instance.EffectDef.name}' detected with {target} as target");

                        int damagePerWPDif = 20;
                        int maxDamage = 200;

                        object base_Source = AccessTools.Property(typeof(Effect), "Source").GetValue(__instance, null);
                        object source = base_Source ?? __instance;
                        IDamageReceiver damageReceiver = DamageEffect.GetDamageReceiver(target);
                        DamageEffect.Params param = target.GetParam<DamageEffect.Params>();
                        DamageAccumulation damageAccumulation = param?.DamageAccum;
                        if (damageAccumulation == null)
                        {
                            damageAccumulation = new DamageAccumulation(__instance.DamageEffectDef, source, (param?.DamageTypeDef) ?? __instance.DamageEffectDef.DamageTypeDef);
                            if (param != null)
                            {
                                param.DamageAccum = damageAccumulation;
                            }
                        }
                        if (damageReceiver != null)
                        {
                            TacticalActorBase targetActor = damageReceiver.GetActor();
                            TacticalActorBase sourceActor = TacUtil.GetSourceTacticalActorBase(source);
                            float targetWP = targetActor.Status.GetStat(StatModificationTarget.WillPoints.ToString(), null).Value;
                            float sourceWP = __instance.IsSimulation(target)
                                ? sourceActor.Status.GetStat(StatModificationTarget.WillPoints.ToString(), null).Value
                                : sourceActor.Status.GetStat(StatModificationTarget.WillPoints.ToString(), null).Value + wpCost;
                            int damage = (int)(sourceWP - targetWP) * damagePerWPDif > maxDamage ? maxDamage : (int)(sourceWP - targetWP) * damagePerWPDif;
                            damageAccumulation.InitialAmount = damage;
                            damageAccumulation.Amount = damage;

                            PRMLogger.Debug($"  Target Actor: {targetActor} with {targetWP} WP");
                            PRMLogger.Debug($"  Source Actor: {sourceActor} with {sourceWP} WP");
                            PRMLogger.Debug($"  Damage multiplicator: {(int)(sourceWP - targetWP) * damagePerWPDif}");
                            PRMLogger.Debug($"  DamageAccum initial amount: {damageAccumulation.InitialAmount}");
                            PRMLogger.Debug($"                      amount: {damageAccumulation.Amount}");
                            PRMLogger.Debug($"  Is simulation: {__instance.IsSimulation(target)}");

                            Vector3 damageOrigin = (param != null) ? param.DamageOrigin : Vector3.zero;
                            Vector3 impactForce = (param != null) ? param.ImpactForce : Vector3.zero;
                            CastHit impactHit = (param != null) ? param.ImpactHit : CastHit.Empty;
                            __instance.AddTarget(target, damageAccumulation, damageReceiver, damageOrigin, impactForce, impactHit);
                        }
                        else
                        {
                            damageAccumulation.AddBlocker();
                        }
                        if (param == null || param.ApplyImmediately)
                        {
                            damageAccumulation.ApplyAddedDamage();
                        }

                        PRMLogger.Debug("----------------------------------------------------", false);
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
