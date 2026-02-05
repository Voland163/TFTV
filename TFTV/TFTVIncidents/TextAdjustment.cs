using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TFTV.TFTVIncidents
{
    internal class TextAdjustment
    {

        /// <summary>
        /// Harmony postfix patch for GeoscapeEventContext.ReplaceEventTokens.
        ///
        /// Then use tokens like [MyFactionName], [MyHavenPopulation], [MyUtcDate] in
        /// Geoscape event descriptions/choices.
        /// </summary>
        [HarmonyPatch(typeof(GeoscapeEventContext), nameof(GeoscapeEventContext.ReplaceEventTokens))]
        public static class GeoscapeEventTokenPostfixPatch
        {
            // Register any custom tokens you want to support.
            private static readonly Regex IncidentIdRegex = new Regex(@"TFTV_INCIDENT_(\d+)_", RegexOptions.IgnoreCase);

            private static readonly Dictionary<string, Func<GeoscapeEventContext, string>> _customTokenReplacers =
                new Dictionary<string, Func<GeoscapeEventContext, string>>(StringComparer.Ordinal)
                {
                    { "[RequirementOperativeName]", GetRequirementOperativeName },
                    { "[RequirementHavenName]", GetRequirementHavenName },
                    { "[OperativeName]", GetOperativeName }
                };

            public static void Postfix(GeoscapeEventContext __instance, ref string __result)
            {
                if (string.IsNullOrEmpty(__result))
                {
                    return;
                }

                foreach (KeyValuePair<string, Func<GeoscapeEventContext, string>> entry in _customTokenReplacers)
                {
                    if (!__result.Contains(entry.Key))
                    {
                        continue;
                    }

                    string replacement = string.Empty;
                    try
                    {
                        replacement = entry.Value?.Invoke(__instance) ?? string.Empty;
                    }
                    catch
                    {
                        // Keep event rendering safe even if a token callback fails.
                        replacement = string.Empty;
                    }

                    __result = __result.Replace(entry.Key, replacement);
                }
            }

            private static string GetRequirementOperativeName(GeoscapeEventContext context)
            {
                Objects.GeoIncidentDefinition incident = GetIncidentDefinition(context);
                if (incident == null || incident.EligibilityConditions == null)
                {
                    return string.Empty;
                }

                GeoFactionDef backgroundFaction = incident.EligibilityConditions
                    .Select(c => c?.RequiredCharacterBackgroundFaction)
                    .FirstOrDefault(f => f != null);

                if (backgroundFaction == null)
                {
                    return string.Empty;
                }

                GeoPhoenixFaction phoenixFaction = GetPhoenixFaction(context);
                GeoCharacter operative = phoenixFaction?.Soldiers
                    ?.FirstOrDefault(c => c != null && c.OriginalFactionDef == backgroundFaction);

                return GetCharacterName(operative);
            }

            private static string GetRequirementHavenName(GeoscapeEventContext context)
            {
                Objects.GeoIncidentDefinition incident = GetIncidentDefinition(context);
                if (incident == null || incident.EligibilityConditions == null)
                {
                    return string.Empty;
                }

                Objects.GeoIncidentEligibilityCondition nearbyCondition = incident.EligibilityConditions
                    .FirstOrDefault(c => c != null && c.NearbyHavenRange > EarthUnits.Zero);

                if (nearbyCondition == null)
                {
                    return string.Empty;
                }

                GeoHaven originHaven = GetIncidentHaven(context);
                if (originHaven == null)
                {
                    return string.Empty;
                }

                GeoFaction visitingFaction = GetPhoenixFaction(context);
                GeoHaven nearbyHaven = GetFirstNearbyEligibleHaven(originHaven, visitingFaction, nearbyCondition);
                return nearbyHaven?.Site?.LocalizedSiteName ?? string.Empty;
            }

            private static string GetOperativeName(GeoscapeEventContext context)
            {
                GeoSite site = GetContextSite(context);
                GeoPhoenixFaction phoenixFaction = GetPhoenixFaction(context);
                if (site == null || phoenixFaction == null)
                {
                    return string.Empty;
                }

                GeoCharacter operative = site.Vehicles
                    ?.Where(v => v != null && v.Owner == phoenixFaction)
                    .SelectMany(v => v.GetAllCharacters())
                    .FirstOrDefault(IsHumanGeoCharacter);

                return GetCharacterName(operative);
            }

            private static GeoHaven GetFirstNearbyEligibleHaven(
                GeoHaven haven,
                GeoFaction visitingFaction,
                Objects.GeoIncidentEligibilityCondition condition)
            {
                GeoSite site = haven?.Site;
                if (site == null || haven.Range == null)
                {
                    return null;
                }

                IEnumerable<Objects.GeoIncidentEligibilityCondition> nearbyConditions = condition.NearbyHavenConditions;
                EarthUnits range = haven.Range.Range;

                foreach (GeoSite geoSite in haven.Range.SitesInRange)
                {
                    if (geoSite == site)
                    {
                        continue;
                    }

                    GeoHaven candidate = geoSite.GetComponent<GeoHaven>();
                    if (candidate == null)
                    {
                        continue;
                    }

                    bool isEligible = nearbyConditions == null || nearbyConditions.All(c => c.IsEligible(candidate, visitingFaction));
                    if (isEligible && GeoMap.Distance(site, geoSite) <= range)
                    {
                        return candidate;
                    }
                }

                return null;
            }

            private static Objects.GeoIncidentDefinition GetIncidentDefinition(GeoscapeEventContext context)
            {
                if (GeoscapeEvents.IncidentDefinitions == null || GeoscapeEvents.IncidentDefinitions.Count == 0)
                {
                    return null;
                }

                string eventId = GetEventId(context);
                if (string.IsNullOrEmpty(eventId))
                {
                    return null;
                }

                Match match = IncidentIdRegex.Match(eventId);
                if (!match.Success)
                {
                    return null;
                }

                int incidentId;
                if (!int.TryParse(match.Groups[1].Value, out incidentId))
                {
                    return null;
                }

                return GeoscapeEvents.IncidentDefinitions.FirstOrDefault(i => i.Id == incidentId);
            }

            private static string GetEventId(GeoscapeEventContext context)
            {
                string eventId = GetStringMemberValue(context, "EventID");
                if (!string.IsNullOrEmpty(eventId))
                {
                    return eventId;
                }

                object eventObj = GetMemberValue(context, "Event") ?? GetMemberValue(context, "GeoscapeEvent");
                eventId = GetStringMemberValue(eventObj, "EventID");
                if (!string.IsNullOrEmpty(eventId))
                {
                    return eventId;
                }

                object eventData = GetMemberValue(context, "EventData")
                    ?? GetMemberValue(eventObj, "EventData")
                    ?? GetMemberValue(eventObj, "GeoscapeEventData");

                return GetStringMemberValue(eventData, "EventID");
            }

            private static GeoSite GetContextSite(GeoscapeEventContext context)
            {
                GeoSite site = GetMemberValue(context, "Site") as GeoSite;
                if (site != null)
                {
                    return site;
                }

                object eventObj = GetMemberValue(context, "Event") ?? GetMemberValue(context, "GeoscapeEvent");
                site = GetMemberValue(eventObj, "Site") as GeoSite;
                if (site != null)
                {
                    return site;
                }

                object eventData = GetMemberValue(context, "EventData")
                    ?? GetMemberValue(eventObj, "EventData")
                    ?? GetMemberValue(eventObj, "GeoscapeEventData");

                return GetMemberValue(eventData, "Site") as GeoSite;
            }

            private static GeoLevelController GetGeoLevel(GeoscapeEventContext context)
            {
                GeoSite site = GetContextSite(context);
                if (site?.GeoLevel != null)
                {
                    return site.GeoLevel;
                }

                return GetMemberValue(context, "GeoLevel") as GeoLevelController;
            }

            private static GeoPhoenixFaction GetPhoenixFaction(GeoscapeEventContext context)
            {
                return GetGeoLevel(context)?.PhoenixFaction ?? GetMemberValue(context, "PhoenixFaction") as GeoPhoenixFaction;
            }

            private static GeoHaven GetIncidentHaven(GeoscapeEventContext context)
            {
                return GetContextSite(context)?.GetComponent<GeoHaven>();
            }

            private static bool IsHumanGeoCharacter(GeoCharacter character)
            {
                if (character == null || character.IsMutoid)
                {
                    return false;
                }

                return !GetBoolMemberValue(character, "IsMutog");
            }

            private static string GetCharacterName(GeoCharacter character)
            {
                if (character == null)
                {
                    return string.Empty;
                }

                string name = GetStringMemberValue(character, "DisplayName");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                name = GetStringMemberValue(character, "Name");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                object identity = GetMemberValue(character, "Identity");
                name = GetStringMemberValue(identity, "Name");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return string.Empty;
            }

            private static object GetMemberValue(object instance, string memberName)
            {
                if (instance == null || string.IsNullOrEmpty(memberName))
                {
                    return null;
                }

                Type type = instance.GetType();
                var property = AccessTools.Property(type, memberName);
                if (property != null)
                {
                    return property.GetValue(instance, null);
                }

                var field = AccessTools.Field(type, memberName);
                return field != null ? field.GetValue(instance) : null;
            }

            private static string GetStringMemberValue(object instance, string memberName)
            {
                object value = GetMemberValue(instance, memberName);
                if (value == null)
                {
                    return string.Empty;
                }

                string text = value as string;
                return !string.IsNullOrEmpty(text) ? text : value.ToString();
            }

            private static bool GetBoolMemberValue(object instance, string memberName)
            {
                object value = GetMemberValue(instance, memberName);
                return value is bool boolValue && boolValue;
            }


        }

    }
}
