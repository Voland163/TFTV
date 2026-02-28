using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class AdvanceWarningHavenAttack
    {
        internal enum RiskWindow
        {
            None,
            Hours12,
            Hours8,
            Hours4
        }

        internal static class HavenAttackRiskService
        {
            private const float RebuildIntervalSeconds = 1f;

            private static readonly Dictionary<int, RiskWindow> SiteRiskById = new Dictionary<int, RiskWindow>();
            private static float _lastRebuildAtRealtime = -9999f;

            public static RiskWindow GetRisk(GeoSite site)
            {
                if (site == null)
                {
                    return RiskWindow.None;
                }

                return SiteRiskById.TryGetValue(site.SiteId, out RiskWindow risk) ? risk : RiskWindow.None;
            }

            public static void RebuildIfNeeded(GeoSite anySite)
            {
                if (anySite?.GeoLevel?.AlienFaction == null)
                {
                    return;
                }

                if (Time.realtimeSinceStartup - _lastRebuildAtRealtime < RebuildIntervalSeconds)
                {
                    return;
                }

                _lastRebuildAtRealtime = Time.realtimeSinceStartup;
                Rebuild(anySite.GeoLevel.AlienFaction);
            }

            private static void Rebuild(GeoAlienFaction alienFaction)
            {
                SiteRiskById.Clear();

                // Track the earliest base timer for each haven.
                var minCounterByHaven = new Dictionary<GeoSite, int>();

                foreach (GeoAlienBase alienBase in alienFaction.Bases)
                {
                    if (alienBase == null)
                    {
                        continue;
                    }

                    int counter = alienBase.HavenAttackCounter;
                    foreach (GeoSite site in alienBase.SitesInRange)
                    {
                        if (site == null || site.Type != GeoSiteType.Haven)
                        {
                            continue;
                        }

                        if (!minCounterByHaven.TryGetValue(site, out int oldCounter) || counter < oldCounter)
                        {
                            minCounterByHaven[site] = counter;
                        }
                    }
                }

                foreach (KeyValuePair<GeoSite, int> kvp in minCounterByHaven)
                {
                    GeoSite havenSite = kvp.Key;

                    // Mirror game-side attackability checks for a meaningful "at risk" list.
                    if (!alienFaction.CanSiteBeAttacked(havenSite))
                    {
                        continue;
                    }

                    GeoHaven haven = havenSite.GetComponent<GeoHaven>();
                    if (haven == null || haven.Zones == null || haven.Zones.Count() == 0)
                    {
                        continue;
                    }

                    RiskWindow risk = ToRiskWindow(kvp.Value);
                    if (risk != RiskWindow.None)
                    {
                        SiteRiskById[havenSite.SiteId] = risk;
                    }
                }
            }

            private static RiskWindow ToRiskWindow(int hoursUntilAttackRoll)
            {
                if (hoursUntilAttackRoll <= 4)
                {
                    return RiskWindow.Hours4;
                }

                if (hoursUntilAttackRoll <= 8)
                {
                    return RiskWindow.Hours8;
                }

                if (hoursUntilAttackRoll <= 12)
                {
                    return RiskWindow.Hours12;
                }

                return RiskWindow.None;
            }
        }

        internal static class HavenAttackRiskVisuals
        {
            private const string MarkerRootName = "HavenAttackRiskMarker";

            public static void RefreshMarker(GeoSiteVisualsController controller, RiskWindow risk)
            {
                Transform iconParent = controller.LocationIconParent;
                if (iconParent == null)
                {
                    return;
                }

                Transform existing = iconParent.Find(MarkerRootName);
                if (risk == RiskWindow.None)
                {
                    if (existing != null)
                    {
                        UnityEngine.Object.Destroy(existing.gameObject);
                    }

                    return;
                }

                GameObject markerRoot;
                TextMesh textMesh;
                if (existing == null)
                {
                    markerRoot = new GameObject(MarkerRootName);
                    markerRoot.transform.SetParent(iconParent, false);
                    markerRoot.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                    markerRoot.transform.localScale = Vector3.one * 0.1f;
                    markerRoot.transform.localRotation = Quaternion.identity;

                    textMesh = markerRoot.AddComponent<TextMesh>();
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.characterSize = 0.1f;
                    textMesh.fontSize = 64;
                }
                else
                {
                    markerRoot = existing.gameObject;
                    textMesh = markerRoot.GetComponent<TextMesh>() ?? markerRoot.AddComponent<TextMesh>();
                }

                switch (risk)
                {
                    case RiskWindow.Hours4:
                        textMesh.text = "[4h]";
                        textMesh.color = new Color(1f, 0.2f, 0.2f, 1f);
                        break;
                    case RiskWindow.Hours8:
                        textMesh.text = "[8h]";
                        textMesh.color = new Color(1f, 0.6f, 0.1f, 1f);
                        break;
                    default:
                        textMesh.text = "[12h]";
                        textMesh.color = new Color(1f, 0.92f, 0.2f, 1f);
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(GeoSiteVisualsController), "Update")]
        internal static class GeoSiteVisualsController_Update_Patch
        {
            private static void Postfix(GeoSiteVisualsController __instance)
            {
                GeoSite site = __instance.Site;
                if (site == null || site.Type != GeoSiteType.Haven)
                {
                    return;
                }

                HavenAttackRiskService.RebuildIfNeeded(site);
                RiskWindow risk = HavenAttackRiskService.GetRisk(site);
                HavenAttackRiskVisuals.RefreshMarker(__instance, risk);
            }
        }

    }
}
