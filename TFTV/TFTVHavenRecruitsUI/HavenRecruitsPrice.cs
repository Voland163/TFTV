using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HavenRecruitsUtils = TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;
using static TFTV.HavenRecruitsMain;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsPrice
    {
        internal sealed class ResourceVisual
        {
            public Sprite Icon;
            public Color Color;
        }

        internal static readonly Dictionary<ResourceType, ResourceVisual> _resourceVisuals = new Dictionary<ResourceType, ResourceVisual>();

        internal static readonly ResourceType[] _resourceDisplayOrder =
         {
                ResourceType.Tech,
                ResourceType.Materials,
                ResourceType.Supplies
            };

        private static void EnsureResourceVisuals()
        {
            if (_resourceVisuals.Count > 0)
            {
                return;
            }

            try
            {
                var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                var resourcesModule = geoLevel?.View?.GeoscapeModules?.ResourcesModule;
                if (resourcesModule == null)
                {
                    return;
                }

                AddResourceVisual(ResourceType.Materials, resourcesModule.MaterialsController);
                AddResourceVisual(ResourceType.Tech, resourcesModule.TechController);
                AddResourceVisual(ResourceType.Supplies, resourcesModule.FoodController);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

        }

        private static void AddResourceVisual(ResourceType resourceType, MonoBehaviour controller)
        {
            if (controller == null)
            {
                return;
            }

            var container = controller.transform.parent.GetComponent<ResourceIconContainer>();
            if (container?.Icon == null)
            {
                return;
            }
            _resourceVisuals[resourceType] = new ResourceVisual
            {
                Icon = container.Icon.sprite,
                Color = container.Icon.color
            };
        }

        internal static GameObject CreateCostRow(Transform parent, GeoHaven haven, GeoPhoenixFaction phoenix, RecruitCardView cardView, bool detailPanel = false)
        {
            EnsureResourceVisuals();

            var (row, _) = RecruitOverlayManagerHelpers.NewUI("Row_Cost", parent);

            PopulateCostRow(row.transform, haven, phoenix, cardView, detailPanel);

            return row;

        }

        internal static void PopulateCostRow(Transform row, GeoHaven haven, GeoPhoenixFaction phoenix, RecruitCardView cardView, bool detailPanel = false)
        {
            if (row == null)
            {
                return;
            }

            EnsureResourceVisuals();

            var resourceCosts = HavenRecruitsUtils.GetRecruitCost(haven, phoenix);

            if (resourceCosts == null)
            {
                return;
            }

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = detailPanel ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            layout.spacing = detailPanel ? 16f : 0f;
            layout.childControlWidth = detailPanel ? false : true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var layoutElement = row.GetComponent<LayoutElement>();
            if (!detailPanel)
            {
                if (layoutElement == null)
                {
                    layoutElement = row.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.minWidth = 0f;
                layoutElement.preferredWidth = 0f;
                layoutElement.flexibleWidth = 1f;
            }
            else if (layoutElement != null)
            {
                layoutElement.minWidth = 0f;
                layoutElement.preferredWidth = 0f;
                layoutElement.flexibleWidth = 0f;
            }

            RecruitOverlayManagerHelpers.ClearTransformChildren(row);

            foreach (var type in _resourceDisplayOrder)
            {
                if (resourceCosts.TryGetValue(type, out var orderedAmount))
                {
                    CreateResourceChip(row, type, orderedAmount, cardView, detailPanel);
                    resourceCosts.Remove(type);
                }
            }

            foreach (var kvp in resourceCosts)
            {
                CreateResourceChip(row, kvp.Key, kvp.Value, cardView, detailPanel);
            }

        }


        private static GameObject CreateResourceChip(Transform parent, ResourceType resourceType, int amount, RecruitCardView cardView, bool detailPanel)
        {
            if (amount <= 0)
            {
                return null;
            }


            var (chip, chipRT) = RecruitOverlayManagerHelpers.NewUI("Res", parent);

            if (detailPanel)
            {
                var layout = chip.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
            else
            {
                var layout = chip.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 1f;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            chipRT.pivot = new Vector2(0.5f, 0.5f);

            if (!detailPanel)
            {
                var chipLE = chip.AddComponent<LayoutElement>();
                float chipWidth = ResourceIconSize + 20f;
                chipLE.minWidth = chipWidth;
                chipLE.preferredWidth = chipWidth;
                chipLE.flexibleWidth = 0f;
            }



            Image img = null;
            if (_resourceVisuals.TryGetValue(resourceType, out var visual) && visual?.Icon != null)
            {
                img = RecruitOverlayManagerHelpers.MakeFixedIcon(chip.transform, visual.Icon, ResourceIconSize);
                img.color = visual.Color;
            }
            else
            {
                var (typeLabelGO, _) = RecruitOverlayManagerHelpers.NewUI("Type", chip.transform);
                var typeLabel = typeLabelGO.AddComponent<Text>();
                typeLabel.text = resourceType.ToString();
                typeLabel.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                typeLabel.fontSize = TextFontSize - 4;
                typeLabel.alignment = detailPanel ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

            }

            // amount
            var (txtGO, _) = RecruitOverlayManagerHelpers.NewUI("Amt", chip.transform);
            var t = txtGO.AddComponent<Text>();
            t.text = amount.ToString();
            t.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = TextFontSize - 2;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            cardView?.RegisterResourceAmount(t);

            return chip;
        }
    }
}
