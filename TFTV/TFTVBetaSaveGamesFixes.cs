using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.AI.Considerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Base.Input.InputController;
using static PhoenixPoint.Common.Core.PartyDiplomacy;
using UnityEngine.EventSystems;
using I2.Loc;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Geoscape.Entities.Research;

namespace TFTV
{
    internal class TFTVBetaSaveGamesFixes
    {
        public static bool LOTAapplied = false;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void SpecialFixForTesting(GeoLevelController controller) 
        {
            try
            {
                controller.EventSystem.SetVariable("NewGameStarted", 1);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }



        public static void CheckNewLOTA(GeoLevelController controller)
        {
            try 
            {
                if (controller.EventSystem.GetVariable("NewGameStarted") == 1 && !LOTAapplied) 
                {
                    LOTAapplied = true;
                    TFTVDefsInjectedOnlyOnce.ChangesToLOTA2();
                    TFTVAncients.LOTAReworkActive = true;
                    TFTVLogger.Always("LOTA rework activared!");
                }
                CheckIfLOTAReworkActive();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckIfLOTAReworkActive()
        {
            try
            {

                ContextHelpHintDef hintStory1 = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_STORY1");
                ContextHelpHintDef hintCyclops = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPS");
                ContextHelpHintDef hintCyclopsDefense = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPSDEFENSE");
                ContextHelpHintDef hintHoplites = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITS");
                ContextHelpHintDef hintHopliteRepair = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSREPAIR");
                ContextHelpHintDef hintHopliteMaxPower = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSMAXPOWER");

                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");

                ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                ResearchDef ancientAutomataResearch = DefCache.GetDef<ResearchDef>("AncientAutomataResearch");
                ResearchDef pX_LivingCrystalResearch = DefCache.GetDef<ResearchDef>("PX_LivingCrystalResearchDef");
                ResearchDef pX_ProteanMutaneResearch = DefCache.GetDef<ResearchDef>("PX_ProteanMutaneResearchDef");


                if (TFTVAncients.LOTAReworkActive) 
                {
                    if (!researchDB.Researches.Contains(ancientAutomataResearch)) 
                    {
                        researchDB.Researches.Add(ancientAutomataResearch);              
                    }
                    if (!researchDB.Researches.Contains(pX_LivingCrystalResearch))
                    {
                        researchDB.Researches.Add(pX_LivingCrystalResearch);
                    }
                    if (!researchDB.Researches.Contains(pX_ProteanMutaneResearch))
                    {
                        researchDB.Researches.Add(pX_ProteanMutaneResearch);
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintStory1)) 
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintStory1);                   
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclops)) 
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclops);                
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclopsDefense))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclopsDefense);
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHoplites))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHoplites);
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteRepair))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteRepair);
                    }
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteMaxPower))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteMaxPower);
                    }
                }
                else 
                {
                    if (researchDB.Researches.Contains(ancientAutomataResearch))
                    {
                        researchDB.Researches.Remove(ancientAutomataResearch);
                    }
                    if (researchDB.Researches.Contains(pX_LivingCrystalResearch))
                    {
                        researchDB.Researches.Remove(pX_LivingCrystalResearch);
                    }
                    if (researchDB.Researches.Contains(pX_ProteanMutaneResearch))
                    {
                        researchDB.Researches.Remove(pX_ProteanMutaneResearch);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintStory1))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintStory1);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclops))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintCyclops);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclopsDefense))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintCyclopsDefense);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHoplites))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHoplites);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteRepair))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHopliteRepair);
                    }
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteMaxPower))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHopliteMaxPower);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void CheckSaveGameEventChoices(GeoLevelController controller) 
        {           
            try 
            {
                if(controller.EventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == -1) 
                {
                    controller.EventSystem.GetEventRecord("PROG_AN2").SelectChoice(0);
                    controller.EventSystem.GetEventRecord("PROG_AN2").Complete(controller.Timing.Now);
                }
                if(controller.EventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == -1)
                {
                    controller.EventSystem.GetEventRecord("PROG_AN4").SelectChoice(1);
                    controller.EventSystem.GetEventRecord("PROG_AN4").Complete(controller.Timing.Now);
                }             
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckUmbraResearchVariable(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0) //|| controller.AlienFaction.EvolutionProgress>=4700)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 4);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }
                else if (controller.EventSystem.GetEventRecord("SDI_09")?.SelectedChoice == 0)// || controller.AlienFaction.EvolutionProgress >= 4230)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 3);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }
                else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)// || controller.AlienFaction.EvolutionProgress >= 2820)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 2);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
