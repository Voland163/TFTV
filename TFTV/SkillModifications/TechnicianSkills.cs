using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PRMBetterClasses.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using TFTV.TFTVDrills;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class TechnicianSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            // Fix Field Medic for mutoids
            Fix_FieldMedic();

            // Remote Deployment: change cost 0->2 WP
            Change_RemoteDeployment();

            // Electric Reinforcements: 10 tiles range, +10 armor, 1 AP and 3 WP
            Change_ElectricReinforcements();

            // Stability: Gain 5% extra accuracy per remaining AP up to 20%
            Create_Stability();

            // Amplify Pain: If your next attack deals special damage, double that damage (Bleeding, Paralysis, Viral, Poison, Fire, EMP, Sonic, Shock, Virophage)
            Create_AmplifyPain();

            if (TFTVAircraftReworkMain.AircraftReworkOn)
            {
                Create_CommandOverlay();
               // ApplyAircraftReworkSkillSwaps();
            }
        }

        private static void Fix_FieldMedic()
        {
            HealAbilityDef fieldMedic = DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef");
            if (fieldMedic != null)
            {
                fieldMedic.AdaptiveWeaponStatusDef = null;
            }
        }

        private static void Change_RemoteDeployment()
        {
            // Collecting all turret deploy and throw abilities
            TacticalAbilityDef[] turretPlacementAbilities =
            {
                DefCache.GetDef<TacticalAbilityDef>("DeployTurret_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("DeployPRCRTurret_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("DeployLaserTurret_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("ThrowTurret_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("ThrowPRCRTurret_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("ThrowLaserTurret_AbilityDef")
            };
            foreach (TacticalAbilityDef turretPlacementAbility in turretPlacementAbilities)
            {
                // Delete the Category so the throw ability will not longer replace the deploy ability
                // i.e. both are usable even with Remote Deployment learned
                turretPlacementAbility.ViewElementDef.Category.LocalizationKey = "";
                // All of them used for 1 AP cost (reducing deploy from 2 to 1 AP)
                turretPlacementAbility.ActionPointCost = 0.25f;
                // Add WP cost for throw abilities
                if (turretPlacementAbility.name.StartsWith("Throw"))
                {
                    turretPlacementAbility.WillPointCost = 2;
                }
            }
        }

        private static void Change_ElectricReinforcements()
        {
            float armorBonus = 10f;
            ApplyStatusAbilityDef eR = DefCache.GetDef<ApplyStatusAbilityDef>("ElectricReinforcement_AbilityDef");
            ItemSlotStatsModifyStatusDef eRStatus = (ItemSlotStatsModifyStatusDef)eR.StatusDef;

            eR.TargetingDataDef.Origin.Range = 10;
            eR.ActionPointCost = 0.25f;
            eR.WillPointCost = 3;
            eR.ViewElementDef.Description.LocalizationKey = "PR_BC_ELECTRIC_REINFORCEMENT_DESC"; // new LocalizedTextBind($"Give yourself and allies within 20 tiles a bonus of {armorBonus} armour for 1 turn. This effect does not stack.", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            eRStatus.StatsModifications[0].Value = armorBonus;
            eRStatus.StatsModifications[1].Value = armorBonus;
        }

        private static void Create_Stability()
        {
            float maxAccBoost = 0.4f;
            string skillName = "Stability_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BloodLust_AbilityDef");
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Strategist.png");

            ApplyStatusAbilityDef Stability = Helper.CreateDefFromClone(
                source,
                "697a87ab-a799-4c7a-9332-e0b411a2e82d",
                skillName);
            Stability.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "4f447b56-c8e2-4e25-8a3c-16f599b5cc0c",
                skillName);
            Stability.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "868adb42-e012-4c7a-9d39-fe9d64d95be9",
                skillName);
            Stability.StatusDef = Helper.CreateDefFromClone<ActionpointsRelatedStatusDef>(
                null,
                "997a4627-2982-44d4-944d-1c8cf76acb02",
                $"E_Status [{skillName}]");
            Stability.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_STABILITY";
            Stability.ViewElementDef.Description.LocalizationKey = "PR_BC_STABILITY_DESC";
            Stability.ViewElementDef.LargeIcon = icon;
            Stability.ViewElementDef.SmallIcon = icon;
            ActionpointsRelatedStatusDef apRelatedStatus = (ActionpointsRelatedStatusDef)Stability.StatusDef;
            apRelatedStatus.EffectName = "StabilityStatus";
            apRelatedStatus.ApplicationConditions = new EffectConditionDef[0];
            apRelatedStatus.DisablesActor = false;
            apRelatedStatus.SingleInstance = false;
            apRelatedStatus.ShowNotification = false;
            apRelatedStatus.VisibleOnPassiveBar = false;
            apRelatedStatus.VisibleOnHealthbar = 0;
            apRelatedStatus.VisibleOnStatusScreen = 0;
            apRelatedStatus.HealthbarPriority = 0;
            apRelatedStatus.StackMultipleStatusesAsSingleIcon = false;
            apRelatedStatus.Visuals = Stability.ViewElementDef;
            apRelatedStatus.ParticleEffectPrefab = null;
            apRelatedStatus.DontRaiseOnApplyOnLoad = false;
            apRelatedStatus.EventOnApply = null;
            apRelatedStatus.EventOnUnapply = null;
            apRelatedStatus.ActionpointsLowBound = 1.0f;
            apRelatedStatus.MaxBoost = maxAccBoost;
            apRelatedStatus.StatModificationTargets = new StatModificationTarget[]
            {
                StatModificationTarget.Accuracy
            };
        }

        private static void Create_AmplifyPain()
        {
            string skillName = "AmplifyPain_AbilityDef";
            float healMultiplier = 2f;
            float additionalDamageMultiplier = 0.5f;
            float wpCost = 4f;
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef");
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_CharacterAbility_AmplifyPain01.png");

            ApplyStatusAbilityDef AmplifyPain = Helper.CreateDefFromClone(
                source,
                "463a6458-e45c-4310-abb6-c7cb904cb918",
                skillName);
            AmplifyPain.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "5938a019-b798-4076-98a8-a58d100411f3",
                skillName);
            AmplifyPain.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "a4812b9a-385d-4083-a494-3e169d08c39e",
                skillName);
            AmplifyPain.StatusDef = Helper.CreateDefFromClone(
                source.StatusDef,
                "d3ce1f20-3503-4dfb-a836-6bdc78064d4e",
                skillName);
            StanceStatusDef HealMod = Helper.CreateDefFromClone(
                DefCache.GetDef<StanceStatusDef>("E_Status [ImprovedMedkit_FactionEffectDef]"),
                "66cf66aa-c0f5-4711-b8de-52a5a478b389",
                $"E_HealMultiplier [{skillName}]");
            AddDependentDamageKeywordsStatusDef DamageMod = Helper.CreateDefFromClone<AddDependentDamageKeywordsStatusDef>(
                null,
                "247f3abb-420f-42ac-8af3-d3ca12fb5787",
                $"E_DamageMultiplier [{skillName}]");

            AmplifyPain.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_AMPLIFY_PAIN";
            AmplifyPain.ViewElementDef.Description.LocalizationKey = "PR_BC_AMPLIFY_PAIN_DESC";
            AmplifyPain.ViewElementDef.LargeIcon = icon;
            AmplifyPain.ViewElementDef.SmallIcon = icon;
            AmplifyPain.UsesPerTurn = 1;
            AmplifyPain.WillPointCost = wpCost;
            AmplifyPain.DisablingStatuses = new StatusDef[] { AmplifyPain.StatusDef };

            (AmplifyPain.StatusDef as AddAttackBoostStatusDef).DurationTurns = 0;
            (AmplifyPain.StatusDef as AddAttackBoostStatusDef).ExpireOnEndOfTurn = true;
            (AmplifyPain.StatusDef as AddAttackBoostStatusDef).Visuals = AmplifyPain.ViewElementDef;
            (AmplifyPain.StatusDef as AddAttackBoostStatusDef).NumberOfAttacks = -1; // lasts as long as the status = end of turn
            (AmplifyPain.StatusDef as AddAttackBoostStatusDef).AdditionalStatusesToApply = new TacStatusDef[] { HealMod, DamageMod };

            HealMod.DurationTurns = 0;
            HealMod.EquipmentsStatModifications = new EquipmentItemTagStatModification[]
            {
                new EquipmentItemTagStatModification()
                {
                    ItemTag = DefCache.GetDef < GameTagDef >("MedkitItem_TagDef"),
                    EquipmentStatModification = new ItemStatModification()
                    {
                        TargetStat = StatModificationTarget.BonusHealValue,
                        Modification = StatModificationType.Multiply,
                        Value = healMultiplier
                    }
                },
                new EquipmentItemTagStatModification()
                {
                    ItemTag = DefCache.GetDef < GameTagDef >("RoboticArmItem_TagDef"),
                    EquipmentStatModification = new ItemStatModification()
                    {
                        TargetStat = StatModificationTarget.BonusHealValue,
                        Modification = StatModificationType.Multiply,
                        Value = healMultiplier
                    }
                }
            };

            DamageMod.EffectName = "AmplifyPain";
            DamageMod.ApplicationConditions = new EffectConditionDef[0];
            DamageMod.DurationTurns = 0;
            DamageMod.DisablesActor = false;
            DamageMod.SingleInstance = true;
            DamageMod.ShowNotification = true;
            DamageMod.VisibleOnPassiveBar = false;
            DamageMod.VisibleOnHealthbar = 0;
            DamageMod.VisibleOnStatusScreen = 0;
            DamageMod.HealthbarPriority = 0;
            DamageMod.StackMultipleStatusesAsSingleIcon = false;
            DamageMod.Visuals = AmplifyPain.ViewElementDef;
            DamageMod.ParticleEffectPrefab = null;
            DamageMod.DontRaiseOnApplyOnLoad = false;
            DamageMod.EventOnApply = null;
            DamageMod.EventOnUnapply = null;
            DamageMod.DamageKeywordDefs = new DamageKeywordDef[]
            {
                SkillModsMain.sharedSoloDamageKeywords.SoloViralKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloAcidKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloPoisonousKeyword,
                SkillModsMain.sharedSoloDamageKeywords.SoloParalysingKeyword,
                //SkillModsMain.sharedSoloDamageKeywords.SoloShockKeyword,
                //SkillModsMain.sharedSoloDamageKeywords.SoloSonicKeyword
            };
            DamageMod.BonusDamagePerc = additionalDamageMultiplier;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(AmplifyPain))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(AmplifyPain).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }

        private static void Create_CommandOverlay()
        {
            string abilityName = "CommandOverlay_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef");
            Sprite icon = Helper.CreateSpriteFromImageFile("CommandOverlay.png");

            ApplyStatusAbilityDef commandOverlay = Helper.CreateDefFromClone(
                source,
                "c6e3321c-1d8d-4a05-9f73-50a4c7d8ff5b",
                abilityName);

            commandOverlay.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "bc6d0b8f-b0ed-42db-87b6-411d2c42f4bf",
                abilityName);

            commandOverlay.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "fde2c01a-6a5a-4e1d-a4a1-f7e85e67f124",
                abilityName);

            DrillCommandOverlay.PerceptionAuraStatusDef auraStatus = Helper.CreateDefFromClone<DrillCommandOverlay.PerceptionAuraStatusDef>(
                null,
                "75c7d3d5-6fba-4b59-ae5e-e1d9cd7742df",
                $"E_Status [{abilityName}]");

            commandOverlay.StatusDef = auraStatus;
            commandOverlay.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_TECH7_COMMANDOVERLAY_NAME";
            commandOverlay.ViewElementDef.Description.LocalizationKey = "TFTV_TECH7_COMMANDOVERLAY_DESC";
            commandOverlay.ViewElementDef.LargeIcon = icon;
            commandOverlay.ViewElementDef.SmallIcon = icon;

            commandOverlay.TargetingDataDef.Origin.Range = 10f;
            commandOverlay.ActionPointCost = 0f;
            commandOverlay.WillPointCost = 2f;
            commandOverlay.UsesPerTurn = 1;
            commandOverlay.DisablingStatuses = new StatusDef[] { commandOverlay.StatusDef };

            commandOverlay.CharacterProgressionData.RequiredSpeed = 0;
            commandOverlay.CharacterProgressionData.RequiredStrength = 0;
            commandOverlay.CharacterProgressionData.RequiredWill = 0;

            auraStatus.EffectName = "CommandOverlay";
            auraStatus.ApplicationConditions = Array.Empty<EffectConditionDef>();
            auraStatus.DisablesActor = false;
            auraStatus.SingleInstance = true;
            auraStatus.ShowNotification = false;
            auraStatus.VisibleOnPassiveBar = true;
            auraStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            auraStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
            auraStatus.HealthbarPriority = 0;
            auraStatus.StackMultipleStatusesAsSingleIcon = false;
            auraStatus.Visuals = commandOverlay.ViewElementDef;
            auraStatus.ParticleEffectPrefab = null;
            auraStatus.DontRaiseOnApplyOnLoad = false;
            auraStatus.EventOnApply = null;
            auraStatus.EventOnUnapply = null;
            auraStatus.DurationTurns = 1;
            auraStatus.ExpireOnEndOfTurn = true;
            auraStatus.AccuracyBonus = 20f;

            DrillsDefs._commandOverlayStatus = auraStatus;
        }

        private static void ApplyAircraftReworkSkillSwaps()
        {
            BCSettings config = TFTVMain.Main.Settings;

            int technicianIndex = config.ClassSpecializations.FindIndex(cs => cs.ClassName.Equals(ClassKeys.Technician.Name, StringComparison.OrdinalIgnoreCase));
            if (technicianIndex >= 0)
            {
                ClassSpecDef technicianSpec = config.ClassSpecializations[technicianIndex];
                if (technicianSpec.MainSpec != null && technicianSpec.MainSpec.Length > 6)
                {
                    string[] updatedMainSpec = technicianSpec.MainSpec.ToArray();
                    updatedMainSpec[6] = "COMMAND OVERLAY";
                    technicianSpec.MainSpec = updatedMainSpec;
                    config.ClassSpecializations[technicianIndex] = technicianSpec;
                }
            }

            if (Helper.AbilityNameToDefMap != null)
            {
                Helper.AbilityNameToDefMap["COMMAND OVERLAY"] = "CommandOverlay_AbilityDef";
                Helper.AbilityNameToDefMap["AMPLIFY PAIN"] = "AmplifyPain_AbilityDef";
            }

            for (int i = 0; i < config.PersonalPerks.Count; i++)
            {
                PersonalPerksDef perk = config.PersonalPerks[i];
                if (!perk.PerkKey.Equals(PerkType.Faction_2, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dictionary<string, Dictionary<string, string>> relatedPerks = perk.RelatedFixedPerks?.ToDictionary(
                   outer => outer.Key,
                   outer => new Dictionary<string, string>(outer.Value));
                if (relatedPerks == null)
                {
                    continue;
                }

                if (relatedPerks.TryGetValue(FactionKeys.NJ, out Dictionary<string, string> njPerks)
                    && njPerks.ContainsKey(ClassKeys.Technician.Name))
                {
                    Dictionary<string, string> updatedNjPerks = new Dictionary<string, string>(njPerks)
                    {
                        [ClassKeys.Technician.Name] = "AMPLIFY PAIN"
                    };
                    relatedPerks[FactionKeys.NJ] = updatedNjPerks;
                    perk.RelatedFixedPerks = relatedPerks;
                    config.PersonalPerks[i] = perk;
                }
            }
        }
    }
}
