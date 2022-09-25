using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PRMBetterClasses.Tactical.Entities.DamageKeywords;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class SkillModsMain
    {

        public static SharedSoloEffectorDamageKeywordsDataDef sharedSoloDamageKeywords;

        public static void ApplyChanges()
        {
            try
            {
                // Create solo DamageKeywords
                sharedSoloDamageKeywords = new SharedSoloEffectorDamageKeywordsDataDef();

                // Change Recover to reduce viral by half
                Change_RecoverToReduceViral();

                // Change stealth ability and indicator skill and apply on all base class proficiency skills
                Apply_StealthIndicator_AllClasses();

                // Assault skills ------------------------------------------------------
                AssaultSkills.ApplyChanges();

                // Sniper skills start ------------------------------------------------------
                SniperSkills.ApplyChanges();

                // Heavy skills start --------------------------------------------------------
                HeavySkills.ApplyChanges();

                // Berserker skills start ----------------------------------------------------
                BerserkerSkills.ApplyChanges();

                // Infiltrator skills start --------------------------------------------------
                InfiltratorSkills.ApplyChanges();

                // Technician skills start ---------------------------------------------------
                TechnicianSkills.ApplyChanges();

                // Priest skills start -------------------------------------------------------
                PriestSkills.ApplyChanges();

                // Call Background perk changes -------------------------------------------------------
                BackgroundPerks.ApplyChanges();

                // Faction perks
                FactionPerks.ApplyChanges();

                // Tweaking the weapon proficiency perks incl. descriptions, see below
                Change_ProficiencyPerks();

                // BattleFocus, currently used as placeholder, will go to Vengeance Torso
                Create_BattleFocus();

                // Set SP for all skills according to where they are set
                Set_SPcost();
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
            }
        }

        // Make ViralValueChange_EffectDef accessible to Harmony patch for Revover ability (see below)
        internal static ChangeStatusValueEffectDef ViralValueChange_EffectDef;
        private static void Change_RecoverToReduceViral()
        {
            DefRepository Repo = TFTVMain.Repo;
            // Change description of Recover ability to reflect that it reduces Virus by half
            RecoverWillAbilityDef recover = Repo.GetAllDefs<RecoverWillAbilityDef>().FirstOrDefault(rw => rw.name.Equals("RecoverWill_AbilityDef"));
            recover.ViewElementDef.Description.LocalizationKey = "PR_BC_RECOVER_DESC";
            // Create a new effect to change the virus value, cloned from 'ParalysisValueChange_0.5_EffectDef'
            ViralValueChange_EffectDef = Helper.CreateDefFromClone(
                Repo.GetAllDefs<ChangeStatusValueEffectDef>().FirstOrDefault(csv => csv.name.Equals("ParalysisValueChange_0.5_EffectDef")),
                "1bb3b06f-55d5-44a0-8cf7-e5382577c4df",
                "ViralValueChange_0.5_EffectDef");
            ViralValueChange_EffectDef.StatusDef = Repo.GetAllDefs<TacStatusDef>().FirstOrDefault(ts => ts.name.Equals("Infected_StatusDef")); // Infected_StatusDef = virus applied
        }
        // Recover ability: Patching GetWillpowerRecover when character uses Recover to also reduce viral value by half
        [HarmonyPatch(typeof(RecoverWillAbility), "GetStatusSource")]
        internal static class RecoverWillAbility_GetStatusSource_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(RecoverWillAbility __instance)
            {
                TacticalActorBase Base_TacticalActorBase = (TacticalActor)AccessTools.Property(typeof(TacticalAbility), "TacticalActorBase").GetValue(__instance, null);
                DefRepository Repo = TFTVMain.Repo;
                Effect.Apply(Repo, ViralValueChange_EffectDef, TacUtil.GetActorEffectTarget(Base_TacticalActorBase, null));
            }
        }

        // Static variables to save icon and localization keys that otherwise would get lost in subsequent calls
        internal static Sprite InfiltratorStealthIcon = null;
        internal static string InfiltratorStealthDisplayName1LocKey = string.Empty;
        internal static string InfiltratorStealthDescriptionLocKey = string.Empty;
        private static void Apply_StealthIndicator_AllClasses()
        {
            DefRepository Repo = TFTVMain.Repo;
            // Get stealth indicator ability
            ApplyStatusAbilityDef baseForAll = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(asa => asa.name.Equals("Stealth_AbilityDef"));
            // Save icon and localization keys for subsequent calls, the base ones get overwritten (see below)
            if (InfiltratorStealthIcon == null)
            {
                InfiltratorStealthIcon = baseForAll.ViewElementDef.LargeIcon;
                InfiltratorStealthDisplayName1LocKey = baseForAll.ViewElementDef.DisplayName1.LocalizationKey;
                InfiltratorStealthDescriptionLocKey = baseForAll.ViewElementDef.Description.LocalizationKey;
            }

            // Clone base skill for special Infiltrator skill to keep the stealth bonus on Infiltrator
            string skillName = "StealthInfiltrator_";
            ApplyStatusAbilityDef stealthInfiltrator = Helper.CreateDefFromClone(
                baseForAll,
                "842d9c62-34f7-474c-942c-2676fef2f7e6",
                skillName + "AbilityDef");
            stealthInfiltrator.ViewElementDef = Helper.CreateDefFromClone(
                baseForAll.ViewElementDef,
                "6dfeca1f-1434-44d1-8f40-1843ce204044",
                skillName + "ViewElementDef");
            stealthInfiltrator.ViewElementDef.LargeIcon = InfiltratorStealthIcon;
            stealthInfiltrator.ViewElementDef.SmallIcon = InfiltratorStealthIcon;
            stealthInfiltrator.ViewElementDef.DisplayName1.LocalizationKey = InfiltratorStealthDisplayName1LocKey;
            stealthInfiltrator.ViewElementDef.Description.LocalizationKey = InfiltratorStealthDescriptionLocKey;

            FactionVisibilityConditionStatusDef visibilityConditionStatus = Helper.CreateDefFromClone(
                baseForAll.StatusDef as FactionVisibilityConditionStatusDef,
                "3bae4197-6f49-4ee2-984b-9779321aa9a5",
                skillName + "VisibiltyConditionStatusDef");
            stealthInfiltrator.StatusDef = visibilityConditionStatus;

            // Create new StatModifications array for stealth bonus
            ItemStatModification[] statModifications = new ItemStatModification[] {new ItemStatModification()
            {
                TargetStat = StatModificationTarget.Stealth,
                Modification = StatModificationType.Add,
                Value = 0.25f
            }};

            StanceStatusDef hiddenStatus = Helper.CreateDefFromClone(
                visibilityConditionStatus.HiddenStateStatusDef as StanceStatusDef,
                "a51977b0-be83-4249-8bef-30fe04fff4b3",
                skillName + "HiddenStatusDef");
            hiddenStatus.StatModifications = statModifications;
            hiddenStatus.Visuals = Helper.CreateDefFromClone(
                stealthInfiltrator.ViewElementDef,
                "31859328-2bf7-4ee5-b59a-2600da67a1e8",
                $"E_View [{hiddenStatus.name}]");
            hiddenStatus.Visuals.Color = Color.green;
            hiddenStatus.Visuals.Description.LocalizationKey = "PR_BC_INFILTRATOR_HIDDEN";
            visibilityConditionStatus.HiddenStateStatusDef = hiddenStatus;

            StanceStatusDef locatedStatus = Helper.CreateDefFromClone(
                visibilityConditionStatus.LocatedStateStatusDef as StanceStatusDef,
                "4d827076-1447-4d81-b398-9f42a29fb645",
                skillName + "LocatedStateStatusDef");
            locatedStatus.StatModifications = statModifications;
            locatedStatus.Visuals = Helper.CreateDefFromClone(
                stealthInfiltrator.ViewElementDef,
                "4dfe23e9-e1d8-4233-bf56-fdaae41b5c54",
                $"E_View [{locatedStatus.name}]");
            locatedStatus.Visuals.Color = Color.yellow;
            locatedStatus.Visuals.Description.LocalizationKey = "PR_BC_INFILTRATOR_LOCATED";
            visibilityConditionStatus.LocatedStateStatusDef = locatedStatus;

            StanceStatusDef revealedStatus = Helper.CreateDefFromClone(
                visibilityConditionStatus.RevealedStateStatusDef as StanceStatusDef,
                "ed5e20f8-5324-49fe-b202-aa927df3e3f4",
                skillName + "RevealedStateStatusDef");
            revealedStatus.Visuals = Helper.CreateDefFromClone(
                stealthInfiltrator.ViewElementDef,
                "42f16776-e30e-4435-90a6-3977df8ba154",
                $"E_View [{revealedStatus.name}]");
            revealedStatus.Visuals.Color = Color.red;
            revealedStatus.Visuals.Description.LocalizationKey = "PR_BC_INFILTRATOR_REVEALED";
            visibilityConditionStatus.RevealedStateStatusDef = revealedStatus;

            //Delete stealth bonus from base skill
            ((baseForAll.StatusDef as FactionVisibilityConditionStatusDef).HiddenStateStatusDef as StanceStatusDef).StatModifications = new ItemStatModification[0];
            // New Icon and texts for base skill
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_SneakerLegs_Stealth-2.png");
            baseForAll.ViewElementDef.LargeIcon = icon;
            baseForAll.ViewElementDef.SmallIcon = icon;
            baseForAll.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_HIDDEN";
            baseForAll.ViewElementDef.Description.LocalizationKey = "PR_BC_HIDDEN_DESC";

            // Add stealth indicator ability to Soldier_ActorDef.Abilities if it does not already contains it (set it for all characters)
            TacticalActorDef soldierActorDef = Repo.GetAllDefs<TacticalActorDef>().FirstOrDefault(ta => ta.name.Equals("Soldier_ActorDef"));
            if (!soldierActorDef.Abilities.Contains(baseForAll))
            {
                soldierActorDef.Abilities = soldierActorDef.Abilities.Append(baseForAll).ToArray();
            }

            // Replace base stealth inidcator ability on Infiltrator_ClassProficiency_AbilityDef with new ctreated stealth buff ability
            ClassProficiencyAbilityDef infiltratorCPAD = Repo.GetAllDefs<ClassProficiencyAbilityDef>().FirstOrDefault(cp => cp.name.Equals("Infiltrator_ClassProficiency_AbilityDef"));
            if (!infiltratorCPAD.AbilityDefs.Contains(stealthInfiltrator))
            {
                List<AbilityDef> abilityDefs = infiltratorCPAD.AbilityDefs.ToList();
                _ = abilityDefs.Remove(baseForAll);
                abilityDefs.Add(stealthInfiltrator);
                infiltratorCPAD.AbilityDefs = abilityDefs.ToArray();
            }
        }

        private static void Set_SPcost()
        {
            PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
            PRMLogger.Debug("Set SP cost for all abilities.");
            PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);

            BCSettings Config = TFTVMain.Main.Settings;
            DefRepository Repo = TFTVMain.Repo;
            string abilityName = "";
            // Main spec
            foreach (ClassSpecDef classSpec in Config.ClassSpecializations)
            {
                for (int i = 0; i < classSpec.MainSpec.Length; i++)
                {
                    if (i != 0 && i != 3 && Helper.AbilityNameToDefMap.ContainsKey(classSpec.MainSpec[i]))
                    {
                        abilityName = Helper.AbilityNameToDefMap[classSpec.MainSpec[i]];
                        TacticalAbilityDef tacticalAbility = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(ta => ta.name.Equals(abilityName));
                        if (tacticalAbility != null && tacticalAbility.CharacterProgressionData != null)
                        {
                            tacticalAbility.CharacterProgressionData.SkillPointCost = Helper.SPperLevel[i];
                            PRMLogger.Debug($"Set ability {tacticalAbility.name} to {Helper.SPperLevel[i]} SP cost.");
                        }
                    }
                }
            }
            foreach (PersonalPerksDef ppd in Config.PersonalPerks)
            {
                switch (ppd.PerkKey)
                {
                    case PerkType.Background:
                    case PerkType.Proficiency:
                        foreach (string skillName in ppd.UnrelatedRandomPerks)
                        {
                            abilityName = Helper.AbilityNameToDefMap[skillName];
                            TacticalAbilityDef tacticalAbility = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(ta => ta.name.Equals(abilityName));
                            if (tacticalAbility != null && tacticalAbility.CharacterProgressionData != null)
                            {
                                tacticalAbility.CharacterProgressionData.SkillPointCost = ppd.SPcost;
                                PRMLogger.Debug($"Set ability {tacticalAbility.name} to {ppd.SPcost} SP cost.");
                            }
                        }
                        break;
                    case PerkType.Class_1:
                    case PerkType.Class_2:
                    case PerkType.Faction_1:
                    case PerkType.Faction_2:
                        foreach (KeyValuePair<string, Dictionary<string, string>> outerRelation in ppd.RelatedFixedPerks)
                        {
                            foreach (KeyValuePair<string, string> innerRelation in outerRelation.Value)
                            {
                                abilityName = Helper.AbilityNameToDefMap[innerRelation.Value];
                                TacticalAbilityDef tacticalAbility = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(ta => ta.name.Equals(abilityName));
                                if (tacticalAbility != null && tacticalAbility.CharacterProgressionData != null)
                                {
                                    tacticalAbility.CharacterProgressionData.SkillPointCost = ppd.SPcost;
                                    PRMLogger.Debug($"Set ability {tacticalAbility.name} to {ppd.SPcost} SP cost.");
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            PRMLogger.Debug("----------------------------------------------------", false);
        }

        private static void Change_ProficiencyPerks()
        {
            BCSettings Config = TFTVMain.Main.Settings;
            DefRepository Repo = TFTVMain.Repo;
            foreach (PassiveModifierAbilityDef pmad in Repo.GetAllDefs<PassiveModifierAbilityDef>())
            {
                if (pmad.CharacterProgressionData != null && pmad.name.Contains("Talent"))
                {
                    // Assault rifle proficiency fix, was set to shotguns
                    if (pmad.name.Contains("Assault"))
                    {
                        GameTagDef ARtagDef = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(gtd => gtd.name.Equals("AssaultRifleItem_TagDef"));
                        pmad.ItemTagStatModifications[0].ItemTag = ARtagDef;
                    }

                    // Change description text, not localized (currently), old one mentions fixed buffs that are taken away or set differently by this mod
                    string newText = Helper.NotLocalizedTextMap[pmad.ViewElementDef.name][ViewElement.Description];
                    pmad.ViewElementDef.Description = new LocalizedTextBind(newText, Config.DoNotLocalizeChangedTexts);

                    PRMLogger.Debug("Proficiency def name: " + pmad.name);
                    PRMLogger.Debug("Viewelement name:     " + pmad.ViewElementDef.name);
                    PRMLogger.Debug("Display1 name:        " + pmad.ViewElementDef.DisplayName1.Localize());
                    PRMLogger.Debug("Description:          " + pmad.ViewElementDef.Description.Localize());

                    // Get modification from config, but first -0.1 to normalise to 0.0 (proficiency perks are all set to +0.1 buff)
                    float newStatModification = -0.1f + Config.BuffsForAdditionalProficiency[Proficiency.Buff];
                    // Loop through all subsequent item stat modifications
                    if (pmad.ItemTagStatModifications.Length > 0)
                    {
                        for (int i = 0; i < pmad.ItemTagStatModifications.Length; i++)
                        {
                            if (pmad.ItemTagStatModifications[i].EquipmentStatModification.Value != (0 + Config.BuffsForAdditionalProficiency[Proficiency.Buff])
                                && pmad.ItemTagStatModifications[i].EquipmentStatModification.Value != (1 + Config.BuffsForAdditionalProficiency[Proficiency.Buff]))
                            {
                                pmad.ItemTagStatModifications[i].EquipmentStatModification.Value += newStatModification;
                            }

                            PRMLogger.Debug("  Target item: " + pmad.ItemTagStatModifications[i].ItemTag.name);
                            PRMLogger.Debug("  Target stat: " + pmad.ItemTagStatModifications[i].EquipmentStatModification.TargetStat);
                            PRMLogger.Debug(" Modification: " + pmad.ItemTagStatModifications[i].EquipmentStatModification.Modification);
                            PRMLogger.Debug("        Value: " + pmad.ItemTagStatModifications[i].EquipmentStatModification.Value);
                        }
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }

        // New Battle Focus ability
        public static void Create_BattleFocus()
        {
            float damageMod = 1.2f;
            float range = 10.0f;
            string skillName = "BattleFocus_AbilityDef";
            DefRepository Repo = TFTVMain.Repo;

            // Source to clone from
            ApplyStatusAbilityDef masterMarksman = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("MasterMarksman_AbilityDef"));

            // Create Neccessary RuntimeDefs
            ApplyStatusAbilityDef battleFocusAbility = Helper.CreateDefFromClone(
                masterMarksman,
                "64fc75aa-93be-4d79-b5ac-191c5c7820da",
                skillName);
            AbilityCharacterProgressionDef progression = Helper.CreateDefFromClone(
                masterMarksman.CharacterProgressionData,
                "7ffae720-a656-454e-a95b-b861a673718a",
                skillName);
            TacticalTargetingDataDef targetingData = Helper.CreateDefFromClone(
                masterMarksman.TargetingDataDef,
                "fed0600a-14b3-4ef5-ac0c-31b3bf6f1e6c",
                skillName);
            TacticalAbilityViewElementDef viewElement = Helper.CreateDefFromClone(
                masterMarksman.ViewElementDef,
                "b498b9de-f10b-464c-a9f9-29a293568b04",
                skillName);
            StanceStatusDef stanceStatus = Helper.CreateDefFromClone( // Borrow status from Sneak Attack, Master Marksman status does not fit
                Repo.GetAllDefs<StanceStatusDef>().FirstOrDefault(p => p.name.Equals("E_SneakAttackStatus [SneakAttack_AbilityDef]")),
                "05929419-7d20-47aa-b700-fa6bc6602716",
                "E_Status [" + skillName + "]");
            VisibleActorsInRangeEffectConditionDef visibleActorsInRangeEffectCondition = Helper.CreateDefFromClone(
                (VisibleActorsInRangeEffectConditionDef)masterMarksman.TargetApplicationConditions[0],
                "63a34054-28de-488e-ae4a-af451434f0d4",
                skillName);

            // Set fields
            battleFocusAbility.CharacterProgressionData = progression;
            battleFocusAbility.TargetingDataDef = targetingData;
            battleFocusAbility.ViewElementDef = viewElement;
            battleFocusAbility.StatusDef = stanceStatus;
            battleFocusAbility.TargetApplicationConditions = new EffectConditionDef[] { visibleActorsInRangeEffectCondition };
            progression.RequiredStrength = 0;
            progression.RequiredWill = 0;
            progression.RequiredSpeed = 0;
            targetingData.Origin.Range = range;
            viewElement.DisplayName1.LocalizationKey = "PR_BC_BATTLE_FOCUS";
            viewElement.Description.LocalizationKey = "PR_BC_BATTLE_FOCUS_DESC";
            viewElement.ShowInInventoryItemTooltip = true;
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_TacticalAnalyst.png");
            viewElement.LargeIcon = icon;
            viewElement.SmallIcon = icon;
            stanceStatus.EffectName = skillName;
            stanceStatus.ShowNotification = true;
            stanceStatus.Visuals = battleFocusAbility.ViewElementDef;
            stanceStatus.StatModifications[0].Value = damageMod;
            visibleActorsInRangeEffectCondition.TargetingData = battleFocusAbility.TargetingDataDef;
            visibleActorsInRangeEffectCondition.ActorsInRange = true;
        }

    }
}
