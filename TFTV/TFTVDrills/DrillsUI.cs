using Base.Core;
using Base.Entities.Abilities;
using Base.Input;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private const int SwapSpCost = 10;
        private const float MenuMaxHeight = 950f;
        private const float MenuWidth = 760f;
        private const int GridColumns = 5;
        private const int VisibleGridRows = 5;
        private const float GridCellWidth = 128f;
        private const float GridCellHeight = 140f;
        private const float GridSpacing = 12f;
        private const float GridPadding = 18f;
        private const float HeaderIconFrameSize = 100f;
        private const float HeaderIconSize = 92f;
        private const float HeaderFrameBorderThickness = 4f;
        private const float HeaderSectionHeight = 120f;
        private const float ContentTopPadding = 20f;
        private const float ContentSpacing = 20f;
        private const float FacilityOverlayOpacity = 0.75f;
        private const float FacilityIconSize = 72f;

        private static readonly Color LockedIconTint = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color LockedLabelTint = new Color(0.82f, 0.82f, 0.82f, 1f);
        internal static readonly Color DrillPulseColor = new Color(1f, 0.4f, 0f, 1f);
        private static readonly Color DrillFrameColor = new Color(0.29803923f, 0.09019608f, 0f, 1f);
        private static readonly Color LockedFrameColor = new Color(0.15294118f, 0.15294118f, 0.15294118f, 1f);

        private static Sprite _originalAvailableImage = null;
        private static DrillConfirmationContext _pendingDrillConfirmation;

        private static bool IsDrillAbility(TacticalAbilityDef ability)
        {
            return ability != null && DrillsDefs.Drills != null && DrillsDefs.Drills.Contains(ability);
        }

        private static Image CreateHeaderIcon(Transform parent, TacticalAbilityDef ability, Color iconColor, bool showFrame, bool isLocked)
        {
            if (parent == null || ability == null)
            {
                return null;
            }

            var iconRootRect = UIBuilder.CreateChildRectTransform(parent,
                 showFrame ? "IconFrame" : "IconContainer",
                 anchorMin: new Vector2(0.5f, 0.5f),
                 anchorMax: new Vector2(0.5f, 0.5f),
                 pivot: new Vector2(0.5f, 0.5f),
                 anchoredPosition: Vector2.zero,
                 sizeDelta: new Vector2(HeaderIconFrameSize, HeaderIconFrameSize));

            UIBuilder.ConfigureLayoutElement(iconRootRect,
                minWidth: HeaderIconFrameSize,
                minHeight: HeaderIconFrameSize,
                preferredWidth: HeaderIconFrameSize,
                preferredHeight: HeaderIconFrameSize,
                flexibleWidth: 0f,
                flexibleHeight: 0f);

            Transform iconParent = iconRootRect;

            if (showFrame)
            {
                Color frameColor = isLocked ? LockedFrameColor : DrillFrameColor;

                UIBuilder.CreateFrameBorders(iconRootRect, frameColor, HeaderFrameBorderThickness);

                var frameBackgroundRect = UIBuilder.CreateChildRectTransform(iconRootRect,
                    "FrameBackground",
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    components: typeof(Image));

                frameBackgroundRect.offsetMin = new Vector2(HeaderFrameBorderThickness, HeaderFrameBorderThickness);
                frameBackgroundRect.offsetMax = new Vector2(-HeaderFrameBorderThickness, -HeaderFrameBorderThickness);

                var frameBackgroundImage = frameBackgroundRect.GetComponent<Image>();
                frameBackgroundImage.color = Color.black;
                frameBackgroundImage.raycastTarget = false;

                iconParent = frameBackgroundRect;
            }

            var iconRect = UIBuilder.CreateChildRectTransform(iconParent,
                "Icon",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(HeaderIconSize, HeaderIconSize),
                components: typeof(Image));

            var iconImage = iconRect.GetComponent<Image>();
            iconImage.sprite = ability.ViewElementDef?.LargeIcon ?? ability.ViewElementDef?.SmallIcon;

            if (iconImage.sprite == null)
            {
                iconRootRect.gameObject.SetActive(false);
                return null;
            }

            iconImage.preserveAspect = true;
            iconImage.color = iconColor;
            iconImage.raycastTarget = false;

            return iconImage;
        }


    }
}
