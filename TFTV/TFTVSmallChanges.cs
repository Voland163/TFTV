using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Home.View.ViewModules;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVSmallChanges
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void ChangesToMedbay()
        {
            try
            {
                HealFacilityComponentDef e_HealMedicalBay_PhoenixFacilityDe = Repo.GetAllDefs<HealFacilityComponentDef>().FirstOrDefault(ged => ged.name.Equals("E_Heal [MedicalBay_PhoenixFacilityDef]"));
                e_HealMedicalBay_PhoenixFacilityDe.BaseHeal = 16;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
      
    }
}
