using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

namespace TFTV
{
    internal class TFTVUIGeoMap
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color blue = new Color32(62, 12, 224, 255);
        internal static Color green = new Color32(12, 224, 30, 255);
        internal static Color anu = new Color(0.9490196f, 0.0f, 1.0f, 1.0f);
        internal static Color nj = new Color(0.156862751f, 0.6156863f, 1.0f, 1.0f);
        internal static Color syn = new Color(0.160784319f, 0.8862745f, 0.145098045f, 1.0f);


        internal class Miscelaneous
        {
            [HarmonyPatch(typeof(UIModuleCorruptionReport), "Init")]
            public static class UIModuleCorruptionReport_Init_patch
            {
                public static bool Prefix(UIModuleCorruptionReport __instance, GeoscapeViewContext context)
                {
                    try
                    {
                        return false;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal class UnpoweredFacilitiesInfo
        {

            private static readonly Sprite _lightningSprite = Helper.CreateSpriteFromImageFile("Power.png");
            private static readonly Sprite _attackSprite = Helper.CreateSpriteFromImageFile("TBTV_MarkForDeath.png");


            /// <summary>
            /// first transform is the resource, second transform is the lightning
            /// </summary>
            private static Dictionary<Transform, List<GeoPhoenixFacility>> _resourcePowerWarnings = new Dictionary<Transform, List<GeoPhoenixFacility>>();
            private static Dictionary<Transform, UITooltipText> _savedTooltips = new Dictionary<Transform, UITooltipText>();

          

            public static void ClearInternalDataForUIGeo()
            {
                try
                {
                    _resourcePowerWarnings.Clear();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void RemoveFacilityFromList(GeoPhoenixFacility facility)
            {
                try
                {
                    foreach (Transform transform in _resourcePowerWarnings.Keys)
                    {

                        if (_resourcePowerWarnings[transform].Contains(facility))
                        {
                            _resourcePowerWarnings[transform].Remove(facility);

                            // TFTVLogger.Always($"found previosuly unpowered facility! {geoPhoenixFacility.Def.name} at {site.LocalizedSiteName}. Facilities count now: {_resourcePowerWarnings[transform][transformResource].Count}");

                            if (_resourcePowerWarnings[transform].Count == 0)
                            {
                                // TFTVLogger.Always($"colors should reset");
                                UIColorController uIColorController = transform.GetComponentInChildren<UIColorController>();

                                if (uIColorController != null)
                                {
                                    uIColorController.gameObject.GetComponent<Text>().color = Color.white;
                                }
                                else
                                {
                                    if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("VehicleBay_PhoenixFacilityDef"))
                                    {
                                        UIModuleInfoBar uIModuleInfoBar = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ResourcesModule;

                                        ResourceIconContainer resourceIconContainer1 = uIModuleInfoBar.AirVehiclesLabel.transform.parent.GetComponent<ResourceIconContainer>();
                                        Transform transformIconContainer1 = resourceIconContainer1.transform;
                                        resourceIconContainer1.Value.color = Color.white;

                                        ResourceIconContainer resourceIconContainer2 = uIModuleInfoBar.GroundVehiclesLabel.transform.parent.GetComponent<ResourceIconContainer>();
                                        Transform transformIconContainerGroundVehicles = resourceIconContainer2.transform;
                                        resourceIconContainer2.Value.color = Color.white;
                                    }
                                    else
                                    {

                                        ResourceIconContainer resourceIconContainer = transform.GetComponent<ResourceIconContainer>() ?? transform.GetComponentInParent<ResourceIconContainer>();
                                        resourceIconContainer.Value.color = resourceIconContainer.DefaultColor;

                                    }
                                }

                                if (_savedTooltips.ContainsKey(transform))
                                {
                                    if (_savedTooltips[transform] != null)
                                    {
                                        _savedTooltips[transform].enabled = false;
                                    }
                                    _savedTooltips.Remove(transform);
                                }

                            }

                            break;
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void CheckTopBarTooltip(UITooltip tooltip, GameObject parent)
            {
                try
                {

                   // TFTVLogger.Always($"{parent.name}");

                    if (parent!=null && parent.name!=null && _savedTooltips!=null && _savedTooltips.Count > 0 && _savedTooltips.Values.Any(k => k.name == parent.name))
                    {
                        RectTransform rectTransform = tooltip.gameObject.GetComponent<RectTransform>();
                     //   TFTVLogger.Always($"found {tooltip.name} pivot: {rectTransform.pivot}");

                        rectTransform.pivot = new Vector2(0.5f, 1);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static void AddClickChangeTooltipAndColor(Transform label, GeoPhoenixFacility geoPhoenixFacility, Transform separateTextHook = null)
            {
                try
                {
                    GeoLevelController geoLevelController = geoPhoenixFacility.PxBase.Site.GeoLevel;

                    Transform transformHook = separateTextHook ?? label;

                    if (!_resourcePowerWarnings.ContainsKey(transformHook))
                    {

                        List<GeoPhoenixFacility> geoPhoenixFacilities = new List<GeoPhoenixFacility>() { geoPhoenixFacility };

                        _resourcePowerWarnings.Add(transformHook, geoPhoenixFacilities);

                        GameObject go = label.gameObject;

                        if (!go.GetComponent<EventTrigger>())
                        {
                            go.AddComponent<EventTrigger>();
                        }


                        EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                        eventTrigger.triggers.Clear();
                        EventTrigger.Entry click = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerClick
                        };

                        click.callback.AddListener((eventData) =>
                        {
                            geoLevelController.Timing.Paused = true;

                            GeoPhoenixFacility currentFacility = _resourcePowerWarnings[transformHook].FirstOrDefault();
                            if (currentFacility.PxBase.Site.Owner != geoLevelController.PhoenixFaction)
                            {
                                RemoveFacilityFromList(currentFacility);
                                currentFacility = _resourcePowerWarnings[transformHook].FirstOrDefault();
                            }

                            if (currentFacility != null)
                            {
                                geoLevelController.View.ToBaseLayoutState(currentFacility.PxBase);
                                _resourcePowerWarnings[transformHook].Remove(currentFacility);
                                _resourcePowerWarnings[transformHook].Add(currentFacility);
                            }
                        });

                        eventTrigger.triggers.Add(click);

                        string facilityType = geoPhoenixFacility.ViewElementDef.DisplayName1.Localize();

                        if (geoPhoenixFacility.Def == DefCache.GetDef<PhoenixFacilityDef>("ResearchLab_PhoenixFacilityDef") ||
                        geoPhoenixFacility.Def == DefCache.GetDef<PhoenixFacilityDef>("BionicsLab_PhoenixFacilityDef"))
                        {
                            facilityType = TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_UNPOWERED_MARKER_RESEARCH");
                        }

                        string tip = $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_UNPOWERED_MARKER_TOOLTIP").Replace("{0}", facilityType)}";

                        UITooltipText uITooltipText = label.gameObject.GetComponent<UITooltipText>() ?? label.gameObject.AddComponent<UITooltipText>();

                        uITooltipText.name = $"ToolTip_{facilityType}";

                        if (!_savedTooltips.ContainsKey(transformHook))
                        {
                            _savedTooltips.Add(transformHook, uITooltipText);
                        }

                        uITooltipText.TipText = tip;
                        uITooltipText.Position = UITooltip.Position.Bottom;
                        uITooltipText.enabled = true;
                    }
                    else
                    {
                        if (!_resourcePowerWarnings[transformHook].Contains(geoPhoenixFacility))
                        {
                            _resourcePowerWarnings[transformHook].Add(geoPhoenixFacility);
                        }
                    }

                    // TFTVLogger.Always($"should activate lightning {site.LocalizedSiteName}");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void DisplayWarningInfoBar(GeoLevelController controller, GeoPhoenixFacility facility)
            {
                try
                {
                    UIModuleInfoBar uIModuleInfoBar = controller.View.GeoscapeModules.ResourcesModule;
                    UIColorDef warningColor = DefCache.GetDef<UIColorDef>("UIColorDef_UIColor_Warning");

                    if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("ResearchLab_PhoenixFacilityDef") ||
                        facility.Def == DefCache.GetDef<PhoenixFacilityDef>("BionicsLab_PhoenixFacilityDef"))
                    {

                        UIColorController uIColorController = uIModuleInfoBar.ResearchLabel.GetComponentInChildren<UIColorController>();

                        uIColorController.gameObject.GetComponent<Text>().color = warningColor.Color;

                        /*  if (uIColorController != null)
                          {
                              uIColorController.SecondaryColorDef = warningColor;
                              uIColorController.UseSecondaryColor = true;
                              uIColorController.WarningActive = false;
                              uIColorController.UpdateColor();
                          }*/


                        AddClickChangeTooltipAndColor(uIModuleInfoBar.ResearchRoot.transform, facility, uIModuleInfoBar.ResearchLabel.transform);
                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("FabricationPlant_PhoenixFacilityDef"))
                    {
                        UIColorController uIColorController = uIModuleInfoBar.ProductionLabel.GetComponentInChildren<UIColorController>();
                        uIColorController.gameObject.GetComponent<Text>().color = warningColor.Color;

                        /*   if (uIColorController != null)
                           {
                               uIColorController.SecondaryColorDef = warningColor;
                               uIColorController.UseSecondaryColor = true;
                               uIColorController.WarningActive = false;
                               uIColorController.UpdateColor();
                           }*/
                        AddClickChangeTooltipAndColor(uIModuleInfoBar.ProductionRoot.transform, facility, uIModuleInfoBar.ProductionLabel.transform);

                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("LivingQuarters_PhoenixFacilityDef"))
                    {
                        ResourceIconContainer resourceIconContainer = uIModuleInfoBar.SoldiersLabel.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainer = resourceIconContainer.transform;
                        resourceIconContainer.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainer, facility, uIModuleInfoBar.SoldiersLabel.transform);

                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("VehicleBay_PhoenixFacilityDef"))
                    {

                        ResourceIconContainer resourceIconContainerAirVehicles = uIModuleInfoBar.AirVehiclesLabel.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainerAirVehicles = resourceIconContainerAirVehicles.transform;
                        resourceIconContainerAirVehicles.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainerAirVehicles, facility, uIModuleInfoBar.AirVehiclesLabel.transform);

                        ResourceIconContainer resourceIconContainerGroundVehicles = uIModuleInfoBar.GroundVehiclesLabel.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainerGroundVehicles = resourceIconContainerGroundVehicles.transform;
                        resourceIconContainerGroundVehicles.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainerGroundVehicles, facility, uIModuleInfoBar.GroundVehiclesLabel.transform);

                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("AlienContainment_PhoenixFacilityDef"))
                    {
                        ResourceIconContainer resourceIconContainer = uIModuleInfoBar.AliensContainedLabel.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainer = resourceIconContainer.transform;
                        resourceIconContainer.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainer, facility, uIModuleInfoBar.AliensContainedLabel.transform);
                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("MutationLab_PhoenixFacilityDef"))
                    {

                        ResourceIconContainer resourceIconContainer = uIModuleInfoBar.MutagensController.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainer = resourceIconContainer.transform;
                        resourceIconContainer.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainer, facility, uIModuleInfoBar.MutagensController.transform);

                    }
                    else if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("FoodProduction_PhoenixFacilityDef"))
                    {
                        ResourceIconContainer resourceIconContainer = uIModuleInfoBar.FoodController.transform.parent.GetComponent<ResourceIconContainer>();
                        Transform transformIconContainer = resourceIconContainer.transform;
                        resourceIconContainer.Value.color = warningColor.Color;

                        AddClickChangeTooltipAndColor(transformIconContainer, facility, uIModuleInfoBar.FoodController.transform);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static void AddBlinkingPowerMarkerGeoMap(GeoSiteVisualsController geoSiteVisualsController, GeoSite site)
            {
                try
                {
                    if (site.GeoLevel != null && site.Type == GeoSiteType.PhoenixBase)
                    {
                        if (site.GetComponent<GeoPhoenixBase>() == null || site.GetComponent<GeoPhoenixBase>().Layout == null || site.GetComponent<GeoPhoenixBase>().Layout.Facilities == null)
                        {
                            return;
                        }

                        Transform lightningTransform = geoSiteVisualsController.CanvasIcons.transform.Find("LightningImage");

                        if (site.Owner != site.GeoLevel.PhoenixFaction)
                        {
                            lightningTransform?.gameObject?.SetActive(false);
                            return;
                        }

                        bool baseHasUnpoweredFacilities = false;


                        foreach (GeoPhoenixFacility geoPhoenixFacility in
                            site.GetComponent<GeoPhoenixBase>().Layout.Facilities.Where
                            (f => f.Def.PowerCost > 0 && 
                            f.State != GeoPhoenixFacility.FacilityState.UnderContstruction && 
                            f.State != GeoPhoenixFacility.FacilityState.Damaged &&
                            f.State != GeoPhoenixFacility.FacilityState.Repairing &&
                            !f.IsAvailableForRepair))
                        {

                            bool isPowered = geoPhoenixFacility.IsPowered; //(bool)isPoweredField.GetValue(geoPhoenixFacility);

                            //  TFTVLogger.Always($"looking at facility {geoPhoenixFacility.Def.name} at {geoPhoenixFacility.PxBase.Site.LocalizedSiteName}, powered: {isPowered} state: {geoPhoenixFacility.State} ");

                            if (isPowered)
                            {
                                RemoveFacilityFromList(geoPhoenixFacility);

                                continue;
                            }

                            //   TFTVLogger.Always($"found unpowered facility {geoPhoenixFacility.Def.name} at {geoPhoenixFacility.PxBase.Site.LocalizedSiteName}!", false);

                            if (lightningTransform == null)
                            {
                                //   TFTVLogger.Always($"got here6");

                                // Create a new GameObject to hold the Image component
                                GameObject lightningImageObject = new GameObject("LightningImage");
                                lightningImageObject.transform.SetParent(geoSiteVisualsController.CanvasIcons.transform, false);

                                // Add an Image component to display the lightning sprite
                                Image lightningImage = lightningImageObject.AddComponent<Image>();


                                // Load the lightning sprite using your helper method

                                lightningImage.sprite = _lightningSprite;
                                //  lightningImage.color = Color.yellow;

                                // Optionally set the size and position of the image relative to the facility
                                RectTransform rectTransform = lightningImageObject.GetComponent<RectTransform>();
                                rectTransform.localScale = new Vector3(0.75f, 0.75f, 0.75f); // Adjust the size if necessary
                                rectTransform.anchoredPosition += new Vector2(10, 50);//Vector2.zero; // Position it over the facility
                                lightningTransform = lightningImageObject.transform;

                            }

                            //  TFTVLogger.Always($"should activate lightning {site.LocalizedSiteName}");
                            lightningTransform.gameObject.SetActive(true);

                            if (site.Owner == site.GeoLevel.PhoenixFaction && !TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(site.SiteId))
                            {
                                // TFTVLogger.Always($"adding {geoPhoenixFacility.Def.name} at {site.LocalizedSiteName}");
                                DisplayWarningInfoBar(site.GeoLevel, geoPhoenixFacility);
                            }

                            if (!baseHasUnpoweredFacilities && !TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(site.SiteId))
                            {
                                geoSiteVisualsController.StopAllCoroutines();

                                geoSiteVisualsController.StartCoroutine(BlinkLightning(lightningTransform.gameObject));
                                baseHasUnpoweredFacilities = true;
                            }

                        }

                        if (!baseHasUnpoweredFacilities && lightningTransform != null)
                        {

                            //   TFTVLogger.Always($"should disable lightning for {site.LocalizedSiteName}");
                            lightningTransform.gameObject.SetActive(false);
                            geoSiteVisualsController.StopAllCoroutines();
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static Transform CreateInBaseMarker(Transform transformParent, string name, Sprite picture, float scale = 1)
            {
                try
                {
                    GameObject imageObject = new GameObject(name);
                    imageObject.transform.SetParent(transformParent, false);
                    Image image = imageObject.AddComponent<Image>();
                    image.sprite = picture;
                    // Optionally set the size and position of the image relative to the facili              
                    RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
                    rectTransform.localScale = new Vector3(scale, scale, scale); // Adjust the size if necessary
                    rectTransform.anchoredPosition = Vector2.zero; // Position it over the facility

                    return imageObject.transform;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void DisplayRepairCost(PhoenixFacilityController facilityController)
            {
                try
                {
                    facilityController.transform.Find("RepairCostDisplay")?.gameObject?.SetActive(false);

                    if (facilityController.Facility != null && facilityController.Facility.IsAvailableForRepair
                        && facilityController.PowerButton != null && facilityController.PowerButton.enabled)
                    {
                        UIModuleInfoBar uIModuleInfoBar = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ResourcesModule;

                        PhoenixGeneralButton repairButton = facilityController.RepairButton;
                        GeoPhoenixFacility facility = facilityController.Facility;
                        ResourcePack repairCost = facility.GetRepairCost();
                        Wallet wallet = facility.PxBase.Site.Owner.Wallet;
                        bool isValidTech = wallet.HasResources(repairCost.ByResourceType(ResourceType.Tech));
                        bool isValidMaterials = wallet.HasResources(repairCost.ByResourceType(ResourceType.Materials));

                        // Get a container (e.g., panel) to hold the repair cost display under the repair button, but outside the button itself
                        Transform parentContainer = facilityController.transform; // Adjust if there's a more suitable container in the UI
                        Transform existingRepairCostDisplay = parentContainer.Find("RepairCostDisplay");
                        GameObject repairCostDisplay;

                        if (existingRepairCostDisplay == null)
                        {
                            // Create the RepairCostDisplay if it doesn't exist
                            repairCostDisplay = new GameObject("RepairCostDisplay");
                            repairCostDisplay.transform.SetParent(parentContainer, false);
                            Image background = repairCostDisplay.AddComponent<Image>();
                            background.color = new Color(0, 0, 0, 0.0f);
                            //  background.type = Image.Type.Sliced;  // Optionally make it sliced for better scaling
                            background.rectTransform.sizeDelta = new Vector2(350, 50);
                            RectTransform rectTransform = repairCostDisplay.GetComponent<RectTransform>();
                            //    rectTransform.localScale = new Vector3(scale, scale, scale); // Adjust the size if necessary
                            rectTransform.anchoredPosition = Vector2.zero - new Vector2(0, 120);
                            rectTransform.sizeDelta = new Vector2(350, 50);
                            // Position it over the facility
                            // Use VerticalLayoutGroup to display costs in a vertical arrangement
                            HorizontalLayoutGroup horizontalLayoutGroup0 = repairCostDisplay.AddComponent<HorizontalLayoutGroup>();
                            horizontalLayoutGroup0.childAlignment = TextAnchor.MiddleLeft;
                            horizontalLayoutGroup0.spacing = 0; // Adjust spacing between resource entries
                        }
                        else
                        {
                            // If it already exists, clear its content
                            existingRepairCostDisplay.gameObject.SetActive(true);
                            repairCostDisplay = existingRepairCostDisplay.gameObject;
                            foreach (Transform child in repairCostDisplay.transform)
                            {
                                GameObject.Destroy(child.gameObject); // Remove old resource displays
                            }
                        }

                        Text repairButtonText = repairButton.GetComponentInChildren<Text>();

                        foreach (ResourceUnit resourceUnit in repairCost)
                        {
                            if (resourceUnit.RoundedValue > 0)
                            {
                                string resourcesInfo = "";
                                ResourceType type = resourceUnit.Type;
                                bool isValid = (type == ResourceType.Materials) ? isValidMaterials : isValidTech;

                                // Create a horizontal layout to display both the icon and the text
                                GameObject resourceDisplay = new GameObject($"{type}CostDisplay");
                                resourceDisplay.transform.SetParent(repairCostDisplay.transform, false);
                                RectTransform resourceDislayRect = resourceDisplay.AddComponent<RectTransform>();
                                resourceDislayRect.sizeDelta = new Vector2(175, 50);
                                resourceDislayRect.anchoredPosition = Vector2.zero;
                                HorizontalLayoutGroup horizontalLayout = resourceDisplay.AddComponent<HorizontalLayoutGroup>();
                                horizontalLayout.spacing = 1; // Add spacing between icon and text
                                horizontalLayout.childAlignment = TextAnchor.MiddleCenter;

                                // Icon Image
                                GameObject iconObject = new GameObject($"{type}Icon");
                                iconObject.transform.SetParent(resourceDisplay.transform);
                                Image iconImage = iconObject.AddComponent<Image>();
                                Dictionary<Sprite, Color> keyValuePairs = GetResourceIcon(type);
                                iconImage.sprite = keyValuePairs.Keys.First();
                                iconImage.color = keyValuePairs.Values.First();
                                iconImage.preserveAspect = true;

                                // Resize the icon
                                RectTransform iconRectTransform = iconImage.GetComponent<RectTransform>();
                                iconRectTransform.sizeDelta = new Vector2(50, 50); // Set the width and height to 20x20, adjust as needed
                                                                                   //   iconImage.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                                // Text for resource cost using Unity's standard Text component
                                GameObject textObject = new GameObject($"{type}CostText");
                                textObject.transform.SetParent(resourceDisplay.transform);
                                Text resourceText = textObject.AddComponent<Text>();
                                resourcesInfo = resourceUnit.RoundedValue.ToString();
                                if (!isValid)
                                {
                                    resourcesInfo = $"<color=#FF0000>{resourcesInfo}</color>"; // Wrap the string with red color if not valid
                                }
                                resourceText.text = resourcesInfo;
                                resourceText.font = repairButtonText.font;
                                resourceText.color = Color.white;
                                resourceText.fontSize = 30;
                                resourceText.alignment = TextAnchor.MiddleLeft;
                                resourceText.transform.localScale = new Vector3(1, 1, 1);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            private static Dictionary<Sprite, Color> GetResourceIcon(ResourceType resourceType)
            {
                UIModuleInfoBar uIModuleInfoBar = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ResourcesModule;
                ResourceIconContainer resourceIconContainerMaterials = uIModuleInfoBar.MaterialsController.transform.parent.GetComponent<ResourceIconContainer>();
                ResourceIconContainer resourceIconContainerTech = uIModuleInfoBar.TechController.transform.parent.GetComponent<ResourceIconContainer>();
                Dictionary<Sprite, Color> iconColorPairs = new Dictionary<Sprite, Color>();

                // You can map the resourceType to corresponding icons
                switch (resourceType)
                {
                    case ResourceType.Materials:
                        iconColorPairs.Add(resourceIconContainerMaterials.Icon.sprite, resourceIconContainerMaterials.Icon.color);
                        break;
                    case ResourceType.Tech:
                        iconColorPairs.Add(resourceIconContainerTech.Icon.sprite, resourceIconContainerTech.Icon.color);
                        break;


                }

                return iconColorPairs;
            }



            public static void SetMarkerInBaseScreen(PhoenixFacilityController facilityController)
            {
                try
                {
                    DisplayRepairCost(facilityController);

                    bool flagCanbeUnpowered = facilityController.ContainedFacility != null 
                        && facilityController.ContainedFacility.Def.PowerCost > 0 
                        && facilityController.Facility.State != GeoPhoenixFacility.FacilityState.UnderContstruction
                        && !facilityController.Facility.IsAvailableForRepair
                        && !facilityController.Facility.IsRepairing;

                    string nameUnpowered = "LightningImage";
                    string nameUnderAttack = "AttackImage";


                    Transform lightningTransform = facilityController.transform.Find(nameUnpowered);
                    Transform attackTransform = facilityController.transform.Find(nameUnderAttack);


                    lightningTransform?.gameObject.SetActive(false);
                    attackTransform?.gameObject.SetActive(false);


                    if (facilityController.RepairBuildContainer.activeSelf &&
                        facilityController.RepairBuildSlidingContainer.gameObject.activeSelf &&
                        facilityController.RepairBuildSlidingContainer.offsetMin.y == -1f * facilityController.FacilityInfoContainer.rect.height)
                    {
                        facilityController.RepairBuildContainer.SetActive(false);
                        facilityController.RepairBuildSlidingContainer.gameObject.SetActive(false);
                    }

                    if (!flagCanbeUnpowered)
                    {
                        // Disable lightning if there's no facility or it has no power cost                       
                        facilityController?.Facility?.PxBase?.Site?.RefreshVisuals();
                        return;
                    }

                    GeoPhoenixFacility facility = facilityController.Facility;

                    bool isPowered = facility.IsPowered;
                    bool facilityPausedUnderAttack = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(facility.PxBase.Site.SiteId) &&
                        facility.GetComponent<SecurityStationFacilityComponent>() == null && facility.GetComponent<PrisonFacilityComponent>() == null;

                    if (!isPowered && !facilityPausedUnderAttack && lightningTransform == null)
                    {
                        lightningTransform = CreateInBaseMarker(facilityController.transform, nameUnpowered, _lightningSprite, 1.5f);
                        lightningTransform.gameObject.SetActive(false);
                    }

                    if (facilityPausedUnderAttack && attackTransform == null)
                    {
                        attackTransform = CreateInBaseMarker(facilityController.transform, nameUnderAttack, _attackSprite, 1.5f);
                        attackTransform.GetComponent<Image>().color = DefCache.GetDef<UIColorDef>("UIColorDef_UIColor_Warning").Color;
                        attackTransform.gameObject.SetActive(false);
                    }

                    if (!isPowered && !facilityPausedUnderAttack)
                    {
                        facilityController.RepairBuildContainer.SetActive(true);
                        facilityController.RepairBuildSlidingContainer.gameObject.SetActive(true);
                        facilityController.RepairBuildSlidingContainer.offsetMin = new Vector2(facilityController.RepairBuildSlidingContainer.offsetMin.x, -1f * facilityController.FacilityInfoContainer.rect.height);
                        // Activate the lightning image if not powered
                        lightningTransform.gameObject.SetActive(true);
                    }

                    if (facilityPausedUnderAttack)
                    {
                        attackTransform.gameObject.SetActive(true);
                        facilityController.PowerButton.SetInteractable(false);
                        facilityController.RepairBuildContainer.SetActive(true);
                        facilityController.RepairBuildSlidingContainer.gameObject.SetActive(true);
                        facilityController.RepairBuildSlidingContainer.offsetMin = new Vector2(facilityController.RepairBuildSlidingContainer.offsetMin.x, -1f * facilityController.FacilityInfoContainer.rect.height);
                        string tip = TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_UNPOWERED_UNDER_ATTACK_TOOLTIP");

                        attackTransform.gameObject.AddComponent<UITooltipText>().TipText = tip;
                        //  facilityController.CanTogglePower = false;
                    }
                    else
                    {
                        facilityController.PowerButton.SetInteractable(true);
                    }

                    facilityController.Facility.PxBase.Site.RefreshVisuals();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static IEnumerator BlinkLightning(GameObject lightningImageObject)
            {

                while (true)
                {
                    // TFTVLogger.Always($"coroutine started");
                    lightningImageObject.SetActive(!lightningImageObject.activeSelf); // Toggle the active state
                    yield return new WaitForSeconds(0.4f); // Wait for half a second before toggling again
                }

            }



            [HarmonyPatch(typeof(PhoenixFacilityController), "InitEmptyBlock")]
            public static class PhoenixFacilityController_InitEmptyBlockpatch
            {
                public static void Postfix(PhoenixFacilityController __instance)
                {
                    try
                    {
                        SetMarkerInBaseScreen(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(PhoenixFacilityController), "InitRockBlock")]
            public static class PhoenixFacilityController_UInitRockBlockpatch
            {
                public static void Postfix(PhoenixFacilityController __instance)
                {
                    try
                    {
                        SetMarkerInBaseScreen(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(PhoenixFacilityController), "UpdatePowerState")]
            public static class PhoenixFacilityController_UpdatePowerStatepatch
            {
                public static void Postfix(PhoenixFacilityController __instance)
                {
                    try
                    {
                        SetMarkerInBaseScreen(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(PhoenixFacilityController), "RefreshFacilityState")]
            public static class PhoenixFacilityController_RefreshFacilityState_patch
            {
                public static void Postfix(PhoenixFacilityController __instance)
                {
                    try
                    {
                        SetMarkerInBaseScreen(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


        }

        internal class TopInfoBar
        {

            [HarmonyPatch(typeof(UIAnimatedResourceController), "DisplayValue")]
            public static class UIAnimatedResourceController_DisplayValue_patch
            {
                public static bool Prefix(UIAnimatedResourceController __instance, ref Text ____text)
                {
                    try
                    {
                        //  TFTVLogger.Always($"Looking at {__instance.name}. Parent transform? {__instance.transform?.parent?.name}");

                        if (__instance.transform.parent.name.Equals("MutagenRes") || __instance.transform.parent.name.Equals("FoodRes"))
                        {
                            return true;
                        }


                        if (____text == null)
                        {
                            ____text = __instance.GetComponent<Text>();
                        }

                        char c = ((__instance.Income >= 0) ? '+' : '-');

                        if (__instance.Income == 0)
                        {
                            ____text.text = $"{__instance.DisplayedValue.ToString()}";
                        }
                        else
                        {
                            ____text.text = $"{__instance.DisplayedValue.ToString()} [{c}{Math.Abs(__instance.Income)}]";
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateContainedAliensData")]
            public static class UIModuleInfoBar_UpdateContainedAliensData_patch
            {
                public static void Postfix(UIModuleInfoBar __instance, GeoscapeViewContext ____context, LayoutGroup ____layoutGroup)
                {
                    try
                    {
                        if (____context.ViewerFaction is GeoPhoenixFaction geoPhoenixFaction)
                        {
                            if (!__instance.AliensContainedLabel.transform.parent.gameObject.activeSelf)
                            {
                                if (geoPhoenixFaction.Bases.Any(b => b.Layout.Facilities.Any(f => !f.IsUnderConstruction
                                && !f.IsPowered && f.GetComponent<PrisonFacilityComponent>() != null)))
                                {
                                    __instance.AliensContainedLabel.transform.parent.gameObject.SetActive(true);
                                    ____layoutGroup.enabled = true;

                                }
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

            //Adapted from Mad´s Assorted Adjustments; this patch changes Geoescape UI
            internal static bool moduleInfoBarAdjustmentsExecuted = false;
            public static void AdjustInfoBarGeoscape(UIModuleInfoBar uIModuleInfoBar)
            {
                try
                {
                    if (moduleInfoBarAdjustmentsExecuted)
                    {
                        return;
                    }

                    Resolution resolution = Screen.currentResolution;

                    // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;
                    //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                    // Declutter
                    Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;

                    //Use this to catch the ToolTip
                    Transform[] thingsToUse = new Transform[2];

                    uIModuleInfoBar.PopulationTooltip.enabled = false;

                    foreach (Transform t in tInfoBar.GetComponentsInChildren<Transform>())
                    {
                        if (t.GetComponent<Text>() != null)
                        {
                            t.GetComponent<Text>().color = Color.white;
                        }


                        if (t.name == "TooltipCatcher")
                        {
                            if (t.GetComponent<UITooltipText>().TipKey.LocalizeEnglish() == "Stores - used space / capacity of all stores facilities")
                            {
                                thingsToUse[0] = t;
                            }
                        }

                        // Hide useless icons at production and research
                        if (t.name == "UI_Clock")
                        {
                            t.gameObject.SetActive(false);
                        }
                        //Add Delirium and Pandoran evolution icons, as well as factions icons.
                        if (t.name == "Requirement_Icon")
                        {
                            Image icon = t.gameObject.GetComponent<Image>();
                            if (icon.sprite.name == "Geoscape_UICanvasIcons_Actions_EditSquad")
                            {
                                icon.sprite = Helper.CreateSpriteFromImageFile("Void-04P.png");
                                t.gameObject.name = "DeliriumIcon";
                                t.parent = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");
                                t.Translate(new Vector3(30f * resolutionFactorWidth, 0f, 0f));
                                t.localScale = new Vector3(1.3f, 1.3f, 1f);
                                t.gameObject.SetActive(false);
                                //  icon.color = purple;

                                //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}");

                                Transform pandoranEvolution = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                pandoranEvolution.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                                pandoranEvolution.gameObject.GetComponent<Image>().color = red;
                                pandoranEvolution.gameObject.name = "PandoranEvolutionIcon";
                                // pandoranEvolution.localScale = new Vector3(0.9f, 0.9f, 1);
                                pandoranEvolution.Translate(new Vector3(110f * resolutionFactorWidth, 0f, 0f));
                                // pandoranEvolution.Translate(80f*resolutionFactor, 0f, 0f, t);
                                pandoranEvolution.gameObject.SetActive(false);


                                Transform anuDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                anuDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                anuDiploInfoIcon.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().color = anu;
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Anu.png");
                                anuDiploInfoIcon.gameObject.name = "AnuIcon";
                                anuDiploInfoIcon.gameObject.SetActive(false);

                                Transform njDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                njDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                njDiploInfoIcon.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                                njDiploInfoIcon.gameObject.GetComponent<Image>().color = nj;
                                njDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_NewJericho.png");
                                njDiploInfoIcon.gameObject.name = "NJIcon";
                                njDiploInfoIcon.gameObject.SetActive(false);

                                Transform synDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                synDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                synDiploInfoIcon.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                                synDiploInfoIcon.gameObject.GetComponent<Image>().color = syn;
                                synDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Synedrion.png");
                                synDiploInfoIcon.gameObject.name = "SynIcon";
                                synDiploInfoIcon.gameObject.SetActive(false);
                                //  anuDiploInfo.gameObject.GetComponent<Image>().color = red;

                            }

                            // t.name = "ODI_icon";
                            // TFTVLogger.Always("Req_Icon name is " + icon.sprite.name);
                        }

                        if (t.name == "UI_underlight")
                        {
                            if (t.parent.name == "StoresRes")
                            {
                                thingsToUse[1] = t;
                            }


                            // TFTVLogger.Always("Parent of UI_underlight " + t.parent.name);


                            // separator.position = anuDiploInfoIcon.position - new Vector3(-100, 0, 0);
                        }

                        //Create separators to hold Delirium and Pandoran Evolution icons
                        if (t.name == "Separator")
                        {
                            Transform separator = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator.Translate(new Vector3(0f, 12f * resolutionFactorHeight, 0f));
                            separator.gameObject.name = "ODISeparator1";
                            separator.gameObject.SetActive(false);
                            // separator.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                            Transform separator2 = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator2.Translate(new Vector3(180f * resolutionFactorWidth, 12f * resolutionFactorHeight, 0f));
                            separator2.gameObject.name = "ODISeparator2";
                            separator2.gameObject.SetActive(false);
                            //  separator2.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                        }
                        // Remove skull icon
                        if (t.name == "skull")
                        {
                            t.gameObject.SetActive(false);
                        }

                        // Removed tiled gameover bar
                        if (t.name == "tiled_gameover")
                        {

                            t.gameObject.SetActive(false);
                        }

                        //Remove other bits and pieces of doomsday clock
                        if (t.name == "alive_mask" || t.name == "alive_animation" ||
                            t.name.Contains("alive_animated") || t.name == "dead" || t.name.Contains("death"))
                        {

                            t.gameObject.SetActive(false);
                        }

                        //    TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " root position " + "x: " + t.root.position.x);
                        //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " right " + "x: " + t.right.x);

                    }



                    Transform deliriumTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Delirium tooltip";
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    deliriumTooltip.gameObject.name = "DeliriumTooltip";
                    deliriumTooltip.gameObject.SetActive(false);
                    //TFTVLogger.Always("");

                    Transform evolutionTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Pandoran Evolution tooltip";
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    evolutionTooltip.gameObject.name = "PandoranEvolutionTooltip";
                    evolutionTooltip.gameObject.SetActive(false);

                    Transform anuTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuIcon"));
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Anu tooltip";
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    anuTooltip.gameObject.name = "AnuTooltip";
                    anuTooltip.gameObject.SetActive(false);

                    Transform njTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NJIcon"));
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipText = "nj tooltip";
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    njTooltip.gameObject.name = "NJTooltip";
                    njTooltip.gameObject.SetActive(false);

                    Transform synTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynIcon"));
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipText = "syn tooltip";
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    synTooltip.gameObject.name = "SynTooltip";
                    synTooltip.gameObject.SetActive(false);


                    //Create percentages next to each faction icon

                    Transform anuDiploInfo = UnityEngine.Object.Instantiate(uIModuleInfoBar.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    anuDiploInfo.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                    anuDiploInfo.gameObject.name = "AnuPercentage";
                    anuDiploInfo.gameObject.SetActive(false);
                    // anuDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    // anuDiploInfo.gameObject.SetActive(false);

                    Transform njDiploInfo = UnityEngine.Object.Instantiate(uIModuleInfoBar.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    njDiploInfo.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                    njDiploInfo.gameObject.name = "NjPercentage";
                    njDiploInfo.gameObject.SetActive(false);
                    njDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploInfo = UnityEngine.Object.Instantiate(uIModuleInfoBar.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    synDiploInfo.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                    synDiploInfo.gameObject.name = "SynPercentage";
                    synDiploInfo.gameObject.SetActive(false);
                    //   synDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    //Create highlights for new elements

                    Transform deliriumIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    deliriumIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform PandoranEvolutionIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    PandoranEvolutionIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    PandoranEvolutionIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform anuDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuPercentage"));
                    anuDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));


                    Transform njDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NjPercentage"));
                    njDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // njDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynPercentage"));
                    synDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // synDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    uIModuleInfoBar.PopulationPercentageText.gameObject.SetActive(false);

                    // Set a flag so that this whole stuff is only done ONCE
                    // Otherwise the visual transformations are repeated everytime leading to weird results
                    // This is reset on every level change (see below)
                    moduleInfoBarAdjustmentsExecuted = true;







                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            //Patch to ensure that patch above is only run once
            [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
            public static class PhoenixGame_RunGameLevel_Patch
            {
                public static void Prefix()
                {
                    moduleInfoBarAdjustmentsExecuted = false;
                }
            }

            //Second patch to update Geoscape UI
            [HarmonyPatch(typeof(UIModuleInfoBar), "UpdatePopulation")]
            public static class TFTV_ODI_meter_patch
            {
                public static void Postfix(UIModuleInfoBar __instance, GeoscapeViewContext ____context, LayoutGroup ____layoutGroup)
                {

                    try
                    {
                        //  TFTVLogger.Always("Running UpdatePopulation");

                        GeoLevelController controller = ____context.Level;

                        List<GeoAlienBase> listOfAlienBases = controller.AlienFaction.Bases.ToList();

                        int nests = 0;
                        int lairs = 0;
                        int citadels = 0;


                        foreach (GeoAlienBase alienBase in listOfAlienBases)
                        {
                            if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef")))
                            {
                                nests++;
                            }
                            else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef")))
                            {
                                lairs++;
                            }
                            else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef")))
                            {
                                citadels++;
                            }

                        }


                        int pEPerDay = nests + lairs * 2 + citadels * 3 + controller.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 2;
                        //max, not counting IH, is 3 + 6 + 9 = 18
                        //>=66%, evo high, so 12+
                        //<66% >33%, evo normal, 6+ 
                        //<33%, evo slow, else


                        Transform tInfoBar = __instance.PopulationBarRoot.transform.parent?.transform;
                        Transform populationBar = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");

                        //     TFTVLogger.Always("");


                        Transform anuInfo = populationBar.GetComponent<Transform>().Find("AnuPercentage");
                        anuInfo.gameObject.GetComponent<Text>().text = $"{____context.Level.AnuFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%";
                        //$"<color=#f200ff>{____context.Level.AnuFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";

                        Transform njInfo = populationBar.GetComponent<Transform>().Find("NjPercentage");
                        njInfo.gameObject.GetComponent<Text>().text = $"{____context.Level.NewJerichoFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%";//$"<color=#289eff>{____context.Level.NewJerichoFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";

                        Transform synInfo = populationBar.GetComponent<Transform>().Find("SynPercentage");
                        synInfo.gameObject.GetComponent<Text>().text = $"{____context.Level.SynedrionFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%";// $"<color=#28e225>{____context.Level.SynedrionFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";

                        Transform anuIcon = populationBar.GetComponent<Transform>().Find("AnuIcon");
                        Transform njIcon = populationBar.GetComponent<Transform>().Find("NJIcon");
                        Transform synIcon = populationBar.GetComponent<Transform>().Find("SynIcon");

                        //   TFTVLogger.Always(" 2");

                        Transform anuTooltip = populationBar.GetComponent<Transform>().Find("AnuIcon").GetComponent<Transform>().Find("AnuTooltip");
                        Transform njTooltip = populationBar.GetComponent<Transform>().Find("NJIcon").GetComponent<Transform>().Find("NJTooltip");
                        Transform synTooltip = populationBar.GetComponent<Transform>().Find("SynIcon").GetComponent<Transform>().Find("SynTooltip");


                        string anuToolTipText = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_NAME_ANU")}</b>";
                        string njToolTipText = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_NAME_NEW_JERICHO")}</b>";
                        string synToolTipText = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_NAME_SYNEDRION")}</b>";

                        if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("AN_Discovered_DiplomacyStateTagDef")))
                        {
                            anuInfo.gameObject.SetActive(true);
                            anuIcon.gameObject.SetActive(true);
                            anuTooltip.gameObject.SetActive(true);

                            anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = anuToolTipText + "\n" + CreateTextForAnuTooltipText(controller);

                        }

                        if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("NJ_Discovered_DiplomacyStateTagDef")))
                        {
                            njInfo.gameObject.SetActive(true);
                            njIcon.gameObject.SetActive(true);
                            njTooltip.gameObject.SetActive(true);

                            njTooltip.gameObject.GetComponent<UITooltipText>().TipText = njToolTipText + "\n" + CreateTextForNJTooltipText(controller);
                        }
                        if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("SY_Discovered_DiplomacyStateTagDef")))
                        {
                            synInfo.gameObject.SetActive(true);
                            synIcon.gameObject.SetActive(true);
                            synTooltip.gameObject.SetActive(true);

                            synTooltip.gameObject.GetComponent<UITooltipText>().TipText = synToolTipText + "\n" + CreateTextForSynTooltipText(controller);
                        }

                        //   TFTVLogger.Always(" 3");
                        Transform deliriumIconHolder = populationBar.GetComponent<Transform>().Find("DeliriumIcon");
                        Image deliriumIcon = deliriumIconHolder.GetComponent<Image>();
                        Transform separator = populationBar.GetComponent<Transform>().Find("ODISeparator1");

                        Transform separator2 = populationBar.GetComponent<Transform>().Find("ODISeparator2");

                        //    TFTVLogger.Always(" 4");




                        string deliriumToolTipText = "";
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __instance.PopulationBarRoot.gameObject.SetActive(true);
                            populationBar.gameObject.SetActive(true);
                            deliriumIconHolder.gameObject.SetActive(true);
                            deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04P.png");
                            deliriumToolTipText = $"<color=#ec9006><b>{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_MAX_TIP")}</b></color>";
                            separator.gameObject.SetActive(true);
                            separator2.gameObject.SetActive(true);
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            populationBar.gameObject.SetActive(true);
                            __instance.PopulationBarRoot.gameObject.SetActive(true);
                            deliriumIconHolder.gameObject.SetActive(true);
                            deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Phalf.png");
                            deliriumToolTipText = $"<color=#ec9006><b>{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_MED_TIP")}</b></color>";
                            separator.gameObject.SetActive(true);
                            separator2.gameObject.SetActive(true);
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0)
                        {
                            // TFTVLogger.Always("Got to SDI01");
                            deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Pthird.png");
                            populationBar.gameObject.SetActive(true);
                            __instance.PopulationBarRoot.gameObject.SetActive(true);
                            deliriumIconHolder.gameObject.SetActive(true);
                            deliriumToolTipText = $"<color=#ec9006><b>{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_LOW_TIP")}</b></color>";
                            separator.gameObject.SetActive(true);
                            separator2.gameObject.SetActive(true);
                        }

                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            deliriumToolTipText += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_VO_TIP")}";
                        }

                        if (controller.EventSystem.GetEventRecord("SDI_09")?.SelectedChoice == 0)
                        {
                            deliriumToolTipText += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_EVOLVED_UMBRA_TIP")}";
                        }
                        else if (controller.EventSystem.GetVariable("UmbraResearched") == 1)
                        {
                            deliriumToolTipText += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_UMBRA_TIP")}";
                        }
                        if (controller.EventSystem.GetEventRecord("SDI_07")?.SelectedChoice == 0)
                        {
                            deliriumToolTipText += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_MIST_INFESTATION_TIP")}";
                        }


                        Transform deliriumTooltip = populationBar.GetComponent<Transform>().Find("DeliriumIcon").GetComponent<Transform>().Find("DeliriumTooltip");
                        deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = deliriumToolTipText;
                        deliriumTooltip.gameObject.SetActive(true);
                        //TFTVLogger.Always("");




                        /* if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0 && controller.EventSystem.GetEventRecord("PROG_FS2_WIN")?.SelectedChoice == 0)
                         {
                             deliriumIconHolder.gameObject.SetActive(false);
                         }*/

                        Transform evolutionIconHolder = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon");
                        Image evolutionIcon = evolutionIconHolder.GetComponent<Image>();

                        Transform evolutionTooltip = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon").GetComponent<Transform>().Find("PandoranEvolutionTooltip");
                        string evolutionToolTipText = $"{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_PANDORAN_EVO_TIP0")} ";
                        if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_EvolvedAliens_ResearchDef"))
                        {
                            // TFTVLogger.Always(" 5");
                            evolutionIconHolder.gameObject.SetActive(true);
                            populationBar.gameObject.SetActive(true);
                            __instance.PopulationBarRoot.gameObject.SetActive(true);
                            evolutionTooltip.gameObject.SetActive(true);

                            if (pEPerDay >= 12)
                            {
                                evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_fast.png");
                                evolutionToolTipText += $"{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_PANDORAN_EVO_TIP1")}";
                            }
                            else if (pEPerDay >= 6)
                            {
                                evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_medium.png");
                                evolutionToolTipText += $"{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_PANDORAN_EVO_TIP2")}";
                            }
                            else
                            {
                                evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                                evolutionToolTipText += $"{TFTVCommonMethods.ConvertKeyToString("KEY_DELIRIUM_UI_PANDORAN_EVO_TIP3")}";
                            }
                        }

                        evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = evolutionToolTipText;


                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            public static string CreateTextForAnuTooltipText(GeoLevelController controller)
            {
                try
                {
                    string text = "";
                    GeoFaction phoenix = controller.PhoenixFaction;
                    PartyDiplomacyStateEntry relation = controller.AnuFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                    text = relation.StateText.Localize();


                    if (controller.EventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 2 || controller.EventSystem.GetEventRecord("PROG_AN6_2")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY_ALMOST")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALIGNED_ALMOST")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE_ALMOST")}";
                    }

                    if (controller.EventSystem.GetEventRecord("PROG_AN6_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_AN6_WIN2")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_AN4_WIN")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALIGNED")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_AN2_WIN")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE")}";
                    }


                    return text;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

            public static string CreateTextForSynTooltipText(GeoLevelController controller)
            {
                try
                {
                    string text = "";
                    GeoFaction phoenix = controller.PhoenixFaction;
                    PartyDiplomacyStateEntry relation = controller.SynedrionFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                    text = relation.StateText.Localize();
                    int polyCounter = controller.EventSystem.GetVariable("Polyphonic");
                    int terraCounter = controller.EventSystem.GetVariable("Terraformers");

                    if (controller.EventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1 || controller.EventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY_ALMOST")}";
                    }

                    else if (controller.EventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice == 2)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE_ALMOST")}";
                    }

                    if (controller.EventSystem.GetEventRecord("PROG_SY4_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_SY4_WIN2")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_SY3_WIN")?.SelectedChoice != null)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALIGNED")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_SY1_WIN1")?.SelectedChoice != null || controller.EventSystem.GetEventRecord("PROG_SY1_WIN2")?.SelectedChoice != null)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE")}";
                    }

                    if (polyCounter > terraCounter)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_POLY")}";

                    }
                    else if (polyCounter < terraCounter)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_TERRA")}";
                    }



                    return text;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

            public static string CreateTextForNJTooltipText(GeoLevelController controller)
            {
                try
                {
                    // TFTVLogger.Always($"Checking NJ Diplo status {controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice}");

                    string text = "";
                    GeoFaction phoenix = controller.PhoenixFaction;
                    PartyDiplomacyStateEntry relation = controller.NewJerichoFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                    text = relation.StateText.Localize();


                    if (controller.EventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY_ALMOST")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALIGNED_ALMOST")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE_ALMOST")}";
                    }

                    if (controller.EventSystem.GetEventRecord("PROG_NJ3_WIN")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALLY")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 1)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_ALIGNED")}";
                    }
                    else if (controller.EventSystem.GetEventRecord("PROG_NJ1_WIN")?.SelectedChoice == 0)
                    {
                        text += $"\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DIPLOMACY_TIP_SUPPORTIVE")}";
                    }


                    return text;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }
        }
    }
}
