using Base.Core;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using TFTV.TFTVBaseRework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Resolution = TFTV.TFTVIncidents.Resolution;

namespace TFTV.AgendaTracker
{
    internal static class AgendaHelpers
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        // Well-known ViewElementDef GUIDs
        private const string AncientSiteViewElementGuid = "E_ViewElement [ArcheologyLab_PhoenixFacilityDef]";
        private const string CrabmanViewElementGuid = "E_View [Crabman_ActorViewDef]";

        #region ViewElementDef helpers

        internal static ViewElementDef GetGenericSiteViewElement()
        {
            return DefCache.GetDef<ViewElementDef>(AncientSiteViewElementGuid);
        }

        internal static ViewElementDef GetCrabmanViewElement()
        {
            return DefCache.GetDef<ViewElementDef>(CrabmanViewElementGuid);
        }

        internal static ViewElementDef GetTrainingViewElement()
        {
            var trainingFacility = DefCache.GetDef<PhoenixFacilityDef>("TrainingFacility_PhoenixFacilityDef");
            return trainingFacility?.ViewElementDef ?? GetGenericSiteViewElement();
        }

        internal static ViewElementDef GetCustomSiteViewElement(GeoSite site)
        {
            return site != null && GetPendingBaseAction(site).HasValue
                ? GetTrainingViewElement()
                : GetGenericSiteViewElement();
        }

        internal static ViewElementDef GetAttackViewElement(GeoSite site)
        {
            return site.IsArcheologySite
                ? GetGenericSiteViewElement()
                : GetCrabmanViewElement();
        }

        #endregion

        #region Pending base / incident helpers

        internal static BaseActivation.PendingBaseAction? GetPendingBaseAction(GeoSite site)
        {
            if (site == null) return null;
            if (site.SiteTags.Contains(BaseActivation.PhoenixBaseReworkState.PendingOutpostTag))
                return BaseActivation.PendingBaseAction.Outpost;
            if (site.SiteTags.Contains(BaseActivation.PhoenixBaseReworkState.PendingBaseUpgradeTag))
                return BaseActivation.PendingBaseAction.UpgradeToBase;
            if (site.SiteTags.Contains(BaseActivation.PhoenixBaseReworkState.PendingBaseTag))
                return BaseActivation.PendingBaseAction.FullBase;
            return null;
        }

        internal static Resolution.ActiveTimedProblem GetActiveIncident(GeoSite site)
        {
            return Resolution.IncidentController.GetActiveTimedProblem(site);
        }

        internal static bool HasCustomSiteTracker(GeoSite site)
        {
            return GetPendingBaseAction(site).HasValue || GetActiveIncident(site) != null;
        }

        #endregion

        #region Tracker text builders

        internal static string GetSiteName(GeoSite site, GeoFaction faction)
        {
            if (site == null) return "POI";

            string siteName = null;
            if (site.GetInspected(faction))
            {
                switch (site.Type)
                {
                    case GeoSiteType.PhoenixBase:
                    case GeoSiteType.Haven:
                        siteName = site.Name;
                        break;
                    case GeoSiteType.AlienBase:
                        siteName = site.GetComponent<GeoAlienBase>()?.AlienBaseTypeDef?.Name?.Localize();
                        break;
                    case GeoSiteType.Scavenging:
                        siteName = AgendaConstants.scavengingSiteName;
                        break;
                    case GeoSiteType.Exploration:
                        siteName = AgendaConstants.explorationSiteName;
                        break;
                    default:
                        if (site.IsArcheologySite) siteName = AgendaConstants.ancientSiteName;
                        break;
                }
            }
            else
            {
                siteName = AgendaConstants.unexploredSiteName;
            }

            return string.IsNullOrEmpty(siteName) ? "POI" : siteName;
        }

        internal static string GetCustomSiteTrackerText(GeoSite site, GeoFaction viewerFaction)
        {
            string siteName = GetSiteName(site, viewerFaction ?? site?.Owner);

            var pendingAction = GetPendingBaseAction(site);
            if (pendingAction.HasValue)
            {
                switch (pendingAction.Value)
                {
                    case BaseActivation.PendingBaseAction.Outpost:
                        return $"{AgendaConstants.actionActivatingOutpost} {siteName}";
                    case BaseActivation.PendingBaseAction.UpgradeToBase:
                        return $"{AgendaConstants.actionUpgradingBase} {siteName}";
                    default:
                        return $"{AgendaConstants.actionActivatingBase} {siteName}";
                }
            }

            if (GetActiveIncident(site) != null)
                return $"{AgendaConstants.actionResolvingIncident} {siteName}";

            return null;
        }

        internal static string GetRecruitTrainingTrackerText(GeoCharacter character)
        {
            if (character == null) return null;
            var session = TrainingFacilityRework.GetRecruitSession(character);
            if (session == null) return null;

            string spec = session.TargetSpecialization?.ViewElementDef?.DisplayName1?.Localize();
            string levelInfo = $"Lvl {session.VirtualLevelAchieved}\u2192{session.TargetLevel}";

            return string.IsNullOrEmpty(spec)
                ? $"{AgendaConstants.actionTrainingOperative} {character.DisplayName} ({levelInfo})"
                : $"{AgendaConstants.actionTrainingOperative} {character.DisplayName} ({spec}, {levelInfo})";
        }

        
        

        #endregion

        #region Time calculations

        internal static TimeUnit GetRecruitTrainingRemainingTime(GeoCharacter character, GeoLevelController level)
        {
            if (character == null || level == null) return TimeUnit.Zero;
            var session = TrainingFacilityRework.GetRecruitSession(character);
            if (session == null) return TimeUnit.Zero;

            // Delegate to the same float-hours helper used by IsRecruitTrainingComplete, so the
            // Agenda Tracker's countdown/removal and the auto-deploy popup always agree on
            // "time remaining" to the same precision instead of computing it independently.
            double remainingHours = TrainingFacilityRework.GetRecruitRemainingHours(character, level);
            return TimeUnit.FromHours((float)remainingHours);
        }

       

        #endregion

        #region Travel time cache

        private sealed class TravelTimeCacheEntry
        {
            public GeoSite Destination;
            public Vector3 DestinationPosition;
            public float CachedHours;
            public float Speed;
            public bool HasCalculationTime;
            public TimeUnit CalculatedAt;

            public bool Matches(GeoSite dest, Vector3 pos, float spd)
            {
                return Destination == dest
                    && (DestinationPosition - pos).sqrMagnitude <= 0.01f
                    && Mathf.Abs(Speed - spd) <= 0.01f;
            }
        }

        private static readonly Dictionary<int, TravelTimeCacheEntry> _travelTimeCache = new Dictionary<int, TravelTimeCacheEntry>();

       

        #endregion

        #region Sprite loading

        internal static void LoadSprites(UIModuleInfoBar infoBar)
        {
            try
            {
                if (AgendaConstants.aircraftSprite == null)
                    AgendaConstants.aircraftSprite = infoBar.AirVehiclesLabel.transform.parent.gameObject.GetComponentInChildren<Image>(true).sprite;

                if (AgendaConstants.ancientSiteProbeSprite == null)
                    AgendaConstants.ancientSiteProbeSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [AncientSiteProbeAbilityDef]").SmallIcon;

                if (AgendaConstants.archeologyLabSprite == null)
                    AgendaConstants.archeologyLabSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [ArcheologyLab_PhoenixFacilityDef]").SmallIcon;

                if (AgendaConstants.phoenixFactionSprite == null)
                    AgendaConstants.phoenixFactionSprite = DefCache.GetDef<GeoFactionViewDef>("E_Phoenix_GeoFactionView [Phoenix_GeoPhoenixFactionDef]").FactionIcon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #endregion

        #region Event trigger wiring

        internal static void WireClickEvent(UIFactionDataTrackerElement element, Action onClick)
        {
            GameObject go = element.gameObject;
            if (!go.GetComponent<EventTrigger>())
                go.AddComponent<EventTrigger>();

            EventTrigger trigger = go.GetComponent<EventTrigger>();
            trigger.triggers.Clear();

            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener((_) => onClick());
            trigger.triggers.Add(click);
        }

        #endregion

        #region Tracker element management (add / remove / find / update text)

        internal static UIFactionDataTrackerElement FindTrackedElement<T>(T obj) where T : class
        {
            var elements = AgendaConstants.GetLiveTrackedElements();
            if (elements == null) return null;

            foreach (var e in elements)
            {
                if (e.TrackedObject is T tracked && ReferenceEquals(tracked, obj))
                    return e;
            }
            return null;
        }

        internal static UIFactionDataTrackerElement FindTrackedElementById(GeoCharacter character)
        {
            var elements = AgendaConstants.GetLiveTrackedElements();
            if (elements == null) return null;

            foreach (var e in elements)
            {
                if (e.TrackedObject is GeoCharacter c && c.Id == character.Id)
                    return e;
            }
            return null;
        }

        internal static UIFactionDataTrackerElement AddTrackerElement(
      object trackedObject,
      string text,
      ViewElementDef viewElement)
        {
            UIModuleFactionAgendaTracker tracker = AgendaConstants.factionTracker;
            if (tracker == null
                || AgendaConstants.GetFreeElement == null
                || AgendaConstants.OnAddedElement == null)
            {
                return null;
            }

            var freeElement = (UIFactionDataTrackerElement)AgendaConstants.GetFreeElement.Invoke(
                tracker,
                null);

            if (freeElement == null)
            {
                return null;
            }

            // Stock Dispose() immediately puts the same UI object back into the pool.
            // It can therefore be reused before stock removes its old list entry.
            // A reused active object must no longer be treated as pending removal.
            AgendaConstants.PendingPurge.Remove(freeElement);

            ViewElementDef initViewElement = viewElement;

            // Custom site text/icons are applied by the Init postfix.
            if (trackedObject is GeoSite site && HasCustomSiteTracker(site))
            {
                initViewElement = null;
            }

            freeElement.Init(trackedObject, text, initViewElement, false);
            AgendaConstants.OnAddedElement.Invoke(tracker, new object[] { freeElement });

            return freeElement;
        }

        internal static void RemoveTrackerElement(UIFactionDataTrackerElement element)
        {
            UIModuleFactionAgendaTracker tracker = AgendaConstants.factionTracker;
            if (tracker == null || element == null || AgendaConstants.Dispose == null)
            {
                return;
            }

            // Dispose() logically removes the row for this mod through PendingPurge.
            // Physical removal from vanilla's _currentTrackedElements remains deferred
            // until the next parameterless UpdateData() processes _elementsToRemove.
            AgendaConstants.Dispose.Invoke(tracker, new object[] { element });
        }

        internal static void RefreshTracker()
        {
            UIModuleFactionAgendaTracker tracker = AgendaConstants.factionTracker;
            if (tracker == null || AgendaConstants.UpdateData == null)
            {
                return;
            }

            // The postfix on parameterless UpdateData already applies custom ordering.
            AgendaConstants.UpdateData.Invoke(tracker, null);
        }

        internal static bool TryUpdateCustomSiteElement(UIFactionDataTrackerElement element, GeoSite site, GeoscapeViewContext context, out bool isExpired)
        {
            isExpired = false;

            ApplyCustomSiteTrackerText(element, site, context?.ViewerFaction);

            var pendingAction = GetPendingBaseAction(site);
            if (pendingAction.HasValue && site.ExpiringTimerAt > TimeUnit.Zero)
            {
                TimeUnit remaining = site.ExpiringTimerAt - context.Level.Timing.Now;
                element.UpdateData(remaining, true, null);
                isExpired = remaining <= TimeUnit.Zero;
                return true;
            }

            var incident = GetActiveIncident(site);
            if (incident != null)
            {
                TimeUnit remaining = incident.EndAt - context.Level.Timing.Now;
                element.UpdateData(remaining, true, null);
                isExpired = remaining <= TimeUnit.Zero;
                return true;
            }

            return false;
        }

        internal static void ApplyCustomSiteTrackerText(UIFactionDataTrackerElement element, GeoSite site, GeoFaction viewerFaction)
        {
            if (element == null || site == null) return;
            string text = GetCustomSiteTrackerText(site, viewerFaction ?? site.GeoLevel?.ViewerFaction ?? site.Owner);
            if (!string.IsNullOrEmpty(text))
                element.TrackedName.text = text;
        }

        internal static void ApplyRecruitTrainingTrackerText(UIFactionDataTrackerElement element, GeoCharacter character)
        {
            if (element == null || character == null) return;
            string text = GetRecruitTrainingTrackerText(character);
            if (!string.IsNullOrEmpty(text))
                element.TrackedName.text = text;
        }

        internal static void ReapplyResolvedTrackerTexts(UIModuleFactionAgendaTracker tracker, GeoFaction viewerFaction)
        {
            if (tracker == null) return;
            var elements = AgendaConstants.GetLiveTrackedElements();
            if (elements == null) return;

            foreach (var el in elements)
            {
                if (el.TrackedObject is GeoSite site && HasCustomSiteTracker(site))
                {
                    ApplyCustomSiteTrackerText(el, site, viewerFaction);
                    continue;
                }
                if (el.TrackedObject is GeoCharacter character && TrainingFacilityRework.GetRecruitSession(character) != null)
                {
                    ApplyRecruitTrainingTrackerText(el, character);
                }
            }
        }

        #endregion
    }
}