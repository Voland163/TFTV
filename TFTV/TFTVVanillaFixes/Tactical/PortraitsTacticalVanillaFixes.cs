using Base;
using Base.Rendering.ObjectRendering;
using HarmonyLib;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using SETUtil.Common.Extend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.SquadPortraitsDef;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class PortraitsTacticalVanillaFixes
    {
        //Codemite's solution to pink portrait backgrounds on Macs
        [HarmonyPatch(typeof(RenderingEnvironment), MethodType.Constructor, new Type[]
{
    typeof(Vector2Int),
    typeof(RenderingEnvironmentOption),
    typeof(Color),
    typeof(Camera)
  })]
        public static class RenderingEnvironmentPatch
        {

            public static bool Prefix(ref RenderingEnvironment __instance, ref Transform ____origin, ref bool ____isExternalCamera, ref Camera ____camera,
                Vector2Int resolution, RenderingEnvironmentOption option, Color? backgroundColor, Camera cam)
            {
                try
                {

                    ____origin = new GameObject("_RenderingEnvironmentOrigin_").transform;
                    ____origin.position = new Vector3(0f, 1500f, 0f);
                    ____origin.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                    ____isExternalCamera = cam != null;

                    ____camera = cam != null ? cam : new GameObject("_RenderingCamera_").AddComponent<Camera>();
                    ____camera.gameObject.SetActive(false);
                    ____camera.clearFlags = option.ContainsFlag(RenderingEnvironmentOption.NoBackground) && backgroundColor == null
                        ? CameraClearFlags.Depth
                        : (backgroundColor != null ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox);
                    ____camera.fieldOfView = 60;
                    ____camera.orthographicSize = 2;
                    ____camera.orthographic = option.ContainsFlag(RenderingEnvironmentOption.Orthographic);
                    ____camera.farClipPlane = 100;
                    ____camera.renderingPath = RenderingPath.Forward;
                    ____camera.enabled = false;
                    ____camera.allowHDR = false;
                    ____camera.allowDynamicResolution = false;
                    ____camera.usePhysicalProperties = false;

                    // Use reflection to set the value of RenderTexture
                    var renderTextureField = typeof(RenderingEnvironment).GetField("RenderTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (renderTextureField != null)
                    {
                        renderTextureField.SetValue(__instance, RenderTexture.GetTemporary(resolution.x, resolution.y, 1, RenderTextureFormat.ARGB32));
                    }

                    ____camera.targetTexture = (RenderTexture)renderTextureField.GetValue(__instance);

                    if (backgroundColor != null)
                    {
                        ____camera.backgroundColor = (Color)backgroundColor;
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static void AdjustPortraitLights(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
        {
            try
            {
                // --- Key Light (e.g., DirLighjt2) ---
                // Aim for a warm, flattering tone.
                keyLight.useColorTemperature = true;
                keyLight.colorTemperature = 5500; // Warmer than 6570K for a cozy feel
                                                  // Optionally, adjust the color to emphasize warmth:
                keyLight.color = new Color(1.0f, 0.9f, 0.8f, 1.0f);
                keyLight.intensity = 1.0f; // Adjust intensity as needed

                // --- Fill Light (e.g., DirLighjt3) ---
                // Use a softer light to reduce harsh shadows.
                fillLight.useColorTemperature = true;
                fillLight.colorTemperature = 5500; // Consistent with the key light
                                                   // Slightly modify the color to be neutral but warm
                fillLight.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                fillLight.intensity = 0.5f; // Lower intensity for subtle filling

                // --- Rim/Hair Light (e.g., DirLighjt1) ---
                // This light provides a cool accent to separate the character from the background.
                rimLight.useColorTemperature = true;
                rimLight.colorTemperature = 6500; // Keep it cooler for contrast
                rimLight.color = new Color(0.2f, 0.825f, 1.0f, 1.0f);
                rimLight.intensity = 0.7f; // Adjust intensity so it provides a gentle highlight

                // --- Ambient Light (e.g., AmbienceLight) ---
                // A lower-intensity, soft warm light to ensure overall balance.
                ambientLight.useColorTemperature = true;
                ambientLight.colorTemperature = 5500; // Warmer ambient tone
                                                      // Slightly tinted to avoid a flat look
                ambientLight.color = new Color(1.0f, 1.0f, 0.95f, 1.0f);
                ambientLight.intensity = 0.3f; // Lower intensity to prevent wash-out
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void AdjustLovecraftianPortraitLights(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
        {
            // --- Key Light ---
            // Use a directional key light with a dark, cold cyan tint.
            // A relatively high intensity with a hard edge creates stark shadows.
            keyLight.useColorTemperature = true;
            keyLight.colorTemperature = 6500; // Cold tone.
            keyLight.color = new Color(0.25f, 0.45f, 0.55f, 1.0f); // Dark cyan.
            keyLight.intensity = 1.2f; // Strong key light for dramatic shadows.
                                       // Optionally, if your Light supports a spot or angle, narrow the beam.

            // --- Fill Light ---
            // Use a very low-intensity fill to let the shadows dominate.
            fillLight.useColorTemperature = true;
            fillLight.colorTemperature = 6500;
            fillLight.color = new Color(0.1f, 0.15f, 0.2f, 1.0f); // Extremely muted, dark fill.
            fillLight.intensity = 0.3f; // Low intensity to maintain deep shadows.

            // --- Rim/Hair Light ---
            // A rim light can help define the edges of the character.
            // Use a cooler tone (even slightly bluer) for an eerie outline.
            rimLight.useColorTemperature = true;
            rimLight.colorTemperature = 7000;
            rimLight.color = new Color(0.2f, 0.35f, 0.6f, 1.0f); // Cool blue.
            rimLight.intensity = 0.8f; // Sufficient to outline without overpowering.

            // --- Ambient Light ---
            // Keep ambient light very low to prevent flattening the mood.
            ambientLight.useColorTemperature = true;
            ambientLight.colorTemperature = 6500;
            ambientLight.color = new Color(0.05f, 0.05f, 0.1f, 1.0f); // Nearly dark with a hint of blue.
            ambientLight.intensity = 0.2f; // Low overall illumination.
        }



        private static void AdjustPortraitLightsCold(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
        {
            try
            {
                // --- Key Light (Primary Illumination) ---
                // Use a cool blue-tinted key light.
                keyLight.useColorTemperature = true;
                keyLight.colorTemperature = 7000; // A cooler temperature for a cold look.
                keyLight.color = new Color(0.8f, 0.9f, 1.0f, 1.0f); // Slight blue tint.
                keyLight.intensity = 1.0f; // Adjust intensity as needed.

                // --- Fill Light (Subtle Shadow Reduction) ---
                // Use a softer fill with a similar cool tone.
                fillLight.useColorTemperature = true;
                fillLight.colorTemperature = 7000;
                fillLight.color = new Color(0.7f, 0.8f, 0.9f, 1.0f); // A slightly softer blue tint.
                fillLight.intensity = 0.5f; // Lower intensity to gently fill shadows.

                // --- Rim/Hair Light (Accent/Edge Highlight) ---
                // Use an even cooler tone to create a distinct rim effect.
                rimLight.useColorTemperature = true;
                rimLight.colorTemperature = 7500; // Even cooler for a crisp rim.
                rimLight.color = new Color(0.3f, 0.5f, 0.9f, 1.0f); // A deeper blue.
                rimLight.intensity = 0.7f; // Adjust so it provides a subtle edge highlight.

                // --- Ambient Light (Overall Scene Illumination) ---
                // A soft, low-intensity ambient light to round out the scene.
                ambientLight.useColorTemperature = true;
                ambientLight.colorTemperature = 7000;
                ambientLight.color = new Color(0.9f, 0.9f, 1.0f, 1.0f); // Light blue tint.
                ambientLight.intensity = 0.3f; // Keep it low to avoid washing out details.
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        private static bool CheckOSX()
        {
            try
            {
                if (Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.OSXEditor)
                {
                    return true;

                }
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(SoldierPortraitUtil), nameof(SoldierPortraitUtil.RenderSoldierNoCopy))]
        public static class SoldierPortraitUtil_RenderSoldierNoCopy_patch
        {

            public static bool Prefix(GameObject soldierToRender, RenderPortraitParams renderParams, Camera usedCamera, ref Texture2D __result)
            {
                try
                {
                    // TFTVLogger.Always($"[SoldierPortraitUtil.RenderSoldierNoCopy] running for {soldierToRender.name} with camera {usedCamera.name} and resolution {renderParams.RenderedPortraitsResolution}");

                    /* List<Light> lights = new List<Light>()
                     {
                         soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt2")),
                         soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt3")),
                         soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt1")),
                         soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("AmbienceLight")),
                     };*/

                    // AdjustPortraitLights(lights[0], lights[1], lights[2], lights[3]);
                    //   AdjustPortraitLightsCold(lights[0], lights[1], lights[2], lights[3]);
                    //  AdjustLovecraftianPortraitLights(lights[0], lights[1], lights[2], lights[3]);

                    Texture2D texture2D = new Texture2D(renderParams.RenderedPortraitsResolution.x, renderParams.RenderedPortraitsResolution.y, TextureFormat.RGBA32, mipChain: true);
                    texture2D.filterMode = FilterMode.Trilinear;


                    RenderingEnvironment renderingEnvironment = new RenderingEnvironment(renderParams.RenderedPortraitsResolution, RenderingEnvironmentOption.NoBackground, null, usedCamera);

                    if (CheckOSX())
                    {
                        renderingEnvironment = new RenderingEnvironment(renderParams.RenderedPortraitsResolution, RenderingEnvironmentOption.NoBackground, Color.black, usedCamera);
                    }

                    //  TFTVLogger.Always($"QualitySettings.antiAliasing: {QualitySettings.antiAliasing}");

                    //  renderingEnvironment.RenderTexture.antiAliasing = (QualitySettings.antiAliasing > 0) ? QualitySettings.antiAliasing : 4;

                    float cameraDistance = renderParams.CameraDistance;

                    Transform transform = soldierToRender.transform.FindTransformInChildren("Nose");
                    if (transform == null)
                    {
                        transform = soldierToRender.transform.FindTransformInChildren("Jaw");//Head");
                        cameraDistance = 0.63f;

                        if (transform == null)
                        {
                            transform = soldierToRender.transform;
                        }
                        ;
                    }

                    Transform transform2 = soldierToRender.transform;
                    Vector3 position = transform2.position;
                    Quaternion rotation = transform2.rotation;
                    transform2.position = renderingEnvironment.OriginPosition;
                    transform2.rotation = renderingEnvironment.OriginRotation;

                    SoldierFrame cameraFrameLogic = new SoldierFrame(transform, renderParams.CameraFoV, cameraDistance, renderParams.CameraHeight, renderParams.CameraSide);
                    renderingEnvironment.Render(cameraFrameLogic, useOrigin: false);
                    renderingEnvironment.WriteResultsToTexture(texture2D);
                    texture2D.Apply(updateMipmaps: true);

                    transform2.position = position;
                    transform2.rotation = rotation;
                    __result = texture2D;

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
