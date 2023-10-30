using Base;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFTV
{
    internal class TFTVVanillaFixes
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        /// <summary>
        /// Not strictly a bug, but once partial magazines become visible, without this patch they can be scrapped for the same price as a full one.
        /// This patch ensures that scrapping ammo is never profitable.
        /// </summary>

        [HarmonyPatch(typeof(ItemDef), "get_ScrapPrice")]
        public static class ItemDef_get_ScrapPrice_patch
        {
            public static void Postfix(ItemDef __instance, ref ResourcePack __result, ResourcePack ____scrapPrice)
            {
                try
                {

                    if (__instance.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                    {
                        TacticalItemDef tacticalItemDef = __instance as TacticalItemDef;

                        WeaponDef weaponDef = (WeaponDef)AmmoWeaponDatabase.AmmoToWeaponDictionary[tacticalItemDef][0];

                        float costMultiplier = Math.Max(weaponDef.DamagePayload.AutoFireShotCount, 2);
                        __result = new ResourcePack(new ResourceUnit[]
                             {
                        new ResourceUnit(ResourceType.Tech, Mathf.Floor(__instance.ManufactureTech / costMultiplier)),
                        new ResourceUnit(ResourceType.Materials, Mathf.Floor(__instance.ManufactureMaterials / costMultiplier)),
                        new ResourceUnit(ResourceType.Mutagen, Mathf.Floor(__instance.ManufactureMutagen / costMultiplier)),
                        new ResourceUnit(ResourceType.LivingCrystals, Mathf.Floor(__instance.ManufactureLivingCrystals / costMultiplier)),
                        new ResourceUnit(ResourceType.Orichalcum, Mathf.Floor(__instance.ManufactureOricalcum / costMultiplier)),
                        new ResourceUnit(ResourceType.ProteanMutane, Mathf.Floor(__instance.ManufactureProteanMutane / costMultiplier))
                             });


                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        //Code provided by Codemite
        [HarmonyPatch(typeof(UIInventorySlot), "UpdateItem")]
        public static class UIInventorySlot_UpdateItem_patch
        {
            public static void Postfix(UIInventorySlot __instance, ICommonItem ____item)
            {
                try
                {
                    if (____item == null || ____item.CommonItemData.Count == 1 && (____item.CommonItemData.CurrentCharges == ____item.ItemDef.ChargesMax || ____item.CommonItemData.CurrentCharges == 0))
                    {
                        __instance.NumericBackground.gameObject.SetActive(false);
                    }
                    else
                    {
                        __instance.NumericBackground.gameObject.SetActive(true);

                        if (____item.CommonItemData.CurrentCharges == ____item.ItemDef.ChargesMax)
                        {
                            __instance.NumericField.text = ____item.CommonItemData.Count.ToString();
                        }
                        else
                        {
                            string ammoCount = $"{____item.CommonItemData.CurrentCharges}/{____item.ItemDef.ChargesMax}";
                            string textToShow;
                            string greyColor = "<color=#b6b6b6>";

                            if (____item.CommonItemData.Count - 1 == 0)
                            {
                                if (____item.ItemDef.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                                {
                                    textToShow = $"{greyColor}(1) {ammoCount}</color>";
                                }
                                else
                                {
                                    textToShow = $"{greyColor} {ammoCount}</color>";
                                }
                            }
                            else
                            {

                                textToShow = $"{____item.CommonItemData.Count - 1} {greyColor}+ {____item.CommonItemData.CurrentCharges}/{____item.ItemDef.ChargesMax}</color>";
                            }

                            __instance.NumericField.text = textToShow;
                            __instance.NumericField.alignment = TextAnchor.MiddleLeft;
                        }
                    }

                    // return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        /// <summary>
        /// Fixes money spent no purchase made at Marketplace if 2 or more aircraft at Marketplace
        /// </summary>
        [HarmonyPatch(typeof(GeoscapeEvent), "CompleteMarketplaceEvent")]
        public static class GeoscapeEvent_CompleteMarketplaceEvent_patch
        {

            public static bool Prefix(GeoscapeEvent __instance, GeoEventChoice choice, GeoFaction faction)
            {
                try
                {
                   // TFTVLogger.Always($"CompleteMarketplaceEvent triggered for choice {choice.Outcome.Items[0].ItemDef?.name}");

                    if (__instance.Context.Site.Vehicles.Count() > 1)
                    {
                        TFTVLogger.Always($"There is a more than one vehicle at {__instance.Context.Site.LocalizedSiteName}! Need to execute alternative code");

                        PropertyInfo propertyInfo = typeof(GeoscapeEvent).GetProperty("ChoiceReward", BindingFlags.Instance | BindingFlags.Public);




                        GeoLevelController component = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(component.PhoenixFaction.StartingBase, component.PhoenixFaction, __instance.Context.Site.Vehicles.First());
                        // TFTVLogger.Always($"geoscapeEventContext is null? {geoscapeEventContext==null} is faction null? {faction==null}");

                        propertyInfo?.SetValue(__instance, choice.Outcome.GenerateFactionReward(faction, geoscapeEventContext, __instance.EventID));

                        // TFTVLogger.Always($"got here. is propertyInfo null? {propertyInfo==null} is choiceReward null? {__instance.ChoiceReward==null}");

                        // __instance.ChoiceReward = choice.Outcome.GenerateFactionReward(faction, geoscapeEventContext, __instance.EventID);
                        __instance.ChoiceReward.Apply(faction, geoscapeEventContext.Site, geoscapeEventContext.Vehicle);
                        // TFTVLogger.Always($"got here2");

                        if (choice.Outcome.ReEneableEvent)
                        {
                            //   TFTVLogger.Always($"got here");
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



        //Factions attacking Phoenix bases fix
        //Method by Dimitar "Codemite" Evtimov from Snapshot Games
        public static void PatchInAllBaseDefenseDefs()
        {
            try
            {

                CustomMissionTypeDef alienDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                CustomMissionTypeDef anuDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAnu_CustomMissionTypeDef");
                CustomMissionTypeDef njDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseNJ_CustomMissionTypeDef");
                CustomMissionTypeDef syDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseSY_CustomMissionTypeDef");
                CustomMissionTypeDef infestationDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseInfestationAlien_CustomMissionTypeDef");

                TacMissionTypeDef[] defenseMissions = { alienDef, anuDef, njDef, syDef, infestationDef };

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded)
                        continue;

                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetTransformsInChildrenStable())
                        {
                            var objActivator = transform.GetComponent<TacMissionObjectActivator>();
                            if (objActivator && objActivator.Missions.Length == 1 && objActivator.Missions.Contains(alienDef))
                            {
                                objActivator.Missions = defenseMissions;
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(TacMission), "PrepareMissionActivators")]

        public static class TacMission_PrepareMissionActivators_Experiment_patch
        {
            public static void Prefix(TacMission __instance)
            {
                try
                {

                    TFTVLogger.Always("PrepareMissionActivators");
                    PatchInAllBaseDefenseDefs();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }


        //Strates fix for bloodlust
        [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
        class DamageAccumulation_GenerateStandardDamageTargetData_VanillaBugFix
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Div)
            };

                // insert after each of the first 3 divide opcodes
                int divs = 0;
                for (int index = 0; index < instructions.Count(); index++)
                {
                    if (listInstructions[index].opcode == OpCodes.Div)
                    {
                        listInstructions.InsertRange(index + 1, insert);
                        index += 2;
                        divs++;
                        if (divs == 3)
                        {
                            break;
                        }
                    }
                }

                if (divs != 3)
                {
                    return instructions; // didn't find three, function signature changed, abort
                }
                return listInstructions;
            }

        }


        //Reduce population by 1 when recruiting at havens

        [HarmonyPatch(typeof(GeoHaven), "TakeRecruit")]

        public static class TFTV_GeoHaven_TakeRecruit_VanillaBugBix_patch
        {
            public static void Postfix(GeoHaven __instance, IGeoCharacterContainer __result, ref int ____population)
            {
                try
                {
                    if (__result != null)
                    {
                        ____population -= 1;
                        HavenInfoController havenInfo = (HavenInfoController)UnityEngine.Object.FindObjectOfType(typeof(HavenInfoController));


                        int populationChange = __instance.GetPopulationChange(__instance.ZonesStats.GetTotalHavenOutput());
                        if (populationChange > 0)
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationPositiveTextPattern, __instance.Population.ToString(), populationChange);
                        }
                        else if (populationChange == 0)
                        {
                            havenInfo.PopulationValueText.text = __instance.Population.ToString();
                        }
                        else
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationNegativeTextPattern, __instance.Population.ToString(), populationChange);
                        }


                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Remove negative damage notices with very large numbers when character with elemental immunity hit by elemental damage
        [HarmonyPatch(typeof(HealthbarUIActorElement), "AddNotificationMessage")]
        public class HealthbarUIActorElement_AddNotificationMessage_VanillaBugFix_Patch
        {
            static bool Prefix(int? val = null)
            {
                try
                {
                    // Check if val is outside the specified range
                    if (val.HasValue && (val.Value > 1000000 || val.Value < -1000000))
                    {
                        //TFTVLogger.Always("it worked");
                        // Return false to cancel the original method call
                        return false;
                    }

                    // Return true to allow the original method call
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }


        //Ensure facilities are working after repairing Power Generator
        [HarmonyPatch(typeof(GeoPhoenixFacility), "SetFacilityFunctioning")]
        public static class GeoPhoenixFacility_SetFacilityFunctioning_AfterGenRepairedVanillaBugFix_Patch
        {
            public static void Postfix(GeoPhoenixFacility __instance)
            {
                try
                {

                    //  TFTVLogger.Always($"SetFacilityFunctioning {__instance.ViewElementDef.name}");

                    if (__instance.GetComponent<PowerFacilityComponent>() != null)
                    {
                        CheckFacilitesNotWorking(__instance.PxBase);
                        //  __instance.PxBase.RoutePower();
                    }

                    //

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        public static void CheckFacilitesNotWorking(GeoPhoenixBase phoenixBase)
        {
            try
            {


                foreach (GeoPhoenixFacility baseFacility in phoenixBase.Layout.Facilities)
                {

                    if (baseFacility.IsPowered)
                    {
                        baseFacility.SetPowered(false);
                        baseFacility.SetPowered(true);
                    }
                    // TFTVLogger.Always($"{baseFacility.ViewElementDef.name} at {phoenixBase.name} is working? {baseFacility.IsWorking}. is it powered? {baseFacility.IsPowered} ");



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
