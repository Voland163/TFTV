using Base;
using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVUI.Personnel
{
    internal static class LoadoutService
    {
        private const string ArmourItemsString = "ArmourItems";
        private const string EquipmentItemsString = "EquipmentItems";
        private const string InventoryItemsString = "InventoryItems";

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal sealed class LoadoutSpec
        {
            internal List<string> Armour { get; } = new List<string>();
            internal List<string> Equipment { get; } = new List<string>();
            internal List<string> Inventory { get; } = new List<string>();

            internal static LoadoutSpec FromDictionary(Dictionary<string, List<string>> data)
            {
                if (data == null)
                {
                    return null;
                }

                LoadoutSpec spec = new LoadoutSpec();
                if (data.TryGetValue(ArmourItemsString, out List<string> armour))
                {
                    spec.Armour.AddRange(armour);
                }
                if (data.TryGetValue(EquipmentItemsString, out List<string> equipment))
                {
                    spec.Equipment.AddRange(equipment);
                }
                if (data.TryGetValue(InventoryItemsString, out List<string> inventory))
                {
                    spec.Inventory.AddRange(inventory);
                }
                return spec;
            }
        }

        internal sealed class LoadoutApplyResult
        {
            internal List<string> MissingItems { get; } = new List<string>();
        }

        internal sealed class LoadoutPlan
        {
            internal GeoCharacter Character { get; }
            internal LoadoutSpec Desired { get; }
            internal LoadoutApplyResult FirstPassResult { get; } = new LoadoutApplyResult();

            internal LoadoutPlan(GeoCharacter character, LoadoutSpec desired)
            {
                Character = character;
                Desired = desired;
            }
        }

        internal static IReadOnlyList<LoadoutApplyResult> ApplySquadLoadouts(
            IReadOnlyList<GeoCharacter> squad,
            Dictionary<int, Dictionary<string, List<string>>> characterLoadouts,
            bool allowReassignFromOtherOperatives = true)
        {
            if (squad == null || squad.Count == 0 || characterLoadouts == null)
            {
                TFTVLogger.Always("[LoadoutService] ApplySquadLoadouts aborted: squad or loadouts missing.");
                return Array.Empty<LoadoutApplyResult>();
            }

            TFTVLogger.Always($"[LoadoutService] ApplySquadLoadouts starting. SquadCount={squad.Count} LoadoutCount={characterLoadouts.Count} Reassign={allowReassignFromOtherOperatives}.");

            GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
            GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
            ItemStorage storage = phoenixFaction.ItemStorage;

            List<LoadoutPlan> plans = BuildPlans(squad, characterLoadouts);
            TFTVLogger.Always($"[LoadoutService] Built {plans.Count} loadout plans.");
            foreach (LoadoutPlan plan in plans)
            {
                ApplyLoadout(plan, storage, plan.FirstPassResult, recordMissing: true);
            }

            HashSet<string> neededItems = new HashSet<string>(plans.SelectMany(plan => plan.FirstPassResult.MissingItems));
            TFTVLogger.Always($"[LoadoutService] First pass missing items: {neededItems.Count}.");
            if (neededItems.Count > 0 && allowReassignFromOtherOperatives)
            {
                List<GeoCharacter> otherOperatives = phoenixFaction.Soldiers
                    .Where(s => !squad.Contains(s) && s.Fatigue != null)
                    .OrderBy(o => o.Fatigue.Stamina.Value.EndValueInt)
                    .ToList();

                TFTVLogger.Always($"[LoadoutService] Reassign phase scanning {otherOperatives.Count} other operatives.");
                foreach (GeoCharacter operative in otherOperatives)
                {
                    if (neededItems.Count == 0)
                    {
                        break;
                    }

                    MoveItemsToStorage(operative, storage, neededItems);
                }

                foreach (LoadoutPlan plan in plans)
                {
                    plan.FirstPassResult.MissingItems.Clear();
                    ApplyLoadout(plan, storage, plan.FirstPassResult, recordMissing: false);
                }
            }

            TFTVLogger.Always("[LoadoutService] ApplySquadLoadouts finished.");
            return plans.Select(plan => plan.FirstPassResult).ToList();
        }

        internal static void EquipAllFromButton(
            List<GeoCharacter> squad,
            Dictionary<int, Dictionary<string, List<string>>> characterLoadouts,
            bool allowReassignFromOtherOperatives = true)
        {
            ApplySquadLoadouts(squad, characterLoadouts, allowReassignFromOtherOperatives);
        }

        private static List<LoadoutPlan> BuildPlans(
            IReadOnlyList<GeoCharacter> squad,
            Dictionary<int, Dictionary<string, List<string>>> characterLoadouts)
        {
            List<LoadoutPlan> plans = new List<LoadoutPlan>();
            foreach (GeoCharacter geoCharacter in squad)
            {
                if (!characterLoadouts.TryGetValue(geoCharacter.Id, out Dictionary<string, List<string>> rawLoadout))
                {
                    TFTVLogger.Always($"[LoadoutService] No custom loadout for {geoCharacter.DisplayName} ({geoCharacter.Id}). Trying character loadout.");
                    LoadoutSpec fallback = CreateLoadoutFromCharacter(geoCharacter);
                    if (fallback == null)
                    {
                        TFTVLogger.Always($"[LoadoutService] No fallback loadout for {geoCharacter.DisplayName} ({geoCharacter.Id}). Skipping.");
                        continue;
                    }

                    plans.Add(new LoadoutPlan(geoCharacter, fallback));
                    TFTVLogger.Always($"[LoadoutService] Added fallback loadout for {geoCharacter.DisplayName} ({geoCharacter.Id}).");
                    continue;
                }

                LoadoutSpec desired = LoadoutSpec.FromDictionary(rawLoadout);
                if (desired == null)
                {
                    TFTVLogger.Always($"[LoadoutService] Custom loadout invalid for {geoCharacter.DisplayName} ({geoCharacter.Id}).");
                    continue;
                }

                plans.Add(new LoadoutPlan(geoCharacter, desired));
                TFTVLogger.Always($"[LoadoutService] Added custom loadout for {geoCharacter.DisplayName} ({geoCharacter.Id}).");
            }

            return plans;
        }

        private static LoadoutSpec CreateLoadoutFromCharacter(GeoCharacter character)
        {
            if (character == null)
            {
                return null;
            }

            if ((character.ArmourLoadoutItems == null || character.ArmourLoadoutItems.Count == 0) &&
                (character.EquipmentLoadoutItems == null || character.EquipmentLoadoutItems.Count == 0) &&
                (character.InventoryLoadoutItems == null || character.InventoryLoadoutItems.Count == 0))
            {
                return null;
            }

            LoadoutSpec spec = new LoadoutSpec();
            spec.Armour.AddRange(character.ArmourLoadoutItems.Select(item => item.ItemDef.Guid));
            spec.Equipment.AddRange(character.EquipmentLoadoutItems.Select(item => item.ItemDef.Guid));
            spec.Inventory.AddRange(character.InventoryLoadoutItems.Select(item => item.ItemDef.Guid));
            return spec;
        }

        private static void ApplyLoadout(LoadoutPlan plan, ItemStorage storage, LoadoutApplyResult result, bool recordMissing)
        {
            if (plan == null || plan.Character == null || plan.Desired == null)
            {
                TFTVLogger.Always("[LoadoutService] ApplyLoadout skipped: plan/character/desired missing.");
                return;
            }

            TFTVLogger.Always($"[LoadoutService] Applying loadout for {plan.Character.DisplayName} ({plan.Character.Id}). RecordMissing={recordMissing}.");
            Dictionary<string, List<GeoItem>> current = GetCurrentItems(plan.Character);
            List<GeoItem> claimedFromStorage = new List<GeoItem>();
            List<GeoItem> newArmour = BuildSlotList(plan.Desired.Armour, current[ArmourItemsString], storage, result, recordMissing, claimedFromStorage);
            List<GeoItem> newEquipment = BuildSlotList(plan.Desired.Equipment, current[EquipmentItemsString], storage, result, recordMissing, claimedFromStorage);
            List<GeoItem> newInventory = BuildSlotList(plan.Desired.Inventory, current[InventoryItemsString], storage, result, recordMissing, claimedFromStorage);

            TFTVLogger.Always($"[LoadoutService] {plan.Character.DisplayName} new counts: armour={newArmour.Count} equipment={newEquipment.Count} inventory={newInventory.Count} missing={result.MissingItems.Count}.");
            MoveRemainingToStorage(current, storage);
            plan.Character.SetItems(newArmour, newEquipment, newInventory, false);

            for (int i = 0; i < claimedFromStorage.Count; i++)
            {
                storage.RemoveItem(claimedFromStorage[i]);
            }
        }

        private static List<GeoItem> BuildSlotList(
            IReadOnlyList<string> desiredGuids,
            List<GeoItem> currentItems,
            ItemStorage storage,
            LoadoutApplyResult result,
            bool recordMissing,
            List<GeoItem> claimedFromStorage)
        {
            List<GeoItem> newItems = new List<GeoItem>();
            if (desiredGuids == null)
            {
                return newItems;
            }

            for (int i = 0; i < desiredGuids.Count; i++)
            {
                string guid = desiredGuids[i];
                GeoItem claimed = ClaimItem(guid, currentItems, storage, claimedFromStorage);
                if (claimed == null)
                {
                    if (recordMissing)
                    {
                        result.MissingItems.Add(guid);
                    }
                    continue;
                }

                newItems.Add(claimed);
            }

            return newItems;
        }

        private static GeoItem ClaimItem(string guid, List<GeoItem> currentItems, ItemStorage storage, List<GeoItem> claimedFromStorage)
        {
            GeoItem current = currentItems.FirstOrDefault(item => item.ItemDef.Guid == guid);
            if (current != null)
            {
                TFTVLogger.Always($"[LoadoutService] ClaimItem from current: {guid}.");
                currentItems.Remove(current);
                return current;
            }

            ItemDef itemDef = (ItemDef)Repo.GetDef(guid);
            if (itemDef == null)
            {
                TFTVLogger.Always($"[LoadoutService] ClaimItem missing def for guid {guid}.");
                return null;
            }

            if (!storage.Items.ContainsKey(itemDef))
            {
                return null;
            }

            TFTVLogger.Always($"[LoadoutService] ClaimItem from storage: {guid}.");
            GeoItem cloned = storage.Items[itemDef].Clone() as GeoItem;
            if (cloned != null)
            {
                claimedFromStorage.Add(cloned);
            }
            return cloned;
        }

        private static Dictionary<string, List<GeoItem>> GetCurrentItems(GeoCharacter character)
        {
            Dictionary<string, List<GeoItem>> characterItems = new Dictionary<string, List<GeoItem>>
            {
                { ArmourItemsString, new List<GeoItem>() },
                { EquipmentItemsString, new List<GeoItem>() },
                { InventoryItemsString, new List<GeoItem>() }
            };

            characterItems[ArmourItemsString].AddRange(character.ArmourItems
                .Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                .Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)));
            characterItems[EquipmentItemsString].AddRange(character.EquipmentItems);
            characterItems[InventoryItemsString].AddRange(character.InventoryItems);

            return characterItems;
        }

        private static void MoveRemainingToStorage(Dictionary<string, List<GeoItem>> current, ItemStorage storage)
        {
            foreach (List<GeoItem> items in current.Values)
            {
                foreach (GeoItem item in items)
                {
                    storage.AddItem(item);
                }
            }
        }

        private static void MoveItemsToStorage(GeoCharacter character, ItemStorage storage, HashSet<string> neededItems)
        {
            Dictionary<string, List<GeoItem>> current = GetCurrentItems(character);

            List<string> keys = current.Keys.ToList();
            foreach (string key in keys)
            {
                List<GeoItem> items = current[key];
                List<GeoItem> remainingItems = new List<GeoItem>();
                foreach (GeoItem item in items)
                {
                    if (neededItems.Contains(item.ItemDef.Guid))
                    {
                        TFTVLogger.Always($"[LoadoutService] Reassign {item.ItemDef.Guid} from {character.DisplayName}.");
                        storage.AddItem(item);
                        neededItems.Remove(item.ItemDef.Guid);
                    }
                    else
                    {
                        remainingItems.Add(item);
                    }
                }

                current[key] = remainingItems;
            }

            character.SetItems(current[ArmourItemsString], current[EquipmentItemsString], current[InventoryItemsString], false);
        }
    }
}