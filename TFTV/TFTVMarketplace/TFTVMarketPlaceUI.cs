using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.Vehicles.Ammo;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {
        internal class TFTVMarketPlaceUI
        {
            private static void FakeResearchOptionToSetupCharacterSale(UIModuleTheMarketplace uIModuleTheMarketplace)
            {
                try
                {
                    // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    uIModuleTheMarketplace.ResearchRoot.SetActive(value: true);
                    uIModuleTheMarketplace.ItemsRoot.SetActive(value: false);
                    ResearchDef researchById = uIModuleTheMarketplace.Context.Level.GetResearchById("PX_Synedrion_ResearchDef");
                    ResearchElement researchElement = new ResearchElement(researchById);
                    researchElement.Init(uIModuleTheMarketplace.Context.ViewerFaction, researchById);
                    researchElement.State = ResearchState.Revealed;
                    uIModuleTheMarketplace.ResearchInfo.Init(researchElement);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ResearchInfoForCharacterKludge(GeoEventChoice choice, UIModuleTheMarketplace uIModuleTheMarketplace)
            {
                try
                {


                    if (choice != null && choice.Outcome != null && choice.Outcome.Units != null && choice.Outcome.Units.Count > 0
                        && choice.Outcome.Units[0] is TacCharacterDef tacCharacterDef && tacCharacterDef.Data.GameTags.Contains(MercenaryTag))
                    {
                        FakeResearchOptionToSetupCharacterSale(uIModuleTheMarketplace);

                        uIModuleTheMarketplace.ResearchRoot.SetActive(false);
                        uIModuleTheMarketplace.ResearchRoot.SetActive(true);

                        uIModuleTheMarketplace.ResearchInfo.Title.text = TFTVCommonMethods.ConvertKeyToString(tacCharacterDef.Data.ViewElementDef.DisplayName1.LocalizationKey);
                        /*  uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta =
                              new Vector2(uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta.x * 2, uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta.y);
                          uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMaxSize = 48;*/

                        /*   TFTVLogger.Always($"font size: {uIModuleTheMarketplace.ResearchInfo.Title.fontSize}; " +
                               $"size of rectransfrom {uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta}; " +
                               $"resize text max size: {uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMaxSize};" +
                               $"resize text min size:{uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMinSize}" +
                               $"resize text for best fit: {uIModuleTheMarketplace.ResearchInfo.Title.resizeTextForBestFit}");*/

                        uIModuleTheMarketplace.ResearchInfo.Description.text = TFTVCommonMethods.ConvertKeyToString(tacCharacterDef.Data.ViewElementDef.Description.LocalizationKey);
                        uIModuleTheMarketplace.ResearchInfo.BenefitsContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.ResourceContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.RequirementsContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.Icon.sprite = tacCharacterDef.Data.ViewElementDef.InventoryIcon;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            [HarmonyPatch(typeof(UIModuleTheMarketplace), nameof(UIModuleTheMarketplace.SetupChoiceInfoBlock))]
            public static class UIModuleTheMarketplace_SetupChoiceInfoBlock_patch
            {
                public static void Postfix(UIModuleTheMarketplace __instance, GeoEventChoice choice)
                {
                    try
                    {

                        ResearchInfoForCharacterKludge(choice, __instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleTheMarketplace), nameof(UIModuleTheMarketplace.UpdateVisuals))]
            public static class UIModuleTheMarketplace_UpdateVisuals_patch
            {
                public static void Postfix(UIModuleTheMarketplace __instance)
                {
                    try
                    {
                        Text timeToRestock = __instance.transform.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name.Equals("OffersHint"));
                        string text = TFTVCommonMethods.ConvertKeyToString("DLC5/KEY_MARKETPLACE_UPDATE_DESCRIPTION");

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        GeoMarketplace geoMarketplace = controller.Marketplace;

                        FieldInfo fieldInfo_updateOptionsNextTime = typeof(GeoMarketplace).GetField("_updateOptionsNextTime", BindingFlags.Instance | BindingFlags.NonPublic);

                        TimeUnit updateTime = (TimeUnit)fieldInfo_updateOptionsNextTime.GetValue(geoMarketplace);
                        TimeUnit currentTime = controller.Timing.Now;

                        int daysToRotation = Mathf.Max(updateTime.DateTime.Day - currentTime.DateTime.Day, 1);

                        string suffix = TFTVCommonMethods.ConvertKeyToString("KEY_DAYS");

                        if (daysToRotation == 1)
                        {
                            suffix = TFTVCommonMethods.ConvertKeyToString("KEY_DAY");
                        }

                        timeToRestock.text = $"{text} {daysToRotation} {suffix.ToUpper()}";

                        __instance.NoOffersAvailableHint.SetActive(false);

                        //   TFTVLogger.Always($"Running UpdateVisuals");

                        CreateItemFilter();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleTheMarketplace), "OnChoiceSelected")] //VERIFIED
            public static class UIModuleTheMarketplace_OnChoiceSelected_patch
            {

                public static void Postfix(UIModuleTheMarketplace __instance, GeoEventChoice choice)
                {
                    try
                    {

                        // TFTVLogger.Always($"Running OnChoiceSelected");

                        if (choice.Outcome.Units.Count > 0 && choice.Outcome.Units[0] is TacCharacterDef tacCharacterDef && tacCharacterDef.Data.GameTags.Contains(MercenaryTag))
                        {
                            __instance.Loca_AllMissionsFinishedDesc.LocalizationKey = tacCharacterDef.Data.ViewElementDef.Category.LocalizationKey;
                            __instance.UpdateVisuals();
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /// <summary>
            /// Internal stock record keeping
            /// Fixes money spent no purchase made at Marketplace if 2 or more aircraft at Marketplace
            /// </summary>
            // inside TFTVChangesToDLC5 (same containing type where your existing patch lives)
            [HarmonyPatch(typeof(GeoscapeEvent), nameof(GeoscapeEvent.CompleteMarketplaceEvent))]
            public static class GeoscapeEvent_CompleteMarketplaceEvent_patch
            {

                public static bool Prefix(GeoscapeEvent __instance, GeoEventChoice choice, GeoFaction faction)
                {
                    try
                    {
                        CheckSecretMPCounter();

                        if (MPGeoEventChoices != null && MPGeoEventChoices.Contains(choice))
                        {
                            MPGeoEventChoices.Remove(choice);
                        }

                        if (choice.Outcome != null && choice.Outcome.Items != null && choice.Outcome.Items.Count > 0)
                        {
                            TFTVLogger.Always($"CompleteMarketplaceEvent choice: {choice.Outcome?.Items[0].ItemDef.name}");
                        }

                        if (__instance.Context.Site.Vehicles.Count() > 1)
                        {
                            TFTVLogger.Always($"There is a more than one vehicle at {__instance.Context.Site.LocalizedSiteName}! Need to execute alternative code");

                            PropertyInfo propertyInfo = typeof(GeoscapeEvent).GetProperty("ChoiceReward", BindingFlags.Instance | BindingFlags.Public);

                            GeoLevelController component = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(component.PhoenixFaction.StartingBase, component.PhoenixFaction, __instance.Context.Site.Vehicles.First());

                            propertyInfo?.SetValue(__instance, choice.Outcome.GenerateFactionReward(faction, geoscapeEventContext, __instance.EventID));
                            __instance.ChoiceReward.Apply(faction, geoscapeEventContext.Site, geoscapeEventContext.Vehicle);

                            if (choice.Outcome.ReEneableEvent)
                            {
                                GameUtl.CurrentLevel().GetComponent<GeoscapeEventSystem>().EnableGeoscapeEvent(__instance.EventID);
                            }

                            return false;
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /* public static void Postfix(GeoscapeEvent __instance, GeoEventChoice choice, GeoFaction faction)
             {
                 try
                 {
                     if (__instance.Context.Site.Vehicles.Count() == 0 && choice != null && choice.Outcome != null && choice.Outcome.Units != null && choice.Outcome.Units.Count > 0
                     && choice.Outcome.Units[0] is TacCharacterDef tacCharacterDef) 
                     { 
                     faction.GeoLevel.PhoenixFaction.AddRecruit()

                     }



                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }*/


            [HarmonyPatch(typeof(UIStateMarketplaceGeoscapeEvent), "ExitState")] //VERIFIED
            public static class UIStateMarketplaceGeoscapeEvent_ExitState_patch
            {

                public static void Postfix()
                {
                    try
                    {
                        //   TFTVLogger.Always($"Running ExitState marketplace");
                        GeoMarketplace geoMarketplace = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().Marketplace;
                        if (MPGeoEventChoices != null && MPGeoEventChoices.Count > 0)
                        {
                            /* foreach(GeoEventChoice geoEventChoice in MPGeoEventChoices) 
                             {
                                 if(geoEventChoice.Outcome!=null && geoEventChoice.Outcome.Items!=null && geoEventChoice.Outcome.Items.Count>0)

                               //  TFTVLogger.Always($"geoeventChoice: {geoEventChoice.Outcome?.Items[0].ItemDef.name}");          
                             }*/

                            PropertyInfo propertyInfo = typeof(GeoMarketplace).GetProperty("MarketplaceChoices", BindingFlags.Instance | BindingFlags.Public);

                            // TFTVLogger.Always($"before manually transferring the MarketChoices {propertyInfo.GetValue(geoMarketplace)}");                
                            propertyInfo.SetValue(geoMarketplace, new List<GeoEventChoice>(MPGeoEventChoices));
                            //  TFTVLogger.Always($"after manually transferring the MarketChoices {propertyInfo.GetValue(geoMarketplace)}");
                            MPGeoEventChoices = null;
                            //  TFTVLogger.Always($"after clearing the internal MarketChoices list {propertyInfo.GetValue(geoMarketplace)}");
                            UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;
                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("UI_KaosMarket_Image_uinomipmaps.jpg");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            public static PhoenixGeneralButton MarketToggleButton = null;

            private static void CreateItemFilter()
            {
                try
                {
                    if (MarketToggleButton == null)
                    {

                        UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;

                        marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                        marketplaceUI.MissionRewardDescriptionText.gameObject.SetActive(true);

                        marketplaceUI.MissionRewardHeaderText.text = "";
                        marketplaceUI.MissionRewardDescriptionText.text = "";

                        Resolution resolution = Screen.currentResolution;



                        // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                        float resolutionFactorHeight = (float)resolution.height / 1080f;
                        //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);
                        bool ultrawideresolution = resolutionFactorWidth / resolutionFactorHeight > 2;
                        //  marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                        PhoenixGeneralButton allToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton vehicleToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton equipmentToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton otherToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);


                        string allText = TFTVCommonMethods.ConvertKeyToString("KEY_GEOSCAPE_ALL");

                        allToggle.gameObject.AddComponent<UITooltipText>().TipText = allText;
                        allToggle.gameObject.SetActive(true);
                        allToggle.PointerClicked += () => ToggleButtonClicked(0);
                        allToggle.transform.GetComponentInChildren<Text>().text = allText;
                        //  allToggle.transform.localScale *= 0.6f;

                        if (!ultrawideresolution)
                        {
                            allToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }


                        allToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x * 0.65f, allToggle.GetComponent<RectTransform>().sizeDelta.y);

                        string vehiclesText = TFTVCommonMethods.ConvertKeyToString("KEY_GAME_SUMMARY_PROD_VEHICLES");

                        vehicleToggle.gameObject.AddComponent<UITooltipText>().TipText = vehiclesText;
                        vehicleToggle.gameObject.SetActive(true);
                        vehicleToggle.PointerClicked += () => ToggleButtonClicked(1);
                        vehicleToggle.transform.GetComponentInChildren<Text>().text = vehiclesText;
                        vehicleToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("UI_Vehicle_FilterIcon.png");

                        if (!ultrawideresolution)
                        {
                            vehicleToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 0, 0); //new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }

                        vehicleToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);

                        string equipmentText = TFTVCommonMethods.ConvertKeyToString("KEY_GAME_SUMMARY_PROD_EQUIPMENT");
                        equipmentToggle.gameObject.AddComponent<UITooltipText>().TipText = equipmentText;
                        equipmentToggle.gameObject.SetActive(true);
                        equipmentToggle.PointerClicked += () => ToggleButtonClicked(2);
                        equipmentToggle.transform.GetComponentInChildren<Text>().text = equipmentText;
                        //    equipmentToggle.transform.localScale *= 0.5f;

                        if (!ultrawideresolution)
                        {
                            equipmentToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 0, 0);
                        }
                        equipmentToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);
                        equipmentToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("MP_UI_Choices_Equipment.png");


                        string otherText = TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_OTHER");

                        otherToggle.gameObject.AddComponent<UITooltipText>().TipText = otherText;
                        otherToggle.gameObject.SetActive(true);
                        otherToggle.PointerClicked += () => ToggleButtonClicked(3);
                        otherToggle.transform.GetComponentInChildren<Text>().text = otherText;
                        //   otherToggle.transform.localScale *= 0.5f;
                        if (!ultrawideresolution)
                        {
                            otherToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }
                        otherToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);
                        otherToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("Geoscape_Icon_Research.png");

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static List<GeoEventChoice> MPGeoEventChoices = null;


            private static bool CheckIfMarketChoiceVehicle(GeoEventChoice choice)
            {
                try
                {

                    GeoEventChoiceOutcome outcome = choice.Outcome;

                    /* if (outcome.Items.Count > 0) 
                     {
                         TFTVLogger.Always($"Checking if Market choice is vehicle: {outcome.Items[0].ItemDef.name}; {outcome.Items[0].ItemDef.Tags.Contains(VehiclesAmmoMain.MarketplaceGroundVehicleWeapon)}");
                     }*/



                    if (outcome.Items.Count > 0 && (outcome.Items[0].ItemDef.name.Contains("GroundVehicle")
                        || VehiclesAmmoMain.MarketplaceWeaponsAndAmmo.Keys.Any(k => k.CompatibleAmmunition[0] == outcome.Items[0].ItemDef))
                        || outcome.Units.Count > 0 && outcome.Units[0].name.Contains("KS_Kaos_Buggy")) //&& choice.Outcome.Units[0].name.Contains("KS_Kaos_Buggy")))
                    {
                        return true;
                    }
                    else return false;
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool CheckIfMarketChoiceWeaponOrAmmo(GeoEventChoice choice)
            {
                try
                {
                    if (choice.Outcome != null && choice.Outcome.Items != null
                                        && choice.Outcome.Items.Count > 0 && choice.Outcome.Items[0].ItemDef != null
                                        && (choice.Outcome.Items[0].ItemDef.name.Contains("WeaponDef") || choice.Outcome.Items[0].ItemDef.name.Contains("AmmoClip"))
                                        && !CheckIfMarketChoiceVehicle(choice))//!choice.Outcome.Items[0].ItemDef.name.Contains("GroundVehicle"))
                    {

                        return true;

                    }
                    else return false;


                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static void FilterMarketPlaceOptions(GeoMarketplace geoMarketplace, int filter)
            {
                try
                {
                    PropertyInfo propertyInfoMarketPlaceChoicesGeoEventChoice = typeof(GeoMarketplace).GetProperty("MarketplaceChoices", BindingFlags.Instance | BindingFlags.Public);

                    if (MPGeoEventChoices == null)
                    {
                        // TFTVLogger.Always($"saving all Choices to internal list, count is {geoMarketplace.MarketplaceChoices.Count}");
                        MPGeoEventChoices = geoMarketplace.MarketplaceChoices;
                    }
                    else
                    {
                        // TFTVLogger.Always($"passing all Choices from internal list, count {MPGeoEventChoices.Count}, to proper list, count {geoMarketplace.MarketplaceChoices.Count}");
                        propertyInfoMarketPlaceChoicesGeoEventChoice?.SetValue(geoMarketplace, MPGeoEventChoices);

                    }

                    List<GeoEventChoice> choicesToShow = new List<GeoEventChoice>();

                    if (filter != 0)
                    {
                        if (filter == 1)
                        {
                            // TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (CheckIfMarketChoiceVehicle(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    //TFTVLogger.Always($"the vehicle equipment choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }

                            propertyInfoMarketPlaceChoicesGeoEventChoice.SetValue(geoMarketplace, choicesToShow);

                        }
                        else if (filter == 2)
                        {
                            //   TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (CheckIfMarketChoiceWeaponOrAmmo(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    // TFTVLogger.Always($"the weapon or ammo choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }

                        }
                        else if (filter == 3)
                        {
                            //   TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (!CheckIfMarketChoiceWeaponOrAmmo(geoMarketplace.MarketplaceChoices[i]) && !CheckIfMarketChoiceVehicle(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    // TFTVLogger.Always($"the other choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }
                        }

                        propertyInfoMarketPlaceChoicesGeoEventChoice.SetValue(geoMarketplace, choicesToShow);
                    }
                    //  TFTVLogger.Always($"Count of proper list (that will be shown) is {geoMarketplace.MarketplaceChoices.Count}");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ToggleButtonClicked(int filter)
            {
                try
                {
                    GeoMarketplace geoMarketplace = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().Marketplace;
                    UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;
                    FieldInfo fieldInfoGeoEventGeoscapeEvent = typeof(UIModuleTheMarketplace).GetField("_geoEvent", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo methodInfoUpdateList = typeof(UIModuleTheMarketplace).GetMethod("UpdateList", BindingFlags.NonPublic | BindingFlags.Instance);


                    switch (filter)
                    {
                        case 0:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_All.jpg");
                            break;

                        case 1:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("loading_screen26.jpg");
                            break;

                        case 2:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_Equipment.jpg");
                            break;

                        case 3:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_Other.jpg");
                            break;

                    }

                    marketplaceUI.ListScrollRect.ScrollToElement(0);
                    FilterMarketPlaceOptions(geoMarketplace, filter);

                    methodInfoUpdateList.Invoke(marketplaceUI, new object[] { fieldInfoGeoEventGeoscapeEvent.GetValue(marketplaceUI) });


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            /// <summary>
            /// Easter egg conversation in the Marketplace after player makes a lot of purchases
            /// </summary>


            public static int SecretMPCounter;
            private static void CheckSecretMPCounter()
            {
                try
                {
                    SecretMPCounter++;

                    UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;

                    if (SecretMPCounter >= 20 && SecretMPCounter <= 41)
                    {

                        TFTVLogger.Always("Should trigger MP EE");
                        // TFTVLogger.Always($"{marketplaceUI.MissionDescriptionText.text}");

                        marketplaceUI.Loca_AllMissionsFinishedDesc.LocalizationKey = "KEY_SECRET_MARKETPLACE_TEXT" + (SecretMPCounter - 20);

                        // marketplaceUI.MissionDescriptionText.text = TFTVCommonMethods.ConvertKeyToString("KEY_SECRET_MARKETPLACE_TEXT0");// + );


                    }

                    if (SecretMPCounter >= 40)
                    {
                        SecretMPCounter = 0;
                        marketplaceUI.Loca_AllMissionsFinishedDesc.LocalizationKey = "KEY_MARKETPLACE_DESCRIPTION_5";
                    }

                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
    }
}


