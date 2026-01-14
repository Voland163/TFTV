using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DynamicBoneColliderBase;
using static PhoenixPoint.Common.Entities.Addons.AddonDef;
using static PhoenixPoint.Tactical.Entities.Statuses.ItemSlotStatsModifyStatusDef;

namespace TFTV
{
    internal class TFTVVests
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        public static TacticalItemDef AblativeVestDef = null;
        public static TacticalItemDef HazmatVestDef = null;
        public static DamageMultiplierStatusDef AblativeResistancesDef = null;
        public static DamageMultiplierStatusDef HazmatResistancesDef = null;
        public static ItemSlotStatsModifyStatusDef HealthBuffStatusDef = null;
        public static ItemSlotStatsModifyStatusDef ArmorBuffStatusDef = null;

        // Constants: names are part of the requirements.
        private const string AblativeVestDefName = "TFTV_AblativeVest_Attachment_ItemDef";
        private const string HazmatVestDefName = "TFTV_HazmatVest_Attachment_ItemDef";
       


        [HarmonyPatch(typeof(ApplyStatusAbility), "GetStatusTarget")]
        public static class ApplyStatusAbilityDamageMultiplierPatch
        {
            public static void Postfix(ApplyStatusAbility __instance, ref object __result)
            {
                if (!(__result is ItemSlot itemSlot))
                {
                    return;
                }

                DamageMultiplierStatusDef damageMultiplierStatusDef = __instance?.ApplyStatusAbilityDef?.StatusDef as DamageMultiplierStatusDef;

                if (damageMultiplierStatusDef == null)
                {
                    return;
                }

                if (itemSlot.DamageHandler != DamageHandler.Slot)
                {
                    __result = null;
                }
            }
        }


        internal class Defs
        {
            public static void CreateDefs()
            {
                try
                {
                    CreateNanoVestAbilityAndStatus();
                    CreateParalysisDamageResistance();
                    CreateNanotechVest();
                    ModifyPoisonResVest();
                    ModifyBlastAndFireResVests();
                    CreateAcidResistantVest();

                    // New vests (requested)
                    CreateAblativeVest();
                    CreateHazmatVest();

                    MakeVestsOnlyForOrganicMeatbags();

                    // Existing research edits (kept as-is)
                    AdjustResearches();

                    // Add new vests to PX_Alien_EvolvedAliens_ResearchDef unlocks (requested)
                    AddNewVestsToEvolvedAliensResearch();

                    GetLegacyVestTemplates();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            /// <summary>
            /// Creates (idempotent) a single ApplyStatus ability + DamageMultiplierStatus for a vest.
            /// Clones:
            /// - `BionicDamageMultipliers_AbilityDef` (ApplyStatusAbilityDef) for the ability "shape"
            /// - `BionicResistances_StatusDef` (DamageMultiplierStatusDef) for the status "shape"
            /// </summary>
            private static ApplyStatusAbilityDef CreateVestResistanceAbility(
                string abilityGuid,
                string abilityDefName,
                string vedGuid,
                string vedName,
                string statusGuid,
                string statusDefName,
                string effectName,
                float multiplier,
                List<DamageTypeBaseEffectDef> damageTypes,
                string displayNameKey,
                string descriptionKey)
            {
                // Idempotency on def-name.
                ApplyStatusAbilityDef existingAbility = DefCache.GetDef<ApplyStatusAbilityDef>(abilityDefName);
                if (existingAbility != null)
                {
                    return existingAbility;
                }

                ApplyStatusAbilityDef abilitySource = DefCache.GetDef<ApplyStatusAbilityDef>("BionicDamageMultipliers_AbilityDef");


                DamageMultiplierStatusDef statusSource = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");


                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(abilitySource, abilityGuid, abilityDefName);

                // VED: owned by this vest ability (shows in item tooltip)
                newAbility.ViewElementDef = Helper.CreateDefFromClone(abilitySource.ViewElementDef, vedGuid, vedName);
                newAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind() { LocalizationKey = displayNameKey };
                newAbility.ViewElementDef.Description = new LocalizedTextBind() { LocalizationKey = descriptionKey };
                newAbility.ViewElementDef.ShowInInventoryItemTooltip = true;

                // Ensure this is a passive "always-on" status, like bionics.
                newAbility.StatusApplicationTrigger = abilitySource.StatusApplicationTrigger;
                newAbility.ApplyStatusToAllTargets = abilitySource.ApplyStatusToAllTargets;
                newAbility.TargetingDataDef = abilitySource.TargetingDataDef;
                newAbility.TargetApplicationConditions = abilitySource.TargetApplicationConditions;
                newAbility.CanApplyToOffMapTarget = abilitySource.CanApplyToOffMapTarget;
                newAbility.ShowNotificationOnUse = false;

                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(statusSource, statusGuid, statusDefName);
                newStatus.EffectName = effectName;
                newStatus.SingleInstance = true;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnHealthbar = statusSource.VisibleOnHealthbar;
                newStatus.VisibleOnStatusScreen = statusSource.VisibleOnStatusScreen;
                newStatus.Multiplier = multiplier;

                // IMPORTANT: use the ability's visuals, not the bionic visuals.
                newStatus.Visuals = newAbility.ViewElementDef;

                newStatus.DamageTypeDefs = damageTypes.ToArray(); //damageTypes?.Where(dt => dt != null).Distinct().ToArray() ?? new DamageTypeBaseEffectDef[0];

                newAbility.StatusDef = newStatus;
                return newAbility;
            }


            /// <summary>
            /// Vest 1: Ablative Vest
            /// - Torso attachment item
            /// - 10% reduced Blast + Shock damage taken
            /// - +10 HP to each bodypart (same pattern as NanoVest: ItemSlotStatsModifyStatusDef)
            /// - Research unlock: PX_Alien_EvolvedAliens_ResearchDef
            /// </summary>
            private static TacticalItemDef CreateAblativeVest()
            {
                try
                {
                    if (AblativeVestDef != null)
                    {
                        return AblativeVestDef;
                    }


                    TacticalItemDef sourceVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");

                    // One resistance ability -> one DamageMultiplierStatus with multiple damage types.
                    ApplyStatusAbilityDef ablativeResistAbility = CreateVestResistanceAbility(
                        abilityGuid: "{F6F38C9F-4EAF-4DE3-8B51-0D8A6FB8CB8B}",
                        abilityDefName: "TFTV_AblativeVest_Resistance_AbilityDef",
                        vedGuid: "{B083A226-3E9F-4D93-9D7B-2D0C7E2CD9E9}",
                        vedName: "TFTV_AblativeVest_Resistance_ViewElementDef",
                        statusGuid: "{4C0F88B2-99B0-47D3-A9DA-49A05A7C7D62}",
                        statusDefName: "TFTV_AblativeVest_Resistance_StatusDef",
                        effectName: "TFTV_AblativeVest_Resistance",
                        multiplier: 0.9f, // 10% reduction
                        damageTypes: new List<DamageTypeBaseEffectDef>
                        {
                        DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef"),
                        DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Electroshock_AttenuatingDamageTypeEffectDef"),
                        DefCache.GetDef<StandardDamageTypeEffectDef>("Bash_StandardDamageTypeEffectDef")
                        },
                        displayNameKey: "TFTV_ABLATIVEVEST_RES_NAME",
                        descriptionKey: "TFTV_ABLATIVEVEST_RES_DESC");

                    // +HP to each bodypart: ItemSlotStatsModifyStatusDef attached via a dedicated ApplyStatusAbilityDef
                    const string ablativeHealthAbilityName = "TFTV_AblativeVest_Health_AbilityDef";

                    ApplyStatusAbilityDef existingHealthAbility = DefCache.GetDef<ApplyStatusAbilityDef>(ablativeHealthAbilityName);
                    ApplyStatusAbilityDef ablativeHealthAbility = existingHealthAbility;

                    if (ablativeHealthAbility == null)
                    {
                        // Clone an existing ApplyStatus ability for reliable wiring (same pattern as Hazmat armor ability).
                        ApplyStatusAbilityDef sourceApply = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");

                        ablativeHealthAbility = Helper.CreateDefFromClone(
                            sourceApply,
                            "{D18C2E2A-2E3B-4C1C-90D2-1F1B6D2C9A0B}",
                            ablativeHealthAbilityName);

                        ablativeHealthAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            sourceApply.CharacterProgressionData,
                            "{9F8E6B21-6C4F-4B73-8F4F-9B01B9D8B3A3}",
                            ablativeHealthAbilityName);

                        ablativeHealthAbility.ViewElementDef = Helper.CreateDefFromClone(
                            sourceApply.ViewElementDef,
                            "{B3F4D1A7-1D0E-4B39-9C6E-0FBAA7E6F8A1}",
                            "TFTV_AblativeVest_Health_ViewElementDef");

                        ablativeHealthAbility.ViewElementDef.ShowInInventoryItemTooltip = true;
                        ablativeHealthAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind() { LocalizationKey = "NANOVEST_ABILITY_NAME" };
                        ablativeHealthAbility.ViewElementDef.Description = new LocalizedTextBind() { LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION" };

                        ItemSlotStatsModifyStatusDef sourceStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]");

                        ItemSlotStatsModifyStatusDef ablativeHealthStatus = Helper.CreateDefFromClone(
                            sourceStatus,
                            "{2B2C5C9F-0F7D-4B7B-8C5E-3C0B2D1A9E6F}",
                            "TFTV_AblativeVest_Health_StatusDef");

                        ablativeHealthStatus.Visuals = ablativeHealthAbility.ViewElementDef;
                        ablativeHealthStatus.Duration = -1;

                        ablativeHealthStatus.StatsModifications = new ItemSlotModification[]
                        {
                            new ItemSlotModification
                            {
                                Type = StatType.Health,
                                ModificationType = StatModificationType.AddMax,
                                Value = 10f,
                                ShowsNotification = false,
                                NotifyOnce = false
                            },
                            new ItemSlotModification
                            {
                                Type = StatType.Health,
                                ModificationType = StatModificationType.AddRestrictedToBounds,
                                Value = 10f,
                                ShowsNotification = true,
                                NotifyOnce = true
                            }
                        };

                        ablativeHealthAbility.StatusDef = ablativeHealthStatus;

                        // Keep using the shared reference the tier-upgrade system expects to adjust.
                        HealthBuffStatusDef = ablativeHealthStatus;
                    }

                    TacticalItemDef ablativeVest = Helper.CreateDefFromClone(
                        sourceVest,
                        "{B0E2F4CE-2A31-4877-9C21-2A27A9B6B7D4}",
                        AblativeVestDefName);

                    ablativeVest.ViewElementDef = Helper.CreateDefFromClone(
                        sourceVest.ViewElementDef,
                        "{AD93CFF1-06E9-4D94-9B5E-5B5C9F3F7C2B}",
                        "TFTV_AblativeVest_ViewElementDef");
                    ablativeVest.ViewElementDef.DisplayName1 = new LocalizedTextBind() { LocalizationKey = "TFTV_ABLATIVEVEST_NAME" };
                    ablativeVest.ViewElementDef.DisplayName2 = new LocalizedTextBind() { LocalizationKey = "TFTV_ABLATIVEVEST_NAME" };
                    ablativeVest.ViewElementDef.Description = new LocalizedTextBind() { LocalizationKey = "TFTV_ABLATIVEVEST_DESC" };

                    var icon = Helper.CreateSpriteFromImageFile("vest_ablative_0.png");
                    ablativeVest.ViewElementDef.LargeIcon = icon;
                    ablativeVest.ViewElementDef.InventoryIcon = icon;

                    // Single resistance ability + dedicated HP buff ability
                    ablativeVest.Abilities = new AbilityDef[]
                    {
                    ablativeResistAbility,
                    ablativeHealthAbility
                    };

                    ablativeVest.ManufactureTech = Math.Max(ablativeVest.ManufactureTech, 25);
                    ablativeVest.ManufactureMaterials = Math.Max(ablativeVest.ManufactureMaterials, 35);

                    AblativeVestDef = ablativeVest;
                    AblativeResistancesDef = ablativeResistAbility.StatusDef as DamageMultiplierStatusDef;

                    return ablativeVest;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// Vest 2: Hazmat Vest
            /// - Torso attachment item
            /// - 20% reduced Fire, Poison, Acid, Paralysis, Sonic, Viral damage taken
            /// - +2 armor to each body part (ItemSlotStatsModifyStatusDef)
            /// - Research unlock: PX_Alien_EvolvedAliens_ResearchDef
            /// </summary>
            private static TacticalItemDef CreateHazmatVest()
            {
                try
                {
                    if (HazmatVestDef != null)
                    {
                        return HazmatVestDef;
                    }

                    TacticalItemDef sourceVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");

                    // One resistance ability -> one DamageMultiplierStatus with multiple damage types.
                    ApplyStatusAbilityDef hazmatResistAbility = CreateVestResistanceAbility(
                        abilityGuid: "{7E4D7A8D-950F-4E4F-AC0B-7C6E1B64B0ED}",
                        abilityDefName: "TFTV_HazmatVest_Resistance_AbilityDef",
                        vedGuid: "{2F3258C2-2D7B-4A8B-9A4C-0C3F5C8E4C72}",
                        vedName: "TFTV_HazmatVest_Resistance_ViewElementDef",
                        statusGuid: "{2EBCB5D3-87C9-4B05-9D8F-2C60E0B8C33A}",
                        statusDefName: "TFTV_HazmatVest_Resistance_StatusDef",
                        effectName: "TFTV_HazmatVest_Resistance",
                        multiplier: 0.8f, // 20% reduction
                        damageTypes: new List<DamageTypeBaseEffectDef>
                        {
                        DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef"),
                        DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Poison_DamageOverTimeDamageTypeEffectDef"),
                        DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef"),
                        DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef"),
                        DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Sonic_AttenuatingDamageTypeEffectDef"),
                        DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef"),
                        },
                        displayNameKey: "TFTV_HAZMATVEST_RES_NAME",
                        descriptionKey: "TFTV_HAZMATVEST_RES_DESC");



                    // +2 armor to each bodypart: ItemSlotStatsModifyStatusDef attached via ApplyStatusAbilityDef.
                    const string hazmatArmorAbilityName = "TFTV_HazmatVest_Armor_AbilityDef";

                    // Clone an existing ApplyStatus ability for reliable wiring.
                    ApplyStatusAbilityDef sourceApply = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");

                    ApplyStatusAbilityDef hazmatArmorAbility = Helper.CreateDefFromClone(
                        sourceApply,
                        "{7D7DE5C1-0A5D-4B4A-8E5D-0D6AA0A0C1D2}",
                        hazmatArmorAbilityName);

                    hazmatArmorAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        sourceApply.CharacterProgressionData,
                        "{C2A3E2B1-1BDE-4C2B-86A0-7A3B2E2E3D21}",
                        hazmatArmorAbilityName);

                    hazmatArmorAbility.ViewElementDef = Helper.CreateDefFromClone(
                        sourceApply.ViewElementDef,
                        "{A13D7B45-7D11-4C0B-9C44-0A2E3B8EF8A2}",
                        "TFTV_HazmatVest_Armor_ViewElementDef");

                    hazmatArmorAbility.ViewElementDef.ShowInInventoryItemTooltip = true;
                    hazmatArmorAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind() { LocalizationKey = "TFTV_HAZMATVEST_ARMOR_NAME" };
                    hazmatArmorAbility.ViewElementDef.Description = new LocalizedTextBind() { LocalizationKey = "TFTV_HAZMATVEST_ARMOR_DESC" };

                    ItemSlotStatsModifyStatusDef sourceStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]");

                    ItemSlotStatsModifyStatusDef hazmatArmorStatus = Helper.CreateDefFromClone(
                        sourceStatus,
                        "{E0D9CB18-AB7A-4E0B-9F8B-8F01A50C3BE0}",
                        "TFTV_HazmatVest_Armor_StatusDef");

                    hazmatArmorStatus.Visuals = hazmatArmorAbility.ViewElementDef;
                    hazmatArmorStatus.Duration = -1;

                    hazmatArmorStatus.StatsModifications = new ItemSlotModification[]
                    {
                        new ItemSlotModification
                        {
                            Type = StatType.Armour,
                            ModificationType = StatModificationType.AddMax,
                            Value = 2f,
                            ShowsNotification = false,
                            NotifyOnce = false
                        },
                        new ItemSlotModification
                        {
                            Type = StatType.Armour,
                            ModificationType = StatModificationType.AddRestrictedToBounds,
                            Value = 2f,
                            ShowsNotification = false,
                            NotifyOnce = false
                        }
                    };

                    hazmatArmorAbility.StatusDef = hazmatArmorStatus;





                    TacticalItemDef hazmatVest = Helper.CreateDefFromClone(
                        sourceVest,
                        "{4E2EA3E7-42D2-4F26-8C4B-7F32F2C027CE}",
                        HazmatVestDefName);

                    hazmatVest.ViewElementDef = Helper.CreateDefFromClone(
                        sourceVest.ViewElementDef,
                        "{9B3A2D0A-0B4E-4D7D-8B9A-2B3B3A8C3B17}",
                        "TFTV_HazmatVest_ViewElementDef");
                    hazmatVest.ViewElementDef.DisplayName1 = new LocalizedTextBind() { LocalizationKey = "TFTV_HAZMATVEST_NAME" };
                    hazmatVest.ViewElementDef.DisplayName2 = new LocalizedTextBind() { LocalizationKey = "TFTV_HAZMATVEST_NAME" };
                    hazmatVest.ViewElementDef.Description = new LocalizedTextBind() { LocalizationKey = "TFTV_HAZMATVEST_DESC" };

                    var icon = Helper.CreateSpriteFromImageFile("vest_hazmat_0.png");
                    hazmatVest.ViewElementDef.LargeIcon = icon;
                    hazmatVest.ViewElementDef.InventoryIcon = icon;

                    hazmatVest.Abilities = new AbilityDef[]
                    {
                    hazmatResistAbility,
                    hazmatArmorAbility
                    };

                    hazmatVest.ManufactureTech = Math.Max(hazmatVest.ManufactureTech, 30);
                    hazmatVest.ManufactureMaterials = Math.Max(hazmatVest.ManufactureMaterials, 45);

                    HazmatVestDef = hazmatVest;
                    HazmatResistancesDef = hazmatResistAbility.StatusDef as DamageMultiplierStatusDef;
                    ArmorBuffStatusDef = hazmatArmorStatus;


                    return hazmatVest;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// Adds `TFTV_AblativeVest_Attachment_ItemDef` and `TFTV_HazmatVest_Attachment_ItemDef`
            /// to PX_Alien_EvolvedAliens_ResearchDef manufacturing unlocks.
            /// Idempotent: will not add duplicates.
            /// </summary>
            private static void AddNewVestsToEvolvedAliensResearch()
            {
                try
                {
                    ResearchDef evolvedAliens = DefCache.GetDef<ResearchDef>("PX_Alien_EvolvedAliens_ResearchDef");


                    TacticalItemDef ablative = DefCache.GetDef<TacticalItemDef>(AblativeVestDefName);
                    TacticalItemDef hazmat = DefCache.GetDef<TacticalItemDef>(HazmatVestDefName);

                    // Find an existing ManufactureResearchRewardDef on this research; otherwise create one by cloning a known reward def.
                    ManufactureResearchRewardDef manuReward =
                        evolvedAliens.Unlocks?.OfType<ManufactureResearchRewardDef>().FirstOrDefault();

                    if (manuReward == null)
                    {
                        ManufactureResearchRewardDef template = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0");


                        manuReward = Helper.CreateDefFromClone(
                            template,
                            "{6A9A4F8C-6F0B-46E1-9D73-7E0A5D1D0E10}",
                            "PX_Alien_EvolvedAliens_ManufactureResearchRewardDef_TFTV_Vests");

                        // start clean and only add our items (avoid accidentally inheriting unrelated items)
                        manuReward.Items = new ItemDef[] { };

                        List<ResearchRewardDef> unlocks = (evolvedAliens.Unlocks ?? new ResearchRewardDef[] { }).ToList();
                        unlocks.Add(manuReward);
                        evolvedAliens.Unlocks = unlocks.ToArray();
                    }

                    List<ItemDef> items = (manuReward.Items ?? new ItemDef[] { }).ToList();

                    if (!items.Contains(ablative))
                    {
                        items.Add(ablative);
                    }

                    if (!items.Contains(hazmat))
                    {
                        items.Add(hazmat);
                    }

                    manuReward.Items = items.ToArray();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            // Existing methods below (unchanged)
            private static void ModifyPoisonResVest()
            {
                try
                {
                    TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                    DamageMultiplierAbilityDef ParalysisNotShockResistance = DefCache.GetDef<DamageMultiplierAbilityDef>("ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");

                    //Not working correctly
                    //DamageMultiplierAbilityDef ExtraHealing = DefCache.GetDef<DamageMultiplierAbilityDef>("ExtraHealing_DamageMultiplierAbilityDef");

                    poisonVest.Abilities = new AbilityDef[] { poisonVest.Abilities[0], ParalysisNotShockResistance };
                    // TFTVAircraftRework.PoisonVestResistance = (DamageMultiplierAbilityDef)poisonVest.Abilities[0];
                   // TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)poisonVest.Abilities[0]);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateNanoVestAbilityAndStatus()
            {
                try
                {

                    string skillName = "NanoVest_AbilityDef";
                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");
                    ApplyStatusAbilityDef nanoVestAbility = Helper.CreateDefFromClone(
                        source,
                        "{FEF02379-A90F-4670-8FD7-574CDCB5753F}",
                        skillName);
                    nanoVestAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "{15458996-77C2-4F9B-8E31-0DD1A6D77571}",
                        skillName);
                    nanoVestAbility.TargetingDataDef = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef").TargetingDataDef;
                    nanoVestAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "{8959A8C5-0405-4D46-8632-0CCA9EF029DB}",
                        skillName);
                    nanoVestAbility.ViewElementDef.ShowInInventoryItemTooltip = true;

                    nanoVestAbility.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
                    nanoVestAbility.ViewElementDef.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
                    nanoVestAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("module_nanovest_ability.png");
                    nanoVestAbility.ViewElementDef.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;

                    string statusName = "NanoVest_StatusDef";
                    ItemSlotStatsModifyStatusDef nanoVestBuffStatus = Helper.CreateDefFromClone(
                        DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]"),
                        "{57E033FC-FECD-4A9E-8AE6-CC17FB5116A9}",
                        statusName);
                    nanoVestBuffStatus.Visuals = Helper.CreateDefFromClone(
                        nanoVestAbility.ViewElementDef,
                        "{8F111A9C-020C-4166-9444-1211CF517884}",
                        statusName);

                    nanoVestBuffStatus.Duration = -1;

                    nanoVestBuffStatus.Visuals.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
                    nanoVestBuffStatus.Visuals.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
                    nanoVestBuffStatus.Visuals.LargeIcon = nanoVestAbility.ViewElementDef.LargeIcon;
                    nanoVestBuffStatus.Visuals.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;
                    nanoVestBuffStatus.StatsModifications = new ItemSlotModification[]
                    {
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddMax,
            Value = 10f,
            ShowsNotification = false,
            NotifyOnce = false
        },
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddRestrictedToBounds,
            Value = 10f,
            ShowsNotification = true,
            NotifyOnce = true
        }
                    };

                    nanoVestAbility.StatusDef = nanoVestBuffStatus;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ModifyBlastAndFireResVests()
            {
                try
                {


                    TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                    TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                    blastVest.Abilities = new AbilityDef[] { fireVest.Abilities[0], blastVest.Abilities[0] };
                    blastVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_blastresvest.png");
                    blastVest.ViewElementDef.InventoryIcon = blastVest.ViewElementDef.LargeIcon;
                    // TFTVAircraftRework.BlastVestResistance = (DamageMultiplierAbilityDef)blastVest.Abilities[0];
                    // TFTVAircraftRework.FireVestResistance = (DamageMultiplierAbilityDef)fireVest.Abilities[0];
                  //  TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)blastVest.Abilities[0]);
                  //  TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)fireVest.Abilities[0]);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            

            private static void GetLegacyVestTemplates()
            {

            string BlastVestDefName = "PX_BlastResistanceVest_Attachment_ItemDef";
            string FireVestDefName = "NJ_FireResistanceVest_Attachment_ItemDef";
            string PoisonVestDefName = "SY_PoisonResistanceVest_Attachment_ItemDef";
            string NanoVestDefName = "NanotechVest";

                List<TacticalItemDef> legacyVests = new List<TacticalItemDef>()
            {
            DefCache.GetDef<TacticalItemDef>(BlastVestDefName),
            DefCache.GetDef<TacticalItemDef>(FireVestDefName),
            DefCache.GetDef<TacticalItemDef>(PoisonVestDefName),
            DefCache.GetDef<TacticalItemDef>(NanoVestDefName)

            };

                

                List<TacCharacterDef> tacCharacterDefs = new List<TacCharacterDef>()
                {
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault7_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault7_recruitable_CharacterTemplateDef"),       
                    DefCache.GetDef<TacCharacterDef>("NJ_Sniper6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Sniper7_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Assault6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Assault7_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Infiltrator6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Infiltrator7_CharacterTemplateDef"),

                    DefCache.GetDef<TacCharacterDef>("SY_Sniper6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Sniper6_P_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Sniper7_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("SY_Sniper7_P_CharacterTemplateDef"),

                };

                foreach (TacCharacterDef tacCharacterDef in tacCharacterDefs)
                {
                    List <ItemDef> itemDefs = tacCharacterDef.Data.BodypartItems.ToList();
                    itemDefs.RemoveAll(item => legacyVests.Contains(item));
                    tacCharacterDef.Data.BodypartItems = itemDefs.ToArray();

                }


            }



            private static void CreateNanotechVest()
            {
                try
                {

                    TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");

                    FactionTagDef nJTag = DefCache.GetDef<FactionTagDef>("NewJerico_FactionTagDef");
                    FactionTagDef pXTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");


                    if (fireVest.Tags.Contains(nJTag))
                    {
                        fireVest.Tags.Remove(nJTag);

                    }

                    if (!fireVest.Tags.Contains(pXTag))
                    {
                        fireVest.Tags.Add(pXTag);

                    }

                    TacticalItemDef newNanoVest = Helper.CreateDefFromClone(fireVest, "{D07B639A-E1F4-46F4-91BB-1CCDCCCE8EC1}", "NanotechVest");
                    newNanoVest.ViewElementDef = Helper.CreateDefFromClone(fireVest.ViewElementDef, "{0F1BD9BA-1895-46C7-90AF-26FB92D702F6}", "Nanotech_ViewElement");


                    newNanoVest.Abilities = new AbilityDef[] { DefCache.GetDef<ApplyStatusAbilityDef>("NanoVest_AbilityDef") };
                    newNanoVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_nanovest.png");
                    newNanoVest.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_NAME";
                    newNanoVest.ViewElementDef.DisplayName2.LocalizationKey = "NANOVEST_NAME";
                    newNanoVest.ViewElementDef.Description.LocalizationKey = "NANOVEST_DESCRIPTION";
                    newNanoVest.ViewElementDef.InventoryIcon = newNanoVest.ViewElementDef.LargeIcon;

                    newNanoVest.ManufactureTech = 20;
                    newNanoVest.ManufactureMaterials = 30;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void CreateAcidResistantVest()
            {
                try
                {
                    TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                    fireVest.Abilities = new AbilityDef[] { DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef") };
                    fireVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_fireresvest.png");
                    fireVest.ViewElementDef.InventoryIcon = fireVest.ViewElementDef.LargeIcon;
                   // TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)fireVest.Abilities[0]);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            private static void CreateParalysisDamageResistance()
            {
                try
                {
                    DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                    DamageMultiplierAbilityDef ParalysisNotShockResistance = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "{A044047F-A462-46FC-B06A-191181B67800}", "ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");
                    ParalysisNotShockResistance.DamageTypeDef = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
                    ParalysisNotShockResistance.Multiplier = 0.5f;
                    ParalysisNotShockResistance.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "{F157A5A2-16A0-491A-ABE8-6CF88DEBE1DF}", "ParalysisNotShockImmunityResistance_ViewElementDef");
                    ParalysisNotShockResistance.ViewElementDef.DisplayName1.LocalizationKey = "RESISTANCE_TO_PARALYSIS_NAME";
                    ParalysisNotShockResistance.ViewElementDef.Description.LocalizationKey = "RESISTANCE_TO_PARALYSIS_DESCRIPTION";
                    ParalysisNotShockResistance.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                    ParalysisNotShockResistance.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                    // TFTVAircraftRework.ParalysysVestResistance = ParalysisNotShcokResistance;
                   // TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add(ParalysisNotShockResistance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void MakeVestsOnlyForOrganicMeatbags()
            {
                try
                {


                    GameTagDef organicMeatbagTorsoTag = TFTVCommonMethods.CreateNewTag("MeatBagTorso", "{8D13AAD6-BA65-4907-B3C8-C977B819BF48}");

                    GameTagDef rocketMountTorsoTag = TFTVCommonMethods.CreateNewTag("RocketMountTorso", "{04ABD5E5-6666-4E8E-AF19-EF958315CDE1}");

                    foreach (TacticalItemDef item in Repo.GetAllDefs<TacticalItemDef>()

                        .Where(ti => ti.name.Contains("Torso"))

                        .Where(ti => ti.name.StartsWith("AN_") || ti.name.StartsWith("SY_") || ti.name.StartsWith("NJ_")
                        || ti.name.StartsWith("NEU") || ti.name.StartsWith("PX_") || ti.name.StartsWith("IN_")))

                    {
                        if (!item.Tags.Contains(organicMeatbagTorsoTag) && !item.name.Contains("BIO"))
                        {
                            item.Tags.Add(organicMeatbagTorsoTag);
                            //  TFTVLogger.Always($"adding organicMeatbagTorsoTag to {item.name}");
                        }

                        if (!item.Tags.Contains(rocketMountTorsoTag) &&
                            item.Tags.Contains(DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef"))
                            && !item.name.StartsWith("IN_"))
                        {
                            item.Tags.Add(rocketMountTorsoTag);
                            //  TFTVLogger.Always($"adding rocketMountTorsoTag to {item.name}");
                        }
                    }

                    WeaponDef fury = DefCache.GetDef<WeaponDef>("NJ_RocketLauncherPack_WeaponDef");
                    WeaponDef thor = DefCache.GetDef<WeaponDef>("NJ_GuidedMissileLauncherPack_WeaponDef");
                    WeaponDef destiny = DefCache.GetDef<WeaponDef>("PX_LaserArrayPack_WeaponDef");
                    WeaponDef ragnarok = DefCache.GetDef<WeaponDef>("PX_ShredingMissileLauncherPack_WeaponDef");

                    fury.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                    thor.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                    destiny.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                    ragnarok.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;

                    TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                    TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                    TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                    TacticalItemDef nanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                    blastVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                    poisonVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                    fireVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                    nanoVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                    HazmatVestDef.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                    AblativeVestDef.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;

                    TacticalItemDef indepHeavyArmor = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Torso_BodyPartDef");
                    TacticalItemDef njHeavyArmor = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Torso_BodyPartDef");
                    indepHeavyArmor.ProvidedSlots = new ProvidedSlotBind[] { indepHeavyArmor.ProvidedSlots[0], njHeavyArmor.ProvidedSlots[0] };


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

           

          

            private static void AdjustResearches()
            {
                try
                {
                    //nanovest removed
                    //blastvest removed
                    //acidvest removed
                    //poisonvest removed

                    ResearchDef terrorSentinelResearch = DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef");
                    ManufactureResearchRewardDef advNanotechRewards = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0"); //Motion detector
                    ManufactureResearchRewardDef newRewardsForTerrorSentinel = Helper.CreateDefFromClone(advNanotechRewards, "{41636380-9889-4D4A-8E0A-8D32A9196DD1}", terrorSentinelResearch.name + "ManuReward");

                    ResearchDef reverseEngineeringMVS = DefCache.GetDef<ResearchDef>("PX_SY_MultiVisualSensor_Attachment_ItemDef_ResearchDef");

                    ResearchDef reverseEngineeringMotionDetector = DefCache.GetDef<ResearchDef>("PX_SY_MotionDetector_Attachment_ItemDef_ResearchDef");

                    ResearchDef reverseEngineeringAcidVest = DefCache.GetDef<ResearchDef>("PX_NJ_FireResistanceVest_Attachment_ItemDef_ResearchDef");

                    ResearchDbDef pxResearch = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                    if (pxResearch.Researches.Contains(reverseEngineeringMVS))
                    {
                        pxResearch.Researches.Remove(reverseEngineeringMVS);
                    }

                    if (pxResearch.Researches.Contains(reverseEngineeringMotionDetector))
                    {
                        pxResearch.Researches.Remove(reverseEngineeringMotionDetector);
                    }

                    if (pxResearch.Researches.Contains(reverseEngineeringAcidVest))
                    {
                        pxResearch.Researches.Remove(reverseEngineeringAcidVest);
                    }

                    //Moving Motion Detection Module to Terror Sentinel Autopsy               
                    terrorSentinelResearch.Unlocks = new ResearchRewardDef[] { terrorSentinelResearch.Unlocks[0], newRewardsForTerrorSentinel };

                    //Remove adv nanotech buff and add Repair Kit to manufacturing reward

                    ResearchDef advNanotechRes = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");
                    //  advNanotechRes.ViewElementDef.BenefitsText = new LocalizedTextBind() { }; // DefCache.GetDef<ResearchViewElementDef>("PX_ShardGun_ViewElementDef").BenefitsText;
                    advNanotechRes.Unlocks = new ResearchRewardDef[] { advNanotechRes.Unlocks[0] };

                    EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");


                    //removing nanokit from Bionic Reserach
                    ManufactureResearchRewardDef bionicsReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_Bionics1_ResearchDef_ManufactureResearchRewardDef_0");
                    bionicsReward.Items = new ItemDef[] { bionicsReward.Items[0], bionicsReward.Items[1], bionicsReward.Items[2] };


                    TacticalItemDef newNanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                    List<ItemDef> manuRewards = new List<ItemDef>() { repairKit };//, newNanoVest }; removing Nanovest from manufacturing
                    advNanotechRewards.Items = manuRewards.ToArray();

                 /*   TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                    ManufactureResearchRewardDef njFireReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_PurificationTech_ResearchDef_ManufactureResearchRewardDef_0");
                    List<ItemDef> itemDefs = new List<ItemDef>(njFireReward.Items) { blastVest };
                    njFireReward.Items = itemDefs.ToArray();*/
                    //remove NJ Fire Resistance tech, folding it into fire tech, and removed the blastVest 

                

                    ResearchDbDef njResearch = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                    njResearch.Researches.Remove(DefCache.GetDef<ResearchDef>("NJ_FireResistanceTech_ResearchDef"));

                    //Fireworm unlocks Vidar
                    DefCache.GetDef<ExistingResearchRequirementDef>("PX_AGL_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Fireworm_ResearchDef";

                    //Blast res research changed to acid res, because blast vest moved to NJ Fire Tech research
                    //Acidworm unlocks BlastResTech, which is now AcidResTech

                    //removing acid vest research
                    /* TacticalItemDef acidVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                     ManufactureResearchRewardDef pxBlastResReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_BlastResistanceVest_ResearchDef_ManufactureResearchRewardDef_0");
                     pxBlastResReward.Items = new ItemDef[] { acidVest };
                     DefCache.GetDef<ExistingResearchRequirementDef>("PX_BlastResistanceVest_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Acidworm_ResearchDef";

                     DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef").ViewElementDef.BenefitsText.LocalizationKey = "PX_ALIEN_ACIDWORM_RESEARCHDEF_BENEFITS";*/

                    pxResearch.Researches.Remove(DefCache.GetDef<ResearchDef>("PX_BlastResistanceVest_ResearchDef"));
                    ResearchDbDef synResearch = DefCache.GetDef<ResearchDbDef>("syn_ResearchDB");
                    synResearch.Researches.Remove(DefCache.GetDef<ResearchDef>("SYN_PoisonResistance_ResearchDef"));

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        internal class Tiers
        {
            [HarmonyPatch(typeof(GeoFaction), "OnResearchCompleted")]
            internal static class VestTierUpgradesPatch
            {
                private static readonly IReadOnlyList<VestTierDefinition> AblativeTiers = new List<VestTierDefinition>
        {
            new VestTierDefinition(
                isUnlocked: null,
                resistanceMultiplier: 0.9f,
                healthBonus: 10f,
                displayNameKey: "TFTV_ABLATIVEVEST_NAME",
                resistanceDesc: "TFTV_ABLATIVEVEST_RES_DESC",
                buffDesc: "NANOVEST_ABILITY_DESCRIPTION",
                iconFileName: "vest_ablative_0.png"),
            new VestTierDefinition(
                isUnlocked: faction => IsAnyResearchCompleted(faction, WormResearchDefNames),
                resistanceMultiplier: 0.8f,
                healthBonus: 15f,
                displayNameKey: "TFTV_ABLATIVEVEST_NAME_T2",
                resistanceDesc: "TFTV_ABLATIVEVEST_RES_DESC_T2",
                buffDesc: "NANOVEST_ABILITY_DESCRIPTION_T2",
                iconFileName: "vest_ablative_1.png"),
            new VestTierDefinition(
                isUnlocked: faction => IsResearchCompleted(faction, "PX_Alien_LiveChiron_ResearchDef"),
                resistanceMultiplier: 0.7f,
                healthBonus: 20f,
                displayNameKey: "TFTV_ABLATIVEVEST_NAME_T3",
                resistanceDesc: "TFTV_ABLATIVEVEST_RES_DESC_T3",
                buffDesc: "NANOVEST_ABILITY_DESCRIPTION_T3",
                iconFileName: "vest_ablative_2.png")
        };

                private static readonly IReadOnlyList<VestTierDefinition> HazmatTiers = new List<VestTierDefinition>
        {
            new VestTierDefinition(
                isUnlocked: null,
                resistanceMultiplier: 0.8f,
                armorBonus: 2f,
                displayNameKey: "TFTV_HAZMATVEST_NAME",
                resistanceDesc: "TFTV_HAZMATVEST_RES_DESC",
                buffDesc: "TFTV_HAZMATVEST_ARMOR_DESC",
                iconFileName: "vest_hazmat_0.png"),
            new VestTierDefinition(
                isUnlocked: faction => IsResearchCompleted(faction, "PX_Alien_LiveSwarmer_ResearchDef"),
                resistanceMultiplier: 0.65f,
                armorBonus: 4f,
                displayNameKey: "TFTV_HAZMATVEST_NAME_T2",
                resistanceDesc: "TFTV_HAZMATVEST_RES_DESC_T2",
                buffDesc: "TFTV_HAZMATVEST_ARMOR_DESC_T2",
                iconFileName: "vest_hazmat_1.png"),
            new VestTierDefinition(
                isUnlocked: faction => IsResearchCompleted(faction, "PX_Alien_LiveAcheron_ResearchDef"),
                resistanceMultiplier: 0.5f,
                armorBonus: 6f,
                displayNameKey: "TFTV_HAZMATVEST_NAME_T3",
                resistanceDesc: "TFTV_HAZMATVEST_RES_DESC_T3",
                buffDesc: "TFTV_HAZMATVEST_ARMOR_DESC_T3",
                iconFileName: "vest_hazmat_2.png")
        };

                private static readonly string[] WormResearchDefNames =
                {
            "PX_Alien_LiveAcidworm_ResearchDef",
            "PX_Alien_LiveFireworm_ResearchDef",
            "PX_Alien_LivePoisonworm_ResearchDef"
        };

                private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

                [HarmonyPostfix]
                private static void OnResearchCompletedPostfix(GeoFaction __instance)
                {
                    ApplyTierUpgrades(__instance);
                }

                [HarmonyPatch(typeof(GeoFaction), "RebuildBonusesFromResearchState")]
                [HarmonyPostfix]
                private static void RebuildBonusesFromResearchStatePostfix(GeoFaction __instance)
                {
                    ApplyTierUpgrades(__instance);
                }

                private static void ApplyTierUpgrades(GeoFaction faction)
                {
                    if (!(faction is GeoPhoenixFaction))
                    {
                        return;
                    }

                    TacticalItemDef ablativeVest = DefCache.GetDef<TacticalItemDef>(AblativeVestDefName);
                    TacticalItemDef hazmatVest = DefCache.GetDef<TacticalItemDef>(HazmatVestDefName);

                    ApplyTierToVest(faction, ablativeVest, AblativeTiers, AblativeResistancesDef, HealthBuffStatusDef);
                    ApplyTierToVest(faction, hazmatVest, HazmatTiers, HazmatResistancesDef, ArmorBuffStatusDef);
                }

                private static void ApplyTierToVest(
                    GeoFaction faction,
                    TacticalItemDef vestDef,
                    IReadOnlyList<VestTierDefinition> tiers,
                    DamageMultiplierStatusDef resistanceStatusDef,
                    ItemSlotStatsModifyStatusDef buffStatusDef)
                {
                   
                    int tierIndex = GetActiveTierIndex(faction, tiers);
                    VestTierDefinition tier = tiers[tierIndex];

                    UpdateVestView(vestDef, tier);
                    UpdateResistanceStatus(resistanceStatusDef, tier);
                    UpdateArmorStatus(buffStatusDef, tier);
                    UpdateHealthStatus(buffStatusDef, tier);
                }

                private static int GetActiveTierIndex(GeoFaction faction, IReadOnlyList<VestTierDefinition> tiers)
                {
                    for (int i = tiers.Count - 1; i >= 0; i--)
                    {
                        Func<GeoFaction, bool> isUnlocked = tiers[i].IsUnlocked;
                        if (isUnlocked == null)
                        {
                            return i;
                        }

                        if (isUnlocked(faction))
                        {
                            return i;
                        }
                    }

                    return 0;
                }

                private static bool IsResearchCompleted(GeoFaction faction, string researchDefName)
                {
                    return faction.Research.Completed.Any(research =>
                        research?.ResearchDef != null && research.ResearchDef.Id == researchDefName);
                }

                private static void UpdateVestView(TacticalItemDef vestDef, VestTierDefinition tier)
                {
                    ViewElementDef viewElement = vestDef.ViewElementDef;
                    if (viewElement == null)
                    {
                        return;
                    }

                    viewElement.DisplayName1 = new LocalizedTextBind
                    {
                        LocalizationKey = tier.DisplayNameKey
                    };
                    viewElement.DisplayName2 = new LocalizedTextBind
                    {
                        LocalizationKey = tier.DisplayNameKey
                    };

                    Sprite icon = Helper.CreateSpriteFromImageFile(tier.IconFileName);
                    if (icon != null)
                    {
                        viewElement.LargeIcon = icon;
                        viewElement.InventoryIcon = icon;
                    }

                }

                private static void UpdateResistanceStatus(DamageMultiplierStatusDef statusDef, VestTierDefinition tier)
                {
                   
                    statusDef.Multiplier = tier.ResistanceMultiplier;
                    if (statusDef.Visuals != null)
                    {
                        statusDef.Visuals.Description = new LocalizedTextBind
                        {
                            LocalizationKey = tier.ResistanceDesc
                        };
                    }
                }

                private static void UpdateArmorStatus(ItemSlotStatsModifyStatusDef armorStatusDef, VestTierDefinition tier)
                {
                    if (!tier.ArmorBonus.HasValue)
                    {
                        return;
                    }

                    foreach (ItemSlotModification modification in armorStatusDef.StatsModifications)
                    {
                        if (modification.Type == StatType.Armour)
                        {
                            modification.Value = tier.ArmorBonus.Value;
                        }
                    }

                    armorStatusDef.Visuals.Description = new LocalizedTextBind
                    {
                        LocalizationKey = tier.BuffDesc
                    };
                }

                private static void UpdateHealthStatus(ItemSlotStatsModifyStatusDef healthStatusDef, VestTierDefinition tier)
                {
                   
                    if (!tier.HealthBonus.HasValue)
                    {
                        return;
                    }



                    foreach (ItemSlotModification modification in healthStatusDef.StatsModifications)
                    {
                      

                        if (modification.Type == StatType.Health)
                        {
                            modification.Value = tier.HealthBonus.Value;
                        }
                    }



                    healthStatusDef.Visuals.Description = new LocalizedTextBind
                    {
                        LocalizationKey = tier.BuffDesc
                    };
                }

                private static bool IsAnyResearchCompleted(GeoFaction faction, IEnumerable<string> researchDefNames)
                {
                    foreach (string researchDefName in researchDefNames)
                    {
                        if (IsResearchCompleted(faction, researchDefName))
                        {
                            return true;
                        }
                    }

                    return false;
                }



                private sealed class VestTierDefinition
                {
                    public VestTierDefinition(
                        Func<GeoFaction, bool> isUnlocked,
                        float resistanceMultiplier,
                        string displayNameKey,
                        string resistanceDesc,
                        string buffDesc,
                        string iconFileName,
                        float? armorBonus = null,
                        float? healthBonus = null)
                    {
                        IsUnlocked = isUnlocked;
                        ResistanceMultiplier = resistanceMultiplier;
                        DisplayNameKey = displayNameKey;
                        ResistanceDesc = resistanceDesc;
                        BuffDesc = buffDesc;
                        IconFileName = iconFileName;
                        ArmorBonus = armorBonus;
                        HealthBonus = healthBonus;
                    }

                    public Func<GeoFaction, bool> IsUnlocked { get; }

                    public float ResistanceMultiplier { get; }

                    public string DisplayNameKey { get; }

                    public string ResistanceDesc { get; }

                    public string BuffDesc { get; }

                    public string IconFileName { get; }

                    public float? ArmorBonus { get; }

                    public float? HealthBonus { get; }
                }
            }


        }
    }
}
