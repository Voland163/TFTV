using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.AircraftEquipment;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleEquipmentInventory;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleRoster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.AircraftReworkHelpers;
using static TFTV.TFTVAircraftReworkMain;


namespace TFTV
{

    internal class AircraftReworkUI
    {

        [HarmonyPatch(typeof(AircraftCrewController), nameof(AircraftCrewController.SetCrew))]
        internal static class AircraftCrewController_SetCrew_ZeroSpacePatch
        {
            private static readonly FieldInfo CrewField = AccessTools.Field(typeof(AircraftCrewController), "_crew");
            private static readonly FieldInfo UnitsField = AccessTools.Field(typeof(AircraftCrewController), "_unitsOnBoardElements");
            private static readonly MethodInfo RefreshCrewBarsMethod = AccessTools.Method(typeof(AircraftCrewController), "RefreshCrewBars");
            private static readonly MethodInfo OnSoldierSlotClickedMethod = AccessTools.Method(typeof(AircraftCrewController), "OnSoldierSlotClicked", new Type[] { typeof(int) });
            private static readonly MethodInfo OnCharInfoSlotClickedMethod = AccessTools.Method(typeof(AircraftCrewController), "OnCharInfoSlotClicked", new Type[] { typeof(int) });
            private static readonly HashSet<int> ExtraSlotIds = new HashSet<int>();

            static void Postfix(AircraftCrewController __instance, int maxSpace)
            {


                if (__instance == null)
                {
                    return;
                }

                List<GeoCharacter> crew = CrewField != null ? (CrewField.GetValue(__instance) as List<GeoCharacter>) : null;
                List<UnitOnBoardElementController> slots = UnitsField != null ? (UnitsField.GetValue(__instance) as List<UnitOnBoardElementController>) : null;
                if (crew == null || slots == null)
                {
                    return;
                }

                List<GeoCharacter> zeroSpacePassengers = crew.Where(c => c != null && c.OccupingSpace <= 0).ToList();
                if (zeroSpacePassengers.Count == 0)
                {
                    return;
                }



                bool applied = false;
                int baseSlotCount = slots.Count;
                Action<int> slotClickedHandler = CreateInstanceHandler(__instance, OnSoldierSlotClickedMethod);
                Action<int> charInfoHandler = CreateInstanceHandler(__instance, OnCharInfoSlotClickedMethod);
                foreach (GeoCharacter character in zeroSpacePassengers)
                {
                    int crewIndex = crew.IndexOf(character);
                    if (crewIndex < 0)
                    {
                        continue;
                    }

                    UnitOnBoardElementController slot = GetOrCreateExtraSlot(__instance, slots, baseSlotCount, slotClickedHandler, charInfoHandler);
                    if (slot == null)
                    {
                        break;
                    }

                    PrepareSlot(__instance, slot);
                    PopulateSlot(__instance, slot, character, crewIndex);
                    applied = true;
                }

                if (applied)
                {
                    if (RefreshCrewBarsMethod != null)
                    {
                        RefreshCrewBarsMethod.Invoke(__instance, Array.Empty<object>());
                    }
                    else
                    {
                        __instance.CrewBarsNeedRefresh = true;
                    }
                }
            }

            private static void PrepareSlot(AircraftCrewController controller, UnitOnBoardElementController slot)
            {
                if (controller == null || slot == null)
                {
                    return;
                }

                slot.SetEmpty();
                if (controller.EmptySlotTooltipText != null && slot.TooltipText != null)
                {
                    slot.TooltipText.TipText = controller.EmptySlotTooltipText.Localize(null);
                }
            }

            private static UnitOnBoardElementController GetOrCreateExtraSlot(
                AircraftCrewController controller,
                List<UnitOnBoardElementController> slots,
                int baseSlotCount,
                Action<int> slotClickedHandler,
                Action<int> slotInfoHandler)
            {
                if (controller == null || slots == null)
                {
                    return null;
                }

                for (int i = baseSlotCount; i < slots.Count; i++)
                {
                    UnitOnBoardElementController existing = slots[i];
                    if (existing != null && existing.CharIndex < 0)
                    {
                        return existing;
                    }
                }

                UnitOnBoardElementController reusable = FindReusableExtraSlot(controller, slots, slotClickedHandler, slotInfoHandler);
                if (reusable != null)
                {
                    slots.Add(reusable);
                    return reusable;
                }

                UnitOnBoardElementController created = CreateExtraSlot(controller, slotClickedHandler, slotInfoHandler);
                if (created != null)
                {
                    slots.Add(created);
                }

                return created;
            }

            private static UnitOnBoardElementController FindReusableExtraSlot(
                AircraftCrewController controller,
                List<UnitOnBoardElementController> slots,
                Action<int> slotClickedHandler,
                Action<int> slotInfoHandler)
            {
                if (controller?.CrewContainer == null)
                {
                    return null;
                }

                UnitOnBoardElementController[] allSlots = controller.CrewContainer.GetComponentsInChildren<UnitOnBoardElementController>(true);
                foreach (UnitOnBoardElementController candidate in allSlots)
                {
                    if (candidate == null || slots.Contains(candidate))
                    {
                        continue;
                    }

                    if (ExtraSlotIds.Contains(candidate.GetInstanceID()))
                    {
                        candidate.gameObject.SetActive(true);
                        AttachHandlers(candidate, slotClickedHandler, slotInfoHandler);
                        return candidate;
                    }
                }

                return null;
            }

            private static UnitOnBoardElementController CreateExtraSlot(
                AircraftCrewController controller,
                Action<int> slotClickedHandler,
                Action<int> slotInfoHandler)
            {
                if (controller?.CrewContainer == null || controller.UnitOnBoardPrefab == null)
                {
                    return null;
                }

                UnitOnBoardElementController newSlot = UnityEngine.Object.Instantiate(controller.UnitOnBoardPrefab, controller.CrewContainer);
                if (newSlot == null)
                {
                    return null;
                }

                newSlot.gameObject.SetActive(true);
                AttachHandlers(newSlot, slotClickedHandler, slotInfoHandler);
                ExtraSlotIds.Add(newSlot.GetInstanceID());
                return newSlot;
            }

            private static void AttachHandlers(UnitOnBoardElementController slot, Action<int> slotClickedHandler, Action<int> slotInfoHandler)
            {
                if (slot == null)
                {
                    return;
                }

                if (slotClickedHandler != null)
                {
                    slot.SlotClicked = RemoveHandler(slot.SlotClicked, slotClickedHandler);
                    slot.SlotClicked = (Action<int>)Delegate.Combine(slot.SlotClicked, slotClickedHandler);
                }

                if (slotInfoHandler != null)
                {
                    slot.SlotCharacterInfoClicked = RemoveHandler(slot.SlotCharacterInfoClicked, slotInfoHandler);
                    slot.SlotCharacterInfoClicked = (Action<int>)Delegate.Combine(slot.SlotCharacterInfoClicked, slotInfoHandler);
                }
            }

            private static Action<int> RemoveHandler(Action<int> source, Action<int> handler)
            {
                if (source == null || handler == null)
                {
                    return source;
                }

                Delegate[] invocationList = source.GetInvocationList();
                foreach (Delegate existing in invocationList)
                {
                    if (existing.Equals(handler))
                    {
                        source = (Action<int>)Delegate.Remove(source, handler);
                        break;
                    }
                }

                return source;
            }

            private static Action<int> CreateInstanceHandler(AircraftCrewController controller, MethodInfo method)
            {
                if (controller == null || method == null)
                {
                    return null;
                }

                return (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), controller, method, false);
            }

            private static void PopulateSlot(AircraftCrewController controller, UnitOnBoardElementController slot, GeoCharacter character, int crewIndex)
            {
                slot.SetUsed(crewIndex);
                slot.UnitIcon.SetClassIcons(character.GetClassViewElementDefs());
                slot.UnitIcon.SetColor(true);
                slot.LockedLine.SetActive(false);
                slot.LockedDot.SetActive(false);
                slot.LevelUpIndicator.SetActive(false);

                if (character.LevelProgression != null)
                {
                    List<string> classNames = new List<string>();
                    foreach (ViewElementDef viewElementDef in character.ClassViewElementDefs)
                    {
                        classNames.Add(viewElementDef.DisplayName1.Localize(null));
                    }

                    string tooltip = string.Format("{0}{1}LVL {2} - {3}", character.DisplayName, Environment.NewLine, character.LevelProgression.Level, string.Join("\\", classNames));
                    if (character.LevelProgression.HasNewLevel)
                    {
                        slot.LevelUpIndicator.SetActive(true);
                    }

                    if (character.CharacterStats.Corruption.IntValue > 0)
                    {
                        tooltip += Environment.NewLine + controller.CorruptionTooltipText.Localize(null) + string.Format(" - {0}/{1}", character.CharacterStats.Corruption.IntValue, character.CharacterStats.Willpower.IntValue);
                    }

                    slot.TooltipText.TipText = tooltip;
                }
                else
                {
                    slot.TooltipText.TipText = character.DisplayName ?? string.Empty;
                }
            }
        }

        [HarmonyPatch(typeof(GeoRosterContainterItem), nameof(GeoRosterContainterItem.Refresh))]
        internal static class GeoRosterContainterItem_Refresh_ZeroSpacePatch
        {
            static void Postfix(GeoRosterContainterItem __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                IGeoCharacterContainer container = __instance.Container;
                if (container == null)
                {
                    return;
                }

                int characterCount = container.GetCharacterCount();
                bool hasCharacters = characterCount > 0;

                if (hasCharacters && __instance.EmptySlot != null && __instance.EmptySlot.activeSelf)
                {
                    __instance.EmptySlot.SetActive(false);
                }

                if (__instance.ContainerCapacity != null && __instance.ContainerCapacity.gameObject.activeSelf && container.MaxCharacterSpace != int.MaxValue)
                {
                    int occupied = container.CurrentOccupiedSpace;
                    __instance.ContainerCapacity.text = string.Format("| {0}/{1}", occupied, container.MaxCharacterSpace);
                }
            }
        }


        [HarmonyPatch(typeof(VehicleSelectionAircraftElementController), "SetItem")]
        public static class VehicleSelectionAircraftElementController_SetItem_Patch
        {
            public static void Postfix(VehicleSelectionAircraftElementController __instance, GeoVehicle vehicle)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    int adjustedOccupiedSpace = GetAdjustedPassengerManifestAircraftRework(vehicle);

                    //   TFTVLogger.Always($"{vehicle.Name} {__instance.SoldierCapacity.text}");

                    __instance.SoldierCapacity.text = $"{adjustedOccupiedSpace}/{vehicle.MaxCharacterSpace}";

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(ShortEquipmentInfoButton), "SetEquipment")]
        public static class ShortEquipmentInfoButton_SetEquipment_Patch
        {
            public static void Postfix(ShortEquipmentInfoButton __instance, GeoVehicleEquipment equipment)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (equipment != null && equipment.EquipmentDef != null)
                    {
                        __instance.WeaponIcon.sprite = GetTierSprite(equipment.EquipmentDef);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(AircraftEquipmentViewController), "SetEquipmentUIData")]
        public static class AircraftEquipmentViewController_SetEquipmentUIData_Patch
        {
            public static void Postfix(AircraftEquipmentViewController __instance, GeoVehicleEquipmentUIData data)
            {

                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    // TFTVLogger.Always($"SetEquipmentUIData {data == null} {data?.AircraftEquipmentDef?.name}");

                    if (data == null)
                    {
                        return;
                    }


                    ItemDef itemDef = data.AircraftEquipmentDef;

                    int tier = GetTier(itemDef);
                    __instance.Health.text = "";

                    //  TFTVLogger.Always($"for {__instance.name}, tier {tier}, itemDef {itemDef?.name}");

                    if (tier > 0)
                    {
                        __instance.Health.text = $"{TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_TIER")} {tier}";
                    }

                    __instance.HealthBar.HealthBar.gameObject.SetActive(false);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }



        [HarmonyPatch(typeof(UIAircraftEquipmentTooltip), "DisplayAllStats")]
        public static class Patch_UIAircraftEquipmentTooltip_DisplayAllStats
        {
            public static bool Prefix(UIAircraftEquipmentTooltip __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    // Access private fields via reflection
                    var type = typeof(UIAircraftEquipmentTooltip);

                    // Icon
                    var Icon = __instance.Icon;

                    //     TFTVLogger.Always($"icon null? {Icon == null}");

                    // UISettings

                    // DisplayedData
                    var DisplayedData = __instance.DisplayedData;

                    //   TFTVLogger.Always($"DisplayedData null? {DisplayedData == null}");

                    // ItemNameLocComp, ItemDescriptionLocComp
                    var ItemNameLocComp = __instance.ItemNameLocComp;
                    var ItemDescriptionLocComp = __instance.ItemDescriptionLocComp;

                    _ = Icon != null;

                    //  TFTVLogger.Always($"icon still null? {Icon == null}");

                    if (__instance.UISettings.ShowNameDescription)
                    {
                        //    TFTVLogger.Always("Showing name and description");
                        type.GetMethod("DisplayNameDescription", BindingFlags.NonPublic | BindingFlags.Instance)
                             .Invoke(__instance, null);

                        //  TFTVLogger.Always("Displayed name and description");
                    }
                    else
                    {
                        ItemNameLocComp.gameObject.SetActive(value: false);
                        ItemDescriptionLocComp.gameObject.SetActive(value: false);
                    }

                    // GeoVehicleWeaponDef geoVehicleWeaponDef = DisplayedData.AircraftEquipmentDef as GeoVehicleWeaponDef;
                    var AircraftEquipmentDef = DisplayedData.AircraftEquipmentDef;

                    //                    TFTVLogger.Always($"AircraftEquipmentDef null? {AircraftEquipmentDef == null}");

                    var geoVehicleModuleDef = AircraftEquipmentDef as GeoVehicleModuleDef;



                    if (geoVehicleModuleDef != null)
                    {
                        type.GetMethod("DisplayGeoscapeBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(__instance, new object[] { geoVehicleModuleDef });

                        List<string> benefitKeys = GetModuleBenefitKeys(geoVehicleModuleDef);

                        if (benefitKeys.Count > 0)
                        {
                           
                            MethodInfo methodInfo = type.GetMethod("AddStatObject", BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach (string key in benefitKeys)
                            {
                                LocalizedTextBind localizedTextBind = new LocalizedTextBind(key, true);
                                methodInfo.Invoke(__instance, new object[] { localizedTextBind, null, string.Empty });
                            }
                        }
                        else
                        {
                            int tier = GetTier(geoVehicleModuleDef);

                            if (tier > 0)
                            {
                                LocalizedTextBind localizedTextBindTest2 = new LocalizedTextBind("TFTV_KEY_TIER", true);

                                MethodInfo methodInfo = type.GetMethod("AddStatObject", BindingFlags.NonPublic | BindingFlags.Instance);
                                methodInfo.Invoke(__instance, new object[] { localizedTextBindTest2, null, tier.ToString() });
                            }
                        }

                    }


                    Text text = ItemDescriptionLocComp.transform.GetComponent<Text>();

                    if (text != null)
                    {
                        // TFTVLogger.Always($"found text {text.text}");
                        text.verticalOverflow = VerticalWrapMode.Overflow;
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



        [HarmonyPatch(typeof(GeoVehicleRosterEquipmentSlot), "SetItem")]
        public static class GeoVehicleRosterEquipmentSlot_SetItem_Patch
        {
            public static void Postfix(GeoVehicleRosterEquipmentSlot __instance, GeoVehicleEquipmentUIData item)
            {

                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }


                    if (item != null && item.AircraftEquipmentDef != null)
                    {
                        __instance.ItemImage.overrideSprite = GetTierSprite(item.AircraftEquipmentDef);
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



        [HarmonyPatch(typeof(UIModuleSoldierEquip), "DoFilter")]
        public static class UIModuleSoldierEquip_DoFilter_Patch
        {
            public static void Prefix(UIModuleSoldierEquip __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }


                    // Get the private method 'TypeFilter' using Harmony's AccessTools.
                    MethodInfo typeFilterMethod = AccessTools.Method(typeof(UIModuleSoldierEquip), "TypeFilter");
                    if (typeFilterMethod == null)
                    {
                        // TFTVLogger.Always("Could not locate the private method 'TypeFilter' in UIModuleSoldierEquip.");
                        return;
                    }

                    // Use the public StorageList property.
                    var storageList = __instance.StorageList;
                    if (storageList == null)
                    {
                        // TFTVLogger.Always("StorageList is null in UIModuleSoldierEquip.");
                        return;
                    }

                    if (storageList.UnfilteredItems == null)
                    {
                        // TFTVLogger.Always("UnfilteredItems is null in StorageList.");
                        return;
                    }

                    // Iterate over each unfiltered item.
                    foreach (var item in storageList.UnfilteredItems)
                    {

                        // Attempt to cast the item’s ItemDef to TacticalItemDef.
                        var tacticalItemDef = item.ItemDef as TacticalItemDef;
                        if (tacticalItemDef == null)
                        {
                            TFTVLogger.Always($"ItemDef is null or not a TacticalItemDef. Item: {item} {item.GetType()}");

                            continue;
                        }

                        // Invoke the private TypeFilter method on the instance.
                        bool passesTypeFilter = (bool)typeFilterMethod.Invoke(__instance, new object[] { tacticalItemDef });
                        // Optionally log the result:
                        // TFTVLogger.Info($"Item: {item}, passesTypeFilter: {passesTypeFilter}");

                    }

                    int removedCount = storageList.UnfilteredItems.RemoveAll(item => !(item.ItemDef is TacticalItemDef));
                    if (removedCount > 0)
                    {
                        TFTVLogger.Info($"Removed {removedCount} items that are not TacticalItemDef from UnfilteredItems.");
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Commenting out removes info about HP, crew and modules on mouseover from all aircraft, also faction aircraft
        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetActorInfo")]
        public static class UIModuleSelectionInfoBox_SetActorInfo_Patch
        {
            public static bool Prefix(UIModuleSelectionInfoBox __instance,
                GeoscapeViewContext context, GeoActor actor, Vector3 tooltipPosition, float fov,
                ref GeoscapeViewContext ____context, ref RectTransform ____moduleRect, ref RectTransform ____panelRect, ref bool ____showTooltip)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return true;
                    }


                    MethodInfo methodInfoSetExtendedGeoVehicleInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetExtendedGeoVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo methodInfoSetGeoVehicleInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetGeoVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo methodInfoSetGeoSiteInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetGeoSiteInfo", BindingFlags.Instance | BindingFlags.NonPublic);


                    ____context = context;
                    __instance.ClearSelectionInfo();
                    __instance.PanelAlpha.alpha = 0f;


                    if (____moduleRect == null)
                    {
                        ____moduleRect = __instance.gameObject.GetComponent<RectTransform>();
                    }

                    ____moduleRect.position = new Vector3(tooltipPosition.x, tooltipPosition.y, 0f);
                    if (____panelRect == null)
                    {
                        ____panelRect = __instance.PanelAlpha.gameObject.GetComponent<RectTransform>();
                    }

                    ____panelRect.anchoredPosition = new Vector3(0f, 0f, 0f);

                    if (actor is GeoSite && (actor.GetComponent<GeoAlienBase>() == null || !actor.GetComponent<GeoAlienBase>().IsPalace))
                    {
                        ____panelRect.anchoredPosition = new Vector3(____panelRect.anchoredPosition.x + __instance.CenterXOffset.Evaluate(fov), ____panelRect.anchoredPosition.y + __instance.CenterYOffset.Evaluate(fov), 0f);
                        GeoSite geoSite = (GeoSite)actor;
                        __instance.BaseInformation.SetActive(value: true);
                        __instance.VehicleTooltipInfoRoot.SetActive(value: false);
                        if (geoSite.GetVisible(____context.ViewerFaction))
                        {
                            methodInfoSetGeoSiteInfo.Invoke(__instance, new object[] { geoSite });

                        }

                        ____showTooltip = true;
                    }
                    else if (actor is GeoVehicle)
                    {
                        ____panelRect.anchoredPosition = new Vector3(____panelRect.anchoredPosition.x + __instance.AircraftCenterXOffset.Evaluate(fov), ____panelRect.anchoredPosition.y + __instance.AircraftCenterYOffset.Evaluate(fov), 0f);
                        GeoVehicle geoVehicle = (GeoVehicle)actor;
                        if (geoVehicle.IsVisible && geoVehicle.IsOwnedByViewer)
                        {
                            __instance.BaseInformation.SetActive(value: false);
                            __instance.VehicleTooltipInfoRoot.SetActive(value: true);
                            methodInfoSetExtendedGeoVehicleInfo.Invoke(__instance, new object[] { geoVehicle });
                            // __instance.SetExtendedGeoVehicleInfo(geoVehicle);
                        }
                        else if (geoVehicle.IsVisible)
                        {
                            __instance.BaseInformation.SetActive(value: true);
                            __instance.VehicleTooltipInfoRoot.SetActive(value: false);
                            methodInfoSetGeoVehicleInfo.Invoke(__instance, new object[] { geoVehicle });
                        }

                        ____showTooltip = true;
                    }
                    else
                    {
                        ____showTooltip = false;
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


        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetExtendedGeoVehicleInfo")]
        public static class UIModuleSelectionInfoBox_SetExtendedGeoVehicleInfo_Patch
        {
            public static bool Prefix(UIModuleSelectionInfoBox __instance, GeoVehicle vehicle)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    AircraftInfoData aircraftInfo = vehicle.GetAircraftInfo();
                    __instance.VehicleCrewText.gameObject.SetActive(aircraftInfo.IsOwnedByViewer);
                    __instance.RootSeparator.gameObject.SetActive(true);//aircraftInfo.IsOwnedByViewer);
                    __instance.PersonalCrewContainer.SetActive(vehicle.IsOwnedByViewer);

                    // This sets up common visuals (unchanged)
                    AircraftEquipmentViewController.AircraftCommonEquipmentVisualData aircraftCommonEquipmentVisualData
                        = new AircraftEquipmentViewController.AircraftCommonEquipmentVisualData(vehicle);

                    float maintenanceLevel = (float)aircraftInfo.CurrentHitPoints / (float)aircraftInfo.MaxHitPoints;

                    //   TFTVLogger.Always($"{aircraftInfoData.DisplayName}: {maintenanceLevel}");

                    int maintenancePercentage = (int)(maintenanceLevel * 100);
                    aircraftCommonEquipmentVisualData.Health = maintenancePercentage;
                    aircraftCommonEquipmentVisualData.MaxHealth = 100;

                    aircraftCommonEquipmentVisualData.IsFriendlyModule = vehicle.IsOwnedByViewer;
                    __instance.AircraftInfo.SetEquipmentData(aircraftCommonEquipmentVisualData);

                    __instance.DisengageText.gameObject.SetActive(!vehicle.CanRedirect);
                    __instance.VehicleCrewText.text = $"{aircraftInfo.CurrentCrew}/{aircraftInfo.MaxCrew}";
                    //  __instance.VehicleArmorText.text = aircraftInfo.CurrentArmor.ToString();

                    List<GeoVehicleModuleDef> modules = vehicle.Modules.Select(m => m?.ModuleDef).ToList();



                    float maintenanceFactor = AircraftReworkMaintenance.GetMaintenanceFactor(modules);
                    float currentHitPoints = aircraftInfo.CurrentHitPoints;

                    if (maintenanceFactor > 0)
                    {

                        int flightHours = (int)Mathf.Max((currentHitPoints - 200) / maintenanceFactor, 0);
                        int flightHoursTotal = (int)Mathf.Max(currentHitPoints / maintenanceFactor, 0);

                        __instance.VehicleArmorText.text = $"{flightHours}({flightHoursTotal})";

                    }
                    else
                    {
                        __instance.VehicleArmorText.text = $"UNLIMITED";

                    }

                    __instance.VehicleArmorText.gameObject.SetActive(true);



                    Transform parent = __instance.VehicleArmorText.transform.parent;

                    //  TFTVLogger.Always($"parent is {parent.name}");

                    foreach (Component component in parent.GetComponentsInChildren<Component>())
                    {
                        if (component is Image image)
                        {
                            // TFTVLogger.Always($"image: {component.name} {component.GetType()}");
                            component.gameObject.SetActive(false);
                        }
                    }



                    // --- Filter out weapons, show only modules, limit to 3 if desired ---
                    List<GeoVehicleEquipment> list = vehicle.Equipments
                        .Where(eq => eq != null && eq.IsModule) // only modules
                        .Take(3)                                // up to 3
                        .ToList();

                    // The original sets the bottom separator visible if there's at least 1 piece of equipment
                    __instance.BottomSeparator.SetActive(list.Count > 0);

                    // Then the original loops over each equipment, assigning it to an EquipmentButton
                    int num = 0;
                    for (num = 0; num < list.Count; num++)
                    {
                        __instance.EquipmentButtons[num].SetEquipment(list[num]);
                        __instance.EquipmentButtons[num].gameObject.SetActive(true);
                    }

                    // Hide any leftover buttons
                    for (int i = num; i < __instance.EquipmentButtons.Count; i++)
                    {
                        if (__instance.EquipmentButtons[i].gameObject.activeSelf)
                        {
                            __instance.EquipmentButtons[i].gameObject.SetActive(false);
                        }
                    }

                    // Return false so the original method is skipped
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleVehicleSelection), "RefreshVehicleBars")]
        public static class UIModuleVehicleSelection_RefreshVehicleBars_Patch
        {
            public static void Postfix(UIModuleVehicleSelection __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    Slider slider = __instance.VehicleHPBar;

                    // Try to get the fill image
                    var fill = slider.fillRect?.GetComponent<Image>();
                    if (fill == null) return;

                    // TFTVLogger.Always($"got here");

                    // Choose color based on value
                    Color color;
                    if (slider.value > 0.5f)
                        color = Color.green;
                    else if (slider.value > 0.25f)
                        color = Color.yellow;
                    else
                        color = Color.red;

                    fill.color = color;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleActionsBar), "Awake")]
        public static class UIModuleActionsBar_Awake_Patch
        {
            public static void Postfix(UIModuleActionsBar __instance, ref List<ShortEquipmentInfoButton> ____shortEquipmentInfoButtons)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    ____shortEquipmentInfoButtons.Last().gameObject.SetActive(false);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleActionsBar), "SetEquipment")]
        public static class UIModuleActionsBar_SetEquipment_Patch
        {
            public static void Prefix(ref List<GeoVehicleEquipment> equipments, ref List<ShortEquipmentInfoButton> ____shortEquipmentInfoButtons)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    // Filter out any equipment that is not a module.
                    // Assuming that modules have a definition of type GeoVehicleModuleDef.
                    equipments = equipments
                        .Where(e => e?.EquipmentDef is GeoVehicleModuleDef)
                        .ToList();



                    // Force the list to exactly 3 entries:
                    while (equipments.Count < 3)
                    {
                        equipments.Add(null);
                    }
                    if (equipments.Count > 3)
                    {
                        // If for some reason there are more than 3 modules, trim the list.
                        equipments = equipments.Take(3).ToList();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(UIModuleVehicleSelection), "Init")]
        public static class UIModuleVehicleSelection_Init_Patch
        {
            public static void Prefix(UIModuleVehicleSelection __instance, GeoscapeViewContext context)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    context.View.SelectOnlyOwnedAircraft = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleVehicleSelection), "SetActiveAircraftListTab")]
        public static class UIModuleVehicleSelection_SetActiveAircraftListTab_Patch
        {
            public static bool Prefix(UIModuleVehicleSelection __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    __instance?.AircraftPanelBackground?.gameObject?.SetActive(false);

                    return false;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        private static List<GeoVehicleEquipmentUIData> _modules = new List<GeoVehicleEquipmentUIData>();

        [HarmonyPatch(typeof(AircraftInfoController), "SetInfo")]
        public static class AircraftInfoController_SetInfo_Patch
        {
            public static void Prefix(AircraftInfoController __instance, List<GeoVehicleEquipmentUIData> modules, AircraftInfoData aircraftInfoData)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    _modules = modules;

                    // TFTVLogger.Always($"AircraftInfoController.SetInfo: {aircraftInfoData.DisplayName} {aircraftInfoData.MaxCrew}");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            public static void Postfix(AircraftInfoController __instance, ref GeoVehicleRosterEquipmentSlot[] ____equipmentSlots)
            {

                try
                {
                    if (!AircraftReworkOn || _modules == null)
                    {
                        return;
                    }


                    __instance.WeaponSlot02.gameObject.SetActive(false);

                    // Add three module slots
                    if (_modules != null)
                    {
                        // TFTVLogger.Always($"Modules count: {modules.Count}");

                        if (_modules.Count >= 1)
                        {
                            __instance.WeaponSlot01.SetItem(_modules[0]);
                        }
                        else
                        {
                            __instance.WeaponSlot01.ResetItem();
                        }

                        if (_modules.Count >= 2)
                        {
                            __instance.ModuleSlot.SetItem(_modules[1]);
                        }
                        else
                        {
                            __instance.ModuleSlot.ResetItem();
                        }

                        if (_modules.Count >= 3)
                        {
                            // Assuming you have added a second and third module slot in the AircraftInfoController class
                            __instance.ModuleSlot2.SetItem(_modules[2]);
                        }
                        else
                        {
                            __instance.ModuleSlot2.ResetItem();
                        }
                    }

                    _modules.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(AircraftStatsController), "SetInfo")]
        public static class AircraftStatsController_SetInfo_Patch
        {
            public static void Postfix(AircraftStatsController __instance, AircraftInfoData aircraftInfoData)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }


                    float maintenanceLevel = (float)aircraftInfoData.CurrentHitPoints / (float)aircraftInfoData.MaxHitPoints;

                    //   TFTVLogger.Always($"{aircraftInfoData.DisplayName}: {maintenanceLevel}");

                    int maintenancePercentage = (int)(maintenanceLevel * 100);

                    __instance.AircraftHitPoints.text = $"{maintenancePercentage}%";

                    Transform parent = __instance.AircraftArmor.transform.parent;

                    //  TFTVLogger.Always($"parent is {parent.name}");

                    foreach (Component component in parent.GetComponentsInChildren<Component>())
                    {
                        if (component is Image image)
                        {
                            // TFTVLogger.Always($"image: {component.name} {component.GetType()}");
                            component.gameObject.SetActive(false);
                        }

                    }

                    Transform grandParent = parent.parent;

                    foreach (Component component in grandParent.GetComponentsInChildren<Component>())
                    {
                        if (component is Text text && text.name == "UITextGeneric_Small_StatName")
                        {
                            text.text = TFTVCommonMethods.ConvertKeyToString("Geoscape/KEY_AIRCRAFT_STATS_DURABILITY");
                            break;
                        }
                    }

                    List<GeoVehicleModuleDef> aircraftEquipmentDefs = new List<GeoVehicleModuleDef>();

                    if (_modules != null)
                    {
                        foreach (GeoVehicleEquipmentUIData module in _modules)
                        {
                            if (module != null && module.AircraftEquipmentDef != null && module.AircraftEquipmentDef is GeoVehicleModuleDef moduleDef)
                            {
                                aircraftEquipmentDefs.Add(moduleDef);
                            }
                        }
                    }

                    // TFTVLogger.Always($"modules count: {aircraftEquipmentDefs?.Count}");

                    float maintenanceFactor = AircraftReworkMaintenance.GetMaintenanceFactor(aircraftEquipmentDefs);
                    float currentHitPoints = aircraftInfoData.CurrentHitPoints;

                    if (maintenanceFactor > 0)
                    {

                        int flightHours = (int)Mathf.Max((currentHitPoints - 200) / maintenanceFactor, 0);
                        int flightHoursTotal = (int)Mathf.Max(currentHitPoints / maintenanceFactor, 0);

                        __instance.AircraftArmor.text = $"{flightHours}({flightHoursTotal})";
                        __instance.AircraftArmor.gameObject.SetActive(true);


                    }
                    else
                    {
                        __instance.AircraftArmor.text = $"UNLIMITED";
                        __instance.AircraftArmor.gameObject.SetActive(true);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(GeoVehicleRosterSlot), "UpdateVehicleEquipments")]
        public static class GeoVehicleRosterSlot_UpdateVehicleEquipments_Patch
        {
            static bool Prefix(GeoVehicleRosterSlot __instance)
            {

                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    if (__instance.Vehicle != null)
                    {
                        GeoVehicle baseObject = __instance.Vehicle.GetBaseObject<GeoVehicle>();

                        UIStatBar uIStatBar = __instance.VehicleHealthBar;

                        float maintenanceLevel = (float)baseObject.Stats.HitPoints / (float)baseObject.Stats.MaxHitPoints;

                        uIStatBar.SetValuePercent(maintenanceLevel);

                        if (uIStatBar != null && uIStatBar.CurrentValueBar != null)
                        {
                            if (maintenanceLevel > 0.5f)
                            {
                                uIStatBar.CurrentValueBar.color = Color.green;
                            }
                            else if (maintenanceLevel > 0.25f)
                            {
                                uIStatBar.CurrentValueBar.color = Color.yellow;
                            }
                            else
                            {
                                uIStatBar.CurrentValueBar.color = Color.red;
                            }
                        }

                        Transform statBarParentTransform = uIStatBar.transform.parent;


                        Text healthText = null;
                        foreach (Text text in statBarParentTransform.GetComponentsInChildren<Text>(true))
                        {
                            if (text.name == "Health_Text")
                            {
                                healthText = text;
                                break;
                            }
                        }
                        if (healthText != null)
                        {
                            healthText.text = TFTVCommonMethods.ConvertKeyToString("DLC 3 - Behemoth/KEY_DLC3_HULL_POINTS");
                        }


                        int maintenancePercentage = (int)(maintenanceLevel * 100);

                        __instance.VehicleHealthText.text = $"{maintenancePercentage}%";

                        //   __instance.VehicleHealthText.text = baseObject.Stats.HitPoints.ToString() + "/" + baseObject.Stats.MaxHitPoints;
                        List<GeoVehicleEquipmentUIData> list = baseObject.Modules.Select((GeoVehicleEquipment m) => m?.CreateUIData()).ToList();
                        // List<GeoVehicleEquipmentUIData> list2 = baseObject.Modules.Select((GeoVehicleEquipment m) => m?.CreateUIData()).ToList();
                        if (list.Count >= 1)
                        {
                            __instance.WeaponSlot01.SetItem(list[0]);
                        }
                        else
                        {
                            __instance.WeaponSlot01.ResetItem();
                        }

                        if (list.Count >= 2)
                        {
                            __instance.WeaponSlot02.SetItem(list[1]);
                        }
                        else
                        {
                            __instance.WeaponSlot02.ResetItem();
                        }

                        if (list.Count >= 3)
                        {
                            __instance.ModuleSlot.SetItem(list[2]);
                        }
                        else
                        {
                            __instance.ModuleSlot.ResetItem();
                        }
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




        [HarmonyPatch(typeof(UIVehicleEquipmentInventoryList), "Init")]
        public static class UIVehicleEquipmentInventoryList_Init_Patch
        {
            static void Prefix(UIVehicleEquipmentInventoryList __instance, ref IEnumerable<GeoVehicleEquipmentUIData> equipments)
            {

                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    // Identify the module list. Adjust the condition as needed.
                    if (__instance.gameObject.name.Contains("Module"))
                    {

                        // Set the fixed slot count to 3.
                        __instance.FixedCount = 3;

                        // Ensure the ItemSlotPrefab is not null.
                        if (__instance.ItemSlotPrefab == null)
                        {
                            // Try to use an existing slot as a template.
                            var fallback = __instance.GetComponentsInChildren<UIVehicleEquipmentInventorySlot>(true).FirstOrDefault();
                            if (fallback != null)
                            {
                                __instance.ItemSlotPrefab = fallback;
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





        [HarmonyPatch(typeof(UIModuleVehicleEquip), "UpdateData")]
        public static class UIModuleVehicleEquip_UpdateData_Patch
        {

            static void Postfix(UIModuleVehicleEquip __instance, IEnumerable<GeoVehicleEquipmentUIData> modules, bool ____inPhoenixBase)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    UIModuleVehicleRoster uIModuleVehicleRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.VehicleRoster;
                    GeoVehicle selectedAircraft = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                    // 1) Hide weapon list
                    __instance.WeaponList?.gameObject.SetActive(false);

                    // 2) Ensure modules are not restricted
                    __instance.ModuleList.InventoryListFilter = null; // Option A: no filter at all
                                                                      //  __instance.ModuleList.ReplaceByDefault = false;

                    // 3) Let them be interactive (if needed)                     
                    __instance.ModuleList.EnableEventHandlers = ____inPhoenixBase || !__instance.DisableListsInTransit;
                    __instance.StorageList.EnableEventHandlers = ____inPhoenixBase || !__instance.DisableListsInTransit;

                    // 4) Reinit the module list
                    if (modules != null)
                    {
                        __instance.ModuleList.Deinit();
                        __instance.ModuleList.Init(modules);
                    }


                    bool IsModuleCompatible(GeoVehicleModuleDef moduleDef, GeoVehicleDef vehicleDef)
                    {

                        if (vehicleDef == manticore)
                        {
                            return true;
                        }
                        else if (vehicleDef == blimp)
                        {
                            if (moduleDef == _vehicleHarnessModule)
                            {
                                return false;
                            }

                            if (_blimpModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                            {
                                return true;
                            }
                        }
                        else if (vehicleDef == helios)
                        {
                            if (_heliosModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                            {
                                return true;
                            }
                        }
                        else if (vehicleDef == thunderbird)
                        {
                            if (_thunderbirdModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                            {
                                return true;
                            }
                        }
                        return false;

                    }

                    // Now, set the filter on the storage list:
                    __instance.StorageList.SetFilter((GeoVehicleEquipmentDef def) =>
                    {
                        // Only show the module if it is a GeoVehicleModuleDef and is compatible with the current vehicle.
                        var moduleDef = def as GeoVehicleModuleDef;
                        if (moduleDef == null)
                        {
                            return false;
                        }
                        return IsModuleCompatible(moduleDef, selectedAircraft.VehicleDef);
                    });

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleVehicleEquip), "AttemptSlotSwap")]
        public static class PreventDuplicateModule_DragDrop_Patch
        {


            static bool Prefix(UIModuleVehicleEquip __instance, UIVehicleEquipmentInventorySlot sourceSlot, UIVehicleEquipmentInventorySlot destinationSlot, ref bool __result)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    // Only check if we're moving an item into the module list
                    // AND if the source is not already in the module list (i.e. we're equipping from storage).
                    if (sourceSlot.ParentList != __instance.ModuleList && destinationSlot.ParentList == __instance.ModuleList)
                    {
                        // If any module slot already has a module with the same definition as the one we're trying to add,
                        // then cancel the swap.
                        if (__instance.ModuleList.Slots.Any(s => !s.Empty && s.Equipment != null &&
                               s.Equipment.AircraftEquipmentDef == sourceSlot.Equipment.AircraftEquipmentDef))
                        {
                            __instance.DeselectSlot();
                            __result = false;
                            return false; // Skip original method.
                        }
                    }


                    // Otherwise, let the original method run.
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }



        [HarmonyPatch(typeof(UIModuleVehicleEquip), "HandleDoubleclickOnSlot")]
        public static class ModuleDoubleClickRefinedPatch
        {
            static bool Prefix(UIVehicleEquipmentInventorySlot clickedSlot, UIModuleVehicleEquip __instance, ref bool __result)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    // Get references for clarity.
                    var moduleList = __instance.ModuleList;
                    var storageList = __instance.StorageList;

                    // Case 1: Double-click on a module in storage (attempting to equip it)
                    if (clickedSlot.ParentList == storageList)
                    {
                        // Save the new module (from storage) that we want to equip.
                        GeoVehicleEquipmentUIData newModule = clickedSlot.Equipment;
                        if (newModule == null)
                        {
                            __result = false;
                            return false;
                        }

                        // Check if a module with the same definition is already equipped.
                        if (moduleList.Slots.Any(s => !s.Empty && s.Equipment != null &&
                                s.Equipment.AircraftEquipmentDef == newModule.AircraftEquipmentDef))
                        {
                            UnityEngine.Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
                            __result = false;
                            return false;
                        }

                        // Check for an empty slot in the module list.
                        var emptySlot = moduleList.Slots.FirstOrDefault(s => s.Empty);
                        if (emptySlot != null)
                        {
                            // Use the original swap method to equip into an empty slot.
                            var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                            bool success = (bool)attemptMethod.Invoke(__instance, new object[] { clickedSlot, emptySlot });
                            __result = success;
                            return false;
                        }
                        else
                        {
                            // All module slots are filled.
                            // We want to replace the oldest module by shifting the equipped modules.
                            var slots = moduleList.Slots;

                            // (Double-check for duplicates, though this should have been caught above.)
                            if (slots.Any(s => !s.Empty && s.Equipment != null &&
                                s.Equipment.AircraftEquipmentDef == newModule.AircraftEquipmentDef))
                            {
                                UnityEngine.Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
                                __result = false;
                                return false;
                            }

                            // Save the module in slot 0 (the oldest) for removal.
                            GeoVehicleEquipmentUIData oldModule = slots[0].Equipment;
                            // Find a free slot in storage for the module being removed.
                            var freeStorageSlot = storageList.GetFirstAvailableSlot(oldModule, false);
                            if (freeStorageSlot == null)
                            {
                                UnityEngine.Debug.LogError("No free storage slot available for the replaced module.");
                                __result = false;
                                return false;
                            }

                            // Swap the oldest module into storage.
                            var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                            attemptMethod.Invoke(__instance, new object[] { slots[0], freeStorageSlot });

                            // Build the new ordering:
                            // • Slot 0 gets what was in slot 1.
                            // • Slot 1 gets what was in slot 2.
                            // • Slot 2 receives the new module.
                            GeoVehicleEquipmentUIData[] newModules = new GeoVehicleEquipmentUIData[3];
                            newModules[0] = slots[1].Equipment;
                            newModules[1] = slots[2].Equipment;
                            newModules[2] = newModule;

                            // Remove the new module from storage.
                            clickedSlot.Equipment = null;

                            // Reinitialize the module list with the new ordering.
                            moduleList.Deinit();
                            moduleList.Init(newModules);
                            __result = true;
                            return false;
                        }
                    }
                    // Case 2: Double-click on a module that is already equipped (attempting to unequip it)
                    else if (clickedSlot.ParentList == moduleList)
                    {
                        // Unequip by moving it back to storage.
                        var storageSlot = storageList.GetFirstAvailableSlot(clickedSlot.Equipment, false);
                        if (storageSlot != null)
                        {
                            var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                            bool success = (bool)attemptMethod.Invoke(__instance, new object[] { clickedSlot, storageSlot });
                            __result = success;
                        }
                        else
                        {
                            __result = false;
                        }
                        return false;
                    }

                    // For any other list, use the original behavior.
                    return true;
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

