using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.BaseReworkCheck;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.TrainingFacilityRework;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{

    [HarmonyPatch(typeof(UIModuleGeoRosterTabs), nameof(UIModuleGeoRosterTabs.CheckAvailableTabs))]
    public static class UIModuleGeoRosterTabs_CheckAvailableTabs_Patch
    {
        private static void Postfix(UIModuleGeoRosterTabs __instance)
        {

            if (!BaseReworkEnabled)
            {
                return;
            }

            if (__instance?.SoldiersTab == null)
                return;

            string replacement = TFTVCommonMethods.ConvertKeyToString("KEY_FIELD_OPERATIVES");


            Text tmp = __instance.SoldiersTab.GetComponentInChildren<Text>(true);
            if (tmp != null)
            {
                tmp.text = replacement;
                return;
            }

        }
    }

    public static partial class PersonnelManagementUI
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private const string PersonnelContainerName = "TFTV_PersonnelContainer";
        private const string LogPrefix = "[PersonnelUI]";

        private static Font _puristaSemibold;
        internal static Font PuristaSemibold
        {
            get
            {
                if (_puristaSemibold == null)
                {
                    _puristaSemibold = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()
                        ?.View?.GeoscapeModules?.PhoenixpediaModule?.EntryTitle?.font;
                }
                return _puristaSemibold;
            }
        }

        private static GameObject _personnelPanel;
        private static GameObject _modalRoot;
        private static bool _deploymentUIActive;

        // Cached state/level so button handlers can rebuild the panel.
        private static UIStateRosterRecruits _cachedState;
        private static GeoLevelController _cachedLevel;

        #region Column Icon Placeholder
        /// <summary>
        /// Placeholder: replace with actual sprite lookup per assignment.
        /// Return null to show a gray square placeholder.
        /// </summary>
        /// 

        private static Sprite _unassignedSprite = null;
        private static Sprite _researchSprite = null;
        private static Sprite _manufacturingSprite = null;
        private static Sprite _deployTrainSprite = null;

        private static Sprite GetColumnIconSprite(PersonnelAssignment assignment)
        {
            // TODO: Return actual sprites here. Example:
            switch (assignment)
            {
                case PersonnelAssignment.Unassigned:

                    if (_unassignedSprite != null)
                    {
                        return _unassignedSprite;
                    }
                    else
                    {
                        _unassignedSprite = Helper.CreateSpriteFromImageFile("personnel.png");
                        return _unassignedSprite;
                    }


                case PersonnelAssignment.Research:

                    if (_researchSprite != null)
                    {
                        return _researchSprite;
                    }
                    else
                    {
                        _researchSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [ResearchLab_PhoenixFacilityDef]").SmallIcon;
                        return _researchSprite;
                    }

                case PersonnelAssignment.Manufacturing:
                    if (_manufacturingSprite != null)
                    {
                        return _manufacturingSprite;
                    }
                    else
                    {
                        _manufacturingSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [FabricationPlant_PhoenixFacilityDef]").SmallIcon;
                        return _manufacturingSprite;
                    }

                case PersonnelAssignment.Training:
                    if (_deployTrainSprite != null)

                    {
                        return _deployTrainSprite;
                    }
                    else
                    {
                        _deployTrainSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [TrainingFacility_PhoenixFacilityDef]").SmallIcon;
                        return _deployTrainSprite;

                    }

            }
            return null;
        }
        #endregion

        #region Daily Tick
        internal static void DailyTick(GeoLevelController level)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }

            try
            {
                if (level?.PhoenixFaction == null)
                {
                    TFTVLogger.Always($"{LogPrefix} DailyTick skipped: level or PhoenixFaction is null.");
                    return;
                }

                bool opened = TryOpenNextCompletedDeployment(level);

            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        // Tracks the currently-hooked level/handler pair so we can unsubscribe safely
        // (GeoLevelController.HourTicked is a real instance event; Harmony patching won't
        // reach it, so we subscribe/unsubscribe explicitly on Geoscape start/end).
        private static GeoLevelController _hourlyHookedLevel;
        private static Action<int> _hourlyTickHandler;

        /// <summary>
        /// Subscribes to GeoLevelController.HourTicked so completed training deployments
        /// are offered to the player up to once per in-game hour, instead of waiting for
        /// the once-per-day DailyUpdate checkpoint. Safe to call multiple times; any
        /// previous subscription is removed first.
        /// </summary>
        internal static void HookHourlyDeploymentCheck(GeoLevelController level)
        {
            try
            {
                UnhookHourlyDeploymentCheck();

                if (level == null)
                {
                    TFTVLogger.Always($"{LogPrefix} HookHourlyDeploymentCheck aborted: level is null.");
                    return;
                }

                _hourlyTickHandler = _ => DailyTick(level);
                level.HourTicked += _hourlyTickHandler;
                _hourlyHookedLevel = level;

                TFTVLogger.Always($"{LogPrefix} Hooked GeoLevelController.HourTicked for hourly deployment-completion checks.");
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        /// <summary>
        /// Unsubscribes from GeoLevelController.HourTicked. Must be called on Geoscape end
        /// to avoid leaking a handler onto a stale/destroyed level instance.
        /// </summary>
        internal static void UnhookHourlyDeploymentCheck()
        {
            try
            {
                if (_hourlyHookedLevel != null && _hourlyTickHandler != null)
                {
                    _hourlyHookedLevel.HourTicked -= _hourlyTickHandler;
                    TFTVLogger.Always($"{LogPrefix} Unhooked GeoLevelController.HourTicked.");
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
            finally
            {
                _hourlyHookedLevel = null;
                _hourlyTickHandler = null;
            }
        }

        private static int _deploymentUIActiveSetAtFrame = -1;
        private const int DeploymentUIStaleFrameThreshold = 180; // ~3s at 60fps safety window

        private static void ResetStaleDeploymentUIFlag()
        {
            if (_deploymentUIActive && _deploymentUIActiveSetAtFrame >= 0
                && Time.frameCount - _deploymentUIActiveSetAtFrame > DeploymentUIStaleFrameThreshold)
            {
                TFTVLogger.Always($"{LogPrefix} _deploymentUIActive detected stale (deployment state never entered); resetting.");
                _deploymentUIActive = false;
                _deploymentUIActiveSetAtFrame = -1;
            }
        }

        private static void AutoOpenVanillaDeploymentUI(GeoLevelController level, GeoPhoenixFaction faction, PersonnelInfo person)
        {
            try
            {
                if (level == null || faction == null || person == null)
                {
                    TFTVLogger.Always($"{LogPrefix} AutoOpenVanillaDeploymentUI aborted: level/faction/person null.");
                    return;
                }

                if (faction != level.PhoenixFaction)
                {
                    TFTVLogger.Always($"{LogPrefix} AutoOpenVanillaDeploymentUI aborted: faction is not the current viewer faction, PrepareDeployAsset would no-op.");
                    return;
                }

                GeoCharacter character = TrainingFacilityRework.FinalizeRecruitTrainingForUI(level, person.Character, early: false);
                if (character == null)
                {
                    TFTVLogger.Always($"{LogPrefix} AutoOpenVanillaDeploymentUI failed: FinalizeRecruitTrainingForUI returned null for {GetPersonnelName(person)}.");
                    return;
                }

                PersonnelData.RemovePersonnel(faction, person);

                CloseModal();

                _deploymentUIActive = true;
                _deploymentUIActiveSetAtFrame = Time.frameCount;

                faction.RemoveCharacter(character);

                level.View.PrepareDeployAsset(faction, character, null, null, manufactured: false, spaceFull: false);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIStateAssetDeployment), "EnterState")]
        internal static class UIStateAssetDeployment_EnterState_PersonnelManagement
        {
            private static void Postfix()
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }
                // Confirms the deployment state actually entered; keeps the stale-flag window tight.
                _deploymentUIActiveSetAtFrame = Time.frameCount;
            }
        }

        private static bool TryOpenNextCompletedDeployment(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction == null)
                {
                    TFTVLogger.Always($"{LogPrefix} TryOpenNextCompletedDeployment aborted: level or PhoenixFaction is null.");
                    return false;
                }

                ResetStaleDeploymentUIFlag();

                if (_deploymentUIActive)
                {
                    TFTVLogger.Always($"{LogPrefix} TryOpenNextCompletedDeployment blocked: deployment UI already active.");
                    return false;
                }

                foreach (PersonnelInfo p in Assignments.Values.OrderBy(x => GetPersonnelName(x)))
                {
                    if (p == null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Candidate skipped: PersonnelInfo is null.");
                        continue;
                    }

                    string name = GetPersonnelName(p);
                    string assignment = p.Assignment.ToString();
                    bool hasCharacter = p.Character != null;


                    if (p.Assignment != PersonnelAssignment.Training)
                    {
                        continue;
                    }

                    if (p.Character == null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Candidate {name} skipped: training assignment but Character is null.");
                        continue;
                    }

                    RecruitTrainingSession session = TrainingFacilityRework.GetRecruitSession(p.Character);
                    bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(p.Character, level);


                    if (complete)
                    {

                        AutoOpenVanillaDeploymentUI(level, level.PhoenixFaction, p);

                        return true;
                    }
                }


            }
            catch (Exception e) { TFTVLogger.Error(e); }

            return false;
        }
        #endregion

        #region Harmony
        [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
        internal static class UIStateRosterRecruits_EnterState_PersonnelManagement
        {
            private static void Postfix(UIStateRosterRecruits __instance)
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {
                    CreatePersonnelPanel(__instance);
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    TFTVHints.BaseReworkHints.TriggerPersonnelHint(controller);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "ExitState")]
        internal static class UIStateRosterRecruits_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {

                    if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
                    CloseModal();
                    _deploymentUIActive = false;
                    _expandedCharacterId = 0;
                    _cachedState = null;
                    _cachedLevel = null;
                    _puristaSemibold = null; // reset so it re-resolves from live level on next open

                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }



        [HarmonyPatch(typeof(UIStateAssetDeployment), "ExitState")]
        internal static class UIStateAssetDeployment_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {

                    _deploymentUIActive = false;

                    GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                    bool opened = TryOpenNextCompletedDeployment(level);

                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }


        [HarmonyPatch(typeof(UIModuleRecruitsList), nameof(UIModuleRecruitsList.SetRecruitsList))]
        public static class PersonnelManagementPatch
        {
            public static void Postfix(UIModuleRecruitsList __instance)
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }


                if (__instance == null || __instance.RecruitsListRoot == null) return;
                __instance.NoRecruitsMessage.SetActive(false);
                __instance.NoRecruitsMessageTextBackground.SetActive(false);
                __instance.SpecializationController.gameObject.SetActive(false);
                __instance.InfoController.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}
