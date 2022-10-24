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


    }
}
