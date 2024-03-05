using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using Newtonsoft.Json.Serialization;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
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

                    if (phoenixFaction.Research.InProgress.Contains(researchElement))
                    {
                        TFTVLogger.Always($"{researchElement.GetLocalizedName()} in progress will get canceled because the last Pandoran of the required type has been killed");

                        phoenixFaction.Research.Cancel(researchElement);
                        researchElement.State = ResearchState.Revealed;
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
