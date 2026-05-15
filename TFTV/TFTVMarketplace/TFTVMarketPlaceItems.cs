using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using PhoenixPoint.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {
        internal class TFTVMarketPlaceItems
        {
            public static void AdjustMarketPlaceOptions()
            {
                try
                {
                    TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");

                    List<GeoMarketplaceOptionDef> geoMarketplaceOptionDefs = new List<GeoMarketplaceOptionDef>(marketplaceSettings.PossibleOptions.ToList());

                    geoMarketplaceOptionDefs.Remove(DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef"));

                    marketplaceSettings.PossibleOptions = geoMarketplaceOptionDefs.ToArray();

                    DefCache.GetDef<GeoMarketplaceOptionDef>("Redemptor_MarketplaceItemOptionDef").Availability = 3;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Subjector_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Tormentor_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Devastator_Redemptor_MarketplaceItemOptionDef").Availability = 2;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("JetBoosters_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("TheFullstop_MarketplaceItemOptionDef").Availability = 5;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("TheScreamer_MarketplaceItemOptionDef").Availability = 3;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("AdvancedEngineMappingModule_MarketplaceItemOptionDef").Availability = 0;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("RevisedArmorPlating_MarketplaceItemOptionDef").Availability = 2;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("ReinforcedCargoRacks_MarketplaceItemOptionDef").Availability = 3;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }
    }
}


