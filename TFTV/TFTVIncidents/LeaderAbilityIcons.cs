using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// Shows the selected incident operative's learned abilities as a strip of icons in the leader
    /// picture slot, each hovering to the game's own ability tooltip.
    ///
    /// The learned set is <see cref="CharacterProgression.Abilities"/>, which is what the rest of the
    /// mod treats as acquired - class and personal skills, perks and drills all land there, and
    /// DrillSwapUI tests drill ownership against the same list.
    /// </summary>
    internal static class LeaderAbilityIcons
    {
        private const string ContainerName = "[TFTV]LeaderAbilityIcons";

        // Sized to sit along the bottom of the leader picture without crowding the face.
        private const int IconSize = 34;
        private const int IconSpacing = 3;
        private const int IconsPerRow = 6;
        private const float BottomOffset = 6f;

        private static GameObject _container;

        /// <summary>
        /// Rebuilds the icon strip for the given operative. Safe to call on every leader change.
        /// </summary>
        internal static void Show(UIModuleSiteEncounters module, GeoCharacter character)
        {
            try
            {
                if (module?.EncounterLeaderImage == null || character == null)
                {
                    return;
                }

                GameObject container = EnsureContainer(module);
                if (container == null)
                {
                    return;
                }

                for (int i = container.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(container.transform.GetChild(i).gameObject);
                }

                List<TacticalAbilityDef> abilities = GetLearnedAbilities(character);
                container.SetActive(abilities.Count > 0);

                foreach (TacticalAbilityDef ability in abilities)
                {
                    CreateIcon(container.transform, ability, module.EncounterLeaderImage.canvas);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Drops the strip, for when the incident goes away.
        /// </summary>
        internal static void Clear()
        {
            try
            {
                AbilityIconTooltip.HideShared();

                if (_container != null)
                {
                    UnityEngine.Object.Destroy(_container);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            finally
            {
                _container = null;
            }
        }

        /// <summary>
        /// Learned abilities that have an icon to show, in track order and without duplicates - an
        /// ability can sit on more than one track, and the same one twice in a row reads as a bug.
        /// </summary>
        private static List<TacticalAbilityDef> GetLearnedAbilities(GeoCharacter character)
        {
            List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>();
            if (character.Progression?.Abilities == null)
            {
                return abilities;
            }

            foreach (TacticalAbilityDef ability in character.Progression.Abilities)
            {
                if (ability?.ViewElementDef?.SmallIcon == null || abilities.Contains(ability))
                {
                    continue;
                }

                abilities.Add(ability);
            }

            return abilities;
        }

        private static GameObject EnsureContainer(UIModuleSiteEncounters module)
        {
            Transform parent = module.EncounterLeaderImage.transform;
            Transform existing = parent.Find(ContainerName);
            if (existing != null)
            {
                _container = existing.gameObject;
                return _container;
            }

            GameObject container = new GameObject(ContainerName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(parent, false);

            // Pinned along the bottom edge of the picture, growing upwards as rows are added.
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, BottomOffset);

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(IconSize, IconSize);
            grid.spacing = new Vector2(IconSpacing, IconSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = IconsPerRow;
            grid.childAlignment = TextAnchor.LowerCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _container = container;
            return container;
        }

        private static void CreateIcon(Transform parent, TacticalAbilityDef ability, Canvas canvas)
        {
            GameObject iconObject = new GameObject($"Ability_{ability.name}", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = ability.ViewElementDef.SmallIcon;
            image.preserveAspect = true;
            image.raycastTarget = true;

            iconObject.AddComponent<AbilityIconTooltip>().Initialize(ability, canvas);
        }

        /// <summary>
        /// Hover behaviour for one icon: shows the roster's own ability tooltip, positioned beside the
        /// icon and kept inside the canvas.
        /// </summary>
        private sealed class AbilityIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private const float Gap = 12f;
            private static readonly Vector3[] CornerBuffer = new Vector3[4];
            private static GeoRosterAbilityDetailTooltip _shared;

            private TacticalAbilityDef _ability;
            private Canvas _canvas;

            internal void Initialize(TacticalAbilityDef ability, Canvas canvas)
            {
                _ability = ability;
                _canvas = canvas;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                try
                {
                    GeoRosterAbilityDetailTooltip tooltip = EnsureTooltip();
                    if (tooltip == null || _ability == null)
                    {
                        return;
                    }

                    tooltip.Show(_ability, _ability.ViewElementDef, useMutagens: false, cost: 0);
                    tooltip.transform.SetAsLastSibling();
                    Position(tooltip);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                HideShared();
            }

            private void OnDisable()
            {
                HideShared();
            }

            internal static void HideShared()
            {
                if (_shared != null)
                {
                    _shared.Hide();
                }
            }

            /// <summary>
            /// Places the tooltip to the right of the icon, or to its left when that would run off the
            /// canvas, and keeps it vertically inside.
            /// </summary>
            private void Position(GeoRosterAbilityDetailTooltip tooltip)
            {
                if (!(tooltip.transform is RectTransform tooltipRect)
                    || _canvas == null
                    || !(_canvas.transform is RectTransform canvasRect))
                {
                    return;
                }

                Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

                ((RectTransform)transform).GetWorldCorners(CornerBuffer);
                Vector2 iconRightScreen = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[2]);
                Vector2 iconLeftScreen = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[0]);

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, iconRightScreen, camera, out Vector2 rightLocal)
                    || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, iconLeftScreen, camera, out Vector2 leftLocal))
                {
                    return;
                }

                Vector2 size = tooltipRect.rect.size * tooltipRect.localScale;
                float x = rightLocal.x + Gap + size.x * tooltipRect.pivot.x;
                if (x + size.x * (1f - tooltipRect.pivot.x) > canvasRect.rect.xMax)
                {
                    x = leftLocal.x - Gap - size.x * (1f - tooltipRect.pivot.x);
                }

                float y = Mathf.Clamp(
                    rightLocal.y,
                    canvasRect.rect.yMin + size.y * tooltipRect.pivot.y,
                    canvasRect.rect.yMax - size.y * (1f - tooltipRect.pivot.y));

                tooltipRect.anchoredPosition = new Vector2(x, y);
            }

            private GeoRosterAbilityDetailTooltip EnsureTooltip()
            {
                if (_shared != null)
                {
                    return _shared;
                }

                GeoRosterAbilityDetailTooltip template = Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>()
                    .FirstOrDefault(t => t != null);

                if (template == null)
                {
                    return null;
                }

                Transform parent = _canvas != null ? _canvas.transform : template.transform.parent;
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                clone.name = "[TFTV]IncidentAbilityTooltip";
                clone.SetActive(false);

                _shared = clone.GetComponent<GeoRosterAbilityDetailTooltip>();
                return _shared;
            }
        }
    }
}
