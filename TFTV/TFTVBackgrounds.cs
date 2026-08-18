using Base.Core;
using Base.Defs;
using Base.Lighting;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Geoscape.Levels.GeoSceneReferences;

namespace TFTV
{
    internal class TFTVBackgrounds
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static class TFTVBackgroundDeploymentSelector
        {
            private const float SquadBayBackgroundScale = 1f;//1.05f;

            internal enum LightCondition
            {
                Day,
                Night
            }

            private static readonly Dictionary<string, Dictionary<LightCondition, Sprite>> MissionBackgrounds =
                new Dictionary<string, Dictionary<LightCondition, Sprite>>(StringComparer.OrdinalIgnoreCase);

            private static Sprite _defaultDayBackground;
            private static Sprite _defaultNightBackground;

            internal static void SetDefaults(Sprite dayBackground, Sprite nightBackground)
            {
                _defaultDayBackground = dayBackground;
                _defaultNightBackground = nightBackground ?? dayBackground;
            }

            internal static void Register(string missionDefName, LightCondition lightCondition, Sprite background)
            {
                if (string.IsNullOrEmpty(missionDefName) || background == null)
                {
                    return;
                }

                Dictionary<LightCondition, Sprite> backgrounds;
                if (!MissionBackgrounds.TryGetValue(missionDefName, out backgrounds))
                {
                    backgrounds = new Dictionary<LightCondition, Sprite>();
                    MissionBackgrounds.Add(missionDefName, backgrounds);
                }

                backgrounds[lightCondition] = background;
            }

            internal static Sprite Select(GeoMission mission)
            {
                LightCondition lightCondition = GetLightCondition(mission);
                Sprite background;
                Dictionary<LightCondition, Sprite> missionBackgrounds;
                string missionDefName = mission != null && mission.MissionDef != null ? mission.MissionDef.name : null;

                if (!string.IsNullOrEmpty(missionDefName)
                    && MissionBackgrounds.TryGetValue(missionDefName, out missionBackgrounds)
                    && missionBackgrounds.TryGetValue(lightCondition, out background))
                {
                    return background;
                }

                return lightCondition == LightCondition.Night
                    ? _defaultNightBackground ?? _defaultDayBackground
                    : _defaultDayBackground ?? _defaultNightBackground;
            }

            internal static void FitFullHeight(RectTransform imageTransform, Sprite background)
            {
                if (imageTransform == null || background == null || background.texture == null)
                {
                    return;
                }

                // The image is rendered on a transformed object in the 3D squad-bay scene. Its parent is
                // not a screen-sized clipping viewport and can be much larger than the visible display.
                // Using the parent's height therefore makes the artwork enormous.
                float availableHeight = imageTransform.rect.height;

                if (availableHeight <= 0f)
                {
                    availableHeight = imageTransform.rect.height;
                }

                float aspect = (float)background.texture.width / background.texture.height;
                imageTransform.sizeDelta = new Vector2(availableHeight * aspect, availableHeight);

                // This is a world-space display viewed through the squad-bay camera, not a normal
                // screen-space Image. A unit scale leaves it as the small rectangle seen in the roster.
                // Retain the scene's established background magnification while preserving scale Z.
                float displayScale = aspect * SquadBayBackgroundScale;
                imageTransform.localScale = new Vector3(
                    displayScale,
                    displayScale,
                    imageTransform.localScale.z);
                // In particular, do not set anchoredPosition3D.z to zero. The inherited Z position places
                // this display correctly in the squad-bay camera; zero moves it into the foreground.
            }

            private static LightCondition GetLightCondition(GeoMission mission)
            {
                if (mission == null || mission.Site == null)
                {
                    return LightCondition.Day;
                }

                int localHour = mission.Site.LocalTime.DateTime.Hour;
                return localHour >= 6 && localHour < 18 ? LightCondition.Day : LightCondition.Night;
            }
        }



        private static Sprite _backgroundSquadDeploy = null;
        private static Sprite _backgroundContainment = null;
        private static Sprite _backgroundBionics = null;
        private static Sprite _activeBackground = null;
        private static Sprite _backgroundMutation = null;
        private static Sprite _backgroundCustomization = null;
        private static Sprite _backgroundMemorial = null;
        private static Sprite _backgroundAirForce = null;
        private static Sprite _backgroundDeployment = null;
        private static Sprite _backgroundDeploymentNight = null;

        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayMain = null;

        private static void ModifyLightningAndPlatform(Transform transform)
        {
            try
            {
                var sceneLightingDef = DefCache.GetDef<SceneLightingDef>("EditSoldier_LightingDef");
                if (sceneLightingDef == null || transform == null)
                {
                    return; // nothing to modify safely
                }

                if (_activeBackground == _backgroundContainment)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundBionics)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundCustomization)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.9f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.8f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.7f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(false);
                }
                else if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundMemorial)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.06f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.14f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.49f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        [HarmonyPatch(typeof(CharacterClassWorldDisplay), nameof(CharacterClassWorldDisplay.SetDisplay))]
        public static class TFTV_CharacterClassWorldDisplay_SetDisplay_patch
        {

            public static bool Prefix(CharacterClassWorldDisplay __instance)
            {
                try
                {
                    __instance.gameObject.SetActive(false);

                    return false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        [HarmonyPatch(typeof(UIStateRosterAliens), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterAliens_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    //   UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                    //   uIModuleGeneralPersonelRoster.RosterList.gameObject.SetActive(true);

                    _activeBackground = _backgroundContainment;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")] //VERIFIED
        public static class TFTV_UIStateEditSoldier_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoCharacterStatus), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoCharacterStatus_EnterState_patch
        {

            public static void Prefix(UIStateGeoCharacterStatus __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoCharacterStatus ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoRoster_EnterState_patch
        {

            public static void Prefix(UIStateGeoRoster __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoRoster");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateInitial), "EnterState")] //VERIFIED
        public static class TFTV_UIStateInitial_EnterState_patch
        {

            public static void Prefix(UIStateInitial __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateInitial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static bool MemorialPushStateRunning = false;

        [HarmonyPatch(typeof(UIStateMemorial), "PushState")] //VERIFIED
        public static class TFTV_UIStateMemorial_PushState_patch
        {

            public static void Prefix(UIStateMemorial __instance)
            {
                try
                {




                    _activeBackground = _backgroundMemorial;

                    // TFTVLogger.Always($"entering UIStateMemorial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }


        [HarmonyPatch(typeof(UIStateEditVehicle), "PushState")] //VERIFIED
        public static class TFTV_UIStateEditVehicle_PushState_patch
        {

            public static void Prefix(UIStateEditVehicle __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateEditVehicle");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateMutate), "PushState")] //VERIFIED
        public static class TFTV_UIStateMutate_PushState_patch
        {

            public static void Prefix(UIStateMutate __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    // TFTVLogger.Always($"entering UIStateMutate");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateBuyMutoid), "PushState")] //VERIFIED
        public static class TFTV_UIStateBuyMutoid_PushState_patch
        {

            public static void Prefix(UIStateBuyMutoid __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    //  TFTVLogger.Always($"entering UIStateBuyMutoid");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateBionics), "PushState")] //VERIFIED
        public static class TFTV_UIStateBionics_PushState_patch
        {

            public static void Prefix(UIStateBionics __instance)
            {
                try
                {
                    _activeBackground = _backgroundBionics;
                    // TFTVLogger.Always($"entering UIStateBionics");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_EnterState_patch
        {

            public static void Prefix(UIStateRosterDeployment __instance)
            {
                try
                {
                    _backgroundDeployment = TFTVBackgroundDeploymentSelector.Select(__instance.Mission);
                    _activeBackground = _backgroundDeployment;

                    // _activeBackground = _backgroundSquadDeploy;
                    //TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterRecruits_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")] //VERIFIED
        public static class TFTV_UIStateSoldierCustomization_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundCustomization;
                    // TFTVLogger.Always($"entering UIStateSoldierCustomization ");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void LoadTFTVBackgrounds()
        {
            try
            {
                _backgroundSquadDeploy = Helper.CreateSpriteFromImageFile("squadbay.jpg");
                _backgroundContainment = Helper.CreateSpriteFromImageFile("containment.jpg");
                _backgroundMutation = Helper.CreateSpriteFromImageFile("scenemutation.jpg");
                _backgroundCustomization = Helper.CreateSpriteFromImageFile("scenecustomization.jpg");
                _backgroundBionics = Helper.CreateSpriteFromImageFile("scenebionics.jpg");
                _backgroundMemorial = Helper.CreateSpriteFromImageFile("scenememorial.jpg");
                _backgroundAirForce = Helper.CreateSpriteFromImageFile("sceneairforce.jpg");


                _backgroundDeployment = Helper.CreateSpriteFromImageFile("deployment_a.jpg");
                _backgroundDeploymentNight = Helper.CreateSpriteFromImageFile("deployment_b.jpg");
                TFTVBackgroundDeploymentSelector.SetDefaults(_backgroundDeployment, _backgroundDeploymentNight);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void RemoveSceneDoF()
        {
            try
            {
                FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);

                LightingManager lightingManager = context.LightingManager;
                OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                OptionsManager.GraphicsQualityPreset preset = optionsManager.CurrentGraphicsPreset;

                preset.DepthOfField = false;

                MethodInfo methodInfo = typeof(LightingManager).GetMethod("ApplyPostProcessOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(lightingManager, new object[] { preset });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void ChangeSceneBackgroundSquadDeploy(GeoSceneReferences geoSceneReferences)
        {
            try
            {
                if (geoSceneReferences == null || geoSceneReferences.SquadBay == null)
                {
                    return;
                }



                if (_copyCharacterClassWorldDisplayMain != null)
                {
                    if (_copyCharacterClassWorldDisplayMain.gameObject == null) return;

                    _copyCharacterClassWorldDisplayMain.gameObject.SetActive(true);

                    if (_activeBackground == null && _backgroundSquadDeploy == null) return;

                    var bg = _activeBackground ?? _backgroundSquadDeploy;
                    _copyCharacterClassWorldDisplayMain.SingleClassImage.sprite = bg;

                    var backgroundPicRT = _copyCharacterClassWorldDisplayMain.SingleClassImage.GetComponent<RectTransform>();
                    if (backgroundPicRT == null || bg.texture == null) return;

                    float imageAspectCurrentBackground = (float)bg.texture.width / bg.texture.height;

                    // Adjustments per background with guards
                    backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);

                    if (_activeBackground == _backgroundDeployment)
                    {
                        TFTVBackgroundDeploymentSelector.FitFullHeight(backgroundPicRT, bg);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundBionics)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.08f, imageAspectCurrentBackground * 1.08f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                    }
                    else if (_activeBackground == _backgroundCustomization)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.1f, imageAspectCurrentBackground * 1.1f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMemorial)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.15f, imageAspectCurrentBackground * 1.15f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, backgroundPicRT.anchoredPosition3D.z + 20);
                        RemoveSceneDoF();
                    }
                    else
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.31f, imageAspectCurrentBackground * 1.31f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }

                    return;
                }

                // Create copy safely
                var sourceDisplay = geoSceneReferences.SquadBay.ClassWorldDisplay;
                if (sourceDisplay == null || sourceDisplay.gameObject == null)
                {
                    return;
                }

                GameObject copy = UnityEngine.Object.Instantiate(sourceDisplay.gameObject, sourceDisplay.transform.parent);
                var copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                if (copyDisplay == null) return;

                _copyCharacterClassWorldDisplayMain = copyDisplay;
                var bg2 = _activeBackground ?? _backgroundSquadDeploy;

                copyDisplay.SingleClassImage.sprite = bg2;

                var rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                if (rt != null && _backgroundSquadDeploy != null && _backgroundSquadDeploy.texture != null)
                {
                    float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                    rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                    rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);
                    rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                    rt.eulerAngles = new Vector3(2.8f, 346, 0);
                }

                copyDisplay.SingleClassImage.gameObject.SetActive(true);
                if (copyDisplay.RightClassImage != null) copyDisplay.RightClassImage.gameObject.SetActive(false);
                if (copyDisplay.LeftClassImage != null) copyDisplay.LeftClassImage.gameObject.SetActive(false);

                // Remove DOF only if geoscape view exists
                RemoveSceneDoF();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // private static Sprite _airForceBackground = null;



        [HarmonyPatch(typeof(UIStateVehicleRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateVehicleRoster_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundAirForce;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayVehicleRoster = null;

        [HarmonyPatch(typeof(GeoSceneReferences), nameof(GeoSceneReferences.ActivateScene))]
        public static class TFTV_GeoSceneReferences_ActivateScene_patch
        {
            public static void Prefix(GeoSceneReferences __instance, ActiveSceneReference activeScene, Dictionary<ActiveSceneReference, Transform> ____scenes)
            {
                try
                {

                    // Safety: bail out if the scene reference or scenes map is not usable.
                    if (__instance == null || ____scenes == null)
                    {
                        return;
                    }

                    // Only proceed if the requested scene exists in the map
                    if (!____scenes.ContainsKey(activeScene) || ____scenes[activeScene] == null)
                    {
                        // Still attempt to cleanup UI panel, but don’t touch scene-specific objects
                        TFTVUI.Geoscape.ContainmentScreen.RemoveContainmentInfoPanel();
                        return;
                    }

                    if (activeScene == ActiveSceneReference.SquadBay)
                    {
                        // Guard against missing SquadBay sub-refs in some transitions
                        if (__instance.SquadBay != null)
                        {
                            ChangeSceneBackgroundSquadDeploy(__instance);

                            if (__instance.SquadBay.CharBuilderPlatform != null)
                            {
                                ModifyLightningAndPlatform(__instance.SquadBay.CharBuilderPlatform);
                            }
                        }
                    }
                    else if (activeScene == ActiveSceneReference.VehicleBay)
                    {
                        // Vehicle roster background; make sure base display exists before using it
                        if (_copyCharacterClassWorldDisplayVehicleRoster != null)
                        {
                            if (_copyCharacterClassWorldDisplayVehicleRoster.gameObject != null)
                            {
                                _copyCharacterClassWorldDisplayVehicleRoster.gameObject.SetActive(true);
                            }
                        }
                        else if (__instance.SquadBay != null && __instance.VehicleBay != null)
                        {
                            // Create a copy safely only when the source exists
                            var source = __instance.SquadBay.ClassWorldDisplay;
                            if (source != null && source.gameObject != null)
                            {
                                GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, __instance.VehicleBay.transform);
                                var copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                                if (copyDisplay != null)
                                {
                                    copy.SetActive(true);
                                    copyDisplay.SingleClassImage.sprite = _backgroundAirForce;

                                    RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                                    if (rt != null && _backgroundSquadDeploy != null && _backgroundSquadDeploy.texture != null)
                                    {
                                        float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                                        rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                                        rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);
                                        rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                                        rt.eulerAngles = new Vector3(2.8f, 346, 0);
                                    }

                                    copyDisplay.SingleClassImage.gameObject.SetActive(true);
                                    if (copyDisplay.RightClassImage != null) copyDisplay.RightClassImage.gameObject.SetActive(false);
                                    if (copyDisplay.LeftClassImage != null) copyDisplay.LeftClassImage.gameObject.SetActive(false);

                                    _copyCharacterClassWorldDisplayVehicleRoster = copyDisplay;
                                }
                            }
                        }
                    }

                    // Cleanup info panel safely every time
                    TFTVUI.Geoscape.ContainmentScreen.RemoveContainmentInfoPanel();
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
