using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVIncidents
{
    internal class Affinities
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static PassiveModifierAbilityDef[] PsychoSociology = new PassiveModifierAbilityDef[] { };
        public static PassiveModifierAbilityDef[] Exploration = new PassiveModifierAbilityDef[] { };
        public static PassiveModifierAbilityDef[] Occult = new PassiveModifierAbilityDef[] { };
        public static PassiveModifierAbilityDef[] Biotech = new PassiveModifierAbilityDef[] { };
        public static PassiveModifierAbilityDef[] Machinery = new PassiveModifierAbilityDef[] { };
        public static PassiveModifierAbilityDef[] Compute = new PassiveModifierAbilityDef[] { };

        public static SkillTagDef AffinityTag;

        internal class Defs
        {

            public static void CreateDefs()
            {
                try
                {
                    CreateAffinityTag();

                    CreateAffinityAbilities(
                        "PSYCHO_SOCIOLOGY",
                        "KEY_AFFINITY_TITLE_PSYCHO_SOCIOLOGY_",
                        "KEY_AFFINITY_DESC_PSYCHO_SOCIOLOGY_",
                        PsychoSociologyGuids,
                        ref PsychoSociology);

                    CreateAffinityAbilities(
                        "EXPLORATION",
                        "KEY_AFFINITY_TITLE_EXPLORATION_",
                        "KEY_AFFINITY_DESC_EXPLORATION_",
                        ExplorationGuids,
                        ref Exploration);

                    CreateAffinityAbilities(
                        "OCCULT",
                        "KEY_AFFINITY_TITLE_OCCULT_",
                        "KEY_AFFINITY_DESC_OCCULT_",
                        OccultGuids,
                        ref Occult);

                    CreateAffinityAbilities(
                        "BIOTECH",
                        "KEY_AFFINITY_TITLE_BIOTECH_",
                        "KEY_AFFINITY_DESC_BIOTECH_",
                        BiotechGuids,
                        ref Biotech);

                    CreateAffinityAbilities(
                        "MACHINERY",
                        "KEY_AFFINITY_TITLE_MACHINERY_",
                        "KEY_AFFINITY_DESC_MACHINERY_",
                        MachineryGuids,
                        ref Machinery);

                    CreateAffinityAbilities(
                        "COMPUTE",
                        "KEY_AFFINITY_TITLE_COMPUTE_",
                        "KEY_AFFINITY_DESC_COMPUTE_",
                        ComputeGuids,
                        ref Compute);

                    AffinityBenefitsChoices.InitializeDynamicDescriptionResolver();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateAffinityTag()
            {
                try
                {
                    SkillTagDef source = DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef");
                    AffinityTag = Helper.CreateDefFromClone(
                        source,
                        "0f2c8b6e-4b1a-4d39-9a73-4b3f2b0d1c9e",
                        "Affinity_SkillTagDef");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateAffinityAbilities(
                string nameToken,
                string titleKeyBase,
                string descKeyBase,
                List<string> guids,
                ref PassiveModifierAbilityDef[] target)
            {
                try
                {
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            nameToken + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            nameToken + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = titleKeyBase + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = descKeyBase + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_AffinityIcon_{nameToken}_0.png");//{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            nameToken + x.ToString());

                        newAbility.StatModifications = new ItemStatModification[0];
                        newAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];

                        if (AffinityTag != null)
                        {
                            newAbility.SkillTags = (newAbility.SkillTags ?? Array.Empty<SkillTagDef>()).AddItem(AffinityTag).ToArray();
                        }

                        target = target.AddToArray(newAbility);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static readonly List<string> PsychoSociologyGuids = new List<string>()
        {
            "b8b48f18-2c0b-4d3c-a1c3-3f8225d68221",
            "6a0b2a9a-88a2-4c76-8ac4-4f0f9e2a8c5f",
            "c1f7d0d2-6a45-4be4-9f02-4f2c72a1e1c7",
            "5debdc8c-82bb-4f40-8795-7b9bf1b0f2d9",
            "4d95f13d-357d-48ed-9ef4-2d4b4dff2aa2",
            "0c86a0e1-3f02-4c9d-8b4f-8d0cbe3d1d21",
            "d3a236e6-1a9c-4c2d-9fb5-9aa0d7e1f873",
            "e7b26f7c-4df6-4d5c-8e64-67b7d79f7a7b",
            "b2a49c0f-955d-4fd3-8c8a-4b3512a9f2df"
        };

            private static readonly List<string> ExplorationGuids = new List<string>()
        {
            "2e74bda8-3e61-4d63-927f-38be6d2f1a6c",
            "7d9d9846-7a64-4e3a-b4f6-53f45f5c0b4f",
            "1f205173-3a5a-4f78-9c6c-34b8d2c3d6e6",
            "5d68b3f3-9c8d-4c2c-8e1c-8d6e9a7c2a77",
            "bc5bca2e-7d8d-45c0-92cb-4cdd02b4a9a3",
            "aa3e9c9c-b4b5-4c88-8d90-25f4b92c54d2",
            "6c0d6db8-7c61-4c4c-8f9f-9f3a2c6b4f41",
            "b3a2f9f1-4a2d-4f3b-a0cc-3f1b46ab6c87",
            "3f90760f-5f75-4c3b-97af-0a6e4f2c2b9d"
        };

            private static readonly List<string> OccultGuids = new List<string>()
        {
            "d0f7c89d-3f05-4a42-9cb5-88b0c7b3baba",
            "4bf40a64-0c52-4e1a-9a95-6b9e0b21d2ff",
            "9c9c907e-0fd9-4f7b-bbb0-3b5b8d401327",
            "f6cfda1b-4e0b-4b6c-92b2-7a4f39b6d0b2",
            "7d7e1b38-8f4a-4f5b-85a6-2caa2a925e4d",
            "54e67dd8-7e5d-4b3f-9f73-5f3f3ce0d1a0",
            "8b6a1c4f-f4d8-4af6-9f30-9a3b59c90a4b",
            "19b6841f-76c5-4e2b-8d62-5d9b8f1e0d8c",
            "f1c78175-3ab3-4c9c-9b76-4a9dc8d7b0d6"
        };

            private static readonly List<string> BiotechGuids = new List<string>()
        {
            "3a7b5e2b-17b6-4a6c-8b2f-1b29f2b7f6d1",
            "81b9f6d2-1b9a-4d97-9f3e-5f5f6f0a7a49",
            "c7b8c1a5-36f6-4e4a-9df4-1d8c0c53f2a0",
            "9b2db1da-8d83-41ff-8c8e-0e723e3baf53",
            "0bd2c8dd-7f8c-44a9-9c6d-1e7c3c5f2a6d",
            "f2a2d5b3-1441-4b76-9c63-8e2a65e0f32b",
            "2d8f7a1e-2b1d-48d0-98cb-7c2f5b5b9a8a",
            "c51a8b4e-3b2d-4cf0-8b0a-9e6b4c4a6c8f",
            "a4d7a1be-8b89-4d66-9bd3-6c8b5f5a7f9e"
        };

            private static readonly List<string> MachineryGuids = new List<string>()
        {
            "5b2d7f4a-7d6a-4f6c-8a7a-4f2c6d7e8a9b",
            "d4a1b7c9-1f3c-4a5b-9c7d-8f2a3b4c5d6e",
            "6e7f8a9b-0c1d-4e2f-9a3b-4c5d6e7f8a9b",
            "3c4d5e6f-7a8b-4c9d-8e1f-2a3b4c5d6e7f",
            "9a8b7c6d-5e4f-4d3c-8b2a-1c2d3e4f5a6b",
            "1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d",
            "7f6e5d4c-3b2a-4c1d-8e9f-0a1b2c3d4e5f",
            "2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e",
            "8c9d0e1f-2a3b-4c5d-8e9f-0a1b2c3d4e5f"
        };

            private static readonly List<string> ComputeGuids = new List<string>()
        {
            "1f2e3d4c-5b6a-4f7e-8d9c-0b1a2c3d4e5f",
            "9e8d7c6b-5a4f-4e3d-8c2b-1a0f9e8d7c6b",
            "4c3d2e1f-0a9b-4c8d-7e6f-5d4c3b2a1f0e",
            "6a7b8c9d-0e1f-4a2b-8c3d-4e5f6a7b8c9d",
            "0f1e2d3c-4b5a-4c6d-8e7f-9a0b1c2d3e4f",
            "7e6f5d4c-3b2a-4c1d-8e9f-0a1b2c3d4e5f",
            "3d2c1b0a-9f8e-4d7c-6b5a-4c3d2e1f0a9b",
            "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
            "c2d3e4f5-a6b7-4c8d-9e0f-1a2b3c4d5e6f"
        };
        }

        internal class AffinityBenefitsChoices 
        {
            private const string GeoBenefitVariablePrefix = "TFTV_AFFINITY_GEO_OPTION_";
            private const string TacticalBenefitVariablePrefix = "TFTV_AFFINITY_TAC_OPTION_";

            private const string KeyPsychoGeoOpt1 = "KEY_AFFINITY_GEO_DESC_PSYCHO_SOCIOLOGY_OPTION_1";
            private const string KeyPsychoGeoOpt2 = "KEY_AFFINITY_GEO_DESC_PSYCHO_SOCIOLOGY_OPTION_2";
            private const string KeyExplorationGeoOpt1 = "KEY_AFFINITY_GEO_DESC_EXPLORATION_OPTION_1";
            private const string KeyExplorationGeoOpt2 = "KEY_AFFINITY_GEO_DESC_EXPLORATION_OPTION_2";
            private const string KeyOccultGeoOpt1 = "KEY_AFFINITY_GEO_DESC_OCCULT_OPTION_1";
            private const string KeyOccultGeoOpt2 = "KEY_AFFINITY_GEO_DESC_OCCULT_OPTION_2";
            private const string KeyBiotechGeoOpt1 = "KEY_AFFINITY_GEO_DESC_BIOTECH_OPTION_1";
            private const string KeyBiotechGeoOpt2 = "KEY_AFFINITY_GEO_DESC_BIOTECH_OPTION_2";
            private const string KeyMachineryGeoOpt1 = "KEY_AFFINITY_GEO_DESC_MACHINERY_OPTION_1";
            private const string KeyMachineryGeoOpt2 = "KEY_AFFINITY_GEO_DESC_MACHINERY_OPTION_2";
            private const string KeyComputeGeoOpt1 = "KEY_AFFINITY_GEO_DESC_COMPUTE_OPTION_1";
            private const string KeyComputeGeoOpt2 = "KEY_AFFINITY_GEO_DESC_COMPUTE_OPTION_2";

            private const string KeyPsychoTacOpt1 = "KEY_AFFINITY_TAC_DESC_PSYCHO_SOCIOLOGY_OPTION_1";
            private const string KeyPsychoTacOpt2 = "KEY_AFFINITY_TAC_DESC_PSYCHO_SOCIOLOGY_OPTION_2";
            private const string KeyExplorationTacOpt1 = "KEY_AFFINITY_TAC_DESC_EXPLORATION_OPTION_1";
            private const string KeyExplorationTacOpt2 = "KEY_AFFINITY_TAC_DESC_EXPLORATION_OPTION_2";
            private const string KeyOccultTacOpt1 = "KEY_AFFINITY_TAC_DESC_OCCULT_OPTION_1";
            private const string KeyOccultTacOpt2 = "KEY_AFFINITY_TAC_DESC_OCCULT_OPTION_2";
            private const string KeyBiotechTacOpt1 = "KEY_AFFINITY_TAC_DESC_BIOTECH_OPTION_1";
            private const string KeyBiotechTacOpt2 = "KEY_AFFINITY_TAC_DESC_BIOTECH_OPTION_2";
            private const string KeyMachineryTacOpt1 = "KEY_AFFINITY_TAC_DESC_MACHINERY_OPTION_1";
            private const string KeyMachineryTacOpt2 = "KEY_AFFINITY_TAC_DESC_MACHINERY_OPTION_2";
            private const string KeyComputeTacOpt1 = "KEY_AFFINITY_TAC_DESC_COMPUTE_OPTION_1";
            private const string KeyComputeTacOpt2 = "KEY_AFFINITY_TAC_DESC_COMPUTE_OPTION_2";

            private static readonly Dictionary<LeaderSelection.AffinityApproach, int> GeoBenefitChoiceCache =
                new Dictionary<LeaderSelection.AffinityApproach, int>();

            private static readonly Dictionary<LeaderSelection.AffinityApproach, int> TacticalBenefitChoiceCache =
                new Dictionary<LeaderSelection.AffinityApproach, int>();

            internal static void SetGeoscapeBenefitChoice(
               GeoLevelController level,
               LeaderSelection.AffinityApproach approach,
               int option)
            {
                try
                {
                    int normalized = option <= 1 ? 1 : 2;
                    GeoBenefitChoiceCache[approach] = normalized;

                    if (level?.EventSystem != null)
                    {
                        level.EventSystem.SetVariable(GetGeoBenefitVariableName(approach), normalized);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static int GetGeoscapeBenefitChoice(
                GeoLevelController level,
                LeaderSelection.AffinityApproach approach)
            {
                try
                {
                    if (GeoBenefitChoiceCache.TryGetValue(approach, out int cached) && (cached == 1 || cached == 2))
                    {
                        return cached;
                    }

                    int stored = 0;
                    if (level?.EventSystem != null)
                    {
                        stored = level.EventSystem.GetVariable(GetGeoBenefitVariableName(approach));
                    }

                    int normalized = (stored == 2) ? 2 : 1;
                    GeoBenefitChoiceCache[approach] = normalized;
                    return normalized;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return 1;
                }
            }

            internal static void SetTacticalBenefitChoice(
                GeoLevelController level,
                LeaderSelection.AffinityApproach approach,
                int option)
            {
                try
                {
                    int normalized = option <= 1 ? 1 : 2;
                    TacticalBenefitChoiceCache[approach] = normalized;

                    if (level?.EventSystem != null)
                    {
                        level.EventSystem.SetVariable(GetTacticalBenefitVariableName(approach), normalized);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static int GetTacticalBenefitChoice(
                GeoLevelController level,
                LeaderSelection.AffinityApproach approach)
            {
                try
                {
                    if (TacticalBenefitChoiceCache.TryGetValue(approach, out int cached) && (cached == 1 || cached == 2))
                    {
                        return cached;
                    }

                    int stored = 0;
                    if (level?.EventSystem != null)
                    {
                        stored = level.EventSystem.GetVariable(GetTacticalBenefitVariableName(approach));
                    }

                    int normalized = (stored == 2) ? 2 : 1;
                    TacticalBenefitChoiceCache[approach] = normalized;
                    return normalized;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return 1;
                }
            }

            private static string GetGeoBenefitVariableName(LeaderSelection.AffinityApproach approach)
            {
                return GeoBenefitVariablePrefix + approach.ToString().ToUpperInvariant();
            }

            private static string GetTacticalBenefitVariableName(LeaderSelection.AffinityApproach approach)
            {
                return TacticalBenefitVariablePrefix + approach.ToString().ToUpperInvariant();
            }

            internal static void InitializeDynamicDescriptionResolver()
            {
                AffinityViewElementIntercept.DynamicAbilityDescriptionPatch.DescriptionResolver = ResolveDynamicAffinityDescription;
            }

            private static string ResolveDynamicAffinityDescription(TacticalAbilityDef abilityDef)
            {
                try
                {
                    if (!TryGetAffinityAndRank(abilityDef, out LeaderSelection.AffinityApproach approach, out int rank))
                    {
                        return string.Empty;
                    }

                    GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    int geoOption = GetGeoscapeBenefitChoice(level, approach);
                    int tacOption = GetTacticalBenefitChoice(level, approach);

                    string geo = BuildGeoscapeBenefitDescription(approach, rank, geoOption);
                    string tac = BuildTacticalBenefitDescription(approach, rank, tacOption);

                    if (string.IsNullOrEmpty(geo))
                    {
                        return tac;
                    }

                    if (string.IsNullOrEmpty(tac))
                    {
                        return geo;
                    }

                    return geo + "\n" + tac;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return string.Empty;
                }
            }

            private static bool TryGetAffinityAndRank(
                TacticalAbilityDef abilityDef,
                out LeaderSelection.AffinityApproach approach,
                out int rank)
            {
                approach = default(LeaderSelection.AffinityApproach);
                rank = 0;

                if (abilityDef == null)
                {
                    return false;
                }

                if (TryGetRankFromArray(abilityDef, PsychoSociology, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.PsychoSociology;
                    return true;
                }

                if (TryGetRankFromArray(abilityDef, Exploration, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.Exploration;
                    return true;
                }

                if (TryGetRankFromArray(abilityDef, Occult, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.Occult;
                    return true;
                }

                if (TryGetRankFromArray(abilityDef, Biotech, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.Biotech;
                    return true;
                }

                if (TryGetRankFromArray(abilityDef, Machinery, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.Machinery;
                    return true;
                }

                if (TryGetRankFromArray(abilityDef, Compute, out rank))
                {
                    approach = LeaderSelection.AffinityApproach.Compute;
                    return true;
                }

                return false;
            }

            private static bool TryGetRankFromArray(TacticalAbilityDef abilityDef, PassiveModifierAbilityDef[] abilities, out int rank)
            {
                rank = 0;
                if (abilities == null)
                {
                    return false;
                }

                for (int i = 0; i < abilities.Length; i++)
                {
                    if (abilities[i] == abilityDef)
                    {
                        rank = i + 1;
                        return true;
                    }
                }

                return false;
            }

            private static string BuildGeoscapeBenefitDescription(
                LeaderSelection.AffinityApproach approach,
                int rank,
                int option)
            {
                int r = Math.Max(1, rank);

                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology:
                        return option == 1
                            ? LocalizeAndFormat(KeyPsychoGeoOpt1, 15 * r)
                            : LocalizeAndFormat(KeyPsychoGeoOpt2, 15 * r);

                    case LeaderSelection.AffinityApproach.Exploration:
                        return option == 1
                            ? LocalizeAndFormat(KeyExplorationGeoOpt1, r)
                            : LocalizeAndFormat(KeyExplorationGeoOpt2, 2 * r);

                    case LeaderSelection.AffinityApproach.Occult:
                        return option == 1
                            ? LocalizeAndFormat(KeyOccultGeoOpt1, 6 * r)
                            : LocalizeAndFormat(KeyOccultGeoOpt2, 15 * r);

                    case LeaderSelection.AffinityApproach.Biotech:
                        return option == 1
                            ? LocalizeAndFormat(KeyBiotechGeoOpt1, 30 * r)
                            : LocalizeAndFormat(KeyBiotechGeoOpt2, 15 * r);

                    case LeaderSelection.AffinityApproach.Machinery:
                        return option == 1
                            ? LocalizeAndFormat(KeyMachineryGeoOpt1, 150 * r)
                            : LocalizeAndFormat(KeyMachineryGeoOpt2, 25 * r);

                    case LeaderSelection.AffinityApproach.Compute:
                        return option == 1
                            ? LocalizeAndFormat(KeyComputeGeoOpt1, 10 * r)
                            : LocalizeAndFormat(KeyComputeGeoOpt2);

                    default:
                        return string.Empty;
                }
            }

            private static string BuildTacticalBenefitDescription(
                LeaderSelection.AffinityApproach approach,
                int rank,
                int option)
            {
                int r = Math.Max(1, rank);

                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology:
                        return option == 1
                            ? LocalizeAndFormat(KeyPsychoTacOpt1, 2 * r)
                            : LocalizeAndFormat(KeyPsychoTacOpt2, r);

                    case LeaderSelection.AffinityApproach.Exploration:
                        return option == 1
                            ? LocalizeAndFormat(KeyExplorationTacOpt1, r)
                            : LocalizeAndFormat(KeyExplorationTacOpt2, r, r);

                    case LeaderSelection.AffinityApproach.Occult:
                        return option == 1
                            ? LocalizeAndFormat(KeyOccultTacOpt1, 5 * r)
                            : LocalizeAndFormat(KeyOccultTacOpt2, 50 * r);

                    case LeaderSelection.AffinityApproach.Biotech:
                        return option == 1
                            ? LocalizeAndFormat(KeyBiotechTacOpt1, 10 * r)
                            : LocalizeAndFormat(KeyBiotechTacOpt2, 4 * r);

                    case LeaderSelection.AffinityApproach.Machinery:
                        return option == 1
                            ? LocalizeAndFormat(KeyMachineryTacOpt1, 10 * r)
                            : LocalizeAndFormat(KeyMachineryTacOpt2, 4 * r);

                    case LeaderSelection.AffinityApproach.Compute:
                        return option == 1
                            ? LocalizeAndFormat(KeyComputeTacOpt1, 3 * r, 10 * r)
                            : LocalizeAndFormat(KeyComputeTacOpt2, 50 * r);

                    default:
                        return string.Empty;
                }
            }

            private static string LocalizeAndFormat(string key, params object[] args)
            {
                try
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        return string.Empty;
                    }

                    string localized = new LocalizedTextBind { LocalizationKey = key }.Localize();
                    if (string.IsNullOrEmpty(localized))
                    {
                        localized = key;
                    }

                    if (args == null || args.Length == 0)
                    {
                        return localized;
                    }

                    try
                    {
                        return string.Format(localized, args);
                    }
                    catch
                    {
                        return localized;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return key ?? string.Empty;
                }
            }
        }

        internal class AffinityViewElementIntercept
        {
            /// <summary>
            /// Harmony hook for replacing Geoscape ability description text at runtime.
            /// Patch target: GeoRosterAbilityDetailTooltip.Show(TacticalAbilityDef, ViewElementDef, bool, int).
            /// </summary>
            [HarmonyPatch(typeof(GeoRosterAbilityDetailTooltip), "Show", new Type[]
            {
        typeof(TacticalAbilityDef),
        typeof(ViewElementDef),
        typeof(bool),
        typeof(int)
            })]
            public static class DynamicAbilityDescriptionPatch
            {
                /// <summary>
                /// Assign this from your mod entry to supply dynamic descriptions.
                /// Return null or empty to keep vanilla text.
                /// </summary>
                public static Func<TacticalAbilityDef, string> DescriptionResolver;

                private static void Postfix(GeoRosterAbilityDetailTooltip __instance, TacticalAbilityDef abilityDef)
                {
                    if (abilityDef == null || DescriptionResolver == null)
                    {
                        return;
                    }

                    string text = DescriptionResolver(abilityDef);
                    if (!string.IsNullOrEmpty(text))
                    {
                        __instance.AbilityDescription.text = text;
                    }
                }
            }
        }
    }
}