using Base;
using HarmonyLib;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Base.Audio.WwiseIDs.SWITCHES;

namespace TFTV
{
    internal class TFTVSDIandVoidOmenRoll
    {
        // private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        // Current and last ODI level
        public static int CurrentODI_Level = 0;
       // public static List<bool> PlayedODIEvents = new List<bool>();
        // All SDI (ODI) event IDs, levels as array, index 0 - 19
        public static readonly string[] ODI_EventIDs = new string[]
        {
    "SDI_01",
    "SDI_02",
    "SDI_03",
    "SDI_04",
    "SDI_05",
    "SDI_06",
    "SDI_07",
    "SDI_08",
    "SDI_09",
    "SDI_10",
    "SDI_11",
    "SDI_12",
    "SDI_13",
    "SDI_14",
    "SDI_15",
    "SDI_16",
    "SDI_17",
    "SDI_18",
    "SDI_19",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
    "SDI_20",
        };

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        internal static class BC_GeoAlienFaction_UpdateFactionDaily_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(GeoAlienFaction __instance, int ____evolutionProgress)
            {
                if (!__instance.GeoLevel.Tutorial.InProgress)
                {
                    ApplyNewODI(__instance, ____evolutionProgress);
                }
            }
        }

        public static List<int> GetPossibleVoidOmens(GeoLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
                List<int> voidOmensList = new List<int> { 1, 2, 4, 5, 6, 8, 9, 12, 13, 14, 18 };
                int currentODIlevel = controller.EventSystem.GetVariable("BC_SDI");
                // TFTVLogger.Always("CurrentODIlevel is " + currentODIlevel);
                int odiPerc = currentODIlevel * 5;


                if (config.MoreMistVO)
                {
                    voidOmensList.Add(7);
                }

                if (odiPerc < 30) //45) //only add unlimited Delirium if max Delirium is not reached
                {
                    voidOmensList.Add(10);
                }

                if (controller.AlienFaction.Research.HasCompleted("ALN_CrabmanUmbra_ResearchDef"))
                {
                    voidOmensList.Add(15);
                    voidOmensList.Add(16);

                }
                if (controller.EventSystem.GetVariable("BehemothEggHatched") == 1 && controller.AlienFaction.Behemoth != null)
                {
                    TFTVLogger.Always("Behemoth vo check");
                    voidOmensList.Add(11);

                }
                if (controller.EventSystem.GetVariable("Infestation_Encounter_Variable") == 1)
                {
                    voidOmensList.Add(17);

                }
                if (TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(controller).Count >= 8)
                {
                    voidOmensList.Add(3);
                }
                if (TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(controller).Count >= 5 && TFTVRevenant.DeadSoldiersDelirium.Keys.Count == 0)
                {
                    voidOmensList.Add(19);
                }

                foreach (int voidOmensInPlay in TFTVVoidOmens.CheckFordVoidOmensInPlay(controller))
                {
                    if (voidOmensList.Contains(voidOmensInPlay))
                    {
                        voidOmensList.Remove(voidOmensInPlay);
                    }
                }

                return voidOmensList;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            throw new InvalidOperationException();


        }

        internal static void Calculate_ODI_Level(GeoLevelController controller)
        {
            try
            {
                int evolutionProgress = controller.AlienFaction.EvolutionProgress;
                // Index of last element of the ODI event ID array is Length - 1
                int ODI_EventIDs_LastIndex = ODI_EventIDs.Length - 1;
                // Set a maximum number to determine the upper limit from when the maximum ODI level is reached
                int maxODI_Progress = 470 * ODI_EventIDs_LastIndex;
                // Calculate the current ODI level = index for the ODI event ID array
                // Mathf.Min = cap the lavel at max index, after that the index will not longer get increased wiht higher progress
                CurrentODI_Level = Mathf.Min(ODI_EventIDs_LastIndex, evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress);
                // CurrentODI_Level = (evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress) % ODI_EventIDs_LastIndex;
                // CurrentODI_Level = evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void ApplyNewODI(GeoAlienFaction geoAlienFaction, int evolutionProgress)
        {
            try
            {

                // Index of last element of the ODI event ID array is Length - 1
                int ODI_EventIDs_LastIndex = ODI_EventIDs.Length - 1;
                // Set a maximum number to determine the upper limit from when the maximum ODI level is reached
                int maxODI_Progress = 470 * ODI_EventIDs_LastIndex;
                // Calculate the current ODI level = index for the ODI event ID array
                // Mathf.Min = cap the lavel at max index, after that the index will not longer get increased wiht higher progress
                CurrentODI_Level = Mathf.Min(ODI_EventIDs_LastIndex, evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress);
                // CurrentODI_Level = (evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress) % ODI_EventIDs_LastIndex;
               // CurrentODI_Level = evolutionProgress * ODI_EventIDs_LastIndex / maxODI_Progress;



                // Get the GeoLevelController to get access to the event system and the variable
                GeoLevelController geoLevelController = geoAlienFaction.GeoLevel;
                // If current calculated level is different to last saved one then new ODI level is reached, show the new ODI event

                //removed for update #17
                /* if (geoLevelController.EventSystem.GetEventRecord("PROG_FS2_WIN") != null && geoLevelController.EventSystem.GetEventRecord("PROG_FS2_WIN").Completed)
                 {
                     TFTVLogger.Always("Behemoth defeated, so no more ODI");
                     return;
                 }*/

                //Need to always have ODIEvent 20 clear of variables
                geoLevelController.EventSystem.GetEventByID("SDI_20").GeoscapeEventData.Choices[0].Outcome.VariablesChange.Clear();

                if (CurrentODI_Level != geoLevelController.EventSystem.GetVariable("BC_SDI", -1))
                {

                 //   CurrentODI_Level = CurrentODI_Level > ODI_EventIDs_LastIndex ? ODI_EventIDs_LastIndex : CurrentODI_Level;
                    TFTVLogger.Always("CurrentODI_Level is " + CurrentODI_Level);
                    // Get the Event ID from array dependent on calculated level index
                    string eventID = ODI_EventIDs[CurrentODI_Level];

                  /*  if (ODI_EventIDs.Length < CurrentODI_Level)
                    {
                        eventID = ODI_EventIDs.Last();
                    }*/
                    TFTVLogger.Always("ODI Event is " + eventID + " and Alien EP are " + geoAlienFaction.EvolutionProgress);
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(geoAlienFaction, geoLevelController.ViewerFaction);
                    GeoscapeEventDef oDIEventToTrigger = geoLevelController.EventSystem.GetEventByID(eventID);

                    // Void Omens roll
                    // Before the roll, Void Omen has not been rolled
                    bool voidOmenRolled = false;
                    int voidOmenRoll = 0;
                    // Create variable reflecting difficulty level, 1 being the easiest, and 4 the hardest
                    // This will determine amount of possible simultaneous Void Omens
                    int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(geoLevelController.CurrentDifficultyLevel.Order);
                    string triggeredVoidOmens = "TriggeredVoidOmen_";
                    string voidOmen = "VoidOmen_";
                    string voidOmenTitle = "VOID_OMEN_TITLE_";
                    string voidOmenDescription = "VOID_OMEN_DESCRIPTION_TEXT_";
                    //This is a bool to check if the list of possible Void Omens has been repopulated
                    bool voidOmenListRepopulated = false;

                    if (geoLevelController.EventSystem.GetVariable("BC_SDI") > 0)
                    {
                        // Here comes the roll, with 1/10 chance of no VO happening    
                        int roll = UnityEngine.Random.Range(1, 11);
                        TFTVLogger.Always("The roll on the 1D10 is " + roll);
                        if (roll == 1)
                        {
                            TFTVVoidOmens.RemoveEarliestVoidOmen(geoLevelController);
                        }

                        if (roll >= 2 && roll <= 10)
                        {

                            // If a Void Omen rolls
                            // Create list of Void Omens currently implemented
                            List<int> voidOmensList = GetPossibleVoidOmens(geoLevelController);

                            // Check for already rolled Void Omens
                            List<int> allVoidOmensAlreadyRolled = TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(geoLevelController);
                            //Remove already rolled Void Omens from the list of available Void Omens
                            if (allVoidOmensAlreadyRolled.Count > 0)
                            {
                                foreach (int i in allVoidOmensAlreadyRolled)
                                {
                                    voidOmensList.Remove(i);

                                }
                            }
                            TFTVLogger.Always("The number of remaining VOs is " + voidOmensList.Count);

                            for (int x = 0; x < voidOmensList.Count; x++)
                            {
                                TFTVLogger.Always(voidOmensList[x] + " available for roll");

                            }

                            if (voidOmensList.Count == 0)
                            {
                                TFTVVoidOmens.ClearListOfAlreadyRolledVoidOmens(geoLevelController);
                                voidOmensList = GetPossibleVoidOmens(geoLevelController);
                                voidOmenListRepopulated = true;
                            }

                            // Get a random void omen from the available Void Omens list
                            voidOmenRoll = voidOmensList.GetRandomElement();

                            // We can have as many simulateneous Void Omens in play as the mathematical expression of the difficulty level
                            // Lets check how many Void Omens are already in play and if there is space for more
                            int[] voidOmensInPlay = TFTVVoidOmens.CheckFordVoidOmensInPlay(geoLevelController);

                            //If there is no space, we have to remove the earliest one
                            if (!voidOmensInPlay.Contains(0))
                            {
                                TFTVLogger.Always($"All VO slots taken, need to remove an existing VO");
                                TFTVVoidOmens.RemoveEarliestVoidOmen(geoLevelController);
                            }
                            //Then let's find a spot for the new Void Omen
                            for (int t = 0; t < difficulty; t++)
                            {
                                // There will be as many Void Omen variables (each storing an active Void Omen) as the ME of the difficulty level
                                // The first empty Dark Event variable will receive the new Void Omen
                                if (geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - t)) == 0)
                                {
                                    // This is the regular code to modify a Def, in this case the ODI event to which the Void Omen will be attached,
                                    // so that it sets the Void Omen variable
                                    oDIEventToTrigger.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(new OutcomeVariableChange
                                    {
                                        VariableName = voidOmen + (difficulty - t),
                                        Value = { Min = voidOmenRoll, Max = voidOmenRoll },
                                        IsSetOperation = true,
                                    });
                                    // This records which ODI event triggered which Void Omen
                                    int variableRolledVoidOmen = geoAlienFaction.EvolutionProgress / 470;
                                    geoLevelController.EventSystem.SetVariable(triggeredVoidOmens + variableRolledVoidOmen, voidOmenRoll);
                                    // Raise the flag, we have a Void Omen!
                                    voidOmenRolled = true;
                                    // Then close the loop:
                                    t = 4;
                                }
                            }
                        }
                    }

                    // The ODI event is triggered
                    geoLevelController.EventSystem.TriggerGeoscapeEvent(oDIEventToTrigger.EventID, geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BC_SDI", CurrentODI_Level);



                    //UpdateODITracker(CurrentODI_Level, geoLevelController); not used currently, because clogs the UI
                    // And if a Void Omen has been rolled, a Void Omen will appear
                    if (voidOmenRolled && TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(geoLevelController).Count == 1 && !voidOmenListRepopulated)
                    {
                        GeoscapeEventDef voidOmenIntro = geoLevelController.EventSystem.GetEventByID("VoidOmenIntro");
                        voidOmenIntro.GeoscapeEventData.Title.LocalizationKey = "VOID_OMEN_INTRO_TITLE";
                        voidOmenIntro.GeoscapeEventData.Description[0].General.LocalizationKey = "VOID_OMEN_INTRO";
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("VoidOmenIntro", geoscapeEventContext);
                    }
                    // This adds the Void Omen to the objective list
                    if (voidOmenRolled)
                    {
                        //  string title = TFTVVoidOmens.VoidOmens_Title.GetValue(voidOmenRoll - 1).ToString();
                        //  string description = TFTVVoidOmens.VoidOmens_Description.GetValue(voidOmenRoll - 1).ToString();
                        GeoscapeEventDef voidOmenEvent = geoLevelController.EventSystem.GetEventByID("VoidOmen");

                        voidOmenEvent.GeoscapeEventData.Title.LocalizationKey = voidOmenTitle + voidOmenRoll;
                        voidOmenEvent.GeoscapeEventData.Description[0].General.LocalizationKey = voidOmenDescription + voidOmenRoll;
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("VoidOmen", geoscapeEventContext);
                        TFTVVoidOmens.CreateVoidOmenObjective(voidOmenTitle + voidOmenRoll, voidOmenDescription + voidOmenRoll, geoLevelController);
                    }
                    // Implement the new Void Omen situation
                   // TFTVVoidOmens.CheckVoidOmensBeforeImplementing(geoLevelController);
                    TFTVVoidOmens.ImplementVoidOmens(geoLevelController);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
