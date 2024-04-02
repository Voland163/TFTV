using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using Newtonsoft.Json.Serialization;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVResearch
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;     

        internal class Vivisections
        {

            [HarmonyPatch(typeof(UIStateRosterAliens), "OnDismantleForMutagens")]
            public static class TFTV_UIStateRosterAliens_OnDismantleForMutagens_Patch
            {
                public static bool Prefix(UIStateRosterAliens __instance)
                {
                    try
                    {
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        UIModuleActorCycle actorCycleModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;

                        GeoUnitDescriptor unit = actorCycleModule.GetCurrent<GeoUnitDescriptor>();

                        if (IsPandoranVivisectionRelevant(unit, phoenixFaction) && CountPandoranType(unit, phoenixFaction) == 1)
                        {
                            string warningText = new LocalizedTextBind() { LocalizationKey = "KEY_VIVISECTION_WARNING" }.Localize();

                            // Show the message box with options
                            GameUtl.GetMessageBox().ShowSimplePrompt(warningText, MessageBoxIcon.Warning, MessageBoxButtons.YesNo, (msgResult) =>
                            {
                                // Invoke the callback method with the chosen result
                                MethodInfo methodInfo = typeof(UIStateRosterAliens).GetMethod("OnDismantpleForMutagensDialogCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                                methodInfo.Invoke(__instance, new object[] { msgResult });

                            });

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

           


            [HarmonyPatch(typeof(UIStateRosterAliens), "OnSlotKillAlien")]
            public static class TFTV_UIStateRosterAliens_OnSlotKillAlien_Patch
            {
               
                public static bool Prefix(UIStateRosterAliens __instance, GeoRosterAlienContainmentItem item, ref GeoRosterAlienContainmentItem ____dismissedSlot)
                {
                    try
                    {
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        if(IsPandoranVivisectionRelevant(item.Unit, phoenixFaction) && CountPandoranType(item.Unit, phoenixFaction) == 1) 
                        {
                            ____dismissedSlot = item;
                            GenerateWarningMessagePandoranVivisection(__instance);

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

            public static void GenerateWarningMessagePandoranVivisection(UIStateRosterAliens uIStateRosterAliens)
            {
                try
                {
                    // Define the message box content
                    string warningText = new LocalizedTextBind() { LocalizationKey = "KEY_VIVISECTION_WARNING" }.Localize();

                    // Show the message box with options
                    GameUtl.GetMessageBox().ShowSimplePrompt(warningText, MessageBoxIcon.Warning, MessageBoxButtons.YesNo, (msgResult) =>
                    {
                        // Invoke the callback method with the chosen result
                        MethodInfo methodInfo = typeof(UIStateRosterAliens).GetMethod("OnKillAlienDialogCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                        methodInfo.Invoke(uIStateRosterAliens, new object[] { msgResult });
                    });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            /*   public static void GenerateWarningMessagePandoranVivisection(UIStateRosterAliens uIStateRosterAliens)
               {
                   try
                   {
                       MethodInfo methodInfo = typeof(UIStateRosterAliens).GetMethod("OnKillAlienDialogCallback", BindingFlags.Instance | BindingFlags.NonPublic);

                       string warningText = new LocalizedTextBind() { LocalizationKey = "KEY_VIVISECTION_WARNING" }.Localize();

                           GameUtl.GetMessageBox().ShowSimplePrompt(warningText, MessageBoxIcon.Warning, MessageBoxButtons.YesNo, (MessageBox.MessageBoxCallback)methodInfo.Invoke(uIStateRosterAliens, new object[] {}));


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }*/


            public static bool IsPandoranVivisectionRelevant(GeoUnitDescriptor geoUnitDescriptor, GeoPhoenixFaction phoenixFaction)
            {
                try
                {
                    return phoenixFaction.Research.FactionResearches.Any
                        (re => !re.IsCompleted
                        && re.ResearchDef.UnlockRequirements.Container.Count() > 0
                        && re.ResearchDef.UnlockRequirements.Container[0].Requirements.Count() > 0
                        && re.ResearchDef.UnlockRequirements.Container[0].Requirements.Any(
                            req => req is CaptureActorResearchRequirementDef actorResearchRequirementDef
                            && actorResearchRequirementDef.Actor == geoUnitDescriptor.UnitType.TemplateDef.ComponentSetDef.Components.FirstOrDefault
                            (c => c is TacticalActorDef)));

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static int CountPandoranType(GeoUnitDescriptor geoUnitDescriptor, GeoPhoenixFaction phoenixFaction)
            {
                try
                {
                    return phoenixFaction.CapturedUnits.Where(
                            cu => cu.UnitType.TemplateDef.ComponentSetDef.Components.FirstOrDefault
                            (c => c == geoUnitDescriptor.UnitType.TemplateDef.ComponentSetDef.Components.FirstOrDefault(c2 => c2 is TacticalActorDef))).Count();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void ResetResearch(GeoUnitDescriptor geoUnitDescriptor, GeoPhoenixFaction phoenixFaction)
            {
                try
                {
                    if (!IsPandoranVivisectionRelevant(geoUnitDescriptor, phoenixFaction) || CountPandoranType(geoUnitDescriptor, phoenixFaction) > 0)
                    {
                        return;
                    }

                    ResearchElement researchElement = phoenixFaction.Research.FactionResearches.FirstOrDefault
                        (re => !re.IsCompleted
                        && re.ResearchDef.UnlockRequirements.Container.Count() > 0
                        && re.ResearchDef.UnlockRequirements.Container[0].Requirements.Count() > 0
                        && re.ResearchDef.UnlockRequirements.Container[0].Requirements.Any(
                            req => req is CaptureActorResearchRequirementDef actorResearchRequirementDef
                            && actorResearchRequirementDef.Actor == geoUnitDescriptor.UnitType.TemplateDef.ComponentSetDef.Components.FirstOrDefault
                            (c => c is TacticalActorDef)));

                    TFTVLogger.Always($"Killing {geoUnitDescriptor.UnitType.TemplateDef.name}, relevant for {researchElement.ResearchID} {researchElement.State}");

                    if (phoenixFaction.Research.InProgress.Contains(researchElement))
                    {
                        TFTVLogger.Always($"{researchElement.GetLocalizedName()} in progress will get canceled because the last Pandoran of the required type has been killed");

                        phoenixFaction.Research.Cancel(researchElement);
                        researchElement.State = ResearchState.Revealed;
                    }
                    else if (researchElement.State==ResearchState.Unlocked) 
                    {
                        TFTVLogger.Always($"unlocked {researchElement.GetLocalizedName()} will get locked because the last Pandoran of the required type has been killed");
                        researchElement.State = ResearchState.Revealed;
                    }

                    TFTVLogger.Always($"{researchElement.State} {researchElement.GetLocalizedName()} research will get requirement reset because the last Pandoran of the required type has been killed");
                    foreach (ResearchRequirement item in researchElement.GetUnlockRequirements())
                    {
                        TFTVLogger.Always($"{item.RequirementDef.name} item.Progress: {item.Progress}");
                        PropertyInfo fieldInfo = typeof(ResearchRequirement).GetProperty("IsCompleted", BindingFlags.Public | BindingFlags.Instance);
                        MethodInfo methodInfo = typeof(ResearchRequirement).GetMethod("UpdateProgress", BindingFlags.NonPublic | BindingFlags.Instance);
                        // fieldInfo.SetValue(item, 0);
                        fieldInfo.SetValue(item, false);
                        methodInfo.Invoke(item, new object[] { -1 });
                        TFTVLogger.Always($"new value: {item.RequirementDef.name} item.Progress: {item.Progress}");

                    }
                  
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }



        [HarmonyPatch(typeof(Research), "CompleteResearch")]
        public static class Research_CompleteResearch_TFTV_Patch
        {
            public static void Postfix(ResearchElement research)
            {

                try
                {
                    TFTVLogger.Always($"{research.ResearchID} completed by {research.Faction}");

                    GeoLevelController controller = research.Faction.GeoLevel;
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                    ResearchElement mutationTechResearchElement = controller.PhoenixFaction.Research.GetResearchById(mutationTech.name);

                    if (research.ResearchID == "ALN_CrabmanUmbra_ResearchDef")
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("UmbraResearched", 1);
                        TFTVLogger.Always("Umbra Researched variable is set to " + research.Faction.GeoLevel.EventSystem.GetVariable("UmbraResearched"));
                    }
                    else if (research.Faction != research.Faction.GeoLevel.PhoenixFaction && research.ResearchID == "ANU_AnuPriest_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 1)
                    {

                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        phoenixFaction.Research.CompleteResearch(phoenixResearch);
                    }

                    else if (research.Faction != phoenixFaction && research.ResearchID == "NJ_Technician_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 2)
                    {
                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        controller.PhoenixFaction.Research.CompleteResearch(phoenixResearch);
                    }
                    else if (research.Faction != phoenixFaction && research.ResearchID == "SYN_InfiltratorTech_ResearchDef" && controller.EventSystem.GetVariable("BG_Start_Faction") == 3)
                    {

                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        phoenixFaction.Research.CompleteResearch(phoenixResearch);


                    }
                    //To trigger change of rate in Pandoran Evolution
                    else if (research.ResearchID == "ALN_Citadel_ResearchDef")
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("Pandorans_Researched_Citadel", 1);
                        research.Faction.GeoLevel.AlienFaction.SpawnNewAlienBase();
                        GeoAlienBase citadel = research.Faction.GeoLevel.AlienFaction.Bases.FirstOrDefault(ab => ab.AlienBaseTypeDef.name == "Citadel_GeoAlienBaseTypeDef");
                        ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                        TacCharacterDef startingScylla = DefCache.GetDef<TacCharacterDef>("Scylla1_FrenzyMistSmasherAgileSpawner_AlienMutationVariationDef");

                        citadel.SpawnMonster(queenTag, startingScylla);

                    }
                    else if (research.ResearchID == "PX_VirophageWeapons_ResearchDef")
                    {
                        if (controller.EventSystem.GetVariable("SymesAlternativeCompleted") == 1)
                        {
                            GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                            research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Virophage", context);

                        }
                    }


                    else if (research.ResearchID == "PX_YuggothianEntity_ResearchDef")
                    {

                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("AlistairOnMessagesFromTheVoid", context);

                    }
                    else if (research.ResearchID == "PX_AntediluvianArchaeology_ResearchDef")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Echoes", context);
                    }
                    else if (research.ResearchID == "AncientAutomataResearch")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Olena_Styx", context);

                        //  ResearchElement exoticMaterialsResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("ExoticMaterialsResearch");
                        //  research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, exoticMaterialsResearch);
                    }

                    else if (research.ResearchID == "PX_LivingCrystalResearchDef")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Oneiromancy", context);
                        // GeoscapeEventSystem eventSystem = research.Faction.GeoLevel.EventSystem;
                        // eventSystem.SetVariable("ProteanMutaneResearched", eventSystem.GetVariable("ProteanMutaneResearched") + 1);
                        TFTVAncientsGeo.DefendCyclopsStoryMission.SetReactivateCyclopsObjective(controller);
                    }
                    else if (research.ResearchID == "ExoticMaterialsResearch")
                    {
                        TFTVAncientsGeo.AncientsResearch.AncientsCheckResearchState(research.Faction.GeoLevel);
                        TFTVAncientsGeo.AncientsResearch.SetObtainLCandPMSamplesObjective(controller);

                        //   ResearchElement livingCrystalsResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("PX_LivingCrystalResearchDef");
                        //   GeoFactionObjective researchLC = research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, livingCrystalsResearch);
                        //  controller.PhoenixFaction.AddObjective(researchLC);
                        //  ResearchElement proteanMutaneResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("PX_ProteanMutaneResearchDef");
                        //  GeoFactionObjective researchPM = research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, proteanMutaneResearch);
                        //  controller.PhoenixFaction.AddObjective(researchPM);
                    }
                    else if (research.ResearchID == "PX_ProteanMutaneResearchDef")
                    {
                        GeoscapeEventSystem eventSystem = controller.EventSystem;
                        //  eventSystem.SetVariable("ProteanMutaneResearched", eventSystem.GetVariable("ProteanMutaneResearched") + 1);
                        TFTVAncientsGeo.DefendCyclopsStoryMission.SetReactivateCyclopsObjective(controller);
                    }

                    /*  else if (research.ResearchID == "NJ_Bionics2_ResearchDef")
                      {

                          ResearchElement bionics3 = controller.SynedrionFaction.Research.GetResearchById("SYN_Bionics3_ResearchDef");
                          controller.SynedrionFaction.Research.GiveResearch(bionics3);
                          controller.SynedrionFaction.Research.CompleteResearch(bionics3);
                          //controller.SynedrionFaction.Research.FactionResearches.AddItem(research);
                          //controller.SynedrionFaction.Research.AddProgressToResearch(research, 700);

                      }*/

                    else if (research.ResearchID == "PX_Mutoid_ResearchDef" && !controller.PhoenixFaction.Research.HasCompleted(mutationTech.name) &&
                   !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == mutationTech))
                    {

                        mutationTechResearchElement.State = ResearchState.Unlocked;
                        TFTVLogger.Always($"{mutationTech.name} available to PX? {mutationTechResearchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                    }


                    FactionFunctionalityTagDef alienContainmentFunctionality = DefCache.GetDef<FactionFunctionalityTagDef>("AlienContainment_FactionFunctionalityTagDef");

                    if (research.ResearchID == "PX_Alien_Acheron_ResearchDef" && controller.PhoenixFaction.GameTags.Contains(alienContainmentFunctionality)
                        && controller.EventSystem.GetEventRecord("PROG_CH0") == null)
                    {


                        TFTVLogger.Always($"Built containment facility and has completed PX_Alien_Acheron_ResearchDef, triggering CH0");

                        //     controller.EventSystem.SetVariable("FavorForAFriend", 1);


                        GeoscapeEventContext context = new GeoscapeEventContext(controller.PhoenixFaction.Bases.First().Site, controller.AlienFaction);
                        controller.EventSystem.TriggerGeoscapeEvent("PROG_CH0", context);

                    }

                    // TFTVChangesToDLC1andDLC2Events.CheckTriggerPU5(controller, research);

                    TFTVCapturePandoransGeoscape.RefreshFoodAndMutagenProductionTooltupUI();
                    //   TFTVAncientsGeo.ImpossibleWeapons.CheckImpossibleWeaponsAdditionalRequirements(controller);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
