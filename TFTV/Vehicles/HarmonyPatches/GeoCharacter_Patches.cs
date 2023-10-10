using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;

namespace TFTVVehicleRework.HarmonyPatches
{
    [HarmonyPatch(typeof(GeoCharacter), "CreateCharacter")]
    internal static class CreateCharacter_Patch
    {
        public static void Prefix(ref GeoUnitDescriptor unit)
        {
            if (unit.UnitType.IsVehicle)
            {
                unit.BonusStats = new BaseCharacterStats( 0f, 0f, 0f);
            }
        }
    }
}