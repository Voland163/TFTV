using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                if (level == null || !TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
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
                if (site == null || SiteRiskById.Count == 0)
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

            private static readonly Color Hours4Color = new Color(1f, 0.2f, 0.2f, 1f);
            private static readonly Color Hours8Color = new Color(1f, 0.6f, 0.1f, 1f);
            private static readonly Color Hours12Color = new Color(1f, 0.92f, 0.2f, 1f);

            /// <summary>
            /// Per-controller marker bookkeeping. This runs from GeoSiteVisualsController.Update, i.e.
            /// once per site visual per frame, so nothing here may re-discover or re-style the marker
            /// on every call: Transform.Find is a native child-name search, and assigning TextMesh.text
            /// or fontSize regenerates the glyph mesh even when the value is unchanged.
            /// </summary>
            private sealed class MarkerState
            {
                public int SiteId = int.MinValue;
                public bool Probed;
                public Transform Marker;
                public TextMesh Text;
                public RiskWindow AppliedRisk = RiskWindow.None;
                public bool AlignedFromTemplate;
            }

            private static readonly ConditionalWeakTable<GeoSiteVisualsController, MarkerState> MarkerStates =
                new ConditionalWeakTable<GeoSiteVisualsController, MarkerState>();

            private static Camera _cachedCamera;

            public static void RefreshMarker(GeoSiteVisualsController controller, RiskWindow risk)
            {
                MarkerState state = MarkerStates.GetOrCreateValue(controller);

                GeoSite site = controller.Site;
                int siteId = site != null ? site.SiteId : int.MinValue;

                // Site visuals are recycled between sites, so a marker left over from a previous
                // occupant has to be re-discovered when the controller changes hands.
                if (state.SiteId != siteId)
                {
                    state.SiteId = siteId;
                    state.Probed = false;
                }

                if (!state.Probed)
                {
                    state.Probed = true;
                    Transform iconParent = controller.LocationIconParent;
                    AdoptMarker(state, iconParent != null ? iconParent.Find(MarkerRootName) : null);
                }

                if (risk == RiskWindow.None)
                {
                    DestroyMarker(state);
                    return;
                }

                if (state.Marker == null)
                {
                    CreateMarker(controller, state);
                    if (state.Marker == null)
                    {
                        return;
                    }
                }

                // Re-align until a template transform is actually available: on the frame the marker
                // is created the site visual's text objects may not be wired up yet.
                if (!state.AlignedFromTemplate || state.AppliedRisk != risk)
                {
                    state.AlignedFromTemplate = AlignMarkerTransform(controller, state.Marker);
                }

                if (state.AppliedRisk != risk)
                {
                    state.AppliedRisk = risk;
                    EnsureTextVisibility(state.Text);
                    ApplyRiskStyle(state.Text, risk);
                }

                // The camera moves every frame so the billboard genuinely has to update every frame,
                // but only havens that are actually flagged ever get here.
                FaceMarkerTowardsCamera(state.Marker);
            }

            private static void AdoptMarker(MarkerState state, Transform existing)
            {
                if (existing == null)
                {
                    state.Marker = null;
                    state.Text = null;
                    state.AppliedRisk = RiskWindow.None;
                    state.AlignedFromTemplate = false;
                    return;
                }

                state.Marker = existing;
                state.Text = existing.GetComponent<TextMesh>() ?? existing.gameObject.AddComponent<TextMesh>();
                // The adopted marker's styling is unknown, so force a re-apply on the next refresh.
                state.AppliedRisk = RiskWindow.None;
                state.AlignedFromTemplate = false;
            }

            private static void CreateMarker(GeoSiteVisualsController controller, MarkerState state)
            {
                Transform iconParent = controller.LocationIconParent;
                if (iconParent == null)
                {
                    return;
                }

                GameObject markerRoot = new GameObject(MarkerRootName);
                markerRoot.transform.SetParent(iconParent, false);

                TextMesh textMesh = markerRoot.AddComponent<TextMesh>();
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.characterSize = 0.1f;
                textMesh.fontSize = 64;

                state.Marker = markerRoot.transform;
                state.Text = textMesh;
                state.AppliedRisk = RiskWindow.None;
                state.AlignedFromTemplate = false;
            }

            private static void DestroyMarker(MarkerState state)
            {
                if (state.Marker != null)
                {
                    UnityEngine.Object.Destroy(state.Marker.gameObject);
                }

                state.Marker = null;
                state.Text = null;
                state.AppliedRisk = RiskWindow.None;
                state.AlignedFromTemplate = false;
            }

            private static void ApplyRiskStyle(TextMesh textMesh, RiskWindow risk)
            {
                if (textMesh == null)
                {
                    return;
                }

                switch (risk)
                {
                    case RiskWindow.Hours4:
                        textMesh.text = "[4h]";
                        textMesh.color = Hours4Color;
                        break;
                    case RiskWindow.Hours8:
                        textMesh.text = "[8h]";
                        textMesh.color = Hours8Color;
                        break;
                    default:
                        textMesh.text = "[12h]";
                        textMesh.color = Hours12Color;
                        break;
                }
            }

            /// <summary>
            /// Returns true when a template transform was found and used.
            /// </summary>
            private static bool AlignMarkerTransform(GeoSiteVisualsController controller, Transform marker)
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

                return template != null;
            }

            private static void EnsureTextVisibility(TextMesh textMesh)
            {
                if (textMesh == null)
                {
                    return;
                }

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
                // Camera.main is a tagged-object search on this Unity version, so hold on to it.
                if (_cachedCamera == null)
                {
                    _cachedCamera = Camera.main;
                }

                if (_cachedCamera == null)
                {
                    return;
                }

                Vector3 viewDirection = _cachedCamera.transform.position - marker.position;
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