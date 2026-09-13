using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// What a response pays if it succeeds, as icons and a rough size, for the strip under each
    /// response on the incident screen.
    ///
    /// The player's real question at this screen is "I have two operatives whose affinities fit
    /// different approaches - which is the better pick?", and without this there is nothing on
    /// screen to answer it with: both responses look identical apart from their prose. What is shown
    /// is the kind of payoff and a magnitude of one to three marks, deliberately not the numbers -
    /// enough to tell a supply run from a diplomatic favour, and a small favour from a large one,
    /// without turning the choice into arithmetic before it is made.
    ///
    /// These are the rewards of the *success* outcome. An incident can fail, and a failure pays its
    /// own outcome; the strip is what is at stake, not what is promised.
    /// </summary>
    internal static class ChoicePayoff
    {
        /// <summary>Most marks a payoff can show. Three reads as a scale; more reads as a number.</summary>
        internal const int MaxTier = 3;

        /// <summary>
        /// One payoff: what it is, and how much of it on a one-to-three scale.
        /// </summary>
        internal sealed class Entry
        {
            public Sprite Icon;
            public int Tier;
            public Color Tint;
        }

        // Resource icons by type, resolved once from the def repository. There is no lookup on
        // SharedData for this, and walking every def to find one sprite per resource type is not
        // something to do while a UI is being built.
        private static Dictionary<ResourceType, Sprite> _resourceIcons;

        // The geoscape info bar's own personnel mark, so a crew reward is drawn with the same icon
        // the player counts their operatives with. Cached per level, since the bar is rebuilt with
        // the geoscape.
        private static Sprite _personnelIcon;
        private static bool _personnelIconResolved;

        /// <summary>
        /// The payoffs of the given choice's success outcome, in a fixed order - resources, then
        /// diplomacy, then personnel - so the same incident always reads the same way.
        /// Returns an empty list for a choice with no success outcome, which is the cancel choice.
        /// </summary>
        internal static List<Entry> Resolve(GeoscapeEvent introEvent, int choiceIndex)
        {
            List<Entry> entries = new List<Entry>();

            try
            {
                GeoEventChoiceOutcome outcome = ResolveSuccessOutcome(introEvent, choiceIndex);
                if (outcome == null)
                {
                    return entries;
                }

                AddResourceEntries(outcome, entries);
                AddDiplomacyEntries(outcome, entries);
                AddPersonnelEntry(outcome, entries);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return entries;
        }

        /// <summary>
        /// Forgets the lookups that do not outlive a geoscape. The icons are owned by defs and by the
        /// info bar rather than by this class, so nothing is freed here.
        ///
        /// The resource icons are not among them: they come from defs that are loaded once for the
        /// process, and rebuilding that map means walking every def in the game - not something to
        /// do each time an incident is opened.
        /// </summary>
        internal static void ClearCaches()
        {
            _personnelIcon = null;
            _personnelIconResolved = false;
        }

        private static GeoEventChoiceOutcome ResolveSuccessOutcome(GeoscapeEvent introEvent, int choiceIndex)
        {
            string eventId = introEvent?.EventID;
            if (string.IsNullOrEmpty(eventId) || GeoscapeEvents.IncidentDefinitions == null)
            {
                return null;
            }

            Objects.GeoIncidentDefinition incident = GeoscapeEvents.IncidentDefinitions.FirstOrDefault(i =>
                i?.IntroEvent != null
                && string.Equals(i.IntroEvent.EventID, eventId, StringComparison.OrdinalIgnoreCase));

            GeoscapeEventDef resolution = choiceIndex == 0
                ? incident?.ChoiceAResolutionSuccess
                : incident?.ChoiceBResolutionSuccess;

            List<GeoEventChoice> choices = resolution?.GeoscapeEventData?.Choices;
            return choices != null && choices.Count > 0 ? choices[0].Outcome : null;
        }

        private static void AddResourceEntries(GeoEventChoiceOutcome outcome, List<Entry> entries)
        {
            if (outcome.Resources == null)
            {
                return;
            }

            // One mark per resource type, not per entry: an outcome that grants the same resource
            // twice is one payoff of their combined size, which is what the player receives.
            Dictionary<ResourceType, float> totals = new Dictionary<ResourceType, float>();

            foreach (ResourceUnit unit in outcome.Resources)
            {
                if (unit.Value <= 0f)
                {
                    continue;
                }

                totals.TryGetValue(unit.Type, out float running);
                totals[unit.Type] = running + unit.Value;
            }

            foreach (KeyValuePair<ResourceType, float> total in totals)
            {
                Sprite icon = ResolveResourceIcon(total.Key);
                if (icon == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Icon = icon,
                    Tier = ResourceTier(total.Key, total.Value),
                    Tint = IncidentUIStyle.Payoff,
                });
            }
        }

        /// <summary>
        /// Standing with a faction, one mark per faction. An outcome usually moves both the faction's
        /// own opinion and its leader's, and those are one payoff to the player, so the faction is
        /// shown once at the larger of the two.
        /// </summary>
        private static void AddDiplomacyEntries(GeoEventChoiceOutcome outcome, List<Entry> entries)
        {
            if (outcome.Diplomacy == null)
            {
                return;
            }

            Dictionary<GeoFactionDef, int> best = new Dictionary<GeoFactionDef, int>();

            foreach (OutcomeDiplomacyChange change in outcome.Diplomacy)
            {
                if (change.PartyFaction == null || change.Value <= 0)
                {
                    continue;
                }

                best.TryGetValue(change.PartyFaction, out int running);
                if (change.Value > running)
                {
                    best[change.PartyFaction] = change.Value;
                }
            }

            foreach (KeyValuePair<GeoFactionDef, int> faction in best)
            {
                Sprite icon = faction.Key.SmallBaseIcon;
                if (icon == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Icon = icon,
                    Tier = DiplomacyTier(faction.Value),
                    Tint = faction.Key.FactionColor,
                });
            }
        }

        private static void AddPersonnelEntry(GeoEventChoiceOutcome outcome, List<Entry> entries)
        {
            int count = outcome.CustomCharacters != null ? outcome.CustomCharacters.Count(c => c != null) : 0;
            if (count <= 0)
            {
                return;
            }

            Sprite icon = ResolvePersonnelIcon();
            if (icon == null)
            {
                return;
            }

            entries.Add(new Entry
            {
                Icon = icon,
                Tier = Mathf.Clamp(count, 1, MaxTier),
                Tint = IncidentUIStyle.Payoff,
            });
        }

        /// <summary>
        /// Where a resource amount falls on the one-to-three scale.
        ///
        /// Tech is on its own thresholds because it is handed out in far smaller numbers than the
        /// rest - a hundred Tech is a substantial reward where a hundred Supplies is not - so one
        /// shared scale would mark every Tech reward as the smallest there is.
        /// </summary>
        private static int ResourceTier(ResourceType type, float value)
        {
            if (type == ResourceType.Tech)
            {
                return value < 80f ? 1 : value < 200f ? 2 : MaxTier;
            }

            return value < 300f ? 1 : value < 500f ? 2 : MaxTier;
        }

        private static int DiplomacyTier(int value)
        {
            return value < 5 ? 1 : value < 8 ? 2 : MaxTier;
        }

        private static Sprite ResolveResourceIcon(ResourceType type)
        {
            if (_resourceIcons == null)
            {
                _resourceIcons = new Dictionary<ResourceType, Sprite>();

                DefRepository repository = GameUtl.GameComponent<DefRepository>();
                IEnumerable<BaseDef> allDefs = repository?.DefRepositoryDef?.AllDefs;
                if (allDefs != null)
                {
                    foreach (ResourceViewElementDef def in allDefs.OfType<ResourceViewElementDef>())
                    {
                        if (def.Visual != null && !_resourceIcons.ContainsKey(def.Type))
                        {
                            _resourceIcons[def.Type] = def.Visual;
                        }
                    }
                }
            }

            return _resourceIcons.TryGetValue(type, out Sprite icon) ? icon : null;
        }

        /// <summary>
        /// The personnel mark from the geoscape info bar - the icon next to the operative count the
        /// player already reads their roster off.
        /// </summary>
        private static Sprite ResolvePersonnelIcon()
        {
            if (_personnelIconResolved)
            {
                return _personnelIcon;
            }

            _personnelIconResolved = true;

            try
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                Text soldiersLabel = level?.View?.GeoscapeModules?.ResourcesModule?.SoldiersLabel;
                Transform container = soldiersLabel != null ? soldiersLabel.transform.parent : null;
                ResourceIconContainer icons = container != null ? container.GetComponent<ResourceIconContainer>() : null;
                _personnelIcon = icons?.Icon != null ? icons.Icon.sprite : null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return _personnelIcon;
        }
    }
}
