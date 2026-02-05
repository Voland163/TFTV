using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*namespace TFTV.TFTVIncidents
{
    internal static class PortraitGenerator
    {
        [HarmonyPatch(typeof(UIModuleSiteEncounters), nameof(UIModuleSiteEncounters.ShowEncounter))]
        internal static class GeoscapeEncounterLeaderPortraitPatch
        {
            private static readonly HashSet<GeoCharacter> InProgress = new HashSet<GeoCharacter>();
            private const string LogPrefix = "[TFTV][PortraitGenerator]";

            private static readonly PortraitRenderProfile Profile = new PortraitRenderProfile
            {
                Resolution = 1024,
                CameraFoV = 40f,
                NoseDistance = 0.62f,
                JawDistance = 0.72f,
                HeadDistance = 0.68f,
                AmbientIntensity = 0.05f,
                ReflectionIntensity = 0f,
                DirectionalLightMultiplier = 0.24f,
                PointLightMultiplier = 0.40f,
                SpotLightMultiplier = 0.34f,
                OtherLightMultiplier = 0.34f,
                MinCameraNearClip = 0.02f,
                MaxCameraFarClip = 25f,
                ApplyPostProcess = false,//true,
                PostGamma = 0.95f,
                PostContrast = 1.05f,
                PostSharpen = 0.08f,
                LightNameTokens = new[] { "soft", "portrait", "fill" }
            };

            [HarmonyPostfix]
            private static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent)
            {
                try
                {
                    if (__instance == null || geoEvent?.Context?.Site == null || geoEvent.Context.Vehicle == null)
                    {
                        return;
                    }

                    GeoCharacter character = geoEvent.Context.Vehicle.Units.FirstOrDefault();
                    if (character == null || InProgress.Contains(character))
                    {
                        return;
                    }

                    CoroutineRunner runner = GetCoroutineRunner();
                    if (runner != null)
                    {
                        runner.StartCoroutine(RenderAndApply(__instance, character));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{LogPrefix} Postfix failed: {e}");
                }
            }

            private static IEnumerator RenderAndApply(UIModuleSiteEncounters module, GeoCharacter character)
            {
                InProgress.Add(character);
                try
                {
                    Sprite portrait = null;
                    yield return RenderPortrait(character, s => portrait = s);

                    if (portrait != null && module != null && module.isActiveAndEnabled)
                    {
                        module.EncunterLeaderGroup.SetActive(true);
                        module.EncunterLeaderInkGroup.SetActive(true);
                        module.EncounterLeaderImage.sprite = portrait;
                    }
                }
                finally
                {
                    InProgress.Remove(character);
                }
            }

            private static IEnumerator RenderPortrait(GeoCharacter character, Action<Sprite> onDone)
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                if (level?.SceneReferences == null)
                {
                    Debug.LogWarning($"{LogPrefix} Geo scene references are missing.");
                    onDone?.Invoke(null);
                    yield break;
                }

                AddonsCharacterBuilder sourceBuilder = ResolveSourceBuilder(level);
                if (sourceBuilder == null)
                {
                    Debug.LogWarning($"{LogPrefix} No usable CharacterBuilder found (UICaptureEnvironment/SquadBay).");
                    onDone?.Invoke(null);
                    yield break;
                }

                AddonsCharacterBuilder tempBuilder = UnityEngine.Object.Instantiate(sourceBuilder);
                tempBuilder.gameObject.SetActive(true);

                bool rebuildDone = false;
                Action rebuiltCallback = () => rebuildDone = true;
                tempBuilder.OnCharacterRebuilded += rebuiltCallback;

                try
                {
                    SharedData sharedData = GameUtl.GameComponent<SharedData>();
                    UnitDisplayData displayData = new UnitDisplayData(character, sharedData);

                    bool rigChanged;
                    CommonCharacterUtils.DisplayCharacter(tempBuilder, displayData, out rigChanged);

                    // Preserve identity-specific visuals: force current character tags onto the cloned builder
                    // before rebuilding armor/body parts.
                    tempBuilder.AddonsManager.SetAutorefreshOnTagsChanged(false);
                    tempBuilder.AddonsManager.GameTags.Clear();
                    tempBuilder.AddonsManager.GameTags.AddRange(
                        displayData.GameTags,
                        PhoenixPoint.Common.Entities.GameTags.GameTagAddMode.ErrorOnExistingExclusive);
                    tempBuilder.AddonsManager.SetAutorefreshOnTagsChanged(true);

                    List<ItemDef> bodyParts = character.GetBodyParts()
                        .OfType<ItemDef>()
                        .Concat(displayData.ArmourItems)
                        .Where(i => i != null)
                        .Distinct()
                        .Where(i => !IsHelmetOrAttachment(i))
                        .ToList();

                    CommonCharacterUtils.RebuildCharacter(tempBuilder, bodyParts, null, null);

                    int guardFrames = 0;
                    while (!rebuildDone && guardFrames++ < 120)
                    {
                        yield return null;
                    }

                    if (!rebuildDone)
                    {
                        Debug.LogWarning($"{LogPrefix} Character rebuild timed out.");
                        onDone?.Invoke(null);
                        yield break;
                    }

                    CommonCharacterUtils.ResetCharacterAnimation(tempBuilder);
                    // Let skinned meshes settle to avoid one-frame head/helmet ghosting artifacts.
                    yield return null;
                    yield return null;

                    Texture2D rendered = RenderTextureWithPortraitLights(level, tempBuilder.gameObject);
                    if (rendered == null)
                    {
                        onDone?.Invoke(null);
                        yield break;
                    }

                   // rendered.filterMode = FilterMode.Trilinear;


                    if (Profile.ApplyPostProcess)
                    {
                        rendered = ApplyPostProcess(rendered, Profile.PostGamma, Profile.PostContrast, Profile.PostSharpen);
                    }

                    Sprite sprite = Sprite.Create(
                        rendered,
                        new Rect(0f, 0f, rendered.width, rendered.height),
                        new Vector2(0.5f, 0.5f),
                        100f);

                    onDone?.Invoke(sprite);
                }
                finally
                {
                    tempBuilder.OnCharacterRebuilded -= rebuiltCallback;
                    UnityEngine.Object.Destroy(tempBuilder.gameObject);
                }
            }

            private static Texture2D RenderTextureWithPortraitLights(GeoLevelController level, GameObject characterObject)
            {
                LightsPicker lightsPicker = characterObject.GetComponentInChildren<LightsPicker>(true);
                Camera usedCamera = ResolvePortraitCamera(level, lightsPicker, characterObject);

                HashSet<Light> worldLightsToRestore = new HashSet<Light>();
                List<KeyValuePair<Light, float>> portraitLightIntensities = new List<KeyValuePair<Light, float>>();

                float ambientBefore = RenderSettings.ambientIntensity;
                float reflectionBefore = RenderSettings.reflectionIntensity;

                CameraState cameraState = CaptureCameraState(usedCamera);

                try
                {
                    RenderSettings.ambientIntensity = Profile.AmbientIntensity;
                    RenderSettings.reflectionIntensity = Profile.ReflectionIntensity;
                    ApplyCameraOverrides(usedCamera);

                    foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
                    {
                        if (light != null && light.isActiveAndEnabled)
                        {
                            worldLightsToRestore.Add(light);
                            light.gameObject.SetActive(false);
                        }
                    }

                    GameObject selectedRig = SelectLightRig(lightsPicker);
                    if (selectedRig != null)
                    {
                        selectedRig.SetActive(true);
                        foreach (Light light in selectedRig.GetComponentsInChildren<Light>(true))
                        {
                            if (light == null)
                            {
                                continue;
                            }

                            light.gameObject.SetActive(true);
                            portraitLightIntensities.Add(new KeyValuePair<Light, float>(light, light.intensity));
                            light.intensity *= GetLightMultiplier(light);
                        }
                    }

                    return RenderWithAnchorFallback(characterObject, usedCamera);
                }
                finally
                {
                    RestoreCameraState(usedCamera, cameraState);

                    foreach (Light light in worldLightsToRestore)
                    {
                        if (light != null)
                        {
                            light.gameObject.SetActive(true);
                        }
                    }

                    foreach (var kv in portraitLightIntensities)
                    {
                        if (kv.Key != null)
                        {
                            kv.Key.intensity = kv.Value;
                        }
                    }

                    lightsPicker?.DisableAllControlledLights();
                    RenderSettings.ambientIntensity = ambientBefore;
                    RenderSettings.reflectionIntensity = reflectionBefore;
                }
            }

            private static Texture2D RenderWithAnchorFallback(GameObject characterObject, Camera usedCamera)
            {
                string[] anchors = { "Nose", "Jaw", "Head" };
                float[] distances = { Profile.NoseDistance, Profile.JawDistance, Profile.HeadDistance };

                for (int i = 0; i < anchors.Length; i++)
                {
                    var p = new SquadPortraitsDef.RenderPortraitParams
                    {
                        RenderedPortraitsResolution = new Vector2Int(Profile.Resolution, Profile.Resolution),
                        TargetBoneName = anchors[i],
                        CameraFoV = Profile.CameraFoV,
                        CameraDistance = distances[i],
                        CameraHeight = 0f,
                        CameraSide = 0f
                    };

                    Texture2D t = SoldierPortraitUtil.RenderSoldierNoCopy(characterObject, p, usedCamera);
                    if (t != null)
                    {
                        return t;
                    }
                }

                // Last chance: explicit head render. SoldierPortraitUtil will fallback to root if bone is missing.
                var fallback = new SquadPortraitsDef.RenderPortraitParams
                {
                    RenderedPortraitsResolution = new Vector2Int(Profile.Resolution, Profile.Resolution),
                    TargetBoneName = "Head",
                    CameraFoV = Profile.CameraFoV,
                    CameraDistance = Profile.HeadDistance,
                    CameraHeight = 0f,
                    CameraSide = 0f
                };

                return SoldierPortraitUtil.RenderSoldierNoCopy(characterObject, fallback, usedCamera);
            }

            private static AddonsCharacterBuilder ResolveSourceBuilder(GeoLevelController level)
            {
                AddonsCharacterBuilder captureBuilder = level.SceneReferences.UICaptureEnvironment?.CharacterBuilder;
                if (captureBuilder != null)
                {
                    return captureBuilder;
                }

                Debug.LogWarning($"{LogPrefix} UICaptureEnvironment is missing; falling back to SquadBay.CharacterBuilder.");
                return level.SceneReferences.SquadBay?.CharacterBuilder;
            }

            private static Camera ResolvePortraitCamera(GeoLevelController level, LightsPicker lightsPicker, GameObject characterObject)
            {
                Camera camera = lightsPicker?.UsedCamera;
                if (camera != null)
                {
                    return camera;
                }

                camera = level.SceneReferences.UICaptureEnvironment?.CameraArm?.CaptureCamera;
                if (camera != null)
                {
                    return camera;
                }

                camera = characterObject.GetComponentsInChildren<Camera>(true).FirstOrDefault();
                if (camera != null)
                {
                    return camera;
                }

                Debug.LogWarning($"{LogPrefix} No portrait camera found; allowing SoldierPortraitUtil to create an internal camera.");
                return null;
            }

            private static float GetLightMultiplier(Light light)
            {
                if (light == null)
                {
                    return Profile.OtherLightMultiplier;
                }

                switch (light.type)
                {
                    case LightType.Directional:
                        return Profile.DirectionalLightMultiplier;
                    case LightType.Point:
                        return Profile.PointLightMultiplier;
                    case LightType.Spot:
                        return Profile.SpotLightMultiplier;
                    default:
                        return Profile.OtherLightMultiplier;
                }
            }

            private static CameraState CaptureCameraState(Camera camera)
            {
                if (camera == null)
                {
                    return default(CameraState);
                }

                return new CameraState
                {
                    FieldOfView = camera.fieldOfView,
                    NearClipPlane = camera.nearClipPlane,
                    FarClipPlane = camera.farClipPlane,
                    AllowHDR = camera.allowHDR
                };
            }

            private static void ApplyCameraOverrides(Camera camera)
            {
                if (camera == null)
                {
                    return;
                }

                camera.fieldOfView = Profile.CameraFoV;
                camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, Profile.MinCameraNearClip);
                camera.farClipPlane = Mathf.Min(camera.farClipPlane, Profile.MaxCameraFarClip);
                camera.allowHDR = false;
            }

            private static void RestoreCameraState(Camera camera, CameraState state)
            {
                if (camera == null)
                {
                    return;
                }

                camera.fieldOfView = state.FieldOfView;
                camera.nearClipPlane = state.NearClipPlane;
                camera.farClipPlane = state.FarClipPlane;
                camera.allowHDR = state.AllowHDR;
            }

            private static Texture2D ApplyPostProcess(Texture2D source, float gamma, float contrast, float sharpen)
            {
                if (source == null)
                {
                    return null;
                }

                Texture2D output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = 0; x < source.width; x++)
                    {
                        Color c = source.GetPixel(x, y);
                        c.r = Mathf.Pow(Mathf.Clamp01(c.r), gamma);
                        c.g = Mathf.Pow(Mathf.Clamp01(c.g), gamma);
                        c.b = Mathf.Pow(Mathf.Clamp01(c.b), gamma);

                        c.r = Mathf.Clamp01((c.r - 0.5f) * contrast + 0.5f);
                        c.g = Mathf.Clamp01((c.g - 0.5f) * contrast + 0.5f);
                        c.b = Mathf.Clamp01((c.b - 0.5f) * contrast + 0.5f);

                        if (x > 0 && x < source.width - 1 && y > 0 && y < source.height - 1)
                        {
                            Color n = source.GetPixel(x, y + 1);
                            Color s = source.GetPixel(x, y - 1);
                            Color e = source.GetPixel(x + 1, y);
                            Color w = source.GetPixel(x - 1, y);
                            Color edge = (n + s + e + w) * 0.25f;
                            c = Color.Lerp(c, c + (c - edge), sharpen);
                            c.r = Mathf.Clamp01(c.r);
                            c.g = Mathf.Clamp01(c.g);
                            c.b = Mathf.Clamp01(c.b);
                        }

                        output.SetPixel(x, y, c);
                    }
                }

                output.Apply(false, false);
                return output;
            }

            private static GameObject SelectLightRig(LightsPicker lightsPicker)
            {
                if (lightsPicker?.LightSet == null || lightsPicker.LightSet.Count == 0)
                {
                    return null;
                }

                IEnumerable<GameObject> candidates = lightsPicker.LightSet.Where(go => go != null);
                foreach (string token in Profile.LightNameTokens)
                {
                    GameObject match = candidates.FirstOrDefault(go => go.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null)
                    {
                        return match;
                    }
                }

                return candidates.FirstOrDefault();
            }

            private static bool IsHelmetOrAttachment(ItemDef armorItem)
            {
                if (armorItem?.RequiredSlotBinds == null)
                {
                    return false;
                }

                bool headAttachment = false;
                bool headSlot = false;

                foreach (AddonDef.RequiredSlotBind slotBind in armorItem.RequiredSlotBinds)
                {
                    ItemSlotDef slot = slotBind.RequiredSlot as ItemSlotDef;
                    if (slot == null)
                    {
                        continue;
                    }

                    if (slot.SlotName == "HeadAttachment")
                    {
                        headAttachment = true;
                    }
                    else if (slot.SlotName == "Head")
                    {
                        headSlot = true;
                    }
                }

                if (headAttachment)
                {
                    return true;
                }

                // For portraits, any item occupying the head slot is prone to detached-mesh artifacts.
                // Keep the actual face via tags; remove head-slot equipment entirely.
                return headSlot;
            }

            private static CoroutineRunner GetCoroutineRunner()
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                if (level == null)
                {
                    return null;
                }

                CoroutineRunner runner = level.GetComponent<CoroutineRunner>();
                return runner ?? level.gameObject.AddComponent<CoroutineRunner>();
            }

            private sealed class CoroutineRunner : MonoBehaviour
            {
            }

            private struct PortraitRenderProfile
            {
                public int Resolution;
                public float CameraFoV;
                public float NoseDistance;
                public float JawDistance;
                public float HeadDistance;
                public float AmbientIntensity;
                public float ReflectionIntensity;
                public float DirectionalLightMultiplier;
                public float PointLightMultiplier;
                public float SpotLightMultiplier;
                public float OtherLightMultiplier;
                public float MinCameraNearClip;
                public float MaxCameraFarClip;
                public bool ApplyPostProcess;
                public float PostGamma;
                public float PostContrast;
                public float PostSharpen;
                public string[] LightNameTokens;
            }

            private struct CameraState
            {
                public float FieldOfView;
                public float NearClipPlane;
                public float FarClipPlane;
                public bool AllowHDR;
            }
        }
    }
}*/

