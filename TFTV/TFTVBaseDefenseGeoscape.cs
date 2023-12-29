using Base;
using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVBaseDefenseGeoscape
    {
        public static Dictionary<int, Dictionary<string, double>> PhoenixBasesUnderAttack = new Dictionary<int, Dictionary<string, double>>();
        public static Dictionary<int, int> PhoenixBasesContainmentBreach = new Dictionary<int, int>();
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static List<int> PhoenixBasesInfested = new List<int>();
        public static Sprite VoidIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color brightRed = new Color32(255, 0, 0, 255);

        internal static GeoSite KludgeSite = null;

        internal class InitAttack 
        {
            public static GeoSite FindPhoenixBase(int id, GeoLevelController controller)
            {
                try
                {
                    List<GeoPhoenixBase> allPhoenixBases = controller.PhoenixFaction.Bases.ToList();

                    GeoSite targetPhoenixBase = null;

                    foreach (GeoPhoenixBase phoenixBase in allPhoenixBases)
                    {
                        if (phoenixBase.Site.SiteId == id)
                        {
                            targetPhoenixBase = phoenixBase.Site;
                        }
                    }

                    return targetPhoenixBase;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void AddToTFTVAttackSchedule(GeoSite phoenixBase, GeoLevelController controller, GeoFaction attacker, int hours)
            {
                try
                {
                    TimeUnit timeForAttack = TimeUnit.FromHours(hours);
                    TimeUnit timer = controller.Timing.Now + timeForAttack;
                    PhoenixBasesUnderAttack.Add(
                        phoenixBase.SiteId,
                        new Dictionary<string, double>()
                        {
                { attacker.GetPPName(), timer.TimeSpan.TotalHours}
                        });
                    phoenixBase.RefreshVisuals();
                    TFTVLogger.Always($"{phoenixBase.LocalizedSiteName} was added to the list of Phoenix bases under attack by {attacker}. " +
                        $"Attack will be completed successfully by {timer}");

                    if (hours != 18)
                    {
                        PhoenixBasesContainmentBreach.Add(phoenixBase.SiteId, hours);

                    }


                    //For implementing base defense as proper objective:
                    DiplomaticGeoFactionObjective protectBase = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                    {
                        Title = new LocalizedTextBind($"<color=#FF0000>Phoenix base {phoenixBase.LocalizedSiteName} is under attack!</color>", true)

                    };

                    /*  FieldInfo assignedSitesField = typeof(DiplomaticGeoFactionObjective).GetField("_assignedSites", BindingFlags.Instance | BindingFlags.NonPublic);

                          List<GeoSite> assignedSitesList = (List<GeoSite>)assignedSitesField.GetValue(protectBase);
                          assignedSitesList.Add(phoenixBase);*/



                    controller.PhoenixFaction.AddObjective(protectBase);



                    // phoenixBase.RefreshVisuals();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static int CountContainmentFacilities(GeoPhoenixFaction phoenixFaction)
            {
                try
                {
                    PrisonFacilityComponentDef containmentDef = DefCache.GetDef<PrisonFacilityComponentDef>("E_Prison [AlienContainment_PhoenixFacilityDef]");

                    int count = 0;

                    foreach (GeoPhoenixBase bases in phoenixFaction.Bases)
                    {
                        foreach (GeoPhoenixFacility facility in bases.Layout.Facilities)
                        {
                            if (facility.GetComponent<PrisonFacilityComponent>() is PrisonFacilityComponent containmentFacility
                                && containmentFacility.ComponentDef == containmentDef && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
                            {
                                count += 1;

                            }
                        }
                    }

                    return count;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void StartPandoranAttackOnBase(GeoLevelController controller, PPFactionDef factionDef, GeoSite phoenixBase)
            {
                try
                {
                    GeoFaction attacker = controller.GetFaction(factionDef);
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    PhoenixFacilityDef containment = DefCache.GetDef<PhoenixFacilityDef>("AlienContainment_PhoenixFacilityDef");
                    TFTVLogger.Always($"Phoenix base under attack? {PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId)} Phoenix base infested? {PhoenixBasesInfested.Contains(phoenixBase.SiteId)} ");

                    if (phoenixBase.Type == GeoSiteType.PhoenixBase && !PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId) && !PhoenixBasesInfested.Contains(phoenixBase.SiteId))
                    {
                        List<GeoUnitDescriptor> capturedUnits = controller.PhoenixFaction.CapturedUnits.ToList();
                        int containmentFacilitiesOutsideBase = CountContainmentFacilities(controller.PhoenixFaction) - phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Where(f => f.Def == containment).Count();

                        int roll = UnityEngine.Random.Range(1, capturedUnits.Count() / 1 + containmentFacilitiesOutsideBase);

                        int hoursToCompleteAttack = 18;

                        bool hasSiren =
                                    phoenixFaction.ContaimentUsage > 0 &&
                                    phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(f => f.GetComponent<PrisonFacilityComponent>() != null) &&
                                    phoenixFaction.CapturedUnits.Any(gud => gud.ClassTag == DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef"));

                        bool hasScylla =
                                    phoenixFaction.ContaimentUsage > 0 &&
                                    phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(f => f.GetComponent<PrisonFacilityComponent>() != null) &&
                                    phoenixFaction.CapturedUnits.Any(gud => gud.ClassTag == DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef"));

                        string textForOlena = TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_EVENT_TEXT");

                        TFTVLogger.Always($"captured units: {capturedUnits.Count()} containment facilities outside base: {containmentFacilitiesOutsideBase}, roll: {roll}");

                        if (phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(f => f.Def == containment) && controller.PhoenixFaction.CapturedUnits.Count() > 0 && roll > 5)
                        {
                            TFTVLogger.Always($"Containment Breached! There are {capturedUnits.Count()} captured aliens, all at this base {containmentFacilitiesOutsideBase > 0}");

                            GeoscapeEventDef olenaBaseDefense = DefCache.GetDef<GeoscapeEventDef>("OlenaBaseDefense");

                            olenaBaseDefense.GeoscapeEventData.Description[0].General.LocalizationKey = "BASEDEFENSE_CONTAINMENTBREACH_TEXT";

                            hoursToCompleteAttack = Math.Max(hoursToCompleteAttack - capturedUnits.Count(), 4);

                            foreach (GeoPhoenixFacility containmentFacility in phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Where(f => f.Def == containment).ToList())
                            {
                                containmentFacility.DamageFacility(UnityEngine.Random.Range(20, 85));
                            }

                            for (int i = 0; i < capturedUnits.Count() / (1 + containmentFacilitiesOutsideBase); i++)
                            {
                                controller.PhoenixFaction.KillCapturedUnit(capturedUnits[i]);
                            }

                            textForOlena = TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_CONTAINMENTBREACH_TEXT"); 
                        }
                    
                        if (hasScylla) 
                        {
                            textForOlena += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_CAPTIVE_SCYLLA_TEXT")}";
                        }
                        else if (hasSiren) 
                        {
                            textForOlena += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_CAPTIVE_SIREN_TEXT")}";
                        }

                       

                        if (controller.PhoenixFaction.Research.HasCompleted("NJ_WallsOfJericho_ResearchDef"))
                        {
                            TFTVLogger.Always($"Player has researched Walls of Jericho! Getting extra 6 hours.");
                            textForOlena += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_NJWALLS_TEXT")}";
                            hoursToCompleteAttack += 6;
                        }

                        DefCache.GetDef<GeoscapeEventDef>("OlenaBaseDefense").GeoscapeEventData.Description[0].General = new LocalizedTextBind(textForOlena, true);

                        AddToTFTVAttackSchedule(phoenixBase, controller, attacker, hoursToCompleteAttack);

                        GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, controller.PhoenixFaction);

                        controller.EventSystem.TriggerGeoscapeEvent("OlenaBaseDefense", context);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            //Patch to add base to TFTV schedule
            [HarmonyPatch(typeof(SiteAttackSchedule), "StartAttack")]
            public static class SiteAttackSchedule_StartAttack_Experiment_patch
            {
                public static void Prefix(SiteAttackSchedule __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"StartAttack invoked for {__instance.Site.LocalizedSiteName}");
                        GeoSite phoenixBase = __instance.Site;
                        GeoLevelController controller = __instance.Site.GeoLevel;
                        PPFactionDef factionDef = __instance.Attacker;

                        if (factionDef == Shared.AlienFactionDef)
                        {
                            StartPandoranAttackOnBase(controller, factionDef, phoenixBase);

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            //Patch to avoid showing attack in log
            [HarmonyPatch(typeof(GeoscapeLog), "OnFactionSiteAttackScheduled")]
            internal static class TFTV_GeoscapeLog_OnFactionSiteAttackScheduled_HideAttackInLogger
            {
                public static bool Prefix()
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

        internal class GeoObjective
        {
            internal static void RemoveBaseDefenseObjective(string baseName)
            {
                try
                {
                    GeoPhoenixFaction geoPhoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<GeoFactionObjective> listOfObjectives = geoPhoenixFaction.Objectives.ToList();

                    foreach (GeoFactionObjective objective in listOfObjectives)
                    {
                        if (objective.GetTitle() == null)
                        {
                            TFTVLogger.Always("objective.GetTitle() returns null!");
                        }
                        else
                        {
                            foreach (GeoSite geoSite in geoPhoenixFaction.Sites)
                            {

                                if (objective.GetTitle().Contains(baseName))
                                {
                                    geoPhoenixFaction.RemoveObjective(objective);

                                }
                            }

                        }

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(DiplomaticGeoFactionObjective), "GetRelatedActors")]
            internal static class TFTV_DiplomaticGeoFactionObjective_GetRelatedActors_ExperimentPatch
            {
                public static void Postfix(DiplomaticGeoFactionObjective __instance, ref IEnumerable<GeoActor> __result, ref List<GeoSite> ____assignedSites)
                {
                    try
                    {
                        //  TFTVLogger.Always($"GetRelatedActorsInvoked objective {__instance.GetTitle()}");

                        GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        foreach (GeoSite geoSite in geoLevelController.PhoenixFaction.Sites)
                        {

                            if (__instance.GetTitle().Contains(geoSite.LocalizedSiteName))
                            {
                                ____assignedSites.Add(geoSite);

                                __result = ____assignedSites;
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

            [HarmonyPatch(typeof(GeoFaction), "get_Objectives")]
            internal static class TFTV_GeoFaction_get_Objectives_ExperimentPatch
            {
                public static void Postfix(GeoFaction __instance, ref IReadOnlyList<GeoFactionObjective> __result)
                {

                    try
                    {
                        List<GeoFactionObjective> reOrderedObjectiveList = new List<GeoFactionObjective>();


                        if (__result != null && GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                        {
                            foreach (GeoFactionObjective objective in __result)
                            {
                                if (objective.GetTitle() != null && objective.GetTitle().Contains("Phoenix base") && objective.GetTitle().Contains("is under attack!"))
                                {
                                    //  TFTVLogger.Always($"Found base under attack objective");
                                    reOrderedObjectiveList.Add(objective);

                                }
                            }
                        }


                        if (__result != null && GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                        {
                            foreach (GeoFactionObjective objective in __result)
                            {
                                string localizationKey = objective.Title?.LocalizationKey;

                                if (localizationKey != null && localizationKey.Contains("VOID_OMEN_TITLE_"))
                                {
                                    //  TFTVLogger.Always($"Found VO objective");
                                    reOrderedObjectiveList.Add(objective);
                                }
                            }
                        }


                        if (reOrderedObjectiveList.Count > 0)
                        {

                            reOrderedObjectiveList.AddRange(__result.Where(o => !reOrderedObjectiveList.Contains(o)));

                            __result = reOrderedObjectiveList;

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleGeoObjectives), "InitObjective")]
            internal class TFTV_UIModuleGeoObjectives_SetObjective_ExperimentPatch
            {

                public static bool Prefix(UIModuleGeoObjectives __instance, GeoObjectiveElementController element,
                    GeoFactionObjective objective, GeoLevelController ____level, IEnumerable<GeoFactionObjective> ____rawObjectives,
                   ref int ____objectiveClickIndex, ref GeoObjectiveElementController ____lastObjectiveClicked)
                {
                    try
                    {


                        string warningMessage = "<color=#FF0000> !!! WARNING !!! A PHOENIX BASE IS UNDER ATTACK</color>";
                        Text objectivesHeaderText = __instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>();
                        string objectivesRegularHeader = new LocalizedTextBind() { LocalizationKey = "KEY_OBJECTIVES" }.Localize();


                        if (PhoenixBasesUnderAttack.Count > 0)
                        {
                            objectivesHeaderText.text += warningMessage;
                        }
                        else
                        {
                            if (__instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>().text.Contains(warningMessage))
                            {
                                objectivesHeaderText.text = objectivesRegularHeader;


                            }
                        }

                        if (objective.GetTitle().Contains("Phoenix base") && objective.GetTitle().Contains("is under attack!"))
                        {

                            //   MethodInfo getObjectiveTooltipMethod = __instance.GetType().GetMethod("GetObjectiveTooltip", BindingFlags.NonPublic | BindingFlags.Instance);

                            if (____level == null)
                            {
                                ____level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            }

                            Color factionColor = brightRed;


                            EventGeoFactionObjective eventGeoFactionObjective = objective as EventGeoFactionObjective;
                            TimeUnit endTime = TimeUnit.Invalid;
                            if (eventGeoFactionObjective != null)
                            {
                                string timerID = ____level.EventSystem.GetEventByID(eventGeoFactionObjective.EventID).GeoscapeEventData.TimerID;
                                if (!string.IsNullOrWhiteSpace(timerID))
                                {
                                    GeoEventTimer timerById = ____level.EventSystem.GetTimerById(timerID);
                                    if (timerById != null)
                                    {
                                        endTime = timerById.EndAt;
                                    }
                                }
                            }


                            bool isLocationObjective = true;//objective.GetRelatedActors().Any((GeoActor x) => objective.IsActorFocusableByObjective(x));
                            LocalizedTextBind objectiveTooltip = new LocalizedTextBind();//(LocalizedTextBind)getObjectiveTooltipMethod.Invoke(__instance, new object[] { objective });
                            element.SetObjective(objective.GetIcon(), factionColor, objective.GetTitle(), ____rawObjectives.IndexOf(objective), isLocationObjective, true, objectiveTooltip, endTime, ____level.Timing);

                            if (____lastObjectiveClicked != element)
                            {
                                ____lastObjectiveClicked = element;
                                ____objectiveClickIndex = -1;
                            }

                            ____objectiveClickIndex++;

                            MethodInfo onElementClickedMethod = __instance.GetType().GetMethod("OnElementClicked", BindingFlags.NonPublic | BindingFlags.Instance);

                            element.OnElementClicked = (Action<GeoObjectiveElementController>)Delegate.CreateDelegate(typeof(Action<GeoObjectiveElementController>), __instance, onElementClickedMethod);

                            return false;

                        }

                        string localizationKey = objective.Title?.LocalizationKey;

                        if (localizationKey != null && localizationKey.Contains("VOID_OMEN_TITLE_"))
                        {
                            MethodInfo onElementClickedMethod = __instance.GetType().GetMethod("OnElementClicked", BindingFlags.NonPublic | BindingFlags.Instance);
                            MethodInfo getObjectiveTooltipMethod = __instance.GetType().GetMethod("GetObjectiveTooltip", BindingFlags.NonPublic | BindingFlags.Instance);


                            if (____level == null)
                            {
                                ____level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            }

                            Color factionColor = purple;
                            EventGeoFactionObjective eventGeoFactionObjective = objective as EventGeoFactionObjective;
                            TimeUnit endTime = TimeUnit.Invalid;
                            if (eventGeoFactionObjective != null)
                            {
                                string timerID = ____level.EventSystem.GetEventByID(eventGeoFactionObjective.EventID).GeoscapeEventData.TimerID;
                                if (!string.IsNullOrWhiteSpace(timerID))
                                {
                                    GeoEventTimer timerById = ____level.EventSystem.GetTimerById(timerID);
                                    if (timerById != null)
                                    {
                                        endTime = timerById.EndAt;
                                    }
                                }
                            }


                            bool isLocationObjective = objective.GetRelatedActors().Any((GeoActor x) => objective.IsActorFocusableByObjective(x));
                            LocalizedTextBind objectiveTooltip = (LocalizedTextBind)getObjectiveTooltipMethod.Invoke(__instance, new object[] { objective });
                            element.SetObjective(objective.GetIcon(), factionColor, objective.GetTitle(), ____rawObjectives.IndexOf(objective), isLocationObjective, false, objectiveTooltip, endTime, ____level.Timing);

                            element.OnElementClicked = (Action<GeoObjectiveElementController>)onElementClickedMethod.Invoke(__instance, new object[] { element });

                            //  __instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>().color = purple;

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

        }

        internal class Briefing
        {

            //Patch to change briefing depending on attack progress
            [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]
            public static class PhoenixBaseDefenseDataBind_ModalShowHandler_DontCancelMission_patch
            {
                public static void Postfix(PhoenixBaseDefenseDataBind __instance, UIModal modal)
                {
                    try
                    {

                        GeoMission geoMission = (GeoMission)modal.Data;

                        if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId))
                        {

                            float timer = (geoMission.Site.ExpiringTimerAt.DateTime - geoMission.Level.Timing.Now.DateTime).Hours;
                            float timeToCompleteAttack = 18;

                            if (PhoenixBasesContainmentBreach.ContainsKey(geoMission.Site.SiteId))
                            {
                                timeToCompleteAttack = PhoenixBasesContainmentBreach[geoMission.Site.SiteId];

                            }

                            float progress = 1f - timer / timeToCompleteAttack;

                            if (timeToCompleteAttack != 18 && progress < 0.3)
                            {

                                progress = 0.3f;

                            }

                            TFTVLogger.Always($"DontCancelMission: attack progress is {progress}");

                            FactionInfoMapping factionInfo = __instance.Resources.GetFactionInfo(geoMission.GetEnemyFaction());
                            LocalizedTextBind text = new LocalizedTextBind();
                            LocalizedTextBind objectivesText = new LocalizedTextBind();
                            Sprite sprite;

                            if (progress < 0.3)
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_TACTICALADVANTAGE" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_SIMPLE" };
                            }
                            else if (progress >= 0.8 || PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint_infestation.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_INFESTATION" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_DOUBLE" };

                            }
                            else
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint_nesting.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_NESTING" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_DOUBLE" };
                            }




                            /*  if (progress == 1 && geoMission.Site.CharactersCount > 0)
                              {
                                  TFTVLogger.Always("The Phoenix Base has people inside! Mission cannot be canceled!");



                              //       UIModuleDeploymentMissionBriefing missionBriefing = (UIModuleDeploymentMissionBriefing)UnityEngine.Object.FindObjectOfType(typeof(UIModuleDeploymentMissionBriefing));

                                 //    missionBriefing.BackButton.gameObject.SetActive(false);






                                  // modal.DisableCancel = true;

                                  List<Button> buttons = UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.name.Equals("UI_Button_Back")).ToList();

                                  TFTVLogger.Always($"There are {buttons.Count} buttons");

                                  if (buttons.Count > 0)
                                  {                              
                                      Button cancel = buttons.First();

                                      if (cancel != null)
                                      {
                                          cancel.gameObject.SetActive(false);
                                      }
                                  }
                              }*/

                            __instance.Background.sprite = sprite;
                            __instance.Warning.SetWarning(text, factionInfo.Name, geoMission.Site.SiteName);
                            Text description = __instance.GetComponentInChildren<ObjectivesController>().Objectives;
                            description.GetComponent<I2.Loc.Localize>().enabled = false;
                            description.text = objectivesText.Localize();



                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

        }

        internal class BaseFacilities
        {

            internal class EnsureCorrectLayout
            {

                //Patch and methods for demolition PhoenixFacilityConfirmationDialogue
                internal static List<Vector2Int> CheckAdjacency(Vector2Int position)
                {
                    try
                    {

                        List<Vector2Int> adjacentTiles = new List<Vector2Int>();

                        if (position.x + 1 <= 4)
                        {
                            adjacentTiles.Add(new Vector2Int() + position + Vector2Int.right);
                            // TFTVLogger.Always($"right from position {position} is {new Vector2Int() + position + Vector2Int.right}");
                        }
                        if (position.x - 1 >= 0)
                        {
                            adjacentTiles.Add(new Vector2Int() + position + Vector2Int.left);
                            // TFTVLogger.Always($"left from position {position} is {new Vector2Int() + position + Vector2Int.left}");
                        }
                        if (position.y + 1 <= 3)
                        {
                            adjacentTiles.Add(new Vector2Int() + position + Vector2Int.up);
                            // TFTVLogger.Always($"down from position {position} is {new Vector2Int() + position + Vector2Int.up}");
                        }
                        if (position.y - 1 >= 0)
                        {
                            adjacentTiles.Add(new Vector2Int() + position + Vector2Int.down);
                            // TFTVLogger.Always($"up from position {position} is {new Vector2Int() + position + Vector2Int.down}");
                        }

                        return adjacentTiles;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static bool CheckConnectionToHangar(GeoPhoenixBaseLayout layout, Vector2Int tileToExclude, GeoPhoenixFacility baseFacility)
                {
                    try
                    {
                        GeoPhoenixFacility hangar = layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);

                        List<Vector2Int> adjacentTiles = CheckAdjacency(baseFacility.GridPosition);


                        foreach (Vector2Int adjacentTile in adjacentTiles)
                        {
                            if (hangar.FacilityTiles.Contains(adjacentTile))
                            {
                                return true;

                            }
                            else if (layout.GetFacilityAtPosition(adjacentTile) != null && adjacentTile != tileToExclude)
                            {
                                //  TFTVLogger.Always($"0, facility: {layout.GetFacilityAtPosition(adjacentTile).Def.name}");

                                foreach (Vector2Int connectedTile in CheckAdjacency(adjacentTile))
                                {
                                    if (hangar.FacilityTiles.Contains(connectedTile))
                                    {
                                        // TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");
                                        return true;

                                    }
                                    else if (layout.GetFacilityAtPosition(connectedTile) != null && connectedTile != tileToExclude)
                                    {
                                        //   TFTVLogger.Always($"1, facility: {layout.GetFacilityAtPosition(connectedTile).Def.name}");

                                        foreach (Vector2Int connectedTile2 in CheckAdjacency(connectedTile))
                                        {
                                            if (hangar.FacilityTiles.Contains(connectedTile2))
                                            {
                                                //  TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");
                                                return true;

                                            }
                                            else if (layout.GetFacilityAtPosition(connectedTile2) != null && connectedTile2 != tileToExclude)
                                            {
                                                //  TFTVLogger.Always($"2, facility: {layout.GetFacilityAtPosition(connectedTile2).Def.name}");

                                                foreach (Vector2Int connectedTile3 in CheckAdjacency(connectedTile2))
                                                {
                                                    if (hangar.FacilityTiles.Contains(connectedTile3))
                                                    {
                                                        //     TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");
                                                        return true;

                                                    }
                                                    else if (layout.GetFacilityAtPosition(connectedTile3) != null && connectedTile3 != tileToExclude)
                                                    {
                                                        //   TFTVLogger.Always($"3, facility: {layout.GetFacilityAtPosition(connectedTile3).Def.name}");
                                                        foreach (Vector2Int connectedTile4 in CheckAdjacency(connectedTile3))
                                                        {
                                                            if (hangar.FacilityTiles.Contains(connectedTile4))
                                                            {
                                                                //   TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");
                                                                return true;

                                                            }
                                                            else if (layout.GetFacilityAtPosition(connectedTile4) != null && connectedTile4 != tileToExclude)
                                                            {
                                                                //  TFTVLogger.Always($"4, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                foreach (Vector2Int connectedTile5 in CheckAdjacency(connectedTile4))
                                                                {
                                                                    if (hangar.FacilityTiles.Contains(connectedTile5))
                                                                    {
                                                                        //   TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");

                                                                        return true;

                                                                    }
                                                                    else if (layout.GetFacilityAtPosition(connectedTile5) != null && connectedTile5 != tileToExclude)
                                                                    {
                                                                        //  TFTVLogger.Always($"5, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                        foreach (Vector2Int connectedTile6 in CheckAdjacency(connectedTile5))
                                                                        {
                                                                            if (hangar.FacilityTiles.Contains(connectedTile6))
                                                                            {
                                                                                //  TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");

                                                                                return true;

                                                                            }
                                                                            else if (layout.GetFacilityAtPosition(connectedTile6) != null && connectedTile6 != tileToExclude)
                                                                            {
                                                                                //  TFTVLogger.Always($"5, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                foreach (Vector2Int connectedTile7 in CheckAdjacency(connectedTile6))
                                                                                {
                                                                                    if (hangar.FacilityTiles.Contains(connectedTile7))
                                                                                    {
                                                                                        //  TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");

                                                                                        return true;

                                                                                    }
                                                                                    else if (layout.GetFacilityAtPosition(connectedTile7) != null && connectedTile7 != tileToExclude)
                                                                                    {
                                                                                        //  TFTVLogger.Always($"5, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                        foreach (Vector2Int connectedTile8 in CheckAdjacency(connectedTile7))
                                                                                        {
                                                                                            if (hangar.FacilityTiles.Contains(connectedTile8))
                                                                                            {
                                                                                                //  TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");

                                                                                                return true;

                                                                                            }
                                                                                            else if (layout.GetFacilityAtPosition(connectedTile8) != null && connectedTile8 != tileToExclude)
                                                                                            {
                                                                                                //  TFTVLogger.Always($"5, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                                foreach (Vector2Int connectedTile9 in CheckAdjacency(connectedTile8))
                                                                                                {
                                                                                                    if (hangar.FacilityTiles.Contains(connectedTile9))
                                                                                                    {
                                                                                                        //  TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {hangar.Def.name}");

                                                                                                        return true;

                                                                                                    }



                                                                                                }

                                                                                            }


                                                                                        }

                                                                                    }


                                                                                }

                                                                            }


                                                                        }

                                                                    }


                                                                }

                                                            }

                                                        }

                                                    }

                                                }

                                            }

                                        }

                                    }

                                }

                            }

                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                [HarmonyPatch(typeof(UIFacilityInfoPopup), "Show")]
                public static class UIFacilityInfoPopup_Show_PreventBadDemolition_patch
                {

                    public static void Postfix(UIFacilityInfoPopup __instance, GeoPhoenixFacility facility)
                    {
                        try
                        {
                            //  TFTVLogger.Always($"facility is {facility.ViewElementDef.name} and it can't be demolished? {facility.CannotDemolish}");

                            Vector2Int positionToExclude = facility.GridPosition;
                            GeoPhoenixFacility hangar = facility.PxBase.Layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);
                            GeoPhoenixBaseLayout layout = facility.PxBase.Layout;

                            foreach (GeoPhoenixFacility baseFacility in layout.Facilities.Where(bf => bf != facility))
                            {
                                if (CheckConnectionToHangar(layout, positionToExclude, baseFacility))
                                {
                                    // TFTVLogger.Always($"{baseFacility.Def.name} at {baseFacility.GridPosition} is connected to Hangar");

                                }
                                else
                                {
                                    // connectionToHangarLoss = true;
                                    //    TFTVLogger.Always($"if {facility.Def.name} at {facility.GridPosition} is demolished, {baseFacility.Def.name} at {baseFacility.GridPosition} will lose connection to Hangar");
                                    __instance.DemolishFacilityBtn.SetInteractable(false);
                                    //  __instance.CanDemolish = false;
                                    __instance.Description.text += "\n\n<b>Can't demolish this facility, as otherwise other facilities will be cut off from the Hangar!</b>";

                                    return;

                                }
                            }
                            if (!facility.CannotDemolish)
                            {

                                __instance.DemolishFacilityBtn.SetInteractable(true);
                            }

                            // TFTVLogger.Always($"{facility.Def.name} is demolished, some facility will lose connection to Hangar is {connectionToHangarLoss}");
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }

            }

            internal class AddressBadLayout
            {
                private static void RemoveFakeFacilities(GeoPhoenixBaseLayout layout)
                {

                    try
                    {
                        PhoenixFacilityDef fakeFacility = DefCache.GetDef<PhoenixFacilityDef>("FakeFacility");

                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.Where(f => f.Def == fakeFacility).ToList();

                        if (geoPhoenixFacilities.Count > 0)
                        {
                            for (int x = 0; x < geoPhoenixFacilities.Count(); x++)
                            {

                                layout.RemoveFacility(geoPhoenixFacilities[x]);

                            }

                            TFTVLogger.Always($"removing fake facilities");
                        }

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static List<GeoPhoenixFacility> SetFacilitiesUnderConstructionToCompleted(GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.Where(f => f.State == GeoPhoenixFacility.FacilityState.UnderContstruction).ToList();

                        PropertyInfo stateProperty = typeof(GeoPhoenixFacility).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);


                        if (geoPhoenixFacilities.Count > 0)
                        {
                            TFTVLogger.Always($"Facilities under construction at base under attack detected; setting to damaged");

                            foreach (GeoPhoenixFacility facility in geoPhoenixFacilities)
                            {
                                stateProperty.SetValue(facility, GeoPhoenixFacility.FacilityState.Damaged);
                            }


                        }

                        return geoPhoenixFacilities;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void RestoreFacilityState(List<GeoPhoenixFacility> geoPhoenixFacilities)
                {
                    try
                    {

                        PropertyInfo stateProperty = typeof(GeoPhoenixFacility).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (geoPhoenixFacilities.Count > 0)
                        {
                            TFTVLogger.Always($"Restoring faciliites");

                            foreach (GeoPhoenixFacility facility in geoPhoenixFacilities)
                            {
                                stateProperty.SetValue(facility, GeoPhoenixFacility.FacilityState.UnderContstruction);
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                [HarmonyPatch(typeof(GeoPhoenixBaseLayout), "ModifyMissionData")]
                public static class TFTV_GeoPhoenixBaseLayout_ModifyMissionData_patch
                {

                    public static void Prefix(GeoPhoenixBaseLayout __instance, GeoMission mission, TacMissionData missionData, out List<GeoPhoenixFacility> __state)
                    {
                        try
                        {
                            __state = SetFacilitiesUnderConstructionToCompleted(__instance);

                            TFTVLogger.Always("ModifyMissionData");
                            if ((PhoenixBasesUnderAttack.ContainsKey(mission.Site.SiteId) || PhoenixBasesInfested.Contains(mission.Site.SiteId)) && !CheckIfBaseLayoutOK(__instance))
                            {
                                TFTVLogger.Always("Bad layout!");
                                FixBadLayout(__instance);
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                    public static void Postfix(GeoPhoenixBaseLayout __instance, GeoMission mission, TacMissionData missionData, in List<GeoPhoenixFacility> __state)
                    {
                        try
                        {
                            RestoreFacilityState(__state);
                            RemoveFakeFacilities(mission.Site.GetComponent<GeoPhoenixBase>().Layout);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                private static bool CheckIfBaseLayoutOK(GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.ToList();
                        GeoPhoenixFacility hangar = layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);

                        GeoPhoenixFacility powerGenerator = geoPhoenixFacilities.FirstOrDefault(f => f.GetComponent<PowerFacilityComponent>() != null);

                        if (powerGenerator != null && powerGenerator.HealthPercentage == 0)
                        {
                            FieldInfo fieldInfo = typeof(GeoPhoenixFacility).GetField("_health", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fieldInfo != null)
                            {
                                fieldInfo.SetValue(powerGenerator, 50);
                                TFTVLogger.Always($"{powerGenerator.HealthPercentage}");
                            }
                        }



                        foreach (GeoPhoenixFacility geoPhoenixFacility in geoPhoenixFacilities)
                        {
                            TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {geoPhoenixFacility.GridPosition}");

                        }

                        if (hangar.GridPosition.y == 0)
                        {
                            TFTVBaseDefenseTactical.TutorialPhoenixBase = true;

                            return true;
                        }


                        if (layout.GetFacilityAtPosition(hangar.GridPosition - new Vector2Int(0, 1)) != null || layout.GetFacilityAtPosition(hangar.GridPosition - new Vector2Int(-1, 1)) != null)
                        {
                            return true;

                        }
                        else
                        {

                            TFTVLogger.Always($"there are no facilities at grid {hangar.GridPosition - new Vector2Int(0, 1)} or {hangar.GridPosition - new Vector2Int(-1, 1)}");
                            return false;

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void FixBadLayout(GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        PhoenixFacilityDef fakeFacility = DefCache.GetDef<PhoenixFacilityDef>("FakeFacility");

                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.ToList();
                        GeoPhoenixFacility hangar = layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);

                        if (layout.CanPlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(0, 1)))
                        {
                            layout.PlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(0, 1), false);
                        }

                        if (layout.CanPlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(-1, 1)))
                        {
                            layout.PlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(-1, 1), false);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



            }

            public static void DisableFacilitiesAtBase(GeoPhoenixBase phoenixBase)
            {
                try
                {
                    foreach (GeoPhoenixFacility facility in phoenixBase.Layout.Facilities)
                    {
                        if (facility.GetComponent<HealFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
                        }
                        if (facility.GetComponent<ExperienceFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
                        }
                        if (facility.GetComponent<VehicleSlotFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
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

        internal class Infestation 
        {
            //Patch to replace infestation mission with base defense mission
            [HarmonyPatch(typeof(GeoSite), "CreatePhoenixBaseInfestationMission")]
            public static class GeoSite_CreatePhoenixBaseInfestationMission_Experiment_patch
            {
                public static bool Prefix(GeoSite __instance)
                {
                    try
                    {
                        if (PhoenixBasesInfested.Contains(__instance.SiteId))
                        {

                            GeoMissionGenerator.ParticipantFilter participantFilter = new GeoMissionGenerator.ParticipantFilter { Faction = __instance.GeoLevel.SharedData.AlienFactionDef, ParticipantType = TacMissionParticipant.Intruder };
                            TacMissionTypeDef mission = __instance.GeoLevel.MissionGenerator.GetRandomMission(__instance.GeoLevel.SharedData.SharedGameTags.BaseDefenseMissionTag, participantFilter);

                            GeoPhoenixBaseInfestationMission activeMission = new GeoPhoenixBaseInfestationMission(mission, __instance);
                            __instance.SetActiveMission(activeMission);

                            FieldInfo basesField = AccessTools.Field(typeof(GeoPhoenixFaction), "_bases");
                            List<GeoPhoenixBase> bases = (List<GeoPhoenixBase>)basesField.GetValue(__instance.GeoLevel.PhoenixFaction);
                            bases.Remove(__instance.GetComponent<GeoPhoenixBase>());

                            __instance.RefreshVisuals();

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

        }

        internal class Debriefing 
        {
            [HarmonyPatch(typeof(HavenDefenceOutcomeDataBind), "ModalShowHandler")]
            public static class HavenDefenceOutcomeDataBind_ModalShowHandler_Experiment_patch
            {
                public static void Postfix(HavenDefenceOutcomeDataBind __instance, UIModal modal)
                {
                    try
                    {
                        // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();


                        TFTVLogger.Always($"Haven Defense mission outcome showing.");

                        GeoMission geoMission = (GeoMission)modal.Data;

                        if (geoMission.GetMissionOutcomeState() == TacFactionState.Defeated)
                        {
                            GeoHavenDefenseMission geoHavenDefenseMission = (GeoHavenDefenseMission)modal.Data;
                            int result = geoHavenDefenseMission.DefenderDeployment - geoHavenDefenseMission.AttackerDeployment;

                            if (result > 0)
                            {
                                TFTVLogger.Always($"Haven Defense mission won.");
                                geoMission.Site.ActiveMission = null;
                                geoMission.Site.RefreshVisuals();

                            }
                            else
                            {
                                TFTVLogger.Always($"Haven Defense mission lost.");
                                geoMission.Site.DestroySite();
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



            [HarmonyPatch(typeof(PhoenixBaseDefenseOutcomeDataBind), "ModalShowHandler")]
            public static class PhoenixBaseDefenseOutcomeDataBind_ModalShowHandler_Experiment_patch
            {
                public static bool Prefix(UIModal modal, ref bool ____shown, ref UIModal ____modal, PhoenixBaseDefenseOutcomeDataBind __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"Defense mission outcome showing.");
                        MissionTagDef pxBaseInfestationTag = DefCache.GetDef<MissionTagDef>("MissionTypeBaseInfestation_MissionTagDef");

                        GeoMission geoMission = (GeoMission)modal.Data;

                        if (!geoMission.MissionDef.MissionTags.Contains(pxBaseInfestationTag))
                        {

                            if (!____shown)
                            {

                                if (____modal == null)
                                {
                                    ____modal = modal;
                                    ____modal.OnModalHide += __instance.ModalHideHandler;
                                }

                                ____shown = true;



                                if (geoMission.GetMissionOutcomeState() == TacFactionState.Won)
                                {
                                    TFTVLogger.Always("Defense mission won");

                                    __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;
                                    __instance.Background.sprite = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                                    __instance.Rewards.SetReward(geoMission.Reward);



                                    if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId))
                                    {
                                        PhoenixBasesUnderAttack.Remove(geoMission.Site.SiteId);

                                        if (PhoenixBasesContainmentBreach.ContainsKey(geoMission.Site.SiteId))
                                        {
                                            PhoenixBasesContainmentBreach.Remove(geoMission.Site.SiteId);
                                        }

                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                    }
                                    if (PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                                    {

                                        PhoenixBasesInfested.Remove(geoMission.Site.SiteId);
                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                        //  geoMission.Level.PhoenixFaction.ActivatePhoenixBase(geoMission.Site, true);



                                        FieldInfo basesField = AccessTools.Field(typeof(GeoPhoenixFaction), "_bases");
                                        List<GeoPhoenixBase> bases = (List<GeoPhoenixBase>)basesField.GetValue(geoMission.Level.PhoenixFaction);
                                        bases.Add(geoMission.Site.GetComponent<GeoPhoenixBase>());
                                        geoMission.Site.RefreshVisuals();


                                    }

                                    geoMission.Site.RefreshVisuals();
                                    TFTVVanillaFixes.CheckFacilitesNotWorking(geoMission.Site.GetComponent<GeoPhoenixBase>());


                                }
                                else
                                {
                                    TFTVLogger.Always("Defense mission lost");

                                    if (!PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                                    {
                                        TFTVLogger.Always($"{geoMission.Site.SiteId} should get added to infested bases");
                                        PhoenixBasesInfested.Add(geoMission.Site.SiteId);
                                    }
                                    if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId))
                                    {
                                        PhoenixBasesUnderAttack.Remove(geoMission.Site.SiteId);

                                        if (PhoenixBasesContainmentBreach.ContainsKey(geoMission.Site.SiteId))
                                        {
                                            PhoenixBasesContainmentBreach.Remove(geoMission.Site.SiteId);
                                        }

                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                    }
                                    __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;
                                    __instance.Background.sprite = Helper.CreateSpriteFromImageFile("base_defense_lost.jpg");
                                    __instance.Rewards.SetReward(geoMission.Reward);

                                    GeoPhoenixBase geoPhoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();
                                    geoMission.Site.Owner = geoMission.Site.GeoLevel.AlienFaction;
                                    geoMission.Site.CreatePhoenixBaseInfestationMission();

                                    Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                                    description.GetComponent<I2.Loc.Localize>().enabled = false;
                                    LocalizedTextBind descriptionText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TEXT" };
                                    description.text = descriptionText.Localize();
                                    Text title = __instance.TopBar.Title;
                                    title.GetComponent<I2.Loc.Localize>().enabled = false;
                                    title.text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TITLE" }.Localize();

                                    if (geoMission.Site.CharactersCount > 0)
                                    {
                                        List<GeoSite> phoenixBases = new List<GeoSite>();

                                        foreach (GeoPhoenixBase phoenixBase in geoMission.Site.GeoLevel.PhoenixFaction.Bases)
                                        {
                                            // TFTVLogger.Always($"Phoenix base is {phoenixBase.Site.LocalizedSiteName}");
                                            phoenixBases.Add(phoenixBase.Site);

                                        }

                                        phoenixBases.Remove(geoMission.Site);

                                        phoenixBases = phoenixBases.OrderBy(b => (b.WorldPosition - geoMission.Site.WorldPosition).magnitude).ToList();

                                        List<GeoCharacter> charactersToMove = new List<GeoCharacter>();

                                        foreach (GeoCharacter character in geoMission.Site.GetAllCharacters())
                                        {
                                            charactersToMove.Add(character);
                                            // TFTVLogger.Always($"character to move {character.DisplayName}");

                                        }

                                        foreach (GeoCharacter geoCharacter in charactersToMove)
                                        {
                                            geoMission.Site.RemoveCharacter(geoCharacter);
                                            phoenixBases.First().AddCharacter(geoCharacter);
                                            //  TFTVLogger.Always($"{geoCharacter.DisplayName} moved to {phoenixBases.First().LocalizedSiteName}");
                                            //  description.text += $" {geoCharacter.DisplayName} escaped to {phoenixBases.First().LocalizedSiteName}.";
                                        }

                                        for (int x = 0; x < charactersToMove.Count; x++)
                                        {
                                            if (x < charactersToMove.Count - 2)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName},";
                                            }
                                            else if (x == charactersToMove.Count - 2)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName} and";
                                            }
                                            else if (x == charactersToMove.Count - 1)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName} ";
                                            }

                                        }

                                        description.text += $"escaped to {phoenixBases.First().LocalizedSiteName}.";
                                    }

                                }
                            }
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



        }

        internal class Visuals 
        {
            //Patch to create/manage visuals of attack on Phoenix base
            public static GeoUpdatedableMissionVisualsController GetBaseAttackVisuals(GeoSite phoenixBase)
            {
                try
                {
                    GeoUpdatedableMissionVisualsController missionVisualsController = null;


                    // Get the FieldInfo object representing the _visuals field
                    FieldInfo visualsField = typeof(GeoSite).GetField("_visuals", BindingFlags.Instance | BindingFlags.NonPublic);

                    // Get the value of the _visuals field using reflection
                    GeoSiteVisualsController visuals = (GeoSiteVisualsController)visualsField.GetValue(phoenixBase);


                    GeoUpdatedableMissionVisualsController[] visualsControllers = visuals.VisualsContainer.GetComponentsInChildren<GeoUpdatedableMissionVisualsController>();
                    missionVisualsController = visualsControllers.FirstOrDefault(vc => vc.gameObject.name == "kludge");

                    return missionVisualsController;
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            

            public static void RefreshBaseDefenseVisuals(GeoSiteVisualsController geoSiteVisualsController, GeoSite site)
            {
                try
                {
                    if (KludgeSite != null)
                    {
                        KludgeSite = null;

                    }

                    if (site.Type == GeoSiteType.PhoenixBase)
                    {
                        GeoUpdatedableMissionVisualsController missionVisualsController = GetBaseAttackVisuals(site);

                        if (missionVisualsController != null || PhoenixBasesUnderAttack.ContainsKey(site.SiteId))
                        {

                            if (PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController == null)
                            {
                                geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(true);
                                Color baseAttackTrackerColor = new Color32(192, 32, 32, 255);
                                geoSiteVisualsController.BaseIDText.gameObject.SetActive(false);

                                site.ExpiringTimerAt = TimeUnit.FromSeconds((float)(3600 * PhoenixBasesUnderAttack[site.SiteId].First().Value));
                                //  TFTVLogger.Always($"saved time value is {TimeUnit.FromHours(PhoenixBasesUnderAttack[site.SiteId].First().Value)}");

                                GeoUpdatedableMissionVisualsController missionPrefab = GeoSiteVisualsDefs.Instance.HavenDefenseVisualsPrefab;
                                missionVisualsController = UnityEngine.Object.Instantiate(missionPrefab, geoSiteVisualsController.VisualsContainer);
                                missionVisualsController.name = "kludge";

                                float totalTimeForAttack = 18;

                                if (PhoenixBasesContainmentBreach.ContainsKey(site.SiteId))
                                {
                                    totalTimeForAttack = PhoenixBasesContainmentBreach[site.SiteId];

                                }

                                float progress = 1f - (site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour) / totalTimeForAttack;

                                TFTVLogger.Always($"timeToCompleteAttack is {site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour}, total time for attack is {totalTimeForAttack} progress is {progress}");

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);
                                progressRenderer.gameObject.SetChildrenVisibility(true);

                                //  TFTVLogger.Always($"");

                                IGeoFactionMissionParticipant factionMissionParticipant = site.ActiveMission.GetEnemyFaction();

                                //   TFTVLogger.Always($"2");

                                IGeoFactionMissionParticipant owner = site.Owner;


                                //  TFTVLogger.Always($"3");
                                progressRenderer.material.SetColor("_SecondColor", factionMissionParticipant.ParticipantViewDef.FactionColor);
                                progressRenderer.material.SetColor("_FirstColor", site.GeoLevel.PhoenixFaction.FactionDef.FactionColor);
                                progressRenderer.material.SetFloat("_Progress", progress);
                                // TFTVBaseDefenseTactical.AttackProgress = progress;

                            }
                            else if (!PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {
                                TFTVLogger.Always("missionVisualsController found, though it's not active");
                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);


                                geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                                geoSiteVisualsController.BaseIDText.gameObject.SetActive(true);
                                missionVisualsController.gameObject.SetActive(false);

                            }
                            else if (PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);

                                float totalTimeForAttack = 18;

                                if (PhoenixBasesContainmentBreach.ContainsKey(site.SiteId))
                                {
                                    totalTimeForAttack = PhoenixBasesContainmentBreach[site.SiteId];

                                }


                                float progress = 1f - (site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour) / totalTimeForAttack;
                                progressRenderer.material.SetFloat("_Progress", progress);

                                if (progress == 1)
                                {


                                    KludgeSite = site;
                                    TFTVLogger.Always("Progress 1 reached!");
                                    MethodInfo registerMission = typeof(GeoSite).GetMethod("RegisterMission", BindingFlags.NonPublic | BindingFlags.Instance);
                                    registerMission.Invoke(site, new object[] { site.ActiveMission });


                                    geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                                    geoSiteVisualsController.BaseIDText.gameObject.SetActive(true);
                                    missionVisualsController.gameObject.SetActive(false);


                                }
                                else if (progress >= 0.3 || totalTimeForAttack < 12)
                                {
                                    // TFTVLogger.Always($"Deactivating facilities at base {site.LocalizedSiteName}");

                                    BaseFacilities.DisableFacilitiesAtBase(site.GetComponent<GeoPhoenixBase>());
                                }


                                // TFTVBaseDefenseTactical.AttackProgress = progress;
                            }
                        }
                        if (site.ExpiringTimerAt != null && !PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController == null)
                        {
                            geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteContextualMenu), "SetMenuItems")]
        public static class UIModuleSiteContextualMenu_SetMenuItems_patch
        {
            public static void Postfix(GeoSite site, List<GeoAbility> rawAbilities, Vector3 position, UIModuleSiteContextualMenu __instance)
            {
                try
                {
                    if (site.GetComponent<GeoPhoenixBase>() != null && site.ActiveMission != null && site.CharactersCount > 0 &&
                        (site.Vehicles.Count() == 0 || !site.Vehicles.Any(v => v.GetCharacterCount() > 0)))
                    {

                        FieldInfo fieldInfoListSiteContextualMenuItem = typeof(UIModuleSiteContextualMenu).GetField("_menuItems", BindingFlags.NonPublic | BindingFlags.Instance);
                        List<SiteContextualMenuItem> menuItems = fieldInfoListSiteContextualMenuItem.GetValue(__instance) as List<SiteContextualMenuItem>;

                        foreach (SiteContextualMenuItem menuItem in menuItems)
                        {
                            // TFTVLogger.Always($"menu item: {menuItem?.ItemText?.text}");
                            if (menuItem.ItemText.text == DefCache.GetDef<EnterBaseAbilityDef>("EnterBaseAbilityDef").ViewElementDef.DisplayName1.Localize())
                            {
                                menuItem.ItemText.text = TFTVCommonMethods.ConvertKeyToString("KEY_DEPLOY_BASE_DEFENSE_TEXT");
                                menuItem.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_DEPLOY_BASE_DEFENSE_TIP");
                                // TFTVLogger.Always($"menu item: {menuItem?.ItemText?.text}");
                            }
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

        [HarmonyPatch(typeof(EnterBaseAbility), "ActivateInternal")]
        public static class EnterBaseAbility_ActivateInternal_patch
        {

            public static bool Prefix(EnterBaseAbility __instance, GeoAbilityTarget target)
            {
                try
                {
                    TFTVLogger.Always($"ActivateInternalfor ability {__instance.GeoscapeAbilityDef.name}");

                    if (target.Actor is GeoSite site && site.ActiveMission != null && site.CharactersCount > 0
                        &&
                        (site.Vehicles.Count() == 0 || !site.Vehicles.Any(v => v.GetCharacterCount() > 0)))
                    {
                        TFTVLogger.Always($"Deploying Base Defense");
                        GeoSite geoSite = (GeoSite)target.Actor;
                        //  GeoVehicle initialContainer = (GeoVehicle)base.Actor;


                        __instance.GeoLevel.View.LaunchMission(geoSite.ActiveMission);
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

        //These patches cancel the listeners added by the Register Mission method used in the TFTVAAExperiment.cs to force the base defense
        //mission when timer expires
        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionCancel_Patch
        {
            [HarmonyPatch("add_OnMissionCancel")]
            [HarmonyPrefix]
            public static bool OnMissionCancelAdded(GeoMission __instance, Action<GeoMission> value)
            {
                try
                {

                    if (KludgeSite == __instance.Site) //&& TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionCancel invoked");

                        return false;
                        // ____missionCancelled = false;
                    }
                    //  }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }  // Your patch code here
            }
        }

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionActivated_Patch
        {
            [HarmonyPatch("add_OnMissionActivated")]
            [HarmonyPrefix]
            public static bool OnMissionActivatedAdded(GeoMission __instance, Action<GeoMission, PlayTacticalGameLevelResult> value)
            {
                try
                {
                    if (KludgeSite == __instance.Site)// && TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionActivated invoked");

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

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionPreApplyResult_Patch
        {
            [HarmonyPatch("add_OnMissionPreApplyResult")]
            [HarmonyPrefix]
            public static bool OnMissionPreApplyResultAdded(GeoMission __instance, Action<GeoMission> value)
            {
                try
                {


                    if (KludgeSite == __instance.Site) //&& TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionPreApplyResult invoked");

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

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionCompleted_Patch
        {
            [HarmonyPatch("add_OnMissionCompleted")]
            [HarmonyPrefix]
            public static bool OnMissionCompletedAdded(GeoMission __instance, Action<GeoMission, GeoFactionReward> value)
            {
                try
                {
                    if (KludgeSite == __instance.Site)
                    {
                        TFTVLogger.Always("add_OnMissionCompleted invoked");
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


        //Patches to close deployment screen if no characters present
        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
        public static class TFTV_UIStateRosterDeployment_EnterState_BaseDefenseGeo_patch
        {
            public static void Postfix(UIStateRosterDeployment __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    UIModuleActorCycle uIModuleActorCycle = controller.View.GeoscapeModules.ActorCycleModule;
                    UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                    if (uIModuleActorCycle.CurrentCharacter == null)
                    {
                        Type uiStateType = typeof(UIStateRosterDeployment);
                        MethodInfo exitStateMethod = uiStateType.GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance);
                        exitStateMethod.Invoke(__instance, null);
                        TFTVLogger.Always("OnCancel exitState");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoMission), "get_SkipDeploymentSelection")]
        public static class GeoMission_get_SkipDeploymentSelection_BaseDefenseProgressOne_patch
        {
            public static void Postfix(GeoMission __instance)
            {
                try
                {
                    MissionTagDef pxBaseDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");

                    if (__instance.MissionDef.MissionTags.Contains(pxBaseDefenseTag))
                    {
                        GeoSite geoSite = __instance.Site;

                        float timer = (__instance.Site.ExpiringTimerAt.DateTime - __instance.Level.Timing.Now.DateTime).Hours;
                        float timeToCompleteAttack = 18;
                        float progress = 1f - timer / timeToCompleteAttack;

                        TFTVLogger.Always($"attack progress is {progress}");

                        List<IGeoCharacterContainer> characterContainers = __instance.GetDeploymentSources(__instance.Site.Owner);

                        IEnumerable<GeoCharacter> deployment = characterContainers.SelectMany((IGeoCharacterContainer s) => s.GetAllCharacters());

                        if (deployment.Count() == 0 && progress == 1)
                        {
                            __instance.Cancel();
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

        [HarmonyPatch(typeof(UIModal), "Cancel")]
        public static class UIModal_Cancel_BaseInfestation_patch
        {
            public static bool Prefix(UIModal __instance, DialogCallback ____handler)
            {
                try
                {

                    //TFTVLogger.Always($"Showing modal {__instance.name}");

                    if (__instance.Data is GeoMission geoMission && __instance.name.Contains("Brief"))
                    {
                        //  TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission.GetMissionOutcomeState()}");
                        GeoSite geoSite = geoMission.Site;
                        //  TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method invoked.");
                        if (PhoenixBasesInfested.Contains(geoSite.SiteId))
                        {
                            TFTVLogger.Always("GeoPhoenixBaseDefense(Infestation)Mission Cancel method canceled.");

                            ____handler(ModalResult.Close);

                            return false;
                        }
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

        [HarmonyPatch(typeof(GeoMission), "Cancel")]
        public static class GeoMission_Cancel_BaseInfestation_patch
        {
            public static bool Prefix(GeoMission __instance)
            {
                try
                {
                    GeoSite geoSite = __instance.Site;


                    //  TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method invoked.");
                    if (PhoenixBasesInfested.Contains(geoSite.SiteId))
                    {
                        TFTVLogger.Always("GeoPhoenixBaseDefense(Infestation)Mission Cancel method canceled.");

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

        //Patch not to remove base defense mission when it is canceled

        [HarmonyPatch(typeof(GeoPhoenixBaseDefenseMission), "Cancel")]
        public static class GeoPhoenixBaseDefenseMission_Cancel_Experiment_patch
        {
            public static bool Prefix(GeoPhoenixBaseDefenseMission __instance)
            {
                try
                {

                    GeoSite phoenixBase = __instance.Site;

                    float timer = (__instance.Site.ExpiringTimerAt.DateTime - __instance.Level.Timing.Now.DateTime).Hours;
                    float timeToCompleteAttack = 18;

                    if (PhoenixBasesContainmentBreach.ContainsKey(phoenixBase.SiteId))
                    {
                        timeToCompleteAttack = PhoenixBasesContainmentBreach[phoenixBase.SiteId];

                    }


                    float progress = 1f - timer / timeToCompleteAttack;

                    TFTVLogger.Always($"GeoPhoenixBaseDefenseMission Cancel method invoked. Progress is {progress}");

                    if (PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId) && progress < 1)
                    {
                        TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method canceled.");

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


        //Patches to prevent recruiting to a base under attack
        [HarmonyPatch(typeof(RecruitsBaseDeployElementController), "SetBaseElement")]
        public static class RecruitsBaseDeployElementController_SetBasesForDeployment_BaseDefense_patch
        {
            public static void Postfix(bool isBaseUnderAttack, RecruitsBaseDeployElementController __instance)
            {
                try
                {
                    if (isBaseUnderAttack)
                    {
                        __instance.BaseButton.SetInteractable(false);
                    }
                    else if (!__instance.BaseButton.IsInteractable())
                    {
                        __instance.BaseButton.SetInteractable(true);

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(RecruitsBaseDeployData), "get_IsOperational")]
        public static class RecruitsBaseDeployData_SetBasesForDeployment_BaseDefense_patch
        {
            public static void Postfix(ref bool __result, RecruitsBaseDeployData __instance)
            {
                try
                {

                    if (PhoenixBasesUnderAttack.ContainsKey(__instance.PhoenixBase.Site.SiteId)
                        || PhoenixBasesInfested.Contains(__instance.PhoenixBase.Site.SiteId))
                    {
                        __result = false;

                    }
                    else
                    {
                        __result = true;

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGeoAssetDeployment), "ShowDeployDialog")]
        public static class UIModuleGeoAssetDeployment_ShowDeployDialog_BaseDefense_patch
        {


            public static void Prefix(UIModuleGeoAssetDeployment __instance, ref IEnumerable<GeoSite> sites)
            {
                try
                {
                    // Create a modified collection of sites without the ones under attack
                    List<GeoSite> modifiedSites = new List<GeoSite>(sites);
                    modifiedSites.RemoveAll(site => PhoenixBasesUnderAttack.ContainsKey(site.SiteId));
                    modifiedSites.RemoveAll(site => PhoenixBasesInfested.Contains(site.SiteId));

                    sites = modifiedSites;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }

        //Patch to close mission briefings for missions not in play (the Vanilla double mission bug)
        //and also to close Deploy screen if there are no characters to deploy to mission
        [HarmonyPatch(typeof(UIModal), "Show")]
        public static class UIModal_Show_DoubleMissionVanillaFixAndBaseDefense_patch
        {

            public static void Postfix(UIModal __instance, object data)
            {
                try
                {
                    TFTVLogger.Always($"Showing modal {__instance.name}");
                    if (data is GeoMission geoMission && __instance.name.Contains("Brief"))
                    {
                        TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission.GetMissionOutcomeState()}");
                        GeoSite geoSite = geoMission.Site;

                        if (geoMission.GetMissionOutcomeState() != TacFactionState.Playing)
                        {

                            __instance.Close();
                            TFTVLogger.Always("Closing modal because mission is not in play");
                        }

                        if (PhoenixBasesUnderAttack.ContainsKey(geoSite.SiteId) && geoSite.CharactersCount == 0)
                        {
                            float timer = (geoMission.Site.ExpiringTimerAt.DateTime - geoMission.Level.Timing.Now.DateTime).Hours;
                            float timeToCompleteAttack = 18;

                            if (PhoenixBasesContainmentBreach.ContainsKey(geoSite.SiteId))
                            {
                                timeToCompleteAttack = PhoenixBasesContainmentBreach[geoSite.SiteId];

                            }

                            float progress = 1f - timer / timeToCompleteAttack;

                            List<IGeoCharacterContainer> characterContainers = geoMission.GetDeploymentSources(geoSite.Owner);

                            IEnumerable<GeoCharacter> deployment = characterContainers.SelectMany((IGeoCharacterContainer s) => s.GetAllCharacters());


                            if (deployment.Count() == 0 && progress < 1)
                            {
                                __instance.Close();

                                TFTVLogger.Always("Closing modal because no troops to deploy in mission.");
                            }




                        }

                        MissionTypeTagDef ancientSiteDefense = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef");
                        if (geoSite.ActiveMission != null && (geoSite.Type == GeoSiteType.AncientHarvest || geoSite.Type == GeoSiteType.AncientRefinery) && geoSite.ActiveMission.MissionDef.MissionTags.Contains(ancientSiteDefense))
                        {
                            geoSite.ActiveMission.Launch(new GeoSquad() { });
                        }

                        /*  float timer = (geoMission.Site.ExpiringTimerAt.DateTime - geoMission.Level.Timing.Now.DateTime).Hours;
                          float timeToCompleteAttack = 18;
                          float progress = 1f - timer / timeToCompleteAttack;

                          if (PhoenixBasesUnderAttack.ContainsKey(geoSite.SiteId) && progress == 1 && geoMission.Site.CharactersCount > 0)
                              {
                                  TFTVLogger.Always("UIModal_Show_DoubleMissionVanillaFixAndBaseDefense The Phoenix Base has people inside! Mission cannot be canceled!");



                                   //  UIModuleDeploymentMissionBriefing missionBriefing = (UIModuleDeploymentMissionBriefing)UnityEngine.Object.FindObjectOfType(typeof(UIModuleDeploymentMissionBriefing));

                              //       missionBriefing.BackButton.gameObject.SetActive(false);






                               //  __instance.DisableCancel = true;

                                  List<Button> buttons = UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.name.Equals("UI_Button_Back")).ToList();

                                  TFTVLogger.Always($"There are {buttons.Count} buttons");

                                  if (buttons.Count > 0)
                                  {
                                      Button cancel = buttons.First();

                                      if (cancel != null)
                                      {
                                          cancel.gameObject.SetActive(false);
                                      }
                                  }
                              }
                          }*/
                        /*  float timer = (geoMission.Site.ExpiringTimerAt.DateTime - geoMission.Level.Timing.Now.DateTime).Hours;
                          float timeToCompleteAttack = 18;
                          float progress = 1f - timer / timeToCompleteAttack;


                          TFTVLogger.Always($"attack progress is {progress}");

                          MissionTagDef pxBaseInfestationTag = DefCache.GetDef<MissionTagDef>("MissionTypeBaseInfestation_MissionTagDef");

                          if (!geoMission.MissionDef.MissionTags.Contains(pxBaseInfestationTag))
                          {

                              if (progress == 1 && geoMission.Site.CharactersCount > 0)
                              {
                                  TFTVLogger.Always("The Phoenix Base has people inside! Mission cannot be canceled!");

                                  __instance.DisableCancel = true;


                              }

                          }*/


                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Patch to create base defense mission even when no characters present at the base
        [HarmonyPatch(typeof(GeoPhoenixBase), "get_CanCreateBaseDefense")]

        public static class GeoPhoenixBase_get_CanCreateBaseDefense_Patch
        {
            public static void Postfix(GeoPhoenixBase __instance, ref bool __result)
            {
                try
                {
                    TFTVLogger.Always("get_CanCreateBaseDefense invoked");

                    if (PhoenixBasesUnderAttack.ContainsKey(__instance.Site.SiteId))
                    {
                        __result = true;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/


        /*  [HarmonyPatch(typeof(UIModal), "Show")]
          public static class PhoenixBaseDefenseDataBind_ModalShowHandler_Experiment_patch
          {
              private static bool previousConditionFulfilled = false;
              private static Vector3 originalCancelPos = new Vector3();
              private static string originalCancelText = "";
              private static string newStartMissionName = "StartButtonHidden";
              private static string newCancelName = "CancelButtonChanged";

              public static void Postfix(UIModal __instance, object data)
              {

                  try
                  {

                      if (data is GeoMission geoMission && __instance.name.Contains("Brief_PhoenixBaseDefense"))
                      {

                          GeoPhoenixBase phoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();

                          if (phoenixBase.SoldiersInBase.Count() == 0 && !geoMission.Site.Vehicles.Any(v => v.Soldiers.Count() > 0))
                          {

                              TFTVLogger.Always("");

                              PhoenixGeneralButton startMission = FindButtonByText("START MISSION");
                              PhoenixGeneralButton cancel = FindButtonByText("CANCEL");

                              if (startMission != null)
                              {
                                  startMission.gameObject.SetActive(false);
                                  startMission.gameObject.name = newStartMissionName;
                              }

                              if (cancel != null)
                              {
                                  originalCancelPos = cancel.gameObject.transform.position;
                                  originalCancelText = cancel.gameObject.GetComponentInChildren<Text>().text;
                                  cancel.gameObject.transform.position = startMission.gameObject.transform.position;
                                  cancel.gameObject.GetComponentInChildren<Text>().text = "Alrighty, will be back later";
                                  cancel.gameObject.name = newCancelName;
                                  previousConditionFulfilled = true;
                              }
                          }
                          else if (previousConditionFulfilled)
                          {
                              TFTVLogger.Always(", past previousConditionFulfilled");

                              PhoenixGeneralButton startMission = FindButtonByName(newStartMissionName);
                              PhoenixGeneralButton cancel = FindButtonByName(newCancelName);

                              TFTVLogger.Always($"buttons");

                              if (startMission != null)
                              {
                                  startMission.gameObject.SetActive(true);
                              }

                              if (cancel != null)
                              {
                                  cancel.gameObject.transform.position = originalCancelPos;
                                  cancel.gameObject.GetComponentInChildren<Text>().text = originalCancelText;
                              }

                              previousConditionFulfilled = false;
                          }
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }


              }

          }*/

        internal static PhoenixGeneralButton FindButtonByName(string name)
        {
            try
            {

                PhoenixGeneralButton result = null;

                PhoenixGeneralButton[] buttons = UnityEngine.Object.FindObjectsOfType<PhoenixGeneralButton>();

                foreach (PhoenixGeneralButton button in buttons)
                {

                    if (button.gameObject.name == name)
                    {

                        result = button;


                    }
                }

                return result;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }
        internal static PhoenixGeneralButton FindButtonByText(string ButtonText)
        {
            try
            {
                PhoenixGeneralButton result = null;

                PhoenixGeneralButton[] buttons = UnityEngine.Object.FindObjectsOfType<PhoenixGeneralButton>();

                foreach (PhoenixGeneralButton button in buttons)
                {
                    Text text = button.gameObject.GetComponentInChildren<Text>();

                    if (text != null)
                    {
                        TFTVLogger.Always($"text is {text.text}");
                        if (text.text == ButtonText)
                        {
                            // text.text = "SOMETHING NEW";
                            //button.gameObject.SetActive(false);
                            result = button;
                        }

                    }

                    // Do something with the PhoenixGeneralButton component
                }

                return result;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        /*  [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]
          public static class PhoenixBaseDefenseDataBind_ModalShowHandler_Experiment_patch
          {
              public static void Postfix(UIModal modal, PhoenixBaseDefenseDataBind __instance)
              {
                  try
                  {
                      GeoMission geoMission = (GeoMission)modal.Data;
                      GeoPhoenixBase phoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();

                      if (phoenixBase.SoldiersInBase.Count()==0 && !geoMission.Site.Vehicles.Any(v=>v.Soldiers.Count()>0))
                      {
                          TFTVLogger.Always("");

                          Button startMission = UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.name.Equals("UIMainButton_HPriority")).First();

                          Button cancel = UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.name.Equals("UI_Button_Back")).First();

                          cancel.gameObject.transform.position = startMission.gameObject.transform.position;
                          startMission.gameObject.SetActive(false);
                          cancel.gameObject.GetComponentInChildren<Text>().text = "Alrighty, will be back later";
                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/


        /*  foreach (Button button in buttons)
          {
              TFTVLogger.Always($"{button.name}");

              Component[] components = button.gameObject.GetComponentsInChildren<PhoenixGeneralButton>();

              foreach (Component component in components)
              {
                  PhoenixGeneralButton pgButton = component as PhoenixGeneralButton;
                  if (pgButton != null)
                  {
                      TFTVLogger.Always($"Found PhoenixGeneralButton component in {button.name}");

                      Text text = pgButton.gameObject.GetComponentInChildren<Text>();

                      if (text != null)
                      {
                          TFTVLogger.Always($"text is {text.text}");
                          if (text.text == "START MISSION")
                          {
                             // text.text = "SOMETHING NEW";
                              //button.gameObject.SetActive(false);
                              startMission = button;
                          }
                          if (text.text == "CANCEL")
                          {
                              //   text.text = "SOMETHING NEW";
                              //button.gameObject.SetActive(false);
                              cancel = button;
                          }
                      }

                      // Do something with the PhoenixGeneralButton component
                  }
              }
          }*/



    }
}
