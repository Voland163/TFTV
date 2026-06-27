using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TFTV.Vehicles.Ammo.VehicleModuleAmmoHarmonyPatches;

namespace TFTV.Vehicles.Ammo
{
    internal class MissionEndReplenish
    {
        private sealed class AmmoDefHolder
        {
            public TacticalItemDef AmmoDef;
        }

        private static readonly ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder> ReplenishAmmoDefs = new ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder>();

        // Vanilla CanManufacture crashes (NRE on ManufacturableItem.ManufacturePrice) for any
        // ItemDef absent from the faction's manufacture list, because AddMissingItem and
        // ReplenishList both set ManufacturableItem = GetManufacturableItemByDef(def) without a
        // null guard before calling CanManufacture. This affects GroundVehicleModuleDef items AND
        // vehicle-specific ammo clips (e.g. hailstorm_AmmoClipDef) that end up in MissingInventory.
        // Fix: strip all non-manufacturable items from every missing-items list at the source, and
        // prune entries that become empty (prevents a ghost soldier header in the replenish UI).
        [HarmonyPatch]
        public static class PostmissionReplenishManager_GetMissingItems_Patch
        {
            static MethodBase TargetMethod()
            {
                return typeof(PostmissionReplenishManager).GetMethod(
                    "GetMissingItems",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
            }

            public static void Postfix(List<PostmissionReplenishManager.ReplenishableItems> __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                        return;

                    TFTVLogger.Always($"[ReplenishFix] GetMissingItems Postfix running; entry count={__result?.Count ?? -1}");

                    if (__result == null)
                        return;

                    GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    GeoPhoenixFaction phoenixFaction = controller?.PhoenixFaction;

                    if (phoenixFaction == null)
                        TFTVLogger.Always($"[ReplenishFix] PhoenixFaction unavailable; falling back to GroundVehicleModuleDef-only filter.");

                    for (int i = __result.Count - 1; i >= 0; i--)
                    {
                        PostmissionReplenishManager.ReplenishableItems replenishable = __result[i];
                        if (replenishable == null)
                            continue;

                        string charName = replenishable.Character?.DisplayName ?? "null";
                        TFTVLogger.Always($"[ReplenishFix] Character '{charName}': " +
                            $"MissingEquip={replenishable.MissingEquipmentItems?.Count ?? 0} " +
                            $"MissingInv={replenishable.MissingInventoryItems?.Count ?? 0} " +
                            $"MissingArmour={replenishable.MissingArmourItems?.Count ?? 0} " +
                            $"Reloadable={replenishable.ReloadableItems?.Count ?? 0}");

                        if (replenishable.MissingEquipmentItems != null)
                            foreach (ItemDef def in replenishable.MissingEquipmentItems)
                                TFTVLogger.Always($"[ReplenishFix]   MissingEquip: {def?.name} ({def?.GetType()?.Name})");

                        int removed;
                        if (phoenixFaction != null)
                        {
                            removed = replenishable.MissingEquipmentItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                            removed += replenishable.MissingInventoryItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                            removed += replenishable.MissingArmourItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                        }
                        else
                        {
                            // Faction unavailable: narrow fallback keeps the game safe for module defs.
                            removed = replenishable.MissingEquipmentItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                            removed += replenishable.MissingInventoryItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                            removed += replenishable.MissingArmourItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                        }

                        if (removed > 0)
                            TFTVLogger.Always($"[ReplenishFix] Stripped {removed} non-manufacturable item(s) for '{charName}'.");

                        // If nothing remains for this character, remove the entry entirely so no
                        // ghost soldier header appears in the replenish UI.
                        if (replenishable.IsEmpty())
                        {
                            TFTVLogger.Always($"[ReplenishFix] Entry for '{charName}' is empty after filtering; removing.");
                            __result.RemoveAt(i);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // Final safety net: if anything slips through the Postfix filter, block it here rather
        // than letting vanilla CanManufacture crash on a null ManufacturableItem.
        [HarmonyPatch]
        public static class UIModuleReplenish_AddMissingItem_Patch
        {
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoPhoenixFaction> FactionRef =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoPhoenixFaction>("_faction");

            static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UIModuleReplenish), "AddMissingItem");

            public static bool Prefix(UIModuleReplenish __instance, GeoCharacter character, ItemDef def, ref bool __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                        return true;

                    GeoPhoenixFaction faction = FactionRef(__instance);
                    if (faction != null && faction.Manufacture.GetManufacturableItemByDef(def) == null)
                    {
                        TFTVLogger.Always($"[ReplenishFix] AddMissingItem safety-net: blocking '{def?.name}' ({def?.GetType()?.Name})" +
                            $" for '{character?.DisplayName}' — not in faction.Manufacture.");
                        __result = false;
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        // Intercept TryReloadItem for GroundVehicleModuleDef items, which vanilla does not handle.
        // Vanilla's non-equipment branch looks for the module def itself in storage (wrong),
        // and the equipment branch (CompatibleAmmunition) is only reached for EquipmentDef.
        // We handle each sub-weapon ammo def independently using our existing helpers.
        [HarmonyPatch(typeof(GeoMission), "TryReloadItem")]
        public static class GeoMission_TryReloadItem_Patch
        {
            public static bool Prefix(GeoItem item, ItemStorage storage, string storageName, ref bool __result)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    return true;

                GroundVehicleModuleDef moduleDef = item?.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null)
                    return true;

                if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    __result = true;
                    return false;
                }

                bool allFull = true;
                foreach (TacticalItemDef ammoDef in GetModuleAmmoDefs(moduleDef))
                {
                    int maxCharges = GetAmmoCapacityForDef(moduleDef, ammoDef);
                    if (maxCharges <= 0)
                        continue;

                    int currentCharges = GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                    if (currentCharges >= maxCharges)
                        continue;

                    if (!storage.Items.ContainsKey(ammoDef))
                    {
                        Debug.Log($"POSTMISSION RELOAD: Trying to reload {item} ({ammoDef.name}) but there is no ammo available in {storageName}!");
                        allFull = false;
                        continue;
                    }

                    GeoItem storageAmmo = storage.Items[ammoDef];
                    int clipSize = ammoDef.ChargesMax;

                    while (!storageAmmo.CommonItemData.IsEmpty())
                    {
                        int needed = maxCharges - GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                        if (needed <= 0)
                            break;

                        int toLoad = Mathf.Min(needed, clipSize);
                        GeoItem clip = new GeoItem(ammoDef, 1, -1, null, -100);
                        // Zero out and set to exactly toLoad charges (handles partial last clip)
                        clip.CommonItemData.ModifyCharges(-clip.CommonItemData.CurrentCharges, false);
                        clip.CommonItemData.ModifyCharges(toLoad, false);
                        item.CommonItemData.Ammo.LoadMagazine(clip);
                        storageAmmo.CommonItemData.Subtract(clip);
                    }

                    int finalCharges = GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                    Debug.Log($"POSTMISSION RELOAD: Reloaded {item} ({ammoDef.name}) now at {finalCharges}/{maxCharges} from {storageName}!");

                    if (storageAmmo.CommonItemData.IsEmpty())
                    {
                        storage.RemoveItem(storageAmmo);
                        Debug.Log($"POSTMISSION RELOAD: No more {ammoDef.name} left in {storageName}!");
                    }

                    if (finalCharges < maxCharges)
                        allFull = false;
                }

                __result = allFull;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIModuleReplenish), "AddMissingAmmo")]
        public static class UIModuleReplenish_AddMissingAmmo_Patch
        {
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoscapeViewContext> ReplenishContext =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoscapeViewContext>("_context");
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoPhoenixFaction> ReplenishFaction =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoPhoenixFaction>("_faction");

            private static InteractHandler GetInteractHandler(UIModuleReplenish instance, string methodName)
            {
                var methodInfo = AccessTools.Method(typeof(UIModuleReplenish), methodName);
                if (methodInfo == null)
                {
                    return null;
                }
                return (InteractHandler)Delegate.CreateDelegate(typeof(InteractHandler), instance, methodInfo);
            }

            private static Action<GeoManufactureItem> GetManufactureItemHandler(UIModuleReplenish instance, string methodName)
            {
                var methodInfo = AccessTools.Method(typeof(UIModuleReplenish), methodName);
                if (methodInfo == null)
                {
                    return null;
                }
                return (Action<GeoManufactureItem>)Delegate.CreateDelegate(typeof(Action<GeoManufactureItem>), instance, methodInfo);
            }

            public static bool Prefix(UIModuleReplenish __instance, GeoCharacter character, GeoItem item, ref int materialsCost, ref int techCost, ref bool __result)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                TFTVLogger.Always($"UIModuleReplenish_AddMissingAmmo_Patch Prefix called for item {item.ItemDef.name}");

                var moduleDef = (item != null) ? (item.ItemDef as GroundVehicleModuleDef) : null;
                if (moduleDef == null)
                {
                    return true;
                }

                TFTVLogger.Always($"UIModuleReplenish_AddMissingAmmo_Patch got past here for {item.ItemDef.name}");

                if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    __result = false;
                    return false;
                }
                bool flag = false;
                foreach (TacticalItemDef tacticalItemDef in GetModuleAmmoDefs(moduleDef))
                {
                    int maxCharges = GetAmmoCapacityForDef(moduleDef, tacticalItemDef);
                    if (maxCharges <= 0)
                    {
                        continue;
                    }
                    int currentCharges = GetAmmoChargesForDef(item.CommonItemData, tacticalItemDef);

                    TFTVLogger.Always($"UIModuleReplenish_AddMissingAmmo_Patch current charges for {tacticalItemDef.name} {currentCharges}, max charges {maxCharges}");

                    if (currentCharges >= maxCharges)
                    {
                        continue;
                    }
                    float num = (float)currentCharges / (float)maxCharges;

                    ResourcePack repairCost = GeoCharacter.GetRepairCost(tacticalItemDef, num);
                    GeoManufactureItem geoManufactureItem = UnityEngine.Object.Instantiate<GeoManufactureItem>(__instance.ItemListPrefab, __instance.ItemListContainer);
                    GeoManufactureItem geoManufactureItem2 = geoManufactureItem;
                    InteractHandler interactHandler = GetInteractHandler(__instance, "OnEnterSlot");
                    if (interactHandler != null)
                    {
                        geoManufactureItem2.OnEnter = (InteractHandler)Delegate.Combine(geoManufactureItem2.OnEnter, interactHandler);
                    }
                    GeoManufactureItem geoManufactureItem3 = geoManufactureItem;
                    InteractHandler interactHandler2 = GetInteractHandler(__instance, "OnExitSlot");
                    if (interactHandler2 != null)
                    {
                        geoManufactureItem3.OnExit = (InteractHandler)Delegate.Combine(geoManufactureItem3.OnExit, interactHandler2);
                    }
                    GeoManufactureItem geoManufactureItem4 = geoManufactureItem;
                    Action<GeoManufactureItem> action = GetManufactureItemHandler(__instance, "SingleItemReloadAndRefresh");
                    if (action != null)
                    {
                        geoManufactureItem4.OnSelected = (Action<GeoManufactureItem>)Delegate.Combine(geoManufactureItem4.OnSelected, action);
                    }
                    GeoscapeViewContext geoscapeViewContext = ReplenishContext(__instance);
                    geoManufactureItem.Init(tacticalItemDef, geoscapeViewContext.ViewerFaction, repairCost, false);
                    geoManufactureItem.CanCraftQuantityText.transform.parent.gameObject.SetActive(false);
                    ReplenishmentElementController.CreateAndAdd(geoManufactureItem.gameObject, ReplenishmentType.Reload, character, item.ItemDef, item);
                    ReplenishAmmoDefs.Remove(geoManufactureItem);
                    ReplenishAmmoDefs.Add(geoManufactureItem, new AmmoDefHolder
                    {
                        AmmoDef = tacticalItemDef
                    });
                    __instance.Items.Add(geoManufactureItem);
                    GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;
                    GeoPhoenixFaction geoPhoenixFaction = ReplenishFaction(__instance);
                    bool flag2 = geoPhoenixFaction.Wallet.HasResources(repairCost) && tacticalItemDef.Tags.Contains(manufacturableTag) && geoPhoenixFaction.Manufacture.Contains(tacticalItemDef);
                    PhoenixGeneralButton component = geoManufactureItem.AddToQueueButton.GetComponent<PhoenixGeneralButton>();
                    if (component != null)
                    {
                        component.SetEnabled(flag2);
                    }
                    geoManufactureItem.AddToQueueButton.SetInteractable(flag2);
                    if (flag2)
                    {
                        materialsCost += repairCost.ByResourceType(ResourceType.Materials).RoundedValue;
                        techCost += repairCost.ByResourceType(ResourceType.Tech).RoundedValue;
                        flag = true;
                    }
                }
                __result = flag;
                return false;
            }
        }

        /*     [HarmonyPatch(typeof(UIModuleReplenish), "SingleItemReloadAndRefresh")]
             public static class UIModuleReplenish_SingleItemReloadAndRefresh_Patch
             {
                 public static bool Prefix(UIModuleReplenish __instance, GeoManufactureItem item)
                 {
                     if (!TFTVAircraftReworkMain.AircraftReworkOn)
                     {
                         return true;
                     }

                     if (item == null)
                     {
                         return true;
                     }
                     AmmoDefHolder ammoDefHolder;
                     if (!ReplenishAmmoDefs.TryGetValue(item, out ammoDefHolder))
                     {
                         return true;
                     }
                     GeoItem item2 = item.GetComponent<ReplenishmentElementController>().Item;
                     if (ReloadModuleAmmo(item2, ammoDefHolder.AmmoDef))
                     {
                         AccessTools.Method(typeof(UIModuleReplenish), "RemoveFromList")?.Invoke(__instance, new object[] { item, true });
                         AccessTools.Method(typeof(UIModuleReplenish), "RefreshItemList")?.Invoke(__instance, null);
                     }
                     return false;
                 }
             }*/
    }
}