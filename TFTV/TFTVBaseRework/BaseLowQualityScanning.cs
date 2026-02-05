using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;

namespace TFTV.TFTVBaseRework
{
    internal static class BaseLowQualityScanning
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private const int LowQualityScanMaterialsCost = 60;
        private const int LowQualityScanTechCost = 10;

        private static readonly ResourcePack LowQualityScanCost = new ResourcePack
        {
            new ResourceUnit(ResourceType.Materials, LowQualityScanMaterialsCost),
            new ResourceUnit(ResourceType.Tech, LowQualityScanTechCost)
        };

        internal static string GetScanCostText()
        {
            return $"Cost: {LowQualityScanMaterialsCost} Materials, {LowQualityScanTechCost} Tech";
        }

        internal static bool CanAffordLowQualityScan(GeoPhoenixFaction faction)
        {
            return faction?.Wallet != null && faction.Wallet.HasResources(LowQualityScanCost);
        }

        internal static bool TryStartLowQualityScan(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {
                if (site == null || faction == null)
                {
                    return false;
                }

                ScanAbilityDef scanAbilityDef = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");
                if (scanAbilityDef?.ScanActorDef == null)
                {
                    return false;
                }

                if (!CanAffordLowQualityScan(faction))
                {
                    return false;
                }

                faction.Wallet.Take(LowQualityScanCost, OperationReason.Purchase);
                faction.CreateScanner(site, scanAbilityDef.ScanActorDef);
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        internal static ResourcePack GetScanCostPack()
        {
            return LowQualityScanCost;
        }
    }
}
