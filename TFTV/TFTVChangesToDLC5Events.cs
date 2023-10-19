using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Interception;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Levels.GeoMissionGenerator;

namespace TFTV
{
    internal class TFTVChangesToDLC5Events
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly WeaponDef Obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
        private static readonly WeaponDef Subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
        private static readonly WeaponDef Redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
        private static readonly WeaponDef Devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
        private static readonly WeaponDef Tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static void ChangesToDLC5Defs()
        {
            try
            {
                foreach (GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef in Repo.GetAllDefs<GeoMarketplaceItemOptionDef>())
                {
                    if (!geoMarketplaceItemOptionDef.DisallowDuplicates)
                    {
                        geoMarketplaceItemOptionDef.DisallowDuplicates = true;

                    }
                }


                GeoMarketplaceResearchOptionDef randomMarketResearch = DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef");
                randomMarketResearch.DisallowDuplicates = true;
                randomMarketResearch.Availability = 8;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

      


        /// <summary>
        /// Ensures that rescue vehicle missions will not contain faction vehicles if they haven't been researched by the faction yet.
        /// </summary>

        [HarmonyPatch(typeof(GeoMissionGenerator), "GetRandomMission", new Type[] { typeof(IEnumerable<MissionTagDef>), typeof(ParticipantFilter), typeof(Func<TacMissionTypeDef, bool>) })]
        public static class GeoMissionGenerator_GetRandomMission_patch
        {

            public static void Prefix(GeoMissionGenerator __instance, IEnumerable<MissionTagDef> tags, out List<CustomMissionTypeDef> __state, GeoLevelController ____level)
            {
                try
                {
                    ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                    ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");

                    MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                    __state = new List<CustomMissionTypeDef>();


                    if (tags.Contains(requiresVehicle) && ____level != null)
                    {
                        TFTVLogger.Always($"Generating rescue Vehicle scav; checking if factions have researched Aspida/Armadillo");
                        GeoLevelController controller = ____level;

                        if (!CheckResearchCompleted(controller.NewJerichoFaction, "NJ_VehicleTech_ResearchDef"))
                        {

                            TFTVLogger.Always($"Armadillo not researched by New Jericho");

                            foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                            {
                                if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == armadillo)
                                {

                                    __state.Add(customMissionTypeDef);

                                }

                            }

                        }
                        if (!CheckResearchCompleted(controller.SynedrionFaction, "SYN_Rover_ResearchDef"))
                        {
                            TFTVLogger.Always($"Aspida not researched by Synedrion");

                            foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                            {
                                if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == aspida)
                                {

                                    __state.Add(customMissionTypeDef);

                                }
                            }
                        }

                        if (__state.Count > 0)
                        {
                            TFTVLogger.Always($"Removing rescue vehicle missions with not researched vehicles from generation pool");

                            foreach (CustomMissionTypeDef mission in __state)
                            {
                                mission.Tags.Remove(requiresVehicle);



                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static void Postfix(GeoMissionGenerator __instance, IEnumerable<MissionTagDef> tags, in List<CustomMissionTypeDef> __state)
            {
                try
                {
                    ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                    ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");


                    MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                    if (tags.Contains(DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef")) && __state.Count > 0)
                    {
                        TFTVLogger.Always($"Adding back missions that were removed from the pool");

                        foreach (CustomMissionTypeDef mission in __state)
                        {

                            if (!mission.Tags.Contains(requiresVehicle))
                            {
                                mission.Tags.Add(requiresVehicle);

                            }

                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static bool CheckResearchCompleted(GeoFaction faction, string researchID)
        {
            try
            {
                if (faction != null && faction.Research != null && faction.Research.HasCompleted(researchID))
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


        [HarmonyPatch(typeof(UIModuleTheMarketplace), "UpdateVisuals")]
        public static class UIModuleTheMarketplace_UpdateVisuals_patch
        {

            public static void Postfix(UIModuleTheMarketplace __instance)
            {
                try
                {

                    //   TFTVLogger.Always($"Running UpdateVisuals");

                    CreateTestingButton();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleTheMarketplace), "OnChoiceSelected")]
        public static class UIModuleTheMarketplace_OnChoiceSelected_patch
        {

            public static void Postfix(UIModuleTheMarketplace __instance, GeoEventChoice choice)
            {
                try
                {

                    //  TFTVLogger.Always($"Running OnChoiceSelected");
                    if (MPGeoEventChoices != null && MPGeoEventChoices.Contains(choice))
                    {
                        //    TFTVLogger.Always($"Removing choice from internally saved list");

                        MPGeoEventChoices.Remove(choice);
                    }

                    CheckSecretMPCounter();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIStateMarketplaceGeoscapeEvent), "ExitState")]
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


        private static void CreateTestingButton()
        {
            try
            {
                if (MarketToggleButton == null)
                {



                    UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;
                    //   UIModuleManufacturing sourceModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ManufacturingModule;//.CommonModules.PauseScreenModule.OptionsSubmenuModule;

                    marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                    marketplaceUI.MissionRewardDescriptionText.gameObject.SetActive(true);

                    marketplaceUI.MissionRewardHeaderText.text = "";
                    marketplaceUI.MissionRewardDescriptionText.text = "";

                    Resolution resolution = Screen.currentResolution;

                    /*     List<Image> images = marketplaceUI.transform.GetComponentsInChildren<Image>().ToList();

                         TFTVLogger.Always($"there are {images.Count} images");

                         foreach (Image image in images) 
                         {
                             TFTVLogger.Always($"{image.name}");


                         }





                         foreach (Transform transform in marketplaceUI.transform.GetChildren())
                         {
                             TFTVLogger.Always($"{transform.name}, it's level 1, has {transform.GetChildren().Count()} children");

                             foreach (Transform transform1 in transform.GetChildren())
                             {
                                 TFTVLogger.Always($"{transform1.name}, it's level 2, has {transform1.GetChildren().Count()} children");

                                 foreach (Transform transform2 in transform1.GetChildren())
                                 {
                                     TFTVLogger.Always($"{transform2.name},  level3, has {transform2.GetChildren().Count()} children");

                                     foreach (Transform transform3 in transform2.GetChildren())
                                     {
                                         TFTVLogger.Always($"{transform3.name},  level3, has {transform3.GetChildren().Count()} children");

                                         foreach (Transform transform4 in transform3.GetChildren())
                                         {
                                             TFTVLogger.Always($"{transform4.name},  level3, has {transform4.GetChildren().Count()} children");

                                             foreach (Transform transform5 in transform4.GetChildren())
                                             {
                                                 TFTVLogger.Always($"{transform5.name},  level3, has {transform5.GetChildren().Count()} children");



                                             }

                                         }

                                     }

                                 }

                             }
                         }*/



                    // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;
                    //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                    //  marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                    PhoenixGeneralButton allToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                    PhoenixGeneralButton vehicleToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                    PhoenixGeneralButton equipmentToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                    PhoenixGeneralButton otherToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);

                    allToggle.gameObject.AddComponent<UITooltipText>().TipText = "ALL";
                    allToggle.gameObject.SetActive(true);
                    allToggle.PointerClicked += () => ToggleButtonClicked(0);
                    allToggle.transform.GetComponentInChildren<Text>().text = "ALL";
                    //  allToggle.transform.localScale *= 0.6f;
                    allToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);



                    allToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x * 0.65f, allToggle.GetComponent<RectTransform>().sizeDelta.y);


                    vehicleToggle.gameObject.AddComponent<UITooltipText>().TipText = "VEHICLES";
                    vehicleToggle.gameObject.SetActive(true);
                    vehicleToggle.PointerClicked += () => ToggleButtonClicked(1);
                    vehicleToggle.transform.GetComponentInChildren<Text>().text = "VEHICLES";
                    vehicleToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("UI_Vehicle_FilterIcon.png");
                    // vehicleToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").rectTransform.sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x * 0.7f, allToggle.GetComponent<RectTransform>().sizeDelta.y * 0.7f);


                    /*  List<Image> images = vehicleToggle.transform.GetComponentsInChildren<Image>().ToList();

                      TFTVLogger.Always($"there are {images.Count} images");

                      foreach (Image image in images)
                      {
                          TFTVLogger.Always($"{image.name}");


                      }*/

                    //  vehicleToggle.transform.localScale *= 0.5f;
                    vehicleToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 0, 0); //new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                    vehicleToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);


                    equipmentToggle.gameObject.AddComponent<UITooltipText>().TipText = "EQUIPMENT";
                    equipmentToggle.gameObject.SetActive(true);
                    equipmentToggle.PointerClicked += () => ToggleButtonClicked(2);
                    equipmentToggle.transform.GetComponentInChildren<Text>().text = "EQUIPMENT";
                    //    equipmentToggle.transform.localScale *= 0.5f;
                    equipmentToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 0, 0);
                    equipmentToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);
                    equipmentToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("MP_UI_Choices_Equipment.png");

                    otherToggle.gameObject.AddComponent<UITooltipText>().TipText = "OTHER";
                    otherToggle.gameObject.SetActive(true);
                    otherToggle.PointerClicked += () => ToggleButtonClicked(3);
                    otherToggle.transform.GetComponentInChildren<Text>().text = "OTHER";
                    //   otherToggle.transform.localScale *= 0.5f;
                    otherToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
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

                if (choice.Outcome.Items.Count > 0 && choice.Outcome.Items[0].ItemDef.name.Contains("GroundVehicle")
                    || choice.Outcome.Units.Count > 0 && choice.Outcome.Units[0].name.Contains("KS_Kaos_Buggy")) //&& choice.Outcome.Units[0].name.Contains("KS_Kaos_Buggy")))
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
                                    && !choice.Outcome.Items[0].ItemDef.name.Contains("GroundVehicle"))
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
                PropertyInfo propertyInfo = typeof(GeoMarketplace).GetProperty("MarketplaceChoices", BindingFlags.Instance | BindingFlags.Public);

                if (MPGeoEventChoices == null)
                {
                    //  TFTVLogger.Always($"saving all Choices to internal list, count is {geoMarketplace.MarketplaceChoices.Count}");
                    MPGeoEventChoices = geoMarketplace.MarketplaceChoices;
                }
                else
                {
                    //  TFTVLogger.Always($"passing all Choices from internal list, count {MPGeoEventChoices.Count}, to proper list, count {geoMarketplace.MarketplaceChoices.Count}");
                    propertyInfo?.SetValue(geoMarketplace, MPGeoEventChoices);

                }

                List<GeoEventChoice> choicesToShow = new List<GeoEventChoice>();

                if (filter != 0)
                {
                    if (filter == 1)
                    {
                        //  TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                        for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                        {
                            if (CheckIfMarketChoiceVehicle(geoMarketplace.MarketplaceChoices[i]))
                            {
                                // TFTVLogger.Always($"the vehicle equipment choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                            }
                        }

                        propertyInfo.SetValue(geoMarketplace, choicesToShow);

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

                    propertyInfo.SetValue(geoMarketplace, choicesToShow);
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
                FieldInfo fieldInfo = typeof(VirtualScrollRect).GetField("_visibleElements", BindingFlags.NonPublic | BindingFlags.Instance);

                //   TFTVLogger.Always($"Checking before filtering: visible elements {fieldInfo.GetValue(marketplaceUI.ListScrollRect)}");

                switch (filter)
                {
                    case 0:

                        marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_All.jpg");
                        break;

                    case 1:

                        marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("Encounter_4_Kaos_Buggy_uinomipmaps.jpg");
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

                int visibleElements = (int)fieldInfo.GetValue(marketplaceUI.ListScrollRect);
                int selectionChoices = geoMarketplace.MarketplaceChoices.Count();

                //  TFTVLogger.Always($"Checking after filtering: {visibleElements} visible elements  vs {selectionChoices} elements in selection");

                if (visibleElements > selectionChoices)
                {
                    fieldInfo.SetValue(marketplaceUI.ListScrollRect, selectionChoices);
                }
                else if (selectionChoices > visibleElements && visibleElements < 7)
                {
                    fieldInfo.SetValue(marketplaceUI.ListScrollRect, Math.Min(selectionChoices, 7));

                }

                //   TFTVLogger.Always($"Checking after filtering and after manually setting the field: : {visibleElements} visible elements  vs {selectionChoices} elements in selection");

                marketplaceUI.ListScrollRect.RefreshContents(true);

                //   TFTVLogger.Always($"Checking after refreshing contents:: {visibleElements} visible elements  vs {selectionChoices} elements in selection");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ForceMarketPlaceUpdate()
        {

            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                GeoMarketplace geoMarketplace = controller.Marketplace;
                MethodInfo updateOptionsMethod = typeof(GeoMarketplace).GetMethod("UpdateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

                updateOptionsMethod.Invoke(geoMarketplace, null);
                TFTVLogger.Always($"Forced Marketplace options update");


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }







        }


      

        private static GeoEventChoice GenerateResearchChoice(ResearchDef researchDef, float price)
        {
            GeoEventChoice geoEventChoice = GenerateChoice(price);
            geoEventChoice.Outcome.GiveResearches.Add(researchDef.Id);
            geoEventChoice.Text = researchDef.ViewElementDef?.ResearchName;
            return geoEventChoice;
        }

        private static GeoEventChoice GenerateItemChoice(ItemDef itemDef, float price)
        {
            GeoEventChoice geoEventChoice = GenerateChoice(price);
            GroundVehicleItemDef groundVehicleItemDef;
            if ((object)(groundVehicleItemDef = itemDef as GroundVehicleItemDef) != null)
            {
                geoEventChoice.Outcome.Units.Add(groundVehicleItemDef.VehicleTemplateDef);
            }
            else
            {
                geoEventChoice.Outcome.Items.Add(new ItemUnit(itemDef, 1));
            }

            geoEventChoice.Text = itemDef.GetDisplayName();
            return geoEventChoice;
        }

        private static GeoEventChoice GenerateChoice(float price)
        {
            GeoEventChoice geoEventChoice = new GeoEventChoice();
            geoEventChoice.Requirments = new GeoEventChoiceRequirements();
            geoEventChoice.Outcome = new GeoEventChoiceOutcome();
            geoEventChoice.Requirments.Resources.Add(new ResourceUnit(ResourceType.Materials, price));
            geoEventChoice.Outcome.ReEneableEvent = true;
            return geoEventChoice;
        }



        private static GeoEventChoice GenerateRandomChoiceTFTV(GeoLevelController controller, List<GeoMarketplaceOptionDef> currentlyPossibleOptions, TheMarketplaceSettingsDef settings, List <ResearchDef> researchOffers, List <ItemDef> ammoOffers, float priceMultiplierVO19)
        {
            try
            {

                bool flag = false;
                GeoEventChoice result = null;

               // float ammoCostMultiplier = 2.5f;
               
                GeoMarketplaceOptionDef geoMarketplaceOptionDef;
                do
                {
                    geoMarketplaceOptionDef = currentlyPossibleOptions[UnityEngine.Random.Range(0, currentlyPossibleOptions.Count)];
                                  
                    GeoMarketplaceOptionDef geoMarketplaceOptionDef2 = geoMarketplaceOptionDef;
                    if (geoMarketplaceOptionDef2 is null)
                    {
                        continue;
                    }

                    float price = UnityEngine.Random.Range(geoMarketplaceOptionDef.MinPrice, geoMarketplaceOptionDef.MaxPrice);

                    


                    GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef;
                    if ((geoMarketplaceItemOptionDef = geoMarketplaceOptionDef2 as GeoMarketplaceItemOptionDef) is null)
                    {
                        GeoMarketplaceResearchOptionDef geoMarketplaceResearchOptionDef;
                        if ((geoMarketplaceResearchOptionDef = geoMarketplaceOptionDef2 as GeoMarketplaceResearchOptionDef) is object)
                        {
                            ResearchDef research = geoMarketplaceResearchOptionDef.GetResearch();
                            if (!(research == null) && !researchOffers.Contains(research))
                            {
                                researchOffers.Add(research);
                                result = GenerateResearchChoice(research, price * priceMultiplierVO19);
                                flag = true;
                            }
                        }
                    }
                    else
                    {
                        GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef2 = geoMarketplaceItemOptionDef;

                        ItemDef item = geoMarketplaceItemOptionDef2.ItemDef;


                        if (item.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                        {
                            if (ammoOffers.Contains(item)) 
                            {
                                price *= 2f;
                                result = GenerateItemChoice(item, price * priceMultiplierVO19);
                                

                            }
                            else 
                            {
                                ammoOffers.Add(item);
                                result = GenerateItemChoice(item, price * priceMultiplierVO19);


                            }


                        }
                        else if (item.name.Contains("SY")|| item.name.Contains("NJ")) 
                        {
                            if (controller != null && controller.NewJerichoFaction != null && controller.SynedrionFaction != null)
                            {
                                
                                bool armadilloResearched = CheckResearchCompleted(controller.NewJerichoFaction, "NJ_VehicleTech_ResearchDef");
                                bool aspidaResearched = CheckResearchCompleted(controller.SynedrionFaction, "SYN_Rover_ResearchDef");

                                if((item.name.Contains("SY") && !aspidaResearched)||(item.name.Contains("NJ") && !armadilloResearched)) 
                                {
                                    continue;                               
                                }
                                
                            }


                            result = GenerateItemChoice(item, price * priceMultiplierVO19);


                        }
                        else
                        {
                            if (ammoOffers.Count() < 5) 
                            {
                                continue;
                            
                            }

                            result = GenerateItemChoice(item, price * priceMultiplierVO19);
                        }
                        flag = true;
                    }
                }
                while (!flag);
                if (geoMarketplaceOptionDef.DisallowDuplicates)
                {
                    currentlyPossibleOptions.Remove(geoMarketplaceOptionDef);
                }

                return result;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

            

        


        [HarmonyPatch(typeof(GeoMarketplace), "OnSiteVisited")]
        public static class GeoMarketplace_OnSiteVisited_MarketPlace_patch
        {
            /*     public static bool Prepare()
                 {
                     TFTVConfig config = TFTVMain.Main.Config;
                     return config.ActivateKERework;
                 }*/

            public static void Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings)
            {
                try
                {
                    if (____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) == 0)
                    {
                        ____level.EventSystem.SetVariable(____settings.NumberOfDLC5MissionsCompletedVariable, 4);
                        ____level.EventSystem.SetVariable(____settings.DLC5IntroCompletedVariable, 1);
                        ____level.EventSystem.SetVariable(____settings.DLC5FinalMovieCompletedVariable, 1);
                        __instance.UpdateOptions(____level.Timing);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
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

                if (SecretMPCounter >= 43)
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

