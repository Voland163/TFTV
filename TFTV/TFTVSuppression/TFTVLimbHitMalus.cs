using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Limb hit malus: hits that get past armour on a head, an arm or a leg lower the stat tied to
    /// that part for a while. It has its own option (Config.LimbHitMalus).
    ///
    /// Each part has a severity (0-100%): every projectile or strike of standard damage that gets
    /// through adds its raw damage (before armour) as a share of the part's max HP. Fire adds
    /// severity even through armour - this is the fire rework's linked debuff. The malus is severity
    /// times the part's maximum hit malus below, and lasts until the end of the character's next turn
    /// after the last hit. A disabled part replaces its severity with double the maximum hit malus
    /// for as long as it stays disabled; a part restored after being disabled keeps a residual
    /// malus for the rest of the mission.
    ///
    /// Malus sizes are counted in "units" of a part's maximum hit malus: a hit adds up to 1 unit,
    /// a disabled part is 2, each restoration leaves 0.2 (10% of the disabled malus) up to 1.
    /// </summary>
    internal static class TFTVLimbHitMalus
    {
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal enum LimbRole
        {
            None,
            /// <summary>Heads and sensory organs: Perception and the Willpower the part provides.</summary>
            Sensory,
            /// <summary>Arms, claws and weapon mounts: Accuracy.</summary>
            Manipulator,
            /// <summary>Legs, tails and other movement parts: the Speed the part provides.</summary>
            Locomotion,
        }

        /// <summary>At 100% severity: this share of Perception.</summary>
        internal const float PerceptionHitCap = 0.25f;
        /// <summary>At 100% severity: this share of the Willpower the part provides.</summary>
        internal const float WillpowerHitCap = 0.5f;
        /// <summary>At 100% severity: this many Accuracy points (0.25 = 25%).</summary>
        internal const float AccuracyHitCap = 0.25f;
        /// <summary>At 100% severity: this share of the Speed the part provides.</summary>
        internal const float SpeedHitCap = 0.5f;

        private const float DisabledUnits = 2f;
        private const float ResidualUnitsPerRestoration = 0.2f;
        private const float MaxResidualUnits = 1f;
        private const float IgnorePainFactor = 0.5f;

        private const string PerceptionStat = "Perception";
        private const string WillpowerStat = "Willpower";
        private const string SpeedStat = "Speed";
        private const string AccuracyStat = "Accuracy";

        private static readonly string[] AffectedStats = { PerceptionStat, WillpowerStat, SpeedStat, AccuracyStat };

        /// <summary>
        /// Standard damage types that are not blast, projectile, bash or melee. Every other standard
        /// type adds severity - including clones such as TFTV's own melee type (TFTV_Melee_damageType,
        /// which TFTVMeleeDamage puts on every melee weapon). Fire is handled on its own.
        /// </summary>
        private static readonly HashSet<string> ExcludedStandardDamageTypeNames = new HashSet<string>
        {
            "Fire_StandardDamageTypeEffectDef",
            "Shred_StandardDamageTypeEffectDef",
            "Healing_StandardDamageTypeEffectDef",
            "EMP_StandardDamageTypeEffectDef",
            "Corruption_StandardDamageTypeEffectDef",
            "Strained_StandardDamageTypeEffectDef",
            "TFTV_PsychicStandard_damageType",
        };

        internal static LimbMalusStatusDef StatusDef { get; set; }

        internal static bool IsEnabled => TFTVMain.Main?.Config?.LimbHitMalus ?? false;

        #region parts

        internal static LimbRole GetRole(ItemSlot slot)
        {
            ItemSlotDef def = slot?.ItemSlotDef;

            if (def == null)
            {
                return LimbRole.None;
            }

            string name = (def.SlotName ?? string.Empty).ToLowerInvariant();

            if (name.Contains("head"))
            {
                return LimbRole.Sensory;
            }

            if (def.ProvidesHand
                || (name.Contains("arm") && !name.Contains("armo"))
                || name.Contains("hand")
                || name.Contains("claw")
                || name.Contains("weapon")
                || name.Contains("mount"))
            {
                return LimbRole.Manipulator;
            }

            if (name.Contains("leg") || name.Contains("tail") || name.Contains("foot") || name.Contains("feet"))
            {
                return LimbRole.Locomotion;
            }

            // Alien weapon mounts that are named after where they sit (an abdomen launcher, say).
            if (slot.GetAllDirectItems(false).Any(item => item is Weapon))
            {
                return LimbRole.Manipulator;
            }

            return LimbRole.None;
        }

        /// <summary>
        /// The flat amount of a stat the items in this part give the character - a helmet's Willpower,
        /// a pair of legs' Speed - read from the part definitions, so it is the same whether or not
        /// the part is currently disabled.
        /// </summary>
        internal static float GetProvidedStat(ItemSlot slot, string statName)
        {
            float total = 0f;

            foreach (TacticalItem item in slot.GetAllDirectItems(false))
            {
                BodyPartAspectDef aspectDef = item?.BodyPartAspect?.BodyPartAspectDef;

                if (aspectDef == null)
                {
                    continue;
                }

                foreach (StatModification modification in aspectDef.GetAllStatModifications(null, false))
                {
                    if (modification.Modification == StatModificationType.Add && modification.StatName == statName)
                    {
                        total += modification.Value;
                    }
                }
            }

            return Mathf.Max(0f, total);
        }

        private static IEnumerable<ItemSlot> GetMappedSlots(TacticalActor actor)
        {
            return actor.BodyState?.GetHealthSlots()?.Where(slot => GetRole(slot) != LimbRole.None) ?? Enumerable.Empty<ItemSlot>();
        }

        /// <summary>Ignore Pain (and TFTV's Derealization, built on it) freezes body-part stats.</summary>
        internal static bool HasIgnorePain(TacticalActor actor)
        {
            return actor?.Status?.GetStatus<FreezeAspectStatsStatus>() != null;
        }

        internal static bool IsEligible(TacticalActor actor)
        {
            return actor != null
                && actor.IsAlive
                && actor.BodyState != null
                && actor.Status != null
                && actor.CharacterStats != null
                && !actor.HasGameTag(Shared.SharedGameTags.VehicleTag)
                && !TFTVSuppression.SuppressionRuntime.IsSuppressionExcluded(actor);
        }

        #endregion

        #region malus calculation

        internal sealed class PartMalus
        {
            internal ItemSlot Slot;
            internal LimbRole Role;
            internal bool Disabled;
            internal float Severity;
            internal int EndsLeft;
            internal int Restorations;
            /// <summary>Malus in units of the part's maximum hit malus, after Ignore Pain.</summary>
            internal float Units;
            internal float PerceptionShare;
            internal float WillpowerLoss;
            internal float SpeedLoss;
            internal float AccuracyLoss;
        }

        internal sealed class MalusTotals
        {
            internal readonly List<PartMalus> Parts = new List<PartMalus>();
            internal bool IgnorePain;
            internal float PerceptionShare;
            internal float WillpowerLoss;
            internal float SpeedLoss;
            internal float AccuracyLoss;
            /// <summary>Hands the selected weapon needs, or -1 when it rides on a body part.</summary>
            internal int WeaponHands;

            internal bool AnyActive => Parts.Any(part => part.Units > 0f);
        }

        internal static MalusTotals Calculate(TacticalActor actor, LimbMalusStatus status)
        {
            MalusTotals totals = new MalusTotals { IgnorePain = HasIgnorePain(actor) };

            if (actor?.BodyState == null || status == null)
            {
                return totals;
            }

            foreach (ItemSlot slot in GetMappedSlots(actor))
            {
                string slotName = slot.GetSlotName();
                PartMalus part = new PartMalus
                {
                    Slot = slot,
                    Role = GetRole(slot),
                    Disabled = !slot.Enabled,
                    Severity = Mathf.Clamp01(status.GetSeverity(slotName)),
                    EndsLeft = status.GetEndsLeft(slotName),
                    Restorations = status.GetRestorations(slotName),
                };

                part.Units = part.Disabled
                    ? DisabledUnits
                    : Mathf.Min(MaxResidualUnits, ResidualUnitsPerRestoration * part.Restorations) + part.Severity;

                if (totals.IgnorePain)
                {
                    part.Units *= IgnorePainFactor;
                }

                // A disabled part already loses the stats its items give, through vanilla's
                // BodyPartAspect.UnapplyStats - that is exactly "100% of what the part provides".
                // Ignore Pain stops that removal, so then the (halved) malus has to be applied here.
                bool vanillaRemovedStats = part.Disabled && !totals.IgnorePain;

                switch (part.Role)
                {
                    case LimbRole.Sensory:
                        part.PerceptionShare = part.Units * PerceptionHitCap;
                        part.WillpowerLoss = vanillaRemovedStats ? 0f : part.Units * WillpowerHitCap * GetProvidedStat(slot, WillpowerStat);
                        break;
                    case LimbRole.Manipulator:
                        part.AccuracyLoss = part.Units * AccuracyHitCap;
                        break;
                    case LimbRole.Locomotion:
                        part.SpeedLoss = vanillaRemovedStats ? 0f : part.Units * SpeedHitCap * GetProvidedStat(slot, SpeedStat);
                        break;
                }

                totals.Parts.Add(part);
            }

            totals.PerceptionShare = Mathf.Min(1f, totals.Parts.Sum(part => part.PerceptionShare));
            totals.WillpowerLoss = totals.Parts.Sum(part => part.WillpowerLoss);
            totals.SpeedLoss = totals.Parts.Sum(part => part.SpeedLoss);
            totals.AccuracyLoss = CombineArms(actor, totals);

            return totals;
        }

        /// <summary>
        /// Arms and weapon handedness. A weapon mounted on a body part takes that part's malus. A
        /// two-handed weapon takes the maluses of both arms added up. A one-handed weapon is only
        /// affected when every hand has a malus, and then by the lowest one.
        /// </summary>
        private static float CombineArms(TacticalActor actor, MalusTotals totals)
        {
            List<PartMalus> arms = totals.Parts.Where(part => part.Role == LimbRole.Manipulator).ToList();

            if (arms.Count == 0)
            {
                return 0f;
            }

            Weapon weapon = actor.Equipments?.SelectedWeapon;

            if (weapon == null)
            {
                totals.WeaponHands = 0;
                return 0f;
            }

            ItemSlot mount = weapon.ParentItemSlot;
            PartMalus mountedOn = mount == null ? null : arms.FirstOrDefault(arm => arm.Slot == mount);

            if (mountedOn != null)
            {
                totals.WeaponHands = -1;
                return Mathf.Min(1f, mountedOn.AccuracyLoss);
            }

            List<PartMalus> hands = arms.Where(arm => arm.Slot.ItemSlotDef.ProvidesHand).ToList();

            if (hands.Count == 0)
            {
                hands = arms;
            }

            totals.WeaponHands = weapon.HandsToUse;

            if (weapon.HandsToUse >= 2)
            {
                return Mathf.Min(1f, hands.Sum(arm => arm.AccuracyLoss));
            }

            if (hands.Count == 1)
            {
                return Mathf.Min(1f, hands[0].AccuracyLoss);
            }

            return hands.All(arm => arm.AccuracyLoss > 0f) ? Mathf.Min(1f, hands.Min(arm => arm.AccuracyLoss)) : 0f;
        }

        /// <summary>Nearest whole point, halves up (22.5 → 23), unlike Mathf.Round.</summary>
        private static float RoundHalfUp(float value)
        {
            return Mathf.Floor(value + 0.5f);
        }

        #endregion

        #region stats

        internal static void Refresh(LimbMalusStatus status, bool loading = false)
        {
            try
            {
                TacticalActor actor = status?.TacticalActor;

                if (actor == null || status.UnapplyRequested)
                {
                    return;
                }

                if (loading)
                {
                    // Whatever a save brought back of our own modifications is rebuilt from scratch.
                    RemoveAnyLimbMalusModifications(actor);
                }

                if (!IsEnabled || !actor.IsAlive)
                {
                    RemoveStats(actor, status);
                    status.RequestUnapply(status.StatusComponent);
                    return;
                }

                MalusTotals totals = Calculate(actor, status);

                if (!totals.AnyActive)
                {
                    RemoveStats(actor, status);
                    status.RequestUnapply(status.StatusComponent);
                    return;
                }

                ApplyStats(actor, status, totals);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Rebuilds every character's malus once the level is up - after a save is loaded the
        /// statuses come back before the stats and equipment they read are ready.
        /// </summary>
        internal static void RefreshAll(PhoenixPoint.Tactical.Levels.TacticalLevelController controller)
        {
            try
            {
                if (controller?.Map == null)
                {
                    return;
                }

                foreach (TacticalActor actor in controller.Map.GetActors<TacticalActor>(null).ToList())
                {
                    if (actor?.Status == null)
                    {
                        continue;
                    }

                    foreach (LimbMalusStatus status in actor.Status.GetStatuses<LimbMalusStatus>().ToList())
                    {
                        Refresh(status, loading: true);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyStats(TacticalActor actor, LimbMalusStatus status, MalusTotals totals)
        {
            RemoveModifications(actor, status);

            CharacterStatsAdd(actor, status, PerceptionStat, current => RoundHalfUp(current * (1f - totals.PerceptionShare)) - current);
            CharacterStatsAdd(actor, status, WillpowerStat, current => RoundHalfUp(current - totals.WillpowerLoss) - current);
            CharacterStatsAdd(actor, status, SpeedStat, current => RoundHalfUp(current - totals.SpeedLoss) - current);
            CharacterStatsAdd(actor, status, AccuracyStat, current => -totals.AccuracyLoss);

            // Will Points and Action Points take their maximums from Willpower and Speed here.
            actor.UpdateStats();
        }

        private static void CharacterStatsAdd(TacticalActor actor, LimbMalusStatus status, string statName, Func<float, float> delta)
        {
            BaseStat stat = actor.Status.GetStat(statName);

            if (stat == null)
            {
                return;
            }

            // Never below 0 (or whatever the stat's own floor makes of that).
            float current = stat;
            float change = Mathf.Max(delta(current), -Mathf.Max(0f, current));

            if (change > -0.001f)
            {
                return;
            }

            stat.AddStatModification(new StatModification(StatModificationType.Add, statName, change, status, change), true);
            stat.ReapplyModifications();
        }

        internal static void RemoveStats(TacticalActor actor, LimbMalusStatus status)
        {
            if (actor?.Status == null)
            {
                return;
            }

            RemoveModifications(actor, status);
            actor.UpdateStats();
        }

        private static void RemoveModifications(TacticalActor actor, LimbMalusStatus status)
        {
            foreach (string statName in AffectedStats)
            {
                BaseStat stat = actor.Status.GetStat(statName);

                if (stat == null)
                {
                    continue;
                }

                stat.RemoveStatModificationsWithSource(status, true);
                stat.ReapplyModifications();
            }
        }

        private static void RemoveAnyLimbMalusModifications(TacticalActor actor)
        {
            foreach (string statName in AffectedStats)
            {
                BaseStat stat = actor.Status.GetStat(statName);

                if (stat?.Modifications == null)
                {
                    continue;
                }

                List<object> sources = stat.Modifications
                    .Select(modification => modification.Source)
                    .Where(source => source is LimbMalusStatus || source is LimbMalusStatusDef)
                    .Distinct()
                    .ToList();

                foreach (object source in sources)
                {
                    stat.RemoveStatModificationsWithSource(source, true);
                }

                if (sources.Count > 0)
                {
                    stat.ReapplyModifications();
                }
            }
        }

        internal static IEnumerable<string> GetAffectedSlotNames(LimbMalusStatus status)
        {
            TacticalActor actor = status?.TacticalActor;

            if (actor == null)
            {
                return Enumerable.Empty<string>();
            }

            return Calculate(actor, status).Parts.Where(part => part.Units > 0f).Select(part => part.Slot.GetSlotName()).ToList();
        }

        #endregion

        #region events

        private static LimbMalusStatus GetStatus(TacticalActor actor)
        {
            return actor?.Status?.GetStatuses<LimbMalusStatus>().FirstOrDefault(status => !status.UnapplyRequested);
        }

        private static LimbMalusStatus GetOrApplyStatus(TacticalActor actor)
        {
            LimbMalusStatus status = GetStatus(actor);

            if (status != null || StatusDef == null)
            {
                return status;
            }

            return actor.Status.ApplyStatus(StatusDef) as LimbMalusStatus;
        }

        /// <summary>
        /// The current turn does not count when the hit lands in the character's own turn (reaction
        /// fire, a trap, a fire tick), so the severity then lasts through two of its turn ends.
        /// </summary>
        private static int TurnEndsFor(TacticalActor actor)
        {
            PhoenixPoint.Tactical.Levels.TacticalLevelController level = actor.TacticalLevel;
            return level != null && level.CurrentFaction == actor.TacticalFaction ? 2 : 1;
        }

        private static void AddSeverity(TacticalActor actor, ItemSlot slot, float amount)
        {
            if (amount <= 0f || !slot.Enabled)
            {
                return;
            }

            LimbMalusStatus status = GetOrApplyStatus(actor);

            if (status == null)
            {
                return;
            }

            string slotName = slot.GetSlotName();
            float severity = Mathf.Min(1f, status.GetSeverity(slotName) + amount);
            status.SetSeverity(slotName, severity, TurnEndsFor(actor));
            Refresh(status);
        }

        private static float MaxHealth(ItemSlot slot)
        {
            StatusStat health = slot.GetHealth();
            return health == null ? 0f : (float)health.Max;
        }

        /// <summary>One projectile or strike of standard damage that got through armour on this part.</summary>
        internal static void OnStandardHit(ItemSlot slot, float rawDamage)
        {
            if (!IsEnabled || rawDamage <= 0f || GetRole(slot) == LimbRole.None)
            {
                return;
            }

            TacticalActor actor = slot.GetActor() as TacticalActor;

            if (!IsEligible(actor))
            {
                return;
            }

            float maxHealth = MaxHealth(slot);

            if (maxHealth > 0f)
            {
                AddSeverity(actor, slot, rawDamage / maxHealth);
            }
        }

        /// <summary>
        /// A fire tick on a burning part, the application tick included. Counts even when armour
        /// blocks all of it - this is what gives incendiaries a use against armour. The damage passed
        /// in is after fire resistance.
        /// </summary>
        internal static void OnFireTick(TacticalActor actor, ItemSlot slot, float fireDamage)
        {
            if (!IsEnabled || fireDamage <= 0f || GetRole(slot) == LimbRole.None || !IsEligible(actor))
            {
                return;
            }

            float maxHealth = MaxHealth(slot);

            if (maxHealth > 0f)
            {
                AddSeverity(actor, slot, fireDamage / maxHealth);
            }
        }

        /// <summary>Starting the turn in fire: the head, arms and legs all take severity from the ground fire damage.</summary>
        internal static void OnGroundFireTurnStart(TacticalActor actor, float fireDamage)
        {
            if (!IsEnabled || fireDamage <= 0f || !IsEligible(actor))
            {
                return;
            }

            foreach (ItemSlot slot in GetMappedSlots(actor).ToList())
            {
                float maxHealth = MaxHealth(slot);

                if (maxHealth > 0f)
                {
                    AddSeverity(actor, slot, fireDamage / maxHealth);
                }
            }
        }

        private static void OnSlotDisabled(ItemSlot slot)
        {
            if (!IsEnabled || GetRole(slot) == LimbRole.None)
            {
                return;
            }

            TacticalActor actor = slot.GetActor() as TacticalActor;

            if (!IsEligible(actor) || !actor.HasEnteredPlay || actor.TacticalLevel == null || !actor.TacticalLevel.TurnIsPlaying)
            {
                return;
            }

            LimbMalusStatus status = GetOrApplyStatus(actor);

            if (status == null)
            {
                return;
            }

            // The disabled malus replaces the hit severity, and further hits add nothing until restored.
            status.ClearSeverity(slot.GetSlotName());
            Refresh(status);
        }

        private static void OnSlotRestored(ItemSlot slot)
        {
            if (!IsEnabled || GetRole(slot) == LimbRole.None)
            {
                return;
            }

            TacticalActor actor = slot.GetActor() as TacticalActor;

            // Only a restoration during play counts; stats being set up or loaded are not one.
            if (!IsEligible(actor) || !actor.HasEnteredPlay || actor.TacticalLevel == null || !actor.TacticalLevel.TurnIsPlaying)
            {
                return;
            }

            LimbMalusStatus status = GetOrApplyStatus(actor);

            if (status == null)
            {
                return;
            }

            status.AddRestoration(slot.GetSlotName());
            Refresh(status);
        }

        internal static void OnEndTurn(LimbMalusStatus status)
        {
            status.CountDownTurnEnd();
            Refresh(status);
        }

        /// <summary>
        /// Parts that were already disabled when the mission started (or when the option was turned
        /// on) never sent a disabled event; pick them up at the start of the character's turn.
        /// </summary>
        private static void PickUpDisabledParts(TacticalActor actor)
        {
            if (!IsEnabled || !IsEligible(actor) || GetStatus(actor) != null)
            {
                return;
            }

            if (GetMappedSlots(actor).Any(slot => !slot.Enabled))
            {
                LimbMalusStatus status = GetOrApplyStatus(actor);

                if (status != null)
                {
                    Refresh(status);
                }
            }
        }

        private static bool IsHitDamageType(DamageTypeBaseEffectDef type)
        {
            return type is StandardDamageTypeEffectDef && !ExcludedStandardDamageTypeNames.Contains(type.name);
        }

        private static bool IsStandardDamage(DamageResult result)
        {
            return IsHitDamageType(result.DamageTypeDef)
                || (result.RelatedDamageTypeDefs != null && result.RelatedDamageTypeDefs.Any(IsHitDamageType));
        }

        #endregion

        #region patches

        /// <summary>
        /// Where one projectile's (or strike's) damage lands on one body part. Each projectile is its
        /// own accumulation, so a burst adds severity once per bullet that hits, and a blast once per
        /// part it reaches. AmountApplied is the damage past armour before the part's HP caps it;
        /// adding back what armour stopped gives the raw damage the spec scales by.
        /// </summary>
        [HarmonyPatch(typeof(DamageAccumulation.TargetData), nameof(DamageAccumulation.TargetData.ApplyToTarget))]
        internal static class DamageAccumulation_TargetData_ApplyToTarget_Patch
        {
            private static void Postfix(DamageAccumulation.TargetData __instance)
            {
                try
                {
                    if (!IsEnabled || !(__instance.Target is ItemSlot slot) || __instance.AmountApplied <= 0f)
                    {
                        return;
                    }

                    DamageResult result = __instance.DamageResult;

                    if (!IsStandardDamage(result))
                    {
                        return;
                    }

                    OnStandardHit(slot, __instance.AmountApplied + Mathf.Max(0f, result.ArmorMitigatedDamage));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlot), "OnHealthReachingZero")]
        internal static class ItemSlot_OnHealthReachingZero_Patch
        {
            private static void Postfix(ItemSlot __instance)
            {
                try
                {
                    OnSlotDisabled(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlot), "OnHealthNoLongerZero")]
        internal static class ItemSlot_OnHealthNoLongerZero_Patch
        {
            private static void Postfix(ItemSlot __instance)
            {
                try
                {
                    OnSlotRestored(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.StartTurn))]
        internal static class TacticalActor_StartTurn_LimbMalus_Patch
        {
            private static void Postfix(TacticalActor __instance)
            {
                try
                {
                    PickUpDisabledParts(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion
    }
}
