using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Animations;
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
    ///
    /// The character is built on a private builder of our own and handed to the game's own
    /// portrait renderer. Everything about the build mirrors UIModuleActorCycle.DisplaySoldier
    /// (the personnel screen), so a portrait shows the same armour, colours and face the
    /// personnel screen shows for that operative.
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
        // renders as garbage).
        private static UIModuleSiteEncounters _appliedModule;

        // Render target is sized to the leader pic slot's on-screen size (clamped), so the
        // portrait matches the display resolution instead of a fixed 1024px.
        private const int MinPortraitResolution = 128;
        private const int MaxPortraitResolution = 1024;
        private const int FallbackPortraitResolution = 512;

        // Framing of the head close-up.
        private const float CameraFoV = 40f;
        private const float NoseDistance = 0.80f;
        private const float HeadDistance = 0.88f;

        // Generous: the first build of a session waits on addon assets being loaded from disk.
        private const float RebuildTimeoutSeconds = 20f;

        // Where the subject stands while it builds - far from anything the geoscape camera sees.
        // The renderer moves it to its own staging origin for the actual render.
        private static readonly Vector3 SubjectStagingPosition = new Vector3(1000f, 1000f, 1000f);

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

                GetCoroutineRunner()?.StartCoroutine(RenderAndApply(module, character));
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

                _appliedModule.EncunterLeaderGroup?.SetActive(false);
                _appliedModule.EncunterLeaderInkGroup?.SetActive(false);
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
            if (level == null)
            {
                onDone?.Invoke(null);
                yield break;
            }

            UnitDisplayData displayData = new UnitDisplayData(character, GameUtl.GameComponent<SharedData>());
            AddonsCharacterBuilder builder = CreateSubjectBuilder();
            try
            {
                bool built = false;
                yield return BuildSubject(builder, character, displayData, level, ok => built = ok);
                if (!built)
                {
                    onDone?.Invoke(null);
                    yield break;
                }

                Texture2D rendered = RenderSubject(builder, displayData, resolution);
                if (rendered == null)
                {
                    onDone?.Invoke(null);
                    yield break;
                }

                // The render already carries mipmaps; trilinear + anisotropic filtering keeps it
                // clean when the UI displays it slightly scaled.
                rendered.filterMode = FilterMode.Trilinear;
                rendered.anisoLevel = 4;

                onDone?.Invoke(Sprite.Create(
                    rendered,
                    new Rect(0f, 0f, rendered.width, rendered.height),
                    new Vector2(0.5f, 0.5f),
                    100f));
            }
            finally
            {
                UnityEngine.Object.Destroy(builder.gameObject);
            }
        }

        /// <summary>
        /// A character builder of our own, built from nothing.
        ///
        /// It must not be cloned off a scene builder (UICaptureEnvironment / SquadBay): Instantiate
        /// copies the meshes that builder currently displays - the soldier the personnel screen was
        /// last showing - and those copies belong to no addons manager, so nothing ever removes them
        /// and they end up in the portrait alongside the operative.
        /// </summary>
        private static AddonsCharacterBuilder CreateSubjectBuilder()
        {
            // Inactive first: Awake runs on activation, and it must see the fields below.
            GameObject host = new GameObject("[TFTV]PortraitSubject");
            host.SetActive(false);
            host.transform.position = SubjectStagingPosition;

            AddonsCharacterBuilder builder = host.AddComponent<AddonsCharacterBuilder>();
            builder.AddonsManagerDef = null;   // DisplayCharacter installs the operative's own rig.
            builder.TacCharacterDef = null;
            builder.Addons.Clear();
            builder.ProcessRigidbodiesAndJoints = false;

            // DisplayCharacter routes the rig's animator controller through this component.
            host.AddComponent<TacActorAnimActions>();

            host.SetActive(true);
            return builder;
        }

        /// <summary>
        /// Builds the operative on the given builder, following UIModuleActorCycle.DisplaySoldier and
        /// its OnCharacterRebuilded step: tags first with autorefresh off, rebuild, then autorefresh
        /// back on - which is what applies the customization (armour colours and patterns, skin, hair,
        /// eyes) to the addons that were just built.
        /// </summary>
        private static IEnumerator BuildSubject(AddonsCharacterBuilder builder, GeoCharacter character, UnitDisplayData displayData, GeoLevelController level, Action<bool> onDone)
        {
            bool rebuilt = false;
            Action onRebuilt = () => rebuilt = true;
            builder.OnCharacterRebuilded += onRebuilt;
            try
            {
                CommonCharacterUtils.DisplayCharacter(builder, displayData, out bool _);

                AddonsManager manager = builder.AddonsManager;
                manager.SetAutorefreshOnTagsChanged(false);
                manager.GameTags.Clear();
                manager.GameTags.AddRange(displayData.GameTags);

                // A character's own tag list can still be carrying the template's default
                // customization - flat grey armour, no pattern - because GeoCharacter.ReinitTags
                // applies the identity's tags last and only runs when the character's stats are
                // recalculated. Opening the personnel screen is one of the things that triggers it,
                // which is why a portrait taken beforehand came out uncustomized and the same
                // operative looked right afterwards. Merge the identity over the character's tags
                // the way ReinitTags does, on our own builder's list rather than on the character.
                character.Identity?.ApplyGameTags(manager.GameTags);

                // Helmets and head attachments hide the face the portrait is about; this is vanilla's
                // showHelmet: false. The body itself comes from the armour items, exactly as on the
                // personnel screen - an unarmoured operative carries their bare body parts there.
                List<ItemDef> armour = displayData.ArmourItems
                    .Where(item => item != null && !IsHelmetOrAttachment(item))
                    .ToList();

                CommonCharacterUtils.RebuildCharacter(builder, armour, null);

                // When an addon is not loaded yet the rebuild hands off to the asset loader and only
                // starts once loading finishes, which takes far longer than a couple of frames.
                float deadline = Time.realtimeSinceStartup + RebuildTimeoutSeconds;
                while (!rebuilt && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                if (!rebuilt)
                {
                    TFTVLogger.Always($"{LogPrefix} Rebuild of {character.DisplayName} timed out after {RebuildTimeoutSeconds}s.");
                    onDone?.Invoke(false);
                    yield break;
                }

                manager.SetAutorefreshOnTagsChanged(true);
                RefreshAddonTags(manager);
                HideCoveredAddonVisuals(manager);
                ApplyFaceCorruption(builder, character, level);
                CommonCharacterUtils.ResetCharacterAnimation(builder);

                // One line per portrait: what the operative is actually made of. Anything unexpected
                // in here (a bare body part next to the armour that covers it, a missing armour piece)
                // is what a wrong-looking portrait will be made of too.
                TFTVLogger.Always($"{LogPrefix} {character.DisplayName} built from {string.Join(", ", VisibleItemNames(manager))} " +
                    $"| customization {string.Join(", ", CustomizationTagNames(manager))}");

                // Let the skinned meshes settle into the pose before the render.
                yield return null;

                onDone?.Invoke(true);
            }
            finally
            {
                builder.OnCharacterRebuilded -= onRebuilt;
            }
        }

        /// <summary>
        /// Recomputes the manager's merged tag list and refreshes every addon from it - the same work
        /// AddonsManager.OnGameTagsChanged does when autorefresh is turned back on. Doing it explicitly
        /// costs nothing and makes the customization pass unconditional rather than dependent on the
        /// tag list's change bookkeeping.
        /// </summary>
        private static void RefreshAddonTags(AddonsManager manager)
        {
            if (manager?.RootAddon == null)
            {
                return;
            }

            manager.MergeWithAddonsTags.ReplaceRange(manager.GameTags
                .Where(tag => tag != null && AddonMergeGameTagsWithManagerAttribute.ShouldAddonMergeTagsWithAddonManager(tag.GetType())));

            foreach (Addon addon in manager.RootAddon)
            {
                addon?.RefreshTags();
            }
        }

        /// <summary>
        /// Hides the visuals of every addon that a stronger addon in the same slot covers - the bare
        /// body parts under an armour piece, mainly. The engine does this as addons attach; enforcing
        /// it once more after the build keeps a bare torso or arm from showing through the armour when
        /// an attach order left it visible.
        /// </summary>
        private static void HideCoveredAddonVisuals(AddonsManager manager)
        {
            if (manager?.RootAddon == null)
            {
                return;
            }

            foreach (Addon addon in manager.RootAddon)
            {
                foreach (Addon.AddonSlotImpl slot in addon.ProvidedSlots)
                {
                    Addon strong = slot?.StrongAddon;
                    if (strong == null || strong.AddonDef.HideWeakVisuals == AddonDef.HideMode.Show)
                    {
                        continue;
                    }

                    bool recursive = strong.AddonDef.HideWeakVisuals == AddonDef.HideMode.HideAllRecursively;

                    foreach (Addon weak in slot.WeakAddons)
                    {
                        if (weak == null || weak.OwnTags.OfType<AlwaysVisibleAddonTagDef>().Any())
                        {
                            continue;
                        }

                        // Same reach as Addon.HideVisualsUsingHidePolicy: the covered addon alone,
                        // or the whole branch under it when the covering addon hides recursively.
                        foreach (Addon covered in recursive ? weak.AsEnumerable() : new[] { weak })
                        {
                            if (covered?.VisualRoot != null)
                            {
                                covered.VisualRoot.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Item defs the subject actually shows, for the build log line.
        /// </summary>
        private static string[] VisibleItemNames(AddonsManager manager)
        {
            if (manager?.RootAddon == null)
            {
                return new string[0];
            }

            return manager.RootAddon
                .OfType<Item>()
                .Where(item => item.VisualRoot != null && item.VisualRoot.gameObject.activeSelf)
                .Select(item => item.ItemDef?.name ?? "?")
                .ToArray();
        }

        /// <summary>
        /// Customization tags the subject was built with, for the build log line. A portrait that
        /// comes out uncustomized is a portrait whose tags were the template's defaults.
        /// </summary>
        private static string[] CustomizationTagNames(AddonsManager manager)
        {
            return manager.MergeWithAddonsTags
                .Where(tag => tag is CustomizationColorTagDef || tag is CustomizationPatternTagDef)
                .Select(tag => tag.name)
                .ToArray();
        }

        /// <summary>
        /// Applies the corruption (Delirium) face shader, mirroring
        /// UIModuleActorCycle.SetupFaceCorruptionShader.
        /// </summary>
        private static void ApplyFaceCorruption(AddonsCharacterBuilder builder, GeoCharacter character, GeoLevelController level)
        {
            try
            {
                if (character == null || character.IsMutoid || level == null || (float)character.CharacterStats.Corruption <= 0f)
                {
                    return;
                }

                AddonsManager manager = builder?.AddonsManager;
                TacticalPerceptionDef perception = character.TemplateDef?.ComponentSetDef?.GetComponentDef<TacticalPerceptionDef>();
                if (manager?.RootAddon == null || perception == null)
                {
                    return;
                }

                AddonSlot headSlot = manager.RootAddon.FindAddonSlot(perception.HeadSlot);
                if (headSlot == null)
                {
                    return;
                }

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetFloat(CorruptionShaderPropertyName, level.CorruptedHorizonsSettings.CorruptionSettings
                    .CalculateCorruptionShaderValue(character.CharacterStats.CorruptionProgressRel));

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
        /// Renders the built subject with the game's own soldier portrait renderer.
        ///
        /// That renderer stages the subject at its private origin and frames it with a camera culled
        /// to the Characters layer and clipped 2.5m out, so nothing else in the scene can reach the
        /// image - all we have to do is light the subject and make sure it is on that layer.
        /// </summary>
        private static Texture2D RenderSubject(AddonsCharacterBuilder builder, UnitDisplayData displayData, Vector2Int resolution)
        {
            GameObject subject = builder.gameObject;
            SetLayerRecursively(subject, LayerMask.NameToLayer("Characters"));

            List<Light> disabledLights = new List<Light>();
            GameObject lightRig = null;

            UnityEngine.Rendering.AmbientMode ambientModeBefore = RenderSettings.ambientMode;
            Color ambientLightBefore = RenderSettings.ambientLight;
            float ambientIntensityBefore = RenderSettings.ambientIntensity;
            float reflectionBefore = RenderSettings.reflectionIntensity;

            try
            {
                // Mirror the vanilla tactical squad-portrait setup (SquadMemberScrollerController):
                // flat ambient, no reflections, no world lights, a dedicated portrait rig.
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.26f, 0.27f, 0.30f);
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.reflectionIntensity = 0f;

                foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
                {
                    // Never disable the character's own lights (the rig carries one).
                    if (light == null || !light.isActiveAndEnabled || light.transform.IsChildOf(subject.transform))
                    {
                        continue;
                    }

                    light.gameObject.SetActive(false);
                    disabledLights.Add(light);
                }

                EnableCharacterLight(builder, displayData.CharacterLightObjectName);
                lightRig = CreatePortraitLightRig(subject.transform);

                bool hasNose = builder.AddonsManager?.FindTransform("Nose", rigBonesOnly: true) != null;
                SquadPortraitsDef.RenderPortraitParams renderParams = new SquadPortraitsDef.RenderPortraitParams
                {
                    RenderedPortraitsResolution = resolution,
                    TargetBoneName = hasNose ? "Nose" : "Head",
                    CameraFoV = CameraFoV,
                    CameraDistance = hasNose ? NoseDistance : HeadDistance,
                    CameraHeight = 0f,
                    CameraSide = 0f
                };

                return SoldierPortraitUtil.RenderSoldierNoCopy(subject, renderParams, null);
            }
            finally
            {
                if (lightRig != null)
                {
                    UnityEngine.Object.Destroy(lightRig);
                }

                foreach (Light light in disabledLights)
                {
                    if (light != null)
                    {
                        light.gameObject.SetActive(true);
                    }
                }

                RenderSettings.ambientMode = ambientModeBefore;
                RenderSettings.ambientLight = ambientLightBefore;
                RenderSettings.ambientIntensity = ambientIntensityBefore;
                RenderSettings.reflectionIntensity = reflectionBefore;
            }
        }

        /// <summary>
        /// Turns on the rig's built-in character light (what lights the model on the personnel screen).
        /// It is far too dim to carry a portrait on its own, so it only supplements the portrait rig.
        /// </summary>
        private static void EnableCharacterLight(AddonsCharacterBuilder builder, string lightObjectName)
        {
            try
            {
                if (string.IsNullOrEmpty(lightObjectName))
                {
                    return;
                }

                Transform lightTransform = builder.AddonsManager?.FindTransform(lightObjectName, rigBonesOnly: true);
                Light characterLight = lightTransform != null ? lightTransform.GetComponent<Light>() : null;
                if (characterLight == null)
                {
                    return;
                }

                characterLight.gameObject.SetActive(true);
                characterLight.enabled = true;
                if (LightingSettingsCharacters.Instance != null)
                {
                    characterLight.intensity = LightingSettingsCharacters.Instance.CharacterLightsIntensity;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Three-point portrait rig of directional lights, parented to the subject so the angles stay
        /// relative to where the face points. Directional lights are position-independent, so they work
        /// at the staging origin where scene point/spot lights cannot reach. Angles are kept close to
        /// horizontal so the top of the head does not blow out.
        /// </summary>
        private static GameObject CreatePortraitLightRig(Transform subject)
        {
            GameObject rig = new GameObject("[TFTV]PortraitLightRig");
            rig.transform.SetParent(subject, false);

            // Key: warm, from the front-left, slightly above eye level.
            AddRigLight(rig.transform, "Key", new Vector3(15f, 205f, 0f), new Color(1f, 0.96f, 0.90f), 1.05f);
            // Fill: cool and soft, from the front-right, at eye level - also lifts the armour.
            AddRigLight(rig.transform, "Fill", new Vector3(5f, 150f, 0f), new Color(0.78f, 0.82f, 0.92f), 0.55f);
            // Rim: from behind at eye level, separating hair and shoulders from the dark background.
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

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null || layer < 0)
            {
                return;
            }

            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Same test as UIModuleActorCycle.IsHelmetOrAttachment.
        /// </summary>
        private static bool IsHelmetOrAttachment(ItemDef armourItem)
        {
            if (armourItem?.RequiredSlotBinds == null)
            {
                return false;
            }

            foreach (AddonDef.RequiredSlotBind slotBind in armourItem.RequiredSlotBinds)
            {
                ItemSlotDef slot = slotBind.RequiredSlot as ItemSlotDef;
                if (slot != null && (slot.SlotName == "Head" || slot.SlotName == "HeadAttachment"))
                {
                    return true;
                }
            }

            return false;
        }

        private static CoroutineRunner GetCoroutineRunner()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null)
            {
                return null;
            }

            return level.GetComponent<CoroutineRunner>() ?? level.gameObject.AddComponent<CoroutineRunner>();
        }

        private sealed class CoroutineRunner : MonoBehaviour
        {
        }
    }
}
