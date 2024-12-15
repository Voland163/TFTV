using Base.Core;
using Base.Defs;
using Base.Lighting;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Levels.GeoSceneReferences;

namespace TFTV
{
    internal class TFTVBackgrounds
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static Sprite _backgroundSquadDeploy = null;
        private static Sprite _backgroundContainment = null;
        private static Sprite _backgroundBionics = null;
        private static Sprite _activeBackground = null;
        private static Sprite _backgroundMutation = null;
        private static Sprite _backgroundCustomization = null;
        private static Sprite _backgroundMemorial = null;
        private static Sprite _backgroundAirForce = null;



        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplay = null;


        private static void ModifyLightningAndPlatform(Transform transform)
        {
            try
            {
                SceneLightingDef sceneLightingDef = DefCache.GetDef<SceneLightingDef>("EditSoldier_LightingDef");

                if (_activeBackground == _backgroundContainment)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundBionics)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundCustomization)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.9f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.8f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.7f;
                    transform.gameObject.SetActive(false);
                }
                else if (_activeBackground == _backgroundMutation)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundMemorial)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    transform.gameObject.SetActive(true);
                }

                else
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.06f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.14f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.49f;
                    transform.gameObject.SetActive(false);
                }
                // Default:
                //    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5660378f;
                //    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5343573f;
                //    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.520647943f;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        [HarmonyPatch(typeof(CharacterClassWorldDisplay), "SetDisplay")]
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



        [HarmonyPatch(typeof(UIStateVehicleRoster), "EnterState")]
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


        [HarmonyPatch(typeof(UIStateRosterAliens), "PushState")]
        public static class TFTV_UIStateRosterAliens_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundContainment;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")]
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

        [HarmonyPatch(typeof(UIStateGeoCharacterStatus), "EnterState")]
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

        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")]
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

        [HarmonyPatch(typeof(UIStateInitial), "EnterState")]
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


        [HarmonyPatch(typeof(UIStateMemorial), "PushState")]
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
            public static void Postfix(UIStateMemorial __instance)
            {
                try
                {
                    _activeBackground = _backgroundMemorial;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateEditVehicle), "PushState")]
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

        [HarmonyPatch(typeof(UIStateMutate), "PushState")]
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


        [HarmonyPatch(typeof(UIStateBuyMutoid), "PushState")]
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

        [HarmonyPatch(typeof(UIStateBionics), "PushState")]
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



        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
        public static class TFTV_UIStateRosterDeployment_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "PushState")]
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

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")]
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
               /* if (_deactivateBackgroundPic)
                {
                    if (_copyCharacterClassWorldDisplay != null)
                    {
                        _copyCharacterClassWorldDisplay.gameObject.SetActive(false);
                    }
                    return;
                }*/


                if (_copyCharacterClassWorldDisplay != null)
                {
                    _copyCharacterClassWorldDisplay.gameObject.SetActive(true);

                    _copyCharacterClassWorldDisplay.SingleClassImage.sprite = _activeBackground ?? _backgroundSquadDeploy;
                    RectTransform backgroundPicRT = _copyCharacterClassWorldDisplay.SingleClassImage.GetComponent<RectTransform>();
                    float imageAspectCurrentBackground = (float)_activeBackground.texture.width / _activeBackground.texture.height;

                   /* TFTVLogger.Always($"Before changes: background {_activeBackground.name}, " +
                        $"anchoredPostion3d {backgroundPicRT.anchoredPosition3D}, " +
                        $"imageAspectCurrentBackground {imageAspectCurrentBackground}, " +
                        $"backgroundPicRT.sizeDelta {backgroundPicRT.sizeDelta}");*/

                    if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundBionics)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.08f, imageAspectCurrentBackground * 1.08f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                    }
                    else if (_activeBackground == _backgroundCustomization)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.1f, imageAspectCurrentBackground * 1.1f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMemorial)
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.15f, imageAspectCurrentBackground * 1.15f);
                        
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, backgroundPicRT.anchoredPosition3D.z+20);
                        RemoveSceneDoF();
                    }
                    else
                    {
                        backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.31f, imageAspectCurrentBackground * 1.31f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }

                   /* TFTVLogger.Always($"After changes: background {_activeBackground.name}, " +
                       $"anchoredPostion3d {backgroundPicRT.anchoredPosition3D}, " +
                       $"imageAspectCurrentBackground {imageAspectCurrentBackground}, " +
                       $"backgroundPicRT.sizeDelta {backgroundPicRT.sizeDelta}");*/

                    return;
                }

                CharacterClassWorldDisplay characterClassWorldDisplay = geoSceneReferences.SquadBay.ClassWorldDisplay;


                GameObject copy = UnityEngine.Object.Instantiate(characterClassWorldDisplay.gameObject, characterClassWorldDisplay.transform.parent);
                CharacterClassWorldDisplay copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                _copyCharacterClassWorldDisplay = copyDisplay;

                copyDisplay.SingleClassImage.sprite = _activeBackground ?? _backgroundSquadDeploy;

                RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);

                rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                rt.eulerAngles = new Vector3(2.8f, 346, 0);


                copyDisplay.SingleClassImage.gameObject.SetActive(true);
                copyDisplay.RightClassImage.gameObject.SetActive(false);
                copyDisplay.LeftClassImage.gameObject.SetActive(false);

                Transform transform = geoSceneReferences.SquadBay.CharBuilderPlatform;

                RemoveSceneDoF();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static Sprite _airForceBackground = null;

        [HarmonyPatch(typeof(GeoSceneReferences), "ActivateScene")]
        public static class TFTV_GeoSceneReferences_ActivateScene_patch
        {
            public static void Prefix(GeoSceneReferences __instance, ActiveSceneReference activeScene, Dictionary<ActiveSceneReference, Transform> ____scenes)
            {
                try
                {

                   // TFTVLogger.Always($"{activeScene} {__instance.name}");

                    if (activeScene == ActiveSceneReference.SquadBay)
                    {
                        ChangeSceneBackgroundSquadDeploy(__instance);
                        ModifyLightningAndPlatform(__instance.SquadBay.CharBuilderPlatform);
                    }
                    else if(activeScene == ActiveSceneReference.VehicleBay)
                    {

                        CharacterClassWorldDisplay characterClassWorldDisplay = __instance.SquadBay.ClassWorldDisplay;


                        GameObject copy = UnityEngine.Object.Instantiate(characterClassWorldDisplay.gameObject, __instance.VehicleBay.transform);
                        CharacterClassWorldDisplay copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                        // _copyCharacterClassWorldDisplay = copyDisplay;


                        if (_airForceBackground == null) 
                        { 
                        _airForceBackground = Helper.CreateSpriteFromImageFile("sceneairforce.jpg");
                        }

                        copyDisplay.SingleClassImage.sprite = _airForceBackground;

                        RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                        float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                        rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                        rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);

                        rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                        rt.eulerAngles = new Vector3(2.8f, 346, 0);


                        copyDisplay.SingleClassImage.gameObject.SetActive(true);
                        copyDisplay.RightClassImage.gameObject.SetActive(false);
                        copyDisplay.LeftClassImage.gameObject.SetActive(false);

                    }

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
