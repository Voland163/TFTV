using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

namespace TFTV.TFTVUI.Tactical
{
    internal class Data
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static Font PuristaSemiboldFontCache = null;
        public static Color LeaderColor = new Color(1f, 0.72f, 0f);
        public static Color NegativeColor = new Color(1.0f, 0.145f, 0.286f);
        public static Color RegularNoLOSColor = Color.gray;
        public static Color WhiteColor = new Color(0.820f, 0.859f, 0.914f);
        public static Color VoidColor = new Color(0.525f, 0.243f, 0.937f);

        public static void CachePuristaSemiboldFont(UIModuleObjectives uIModuleObjectives)
        {
            try
            {
                // TFTVLogger.Always($"uIModuleObjectives.transform.Find(\"Objectives_Text\").GetComponentInChildren<Text>().font: {uIModuleObjectives.transform.Find("Objectives_Text").GetComponentInChildren<Text>().font}");

                if (PuristaSemiboldFontCache == null)
                {
                    PuristaSemiboldFontCache = uIModuleObjectives.transform.Find("Objectives_Text").GetComponentInChildren<Text>().font;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static void ClearDataOnLoadAndStateChange()
        {
            try
            {
                DeliriumWidget.ClearDataOnLoadAndStateChange();
                CaptureTacticalWidget.ClearData();
                SecondaryObjectivesTactical.ClearDataOnGameLoadAndStateChange();
                OpposingHumanoidForceWidget.ClearData();
                Ancients.ClearData();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void ClearDataOnMissionRestart()
        {
            try
            {
                DeliriumWidget.ClearDataOnMissionRestart();
                CaptureTacticalWidget.ClearData();
                OpposingHumanoidForceWidget.ClearData();
                Ancients.ClearData();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public class AddOutlineToIcon : MonoBehaviour
        {
            public GameObject icon; // The icon GameObject to outline

            public void InitOrUpdate()
            {
                try
                {
                    // Ensure the icon has a Graphic component, like Image or Text
                    var graphic = icon.GetComponent<Graphic>();
                    if (graphic != null)
                    {
                        // Add the Outline component
                        Outline outline = icon.GetComponent<Outline>() ?? icon.AddComponent<Outline>();
                        outline.effectColor = Color.black;         // Outline color

                        if (icon.GetComponent<Image>() != null && icon.GetComponent<Image>().sprite == DefCache.GetDef<ViewElementDef>("E_ViewElement [TBTV_Hidden_AbilityDef]").SmallIcon)
                        {
                            // TFTVLogger.Always($"got here");
                            outline.effectColor = VoidColor;
                            icon.GetComponent<Image>().color = Color.black;
                        }
                        outline.effectDistance = new Vector2(2.5f, 2.5f); // Outline thickness
                    }
                    else
                    {
                        TFTVLogger.Always("No Graphic component found on icon.");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
