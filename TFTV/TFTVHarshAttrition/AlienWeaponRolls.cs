using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewControllers.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVHarshAttrition
    {
        internal static partial class PandoranVariance
        {
            #region weapon rolls

            /// <summary>
            /// Every weapon a rolled Pandoran carries - claws, stings, vampire arms, guns, acid - gets its own damage roll:
            /// each damage keyword's value (not Piercing or Shredding, which are about armour) times the roll, rounded,
            /// never below 1. Umbras roll wider. The roll is kept per weapon def on the creature's status and made the
            /// first time the weapon is looked at, so a weapon grown back later gets one too.
            ///
            /// The tooltip of what a creature carries shows that weapon's rolled values once the player has seen it attack
            /// with it. Before that it shows the template's values with a "?" after vivisection, and nothing before it.
            /// </summary>
            internal const float WeaponMinRoll = 0.85f;
            internal const float WeaponMaxRoll = 1.15f;

            private static float NextWeaponRoll(TacticalActorBase actor)
            {
                return IsUmbra(actor) ? RollBetween(UmbraMinDamageRoll, UmbraMaxDamageRoll) : RollBetween(WeaponMinRoll, WeaponMaxRoll);
            }

            /// <summary>
            /// The status whose weapons are rolled: Harsh Attrition on and the creature rolled. A creature rolled before the
            /// weapon rolls (a tactical save) carries a creature-wide melee roll instead: it moves to weapon rolls here, the
            /// first time anything asks, before any damage is worked out, so the two never add up.
            /// </summary>
            private static PandoranVarianceStatus WeaponRollStatus(TacticalActorBase actorBase)
            {
                if (!IsEnabled || !(actorBase is TacticalActor actor))
                {
                    return null;
                }

                PandoranVarianceStatus status = actor.Status?.GetStatus<PandoranVarianceStatus>();

                if (status == null)
                {
                    return null;
                }

                if (status.MeleeDamageRoll != 0)
                {
                    TFTVLogger.Always($"[HarshAttrition] {actor.name}: melee roll {status.MeleeDamageRoll:+0;-0} from an older save replaced by weapon rolls");
                    status.MeleeDamageRoll = 0;
                }

                return status;
            }

            private static bool TryReadWeaponRoll(PandoranVarianceStatus status, string weaponDef, out float roll)
            {
                string prefix = weaponDef + "=";

                foreach (string entry in status.WeaponRolls)
                {
                    if (entry != null && entry.StartsWith(prefix, StringComparison.Ordinal)
                        && float.TryParse(entry.Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out roll))
                    {
                        return true;
                    }
                }

                roll = 1f;
                return false;
            }

            /// <summary>This weapon's roll on this creature (1 when it is not rolled), rolling it now if it has none yet.</summary>
            internal static float GetWeaponRoll(TacticalActorBase actor, WeaponDef weaponDef)
            {
                PandoranVarianceStatus status = WeaponRollStatus(actor);

                if (status == null || weaponDef == null)
                {
                    return 1f;
                }

                if (TryReadWeaponRoll(status, weaponDef.name, out float roll))
                {
                    return roll;
                }

                roll = NextWeaponRoll(actor);
                status.WeaponRolls.Add($"{weaponDef.name}={roll.ToString("0.0000", CultureInfo.InvariantCulture)}");
                return roll;
            }

            /// <summary>Rolls every weapon the creature carries now, for the log: "Crabman_RightHand_Pincer_WeaponDef x1.08".</summary>
            private static string RollAllWeapons(TacticalActor actor)
            {
                List<Weapon> weapons = actor.Equipments?.GetWeapons()?.Where(w => w?.WeaponDef != null).ToList();

                if (weapons == null || weapons.Count == 0)
                {
                    return "no weapons";
                }

                return string.Join(", ", weapons.Select(w => $"{w.WeaponDef.name} x{GetWeaponRoll(actor, w.WeaponDef):0.00}"));
            }

            /// <summary>Piercing and Shredding are about armour, not damage: they stay as they are.</summary>
            private static bool IsRolledKeyword(DamageKeywordDef keyword)
            {
                if (keyword == null)
                {
                    return false;
                }

                SharedDamageKeywordsDataDef shared = TFTVMain.Shared?.SharedDamageKeywords;
                return shared == null || keyword != shared.PiercingKeyword && keyword != shared.ShreddingKeyword;
            }

            private static float ApplyWeaponRoll(float value, float roll)
            {
                return value > 0f ? Mathf.Max(1f, Mathf.Round(value * roll)) : value;
            }

            private static Weapon SourceWeapon(object source)
            {
                return source as Weapon ?? TacUtil.GetSourceOfType<Weapon>(source);
            }

            /// <summary>The roll of the attack whose keyword is being worked out; 1 outside one.</summary>
            [ThreadStatic]
            private static float _keywordRoll;

            /// <summary>
            /// Every keyword of a hit gets its value from DamageKeywordPair.GenerateDamageValue, called from here with the
            /// accumulation (and so the weapon) at hand. The pair itself belongs to the weapon def and must not be changed.
            /// </summary>
            [HarmonyPatch(typeof(DamageAccumulation), "ProcessDamageKeyword")]
            internal static class DamageAccumulation_ProcessDamageKeyword_WeaponRoll_Patch
            {
                private static void Prefix(DamageAccumulation __instance, out float __state)
                {
                    __state = _keywordRoll;

                    try
                    {
                        Weapon weapon = SourceWeapon(__instance.Source);
                        TacticalActorBase actor = __instance.SourceActor ?? weapon?.TacticalActorBase;
                        _keywordRoll = weapon?.WeaponDef == null ? 1f : GetWeaponRoll(actor, weapon.WeaponDef);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        _keywordRoll = 1f;
                    }
                }

                private static void Postfix(float __state)
                {
                    _keywordRoll = __state;
                }
            }

            [HarmonyPatch(typeof(DamageKeywordPair), nameof(DamageKeywordPair.GenerateDamageValue))]
            internal static class DamageKeywordPair_GenerateDamageValue_WeaponRoll_Patch
            {
                private static void Postfix(DamageKeywordPair __instance, ref float __result)
                {
                    try
                    {
                        float roll = _keywordRoll;

                        if (roll > 0f && roll != 1f && IsRolledKeyword(__instance.DamageKeywordDef))
                        {
                            __result = ApplyWeaponRoll(__result, roll);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            #endregion

            #region weapon reveal

            /// <summary>A shot, hit or miss: seen, it gives the weapon's values away.</summary>
            [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.Activate))]
            internal static class ShootAbility_Activate_WeaponReveal_Patch
            {
                private static void Postfix(ShootAbility __instance)
                {
                    try
                    {
                        RevealWeapon(__instance?.TacticalActor, __instance?.Weapon);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>A strike: Arthron claws and other melee weapons attack with BashStrike (a BashAbility), not a ShootAbility.</summary>
            [HarmonyPatch(typeof(BashAbility), nameof(BashAbility.Activate))]
            internal static class BashAbility_Activate_WeaponReveal_Patch
            {
                private static void Postfix(BashAbility __instance)
                {
                    try
                    {
                        RevealWeapon(__instance?.TacticalActor, __instance?.TryGetWeapon());
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void RevealWeapon(TacticalActor actor, Weapon weapon)
            {
                PandoranVarianceStatus status = WeaponRollStatus(actor);

                if (status == null || weapon?.WeaponDef == null || status.RevealedWeapons.Contains(weapon.WeaponDef.name))
                {
                    return;
                }

                // The player's faction is always in a tactical level ("px"); GetFactionByCommandName throws for a missing one.
                TacticalFaction player = actor.TacticalLevel?.GetFactionByCommandName(PlayerFactionCommandName);

                if (player == null || actor.TacticalFaction == player || player.Vision?.IsRevealed(actor) != true)
                {
                    return;
                }

                float roll = GetWeaponRoll(actor, weapon.WeaponDef);
                status.RevealedWeapons.Add(weapon.WeaponDef.name);
                TFTVLogger.Always($"[HarshAttrition] {actor.name} attacked in view with {weapon.WeaponDef.name}: its rolled values (x{roll:0.00}) now show in its tooltip");
            }

            /// <summary>How the open tooltip shows a rolled weapon's damage: as rolled, as the template's with a "?", or untouched.</summary>
            private enum WeaponValues { Untouched, Rolled, Approximate }

            private static WeaponValues _tooltipValues;
            private static float _tooltipRoll = 1f;
            private static WeaponDef _tooltipWeapon;

            /// <summary>
            /// The tooltip shows when the creature's stats are known (vivisection) or the player has seen it attack with this
            /// weapon; damage rows then read as rolled once seen, as the template's with a "?" before.
            /// </summary>
            internal static bool BeginItemTooltip(TacticalItem item)
            {
                EndItemTooltip();

                Insight insight = GetInsight(item?.TacticalActorBase);
                Weapon weapon = item as Weapon;
                PandoranVarianceStatus status = WeaponRollStatus(item?.TacticalActorBase);

                if (weapon?.WeaponDef == null || status == null)
                {
                    return insight.Stats;
                }

                bool revealed = status.RevealedWeapons.Contains(weapon.WeaponDef.name) || !insight.Gated;

                if (!revealed && !insight.Stats)
                {
                    return false;
                }

                _tooltipWeapon = weapon.WeaponDef;
                _tooltipRoll = GetWeaponRoll(item.TacticalActorBase, weapon.WeaponDef);
                _tooltipValues = revealed ? WeaponValues.Rolled : WeaponValues.Approximate;
                return true;
            }

            internal static void EndItemTooltip()
            {
                _tooltipValues = WeaponValues.Untouched;
                _tooltipRoll = 1f;
                _tooltipWeapon = null;
            }

            /// <summary>SetWeaponStats passes each keyword's display name and value here; the rolled weapon's are swapped.</summary>
            [HarmonyPatch(typeof(UIItemTooltip), "SetStat")]
            internal static class UIItemTooltip_SetStat_WeaponRoll_Patch
            {
                private static void Prefix(LocalizedTextBind statLocalization, ref object statValue)
                {
                    try
                    {
                        if (_tooltipValues == WeaponValues.Untouched || statLocalization == null || _tooltipWeapon?.DamagePayload?.DamageKeywords == null)
                        {
                            return;
                        }

                        DamageKeywordPair pair = _tooltipWeapon.DamagePayload.DamageKeywords
                            .FirstOrDefault(k => k?.DamageKeywordDef?.Visuals != null && k.DamageKeywordDef.Visuals.DisplayName1 == statLocalization);

                        if (pair == null || !IsRolledKeyword(pair.DamageKeywordDef))
                        {
                            return;
                        }

                        statValue = _tooltipValues == WeaponValues.Rolled
                            ? (object)ApplyWeaponRoll(pair.Value, _tooltipRoll)
                            : Approximately(pair.Value);
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
}
