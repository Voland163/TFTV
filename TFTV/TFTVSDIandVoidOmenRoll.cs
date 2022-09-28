using Base;
using HarmonyLib;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV
{
    internal class TFTVSDIandVoidOmenRoll
    {

        // Current and last ODI level
        public static int CurrentODI_Level = 0;
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
    "SDI_20"
        };

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        internal static class BC_GeoAlienFaction_UpdateFactionDaily_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(GeoAlienFaction __instance, int ____evolutionProgress)
            {
                Calculate_ODI_Level(__instance, ____evolutionProgress);
            }
        }


        internal static void Calculate_ODI_Level(GeoAlienFaction geoAlienFaction, int evolutionProgress)
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
                // Get the GeoLevelController to get access to the event system and the variable
                GeoLevelController geoLevelController = geoAlienFaction.GeoLevel;
                // If current calculated level is different to last saved one then new ODI level is reached, show the new ODI event
                if (geoLevelController.EventSystem.GetVariable("CorruptedLairDestroyed") == 1)
                {
                    return;
                }
                
                else

                    if (CurrentODI_Level != geoLevelController.EventSystem.GetVariable("BC_SDI", -1))
                {
                    // Get the Event ID from array dependent on calculated level index

                    string eventID = ODI_EventIDs[CurrentODI_Level];
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(geoAlienFaction, geoLevelController.ViewerFaction);
                    GeoscapeEventDef oDIEventToTrigger = geoLevelController.EventSystem.GetEventByID(ODI_EventIDs[CurrentODI_Level]);

                    // Void Omens roll
                    // Before the roll, Void Omen has not been rolled
                    bool voidOmenRolled = false;
                    int voidOmenRoll = 0;
                    // Create variable reflecting difficulty level, 1 being the easiest, and 4 the hardest
                    // This will determine amount of possible simultaneous Void Omens
                    int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                    string triggeredVoidOmens = "TriggeredVoidOmen_";
                    string voidOmen = "VoidOmen_";
                    string voidOmenTitle = "VOID_OMEN_TITLE_";
                    string voidOmenDescription = "VOID_OMEN_DESCRIPTION_TEXT_";

                    if (geoLevelController.EventSystem.GetVariable("BC_SDI") > 0)
                    {
                        // Here comes the roll, for testing purposes with 1/10 chance of no VO happening    
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
                            List<int> voidOmensList = new List<int> { 1, 2, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 18 };
                            
                            if (geoAlienFaction.Research.HasCompleted("ALN_CrabmanUmbra_ResearchDef"))
                            {
                                voidOmensList.Add(15);
                                voidOmensList.Add(16);
                                
                            }
                            if (geoAlienFaction.GeoLevel.EventSystem.GetVariable("BehemothEggHatched") == 1)
                            {
                                voidOmensList.Add(11);
                                
                            }
                            if (geoAlienFaction.GeoLevel.EventSystem.GetVariable("Infestation_Encounter_Variable") == 1)
                            {
                                voidOmensList.Add(17);
                                
                            }
                            if (TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(geoLevelController).Count >= 8) 
                            {
                                voidOmensList.Add(3);                           
                            }
                            if (TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(geoLevelController).Count >= 5 && TFTVRevenant.DeadSoldiersDelirium.Keys.Count==0)
                            {
                                voidOmensList.Add(19);
                            }

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
                            TFTVLogger.Always("The number of remaining VOs is" + voidOmensList.Count);

                            // Get a random dark event from the available Void Omens list
                            voidOmenRoll = voidOmensList.GetRandomElement();
   
                            // We can have as many simulateneous Void Omens in play as the mathematical expression of the difficulty level
                            // Lets check how many Void Omens are already in play and if there is space for more
                            int[] voidOmensInPlay = TFTVVoidOmens.CheckFordVoidOmensInPlay(geoLevelController);
                            
                            //If there is no space, we have to remove the earliest one
                            if (!voidOmensInPlay.Contains(0))
                            {
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
                                    geoLevelController.EventSystem.SetVariable(triggeredVoidOmens + CurrentODI_Level, voidOmenRoll);
                                    // Raise the flag, we have a Void Omen!
                                    voidOmenRolled = true;
                                    // Then close the loop:
                                    t = 4;
                                }
                            }
                        }
                    }

                    // The ODI event is triggered
                    geoLevelController.EventSystem.TriggerGeoscapeEvent(ODI_EventIDs[CurrentODI_Level], geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BC_SDI", CurrentODI_Level);
                    //UpdateODITracker(CurrentODI_Level, geoLevelController); not used currently, because clogs the UI
                    // And if a Void Omen has been rolled, a Void Omen will appear
                    if (voidOmenRolled && TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(geoLevelController).Count==1)
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
