using Base.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The game's own ability and item tooltips, cloned onto this screen's canvas.
    ///
    /// The Haven Recruits panel shows exactly these, but its copies live on that overlay's canvas
    /// and are reached through its statics; ours are hosted here instead, so they sort above this
    /// panel's dialogs and die with them.
    /// </summary>
    internal static class PersonnelVanillaTooltips
    {
        private const float TooltipScale = 0.5f;
        private const float TooltipMargin = 8f;

        private static GeoRosterAbilityDetailTooltip _abilityTooltip;
        private static UIGeoItemTooltip _itemTooltip;

        internal static GeoRosterAbilityDetailTooltip EnsureAbilityTooltip(Transform host)
        {
            try
            {
                if (_abilityTooltip != null)
                {
                    return _abilityTooltip;
                }

                Canvas canvas = host?.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return null;
                }

                GeoRosterAbilityDetailTooltip template = FindTemplate<GeoRosterAbilityDetailTooltip>(t => t != _abilityTooltip);
                if (template == null)
                {
                    return null;
                }

                GameObject clone = Object.Instantiate(template.gameObject, canvas.transform, false);
                clone.name = "TFTV_PersonnelAbilityTooltip";
                clone.transform.localScale = Vector3.one * TooltipScale;
                clone.SetActive(false);

                LayoutElement element = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
                element.ignoreLayout = true;

                _abilityTooltip = clone.GetComponent<GeoRosterAbilityDetailTooltip>();
                return _abilityTooltip;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        internal static UIGeoItemTooltip EnsureItemTooltip(Transform host)
        {
            try
            {
                if (_itemTooltip != null)
                {
                    return _itemTooltip;
                }

                Canvas canvas = host?.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return null;
                }

                UIGeoItemTooltip template = FindTemplate<UIGeoItemTooltip>(t => t != _itemTooltip);
                if (template == null)
                {
                    return null;
                }

                GameObject clone = Object.Instantiate(template.gameObject, canvas.transform, false);
                clone.name = "TFTV_PersonnelItemTooltip";
                clone.transform.localScale = Vector3.one * TooltipScale;
                clone.SetActive(false);

                LayoutElement element = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
                element.ignoreLayout = true;

                _itemTooltip = clone.GetComponent<UIGeoItemTooltip>();
                return _itemTooltip;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// Finds a tooltip to clone, preferring one that is actually part of the live geoscape view.
        ///
        /// Resources.FindObjectsOfTypeAll also returns prefab assets, and a prefab's item tooltip has
        /// no ability rows instantiated yet: the vanilla SetAbilities indexes straight into that list
        /// and threw as soon as gear with abilities was hovered.
        /// </summary>
        private static T FindTemplate<T>(Func<T, bool> isNotOurs) where T : MonoBehaviour
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            GeoscapeView view = level?.View;

            T template = null;
            if (view != null)
            {
                template = view.GetComponentsInChildren<T>(true)
                    .FirstOrDefault(t => t != null && isNotOurs(t) && t.hideFlags == HideFlags.None);
            }

            if (template == null)
            {
                template = Object.FindObjectsOfType<T>()
                    .FirstOrDefault(t => t != null && isNotOurs(t) && t.hideFlags == HideFlags.None);
            }

            if (template == null)
            {
                template = Resources.FindObjectsOfTypeAll<T>()
                    .FirstOrDefault(t => t != null && isNotOurs(t) && t.hideFlags == HideFlags.None);
            }

            return template;
        }

        /// <summary>
        /// Places a tooltip beside the element that raised it, kept inside the canvas.
        /// </summary>
        internal static void PositionNextTo(Transform tooltipTransform, Transform anchor)
        {
            RectTransform tooltip = tooltipTransform as RectTransform;
            RectTransform anchorRect = anchor as RectTransform;
            if (tooltip == null || anchorRect == null)
            {
                return;
            }

            Canvas canvas = tooltip.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas?.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
            {
                return;
            }

            tooltip.anchorMin = new Vector2(0.5f, 0.5f);
            tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            tooltip.pivot = new Vector2(1f, 1f);

            float halfWidth = canvasRect.rect.width * 0.5f;
            float halfHeight = canvasRect.rect.height * 0.5f;
            float width = tooltip.rect.width * TooltipScale;
            float height = tooltip.rect.height * TooltipScale;

            float x = Mathf.Clamp(localPoint.x, -halfWidth + width + TooltipMargin, halfWidth - TooltipMargin);
            float y = Mathf.Clamp(localPoint.y, -halfHeight + height + TooltipMargin, halfHeight - TooltipMargin);

            tooltip.anchoredPosition = new Vector2(x, y);
            tooltip.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Raises the game's ability tooltip - name, description, costs - for one ability icon.
    /// </summary>
    internal sealed class PersonnelAbilityTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal TacticalAbilityDef Ability;
        internal ViewElementDef View;

        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                if (Ability == null)
                {
                    return;
                }

                GeoRosterAbilityDetailTooltip tooltip = PersonnelVanillaTooltips.EnsureAbilityTooltip(transform);
                if (tooltip == null)
                {
                    return;
                }

                tooltip.Show(Ability, View, useMutagens: false, cost: 0);
                PersonnelVanillaTooltips.PositionNextTo(tooltip.transform, transform);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Hide()
        {
            try
            {
                GeoRosterAbilityDetailTooltip tooltip = PersonnelVanillaTooltips.EnsureAbilityTooltip(transform);
                tooltip?.Hide();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
