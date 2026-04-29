using Base.Core;
using Base.Utils.GameConsole;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV;
using TFTV.TFTVBaseRework;
using TFTV.TFTVIncidents;

namespace MadSkunkyTweaks.Tools
{
    public class ConsoleCommands
    {
        private static readonly LeaderSelection.AffinityApproach[] AllAffinityApproaches =
        {
            LeaderSelection.AffinityApproach.PsychoSociology,
            LeaderSelection.AffinityApproach.Exploration,
            LeaderSelection.AffinityApproach.Occult,
            LeaderSelection.AffinityApproach.Biotech,
            LeaderSelection.AffinityApproach.Machinery,
            LeaderSelection.AffinityApproach.Compute
        };

        [ConsoleCommand(Command = "checkcrates", Description = "tell me what's inside the crates")]
        public static void SayHello(IConsole console)
        {
            TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

            foreach (TacticalActorBase actor in tacticalLevelController.Map.GetActors<TacticalActorBase>())
            {
                TFTVLogger.Always($"{actor?.name}");

                if (actor is CrateItemContainer crate)
                {
                    foreach (Item item in crate.Inventory.Items)
                    {
                        TFTVLogger.Always($"item in crate is {item.ItemDef.name}");
                    }
                }
            }
        }

        [ConsoleCommand(
            Command = "list_affinity_ops",
            Description = "Lists Phoenix operative IDs for affinity testing.")]
        public static void ListAffinityOperatives(IConsole console)
        {
            try
            {
                GeoLevelController level = GetCurrentGeoLevel();
                if (level?.PhoenixFaction?.Characters == null)
                {
                    TFTVLogger.Always("[AffinityTest] Geoscape level not available.");
                    return;
                }

                foreach (GeoCharacter operative in level.PhoenixFaction.Characters
                    .Where(c => c != null)
                    .OrderBy(c => c.Id))
                {
                    TFTVLogger.Always($"[AffinityTest] ID {operative.Id}: {GetOperativeName(operative)}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
            Command = "aff_psycho",
            Description = "Usage: aff_psycho <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyPsychoSociologyAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.PsychoSociology,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_exploration",
            Description = "Usage: aff_exploration <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyExplorationAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Exploration,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_occult",
            Description = "Usage: aff_occult <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyOccultAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Occult,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_biotech",
            Description = "Usage: aff_biotech <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyBiotechAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Biotech,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_machinery",
            Description = "Usage: aff_machinery <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyMachineryAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Machinery,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_compute",
            Description = "Usage: aff_compute <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyComputeAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Compute,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "incident_list",
            Description = "Lists available incident IDs.")]
        public static void ListIncidents(IConsole console)
        {
            try
            {
                if (!EnsureIncidentDefinitionsAvailable())
                {
                    TFTVLogger.Always("[IncidentTest] Incident definitions are not available.");
                    return;
                }

                foreach (Objects.GeoIncidentDefinition incident in GeoscapeEvents.IncidentDefinitions
                    .Where(i => i != null && i.IntroEvent != null)
                    .OrderBy(i => i.Id))
                {
                    string factionShortName = incident.FactionDef != null && incident.FactionDef.PPFactionDef != null
                        ? incident.FactionDef.PPFactionDef.ShortName
                        : "ANY";

                    TFTVLogger.Always($"[IncidentTest] {incident.Id} ({factionShortName}) -> {incident.IntroEvent.EventID}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
            Command = "incident_trigger",
            Description = "Usage: incident_trigger <incidentId> [siteNameContains]")]
        public static void TriggerIncident(IConsole console, int incidentId, params string[] siteNameParts)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    TFTVLogger.Always("[IncidentTest] Base Rework is disabled.");
                    return;
                }

                GeoLevelController level = GetCurrentGeoLevel();
                if (level == null)
                {
                    TFTVLogger.Always("[IncidentTest] This command must be used in geoscape.");
                    return;
                }

                string siteNameFilter = siteNameParts == null || siteNameParts.Length == 0
                    ? string.Empty
                    : string.Join(" ", siteNameParts).Trim();

                if (!Roll.TryTriggerIncident(level, incidentId, siteNameFilter))
                {
                    string suffix = string.IsNullOrEmpty(siteNameFilter) ? string.Empty : $" for site filter '{siteNameFilter}'";
                    TFTVLogger.Always($"[IncidentTest] Failed to trigger incident {incidentId}{suffix}.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool EnsureIncidentDefinitionsAvailable()
        {
            if (GeoscapeEvents.IncidentDefinitions != null && GeoscapeEvents.IncidentDefinitions.Count > 0)
            {
                return true;
            }

            GeoscapeEvents.CreateGeoscapeEvents();
            return GeoscapeEvents.IncidentDefinitions != null && GeoscapeEvents.IncidentDefinitions.Count > 0;
        }

        /// Injcecting the mods console commands to the base game console handler
        public static void InjectConsoleCommands()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                foreach (MethodInfo methodInfo in types[i].GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute(methodInfo, typeof(ConsoleCommandAttribute)) is ConsoleCommandAttribute consoleCommandAttribute)
                    {
                        if (!methodInfo.IsPublic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not public."
                            }));
                        }
                        if (!methodInfo.IsStatic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not static."
                            }));
                        }
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0 || !typeof(IConsole).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that does not have something implementing IConsole as first argument."
                            }));
                        }
                        int k = 1;
                        int num = parameters.Length;
                        while (k < num)
                        {
                            ParameterInfo parameterInfo = parameters[k];
                            if (k == parameters.Length - 1 && parameterInfo.ParameterType.IsArray && parameterInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length != 0 && parameterInfo.ParameterType.GetElementType() == typeof(string))
                            {
                                typeof(ConsoleCommandAttribute).GetField("_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, true);
                            }
                            else if (!TypeToConvertFunc.ContainsKey(parameterInfo.ParameterType))
                            {
                                throw new InvalidOperationException(string.Concat(new string[]
                                {
                                    "ConsoleCommandAttribute is defined on method ",
                                    methodInfo.DeclaringType.FullName,
                                    ".",
                                    methodInfo.Name,
                                    " that has a parameter ",
                                    parameterInfo.Name,
                                    " that is of unsupported type."
                                }));
                            }
                            k++;
                        }
                        typeof(ConsoleCommandAttribute).GetField("_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, methodInfo);
                        string key = consoleCommandAttribute.Command ?? methodInfo.Name;

                        // get access to the base game private static field of the console command handler to inject all commands from this mod
                        // Original: ConsoleCommandAttribute.CommandToInfo[key] = consoleCommandAttribute;
                        SortedList<string, ConsoleCommandAttribute> BaseCommandToInfo = (SortedList<string, ConsoleCommandAttribute>)typeof(ConsoleCommandAttribute).GetField("CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                        BaseCommandToInfo[key] = consoleCommandAttribute;
                    }
                }
            }
        }

        private static void ApplyAffinityCommand(
            LeaderSelection.AffinityApproach approach,
            int operativeId,
            int rank,
            int geoOption,
            int tacticalOption)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    TFTVLogger.Always("[AffinityTest] Base Rework is disabled.");
                    return;
                }

                GeoLevelController level = GetCurrentGeoLevel();
                if (level?.PhoenixFaction?.Characters == null)
                {
                    TFTVLogger.Always("[AffinityTest] This command must be used in geoscape.");
                    return;
                }

                GeoCharacter operative = level.PhoenixFaction.Characters.FirstOrDefault(c => c != null && c.Id == operativeId);
                if (operative == null)
                {
                    TFTVLogger.Always($"[AffinityTest] No operative found with ID {operativeId}. Use list_affinity_ops first.");
                    return;
                }

                PassiveModifierAbilityDef abilityToAdd = GetAffinityAbilityForRank(approach, rank, out int normalizedRank);
                if (abilityToAdd == null)
                {
                    TFTVLogger.Always($"[AffinityTest] Could not resolve affinity data for {approach}.");
                    return;
                }

                int removedAbilities = RemoveAllAffinityAbilities(operative);

                if (operative.Progression != null && !operative.Progression.Abilities.Contains(abilityToAdd))
                {
                    operative.Progression.AddAbility(abilityToAdd);
                }

                int normalizedGeoOption = NormalizeOption(geoOption);
                int normalizedTacticalOption = NormalizeOption(tacticalOption);

                Affinities.AffinityBenefitsChoices.SetGeoscapeBenefitChoice(level, approach, normalizedGeoOption);
                Affinities.AffinityBenefitsChoices.SetTacticalBenefitChoice(level, approach, normalizedTacticalOption);
                Affinities.AffinityBenefitsChoices.CaptureTacticalBenefitChoiceSnapshot(level);
                Affinities.AffinityBenefitsChoices.RefreshTacticalAbilityDescriptionsFromSnapshot();

                TFTVLogger.Always(
                    $"[AffinityTest] Applied {approach} rank {normalizedRank} to {GetOperativeName(operative)} (ID {operative.Id}). " +
                    $"Geo option {normalizedGeoOption}, tactical option {normalizedTacticalOption}, removed {removedAbilities} existing affinity ability entries.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static GeoLevelController GetCurrentGeoLevel()
        {
            try
            {
                return GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static int NormalizeOption(int option)
        {
            return option == 2 ? 2 : 1;
        }

        private static PassiveModifierAbilityDef GetAffinityAbilityForRank(
            LeaderSelection.AffinityApproach approach,
            int rank,
            out int normalizedRank)
        {
            normalizedRank = Math.Max(1, Math.Min(3, rank));
            PassiveModifierAbilityDef[] track = GetAffinityTrack(approach);

            if (track == null || track.Length < normalizedRank)
            {
                return null;
            }

            return track[normalizedRank - 1];
        }

        private static PassiveModifierAbilityDef[] GetAffinityTrack(LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.PsychoSociology:
                    return Affinities.PsychoSociology;
                case LeaderSelection.AffinityApproach.Exploration:
                    return Affinities.Exploration;
                case LeaderSelection.AffinityApproach.Occult:
                    return Affinities.Occult;
                case LeaderSelection.AffinityApproach.Biotech:
                    return Affinities.Biotech;
                case LeaderSelection.AffinityApproach.Machinery:
                    return Affinities.Machinery;
                case LeaderSelection.AffinityApproach.Compute:
                    return Affinities.Compute;
                default:
                    return null;
            }
        }

        private static int RemoveAllAffinityAbilities(GeoCharacter operative)
        {
            try
            {
                if (operative?.Progression == null)
                {
                    return 0;
                }

                List<TacticalAbilityDef> abilities = Traverse.Create(operative.Progression)
                    .Field("_abilities")
                    .GetValue<List<TacticalAbilityDef>>();

                if (abilities == null)
                {
                    return 0;
                }

                int removed = 0;

                foreach (LeaderSelection.AffinityApproach approach in AllAffinityApproaches)
                {
                    PassiveModifierAbilityDef[] track = GetAffinityTrack(approach);
                    if (track == null || track.Length == 0)
                    {
                        continue;
                    }

                    removed += abilities.RemoveAll(ability =>
                        ability != null && track.Any(def => def == ability));
                }

                return removed;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        private static string GetOperativeName(GeoCharacter operative)
        {
            if (operative == null)
            {
                return "UNKNOWN";
            }

            if (!string.IsNullOrEmpty(operative.DisplayName))
            {
                return operative.DisplayName;
            }

            return operative.GetName();
        }

        public static readonly Dictionary<Type, Func<string, object>> TypeToConvertFunc = new Dictionary<Type, Func<string, object>>
        {
            {
                typeof(sbyte),
                (string v) => sbyte.Parse(v)
            },
            {
                typeof(short),
                (string v) => short.Parse(v)
            },
            {
                typeof(int),
                (string v) => int.Parse(v)
            },
            {
                typeof(long),
                (string v) => long.Parse(v)
            },
            {
                typeof(byte),
                (string v) => byte.Parse(v)
            },
            {
                typeof(ushort),
                (string v) => ushort.Parse(v)
            },
            {
                typeof(uint),
                (string v) => uint.Parse(v)
            },
            {
                typeof(ulong),
                (string v) => ulong.Parse(v)
            },
            {
                typeof(float),
                (string v) => float.Parse(v)
            },
            {
                typeof(double),
                (string v) => double.Parse(v)
            },
            {
                typeof(string),
                (string v) => v
            },
            {
                typeof(bool),
                delegate(string v)
                {
                    float num;
                    if (float.TryParse(v, out num))
                    {
                        return num != 0f;
                    }
                    return bool.Parse(v);
                }
            }
        };
    }
}
