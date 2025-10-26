using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TFTV
{
    internal class TFTVMeleeDamage
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static StandardDamageTypeEffectDef MeleeStandardDamageType;
        public static DamageKeywordDef MeleeDamageKeywordDef;
        public static StandardDamageTypeEffectDef PsychicStandardDamageType;
        public static DamageKeywordDef PsychicStandardDamageKeywordDef;

        public static void AddMeleeDamageType()
        {
            CreateMeleeDamageType();
            CreatePsychicStandardDamageType();
            ReplaceCorruptionDamageTypes();
        }

        [HarmonyPatch(typeof(DamagePayload), nameof(DamagePayload.GetPayloadAiDamageScore))]
        internal static class DamagePayload_GetPayloadAiDamageScore_Patch
        {
            /// <summary>
            /// Harmony prefix that replaces the vanilla AI damage scoring implementation. The custom
            /// version mirrors the original logic but treats any keyword that applies standard damage
            /// as direct damage so cloned melee keywords still contribute to the score.
            /// </summary>
            /// <returns>Always returns <c>false</c> to skip the original method.</returns>
            [HarmonyPrefix]
            private static bool Prefix(DamagePayload __instance, TacticalActorBase targetActorBase, ref float __result)
            {
                __result = CalculateAiDamageScore(__instance, targetActorBase);
                return false;
            }

            private static float CalculateAiDamageScore(DamagePayload payload, TacticalActorBase target)
            {
                TacticalActor tacticalTarget = target as TacticalActor;
                List<IDamageReceiver> damageReceivers = GetDamageReceivers(tacticalTarget, target);
                if (damageReceivers == null)
                {
                    return 0f;
                }

                bool isAreaDamage = payload.DamageDeliveryType == DamageDeliveryType.Sphere;
                int receiverCount = damageReceivers.Count;
                if (receiverCount == 0)
                {
                    return 1f;
                }

                int projectilesPerAttack = payload.AutoFireShotCount * payload.ProjectilesPerShot;
                float currentHealth = target.Health;
                float maxHealth = target.Health.Max;

                int currentWillPoints = -1;
                int willpowerStat = -1;
                int enduranceStat = -1;
                int speedStat = -1;

                if (tacticalTarget != null)
                {
                    currentWillPoints = tacticalTarget.CharacterStats.WillPoints.IntValue;
                    willpowerStat = tacticalTarget.CharacterStats.Willpower.IntValue;
                    enduranceStat = tacticalTarget.CharacterStats.Endurance.IntValue;
                    speedStat = tacticalTarget.CharacterStats.Speed.IntValue;
                }

                // Keywords are processed in the order the game expects (by application priority).
                List<DamageKeywordPair> orderedKeywords = payload.DamageKeywords
                    .OrderBy(pair => pair.DamageKeywordDef.KeywordApplicationPriority)
                    .ToList();

                float piercingBonus = 0f;
                float directDamageScore = 0f;
                float burnDamageScore = 0f;
                float willDamageScore = 0f;
                float armorShredScore = 0f;
                int paralysisTicks = 0;
                float speedDamageScore = 0f;
                float healthHealScore = 0f;
                float willHealScore = 0f;
                float healthDotScore = 0f;
                float healthDotImmediateDamage = 0f;
                float willDotScore = 0f;
                bool willCausePanic = false;
                bool willDaze = false;
                bool willParalyze = false;
                int mistTilesSpawned = 0;
                int gooTilesSpawned = 0;

                int existingParalysis = 0;
                int existingPoison = 0;
                int existingVirus = 0;
                int existingBleed = 0;
                bool isGooed = false;

                if (tacticalTarget != null)
                {
                    DamageOverTimeStatus paralysisStatus = tacticalTarget.Status.GetStatusByName("Paralysis") as DamageOverTimeStatus;
                    existingParalysis = paralysisStatus?.IntValue ?? 0;

                    DamageOverTimeStatus poisonStatus = tacticalTarget.Status.GetStatusByName("Poison") as DamageOverTimeStatus;
                    existingPoison = poisonStatus?.IntValue ?? 0;

                    DamageOverTimeStatus virusStatus = tacticalTarget.Status.GetStatusByName("Infected") as DamageOverTimeStatus;
                    existingVirus = virusStatus?.IntValue ?? 0;

                    BleedStatus bleedStatus = tacticalTarget.Status.GetStatusByName("Bleed") as BleedStatus;
                    existingBleed = (int)(bleedStatus?.Value ?? 0f);

                    isGooed = tacticalTarget.Status.GetStatusByName("Gooed") != null;
                }

                if (isGooed)
                {
                    speedStat = 0;
                }

                SharedDamageKeywordsDataDef sharedKeywords = target.SharedData.SharedDamageKeywords;

                foreach (DamageKeywordPair keywordPair in orderedKeywords)
                {
                    DamageKeywordDef keyword = keywordPair.DamageKeywordDef;

                    if (target.IsImmuneTo(keyword.DamageTypeDef, null))
                    {
                        continue;
                    }

                    float contribution = 0f;

                    if (IsStandardDamageKeyword(keyword, sharedKeywords.DamageKeyword))
                    {
                        foreach (IDamageReceiver receiver in damageReceivers)
                        {
                            float effectiveDamage = Mathf.Max(keywordPair.Value - Mathf.Max(receiver.GetArmor() - piercingBonus, 0f), 0f);
                            contribution += effectiveDamage;
                        }

                        directDamageScore += contribution / receiverCount * projectilesPerAttack;
                        continue;
                    }

                    if (keyword == sharedKeywords.BlastKeyword)
                    {
                        foreach (IDamageReceiver receiver in damageReceivers)
                        {
                            float effectiveDamage = Mathf.Max(keywordPair.Value - Mathf.Max(receiver.GetArmor() - piercingBonus, 0f), 0f);
                            contribution = Mathf.Max(contribution, effectiveDamage);
                        }

                        directDamageScore += contribution * projectilesPerAttack;
                        continue;
                    }

                    if (keyword == sharedKeywords.ShreddingKeyword)
                    {
                        // Track per-target armour so shredding accounts for previously removed points.
                        List<float> remainingArmor = damageReceivers.Select(receiver => receiver.GetArmor().Value.EndValue).ToList();

                        if (isAreaDamage)
                        {
                            for (int shotIndex = 0; shotIndex < projectilesPerAttack; shotIndex++)
                            {
                                for (int targetIndex = 0; targetIndex < remainingArmor.Count; targetIndex++)
                                {
                                    float shredded = Mathf.Min(keywordPair.Value, remainingArmor[targetIndex]);
                                    contribution += shredded;
                                    remainingArmor[targetIndex] -= shredded;
                                }
                            }
                        }
                        else
                        {
                            for (int shotIndex = 0; shotIndex < projectilesPerAttack; shotIndex++)
                            {
                                for (int targetIndex = 0; targetIndex < remainingArmor.Count; targetIndex++)
                                {
                                    float shredded = Mathf.Min(keywordPair.Value, remainingArmor[targetIndex]);
                                    remainingArmor[targetIndex] -= shredded;
                                    contribution += shredded / receiverCount;
                                }
                            }
                        }

                        armorShredScore += contribution;
                        continue;
                    }

                    if (keyword == sharedKeywords.PiercingKeyword)
                    {
                        piercingBonus = keywordPair.Value;
                        continue;
                    }

                    if (keyword == sharedKeywords.SyphonKeyword)
                    {
                        foreach (IDamageReceiver receiver in damageReceivers)
                        {
                            float effectiveDamage = Mathf.Max(keywordPair.Value - Mathf.Max(receiver.GetArmor() - piercingBonus, 0f), 0f);
                            contribution += effectiveDamage;
                        }

                        directDamageScore += contribution / receiverCount * projectilesPerAttack;
                        healthHealScore += directDamageScore;
                        continue;
                    }

                    if (keyword == sharedKeywords.AcidKeyword)
                    {
                        if (isAreaDamage)
                        {
                            healthDotScore += keywordPair.Value * projectilesPerAttack * receiverCount;
                        }
                        else
                        {
                            healthDotScore += keywordPair.Value * projectilesPerAttack;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.BleedingKeyword)
                    {
                        if (directDamageScore > 0f)
                        {
                            healthDotScore += keywordPair.Value * projectilesPerAttack * 3f;
                            healthDotImmediateDamage += keywordPair.Value * projectilesPerAttack;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.PoisonousKeyword)
                    {
                        if (directDamageScore > 0f)
                        {
                            float poisonStacks = keywordPair.Value * projectilesPerAttack;
                            healthDotScore += (10f + poisonStacks + existingPoison * 2f) * poisonStacks / 20f;
                            healthDotImmediateDamage = poisonStacks;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.PsychicKeyword)
                    {
                        if (currentWillPoints != -1)
                        {
                            float damage = keywordPair.Value * projectilesPerAttack;
                            willDamageScore += Mathf.Min(damage, currentWillPoints);
                            if (currentWillPoints < damage)
                            {
                                willCausePanic = true;
                            }
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.ViralKeyword)
                    {
                        if (currentWillPoints != -1 && directDamageScore > 0f)
                        {
                            float virusStacks = keywordPair.Value * projectilesPerAttack;
                            willDotScore += (virusStacks + 1f + existingVirus * 2f) * virusStacks / 2f;
                            if (currentWillPoints < virusStacks)
                            {
                                willCausePanic = true;
                            }
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.SonicKeyword)
                    {
                        if (willpowerStat != -1 && willpowerStat <= keywordPair.Value)
                        {
                            willDaze = true;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.ShockKeyword)
                    {
                        if (currentHealth <= keywordPair.Value)
                        {
                            willDaze = true;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.ParalysingKeyword)
                    {
                        if (existingParalysis < enduranceStat && directDamageScore > 0f)
                        {
                            float paralysisStacks = keywordPair.Value * projectilesPerAttack;
                            willParalyze = existingParalysis + paralysisStacks >= enduranceStat;
                            paralysisTicks = (int)(((existingParalysis + paralysisStacks) / enduranceStat * 4f)) - existingParalysis / enduranceStat * 4;
                        }

                        continue;
                    }

                    if (keyword == sharedKeywords.BurningKeyword)
                    {
                        foreach (IDamageReceiver receiver in damageReceivers)
                        {
                            float effectiveDamage = Mathf.Max(keywordPair.Value - receiver.GetArmor(), 0f);
                            contribution = Mathf.Max(contribution, effectiveDamage);
                        }

                        burnDamageScore += contribution;
                        continue;
                    }

                    if (keyword == sharedKeywords.GooKeyword)
                    {
                        contribution = 1f;
                        if (isAreaDamage)
                        {
                            contribution = payload.AoeRadius * payload.AoeRadius * Mathf.PI;
                        }

                        if (speedStat > 0)
                        {
                            speedDamageScore = speedStat;
                        }

                        gooTilesSpawned += (int)contribution * projectilesPerAttack;
                        continue;
                    }

                    if (keyword == sharedKeywords.MistKeyword)
                    {
                        contribution = 1f;
                        if (isAreaDamage)
                        {
                            contribution = payload.AoeRadius * payload.AoeRadius * Mathf.PI;
                        }

                        if (currentWillPoints != -1)
                        {
                            willDotScore += 2f;
                            if (currentWillPoints < 2)
                            {
                                willCausePanic = true;
                            }
                        }

                        mistTilesSpawned += (int)contribution * projectilesPerAttack;
                        continue;
                    }
                }

                float totalScore = directDamageScore
                                   + healthDotImmediateDamage
                                   + willDamageScore * 20f
                                   + burnDamageScore * 3f
                                   + armorShredScore * 2f
                                   + (willParalyze ? maxHealth : (paralysisTicks * maxHealth / 4f))
                                   + healthHealScore * 0.3f
                                   + willHealScore
                                   + (healthDotScore - healthDotImmediateDamage) * 0.5f
                                   + willDotScore * 10f
                                   + (willCausePanic ? (maxHealth / 2f) : 0f)
                                   + (willDaze ? (maxHealth / 4f) : 0f)
                                   + mistTilesSpawned * 2f
                                   + gooTilesSpawned * 2f
                                   + ((speedStat <= 0) ? 0f : Mathf.Min(speedStat, speedDamageScore) * 10f);

                bool willKill = currentHealth - directDamageScore - healthDotImmediateDamage - burnDamageScore - existingPoison - existingBleed <= 0f;

                if (DamagePayload.debugAiScore)
                {
                    Debug.Log(string.Concat(new object[]
                    {
                    target.DisplayName,
                    "\nTotal Score: ", totalScore,
                    "\nPiercing: ", piercingBonus,
                    "\nHPDamage: ", directDamageScore,
                    "\nburnDamage: ", burnDamageScore,
                    "\nWPDamage: ", willDamageScore,
                    "\nArmorDamage: ", armorShredScore,
                    "\nAPDamage: ", paralysisTicks,
                    "\nSpeedDamage: ", speedDamageScore,
                    "\nHPHeal: ", healthHealScore,
                    "\nWPHeal: ", willHealScore,
                    "\nHPDotDamage: ", healthDotScore,
                    "\nWPDotDamage: ", willDotScore,
                    "\nwillPanic: ", willCausePanic,
                    "\ndazed: ", willDaze,
                    "\nparalysed: ", willParalyze,
                    "\nspawnedMist: ", mistTilesSpawned,
                    "\nspawnedGoo: ", gooTilesSpawned,
                    "\nparalisisStacks: ", existingParalysis,
                    "\npoisonStacks: ", existingPoison,
                    "\nviralStacks: ", existingVirus
                    }));
                }

                return willKill ? maxHealth : totalScore;
            }

            private static List<IDamageReceiver> GetDamageReceivers(TacticalActor tacticalTarget, TacticalActorBase target)
            {
                if (tacticalTarget != null)
                {
                    return tacticalTarget.BodyState.GetHealthSlots()
                        .OfType<IDamageReceiver>()
                        .ToList();
                }

                if (target is StructuralTarget)
                {
                    return new List<IDamageReceiver> { (IDamageReceiver)target };
                }

                return null;
            }

            private static bool IsStandardDamageKeyword(DamageKeywordDef candidate, DamageKeywordDef shared)
            {
                if (candidate == null)
                {
                    return false;
                }

                if (candidate == shared)
                {
                    return true;
                }

                if (!candidate.AppliesStandardDamage)
                {
                    return false;
                }

                DamageTypeBaseEffectDef damageType = candidate.DamageTypeDef;
                return damageType is StandardDamageTypeEffectDef;
            }
        }


        private static void ReplaceCorruptionDamageTypes()
        {
            try
            {
                CorruptionStatusDef corruptionStatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");

                if (TFTVNewGameOptions.IsReworkEnabled())
                {
                    corruptionStatusDef.DamageTypeDefs = new List<DamageTypeBaseEffectDef> { MeleeStandardDamageType, PsychicStandardDamageType };
                }
                else
                {
                    corruptionStatusDef.DamageTypeDefs.Add(MeleeStandardDamageType);
                    corruptionStatusDef.DamageTypeDefs.Add(PsychicStandardDamageType);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreatePsychicStandardDamageType()
        {
            try
            {

                StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef newStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{14084440-1682-4620-BF4D-62774EBD644A}", "TFTV_PsychicStandard_damageType");

                DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{8AE7DE3C-72EA-4562-A221-2286AEB2ED20}", "TFTV_PsychicStandard_damageKeyword");

                newDamageKeywordDef.DamageTypeDef = newStandardDamageTypeEffectDef;

                PsychicStandardDamageKeywordDef = newDamageKeywordDef;
                PsychicStandardDamageType = newStandardDamageTypeEffectDef;

                DamageEffectDef mindCrushDamageEffectDef = DefCache.GetDef<DamageEffectDef>("E_Effect [MindCrush_AbilityDef]");
                mindCrushDamageEffectDef.DamageTypeDef = PsychicStandardDamageType;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateMeleeDamageType()
        {
            try
            {

                StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef newMeleeStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{4BAEF84E-5985-47D2-897F-99C863C7E71D}", "TFTV_Melee_damageType");

                DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{701EBF00-6BDB-48E2-9635-C854ABC63AFA}", "TFTV_Melee_damageKeyword");

                newDamageKeywordDef.DamageTypeDef = newMeleeStandardDamageTypeEffectDef;

                //"Mutog_HeadRamming_BodyPartDef"
                WeaponDef RammingHead = (WeaponDef)Repo.GetDef("c29d4fc0-cb86-0e54-383c-513f8926e6c1");
                RammingHead.Tags.Add(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef"));


                foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
                {
                    if (weaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef")) && weaponDef.DamagePayload.DamageKeywords.Any(p => p.DamageKeywordDef == sourceDamageKeywordDef))
                    {
                        // TFTVLogger.Always($"{weaponDef.name} has melee weapon tag, replacing damage type");
                        foreach (DamageKeywordPair damageKeywordPair in weaponDef.DamagePayload.DamageKeywords)
                        {
                            if (damageKeywordPair.DamageKeywordDef == sourceDamageKeywordDef)
                            {
                                damageKeywordPair.DamageKeywordDef = newDamageKeywordDef;
                                // TFTVLogger.Always($"replaced");
                            }
                        }
                    }
                }

                MeleeStandardDamageType = newMeleeStandardDamageTypeEffectDef;
                MeleeDamageKeywordDef = newDamageKeywordDef;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
