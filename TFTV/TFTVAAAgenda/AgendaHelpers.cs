using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVBaseRework;
using TFTV.TFTVIncidents;
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
        private const string GenericSiteViewElementGuid = "0481b9e2-947c-fbb2-3d96-8f769e1e05cd";
        private const string CrabmanViewElementGuid = "8188f3a3-befd-e463-f345-4af1815cd848";

        #region ViewElementDef helpers

        internal static ViewElementDef GetGenericSiteViewElement()
        {
            return DefCache.GetDef<ViewElementDef>(GenericSiteViewElementGuid);
        }

        internal static ViewElementDef GetCrabmanViewElement()
        {
            return (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef(CrabmanViewElementGuid);
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
            return string.IsNullOrEmpty(spec)
                ? $"{AgendaConstants.actionTrainingOperative} {character.DisplayName}"
                : $"{AgendaConstants.actionTrainingOperative} {character.DisplayName} ({spec})";
        }

        internal static string BuildVehicleText(GeoVehicle vehicle, bool travelling)
        {
            if (travelling)
            {
                string siteName = GetSiteName(vehicle.FinalDestination, vehicle.Owner);
                return $"{vehicle.Name} {AgendaConstants.actionTraveling} {siteName}";
            }
            else
            {
                string siteName = GetSiteName(vehicle.CurrentSite, vehicle.Owner);
                return $"{vehicle.Name} {AgendaConstants.actionExploring} {siteName}";
            }
        }

        internal static string AppendTime(float hours)
        {
            return $"   ~ {HoursToText(hours)}";
        }

        internal static string HoursToText(float hours)
        {
            TimeUnit timeUnit = TimeUnit.FromHours(hours);
            var formatter = new TimeRemainingFormatterDef
            {
                DaysText = new LocalizedTextBind("{0}d", true),
                HoursText = new LocalizedTextBind("{0}h", true)
            };
            return UIUtil.FormatTimeRemaining(timeUnit, formatter);
        }

        #endregion

        #region Time calculations

        internal static TimeUnit GetRecruitTrainingRemainingTime(GeoCharacter character, GeoLevelController level)
        {
            if (character == null || level == null) return TimeUnit.Zero;
            var session = TrainingFacilityRework.GetRecruitSession(character);
            if (session == null) return TimeUnit.Zero;

            TimeUnit endAt = TimeUnit.FromHours((session.StartDay + session.DurationDays) * 24f);
            TimeUnit remaining = endAt - level.Timing.Now;
            return remaining <= TimeUnit.Zero ? TimeUnit.Zero : remaining;
        }

        internal static float GetExplorationTime(GeoVehicle vehicle, float fallbackHours)
        {
            try
            {
                if (vehicle == null) return fallbackHours;

                object updateable = AgendaConstants.ExplorationUpdateableField?.GetValue(vehicle);
                if (updateable == null || AgendaConstants.ExplorationUpdateableNextUpdateProperty == null)
                    return fallbackHours;

                NextUpdate endTime = (NextUpdate)AgendaConstants.ExplorationUpdateableNextUpdateProperty.GetValue(updateable);
                return (float)-(vehicle.Timing.Now - endTime.NextTime).TimeSpan.TotalHours;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return fallbackHours;
            }
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

        internal static bool GetTravelTime(GeoVehicle vehicle, out float travelTime, GeoSite target = null)
        {
            travelTime = 0f;
            if (vehicle?.Navigation == null) return false;

            GeoSite dest = target ?? vehicle.FinalDestination;
            if (dest == null)
            {
                if (target == null) _travelTimeCache.Remove(vehicle.VehicleID);
                return false;
            }

            float speed = vehicle.Stats?.Speed.Value ?? 0f;
            if (speed <= 0f)
            {
                if (target == null) _travelTimeCache.Remove(vehicle.VehicleID);
                return false;
            }

            Vector3 targetPos = dest.WorldPosition;
            bool cacheResult = target == null;

            if (cacheResult)
            {
                if (!vehicle.Travelling)
                {
                    _travelTimeCache.Remove(vehicle.VehicleID);
                }
                else if (_travelTimeCache.TryGetValue(vehicle.VehicleID, out var entry) && entry.Matches(dest, targetPos, speed))
                {
                    var timing = vehicle.GeoLevel?.Timing;
                    if (entry.HasCalculationTime && timing != null)
                    {
                        float elapsed = (float)(timing.Now - entry.CalculatedAt).TimeSpan.TotalHours;
                        float remaining = Mathf.Max(0f, entry.CachedHours - elapsed);
                        if (remaining > 0f)
                        {
                            entry.CachedHours = remaining;
                            entry.CalculatedAt = timing.Now;
                            travelTime = remaining;
                            return true;
                        }
                        _travelTimeCache.Remove(vehicle.VehicleID);
                    }
                    else
                    {
                        if (timing != null) { entry.HasCalculationTime = true; entry.CalculatedAt = timing.Now; }
                        travelTime = entry.CachedHours;
                        return true;
                    }
                }
            }

            Vector3 currentPos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
            var path = vehicle.Navigation.FindPath(currentPos, targetPos, out bool hasPath);
            if (!hasPath || path.Count < 2)
            {
                if (cacheResult) _travelTimeCache.Remove(vehicle.VehicleID);
                return false;
            }

            float distance = 0f;
            for (int i = 0, len = path.Count - 1; i < len;)
                distance += GeoMap.Distance(path[i].Pos.WorldPosition, path[++i].Pos.WorldPosition).Value;

            travelTime = distance / speed;

            if (cacheResult && vehicle.Travelling)
            {
                var timing = vehicle.GeoLevel?.Timing;
                _travelTimeCache[vehicle.VehicleID] = new TravelTimeCacheEntry
                {
                    Destination = dest,
                    DestinationPosition = targetPos,
                    CachedHours = travelTime,
                    Speed = speed,
                    HasCalculationTime = timing != null,
                    CalculatedAt = timing?.Now ?? TimeUnit.Zero
                };
            }

            return true;
        }

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
            var elements = AgendaConstants.GetTrackedElements();
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
            var elements = AgendaConstants.GetTrackedElements();
            if (elements == null) return null;

            foreach (var e in elements)
            {
                if (e.TrackedObject is GeoCharacter c && c.Id == character.Id)
                    return e;
            }
            return null;
        }

        internal static UIFactionDataTrackerElement AddTrackerElement(object trackedObject, string text, ViewElementDef viewElement)
        {
            var tracker = AgendaConstants.factionTracker;
            if (tracker == null) return null;

            var freeElement = (UIFactionDataTrackerElement)AgendaConstants.GetFreeElement.Invoke(tracker, null);

            ViewElementDef initViewElement = viewElement;
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
            var tracker = AgendaConstants.factionTracker;
            if (tracker == null || element == null) return;
            AgendaConstants.Dispose.Invoke(tracker, new object[] { element });
        }

        internal static void RefreshTracker()
        {
            var tracker = AgendaConstants.factionTracker;
            if (tracker == null) return;
            AgendaConstants.UpdateData.Invoke(tracker, null);
            AgendaConstants.OrderElements.Invoke(tracker, null);
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
            var elements = (List<UIFactionDataTrackerElement>)AgendaConstants.CurrentTrackedElementsField.GetValue(tracker);

            foreach (var el in elements)
            {
                if (el == null) continue;
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