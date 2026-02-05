using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV.TFTVBaseRework
{
    internal class BaseInitialLoot
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static GeoVehicle JustLootedManticore = null;
        public static bool LootedManticoreBeingCreated = false;

        [HarmonyPatch(typeof(GeoVehicle), "TeleportToSite")]
        internal static class GeoVehicle_TeleportToSite_OpenActivationUI_patch
        {
            public static void Prefix(GeoVehicle __instance, GeoSite site)
            {
                try
                {
                    if (!BaseReworkUtils.BaseReworkEnabled || __instance == null)
                    {
                        return;
                    }

                    if (LootedManticoreBeingCreated && __instance.Owner == __instance.GeoLevel.PhoenixFaction && site.GetComponent<GeoPhoenixBase>() != null)
                    {
                        JustLootedManticore = __instance;
                        TFTVLogger.Always($"[GeoVehicle.TeleportToSite] JustLootedManticore set to {__instance.Name}");
                    }

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);

                }
            }
        }


        private const int VehicleBaseWeight = 3;
        private const int VehicleFirstFoundBonusWeight = 4;
        private const int NoVehicleWeight = 3;

        private const float VehicleMinHealthPercent = 0.35f;
        private const float VehicleMaxHealthPercent = 0.75f;

        private sealed class WeightedLootOption
        {
            internal WeightedLootOption(int weight, Func<LootUiResult> roll)
            {
                Weight = Math.Max(0, weight);
                Roll = roll;
            }

            internal int Weight { get; }
            internal Func<LootUiResult> Roll { get; }
        }

        internal sealed class LootUiResult
        {
            internal LootUiResult(string text, List<ItemDef> items)
            {
                Text = text ?? string.Empty;
                Items = items;
            }

            internal string Text { get; }
            internal List<ItemDef> Items { get; }
            internal bool HasItems => Items != null && Items.Count > 0;
        }

        internal static string GetOrCreateFirstVisitPreviewText(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site == null || faction == null || site.SiteTags.Contains(PhoenixBaseReworkState.LootedTag))
                {
                    return string.Empty;
                }

                string cached = site.SiteTags.FirstOrDefault(t => t.StartsWith(PhoenixBaseReworkState.FirstVisitPreviewTagPrefix, StringComparison.Ordinal));

                TFTVLogger.Always($"[BaseActivation] GetOrCreateFirstVisitPreviewText: site={site.SiteId}, cachedPreview={cached}");

                if (!string.IsNullOrEmpty(cached))
                {
                    return cached.Substring(PhoenixBaseReworkState.FirstVisitPreviewTagPrefix.Length);
                }

                string preview = BuildFirstVisitLootPreview(site, faction);

                TFTVLogger.Always($"[BaseActivation] GetOrCreateFirstVisitPreviewText: site={site.SiteId}, preview={preview}");

                if (!string.IsNullOrEmpty(preview))
                {
                    site.SiteTags.Add(PhoenixBaseReworkState.FirstVisitPreviewTagPrefix + preview);
                }

                return preview;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return string.Empty;
            }
        }

        internal static LootUiResult TryGiveFirstVisitLootOnUI(GeoSite site, GeoPhoenixFaction faction, bool hasVehicle)
        {
            try
            {
                if (site == null || faction == null || !hasVehicle)
                {
                    return null;
                }

                if (site.SiteTags.Contains(PhoenixBaseReworkState.LootedTag))
                {
                    return null;
                }

                return GiveInitialLoot(site, faction);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        private static void SetLastLootResult(GeoSite site, string result)
        {
            if (site == null)
            {
                return;
            }

            string existing = site.SiteTags.FirstOrDefault(t =>
                t.StartsWith(PhoenixBaseReworkState.LootResultTagPrefix, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(existing))
            {
                site.SiteTags.Remove(existing);
            }

            if (!string.IsNullOrEmpty(result))
            {
                site.SiteTags.Add(PhoenixBaseReworkState.LootResultTagPrefix + result);
            }
        }

        private static string BuildFirstVisitLootPreview(GeoSite site, GeoPhoenixFaction faction)
        {
            int difficultyOrder = site.GeoLevel.CurrentDifficultyLevel.Order;
            TacCharacterDef scarab = DefCache.GetDef<TacCharacterDef>("PX_Scarab_TacCharacterDef");
            VehicleItemDef manticore = DefCache.GetDef<VehicleItemDef>("PP_Manticore_VehicleItemDef");

            List<string> vehicleCandidates = new List<string>();
            if (manticore != null && CanGiveLimitedVehicle(faction, "PX_REWORK_MANTICORE_COUNT:", difficultyOrder))
            {
                vehicleCandidates.Add("Manticore");
            }
            if (scarab != null && CanGiveLimitedVehicle(faction, "PX_REWORK_SCARAB_COUNT:", difficultyOrder))
            {
                vehicleCandidates.Add("Scarab");
            }

            if (vehicleCandidates.Count > 0)
            {
                return string.Join(" / ", vehicleCandidates);
            }

            return "Random equipment";
        }

        private static LootUiResult GiveInitialLoot(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site.SiteTags.Contains(PhoenixBaseReworkState.LootedTag))
                {
                    return null;
                }

                int difficultyOrder = site.GeoLevel.CurrentDifficultyLevel.Order;

                TacCharacterDef scarab = DefCache.GetDef<TacCharacterDef>("PX_Scarab_CharacterTemplateDef");
                VehicleItemDef manticore = DefCache.GetDef<VehicleItemDef>("PP_Manticore_VehicleItemDef");
                ItemDef scarabItemDef = DefCache.GetDef<ItemDef>("PX_Scarab_ItemDef");

                List<WeightedLootOption> vehicleRollTable = new List<WeightedLootOption>();

                if (manticore != null && CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix, difficultyOrder))
                {
                    int weight = GetVehicleLootWeight(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix);
                    vehicleRollTable.Add(new WeightedLootOption(weight, delegate
                    {
                        LootedManticoreBeingCreated = true;
                        GeoVehicle geoVehicle =  faction.CreateVehicle(site, manticore.ComponentSetDef);
                        JustLootedManticore = null;
                        LootedManticoreBeingCreated = false;
                        ApplyRandomHealthToVehicle(geoVehicle);
                        IncrementLimitedVehicleCount(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix);
                        return new LootUiResult("Manticore", BuildVehicleLootItems(manticore));
                    }));
                }

                if (scarab != null && CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ScarabCountTagPrefix, difficultyOrder))
                {
                    int weight = GetVehicleLootWeight(faction, PhoenixBaseReworkState.ScarabCountTagPrefix);
                    vehicleRollTable.Add(new WeightedLootOption(weight, delegate
                    {
                        GeoVehicle vehicle = site.Vehicles
                             .Where(v => v != null && v.Owner == faction)
                             .OrderByDescending(v => Math.Max(0, v.MaxCharacterSpace - v.UsedCharacterSpace))
                             .FirstOrDefault();

                        GeoCharacter character = faction.GeoLevel.CreateCharacterFromTemplate(scarab, faction, null, null);
                        ApplyRandomHealthToCharacter(character);
                        faction.AddRecruit(character, vehicle);
                        IncrementLimitedVehicleCount(faction, PhoenixBaseReworkState.ScarabCountTagPrefix);
                        return new LootUiResult("Scarab", BuildVehicleLootItems(scarabItemDef));
                    }));
                }

                if (vehicleRollTable.Count > 0)
                {
                    vehicleRollTable.Add(new WeightedLootOption(NoVehicleWeight, null));

                    LootUiResult awarded = RollVehicleLoot(vehicleRollTable);
                    if (awarded != null && !string.IsNullOrEmpty(awarded.Text))
                    {
                        site.SiteTags.Add(PhoenixBaseReworkState.LootedTag);
                        SetLastLootResult(site, awarded.Text);
                        return awarded;
                    }
                }

                List<ItemDef> awardedItems = GiveStartingItems(site, faction, difficultyOrder, out string itemsAwarded);
                if (!string.IsNullOrEmpty(itemsAwarded))
                {
                    site.SiteTags.Add(PhoenixBaseReworkState.LootedTag);
                    SetLastLootResult(site, itemsAwarded);
                }
                return new LootUiResult(itemsAwarded, awardedItems);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        private static List<ItemDef> GiveStartingItems(GeoSite site, GeoPhoenixFaction faction, int difficultyOrder, out string itemsAwarded)
        {
            try
            {
                GameTagDef ammoTag = site.GeoLevel.SharedData.SharedGameTags.AmmoTag;
                List<ItemDef> itemPool = (faction.Def.StartingManufacturableItems ?? Array.Empty<ItemDef>())
                    .Where(i => i != null && 
                    !i.Tags.Contains(ammoTag) && 
                    !i.Tags.Contains(DefCache.GetDef<FactionTagDef>("Neutral_FactionTagDef")) &&
                    (i.Tags.Contains(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef")) ||
                    i.Tags.Contains(DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef")) 
                    || i.Tags.Contains(DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef"))))
                    .ToList();

                if (itemPool.Count == 0)
                {
                    itemsAwarded = "nothing";
                    return new List<ItemDef>();
                }

                int amount = Math.Min(itemPool.Count, Math.Max(1, 8 - difficultyOrder));
                List<ItemDef> awardedItems = new List<ItemDef>();
                List<string> foundItems = new List<string>();
                for (int i = 0; i < amount; i++)
                {
                    int index = UnityEngine.Random.Range(0, itemPool.Count);
                    ItemDef item = itemPool[index];
                    itemPool.RemoveAt(index);
                    faction.ItemStorage.AddItem(new GeoItem(item, 1, -1, null, -100));
                    awardedItems.Add(item);
                    foundItems.Add(item.GetDisplayName().Localize(null));
                }

                itemsAwarded = string.Join(", ", foundItems);
                return awardedItems;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                itemsAwarded = string.Empty;
                return new List<ItemDef>();
            }
        }

        private static bool CanGiveLimitedVehicle(GeoPhoenixFaction faction, string tagPrefix, int difficultyOrder)
        {
            try
            {
                int limit = GetVehicleLootLimit(difficultyOrder);
                return limit > 0 && GetLimitedVehicleCount(faction, tagPrefix) < limit;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        private static int GetVehicleLootLimit(int difficultyOrder)
        {
            if (difficultyOrder == 1 || difficultyOrder == 2)
            {
                return 3;
            }
            if (difficultyOrder == 3)
            {
                return 2;
            }
            if (difficultyOrder == 4)
            {
                return 1;
            }
            return 0;
        }

        private static int GetLimitedVehicleCount(GeoPhoenixFaction faction, string tagPrefix)
        {
            try
            {
                GeoSite trackerSite = faction.StartingBase;
                if (trackerSite == null)
                {
                    return 0;
                }

                string existing = trackerSite.SiteTags.FirstOrDefault(t => t.StartsWith(tagPrefix, StringComparison.Ordinal));
                if (string.IsNullOrEmpty(existing))
                {
                    return 0;
                }

                int parsed;
                if (!int.TryParse(existing.Substring(tagPrefix.Length), out parsed))
                {
                    return 0;
                }

                return Math.Max(0, parsed);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return 0;
            }
        }

        private static void IncrementLimitedVehicleCount(GeoPhoenixFaction faction, string tagPrefix)
        {
            try
            {
                GeoSite trackerSite = faction.StartingBase;
                if (trackerSite == null)
                {
                    return;
                }

                int current = GetLimitedVehicleCount(faction, tagPrefix);
                string existing = trackerSite.SiteTags.FirstOrDefault(t => t.StartsWith(tagPrefix, StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(existing))
                {
                    trackerSite.SiteTags.Remove(existing);
                }

                trackerSite.SiteTags.Add(tagPrefix + (current + 1));
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private static List<ItemDef> BuildVehicleLootItems(ItemDef itemDef)
        {
            if (itemDef == null)
            {
                return null;
            }

            return new List<ItemDef> { itemDef };
        }

        private static int GetVehicleLootWeight(GeoPhoenixFaction faction, string tagPrefix)
        {
            int count = GetLimitedVehicleCount(faction, tagPrefix);
            int weight = VehicleBaseWeight;
            if (count == 0)
            {
                weight += VehicleFirstFoundBonusWeight;
            }
            return Math.Max(0, weight);
        }

        private static LootUiResult RollVehicleLoot(List<WeightedLootOption> options)
        {
            if (options == null || options.Count == 0)
            {
                return null;
            }

            int totalWeight = options.Sum(o => Math.Max(0, o.Weight));
            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (WeightedLootOption option in options)
            {
                cumulative += Math.Max(0, option.Weight);
                if (roll < cumulative)
                {
                    return option.Roll?.Invoke();
                }
            }

            return null;
        }

        private static float RollRandomVehicleHealthPercent()
        {
            return UnityEngine.Random.Range(VehicleMinHealthPercent, VehicleMaxHealthPercent);
        }

        private static void ApplyRandomHealthToVehicle(GeoVehicle vehicle)
        {
            if (vehicle?.Stats == null)
            {
                return;
            }

            int maxHitPoints = vehicle.Stats.MaxHitPoints;
            if (maxHitPoints <= 0)
            {
                return;
            }

            int newHitPoints = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(maxHitPoints * RollRandomVehicleHealthPercent()));
            vehicle.SetHitpoints(newHitPoints);
        }

        private static void ApplyRandomHealthToCharacter(GeoCharacter character)
        {
            if (character?.Health == null)
            {
                return;
            }

            float maxHealth = character.Health.Max;
            if (maxHealth <= 0f)
            {
                return;
            }

            int newHealth = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(maxHealth * RollRandomVehicleHealthPercent()));
            character.Health.Set(newHealth);
        }
    }
}
