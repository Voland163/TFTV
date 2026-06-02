using Base.Core;
using Base.Input;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
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

/// <summary>
/// Player-aircraft ordering and aircraft hotkeys without modifying GeoVehicle.VehicleID.
///
/// The old implementation made VehicleID double as a UI order index. This version keeps player order in a
/// mod-owned list of VehicleID values, then patches the UI lists that vanilla normally sorts by VehicleID.
/// Hotkeys select the Nth vehicle in that mod-owned order without renumbering any aircraft.
/// </summary>
public static class AircraftOrderWithoutVehicleId
{
    public static readonly List<InputAction> ActionsAircraftHotkeys = new List<InputAction>();

    private static readonly Dictionary<string, int> AircraftHotkeySlotByActionName = new Dictionary<string, int>();

    /// <summary>
    /// Player aircraft VehicleID values in the order arranged by the player.
    /// Persist this list in your mod save data if you need the order to survive a full game restart.
    /// Do not use this list to write back to GeoVehicle.VehicleID.
    /// </summary>
    public static readonly List<int> PlayerVehicleOrderIds = new List<int>();

    private static readonly HashSet<InputController> ControllersWithHotkeysApplied = new HashSet<InputController>();

    private static readonly FieldInfo GeoscapeViewContextField = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo InputActiveActionsMapField = typeof(InputController).GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo VehicleSelectedCurrentVehiclesListField = typeof(UIStateVehicleSelected).GetField("_currentVehiclesList", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo VehicleSelectedVehicleSelectionModuleField = typeof(UIStateVehicleSelected).GetField("_vehicleSelectionModule", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo VehicleRosterVehiclesField = typeof(UIModuleVehicleRoster).GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo VehicleRosterStateVehiclesField = typeof(UIStateVehicleRoster).GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo SelectVehicleMethod = typeof(UIStateVehicleSelected).GetMethod("SelectVehicle", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Register one hotkey action and the aircraft slot it should select.
    /// Example: RegisterAircraftHotkeyAction(myActionForKey1, 0) selects the first ordered aircraft.
    /// </summary>
    public static void RegisterAircraftHotkeyAction(InputAction action, int zeroBasedSlot)
    {
        if (action == null)
        {
            return;
        }

        if (!ActionsAircraftHotkeys.Contains(action))
        {
            ActionsAircraftHotkeys.Add(action);
        }

        AircraftHotkeySlotByActionName[action.Name] = zeroBasedSlot;
        ControllersWithHotkeysApplied.Clear();
    }


    /// <summary>
    /// Returns a copy of the current player aircraft order for mod save data.
    /// </summary>
    public static List<int> GetPlayerVehicleOrderIdsSnapshot()
    {
        return PlayerVehicleOrderIds.ToList();
    }

    /// <summary>
    /// Loads a persisted player aircraft order from mod save data.
    /// The next GetOrderedVehicles/RestoreVehicleOrder call will reconcile this list against current aircraft.
    /// </summary>
    public static void LoadPlayerVehicleOrderIds(IEnumerable<int> vehicleIds)
    {
        PlayerVehicleOrderIds.Clear();
        if (vehicleIds == null)
        {
            return;
        }

        PlayerVehicleOrderIds.AddRange(vehicleIds.Distinct());
    }

    /// <summary>
    /// Records the current Phoenix aircraft order as VehicleID values without changing VehicleID.
    /// Call this when entering/leaving relevant geoscape states if you need to seed the custom order.
    /// </summary>
    public static void RecordVehicleOrder(GeoLevelController controller)
    {
        if (controller == null || controller.PhoenixFaction == null)
        {
            return;
        }

        SetVehicleOrder(controller.PhoenixFaction.Vehicles.OfType<GeoVehicle>());
    }

    /// <summary>
    /// Reconciles the saved custom order with the current player aircraft collection.
    /// This intentionally does not write GeoVehicle.VehicleID and does not replace GeoMap._factionVehiclesCache.
    /// </summary>
    public static void RestoreVehicleOrder(GeoLevelController controller)
    {
        if (controller == null || controller.PhoenixFaction == null)
        {
            return;
        }

        GetOrderedVehicles(controller.PhoenixFaction);
    }

    public static List<GeoVehicle> GetOrderedVehicles(GeoPhoenixFaction phoenixFaction)
    {
        if (phoenixFaction == null)
        {
            return new List<GeoVehicle>();
        }

        List<GeoVehicle> currentVehicles = phoenixFaction.Vehicles.OfType<GeoVehicle>().Where(v => v != null).ToList();
        ReconcilePlayerVehicleOrderIds(currentVehicles);

        return currentVehicles
            .OrderBy(v => GetOrderIndex(v.VehicleID))
            .ThenBy(v => v.VehicleID)
            .ToList();
    }

    private static void SetVehicleOrder(IEnumerable<GeoVehicle> orderedVehicles)
    {
        if (orderedVehicles == null)
        {
            return;
        }

        GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
        GeoPhoenixFaction phoenixFaction = controller?.PhoenixFaction;
        if (phoenixFaction == null)
        {
            return;
        }

        List<GeoVehicle> currentVehicles = phoenixFaction.Vehicles.OfType<GeoVehicle>().Where(v => v != null).ToList();
        List<int> newOrderIds = orderedVehicles
            .Where(v => v != null && currentVehicles.Contains(v))
            .Select(v => v.VehicleID)
            .Distinct()
            .ToList();

        newOrderIds.AddRange(currentVehicles.Select(v => v.VehicleID).Where(id => !newOrderIds.Contains(id)));

        PlayerVehicleOrderIds.Clear();
        PlayerVehicleOrderIds.AddRange(newOrderIds);
    }

    private static void ReconcilePlayerVehicleOrderIds(List<GeoVehicle> currentVehicles)
    {
        List<int> currentVehicleIds = currentVehicles.Select(v => v.VehicleID).Distinct().ToList();

        List<int> reconciledOrderIds = PlayerVehicleOrderIds
            .Where(currentVehicleIds.Contains)
            .Distinct()
            .ToList();

        reconciledOrderIds.AddRange(currentVehicleIds.Where(id => !reconciledOrderIds.Contains(id)));

        PlayerVehicleOrderIds.Clear();
        PlayerVehicleOrderIds.AddRange(reconciledOrderIds);
    }

    private static List<VehicleDisplayData> SortVehicleDisplayData(IEnumerable<VehicleDisplayData> vehicles)
    {
        List<VehicleDisplayData> list = vehicles?.Where(v => v != null).ToList() ?? new List<VehicleDisplayData>();
        GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
        GeoPhoenixFaction phoenixFaction = controller?.PhoenixFaction;
        if (phoenixFaction == null)
        {
            return list;
        }

        GetOrderedVehicles(phoenixFaction);
        return list.OrderBy(v => GetOrderIndex(v.GetBaseObject<GeoVehicle>()?.VehicleID ?? int.MinValue)).ToList();
    }

    private static int GetOrderIndex(int vehicleId)
    {
        int index = PlayerVehicleOrderIds.IndexOf(vehicleId);
        return index >= 0 ? index : int.MaxValue;
    }

    private static void EnsureAircraftHotkeysApplied(InputController inputController)
    {
        if (inputController == null || ActionsAircraftHotkeys.Count == 0)
        {
            return;
        }

        InputAction[] activeActions = InputActiveActionsMapField?.GetValue(inputController) as InputAction[];
        if (activeActions == null)
        {
            return;
        }

        bool alreadyApplied = ControllersWithHotkeysApplied.Contains(inputController)
            && ActionsAircraftHotkeys.All(a => a != null && activeActions.Contains(a));

        if (alreadyApplied)
        {
            return;
        }

        foreach (InputAction inputAction in ActionsAircraftHotkeys.Where(a => a != null && !activeActions.Contains(a)))
        {
            if (inputAction.Hash < 0 || inputAction.Hash >= activeActions.Length)
            {
                Debug.LogWarning($"Skipping aircraft hotkey '{inputAction.Name}' because hash {inputAction.Hash} is outside _activeActionsMap length {activeActions.Length}.");
                continue;
            }

            inputController.ApplyKeybinding(inputAction);
        }

        ControllersWithHotkeysApplied.Add(inputController);
    }

    private static bool TryGetAircraftHotkeySlot(InputEvent ev, out int zeroBasedSlot)
    {
        zeroBasedSlot = -1;
        if (!AircraftHotkeySlotByActionName.TryGetValue(ev.Name, out zeroBasedSlot))
        {
            return false;
        }

        return zeroBasedSlot >= 0;
    }

    [HarmonyPatch(typeof(UIStateVehicleSelected), "OnInputEvent")]
    public static class UIStateVehicleSelected_OnInputEvent_patch
    {
        public static void Postfix(UIStateVehicleSelected __instance, InputEvent ev)
        {
            GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (controller == null || controller.PhoenixFaction == null || controller.View == null)
            {
                return;
            }

            GeoscapeViewContext context = GeoscapeViewContextField?.GetValue(controller.View) as GeoscapeViewContext;
            EnsureAircraftHotkeysApplied(context?.Input);

            if (ev.Type != InputEventType.Pressed || !TryGetAircraftHotkeySlot(ev, out int zeroBasedSlot))
            {
                return;
            }

            List<GeoVehicle> orderedVehicles = GetOrderedVehicles(controller.PhoenixFaction);
            if (zeroBasedSlot >= orderedVehicles.Count)
            {
                return;
            }

            GeoVehicle vehicle = orderedVehicles[zeroBasedSlot];
            if (vehicle != null)
            {
                SelectVehicleMethod?.Invoke(__instance, new object[] { vehicle, true });
            }
        }
    }

    /// <summary>
    /// Vanilla SelectVehicle rebuilds _currentVehiclesList with "orderby VehicleID".
    /// This postfix immediately replaces that list with the mod-owned order and refreshes the aircraft strip.
    /// </summary>
    [HarmonyPatch(typeof(UIStateVehicleSelected), "SelectVehicle")]
    public static class UIStateVehicleSelected_SelectVehicle_patch
    {
        public static void Postfix(UIStateVehicleSelected __instance, GeoVehicle vehicle)
        {
            if (__instance == null || vehicle == null || !(vehicle.Owner is GeoPhoenixFaction phoenixFaction))
            {
                return;
            }

            List<GeoVehicle> orderedVehicles = GetOrderedVehicles(phoenixFaction);
            VehicleSelectedCurrentVehiclesListField?.SetValue(__instance, orderedVehicles);

            UIModuleVehicleSelection vehicleSelectionModule = VehicleSelectedVehicleSelectionModuleField?.GetValue(__instance) as UIModuleVehicleSelection;
            if (vehicleSelectionModule != null)
            {
                vehicleSelectionModule.SetCyclingEnabled(orderedVehicles.Count > 1);
                vehicleSelectionModule.SetVehicleInfo(vehicle, orderedVehicles);
            }
        }
    }

    /// <summary>
    /// Makes the vehicle-roster state open in the custom order even though GeoFaction.Vehicles itself is unchanged.
    /// </summary>
    [HarmonyPatch(typeof(UIStateVehicleRoster), MethodType.Constructor, new Type[] { typeof(IEnumerable<GeoVehicle>), typeof(GeoVehicle) })]
    public static class UIStateVehicleRoster_ctor_patch
    {
        public static void Postfix(UIStateVehicleRoster __instance)
        {
            List<VehicleDisplayData> vehicles = VehicleRosterStateVehiclesField?.GetValue(__instance) as List<VehicleDisplayData>;
            if (vehicles == null || vehicles.Count <= 1)
            {
                return;
            }

            VehicleRosterStateVehiclesField.SetValue(__instance, SortVehicleDisplayData(vehicles));
        }
    }

    [HarmonyPatch(typeof(UIModuleVehicleRoster))]
    public static class TFTVDragandDropFunctionality
    {
        [HarmonyPatch("InitSlots")]
        [HarmonyPostfix]
        public static void InitSlotsPostfix(UIModuleVehicleRoster __instance)
        {
            if (__instance == null || __instance.Slots == null)
            {
                return;
            }

            foreach (GeoVehicleRosterSlot slot in __instance.Slots)
            {
                if (slot == null)
                {
                    continue;
                }

                DragHandler dragHandler = slot.GetComponent<DragHandler>();
                if (dragHandler == null)
                {
                    dragHandler = slot.gameObject.AddComponent<DragHandler>();
                }

                dragHandler.Init(__instance, slot);
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
                _roster = roster;
                _slot = slot;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (_slot == null || transform.parent == null)
                {
                    return;
                }

                _originalParent = transform.parent;
                _originalIndex = transform.GetSiblingIndex();

                RectTransform parentRect = _originalParent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }

                transform.SetParent(_originalParent.parent, true);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData != null)
                {
                    transform.position = eventData.position;
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_roster == null || _slot == null || _originalParent == null)
                {
                    return;
                }

                transform.SetParent(_originalParent, true);
                int newIndex = GetNewIndex(eventData?.position ?? transform.position);
                newIndex = Mathf.Clamp(newIndex, 0, Math.Max(0, _roster.Slots.Count - 1));

                transform.SetSiblingIndex(newIndex);

                int oldIndex = _roster.Slots.IndexOf(_slot);
                if (oldIndex < 0)
                {
                    transform.SetSiblingIndex(_originalIndex);
                    return;
                }

                if (newIndex != oldIndex)
                {
                    _roster.Slots.RemoveAt(oldIndex);
                    _roster.Slots.Insert(newIndex, _slot);
                    UpdateVehicleOrder();
                }
            }

            private int GetNewIndex(Vector3 position)
            {
                Vector3 localPosition = _originalParent.InverseTransformPoint(position);
                for (int i = 0; i < _originalParent.childCount; i++)
                {
                    RectTransform child = _originalParent.GetChild(i) as RectTransform;
                    if (child == null || child == transform)
                    {
                        continue;
                    }

                    if (RectTransformUtility.RectangleContainsScreenPoint(child, position) || localPosition.y > child.localPosition.y)
                    {
                        return i;
                    }
                }

                return _originalParent.childCount - 1;
            }

            private void UpdateVehicleOrder()
            {
                List<GeoVehicleRosterSlot> occupiedSlots = _roster.Slots.Where(s => s != null && s.Vehicle != null).ToList();
                List<VehicleDisplayData> orderedDisplayData = occupiedSlots.Select(slot => slot.Vehicle).ToList();
                List<GeoVehicle> orderedVehicles = orderedDisplayData
                    .Select(v => v.GetBaseObject<GeoVehicle>())
                    .Where(v => v != null)
                    .ToList();

                SetVehicleOrder(orderedVehicles);

                VehicleRosterVehiclesField?.SetValue(_roster, orderedDisplayData);

                if (_roster.NavHolder != null)
                {
                    _roster.NavHolder.SetFixedInteractableElements(occupiedSlots
                        .Select(s => s.RowButton?.BaseButton)
                        .OfType<Selectable>()
                        .ToList());
                }
            }
        }
    }
}