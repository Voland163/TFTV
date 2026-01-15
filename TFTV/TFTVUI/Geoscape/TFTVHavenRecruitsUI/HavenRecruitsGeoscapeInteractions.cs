using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsGeoscapeInteractions
    {
        // ====== DOUBLE-CLICK: SEND CLOSEST AIRCRAFT ======

        private static readonly Dictionary<GeoSite, float> _travelTimeCache = new Dictionary<GeoSite, float>();

        private static float EstimateTravelTime(GeoSite site, GeoVehicle vehicle, out bool hasPath)
        {
            hasPath = false;
            try
            {
                if (site == null || vehicle == null || vehicle.Navigation == null) return float.PositiveInfinity;

                var fromPos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
                var targetPos = site.WorldPosition;

                var path = vehicle.Navigation.FindPath(fromPos, targetPos, out hasPath);
                if (!hasPath || path == null || path.Count < 2) return float.PositiveInfinity;

                double totalDist = 0.0;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    totalDist += GeoMap.Distance(path[i].Pos.WorldPosition, path[i + 1].Pos.WorldPosition).Value;
                }

                var speed = vehicle.Stats?.Speed.Value ?? 0f;
                if (speed <= 0f) return float.PositiveInfinity;

                return (float)(totalDist / speed);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return float.PositiveInfinity;
            }
        }

        private static GeoVehicle FindClosestPhoenixAircraft(GeoSite site, out float time)
        {
            time = float.PositiveInfinity;
            try
            {
                if (site?.GeoLevel?.PhoenixFaction?.Vehicles == null) return null;

                GeoVehicle best = null;
                foreach (var v in site.GeoLevel.PhoenixFaction.Vehicles)
                {
                    bool hasPath;
                    float t = EstimateTravelTime(site, v, out hasPath);
                    if (!hasPath) continue;
                    if (t < time)
                    {
                        time = t;
                        best = v;
                    }
                }
                return best;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Try to issue a move order to <paramref name="vehicle"/> toward <paramref name="site"/>.
        /// Uses several likely methods via reflection so it works across variants.
        /// </summary>
        private static bool TryOrderVehicleToSite(GeoVehicle vehicle, GeoSite site)
        {
            try
            {
                if (vehicle == null || site == null) return false;

                Vector3 src = ((vehicle.CurrentSite != null) ? vehicle.CurrentSite.WorldPosition : vehicle.WorldPosition);
                bool foundPath = false;
                IList<SitePathNode> source = vehicle.Navigation.FindPath(src, site.WorldPosition, out foundPath);

                List<GeoSite> geoSites = new List<GeoSite>();

                geoSites.AddRange(from pn in source
                                  where pn.Site != null && pn.Site != vehicle.CurrentSite
                                  select pn.Site);
                vehicle.StartTravel(geoSites);


                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Entry point you can call from the double-click hook.
        /// </summary>
        public static void SendClosestAircraftToSite(GeoSite site)
        {
            try
            {
                if (site == null) return;

                float time;
                var vehicle = FindClosestPhoenixAircraft(site, out time);
                if (vehicle == null || float.IsPositiveInfinity(time))
                {
                    TFTVLogger.Always("[Recruits] No reachable Phoenix aircraft for this site.");
                    return;
                }

                if (TryOrderVehicleToSite(vehicle, site))
                {
                    TFTVLogger.Always($"[Recruits] Sent '{vehicle.name}' to '{site.Name}' (ETA ~{time:0.0} time units).");
                    // Optional: focus the map on the vehicle or site for feedback
                    // site.GeoLevel.View.ChaseTarget(vehicle, false);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }




        // ---------- NAVIGATION ----------

        internal static void FocusOnSite(GeoSite site)
        {
            try
            {
                if (site == null) return;
                site.GeoLevel.View.ChaseTarget(site, false);
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }
    }
}
