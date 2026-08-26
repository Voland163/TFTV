using Base;
using Base.Cameras;
using Base.Core;
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
            // usedCamera (LightsPicker.UsedCamera) is no longer used - CaptureSoldier makes its own
            // camera - but Harmony needs the signature to keep matching the original.
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

                    float cameraDistance = renderParams.CameraDistance;

                    Transform targetBone = soldierToRender.transform.FindTransformInChildren("Nose");
                    if (targetBone == null)
                    {
                        targetBone = soldierToRender.transform.FindTransformInChildren("Jaw");//Head");
                        cameraDistance = 0.63f;

                        if (targetBone == null)
                        {
                            targetBone = soldierToRender.transform;
                        }
                    }

                    __result = CaptureSoldier(targetBone, renderParams, cameraDistance);

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Renders the staged soldier with a camera copied from the one the game draws the world with.
        ///
        /// The game's own path (SoldierPortraitUtil -> RenderingEnvironment) builds a bare camera and
        /// forces RenderingPath.Forward on it, and the character shader's customization - armour
        /// colour and pattern, which live in per-renderer MaterialPropertyBlocks set on the body
        /// part's own material by HighlightControllerComponent - does not survive that path: the
        /// armour renders in its factory texture. Copying the live camera keeps the shader on the
        /// path it takes in the scene, which is where the customization shows. Same fix as
        /// TFTVIncidents.PortraitGenerator.CapturePortrait.
        ///
        /// The subject is framed where it stands, at the char builder's own staging position: vanilla
        /// moved it to the RenderingEnvironment origin, but it passed useOrigin: false, so SoldierFrame
        /// framed off the bone's own axes either way. SoldierFrame also culls to the Characters layer
        /// and clips 2.5m out, which is what keeps the rest of the scene out of the picture.
        /// </summary>
        private static Texture2D CaptureSoldier(Transform targetBone, RenderPortraitParams renderParams, float cameraDistance)
        {
            Vector2Int resolution = renderParams.RenderedPortraitsResolution;

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);

            GameObject cameraHost = new GameObject("_TFTVPortraitCamera_");
            RenderTexture previouslyActive = RenderTexture.active;

            try
            {
                Camera camera = cameraHost.AddComponent<Camera>();
                Camera sceneCamera = GameUtl.GameComponent<CameraManager>()?.Camera;
                if (sceneCamera != null)
                {
                    camera.CopyFrom(sceneCamera);
                }
                else
                {
                    camera.renderingPath = RenderingPath.UsePlayerSettings;
                }

                camera.enabled = false;
                camera.targetTexture = renderTexture;
                camera.clearFlags = CameraClearFlags.SolidColor;
                // Codemite's fix for pink portrait backgrounds on Macs: OSX cannot carry the
                // transparent clear, so clear to black there instead.
                camera.backgroundColor = CheckOSX() ? Color.black : new Color(0f, 0f, 0f, 0f);
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.allowDynamicResolution = false;
                camera.usePhysicalProperties = false;
                camera.orthographic = false;
                camera.nearClipPlane = 0.01f;

                // Vanilla framing, unchanged: position off the bone, LookAt, FoV, the Characters-only
                // culling mask and the 2.5m far clip.
                CameraFrameLogic cameraFrameLogic = new SoldierFrame(targetBone, renderParams.CameraFoV,
                    cameraDistance, renderParams.CameraHeight, renderParams.CameraSide);
                cameraFrameLogic.FrameCamera(camera, null);

                camera.Render();

                RenderTexture.active = renderTexture;
                Texture2D texture2D = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, mipChain: true);
                texture2D.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0, recalculateMipMaps: true);
                texture2D.Apply(updateMipmaps: true);
                texture2D.filterMode = FilterMode.Trilinear;
                texture2D.anisoLevel = 4;
                return texture2D;
            }
            finally
            {
                RenderTexture.active = previouslyActive;
                UnityEngine.Object.Destroy(cameraHost);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
