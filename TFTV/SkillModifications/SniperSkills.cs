using Base.Defs;
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
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class SniperSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

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
            ChangeAbilitiesCostStatusDef extremeFocusAPcostMod = Repo.GetAllDefs<ChangeAbilitiesCostStatusDef>().FirstOrDefault(c => c.name.Contains("ExtremeFocus_AbilityDef"));
            extremeFocusAPcostMod.AbilityCostModification.ActionPointModType = TacticalAbilityModificationType.Set;
            extremeFocusAPcostMod.AbilityCostModification.ActionPointMod = 0.25f;
            extremeFocusAPcostMod.Visuals.Description.LocalizationKey = "PR_BC_EXTREME_FOCUS_DESC"; // new LocalizedTextBind("Overwatch cost is set to 1 Action Point cost for all weapons", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }

        private static void Change_ArmourBreak()
        {
            ApplyStatusAbilityDef armourBreak = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(ad => ad.name.Equals("ArmourBreak_AbilityDef"));
            armourBreak.WillPointCost = 2.0f;
            armourBreak.ViewElementDef.Description.LocalizationKey = "PR_BC_ARMOR_BREAK_DESC"; // new LocalizedTextBind("Next shot has 15 shred but -25% damage", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            AddAttackBoostStatusDef armourBreakShredMod = armourBreak.StatusDef as AddAttackBoostStatusDef;
            armourBreakShredMod.DamageKeywordPairs[0].Value = 15.0f;
            StanceStatusDef armourBreakDamageReduction = Helper.CreateDefFromClone( // Borrow status from Sneak Attack for damage reduction
                Repo.GetAllDefs<StanceStatusDef>().FirstOrDefault(p => p.name.Equals("E_SneakAttackStatus [SneakAttack_AbilityDef]")),
                "e0dcd2aa-0262-41ff-9be0-c7671a6a11e0",
                "E_DamageReductionStatus [ArmourBreak_AbilityDef]");
            armourBreakDamageReduction.EffectName = "ArmourBreak";
            armourBreakDamageReduction.DurationTurns = 0;
            armourBreakDamageReduction.SingleInstance = true;
            armourBreakDamageReduction.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
            armourBreakDamageReduction.VisibleOnStatusScreen = 0;
            armourBreakDamageReduction.Visuals = armourBreak.ViewElementDef;
            armourBreakDamageReduction.StatModifications[0].Value = 0.75f;
            armourBreakShredMod.AdditionalStatusesToApply = new TacStatusDef[] { armourBreakDamageReduction };
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

            ShootAbilityDef source = Repo.GetAllDefs<ShootAbilityDef>().FirstOrDefault(s => s.name.Equals("Gunslinger_AbilityDef"));
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
                Repo.GetAllDefs<GameTagDef>().FirstOrDefault(g => g.name.Equals("HandgunItem_TagDef"))
            };
            gunslinger.ExecutionsCount = burst;
            gunslinger.ProjectileSpreadMultiplier = accPenalty;
        }

        private static void Create_KillZone()
        {
            // Harmony patch PhoenixPoint.Tactical.Entities.Weapons.Weapon.GetNumberOfShots
            // Adding an ability that get checked in the patched method (see below)
            string skillName = "KillZone_AbilityDef";
            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Contains("Talent"));
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
                        TacticalActor ___TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacticalItem), "TacticalActor").GetValue(__instance, null);
                        TacticalAbility killzoneAbility = ___TacticalActor.GetAbilities<TacticalAbility>().FirstOrDefault(s => s.AbilityDef.name.Equals("KillZone_AbilityDef"));
                        if (killzoneAbility != null)
                        {
                            bool ___InfiniteCharges = (bool)AccessTools.Property(typeof(TacticalItem), "InfiniteCharges").GetValue(__instance, null);
                            CommonItemData ___CommonItemData = (CommonItemData)AccessTools.Property(typeof(TacticalItem), "CommonItemData").GetValue(__instance, null);
                            if (___TacticalActor.IsProficientWithEquipment(__instance)
                                && (___InfiniteCharges || ___CommonItemData.CurrentCharges >= __result * 2))
                            {
                                __result *= 2;
                            }
                            PRMLogger.Debug("Overwatch called GetNumberOfShots by ...");
                            PRMLogger.Debug("  Actor           : " + ___TacticalActor.DisplayName);
                            PRMLogger.Debug("  Ability checked : " + killzoneAbility.AbilityDef.name);
                            PRMLogger.Debug("  Weapon          : " + __instance.DisplayName);
                            PRMLogger.Debug("  Actor is prof.  : " + ___TacticalActor.IsProficientWithEquipment(__instance));
                            PRMLogger.Debug("  Infinite charges: " + ___InfiniteCharges);
                            PRMLogger.Debug("  Current charges : " + ___CommonItemData.CurrentCharges);
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
    }
}
