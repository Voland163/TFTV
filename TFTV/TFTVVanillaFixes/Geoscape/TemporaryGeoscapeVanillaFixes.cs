using Base.Serialization.General;
using Base.Utils;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    internal class TemporaryGeoscapeVanillaFixes
    {
        [HarmonyPatch]
        internal static class PreferredLoadoutDuplicateCountPatch
        {
            private static readonly Type LoadoutType =
                AccessTools.Inner(typeof(PostmissionReplenishManager), "Loadout");

            private static readonly FieldInfo ArmourItemsField =
                AccessTools.Field(LoadoutType, "_armourItems");

            private static readonly FieldInfo EquipmentItemsField =
                AccessTools.Field(LoadoutType, "_equipmentItems");

            private static readonly FieldInfo InventoryItemsField =
                AccessTools.Field(LoadoutType, "_inventoryItems");

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    LoadoutType,
                    "IsChanged",
                    new[] { typeof(GeoCharacter) });
            }

            private static bool Prefix(
                object __instance,
                GeoCharacter character,
                ref bool __result)
            {
                List<GeoItem> oldArmour =
                    (List<GeoItem>)ArmourItemsField.GetValue(__instance);

                List<GeoItem> oldEquipment =
                    (List<GeoItem>)EquipmentItemsField.GetValue(__instance);

                List<GeoItem> oldInventory =
                    (List<GeoItem>)InventoryItemsField.GetValue(__instance);

                __result =
                    AreDifferent(oldArmour, character.ArmourItems) ||
                    AreDifferent(oldEquipment, character.EquipmentItems) ||
                    AreDifferent(oldInventory, character.InventoryItems);

                // Skip the original Except-based implementation.
                return false;
            }

            private static bool AreDifferent(
                IReadOnlyList<GeoItem> oldItems,
                IReadOnlyList<GeoItem> newItems)
            {
                if (ReferenceEquals(oldItems, newItems))
                {
                    return false;
                }

                if (oldItems == null || newItems == null)
                {
                    return oldItems != null || newItems != null;
                }

                // This check alone catches the reported one-versus-two grenade case.
                if (oldItems.Count != newItems.Count)
                {
                    return true;
                }

                // Consume exactly one new item for every old item. This preserves
                // duplicate multiplicity while retaining the game's GeoItem.Equals
                // behavior for item state such as count and current charges.
                bool[] matched = new bool[newItems.Count];

                for (int oldIndex = 0; oldIndex < oldItems.Count; oldIndex++)
                {
                    GeoItem oldItem = oldItems[oldIndex];
                    bool found = false;

                    for (int newIndex = 0; newIndex < newItems.Count; newIndex++)
                    {
                        if (matched[newIndex])
                        {
                            continue;
                        }

                        if (!Equals(oldItem, newItems[newIndex]))
                        {
                            continue;
                        }

                        matched[newIndex] = true;
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Re-evaluates a failed assignment while treating every tag whose name
        /// contains TFTV_ATTACK as absent.
        /// Every owner, type, mission, functioning, objective, event-condition, and
        /// encounter-availability check remains the responsibility of the game.
        /// </summary>
        [HarmonyPatch(typeof(GeoscapeEventDef), "CanBeAssignedToSite")]
        internal static class CanBeAssignedToSitePatch
        {
            private const string IgnoredTagNameFragment = "TFTV_ATTACK";

            private static void Postfix(
                GeoscapeEventDef __instance,
                GeoSite site,
                GeoFaction visitingFaction,
                bool ignoreNonFunctioning,
                ref bool __result)
            {
                if (__result ||
                    !site.SiteTags.Any(IsIgnoredTag))
                {
                    return;
                }

                bool passesNormalChecks =
                    !site.HasDiplomaticObjective &&
                    __instance.SiteFilters.Any(filter => filter.IsValidEventForSite(site, ignoreNonFunctioning)) &&
                    __instance.GeoscapeEventData.IsValidForSite(site, visitingFaction);

                bool hasOtherTags = site.SiteTags.Any(tag => !IsIgnoredTag(tag));
                bool hasSiteCondition = __instance.GeoscapeEventData.Conditions.Any(
                    condition => condition is SiteConditionDef);

                if (passesNormalChecks && (!hasOtherTags || hasSiteCondition))
                {
                    __result = true;
                    Debug.Log(string.Format(
                        "[TFTV_ATTACK tag eligibility] Event {0} accepted site {1} after ignoring tags [{2}]",
                        __instance.EventID,
                        site.name,
                        string.Join(",", site.SiteTags.Where(IsIgnoredTag).ToArray())));
                }
            }

            private static bool IsIgnoredTag(string tag)
            {
                return tag != null && tag.IndexOf(IgnoredTagNameFragment, StringComparison.Ordinal) >= 0;
            }
        }

        [HarmonyPatch(typeof(SerializationWriter), MethodType.Constructor, new Type[] { typeof(Serializer) })]
        internal static class SerializationWriterConstructorPatch
        {
            private static void Postfix(ref Dictionary<object, int> ____object2ID)
            {
                // Object IDs describe graph identity. Value equality can merge distinct
                // GeoItem instances into one serialized object shared by many soldiers.
                ____object2ID = new Dictionary<object, int>(ReferenceEqualityComparer<object>.Default);
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")]
        internal static class SharedGeoItemRepairPatch
        {
            private static void Prefix(List<GeoCharacter> ____characters)
            {
                HashSet<GeoItem> seenItems = new HashSet<GeoItem>(ReferenceEqualityComparer<GeoItem>.Default);
                foreach (GeoCharacter character in ____characters)
                {
                    bool repaired = false;
                    List<GeoItem> armour = CloneSharedItems(character.ArmourItems, seenItems, ref repaired);
                    List<GeoItem> equipment = CloneSharedItems(character.EquipmentItems, seenItems, ref repaired);
                    List<GeoItem> inventory = CloneSharedItems(character.InventoryItems, seenItems, ref repaired);
                    if (repaired)
                    {
                        character.SetItems(armour, equipment, inventory, false);
                        Debug.LogWarning(
                            "[InventoryDuplicateItemFix] Repaired shared GeoItem references for "
                            + InventoryDiagnostics.CharacterName(character));
                    }
                }
            }

            private static List<GeoItem> CloneSharedItems(
                IEnumerable<GeoItem> items,
                HashSet<GeoItem> seenItems,
                ref bool repaired)
            {
                List<GeoItem> result = new List<GeoItem>();
                foreach (GeoItem item in items)
                {
                    GeoItem itemForCharacter = item;
                    if (!seenItems.Add(item))
                    {
                        // GeoItem.Clone() loses AmmoManager.LoadedMagazines. Round-trip
                        // through ItemData so loaded magazines and their charges survive.
                        itemForCharacter = new GeoItem(item.ToItemData());
                        seenItems.Add(itemForCharacter);
                        repaired = true;
                    }
                    result.Add(itemForCharacter);
                }
                return result;
            }
        }

        internal static class InventoryDiagnostics
        {
            internal static Dictionary<GeoCharacter, string> Capture(IEnumerable<GeoCharacter> characters)
            {
                return characters.ToDictionary(character => character, Describe);
            }

            internal static void ReportUnexpectedChanges(
                string operation,
                GeoCharacter expectedCharacter,
                Dictionary<GeoCharacter, string> before)
            {
                foreach (KeyValuePair<GeoCharacter, string> entry in before)
                {
                    string after = Describe(entry.Key);
                    if (after != entry.Value && entry.Key != expectedCharacter)
                    {
                        Debug.LogError(
                            "[InventoryDuplicateItemFix] " + operation
                            + " unexpectedly changed non-target character " + CharacterName(entry.Key)
                            + "\nBEFORE " + entry.Value
                            + "\nAFTER  " + after);
                    }
                }
            }

            internal static string CharacterName(GeoCharacter character)
            {
                return character.DisplayName + "#" + RuntimeHelpers.GetHashCode(character);
            }

            internal static string Describe(GeoCharacter character)
            {
                StringBuilder result = new StringBuilder();
                AppendItems(result, "ready", character.EquipmentItems);
                AppendItems(result, "inventory", character.InventoryItems);
                AppendItems(result, "armour", character.ArmourItems);
                return result.ToString();
            }

            private static void AppendItems(StringBuilder result, string label, IEnumerable<GeoItem> items)
            {
                result.Append(label).Append("=[");
                result.Append(string.Join(", ", items.Select(item =>
                    item.ItemDef.name
                    + " x" + item.CommonItemData.Count
                    + " charges=" + item.CommonItemData.CurrentCharges
                    + " ref=" + RuntimeHelpers.GetHashCode(item))));
                result.Append("] ");
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "ItemScrappedHandler")]
        internal static class ItemScrappedDiagnosticPatch
        {
            private static void Prefix(
                UIInventorySlot slot,
                int scrappedAmount,
                List<GeoCharacter> ____characters,
                GeoCharacter ____currentCharacter,
                ref Dictionary<GeoCharacter, string> __state)
            {
                __state = InventoryDiagnostics.Capture(____characters);
                Debug.Log(
                    "[InventoryDuplicateItemFix] Scrapping " + scrappedAmount + " from "
                    + InventoryDiagnostics.CharacterName(____currentCharacter)
                    + "; slot item=" + slot.Item.ItemDef.name
                    + " ref=" + RuntimeHelpers.GetHashCode(slot.Item));
            }

            private static void Postfix(
                GeoCharacter ____currentCharacter,
                Dictionary<GeoCharacter, string> __state)
            {
                InventoryDiagnostics.ReportUnexpectedChanges(
                    "ItemScrappedHandler",
                    ____currentCharacter,
                    __state);
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "UpdateSoldierEquipment")]
        internal static class UpdateSoldierEquipmentDiagnosticPatch
        {
            private static void Prefix(
                GeoCharacter soldier,
                List<GeoCharacter> ____characters,
                ref Dictionary<GeoCharacter, string> __state)
            {
                __state = InventoryDiagnostics.Capture(____characters);
               /* Debug.Log(
                    "[InventoryDuplicateItemFix] Persisting UI equipment into "
                    + InventoryDiagnostics.CharacterName(soldier));*/
            }

            private static void Postfix(
                GeoCharacter soldier,
                Dictionary<GeoCharacter, string> __state)
            {
                InventoryDiagnostics.ReportUnexpectedChanges(
                    "UpdateSoldierEquipment",
                    soldier,
                    __state);
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), "ItemChangingHandler")]
        internal static class ItemChangingHandlerPatch
        {
            private static bool Prefix(
                UIInventoryList __instance,
                ICommonItem oldItem,
                ref bool ____isFiltering)
            {
                if (!____isFiltering && oldItem != null)
                {
                    RemoveByReference(__instance.UnfilteredItems, oldItem);
                }

                // The original uses List.Remove(), which invokes GeoItem.Equals().
                return false;
            }

            internal static void RemoveByReference(List<ICommonItem> items, ICommonItem item)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (ReferenceEquals(items[i], item))
                    {
                        items.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), "ItemChangedHandler")]
        internal static class ItemChangedHandlerPatch
        {
            private static bool Prefix(
                UIInventoryList __instance,
                UIInventorySlot slot,
                ICommonItem oldItem,
                ICommonItem newItem,
                Predicate<TacticalItemDef> ____filter,
                ref bool ____isFiltering)
            {
                if (____isFiltering)
                {
                    return false;
                }

                if (oldItem != null)
                {
                    ItemChangingHandlerPatch.RemoveByReference(__instance.UnfilteredItems, oldItem);
                    ItemChangingHandlerPatch.RemoveByReference(__instance.FilteredItems, oldItem);
                }

                if (newItem == null)
                {
                    return false;
                }

                int index = __instance.Slots.IndexOf(slot);
                if (index != -1)
                {
                    __instance.UnfilteredItems.Insert(Math.Min(index, __instance.UnfilteredItems.Count), newItem);
                }
                else
                {
                    __instance.UnfilteredItems.Add(newItem);
                }

                if (____filter == null || ____filter(newItem.ItemDef as TacticalItemDef))
                {
                    __instance.FilteredItems.Add(newItem);
                    return false;
                }

                ____isFiltering = true;
                slot.Item = null;
                slot.UpdateItem();
                ____isFiltering = false;
                return false;
            }
        }


        //temporary fix for 1.30 locate phoenix base function
        [HarmonyPatch(typeof(UIStatePhoenixBaseLayout), "ShowBaseOnGeoscape")]
        internal static class LocatePhoenixBaseFocusPatch
        {
            private static bool Prefix(UIStatePhoenixBaseLayout __instance, GeoPhoenixBase ____base)
            {
                GeoLevelController geoLevelController = ____base.Site.GeoLevel;

                GeoscapeView view = geoLevelController.View;

                GeoVehicle currentVehicle = view.SelectedActor as GeoVehicle;

                if (currentVehicle == null || !currentVehicle.IsOwnedByViewer)
                {
                    currentVehicle = geoLevelController.PhoenixFaction.Vehicles.FirstOrDefault<GeoVehicle>();
                }
                if (currentVehicle == null)
                {
                    view.ChaseTarget(____base.Site, false);
                    return false;
                }
                List<GeoVehicle> visibleVehicles = view.VisibleVehicles.ToList();
                int currentVehicleIndex = visibleVehicles.IndexOf(currentVehicle);
                if (currentVehicleIndex >= 0)
                {
                    AccessTools.Field(typeof(GeoscapeView), "_lastSelectedVehicle").SetValue(view, currentVehicleIndex);
                }
                view.SelectActorAndVehicle(____base.Site, false);
                return false;
            }
        }
    }
}
