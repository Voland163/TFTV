using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
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