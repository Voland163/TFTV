using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVScrapAircraft
    {


        //taken & adjusted from Mad's Assorted Adjustments. All hail Mad! https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments/blob/main/Source/AssortedAdjustments/Patches/EnableScrapAircraft.cs

        private class ContainerInfo
        {
            public ContainerInfo(string name, int index)
            {
                this.Name = name;
                this.Index = index;
            }
            public string Name { get; }
            public int Index { get; }
        }

        // Cache reflected methods
        internal static MethodInfo ___UpdateResourceInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        private static List<VehicleItemDef> _vehicleDefs;

        private static void PopulateInternalVehicleDefsList()
        {
            try 
            {
                if (_vehicleDefs == null||_vehicleDefs.Count==0)
                {
                    _vehicleDefs = new List<VehicleItemDef>();
                    _vehicleDefs.AddRange(GameUtl.GameComponent<DefRepository>().GetAllDefs<VehicleItemDef>().ToList());
                }  
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        [HarmonyPatch(typeof(GeoRosterContainterItem), "Refresh")]
        public static class GeoRosterContainterItem_Refresh_Patch
        {

            public static void Postfix(GeoRosterContainterItem __instance)
            {
                try
                {

                    if (GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.CurrentViewState is UIStateGeoRoster uIStateGeoRoster)
                    {
                        GeoscapeViewContext ___Context =
                          (GeoscapeViewContext)AccessTools.Property(typeof(GeoscapeViewState), "Context").GetValue(uIStateGeoRoster, null);
                        UIModuleGeneralPersonelRoster ____geoRosterModule =
                            (UIModuleGeneralPersonelRoster)AccessTools.Property(typeof(UIStateGeoRoster), "_geoRosterModule").GetValue(uIStateGeoRoster, null);
                        List<IGeoCharacterContainer> ____characterContainers = (List<IGeoCharacterContainer>)AccessTools.Field(typeof(UIStateGeoRoster), "_characterContainers").GetValue(uIStateGeoRoster);
                        GeoRosterFilterMode ____preferableFilterMode = (GeoRosterFilterMode)AccessTools.Field(typeof(UIStateGeoRoster), "_preferableFilterMode").GetValue(uIStateGeoRoster);

                        CreateScrapeButtons(____geoRosterModule, uIStateGeoRoster, ___Context, ____geoRosterModule, ____characterContainers, ____preferableFilterMode);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void RemoveEquipmentFromScrappedVehicle(GeoVehicle aircraftToScrap)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                GeoFaction geoFaction = controller.PhoenixFaction;

                foreach (GeoVehicleEquipment geoVehicleEquipment in aircraftToScrap.Equipments)
                {

                    if (geoVehicleEquipment != null)
                    {
                        // TFTVLogger.Always($"{geoVehicleEquipment} being added ");
                        // GeoVehicleEquipmentUIData geoVehicleEquipmentUIData = geoVehicleEquipment.CreateUIData();
                        geoFaction.AircraftItemStorage.AddItem(geoVehicleEquipment);
                        // vehicleEquipModule.StorageList.AddItem(geoVehicleEquipmentUIData);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void CreateScrapeButtons(UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster,
            UIStateGeoRoster uIStateGeoRoster, GeoscapeViewContext context, UIModuleGeneralPersonelRoster geoRosterModule, List<IGeoCharacterContainer> characterContainers, GeoRosterFilterMode preferableFilterMode)
        {
            try
            {

                GeoFaction owningFaction = context.ViewerFaction;

                for (int i = 0; i < geoRosterModule.Groups.Count; i++)
                {
                    GeoRosterContainterItem c = geoRosterModule.Groups[i];
              
                    ContainerInfo containerInfo = new ContainerInfo(c.Container.Name, i);
                    string aircraftIdentifier = containerInfo.Name;
                    GeoVehicle aircraftToScrap = owningFaction.Vehicles.Where(v => v.Name == aircraftIdentifier).FirstOrDefault();

                    if (aircraftToScrap != null && c.Container.CurrentOccupiedSpace == 0 && c.GetComponentInChildren<PhoenixGeneralButton>() == null)
                    {
                        ScrapeButtonFunctionality(uIModuleGeneralPersonelRoster, uIStateGeoRoster, context, geoRosterModule,
                            containerInfo, characterContainers, preferableFilterMode, c, aircraftToScrap);
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ScrapeButtonFunctionality(UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster,
            UIStateGeoRoster uIStateGeoRoster, GeoscapeViewContext context, UIModuleGeneralPersonelRoster geoRosterModule,
            ContainerInfo containerInfo, List<IGeoCharacterContainer> characterContainers, GeoRosterFilterMode preferableFilterMode,
            GeoRosterContainterItem geoRosterContainterItem, GeoVehicle geoVehicle)
        {
            try
            {

                Resolution resolution = Screen.currentResolution;

                // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                float resolutionFactorWidth = (float)resolution.width / 1920f;
                //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                float resolutionFactorHeight = (float)resolution.height / 1080f;
                //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                // TFTVLogger.Always($"checking");

                EditUnitButtonsController editUnitButtonsController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;

                PhoenixGeneralButton checkButton = UnityEngine.Object.Instantiate(editUnitButtonsController.DismissButton, geoRosterContainterItem.EmptySlot.transform);
                checkButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_SCRAP_AIRCRAFT");// "Toggles helmet visibility on/off.";

                //  UIButtonIconController uIButtonIconController = checkButton.GetComponent<UIButtonIconController>();

                // uIButtonIconController.Icon.gameObject.SetActive(true);

                // checkButton.GetComponentInChildren<Image>().sprite = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Warcry_Cancel.png");

                checkButton.transform.position += new Vector3(650 * resolutionFactorWidth, 30 * resolutionFactorHeight);
                checkButton.PointerClicked += () => OnScrapAircraftClick();

                void OnScrapAircraftClick()
                {
                    TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftClick(containerInfo: {containerInfo.Name}, {containerInfo.Index})");


                    GeoVehicle aircraftToScrap = geoVehicle;
                    GeoscapeModulesData ____geoscapeModules = (GeoscapeModulesData)AccessTools.Property(typeof(GeoscapeViewState), "_geoscapeModules").GetValue(uIStateGeoRoster, null);
                    UIModuleGeoscapeScreenUtils ____utilsModule = ____geoscapeModules.GeoscapeScreenUtilsModule;
                    string messageBoxText = ____utilsModule.DismissVehiclePrompt.Localize(null);
                    VehicleItemDef aircraftItemDef =_vehicleDefs.Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();

                    if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                    {
                        messageBoxText = messageBoxText + "\n" + ____utilsModule.ScrapResourcesBack.Localize(null) + "\n \n";
                        foreach (ResourceUnit resourceUnit in ((IEnumerable<ResourceUnit>)aircraftItemDef.ScrapPrice))
                        {
                            if (resourceUnit.RoundedValue > 0)
                            {
                                string resourcesInfo = "";
                                ResourceType type = resourceUnit.Type;
                                switch (type)
                                {
                                    case ResourceType.Supplies:
                                        resourcesInfo = ____utilsModule.ScrapSuppliesResources.Localize(null);
                                        break;
                                    case ResourceType.Materials:
                                        resourcesInfo = ____utilsModule.ScrapMaterialsResources.Localize(null);
                                        break;
                                    case (ResourceType)3:
                                        break;
                                    case ResourceType.Tech:
                                        resourcesInfo = ____utilsModule.ScrapTechResources.Localize(null);
                                        break;
                                    default:
                                        if (type == ResourceType.Mutagen)
                                        {
                                            resourcesInfo = ____utilsModule.ScrapMutagenResources.Localize(null);
                                        }
                                        break;
                                }
                                resourcesInfo = resourcesInfo.Replace("{0}", resourceUnit.RoundedValue.ToString());
                                messageBoxText += resourcesInfo;
                            }
                        }
                    }

                    // Safety check as the game's UI fails hard if there's NO GeoVehicle left at all
                    if (geoVehicle.Owner.Vehicles.Count() <= 1)
                    {
                        GameUtl.GetMessageBox().ShowSimplePrompt("This is Phoenix Point's last aircraft available", MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                    }
                    else
                    {
                        GameUtl.GetMessageBox().ShowSimplePrompt(string.Format(messageBoxText, geoVehicle.Name), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnScrapAircraftCallback), null, containerInfo);
                    }
                }

                void OnScrapAircraftImpossibleCallback(MessageBoxCallbackResult msgResult)
                {
                    // Nothing
                }

                void OnScrapAircraftCallback(MessageBoxCallbackResult msgResult)
                {
                    if (msgResult.DialogResult == MessageBoxResult.Yes)
                    {
                        ContainerInfo containerInfo2 = msgResult.UserData as ContainerInfo;
                        TFTVLogger.Info($"[UIStateGeoRoster_EnterState_POSTFIX] OnScrapAircraftCallback(containerInfo: {containerInfo.Name}, {containerInfo.Index})");

                        string aircraftIdentifier = containerInfo.Name;
                        int groupIndex = containerInfo.Index;
                        GeoFaction owningFaction = context.ViewerFaction;
                        GeoVehicle aircraftToScrap = owningFaction.Vehicles.Where(v => v.Name == aircraftIdentifier).FirstOrDefault();

                        if (aircraftToScrap != null)
                        {
                            // Unset vehicle.CurrentSite and trigger site.VehicleLeft
                            aircraftToScrap.Travelling = true;

                            RemoveEquipmentFromScrappedVehicle(aircraftToScrap);
                            // Away with it!
                            aircraftToScrap.Destroy();

                            // Add resources
                            VehicleItemDef aircraftItemDef = _vehicleDefs.Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();
                            if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                            {
                                context.Level.PhoenixFaction.Wallet.Give(aircraftItemDef.ScrapPrice, OperationReason.Scrap);

                                GeoscapeModulesData ____geoscapeModules = (GeoscapeModulesData)AccessTools.Property(typeof(GeoscapeViewState), "_geoscapeModules").GetValue(uIStateGeoRoster, null);

                                //MethodInfo ___UpdateResourceInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                                ___UpdateResourceInfo.Invoke(____geoscapeModules.ResourcesModule, new object[] { owningFaction, true });
                            }

                            // Clean roster from aircraft container
                            characterContainers.RemoveAt(groupIndex);

                            // Reset roster list
                            geoRosterModule.Init(context, characterContainers, null, preferableFilterMode, RosterSelectionMode.SingleSelect);

                            // Reapply events to the correct slots
                            CreateScrapeButtons(uIModuleGeneralPersonelRoster, uIStateGeoRoster, context, geoRosterModule, characterContainers, preferableFilterMode);
                        }
                        else
                        {
                            TFTVLogger.Debug($"[UIStateGeoRoster_EnterState_POSTFIX] Couldn't get GeoVehicle from aircraftIdentifier: {aircraftIdentifier}");
                        }
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")]
        public static class UIStateGeoRoster_EnterState_Patch
        {

            public static void Postfix(UIStateGeoRoster __instance, List<IGeoCharacterContainer> ____characterContainers, GeoRosterFilterMode ____preferableFilterMode)
            {
                try
                {
                    GeoscapeViewContext ___Context =
                        (GeoscapeViewContext)AccessTools.Property(typeof(GeoscapeViewState), "Context").GetValue(__instance, null);

                    UIModuleGeneralPersonelRoster ____geoRosterModule =
                        (UIModuleGeneralPersonelRoster)AccessTools.Property(typeof(UIStateGeoRoster), "_geoRosterModule").GetValue(__instance, null);

                    PopulateInternalVehicleDefsList();

                    CreateScrapeButtons(____geoRosterModule, __instance, ___Context, ____geoRosterModule, ____characterContainers, ____preferableFilterMode);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}

