using Base.Core;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV.TFTVBaseRework
{
    internal class BaseConstructionVisuals
    {
        private static readonly Dictionary<int, GeoActorProgressionVisualController> PendingConstructionVisuals = new Dictionary<int, GeoActorProgressionVisualController>();
        private static readonly HashSet<int> PendingVisualCreationLogged = new HashSet<int>();
        private static readonly HashSet<int> PendingVisualMissingLogged = new HashSet<int>();
        private const string PendingVisualLogPrefix = "[BaseActivation.PendingVisuals] ";

        private const string PendingTimerPrefix = "PX_REWORK_PENDING|";
        internal static readonly Dictionary<string, PendingActionInfo> ActivePendingByTimerId = new Dictionary<string, PendingActionInfo>(StringComparer.Ordinal);

        private static readonly AccessTools.FieldRef<GeoscapeEventSystem, Dictionary<string, GeoEventTimer>> TimersRef =
            AccessTools.FieldRefAccess<GeoscapeEventSystem, Dictionary<string, GeoEventTimer>>("_timers");

        internal sealed class PendingActionInfo
        {
            public string TimerId;
            public int SiteId;
            public PendingBaseAction Action;
            public TimeUnit StartAt;
            public TimeUnit EndAt;
        }

        internal static string BuildPendingTimerId(GeoSite site, PendingBaseAction action)
        {
            return string.Join("|", new[]
            {
                "PX_REWORK_PENDING",
                (site?.SiteId ?? -1).ToString(CultureInfo.InvariantCulture),
                ((int)action).ToString(CultureInfo.InvariantCulture),
                DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static bool TryParsePendingTimer(GeoEventTimer timer, out PendingActionInfo active)
        {
            active = null;
            if (timer == null || string.IsNullOrEmpty(timer.ID) || !timer.ID.StartsWith(PendingTimerPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = timer.ID.Split('|');
            if (parts.Length < 4)
            {
                return false;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int siteId))
            {
                return false;
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int actionValue))
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(PendingBaseAction), actionValue))
            {
                return false;
            }

            active = new PendingActionInfo
            {
                TimerId = timer.ID,
                SiteId = siteId,
                Action = (PendingBaseAction)actionValue,
                StartAt = timer.StartAt,
                EndAt = timer.EndAt
            };
            return true;
        }

        private static void RehydratePendingActions(GeoscapeEventSystem eventSystem, GeoLevelController level)
        {
            if (eventSystem == null)
            {
                return;
            }

            Dictionary<string, GeoEventTimer> timers = TimersRef(eventSystem);

          //  TFTVLogger.Always($"Rehydrating pending actions. Timers count: {(timers != null ? timers.Count.ToString() : "null")}");

            if (timers == null || timers.Count == 0)
            {
                return;
            }

            foreach (GeoEventTimer timer in timers.Values)
            {
                if (timer == null)
                {
                    continue;
                }

                if (!ActivePendingByTimerId.ContainsKey(timer.ID) && TryParsePendingTimer(timer, out PendingActionInfo active))
                {
                    ActivePendingByTimerId[active.TimerId] = active;
                    InvalidatePendingVisuals();

                    GeoSite site = level?.Map?.AllSites?.FirstOrDefault(s => s.SiteId == active.SiteId);
                    if (site != null && site.ExpiringTimerAt != timer.EndAt)
                    {
                        site.ExpiringTimerAt = timer.EndAt;
                    }
                }
            }
        }

        // Reused so the per-frame tick does not allocate.
        private static readonly List<string> _toCompleteBuffer = new List<string>();

        private static void TickPendingActions(GeoscapeEventSystem eventSystem)
        {
            GeoLevelController level = ResolveLevel(eventSystem);
            if (eventSystem == null
                || level == null
                || level.Map == null
                || level.Map.AllSites == null
                || level.PhoenixFaction == null
                || level.Timing == null)
            {
                return;
            }

            RehydratePendingActions(eventSystem, level);

            if (ActivePendingByTimerId.Count == 0)
            {
                return;
            }

            List<string> toComplete = _toCompleteBuffer;
            toComplete.Clear();

            foreach (KeyValuePair<string, PendingActionInfo> kv in ActivePendingByTimerId)
            {
                if (level.Timing.Now >= kv.Value.EndAt)
                {
                    toComplete.Add(kv.Key);
                }
            }

            if (toComplete.Count == 0)
            {
                return;
            }

            foreach (string timerId in toComplete)
            {
                CompletePendingActionFromTimer(level, timerId);
            }

            // Marked afterwards so the rescan is guaranteed to see the completed state, even though
            // the completion path already refreshes on its own.
            InvalidatePendingVisuals();
        }

        private static bool CompletePendingActionFromTimer(GeoLevelController level, string timerId)
        {
            if (level == null
                || string.IsNullOrEmpty(timerId)
                || level.Map == null
                || level.Map.AllSites == null)
            {
                return false;
            }

            GeoscapeEventSystem eventSystem = level.EventSystem;
            if (eventSystem == null || level.PhoenixFaction == null)
            {
                return false;
            }

            if (!ActivePendingByTimerId.TryGetValue(timerId, out PendingActionInfo active) || active == null)
            {
                return false;
            }

            GeoSite site = level.Map.AllSites.FirstOrDefault(s => s != null && s.SiteId == active.SiteId);
            if (site == null)
            {
                if (eventSystem.GetTimerById(timerId) != null)
                {
                    eventSystem.RemoveTimer(timerId);
                }

                ActivePendingByTimerId.Remove(timerId);
                return true;
            }

            PhoenixBaseVisitFlow.CompletePendingAction(site, level.PhoenixFaction, active.Action);

            bool completionSucceeded = !PhoenixBaseVisitFlow.HasPendingActionPublic(site)
                && site.ExpiringTimerAt == TimeUnit.Zero;

            if (!completionSucceeded)
            {
                return false;
            }

            if (eventSystem.GetTimerById(timerId) != null)
            {
                eventSystem.RemoveTimer(timerId);
            }

            ActivePendingByTimerId.Remove(timerId);
            return true;
        }

        /* [HarmonyPatch(typeof(UIModuleSiteContextualMenu), "SetMenuItems")]
         internal static class UIModuleSiteContextualMenu_SetMenuItems_patch
         {
             private static void Prefix(ref List<GeoAbility> rawAbilities)
             {
                 if (rawAbilities == null)
                 {
                     return;
                 }

                 if (!BaseReworkUtils.BaseReworkEnabled)
                 {
                     rawAbilities.RemoveAll(a => a is ActivateBaseAbility);
                 }
             }
         }*/

        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnLevelStart")]
        internal static class GeoscapeEventSystem_OnLevelStart_patch
        {
            public static void Postfix(GeoscapeEventSystem __instance)
            {
                try
                {
                    GeoLevelController level = __instance?.gameObject?.GetComponent<GeoLevelController>();
                    RehydratePendingActions(__instance, level);
                    RefreshPendingConstructionVisuals(level);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(GeoscapeEventSystem), "Update")]
        internal static class GeoscapeEventSystem_Update_patch
        {
            public static void Postfix(GeoscapeEventSystem __instance)
            {
                try
                {
                    TickPendingActions(__instance);

                    if (!ShouldRescanVisuals())
                    {
                        return;
                    }

                    RefreshPendingConstructionVisuals(ResolveLevel(__instance));
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        /// <summary>
        /// GeoscapeEventSystem is a MonoBehaviour, so its Update runs every frame. Scanning every
        /// site on the map at that rate is wasted work: pending actions only start and finish on
        /// discrete events, and the progression visuals animate themselves from their own Update.
        /// The scan is therefore invalidated explicitly and otherwise throttled to a safety net.
        /// </summary>
        private const int VisualRescanFrameInterval = 30;

        private static int _lastVisualScanFrame = int.MinValue;
        private static bool _visualsDirty = true;

        internal static void InvalidatePendingVisuals()
        {
            _visualsDirty = true;
        }

        private static bool ShouldRescanVisuals()
        {
            return _visualsDirty
                || UnityEngine.Time.frameCount - _lastVisualScanFrame >= VisualRescanFrameInterval;
        }

        // GeoscapeEventSystem and GeoLevelController live on the same GameObject, so the lookup only
        // has to happen once per event system rather than on every frame.
        private static GeoscapeEventSystem _cachedLevelOwner;
        private static GeoLevelController _cachedLevel;

        private static GeoLevelController ResolveLevel(GeoscapeEventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return null;
            }

            if (!ReferenceEquals(_cachedLevelOwner, eventSystem) || _cachedLevel == null)
            {
                _cachedLevelOwner = eventSystem;
                _cachedLevel = eventSystem.gameObject != null
                    ? eventSystem.gameObject.GetComponent<GeoLevelController>()
                    : null;
            }

            return _cachedLevel;
        }

        // Reused across scans so the refresh does not allocate on the geoscape.
        private static readonly HashSet<int> _activeSiteIdsBuffer = new HashSet<int>();
        private static readonly List<int> _staleSiteIdsBuffer = new List<int>();

        // Progression ranges already pushed into each visual. SetProgression touches renderer
        // materials, so it must only be called when the range actually changes.
        private static readonly Dictionary<int, KeyValuePair<TimeUnit, TimeUnit>> AppliedProgression =
            new Dictionary<int, KeyValuePair<TimeUnit, TimeUnit>>();

        public static void RefreshPendingConstructionVisuals(GeoLevelController level)
        {
            if (level?.Map?.AllSites == null)
            {
                return;
            }

            // Also resets the throttle for callers that refresh directly after a player action.
            _visualsDirty = false;
            _lastVisualScanFrame = UnityEngine.Time.frameCount;

            HashSet<int> activeSiteIds = _activeSiteIdsBuffer;
            activeSiteIds.Clear();

            foreach (GeoSite site in level.Map.AllSites)
            {
                if (site == null || !PhoenixBaseVisitFlow.HasPendingActionPublic(site))
                {
                    continue;
                }

                if (site.ExpiringTimerAt == TimeUnit.Zero || site.ExpiringTimerAt <= level.Timing.Now)
                {
                    if (!PendingVisualMissingLogged.Contains(site.SiteId))
                    {
                       // TFTVLogger.Always(PendingVisualLogPrefix +
                         //   $"Skip site {site.SiteId}: ExpiringTimerAt={site.ExpiringTimerAt}, Now={level.Timing.Now}");
                        PendingVisualMissingLogged.Add(site.SiteId);
                    }
                    continue;
                }

                activeSiteIds.Add(site.SiteId);
                PendingVisualMissingLogged.Remove(site.SiteId);

                if (!PendingConstructionVisuals.TryGetValue(site.SiteId, out GeoActorProgressionVisualController controller) || controller == null)
                {
                    GeoVehicle vehicle = ResolveVehicleForSite(site, level);
                    GeoActorProgressionVisualController prefab = vehicle?.VehicleDef?.ExplorationVisualsPrefab
                        ?? level?.PhoenixFaction?.Vehicles?.FirstOrDefault()?.VehicleDef?.ExplorationVisualsPrefab;

                    if (prefab == null || site.Surface == null)
                    {
                        if (!PendingVisualMissingLogged.Contains(site.SiteId))
                        {
                            TFTVLogger.Always(PendingVisualLogPrefix +
                            $"Missing prefab/surface for site {site.SiteId}. Vehicle={(vehicle != null ? vehicle.VehicleID.ToString() : "null")}, Surface={(site.Surface != null)}");
                            PendingVisualMissingLogged.Add(site.SiteId);
                        }
                        continue;
                    }

                    controller = UnityEngine.Object.Instantiate(prefab, site.Surface);
                    if (controller == null)
                    {
                        if (!PendingVisualMissingLogged.Contains(site.SiteId))
                        {
                            TFTVLogger.Always(PendingVisualLogPrefix + $"Instantiate failed for site {site.SiteId}.");
                            PendingVisualMissingLogged.Add(site.SiteId);
                        }
                        continue;
                    }

                    PendingConstructionVisuals[site.SiteId] = controller;

                    if (!PendingVisualCreationLogged.Contains(site.SiteId))
                    {
                        TFTVLogger.Always(PendingVisualLogPrefix +
                            $"Created visual for site {site.SiteId}. Prefab={prefab.name}, Vehicle={(vehicle != null ? vehicle.VehicleID.ToString() : "fallback")}.");
                        PendingVisualCreationLogged.Add(site.SiteId);
                    }
                }

                float durationHours = PhoenixBaseVisitFlow.GetPendingDurationHours(site);
                TimeUnit startAt = site.ExpiringTimerAt - TimeUnit.FromHours(durationHours);

                // SetProgression writes to the renderer's material, and the controller animates
                // itself from its own Update, so only push the range when it has actually changed.
                if (!AppliedProgression.TryGetValue(site.SiteId, out KeyValuePair<TimeUnit, TimeUnit> applied)
                    || applied.Key != startAt
                    || applied.Value != site.ExpiringTimerAt)
                {
                    controller.SetProgression(startAt, site.ExpiringTimerAt, level.Timing);
                    AppliedProgression[site.SiteId] = new KeyValuePair<TimeUnit, TimeUnit>(startAt, site.ExpiringTimerAt);
                }

                if (!controller.gameObject.activeSelf)
                {
                    controller.gameObject.SetActive(true);
                }
            }

            List<int> staleSiteIds = _staleSiteIdsBuffer;
            staleSiteIds.Clear();

            foreach (int id in PendingConstructionVisuals.Keys)
            {
                if (!activeSiteIds.Contains(id))
                {
                    staleSiteIds.Add(id);
                }
            }

            foreach (int staleId in staleSiteIds)
            {
                GeoActorProgressionVisualController controller = PendingConstructionVisuals[staleId];
                PendingConstructionVisuals.Remove(staleId);
                PendingVisualCreationLogged.Remove(staleId);
                PendingVisualMissingLogged.Remove(staleId);
                AppliedProgression.Remove(staleId);

                if (controller != null)
                {
                    UnityEngine.Object.Destroy(controller.gameObject);
                }
            }
        }

        private static GeoVehicle ResolveVehicleForSite(GeoSite site, GeoLevelController level)
        {
            if (site == null)
            {
                return null;
            }

            IEnumerable<GeoVehicle> vehicles = site.GetPlayerVehiclesOnSite();
            if (vehicles == null || !vehicles.Any())
            {
                vehicles = site.Vehicles?.Where(v => v?.Owner == level?.PhoenixFaction);
            }

            return vehicles?
                .Where(v => v != null)
                .OrderByDescending(v => v.MaxCharacterSpace)
                .FirstOrDefault();
        }

    }
}
