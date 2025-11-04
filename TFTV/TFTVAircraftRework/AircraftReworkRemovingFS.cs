using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVAircraftRework
{
    internal class AircraftReworkRemovingFS
    {
        [HarmonyPatch(typeof(GeoLevelController), "get_HasFesteringSkies")]
        public static class GeoLevelController_get_HasFesteringSkies_Patch
        {
            public static void Postfix(GeoLevelController __instance, ref bool __result)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    __result = false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(GeoscapeTutorial), "UnlockUIForStep")]
        public static class GeoscapeTutorial_UnlockUIForStep_Patch
        {
            public static void Postfix(GeoscapeTutorial __instance, GeoscapeTutorialStepType step)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (step == GeoscapeTutorialStepType.TutorialCompleted)
                    {
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        controller.View.GeoscapeModules.GeoSectionBarModule.VehicleRosterButton.SetState(true);
                        controller.View.GeoscapeModules.ActionsBarModule.AircraftEquipmentDisplayEnabled = true;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }






        [HarmonyPatch(typeof(UIModuleGeoSectionBar), "Show")]
        public static class UIModuleGeoSectionBar_Show_Patch
        {
            public static bool Prefix(UIModuleGeoSectionBar __instance, bool showSections)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    __instance.VehicleRosterButton.gameObject.SetActive(true);
                    __instance.SectionsRoot.SetActive(showSections);

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGeoSectionBar), "ActivateVehicleRosterContent")]
        public static class UIModuleGeoSectionBar_ActivateVehicleRosterContent_Patch
        {
            public static bool Prefix(UIModuleGeoSectionBar __instance, GeoscapeViewContext ____context, UIGeoSection ____section)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return true;
                    }


                    if (____section == UIGeoSection.VehicleRoster)
                    {
                        __instance.ActivateGeoscapeContent();
                        return false;
                    }
                    ____context.View.SetGamePauseState(true);
                    ____context.View.ToVehicleRosterState(StateStackAction.ClearStackAndPush, null);

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(GeoscapeView), "InitView")]
        public static class GeoscapeView_InitView_Patch
        {
            public static void Postfix(GeoscapeView __instance)
            {
                try
                {

                    if (!AircraftReworkOn)
                    {
                        return;
                    }


                    __instance.GeoscapeModules.ActionsBarModule.AircraftEquipmentDisplayEnabled = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
