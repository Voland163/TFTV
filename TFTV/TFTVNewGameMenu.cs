using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

      

        //You will need to edit scene hierarchy to add new objects under GameSettingsModule, it has a UIModuleGameSettings script
        //Class UIStateNewGeoscapeGameSettings is responsible for accepting selected settings and start the game, so you'll have to dig inside
        //for changing behaviour.
        /*   [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")]
           internal static class UIStateNewGeoscapeGameSettings_EnterState_patch
           {
               private static void Postfix(UIStateNewGeoscapeGameSettings __instance)
               {
                   try
                   {



                   }



                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }


           }*/


    }
}
