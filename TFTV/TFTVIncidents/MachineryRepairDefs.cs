using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using TFTV.TFTVBaseRework;

namespace TFTV.TFTVIncidents
{
    internal static class MachineryRepairDefs
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly string[] RankAbilityGuids =
        {
            "dbf8f1a1-69d0-4b0a-b2af-8f7f9f6ea101",
            "dbf8f1a1-69d0-4b0a-b2af-8f7f9f6ea102",
            "dbf8f1a1-69d0-4b0a-b2af-8f7f9f6ea103"
        };

        private static readonly string[] RankViewGuids =
        {
            "4ac4a4b9-4f78-4d4f-8d72-b5f91af1d201",
            "4ac4a4b9-4f78-4d4f-8d72-b5f91af1d202",
            "4ac4a4b9-4f78-4d4f-8d72-b5f91af1d203"
        };

        private static readonly string[] RankProgressionGuids =
        {
            "af8d6bd5-0f76-4d7d-b83c-91e36e44a301",
            "af8d6bd5-0f76-4d7d-b83c-91e36e44a302",
            "af8d6bd5-0f76-4d7d-b83c-91e36e44a303"
        };

        private static readonly string[] RankTargetingGuids =
        {
            "c4a2c1e1-e2f8-4cb1-a8d2-9b7cb4a1e401",
            "c4a2c1e1-e2f8-4cb1-a8d2-9b7cb4a1e402",
            "c4a2c1e1-e2f8-4cb1-a8d2-9b7cb4a1e403"
        };

        private static readonly string[] RankMultiStatusGuids =
        {
            "3fb1f078-2088-4708-bd08-22f8a919b501",
            "3fb1f078-2088-4708-bd08-22f8a919b502",
            "3fb1f078-2088-4708-bd08-22f8a919b503"
        };

        private static readonly string[] RankVulnerabilityStatusGuids =
        {
            "f1ad81c2-f9f3-4af7-b6c6-5e5d8fe0c601",
            "f1ad81c2-f9f3-4af7-b6c6-5e5d8fe0c602",
            "f1ad81c2-f9f3-4af7-b6c6-5e5d8fe0c603"
        };

        private static readonly string[] RankVulnerabilityViewGuids =
        {
            "e31624a5-7c84-4cb0-9a84-0f9d1d0fb701",
            "e31624a5-7c84-4cb0-9a84-0f9d1d0fb702",
            "e31624a5-7c84-4cb0-9a84-0f9d1d0fb703"
        };

        private static readonly string[][] RankRestoreStatusGuids =
        {
            new[]
            {
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d801"
            },
            new[]
            {
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d811",
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d812"
            },
            new[]
            {
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d821",
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d822",
                "c0e3f6fe-a86d-4f23-8bc2-01f7f520d823"
            }
        };

        private const string TargetConditionGuid = "80a27cb6-6447-4f95-9444-57e017b5cc01";

        private const string MachineryAbilityNameKey = "TFTV_KEY_MACHINERY_OVERDRIVE_NAME";
        private static readonly string[] MachineryOption1DescKeys =
        {
            "TFTV_KEY_MACHINERY_OVERDRIVE_DESC_R1",
            "TFTV_KEY_MACHINERY_OVERDRIVE_DESC_R2",
            "TFTV_KEY_MACHINERY_OVERDRIVE_DESC_R3"
        };
        private const string MachineryOption2DescKey = "TFTV_KEY_MACHINERY_REPAIR_OPTION2_DESC";
        private const string MachineryVulnerabilityNameKey = "TFTV_KEY_MACHINERY_OVERDRIVE_VULNERABILITY_NAME";
        private const string MachineryVulnerabilityDescKey = "TFTV_KEY_MACHINERY_OVERDRIVE_VULNERABILITY_DESC";

        internal static ApplyStatusAbilityDef[] RankDefs = new ApplyStatusAbilityDef[3];

        internal static void CreateDefs()
        {
            try
            {
                ApplyStatusAbilityDef sourceAbility = DefCache.GetDef<ApplyStatusAbilityDef>("DeterminedAdvance_AbilityDef");
                TacEffectStatusDef sourceRestoreStatus = sourceAbility.StatusDef as TacEffectStatusDef;
                MultiStatusDef sourceMultiStatus = DefCache.GetDef<MultiStatusDef>("E_MultiStatus [RapidClearance_AbilityDef]");
                DamageMultiplierStatusDef vulnerabilitySource = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                ActorHasBodypartTagEffectConditionDef targetCondition = CreateTargetCondition();

                for (int i = 0; i < 3; i++)
                {
                    int rank = i + 1;
                    string defName = $"MachineryOverdrive_Rank{rank}_AbilityDef";

                    ApplyStatusAbilityDef cloned = Helper.CreateDefFromClone(sourceAbility, RankAbilityGuids[i], defName);

                    cloned.ViewElementDef = Helper.CreateDefFromClone(
                        sourceAbility.ViewElementDef,
                        RankViewGuids[i],
                        $"{defName}_View");

                    cloned.CharacterProgressionData = Helper.CreateDefFromClone(
                        sourceAbility.CharacterProgressionData,
                        RankProgressionGuids[i],
                        $"{defName}_Progression");

                    cloned.TargetingDataDef = Helper.CreateDefFromClone(
                        sourceAbility.TargetingDataDef,
                        RankTargetingGuids[i],
                        $"{defName}_Targeting");

                    cloned.TargetingDataDef.Origin.TargetTags.Clear();

                    cloned.TargetApplicationConditions = new EffectConditionDef[]
                    {
                        targetCondition
                    };

                    cloned.StatusDef = CreateMultiStatusForRank(rank, sourceMultiStatus, sourceRestoreStatus, vulnerabilitySource, cloned.ViewElementDef);
                    cloned.StatusApplicationTrigger = StatusApplicationTrigger.ActivateAbility;
                    cloned.ActionPointCost = 0.25f;
                    cloned.WillPointCost = 3f;
                    cloned.UsesPerTurn = 1;
                    cloned.AnimType = -1;

                    UpdateViewElement(cloned, rank, true);

                    RankDefs[i] = cloned;
                }

                ApplyChoiceFromSnapshot();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static ActorHasBodypartTagEffectConditionDef CreateTargetCondition()
        {
            ActorHasBodypartTagEffectConditionDef condition = Helper.CreateDefFromClone<ActorHasBodypartTagEffectConditionDef>(
                null,
                TargetConditionGuid,
                "MachineryOverdrive_TargetConditionDef");
            
            condition.VehicleTag = Shared.SharedGameTags.VehicleTag;
            condition.RequiredBodyPartTag = Shared.SharedGameTags.BionicalTag;
            condition.RequiredCount = 1;

            return condition;
        }

        private static MultiStatusDef CreateMultiStatusForRank(
            int rank,
            MultiStatusDef sourceMultiStatus,
            TacEffectStatusDef sourceRestoreStatus,
            DamageMultiplierStatusDef vulnerabilitySource,
            TacticalAbilityViewElementDef abilityView)
        {
            MultiStatusDef multiStatus = Helper.CreateDefFromClone(
                sourceMultiStatus,
                RankMultiStatusGuids[rank - 1],
                $"MachineryOverdrive_Rank{rank}_MultiStatusDef");

            List<StatusDef> statuses = new List<StatusDef>();

            for (int i = 0; i < rank; i++)
            {
                statuses.Add(CreateRestoreStatus(rank, i, sourceRestoreStatus));
            }

            statuses.Add(CreateVulnerabilityStatus(rank, vulnerabilitySource, abilityView));

            multiStatus.Statuses = statuses.ToArray();
            return multiStatus;
        }

        private static TacEffectStatusDef CreateRestoreStatus(int rank, int index, TacEffectStatusDef sourceRestoreStatus)
        {
            TacEffectStatusDef restoreStatus = Helper.CreateDefFromClone(
                sourceRestoreStatus,
                RankRestoreStatusGuids[rank - 1][index],
                $"MachineryOverdrive_Rank{rank}_Restore_{index + 1}_StatusDef");

            restoreStatus.EffectName = $"MachineryOverdrive_Rank{rank}_Restore_{index + 1}";
            restoreStatus.DurationTurns = 0;
            restoreStatus.ExpireOnEndOfTurn = false;
            restoreStatus.SingleInstance = true;
            restoreStatus.VisibleOnPassiveBar = false;
            restoreStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
          //  restoreStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.;
            restoreStatus.ApplyOnStatusApplication = true;
            restoreStatus.ApplyOnTurnStart = false;

            return restoreStatus;
        }

        private static DamageMultiplierStatusDef CreateVulnerabilityStatus(
            int rank,
            DamageMultiplierStatusDef source,
            TacticalAbilityViewElementDef abilityView)
        {
            string statusName = $"MachineryOverdrive_Rank{rank}_Vulnerability_StatusDef";

            DamageMultiplierStatusDef vulnerabilityStatus = Helper.CreateDefFromClone(
                source,
                RankVulnerabilityStatusGuids[rank - 1],
                statusName);

            vulnerabilityStatus.Visuals = Helper.CreateDefFromClone(
                abilityView,
                RankVulnerabilityViewGuids[rank - 1],
                $"{statusName}_View");

            vulnerabilityStatus.EffectName = $"MachineryOverdrive_Rank{rank}_Vulnerability";
            vulnerabilityStatus.Multiplier = 1.5f;
            vulnerabilityStatus.DurationTurns = 1;
            vulnerabilityStatus.ExpireOnEndOfTurn = true;
            vulnerabilityStatus.SingleInstance = true;
            vulnerabilityStatus.VisibleOnPassiveBar = true;
            vulnerabilityStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            vulnerabilityStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
            vulnerabilityStatus.Visuals.DisplayName1.LocalizationKey = MachineryVulnerabilityNameKey;
            vulnerabilityStatus.Visuals.Description.LocalizationKey = MachineryVulnerabilityDescKey;

            StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
            StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
            AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

            List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
            damageTypeBaseEffectDefs.AddRange(vulnerabilityStatus.DamageTypeDefs);
            damageTypeBaseEffectDefs.Add(fireDamage);
            damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
            damageTypeBaseEffectDefs.Add(acidDamage);
            damageTypeBaseEffectDefs.Add(TFTVMeleeDamage.MeleeStandardDamageType);


            vulnerabilityStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();


            return vulnerabilityStatus;
        }

        internal static void ApplyChoice(int option)
        {
            try
            {
                bool optionOne = option == 1;

                for (int i = 0; i < RankDefs.Length; i++)
                {
                    ApplyStatusAbilityDef rankDef = RankDefs[i];
                    if (rankDef == null)
                    {
                        continue;
                    }

                    UpdateViewElement(rankDef, i + 1, optionOne);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void UpdateViewElement(ApplyStatusAbilityDef rankDef, int rank, bool optionOne)
        {
            if (rankDef == null || rankDef.ViewElementDef == null)
            {
                return;
            }

            rankDef.ViewElementDef.DisplayName1.LocalizationKey = MachineryAbilityNameKey;
            rankDef.ViewElementDef.Description.LocalizationKey = optionOne
                ? MachineryOption1DescKeys[rank - 1]
                : MachineryOption2DescKey;
        }

        internal static void ApplyChoiceFromSnapshot()
        {
            try
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                int option = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(
                    LeaderSelection.AffinityApproach.Machinery);

                ApplyChoice(option);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void GrantRankAbilitiesOnMissionStart(TacticalLevelController level)
        {
            try
            {
                if (level == null)
                {
                    return;
                }

                ApplyChoiceFromSnapshot();

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(
                    LeaderSelection.AffinityApproach.Machinery);

                TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                if (phoenixFaction == null || phoenixFaction.Actors == null)
                {
                    return;
                }

                foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                {
                    TacticalActor actor = actorBase as TacticalActor;
                    if (actor == null || !actor.IsAlive || actor.IsEvacuated)
                    {
                        continue;
                    }

                    int machineryRank = GetMachineryRank(actor);
                    ApplyStatusAbilityDef desiredDef = selectedOption == 1 && machineryRank > 0 && machineryRank <= RankDefs.Length
                        ? RankDefs[machineryRank - 1]
                        : null;

                    for (int i = 0; i < RankDefs.Length; i++)
                    {
                        ApplyStatusAbilityDef rankDef = RankDefs[i];
                        if (rankDef == null || rankDef == desiredDef)
                        {
                            continue;
                        }

                        if (actor.GetAbilityWithDef<TacticalAbility>(rankDef) != null)
                        {
                            actor.RemoveAbility(rankDef);
                        }
                    }

                    if (desiredDef != null && actor.GetAbilityWithDef<TacticalAbility>(desiredDef) == null)
                    {
                        actor.AddAbility(desiredDef, actor);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static int GetMachineryRank(TacticalActor actor)
        {
            if (actor == null || Affinities.Machinery == null || Affinities.Machinery.Length < 3)
            {
                return 0;
            }

            for (int i = Affinities.Machinery.Length - 1; i >= 0; i--)
            {
                PassiveModifierAbilityDef def = Affinities.Machinery[i];
                if (def != null && actor.GetAbilityWithDef<PassiveModifierAbility>(def) != null)
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }
}