using Base;
using Base.Core;
using Base.UI.MessageBox;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;

namespace TFTV
{

    internal class AircraftReworkMissionDeployment
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(UIStateRosterDeployment), "OnEnrollmentChanged")]
        public static class UIStateRosterDeployment_OnEnrollmentChanged_Patch
        {

            private static void NoVehicleMutogWarning(UIStateRosterDeployment __instance, GeoRosterDeploymentItem item, MessageBox ____confirmationBox, List<GeoCharacter> ____selectedDeployment, List<GeoRosterDeploymentItem> ____deploymentItems)
            {
                try
                {
                    UIModuleDeploymentMissionBriefing missionBriefingModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.DeploymentMissionBriefingModule;
                    MethodInfo methodInfoCheckForDeployment = AccessTools.Method(typeof(UIStateRosterDeployment), "CheckForDeployment", new Type[] { typeof(IEnumerable<GeoCharacter>) });

                    item.EnrollForDeployment = !item.EnrollForDeployment;
                    item.RefreshCheckVisuals();

                    ____selectedDeployment.Clear();
                    ____selectedDeployment.AddRange(from s in ____deploymentItems
                                                    where s.EnrollForDeployment
                                                    select s.Character);

                    methodInfoCheckForDeployment.Invoke(__instance, new object[] { ____selectedDeployment });
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static bool Prefix(UIStateRosterDeployment __instance, GeoRosterDeploymentItem item, MessageBox ____confirmationBox, List<GeoCharacter> ____selectedDeployment, List<GeoRosterDeploymentItem> ____deploymentItems)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!item.Character.TemplateDef.IsVehicle && !item.Character.TemplateDef.IsMutog)
                    {
                        return true;
                    }

                    if (config.MultipleVehiclesInAircraftAllowed)
                    {
                        NoVehicleMutogWarning(__instance, item, ____confirmationBox, ____selectedDeployment, ____deploymentItems);
                        return false;
                    }
                    else
                    {
                        if (!AircraftReworkOn)
                        {
                            return true;
                        }
                    }


                    if (__instance.Mission.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef"))
                       || __instance.Mission.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef")))
                    {
                        return true;
                    }


                    GeoVehicle geoVehicle = __instance.Mission.Site.GetPlayerVehiclesOnSite()?.FirstOrDefault(v => v.Units.Contains(item.Character));

                    if (geoVehicle == null)
                    {
                        return true;
                    }

                    bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                    bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                    bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                    if ((hasHarness && item.Character.TemplateDef.IsVehicle) || (hasMutogPen && item.Character.TemplateDef.IsMutog))
                    {

                    }
                    else
                    {
                        return true;
                    }

                    NoVehicleMutogWarning(__instance, item, ____confirmationBox, ____selectedDeployment, ____deploymentItems);

                    return false;

                }



                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(UIStateRosterDeployment), "CheckForDeployment")]
        public static class UIStateRosterDeployment_CheckForDeployment_Patch
        {
            public static bool Prefix(UIStateRosterDeployment __instance, IEnumerable<GeoCharacter> squad, GeoMission ____mission)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    UIModuleDeploymentMissionBriefing missionBriefingModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.DeploymentMissionBriefingModule;

                    if (AircraftReworkOn)
                    {

                        missionBriefingModule.DeployButton.SetInteractable(squad.Any());
                        missionBriefingModule.DeployButton.ResetButtonAnimations();

                        missionBriefingModule.SquadSlotsUsedText.text = "";
                        return false;
                    }

                    if (config.MultipleVehiclesInAircraftAllowed)
                    {
                        int maxUnits = ____mission.MissionDef.MaxPlayerUnits;

                        if (config.UnLimitedDeployment)
                        {
                            maxUnits = 99;
                        }
                        bool flag = squad.Any();
                        int num = squad.Sum((GeoCharacter s) => s.OccupingSpace);
                        // int num2 = squad.Count((GeoCharacter c) => c.TemplateDef.IsVehicle || c.TemplateDef.IsMutog);
                        missionBriefingModule.SetCurrentDeployment(num, maxUnits);
                        bool flag2 = num <= maxUnits;
                        //  bool flag3 = num2 < 2;
                        missionBriefingModule.DeployButton.SetInteractable(flag && flag2);
                        missionBriefingModule.DeployButton.ResetButtonAnimations();
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(GeoMission), "GetDeploymentSources")]
        public static class GeoMission_GetDeploymentSource_Patch
        {
            public static void Postfix(GeoMission __instance, GeoFaction faction, IGeoCharacterContainer priorityContainer, ref List<IGeoCharacterContainer> __result)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef"))
                        || __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef")))
                    {
                        return;
                    }

                    if (priorityContainer == null)
                    {
                        // Debug.LogError($"no vehicle was passed from sources!");
                        // __result = new List<IGeoCharacterContainer>();

                        priorityContainer = __instance.Site.Vehicles.FirstOrDefault((GeoVehicle v) => v.Owner == faction && v.Units.Count() > 0);

                    }

                    __result = new List<IGeoCharacterContainer> { priorityContainer };

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



