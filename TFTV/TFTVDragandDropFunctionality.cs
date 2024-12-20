using Base;
using Base.Core;
using Base.Defs;
using Base.Input;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Research;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleRoster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Levels.GeoMap;
using Text = UnityEngine.UI.Text;

namespace TFTV
{
    internal class TFTVDragandDropFunctionality
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal class Research
        {
            [HarmonyPatch(typeof(UIModuleResearch))]
            public static class UIModuleResearchPatch
            {
                // Patch the Start method to add drag-and-drop event handlers

                [HarmonyPostfix]
                [HarmonyPatch("Awake")]
                public static void Awake_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        __instance.QueueScrollRect.AdditionalVisibleRows = 50;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                [HarmonyPostfix]
                [HarmonyPatch("Init")]
                public static void Init_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                [HarmonyPostfix]
                [HarmonyPatch("AddToQueue")]
                public static void AddToQueue_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                // Patch the SetupQueue method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("SetupQueue")]
                public static void SetupQueue_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddDragHandlers(ResearchQueueItem item, UIModuleResearch instance)
                {
                    try
                    {
                        if (item.gameObject.GetComponent<DragHandler>() != null)
                        {
                            // DragHandler handler = item.gameObject.GetComponent<DragHandler>();
                            // UnityEngine.Object.Destroy(handler);
                            return;
                        }

                        var dragHandler = item.gameObject.AddComponent<DragHandler>();
                        dragHandler.Init(item, instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private ResearchQueueItem _item;
                private UIModuleResearch _roster;
                private RectTransform _originalParent;
                private int _originalIndex;
                private ScrollRect _scrollRect;

                public void Init(ResearchQueueItem item, UIModuleResearch roster)
                {
                    try
                    {
                        _roster = roster;
                        _item = item;
                        _scrollRect = _roster.QueueScrollRect.Scroll;
                        _roster.QueueScrollRect.AdditionalVisibleRows = 50;

                        Debug.Log($"DragHandler initialized for slot: {item.ResearchName.text}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        // TFTVLogger.Always($"OnBeginDrag called for {_item.ResearchName.text}");
                        _originalParent = transform.parent as RectTransform;
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<ResearchElement> researchElementQueue = phoenixFaction.Research.ResearchQueue;

                        for (int i = 0; i < researchElementQueue.Count; i++)
                        {
                            ResearchElement manufactureQueueItem = researchElementQueue[i];

                            if (manufactureQueueItem == _item.Research)
                            {
                                _originalIndex = i;
                                break;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent);
                        transform.SetParent(_originalParent.parent);

                        // Disable the ScrollRect to prevent it from interfering with the drag
                        /*  if (_scrollRect != null)
                          {
                              _scrollRect.enabled = false;
                          }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }



                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {

                        // TFTVLogger.Always($"OnDrag for {_item.ResearchName.text}");
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        //   TFTVLogger.Always($"OnEndDrag called for {_item.ResearchName.text}");

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        // TFTVLogger.Always($"_originalIndex: {_originalIndex} new index {newIndex}, phoenixFaction.Manufacture.Queue.Count {phoenixFaction.Manufacture.Queue.Count}");

                        if (newIndex != _originalIndex)
                        {
                            ResearchElement item = _item.Research;
                            phoenixFaction.Research.ResearchQueue.Remove(item);
                            phoenixFaction.Research.ResearchQueue.Insert(newIndex, item);

                            // TFTVLogger.Always($"Moved {item.ResearchID} from index {_originalIndex} to {newIndex}");
                        }

                        UIModuleResearch uIModuleResearch = phoenixFaction.GeoLevel.View.GeoscapeModules.ResearchModule;

                        MethodInfo method = uIModuleResearch.GetType().GetMethod("SetupQueue", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(uIModuleResearch, null);

                        // Re-enable the ScrollRect after the drag ends
                        /* if (_scrollRect != null)
                         {
                             _scrollRect.enabled = true;
                         }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);
                    GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<ResearchElement> researchQueue = phoenixFaction.Research.ResearchQueue;

                    // TFTVLogger.Always($"There {researchQueue.Count} items in Manufacturing queue");

                    UIModuleResearch uIModuleResearch = phoenixFaction.GeoLevel.View.GeoscapeModules.ResearchModule;

                    //  TFTVLogger.Always($"localPosition: {localPosition}");

                    for (int i = 0; i < researchQueue.Count; i++)
                    {
                        ResearchElement researchElement = researchQueue[i];

                        foreach (var child in uIModuleResearch.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            if (child.Research == researchElement && child != _item)
                            {
                                /* TFTVLogger.Always($"item: {researchElement.ResearchID}, pos: {i}, " +
                                     $"found child {child.ResearchName.text}, at pos {child.transform}, " +
                                     $"RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position): {RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position)}" +
                                     $"\nlocalPosition.y > child.transform.localPosition.y {localPosition.y > child.transform.localPosition.y}");*/

                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position))
                                {
                                    return i;
                                }
                            }
                        }
                    }

                    return _originalIndex;
                }
            }



        }



        internal class Manufacturing
        {
            [HarmonyPatch(typeof(UIModuleManufacturing))]
            public static class UIModuleManufacturingPatch2
            {
                [HarmonyPatch("RefreshItemList")]
                [HarmonyPrefix]
                public static void RefreshItemListPrefix(UIModuleManufacturing __instance, ref IEnumerable<ItemDef> availableItemRecipes)
                {
                    try
                    {
                        List<ItemDef> itemDefs = new List<ItemDef>();

                        foreach (ItemDef itemDef in availableItemRecipes)
                        {
                            if (!itemDef.Tags.Contains(DefCache.GetDef<GameTagDef>("AmmoItem_TagDef")))
                            {
                                itemDefs.Add(itemDef);
                            }
                        }

                        availableItemRecipes = itemDefs.ToArray();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            [HarmonyPatch(typeof(UIModuleManufacturing))]
            public static class UIModuleManufacturingPatch
            {
                // Patch the Start method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("Init")]
                public static void Init_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                [HarmonyPostfix]
                [HarmonyPatch("Start")]
                public static void Start_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                // Patch the SetupQueue method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("SetupQueue")]
                public static void SetupQueue_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddDragHandlers(GeoManufactureQueueItem item, UIModuleManufacturing instance)
                {
                    try
                    {
                        if (item.gameObject.GetComponent<DragHandler>() != null)
                        {
                            // DragHandler handler = item.gameObject.GetComponent<DragHandler>();
                            // UnityEngine.Object.Destroy(handler);
                            return;
                        }

                        var dragHandler = item.gameObject.AddComponent<DragHandler>();
                        dragHandler.Init(item, instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private GeoManufactureQueueItem _item;
                private UIModuleManufacturing _roster;
                private RectTransform _originalParent;
                private int _originalIndex;
                private ScrollRect _scrollRect;

                public void Init(GeoManufactureQueueItem item, UIModuleManufacturing roster)
                {
                    try
                    {
                        _roster = roster;
                        _item = item;
                        _scrollRect = _roster.QueueScroller.Scroll;
                        _roster.QueueScroller.AdditionalVisibleRows = 50;
                        // Debug.Log($"DragHandler initialized for slot: {item.ItemName.text}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        //TFTVLogger.Always($"OnBeginDrag called for {_item.ItemName.text}");
                        _originalParent = transform.parent as RectTransform;

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<ItemManufacturing.ManufactureQueueItem> manufactureQueueItems = phoenixFaction.Manufacture.Queue;

                        for (int i = 0; i < manufactureQueueItems.Count; i++)
                        {
                            ItemManufacturing.ManufactureQueueItem manufactureQueueItem = manufactureQueueItems[i];

                            if (manufactureQueueItem == _item.QueueElement)
                            {
                                _originalIndex = i;
                                break;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent);
                        transform.SetParent(_originalParent.parent);

                        // Disable the ScrollRect to prevent it from interfering with the drag
                        /*  if (_scrollRect != null)
                          {
                              _scrollRect.enabled = false;
                          }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {
                        //TFTVLogger.Always($"OnDrag for {_item.ItemName.text}");
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        // TFTVLogger.Always($"OnEndDrag called for {_item.ItemName.text}");

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        //  TFTVLogger.Always($"_originalIndex: {_originalIndex} new index {newIndex}, phoenixFaction.Manufacture.Queue.Count {phoenixFaction.Manufacture.Queue.Count}");

                        if (newIndex != _originalIndex)
                        {
                            ItemManufacturing.ManufactureQueueItem item = _item.QueueElement;
                            phoenixFaction.Manufacture.Queue.Remove(item);
                            phoenixFaction.Manufacture.Queue.Insert(newIndex, item);

                            // TFTVLogger.Always($"Moved {item.ManufacturableItem.Name.Localize()} from index {_originalIndex} to {newIndex}");
                        }

                        UIModuleManufacturing uIModuleManufacturing = phoenixFaction.GeoLevel.View.GeoscapeModules.ManufacturingModule;
                        MethodInfo method = uIModuleManufacturing.GetType().GetMethod("SetupQueue", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(uIModuleManufacturing, null);

                        // Re-enable the ScrollRect after the drag ends
                        /* if (_scrollRect != null)
                         {
                             _scrollRect.enabled = true;
                         }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);
                    GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<ItemManufacturing.ManufactureQueueItem> manufactureQueueItems = phoenixFaction.Manufacture.Queue;

                    //  TFTVLogger.Always($"There {manufactureQueueItems.Count} items in Manufacturing queue");

                    UIModuleManufacturing uIModuleManufacturing = phoenixFaction.GeoLevel.View.GeoscapeModules.ManufacturingModule;

                    //  TFTVLogger.Always($"localPosition: {localPosition}");

                    for (int i = 0; i < manufactureQueueItems.Count; i++)
                    {
                        ItemManufacturing.ManufactureQueueItem manufactureQueueItem = manufactureQueueItems[i];

                        foreach (var child in uIModuleManufacturing.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            if (child.QueueElement == manufactureQueueItem && child != _item)
                            {
                                /* TFTVLogger.Always($"item: {manufactureQueueItem.ManufacturableItem.Name.Localize()}, pos: {i}, " +
                                     $"found child {child.ItemName.text}, at pos {child.transform.localPosition}, " +
                                     $"RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position): {RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position)}" +
                                     $"\nlocalPosition.y > child.transform.localPosition.y {localPosition.y > child.transform.localPosition.y}");*/

                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position))
                                {
                                    return i;
                                }
                            }
                        }
                    }

                    return _originalIndex;
                }
            }
        }



        internal class VehicleRoster
        {

            public static List<InputAction> ActionsAircraftHotkeys = new List<InputAction>();
            public static bool AircraftHotkeysBindingsApplied = false;



            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnInputEvent")]
            public static class UIStateVehicleSelected_OnInputEvent_patch
            {

                public static void Postfix(UIStateVehicleSelected __instance, InputEvent ev)
                {
                    try
                    {
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        MethodInfo method = typeof(UIStateVehicleSelected).GetMethod("SelectVehicle", BindingFlags.NonPublic | BindingFlags.Instance);

                        GeoscapeViewContext geoscapeViewContext = (GeoscapeViewContext)typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller.View);

                        InputController inputController = geoscapeViewContext.Input;

                        FieldInfo field = inputController.GetType().GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);

                        InputAction[] inputActions = (InputAction[])field.GetValue(inputController);

                        if (!AircraftHotkeysBindingsApplied)
                        {
                            foreach (InputAction inputAction in ActionsAircraftHotkeys.Where(ia => !inputActions.Contains(ia)))
                            {
                                // TFTVLogger.Always($"{inputAction.Name} not found! adding to the list");
                                inputController.ApplyKeybinding(inputAction);

                            }
                            AircraftHotkeysBindingsApplied = true;
                        }


                        if (ev.Type == InputEventType.Pressed)
                        {
                            // TFTVLogger.Always($"evName: {ev.Name}");

                            if (ActionsAircraftHotkeys.Any(a => a.Name == ev.Name))
                            {
                                int id = int.Parse(ActionsAircraftHotkeys.FirstOrDefault(a => a.Name == ev.Name).Chords[0].Keys[0].Name) - 1;
                                GeoVehicle vehicle = controller.PhoenixFaction.Vehicles.FirstOrDefault(v => v.VehicleID == id);
                                if (vehicle != null)
                                {
                                    method.Invoke(__instance, new object[] { vehicle, true });
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

            [HarmonyPatch(typeof(GeoRosterItem), "UpdateLocations")]
            public static class GeoRosterItem_UpdateLocations_patch
            {

                public static void Postfix(GeoRosterItem __instance, IGeoCharacterContainer ____container)
                {
                    try
                    {
                        //TFTVLogger.Always($"UpdateLocations Running");

                        if (____container is GeoVehicle)
                        {
                            GeoVehicle geoVehicle = (GeoVehicle)____container;

                            if (geoVehicle != null)
                            {
                                // TFTVLogger.Always($"looking at {geoVehicle.Name} {geoVehicle.VehicleID}");
                                __instance.VehicleNumberText.text = (geoVehicle.VehicleID + 1).ToString();
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

            [HarmonyPatch(typeof(FacilityRosterSlot), "UpdateLocation")]
            public static class FacilityRosterSlot_UpdateLocations_patch
            {

                public static void Postfix(FacilityRosterSlot __instance, UseSoldiersFacilityComponent ____soldierComponent)
                {
                    try
                    {
                       // TFTVLogger.Always($"UpdateLocation Running");
                        GeoPhoenixBase pxBase = ____soldierComponent.Context.Facility.PxBase;
                        GeoVehicle geoVehicle = pxBase.VehiclesAtBase.FirstOrDefault((GeoVehicle p) => p.Units.Contains(__instance.Character));

                        if (geoVehicle != null)
                        {
                         //   TFTVLogger.Always($"looking at {geoVehicle.Name} {geoVehicle.VehicleID}");
                            Text text = __instance.ThirdLocationSpotText;
                            text.text = (geoVehicle.VehicleID + 1).ToString();

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleActorCycle), "UpdateLocations")]
            public static class UIModuleActorCycle_UpdateLocations_patch
            {

                public static void Postfix(UIModuleActorCycle __instance, UnitDisplayData ____currentUnit)
                {
                    try
                    {
                        GeoCharacter character = ____currentUnit.BaseObject as GeoCharacter;
                        if (character == null)
                        {
                            return;
                        }

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        IGeoCharacterContainer geoCharacterContainer = phoenixFaction.Sites.Concat<IGeoCharacterContainer>(phoenixFaction.Vehicles).FirstOrDefault((IGeoCharacterContainer c) => c.GetAllCharacters().Contains(character));

                        if (geoCharacterContainer == null)
                        {
                            return;
                        }

                        if (geoCharacterContainer is GeoVehicle geoVehicle)
                        {
                            if (geoVehicle != null)
                            {
                                __instance.VehicleNumberText.text = (geoVehicle.VehicleID + 1).ToString();

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


            [HarmonyPatch(typeof(GeoVehicle), "get_Name")]
            public static class GeoVehicle_get_Name_patch
            {

                public static bool Prefix(GeoVehicle __instance, ref string __result, string ____vehicleName)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(____vehicleName))
                        {
                            __result = string.Format(__instance.VehicleDef.ViewElement.DisplayName1.Localize(null), __instance.VehicleID + 1);
                        }
                        else
                        {
                            __result = ____vehicleName;
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


            public static List<int> PlayerVehicles = new List<int>();

            public static void RecordVehicleOrder(GeoLevelController controller)
            {
                try
                {
                    PlayerVehicles.Clear();
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    FieldInfo fieldInfo = controller.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);
                    FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);
                    List<GeoVehicle> vehicles = factionActorCache.Cache[phoenixFaction];
                    for (int x = 0; x < vehicles.Count; x++)
                    {
                        vehicles[x].VehicleID = x;
                        PlayerVehicles.Add(vehicles[x].VehicleID);
                    }
                    TFTVLogger.Always($"Recorded PlayerVehicles: {string.Join(", ", PlayerVehicles)}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            public static void RestoreVehicleOrder(GeoLevelController controller)
            {
                try
                {
                    if (PlayerVehicles != null && PlayerVehicles.Count > 0)
                    {
                        GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                        FieldInfo fieldInfo = phoenixFaction.GeoLevel.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);

                        FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);

                        List<GeoVehicle> vehicles = new List<GeoVehicle>();

                        for (int x = 0; x < PlayerVehicles.Count; x++)
                        {
                            GeoVehicle geoVehicle = phoenixFaction.Vehicles.FirstOrDefault(vehicle => vehicle.VehicleID == PlayerVehicles[x]);
                            geoVehicle.VehicleID = x;
                            vehicles.Add(geoVehicle);
                        }

                        factionActorCache.Cache[phoenixFaction] = vehicles;
                        fieldInfo.SetValue(phoenixFaction.GeoLevel.Map, factionActorCache);

                        //  TFTVLogger.Always($"got here");

                        //   phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Uninit();
                        /*  FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                          GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(controller.View);
                          phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Init(context);*/
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }





            [HarmonyPatch(typeof(UIModuleVehicleRoster))]
            public static class TFTVDragandDropFunctionality
            {
                [HarmonyPatch("InitSlots")]
                [HarmonyPostfix]
                public static void InitSlotsPostfix(UIModuleVehicleRoster __instance)
                {
                    try
                    {
                        Debug.Log($"Initializing DragHandlers for {__instance.Slots.Count} slots.");

                        foreach (var slot in __instance.Slots)
                        {
                            if (slot == null)
                            {
                                Debug.LogWarning("Slot is null during initialization!");
                                continue;
                            }

                            var dragHandler = slot.GetComponent<DragHandler>();
                            if (dragHandler == null)
                            {
                                dragHandler = slot.gameObject.AddComponent<DragHandler>();
                                dragHandler.Init(__instance, slot);
                                Debug.Log($"Added DragHandler to slot: {slot.name}");
                            }
                            else
                            {
                                Debug.Log($"DragHandler already exists for slot: {slot.name}");
                            }

                            EnableRaycastTargets(slot.gameObject);

                        }

                        ScrapeButtonFunctionality(__instance);



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void EnableRaycastTargets(GameObject obj)
                {
                    foreach (var graphic in obj.GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.raycastTarget = true;
                        Debug.Log($"Raycast target enabled for: {graphic.name}");
                    }
                }

                private static List<VehicleItemDef> _vehicleDefs;

                private static void PopulateInternalVehicleDefsList()
                {
                    try
                    {
                        if (_vehicleDefs == null || _vehicleDefs.Count == 0)
                        {
                            _vehicleDefs = new List<VehicleItemDef>();
                            _vehicleDefs.AddRange(GameUtl.GameComponent<DefRepository>().GetAllDefs<VehicleItemDef>().ToList());

                            foreach (VehicleItemDef vehicleItemDef in _vehicleDefs)
                            {
                                TFTVLogger.Always($"VehicleItemDef: {vehicleItemDef.name}, {vehicleItemDef.ViewElementDef.name}");



                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static PhoenixGeneralButton _scrapButton = null;

                //taken & adjusted from Mad's Assorted Adjustments. All hail Mad! https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments/blob/main/Source/AssortedAdjustments/Patches/EnableScrapAircraft.cs
                private static void ScrapeButtonFunctionality(UIModuleVehicleRoster uIModuleVehicleRoster)
                {
                    try
                    {
                        PopulateInternalVehicleDefsList();


                        if (_scrapButton != null)
                        {

                        }
                        else
                        {

                            //   TFTVLogger.Always($"checking");
                            Resolution resolution = Screen.currentResolution;

                            // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                            float resolutionFactorHeight = (float)resolution.height / 1080f;
                            //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                            EditUnitButtonsController editUnitButtonsController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;
                            PhoenixGeneralButton checkButton = UnityEngine.Object.Instantiate(editUnitButtonsController.DismissButton, uIModuleVehicleRoster.transform);
                            checkButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_SCRAP_AIRCRAFT");// "Toggles helmet visibility on/off.";

                            checkButton.transform.position += new Vector3(300 * resolutionFactorWidth, 100 * resolutionFactorHeight);
                            checkButton.PointerClicked += () => OnScrapAircraftClick();
                            _scrapButton = checkButton;
                        }

                        void OnScrapAircraftClick()
                        {
                            GeoVehicle aircraftToScrap = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                            //   TFTVLogger.Always($"aircraftToScrap?.name: {aircraftToScrap?.name} vehicleDef {aircraftToScrap.VehicleDef.name} {aircraftToScrap.VehicleDef.ViewElement.name}"); 

                            UIModuleGeoscapeScreenUtils uIModuleGeoscapeScreenUtils = aircraftToScrap.GeoLevel.View.GeoscapeModules.GeoscapeScreenUtilsModule;

                            string messageBoxText = uIModuleGeoscapeScreenUtils.DismissVehiclePrompt.Localize(null);
                            VehicleItemDef aircraftItemDef = _vehicleDefs.Where(viDef => viDef.ViewElementDef == aircraftToScrap.VehicleDef.ViewElement).FirstOrDefault();

                            //  TFTVLogger.Always($"aircraftItemDef?.name: {aircraftItemDef?.name}");

                            if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                            {
                                messageBoxText = messageBoxText + "\n" + uIModuleGeoscapeScreenUtils.ScrapResourcesBack.Localize(null) + "\n \n";
                                foreach (ResourceUnit resourceUnit in aircraftItemDef.ScrapPrice)
                                {
                                    if (resourceUnit.RoundedValue > 0)
                                    {
                                        string resourcesInfo = "";
                                        ResourceType type = resourceUnit.Type;
                                        switch (type)
                                        {
                                            case ResourceType.Supplies:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapSuppliesResources.Localize(null);
                                                break;
                                            case ResourceType.Materials:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapMaterialsResources.Localize(null);
                                                break;
                                            case (ResourceType)3:
                                                break;
                                            case ResourceType.Tech:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapTechResources.Localize(null);
                                                break;
                                            default:
                                                if (type == ResourceType.Mutagen)
                                                {
                                                    resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapMutagenResources.Localize(null);
                                                }
                                                break;
                                        }
                                        resourcesInfo = resourcesInfo.Replace("{0}", resourceUnit.RoundedValue.ToString());
                                        messageBoxText += resourcesInfo;
                                    }
                                }
                            }


                            // Safety check as the game's UI fails hard if there's NO GeoVehicle left at all
                            if (aircraftToScrap.Owner.Vehicles.Count() <= 1)
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_LAST_AIRCRAFT"), MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                            }
                            else if (aircraftToScrap.Travelling)
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_IN_TRANSIT_AIRCRAFT"), MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                            }
                            else
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(string.Format(messageBoxText, aircraftToScrap.Name), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnScrapAircraftCallback), null, aircraftToScrap);
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

                                GeoVehicle aircraftToScrap = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                                if (aircraftToScrap != null)
                                {
                                    // Unset vehicle.CurrentSite and trigger site.VehicleLeft
                                    aircraftToScrap.Travelling = true;

                                    RemoveEquipmentFromScrappedVehicle(aircraftToScrap);
                                    uIModuleVehicleRoster.UpdateSelectedVehicleEquipments();

                                    // Away with it!
                                    aircraftToScrap.Destroy();

                                    // Add resources
                                    VehicleItemDef aircraftItemDef = _vehicleDefs.Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();

                                    MethodInfo updateResourcInfoMethodInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                                    UIModuleInfoBar uIModuleInfoBar = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ResourcesModule;

                                    if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                                    {
                                        aircraftToScrap.Owner.Wallet.Give(aircraftItemDef.ScrapPrice, OperationReason.Scrap);
                                        updateResourcInfoMethodInfo.Invoke(uIModuleInfoBar, new object[] { aircraftToScrap.Owner, true });
                                    }

                                    FieldInfo fieldInfo = typeof(UIModuleVehicleRoster).GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance);
                                    List<VehicleDisplayData> vehicles = (List<VehicleDisplayData>)fieldInfo.GetValue(uIModuleVehicleRoster);
                                    vehicles.Remove(uIModuleVehicleRoster.SelectedSlot.Vehicle);
                                    fieldInfo.SetValue(uIModuleVehicleRoster, vehicles);

                                    uIModuleVehicleRoster.RosterList.DestroyChildren();

                                    MethodInfo methodInfo = typeof(UIModuleVehicleRoster).GetMethod("InitSlots", BindingFlags.NonPublic | BindingFlags.Instance);
                                    methodInfo.Invoke(uIModuleVehicleRoster, null);

                                    uIModuleVehicleRoster.SetSelectSlot(vehicles.First(), true);

                                    UIModuleVehicleCycle uIModuleVehicleCycle = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.VehicleCycleModule;
                                    uIModuleVehicleCycle.SelectVehicle(vehicles.FirstOrDefault());
                                }



                                /*uIModuleVehicleRoster.SelectedSlot.WeaponSlot01.ResetItem();
                                uIModuleVehicleRoster.SelectedSlot.WeaponSlot02.ResetItem();
                                uIModuleVehicleRoster.SelectedSlot.ModuleSlot.ResetItem();*/

                            }

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void RemoveEquipmentFromScrappedVehicle(GeoVehicle aircraftToScrap)
                {
                    try
                    {
                        GeoLevelController controller = aircraftToScrap.GeoLevel;

                        GeoFaction geoFaction = controller.PhoenixFaction;

                        List<GeoVehicleEquipment> vehicleEquipmentToRemove = new List<GeoVehicleEquipment>();

                        foreach (GeoVehicleEquipment geoVehicleEquipment in aircraftToScrap.Equipments)
                        {
                            if (geoVehicleEquipment != null)
                            {
                                // TFTVLogger.Always($"{geoVehicleEquipment} being added ");
                                // GeoVehicleEquipmentUIData geoVehicleEquipmentUIData = geoVehicleEquipment.CreateUIData();
                                geoFaction.AircraftItemStorage.AddItem(geoVehicleEquipment);
                                vehicleEquipmentToRemove.Add(geoVehicleEquipment);
                                // vehicleEquipModule.StorageList.AddItem(geoVehicleEquipmentUIData);
                            }
                        }

                        foreach (GeoVehicleEquipment vehicleEquipment in vehicleEquipmentToRemove)
                        {
                            aircraftToScrap.RemoveEquipment(vehicleEquipment);

                            TFTVLogger.Always($"removing {vehicleEquipment.EquipmentDef.name} from scrapped vehicle");
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private UIModuleVehicleRoster _roster;
                private GeoVehicleRosterSlot _slot;
                private Transform _originalParent;
                private int _originalIndex;

                public void Init(UIModuleVehicleRoster roster, GeoVehicleRosterSlot slot)
                {
                    try
                    {
                        _roster = roster;
                        _slot = slot;
                        Debug.Log($"DragHandler initialized for slot: {slot.name}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        Debug.Log($"OnBeginDrag called for {_slot.name}");
                        _originalParent = transform.parent;
                        _originalIndex = transform.GetSiblingIndex();

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent.GetComponent<RectTransform>());
                        transform.SetParent(_originalParent.parent);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        Debug.Log($"OnEndDrag called for {_slot.name}");
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        transform.SetSiblingIndex(newIndex);

                        int oldIndex = _roster.Slots.IndexOf(_slot);

                        if (newIndex != oldIndex)
                        {
                            Debug.Log($"Slot moved: Old Index: {oldIndex}, New Index: {newIndex}");
                            _roster.Slots.RemoveAt(oldIndex);
                            _roster.Slots.Insert(newIndex, _slot);
                            UpdateVehicleOrder();
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);

                    for (int i = 0; i < _originalParent.childCount; i++)
                    {
                        RectTransform child = _originalParent.GetChild(i) as RectTransform;

                        if (child != null)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint(child, position))
                            {
                                return i;
                            }

                            if (localPosition.y > child.localPosition.y)
                            {
                                return i;
                            }
                        }
                    }
                    return _originalParent.childCount - 1;
                }

                private void UpdateVehicleOrder()
                {
                    try
                    {
                        Debug.Log("Updating vehicle order...");
                        _roster.GetType().GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.SetValue(_roster, _roster.Slots.Select(slot => slot.Vehicle).ToList());

                        List<GeoVehicle> vehicles = _roster.Slots.Select(slot => (GeoVehicle)slot.Vehicle.BaseObject).ToList();

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        FieldInfo fieldInfo = phoenixFaction.GeoLevel.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);

                        FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);
                        factionActorCache.Cache[phoenixFaction] = vehicles;
                        fieldInfo.SetValue(phoenixFaction.GeoLevel.Map, factionActorCache);
                        //  phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Uninit();
                        RecordVehicleOrder(phoenixFaction.GeoLevel);
                        FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                        GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);
                        phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Init(context);

                        //   MethodInfo method = phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                        //  method.Invoke(phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule, null);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }








            }
        }


    }
}
