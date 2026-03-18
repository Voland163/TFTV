using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class AdvanceWarningHavenAttack
    {
        private const string DiagTag = "[Incidents][ComputeWarning]";

        private static string GetSiteName(GeoSite site)
        {
            if (site == null)
            {
                return "UNKNOWN_SITE";
            }

            return string.IsNullOrEmpty(site.LocalizedSiteName) ? site.name : site.LocalizedSiteName;
        }

        internal static void RefreshForCurrentHour(GeoLevelController level)
        {
            try
            {
                if (level == null || !TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                int leadHours = AffinityGeoscapeEffects.GetComputeHavenAttackWarningLeadHours(level);

               // TFTVLogger.Always($"{DiagTag} RefreshForCurrentHour: leadHours={leadHours}");

                if (leadHours > 0)
                {
                    HavenAttackRiskService.RefreshForCurrentHour(level, leadHours);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal enum RiskWindow
        {
            None,
            Hours12,
            Hours8,
            Hours4
        }

        internal static class HavenAttackRiskService
        {
            private static readonly Dictionary<int, RiskWindow> SiteRiskById = new Dictionary<int, RiskWindow>();
            private static int _currentLeadHours;

            public static int CurrentLeadHours => _currentLeadHours;

            public static RiskWindow GetRisk(GeoSite site)
            {
                if (site == null)
                {
                    return RiskWindow.None;
                }

                return SiteRiskById.TryGetValue(site.SiteId, out RiskWindow risk) ? risk : RiskWindow.None;
            }

            public static void RefreshForCurrentHour(GeoLevelController level, int leadHours)
            {
                try
                {
                    _currentLeadHours = leadHours;
                    Rebuild(level.AlienFaction, leadHours, level.Timing.Now.ToString());
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void Rebuild(GeoAlienFaction alienFaction, int leadHours, string currentTime)
            {
                SiteRiskById.Clear();

               

                Dictionary<GeoSite, int> minCounterByHaven = new Dictionary<GeoSite, int>();

                foreach (GeoAlienBase alienBase in alienFaction.Bases)
                {
                    if (alienBase == null)
                    {
                        continue;
                    }

                    int counter = alienBase.HavenAttackCounter;
                    List<GeoSite> havensInRange = alienBase.SitesInRange != null
                        ? alienBase.SitesInRange.Where(site => site != null && site.Type == GeoSiteType.Haven).ToList()
                        : new List<GeoSite>();

                    
                    foreach (GeoSite site in havensInRange)
                    {
                        if (!minCounterByHaven.TryGetValue(site, out int oldCounter) || counter < oldCounter)
                        {
                            minCounterByHaven[site] = counter;
                        }
                    }
                }

                foreach (KeyValuePair<GeoSite, int> kvp in minCounterByHaven)
                {
                    GeoSite havenSite = kvp.Key;

                    if (!alienFaction.CanSiteBeAttacked(havenSite))
                    {
                       
                        continue;
                    }

                    GeoHaven haven = havenSite.GetComponent<GeoHaven>();
                    if (haven == null)
                    {
                 
                        continue;
                    }

                    if (haven.Zones == null || haven.Zones.Count() == 0)
                    {
         
                        continue;
                    }

                    RiskWindow risk = ToRiskWindow(kvp.Value, leadHours);
     

                    if (risk != RiskWindow.None)
                    {
                        SiteRiskById[havenSite.SiteId] = risk;
                    }
                }
            }

            private static RiskWindow ToRiskWindow(int hoursUntilAttackRoll, int leadHours)
            {
                if (hoursUntilAttackRoll > leadHours)
                {
                    return RiskWindow.None;
                }

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

                AlignMarkerTransform(controller, markerRoot.transform);
                EnsureTextVisibility(textMesh);

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

            private static void AlignMarkerTransform(GeoSiteVisualsController controller, Transform marker)
            {
                Transform template = null;
                if (controller.SoldiersAvailableCountText != null)
                {
                    template = controller.SoldiersAvailableCountText.transform;
                }
                else if (controller.SiteScannerProgressText != null)
                {
                    template = controller.SiteScannerProgressText.transform;
                }
                else if (controller.BaseIDText != null)
                {
                    template = controller.BaseIDText.transform;
                }

                marker.localPosition = template != null
                    ? template.localPosition + new Vector3(0f, 0.14f, 0f)
                    : new Vector3(0f, 0.2f, 0f);

                marker.localRotation = template != null ? template.localRotation : Quaternion.identity;

                float scale = template != null ? Mathf.Max(0.14f, Mathf.Abs(template.localScale.x) * 2.4f) : 0.18f;
                marker.localScale = new Vector3(scale, scale, scale);

                FaceMarkerTowardsCamera(marker);
            }

            private static void EnsureTextVisibility(TextMesh textMesh)
            {
                textMesh.characterSize = Mathf.Max(textMesh.characterSize, 0.14f);
                textMesh.fontSize = Mathf.Max(textMesh.fontSize, 90);
                textMesh.fontStyle = FontStyle.Bold;

                MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = true;
                }
            }

            private static void FaceMarkerTowardsCamera(Transform marker)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }

                Vector3 viewDirection = mainCamera.transform.position - marker.position;
                if (viewDirection.sqrMagnitude < 0.0001f)
                {
                    return;
                }

                marker.rotation = Quaternion.LookRotation(viewDirection.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
            }
        }

        [HarmonyPatch(typeof(GeoSiteVisualsController), "Update")]
        internal static class GeoSiteVisualsController_Update_Patch
        {
            public static void Postfix(GeoSiteVisualsController __instance)
            {
                GeoSite site = __instance.Site;
                if (site == null || site.Type != GeoSiteType.Haven)
                {
                    return;
                }

                int leadHours = HavenAttackRiskService.CurrentLeadHours;
                RiskWindow risk = leadHours > 0 ? HavenAttackRiskService.GetRisk(site) : RiskWindow.None;

                HavenAttackRiskVisuals.RefreshMarker(__instance, risk);
            }
        }
    }
}