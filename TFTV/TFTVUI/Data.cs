using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFTV.TFTVUI.Personnel;

namespace TFTV.TFTVUI
{
    internal class Data
    {
        public static void ClearInternalDataOnLoad()
        {
            try 
            { 
                ShowWithoutHelmet.uIModuleSoldierCustomization = null;
                Loadouts.CharacterLoadouts?.Clear();
                Geoscape.Facilities.ClearInternalDataForUIGeo();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
