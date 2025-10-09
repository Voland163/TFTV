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

        internal static GameObject CreateCostRow(Transform parent, GeoHaven haven, GeoPhoenixFaction phoenix, RecruitCardView cardView, bool detailPanel=false)
        {

            EnsureResourceVisuals();

            var resourceCosts = HavenRecruitsUtils.GetRecruitCost(haven, phoenix);

            var (row, _) = RecruitOverlayManagerHelpers.NewUI("Row_Cost", parent);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleRight;
            h.spacing = 0f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.padding = new RectOffset(0, 0, 0, 0);

            if(detailPanel)
            {
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = false;

            }

            foreach (var type in _resourceDisplayOrder)
            {
                if (resourceCosts.TryGetValue(type, out var amount))
                {
                    CreateResourceChip(row.transform, type, amount, cardView);
                    resourceCosts.Remove(type);
                }
            }

            foreach (var kvp in resourceCosts)
            {
                CreateResourceChip(row.transform, kvp.Key, kvp.Value, cardView);
            }

            return row;

        }


        private static GameObject CreateResourceChip(Transform parent, ResourceType resourceType, int amount, RecruitCardView cardView)
        {
            if (amount <= 0)
            {
                return null;
            }


            var (chip, chipRT) = RecruitOverlayManagerHelpers.NewUI("Res", parent);

            var layout = chip.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 1f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            chipRT.pivot = new Vector2(0.5f, 0.5f);

            var chipLE = chip.AddComponent<LayoutElement>();
            float chipWidth = ResourceIconSize + 20f;
            chipLE.minWidth = chipWidth;
            chipLE.preferredWidth = chipWidth;
            chipLE.flexibleWidth = 0f;


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
                typeLabel.alignment = TextAnchor.MiddleCenter;

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
