using Base.Core;
using Base.Defs;
using Base.Lighting;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Levels.GeoSceneReferences;

namespace TFTV
{
    internal class TFTVBackgrounds
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        private static GameObject infoPanel;
        private static Text infoText;

        public static void RemoveContainmentInfoPanel()
        {
            try
            {
                if (infoPanel != null)
                {
                    UnityEngine.Object.Destroy(infoPanel);
                    infoPanel = null;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal class ContainmentScreen
        {
            [HarmonyPatch(typeof(UIStateRosterAliens), "OnActorCycled")] //VERIFIED
            public static class TFTV_UIStateRosterAliens_OnActorCycled_patch
            {
                public static void Postfix(UIStateRosterAliens __instance)
                {
                    try
                    {
                        GetInfoAboutAlien();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

                

            private static Font _cachedFont = null;

            private const float _verticalOffset = -60;

            private static void InitializeInfoPanel()
            {
                try
                {
                    if (infoPanel != null) return;

                    float offset = 0;
                    if (!TFTVNewGameOptions.LimitedHarvestingSetting) 
                    { 
                    offset = _verticalOffset;
                    }

                    infoPanel = new GameObject("InfoPanel");
                    Canvas canvas = infoPanel.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler canvasScaler = infoPanel.AddComponent<CanvasScaler>();
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    infoPanel.AddComponent<GraphicRaycaster>();

                    // Add a black background
                    GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
                    backgroundObject.transform.SetParent(infoPanel.transform);
                    Image backgroundImage = backgroundObject.AddComponent<Image>();
                    backgroundImage.color = new Color(0, 0, 0, 0.7f);
                    RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
                    backgroundRect.sizeDelta = new Vector2(230, 200);
                    backgroundRect.anchoredPosition = new Vector2(280, -30+offset);

                    GameObject descriptionObject = new GameObject("DescriptionText");
                    descriptionObject.transform.SetParent(backgroundObject.transform);
                    Text descriptionText = descriptionObject.AddComponent<Text>();
                    descriptionText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    descriptionText.fontSize = 10;
                    descriptionText.alignment = TextAnchor.UpperLeft;
                    descriptionText.color = Color.white;
                    descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                    RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
                    descriptionRect.sizeDelta = new Vector2(220, 100);
                    descriptionRect.anchoredPosition = new Vector2(0, 40);

                    GameObject volumeObject = new GameObject("VolumeText");
                    volumeObject.transform.SetParent(backgroundObject.transform);
                    Text volumeText = volumeObject.AddComponent<Text>();
                    volumeText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    volumeText.fontSize = 12;
                    volumeText.alignment = TextAnchor.UpperLeft;
                    volumeText.color = Color.white;
                    RectTransform volumeRect = volumeObject.GetComponent<RectTransform>();
                    volumeRect.sizeDelta = new Vector2(220, 30);
                    volumeRect.anchoredPosition = new Vector2(0, -40);

                    // Create icon object
                    GameObject iconObject = new GameObject("Icon");
                    iconObject.transform.SetParent(backgroundObject.transform);
                    Image iconImage = iconObject.AddComponent<Image>();
                    iconImage.preserveAspect = true;
                    RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(20, 20);
                    iconRect.anchoredPosition = new Vector2(-105, -55);

                    GameObject mutagenTextObject = new GameObject("MutagenText");
                    mutagenTextObject.transform.SetParent(backgroundObject.transform);
                    Text mutagenText = mutagenTextObject.AddComponent<Text>();
                    mutagenText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    mutagenText.fontSize = 12;
                    mutagenText.alignment = TextAnchor.UpperLeft;
                    mutagenText.color = Color.white;
                    RectTransform mutagenTextRect = mutagenTextObject.GetComponent<RectTransform>();
                    mutagenTextRect.sizeDelta = new Vector2(200, 30);
                    mutagenTextRect.anchoredPosition = new Vector2(5, -60);

                    // Create autopsied/vivisected text object
                    GameObject statusObject = new GameObject("StatusText");
                    statusObject.transform.SetParent(backgroundObject.transform);
                    Text statusText = statusObject.AddComponent<Text>();
                    statusText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    statusText.fontSize = 12;
                    statusText.alignment = TextAnchor.UpperLeft;
                    statusText.color = Color.white;
                    RectTransform statusRect = statusObject.GetComponent<RectTransform>();
                    statusRect.sizeDelta = new Vector2(220, 30);
                    statusRect.anchoredPosition = new Vector2(0, -90);

                    // Store references to the text components
                    infoText = descriptionText;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static int CalculateFontSize(string text)
            {
                TFTVLogger.Always($"length: {text.Length}");

                if (text.Length <= 200)
                {
                    return 12;
                }
                else if (text.Length <= 400)
                {
                    return 10;
                }
                else
                {
                    return 8;
                }
            }

            public static void GetInfoAboutAlien()
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    UIModuleActorCycle actorCycleModule = controller.View.GeoscapeModules.ActorCycleModule;
                    GeoUnitDescriptor current = actorCycleModule.GetCurrent<GeoUnitDescriptor>();

                    int volume = current.Volume;
                    float mutagenPerDay = (float)phoenixFaction.GetHarvestingUnitResourceAmount(current, ResourceType.Mutagen) / 10;
                    bool vivisected = false;
                    bool autopsied = false;
                    string description = "";

                    foreach (ResearchElement alnResearch in controller.AlienFaction.Research.FactionResearches)
                    {
                        if (alnResearch.ResearchDef.Unlocks.Any(u => u is UnitTemplateResearchRewardDef templateReward && templateReward.Template == current.UnitType.TemplateDef))
                        {
                            description = alnResearch.ResearchDef.ViewElementDef.CompleteText.Localize();
                        }
                    }

                    if(description=="" && current.UnitType.TemplateDef== DefCache.GetDef<TacCharacterDef>("AcidwormTest_AlienMutationVariationDef")) 
                    {
                        description = TFTVCommonMethods.ConvertKeyToString("ALN_ACIDWORM_RESEARCHDEF_COMPLETE");
                    }

                    foreach (TacticalFactionEffectDef buff in phoenixFaction.ActorModifierEffects)
                    {
                        if (buff.ActorEffectDef is TacStatusEffectDef tacStatusEffectDef && tacStatusEffectDef.StatusDef is DamageMultiplierStatusDef damageMultiplierStatusDef)
                        {
                            if (damageMultiplierStatusDef.OutgoingDamageTargetTags.Any(t => current.UnitType.TemplateDef.ClassTag == t))
                            {
                                vivisected = true;
                                autopsied = false;
                                break;
                            }
                        }
                    }

                    if (!autopsied)
                    {
                        foreach (ResearchElement researchElement in phoenixFaction.Research.Completed)
                        {
                            if (researchElement.GetRevealRequirements().Any(r => r is ActorResearchRequirement researchRequirement
                            && researchRequirement.RequirementDef is ActorResearchRequirementDef actorResearchRequirementDef
                            && actorResearchRequirementDef.Actor != null && actorResearchRequirementDef.Actor.GameTags != null && actorResearchRequirementDef.Actor.GameTags.Contains(current.UnitType.TemplateDef.ClassTag)))
                            {
                                autopsied = true;
                                break;
                            }
                        }
                    }

                    string info = $"{current.GetName()}, {description}\n volume: {volume}, mutagens per day: {mutagenPerDay}, vivisected: {vivisected}, autopsied {autopsied}";
                    TFTVLogger.Always(info);

                    // Initialize and update the info panel
                    InitializeInfoPanel();
                    //infoText.text = info;
                    infoText.fontSize = CalculateFontSize(description);


                    // Update the text components
                    infoPanel.transform.Find("Background").Find("DescriptionText").GetComponent<Text>().text = description;
                    infoPanel.transform.Find("Background").Find("VolumeText").GetComponent<Text>().text = $"Containment slots occupied: {volume}";
                    infoPanel.transform.Find("Background").Find("MutagenText").GetComponent<Text>().text = $"per day: {mutagenPerDay}";

                    // Update the status text
                    string status = "";
                    if (autopsied && !vivisected)
                    {
                        status = "AUTOPSIED";
                    }
                    else if (vivisected)
                    {
                        status = "VIVISECTED";
                    }
                    infoPanel.transform.Find("Background").Find("StatusText").GetComponent<Text>().text = status;

                    // Set the icon sprite (assuming you have a sprite for the icon)
                    Sprite iconSprite = DefCache.GetDef<ResourceViewElementDef>("MutagenResourceViewElementDef").Visual;
                    infoPanel.transform.Find("Background").Find("Icon").GetComponent<Image>().sprite = iconSprite;
                    infoPanel.transform.Find("Background").Find("Icon").GetComponent<Image>().color = DefCache.GetDef<UIColorsDef>("UIColors_MutagenCost_Def").PrimaryUIColor;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            [HarmonyPatch(typeof(GeoRosterItem))]
            [HarmonyPatch("Init", typeof(GeoUnitDescriptor), typeof(IGeoCharacterContainer), typeof(GeoFaction))] //VERIFIED
            public static class GeoRosterItemPatch
            {
                public static void Postfix(GeoRosterItem __instance, IGeoCharacterContainer characterContainer)
                {
                    try
                    {


                        // UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                        //  uIModuleGeneralPersonelRoster.RosterList.gameObject.SetActive(true);

                        RectTransform rectTransform = __instance.RowButton.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name.Contains("SlotContainer_Layout"));

                        if (rectTransform == null)
                        {
                            return;
                        }

                        GeoRosterAlienContainmentItem geoRosterAlienContainmentItem = __instance.RowButton.GetComponent<GeoRosterAlienContainmentItem>();

                        if (geoRosterAlienContainmentItem == null)
                        {
                            return;
                        }

                       // TFTVLogger.Always($"rectTransform.sizeDelta.x {rectTransform.sizeDelta.x}");
                        if (rectTransform.sizeDelta.x == 1250)
                        {
                            float sizeToCut = rectTransform.sizeDelta.x * 1 / 3;
                            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - sizeToCut, rectTransform.sizeDelta.y);


                            geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition =
                                new Vector2(geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition.x - sizeToCut, geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition.y);

                        }
                        if (_cachedFont == null)
                        {
                            _cachedFont = __instance.CharacterName.font;
                            // TFTVLogger.Always($"_cachedFont.name: {_cachedFont.name}");
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }


        internal class PersonnelRosterAdjustments 
        { 
         
         /*   private static Transform _rosterVehicles = null;



            private static Dictionary<GeoSite, List<GeoVehicle>> _sitesVehiclesDict = new Dictionary<GeoSite, List<GeoVehicle>>();
            private static List<GeoVehicle> _vehiclesInTransit = new List<GeoVehicle>();
            private static List<GeoSite> _garrisonPhoenixBases = new List<GeoSite>();


            private static void SortOutSitesAndVehicles(UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster)
            {
                try
                {
                   // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    Transform roster = uIModuleGeneralPersonelRoster.RosterList;

                   

                    foreach (GeoSite geoSite in _sitesVehiclesDict.Keys)
                    {
                        TFTVLogger.Always($"{geoSite?.LocalizedSiteName}");

                        GeoRosterContainterItem siteContainer = roster.GetComponentsInChildren<GeoRosterContainterItem>().FirstOrDefault(ci => ci.ContainerName.text == geoSite.LocalizedSiteName);

                        TFTVLogger.Always($"siteContainer: {siteContainer?.ContainerName.text}");

                       // Transform vehiclesTransform = UnityEngine.Object.Instantiate(siteContainer.transform, siteContainer.transform);
                       // vehiclesTransform.position = new Vector3(siteContainer.transform.position.x + 400, siteContainer.transform.position.y, siteContainer.transform.position.z);
                        foreach (GeoVehicle geoVehicle in _sitesVehiclesDict[geoSite])
                        {
                            TFTVLogger.Always($"{geoVehicle.Name}");

                            GeoRosterContainterItem vehicleContainer = roster.GetComponentsInChildren<GeoRosterContainterItem>().FirstOrDefault(ci => ci.ContainerName.text == geoVehicle.Name);
                            TFTVLogger.Always($"vehicleContainer: {vehicleContainer?.ContainerName.text}");
                            vehicleContainer.transform.SetParent(_rosterVehicles, true);
                            vehicleContainer.transform.position = new Vector3(siteContainer.transform.position.x + 400, siteContainer.transform.position.y, siteContainer.transform.position.z);

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }*/


         /*   [HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "InitGroupItem")]
            public static class UIModuleGeneralPersonelRoster_InitGroupItem_Patch
            {
                public static bool Prefix(UIModuleGeneralPersonelRoster __instance, IGeoCharacterContainer container, int groupIndex, GeoRosterFilter filter, ref GeoRosterContainterItem __result)
                {
                    try
                    {
                        if (filter.GroupPrefab == null)
                        {
                            __result = null;
                        }


                       // if (_rosterVehicles == null)
                       // {
                       //     _rosterVehicles = UnityEngine.Object.Instantiate(__instance.RosterList);
                            // _rosterVehicles.position = new Vector3(__instance.RosterList.position.x+800, __instance.RosterList.position.y, __instance.RosterList.position.z);
                       // }

                        string containerName = container.Name;

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        GeoSite site = null;
                        GeoVehicle aircraft = null;

                        bool containerIsRelevant = false;

                        foreach (GeoSite geoSite in controller.Map.AllSites)
                        {
                            if (geoSite.LocalizedSiteName != null && geoSite.LocalizedSiteName == containerName)
                            {
                                TFTVLogger.Always($"found location at which {containerName} is. It's {geoSite.LocalizedSiteName}");
                                site = geoSite;
                                break;
                            }
                        }

                        foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                        {
                            if (containerName == geoVehicle.Name)
                            {
                                aircraft = geoVehicle;

                                TFTVLogger.Always($"found aircraft for {containerName}");
                                if (geoVehicle.CurrentSite != null && geoVehicle.CurrentSite.GetComponent<GeoPhoenixBase>()!=null)
                                {
                                    TFTVLogger.Always($"and it's at {site?.LocalizedSiteName}");
                                    site = geoVehicle.CurrentSite;

                                    if (_sitesVehiclesDict.ContainsKey(site))
                                    {
                                        _sitesVehiclesDict[site].Add(geoVehicle);
                                    }
                                    else
                                    {
                                        _sitesVehiclesDict.Add(site, new List<GeoVehicle> { geoVehicle });
                                    }

                                }
                                else
                                {
                                    _vehiclesInTransit.Add(geoVehicle);
                                }

                                break;
                            }
                        }

                        if (site != null)
                        {
                            IEnumerable<GeoVehicle> geoVehicles = site.GetPlayerVehiclesOnSite();
                            if ((geoVehicles != null && geoVehicles.Count() > 0 && geoVehicles.Any(v => v.Owner == controller.PhoenixFaction && v.Units.Count() > 0)))
                            {
                                containerIsRelevant = true;
                            }
                            else if (site.Units.Any(u => u.Faction == controller.PhoenixFaction))
                            {
                                containerIsRelevant = true;
                                _garrisonPhoenixBases.Add(site);

                            }
                        }
                        else if (aircraft != null)
                        {
                            if (aircraft.Owner == controller.PhoenixFaction && aircraft.Units.Count() > 0)
                            {
                                containerIsRelevant = true;
                            }
                        }

                        if (containerIsRelevant)
                        {
                            GeoRosterContainterItem geoRosterContainterItem;
                            if (__instance.Groups.Count <= groupIndex)
                            {
                                //geoRosterContainterItem = UnityEngine.Object.Instantiate(filter.GroupPrefab, __instance.RosterList);

                                TFTVLogger.Always($"Creating new group for {containerName} at index {groupIndex}");
                                geoRosterContainterItem = UnityEngine.Object.Instantiate(filter.GroupPrefab, __instance.RosterList);

                                __instance.Groups.Add(geoRosterContainterItem);
                            }
                            else
                            {
                                TFTVLogger.Always($"adding item {containerName} at index {groupIndex}");
                                geoRosterContainterItem = __instance.Groups[groupIndex];
                               

                            }

                            geoRosterContainterItem.Init(container);
                            __result = geoRosterContainterItem;

                        }
                        else
                        {
                            TFTVLogger.Always($"{containerName} is relevant? {containerIsRelevant}, so should not appear");
                            __result = null;
                        }

                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/


/*
[HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "InitRosterSlots")]
    public static class UIModuleGeneralPersonelRoster_InitRosterSlots_Patch
    {
        public static void Postfix(UIModuleGeneralPersonelRoster __instance, IGeoCharacterContainer primaryContainer, GeoRosterFilter filter)
        {
            // Use reflection to access the private _unitContainers field
            FieldInfo unitContainersField = typeof(UIModuleGeneralPersonelRoster).GetField("_unitContainers", BindingFlags.NonPublic | BindingFlags.Instance);
            List<object> unitContainers = (List<object>)unitContainersField.GetValue(__instance);

            // Clear existing slots and groups
            __instance.Slots.Clear();
            __instance.Groups.Clear();

            // Separate GeoSites and GeoVehicles
            var geoSites = new List<GeoRosterContainterItem>();
            var geoVehicles = new List<GeoRosterContainterItem>();
            var otherItems = new List<GeoRosterContainterItem>();

            foreach (var unitContainer in unitContainers)
            {
                IGeoCharacterContainer container = (IGeoCharacterContainer)unitContainer.GetType().GetProperty("Container").GetValue(unitContainer);
                var containerItem = __instance.InitGroupItem(container, __instance.Groups.Count, filter);
                if (containerItem != null)
                {
                    if (container is GeoSite)
                    {
                        geoSites.Add(containerItem);
                    }
                    else if (container is GeoVehicle)
                    {
                        geoVehicles.Add(containerItem);
                    }
                    else
                    {
                        otherItems.Add(containerItem);
                    }
                }
            }

            // Arrange GeoSites on the left and GeoVehicles to the right of their corresponding GeoSite
            int siblingIndex = 0;
            foreach (var site in geoSites)
            {
                site.transform.SetSiblingIndex(siblingIndex++);
                site.gameObject.SetActive(true);

                // Find and arrange GeoVehicles to the right of their corresponding GeoSite
                foreach (var vehicle in geoVehicles.Where(v => ((GeoVehicle)v.Container).CurrentSite == site.Container))
                {
                    vehicle.transform.SetSiblingIndex(siblingIndex++);
                    vehicle.transform.localPosition = new Vector3(site.transform.localPosition.x + 200, site.transform.localPosition.y, site.transform.localPosition.z); // Adjust the x offset as needed
                    vehicle.gameObject.SetActive(true);
                }
            }

            // Arrange remaining items below
            foreach (var item in otherItems)
            {
                item.transform.SetSiblingIndex(siblingIndex++);
                item.gameObject.SetActive(true);
            }

            // Refresh navigation
            __instance.RefreshNavigation();
        }
    }*/





    /*[HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "InitRosterSlots")]
             public static class UIModuleGeneralPersonelRosterPatch
             {
                 public static void Postfix(UIModuleGeneralPersonelRoster __instance, IGeoCharacterContainer primaryContainer, 
                     GeoRosterFilter filter, Predicate<GeoRosterItem> ____selectedCheck)
                 {
                     try
                     {
                        SortOutSitesAndVehicles(__instance);

                       /*  // Clear existing slots
                         __instance.Slots.Clear();
                         __instance.Slots.AddRange(__instance.RosterList.GetComponentsInChildren<GeoRosterItem>(includeInactive: true).Where(r => r.RowMode == filter.Filter));

                         // Use reflection to set the Groups property
                         var groupsProperty = typeof(UIModuleGeneralPersonelRoster).GetProperty("Groups", BindingFlags.Public | BindingFlags.Instance);
                         var groups = __instance.RosterList.GetComponentsInChildren<GeoRosterContainterItem>(includeInactive: true).ToList();



                         groupsProperty.SetValue(__instance, groups);



                         // Use reflection to get the InitGroupItem method
                         var initGroupItemMethod = typeof(UIModuleGeneralPersonelRoster).GetMethod("InitGroupItem", BindingFlags.NonPublic | BindingFlags.Instance);

                         // Use reflection to get the InitRosterSlot method
                         var initRosterSlotMethod = typeof(UIModuleGeneralPersonelRoster).GetMethod("InitRosterSlot", BindingFlags.NonPublic | BindingFlags.Instance);

                         // Use reflection to get the _unitContainers field

                         var unitContainersField = typeof(UIModuleGeneralPersonelRoster).GetField("_unitContainers", BindingFlags.NonPublic | BindingFlags.Instance);


                         var unitContainers = unitContainersField.GetValue(__instance) as IEnumerable<object>;

                         int num = 0;
                         int num2 = 0;
                         int num3 = 0;
                         int num4 = -1;

                         foreach (var unitContainer in unitContainers)
                         {
                             num4++;
                             var container = unitContainer.GetType().GetProperty("Container").GetValue(unitContainer);
                             var geoRosterContainterItem = (GeoRosterContainterItem)initGroupItemMethod.Invoke(__instance, new object[] { container, num4, filter });
                             if (geoRosterContainterItem != null)
                             {
                                 geoRosterContainterItem.transform.SetSiblingIndex(num3);
                                 geoRosterContainterItem.gameObject.SetActive(true);
                                 num2++;
                                 num3++;
                             }

                             var geoTacUnits = unitContainer.GetType().GetProperty("GeoTacUnits").GetValue(unitContainer) as IEnumerable<object>;
                             var units = geoTacUnits ?? (IEnumerable<object>)unitContainer.GetType().GetProperty("Units").GetValue(unitContainer);
                             foreach (var item in units)
                             {
                                 var slot = (GeoRosterItem)initRosterSlotMethod.Invoke(__instance, new object[] { item, container, num, filter });
                                 if (slot != null)
                                 {

                                     slot.transform.SetSiblingIndex(num3);                   
                                     slot.PrimaryContainer = primaryContainer;
                                     slot.Selected = ____selectedCheck != null ? ____selectedCheck(slot) : false;


                                     if (slot.RowMode == GeoRosterFilterMode.Soldiers)
                                     {
                                         if (_unitContainers.Where((ContainerData c) => c.Container != slot.Container && c.Container.CanTransferBetweenContainer(slot.Container)).Count() > 0)
                                         {
                                             slot.TransferButton.SetInteractable(isInteractable: true);
                                             slot.TransferDisplayArrow.SetActive(value: true);
                                         }
                                         else
                                         {
                                             slot.TransferButton.SetInteractable(isInteractable: false);
                                             slot.TransferDisplayArrow.SetActive(value: false);
                                         }
                                     }

                                     slot.gameObject.SetActive(value: true);
                                     num3++;
                                     num++;

                                     slot.gameObject.SetActive(true);
                                     num3++;
                                     num++;
                                 }
                             }
                         }

                         for (int i = num2; i < groups.Count; i++)
                         {
                             groups[i].gameObject.SetActive(false);
                         }

                         for (int j = num; j < __instance.Slots.Count; j++)
                          {
                              __instance.Slots[j].gameObject.SetActive(false);
                          }*/









            /*
                      }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }
             }*/






           /* [HarmonyPatch(typeof(GeoRosterItem))]
            [HarmonyPatch("Init", typeof(GeoCharacter), typeof(IGeoCharacterContainer), typeof(GeoFaction))]
            public static class GeoRosterGeoCharacterItemPatch
            {
                public static void Postfix(GeoRosterItem __instance, IGeoCharacterContainer characterContainer, GeoFaction faction, GeoCharacter character)
                {
                    try
                    {

                        //   __instance.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                        VerticalLayoutGroup verticalLayoutGroup = uIModuleGeneralPersonelRoster.ScrollController.ScrollRect.content.GetComponent<VerticalLayoutGroup>();

                        
//[TFTV @ 1/5/2025 6:57:27 PM] ScrollController: Content UnityEngine.RectTransform
//[TFTV @ 1/5/2025 6:57:27 PM] ScrollController: Content UnityEngine.UI.VerticalLayoutGroup
//[TFTV @ 1/5/2025 6:57:27 PM] ScrollController: Content UnityEngine.UI.ContentSizeFitter
//[TFTV @ 1/5/2025 6:57:27 PM] ScrollController: Content Base.UI.UINavigationalElementsHolder
                        


                        if (verticalLayoutGroup != null && !_scrollAdjusted)
                        {
                            verticalLayoutGroup.GetComponent<RectTransform>().localScale = new Vector3(0.7f, 0.7f, 0.7f);
                            verticalLayoutGroup.GetComponent<RectTransform>().anchoredPosition
                                = new Vector2(verticalLayoutGroup.GetComponent<RectTransform>().anchoredPosition.x - 200, 0);

                            verticalLayoutGroup.SetLayoutVertical();
                            _scrollAdjusted = true;
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/
        }



        private static Sprite _backgroundSquadDeploy = null;
        private static Sprite _backgroundContainment = null;
        private static Sprite _backgroundBionics = null;
        private static Sprite _activeBackground = null;
        private static Sprite _backgroundMutation = null;
        private static Sprite _backgroundCustomization = null;
        private static Sprite _backgroundMemorial = null;
        private static Sprite _backgroundAirForce = null;



        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayMain = null;


        private static void ModifyLightningAndPlatform(Transform transform)
        {
            try
            {
                SceneLightingDef sceneLightingDef = DefCache.GetDef<SceneLightingDef>("EditSoldier_LightingDef");

                if (_activeBackground == _backgroundContainment)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundBionics)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundCustomization)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.9f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.8f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.7f;
                    transform.gameObject.SetActive(false);
                }
                else if (_activeBackground == _backgroundMutation)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundMemorial)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    transform.gameObject.SetActive(true);
                }

                else
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.06f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.14f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.49f;
                    transform.gameObject.SetActive(false);
                }
                // Default:
                //    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5660378f;
                //    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5343573f;
                //    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.520647943f;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        [HarmonyPatch(typeof(CharacterClassWorldDisplay), nameof(CharacterClassWorldDisplay.SetDisplay))]
        public static class TFTV_CharacterClassWorldDisplay_SetDisplay_patch
        {

            public static bool Prefix(CharacterClassWorldDisplay __instance)
            {
                try
                {
                    __instance.gameObject.SetActive(false);

                    return false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        [HarmonyPatch(typeof(UIStateRosterAliens), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterAliens_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                 //   UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                 //   uIModuleGeneralPersonelRoster.RosterList.gameObject.SetActive(true);

                    _activeBackground = _backgroundContainment;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")] //VERIFIED
        public static class TFTV_UIStateEditSoldier_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoCharacterStatus), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoCharacterStatus_EnterState_patch
        {

            public static void Prefix(UIStateGeoCharacterStatus __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoCharacterStatus ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoRoster_EnterState_patch
        {

            public static void Prefix(UIStateGeoRoster __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoRoster");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateInitial), "EnterState")] //VERIFIED
        public static class TFTV_UIStateInitial_EnterState_patch
        {

            public static void Prefix(UIStateInitial __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateInitial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static bool MemorialPushStateRunning = false;

        [HarmonyPatch(typeof(UIStateMemorial), "PushState")] //VERIFIED
        public static class TFTV_UIStateMemorial_PushState_patch
        {

            public static void Prefix(UIStateMemorial __instance)
            {
                try
                {
                    



                    _activeBackground = _backgroundMemorial;

                    // TFTVLogger.Always($"entering UIStateMemorial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
          
        }


        [HarmonyPatch(typeof(UIStateEditVehicle), "PushState")] //VERIFIED
        public static class TFTV_UIStateEditVehicle_PushState_patch
        {

            public static void Prefix(UIStateEditVehicle __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateEditVehicle");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateMutate), "PushState")] //VERIFIED
        public static class TFTV_UIStateMutate_PushState_patch
        {

            public static void Prefix(UIStateMutate __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    // TFTVLogger.Always($"entering UIStateMutate");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateBuyMutoid), "PushState")] //VERIFIED
        public static class TFTV_UIStateBuyMutoid_PushState_patch
        {

            public static void Prefix(UIStateBuyMutoid __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    //  TFTVLogger.Always($"entering UIStateBuyMutoid");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateBionics), "PushState")] //VERIFIED
        public static class TFTV_UIStateBionics_PushState_patch
        {

            public static void Prefix(UIStateBionics __instance)
            {
                try
                {
                    _activeBackground = _backgroundBionics;
                    // TFTVLogger.Always($"entering UIStateBionics");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterRecruits_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")] //VERIFIED
        public static class TFTV_UIStateSoldierCustomization_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundCustomization;
                    // TFTVLogger.Always($"entering UIStateSoldierCustomization ");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void LoadTFTVBackgrounds()
        {
            try
            {
                _backgroundSquadDeploy = Helper.CreateSpriteFromImageFile("squadbay.jpg");
                _backgroundContainment = Helper.CreateSpriteFromImageFile("containment.jpg");
                _backgroundMutation = Helper.CreateSpriteFromImageFile("scenemutation.jpg");
                _backgroundCustomization = Helper.CreateSpriteFromImageFile("scenecustomization.jpg");
                _backgroundBionics = Helper.CreateSpriteFromImageFile("scenebionics.jpg");
                _backgroundMemorial = Helper.CreateSpriteFromImageFile("scenememorial.jpg");
                _backgroundAirForce = Helper.CreateSpriteFromImageFile("sceneairforce.jpg");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void RemoveSceneDoF()
        {
            try
            {
                FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);

                LightingManager lightingManager = context.LightingManager;
                OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                OptionsManager.GraphicsQualityPreset preset = optionsManager.CurrentGraphicsPreset;

                preset.DepthOfField = false;

                MethodInfo methodInfo = typeof(LightingManager).GetMethod("ApplyPostProcessOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(lightingManager, new object[] { preset });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void ChangeSceneBackgroundSquadDeploy(GeoSceneReferences geoSceneReferences)
        {
            try
            {
                /* if (_deactivateBackgroundPic)
                 {
                     if (_copyCharacterClassWorldDisplay != null)
                     {
                         _copyCharacterClassWorldDisplay.gameObject.SetActive(false);
                     }
                     return;
                 }*/

                //   ChangeContainmentScreen(geoSceneReferences);

                if (_copyCharacterClassWorldDisplayMain != null)
                {
                    _copyCharacterClassWorldDisplayMain.gameObject.SetActive(true);

                    _copyCharacterClassWorldDisplayMain.SingleClassImage.sprite = _activeBackground ?? _backgroundSquadDeploy;
                    RectTransform backgroundPicRT = _copyCharacterClassWorldDisplayMain.SingleClassImage.GetComponent<RectTransform>();
                    float imageAspectCurrentBackground = (float)_activeBackground.texture.width / _activeBackground.texture.height;

                    /* TFTVLogger.Always($"Before changes: background {_activeBackground.name}, " +
                         $"anchoredPostion3d {backgroundPicRT.anchoredPosition3D}, " +
                         $"imageAspectCurrentBackground {imageAspectCurrentBackground}, " +
                         $"backgroundPicRT.sizeDelta {backgroundPicRT.sizeDelta}");*/

                    if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundBionics)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.08f, imageAspectCurrentBackground * 1.08f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                    }
                    else if (_activeBackground == _backgroundCustomization)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.1f, imageAspectCurrentBackground * 1.1f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMemorial)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.15f, imageAspectCurrentBackground * 1.15f);

                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, backgroundPicRT.anchoredPosition3D.z + 20);
                        RemoveSceneDoF();
                    }
                    else
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.31f, imageAspectCurrentBackground * 1.31f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }

                    /* TFTVLogger.Always($"After changes: background {_activeBackground.name}, " +
                        $"anchoredPostion3d {backgroundPicRT.anchoredPosition3D}, " +
                        $"imageAspectCurrentBackground {imageAspectCurrentBackground}, " +
                        $"backgroundPicRT.sizeDelta {backgroundPicRT.sizeDelta}");*/

                    return;
                }

                CharacterClassWorldDisplay characterClassWorldDisplay = geoSceneReferences.SquadBay.ClassWorldDisplay;


                GameObject copy = UnityEngine.Object.Instantiate(characterClassWorldDisplay.gameObject, characterClassWorldDisplay.transform.parent);
                CharacterClassWorldDisplay copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                _copyCharacterClassWorldDisplayMain = copyDisplay;

                copyDisplay.SingleClassImage.sprite = _activeBackground ?? _backgroundSquadDeploy;

                RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);

                rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                rt.eulerAngles = new Vector3(2.8f, 346, 0);


                copyDisplay.SingleClassImage.gameObject.SetActive(true);
                copyDisplay.RightClassImage.gameObject.SetActive(false);
                copyDisplay.LeftClassImage.gameObject.SetActive(false);

                Transform transform = geoSceneReferences.SquadBay.CharBuilderPlatform;

                RemoveSceneDoF();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        // private static Sprite _airForceBackground = null;



        [HarmonyPatch(typeof(UIStateVehicleRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateVehicleRoster_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundAirForce;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayVehicleRoster = null;

        [HarmonyPatch(typeof(GeoSceneReferences), nameof(GeoSceneReferences.ActivateScene))]
        public static class TFTV_GeoSceneReferences_ActivateScene_patch
        {
            public static void Prefix(GeoSceneReferences __instance, ActiveSceneReference activeScene, Dictionary<ActiveSceneReference, Transform> ____scenes)
            {
                try
                {

                    // TFTVLogger.Always($"{activeScene} {__instance.name}");

                    if (activeScene == ActiveSceneReference.SquadBay)
                    {
                        ChangeSceneBackgroundSquadDeploy(__instance);
                        ModifyLightningAndPlatform(__instance.SquadBay.CharBuilderPlatform);
                    }
                    else if (activeScene == ActiveSceneReference.VehicleBay)
                    {
                        if (_copyCharacterClassWorldDisplayVehicleRoster != null)
                        {
                            _copyCharacterClassWorldDisplayVehicleRoster.gameObject.SetActive(true);
                        }
                        else
                        {
                            CharacterClassWorldDisplay characterClassWorldDisplay = __instance.SquadBay.ClassWorldDisplay;


                            GameObject copy = UnityEngine.Object.Instantiate(characterClassWorldDisplay.gameObject, __instance.VehicleBay.transform);
                            CharacterClassWorldDisplay copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                            // _copyCharacterClassWorldDisplay = copyDisplay;

                            copy.SetActive(true);

                            copyDisplay.SingleClassImage.sprite = _backgroundAirForce;

                            RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                            float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                            rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                            rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);

                            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                            rt.eulerAngles = new Vector3(2.8f, 346, 0);


                            copyDisplay.SingleClassImage.gameObject.SetActive(true);
                            copyDisplay.RightClassImage.gameObject.SetActive(false);
                            copyDisplay.LeftClassImage.gameObject.SetActive(false);

                            _copyCharacterClassWorldDisplayVehicleRoster = copyDisplay;
                        }
                    }

                    RemoveContainmentInfoPanel();

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
