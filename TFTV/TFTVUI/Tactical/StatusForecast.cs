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
    /// and shown in the status chip's hover tooltip (drawn by <see cref="StatusForecastTooltip"/>).
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
    /// Stats the status takes from are shown as a loss only ("Hit Points −40"), not as before and
    /// after: anything else that happens before the next turn would make the "after" wrong. The
    /// status's own level is the exception and keeps both ends ("40 → 30"), because how far it
    /// drops depends on the character's resistances and is exactly what the player cannot see
    /// anywhere else.
    ///
    /// Anything without an entry here simply gets no forecast rather than a guessed one.
    /// </summary>
    internal static class StatusForecast
    {
        /// <summary>TFTV's poison patch in VariousAdjustments takes this much Will every turn.</summary>
        private const int PoisonWillDamage = 3;

        internal enum RowKind
        {
            /// <summary>A label on the left, a value on the right, optionally a level at the end.</summary>
            Stat,
            /// <summary>The status icon with the level going from one value to the next.</summary>
            Level,
            /// <summary>A rule of the status, small and muted.</summary>
            Note,
            /// <summary>A consequence the player must not miss.</summary>
            Warning,
        }

        internal sealed class Row
        {
            internal RowKind Kind;
            internal string Label;
            internal string Value;
            internal bool IsLoss;
            /// <summary>Rows that break a total down by limb; consecutive ones share a bracket.</summary>
            internal bool Bracketed;
            internal bool HasLevel;
            internal int Before;
            internal int After;
        }

        internal sealed class Forecast
        {
            internal string Title;
            internal Color TitleColor;
            internal Sprite Icon;
            internal Color IconColor;
            /// <summary>Acid needs a third column per limb, so it asks for more room.</summary>
            internal bool Wide;
            internal readonly List<Row> Rows = new List<Row>();

            internal Forecast Stat(string labelKey, string value, bool isLoss)
            {
                Rows.Add(new Row { Kind = RowKind.Stat, Label = Key(labelKey), Value = value, IsLoss = isLoss });
                return this;
            }

            internal Forecast Level(float before, float after)
            {
                Rows.Add(new Row
                {
                    Kind = RowKind.Level,
                    HasLevel = true,
                    Before = Mathf.RoundToInt(before),
                    After = Mathf.RoundToInt(after),
                });
                return this;
            }

            internal Forecast Note(string text)
            {
                Rows.Add(new Row { Kind = RowKind.Note, Label = text });
                return this;
            }

            internal Forecast Warning(string text)
            {
                Rows.Add(new Row { Kind = RowKind.Warning, Label = text });
                return this;
            }
        }

        /// <summary>
        /// The forecast for one status chip, or null when this status has no forecast worth
        /// showing. The caller supplies the already-summed value the chip is displaying.
        /// </summary>
        internal static Forecast Build(TacticalActor actor, TacStatusDef statusDef, float displayedValue)
        {
            try
            {
                if (actor == null || statusDef == null || statusDef.Visuals == null)
                {
                    return null;
                }

                Forecast forecast = new Forecast
                {
                    Title = $"{statusDef.Visuals.DisplayName1.Localize()} {Mathf.RoundToInt(displayedValue)}",
                    TitleColor = statusDef.Visuals.Color,
                    Icon = statusDef.Visuals.SmallIcon,
                    IconColor = statusDef.Visuals.Color,
                };

                if (!Fill(forecast, actor, statusDef) || forecast.Rows.Count == 0)
                {
                    return null;
                }

                return forecast;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// When the damage-over-time statuses together will take at least the Hit Points the
        /// character has left, a forecast listing what each of them takes; otherwise null.
        ///
        /// Each status is counted the way its own forecast counts it - acid per bare limb, burning
        /// as the average across parts - so the total agrees with the chips it is summing. Virus
        /// and paralysis take no Hit Points and do not count.
        /// </summary>
        internal static Forecast BuildLethal(TacticalActor actor, Sprite icon)
        {
            try
            {
                if (actor == null || actor.Health == null)
                {
                    return null;
                }

                List<KeyValuePair<string, float>> losses = new List<KeyValuePair<string, float>>();
                HashSet<TacStatusDef> seen = new HashSet<TacStatusDef>();

                foreach (TacStatus status in actor.Status?.Statuses?.OfType<TacStatus>() ?? Enumerable.Empty<TacStatus>())
                {
                    // Acid is one status per limb; the per-limb list below already covers them all.
                    if (status.TacStatusDef == null || !seen.Add(status.TacStatusDef))
                    {
                        continue;
                    }

                    float loss;

                    if (status is AcidStatus)
                    {
                        loss = TFTVAcid.GetLimbAcid(actor).Sum(limb => limb.HealthDamage);
                    }
                    else if (status is BleedStatus bleed)
                    {
                        loss = bleed.Value;
                    }
                    else if (status is FireStatus fire)
                    {
                        List<KeyValuePair<string, float>> parts = FireDamageByPart(actor, fire);
                        loss = parts.Count > 0 ? parts.Average(part => part.Value) : 0f;
                    }
                    else if (status is DamageOverTimeStatus dot && IsPoison(dot))
                    {
                        loss = dot.FullDamageValue;
                    }
                    else
                    {
                        continue;
                    }

                    if (loss >= 0.5f)
                    {
                        string name = status.TacStatusDef.Visuals != null
                            ? status.TacStatusDef.Visuals.DisplayName1.Localize()
                            : status.TacStatusDef.name;
                        losses.Add(new KeyValuePair<string, float>(name, loss));
                    }
                }

                float total = losses.Sum(loss => loss.Value);
                int health = actor.Health.IntValue;

                if (losses.Count == 0 || Mathf.RoundToInt(total) < health)
                {
                    return null;
                }

                return Breakdown(
                    "TFTV_LETHAL_DOT_TITLE",
                    "TFTV_FORECAST_HIT_POINTS",
                    losses,
                    "",
                    TFTVCommonMethods.FormatKey("TFTV_LETHAL_DOT_WARNING", health),
                    icon);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// When poison and virus together will take the character's last Will Points, a forecast
        /// listing what each takes; otherwise null. TacticalActor.OnWillPointsChange panics a
        /// character whose Will Points reach zero, and both drains land at the start of the turn.
        ///
        /// A character already at zero has nothing left to lose and is not warned again.
        /// </summary>
        internal static Forecast BuildPanic(TacticalActor actor, Sprite icon)
        {
            try
            {
                if (actor == null || actor.CharacterStats == null)
                {
                    return null;
                }

                List<KeyValuePair<string, float>> losses = new List<KeyValuePair<string, float>>();

                foreach (TacStatus status in actor.Status?.Statuses?.OfType<TacStatus>() ?? Enumerable.Empty<TacStatus>())
                {
                    if (status is InfectedStatus infected && infected.FullDamageValue >= 0.5f)
                    {
                        losses.Add(new KeyValuePair<string, float>(StatusName(status), infected.FullDamageValue));
                    }
                    else if (status is DamageOverTimeStatus dot && IsPoison(dot) && dot.IntValue > 0)
                    {
                        losses.Add(new KeyValuePair<string, float>(StatusName(status), PoisonWillDamage));
                    }
                }

                int will = Mathf.RoundToInt(actor.CharacterStats.WillPoints);
                float total = losses.Sum(loss => loss.Value);

                if (losses.Count == 0 || will <= 0 || Mathf.RoundToInt(total) < will)
                {
                    return null;
                }

                return Breakdown(
                    "TFTV_PANIC_DOT_TITLE",
                    "TFTV_FORECAST_WILL_POINTS",
                    losses,
                    "",
                    TFTVCommonMethods.FormatKey("TFTV_PANIC_DOT_WARNING", will),
                    icon);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// When stun, paralysis and suppression together will take every Action Point the character
        /// gets back at the start of next turn, a forecast listing each share; otherwise null.
        ///
        /// All three are charged as shares of maximum Action Points, after the refill, so they add:
        /// StunStatus.ReduceActionPoints and the paralysis effect run in StartTurn, and TFTV's
        /// suppression penalty in a postfix to it. This is the same sum MeleeRangePrediction uses
        /// for an enemy's reach next turn.
        /// </summary>
        internal static Forecast BuildNoActionPoints(TacticalActor actor, Sprite icon)
        {
            try
            {
                if (actor == null || actor.Status == null)
                {
                    return null;
                }

                List<KeyValuePair<string, float>> losses = new List<KeyValuePair<string, float>>();

                StunStatus stun = actor.Status.GetStatus<StunStatus>();
                if (stun != null && stun.StunStatusDef.ActionPointsReduction > 0f)
                {
                    losses.Add(new KeyValuePair<string, float>(StatusName(stun), stun.StunStatusDef.ActionPointsReduction * 100f));
                }

                ParalysisDamageOverTimeStatus paralysis = actor.Status.GetStatus<ParalysisDamageOverTimeStatus>();
                if (paralysis != null)
                {
                    float threshold = GetParalysisThreshold(paralysis);
                    if (threshold > 0f)
                    {
                        float ratio = paralysis.FullDamageValue / threshold;
                        int percent = ratio >= 1f ? 100 : Mathf.FloorToInt(ratio * 4f) * 25;

                        if (percent > 0)
                        {
                            losses.Add(new KeyValuePair<string, float>(StatusName(paralysis), percent));
                        }
                    }
                }

                float suppression = TFTVSuppression.SuppressionRuntime.GetNextTurnActionPointLossFraction(actor);
                if (suppression > 0f)
                {
                    losses.Add(new KeyValuePair<string, float>(Key("TFTV_FORECAST_SUPPRESSION"), suppression * 100f));
                }

                float total = losses.Sum(loss => loss.Value);

                if (losses.Count == 0 || total < 99.5f)
                {
                    return null;
                }

                return Breakdown(
                    "TFTV_NO_AP_TITLE",
                    "TFTV_FORECAST_ACTION_POINTS",
                    losses,
                    "%",
                    Key("TFTV_NO_AP_WARNING"),
                    icon,
                    // Shares past 100% are not lost twice; the total says what actually goes.
                    totalOverride: 100f);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static string StatusName(TacStatus status)
        {
            return status.TacStatusDef?.Visuals != null
                ? status.TacStatusDef.Visuals.DisplayName1.Localize()
                : status.TacStatusDef?.name ?? string.Empty;
        }

        /// <summary>
        /// The shape the three warnings share: the stat's total loss, one bracketed row per status
        /// contributing to it, and the consequence in red.
        /// </summary>
        private static Forecast Breakdown(
            string titleKey,
            string statKey,
            List<KeyValuePair<string, float>> losses,
            string suffix,
            string warning,
            Sprite icon,
            float? totalOverride = null)
        {
            Forecast forecast = new Forecast
            {
                Title = Key(titleKey),
                TitleColor = new Color(0.93f, 0.24f, 0.24f, 1f),
                Icon = icon,
                IconColor = Color.white,
            };

            forecast.Stat(statKey, Loss(totalOverride ?? losses.Sum(loss => loss.Value), suffix), true);

            foreach (KeyValuePair<string, float> loss in losses)
            {
                forecast.Rows.Add(new Row
                {
                    Kind = RowKind.Stat,
                    Label = loss.Key,
                    Value = Loss(loss.Value, suffix),
                    IsLoss = true,
                    Bracketed = true,
                });
            }

            forecast.Warning(warning);

            return forecast;
        }

        private static bool Fill(Forecast forecast, TacticalActor actor, TacStatusDef statusDef)
        {
            foreach (TacStatus status in actor.Status?.Statuses?.OfType<TacStatus>() ?? Enumerable.Empty<TacStatus>())
            {
                if (status.TacStatusDef != statusDef)
                {
                    continue;
                }

                if (status is AcidStatus)
                {
                    return AcidRows(forecast, actor);
                }

                if (status is BleedStatus bleed)
                {
                    return BleedRows(forecast, actor, bleed);
                }

                if (status is FireStatus fire)
                {
                    return FireRows(forecast, actor, fire);
                }

                if (status is ParalysisDamageOverTimeStatus paralysis)
                {
                    return ParalysisRows(forecast, actor, paralysis);
                }

                if (status is StunStatus stun)
                {
                    return StunRows(forecast, stun);
                }

                if (status is InfectedStatus infected)
                {
                    return VirusRows(forecast, actor, infected);
                }

                if (status is DamageOverTimeStatus dot && IsPoison(dot))
                {
                    return PoisonRows(forecast, actor, dot);
                }
            }

            return false;
        }

        private static string Key(string key) => TFTVCommonMethods.ConvertKeyToString(key);

        /// <summary>"−40", or a plain "0" when nothing is lost - a red "−0" would be a false alarm.</summary>
        private static string Loss(float amount, string suffix = "")
        {
            int rounded = Mathf.RoundToInt(amount);
            return rounded > 0 ? $"−{rounded}{suffix}" : $"0{suffix}";
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

        /// <summary>
        /// The Hit Points row is the sum of what every bare limb bills, which unlike the acid total
        /// is a number the game really does take. Under it, one bracketed row per limb: what the
        /// acid takes from that limb, and that limb's own acid level, which decays on its own clock.
        /// </summary>
        private static bool AcidRows(Forecast forecast, TacticalActor actor)
        {
            List<TFTVAcid.LimbAcid> limbs = TFTVAcid.GetLimbAcid(actor);

            if (limbs.Count == 0)
            {
                return false;
            }

            forecast.Wide = true;
            forecast.Stat("TFTV_FORECAST_HIT_POINTS", Loss(limbs.Sum(limb => limb.HealthDamage)),
                limbs.Any(limb => limb.WillCostHealth));

            foreach (TFTVAcid.LimbAcid limb in limbs)
            {
                forecast.Rows.Add(new Row
                {
                    Kind = RowKind.Stat,
                    Label = limb.DisplayName,
                    Value = limb.WillCostHealth
                        ? TFTVCommonMethods.FormatKey("TFTV_ACID_LIMB_HEALTH", Mathf.RoundToInt(limb.HealthDamage))
                        : TFTVCommonMethods.FormatKey("TFTV_ACID_LIMB_ARMOUR", Mathf.RoundToInt(limb.Armour - limb.ArmourAfter)),
                    IsLoss = limb.WillCostHealth || limb.Armour - limb.ArmourAfter > 0.5f,
                    Bracketed = true,
                    HasLevel = true,
                    Before = Mathf.RoundToInt(limb.Acid),
                    After = Mathf.RoundToInt(limb.AcidAfter),
                });
            }

            return true;
        }

        #endregion

        #region bleeding

        /// <summary>
        /// Bleeding is a single status spanning several limbs, and each limb's contribution is the
        /// BleedValue of the body parts in it - a fixed property of the part, not a decaying pool.
        /// The whole total comes off Hit Points once per turn, and keeps doing so until the limb is
        /// healed, which is the part the stock description never says.
        /// </summary>
        private static bool BleedRows(Forecast forecast, TacticalActor actor, BleedStatus bleed)
        {
            CharacterBodyState body = actor.BodyState;
            if (body == null)
            {
                return false;
            }

            forecast.Stat("TFTV_FORECAST_HIT_POINTS", Loss(bleed.Value), true);

            foreach (string slotName in bleed.GetTargetSlotsNames())
            {
                ItemSlot slot = body.GetSlot(slotName);
                if (slot == null)
                {
                    continue;
                }

                float value = slot.GetAllDirectItems(onlyBodyparts: true)
                    .Sum(item => item.BodyPartAspect.BleedValue);

                // A share of the Hit Points row above, not a loss of its own, so not red.
                forecast.Rows.Add(new Row
                {
                    Kind = RowKind.Stat,
                    Label = slot.DisplayName,
                    Value = Mathf.RoundToInt(value).ToString(),
                    Bracketed = true,
                });
            }

            forecast.Note(Key("TFTV_BLEED_NO_DECAY"));

            return true;
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
        private static bool FireRows(Forecast forecast, TacticalActor actor, FireStatus fire)
        {
            List<KeyValuePair<string, float>> byPart = FireDamageByPart(actor, fire);

            if (byPart.Count == 0)
            {
                return false;
            }

            List<Row> parts = byPart
                .Select(part => new Row
                {
                    Kind = RowKind.Stat,
                    Label = part.Key,
                    Value = Loss(part.Value),
                    IsLoss = part.Value >= 0.5f,
                    Bracketed = true,
                })
                .ToList();

            float average = byPart.Average(part => part.Value);

            forecast.Stat("TFTV_FORECAST_HIT_POINTS", Loss(average), average >= 0.5f);
            forecast.Rows.AddRange(parts);
            forecast.Note(Key("TFTV_BURNING_FORECAST_AVERAGE"));
            forecast.Note(Key("TFTV_BURNING_RECALCULATED"));

            return true;
        }

        /// <summary>What each health slot takes from the fire: its damage less the slot's armor, scaled by resistance.</summary>
        private static List<KeyValuePair<string, float>> FireDamageByPart(TacticalActor actor, FireStatus fire)
        {
            List<KeyValuePair<string, float>> parts = new List<KeyValuePair<string, float>>();

            CharacterBodyState body = actor.BodyState;
            if (body == null)
            {
                return parts;
            }

            float resistance = 1f;
            var fireType = actor.TacticalLevel?.VoxelMatrix?.VoxelMatrixData?.FireDamageTypeDef;
            if (fireType != null)
            {
                resistance = actor.GetDamageMultiplierFor(fireType);
            }

            float damage = fire.FullDamageValue;

            foreach (ItemSlot slot in body.GetHealthSlots())
            {
                float armour = (float)slot.GetArmor().Value;
                parts.Add(new KeyValuePair<string, float>(slot.DisplayName, Mathf.Max(0f, damage - armour) * resistance));
            }

            return parts;
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
        private static bool ParalysisRows(Forecast forecast, TacticalActor actor, ParalysisDamageOverTimeStatus paralysis)
        {
            float threshold = GetParalysisThreshold(paralysis);
            float total = paralysis.FullDamageValue;

            if (threshold > 0f)
            {
                float ratio = total / threshold;

                // The effect floors to the quarter below, so 0.6 costs 50%, not 60%.
                int percent = ratio >= 1f ? 100 : Mathf.FloorToInt(ratio * 4f) * 25;

                forecast.Stat("TFTV_FORECAST_ACTION_POINTS", Loss(percent, "%"), percent > 0);

                if (ratio >= 1f)
                {
                    forecast.Warning(Key("TFTV_PARALYSIS_PARALYSED"));
                }
            }

            forecast.Level(paralysis.Value, Mathf.Max(0f, paralysis.Value - DecayPerTurn(actor, paralysis)));

            if (threshold > 0f)
            {
                forecast.Note(TFTVCommonMethods.FormatKey(
                    "TFTV_PARALYSIS_THRESHOLD",
                    Mathf.RoundToInt(total),
                    Mathf.RoundToInt(threshold)));

                if (total < threshold * 0.25f)
                {
                    forecast.Note(TFTVCommonMethods.FormatKey(
                        "TFTV_PARALYSIS_AP_NONE",
                        Mathf.CeilToInt(threshold * 0.25f - total)));
                }
            }

            return true;
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
        private static bool StunRows(Forecast forecast, StunStatus stun)
        {
            forecast.Stat(
                "TFTV_FORECAST_ACTION_POINTS",
                Loss(stun.StunStatusDef.ActionPointsReduction * 100f, "%"),
                true);
            forecast.Note(Key("TFTV_STUN_OF_MAXIMUM"));

            return true;
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
        private static bool VirusRows(Forecast forecast, TacticalActor actor, InfectedStatus infected)
        {
            float damage = infected.FullDamageValue;
            float will = actor.CharacterStats?.WillPoints ?? 0f;

            forecast.Stat("TFTV_FORECAST_WILL_POINTS", Loss(damage), true);

            WillDamageEffectDef willEffect = infected.DamageEffect?.DamageEffectDef as WillDamageEffectDef;

            if (willEffect != null
                && willEffect.StatusOnWillpointsReachingZero != null
                && will <= damage)
            {
                forecast.Warning(Key("TFTV_VIRUS_ZERO_WILL"));
            }

            forecast.Level(infected.Value, Mathf.Max(0f, infected.Value - DecayPerTurn(actor, infected)));

            return true;
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
        /// cost. The Will comes from TFTV's own patch in VariousAdjustments, not from vanilla.
        /// </summary>
        private static bool PoisonRows(Forecast forecast, TacticalActor actor, DamageOverTimeStatus poison)
        {
            forecast.Stat("TFTV_FORECAST_HIT_POINTS", Loss(poison.FullDamageValue), true);
            forecast.Stat("TFTV_FORECAST_WILL_POINTS", Loss(PoisonWillDamage), true);
            forecast.Level(poison.Value, Mathf.Max(0f, poison.Value - DecayPerTurn(actor, poison)));

            return true;
        }

        #endregion
    }
}
