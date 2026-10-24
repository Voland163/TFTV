using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
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
    /// Wounds under Harsh Attrition (see IsEnabled): disabled arms and heads, and the limb hit malus.
    ///
    /// Disabled arms no longer take weapons away. Every arm keeps providing its hand (as under Ignore
    /// Pain), and instead each disabled arm doubles a two-handed weapon's spread, while a one-handed
    /// weapon's spread doubles only once every hand arm is disabled. Melee damage is divided by the same
    /// factor. A weapon that is itself a body part still goes down with its part, as in vanilla.
    ///
    /// A disabled head still loses what its items give (vanilla), except that half its Willpower comes
    /// back, and it costs 10 more Perception and a third more spread on every weapon.
    ///
    /// Hit malus: each part has a severity (0-100%): every projectile or strike of standard damage that
    /// gets through adds its raw damage (before armour) as a share of the part's max HP. Fire adds severity
    /// even through armour - this is the fire rework's linked debuff. The malus is severity times the
    /// part's maximum hit malus below, and lasts until the end of the character's next turn after the last
    /// hit. A restored part keeps a residual malus for the rest of the mission. Disabled arms and heads
    /// carry no hit malus: the rules above replace it. A disabled leg counts double.
    ///
    /// Ignore Pain halves all of it. All wound spread together is capped at x4.
    ///
    /// Malus sizes are counted in "units" of a part's maximum hit malus: a hit adds up to 1 unit, a
    /// disabled leg is 2, each restoration leaves 0.2 up to 1.
    /// </summary>
    internal static class TFTVLimbHitMalus
    {
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal enum LimbRole
        {
            None,
            /// <summary>Heads and sensory organs: Perception, the Willpower the part provides, and aim.</summary>
            Sensory,
            /// <summary>Arms, claws and weapon mounts: weapon spread and melee damage.</summary>
            Manipulator,
            /// <summary>Legs, tails and other movement parts: the Speed the part provides.</summary>
            Locomotion,
        }

        /// <summary>At 100% severity on a head: Perception lost (half of a disabled head's).</summary>
        internal const float PerceptionHitLoss = 5f;
        /// <summary>At 100% severity on a head: this share of the Willpower the head provides (half of a disabled head's).</summary>
        internal const float WillpowerHitShare = 0.25f;
        /// <summary>At 100% severity on a head: spread of every weapon +1/6 (half of a disabled head's).</summary>
        internal const float HeadHitSpreadIncrease = 1f / 6f;
        /// <summary>
        /// At 100% severity on an arm: weapon spread (the aiming cone's angle) +25%. Arms work on spread directly rather
        /// than on the Accuracy stat: vanilla's cone is SpreadDegrees / (1 + Accuracy) and soldiers start at an
        /// Accuracy of 0, so a stat malus either did nothing (the stat may not go below 0) or, allowed below 0,
        /// grew without bound (-0.5 doubles the cone, -1 makes it infinite).
        /// </summary>
        internal const float SpreadHitIncrease = 0.25f;
        /// <summary>The arm hit malus alone never more than doubles weapon spread.</summary>
        internal const float MaxSpreadIncrease = 1f;
        /// <summary>At 100% severity on a leg: this share of the Speed the part provides.</summary>
        internal const float SpeedHitCap = 0.5f;

        /// <summary>Each disabled arm: x2 spread on two-handed weapons; all hand arms disabled: x2 on one-handed ones.</summary>
        internal const float DisabledArmSpreadIncrease = 1f;
        /// <summary>One-Handed Grip (drill) halves the disabled-arm extra on two-handed weapons.</summary>
        internal const float OneHandedGripFactor = 0.5f;
        /// <summary>A disabled head: x4/3 spread on every weapon.</summary>
        internal const float DisabledHeadSpreadIncrease = 1f / 3f;
        /// <summary>A disabled head: Perception lost on top of what its items gave.</summary>
        internal const float DisabledHeadPerceptionLoss = 10f;
        /// <summary>A disabled head: share of its Willpower lost (vanilla loses all of it).</summary>
        internal const float DisabledHeadWillpowerLoss = 0.5f;
        /// <summary>Wound spread from arms, heads and hits together: at most x4. Melee damage never drops below a quarter.</summary>
        internal const float MaxWoundSpreadMultiplier = 4f;

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

        /// <summary>
        /// Part of the experimental Harsh Attrition new-game option, saved with the campaign. There is no
        /// TFTVConfig field: a new one resets every player's saved settings once.
        /// </summary>
        internal static bool IsEnabled => TFTVHarshAttrition.IsEnabled;

        #region parts

        private static readonly Dictionary<ItemSlotDef, LimbRole> RolesByName = new Dictionary<ItemSlotDef, LimbRole>();

        private static LimbRole GetRoleFromName(ItemSlotDef def)
        {
            if (!RolesByName.TryGetValue(def, out LimbRole role))
            {
                string name = (def.SlotName ?? string.Empty).ToLowerInvariant();

                if (name.Contains("head"))
                {
                    role = LimbRole.Sensory;
                }
                else if (def.ProvidesHand
                    || (name.Contains("arm") && !name.Contains("armo"))
                    || name.Contains("hand")
                    || name.Contains("claw")
                    || name.Contains("weapon")
                    || name.Contains("mount"))
                {
                    role = LimbRole.Manipulator;
                }
                else if (name.Contains("leg") || name.Contains("tail") || name.Contains("foot") || name.Contains("feet"))
                {
                    role = LimbRole.Locomotion;
                }
                else
                {
                    role = LimbRole.None;
                }

                RolesByName[def] = role;
            }

            return role;
        }

        internal static LimbRole GetRole(ItemSlot slot)
        {
            ItemSlotDef def = slot?.ItemSlotDef;

            if (def == null)
            {
                return LimbRole.None;
            }

            LimbRole role = GetRoleFromName(def);

            if (role != LimbRole.None)
            {
                return role;
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

        /// <summary>
        /// What the items in this part add to a stat right now. Vanilla applies a part's stats with its
        /// BodyPartAspectDef as source and strips them when the part is disabled, unless Ignore Pain was on
        /// at that moment - so this is 0 or GetProvidedStat, whichever way the part went down.
        /// </summary>
        private static float GetAppliedStat(TacticalActor actor, ItemSlot slot, string statName)
        {
            BaseStat stat = actor.Status?.GetStat(statName);

            if (stat?.Modifications == null)
            {
                return 0f;
            }

            float total = 0f;

            foreach (TacticalItem item in slot.GetAllDirectItems(false))
            {
                BodyPartAspectDef aspectDef = item?.BodyPartAspect?.BodyPartAspectDef;

                if (aspectDef == null)
                {
                    continue;
                }

                foreach (StatModification modification in stat.Modifications)
                {
                    if (modification.Source == (object)aspectDef && modification.Modification == StatModificationType.Add)
                    {
                        total += modification.Value;
                    }
                }
            }

            return total;
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

        private static bool HasOneHandedGrip(TacticalActor actor)
        {
            PassiveModifierAbilityDef oneHandedGrip = TFTVDrills.DrillsHarmony.OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGrip;
            return oneHandedGrip != null && actor.GetAbilityWithDef<PassiveModifierAbility>(oneHandedGrip) != null;
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

        #region disabled arms and heads

        private struct DisabledParts
        {
            internal int HandArms;
            internal int DisabledHandArms;
            internal bool HeadDisabled;
        }

        /// <summary>Read live from the slots: this runs from Weapon.GetAllSpreadMultipliers, for every shot the AI considers.</summary>
        private static DisabledParts ScanDisabledParts(TacticalActor actor)
        {
            DisabledParts parts = new DisabledParts();

            foreach (ItemSlot slot in actor.BodyState.GetHealthSlots())
            {
                ItemSlotDef def = slot?.ItemSlotDef;

                if (def == null)
                {
                    continue;
                }

                if (def.ProvidesHand)
                {
                    parts.HandArms++;

                    if (!slot.Enabled)
                    {
                        parts.DisabledHandArms++;
                    }
                }
                else if (!slot.Enabled && GetRoleFromName(def) == LimbRole.Sensory)
                {
                    parts.HeadDisabled = true;
                }
            }

            return parts;
        }

        /// <summary>
        /// x2 per disabled arm on a two-handed weapon (x1.5 with Ignore Pain or One-Handed Grip, x1.25 with
        /// both); x2 on a one-handed weapon only once every hand arm is disabled. Weapons that are body parts
        /// (hands 0) are not held, so arms don't affect them.
        /// </summary>
        private static float GetDisabledArmMultiplier(TacticalActor actor, int hands, DisabledParts parts, float painFactor)
        {
            if (hands <= 0 || parts.DisabledHandArms == 0)
            {
                return 1f;
            }

            float extra = DisabledArmSpreadIncrease * painFactor;

            if (hands >= 2)
            {
                if (HasOneHandedGrip(actor))
                {
                    extra *= OneHandedGripFactor;
                }

                return Mathf.Pow(1f + extra, parts.DisabledHandArms);
            }

            return parts.DisabledHandArms >= parts.HandArms ? 1f + extra : 1f;
        }

        /// <summary>Disabled arms and hurt working arms together, for a weapon held with this many hands or mounted on this slot.</summary>
        private static float GetArmMultiplier(TacticalActor actor, ItemSlot mount, int hands, out DisabledParts parts, out float painFactor)
        {
            parts = ScanDisabledParts(actor);
            painFactor = HasIgnorePain(actor) ? IgnorePainFactor : 1f;

            float multiplier = GetDisabledArmMultiplier(actor, hands, parts, painFactor);
            LimbMalusStatus status = actor.Status.GetStatus<LimbMalusStatus>();

            if (status != null && !status.UnapplyRequested)
            {
                multiplier *= 1f + CombineArms(mount, hands, status.CachedArms);
            }

            return multiplier;
        }

        /// <summary>
        /// The spread multiplier wounds put on this weapon (1 when none): disabled arms, a disabled head and
        /// the hit malus, capped at x4. Every spread calculation goes through Weapon.GetAllSpreadMultipliers.
        /// </summary>
        internal static float GetSpreadMultiplier(TacticalActor actor, Weapon weapon)
        {
            if (!IsEnabled || weapon == null || !IsEligible(actor))
            {
                return 1f;
            }

            float multiplier = GetArmMultiplier(actor, weapon.ParentItemSlot, weapon.HandsToUse, out DisabledParts parts, out float painFactor);

            if (parts.HeadDisabled)
            {
                multiplier *= 1f + DisabledHeadSpreadIncrease * painFactor;
            }

            LimbMalusStatus status = actor.Status.GetStatus<LimbMalusStatus>();

            if (status != null && !status.UnapplyRequested)
            {
                multiplier *= 1f + status.CachedHeadSpread;
            }

            return Mathf.Min(MaxWoundSpreadMultiplier, multiplier);
        }

        /// <summary>
        /// The melee damage multiplier wounds put on a strike with this equipment (1 when none): one over the
        /// arms' part of the spread multiplier, so each disabled arm halves what it would double. Heads don't count.
        /// </summary>
        internal static float GetMeleeDamageMultiplier(TacticalActor actor, Equipment equipment)
        {
            if (!IsEnabled || !IsEligible(actor))
            {
                return 1f;
            }

            int hands = equipment != null ? equipment.HandsToUse : 1;
            float multiplier = GetArmMultiplier(actor, equipment?.ParentItemSlot, hands, out _, out _);

            return 1f / Mathf.Clamp(multiplier, 1f, MaxWoundSpreadMultiplier);
        }

        private static GameTagDef _meleeWeaponTag;

        internal static bool IsMeleeWeapon(Weapon weapon)
        {
            if (_meleeWeaponTag == null)
            {
                _meleeWeaponTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");
            }

            return weapon?.WeaponDef?.Tags != null && weapon.WeaponDef.Tags.Contains(_meleeWeaponTag);
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
            /// <summary>Hit malus in units of the part's maximum hit malus, after Ignore Pain. 0 on disabled arms and heads.</summary>
            internal float Units;
            /// <summary>Signed change to Perception (negative = lost).</summary>
            internal float PerceptionChange;
            /// <summary>Signed change to Willpower; a disabled head's is positive (half of what vanilla stripped, back).</summary>
            internal float WillpowerChange;
            internal float SpeedLoss;
            /// <summary>This working arm's (or head's) hit share of the spread increase (0.25 = +25%).</summary>
            internal float SpreadIncrease;
        }

        internal sealed class MalusTotals
        {
            internal readonly List<PartMalus> Parts = new List<PartMalus>();
            internal bool IgnorePain;
            internal float PerceptionChange;
            internal float WillpowerChange;
            internal float SpeedLoss;
            /// <summary>Head hits' spread increase on every weapon.</summary>
            internal float HeadSpreadIncrease;
            /// <summary>Hands the selected weapon needs, or -1 when it rides on a body part.</summary>
            internal int WeaponHands;

            internal bool AnyActive => Parts.Any(part => part.Units > 0f || part.Disabled);
        }

        internal static MalusTotals Calculate(TacticalActor actor, LimbMalusStatus status)
        {
            MalusTotals totals = new MalusTotals { IgnorePain = HasIgnorePain(actor) };

            if (actor?.BodyState == null || status == null)
            {
                return totals;
            }

            float painFactor = totals.IgnorePain ? IgnorePainFactor : 1f;

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

                // Disabled arms and heads have rules of their own instead of hit units.
                bool ownRule = part.Disabled && (part.Role == LimbRole.Sensory || part.Role == LimbRole.Manipulator);

                part.Units = ownRule ? 0f
                    : part.Disabled ? DisabledUnits
                    : Mathf.Min(MaxResidualUnits, ResidualUnitsPerRestoration * part.Restorations) + part.Severity;

                part.Units *= painFactor;

                switch (part.Role)
                {
                    case LimbRole.Sensory:
                        if (part.Disabled)
                        {
                            // Aim for what the rule leaves of the head's own stats, from whatever vanilla left
                            // (all of them stripped, or all kept if Ignore Pain was on when it went down).
                            float keptWillpower = GetProvidedStat(slot, WillpowerStat) * (1f - DisabledHeadWillpowerLoss * painFactor);
                            float keptPerception = totals.IgnorePain ? GetProvidedStat(slot, PerceptionStat) : 0f;

                            part.WillpowerChange = keptWillpower - GetAppliedStat(actor, slot, WillpowerStat);
                            part.PerceptionChange = keptPerception - GetAppliedStat(actor, slot, PerceptionStat) - DisabledHeadPerceptionLoss * painFactor;
                        }
                        else
                        {
                            part.PerceptionChange = -part.Units * PerceptionHitLoss;
                            part.WillpowerChange = -part.Units * WillpowerHitShare * GetProvidedStat(slot, WillpowerStat);
                            part.SpreadIncrease = part.Units * HeadHitSpreadIncrease;
                        }
                        break;
                    case LimbRole.Manipulator:
                        part.SpreadIncrease = part.Units * SpreadHitIncrease;
                        break;
                    case LimbRole.Locomotion:
                        // A disabled leg already loses exactly the Speed it provides, through vanilla's
                        // BodyPartAspect.UnapplyStats. Ignore Pain stops that removal, so then the (halved)
                        // malus has to be applied here.
                        bool vanillaRemovedStats = part.Disabled && !totals.IgnorePain;
                        part.SpeedLoss = vanillaRemovedStats ? 0f : part.Units * SpeedHitCap * GetProvidedStat(slot, SpeedStat);
                        break;
                }

                totals.Parts.Add(part);
            }

            totals.PerceptionChange = totals.Parts.Sum(part => part.PerceptionChange);
            totals.WillpowerChange = totals.Parts.Sum(part => part.WillpowerChange);
            totals.SpeedLoss = totals.Parts.Sum(part => part.SpeedLoss);
            totals.HeadSpreadIncrease = totals.Parts.Where(part => part.Role == LimbRole.Sensory).Sum(part => part.SpreadIncrease);

            Weapon selected = actor.Equipments?.SelectedWeapon;
            totals.WeaponHands = selected == null ? 0 : selected.HandsToUse;

            return totals;
        }

        /// <summary>
        /// Hit malus of the working arms (disabled arms are handled by GetDisabledArmMultiplier). A weapon
        /// mounted on a body part takes that part's malus. A two-handed weapon takes the working hand arms'
        /// maluses added up. A one-handed weapon is held in the least hurt working hand arm. Loops rather
        /// than LINQ: the AI calls this for every shot it considers.
        /// </summary>
        private static float CombineArms(ItemSlot mount, int hands, List<PartMalus> workingArms)
        {
            if (workingArms == null || workingArms.Count == 0)
            {
                return 0f;
            }

            if (mount != null)
            {
                foreach (PartMalus arm in workingArms)
                {
                    if (arm.Slot == mount)
                    {
                        return Mathf.Min(MaxSpreadIncrease, arm.SpreadIncrease);
                    }
                }
            }

            if (hands <= 0)
            {
                return 0f;
            }

            float sum = 0f;
            float least = float.MaxValue;

            foreach (PartMalus arm in workingArms)
            {
                if (!arm.Slot.ItemSlotDef.ProvidesHand)
                {
                    continue;
                }

                sum += arm.SpreadIncrease;
                least = Mathf.Min(least, arm.SpreadIncrease);
            }

            if (least == float.MaxValue)
            {
                return 0f;
            }

            return Mathf.Min(MaxSpreadIncrease, hands >= 2 ? sum : least);
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

                ApplyStats(actor, status, totals, loading);
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

        private static void ApplyStats(TacticalActor actor, LimbMalusStatus status, MalusTotals totals, bool loading)
        {
            SetModification(actor, status, PerceptionStat, current => RoundHalfUp(current + totals.PerceptionChange) - current, loading);
            SetModification(actor, status, WillpowerStat, current => RoundHalfUp(current + totals.WillpowerChange) - current, loading);
            SetModification(actor, status, SpeedStat, current => RoundHalfUp(current - totals.SpeedLoss) - current, loading);
            // Arms widen weapon spread instead (GetSpreadMultiplier); this clears what older versions applied.
            SetModification(actor, status, AccuracyStat, null, loading);
            status.CachedArms = totals.Parts.Where(part => part.Role == LimbRole.Manipulator && !part.Disabled).ToList();
            status.CachedHeadSpread = totals.HeadSpreadIncrease;

            // Will Points and Action Points take their maximums from Willpower and Speed here.
            actor.UpdateStats();
        }

        private static readonly System.Reflection.MethodInfo OnStatChangeMethod = AccessTools.Method(typeof(BaseStat), "OnStatChange");

        /// <summary>
        /// Replaces this status's modification of one stat with the one <paramref name="delta"/> asks
        /// for (none when it is null), working from the stat's value without it. The swap itself is
        /// silent: a refresh runs on every hit, and vanilla reacts to a Willpower rise by handing out
        /// Will Points (and shows a floating "+WP"), so removing and re-adding raised a + and a -
        /// notification each time and topped Will Points up. Only a real net change is passed on.
        /// </summary>
        private static void SetModification(TacticalActor actor, LimbMalusStatus status, string statName, Func<float, float> delta, bool loading)
        {
            BaseStat stat = actor.Status.GetStat(statName);

            if (stat == null)
            {
                return;
            }

            float before = stat;

            stat.RemoveStatModificationsWithSource(status, false);
            stat.ReapplyModifications();

            // Never below 0 (or whatever the stat's own floor makes of that). A disabled head gives some
            // Willpower back, so a change can also be positive.
            float current = stat;
            float change = delta == null ? 0f : Mathf.Max(delta(current), -Mathf.Max(0f, current));

            if (Mathf.Abs(change) > 0.001f)
            {
                stat.AddStatModification(new StatModification(StatModificationType.Add, statName, change, status, change), false);
                stat.ReapplyModifications();
            }

            float after = stat;

            if (loading || Mathf.Abs(after - before) < 0.001f)
            {
                return;
            }

            if (statName == WillpowerStat)
            {
                OnWillpowerChanged(actor, status, before, after);
            }
            else
            {
                OnStatChangeMethod?.Invoke(stat, new object[] { StatChangeType.Value, before, after });
            }
        }

        /// <summary>
        /// Will Points follow the malus only as far as it takes them: a lower Willpower trims them to
        /// the new maximum, and a higher one gives back what was trimmed, never more.
        /// </summary>
        private static void OnWillpowerChanged(TacticalActor actor, LimbMalusStatus status, float before, float after)
        {
            StatusStat willPoints = actor.CharacterStats?.WillPoints;

            if (willPoints == null)
            {
                actor.UpdateStats();
                return;
            }

            float pointsBefore = willPoints;
            actor.UpdateStats();

            if (after < before)
            {
                status.WillPointsTrimmed += Mathf.Max(0f, pointsBefore - willPoints);
                return;
            }

            float giveBack = Mathf.Min(after - before, status.WillPointsTrimmed);

            if (giveBack > 0.001f)
            {
                status.WillPointsTrimmed -= giveBack;
                willPoints.AddRestrictedToMax(giveBack);
            }
        }

        internal static void RemoveStats(TacticalActor actor, LimbMalusStatus status)
        {
            if (actor?.Status == null)
            {
                return;
            }

            foreach (string statName in AffectedStats)
            {
                SetModification(actor, status, statName, null, loading: false);
            }

            status.CachedArms = null;
            status.CachedHeadSpread = 0f;
            actor.UpdateStats();
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
                    stat.RemoveStatModificationsWithSource(source, false);
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

            return Calculate(actor, status).Parts.Where(part => part.Units > 0f || part.Disabled).Select(part => part.Slot.GetSlotName()).ToList();
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

            // The disabled rules replace the hit severity, and further hits add nothing until restored.
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
        /// Parts that were already disabled when the mission started never sent a disabled event; pick
        /// them up at the start of the character's turn.
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

        /// <summary>
        /// Vanilla counts a hand only from an enabled arm unless Ignore Pain is on; this makes every arm count,
        /// so RecalculateUsableHands, Equipment.IsUsable, Bash and the equipment bar all keep the weapons usable.
        /// A weapon that is a body part still goes down with it: IsUsable also checks the item's own Enabled.
        /// TFTV's One-Handed Grip prefix on RecalculateUsableHands calls this too, so it sees two hands and
        /// never applies its accuracy penalty.
        /// </summary>
        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetHandsProvided))]
        internal static class ItemSlot_GetHandsProvided_Patch
        {
            private static void Postfix(ItemSlot __instance, ref int __result)
            {
                try
                {
                    if (!IsEnabled || __result > 0 || __instance?.ItemSlotDef == null || !__instance.ItemSlotDef.ProvidesHand)
                    {
                        return;
                    }

                    if (IsEligible(__instance.GetActor() as TacticalActor))
                    {
                        __result = 1;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Every spread calculation goes through here: the aiming cone and reticle, the shot itself
        /// (CreateTrajectory) and the AI's shot evaluation.
        /// </summary>
        [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetAllSpreadMultipliers))]
        internal static class Weapon_GetAllSpreadMultipliers_LimbMalus_Patch
        {
            private static void Postfix(Weapon __instance, TacticalActor sourceActor, ref float __result)
            {
                try
                {
                    __result *= GetSpreadMultiplier(sourceActor, __instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// The attacker's outgoing damage multiplier for one damage accumulation, read when standard damage is
        /// applied and when it is predicted for the UI. Source is the damage dealer: a melee weapon, or the
        /// Bash ability itself.
        /// </summary>
        [HarmonyPatch(typeof(DamageAccumulation), "GetSourceDamageMultiplier")]
        internal static class DamageAccumulation_GetSourceDamageMultiplier_Patch
        {
            /// <summary>BashAbility.BashEquipment is not public.</summary>
            private static readonly System.Reflection.MethodInfo BashEquipmentGetter = AccessTools.PropertyGetter(typeof(BashAbility), "BashEquipment");

            private static void Postfix(DamageAccumulation __instance, ref float __result)
            {
                try
                {
                    if (!IsEnabled || !(__instance.SourceActor is TacticalActor actor))
                    {
                        return;
                    }

                    if (__instance.Source is Weapon weapon && IsMeleeWeapon(weapon))
                    {
                        __result *= GetMeleeDamageMultiplier(actor, weapon);
                    }
                    else if (__instance.Source is BashAbility bash)
                    {
                        __result *= GetMeleeDamageMultiplier(actor, BashEquipmentGetter?.Invoke(bash, null) as Equipment);
                    }
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
