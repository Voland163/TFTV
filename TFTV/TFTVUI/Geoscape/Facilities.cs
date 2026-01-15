using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
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

namespace TFTV.TFTVUI.Geoscape
{
    internal class Facilities
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


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
                _savedTooltips.Clear();
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
                foreach (Transform transform in _resourcePowerWarnings.Keys.ToList())
                {
                    if (transform == null)
                    {
                        if (_resourcePowerWarnings.Remove(transform))
                        {
                            TFTVLogger.Always("RemoveFacilityFromList: Removed stale resource warning entry with destroyed transform.");
                        }

                        if (_savedTooltips.TryGetValue(transform, out UITooltipText staleTooltip) && staleTooltip != null)
                        {
                            staleTooltip.enabled = false;
                        }
                        _savedTooltips.Remove(transform);

                        continue;
                    }

                    if (!_resourcePowerWarnings.TryGetValue(transform, out List<GeoPhoenixFacility> facilitiesForTransform) || !facilitiesForTransform.Contains(facility))
                    {
                        continue;
                    }

                    facilitiesForTransform.Remove(facility);

                    if (facilitiesForTransform.Count == 0)
                    {
                        UIColorController uIColorController = transform.GetComponentInChildren<UIColorController>();

                        if (uIColorController != null)
                        {
                            Text textComponent = uIColorController.gameObject.GetComponent<Text>();
                            if (textComponent != null)
                            {
                                textComponent.color = Color.white;
                            }
                        }
                        else
                        {
                            if (facility.Def == DefCache.GetDef<PhoenixFacilityDef>("VehicleBay_PhoenixFacilityDef"))
                            {
                                GeoLevelController geoLevelController = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                                UIModuleInfoBar resourcesModule = geoLevelController?.View?.GeoscapeModules?.ResourcesModule;

                                if (resourcesModule != null)
                                {
                                    ResourceIconContainer resourceIconContainer1 = resourcesModule.AirVehiclesLabel?.transform?.parent?.GetComponent<ResourceIconContainer>();
                                    if (resourceIconContainer1 != null)
                                    {
                                        resourceIconContainer1.Value.color = Color.white;
                                    }
                                    else
                                    {
                                        TFTVLogger.Always("RemoveFacilityFromList: Missing air vehicle ResourceIconContainer while resetting colors.");
                                    }

                                    ResourceIconContainer resourceIconContainer2 = resourcesModule.GroundVehiclesLabel?.transform?.parent?.GetComponent<ResourceIconContainer>();
                                    if (resourceIconContainer2 != null)
                                    {
                                        resourceIconContainer2.Value.color = Color.white;
                                    }
                                    else
                                    {
                                        TFTVLogger.Always("RemoveFacilityFromList: Missing ground vehicle ResourceIconContainer while resetting colors.");
                                    }
                                }
                                else
                                {
                                    TFTVLogger.Always("RemoveFacilityFromList: ResourcesModule unavailable when resetting vehicle bay colors.");
                                }
                            }
                            else
                            {
                                ResourceIconContainer resourceIconContainer = transform.GetComponent<ResourceIconContainer>() ?? transform.GetComponentInParent<ResourceIconContainer>();

                                if (resourceIconContainer == null)
                                {
                                    GeoLevelController geoLevelController = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                                    UIModuleInfoBar resourcesModule = geoLevelController?.View?.GeoscapeModules?.ResourcesModule;

                                    if (resourcesModule != null)
                                    {
                                        resourceIconContainer = resourcesModule.GetComponentsInChildren<ResourceIconContainer>(true)
                                            .FirstOrDefault(container => container.transform == transform || container.transform == transform.parent);
                                    }
                                    else
                                    {
                                        TFTVLogger.Always("RemoveFacilityFromList: ResourcesModule unavailable while searching for ResourceIconContainer.");
                                    }
                                }

                                if (resourceIconContainer != null)
                                {
                                    resourceIconContainer.Value.color = resourceIconContainer.DefaultColor;
                                }
                                else
                                {
                                    string transformName = transform != null ? transform.name : "<missing transform>";
                                    TFTVLogger.Always($"RemoveFacilityFromList: ResourceIconContainer missing for {transformName}, skipping color reset.");
                                }
                            }
                        }

                        if (_savedTooltips.TryGetValue(transform, out UITooltipText tooltip) && tooltip != null)
                        {
                            tooltip.enabled = false;
                        }
                        _savedTooltips.Remove(transform);

                        _resourcePowerWarnings.Remove(transform);
                    }

                    break;
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

                if (parent != null && parent.name != null && _savedTooltips != null && _savedTooltips.Count > 0 && _savedTooltips.Values.Any(k => k.name == parent.name))
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

        public static void CheckUnpoweredBasesOnGeoscapeStart()
        {
            try
            {
                if (_resourcePowerWarnings != null && _resourcePowerWarnings.Count > 0)
                {
                    foreach (Transform transform in _resourcePowerWarnings.Keys)
                    {
                        foreach (GeoPhoenixFacility geoPhoenixFacility in _resourcePowerWarnings[transform])
                        {
                            geoPhoenixFacility.PxBase.Site.RefreshVisuals();
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
                //  TFTVLogger.Always($"base: {facilityController?.Facility?.PxBase?.Site?.LocalizedSiteName} DisplayRepairCost running for {facilityController?.Facility?.Def?.name}, repair cost display null? " +
                //      $"{facilityController.transform.Find("RepairCostDisplay")==null}");

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
                        repairCostDisplay = new GameObject("RepairCostDisplay", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                        repairCostDisplay.transform.SetParent(parentContainer, false);
                        Image background = repairCostDisplay.GetComponent<Image>();
                        background.color = new Color(0, 0, 0, 0.8f);
                        //  background.type = Image.Type.Sliced;  // Optionally make it sliced for better scaling
                        //   background.rectTransform.sizeDelta = new Vector2(350, 100);
                        RectTransform rectTransform = repairCostDisplay.GetComponent<RectTransform>();
                        //    rectTransform.localScale = new Vector3(scale, scale, scale); // Adjust the size if necessary
                        rectTransform.anchoredPosition = Vector2.zero - new Vector2(0, 120);
                        rectTransform.sizeDelta = new Vector2(450, 100);
                        // Position it over the facility
                        // Use VerticalLayoutGroup to display costs in a vertical arrangement
                        HorizontalLayoutGroup horizontalLayoutGroup0 = repairCostDisplay.GetComponent<HorizontalLayoutGroup>();
                        horizontalLayoutGroup0.childScaleHeight = false;
                        horizontalLayoutGroup0.childScaleWidth = false;
                        horizontalLayoutGroup0.childControlHeight = false;
                        horizontalLayoutGroup0.childControlWidth = false;
                        horizontalLayoutGroup0.childAlignment = TextAnchor.MiddleCenter;
                        horizontalLayoutGroup0.spacing = 5; // Adjust spacing between resource entries*/
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
                            GameObject resourceDisplay = new GameObject($"{type}CostDisplay", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                            resourceDisplay.transform.SetParent(repairCostDisplay.transform, false);
                            RectTransform resourceDislayRect = resourceDisplay.GetComponent<RectTransform>();
                            resourceDislayRect.sizeDelta = new Vector2(225, 50);
                            resourceDislayRect.anchoredPosition = Vector2.zero;
                            HorizontalLayoutGroup horizontalLayout = resourceDisplay.GetComponent<HorizontalLayoutGroup>();
                            horizontalLayout.spacing = 1; // Add spacing between icon and text
                            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;

                            // Icon Image
                            GameObject iconObject = new GameObject($"{type}Icon", typeof(RectTransform), typeof(Image));
                            iconObject.transform.SetParent(resourceDisplay.transform);
                            Image iconImage = iconObject.GetComponent<Image>();
                            Dictionary<Sprite, Color> keyValuePairs = GetResourceIcon(type);
                            iconImage.sprite = keyValuePairs.Keys.First();
                            iconImage.color = keyValuePairs.Values.First();
                            iconImage.preserveAspect = true;

                            // Resize the icon
                            RectTransform iconRectTransform = iconImage.GetComponent<RectTransform>();
                            iconRectTransform.sizeDelta = new Vector2(20, 20); // Set the width and height to 20x20, adjust as needed
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
                            resourceText.horizontalOverflow = HorizontalWrapMode.Overflow;
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


        [HarmonyPatch(typeof(UIModuleBaseLayout), "SetupBaseLayout")] //VERIFIED
        public static class UIModuleBaseLayout_SetupBaseLayoutpatch
        {
            public static void Postfix(UIModuleBaseLayout __instance, PhoenixFacilityController[] ____slots)
            {
                try
                {
                    TFTVLogger.Always($"UIModuleBaseLayout setupbaselayout running for {__instance?.PxBase?.Site?.LocalizedSiteName}");

                    foreach (PhoenixFacilityController facilityController in ____slots)
                    {
                        if (facilityController != null && (facilityController.Facility == null || facilityController.Facility.PxBase != __instance.PxBase))
                        {
                            /*   TFTVLogger.Always($"{__instance?.PxBase?.Site?.LocalizedSiteName}, facility controller base: " +
                                   $"{facilityController?.Facility?.PxBase?.Site?.LocalizedSiteName} " +
                                   $"DisplayRepairCost running for {facilityController?.Facility?.Def?.name}, repair cost display null? " +
                      $"{facilityController.transform.Find("RepairCostDisplay") == null}");*/

                            facilityController.transform.Find("RepairCostDisplay")?.gameObject?.SetActive(false);
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


        [HarmonyPatch(typeof(PhoenixFacilityController), nameof(PhoenixFacilityController.InitEmptyBlock))]
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

        [HarmonyPatch(typeof(PhoenixFacilityController), nameof(PhoenixFacilityController.InitRockBlock))]
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


        [HarmonyPatch(typeof(PhoenixFacilityController), nameof(PhoenixFacilityController.UpdatePowerState))]
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

        [HarmonyPatch(typeof(PhoenixFacilityController), nameof(PhoenixFacilityController.RefreshFacilityState))]
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


}




