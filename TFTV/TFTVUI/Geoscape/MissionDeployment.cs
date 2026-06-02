using Base;
using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace TFTV.TFTVUI.Geoscape
{
    internal class MissionDeployment
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static Color yellow = new Color(255, 255, 0, 1.0f);
        internal static Color dark = new Color(52, 52, 61, 1.0f);



        ///Patches to show mission light conditions
        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_EnterState_patch
        {
            public static void Postfix(UIStateRosterDeployment __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    GeoSite geoSite = null;

                    UIModuleActorCycle uIModuleActorCycle = controller.View.GeoscapeModules.ActorCycleModule;
                    UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                    GeoCharacter geoCharacter = uIModuleActorCycle.CurrentCharacter;

                    if (geoCharacter != null)
                    {
                        foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                        {
                            if (geoCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleClassTag) && geoVehicle.GroundVehicles.Contains(geoCharacter) ||
                                geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                                || geoVehicle.Soldiers.Contains(uIModuleActorCycle.CurrentCharacter))
                            {
                                geoSite = geoVehicle.CurrentSite;
                                break;
                            }
                        }

                        if (geoSite != null)
                        {

                            int hourOfTheDay = geoSite.LocalTime.DateTime.Hour;
                            int minuteOfTheHour = geoSite.LocalTime.DateTime.Minute;
                            bool dayTimeMission = hourOfTheDay >= 6 && hourOfTheDay <= 20;

                            TFTVLogger.Always($"LocalTime: {hourOfTheDay:00}:{minuteOfTheHour:00}");

                            Transform objectives = uIModuleDeploymentMissionBriefing.ObjectivesTextContainer.transform;
                            Transform lootContainer = uIModuleDeploymentMissionBriefing.AutolootContainer.transform;

                            Transform newIcon = UnityEngine.Object.Instantiate(lootContainer.GetComponent<Transform>().GetComponentInChildren<Image>().transform, uIModuleDeploymentMissionBriefing.MissionNameText.transform);

                            Sprite lightConditions = Helper.CreateSpriteFromImageFile(dayTimeMission ? "light_conditions_sun.png" : "light_conditions_moon.png");
                            Color color = dayTimeMission ? yellow : dark;

                            newIcon.GetComponentInChildren<Image>().sprite = lightConditions;
                            newIcon.GetComponentInChildren<Image>().color = color;

                            string text = $"Local time is {hourOfTheDay:00}:{minuteOfTheHour:00}";
                            newIcon.gameObject.AddComponent<UITooltipText>().TipText = text;

                          //  CreateBestEquipmentButton(geoSite);
                        }
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterDeployment), "ExitState")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_ExitState_patch
        {
            public static void Postfix(UIStateRosterDeployment __instance)
            {
                try
                {

                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                    uIModuleDeploymentMissionBriefing.MissionNameText.transform.DestroyChildren();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




    }
}
