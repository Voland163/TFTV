using Base.Core;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using System;
using System.Linq;
using TFTV.TFTVHavenRecruitsUI;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal class BaseInitialLootTooltip
    {
        private static UIGeoItemTooltip _lootItemTooltip;
        private static RectTransform _lootTooltipsRoot;
        private const string LootTooltipsRootName = "TFTV_BaseActivation_TooltipsRoot";

        internal static UIGeoItemTooltip EnsureLootItemTooltip(Transform overlayTransform)
        {
            try
            {
                EnsureLootItemTooltipInstance(ResolveTooltipHost(overlayTransform));
                return _lootItemTooltip;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        private static Transform ResolveTooltipHost(Transform fallback)
        {
            try
            {
                Canvas hostCanvas = null;

                if (fallback != null)
                {
                    var fallbackCanvas = fallback.GetComponentInParent<Canvas>();
                    if (IsValidTooltipCanvas(fallbackCanvas))
                    {
                        hostCanvas = fallbackCanvas;
                    }
                }

                if (hostCanvas == null)
                {
                    GeoLevelController geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    GeoscapeView view = geoLevel?.View;

                    if (view != null)
                    {
                        hostCanvas = view.GetComponentsInChildren<Canvas>(true)
                            .FirstOrDefault(IsValidTooltipCanvas);
                    }
                }

                if (hostCanvas == null)
                {
                    hostCanvas = Resources.FindObjectsOfTypeAll<Canvas>()
                        .FirstOrDefault(IsValidTooltipCanvas);
                }

                TFTVLogger.Always($"[LootTooltip] ResolveTooltipHost hostCanvas={hostCanvas?.name}, renderMode={hostCanvas?.renderMode}, fallback={fallback?.name}");
                return hostCanvas != null ? hostCanvas.transform : fallback;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static bool IsValidTooltipCanvas(Canvas canvas)
        {
            try
            {
                return canvas != null
                    && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    && (canvas.name?.IndexOf("ReportIssue", StringComparison.OrdinalIgnoreCase) ?? -1) < 0;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); throw; }
        }


        private static void EnsureLootItemTooltipInstance(Transform overlayTransform)
        {
            if (overlayTransform == null)
            {
                TFTVLogger.Always("[LootTooltip] EnsureLootItemTooltipInstance overlayTransform null");
                return;
            }

            RectTransform tooltipRoot = EnsureLootTooltipsRoot(overlayTransform);
            if (tooltipRoot == null)
            {
                TFTVLogger.Always("[LootTooltip] EnsureLootItemTooltipInstance tooltipRoot null");
                return;
            }

            if (_lootItemTooltip != null)
            {
                if (_lootItemTooltip.transform.parent != tooltipRoot)
                {
                    _lootItemTooltip.transform.SetParent(tooltipRoot, false);
                }
                TFTVLogger.Always($"[LootTooltip] reuse tooltip parent={_lootItemTooltip.transform.parent?.name}, scale={_lootItemTooltip.transform.localScale}");
                TooltipLayoutFixes.RegisterTooltip(_lootItemTooltip);
                return;
            }

            UIInventoryTooltip template = FindItemTooltipTemplate();
            if (template == null)
            {
                TFTVLogger.Always("[BaseActivation] Could not locate UIInventoryTooltip template.");
                return;
            }

            GameObject cloneGO = UnityEngine.Object.Instantiate(template.gameObject, tooltipRoot, worldPositionStays: false);
            cloneGO.name = "TFTV_BaseActivation_ItemTooltip";
            //  cloneGO.transform.localScale = Vector3.one * 0.5f;
            cloneGO.SetActive(false);

            _lootItemTooltip = cloneGO.GetComponent<UIGeoItemTooltip>();
            TFTVLogger.Always($"[LootTooltip] created tooltip parent={cloneGO.transform.parent?.name}, scale={cloneGO.transform.localScale}");

            TooltipLayoutFixes.RegisterTooltip(_lootItemTooltip);
        }

        private static RectTransform EnsureLootTooltipsRoot(Transform overlayTransform)
        {
            if (overlayTransform == null)
            {
                return null;
            }

            if (_lootTooltipsRoot != null)
            {
                if (_lootTooltipsRoot.transform.parent != overlayTransform)
                {
                    _lootTooltipsRoot.SetParent(overlayTransform, false);
                }
                return _lootTooltipsRoot;
            }

            GameObject go = new GameObject(LootTooltipsRootName, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(overlayTransform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _lootTooltipsRoot = rect;
            return _lootTooltipsRoot;
        }

        private static UIInventoryTooltip FindItemTooltipTemplate()
        {
            try
            {
                UIInventoryTooltip template = null;

                GeoLevelController geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                GeoscapeView view = geoLevel?.View;
                if (view != null)
                {
                    template = view.GetComponentsInChildren<UIInventoryTooltip>(true)
                        .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None);
                }

                if (template == null)
                {
                    template = Resources.FindObjectsOfTypeAll<UIInventoryTooltip>()
                        .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None);
                }

                if (template == null)
                {
                    template = UnityEngine.Object.FindObjectsOfType<UIInventoryTooltip>()
                        .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None);
                }

                return template;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

    }
}
