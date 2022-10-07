using System;
using System.Linq;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Tactical.Entities.Abilities;
using TFTV;

namespace PRMBetterClasses
{
    internal class MainSpecModification
    {
        public static void GenerateMainSpec()
        {
            try
            {
                DefRepository Repo = TFTVMain.Repo;
                DefCache DefCache = TFTVMain.Main.DefCache;
                BCSettings Config = TFTVMain.Main.Settings;

                LevelProgressionDef levelProgressionDef = DefCache.GetDef<LevelProgressionDef>(("LevelProgressionDef"));
                int secondaryClassLevel = levelProgressionDef.SecondSpecializationLevel;
                int secondaryClassCost = levelProgressionDef.SecondSpecializationSpCost;
                string ability;
                foreach (AbilityTrackDef abilityTrackDef in Repo.GetAllDefs<AbilityTrackDef>())
                {
                    if (Config.ClassSpecializations.Any(c => abilityTrackDef.name.Contains(c.ClassName)))
                    {
                        ClassSpecDef classSpec = Config.ClassSpecializations.Find(c => abilityTrackDef.name.Contains(c.ClassName));
                        string[] configMainSpec = classSpec.MainSpec;
                        if (abilityTrackDef.AbilitiesByLevel.Length != configMainSpec.Length)
                        {
                            PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                            PRMLogger.Debug("Not enough or too much level skills for 1st row are configured, this one will NOT be set!");
                            PRMLogger.Debug("AbilityTrackDef name: " + abilityTrackDef.name);
                            PRMLogger.Debug("AbilityTrackDef number of abilities: " + abilityTrackDef.AbilitiesByLevel.Length);
                            PRMLogger.Debug("AbilityTrackDef number of abilities: " + abilityTrackDef.Abilities.Select(a => a.name).Join());
                            PRMLogger.Debug("Class preset: " + classSpec.ClassName);
                            PRMLogger.Debug("Number of skills configured (should be 7): " + configMainSpec.Length);
                            PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        }
                        else
                        {
                            for (int i = 0; i < abilityTrackDef.AbilitiesByLevel.Length && i < configMainSpec.Length; i++)
                            {
                                // 0 = main class proficiency and 3 = secondary class selector skipped, main class is in the config but also skipped here to prevent bugs by misconfiguration
                                if (i != 0 && i != 3)
                                {
                                    if (Helper.AbilityNameToDefMap.ContainsKey(configMainSpec[i]))
                                    {
                                        ability = Helper.AbilityNameToDefMap[configMainSpec[i]];
                                        abilityTrackDef.AbilitiesByLevel[i].Ability = DefCache.GetDef<TacticalAbilityDef>(ability);
                                        abilityTrackDef.AbilitiesByLevel[i].Ability.CharacterProgressionData.SkillPointCost = Helper.SPperLevel[i];
                                        abilityTrackDef.AbilitiesByLevel[i].Ability.CharacterProgressionData.MutagenCost = Helper.SPperLevel[i];
                                        PRMLogger.Debug($"Class '{classSpec.ClassName}' level {i + 1} skill set to: {abilityTrackDef.AbilitiesByLevel[i].Ability.ViewElementDef.DisplayName1.LocalizeEnglish()} ({abilityTrackDef.AbilitiesByLevel[i].Ability.name})");
                                    }
                                }
                            }
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
}
