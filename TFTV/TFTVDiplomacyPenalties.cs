using Base.Core;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static bool VoidOmensImplemented = false;

        private static readonly List<string> ExcludedEventsDiplomacyPenalty = new List<string>
        {
            "PROG_PU4_WIN", "PROG_SY7", "PROG_SY8", "PROG_AN3", "PROG_AN5", "PROG_NJ7", "PROG_NJ8",
        "PROG_AN2", "PROG_NJ1", "PROG_SY1", "PROG_AN4", "PROG_NJ2", "PROG_SY3_WIN", "PROG_AN6", "PROG_NJ3", "PROG_SY4_T", "PROG_SY4_P",
        "PROG_PU2_CHOICE3EVENT","PROG_PU4_WIN","PROG_PU2_CHOICE2EVENT","PROG_PU12WIN2", "PROG_PU12NewNJOption","PROG_LE1_WIN",
        "Anu_Pissed1", "Anu_Pissed2", "NJ_Pissed1", "NJ_Pissed2", "PROG_LE0_WIN"
        };


        [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]

        public static class GeoEventChoiceOutcome_GenerateFactionReward_patch
        {
            private static readonly GeoFactionDef PhoenixFaction = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
 

          /*  public static bool Prepare()
            {
                GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));
                TFTVConfig config = TFTVMain.Main.Config;
                if (config.DiplomaticPenalties || config.ResourceMultiplier != 1 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(2) || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(8))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }*/

            public static void Prefix(GeoEventChoiceOutcome __instance, string eventID)
            {
                try
                {
                   // if (!VoidOmensImplemented)
                   // {
                        GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));
                      //  TFTVVoidOmens.ImplementVoidOmens(controller);
                      //  VoidOmensImplemented = true;
                   // }

                    TFTVConfig config = TFTVMain.Main.Config;
                  
                    if (config.DiplomaticPenalties)
                    {
                        if (!ExcludedEventsDiplomacyPenalty.Contains(eventID) && __instance.Diplomacy.Count > 0)
                        {
                            for (int i = 0; i < __instance.Diplomacy.Count; i++)
                            {
                                if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value <= 0)
                                {
                                    TFTVLogger.Always("Harder diplomacy. The event is " + eventID + ". Original diplo penalty is " + __instance.Diplomacy[i].Value +
                                        ". New diplomacy value is " + __instance.Diplomacy[i].Value * 2);
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                    diplomacyChange.Value *= 2;
                                    __instance.Diplomacy[i] = diplomacyChange;

                                }
                            }
                        }
                    }
                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(2))
                    {
                        if (__instance.Diplomacy.Count > 0)
                        {
                            for (int t = 0; t < __instance.Diplomacy.Count; t++)
                            {
                                if (__instance.Diplomacy[t].Value != 0)
                                {
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                    TFTVLogger.Always("VO#2. Original value was " + diplomacyChange.Value + ". New value is " + diplomacyChange.Value * 0.5f);
                                    diplomacyChange.Value = Mathf.CeilToInt(diplomacyChange.Value * 0.5f);
                                    __instance.Diplomacy[t] = diplomacyChange;

                                }
                            }
                        }
                    }
                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(8))
                    {
                        if (__instance.Diplomacy.Count > 0)
                        {
                            for (int t = 0; t < __instance.Diplomacy.Count; t++)
                            {
                                if (__instance.Diplomacy[t].Value <= 0 && __instance.Diplomacy[t].TargetFaction != PhoenixFaction)
                                {
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                    TFTVLogger.Always("VO#8. Original value was " + diplomacyChange.Value + ". New value is " + diplomacyChange.Value * 1.5f);
                                    diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 1.5f);
                                    __instance.Diplomacy[t] = diplomacyChange;

                                }
                            }
                        }
                    }


                    if (config.ResourceMultiplier != 1)
                    {

                        for (int i = 0; i < __instance.Resources.Count; i++)
                        {
                            ResourceUnit resources = __instance.Resources[i];
                            TFTVLogger.Always("Resource Multiplier changing resource reward from " + __instance.Resources[i].Value + " to " 
                                + __instance.Resources[i].Value * config.ResourceMultiplier);
                            resources.Value *= config.ResourceMultiplier; 
                            __instance.Resources[i] = resources;
                           
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void Postfix(GeoEventChoiceOutcome __instance, string eventID)
            {
                try
                {
                    
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));

                    if (config.DiplomaticPenalties)
                    {
                        if (!ExcludedEventsDiplomacyPenalty.Contains(eventID) && __instance.Diplomacy.Count > 0)
                        {
                            for (int i = 0; i < __instance.Diplomacy.Count; i++)
                            {
                                if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value <= 0)
                                {
                                    TFTVLogger.Always("Harder diplomacy. Reverting to original value for event " + eventID + ". Current diplo penalty is " + __instance.Diplomacy[i].Value +
                                        ". Original diplomacy value is " + __instance.Diplomacy[i].Value / 2);
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                    diplomacyChange.Value /= 2;
                                    __instance.Diplomacy[i] = diplomacyChange;

                                }
                            }
                        }
                    }
                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(2))
                    {
                        TFTVLogger.Always("VoidOmen2 check passed");
                        if (__instance.Diplomacy.Count > 0)
                        {
                            for (int t = 0; t < __instance.Diplomacy.Count; t++)
                            {
                                if (__instance.Diplomacy[t].Value != 0)
                                {
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                    TFTVLogger.Always("VO#2. Reverting to original value, from  " + diplomacyChange.Value + ", to former value " + diplomacyChange.Value * 2f);
                                    diplomacyChange.Value = Mathf.CeilToInt(diplomacyChange.Value * 2f);
                                    __instance.Diplomacy[t] = diplomacyChange;

                                }
                            }
                        }
                    }
                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(8))
                    {
                        if (__instance.Diplomacy.Count > 0)
                        {
                            for (int t = 0; t < __instance.Diplomacy.Count; t++)
                            {
                                if (__instance.Diplomacy[t].Value <= 0 && __instance.Diplomacy[t].TargetFaction != PhoenixFaction)
                                {
                                    OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                    TFTVLogger.Always("VO#8. Reverting to original value, from " + diplomacyChange.Value + ", to former value " + diplomacyChange.Value * (2/3));
                                    diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * (2/3));
                                    __instance.Diplomacy[t] = diplomacyChange;

                                }
                            }
                        }
                    }


                    if (config.ResourceMultiplier != 1)
                    {

                        for (int i = 0; i < __instance.Resources.Count; i++)
                        {
                            ResourceUnit resources = __instance.Resources[i];
                            TFTVLogger.Always("Resource Multiplier, reverting resource reward from " + __instance.Resources[i].Value + " back to "
                                + __instance.Resources[i].Value / config.ResourceMultiplier);
                            resources.Value /= config.ResourceMultiplier;
                            __instance.Resources[i] = resources;

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }



        [HarmonyPatch(typeof(GeoFaction), "OnDiplomacyChanged")]
        public static class GeoBehemothActor_OnDiplomacyChanged_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.DiplomaticPenalties;
            }

            public static void Postfix(GeoFaction __instance, PartyDiplomacy.Relation relation, int newValue)

            {
                try
                {
                    CheckPostponedFactionMissions(__instance, relation, newValue);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        //The Strates Solution
        public static void CheckPostponedFactionMissions(GeoFaction faction, PartyDiplomacy.Relation relation, int newValue)
        {
            GeoscapeEventSystem eventSystem = faction.GeoLevel.EventSystem; // endless dereferencing hurts my poor soul
          //  TFTVLogger.Always("Diplomacy changed, CheckPostponedFactionMissions invoked");
            try
            {
                GeoFaction targetFaction = faction.GeoLevel.GetFaction((PPFactionDef)relation.WithParty);
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(targetFaction, faction.GeoLevel.ViewerFaction);

                if (faction.GetParticipatingFaction() == faction.GeoLevel.AnuFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    TFTVLogger.Always("The record for event PROG_AN2 states that choice " + eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_AN4 states that choice " + eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_AN6 states that choice " + eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record shows PROG_AN4 was completed on " + eventSystem.GetEventRecord("PROG_AN4")?.CompletedAt + " it is now " + faction.GeoLevel.Timing.Now);
                    
                    // GetEventRecord can return null, implying that this event has never spawned. Not sure that should happen in postpone check, but the choice conditional will be false either way
                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0) // choice 0 is postpone for this event, according to TFTVDefsWithConfigDependency.cs
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49 && eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);
                    }
                    else if (newValue == 74 && eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 2)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    TFTVLogger.Always("The record for event PROG_NJ1 states that choice " + eventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_NJ2 states that choice " + eventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_NJ3 states that choice " + eventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice + " was chosen");

                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49 && eventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);
                    }
                    else if (newValue == 74 && eventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    TFTVLogger.Always("The record for event PROG_SY1 states that choice " + eventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_SY4_P states that choice " + eventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice + " was chosen");
                    TFTVLogger.Always("The record for event PROG_SY4_T states that choice " + eventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice + " was chosen");


                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice ==2)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }
                    else if (newValue == 74)
                    {
                        if (eventSystem.GetVariable("Polyphonic") > eventSystem.GetVariable("Terraformers"))
                        {
                            if (eventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                            {
                                eventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                            }
                        }
                        else
                        {
                            if (eventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1)
                            {
                                eventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);
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


/*
        public static void CheckPostponedFactionMissions(GeoFaction faction, PartyDiplomacy.Relation relation, int newValue)
        {
            try
            {
                GeoFaction targetFaction = faction.GeoLevel.GetFaction((PPFactionDef)relation.WithParty);
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(targetFaction, faction.GeoLevel.ViewerFaction);

                if (faction.GetParticipatingFaction() == faction.GeoLevel.AnuFaction
                       && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 2)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);

                    }
                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 3)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);

                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 2)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);

                    }
                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 3)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }

                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 3)
                    {
                        if (faction.GeoLevel.EventSystem.GetVariable("Polyphonic") > faction.GeoLevel.EventSystem.GetVariable("Terraformers"))
                        {

                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                        }
                        else
                        {
                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);

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