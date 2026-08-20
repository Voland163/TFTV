using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Geoscape.Entities;
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

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// Renders the currently selected incident operative's face and shows it in the
    /// encounter UI's leader picture slot. Driven by IncidentResolutionUI leader selection.
    /// </summary>
    internal static class PortraitGenerator
    {
        private const string LogPrefix = "[TFTV][PortraitGenerator]";

        // Rendered portraits per GeoCharacter id, valid for the currently shown incident.
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
        private static readonly HashSet<int> InProgress = new HashSet<int>();
        private static int _currentRequestId = -1;

        private static readonly PortraitRenderProfile Profile = new PortraitRenderProfile
        {
            CameraFoV = 40f,
            NoseDistance = 0.80f,
            JawDistance = 0.92f,
            HeadDistance = 0.88f,
            MinCameraNearClip = 0.02f,
            MaxCameraFarClip = 25f,
            ApplyPostProcess = false,
            PostGamma = 0.95f,
            PostContrast = 1.05f,
            PostSharpen = 0.08f
        };

        // Render target is sized to the leader pic slot's on-screen size (clamped),
        // so the portrait matches the display resolution instead of a fixed 1024px.
        private const int MinPortraitResolution = 128;
        private const int MaxPortraitResolution = 1024;
        private const int FallbackPortraitResolution = 512;

        /// <summary>
        /// Shows the portrait of the given operative in the leader pic slot,
        /// rendering it if it is not cached yet.
        /// </summary>
        internal static void RequestLeaderPortrait(UIModuleSiteEncounters module, GeoCharacter character)
        {
            try
            {
                if (module == null || character == null || character.Id <= 0)
                {
                    return;
                }

                _currentRequestId = character.Id;

                if (Cache.TryGetValue(character.Id, out Sprite cached) && cached != null)
                {
                    ApplyPortrait(module, cached);
                    return;
                }

                if (InProgress.Contains(character.Id))
                {
                    return;
                }

                CoroutineRunner runner = GetCoroutineRunner();
                if (runner != null)
                {
                    runner.StartCoroutine(RenderAndApply(module, character));
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Frees all cached portraits. Called when a new incident is shown and on level load/state change.
        /// </summary>
        internal static void ClearCache()
        {
            try
            {
                _currentRequestId = -1;

                foreach (Sprite sprite in Cache.Values)
                {
                    if (sprite == null)
                    {
                        continue;
                    }

                    if (sprite.texture != null)
                    {
                        UnityEngine.Object.Destroy(sprite.texture);
                    }

                    UnityEngine.Object.Destroy(sprite);
                }

                Cache.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyPortrait(UIModuleSiteEncounters module, Sprite portrait)
        {
            if (module == null || portrait == null)
            {
                return;
            }

            module.EncunterLeaderGroup.SetActive(true);
            module.EncunterLeaderInkGroup.SetActive(true);
            module.EncounterLeaderImage.sprite = portrait;
            module.EncounterLeaderImage.preserveAspect = true;
        }

        private static IEnumerator RenderAndApply(UIModuleSiteEncounters module, GeoCharacter character)
        {
            int characterId = character.Id;
            Vector2Int resolution = ResolvePortraitResolution(module);
            InProgress.Add(characterId);
            try
            {
                Sprite portrait = null;
                yield return RenderPortrait(character, resolution, s => portrait = s);

                if (portrait == null)
                {
                    yield break;
                }

                // Replace any stale entry (should not normally exist).
                if (Cache.TryGetValue(characterId, out Sprite stale) && stale != null && stale != portrait)
                {
                    if (stale.texture != null)
                    {
                        UnityEngine.Object.Destroy(stale.texture);
                    }

                    UnityEngine.Object.Destroy(stale);
                }

                Cache[characterId] = portrait;

                // Only apply if this operative is still the selected one.
                if (module != null && module.isActiveAndEnabled && _currentRequestId == characterId)
                {
                    ApplyPortrait(module, portrait);
                }
            }
            finally
            {
                InProgress.Remove(characterId);
            }
        }

        /// <summary>
        /// On-screen pixel size of the leader pic slot, so the render matches the display resolution.
        /// </summary>
        private static Vector2Int ResolvePortraitResolution(UIModuleSiteEncounters module)
        {
            try
            {
                RectTransform rect = module?.EncounterLeaderImage?.rectTransform;
                if (rect != null)
                {
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);

                    float width;
                    float height;

                    Canvas canvas = module.EncounterLeaderImage.canvas;
                    if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
                    {
                        Vector2 min = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
                        Vector2 max = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
                        width = Mathf.Abs(max.x - min.x);
                        height = Mathf.Abs(max.y - min.y);
                    }
                    else
                    {
                        // Screen-space overlay: world corners are already screen pixels.
                        width = Mathf.Abs(corners[2].x - corners[0].x);
                        height = Mathf.Abs(corners[2].y - corners[0].y);
                    }

                    if (width > 1f && height > 1f)
                    {
                        return new Vector2Int(
                            Mathf.Clamp(Mathf.RoundToInt(width), MinPortraitResolution, MaxPortraitResolution),
                            Mathf.Clamp(Mathf.RoundToInt(height), MinPortraitResolution, MaxPortraitResolution));
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return new Vector2Int(FallbackPortraitResolution, FallbackPortraitResolution);
        }

        private static IEnumerator RenderPortrait(GeoCharacter character, Vector2Int resolution, Action<Sprite> onDone)
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

                Texture2D rendered = RenderTextureWithPortraitLights(level, tempBuilder.gameObject, resolution);
                if (rendered == null)
                {
                    onDone?.Invoke(null);
                    yield break;
                }

                // The render already carries mipmaps; trilinear + anisotropic filtering keeps it
                // clean when the UI displays it slightly scaled.
                rendered.filterMode = FilterMode.Trilinear;
                rendered.anisoLevel = 4;

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

        private static Texture2D RenderTextureWithPortraitLights(GeoLevelController level, GameObject characterObject, Vector2Int resolution)
        {
            LightsPicker lightsPicker = characterObject.GetComponentInChildren<LightsPicker>(true);
            Camera usedCamera = ResolvePortraitCamera(level, lightsPicker, characterObject);

            HashSet<Light> worldLightsToRestore = new HashSet<Light>();

            float ambientBefore = RenderSettings.ambientIntensity;
            float reflectionBefore = RenderSettings.reflectionIntensity;

            CameraState cameraState = CaptureCameraState(usedCamera);

            try
            {
                // Mirror the vanilla tactical squad-portrait setup (SquadMemberScrollerController.FinishPortraitCrt):
                // ambient and reflections fully off, all world lights disabled, and the LightsPicker
                // enabling one of its portrait rigs at full intensity.
                RenderSettings.ambientIntensity = 0f;
                RenderSettings.reflectionIntensity = 0f;
                ApplyCameraOverrides(usedCamera);

                foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
                {
                    if (light != null && light.isActiveAndEnabled)
                    {
                        worldLightsToRestore.Add(light);
                        light.gameObject.SetActive(false);
                    }
                }

                lightsPicker?.PickLights();

                return RenderWithAnchorFallback(characterObject, usedCamera, resolution);
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

                lightsPicker?.DisableAllControlledLights();
                RenderSettings.ambientIntensity = ambientBefore;
                RenderSettings.reflectionIntensity = reflectionBefore;
            }
        }

        private static Texture2D RenderWithAnchorFallback(GameObject characterObject, Camera usedCamera, Vector2Int resolution)
        {
            string[] anchors = { "Nose", "Jaw", "Head" };
            float[] distances = { Profile.NoseDistance, Profile.JawDistance, Profile.HeadDistance };

            for (int i = 0; i < anchors.Length; i++)
            {
                var p = new SquadPortraitsDef.RenderPortraitParams
                {
                    RenderedPortraitsResolution = resolution,
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
                RenderedPortraitsResolution = resolution,
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

            // Only clamp the clip planes for the close-up; FoV comes from the render params
            // and HDR is left as-is, matching the vanilla portrait path.
            camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, Profile.MinCameraNearClip);
            camera.farClipPlane = Mathf.Min(camera.farClipPlane, Profile.MaxCameraFarClip);
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
            public float CameraFoV;
            public float NoseDistance;
            public float JawDistance;
            public float HeadDistance;
            public float MinCameraNearClip;
            public float MaxCameraFarClip;
            public bool ApplyPostProcess;
            public float PostGamma;
            public float PostContrast;
            public float PostSharpen;
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
