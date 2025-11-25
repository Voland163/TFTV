using PhoenixPoint.Tactical.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private const int SwapSpCost = 10;
        private const float MenuMaxHeight = 950f;
        private const float MenuWidth = 900f;
        private const int GridColumns = 6;
        private const int VisibleGridRows = 4;
        private const float GridCellWidth = 128f;
        private const float GridCellHeight = GridCellWidth;
        private const float GridSpacing = 12f;
        private const float GridPadding = 18f;
        private const float HeaderIconFrameSize = 100f;
        private const float HeaderIconSize = 92f;
        private const float OptionIconFrameSize = 92f;
        private const float OptionIconSize = 80f;
        private const float HeaderIconFrameHeight = 100f;
        private const float HeaderIconFrameWidth = HeaderIconFrameHeight * 2f;
        private const float HeaderIconHeight = 92f;
        private const float HeaderIconWidth = HeaderIconHeight * 2f;

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
        private static readonly Color HeaderFrameBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color LockedFrameColor = new Color(0.15294118f, 0.15294118f, 0.15294118f, 1f);
        private static readonly Color StandardOutlineColor = new Color(0.30980393f, 0.30980393f, 0.30980393f, 1f);
        private static readonly Color OptionFillColor = new Color(0.2509804f, 0.11372549f, 0.0627451f, 1f);
        private static readonly Color AcquireHoverFillColor = new Color(0.25098039f, 0.25098039f, 0.25098039f, 1f);

        private static Sprite _originalAvailableImage = null;
        private static DrillConfirmationContext _pendingDrillConfirmation;
        private static Text _headerText = null;

        internal class InternalData
        {
            public static void ClearInternalData()
            {
                _headerText = null;
            }

        }

        private static bool IsDrillAbility(TacticalAbilityDef ability)
        {
            return ability != null && DrillsDefs.Drills != null && DrillsDefs.Drills.Contains(ability);
        }

        private static Image CreateHeaderIcon(Transform parent, TacticalAbilityDef ability, Color iconColor, bool showFrame, bool isLocked, out Image backgroundImage)
        {
            backgroundImage = null;

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
                sizeDelta: new Vector2(HeaderIconFrameWidth, HeaderIconFrameHeight));

            UIBuilder.ConfigureLayoutElement(iconRootRect,
                minWidth: HeaderIconFrameWidth,
                minHeight: HeaderIconFrameHeight,
                preferredWidth: HeaderIconFrameWidth,
                preferredHeight: HeaderIconFrameHeight,
                flexibleWidth: 0f,
                flexibleHeight: 0f);

            Transform iconParent = iconRootRect;

            if (showFrame)
            {
                Color frameColor = isLocked ? LockedFrameColor : HeaderFrameBorderColor;

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

                backgroundImage = frameBackgroundRect.GetComponent<Image>();
                backgroundImage.color = Color.black;
                backgroundImage.raycastTarget = false;

                iconParent = frameBackgroundRect;
            }
            else
            {
                var iconBackgroundRect = UIBuilder.CreateChildRectTransform(iconRootRect,
                    "IconBackground",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    sizeDelta: new Vector2(HeaderIconWidth, HeaderIconHeight),
                    components: typeof(Image));

                backgroundImage = iconBackgroundRect.GetComponent<Image>();
                backgroundImage.color = Color.black;
                backgroundImage.raycastTarget = false;

                iconParent = iconBackgroundRect;
            }

            var iconRect = UIBuilder.CreateChildRectTransform(iconParent,
                "Icon",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
               sizeDelta: new Vector2(HeaderIconWidth, HeaderIconHeight),
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
