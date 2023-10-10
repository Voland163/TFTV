using PhoenixPoint.Modding;
using PRMBetterClasses;
using TFTVVehicleRework.Armadillo;
using TFTVVehicleRework.Aspida;
using TFTVVehicleRework.KaosBuggy;
using TFTVVehicleRework.Mutog;
using TFTVVehicleRework.Scarab;
using TFTVVehicleRework.Misc;
using System.IO;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using Base.Core;
using PhoenixPoint.Common.Core;
using Base.Defs;

namespace TFTVVehicleRework
{
    public static class VehiclesMain
    {
        internal static string LocalizationFileName = "Vehicles.csv";
        internal static SharedDamageKeywordsDataDef keywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
		internal static readonly DefRepository Repo = GameUtl.GameComponent<DefRepository>();
        public static void ReworkVehicles(ModMain instance)
        {
            //Get Path for Localisation;
            string LocalizationDirectory = Path.Combine(instance.Instance.Entry.Directory, "Assets", "Localization");
            if(File.Exists(Path.Combine(LocalizationDirectory, LocalizationFileName)))
            {
                Helper.AddLocalizationFromCSV(LocalizationFileName, null);
            }

            // Make changes to vehicles via their respective Main methods:
            ArmadilloMain.Change();
            AspidaMain.Change();
            KaosBuggyMain.Change();
            MutogMain.Change();
            ScarabMain.Change();
            MiscChanges.Apply();
        }
    }
}