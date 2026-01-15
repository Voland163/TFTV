using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV
{
    internal class Various
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color blue = new Color32(62, 12, 224, 255);
        internal static Color green = new Color32(12, 224, 30, 255);
        internal static Color anu = new Color(0.9490196f, 0.0f, 1.0f, 1.0f);
        internal static Color nj = new Color(0.156862751f, 0.6156863f, 1.0f, 1.0f);
        internal static Color syn = new Color(0.160784319f, 0.8862745f, 0.145098045f, 1.0f);

        internal class Miscelaneous
        {


            [HarmonyPatch(typeof(GeoscapeLogEntryController), nameof(GeoscapeLogEntryController.SetEntry))]
            public static class GeoscapeLogEntryController_SetEntry_patch
            {
                public static void Postfix(GeoscapeLogEntryController __instance, GeoscapeLogEntry logEntry)
                {
                    try
                    {
                        // TFTVLogger.Always($"GeoscapeLogEntryController_SetEntry_patch {__instance?.Text?.text}");

                        if (__instance.Text.text != null)
                        {
                            GeoActor targetActor = null;

                            GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            List<GeoSite> allSites = controller.Map.AllSites.Where(s => s.LocalizedSiteName != null && s.LocalizedSiteName != "").ToList();


                            GeoSite geoSite = allSites.FirstOrDefault(s => __instance.Text.text.Contains(s.LocalizedSiteName));

                            if (geoSite != null)
                            {
                                if (geoSite.LocalizedSiteName == TFTVCommonMethods.ConvertKeyToString("KEY_HAVEN_NAME_NEW_JERICHO1"))
                                {
                                    for (int x = 1; x <= 50; x++)
                                    {
                                        string havenName = TFTVCommonMethods.ConvertKeyToString($"KEY_HAVEN_NAME_NEW_JERICHO{x}");
                                        if (__instance.Text.text.Contains(havenName))
                                        {
                                            geoSite = allSites.FirstOrDefault(s => s.LocalizedSiteName == havenName);
                                            //  TFTVLogger.Always($"found haven {geoSite.LocalizedSiteName} referenced by {__instance.Text.text}");
                                            break;
                                        }
                                    }
                                }
                            }


                            if (geoSite != null && geoSite.GetVisible(controller.PhoenixFaction) && geoSite.GetInspected(controller.PhoenixFaction))
                            {
                                targetActor = geoSite;

                                TFTVLogger.Always($"found geosite {geoSite.LocalizedSiteName} referenced by {__instance.Text.text}");
                            }
                            else
                            {
                                GeoCharacter geoCharacter = controller.PhoenixFaction.Soldiers.FirstOrDefault(c => __instance.Text.text.Contains(c.Identity.Name));

                                if (geoCharacter != null)
                                {
                                    TFTVLogger.Always($"found character {geoCharacter.Identity.Name} referenced by {__instance.Text.text}");

                                    GeoSite geoSite1 = controller.PhoenixFaction.Bases.FirstOrDefault(b => b.SoldiersInBase.Contains(geoCharacter))?.Site;

                                    if (geoSite1 != null)
                                    {
                                        TFTVLogger.Always($"found site at which the character is: {geoSite1?.LocalizedSiteName}");

                                        targetActor = geoSite1;
                                    }
                                    else
                                    {
                                        foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                                        {
                                            if (geoVehicle.GetAllCharacters().Contains(geoCharacter))
                                            {
                                                TFTVLogger.Always($"found vehicle at which the character is: {geoVehicle.Name}");

                                                targetActor = geoVehicle;
                                                break;

                                            }

                                        }

                                    }
                                }
                                else
                                {
                                    List<GeoVehicle> geoVehicles = new List<GeoVehicle>();

                                    geoVehicles.AddRange(controller.PhoenixFaction.Vehicles.Where(v => __instance.Text.text.Contains(v.Name))?.ToList());

                                    // TFTVLogger.Always($"geoVehicles==null: {geoVehicles==null}");

                                    if (geoVehicles != null && geoVehicles.Count > 0)
                                    {
                                        TFTVLogger.Always($"found {geoVehicles.Last().Name} referenced by {__instance.Text.text}");
                                        targetActor = geoVehicles.Last();
                                    }
                                }
                            }

                            if (targetActor != null)
                            {

                                GameObject go = __instance.gameObject;

                                if (!go.GetComponent<EventTrigger>())
                                {
                                    go.AddComponent<EventTrigger>();
                                }

                                EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                                eventTrigger.triggers.Clear();
                                EventTrigger.Entry click = new EventTrigger.Entry
                                {
                                    eventID = EventTriggerType.PointerClick
                                };

                                click.callback.AddListener((eventData) =>
                                {
                                    controller.Timing.Paused = true;
                                    controller.View.ChaseTarget(targetActor, false);

                                });

                                eventTrigger.triggers.Add(click);
                            }

                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleCorruptionReport), nameof(UIModuleCorruptionReport.Init))]
            public static class UIModuleCorruptionReport_Init_patch
            {
                public static bool Prefix(UIModuleCorruptionReport __instance, GeoscapeViewContext context)
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
}
