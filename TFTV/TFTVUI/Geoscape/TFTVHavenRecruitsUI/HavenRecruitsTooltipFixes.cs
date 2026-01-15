using HarmonyLib;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal static class TooltipLayoutFixes
    {
        private static readonly HashSet<int> ManagedTooltips = new HashSet<int>();

        internal static void RegisterTooltip(UIGeoItemTooltip tooltip)
        {
            if (tooltip == null)
            {
                return;
            }

            ManagedTooltips.Add(tooltip.GetInstanceID());
        }

        private static bool IsManaged(UIGeoItemTooltip tooltip)
        {
            return tooltip != null && ManagedTooltips.Contains(tooltip.GetInstanceID());
        }

        [HarmonyPatch(typeof(UIGeoItemTooltip), "FadeInCrt")] //VERIFIED
        private static class UIGeoItemTooltip_FadeInCrt_Patch
        {
            private static readonly AccessTools.FieldRef<UIGeoItemTooltip, Canvas> CanvasRef =
                AccessTools.FieldRefAccess<UIGeoItemTooltip, Canvas>("_canvas");

            private static readonly AccessTools.FieldRef<UIGeoItemTooltip, CanvasGroup> CanvasGroupRef =
                AccessTools.FieldRefAccess<UIGeoItemTooltip, CanvasGroup>("_canvasGroup");

            private static readonly AccessTools.FieldRef<UIGeoItemTooltip, Transform> AttachedTransformRef =
                AccessTools.FieldRefAccess<UIGeoItemTooltip, Transform>("_attachedTransform");

            private static readonly AccessTools.FieldRef<UIGeoItemTooltip, IEnumerator> FadeInCrtFieldRef =
                AccessTools.FieldRefAccess<UIGeoItemTooltip, IEnumerator>("_fadeInCrt");

            private static readonly System.Func<UIGeoItemTooltip, float> FadeInDelayGetter =
                CreateFloatGetter("FadeInDelay");

            private static readonly System.Func<UIGeoItemTooltip, float> FadeInDurationGetter =
                CreateFloatGetter("FadeInDuration");

            private static System.Func<UIGeoItemTooltip, float> CreateFloatGetter(string propertyName)
            {
                var getter = AccessTools.PropertyGetter(typeof(UIGeoItemTooltip), propertyName);
                if (getter == null)
                {
                    return null;
                }

                return AccessTools.MethodDelegate<System.Func<UIGeoItemTooltip, float>>(getter);
            }

            public static bool Prefix(UIGeoItemTooltip __instance, ref IEnumerator __result)
            {
                if (!IsManaged(__instance))
                {
                    return true;
                }

                __result = FadeInWithoutParentNudge(__instance);
                return false;
            }

            private static IEnumerator FadeInWithoutParentNudge(UIGeoItemTooltip tooltip)
            {
                var canvas = CanvasRef(tooltip);
                if (canvas != null && !canvas.enabled)
                {
                    canvas.enabled = true;
                }

                var canvasGroup = CanvasGroupRef(tooltip);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                var attachedTransform = AttachedTransformRef(tooltip);
                if (attachedTransform != null)
                {
                    tooltip.transform.position = attachedTransform.position;
                }

                var rootLayout = tooltip.RootLayoutGroup;
                if (rootLayout != null)
                {
                    if (rootLayout.transform.parent is RectTransform parentRect)
                    {
                        parentRect.anchoredPosition = Vector3.zero;
                    }

                    GameObject rootCanvas = TTUtil.GetRootCanvas(rootLayout.gameObject);
                    if (rootCanvas != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvas.transform as RectTransform);
                    }

                    LayoutRebuilder.ForceRebuildLayoutImmediate(rootLayout.transform as RectTransform);
                }

                float fadeInDelay = FadeInDelayGetter != null ? FadeInDelayGetter(tooltip) : 0f;
                yield return new WaitForSeconds(Mathf.Max(0f, fadeInDelay));

                float passedTime = 0f;
                float fadeInDuration = FadeInDurationGetter != null ? FadeInDurationGetter(tooltip) : 0f;

                while (passedTime < fadeInDuration)
                {
                    if (canvasGroup != null)
                    {
                        float alpha = fadeInDuration > 0f ? passedTime / fadeInDuration : 1f;
                        canvasGroup.alpha = alpha;
                    }

                    passedTime += Time.deltaTime;
                    yield return null;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                FadeInCrtFieldRef(tooltip) = null;
            }
        }
    }
}
