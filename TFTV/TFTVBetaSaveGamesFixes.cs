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

namespace TFTV
{
    internal class TFTVBetaSaveGamesFixes
    {
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
