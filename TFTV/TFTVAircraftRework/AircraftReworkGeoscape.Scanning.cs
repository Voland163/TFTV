using Base;
using Base.Core;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TFTV.TFTVBaseRework;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.TFTVBaseRework.BaseActivation;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal class Scanning
        {
            [HarmonyPatch(typeof(GeoPhoenixFaction), "OnVehicleAdded")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class GeoPhoenixFaction_OnVehicleAdded_Patch
            {
                static void Postfix(GeoPhoenixFaction __instance, GeoVehicle vehicle)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        CheckAircraftScannerAbility(vehicle);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(GeoAbility), "GetAbilityFaction")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class GeoAbility_GetTargetDisabledState_Patch
            {
                static void Postfix(GeoAbility __instance, ref GeoFaction __result)
                {
                    try
                    {
                        if (!AircraftReworkOn || __instance.GeoscapeAbilityDef != _scanAbilityDef && __instance.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                        {
                            return;
                        }

                        GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                        __result = geoVehicle.Owner;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(GeoAbilityView), "CanActivate")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class GeoAbilityView_CanActivate_Patch
            {
                static void Postfix(GeoAbilityView __instance, GeoAbilityTarget target, ref bool __result)
                {
                    try
                    {
                        if (!AircraftReworkOn || __instance.GeoAbility.GeoscapeAbilityDef != _scanAbilityDef && __instance.GeoAbility.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                        {
                            return;
                        }

                        GeoVehicle geoVehicle = __instance.GeoAbility.Actor as GeoVehicle;

                        if (target.Actor is GeoSite geoSite && geoVehicle.CurrentSite == geoSite && geoVehicle.CanRedirect && __result)
                        {

                        }
                        else
                        {
                            __result = false;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            [HarmonyPatch(typeof(UIModuleActionsBar), "UpdateAbilityInformation")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class UIModuleActionsBar_UpdateAbilityInformation_Patch
            {
                static void Postfix(UIModuleActionsBar __instance, GeoAbility geoAbility, bool showAbilityState)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        if (geoAbility.GeoscapeAbilityDef != _scanAbilityDef && geoAbility.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                        {
                            return;
                        }

                        __instance.MainDescriptionController.ActionHeaderChargesText.gameObject.SetActive(value: false);
                        __instance.MainDescriptionController.CallToActionButton.gameObject.SetActive(value: false);
                        //__instance.MainDescriptionController.SuppliesText.gameObject.SetActive(false);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(ScanAbility), "GetCharges")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class ScanAbility_GetCharges_Patch
            {
                static void Postfix(ScanAbility __instance, ref int __result)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        __result = 1;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(ScanAbility), "GetDisabledStateInternal")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class ScanAbility_GetDisabledStateInternal_Patch
            {
                static void Postfix(ScanAbility __instance, ref GeoAbilityDisabledState __result)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                        if (geoVehicle != null && !geoVehicle.CanRedirect)
                        {
                            __result = GeoAbilityDisabledState.NoScanChargesLeft;
                        }

                        GeoSite geoSite = geoVehicle?.CurrentSite;

                        if (geoSite != null)
                        {
                            int geoSiteId = geoSite.SiteId;
                            if (AircraftScanningSites != null && AircraftScanningSites.ContainsKey(geoSiteId))
                            {
                                __result = GeoAbilityDisabledState.NoScanChargesLeft;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /* [HarmonyPatch(typeof(ScanAbility), "GetTargetDisabledStateInternal")]
             [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
             public static class ScanAbility_GetTargetDisabledStateInternal_Patch
             {
                 static void Postfix(ScanAbility __instance, GeoAbilityTarget target, GeoAbilityTargetDisabledState __result)
                 {
                     try
                     {
                         if (!AircraftReworkOn)
                         {
                             return;
                         }

                         GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                         TFTVLogger.Always($"{geoVehicle.Name} target: {target.Actor?.name} {__result} ");

                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }
             }*/

            /*  [HarmonyPatch(typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer))]
              [HarmonyPatch("Init")]
              public static class GeoscapeRegionDrawer_Init_Patch
              {
                  static void Postfix(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer __instance)
                  {
                      try
                      {
                          // Access the private fields using reflection
                          var rendererField = typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer).GetField("_renderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                          var propertyBlockField = typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer).GetField("_propertyBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                          if (rendererField != null && propertyBlockField != null)
                          {
                              var renderer = (MeshRenderer)rendererField.GetValue(__instance);
                              var propertyBlock = (MaterialPropertyBlock)propertyBlockField.GetValue(__instance);

                              // Clear any existing textures or materials
                              propertyBlock.Clear();

                              // Set a new color with transparency
                              Color color = new Color(0f, 1f, 0f, 0.45f); // Green with 25% opacity
                              propertyBlock.SetColor("_Color", color);

                              // Ensure the shader supports transparency
                              Material material = renderer.material;
                              material.SetOverrideTag("RenderType", "Transparent");
                              material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                              material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                              material.SetInt("_ZWrite", 0);
                              material.DisableKeyword("_ALPHATEST_ON");
                              material.EnableKeyword("_ALPHABLEND_ON");
                              material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                              material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                              // Apply the property block to the renderer
                              renderer.SetPropertyBlock(propertyBlock);
                          }
                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                          throw;
                      }
                  }
              }*/





            public static void CheckAircraftScannerAbility(GeoVehicle geoVehicle)
            {
                try
                {

                    if (geoVehicle.Modules != null && geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule) && geoVehicle.GetAbility<ScanAbility>() == null)
                    {
                        AddAbilityToGeoVehicle(geoVehicle, _scanAbilityDef);
                    }
                    else if (geoVehicle.Modules != null && !geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule) && geoVehicle.GetAbility<ScanAbility>() != null)
                    {
                        RemoveAbilityFromVehicle(geoVehicle, _scanAbilityDef);
                    }

                    if (geoVehicle.Modules != null && geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) && geoVehicle.GetAbility<ScanAbility>() == null)
                    {
                        AddAbilityToGeoVehicle(geoVehicle, _thunderbirdScanAbilityDef);
                    }
                    else if (geoVehicle.Modules != null && !geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) && geoVehicle.GetAbility<ScanAbility>() != null)
                    {
                        RemoveAbilityFromVehicle(geoVehicle, _thunderbirdScanAbilityDef);
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static float GetThunderbirdScannerRange()
            {
                try
                {
                    Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                    if (phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef"))
                    {
                        return 3000;
                    }
                    else
                    {
                        return 2000;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void AdjustArgusArrayRange()
            {
                try
                {


                    GeoScannerDef geoScannerDef = (GeoScannerDef)_thunderbirdScanAbilityDef.ScanActorDef.Components.FirstOrDefault(c => c is GeoScannerDef);

                    geoScannerDef.MaximumRange.Value = GetThunderbirdScannerRange();



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }



            [HarmonyPatch(typeof(ScanAbility), "ActivateInternal")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class ScanAbility_ActivateInternal_Patch
            {
                static void Prefix(ScanAbility __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        AdjustArgusArrayRange();

                        GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                        GeoSite geoSite = geoVehicle?.CurrentSite;

                        if (geoSite == null)
                        {
                            TFTVLogger.Always($"[ScanAbility.ActivateInternal] geoSite is null for {geoVehicle?.Name}!");
                            return;
                        }

                        if (AircraftScanningSites == null)
                        {
                            AircraftScanningSites = new Dictionary<int, List<int>>();
                        }


                        if (!AircraftScanningSites.ContainsKey(geoSite.SiteId))
                        {
                            AircraftScanningSites.Add(geoSite.SiteId, new List<int>());
                            TFTVLogger.Always($"{geoVehicle?.Name} started scan at SiteId {geoSite.SiteId}!");
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                static void Postfix(ScanAbility __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;
                        geoVehicle.CanRedirect = false;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(GeoScanner), "CompleteScan")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class GeoScanner_CompleteScan_Patch
            {
                static void Prefix(GeoScanner __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        GeoVehicle geoVehicle = __instance?.Location?.Vehicles?.FirstOrDefault(v => v.IsOwnedByViewer &&
                         (v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) || v.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule))
                         && !v.CanRedirect);

                        TFTVLogger.Always($"[GeoScanner.CompleteScan] __instance?.Location?.Vehicles?.FirstOrDefault(v => v.IsOwnedByViewer {__instance?.Location?.Vehicles?.FirstOrDefault(v => v.IsOwnedByViewer)?.VehicleID}");


                        if (geoVehicle == null)
                        {

                            TFTVLogger.Always($"[GeoScanner.CompleteScan] geoVehicle is null!");
                            return;
                        }

                        geoVehicle.CanRedirect = true;

                        if (AircraftScanningSites.ContainsKey(__instance.Location.SiteId))
                        {
                            AircraftScanningSites.Remove(__instance.Location.SiteId);
                        }

                        TFTVLogger.Always($"{geoVehicle.Name} finished scan!");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static Dictionary<int, List<int>> AircraftScanningSites = new Dictionary<int, List<int>>();


            [HarmonyPatch(typeof(GeoScanComponent), "DetectSite")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            public static class GeoScanComponent_DetectSite_Patch
            {

                static bool Prefix(GeoScanComponent __instance, GeoSite site, GeoActor ____actor)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return true;
                        }

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        if (controller == null || controller.PhoenixFaction == null || controller.PhoenixFaction.Research == null)
                        {
                            return true;
                        }

                        Research phoenixResearch = controller.PhoenixFaction.Research;

                        if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.AlienBase)
                        {
                            if (phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef") && phoenixResearch.HasCompleted("PX_Alien_Citadel_ResearchDef"))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }


                        if (__instance.ScanDef == _basicScannerComponent ||
                            __instance.ScanDef == _thunderbirdScannerComponent && !phoenixResearch.HasCompleted("NJ_NeuralTech_ResearchDef"))
                        {

                            GeoScanner scanner = (GeoScanner)____actor;


                            if (scanner == null)
                            {
                                //  TFTVLogger.Always($"[GeoScanComponent.DetectSite] scanner is null! This is unexpected");
                                return false;
                            }

                            GeoSite scannerLocationGeoSite = scanner.Location;

                            if (scannerLocationGeoSite == null)
                            {
                                //  TFTVLogger.Always($"[GeoScanComponent.DetectSite] geoSite is null! This is unexpected");
                                return false;
                            }

                            int scannerLocationGeoSiteId = scannerLocationGeoSite.SiteId;

                            if (AircraftScanningSites == null)
                            {
                                AircraftScanningSites = new Dictionary<int, List<int>>();
                            }

                            if (!AircraftScanningSites.ContainsKey(scannerLocationGeoSiteId))
                            {
                                // TFTVLogger.Always($"[GeoScanComponent.DetectSite] ScannerLocationSiteId not in the dictionary! This is unexpected");
                                AircraftScanningSites.Add(scannerLocationGeoSiteId, new List<int>());
                            }


                            if (!AircraftScanningSites[scannerLocationGeoSiteId].Contains(site.SiteId))
                            {
                                AircraftScanningSites[scannerLocationGeoSiteId].Add(site.SiteId);
                            }
                            else
                            {
                                //  TFTVLogger.Always($"site {site.name} already scanned by {geoVehicle.Name}, not rolling again");
                                return false;
                            }


                            int chance = 50;

                            if (__instance.ScanDef == _thunderbirdScannerComponent)
                            {
                                chance = 75;
                            }

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int num = UnityEngine.Random.Range(0, 100);
                            if (num < chance)
                            {
                                // TFTVLogger.Always($"rolled {num} Not revealing {site?.name}");
                                return false;
                            }

                            // TFTVLogger.Always($"rolled {num} revealing {site?.name}");
                        }

                        return true;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                static void Postfix(GeoScanComponent __instance, GeoSite site, GeoFaction owner)
                {
                    try
                    {
                        if (!AircraftReworkOn || site == null)
                        {
                            return;
                        }

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        if (controller == null || controller.PhoenixFaction == null || controller.PhoenixFaction.Research == null)
                        {
                            return;
                        }

                        Research phoenixResearch = controller.PhoenixFaction.Research;

                        // TFTVLogger.Always($"owner null? {owner==null}");

                        /* if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.Haven && !site.GetInspected(__instance.Owner))
                         {
                             site.SetInspected(owner, inspected: true);
                         }*/

                        if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.AlienBase && !site.GetInspected(__instance.Owner)
                            && phoenixResearch.HasCompleted("PX_Alien_Citadel_ResearchDef") && phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef"))
                        {
                            site.SetInspected(owner, inspected: true);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            // ── Context DTO populated in TryRevealAlienBase and consumed by the modal ──
            internal class ColonyDetectionContext
            {

                internal bool SatelliteUplinkInRange;
                internal bool OutpostToss1Succeeded;
                internal bool OutpostWasInRange;
                internal bool ExplorationAffinityTriggered;
                internal string ExplorationOperativeName;
                internal int ExplorationAffinityRank;
                internal int ExplorationAffinityChance;
                internal int PreviousTracingAttempts;
                internal float IncrementalRevealChance;
                internal float BaseRevealChance;          // AlienBaseTypeDef.BaseRevealChance
                internal bool Detected;

                internal static readonly ColonyDetectionContext Last = new ColonyDetectionContext();

                internal void Reset()
                {
                    SatelliteUplinkInRange = false;
                    OutpostToss1Succeeded = false;
                    OutpostWasInRange = false;
                    ExplorationAffinityTriggered = false;
                    ExplorationOperativeName = null;
                    ExplorationAffinityRank = 0;
                    ExplorationAffinityChance = 0;
                    PreviousTracingAttempts = 0;
                    IncrementalRevealChance = 0f;
                    BaseRevealChance = 0f;
                    Detected = false;
                }

            }

            [HarmonyPatch(typeof(PandoranBaseRevealDataBind), "ModalShowHandler", new[] { typeof(UIModal) })]
            public static class Patch_PandoranBaseRevealDataBind_ModalShowHandler
            {
                public static void Postfix(PandoranBaseRevealDataBind __instance, UIModal modal)
                {
                    try
                    {
                        if (__instance?.Description == null || modal == null)
                            return;

                        ColonyDetectionContext ctx = ColonyDetectionContext.Last;
                        GeoSite geoSite = modal.Data as GeoSite;

                        System.Text.StringBuilder sb = new System.Text.StringBuilder();

                        if (geoSite == null)
                        {
                            sb.AppendLine("No Pandoran colony was detected after the attack.");
                        }
                        else
                        {
                            string baseType = geoSite.GetComponent<GeoAlienBase>()
                                ?.AlienBaseTypeDef?.Name?.Localize(null) ?? "Pandoran Colony";
                            sb.AppendLine($"Pandoran colony located: {baseType}.");
                        }

                        sb.AppendLine();
                        sb.AppendLine("Detection contributors:");

                        // Base + effective detection chance
                        int baseChancePct = (int)(ctx.BaseRevealChance * 100f);
                        int effectiveChancePct = Math.Min(100,
                            (int)(ctx.BaseRevealChance * 100f
                                + ctx.IncrementalRevealChance * 100f * ctx.PreviousTracingAttempts));
                        sb.AppendLine($"  Base detection chance: {baseChancePct}%  |  Effective chance: {effectiveChancePct}%");
                        sb.AppendLine();

                        // Satellite Uplink
                        if (ctx.SatelliteUplinkInRange)
                            sb.AppendLine("  + Satellite Uplink: in range");
                        else
                            sb.AppendLine("  - Satellite Uplink: not in range");

                        // Outpost
                        if (ctx.OutpostWasInRange)
                        {
                            string toss1 = ctx.OutpostToss1Succeeded ? "success" : "failed";
                            sb.AppendLine($"  + Outpost: in range ({toss1})");
                        }
                        else
                        {
                            sb.AppendLine("  - Outpost: not in range");
                        }

                        // Exploration Affinity
                        if (ctx.ExplorationAffinityRank > 0)
                        {
                            string opName = ctx.ExplorationOperativeName ?? "Unknown";
                            string triggered = ctx.ExplorationAffinityTriggered ? "triggered" : "did not trigger";
                            sb.AppendLine($"  {(ctx.ExplorationAffinityTriggered ? "+" : "-")} Exploration affinity ({opName}, rank {ctx.ExplorationAffinityRank}): {ctx.ExplorationAffinityChance}% chance, {triggered}");
                        }
                        else
                        {
                            sb.AppendLine("  - Exploration affinity: no operative");
                        }

                        // Previous tracing attempts
                        if (ctx.PreviousTracingAttempts > 0)
                        {
                            float tracingPercent = ctx.IncrementalRevealChance * 100f * ctx.PreviousTracingAttempts;
                            sb.AppendLine($"  + Previous tracing: {ctx.PreviousTracingAttempts} attempt(s) (+{tracingPercent:F0}% base detection chance)");
                        }
                        else
                        {
                            sb.AppendLine("  - Previous tracing: none");
                        }

                        __instance.Description.text = sb.ToString();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            [HarmonyPatch(typeof(GeoAlienFaction), "TryRevealAlienBase")]
            internal static class BC_GeoAlienFaction_TryRevealAlienBase_patch
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                private static bool Prefix(ref bool __result, GeoSite site, GeoFaction revealToFaction, GeoLevelController ____level)
                {
                    try
                    {
                        ColonyDetectionContext ctx = ColonyDetectionContext.Last;
                        ctx.Reset();

                        if (!site.GetVisible(revealToFaction))
                        {
                            GeoAlienBase component = site.GetComponent<GeoAlienBase>();

                            // Capture counter and reveal chance BEFORE any increments
                            ctx.PreviousTracingAttempts = component.BaseAttacksCounter;
                            ctx.IncrementalRevealChance = component.AlienBaseTypeDef?.IncrementalRevealChance ?? 0f;
                            ctx.BaseRevealChance = component.AlienBaseTypeDef?.BaseRevealChance ?? 0f;

                            if (revealToFaction is GeoPhoenixFaction geoPhoenixFaction)
                            {
                                EarthUnits thunderbirdScannerRange = AircraftReworkOn
                                    ? new EarthUnits() { Value = GetThunderbirdScannerRange() }
                                    : new EarthUnits() { Value = 0 };

                                bool anyThunderbirdScannerInRange = AircraftReworkOn
                                    && geoPhoenixFaction.Research.HasCompleted("NJ_SateliteUplink_ResearchDef")
                                    && geoPhoenixFaction.Vehicles.Any(v =>
                                        v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule)
                                        && v.CurrentSite != null
                                        && ____level.Map.SitesInRange(v.CurrentSite, thunderbirdScannerRange, true).Contains(site));

                                ctx.SatelliteUplinkInRange = anyThunderbirdScannerInRange;

                                if (geoPhoenixFaction.IsSiteInBaseScannerRange(site, true) || anyThunderbirdScannerInRange)
                                {
                                    component.IncrementBaseAttacksRevealCounter();
                                }

                                EarthUnits outPostRange = new EarthUnits() { Value = 2500 };

                                bool anyOutpostInRange = BaseReworkCheck.BaseReworkEnabled &&
                                    geoPhoenixFaction.Bases.Any(b =>
                                        b.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag) &&
                                        ____level.Map.SitesInRange(b.Site, outPostRange, true).Contains(site));

                                ctx.OutpostWasInRange = anyOutpostInRange;

                                if (anyOutpostInRange)
                                {
                                    ctx.OutpostToss1Succeeded = UnityEngine.Random.Range(0, 100) < 50;
                                    if (ctx.OutpostToss1Succeeded)
                                        component.IncrementBaseAttacksRevealCounter();
                                }

                                // Exploration affinity option 2: +15% per rank chance to detect colony after a haven attack
                                string explorationOpName;
                                int explorationOpRank;
                                bool hasExplorationOperative = TFTVIncidents.AffinityGeoscapeEffects
                                    .TryGetExplorationColonyDetectionOperative(____level, out explorationOpName, out explorationOpRank);

                                if (hasExplorationOperative && explorationOpRank > 0)
                                {
                                    int detectionChance = 15 * explorationOpRank;
                                    ctx.ExplorationAffinityChance = detectionChance;
                                    ctx.ExplorationOperativeName = explorationOpName;
                                    ctx.ExplorationAffinityRank = explorationOpRank;

                                    UnityEngine.Random.InitState((int)System.Diagnostics.Stopwatch.GetTimestamp());
                                    if (UnityEngine.Random.Range(0, 100) < detectionChance)
                                    {
                                        TFTVLogger.Always($"[Incidents][AffinityEffects] Exploration affinity (rank {explorationOpRank}) triggered extra colony detection increment for {site?.name}.");
                                        component.IncrementBaseAttacksRevealCounter();
                                        ctx.ExplorationAffinityTriggered = true;
                                    }
                                }
                            }

                            if (component.CheckForBaseReveal())
                            {
                                site.RevealSite(____level.PhoenixFaction);
                                ctx.Detected = true;
                                __result = true;
                                return false;
                            }
                            component.IncrementBaseAttacksRevealCounter();
                        }
                        __result = false;
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                    throw new InvalidOperationException();
                }
            }

        }
    }
}





