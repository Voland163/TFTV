using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTagsSharedData;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
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

        // Module a portrait was last applied to, so the cache can detach its sprite from the
        // leader image before destroying it (a destroyed texture left assigned to the Image
        // renders as garbage — typically the last thing rendered, e.g. the personnel screen).
        private static UIModuleSiteEncounters _appliedModule;

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

        // Generous: the first rebuild of a session waits on addon assets being loaded from disk.
        private const float RebuildTimeoutSeconds = 20f;

        // Shader property the corruption (Delirium) face effect is driven by.
        private const string CorruptionShaderPropertyName = "_MaskContrast";

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

                // Detach first: destroying a texture that is still assigned to the leader Image
                // leaves it drawing freed GPU memory until a new sprite arrives.
                DetachPortraitFromUI();

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

        /// <summary>
        /// Clears the leader picture we put in place and hides the slot, so nothing stale is shown
        /// while the next portrait renders.
        /// </summary>
        private static void DetachPortraitFromUI()
        {
            try
            {
                if (_appliedModule == null)
                {
                    return;
                }

                if (_appliedModule.EncounterLeaderImage != null)
                {
                    _appliedModule.EncounterLeaderImage.sprite = null;
                }

                if (_appliedModule.EncunterLeaderGroup != null)
                {
                    _appliedModule.EncunterLeaderGroup.SetActive(false);
                }

                if (_appliedModule.EncunterLeaderInkGroup != null)
                {
                    _appliedModule.EncunterLeaderInkGroup.SetActive(false);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            finally
            {
                _appliedModule = null;
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
            _appliedModule = module;
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

            // Park the clone far away from the capture environment while it builds. Instantiate puts
            // it exactly where the source builder stands — the spot the personnel screen renders — so
            // leaving it there lets it show up in that view (vanilla's portrait builder does the same).
            tempBuilder.transform.position = new Vector3(1000f, 1000f, 1000f);
            tempBuilder.ProcessRigidbodiesAndJoints = false;
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

                // Mirror UIModuleActorCycle.DisplaySoldier: copy the character's tags (they carry the
                // customization — armor colors, patterns, face, hair) and pass ONLY armour items to the
                // rebuild, letting the addons manager derive the customized body parts from the tags.
                // Autorefresh stays OFF across the rebuild and is re-enabled once it completes: turning
                // it back on replays the deferred tag change, which is what fills MergeWithAddonsTags and
                // runs Item.RefreshTags() (the customization pass) on the freshly built addons.
                tempBuilder.AddonsManager.SetAutorefreshOnTagsChanged(false);
                tempBuilder.AddonsManager.GameTags.Clear();
                tempBuilder.AddonsManager.GameTags.AddRange(displayData.GameTags);

                List<ItemDef> armourItems = displayData.ArmourItems
                    .Where(i => i != null && !IsHelmetOrAttachment(i))
                    .ToList();

                if (armourItems.Count == 0)
                {
                    // Naked characters (e.g. civilians) have no armour to imply a body — fall back
                    // to their explicit body parts.
                    armourItems = character.GetBodyParts()
                        .OfType<ItemDef>()
                        .Where(i => i != null && !IsHelmetOrAttachment(i))
                        .Distinct()
                        .ToList();
                }

                CommonCharacterUtils.RebuildCharacter(tempBuilder, armourItems, null, null);

                // The first render of a session usually has to wait on addon assets: when an addon
                // is not loaded yet, StartRebuildCharacter hands off to the asset loader and only
                // starts rebuilding once loading finishes, which takes far longer than a couple of
                // frames. Wait on wall-clock time rather than a frame count.
                float waitDeadline = Time.realtimeSinceStartup + RebuildTimeoutSeconds;
                while (!rebuildDone && Time.realtimeSinceStartup < waitDeadline)
                {
                    yield return null;
                }

                if (!rebuildDone)
                {
                    Debug.LogWarning($"{LogPrefix} Character rebuild timed out after {RebuildTimeoutSeconds}s.");
                    onDone?.Invoke(null);
                    yield break;
                }

                tempBuilder.AddonsManager.SetAutorefreshOnTagsChanged(true);

                // Apply the customization (armor colors/patterns, skin, hair, eye colors) to the addons
                // that were just built. Re-enabling autorefresh above only replays the deferred tag
                // change when the tag set actually differs from what the cloned builder already had, so
                // do the work of AddonsManager.OnGameTagsChanged explicitly instead of relying on it.
                ApplyCustomizationTags(tempBuilder, character);
                ApplyFaceCorruption(tempBuilder, character, level);

                CommonCharacterUtils.ResetCharacterAnimation(tempBuilder);
                // Let skinned meshes settle (and the customization materials apply) to avoid
                // one-frame ghosting and untextured artifacts.
                yield return null;
                yield return null;

                // The rig carries its own character light (what lights the model on the personnel
                // screen) — make sure it is on at the proper intensity for the render.
                TryEnableCharacterLight(tempBuilder, displayData);

                Texture2D rendered = RenderTextureWithPortraitLights(level, tempBuilder.gameObject, resolution, true, out bool usedIsolation);

                // Layer isolation keeps the surrounding scene out of the portrait, but if it yields an
                // empty image (nothing matched the camera's single-layer mask) fall back to a plain
                // render so a portrait always appears.
                if (usedIsolation && (rendered == null || !HasVisibleContent(rendered)))
                {
                    TFTVLogger.Always($"{LogPrefix} Isolated render produced no visible pixels; retrying without isolation.");

                    if (rendered != null)
                    {
                        UnityEngine.Object.Destroy(rendered);
                    }

                    RestoreIsolatedLayers();
                    rendered = RenderTextureWithPortraitLights(level, tempBuilder.gameObject, resolution, false, out _);
                }

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

        /// <summary>
        /// Recomputes the addons manager's merged tag list and refreshes every addon from it —
        /// the same work AddonsManager.OnGameTagsChanged does. Item.RefreshTags() reads
        /// MergeWithAddonsTags to tint materials, so without this the model renders with default
        /// (untinted) materials: no armor colors or patterns, and pale skin/hair/eyes.
        /// </summary>
        private static void ApplyCustomizationTags(AddonsCharacterBuilder builder, GeoCharacter character)
        {
            try
            {
                AddonsManager manager = builder?.AddonsManager;
                if (manager?.RootAddon == null || character == null)
                {
                    return;
                }

                // Source the tags from the character rather than from manager.GameTags: the rebuild
                // manipulates the manager's tag list, so by this point it may no longer carry the
                // identity's customization tags.
                List<GameTagDef> mergeable = character.GameTags
                    .Where(tag => tag != null && AddonMergeGameTagsWithManagerAttribute.ShouldAddonMergeTagsWithAddonManager(tag.GetType()))
                    .ToList();

                manager.MergeWithAddonsTags.ReplaceRange(mergeable);

                int refreshed = 0;
                foreach (Addon addon in manager.RootAddon)
                {
                    if (addon == null)
                    {
                        continue;
                    }

                    addon.RefreshTags();
                    refreshed++;
                }

                // Item.RefreshTags() skips color customization entirely when the merged tags contain
                // the conditional-customization tag, unless the item opts in via AlwaysCustomizeColor.
                bool conditional = mergeable.Any(t => t is ConditionalCustomizationTagDef);

                TFTVLogger.Always($"{LogPrefix} Customization for {character.DisplayName}: " +
                    $"characterTags={character.GameTags.Count} mergeable={mergeable.Count} " +
                    $"colorTags={mergeable.Count(t => t is CustomizationColorTagDef)} " +
                    $"patternTags={mergeable.Count(t => t is CustomizationPatternTagDef)} " +
                    $"conditionalTag={conditional} addonsRefreshed={refreshed}");

                ForceApplyCustomizationColors(manager, mergeable);
                LogAddonCustomizationState(manager, mergeable);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Diagnostic: reports, per built item, whether it is in a state where the customization pass
        /// can tint it (visual root present, highlightable renderers found, AlwaysCustomizeColor).
        /// </summary>
        private static void LogAddonCustomizationState(AddonsManager manager, List<GameTagDef> mergeable)
        {
            try
            {
                int logged = 0;
                foreach (Addon addon in manager.RootAddon)
                {
                    Item item = addon as Item;
                    if (item == null || logged >= 8)
                    {
                        continue;
                    }

                    int rendererCount = 0;
                    try
                    {
                        IEnumerable<Renderer> renderers = item.GetHighlightableRenderers();
                        rendererCount = renderers == null ? -1 : renderers.Count();
                    }
                    catch
                    {
                        rendererCount = -2;
                    }

                    // Read the tint back off the first renderer: an unset property reads as
                    // (0,0,0,0), which distinguishes "customization never applied" from
                    // "applied but looks unchanged".
                    string tintReadback = "n/a";
                    List<CustomizationColorTagDef> colorTags = mergeable?.OfType<CustomizationColorTagDef>().ToList();
                    if (colorTags != null && colorTags.Count > 0 && rendererCount > 0)
                    {
                        try
                        {
                            Renderer first = item.GetHighlightableRenderers().FirstOrDefault();
                            if (first != null)
                            {
                                MaterialPropertyBlock readback = new MaterialPropertyBlock();
                                first.GetPropertyBlock(readback);
                                tintReadback = string.Join(" ", colorTags
                                    .Select(t => $"{t.ShaderParamName}={readback.GetColor(t.ShaderParamName)}")
                                    .ToArray());
                            }
                        }
                        catch (Exception readbackError)
                        {
                            tintReadback = "error:" + readbackError.GetType().Name;
                        }
                    }

                    TFTVLogger.Always($"{LogPrefix}   item={item.ItemDef?.name ?? "null"} " +
                        $"visualRoot={(item.VisualRoot != null)} renderers={rendererCount} " +
                        $"alwaysCustomize={item.ItemDef?.AlwaysCustomizeColor} tint[{tintReadback}]");
                    logged++;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Applies the corruption (Delirium) face shader, mirroring
        /// UIModuleActorCycle.SetupFaceCorruptionShader.
        /// </summary>
        private static void ApplyFaceCorruption(AddonsCharacterBuilder builder, GeoCharacter character, GeoLevelController level)
        {
            try
            {
                if (character == null || character.IsMutoid || level == null)
                {
                    return;
                }

                AddonsManager manager = builder?.AddonsManager;
                if (manager?.RootAddon == null || manager.RigRoot == null)
                {
                    return;
                }

                if ((float)character.CharacterStats.Corruption <= 0f)
                {
                    return;
                }

                TacticalPerceptionDef perception = character.TemplateDef?.ComponentSetDef?.GetComponentDef<TacticalPerceptionDef>();
                if (perception == null)
                {
                    return;
                }

                AddonSlot headSlot = manager.RootAddon.FindAddonSlot(perception.HeadSlot);
                if (headSlot == null)
                {
                    return;
                }

                float shaderValue = level.CorruptedHorizonsSettings.CorruptionSettings
                    .CalculateCorruptionShaderValue(character.CharacterStats.CorruptionProgressRel);

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetFloat(CorruptionShaderPropertyName, shaderValue);

                foreach (TacticalItem item in ((ItemSlot)headSlot).GetAllDirectItems(onlyBodyparts: true))
                {
                    if (item == null)
                    {
                        continue;
                    }

                    bool isFace = item.OwnTags.Count == 0
                        ? item.GameTags.Any(t => t is FaceTagDef)
                        : item.OwnTags.Any(t => t is FaceTagDef);

                    if (!isFace)
                    {
                        continue;
                    }

                    foreach (Renderer renderer in item.GetHighlightableRenderers())
                    {
                        renderer?.SetPropertyBlock(propertyBlock);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Enables the rig's built-in character light (referenced by the template's
        /// CharacterLightObjectName) at the intensity the personnel screen uses.
        /// </summary>
        private static bool TryEnableCharacterLight(AddonsCharacterBuilder builder, UnitDisplayData displayData)
        {
            try
            {
                if (builder?.AddonsManager == null || string.IsNullOrEmpty(displayData?.CharacterLightObjectName))
                {
                    return false;
                }

                Transform lightTransform = builder.AddonsManager.FindTransform(displayData.CharacterLightObjectName, rigBonesOnly: true);
                Light characterLight = lightTransform != null ? lightTransform.GetComponent<Light>() : null;
                if (characterLight == null)
                {
                    TFTVLogger.Always($"{LogPrefix} Character light '{displayData.CharacterLightObjectName}' not found on builder rig.");
                    return false;
                }

                characterLight.gameObject.SetActive(true);
                characterLight.enabled = true;
                if (LightingSettingsCharacters.Instance != null)
                {
                    characterLight.intensity = LightingSettingsCharacters.Instance.CharacterLightsIntensity;
                }

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static Texture2D RenderTextureWithPortraitLights(GeoLevelController level, GameObject characterObject, Vector2Int resolution, bool allowIsolation, out bool usedIsolation)
        {
            LightsPicker lightsPicker = characterObject.GetComponentInChildren<LightsPicker>(true);

            // Always render with the game's own portrait/capture camera: a bare camera created from
            // scratch renders nothing here. Isolation is done by narrowing THAT camera's culling mask
            // to a free layer holding only this character, and is undone with the rest of its state.
            Camera usedCamera = ResolvePortraitCamera(level, lightsPicker, characterObject);
            usedIsolation = false;

            HashSet<Light> worldLightsToRestore = new HashSet<Light>();
            GameObject syntheticRig = null;
            List<GameObject> hiddenVisuals = null;

            float ambientIntensityBefore = RenderSettings.ambientIntensity;
            Color ambientLightBefore = RenderSettings.ambientLight;
            UnityEngine.Rendering.AmbientMode ambientModeBefore = RenderSettings.ambientMode;
            float reflectionBefore = RenderSettings.reflectionIntensity;

            CameraState cameraState = CaptureCameraState(usedCamera);

            try
            {
                // Mirror the vanilla tactical squad-portrait setup (SquadMemberScrollerController.FinishPortraitCrt):
                // ambient (forced to Flat so this works regardless of the scene's ambient source) and
                // reflections off, all world lights disabled, and a dedicated portrait rig at full intensity.
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.26f, 0.27f, 0.30f);
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.reflectionIntensity = 0f;
                ApplyCameraOverrides(usedCamera);

                // When RenderingEnvironment creates the camera internally (the usual case here) it has
                // no culling restrictions, so anything else standing at the render origin lands in the
                // portrait - that is the personnel screen model and backdrop. Hide those meanwhile.
                hiddenVisuals = HideOtherSceneVisuals(level, characterObject, usedCamera);

                if (allowIsolation && usedCamera != null && TryIsolateOnFreeLayer(characterObject, out int isolationLayer))
                {
                    usedCamera.cullingMask = 1 << isolationLayer;
                    usedIsolation = true;
                }

                foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
                {
                    if (light == null || !light.isActiveAndEnabled)
                    {
                        continue;
                    }

                    // Never disable the character's own lights (rig character light, portrait rigs).
                    if (light.transform.IsChildOf(characterObject.transform))
                    {
                        continue;
                    }

                    worldLightsToRestore.Add(light);
                    light.gameObject.SetActive(false);
                }

                if (lightsPicker != null && lightsPicker.LightSet != null && lightsPicker.LightSet.Count > 0)
                {
                    // Tactical-style builder: use its own portrait rigs, like vanilla does.
                    lightsPicker.PickLights();
                }
                else
                {
                    // The geoscape capture-environment builder carries no LightsPicker (its lighting
                    // lives in the environment scene, out of reach at the render origin), so light the
                    // character with a synthetic three-point rig. The rig's own character light is far
                    // too dim to carry the portrait on its own (CharacterLightsIntensity defaults to
                    // 0.15), so it only ever supplements this rig.
                    syntheticRig = CreatePortraitLightRig(characterObject.transform);
                }

                return RenderWithAnchorFallback(characterObject, usedCamera, resolution);
            }
            finally
            {
                RestoreCameraState(usedCamera, cameraState);

                if (hiddenVisuals != null)
                {
                    foreach (GameObject hidden in hiddenVisuals)
                    {
                        if (hidden != null)
                        {
                            hidden.SetActive(true);
                        }
                    }
                }

                if (syntheticRig != null)
                {
                    UnityEngine.Object.Destroy(syntheticRig);
                }

                foreach (Light light in worldLightsToRestore)
                {
                    if (light != null)
                    {
                        light.gameObject.SetActive(true);
                    }
                }

                lightsPicker?.DisableAllControlledLights();
                RenderSettings.ambientMode = ambientModeBefore;
                RenderSettings.ambientLight = ambientLightBefore;
                RenderSettings.ambientIntensity = ambientIntensityBefore;
                RenderSettings.reflectionIntensity = reflectionBefore;
            }
        }

        /// <summary>
        /// Three-point portrait rig out of directional lights, parented to the character so the angles
        /// stay relative to where the face points. Directional lights are position-independent, so they
        /// work at the far-away render origin where scene point/spot lights cannot reach. Angles are
        /// kept close to horizontal so the top of the head does not blow out.
        /// </summary>
        private static GameObject CreatePortraitLightRig(Transform character)
        {
            GameObject rig = new GameObject("[TFTV]PortraitLightRig");
            rig.transform.SetParent(character, false);

            // Key: warm, from the character's front-left, slightly above eye level.
            AddRigLight(rig.transform, "Key", new Vector3(15f, 205f, 0f), new Color(1f, 0.96f, 0.90f), 1.05f);
            // Fill: cool and soft, from the front-right, at eye level — also lifts the armor.
            AddRigLight(rig.transform, "Fill", new Vector3(5f, 150f, 0f), new Color(0.78f, 0.82f, 0.92f), 0.55f);
            // Rim: from behind at eye level, separates hair/shoulders from the dark background.
            // Kept low so it does not burn bright edges onto the face.
            AddRigLight(rig.transform, "Rim", new Vector3(0f, 20f, 0f), new Color(0.90f, 0.93f, 1f), 0.20f);

            return rig;
        }

        private static void AddRigLight(Transform rig, string name, Vector3 eulerAngles, Color color, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(rig, false);
            go.transform.localRotation = Quaternion.Euler(eulerAngles);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
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

        /// <summary>
        /// Moves the character (and everything under it) onto an isolated layer and returns a private
        /// camera that renders only that layer. RenderingEnvironment never restricts the culling mask,
        /// so a shared camera captures whatever else happens to sit at the render origin — which is how
        /// the personnel screen's model and backdrop ended up in the portrait. The clone is destroyed
        /// after the render, so its layers do not need restoring.
        /// </summary>
        // Original layers of everything moved for an isolated render, so a fallback render can undo it.
        private static readonly List<KeyValuePair<GameObject, int>> _isolatedLayers = new List<KeyValuePair<GameObject, int>>();

        private static void RecordLayer(GameObject target)
        {
            if (target != null)
            {
                _isolatedLayers.Add(new KeyValuePair<GameObject, int>(target, target.layer));
            }
        }

        private static void RestoreIsolatedLayers()
        {
            foreach (KeyValuePair<GameObject, int> entry in _isolatedLayers)
            {
                if (entry.Key != null)
                {
                    entry.Key.layer = entry.Value;
                }
            }

            _isolatedLayers.Clear();
        }

        /// <summary>
        /// True when the render actually produced something — sampled sparsely, since a fully
        /// transparent result means the camera saw nothing.
        /// </summary>
        private static bool HasVisibleContent(Texture2D texture)
        {
            try
            {
                if (texture == null)
                {
                    return false;
                }

                Color32[] pixels = texture.GetPixels32();
                if (pixels == null || pixels.Length == 0)
                {
                    return false;
                }

                int visible = 0;
                int step = Mathf.Max(1, pixels.Length / 4096);
                for (int i = 0; i < pixels.Length; i += step)
                {
                    if (pixels[i].a > 16)
                    {
                        visible++;
                        if (visible > 16)
                        {
                            return true;
                        }
                    }
                }

                TFTVLogger.Always($"{LogPrefix} Render appears empty (visible samples={visible}).");
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return true;
            }
        }

        /// <summary>
        /// Temporarily deactivates other character models and the capture environment so they cannot
        /// appear behind the portrait. Returns what was hidden so it can be switched back on.
        /// </summary>
        private static List<GameObject> HideOtherSceneVisuals(GeoLevelController level, GameObject characterObject, Camera usedCamera)
        {
            List<GameObject> hidden = new List<GameObject>();

            try
            {
                foreach (AddonsCharacterBuilder builder in UnityEngine.Object.FindObjectsOfType<AddonsCharacterBuilder>())
                {
                    GameObject candidate = builder?.gameObject;
                    if (candidate == null
                        || candidate == characterObject
                        || candidate.transform.IsChildOf(characterObject.transform)
                        || !candidate.activeSelf)
                    {
                        continue;
                    }

                    candidate.SetActive(false);
                    hidden.Add(candidate);
                }

                GameObject captureEnvironment = level?.SceneReferences?.UICaptureEnvironment?.gameObject;
                bool cameraLivesThere = usedCamera != null
                    && captureEnvironment != null
                    && usedCamera.transform.IsChildOf(captureEnvironment.transform);

                if (captureEnvironment != null
                    && captureEnvironment.activeSelf
                    && !cameraLivesThere
                    && !characterObject.transform.IsChildOf(captureEnvironment.transform))
                {
                    captureEnvironment.SetActive(false);
                    hidden.Add(captureEnvironment);
                }

                if (hidden.Count > 0)
                {
                    TFTVLogger.Always($"{LogPrefix} Hid {hidden.Count} scene object(s) for the portrait render.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return hidden;
        }

        /// <summary>
        /// Applies the customization colors directly to every built item. Item.RefreshTags() skips
        /// colors whenever the merged tags carry the conditional-customization tag (which this
        /// character has) unless the item opts in with AlwaysCustomizeColor, which leaves armor
        /// looking untinted in the portrait.
        /// </summary>
        private static void ForceApplyCustomizationColors(AddonsManager manager, List<GameTagDef> mergeable)
        {
            try
            {
                SharedGameTagsDataDef shared = GameUtl.GameComponent<SharedData>()?.SharedGameTags;
                HumanCustomizationDef customization = shared?.HumanCustomization;
                if (customization == null || manager?.RootAddon == null)
                {
                    return;
                }

                bool anyFancy = mergeable.Any(tag => tag is CustomizationFancyTagDef);
                int customized = 0;

                foreach (Addon addon in manager.RootAddon)
                {
                    Item item = addon as Item;
                    if (item?.VisualRoot == null)
                    {
                        continue;
                    }

                    HighlightControllerComponent controller = item.VisualRoot.gameObject.GetComponent<HighlightControllerComponent>();
                    if (controller == null)
                    {
                        continue;
                    }

                    controller.StartCustomization();

                    foreach (GameTagDef tag in mergeable)
                    {
                        CustomizationColorTagDef colorTag = tag as CustomizationColorTagDef;
                        if (colorTag != null)
                        {
                            Color color = customization.CustomizationPaletteDef.ContainsColor(colorTag)
                                ? customization.CustomizationPaletteDef.MatchColor(colorTag)
                                : (anyFancy
                                    ? customization.FancyVehicleCustomizationPaletteDef.MatchColor(colorTag)
                                    : customization.NPCPaletteDef.MatchColor(colorTag));

                            controller.CustomizeColor(colorTag.ShaderParamName, color);
                            continue;
                        }

                        CustomizationFancyTagDef fancyTag = tag as CustomizationFancyTagDef;
                        if (fancyTag != null)
                        {
                            controller.CustomizeFancy(fancyTag);
                            continue;
                        }

                        CustomizationPatternTagDef patternTag = tag as CustomizationPatternTagDef;
                        if (patternTag != null)
                        {
                            controller.CustomizePattern(patternTag);
                        }
                    }

                    controller.ApplyCustomization();
                    customized++;
                }

                TFTVLogger.Always($"{LogPrefix} Force-applied customization to {customized} item(s).");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Moves the character's renderers onto a free layer so the portrait camera can be narrowed
        /// to just them. Original layers are recorded BEFORE anything is changed, so they can be put
        /// back if the isolated render turns out empty.
        /// </summary>
        private static bool TryIsolateOnFreeLayer(GameObject characterObject, out int layer)
        {
            layer = ResolveIsolationLayer();
            if (layer < 0)
            {
                TFTVLogger.Always($"{LogPrefix} No free layer for portrait isolation; rendering without it.");
                return false;
            }

            // Culling keys off each renderer's own GameObject layer, so move those explicitly rather
            // than trusting that every visual is a transform child of the builder.
            Renderer[] renderers = characterObject.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                TFTVLogger.Always($"{LogPrefix} No renderers under the builder; rendering without isolation.");
                return false;
            }

            _isolatedLayers.Clear();
            foreach (Transform transform in characterObject.GetComponentsInChildren<Transform>(true))
            {
                RecordLayer(transform?.gameObject);
            }

            foreach (Renderer renderer in renderers)
            {
                RecordLayer(renderer?.gameObject);
            }

            SetLayerRecursively(characterObject, layer);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.gameObject.layer = layer;
                }
            }

            TFTVLogger.Always($"{LogPrefix} Isolated {renderers.Length} renderer(s) on layer {layer} for the portrait render.");
            return true;
        }

        /// <summary>
        /// Highest layer with no name assigned — unnamed layers are unused by the project, so nothing
        /// else in the scene renders on it.
        /// </summary>
        private static int ResolveIsolationLayer()
        {
            for (int layer = 31; layer >= 8; layer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
                {
                    return layer;
                }
            }

            return -1;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
            {
                return;
            }

            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
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
                AllowHDR = camera.allowHDR,
                CullingMask = camera.cullingMask
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
            camera.cullingMask = state.CullingMask;
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
            public int CullingMask;
        }
    }
}
