using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using PhoenixPoint.Common.Core;
using System.Linq;

namespace TFTVVehicleRework.Misc
{
    public static class MarketplaceOptions
    {
        private static readonly DefRepository Repo = VehiclesMain.Repo;
        public static void Remove_Options()
        {
            //"TheMarketplaceSettingsDef"
            TheMarketplaceSettingsDef MarketOptions = (TheMarketplaceSettingsDef)Repo.GetDef("dd85d71f-d3ee-b514-286c-f5a3aff40b56"); 

            GeoMarketplaceItemOptionDef[] OptionsToRemove = new GeoMarketplaceItemOptionDef[]
            {
                (GeoMarketplaceItemOptionDef)Repo.GetDef("ecfd923e-571e-bf64-4b0a-fedfd41ee035"),   // Taurus
                (GeoMarketplaceItemOptionDef)Repo.GetDef("1495a499-78c4-5b64-2a80-9ff3f4654ec0"),   // Scorpio
                (GeoMarketplaceItemOptionDef)Repo.GetDef("2e0a4593-5a47-8fb4-985c-1edb6c253dd4"),   // CarbonPlating
                // (GeoMarketplaceItemOptionDef)Repo.GetDef("e06bfda0-da92-1464-2a34-831ad34949ef"),   // CargoRacks
                (GeoMarketplaceItemOptionDef)Repo.GetDef("ea2c418b-7dbd-b3c4-58a8-096383d88591"),   // CaterpillarTracks
                // (GeoMarketplaceItemOptionDef)Repo.GetDef("3785776d-2ed7-bf94-7b0f-4d0ca2c4bf05"),   // EngineMappingModule
                (GeoMarketplaceItemOptionDef)Repo.GetDef("a34b73bb-0497-7434-2892-842f29b91852"),   // Mephistopheles
                (GeoMarketplaceItemOptionDef)Repo.GetDef("bb0b983e-4b09-44b4-399e-2a0c3df7a6d7"),   // ReinforcedPlating
                (GeoMarketplaceItemOptionDef)Repo.GetDef("c841e12a-2625-9814-5ba7-27f54f75b6a0"),   // Bi-Turbo Engine
                // (GeoMarketplaceItemOptionDef)Repo.GetDef("bb0b983e-4b09-44b4-399e-2a0c3df7a6d7"),   // 
                (GeoMarketplaceItemOptionDef)Repo.GetDef("21d837b2-4782-7d84-4a31-f27f3e912f94"),   // Themis
                (GeoMarketplaceItemOptionDef)Repo.GetDef("b4f5b3f5-0dd6-1c84-caff-8ef04a0b5aae"),   // ImprovedChassis
                (GeoMarketplaceItemOptionDef)Repo.GetDef("6add70a2-20ff-a624-99d4-7c732b4f03b8"),   // HybridEngine
            };
            MarketOptions.PossibleOptions = MarketOptions.PossibleOptions.Except(OptionsToRemove).ToArray();
        }
    }
}