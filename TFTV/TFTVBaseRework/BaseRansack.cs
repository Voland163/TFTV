using Base.Core;
using Base.UI.MessageBox;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal class BaseRansack
    {
        internal static string GetRansackPreviewText(GeoSite site)
        {
            try
            {
                TryGetRansackDemolitionValue(site, out int mats, out int tech);
                return $"Gain: {mats} Materials, {tech} Tech";

            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }


        internal static void TryGetRansackDemolitionValue(GeoSite site, out int mats, out int tech)
        {
            mats = tech = 0;

            try
            {
                float matsTotal = 0f;
                float techTotal = 0f;

                GeoPhoenixBase phoenixBase = site?.GetComponent<GeoPhoenixBase>();

                GeoPhoenixBaseLayout layout = phoenixBase?.Layout;

                ResourcePack totalRefundValue = new ResourcePack();

                foreach (GeoPhoenixFacility facility in layout.Facilities)
                {
                    ResourcePack refund = phoenixBase.GetRefundForFacilityScrap(facility);

                  //  TFTVLogger.Always($"[BaseActivation] TryGetRansackDemolitionValue: Checking facility {facility?.Def?.name} for demolition value");

                    foreach (ResourceUnit unit in refund)
                    {
                    //    TFTVLogger.Always($"[BaseActivation] TryGetRansackDemolitionValue: Refund unit - Type: {unit.Type}, Value: {unit.Value}");

                        if (unit.Type == ResourceType.Materials)
                        {
                            matsTotal += unit.Value;
                        }
                        else if (unit.Type == ResourceType.Tech)
                        {
                            techTotal += unit.Value;
                        }
                    }
                }

                int difficulty = site.GeoLevel.CurrentDifficultyLevel.Order;

                mats = Mathf.RoundToInt(matsTotal) + (100 * (6 - difficulty));
                tech = Mathf.RoundToInt(techTotal) + (20 * (6 - difficulty));

               // TFTVLogger.Always($"[BaseActivation] TryGetRansackDemolitionValue: Total refund - Materials: {mats}, Tech: {tech}, from {matsTotal} and {techTotal} adjusted by difficulty {difficulty}");

            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        internal static void Ransack(GeoSite site, GeoPhoenixFaction faction)
        {
            try
            {

                TryGetRansackDemolitionValue(site, out int mats, out int tech);

                var payout = new ResourcePack(new[]
                {
                        new ResourceUnit(ResourceType.Materials, mats),
                        new ResourceUnit(ResourceType.Tech, tech)
                    });

                faction.Wallet.Give(payout, OperationReason.Gift);
                site.DestroySite();

                GameUtl.GetMessageBox().ShowSimplePrompt(
                    $"You gained from ransacking: {mats} Materials, {tech} Tech.",
                    MessageBoxIcon.Information,
                    MessageBoxButtons.OK,
                    null);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }


    }
}
