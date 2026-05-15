using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal static class BaseLowQualityScanning
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private const int BaseScanMaterialsCost = 30;
        private const int BaseScanTechCost = 5;
        private const double ScanRangeKm = 4000.0;

        // Single global counter: increments each time any base scan is initiated.
        private const string TotalScanCountVariable = "TFTV_TotalScanCount";

        // ── GeoVariable helpers ──────────────────────────────────────────────────

        private static int GetTotalScanCount(GeoSite site) =>
            site.GeoLevel.EventSystem.GetVariable(TotalScanCountVariable);

        // Tier = total scans done so far + 1, so first scan globally is tier 1, second is tier 2, etc.
        private static int GetNextScanTier(GeoSite site) => GetTotalScanCount(site) + 1;

        private static void IncrementTotalScanCount(GeoSite site)
        {
            int current = site.GeoLevel.EventSystem.GetVariable(TotalScanCountVariable);
            site.GeoLevel.EventSystem.SetVariable(TotalScanCountVariable, current + 1);
        }

        // ── cost helpers ─────────────────────────────────────────────────────────

        private static ResourcePack BuildScanCostPack(int tier)
        {
            return new ResourcePack
            {
                new ResourceUnit(ResourceType.Materials, BaseScanMaterialsCost * tier),
                new ResourceUnit(ResourceType.Tech, BaseScanTechCost * tier)
            };
        }

        // ── public API ───────────────────────────────────────────────────────────

        internal static ResourcePack GetScanCostPack(GeoSite site)
        {
            if (site == null) return BuildScanCostPack(1);
            return BuildScanCostPack(GetNextScanTier(site));
        }

        internal static bool IsScanInProgress(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site == null || faction?.Scanners == null) return false;
                foreach (GeoScanner scanner in faction.Scanners)
                {
                    if (scanner?.Location?.SiteId == site.SiteId) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        internal static bool CanAffordLowQualityScan(GeoSite site, GeoPhoenixFaction faction)
        {
            if (site == null || faction?.Wallet == null) return false;
            return faction.Wallet.HasResources(BuildScanCostPack(GetNextScanTier(site)));
        }

        internal static bool HasPhoenixAircraftWithinRange(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site == null || faction?.Vehicles == null) return false;
                foreach (GeoVehicle vehicle in faction.Vehicles)
                {
                    if (vehicle == null) continue;
                    Vector3 vehiclePos = vehicle.CurrentSite != null
                        ? vehicle.CurrentSite.WorldPosition
                        : vehicle.WorldPosition;
                    double distKm = GeoMap.Distance(vehiclePos, site.WorldPosition).Value;
                    if (distKm <= ScanRangeKm) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        internal static bool TryStartLowQualityScan(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site == null || faction == null) return false;

                ScanAbilityDef scanAbilityDef = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");
                if (scanAbilityDef?.ScanActorDef == null) return false;

                if (IsScanInProgress(site, faction)) return false;
                if (!HasPhoenixAircraftWithinRange(site, faction)) return false;

                ResourcePack cost = BuildScanCostPack(GetNextScanTier(site));
                if (!faction.Wallet.HasResources(cost)) return false;

                faction.Wallet.Take(cost, OperationReason.Purchase);
                faction.CreateScanner(site, scanAbilityDef.ScanActorDef);
                IncrementTotalScanCount(site);

                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }
    }
}
