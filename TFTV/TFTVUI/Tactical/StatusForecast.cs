using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using Base.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVUI.Tactical
{
    /// <summary>
    /// "What does this do to me next turn?" for the damage-over-time statuses, composed per character
    /// and shown in the status chip's hover tooltip.
    ///
    /// Each status aggregates differently and none of the stock descriptions say so:
    ///
    ///  - Acid is one status per limb. Every bare limb bills the character's Hit Points separately.
    ///  - Bleeding is one status covering several limbs, and each limb contributes a fixed amount
    ///    drawn from its body part. It never decays; it stops when the limb is healed.
    ///  - Burning damages every body part, but the character's Hit Points take the *average* across
    ///    parts, once - not the sum. Its level is also recalculated from the fire around the actor
    ///    each turn rather than decaying, so the level cannot honestly be predicted, only the damage.
    ///  - Poison is a plain actor-level total, and TFTV's own patch takes 3 Will alongside it.
    ///
    /// Anything without an entry here simply gets no forecast rather than a guessed one.
    /// </summary>
    internal static class StatusForecast
    {
        /// <summary>
        /// The tooltip body for one status chip, or null when this status has no forecast worth
        /// showing. The caller supplies the already-summed value the chip is displaying.
        /// </summary>
        internal static string Build(TacticalActor actor, TacStatusDef statusDef, float displayedValue)
        {
            try
            {
                if (actor == null || statusDef == null)
                {
                    return null;
                }

                List<string> lines = FindLines(actor, statusDef);

                if (lines == null || lines.Count == 0)
                {
                    return null;
                }

                if (statusDef.Visuals == null)
                {
                    return null;
                }

                string header = $"{statusDef.Visuals.DisplayName1.Localize()} {Mathf.RoundToInt(displayedValue)}";

                return string.Join(
                    Environment.NewLine,
                    new[] { header, TFTVCommonMethods.ConvertKeyToString("TFTV_ACID_NEXT_TURN") }
                        .Concat(lines)
                        .ToArray());
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static List<string> FindLines(TacticalActor actor, TacStatusDef statusDef)
        {
            foreach (TacStatus status in actor.Status?.Statuses?.OfType<TacStatus>() ?? Enumerable.Empty<TacStatus>())
            {
                if (status.TacStatusDef != statusDef)
                {
                    continue;
                }

                if (status is AcidStatus)
                {
                    return AcidLines(actor);
                }

                if (status is BleedStatus bleed)
                {
                    return BleedLines(actor, bleed);
                }

                if (status is FireStatus fire)
                {
                    return FireLines(actor, fire);
                }

                if (status is ParalysisDamageOverTimeStatus paralysis)
                {
                    return ParalysisLines(actor, paralysis);
                }

                if (status is StunStatus stun)
                {
                    return StunLines(stun);
                }

                if (status is InfectedStatus infected)
                {
                    return VirusLines(actor, infected);
                }

                if (status is DamageOverTimeStatus dot && IsPoison(dot))
                {
                    return PoisonLines(actor, dot);
                }
            }

            return null;
        }

        /// <summary>
        /// How far a damage-over-time status drops at the end of its turn.
        /// DamageOverTimeStatus.LowerDamageOverTimeLevel doubles the step whenever the actor has any
        /// resistance to the damage type at all - it tests the multiplier against 1 rather than
        /// scaling by it - so a forecast that just read LowerLevelPerTurn would be wrong for every
        /// resistant character.
        /// </summary>
        private static float DecayPerTurn(TacticalActor actor, DamageOverTimeStatus status)
        {
            float step = status.DamageOverTimeStatusDef.LowerLevelPerTurn;
            DamageTypeBaseEffectDef damageType = status.DamageOverTimeStatusDef.DamageTypeDef;

            if (damageType != null && actor.GetDamageMultiplierFor(damageType) < 1f)
            {
                step *= 2f;
            }

            return step;
        }

        #region acid

        private static List<string> AcidLines(TacticalActor actor)
        {
            return TFTVAcid.GetLimbAcid(actor)
                .Select(limb => limb.WillCostHealth
                    ? TFTVCommonMethods.FormatKey(
                        "TFTV_ACID_FORECAST_HEALTH",
                        limb.DisplayName,
                        Mathf.RoundToInt(limb.Acid),
                        Mathf.RoundToInt(limb.AcidAfter),
                        Mathf.RoundToInt(limb.HealthDamage))
                    : TFTVCommonMethods.FormatKey(
                        "TFTV_ACID_FORECAST_ARMOUR",
                        limb.DisplayName,
                        Mathf.RoundToInt(limb.Acid),
                        Mathf.RoundToInt(limb.AcidAfter),
                        Mathf.RoundToInt(limb.Armour),
                        Mathf.RoundToInt(limb.ArmourAfter)))
                .ToList();
        }

        #endregion

        #region bleeding

        /// <summary>
        /// Bleeding is a single status spanning several limbs, and each limb's contribution is the
        /// BleedValue of the body parts in it - a fixed property of the part, not a decaying pool.
        /// The whole total comes off Hit Points once per turn, and keeps doing so until the limb is
        /// healed, which is the part the stock description never says.
        /// </summary>
        private static List<string> BleedLines(TacticalActor actor, BleedStatus bleed)
        {
            List<string> lines = new List<string>();

            CharacterBodyState body = actor.BodyState;
            if (body == null)
            {
                return lines;
            }

            List<string> contributions = new List<string>();

            foreach (string slotName in bleed.GetTargetSlotsNames())
            {
                ItemSlot slot = body.GetSlot(slotName);
                if (slot == null)
                {
                    continue;
                }

                float value = slot.GetAllDirectItems(onlyBodyparts: true)
                    .Sum(item => item.BodyPartAspect.BleedValue);

                contributions.Add($"{slot.DisplayName} {Mathf.RoundToInt(value)}");
            }

            lines.Add(TFTVCommonMethods.FormatKey(
                "TFTV_BLEED_FORECAST",
                Mathf.RoundToInt(bleed.Value)));

            if (contributions.Count > 0)
            {
                lines.Add(TFTVCommonMethods.FormatKey(
                    "TFTV_BLEED_FROM",
                    string.Join(" · ", contributions.ToArray())));
            }

            lines.Add(TFTVCommonMethods.ConvertKeyToString("TFTV_BLEED_NO_DECAY"));

            return lines;
        }

        #endregion

        #region burning

        /// <summary>
        /// Fire adds every health slot to one accumulation, which routes to
        /// DamageAccumulation.ApplyAddedDamage_Fire: each part takes its own damage, but the
        /// character's Hit Points take the *average* across parts, once. That is the opposite of
        /// acid, where each affected limb bills Hit Points separately, and no description mentions
        /// either rule.
        ///
        /// The burning level itself is not predicted: FireStatus.StartTurn recalculates it from the
        /// fire voxels the actor is standing in, so next turn's level depends on where they move.
        /// </summary>
        private static List<string> FireLines(TacticalActor actor, FireStatus fire)
        {
            List<string> lines = new List<string>();

            CharacterBodyState body = actor.BodyState;
            if (body == null)
            {
                return lines;
            }

            float resistance = 1f;
            var fireType = actor.TacticalLevel?.VoxelMatrix?.VoxelMatrixData?.FireDamageTypeDef;
            if (fireType != null)
            {
                resistance = actor.GetDamageMultiplierFor(fireType);
            }

            float damage = fire.FullDamageValue;
            List<string> parts = new List<string>();
            float total = 0f;
            int count = 0;

            foreach (ItemSlot slot in body.GetHealthSlots())
            {
                float armour = (float)slot.GetArmor().Value;
                float applied = Mathf.Max(0f, damage - armour) * resistance;

                parts.Add($"{slot.DisplayName} {Mathf.RoundToInt(applied)}");
                total += applied;
                count++;
            }

            if (count == 0)
            {
                return lines;
            }

            lines.Add(string.Join(" · ", parts.ToArray()));
            lines.Add(TFTVCommonMethods.FormatKey(
                "TFTV_BURNING_FORECAST_AVERAGE",
                Mathf.RoundToInt(total / count)));
            lines.Add(TFTVCommonMethods.ConvertKeyToString("TFTV_BURNING_RECALCULATED"));

            return lines;
        }

        #endregion

        #region paralysis

        /// <summary>
        /// Paralysis does nothing at all until it crosses a quarter of the target stat, then removes
        /// Action Points in quarter steps, and at the full stat removes every point and applies
        /// Paralysed. ParalysisDamageEffect.AddTarget holds those thresholds; nothing in the UI hints
        /// that the number has steps at all, so a player watching it climb has no idea whether the
        /// next tick costs them anything.
        /// </summary>
        private static List<string> ParalysisLines(TacticalActor actor, ParalysisDamageOverTimeStatus paralysis)
        {
            List<string> lines = new List<string>();

            float threshold = GetParalysisThreshold(paralysis);
            float total = paralysis.FullDamageValue;

            if (threshold > 0f)
            {
                float ratio = total / threshold;

                if (ratio >= 1f)
                {
                    lines.Add(TFTVCommonMethods.ConvertKeyToString("TFTV_PARALYSIS_AP_FULL"));
                }
                else if (ratio >= 0.25f)
                {
                    // The effect floors to the quarter below, so 0.6 costs 50%, not 60%.
                    int percent = Mathf.FloorToInt(ratio * 4f) * 25;
                    lines.Add(TFTVCommonMethods.FormatKey("TFTV_PARALYSIS_AP_PARTIAL", percent));
                }
                else
                {
                    lines.Add(TFTVCommonMethods.FormatKey(
                        "TFTV_PARALYSIS_AP_NONE",
                        Mathf.CeilToInt(threshold * 0.25f - total)));
                }

                lines.Add(TFTVCommonMethods.FormatKey(
                    "TFTV_PARALYSIS_THRESHOLD",
                    Mathf.RoundToInt(total),
                    Mathf.RoundToInt(threshold)));
            }

            lines.Add(TFTVCommonMethods.FormatKey(
                "TFTV_PARALYSIS_LEVEL",
                Mathf.RoundToInt(paralysis.Value),
                Mathf.RoundToInt(Mathf.Max(0f, paralysis.Value - DecayPerTurn(actor, paralysis)))));

            return lines;
        }

        /// <summary>
        /// The stat paralysis is measured against, read from its own damage effect rather than
        /// assumed - the def picks it, and Limit is only populated when the def opts into displaying
        /// it on the healthbar.
        /// </summary>
        private static float GetParalysisThreshold(ParalysisDamageOverTimeStatus paralysis)
        {
            ParalysisDamageEffectDef effectDef = paralysis.DamageEffect?.DamageEffectDef as ParalysisDamageEffectDef;

            if (effectDef == null || paralysis.TacStatusComponent == null)
            {
                return float.NaN;
            }

            BaseStat stat = paralysis.TacStatusComponent.GetStat(effectDef.TargetStat.ToString());

            return stat == null ? float.NaN : (float)stat;
        }

        #endregion

        #region stun

        /// <summary>
        /// Stun takes a flat share of maximum Action Points every turn it is applied. The share is on
        /// the def, so it is stated rather than hardcoded - TFTV has been changing how stun stacks.
        /// </summary>
        private static List<string> StunLines(StunStatus stun)
        {
            return new List<string>
            {
                TFTVCommonMethods.FormatKey(
                    "TFTV_STUN_AP",
                    Mathf.RoundToInt(stun.StunStatusDef.ActionPointsReduction * 100f)),
            };
        }

        #endregion

        #region virus

        /// <summary>
        /// Virus is a DamageOverTimeStatus like the others, but its damage effect is a
        /// WillDamageEffect: the value comes off Will Points, not Hit Points.
        ///
        /// The consequence worth warning about is the one the description never mentions. When the
        /// hit would take Will Points to zero, WillDamageEffect applies
        /// StatusOnWillpointsReachingZero - the character turns - so the forecast says so before it
        /// happens rather than after.
        /// </summary>
        private static List<string> VirusLines(TacticalActor actor, InfectedStatus infected)
        {
            List<string> lines = new List<string>();

            float damage = infected.FullDamageValue;
            float will = actor.CharacterStats?.WillPoints ?? 0f;

            lines.Add(TFTVCommonMethods.FormatKey(
                "TFTV_VIRUS_FORECAST",
                Mathf.RoundToInt(damage),
                Mathf.RoundToInt(will),
                Mathf.RoundToInt(Mathf.Max(0f, will - damage))));

            WillDamageEffectDef willEffect = infected.DamageEffect?.DamageEffectDef as WillDamageEffectDef;

            if (willEffect != null
                && willEffect.StatusOnWillpointsReachingZero != null
                && will <= damage)
            {
                lines.Add(TFTVCommonMethods.ConvertKeyToString("TFTV_VIRUS_ZERO_WILL"));
            }

            lines.Add(TFTVCommonMethods.FormatKey(
                "TFTV_VIRUS_LEVEL",
                Mathf.RoundToInt(infected.Value),
                Mathf.RoundToInt(Mathf.Max(0f, infected.Value - DecayPerTurn(actor, infected)))));

            return lines;
        }

        #endregion

        #region poison

        private static bool IsPoison(DamageOverTimeStatus status)
        {
            return status.DamageOverTimeStatusDef != null
                && status.DamageOverTimeStatusDef.name.Equals("Poison_DamageOverTimeStatusDef");
        }

        /// <summary>
        /// Poison is honestly a single actor-level number, so the only thing missing is what it will
        /// cost. The 3 Will comes from TFTV's own patch in VariousAdjustments, not from vanilla.
        /// </summary>
        private static List<string> PoisonLines(TacticalActor actor, DamageOverTimeStatus poison)
        {
            float damage = poison.FullDamageValue;
            float after = Mathf.Max(0f, poison.Value - DecayPerTurn(actor, poison));

            return new List<string>
            {
                TFTVCommonMethods.FormatKey(
                    "TFTV_POISON_FORECAST",
                    Mathf.RoundToInt(damage),
                    Mathf.RoundToInt(poison.Value),
                    Mathf.RoundToInt(after)),
            };
        }

        #endregion
    }
}
