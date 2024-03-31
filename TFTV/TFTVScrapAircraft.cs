using AK.Wwise;
using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVScrapAircraft
    {
        

        //taken & slightly adjusted from Mad's Assorted Adjustments. All hail Mad! https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments/blob/main/Source/AssortedAdjustments/Patches/EnableScrapAircraft.cs

        internal static Color emptySlotDefaultColor = new Color32(0, 0, 0, 128);
        internal static string emptySlotDefaultText = "EMPTY";
        internal static string emptySlotScrapText = "SCRAP AIRCRAFT?";

        private class ContainerInfo
        {
            public ContainerInfo(string name, int index)
            {
                this.Name = name;
                this.Index = index;
            }
            public string Name { get; }
            public int Index { get; }
        }

        // Cache reflected methods
        internal static MethodInfo ___UpdateResourceInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);



        [HarmonyPatch(typeof(GeoRosterContainterItem), "Init")]
        public static class GeoRosterContainterItem_Init_Patch
        {
            public static void Prefix(GeoRosterContainterItem __instance)
            {
                try
                {
                    Text emptySlotText = __instance.EmptySlot.GetComponentInChildren<Text>(true);
                    emptySlotDefaultColor = emptySlotText.color;
                    emptySlotDefaultText = emptySlotText.text;
          
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }           
        }






      /*  [HarmonyPatch(typeof(GeoRosterContainterItem), "Refresh")]
        public static class GeoRosterContainterItem_Refresh_Patch
        {

            public static void Postfix(GeoRosterContainterItem __instance)
            {
                try
                {
                    // Empty
                    if (__instance.Container.MaxCharacterSpace > 0 && __instance.Container.CurrentOccupiedSpace == 0)
                    {


                        Text emptySlotText = __instance.EmptySlot.GetComponentInChildren<Text>(true);
                        //Logger.Info($"[GeoRosterContainterItem_Refresh_POSTFIX] emptySlotText: {emptySlotText.text}");

                        // Aircraft 
                        if (__instance.Container.MaxCharacterSpace != 2147483647)
                        {
                          


                            // 
                        }
                        // Base
                        else
                        {
                            emptySlotText.text = emptySlotDefaultText;

                        }
                        //Logger.Info($"[GeoRosterContainterItem_Refresh_POSTFIX] emptySlotText: {emptySlotText.text}");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            

        }*/

        private static void CopyTextProperties(Text source, Text destination)
        {
            // Copy font and font size
            destination.font = source.font;
            destination.fontSize = source.fontSize;

            // Copy color
            destination.color = source.color;

            // Set alignment to center
            destination.alignment = source.alignment;



            // Copy RectTransform properties
            RectTransform sourceRectTransform = source.rectTransform;
            RectTransform destinationRectTransform = destination.rectTransform;

            destinationRectTransform.localPosition = sourceRectTransform.localPosition;
            destinationRectTransform.sizeDelta = sourceRectTransform.sizeDelta;
            destinationRectTransform.anchorMin = sourceRectTransform.anchorMin;
            destinationRectTransform.anchorMax = sourceRectTransform.anchorMax;

            // Set pivot to center
            destinationRectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
        }

        private static void RemoveEquipmentFromScrappedVehicle(GeoVehicle aircraftToScrap)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                GeoFaction geoFaction = controller.PhoenixFaction;

                // UIModuleVehicleEquip vehicleEquipModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.ToVehicleRosterState;

                // TFTVLogger.Always($"vehicleEquipModule is null? {vehicleEquipModule == null}");

              //  TFTVLogger.Always($"{aircraftToScrap.name} has {aircraftToScrap.Equipments.Count()} equipment items");
                
                foreach (GeoVehicleEquipment geoVehicleEquipment in aircraftToScrap.Equipments)
                {

                    if (geoVehicleEquipment != null)
                    {
                       // TFTVLogger.Always($"{geoVehicleEquipment} being added ");
                        // GeoVehicleEquipmentUIData geoVehicleEquipmentUIData = geoVehicleEquipment.CreateUIData();
                        geoFaction.AircraftItemStorage.AddItem(geoVehicleEquipment);
                        // vehicleEquipModule.StorageList.AddItem(geoVehicleEquipmentUIData);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
      

        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")]
        public static class UIStateGeoRoster_EnterState_Patch
        {

            public static void Postfix(UIStateGeoRoster __instance, List<IGeoCharacterContainer> ____characterContainers, GeoRosterFilterMode ____preferableFilterMode)
            {
                try
                {
                    GeoscapeViewContext ___Context = (GeoscapeViewContext)AccessTools.Property(typeof(GeoscapeViewState), "Context").GetValue(__instance, null);
                    UIModuleGeneralPersonelRoster ____geoRosterModule = (UIModuleGeneralPersonelRoster)AccessTools.Property(typeof(UIStateGeoRoster), "_geoRosterModule").GetValue(__instance, null);

                    RefreshScrapTriggers();

                    // Scoped functions
                    void RefreshScrapTriggers()
                    {
                        for (int i = 0; i < ____geoRosterModule.Groups.Count; i++)
                        {
                           GeoRosterContainterItem c = ____geoRosterModule.Groups[i];

                           /* if (!c.EmptySlot.GetComponent<EventTrigger>())
                            {
                                TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} had no event trigger. Adding...");
                                c.EmptySlot.AddComponent<EventTrigger>();
                            }

                            TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} Clearing all mouse events from empty slot.");
                            EventTrigger eventTrigger = c.EmptySlot.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();

                            TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} Refreshing/Resetting text for empty slot. This IS needed.");
                            c.Refresh();*/

                            if (c.Container.MaxCharacterSpace != 2147483647) // !Bases
                            {


                                if (!c.EmptySlot.GetComponent<EventTrigger>())
                                {
                                    TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} had no event trigger. Adding...");
                                    c.EmptySlot.AddComponent<EventTrigger>();
                                }

                                TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} Clearing all mouse events from empty slot.");
                                EventTrigger eventTrigger = c.EmptySlot.GetComponent<EventTrigger>();

                                eventTrigger.triggers.Clear();

                                TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} Refreshing/Resetting text for empty slot. This IS needed.");
                                c.Refresh();

                                TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] {c.Container.Name} is NOT a base. Adding mouse events to empty slot. ");

                                Text oldEmptySlotText = c.EmptySlot.GetComponentInChildren<Text>(true);
                                Text emptySlotText = oldEmptySlotText;

                                if (oldEmptySlotText.text != emptySlotScrapText)
                                {
                                   // TFTVLogger.Always($"Going to destroy text component because it says {oldEmptySlotText.text}");
                                    Canvas.Destroy(oldEmptySlotText.gameObject);

                                    // Add a new Text component
                                    emptySlotText = c.EmptySlot.AddComponent<Text>();
                                    emptySlotText.text = emptySlotScrapText;

                                    // Copy properties from the old Text component
                                    CopyTextProperties(oldEmptySlotText, emptySlotText);

                                    // Store empty slot color and text to reset on mouse exit/refresh list
                                    emptySlotDefaultColor = emptySlotText.color;
                                }


                            //    Text emptySlotText = c.EmptySlot.GetComponentInChildren<Text>(true);
                              

                                ContainerInfo containerInfo = new ContainerInfo(c.Container.Name, i);
                                TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] containerInfo: {containerInfo.Name}, {containerInfo.Index}");

                                EventTrigger.Entry mouseenter = new EventTrigger.Entry();
                                mouseenter.eventID = EventTriggerType.PointerEnter;
                                mouseenter.callback.AddListener((eventData) => { OnScrapAircraftMouseEnter(emptySlotText); });
                                eventTrigger.triggers.Add(mouseenter);

                                EventTrigger.Entry mouseexit = new EventTrigger.Entry();
                                mouseexit.eventID = EventTriggerType.PointerExit;
                                mouseexit.callback.AddListener((eventData) => { OnScrapAircraftMouseExit(emptySlotText); });
                                eventTrigger.triggers.Add(mouseexit);

                                EventTrigger.Entry click = new EventTrigger.Entry();
                                click.eventID = EventTriggerType.PointerClick;
                                click.callback.AddListener((eventData) => { OnScrapAircraftClick(containerInfo); });
                                eventTrigger.triggers.Add(click);

                              


                            }

                        }
                    }

                    void OnScrapAircraftMouseEnter(Text t)
                    {
                        TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftMouseEnter({t})");
                        t.color = Color.red;
                    }

                    void OnScrapAircraftMouseExit(Text t)
                    {
                        TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftMouseExit({t})");
                        t.color = emptySlotDefaultColor;
                    }

                 

                    void OnScrapAircraftClick(ContainerInfo containerInfo)
                    {
                        TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftClick(containerInfo: {containerInfo.Name}, {containerInfo.Index})");

                        string aircraftIdentifier = containerInfo.Name;
                        int groupIndex = containerInfo.Index;
                        GeoFaction owningFaction = ___Context.ViewerFaction;
                        GeoVehicle aircraftToScrap = owningFaction.Vehicles.Where(v => v.Name == aircraftIdentifier).FirstOrDefault();
                        GeoscapeModulesData ____geoscapeModules = (GeoscapeModulesData)AccessTools.Property(typeof(GeoscapeViewState), "_geoscapeModules").GetValue(__instance, null);
                        UIModuleGeoscapeScreenUtils ____utilsModule = ____geoscapeModules.GeoscapeScreenUtilsModule;
                        string messageBoxText = ____utilsModule.DismissVehiclePrompt.Localize(null);
                        VehicleItemDef aircraftItemDef = GameUtl.GameComponent<DefRepository>().GetAllDefs<VehicleItemDef>().Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();

                        if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                        {
                            messageBoxText = messageBoxText + "\n" + ____utilsModule.ScrapResourcesBack.Localize(null) + "\n \n";
                            foreach (ResourceUnit resourceUnit in ((IEnumerable<ResourceUnit>)aircraftItemDef.ScrapPrice))
                            {
                                if (resourceUnit.RoundedValue > 0)
                                {
                                    string resourcesInfo = "";
                                    ResourceType type = resourceUnit.Type;
                                    switch (type)
                                    {
                                        case ResourceType.Supplies:
                                            resourcesInfo = ____utilsModule.ScrapSuppliesResources.Localize(null);
                                            break;
                                        case ResourceType.Materials:
                                            resourcesInfo = ____utilsModule.ScrapMaterialsResources.Localize(null);
                                            break;
                                        case (ResourceType)3:
                                            break;
                                        case ResourceType.Tech:
                                            resourcesInfo = ____utilsModule.ScrapTechResources.Localize(null);
                                            break;
                                        default:
                                            if (type == ResourceType.Mutagen)
                                            {
                                                resourcesInfo = ____utilsModule.ScrapMutagenResources.Localize(null);
                                            }
                                            break;
                                    }
                                    resourcesInfo = resourcesInfo.Replace("{0}", resourceUnit.RoundedValue.ToString());
                                    messageBoxText += resourcesInfo;
                                }
                            }
                        }

                        // Safety check as the game's UI fails hard if there's NO GeoVehicle left at all
                        if (owningFaction.Vehicles.Count() <= 1)
                        {
                            GameUtl.GetMessageBox().ShowSimplePrompt("This is Phoenix Point's last aircraft available", MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                        }
                        else
                        {
                            GameUtl.GetMessageBox().ShowSimplePrompt(string.Format(messageBoxText, aircraftIdentifier), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnScrapAircraftCallback), null, containerInfo);
                        }
                    }

                    void OnScrapAircraftImpossibleCallback(MessageBoxCallbackResult msgResult)
                    {
                        // Nothing
                    }

                    void OnScrapAircraftCallback(MessageBoxCallbackResult msgResult)
                    {
                        if (msgResult.DialogResult == MessageBoxResult.Yes)
                        {
                            ContainerInfo containerInfo = msgResult.UserData as ContainerInfo;
                            TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftCallback(containerInfo: {containerInfo.Name}, {containerInfo.Index})");

                            string aircraftIdentifier = containerInfo.Name;
                            int groupIndex = containerInfo.Index;
                            GeoFaction owningFaction = ___Context.ViewerFaction;
                            GeoVehicle aircraftToScrap = owningFaction.Vehicles.Where(v => v.Name == aircraftIdentifier).FirstOrDefault();

                            if (aircraftToScrap != null)
                            {
                                // Unset vehicle.CurrentSite and trigger site.VehicleLeft
                                aircraftToScrap.Travelling = true;

                                RemoveEquipmentFromScrappedVehicle(aircraftToScrap);
                                // Away with it!
                                aircraftToScrap.Destroy();

                                // Add resources
                                VehicleItemDef aircraftItemDef = GameUtl.GameComponent<DefRepository>().GetAllDefs<VehicleItemDef>().Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();
                                if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                                {
                                    ___Context.Level.PhoenixFaction.Wallet.Give(aircraftItemDef.ScrapPrice, OperationReason.Scrap);

                                    GeoscapeModulesData ____geoscapeModules = (GeoscapeModulesData)AccessTools.Property(typeof(GeoscapeViewState), "_geoscapeModules").GetValue(__instance, null);

                                    //MethodInfo ___UpdateResourceInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                                    ___UpdateResourceInfo.Invoke(____geoscapeModules.ResourcesModule, new object[] { owningFaction, true });
                                }

                                // Clean roster from aircraft container
                                ____characterContainers.RemoveAt(groupIndex);

                                // Reset roster list
                                ____geoRosterModule.Init(___Context, ____characterContainers, null, ____preferableFilterMode, RosterSelectionMode.SingleSelect);

                                // Reapply events to the correct slots
                                RefreshScrapTriggers();
                            }
                            else
                            {
                                TFTVLogger.Debug($"[UIStateGeoRoster_EnterState_POSTFIX] Couldn't get GeoVehicle from aircraftIdentifier: {aircraftIdentifier}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}

