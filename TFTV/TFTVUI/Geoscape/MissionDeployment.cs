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

        private static void CreateBestEquipmentButton(GeoSite geoSite)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                UIModuleGeneralPersonelRoster uIModuleGeoRoster = controller.View.GeoscapeModules.GeneralPersonelRosterModule;

                if (uIModuleDeploymentMissionBriefing.transform.GetComponentsInChildren<PhoenixGeneralButton>().FirstOrDefault(b => b.name == "EquipAllButton") != null)

                {
                    PhoenixGeneralButton phoenixGeneralButton = uIModuleDeploymentMissionBriefing.transform.GetComponentsInChildren<PhoenixGeneralButton>().FirstOrDefault(b => b.name == "EquipAllButton");
                    //TFTVLogger.Always($"found button {phoenixGeneralButton.name} enabled? {phoenixGeneralButton.enabled} gameobject active? {phoenixGeneralButton.gameObject.activeSelf}");

                    phoenixGeneralButton.gameObject.SetActive(true);
                    phoenixGeneralButton.RemoveAllClickedDelegates();
                    phoenixGeneralButton.PointerClicked += () => Personnel.Loadouts.EquipBestCurrentTeam(geoSite, uIModuleGeoRoster);
                    phoenixGeneralButton.ResetButtonAnimations();
                    return;
                }

                TFTVLogger.Always($"CreateBestEquipmentButton running");

                Resolution resolution = Screen.currentResolution;
                float resolutionFactorWidth = (float)resolution.width / 1920f;
                float resolutionFactorHeight = (float)resolution.height / 1080f;

                if (resolution.width == 1920 && resolutionFactorHeight == 1200)
                {
                    resolutionFactorHeight = 1;
                }

                EditUnitButtonsController editUnitButtonsController = controller.View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;

                uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.gameObject.SetActive(true);

                PhoenixGeneralButton useBestEquipmentButton = UnityEngine.Object.Instantiate(uIModuleDeploymentMissionBriefing.DeployButton, uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.transform);

                uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.transform.position += new Vector3(105 * resolutionFactorWidth, 5 * resolutionFactorHeight, 0);
                uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.fontSize -= 10;

                useBestEquipmentButton.name = "EquipAllButton";

                Text text = useBestEquipmentButton.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "UIText3Big").GetComponent<Text>();

                text.GetComponent<I2.Loc.Localize>().enabled = false;
                text.text = TFTVCommonMethods.ConvertKeyToString("KEY_UI_LOADUP_TEXT").ToUpper();

                Image image = useBestEquipmentButton.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Hotkey");


                if (image == null)
                {
                    TFTVLogger.Always($"image==null: {image == null}");
                    /*  GameObject iconObject = new GameObject("IconObject", typeof(Image), typeof(RectTransform));
                      iconObject.GetComponent<RectTransform>().SetParent(useBestEquipmentButton.transform);
                      image = iconObject.GetComponent<Image>();
                      image.preserveAspect = true;
                      image.SetNativeSize();*/
                }
                else
                {
                    image.sprite = Helper.CreateSpriteFromImageFile("Lockers.png");
                }

                useBestEquipmentButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_LOADUP_TIP");

                useBestEquipmentButton.PointerClicked += () => Personnel.Loadouts.EquipBestCurrentTeam(geoSite, uIModuleGeoRoster);
                useBestEquipmentButton.transform.position += new Vector3(-373 * resolutionFactorWidth, 264 * resolutionFactorHeight, 0);

                useBestEquipmentButton.SetInteractable(true);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


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

                            CreateBestEquipmentButton(geoSite);
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
