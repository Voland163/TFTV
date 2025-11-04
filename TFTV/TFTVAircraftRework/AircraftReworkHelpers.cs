using Base.Core;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TFTV.AircraftReworkGeoscape;
using static TFTV.TFTVAircraftReworkMain;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;


namespace TFTV
{

        internal class AircraftReworkHelpers
        {

        internal static int GetAdjustedPassengerManifestAircraftRework(GeoVehicle geoVehicle)
        {
            try
            {
                bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                bool isHelios = geoVehicle.VehicleDef == helios;
                bool aspidaResearchCompleted = HasCompletedResearch(geoVehicle, _aspidaResearchDef);

                List<GeoCharacter> geoCharacters = geoVehicle.Units.ToList();

                int occupiedSpace = 0;

                foreach (GeoCharacter geoCharacter in geoCharacters)
                {
                    occupiedSpace += VehiclesAndMutogs.GetAdjustedCharacterSpace(geoCharacter, hasHarness, hasMutogPen,
                        isThunderbird, isHelios, aspidaResearchCompleted);

                }

               /* if (occupiedSpace >= geoVehicle.MaxCharacterSpace)
                {
                    List<GeoCharacter> list = new List<GeoCharacter>(from u in geoVehicle.Units orderby u.OccupingSpace descending select u);
                    foreach (GeoCharacter character in list)
                    {
                        occupiedSpace -= VehiclesAndMutogs.GetAdjustedCharacterSpace(character, hasHarness, hasMutogPen,
                           isThunderbird, isHelios, aspidaResearchCompleted);
                    }

                }*/

                return occupiedSpace;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static int GetTier(ItemDef moduleDef)
        {
            try
            {
                int tier = 0;

                if (moduleDef == _blimpSpeedModule)
                {
                    tier = Tiers.GetBlimpSpeedTier();
                }
                else if (moduleDef == _blimpMutationLabModule)
                {
                    tier = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches);
                }
                else if (moduleDef == _blimpMistModule)
                {
                    tier = Tiers.GetMistModuleBuffLevel();
                }
                else if (moduleDef == _thunderbirdGroundAttackModule)
                {
                    tier = Tiers.GetGWABuffLevel();
                }
                else if (moduleDef == _heliosStealthModule)
                {
                    tier = Tiers.GetStealthTierForUI();
                }
                else if (moduleDef == _heliosPanaceaModule)
                {
                    tier = Tiers.GetBuffLevelFromResearchDefs(_heliosStatisChamberBuffResearchDefs);
                }
                else if (moduleDef == _thunderbirdWorkshopModule)
                {
                    tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs);
                }
                else if (moduleDef == _thunderbirdScannerModule)
                {
                    tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdScannerBuffResearchDefs);
                }
                else if (moduleDef == _thunderbirdRangeModule)
                {
                    tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdRangeBuffResearchDefs);
                }


                return tier;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static Sprite GetTierSprite(ItemDef moduleDef)
        {
            try
            {
                Sprite sprite = moduleDef.ViewElementDef.SmallIcon;

                if (moduleDef == _blimpSpeedModule)
                {
                    int tier = Tiers.GetBlimpSpeedTier();

                    if (tier > 1)
                    {
                        //TFTVLogger.Always($"adjusting BlimpSpeed module picture in GetSmallIcon");
                        sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_Speed_Small{tier}.png");
                    }
                }
                else if (moduleDef == _blimpMutationLabModule)
                {
                    int tier = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches);

                    if (tier > 1)
                    {
                        sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_MutationLab_Small{tier}.png");
                    }
                }
                else if (moduleDef == _blimpMistModule)
                {
                    int tier = Tiers.GetMistModuleBuffLevel();

                    if (tier > 1)
                    {
                        sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_WP_Small{tier}.png");
                    }

                }
                else if (moduleDef == _thunderbirdGroundAttackModule)
                {
                    int tier = Tiers.GetGWABuffLevel();

                    if (tier > 1)
                    {
                        sprite = Helper.CreateSpriteFromImageFile($"TFTV_Thunderbird_GroundAttack_Small{tier}.png");
                    }

                }

                return sprite;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(ItemDef), "GetDetailedImage")]
        public static class ItemDef_GetDetailedImage_Patch
        {
            public static void Postfix(ItemDef __instance, ref Sprite __result)
            {
                try
                {
                    // Your master toggle for this feature
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (__instance is GeoVehicleEquipmentDef)
                    {
                        __result = GetTierSprite(__instance);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(ItemDef), "GetSmallIcon")]
        public static class ItemDef_GetSmallIcon_Patch
        {
            public static void Postfix(ItemDef __instance, ref Sprite __result)
            {
                try
                {
                    // Your master toggle for this feature
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (__instance is GeoVehicleEquipmentDef)
                    {
                        __result = GetTierSprite(__instance);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static Research GetPhoenixResearch(GeoVehicle geoVehicle)
            {
                try
                {
                    GeoLevelController geoLevelController = geoVehicle?.GeoLevel;

                    if (geoLevelController == null)
                    {
                        Level currentLevel = GameUtl.CurrentLevel();
                        if (currentLevel != null)
                        {
                            geoLevelController = currentLevel.GetComponent<GeoLevelController>();
                        }
                    }

                    return geoLevelController?.PhoenixFaction?.Research;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return null;
                }
            }

            internal static bool HasCompletedResearch(GeoVehicle geoVehicle, ResearchDef researchDef)
            {
                try
                {
                    if (researchDef == null)
                    {
                        return false;
                    }

                    Research phoenixResearch = GetPhoenixResearch(geoVehicle);
                    return phoenixResearch != null && phoenixResearch.HasCompleted(researchDef.Id);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }
            }
            internal class Tiers
            {
                internal static int GetBlimpSpeedTier()
                {
                    try
                    {
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;
                        int speedBuff = 1;

                        foreach (ResearchDef researchDef in _blimpSpeedModuleBuffResearches)
                        {
                            if (phoenixResearch.HasCompleted(researchDef.Id))
                            {

                                speedBuff += 1;
                                //TFTVLogger.Always($"{researchDef.Id} completed, so adding {_mistSpeedModuleBuff} to speed. Current speedbuff for Bioflux is {speedBuff}", false);
                            }
                        }

                        return speedBuff;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                internal static int GetBuffLevelFromResearchDefs(List<ResearchDef> researchDefs)
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;


                        foreach (ResearchDef researchDef in researchDefs)
                        {
                            if (phoenixResearch.HasCompleted(researchDef.Id))
                            {
                                buffLevel++;
                            }
                        }

                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static int GetMistModuleBuffLevel()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                        if (phoenixResearch.HasCompleted("ANU_MutationTech3_ResearchDef"))
                        {
                            buffLevel = 3;
                        }
                        else if (phoenixResearch.HasCompleted("ANU_MutationTech2_ResearchDef"))
                        {
                            buffLevel = 2;
                        }

                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static int GetStealthTierForUI()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                        if (phoenixResearch.HasCompleted("SYN_SafeZoneProject_ResearchDef"))
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("SYN_InfiltratorTech_ResearchDef"))
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("SYN_NightVision_ResearchDef"))
                        {
                            buffLevel += 1;
                        }



                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }





                internal static int GetGWABuffLevel()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;


                        if (phoenixResearch.HasCompleted("NJ_GuidanceTech_ResearchDef")) // Advanced Missile Technology
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("NJ_ExplosiveTech_ResearchDef"))//Advanced Rocket Technology

                        {
                            buffLevel += 1;
                        }

                        return buffLevel;
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

