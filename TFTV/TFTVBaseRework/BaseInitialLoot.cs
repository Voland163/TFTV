using HarmonyLib;
using PhoenixPoint.Common.Core;
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
                    if (!BaseReworkCheck.BaseReworkEnabled || __instance == null)
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

        // ── Chance formula constants ─────────────────────────────────────────
        // Base chance when no vehicles of this type have been found yet and
        // no bases have been explored yet (besides the first one, which is the
        // current visit).  +10 pp per vehicle of this type still remaining,
        // +10 pp per already-looted base (not counting the current one).
        private const int BaseChancePercent = 20;
        private const int BonusPerRemainingVehicle = 10;
        private const int BonusPerExploredBase = 10;

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

        // ── Public surface ────────────────────────────────────────────────────

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

        // ── Loot tag helpers ──────────────────────────────────────────────────

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

        // ── Limit helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the per-type limit from new-game settings if the config has been
        /// committed, otherwise falls back to 0 (no vehicles) so old/vanilla saves
        /// are unaffected.
        /// </summary>
        private static int GetManticoreLimit()
        {
            return TFTVNewGameOptions.ConfigImplemented
                ? TFTVNewGameOptions.InitialManticoreLimit
                : 0;
        }

        private static int GetScarabLimit()
        {
            return TFTVNewGameOptions.ConfigImplemented
                ? TFTVNewGameOptions.InitialScarabLimit
                : 0;
        }

        private static bool CanGiveLimitedVehicle(GeoPhoenixFaction faction, string tagPrefix, int limit)
        {
            try
            {
                return limit > 0 && GetLimitedVehicleCount(faction, tagPrefix) < limit;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
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

        // ── Chance calculation ────────────────────────────────────────────────

        /// <summary>
        /// Returns the number of Phoenix bases that have already been looted
        /// (have LootedTag), NOT counting <paramref name="currentSite"/> itself.
        /// Used to scale up vehicle drop chances the further into the game you are.
        /// </summary>
        private static int GetAlreadyLootedBaseCount(GeoSite currentSite)
        {
            try
            {
                return currentSite.GeoLevel.Map.AllSites
                    .Count(s => s != currentSite
                        && s.Type == GeoSiteType.PhoenixBase
                        && s.SiteTags.Contains(PhoenixBaseReworkState.LootedTag));
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return 0;
            }
        }

        /// <summary>
        /// Computes the chance (0–100, integer percentage points) that this vehicle
        /// type will drop at the current base visit.
        ///
        ///   chance = BASE + (remaining * BONUS_PER_REMAINING) + (explored * BONUS_PER_EXPLORED)
        ///
        /// where <c>remaining = limit - alreadyAwarded</c>.
        /// </summary>
        private static int GetVehicleChancePercent(GeoPhoenixFaction faction, string tagPrefix, int limit, int exploredBases)
        {
            int awarded = GetLimitedVehicleCount(faction, tagPrefix);
            int remaining = limit - awarded;

            if (remaining <= 0)
            {
                return 0;
            }

            int chance = BaseChancePercent
                + (remaining * BonusPerRemainingVehicle)
                + (exploredBases * BonusPerExploredBase);

            return Math.Min(100, Math.Max(0, chance));
        }

        // ── Preview text ──────────────────────────────────────────────────────

        private static string BuildFirstVisitLootPreview(GeoSite site, GeoPhoenixFaction faction)
        {
            int manticoreLimit = GetManticoreLimit();
            int scarabLimit = GetScarabLimit();
            int exploredBases = GetAlreadyLootedBaseCount(site);

            List<string> vehicleCandidates = new List<string>();

            if (CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix, manticoreLimit))
            {
                int chance = GetVehicleChancePercent(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix, manticoreLimit, exploredBases);
                vehicleCandidates.Add($"Manticore ({chance}%)");
            }

            if (CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ScarabCountTagPrefix, scarabLimit))
            {
                int chance = GetVehicleChancePercent(faction, PhoenixBaseReworkState.ScarabCountTagPrefix, scarabLimit, exploredBases);
                vehicleCandidates.Add($"Scarab ({chance}%)");
            }

            if (vehicleCandidates.Count > 0)
            {
                return string.Join(" / ", vehicleCandidates);
            }

            return "Random equipment";
        }

        // ── Core loot roll ────────────────────────────────────────────────────

        private static LootUiResult GiveInitialLoot(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site.SiteTags.Contains(PhoenixBaseReworkState.LootedTag))
                {
                    return null;
                }

                int difficultyOrder = site.GeoLevel.CurrentDifficultyLevel.Order;
                int manticoreLimit = GetManticoreLimit();
                int scarabLimit = GetScarabLimit();
                int exploredBases = GetAlreadyLootedBaseCount(site);

                TacCharacterDef scarabTemplate = DefCache.GetDef<TacCharacterDef>("PX_Scarab_CharacterTemplateDef");
                VehicleItemDef manticoreDef = DefCache.GetDef<VehicleItemDef>("PP_Manticore_VehicleItemDef");
                ItemDef scarabItemDef = DefCache.GetDef<ItemDef>("PX_Scarab_ItemDef");

                // Build a roll table where each vehicle type occupies its computed
                // percentage of the 0-100 space, and the remainder is "no vehicle".
                List<WeightedLootOption> vehicleRollTable = new List<WeightedLootOption>();

                int manticoreChance = 0;
                int scarabChance = 0;

                if (manticoreDef != null && CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix, manticoreLimit))
                {
                    manticoreChance = GetVehicleChancePercent(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix, manticoreLimit, exploredBases);
                    vehicleRollTable.Add(new WeightedLootOption(manticoreChance, delegate
                    {
                        LootedManticoreBeingCreated = true;
                        GeoVehicle geoVehicle = faction.CreateVehicle(site, manticoreDef.ComponentSetDef);
                        JustLootedManticore = null;
                        LootedManticoreBeingCreated = false;
                        ApplyRandomHealthToVehicle(geoVehicle);
                        IncrementLimitedVehicleCount(faction, PhoenixBaseReworkState.ManticoreCountTagPrefix);
                        return new LootUiResult("Manticore", BuildVehicleLootItems(manticoreDef));
                    }));
                }

                if (scarabTemplate != null && CanGiveLimitedVehicle(faction, PhoenixBaseReworkState.ScarabCountTagPrefix, scarabLimit))
                {
                    scarabChance = GetVehicleChancePercent(faction, PhoenixBaseReworkState.ScarabCountTagPrefix, scarabLimit, exploredBases);
                    vehicleRollTable.Add(new WeightedLootOption(scarabChance, delegate
                    {
                        GeoVehicle vehicle = site.Vehicles
                            .Where(v => v != null && v.Owner == faction)
                            .OrderByDescending(v => Math.Max(0, v.MaxCharacterSpace - v.UsedCharacterSpace))
                            .FirstOrDefault();

                        GeoCharacter character = faction.GeoLevel.CreateCharacterFromTemplate(scarabTemplate, faction, null, null);
                        ApplyRandomHealthToCharacter(character);
                        faction.AddRecruit(character, vehicle);
                        IncrementLimitedVehicleCount(faction, PhoenixBaseReworkState.ScarabCountTagPrefix);
                        return new LootUiResult("Scarab", BuildVehicleLootItems(scarabItemDef));
                    }));
                }

                if (vehicleRollTable.Count > 0)
                {
                    // The "no vehicle" bucket fills whatever percentage is left after
                    // clamping individual chances so they never exceed 100 combined.
                    int vehicleTotal = Math.Min(100, manticoreChance + scarabChance);
                    int noVehicleWeight = Math.Max(0, 100 - vehicleTotal);
                    vehicleRollTable.Add(new WeightedLootOption(noVehicleWeight, null));

                    TFTVLogger.Always(
                        $"[BaseInitialLoot] site={site.SiteId} exploredBases={exploredBases} " +
                        $"manticoreChance={manticoreChance}% scarabChance={scarabChance}% noVehicle={noVehicleWeight}%");

                    LootUiResult awarded = RollVehicleLoot(vehicleRollTable);
                    if (awarded != null && !string.IsNullOrEmpty(awarded.Text))
                    {
                        site.SiteTags.Add(PhoenixBaseReworkState.LootedTag);
                        SetLastLootResult(site, awarded.Text);
                        return awarded;
                    }
                }

                // Vehicle roll gave nothing (or no vehicles are available) — fall back
                // to random starting equipment.
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

        // ── Starting items fallback ───────────────────────────────────────────

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
                         i.Tags.Contains(DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef")) ||
                         i.Tags.Contains(DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef"))))
                    .ToList();

                if (itemPool.Count == 0)
                {
                    itemsAwarded = "nothing";
                    return new List<ItemDef>();
                }

                int amount;
                if (TFTVNewGameOptions.ConfigImplemented)
                {
                    amount = Math.Min(itemPool.Count, TFTVNewGameOptions.InitialLootLevel);
                }
                else
                {
                    amount = Math.Min(itemPool.Count, Math.Max(0, 8 - difficultyOrder));
                }

                if (amount <= 0)
                {
                    itemsAwarded = "nothing";
                    return new List<ItemDef>();
                }

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

        // ── Roll helpers ──────────────────────────────────────────────────────

        private static List<ItemDef> BuildVehicleLootItems(ItemDef itemDef)
        {
            if (itemDef == null)
            {
                return null;
            }

            return new List<ItemDef> { itemDef };
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

        // ── Health randomisation ──────────────────────────────────────────────

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