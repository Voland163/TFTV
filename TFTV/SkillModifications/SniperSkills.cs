using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;
using TFTV;
using TFTV.Tactical.Entities.DamageKeywords;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class SniperSkills
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            // Extreme Focus: Set to 1 AP regardless of weapon type
            Change_ExtremeFocus();

            // Armor Break: Set to 15 shred and -25% damage
            Change_ArmourBreak();

            // Gunslinger: 3 pistol shots in one action (like Rage Burst)
            Change_Gunslinger();

            // Kill Zone: An additional overwatch shot
            Create_KillZone();
        }

        private static void Change_ExtremeFocus()
        {
            ChangeAbilitiesCostStatusDef extremeFocusAPcostMod = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_ExtremeFocusStatus [ExtremeFocus_AbilityDef]");
            extremeFocusAPcostMod.AbilityCostModification.ActionPointModType = TacticalAbilityModificationType.Set;
            extremeFocusAPcostMod.AbilityCostModification.ActionPointMod = 0.25f;
            extremeFocusAPcostMod.Visuals.Description.LocalizationKey = "PR_BC_EXTREME_FOCUS_DESC"; // new LocalizedTextBind("Overwatch cost is set to 1 Action Point cost for all weapons", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }

        private static void Change_ArmourBreak()
        {
            ApplyStatusAbilityDef armourBreak = DefCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef");
            armourBreak.WillPointCost = 3.0f;
            armourBreak.UsesPerTurn = 2;
            armourBreak.ViewElementDef.Description.LocalizationKey = "PR_BC_ARMOR_BREAK_DESC";
            // Get status for damage keyword manipulation
            AddAttackBoostStatusDef armourBreakStatus = armourBreak.StatusDef as AddAttackBoostStatusDef;
            armourBreakStatus.WeaponTagFilter = DefCache.GetDef<GameTagDef>("GunWeapon_TagDef");
            // Create new damage keyword
            ArmourBreakDamageKeywordDataDef armourBreakDamageKeyword = Helper.CreateDefFromClone<ArmourBreakDamageKeywordDataDef>(
                null,
                "09EE6453-5D9E-4635-8BD1-F3980C3A1A99",
                "ArmourBreak_DamageKeywordDataDef");
            Helper.CopyFieldsByReflection(armourBreakStatus.DamageKeywordPairs.First().DamageKeywordDef, armourBreakDamageKeyword);
            armourBreakDamageKeyword.DistributeShredAcrossBurst = true;
            armourBreakDamageKeyword.ShredIsAdditive = true;
            // Set newly created damage keyword back to status
            armourBreakStatus.DamageKeywordPairs = new DamageKeywordPair[] //[0].Value = 15.0f;
            {
                new DamageKeywordPair()
                {
                    DamageKeywordDef = armourBreakDamageKeyword,
                    Value = 15
                }
            };
            // Fix to prevent that the skill can be used more than once without shooting, vanilla bug!
            armourBreak.DisablingStatuses = new StatusDef[]
            {
                armourBreak.StatusDef,
                DefCache.GetDef<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef").StatusDef,
                DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef").StatusDef,
            };
        }

        private static void Change_Gunslinger()
        {
            string skillName = "BC_Gunslinger_AbilityDef";
            float apCost = -1.0f;
            float wpCost = 4.0f;
            int burst = 3;
            float accPenalty = 2.0f;
            //LocalizedTextBind name = new LocalizedTextBind("GUNSLINGER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Shoot handgun 3 times at -50% accuracy", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            ShootAbilityDef source = DefCache.GetDef<ShootAbilityDef>("Gunslinger_AbilityDef");
            ShootAbilityDef gunslinger = Helper.CreateDefFromClone(
                source,
                "c6fdce21-fd70-4c8c-a92a-b623715c8762",
                skillName);
            gunslinger.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "0aefa178-33db-4d96-8d95-b548cec1a848",
                skillName);
            gunslinger.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "4c6e3ad0-787a-4185-9011-f568f382abba",
                skillName);
            gunslinger.CharacterProgressionData.RequiredSpeed = 0;
            gunslinger.CharacterProgressionData.RequiredStrength = 0;
            gunslinger.CharacterProgressionData.RequiredWill = 0;
            gunslinger.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_GUNSLINGER"; // name;
            gunslinger.ViewElementDef.Description.LocalizationKey = "PR_BC_GUNSLINGER_DESC"; // description;
            Sprite gunslingerIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_CharacterAbility_Gunslinger-3.png");
            gunslinger.ViewElementDef.LargeIcon = gunslingerIcon;
            gunslinger.ViewElementDef.SmallIcon = gunslingerIcon;
            gunslinger.ActionPointCost = apCost;
            gunslinger.WillPointCost = wpCost;
            gunslinger.EquipmentTags = new GameTagDef[] {
                DefCache.GetDef < GameTagDef >("HandgunItem_TagDef")
            };
            gunslinger.ExecutionsCount = burst;
            gunslinger.ProjectileSpreadMultiplier = accPenalty;
            gunslinger.DisablingStatuses = new StatusDef[]
            {
                DefCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef").StatusDef,
            };
        }

        private static void Create_KillZone()
        {
            // Harmony patch PhoenixPoint.Tactical.Entities.Weapons.Weapon.GetNumberOfShots
            // Adding an ability that get checked in the patched method (see below)
            string skillName = "KillZone_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
            PassiveModifierAbilityDef killZone = Helper.CreateDefFromClone(
                source,
                "a5f9cf13-595b-4f54-8737-063e9219b4b0",
                skillName);
            killZone.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "83d299c4-a35e-4636-b8e8-e95be463b708",
                skillName);
            killZone.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "4f719533-908e-456e-8972-1df45df37740",
                skillName);
            // reset all possible passive modifications, we need none, this ability is only to have something to chose and as flag for the Kill Zone Harmony patch
            killZone.StatModifications = new ItemStatModification[0];
            killZone.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            killZone.DamageKeywordPairs = new DamageKeywordPair[0];
            // Set necessary fields
            killZone.CharacterProgressionData.RequiredSpeed = 0;
            killZone.CharacterProgressionData.RequiredStrength = 0;
            killZone.CharacterProgressionData.RequiredWill = 0;
            killZone.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_KILL_ZONE"; // new LocalizedTextBind("KILL ZONE", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            killZone.ViewElementDef.Description.LocalizationKey = "PR_BC_KILL_ZONE_DESC"; // new LocalizedTextBind("When you take an Overwatch shot you fire twice at the target", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite killZoneIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_CharacterAbility_KillZone-2a.png");
            killZone.ViewElementDef.LargeIcon = killZoneIcon;
            killZone.ViewElementDef.SmallIcon = killZoneIcon;
        }

        // Kill Zone: Patching GetNumberOfShots from active weapon to check if Kill Zone ability is active and double the amount of shots if enough ammo
        [HarmonyPatch(typeof(Weapon), "GetNumberOfShots")]
        internal static class KZ_GetNumberOfShots_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(Weapon __instance, ref int __result, AttackType attackType)
            {
                try
                {
                    if (attackType == AttackType.Overwatch)
                    {
                        TacticalAbility killzoneAbility = __instance.TacticalActor.GetAbilities<TacticalAbility>().FirstOrDefault(s => s.AbilityDef.name.Equals("KillZone_AbilityDef"));
                        if (killzoneAbility != null)
                        {
                            if (__instance.TacticalActor.IsProficientWithEquipment(__instance)
                                && (__instance.InfiniteCharges || __instance.CommonItemData.CurrentCharges >= __result * 2))
                            {
                                __result *= 2;
                            }
                            PRMLogger.Debug("Overwatch called GetNumberOfShots by ...");
                            PRMLogger.Debug("  Actor           : " + __instance.TacticalActor.DisplayName);
                            PRMLogger.Debug("  Ability checked : " + killzoneAbility.AbilityDef.name);
                            PRMLogger.Debug("  Weapon          : " + __instance.DisplayName);
                            PRMLogger.Debug("  Actor is prof.  : " + __instance.TacticalActor.IsProficientWithEquipment(__instance));
                            PRMLogger.Debug("  Infinite charges: " + __instance.InfiniteCharges);
                            PRMLogger.Debug("  Current charges : " + __instance.CommonItemData.CurrentCharges);
                            PRMLogger.Debug("  Result shots    : " + __result);
                            PRMLogger.Debug("----------------------------------------------------", false);
                        }
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        // Patch to change Inspire and Bloodthirsty not giving any WP when the killed actor has WillPointsWorth = 0
        [HarmonyPatch(typeof(OnActorDeathEffectStatus), "ShouldApplyEffect")]
        internal static class OnActorDeathEffectStatus_ShouldApplyEffect_Patch
        {
            public static void Postfix(OnActorDeathEffectStatus __instance, ref bool __result, DeathReport deathReport)
            {
                if (__instance.OnActorDeathEffectStatusDef == DefCache.GetDef<ApplyStatusAbilityDef>("Inspire_AbilityDef").StatusDef
                    || __instance.OnActorDeathEffectStatusDef == DefCache.GetDef<ApplyStatusAbilityDef>("Bloodthirsty_AbilityDef").StatusDef)
                {
                    __result = __result && deathReport.Actor.TacticalActorBaseDef.WillPointWorth > 0;
                }


                // OLD version, has side effects with other death effect statuses ...

                /*TacticalAbilityDef inspireAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Inspire_AbilityDef");
                TacticalAbilityDef bloodThirstyAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Bloodthirsty_AbilityDef");
                
                if (__instance.TacticalActorBase.GetAbilityWithDef<TacticalAbility>(inspireAbilityDef) != null
                    || __instance.TacticalActorBase.GetAbilityWithDef<TacticalAbility>(bloodThirstyAbilityDef) != null)
                {
                    __result = __result && deathReport.Actor.TacticalActorBaseDef.WillPointWorth > 0;
                }*/
            }
        }
    }
}
