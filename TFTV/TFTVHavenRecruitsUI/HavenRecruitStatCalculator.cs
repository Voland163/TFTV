using Base.Entities.Statuses;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitStatCalculator
    {
        public static class GeoUnitStatsHelper
        {
            public static GeoUnitStatsSummary Calculate(GeoUnitDescriptor descriptor)
            {
                if (descriptor == null)
                {
                    throw new ArgumentNullException("descriptor");
                }
                CharacterProgression characterProgression = GeoUnitStatsHelper.CreateProgression(descriptor);
                BaseCharacterStats baseCharacterStats = GeoUnitStatsHelper.GetBaseCharacterStats(descriptor, characterProgression);
                List<BodyPartAspectDef> nakedBodyParts = GeoUnitStatsHelper.GetBaseBodyParts(descriptor);
                CharacterStats baseStats = GeoUnitStatsHelper.BuildCharacterStats(descriptor, nakedBodyParts, Enumerable.Empty<PassiveModifierAbilityDef>(), baseCharacterStats);
                List<BodyPartAspectDef> armoredBodyParts = GeoUnitStatsHelper.GetArmoredBodyParts(descriptor, nakedBodyParts);
                CharacterStats armorOnlyStats = GeoUnitStatsHelper.BuildCharacterStats(descriptor, armoredBodyParts, Enumerable.Empty<PassiveModifierAbilityDef>(), baseCharacterStats);
                List<PassiveModifierAbilityDef> passiveAbilities = GeoUnitStatsHelper.GetPassiveAbilities(descriptor, characterProgression);
                CharacterStats finalStats = GeoUnitStatsHelper.BuildCharacterStats(descriptor, armoredBodyParts, passiveAbilities, baseCharacterStats);
                BaseCharacterStats baseValues = GeoUnitStatsHelper.ExtractStats(baseStats);
                BaseCharacterStats armorValues = GeoUnitStatsHelper.ExtractStats(armorOnlyStats);
                BaseCharacterStats finalValues = GeoUnitStatsHelper.ExtractStats(finalStats);
                List<GeoUnitStatsHelper.ModifierContribution> contributions = new List<GeoUnitStatsHelper.ModifierContribution>();
                contributions.AddRange(GeoUnitStatsHelper.GetArmorContributions(descriptor, nakedBodyParts, baseValues, baseCharacterStats));
                contributions.AddRange(GeoUnitStatsHelper.GetAbilityContributions(descriptor, passiveAbilities, armoredBodyParts, armorValues, baseCharacterStats));
                string modifiers = GeoUnitStatsHelper.BuildModifierSummary(contributions);
                return new GeoUnitStatsSummary(baseValues, finalValues, modifiers);
            }

            private static CharacterProgression CreateProgression(GeoUnitDescriptor descriptor)
            {
                if (descriptor.Progression == null)
                {
                    return null;
                }
                GeoLevelController levelController = descriptor.LevelController;
                FactionCharacterGenerator characterGenerator = levelController.CharacterGenerator;
                LevelProgression levelProgression = descriptor.UnitType.TemplateDef.Data != null ? descriptor.UnitType.TemplateDef.Data.LevelProgression : null;
                LevelProgressionDef levelProgressionDef = (levelProgression != null && levelProgression.Def != null) ? levelProgression.Def : characterGenerator.LevelProgression;
                CharacterProgression characterProgression = new CharacterProgression(new CharacterProgressionDescription
                {
                    BaseStatSheetDef = characterGenerator.BaseStatsSheet,
                    LevelProgressionDef = levelProgressionDef,
                    PersonalAbilityTrack = descriptor.GetPersonalAbilityTrack(),
                    SpecializationDef = descriptor.Progression.MainSpecDef
                });
                if (descriptor.Progression.Level > 1)
                {
                    characterProgression.LevelProgression.SetLevel(descriptor.Progression.Level);
                }
                if (descriptor.Progression.SecondarySpecDef != null)
                {
                    characterProgression.AddSecondaryClass(descriptor.Progression.SecondarySpecDef);
                }
                if (descriptor.Progression.LearnPrimaryAbilities && descriptor.Progression.Level > 1)
                {
                    characterProgression.SkillPoints = 0;
                }
                foreach (TacticalAbilityDef tacticalAbilityDef in descriptor.GetTacticalAbilities())
                {
                    if (!characterProgression.Abilities.Contains(tacticalAbilityDef))
                    {
                        characterProgression.AddAbility(tacticalAbilityDef);
                    }
                }
                return characterProgression;
            }

            private static BaseCharacterStats GetBaseCharacterStats(GeoUnitDescriptor descriptor, CharacterProgression progression)
            {
                BaseCharacterStats bonusStats = descriptor.BonusStats;
                if (progression != null)
                {
                    bonusStats.Endurance += (float)progression.Strength;
                    bonusStats.Willpower += (float)progression.Will;
                    bonusStats.Speed += (float)progression.Speed;
                }
                return bonusStats;
            }

            private static List<BodyPartAspectDef> GetBaseBodyParts(GeoUnitDescriptor descriptor)
            {
                List<BodyPartAspectDef> list = new List<BodyPartAspectDef>();
                if (descriptor.UnitType.AddonDef != null)
                {
                    GeoUnitDescriptor.GetAddonDefBodypartStats(descriptor.UnitType.AddonDef, list);
                }
                return list;
            }

            private static List<BodyPartAspectDef> GetArmoredBodyParts(GeoUnitDescriptor descriptor, List<BodyPartAspectDef> baseBodyParts)
            {
                List<BodyPartAspectDef> list = new List<BodyPartAspectDef>(baseBodyParts);
                if (descriptor.ArmorItems != null && descriptor.ArmorItems.Count > 0)
                {
                    GeoUnitDescriptor.GetBodypartStats(descriptor.ArmorItems, list);
                }
                return list;
            }

            private static CharacterStats BuildCharacterStats(GeoUnitDescriptor descriptor, IEnumerable<BodyPartAspectDef> bodyParts, IEnumerable<PassiveModifierAbilityDef> abilities, BaseCharacterStats baseCharacterStats)
            {
                StatsRepo statsRepo = new StatsRepo();
                statsRepo.AddStat(new StatusStat
                {
                    Name = StatModificationTarget.Health.ToString(),
                    Owner = descriptor.UnitType
                });
                CharacterStats characterStats = new CharacterStats(descriptor.UnitType, statsRepo);
                characterStats.InitStats(descriptor.UnitType.TemplateDef.ComponentSetDef, bodyParts, abilities, new BaseCharacterStats?(baseCharacterStats));
                characterStats.SetCompositePointStatsBounds(descriptor.UnitType.TemplateDef.TacticalActorBaseDef);
                characterStats.CarryWeight.ReapplyModifications();
                foreach (StatusStat statusStat in statsRepo.Stats.OfType<StatusStat>())
                {
                    statusStat.SetToMax();
                }
                return characterStats;
            }

            private static BaseCharacterStats ExtractStats(CharacterStats stats)
            {
                return new BaseCharacterStats(stats.Endurance, stats.Willpower, stats.Speed);
            }

            private static List<PassiveModifierAbilityDef> GetPassiveAbilities(GeoUnitDescriptor descriptor, CharacterProgression progression)
            {
                HashSet<PassiveModifierAbilityDef> hashSet = new HashSet<PassiveModifierAbilityDef>();
                if (descriptor.Progression != null)
                {
                    foreach (PassiveModifierAbilityDef passiveModifierAbilityDef in descriptor.GetTacticalAbilities().OfType<PassiveModifierAbilityDef>())
                    {
                        hashSet.Add(passiveModifierAbilityDef);
                    }
                }
                if (progression != null)
                {
                    foreach (PassiveModifierAbilityDef passiveModifierAbilityDef2 in progression.Abilities.OfType<PassiveModifierAbilityDef>())
                    {
                        hashSet.Add(passiveModifierAbilityDef2);
                    }
                }
                return hashSet.ToList();
            }

            private static IEnumerable<GeoUnitStatsHelper.ModifierContribution> GetArmorContributions(GeoUnitDescriptor descriptor, List<BodyPartAspectDef> baseBodyParts, BaseCharacterStats baseStats, BaseCharacterStats baseCharacterStats)
            {
                List<GeoUnitStatsHelper.ModifierContribution> list = new List<GeoUnitStatsHelper.ModifierContribution>();
                if (descriptor.ArmorItems == null)
                {
                    return list;
                }
                foreach (TacticalItemDef tacticalItemDef in descriptor.ArmorItems)
                {
                    List<BodyPartAspectDef> list2 = new List<BodyPartAspectDef>(baseBodyParts);
                    ItemDef[] itemDefs = new ItemDef[]
                    {
                    tacticalItemDef
                    };
                    GeoUnitDescriptor.GetBodypartStats(itemDefs, list2);
                    CharacterStats characterStats = GeoUnitStatsHelper.BuildCharacterStats(descriptor, list2, Enumerable.Empty<PassiveModifierAbilityDef>(), baseCharacterStats);
                    BaseCharacterStats stats = GeoUnitStatsHelper.ExtractStats(characterStats);
                    GeoUnitStatsHelper.ModifierContribution item = new GeoUnitStatsHelper.ModifierContribution(GeoUnitStatsHelper.GetViewName(tacticalItemDef.ViewElementDef) ?? tacticalItemDef.name, stats.Endurance - baseStats.Endurance, stats.Willpower - baseStats.Willpower, stats.Speed - baseStats.Speed);
                    list.Add(item);
                }
                return list;
            }

            private static IEnumerable<GeoUnitStatsHelper.ModifierContribution> GetAbilityContributions(GeoUnitDescriptor descriptor, IEnumerable<PassiveModifierAbilityDef> abilities, IEnumerable<BodyPartAspectDef> armoredBodyParts, BaseCharacterStats armorStats, BaseCharacterStats baseCharacterStats)
            {
                List<GeoUnitStatsHelper.ModifierContribution> list = new List<GeoUnitStatsHelper.ModifierContribution>();
                foreach (PassiveModifierAbilityDef passiveModifierAbilityDef in abilities)
                {
                    CharacterStats characterStats = GeoUnitStatsHelper.BuildCharacterStats(descriptor, armoredBodyParts, new PassiveModifierAbilityDef[]
                    {
                    passiveModifierAbilityDef
                    }, baseCharacterStats);
                    BaseCharacterStats stats = GeoUnitStatsHelper.ExtractStats(characterStats);
                    GeoUnitStatsHelper.ModifierContribution item = new GeoUnitStatsHelper.ModifierContribution(GeoUnitStatsHelper.GetViewName(passiveModifierAbilityDef.ViewElementDef) ?? passiveModifierAbilityDef.name, stats.Endurance - armorStats.Endurance, stats.Willpower - armorStats.Willpower, stats.Speed - armorStats.Speed);
                    list.Add(item);
                }
                return list;
            }

            private static string GetViewName(ViewElementDef view)
            {
                if (view == null)
                {
                    return null;
                }
                return view.DisplayName1.Localize(null);
            }

            private static string BuildModifierSummary(IEnumerable<GeoUnitStatsHelper.ModifierContribution> contributions)
            {
                List<string> list = new List<string>();
                foreach (GeoUnitStatsHelper.ModifierContribution modifierContribution in contributions)
                {
                    List<string> list2 = new List<string>();
                    GeoUnitStatsHelper.TryAddStatContribution(list2, modifierContribution.Endurance, "STR");
                    GeoUnitStatsHelper.TryAddStatContribution(list2, modifierContribution.Willpower, "WILL");
                    GeoUnitStatsHelper.TryAddStatContribution(list2, modifierContribution.Speed, "SPD");
                    if (list2.Count > 0)
                    {
                        list.Add(string.Format("{0}: {1}", modifierContribution.Source, string.Join(" ", list2)));
                    }
                }
                return string.Join(Environment.NewLine, list);
            }

            private static void TryAddStatContribution(List<string> entries, float value, string stat)
            {
                if (Mathf.Approximately(value, 0f))
                {
                    return;
                }
                string format = (value > 0f) ? "<color=#00FF00>+{0:0.#}</color> {1}" : "<color=#FF0000>-{0:0.#}</color> {1}";
                entries.Add(string.Format(format, Mathf.Abs(value), stat));
            }

            private struct ModifierContribution
            {
                public ModifierContribution(string source, float endurance, float willpower, float speed)
                {
                    this.Source = source;
                    this.Endurance = endurance;
                    this.Willpower = willpower;
                    this.Speed = speed;
                }

                public readonly string Source;

                public readonly float Endurance;

                public readonly float Willpower;

                public readonly float Speed;
            }
        }

        public struct GeoUnitStatsSummary
        {
            public GeoUnitStatsSummary(BaseCharacterStats baseStats, BaseCharacterStats finalStats, string modifiers)
            {
                this.BaseStats = baseStats;
                this.FinalStats = finalStats;
                this.ModifiersDescription = modifiers;
            }

            public BaseCharacterStats BaseStats { get; }

            public BaseCharacterStats FinalStats { get; }

            public string ModifiersDescription { get; }
        }
    }
}

