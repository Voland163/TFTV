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
            foreach (GeoEventTimer timer in timers.Values)
            {
                if (!ActivePendingByTimerId.ContainsKey(timer.ID) && TryParsePendingTimer(timer, out PendingActionInfo active))
                {
                    ActivePendingByTimerId[active.TimerId] = active;

                    GeoSite site = level?.Map?.AllSites?.FirstOrDefault(s => s.SiteId == active.SiteId);
                    if (site != null && site.ExpiringTimerAt != timer.EndAt)
                    {
                        site.ExpiringTimerAt = timer.EndAt;
                    }
                }
            }
        }

        private static void TickPendingActions(GeoscapeEventSystem eventSystem)
        {
            GeoLevelController level = eventSystem?.gameObject?.GetComponent<GeoLevelController>();
            if (level == null)
            {
                return;
            }

            RehydratePendingActions(eventSystem, level);

            List<string> toComplete = new List<string>();
            foreach (KeyValuePair<string, PendingActionInfo> kv in ActivePendingByTimerId)
            {
                GeoEventTimer timer = eventSystem.GetTimerById(kv.Key);
                if (timer == null || level.Timing.Now >= kv.Value.EndAt)
                {
                    toComplete.Add(kv.Key);
                }
            }

            foreach (string timerId in toComplete)
            {
                CompletePendingActionFromTimer(level, timerId);
            }
        }

        private static void CompletePendingActionFromTimer(GeoLevelController level, string timerId)
        {
            if (!ActivePendingByTimerId.TryGetValue(timerId, out PendingActionInfo active))
            {
                return;
            }

            GeoSite site = level.Map.AllSites.FirstOrDefault(s => s.SiteId == active.SiteId);
            GeoPhoenixFaction faction = level.PhoenixFaction;
            if (site != null && faction != null)
            {
                PhoenixBaseVisitFlow.CompletePendingAction(site, faction, active.Action);
            }

            if (level.EventSystem.GetTimerById(timerId) != null)
            {
                level.EventSystem.RemoveTimer(timerId);
            }

            ActivePendingByTimerId.Remove(timerId);
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
                    GeoLevelController level = __instance?.gameObject?.GetComponent<GeoLevelController>();
                    RefreshPendingConstructionVisuals(level);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        public static void RefreshPendingConstructionVisuals(GeoLevelController level)
        {
            if (level?.Map?.AllSites == null)
            {
                return;
            }

            HashSet<int> activeSiteIds = new HashSet<int>();
            foreach (GeoSite site in level.Map.AllSites.Where(s => s != null && PhoenixBaseVisitFlow.HasPendingActionPublic(s)))
            {
                if (site.ExpiringTimerAt == TimeUnit.Zero || site.ExpiringTimerAt <= level.Timing.Now)
                {
                    if (!PendingVisualMissingLogged.Contains(site.SiteId))
                    {
                        TFTVLogger.Always(PendingVisualLogPrefix +
                            $"Skip site {site.SiteId}: ExpiringTimerAt={site.ExpiringTimerAt}, Now={level.Timing.Now}");
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
                controller.SetProgression(startAt, site.ExpiringTimerAt, level.Timing);
                controller.gameObject.SetActive(true);
            }

            foreach (int staleId in PendingConstructionVisuals.Keys.Where(id => !activeSiteIds.Contains(id)).ToList())
            {
                GeoActorProgressionVisualController controller = PendingConstructionVisuals[staleId];
                PendingConstructionVisuals.Remove(staleId);
                PendingVisualCreationLogged.Remove(staleId);
                PendingVisualMissingLogged.Remove(staleId);

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
