using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers.Inventory;

namespace TFTV.Vehicles.Ammo
{
    internal class VehicleAmmoReloadingFixes
    {


        [HarmonyPatch(typeof(UIInventoryList), "IsVehicleEquipment")]
        public static class UIInventoryList_IsVehicleEquipment_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static void Postfix(UIInventoryList __instance, ref bool __result)
            {
                if (TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(UIInventorySlot), "IsVehicleEquipment")]
        public static class UIInventorySlot_IsVehicleEquipment_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static void Postfix(UIInventorySlot __instance, ref bool __result)
            {
                if (TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    __result = false;
                }
            }
        }


    }
}
