using Base.Core;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVPersonnelRoster
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static class CustomRosterUI
        {


           /* [HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "Init", typeof(GeoscapeViewContext), typeof(List<IGeoCharacterContainer>), typeof(IGeoCharacterContainer), typeof(GeoRosterFilterMode), typeof(RosterSelectionMode))]
            public static class UIModuleGeneralPersonelRoster_Init_GeoCharacterContainers
            {
                public static void Postfix(UIModuleGeneralPersonelRoster __instance, GeoscapeViewContext context, List<IGeoCharacterContainer> characterContainers, IGeoCharacterContainer primaryContainer, GeoRosterFilterMode filterMode, RosterSelectionMode preferableSelectionMode = RosterSelectionMode.SingleSelect)
                {
                    try
                    {
                      //  TFTVLogger.Always($"context.View.MainUILayer.ActiveState.Name: {context.View.MainUILayer.ActiveState?.Name}");

                        if (primaryContainer == null)
                        {
                            TFTVLogger.Always($"should be running personnel in base, not deployment!");
                            __instance.RosterList.gameObject.SetActive(false);
                            RenderNewUI(__instance);
                        }
                        else
                        {
                            __instance.RosterList.gameObject.SetActive(true);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }*/

            private static List<GeoSite> _phoenixBases = new List<GeoSite>();
            private static List<GeoSite> _locationsWithAircraft = new List<GeoSite>();
            private static List<GeoVehicle> _aircraft = new List<GeoVehicle> { };

            private static List<GeoVehicle> GetRelevantAircraft()
            {
                try
                {
                    if (_aircraft == null || _aircraft.Count == 0)
                    {

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<GeoVehicle> relevantPhoenixAircraft = phoenixFaction.Vehicles.Where(
                            v => v.Units.Any() ||
                            (v.CurrentSite != null && v.CurrentSite.Vehicles.Any(v2 => v2.Units.Any(u => u.Faction == phoenixFaction)) || v.CurrentSite.Units.Any(u => u.Faction == phoenixFaction))).ToList();

                        _aircraft = relevantPhoenixAircraft;
                    }
                    return _aircraft;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoSite> GetRelevantPhoenixBases()
            {
                try
                {
                    if (_phoenixBases == null || _phoenixBases.Count == 0)
                    {
                        List<GeoSite> relevantSitesWithPhoenixBase = new List<GeoSite>();
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<GeoPhoenixBase> relevantPhoenixBases = phoenixFaction.Bases.Where(b => b.Site.Units.Any(u => u.Faction == phoenixFaction) || b.VehiclesAtBase.Any(v => v.Units.Any(u => u.Faction == phoenixFaction))).ToList();

                        foreach (GeoPhoenixBase geoPhoenixBase in relevantPhoenixBases)
                        {
                            relevantSitesWithPhoenixBase.Add(geoPhoenixBase.Site);
                        }
                        _phoenixBases = relevantSitesWithPhoenixBase;
                    }
                    return _phoenixBases;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoSite> GetRelevantGeoSites()
            {
                try
                {
                    if (_locationsWithAircraft == null || _locationsWithAircraft.Count == 0)
                    {
                        List<GeoSite> relevantSites = new List<GeoSite>();

                        foreach (GeoVehicle geoVehicle in _aircraft)
                        {
                            GeoSite geoSite = geoVehicle.CurrentSite;

                            if (geoSite != null && !_phoenixBases.Contains(geoSite) && geoSite.GetPlayerVehiclesOnSite().Any(v => v != geoVehicle))
                            {
                                relevantSites.Add(geoSite);
                            }
                        }
                        _locationsWithAircraft = relevantSites;
                    }
                    return _locationsWithAircraft;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static GameObject _containterOfContainers = null;

            public static void RenderNewUI(UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster)
            {
                try
                {
                    if (_containterOfContainers == null)
                    {
                        GameObject newUI = new GameObject("TFTV_MainRosterUI");
                        Canvas canvas = newUI.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                       // CanvasScaler canvasScaler = newUI.AddComponent<CanvasScaler>();
                       // canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        newUI.AddComponent<GraphicRaycaster>();

                        // Add a black background
                        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
                        backgroundObject.transform.SetParent(newUI.transform);
                        Image backgroundImage = backgroundObject.AddComponent<Image>();
                        backgroundImage.color = new Color(0, 0, 0, 0.7f);
                        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
                        backgroundRect.sizeDelta = new Vector2(Screen.width/2.1f, Screen.height*0.9f);
                        backgroundRect.anchoredPosition = new Vector2(-400, 0);
                        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                        backgroundRect.pivot = new Vector2(0.5f, 0.5f);

                        _containterOfContainers = newUI;
                    }

                    RectTransform rosterContainer = _containterOfContainers.GetComponent<RectTransform>();

                    

                    Sprite phoenixBaseIcon = DefCache.GetDef<GeoFactionViewDef>("E_Phoenix_GeoFactionView [Phoenix_GeoPhoenixFactionDef]").DefaultBaseIconSmall;

                    // Create a grid layout for locations and aircraft
                    GameObject leftColumn = CreateColumn(rosterContainer, "LocationsColumn");
                    GameObject rightColumn = CreateColumn(rosterContainer, "AircraftColumn");

                    List<GeoSite> bases = GetRelevantPhoenixBases().Concat(GetRelevantGeoSites()).ToList();

                    foreach (var phoenixBase in bases)
                    {
                        GameObject basePanel = CreateBaseOrAircraftPanel(leftColumn.transform, phoenixBase.Name, phoenixBaseIcon);
                        List<GeoVehicle> aircraftAtBase = GetRelevantAircraft().Where(v => v.CurrentSite != null && v.CurrentSite == phoenixBase).ToList();

                        foreach (var aircraft in aircraftAtBase)
                        {
                            Sprite aircraftIcon = aircraft.VehicleDef.ViewElement.LargeIcon;

                            GameObject aircraftPanel = CreateBaseOrAircraftPanel(rightColumn.transform, aircraft.Name, aircraftIcon);
                            AddUnitsToPanel(aircraftPanel, aircraft.Units.ToList());
                        }
                    }

                    // Handle "In Transit" aircraft
                    var inTransitAircraft = _aircraft.Where(v => v.CurrentSite == null).ToList();

                    if (inTransitAircraft.Count > 0)
                    {
                        Sprite transitIcon = Helper.CreateSpriteFromImageFile("car_wheel.png");

                        GameObject transitPanel = CreateBaseOrAircraftPanel(leftColumn.transform, "In Transit", transitIcon);

                        foreach (var aircraft in inTransitAircraft)
                        {
                            Sprite aircraftIcon = aircraft.VehicleDef.ViewElement.LargeIcon;
                            CreateBaseOrAircraftPanel(rightColumn.transform, aircraft.Name, aircraftIcon);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GameObject CreateColumn(RectTransform parent, string name)
            {
                try
                {
                    GameObject column = new GameObject(name);
                    column.transform.SetParent(parent, false);
                    var layout = column.AddComponent<VerticalLayoutGroup>();
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                    layout.spacing = 10;
                    layout.padding = new RectOffset(10, 10, 10, 10);

                    RectTransform rectTransform = column.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(150, 100);

                    return column;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GameObject CreateBaseOrAircraftPanel(Transform parent, string name, Sprite icon)
            {
                try
                {
                    GameObject panel = new GameObject("Panel_" + name, typeof(RectTransform));
                    panel.transform.SetParent(parent);
                    var layout = panel.AddComponent<HorizontalLayoutGroup>();
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                    layout.spacing = 5;
                    layout.padding = new RectOffset(5, 5, 5, 5);

                    RectTransform rectTransform = panel.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(100, 100);
                    //rectTransform.anchoredPosition = new Vector2(Screen.width/2, Screen.height/2);

                    Image background = panel.AddComponent<Image>();
                    background.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black

                    CreateIcon(panel.transform, icon);
                    CreateText(panel.transform, name);

                    return panel;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateIcon(Transform parent, Sprite icon)
            {
                try
                {
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(parent);
                    var image = iconObj.AddComponent<Image>();
                    image.sprite = icon;

                    RectTransform rectTransform = iconObj.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(50, 50);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateText(Transform parent, string text)
            {
                try
                {
                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(parent);
                    var textComponent = textObj.AddComponent<Text>();
                    textComponent.text = text;
                    textComponent.color = Color.white;
                    textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    textComponent.fontSize = 14;

                    RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(100, 50);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void AddUnitsToPanel(GameObject panel, List<GeoCharacter> units)
            {
                try
                {
                    foreach (var unit in units)
                    {
                        GameObject unitPanel = CreateUnitPanel(panel.transform, unit);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GameObject CreateUnitPanel(Transform parent, GeoCharacter unit)
            {
                try
                {
                    GameObject panel = new GameObject("UnitPanel_" + unit.DisplayName);
                    panel.transform.SetParent(parent);

                    Sprite classIcon = null;

                    // Similar panel setup for units with specific icon and stats
                    CreateIcon(panel.transform, classIcon);
                    CreateText(panel.transform, unit.DisplayName + $" HP: {unit.Health} SP: {unit?.Fatigue?.Stamina} Lvl: {unit?.LevelProgression?.Level}");

                    return panel;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }




            public class DragDropHandler : MonoBehaviour, IBeginDragHandler, IDropHandler, IEndDragHandler
            {
                public void OnBeginDrag(PointerEventData eventData) { /* Handle drag start */ }
                public void OnDrop(PointerEventData eventData) { /* Handle drop */ }
                public void OnEndDrag(PointerEventData eventData) { /* Handle drag end */ }
            }

        }

    }
}
