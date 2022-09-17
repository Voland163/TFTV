using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVAugmentations
    {
        public static void ApplyChanges()
        {
            try
            {
                //ID all the factions for later
                GeoFactionDef phoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef newJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                //Anu pissed at player for doing Bionics
                GeoscapeEventDef anuPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed1", "ANU_PISSED_BIONICS_TITLE", "ANU_PISSED_BIONICS_TEXT_GENERAL_0", "ANU_PISSED_BIONICS_CHOICE_0_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_PISSED_BIONICS_CHOICE_0";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_1", "ANU_PISSED_BIONICS_CHOICE_1_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_2", "ANU_PISSED_BIONICS_CHOICE_2_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_Anu_Pissed_Made_Promise", 1, true));


                //Anu really pissed at player for doing Bionics
                GeoscapeEventDef anuReallyPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed2", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_TEXT_GENERAL_0", "ANU_PISSED_BIONICS_CHOICE_0_OUTCOME");
                anuReallyPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_REALLY_PISSED_BIONICS_CHOICE_0";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -6));

                //NJ pissed at player for doing Mutations
                GeoscapeEventDef nJPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed1", "NJ_PISSED_MUTATIONS_TITLE", "NJ_PISSED_MUTATIONS_TEXT_GENERAL_0", "NJ_PISSED_MUTATIONS_CHOICE_0_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_PISSED_MUTATIONS_CHOICE_0";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -5));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_1", "NJ_PISSED_MUTATIONS_CHOICE_1_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -8));
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_2", "NJ_PISSED_MUTATIONS_CHOICE_2_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_NJ_Pissed_Made_Promise", 1, true));

                //NJ really pissed at player for doing Mutations
                GeoscapeEventDef nJReallyPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed2", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_TEXT_GENERAL_0", "NJ_PISSED_MUTATIONS_CHOICE_0_OUTCOME");
                nJReallyPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -6));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static readonly DefRepository Repo = TFTVMain.Repo;

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        public static class PhoenixStatisticsManager_UpdateGeoscapeStats_AnuPissedAtBionics_Patch
        {
            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                    int bionics = 0;
                    GeoLevelController geoLevelController = __instance.GeoLevel;
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance, geoLevelController.ViewerFaction);
                    
                    //check number of bionics player has
                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                    foreach (GeoCharacter geoCharacter in __instance.GeoLevel.PhoenixFaction.Soldiers)
                    {
                        foreach (GeoItem bionic in geoCharacter.ArmourItems)
                        {
                            if (bionic.ItemDef.Tags.Contains(bionicalTag))

                                bionics += 1;
                        }
                    }
                    if (bionics > 6 && geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Over_Bionics") == 0
                        && CheckForFacility(__instance.GeoLevel, "KEY_BASE_FACILITY_BIONICSLAB_NAME"))
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed1", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_Anu_Pissed_Over_Bionics", 1);
                    }

                    if (geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Broke_Promise") == 1
                       && geoLevelController.EventSystem.GetVariable("BG_Anu_Really_Pissed_Over_Bionics") == 0)
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed2", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_Anu_Really_Pissed_Over_Bionics", 1);
                        DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_BIONICSLAB_NAME", __instance.GeoLevel);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        public static class PhoenixStatisticsManager_UpdateGeoscapeStats_NJPissedAtMutations_Patch
        {
            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                    int mutations = 0;
                    GeoLevelController geoLevelController = __instance.GeoLevel;
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance, geoLevelController.ViewerFaction);
                 
                    //check number of mutations player has
                    GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                    foreach (GeoCharacter geoCharacter in __instance.GeoLevel.PhoenixFaction.Soldiers)
                    {
                        foreach (GeoItem mutation in geoCharacter.ArmourItems)
                        {
                            if (mutation.ItemDef.Tags.Contains(mutationTag))
                                mutations += 1;
                        }
                    }
                    if (mutations > 6 && geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Over_Mutations") == 0
                        && CheckForFacility(__instance.GeoLevel, "KEY_BASE_FACILITY_MUTATION_LAB_NAME"))
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed1", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_NJ_Pissed_Over_Mutations", 1);
                    }
                    if (geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Broke_Promise") == 1
                       && geoLevelController.EventSystem.GetVariable("BG_NJ_Really_Pissed_Over_Mutations") == 0)
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed2", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_NJ_Really_Pissed_Over_Mutations", 1);
                        DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_MUTATION_LAB_NAME", __instance.GeoLevel);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        //Used for triggering NJ Pissed events 
        [HarmonyPatch(typeof(UIModuleMutationSection), "ApplyMutation")]
        public static class UIModuleMutationSection_ApplyMutation_PissedEvents_patch
        {
            private static readonly DefRepository Repo = TFTVMain.Repo;
            private static readonly GeoFactionDef newJerico = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));

            public static void Postfix(IAugmentationUIModule ____parentModule)
            {
                try
                {
                  
                    //check if player made promise to New Jericho not to apply more mutations
                    if (____parentModule.Context.Level.EventSystem.GetVariable("BG_NJ_Pissed_Made_Promise") == 1 && ____parentModule.CurrentCharacter.OriginalFactionDef == newJerico)
                    {
                        ____parentModule.Context.Level.EventSystem.SetVariable("BG_NJ_Pissed_Broke_Promise", 1);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Used for triggering Anu pissed events
        [HarmonyPatch(typeof(UIModuleBionics), "OnAugmentApplied")]
        public static class UIModuleBionics_OnAugmentApplied_SetStaminaTo0_patch
        {
            public static void Postfix(UIModuleBionics __instance)
            {
                try
                {
                    //check if player made promise to Anu not to apply more bionics
                    if (__instance.Context.Level.EventSystem.GetVariable("BG_Anu_Pissed_Made_Promise") == 1)
                    {
                        __instance.Context.Level.EventSystem.SetVariable("BG_Anu_Pissed_Broke_Promise", 1);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static bool CheckForFacility(GeoLevelController level, string facilityName)
        {
            try
            {
                List<GeoPhoenixBase> phoenixBases = level.PhoenixFaction.Bases.ToList();

                foreach (GeoPhoenixBase pxBase in phoenixBases)
                {

                    List<GeoPhoenixFacility> facilities = pxBase.Layout.Facilities.ToList();

                    foreach (GeoPhoenixFacility facility in facilities)
                    {
                        if (facility.ViewElementDef.DisplayName1.LocalizationKey == facilityName)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void DestroyFacilitiesOnPXBases(string nameOfFacility, GeoLevelController level)
        {
            try
            {
                List<GeoPhoenixBase> phoenixBases = level.PhoenixFaction.Bases.ToList();

                foreach (GeoPhoenixBase pxBase in phoenixBases)
                {

                    List<GeoPhoenixFacility> facilities = pxBase.Layout.Facilities.ToList();

                    foreach (GeoPhoenixFacility facility in facilities)
                    {
                        if (facility.ViewElementDef.DisplayName1.LocalizationKey == nameOfFacility)

                        {
                            facility.DestroyFacility();

                        }

                    }
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        /*
        public static void AttackPhoenixBase(GeoLevelController level)
        {
            try
            {
                /*

                List<GeoPhoenixFacility> list = GeoVehicle.CurrentSite.GetComponent<GeoPhoenixBase>().Layout.Facilities.Where((GeoPhoenixFacility z) => z.HealthPercentage > 0f).ToList();
                if (list.Any())
                {
                    GeoPhoenixFacility randomElement = list.GetRandomElement();
                    int damagePercent = _raidManager.RaidsSetup.LargeFlyeirDamage;
                    if ((GeoVehicle.AircraftType & AircraftType.Small) != 0)
                    {
                        damagePercent = _raidManager.RaidsSetup.SmallFlyerDamage;
                    }
                    else if ((GeoVehicle.AircraftType & AircraftType.Medium) != 0)
                    {
                        damagePercent = _raidManager.RaidsSetup.MediumFlierDamage;
                    }

                    randomElement.DamageFacility(damagePercent);
                }

                */


        /*
                List<GeoSite> phoenixBases = level.PhoenixFaction.Sites.ToList();
                TimeUnit timeUnit = TimeUnit.FromHours(6);

                foreach (GeoSite site in phoenixBases)
                {
                    if (site.Type == GeoSiteType.PhoenixBase)
                    {
                        foreach (GeoHaven haven in level.SynedrionFaction.Havens)
                        {
                            //  if (Vector3.Distance(site.WorldPosition, haven.Site.WorldPosition) < 1)
                            //  {
                            foreach (GeoVehicle vehicle in level.SynedrionFaction.Vehicles.Where(vehicle => vehicle.CurrentSite.Type == GeoSiteType.Haven))

                            {
                                level.SynedrionFaction.ScheduleAttackOnSite(site, timeUnit);
                                level.SynedrionFaction.AttackPhoenixBaseFromVehicle(vehicle, site);
                            }

                            //                            }
                        }


                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }*/

    }



}

