using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVNewGameOptions
    {


        public enum StartingSquadFaction
        {
            PHOENIX, ANU, NJ, SYNEDRION
        }

        public static StartingSquadFaction startingSquad = StartingSquadFaction.PHOENIX;
        
        public enum StartingBaseLocation
        {
           
            Vanilla,
            Random,
            Antarctica,
            China,
            Australia,
            Honduras,
            Ethiopia,
            Ukraine,
            Greenland,
            Afghanistan,
            Algeria,
            Alaska,
            Quebec,
            Siberia,
            Zimbabwe,
            Bolivia,
            Argentina,
            Cambodia,
            Ghana
        }

        public static StartingBaseLocation startingBaseLocation = StartingBaseLocation.Vanilla;

        public enum StartingSquadCharacters
        {
            UNBUFFED, BUFFED, RANDOM
        }
       
        public static StartingSquadCharacters startingSquadCharacters = StartingSquadCharacters.UNBUFFED;


        public static bool LimitedCapture = true;
        public static bool LimitedHarvesting = true;

        /*         [ConfigField(text: "CAPTURING PANDORANS IS LIMITED",
   description: "There is a limit to how many Pandorans you can capture per mission. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
         public bool LimitedCapture = true;

         [ConfigField(text: "LIMITS ON RENDERING PANDORANS FOR FOOD OR MUTAGENS",
  description: "New mechanics make obtaining food or mutagens from captured Pandorans harder. IF YOU SET THIS TO FALSE, PLEASE QUIT TO DESKTOP BEFORE STARTING A NEW GAME/LOADING A SAVE")]
         public bool LimitedHarvesting = true;*/

        // These settings determine amount of resources player can acquire:
       
        public static int initialScavSites = 8; // 16 on Vanilla

        public enum ScavengingWeight
        {
            High, Medium, Low, None
        }

        public static ScavengingWeight chancesScavCrates = ScavengingWeight.High;
        
        public static ScavengingWeight chancesScavSoldiers = ScavengingWeight.Low;
        
        public static ScavengingWeight chancesScavGroundVehicleRescue = ScavengingWeight.Low;


        


    }
}
