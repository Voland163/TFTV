using PhoenixPoint.Geoscape.Entities;
using HarmonyLib;
using Base.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using TFTV.TFTVHavenRecruitsUI;
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
        // The Haven overlay halves these; at that size they are hard to read on this screen, so ours
        // are shown at the size the game itself uses.
        private const float TooltipScale = 1f;
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

                RaiseAboveHost(clone, canvas);

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

                RaiseAboveHost(clone, canvas);

                _itemTooltip = clone.GetComponent<UIGeoItemTooltip>();

                // Keeps the vanilla fade-in from nudging the tooltip's parent around, the same fix
                // the Haven Recruits panel applies to its own copy.
                TooltipLayoutFixes.RegisterTooltip(_itemTooltip);

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

            // A pristine copy is preferred - cloning another TFTV screen's tooltip inherits whatever
            // it was set up for, scale included - but one of those beats having no tooltip at all.
            bool IsUsable(T candidate) =>
                candidate != null && isNotOurs(candidate) && candidate.hideFlags == HideFlags.None;

            bool IsVanilla(T candidate) =>
                IsUsable(candidate) && !candidate.gameObject.name.StartsWith("TFTV_", StringComparison.Ordinal);

            T template = null;
            if (view != null)
            {
                template = view.GetComponentsInChildren<T>(true).FirstOrDefault(IsVanilla);
            }

            if (template == null)
            {
                template = Object.FindObjectsOfType<T>().FirstOrDefault(IsVanilla);
            }

            if (template == null)
            {
                template = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(IsVanilla);
            }

            if (template == null)
            {
                template = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(IsUsable);
            }

            if (template == null)
            {
                TFTVLogger.Always($"[PersonnelUI] No {typeof(T).Name} to clone for the personnel screen tooltips.");
            }

            return template;
        }

        /// <summary>
        /// Puts a freshly cloned tooltip above the screen that hosts it. These prefabs carry their
        /// own Canvas, which sorts on its own order - left alone, the tooltip renders underneath the
        /// dialog it belongs to and looks like it never appeared.
        /// </summary>
        private static void RaiseAboveHost(GameObject clone, Canvas host)
        {
            Canvas ownCanvas = clone.GetComponent<Canvas>();
            if (ownCanvas == null || host == null)
            {
                return;
            }

            ownCanvas.overrideSorting = true;
            if (ownCanvas.sortingOrder <= host.sortingOrder)
            {
                ownCanvas.sortingOrder = host.sortingOrder + 1;
            }
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

            // From the parent: these tooltips have a Canvas of their own, and measuring against that
            // means measuring the tooltip against itself - which clamped every tooltip to the same
            // spot a few pixels off the middle of the dialog.
            Canvas canvas = tooltip.parent != null
                ? tooltip.parent.GetComponentInParent<Canvas>()
                : tooltip.GetComponentInParent<Canvas>();

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
    /// UIInventoryTooltipItemPanel.SetAbilities writes an item's abilities into a pool of rows the
    /// prefab carries, indexing straight into it: gear with more abilities than the pool has rows
    /// throws ArgumentOutOfRange and the tooltip never appears. The pool is grown here, from an
    /// existing row or the panel's own prefab, before the vanilla method runs.
    /// </summary>
    [HarmonyPatch(typeof(UIInventoryTooltipItemPanel), nameof(UIInventoryTooltipItemPanel.SetAbilities))]
    internal static class UIInventoryTooltipItemPanel_SetAbilities_Capacity
    {
        private static void Prefix(UIInventoryTooltipItemPanel __instance, List<ItemAbilityTooltipData> abilityData)
        {
            try
            {
                if (__instance == null || abilityData == null)
                {
                    return;
                }

                if (__instance.AbilitiesObjects == null)
                {
                    __instance.AbilitiesObjects = new List<UIInventoryTooltipItemAbility>();
                }

                int needed = abilityData.Count;
                if (__instance.AbilitiesObjects.Count >= needed)
                {
                    return;
                }

                UIInventoryTooltipItemAbility source = __instance.AbilitiesObjects.FirstOrDefault(row => row != null)
                    ?? __instance.AbilityPrefab
                    ?? __instance.AbilitiesHeader
                    ?? Resources.FindObjectsOfTypeAll<UIInventoryTooltipItemAbility>()
                        .FirstOrDefault(row => row != null && row.hideFlags == HideFlags.None);

                if (source != null)
                {
                    Transform rowParent = __instance.AbilitiesObjects.FirstOrDefault(row => row != null)?.transform.parent
                        ?? __instance.AbilitiesHeader?.transform.parent
                        ?? __instance.transform;

                    while (__instance.AbilitiesObjects.Count < needed)
                    {
                        UIInventoryTooltipItemAbility row = Object.Instantiate(source, rowParent, false);
                        row.gameObject.SetActive(false);
                        __instance.AbilitiesObjects.Add(row);
                    }

                    return;
                }

                // Nothing to clone from: show what fits rather than letting the vanilla loop run off
                // the end of the list.
                TFTVLogger.Always($"[PersonnelUI] Item tooltip has {__instance.AbilitiesObjects.Count} ability rows for "
                    + $"{needed} abilities and no row to clone; trimming the list.");

                abilityData.RemoveRange(__instance.AbilitiesObjects.Count,
                    needed - __instance.AbilitiesObjects.Count);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }

    /// <summary>
    /// Raises the game's item tooltip for one gear slot.
    ///
    /// The Haven Recruits slot helper attaches a forwarder of its own, but that one drops the tooltip
    /// at a fixed spot chosen for its overlay - on this screen that is off in a corner of the dialog.
    /// This trigger replaces it and places the tooltip beside the slot.
    /// </summary>
    internal sealed class PersonnelItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal GeoItem Item;
        internal string FallbackText;

        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                if (Item == null)
                {
                    return;
                }

                UIGeoItemTooltip tooltip = PersonnelVanillaTooltips.EnsureItemTooltip(transform);
                if (tooltip == null)
                {
                    PersonnelTooltip.Show(FallbackText, transform);
                    return;
                }

                // Without this the tooltip stamps a red "not proficient" warning across gear the
                // operative is carrying and using.
                tooltip.ShowStats(Item, transform, isProficient: true);

                if (tooltip.transform is RectTransform rect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }

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
                PersonnelTooltip.Hide();

                UIGeoItemTooltip tooltip = PersonnelVanillaTooltips.EnsureItemTooltip(transform);
                if (tooltip != null)
                {
                    tooltip.HideStats();
                    tooltip.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }

    /// <summary>
    /// Raises the game's ability tooltip - name, description, costs - for one ability icon.
    /// </summary>
    internal sealed class PersonnelAbilityTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal TacticalAbilityDef Ability;
        internal ViewElementDef View;

        private static bool _logged;
        private static bool _primed;

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
                    // Better a plain description than nothing when the game's tooltip cannot be had.
                    ViewElementDef view = View ?? Ability.ViewElementDef;
                    string title = view?.DisplayName1?.Localize();
                    string description = view?.Description?.Localize();
                    PersonnelTooltip.Show(string.IsNullOrEmpty(description) ? title : $"{title}\n\n{description}", transform);
                    return;
                }

                tooltip.Show(Ability, View, useMutagens: false, cost: 0);

                // The first showing of a fresh clone comes up as "NEEDS TEXT": the localisation is
                // resolved as it is displayed, so it is shown once, hidden, and shown again.
                if (!_primed)
                {
                    _primed = true;
                    tooltip.Hide();
                    tooltip.Show(Ability, View, useMutagens: false, cost: 0);
                }

                // Show fills the text; the tooltip has to be laid out before its size is known, or it
                // is placed against a stale rect and ends up pinned to the edge of the screen.
                if (tooltip.transform is RectTransform rect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }

                PersonnelVanillaTooltips.PositionNextTo(tooltip.transform, transform);

                if (!_logged)
                {
                    // One line, once per session: whether the tooltip exists and where it ended up.
                    _logged = true;
                    var rectTransform = tooltip.transform as RectTransform;
                    TFTVLogger.Always($"[PersonnelUI] Ability tooltip '{tooltip.gameObject.name}' "
                        + $"active={tooltip.gameObject.activeInHierarchy} parent={tooltip.transform.parent?.name} "
                        + $"pos={rectTransform?.anchoredPosition} size={rectTransform?.rect.size} "
                        + $"scale={tooltip.transform.localScale}");
                }
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
                PersonnelTooltip.Hide();

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
