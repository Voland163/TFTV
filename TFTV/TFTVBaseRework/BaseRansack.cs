using Base.Core;
using Base.UI.MessageBox;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;
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

                float multiplier = TFTVNewGameOptions.ConfigImplemented
                    ? TFTVNewGameOptions.RansackResourcesMultiplier
                    : 1f;

                mats = Mathf.RoundToInt(Mathf.RoundToInt(matsTotal) * multiplier);
                tech = Mathf.RoundToInt(Mathf.RoundToInt(techTotal) * multiplier);

                TFTVLogger.Always($"[BaseRansack] Ransack value: mats={mats} tech={tech} (multiplier={multiplier}, difficulty={difficulty})");

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

                if (site.SiteTags.Contains(BaseActivation.PhoenixBaseReworkState.OutpostTag))
                {
                    DismantleOutpost(site, faction);
                }

                site.DestroySite();

                GameUtl.GetMessageBox().ShowSimplePrompt(
                    BaseReworkText.Format(BaseReworkText.RansackPayout, mats, tech),
                    MessageBoxIcon.Information,
                    MessageBoxButtons.OK,
                    null);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        /// <summary>
        /// DestroySite alone leaves an outpost Phoenix-owned and tagged, and the Activate ability and the
        /// arrival hook both let an outpost through whatever its state, so it could be ransacked again
        /// and again. Hand it back to the environment, as an inactive base is, and return its staff.
        /// </summary>
        private static void DismantleOutpost(GeoSite site, GeoPhoenixFaction faction)
        {
            // Nobody should be stationed at an outpost, but don't strand anyone on a site we no longer own.
            GeoSite fallback = faction.Bases
                .Select(phoenixBase => phoenixBase?.Site)
                .FirstOrDefault(other => other != null && other != site
                    && !other.SiteTags.Contains(BaseActivation.PhoenixBaseReworkState.OutpostTag));

            foreach (GeoCharacter character in site.GetAllCharacters().Where(c => c?.Faction == faction).ToList())
            {
                if (fallback == null)
                {
                    TFTVLogger.Always($"[BaseRansack] No base to move {character.DisplayName} to from {site.LocalizedSiteName}.");
                    continue;
                }

                site.RemoveCharacter(character);
                fallback.AddCharacter(character);
                TFTVLogger.Always($"[BaseRansack] Moved {character.DisplayName} from {site.LocalizedSiteName} to {fallback.LocalizedSiteName}.");
            }

            site.SiteTags.Remove(BaseActivation.PhoenixBaseReworkState.OutpostTag);
            site.Owner = site.GeoLevel.EnvironmentFaction;

            // After the hand-back, so replacement personnel for an old outpost can't be placed on it.
            int returned = PersonnelData.ReturnSiteStaff(site, faction, BaseActivation.PhoenixBaseVisitFlow.GetOutpostPersonnelCost());

            TFTVLogger.Always($"[BaseRansack] Dismantled the outpost at {site.LocalizedSiteName}; {returned} personnel returned to the pool.");
        }
    }
}
