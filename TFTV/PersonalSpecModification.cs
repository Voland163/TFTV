using System;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Geoscape.Entities;
using Base.Defs;
using System.Collections.Generic;
using System.Linq;
using Base;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.View.DataObjects;
using UnityEngine;
using PhoenixPoint.Common.Core;
using Base.UI;
using HarmonyLib;
using TFTV;

namespace PRMBetterClasses
{
    internal class PersonalSpecModification
    {
        // Change personal abilities (3rd row skills) by configured settings
        [HarmonyPatch(typeof(FactionCharacterGenerator), "GenerateUnit", new Type[] { typeof(GeoFaction), typeof(TacCharacterDef) })]
        internal static class GenerateUnit_Patches
        {
            // Set by PREFIX call
            private static bool ShouldGeneratePersonalAbilities;

            // Called before 'GenerateUnit' -> PREFIX.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(TacCharacterDef template)
            {
                try
                {
                    PRMLogger.Debug($"FactionCharacterGenerator.Generate Unit, template: {template.name}");
                    
                    // Save ShouldGeneratePersonalAbilities for postfix call, necessary? 
                    ShouldGeneratePersonalAbilities = template.Data.LevelProgression.ShouldGeneratePersonalAbilities;
                    PRMLogger.Debug("----------------------------------------------------", false);
                    PRMLogger.Debug("PREFIX GenerateUnit called:");
                    PRMLogger.Debug("ShouldGeneratePersonalAbilities: " + ShouldGeneratePersonalAbilities);
                    PRMLogger.Debug("----------------------------------------------------", false);
                    return true;
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                    return true;
                }
            }

            // Called after 'GenerateUnit' -> POSTFIX.
            // Set the personal skill tree to configured setup
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref GeoUnitDescriptor __result)
            {
                try
                {
                    //DefRepository Repo = TFTVMain.Repo;
                    DefCache DefCache = TFTVMain.Main.DefCache;
                    BCSettings Config = TFTVMain.Main.Settings;
                    PRMLogger.Debug("POSTFIX GenerateUnit called:");
                    // Probably not necessary, just to be safe ;-)
                    if (__result != null && ShouldGeneratePersonalAbilities && __result.UnitType.IsHuman && !__result.UnitType.IsMutoid && !__result.UnitType.TemplateDef.IsAlien)
                    {
                        string faction = __result.Faction.GetPPName();
                        string className = __result.ClassTag.className;
                        BaseCharacterStats stats = __result.BonusStats;
                        PRMLogger.Debug("      Faction: " + faction);
                        PRMLogger.Debug("        Class: " + className);
                        PRMLogger.Debug("    Endurance: " + stats.Endurance);
                        PRMLogger.Debug("     Strength: " + stats.Strength);
                        PRMLogger.Debug("    Willpower: " + stats.Willpower);
                        PRMLogger.Debug("        Speed: " + stats.Speed);
                        PRMLogger.Debug("----------------------------------------------------", false);
                        string ability;
                        int spCost = 0;
                        TacticalAbilityDef tacticalAbilityDef;
                        string[] ppOrder = Config.OrderOfPersonalPerks;
                        // Temporary dictionary to collect the configured perks
                        Dictionary<string, string> tempDict = new Dictionary<string, string>();
                        // Exclusion list for random selection, initialized with main spec skills
                        List<string> exclusionList = new List<string>(Config.ClassSpecializations.FirstOrDefault(cs => cs.ClassName.Equals(className)).MainSpec);

                        for (int i = 0; i < ppOrder.Length; i++)
                        {
                            PRMLogger.Debug("Set personal perk index: " + i);
                            PersonalPerksDef personalPerksDef = Config.PersonalPerks.FirstOrDefault(pp => pp.PerkKey.Equals(ppOrder[i]));
                            if (!personalPerksDef.IsDefaultValue())
                            {
                                PRMLogger.Debug("           Key: " + personalPerksDef.PerkKey);

                                if (Config.SpecialCharacterPersonalSkills.ContainsKey(__result.UnitType.TemplateDef.Data.Name) && Config.SpecialCharacterPersonalSkills[__result.UnitType.TemplateDef.Data.Name].ContainsKey(i))
                                {
                                    ability = Config.SpecialCharacterPersonalSkills[__result.UnitType.TemplateDef.Data.Name][i];
                                }

                                /*  if (Config.SpecialCharacterPersonalSkills.ContainsKey(__result.UnitType.TemplateDef.Data.Name) && Config.SpecialCharacterPersonalSkills[__result.GetName()].ContainsKey(i))
                                {
                                    ability = Config.SpecialCharacterPersonalSkills[__result.UnitType.TemplateDef.Data.Name][i];
                                }*/

                                else
                                {
                                    (ability, spCost) = personalPerksDef.GetPerk(Config, className, faction, exclusionList);
                                }
                                if (ability != null)
                                {
                                    PRMLogger.Debug("       Ability: " + ability);
                                    tacticalAbilityDef = DefCache.GetDef<TacticalAbilityDef>(ability);
                                    if (spCost == 0)
                                    {
                                        spCost = tacticalAbilityDef.CharacterProgressionData.SkillPointCost;
                                    }
                                    if (i >= 0 && i < 7 && tacticalAbilityDef != null)
                                    {
                                        PRMLogger.Debug("  Ability name: " + tacticalAbilityDef.name);
                                        // Set SP cost to personal ability. Be careful, SP cost are global per ability, regardless where this ability is set!
                                        tacticalAbilityDef.CharacterProgressionData.SkillPointCost = spCost;
                                        __result.Progression.PersonalAbilities[i] = tacticalAbilityDef;
                                        //exclusionList.Add(ability);
                                    }
                                    else
                                    {
                                        PRMLogger.Always(" Faction: " + faction);
                                        PRMLogger.Always("   Class: " + className);
                                        PRMLogger.Always("Ability '" + ability + "' was not configured for level " + i + " or was not received from Repo and so not applied!");
                                        PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                                    }
                                }
                                else
                                {
                                    PRMLogger.Always(" Faction: " + faction);
                                    PRMLogger.Always("   Class: " + className);
                                    PRMLogger.Always(" The method personalPerksDef.GetPerk() does not return an ability for level " + i + " and so none got applied!");
                                    PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                                }
                            }
                            PRMLogger.Debug("----------------------------------------------------", false);
                        }

                        // Some debug outputs in logging file
                        if (Config.Debug >= 2)
                        {
                            AbilityTrackSlot[] ats = __result.Progression.MainSpecDef.AbilityTrack.AbilitiesByLevel;
                            for (int i = 0; i < ats.Length; i++)
                            {
                                PRMLogger.Debug($"    MainSpec {i}: " + ats[i]?.Ability?.ViewElementDef.DisplayName1.LocalizeEnglish());
                            }
                            Dictionary<int, TacticalAbilityDef> tad = __result.Progression.PersonalAbilities;
                            for (int i = 0; i < ats.Length; i++)
                            {
                                PRMLogger.Debug($"PersonalSpec {i}: " + (tad.ContainsKey(i) ? tad[i].ViewElementDef.DisplayName1.LocalizeEnglish() : "none"));
                            }
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

        // Learn personal ability level 1 right by creation of the unit
        [HarmonyPatch(typeof(GeoUnitDescriptor), "GenerateProgression")]
        internal static class GenerateProgression_Patches
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref CharacterProgression __result, GeoUnitDescriptor __instance)
            {
                try
                {
                    TFTVConfig Config = TFTVMain.Main.Config;
                    if (Config.LearnFirstPersonalSkill && __instance.UnitType.IsHuman && !__instance.UnitType.IsMutoid && !__instance.UnitType.TemplateDef.IsAlien)
                    {
                        // Personal ability 0 = first skill in the row
                        TacticalAbilityDef persAbility0 = __result.PersonalAbilityTrack.AbilitiesByLevel[0].Ability;
                        if (persAbility0 != null)
                        {
                            if (!__result.Abilities.Contains(persAbility0))
                            {
                                __result.AddAbility(persAbility0);
                                PRMLogger.Debug("Ability added (learned, set): " + persAbility0);
                            }
                            else
                            {
                                PRMLogger.Always("Character '" + __instance.GetName() + "' of class '" + __instance.ClassTag.className + "' has personal ability on level 0 '" + persAbility0 + "' already learned!");
                                PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                            }
                        }
                        else
                        {
                            PRMLogger.Always("Character '" + __instance.GetName() + "' of class '" + __instance.ClassTag.className + "' has no personal ability on level 0!");
                            PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                        }
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        // Expand ability icon list in PX base recruit screen (vanilla fixed 3, we need 7)
        [HarmonyPatch(typeof(RecruitsListElementController), "SetRecruitElement")]
        public static class RecruitsListElementController_SetRecruitElement_Patch
        {
            public static bool Prefix(RecruitsListElementController __instance, RecruitsListEntryData entryData, List<RowIconTextController> ____abilityIcons)
            {
                try
                {
                    if (____abilityIcons == null)
                    {
                        ____abilityIcons = new List<RowIconTextController>();
                        if (__instance.PersonalTrackRoot.transform.childCount < entryData.PersonalTrackAbilities.Count())
                        {
                            RectTransform parent = __instance.PersonalTrackRoot.GetComponent<RectTransform>();
                            RowIconTextController source = parent.GetComponentInChildren<RowIconTextController>();
                            parent.DetachChildren();
                            source.Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);
                            for (int i = 0; i < entryData.PersonalTrackAbilities.Count(); i++)
                            {
                                RowIconTextController entry = UnityEngine.Object.Instantiate(source, parent, true);
                            }
                        }
                        UIUtil.GetComponentsFromContainer(__instance.PersonalTrackRoot.transform, ____abilityIcons);
                    }
                    __instance.RecruitData = entryData;
                    __instance.RecruitName.SetSoldierData(entryData.Recruit);
                    BC_SetAbilityIcons(entryData.PersonalTrackAbilities.ToList(), ____abilityIcons);
                    if (entryData.SuppliesCost != null && __instance.CostText != null && __instance.CostColorController != null)
                    {
                        __instance.CostText.text = entryData.SuppliesCost.ByResourceType(ResourceType.Supplies).RoundedValue.ToString();
                        __instance.CostColorController.SetWarningActive(!entryData.IsAffordable, true);
                    }
                    __instance.NavHolder.RefreshNavigation();
                    return false;
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                    return true;
                }
            }

            private static void BC_SetAbilityIcons(List<TacticalAbilityViewElementDef> abilities, List<RowIconTextController> abilityIcons)
            {
                foreach (RowIconTextController rowIconTextController in abilityIcons)
                {
                    rowIconTextController.gameObject.SetActive(false);
                }
                for (int i = 0; i < abilities.Count; i++)
                {
                    abilityIcons[i].gameObject.SetActive(true);
                    abilityIcons[i].SetController(abilities[i].LargeIcon, abilities[i].DisplayName1, abilities[i].Description);
                }
            }
        }
    }
}
