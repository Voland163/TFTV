using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Linq;
using TFTV.TFTVBaseRework;

namespace TFTV.TFTVIncidents
{
    internal static class MachineryRepairDefs
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly string[] RankAbilityGuids =
        {
            "6fdb65b8-8d75-489c-8b28-4d2d8f4d8d11",
            "7fdb65b8-8d75-489c-8b28-4d2d8f4d8d12",
            "8fdb65b8-8d75-489c-8b28-4d2d8f4d8d13"
        };

        private static readonly string[] RankViewGuids =
        {
            "1f5f3e22-c3a2-4c2e-b57a-a5164a77a001",
            "2f5f3e22-c3a2-4c2e-b57a-a5164a77a002",
            "3f5f3e22-c3a2-4c2e-b57a-a5164a77a003"
        };

        private static readonly string[] RankProgressionGuids =
        {
            "ad6ab7b5-694b-4f9e-a0c4-6d3ee0ed4001",
            "bd6ab7b5-694b-4f9e-a0c4-6d3ee0ed4002",
            "cd6ab7b5-694b-4f9e-a0c4-6d3ee0ed4003"
        };

        private static readonly string[] RankTargetingGuids =
        {
            "4e5f3e22-c3a2-4c2e-b57a-a5164a77a001",
            "5e5f3e22-c3a2-4c2e-b57a-a5164a77a002",
            "6e5f3e22-c3a2-4c2e-b57a-a5164a77a003"
        };

        private const string MachineryAbilityNameKey = "TFTV_KEY_MACHINERY_REPAIR_NAME";
        private const string MachineryOption1DescKey = "TFTV_KEY_MACHINERY_REPAIR_OPTION1_DESC";
        private const string MachineryOption2DescKey = "TFTV_KEY_MACHINERY_REPAIR_OPTION2_DESC";

        internal static HealAbilityDef[] RankDefs = new HealAbilityDef[3];

        internal static void CreateDefs()
        {
            try
            {
                HealAbilityDef source = DefCache.GetDef<HealAbilityDef>("TechnicianRestoreBodyPart_AbilityDef");
                ReloadAbilityDef animSource = DefCache.GetDef<ReloadAbilityDef>("ReloadTurret_AbilityDef");

                for (int i = 0; i < 3; i++)
                {
                    int rank = i + 1;
                    string defName = $"MachineryRepair_Rank{rank}_AbilityDef";

                    HealAbilityDef cloned = Helper.CreateDefFromClone(source, RankAbilityGuids[i], defName);

                    cloned.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        RankViewGuids[i],
                        $"{defName}_View");

                    cloned.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        RankProgressionGuids[i],
                        $"{defName}_Progression");

                    cloned.TargetingDataDef = Helper.CreateDefFromClone(
                        source.TargetingDataDef,
                        RankTargetingGuids[i],
                        $"{defName}_Targeting");

                    if (cloned.TargetingDataDef != null && cloned.TargetingDataDef.Origin != null && cloned.TargetingDataDef.Origin.TargetTags != null)
                    {
                        cloned.TargetingDataDef.Origin.TargetTags.Clear();
                    }

                    cloned.EquipmentTags = new GameTagDef[] { };
                    cloned.ActorTags = new GameTagDef[] { };
                    cloned.SuppressHealingOnTargetTags.Clear();

                    cloned.RequiredCharges = 0;
                    cloned.ConsumedCharges = 0;
                    cloned.HealEffects.Clear();

                    cloned.ActionPointCost = 0.5f;
                    cloned.WillPointCost = 3f;
                    cloned.GeneralHealAmount = 0f;

                    RankDefs[i] = cloned;
                    cloned.AnimType = -1;
                 /*   TacActorSimpleAbilityAnimActionDef reloadTurretUtka = DefCache.GetDef<TacActorSimpleAbilityAnimActionDef>("E_ReloadTurret [Soldier_Utka_AnimActionsDef]");

                    TFTVLogger.Always($"reloadTurretUtka null? {reloadTurretUtka==null}");

                    reloadTurretUtka.AbilityDefs = reloadTurretUtka.AbilityDefs.Append(cloned).ToArray();*/



                  /*  foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo
                        .GetAllDefs<TacActorSimpleAbilityAnimActionDef>()
                        .Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                    {
                        if (animActionDef.AbilityDefs != null
                            && animActionDef.AbilityDefs.Contains(animSource)
                            && !animActionDef.AbilityDefs.Contains(cloned))
                        {
                            TFTVLogger.Info($"Adding {defName} to {animActionDef.name}");   

                            animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(cloned).ToArray();
                        }
                    }*/
                }

                ApplyChoiceFromSnapshot();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ApplyChoice(int option)
        {
            try
            {
                int normalizedOption = option == 2 ? 2 : 1;

                GameTagDef metallicTag = DefCache.GetDef<GameTagDef>("Metallic_SubstanceTypeTagDef");
                GameTagDef organicTag = DefCache.GetDef<GameTagDef>("Organic_SubstanceTypeTagDef");

                for (int i = 0; i < RankDefs.Length; i++)
                {
                    HealAbilityDef rankDef = RankDefs[i];
                    if (rankDef == null)
                    {
                        continue;
                    }

                    int rank = i + 1;
                    bool optionOne = normalizedOption == 1;

                    rankDef.GeneralHealAmount = 0f;
                    rankDef.BodyPartHealAmount = optionOne ? (10f * rank) : 0f;
                    UpdateViewElement(rankDef, optionOne);

                    rankDef.HealBodyParts = optionOne;
                    rankDef.RestoresArmour = false;

                    if (rankDef.TargetingDataDef != null && rankDef.TargetingDataDef.Origin != null && rankDef.TargetingDataDef.Origin.TargetTags != null)
                    {
                        rankDef.TargetingDataDef.Origin.TargetTags.Clear();

                        if (optionOne && metallicTag != null && !rankDef.TargetingDataDef.Origin.TargetTags.Contains(metallicTag))
                        {
                            rankDef.TargetingDataDef.Origin.TargetTags.Add(metallicTag);
                        }
                    }

                    rankDef.SuppressHealingOnTargetTags.Clear();
                    if (optionOne && organicTag != null && !rankDef.SuppressHealingOnTargetTags.Contains(organicTag))
                    {
                        rankDef.SuppressHealingOnTargetTags.Add(organicTag);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void UpdateViewElement(HealAbilityDef rankDef, bool optionOne)
        {
            if (rankDef == null || rankDef.ViewElementDef == null)
            {
                return;
            }

            if (rankDef.ViewElementDef.DisplayName1 != null)
            {
                rankDef.ViewElementDef.DisplayName1.LocalizationKey = MachineryAbilityNameKey;
            }

            if (rankDef.ViewElementDef.Description != null)
            {
                rankDef.ViewElementDef.Description.LocalizationKey = optionOne
                    ? MachineryOption1DescKey
                    : MachineryOption2DescKey;
            }
        }

        internal static void ApplyChoiceFromSnapshot()
        {
            try
            {
                if (!BaseReworkUtils.BaseReworkEnabled) return;

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
                    HealAbilityDef desiredDef = selectedOption == 1 && machineryRank > 0 && machineryRank <= RankDefs.Length
                        ? RankDefs[machineryRank - 1]
                        : null;

                    for (int i = 0; i < RankDefs.Length; i++)
                    {
                        HealAbilityDef rankDef = RankDefs[i];
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