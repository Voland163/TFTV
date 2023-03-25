using Base;
using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
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
        public static List<int> PhoenixBasesInfested = new List<int>();

        //  public static List<GeoSite> InstantiatedVisuales = new List<GeoSite>();
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


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
                                __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;
                                __instance.Background.sprite = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                                __instance.Rewards.SetReward(geoMission.Reward);
                                PhoenixBasesUnderAttack.Remove(geoMission.Site.SiteId);
                                geoMission.Site.RefreshVisuals();

                            }
                            else
                            {
                                __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;

                                __instance.Background.sprite = Helper.CreateSpriteFromImageFile("base_defense_lost.jpg");

                                __instance.Rewards.SetReward(geoMission.Reward);

                                GeoPhoenixBase geoPhoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();
                                geoMission.Site.Owner = geoMission.Site.GeoLevel.AlienFaction;
                                geoMission.Site.CreatePhoenixBaseInfestationMission();
                                PhoenixBasesInfested.Add(geoMission.Site.SiteId);
                                Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                                description.GetComponent<I2.Loc.Localize>().enabled = false;
                                LocalizedTextBind descriptionText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TEXT" };
                                description.text = descriptionText.Localize();
                                Text title = __instance.TopBar.Title;
                                title.GetComponent<I2.Loc.Localize>().enabled = false;
                                title.text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TITLE" }.Localize();

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


        public static void AddToTFTVAttackSchedule(GeoSite phoenixBase, GeoLevelController controller, GeoFaction attacker)
        {
            try
            {
                TimeUnit timeForAttack = TimeUnit.FromHours(18);
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

                // phoenixBase.RefreshVisuals();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }




        /*  public static TimeUnit RetrieveTimeUnitFromSchedule(int siteId)
          {
              try
              {
                  TimeUnit timer = TimeUnit.FromHours(PhoenixBasesUnderAttack[siteId].First().Value);

                  return timer;
              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
                  throw;
              }
          }*/

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
                    GeoFaction attacker = controller.GetFaction(factionDef);

                    if (phoenixBase.Type == GeoSiteType.PhoenixBase && !PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId) && !PhoenixBasesInfested.Contains(phoenixBase.SiteId))
                    {
                        AddToTFTVAttackSchedule(phoenixBase, controller, attacker);
                        GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, controller.PhoenixFaction);

                        controller.EventSystem.TriggerGeoscapeEvent("OlenaBaseDefense", context);
                    }

                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



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

        internal static GeoSite KludgeSite = null;

        [HarmonyPatch(typeof(GeoSiteVisualsController), "RefreshSiteVisuals")]

        public static class GeoSiteVisualsController_RefreshSiteVisuals_Experiment_patch
        {
            public static void Postfix(GeoSiteVisualsController __instance, GeoSite site)
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
                                __instance.TimerController.gameObject.SetChildrenVisibility(true);
                                Color baseAttackTrackerColor = new Color32(192, 32, 32, 255);

                                __instance.BaseIDText.gameObject.SetActive(false);


                                site.ExpiringTimerAt = TimeUnit.FromSeconds((float)(3600 * PhoenixBasesUnderAttack[site.SiteId].First().Value));
                                //  TFTVLogger.Always($"saved time value is {TimeUnit.FromHours(PhoenixBasesUnderAttack[site.SiteId].First().Value)}");

                                GeoUpdatedableMissionVisualsController missionPrefab = GeoSiteVisualsDefs.Instance.HavenDefenseVisualsPrefab;
                                missionVisualsController = UnityEngine.Object.Instantiate(missionPrefab, __instance.VisualsContainer);
                                missionVisualsController.name = "kludge";

                                float totalTimeForAttack = 18;

                                float progress = 1f - (site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour) / totalTimeForAttack;

                                TFTVLogger.Always($"timeToCompleteAttack is {site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour}, total time for attack is {totalTimeForAttack} progress is {progress}");

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);
                                progressRenderer.gameObject.SetChildrenVisibility(true);


                                IGeoFactionMissionParticipant factionMissionParticipant = site.ActiveMission.GetEnemyFaction();
                                IGeoFactionMissionParticipant owner = site.Owner;
                                progressRenderer.material.SetColor("_SecondColor", factionMissionParticipant.ParticipantViewDef.FactionColor);
                                progressRenderer.material.SetColor("_FirstColor", site.GeoLevel.PhoenixFaction.FactionDef.FactionColor);
                                progressRenderer.material.SetFloat("_Progress", progress);
                                TFTVBaseDefenseTactical.AttackProgress = progress;

                            }
                            else if (!PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {
                                TFTVLogger.Always("missionVisualsController found, though it's not active");
                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);


                                __instance.TimerController.gameObject.SetChildrenVisibility(false);
                                __instance.BaseIDText.gameObject.SetActive(true);
                                missionVisualsController.gameObject.SetActive(false);

                            }
                            else if (PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);

                                float totalTimeForAttack = 18;
                                float progress = 1f - (site.ExpiringTimerAt.DateTime.Hour - site.GeoLevel.Timing.Now.DateTime.Hour) / totalTimeForAttack;
                                progressRenderer.material.SetFloat("_Progress", progress);

                                if (progress == 1)
                                {
                                    KludgeSite = site;
                                    PhoenixBasesUnderAttack.Remove(site.SiteId);
                                    TFTVLogger.Always("Progress 1 reached!");
                                    MethodInfo registerMission = typeof(GeoSite).GetMethod("RegisterMission", BindingFlags.NonPublic | BindingFlags.Instance);
                                    registerMission.Invoke(site, new object[] { site.ActiveMission });
                                    __instance.TimerController.gameObject.SetChildrenVisibility(false);
                                    __instance.BaseIDText.gameObject.SetActive(true);
                                    missionVisualsController.gameObject.SetActive(false);


                                }
                                TFTVBaseDefenseTactical.AttackProgress = progress;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]
        public static class PhoenixBaseDefenseDataBind_ModalShowHandler_Experiment_patch
        {
            public static void Postfix(PhoenixBaseDefenseDataBind __instance, UIModal modal)
            {
                try
                {
                    GeoMission geoMission = (GeoMission)modal.Data;

                    MissionTagDef pxBaseInfestationTag = DefCache.GetDef<MissionTagDef>("MissionTypeBaseInfestation_MissionTagDef");

                    if (!geoMission.MissionDef.MissionTags.Contains(pxBaseInfestationTag))
                    {

                        FactionInfoMapping factionInfo = __instance.Resources.GetFactionInfo(geoMission.GetEnemyFaction());
                        LocalizedTextBind text = new LocalizedTextBind();
                        LocalizedTextBind objectivesText = new LocalizedTextBind();
                        Sprite sprite;

                        if (TFTVBaseDefenseTactical.AttackProgress < 0.3)
                        {
                            sprite = Helper.CreateSpriteFromImageFile("base_defense_hint.jpg");
                            text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_TACTICALADVANTAGE" };
                            objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_SIMPLE" };
                        }
                        else if (TFTVBaseDefenseTactical.AttackProgress >= 0.8)
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


        /*    [HarmonyPatch(typeof(GeoMission), nameof(GeoMission.OnMissionCompleted))]

            public static class GeoMission_OnMissionCompleted_Patch
            {
                public static bool Prefix(GeoMission __instance)
                {
                    try
                    {
                        if (PhoenixBasesUnderAttack.ContainsKey(__instance.Site))
                        {
                            TFTVLogger.Always("OnMissionCompleted invoked");
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
            }*/


        [HarmonyPatch(typeof(GeoPhoenixBaseDefenseMission), "Cancel")]
        public static class GeoPhoenixBaseDefenseMission_Cancel_Experiment_patch
        {
            public static bool Prefix(GeoPhoenixBaseDefenseMission __instance, ref bool ____missionCancelled)
            {
                try
                {



                    GeoSite phoenixBase = __instance.Site;
                    //  TFTVLogger.Always($"Cancel method invoked re base{phoenixBase.SiteId}, while list contains {PhoenixBasesUnderAttack.Keys.First()}");


                    if (PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId))
                    {
                        TFTVLogger.Always("Cancel method canceled.");


                        return false;

                    }
                    /* else
                     {

                         GeoMission geoMission = __instance.Site.ActiveMission;

                         __instance.PhoenixBase.BaseDefenseAbandoned();
                         ____missionCancelled = true;

                         Type type = typeof(GeoMission);
                         PropertyInfo property = type.GetProperty("IsCompleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                         property.SetValue(geoMission, true);

                         if (geoMission.MissionDef.ClearMissionOnCancel)
                         {
                             phoenixBase.ActiveMission = null;
                         }

                         if (geoMission.MissionDef.DestroySiteOnCancel)
                         {
                             phoenixBase.DestroySite();
                         }

                         PropertyInfo reward = type.GetProperty("Reward", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                         reward.SetValue(geoMission, new GeoFactionReward());

                         //  geoMission.OnMissionCancel?.Invoke(__instance.Site.ActiveMission);

                         //geoMission.Cancel();
                         //CancelMethodRun = true;

                     }*/
                    //  }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        /*   [HarmonyPatch(typeof(GeoSite), "RegisterMission")]
           public static class GeoSite_ModalShowHandler_Experiment_patch
           {
               public static void Prefix(GeoMission geoMission, GeoSite __instance)
               {
                   try
                   {


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/





        [HarmonyPatch(typeof(UIModal), "Show")]
        public static class UIModal_Show_Experiment_patch
        {
            public static void Postfix(UIModal __instance, object data)
            {
                try
                {

                    TFTVLogger.Always($"Showing modal {__instance.name}");

                    if (data is GeoMission geoMission && __instance.name.Contains("Brief"))
                    {
                        TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission.GetMissionOutcomeState()}");

                        if (geoMission.GetMissionOutcomeState() != TacFactionState.Playing)
                        {

                            __instance.Close();


                            TFTVLogger.Always("Closing modal because mission is not in play");

                        }
                        /*   PhoenixGeneralButton button = FindButtonByText("START MISSION");

                           if (geoMission.Site.Type == GeoSiteType.PhoenixBase && geoMission.Site.CharactersCount==0 && button!=null)
                           {


                                   button.SetEnabled(false);
                                   button.SetInteractable(false);
                                   button.ResetButtonAnimations();

                           }
                           else if(button!=null && button.enabled == false && geoMission.Site.CharactersCount > 0) 
                           {
                               TFTVLogger.Always("got here");

                               button.SetEnabled(true);
                               button.SetInteractable(true);
                               button.ResetButtonAnimations();


                           }*/
                    }
                    /*  if (data is GeoMission geoMission2 && __instance.name.Contains("Outcome"))
                      {
                          TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission2.GetMissionOutcomeState()}");

                          if (geoMission2.GetMissionOutcomeState() == TacFactionState.Playing)
                          {

                              __instance.Close();


                              TFTVLogger.Always("Closing modal because outcome, but mission is in play");

                          }
                      }*/

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

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

                              TFTVLogger.Always("Got here");

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
                              TFTVLogger.Always("Got here, past previousConditionFulfilled");

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
                          TFTVLogger.Always("Got here");

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
