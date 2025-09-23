using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using TFTVVehicleRework.Armadillo;
using TFTVVehicleRework.Aspida;
using TFTVVehicleRework.KaosBuggy;
using TFTVVehicleRework.Misc;
using TFTVVehicleRework.Mutog;
using TFTVVehicleRework.Scarab;

namespace TFTVVehicleRework
{
    public static class VehiclesMain
    {

        internal static SharedDamageKeywordsDataDef keywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
        internal static readonly DefRepository Repo = GameUtl.GameComponent<DefRepository>();

        public static void ReworkVehicles()
        {

            ArmadilloMain.Change();
            AspidaMain.Change();
            KaosBuggyMain.Change();
            MutogMain.Change();
            ScarabMain.Change();
            MiscChanges.Apply();
        }



    }
}