using Base.Core;
using HarmonyLib;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Linq;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVUITactical
    {

        public static void RemoveDamagePredictionBar()
        {
            try
            {
                if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null)
                {
                    UIModuleShootTargetHealthbar uIModuleShootTargetHealthbar = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.ShootTargetHealthBar;
                    uIModuleShootTargetHealthbar.ModulePanel.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("DamagePrediction")).gameObject.SetActive(false);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        [HarmonyPatch(typeof(UIStateShoot), "GetMinMaxPossibleDamage")]
        public static class UIStateShoot_GetMinMaxPossibleDamage_patch
        {
            public static bool Prefix(UIStateShoot __instance)
            {
                try
                {

                    return false;
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
    }
}
