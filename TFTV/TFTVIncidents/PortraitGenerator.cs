using Base.Cameras;
using HarmonyLib;

using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Characters;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVUI.Personnel;

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

        // Character the last request was for, so a settings change can re-render it in place.
        private static GeoCharacter _currentCharacter;

        // preserveAspect as the leader Image ships it, captured the first time a portrait is
        // applied. A rendered head needs it on; the painted event-leader artwork of every other
        // geoscape event is authored for the slot as-is, so the flag has to go back.
        private static bool _capturedPreserveAspect;
        private static bool _originalPreserveAspect;

        // Render target is sized to the leader pic slot's on-screen size (clamped), so the
        // portrait matches the display resolution instead of a fixed 1024px.
        private const int MinPortraitResolution = 128;
        private const int FallbackPortraitResolution = 512;

        // Hard ceiling on what a single supersampled render may cost. 4096 x 4096 is 64MB of
        // readback and about a tenth of a second of downsampling - past that it stops being free.
        private const int MaxRenderDimension = 4096;

        // ---------------------------------------------------------------------------------------
        // Tunables. All of these are live: the portrait_* console commands write them and re-render,
        // so framing and quality can be compared in the encounter window itself without a rebuild.
        // The defaults here are the ones the mod ships with.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Supersampling factor. The subject is rendered at this multiple of the display resolution
        /// and box-filtered down to it.
        ///
        /// This is the single biggest quality lever, because the capture path has no anti-aliasing at
        /// all: a 1024px dump of the old settings had three partially-transparent pixels in the whole
        /// image. Everything the character shader alpha-tests - hair, facial hair, the worn edges of
        /// armour - therefore came out as a ragged one-pixel stair-step. Averaging N x N samples per
        /// output pixel is what turns those back into soft edges.
        /// </summary>
        internal static int Supersample = 4;

        /// <summary>
        /// MSAA sample count on the render target (1 = off; 2, 4 or 8 otherwise). Mostly redundant
        /// next to supersampling, and the driver ignores it when the copied scene camera is on a
        /// deferred path, but it costs nothing to be able to try it.
        /// </summary>
        internal static int MsaaSamples = 1;

        /// <summary>
        /// Cap on the displayed portrait's pixel size. The render is sized to the leader slot's own
        /// on-screen size and scaled down to fit this, preserving the slot's aspect ratio.
        /// </summary>
        internal static int MaxPortraitResolution = 1024;

        /// <summary>
        /// Uniform scale applied to the leader picture slot and its ink frame. Below 1 the portrait
        /// is shown smaller than the event-leader artwork space it borrows, which raises the number
        /// of rendered pixels per displayed pixel.
        /// </summary>
        internal static float DisplayScale = 0.70f;

        // Framing. The camera sits off to one side of the face rather than square in front of it, a
        // little above eye level, and far enough back for the shoulders and armour to read.
        //
        // Framing is the second big quality lever. At the original 40 degrees and 1.10m the head
        // filled less than a third of the frame height and the top third of the image was empty, so a
        // 1024px render spent about 300px on the face. Holding the distance and narrowing the lens to
        // 25 degrees crops to a bust and flattens the perspective, which stops the nose and brow from
        // being pushed forward the way a wide lens close to a face does.
        internal static float CameraFoV = 25f;
        internal static float NoseDistance = 1.00f;
        internal static float HeadDistance = 1.10f;
        internal static float CameraYawDegrees = -20f;
        internal static float CameraHeight = 0.10f;

        /// <summary>
        /// Vertical offset, in metres, of the point the camera aims at relative to the target bone.
        /// Negative aims below the face, which lifts the head into the upper half of the frame and
        /// leaves the shoulders and armour underneath it - the shape a bust portrait wants.
        /// </summary>
        internal static float LookAtVerticalOffset = 0.04f;

        /// <summary>
        /// Writes every render to persistentDataPath as a PNG named after the settings that produced
        /// it, which is how framing and quality changes get compared side by side.
        /// </summary>
        internal static bool DumpRenderToDisk = true;

        // ---------------------------------------------------------------------------------------
        // Lighting. One directional light per role, angled relative to the subject (the rig is
        // parented to it, so the angles stay put however the subject is turned).
        //
        // The vanilla tactical portrait has more shape to it than this one did for two reasons, both
        // visible in SquadMemberScrollerController.FinishPortraitCrt: it renders with
        // RenderSettings.ambientIntensity at zero, and its light sets - authored prefabs picked at
        // random by LightsPicker, which only exist under the tactical char builder - cast shadows.
        // Ours was drowning the subject in a flat ambient of 1.0 and casting no shadows at all, which
        // is the whole of the difference in modelling.
        // ---------------------------------------------------------------------------------------

        /// <summary>One directional light of the portrait rig.</summary>
        internal sealed class RigLight
        {
            /// <summary>Downward tilt in degrees; larger drops the light towards the top of the head.</summary>
            internal float Pitch;

            /// <summary>Rotation about the subject's up axis. 180 is straight ahead of the face.</summary>
            internal float Yaw;

            internal Color Color;
            internal float Intensity;

            /// <summary>Only the key light casts shadows by default; two shadow casters fight each other.</summary>
            internal bool CastsShadows;

            internal RigLight(float pitch, float yaw, Color color, float intensity, bool castsShadows)
            {
                Pitch = pitch;
                Yaw = yaw;
                Color = color;
                Intensity = intensity;
                CastsShadows = castsShadows;
            }
        }

        // The defaults below are the "dramatic" preset with the rim light switched off, which is
        // where tuning in-game landed. Steep warm key well off to the side doing nearly all the work,
        // a fill low enough to leave the shadow side dark, and next to no ambient under it.

        // Key: warm, high and well round to the front-left, so the brow and nose lay shadow on the face.
        internal static readonly RigLight KeyLight = new RigLight(32f, 230f, new Color(1f, 0.94f, 0.86f), 1.6f, true);

        // Fill: cool and very low, from the front-right at eye level. This is what decides how much
        // of the shadow side survives; raising it flattens the modelling straight back out.
        internal static readonly RigLight FillLight = new RigLight(5f, 140f, new Color(0.55f, 0.62f, 0.80f), 0.12f, false);

        // Rim: from behind, to separate hair and shoulders from the background. Off by default - at
        // zero intensity no light object is created at all, so its angles do nothing until it is
        // given some intensity back.
        internal static readonly RigLight RimLight = new RigLight(100f, 10f, new Color(0.80f, 0.88f, 1f), 0f, false);

        /// <summary>
        /// Flat ambient during the render. Vanilla's tactical portrait uses zero; anything approaching
        /// 1 washes the shadows out completely, which is what the old value did.
        /// </summary>
        internal static float AmbientIntensity = 0.10f;

        internal static Color AmbientColor = new Color(0.26f, 0.27f, 0.30f);

        /// <summary>Shadow mode for whichever rig lights cast: LightShadows.None, Hard or Soft.</summary>
        internal static LightShadows ShadowMode = LightShadows.Soft;

        /// <summary>How dark a cast shadow gets, 0 to 1.</summary>
        internal static float ShadowStrength = 1f;

        /// <summary>
        /// Shadow distance forced during the render, in metres.
        ///
        /// This matters more than it looks. The global value is set from the graphics options and is
        /// tens of metres, so the cascade covering a head one metre from the camera gets a handful of
        /// texels and the shadow arrives as a blocky mess. Pulling it in to a couple of metres spends
        /// the whole shadow map on the subject.
        /// </summary>
        internal static float ShadowDistance = 3f;

        internal static float ShadowNormalBias = 0.05f;
        internal static float ShadowBias = 0.02f;

        /// <summary>
        /// Turns on the light the character rig carries. It is far too dim to carry a portrait, and it
        /// is the personnel screen's light rather than a portrait one, so it is off by default now.
        /// </summary>
        internal static bool UseCharacterLight = false;

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
                _currentCharacter = character;

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
                _currentCharacter = null;

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
        /// Puts the encounter's leader slot back the way the game ships it.
        ///
        /// The slot is shared by every geoscape event, and the adjustments a rendered portrait needs
        /// (the DisplayScale shrink and preserveAspect) are not adjustments the painted event-leader
        /// artwork wants - left in place they follow the player into the next, unrelated encounter.
        /// Called before each encounter is shown, so whatever that encounter then puts in the slot
        /// starts from the vanilla state.
        /// </summary>
        internal static void ResetLeaderSlot(UIModuleSiteEncounters module)
        {
            try
            {
                if (module == null)
                {
                    return;
                }

                // Nothing in flight should paint over the encounter that is being shown now.
                _currentRequestId = -1;
                _currentCharacter = null;

                RestoreDisplayScale(module);

                if (module.EncounterLeaderImage != null)
                {
                    if (_capturedPreserveAspect)
                    {
                        module.EncounterLeaderImage.preserveAspect = _originalPreserveAspect;
                    }

                    // Only our own rendered head is cleared; artwork the game put there is left alone.
                    if (_appliedModule == module)
                    {
                        module.EncounterLeaderImage.sprite = null;
                    }
                }

                if (_appliedModule == module)
                {
                    _appliedModule = null;
                }
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

                RestoreDisplayScale(_appliedModule);
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

            if (!_capturedPreserveAspect && module.EncounterLeaderImage != null)
            {
                _originalPreserveAspect = module.EncounterLeaderImage.preserveAspect;
                _capturedPreserveAspect = true;
            }

            module.EncounterLeaderImage.sprite = portrait;
            module.EncounterLeaderImage.preserveAspect = true;
            ApplyDisplayScale(module);
            _appliedModule = module;
        }

        // Scale the leader slot's transforms were found at, so DisplayScale can be undone rather
        // than compounded every time a portrait is applied.
        private static readonly Dictionary<Transform, Vector3> OriginalScales = new Dictionary<Transform, Vector3>();

        /// <summary>
        /// Shrinks (or grows) the leader picture slot and the ink frame drawn around it.
        ///
        /// The slot the portrait borrows is sized for event-leader artwork, which is far larger than
        /// a rendered head wants to be shown at: the smaller it is drawn, the more rendered pixels
        /// land on each displayed one. Scaling the transforms leaves the layout that positions them
        /// alone, so nothing else in the encounter window moves.
        /// </summary>
        private static void ApplyDisplayScale(UIModuleSiteEncounters module)
        {
            try
            {
                foreach (Transform target in ScaledTransforms(module))
                {
                    if (!OriginalScales.TryGetValue(target, out Vector3 original))
                    {
                        original = target.localScale;
                        OriginalScales[target] = original;
                    }

                    target.localScale = original * DisplayScale;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void RestoreDisplayScale(UIModuleSiteEncounters module)
        {
            try
            {
                foreach (Transform target in ScaledTransforms(module))
                {
                    if (OriginalScales.TryGetValue(target, out Vector3 original))
                    {
                        target.localScale = original;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// The leader picture group and its ink frame - skipping the ink group when it sits under the
        /// picture group, since scaling both would then square the factor.
        /// </summary>
        private static IEnumerable<Transform> ScaledTransforms(UIModuleSiteEncounters module)
        {
            Transform picture = module?.EncunterLeaderGroup != null ? module.EncunterLeaderGroup.transform : null;
            Transform ink = module?.EncunterLeaderInkGroup != null ? module.EncunterLeaderInkGroup.transform : null;

            if (picture != null)
            {
                yield return picture;
            }

            if (ink != null && (picture == null || !ink.IsChildOf(picture)))
            {
                yield return ink;
            }
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
                        // The slot is drawn at DisplayScale, so that is the size the portrait is
                        // actually seen at - measure against that, not the unscaled rect.
                        width *= DisplayScale;
                        height *= DisplayScale;

                        // Scale both axes by the same factor. Clamping them independently, as this
                        // used to, changes the render's aspect ratio away from the slot's, and the
                        // Image then letterboxes the result and throws the difference away.
                        float fit = Mathf.Min(1f, MaxPortraitResolution / Mathf.Max(width, height));
                        Vector2Int resolution = new Vector2Int(
                            Mathf.Max(MinPortraitResolution, Mathf.RoundToInt(width * fit)),
                            Mathf.Max(MinPortraitResolution, Mathf.RoundToInt(height * fit)));

                        TFTVLogger.Always($"{LogPrefix} Leader slot measures {width:F0}x{height:F0} on screen -> portrait {resolution.x}x{resolution.y}.");
                        return resolution;
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

                DumpRender(character, rendered);

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
        /// Writes the render to persistentDataPath under a name carrying the settings that produced
        /// it, so a run of portrait_* variations leaves a directory that can be compared file by file.
        /// </summary>
        private static void DumpRender(GeoCharacter character, Texture2D rendered)
        {
            if (!DumpRenderToDisk)
            {
                return;
            }

            try
            {
                string name = $"TFTV_Portrait_{character.Id}_{SettingsSlug()}.png";
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, name), rendered.EncodeToPNG());
                TFTVLogger.Always($"{LogPrefix} Wrote {name} ({rendered.width}x{rendered.height}).");
            }
            catch (Exception dumpError)
            {
                TFTVLogger.Error(dumpError);
            }
        }

        private static string SettingsSlug()
        {
            return $"ss{Mathf.Clamp(Supersample, 1, 4)}_msaa{ResolveMsaaSamples()}_res{MaxPortraitResolution}" +
                $"_d{NoseDistance:0.00}_fov{CameraFoV:0}_yaw{CameraYawDegrees:0}_h{CameraHeight:0.00}_look{LookAtVerticalOffset:0.00}" +
                $"_amb{AmbientIntensity:0.00}_key{KeyLight.Intensity:0.00}_fill{FillLight.Intensity:0.00}_rim{RimLight.Intensity:0.00}" +
                $"_sh{(ShadowMode == LightShadows.None ? "off" : ShadowMode.ToString().ToLowerInvariant())}";
        }

        /// <summary>
        /// Current settings as one line, for the portrait_settings console command.
        /// </summary>
        internal static string DescribeSettings()
        {
            return $"supersample {Mathf.Clamp(Supersample, 1, 4)} | msaa {ResolveMsaaSamples()} | maxres {MaxPortraitResolution} | " +
                $"displayscale {DisplayScale:0.00} | distance {NoseDistance:0.00} (head bone {HeadDistance:0.00}) | " +
                $"fov {CameraFoV:0.#} | yaw {CameraYawDegrees:0.#} | height {CameraHeight:0.00} | lookoffset {LookAtVerticalOffset:0.00} | " +
                $"dump {(DumpRenderToDisk ? "on" : "off")}";
        }

        /// <summary>
        /// Current lighting as one line, for the portrait_settings console command.
        /// </summary>
        internal static string DescribeLighting()
        {
            return $"ambient {AmbientIntensity:0.00} | shadows {ShadowMode} strength {ShadowStrength:0.00} dist {ShadowDistance:0.0} | " +
                $"key {DescribeRigLight(KeyLight)} | fill {DescribeRigLight(FillLight)} | rim {DescribeRigLight(RimLight)} | " +
                $"charlight {(UseCharacterLight ? "on" : "off")}";
        }

        private static string DescribeRigLight(RigLight light)
        {
            return $"i{light.Intensity:0.00}/p{light.Pitch:0}/y{light.Yaw:0}{(light.CastsShadows ? "+shadow" : string.Empty)}";
        }

        /// <summary>
        /// Throws away every rendered portrait and renders the selected operative again with whatever
        /// the tunables now say. This is what makes a settings change visible without leaving the
        /// encounter window.
        /// </summary>
        internal static bool RefreshCurrent()
        {
            try
            {
                UIModuleSiteEncounters module = _appliedModule;
                GeoCharacter character = _currentCharacter;

                if (module == null || character == null)
                {
                    return false;
                }

                // ClearCache detaches from the module and forgets the character, so hold both first.
                ClearCache();

                _currentRequestId = character.Id;
                _currentCharacter = character;
                GetCoroutineRunner()?.StartCoroutine(RenderAndApply(module, character));
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
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

                // Colour 1, colour 2 and the pattern live on the character's identity, and its own
                // tag list only picks them up when something asks it to. The customization screen
                // updates its model with exactly this pair (UIStateSoldierCustomization.
                // RefreshUnitDisplay): RefreshTags to merge the identity into the character's tags,
                // then rebuild the builder from that list. Do the same rather than an imitation of
                // it on a copy - that merged the same tags into our own list and still came out with
                // the template's grey.
                character.RefreshTags();

                AddonsManager manager = builder.AddonsManager;
                manager.SetAutorefreshOnTagsChanged(false);
                manager.GameTags.Clear();
                manager.GameTags.AddRange(displayData.GameTags);

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
                    $"| identity {IdentityCustomization(character)} " +
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
        /// Colour 1, colour 2 and the pattern as they stand on the character's identity - the values
        /// the customization screen edits. If these read as defaults while the screen shows something
        /// else, the portrait is reading a different identity than that screen is.
        /// </summary>
        private static string IdentityCustomization(GeoCharacter character)
        {
            CharacterIdentity identity = character.Identity;
            if (identity == null)
            {
                return "none";
            }

            return $"{identity.PrimaryColorTag?.name ?? "-"}/{identity.SecondaryColorTag?.name ?? "-"}/{identity.PatternTag?.name ?? "-"}";
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
                if (character == null || character.IsMutoid || level == null)
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
                propertyBlock.SetFloat(CorruptionShaderPropertyName, ResolveCorruptionShaderValue(character, level));

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

            ShadowQuality shadowQualityBefore = QualitySettings.shadows;
            float shadowDistanceBefore = QualitySettings.shadowDistance;
            ShadowResolution shadowResolutionBefore = QualitySettings.shadowResolution;
            int shadowCascadesBefore = QualitySettings.shadowCascades;

            try
            {
                // Mirror the vanilla tactical squad-portrait setup (SquadMemberScrollerController):
                // flat ambient, no reflections, no world lights, a dedicated portrait rig.
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = AmbientColor;
                RenderSettings.ambientIntensity = AmbientIntensity;
                RenderSettings.reflectionIntensity = 0f;

                if (ShadowMode != LightShadows.None)
                {
                    // The player's graphics options may have shadows off entirely, and their shadow
                    // distance is sized for a battlefield rather than a head. Both are restored below.
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = Mathf.Max(0.5f, ShadowDistance);
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

                    // One cascade: the whole shadow map goes on the subject rather than being split
                    // across distance bands that nothing else occupies.
                    QualitySettings.shadowCascades = 1;
                }

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

                if (UseCharacterLight)
                {
                    EnableCharacterLight(builder, displayData.CharacterLightObjectName);
                }

                lightRig = CreatePortraitLightRig(subject.transform);

                // Belt and braces, not the fix: the customization is already applied when the build
                // finishes, and this re-applies it in case anything re-created an item's visuals
                // since - a rebuild pass finishing late, a skin swapped when the merged tags changed
                // - which would leave fresh renderers with no property block on them.
                RefreshAddonTags(builder.AddonsManager);
                HideCoveredAddonVisuals(builder.AddonsManager);
                return CapturePortrait(builder, resolution);
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

                QualitySettings.shadows = shadowQualityBefore;
                QualitySettings.shadowDistance = shadowDistanceBefore;
                QualitySettings.shadowResolution = shadowResolutionBefore;
                QualitySettings.shadowCascades = shadowCascadesBefore;
            }
        }

        /// <summary>
        /// How much Delirium shows on the face, by the mod's reckoning rather than the game's.
        ///
        /// TFTV tones the effect down and folds stamina into it, and it does so by patching
        /// CharacterStats.CorruptionProgressRel - but only while DeliriumFaceShader's hook names the
        /// character being drawn, which is how the personnel screen and the tactical squad portraits
        /// get the reduced value. Set the same hook around the read so an incident portrait shows the
        /// same face as those screens instead of the untouched vanilla amount.
        /// </summary>
        private static float ResolveCorruptionShaderValue(GeoCharacter character, GeoLevelController level)
        {
            GeoCharacter previousHook = DeliriumFaceShader.HookToCharacterForDeliriumShader;
            DeliriumFaceShader.HookToCharacterForDeliriumShader = character;
            try
            {
                return level.CorruptedHorizonsSettings.CorruptionSettings
                    .CalculateCorruptionShaderValue(character.CharacterStats.CorruptionProgressRel);
            }
            finally
            {
                DeliriumFaceShader.HookToCharacterForDeliriumShader = previousHook;
            }
        }

        /// <summary>
        /// Renders the subject with a camera copied from the one the game draws the world with.
        ///
        /// The game's own capture path (SoldierPortraitUtil -> RenderingEnvironment) builds a bare
        /// camera and forces RenderingPath.Forward on it, and the character shader's customization -
        /// armour colour and pattern - does not survive that: vanilla's own rendered tactical
        /// portraits come out in factory colours for the same reason. Copying the live camera keeps
        /// the shader on the path it takes in the scene, which is where the customization shows.
        /// </summary>
        private static Texture2D CapturePortrait(AddonsCharacterBuilder builder, Vector2Int resolution)
        {
            Transform target = builder.AddonsManager?.FindTransform("Nose", rigBonesOnly: true)
                ?? builder.AddonsManager?.FindTransform("Head", rigBonesOnly: true)
                ?? builder.transform;

            float distance = target.name == "Head" ? HeadDistance : NoseDistance;

            // Render bigger than the portrait is shown at, then average the extra samples down.
            // Nothing in this path anti-aliases on its own, so this is where edge quality comes from.
            int factor = ResolveSupersampleFactor(resolution);
            Vector2Int renderResolution = new Vector2Int(resolution.x * factor, resolution.y * factor);
            int msaa = ResolveMsaaSamples();

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                renderResolution.x, renderResolution.y, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default, msaa);

            GameObject cameraHost = new GameObject("[TFTV]PortraitCamera");
            RenderTexture previouslyActive = RenderTexture.active;
            Texture2D supersampled = null;

            try
            {
                Camera camera = cameraHost.AddComponent<Camera>();
                Camera sceneCamera = GameUtl.GameComponent<CameraManager>()?.Camera;
                if (sceneCamera != null)
                {
                    camera.CopyFrom(sceneCamera);
                }

                camera.enabled = false;
                camera.targetTexture = renderTexture;
                camera.cullingMask = 1 << LayerMask.NameToLayer("Characters");
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.allowHDR = false;
                camera.allowMSAA = msaa > 1;
                camera.orthographic = false;
                camera.fieldOfView = CameraFoV;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 2.5f;

                // CopyFrom brings the scene camera's aspect across with it, and assigning a target
                // texture does not clear it. Set it from the render target, or a non-square portrait
                // comes out stretched.
                camera.aspect = (float)renderResolution.x / renderResolution.y;

                // Swing the camera around the head for a three-quarter view. The yaw is taken about
                // world up rather than the bone's, since rig bones do not carry a dependable up, and
                // the look-at levels the shot so the portrait is never tilted.
                Vector3 viewDirection = Quaternion.AngleAxis(CameraYawDegrees, Vector3.up) * target.forward;
                camera.transform.position = target.position
                    + viewDirection.normalized * distance
                    + Vector3.up * CameraHeight;

                // Aiming below the face rather than straight at it lifts the head out of the middle
                // of the frame and leaves the shoulders under it.
                camera.transform.LookAt(target.position + Vector3.up * LookAtVerticalOffset);

                camera.Render();

                RenderTexture.active = renderTexture;
                // At factor 1 this texture is the portrait itself, so it wants the mip chain the UI
                // samples from; at higher factors it is a scratch buffer the downsample reads once.
                bool direct = factor == 1;
                supersampled = new Texture2D(renderResolution.x, renderResolution.y, TextureFormat.RGBA32, mipChain: direct);
                supersampled.ReadPixels(new Rect(0f, 0f, renderResolution.x, renderResolution.y), 0, 0, recalculateMipMaps: direct);
                supersampled.Apply(updateMipmaps: direct);

                if (direct)
                {
                    Texture2D portrait = supersampled;
                    supersampled = null;
                    return portrait;
                }

                return Downsample(supersampled, resolution, factor);
            }
            finally
            {
                if (supersampled != null)
                {
                    UnityEngine.Object.Destroy(supersampled);
                }

                RenderTexture.active = previouslyActive;
                UnityEngine.Object.Destroy(cameraHost);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        /// <summary>
        /// Supersampling factor for this portrait, cut back when the requested resolution times the
        /// factor would exceed what a single readback should cost.
        /// </summary>
        private static int ResolveSupersampleFactor(Vector2Int resolution)
        {
            int factor = Mathf.Clamp(Supersample, 1, 4);
            int longest = Mathf.Max(resolution.x, resolution.y);

            while (factor > 1 && longest * factor > MaxRenderDimension)
            {
                factor--;
            }

            return factor;
        }

        private static int ResolveMsaaSamples()
        {
            if (MsaaSamples >= 8)
            {
                return 8;
            }

            if (MsaaSamples >= 4)
            {
                return 4;
            }

            return MsaaSamples >= 2 ? 2 : 1;
        }

        /// <summary>
        /// Box-filters a factor x factor block of rendered samples into each portrait pixel.
        ///
        /// Two things stop this from being a plain average. Colour is averaged in linear light when
        /// the game renders in linear space, because averaging sRGB bytes darkens every edge it
        /// touches. And RGB is weighted by each sample's alpha, so the transparent background - which
        /// is cleared to black - cannot bleed a dark fringe into the silhouette.
        /// </summary>
        private static Texture2D Downsample(Texture2D source, Vector2Int resolution, int factor)
        {
            Color32[] samples = source.GetPixels32();
            int sourceWidth = source.width;
            Color32[] output = new Color32[resolution.x * resolution.y];

            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            float[] toLinear = linear ? SrgbToLinearTable() : null;
            float perPixel = factor * factor;

            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    float red = 0f;
                    float green = 0f;
                    float blue = 0f;
                    float alpha = 0f;

                    for (int sampleY = 0; sampleY < factor; sampleY++)
                    {
                        int row = (y * factor + sampleY) * sourceWidth + x * factor;

                        for (int sampleX = 0; sampleX < factor; sampleX++)
                        {
                            Color32 sample = samples[row + sampleX];
                            float weight = sample.a;

                            if (weight <= 0f)
                            {
                                continue;
                            }

                            alpha += weight;
                            red += (linear ? toLinear[sample.r] : sample.r / 255f) * weight;
                            green += (linear ? toLinear[sample.g] : sample.g / 255f) * weight;
                            blue += (linear ? toLinear[sample.b] : sample.b / 255f) * weight;
                        }
                    }

                    int index = y * resolution.x + x;

                    if (alpha <= 0f)
                    {
                        output[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    red /= alpha;
                    green /= alpha;
                    blue /= alpha;

                    if (linear)
                    {
                        red = LinearToSrgb(red);
                        green = LinearToSrgb(green);
                        blue = LinearToSrgb(blue);
                    }

                    output[index] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(red * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(green * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(blue * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(alpha / perPixel), 0, 255));
                }
            }

            Texture2D portrait = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, mipChain: true);
            portrait.SetPixels32(output);
            portrait.Apply(updateMipmaps: true);
            return portrait;
        }

        private static float[] _srgbToLinear;

        private static float[] SrgbToLinearTable()
        {
            if (_srgbToLinear != null)
            {
                return _srgbToLinear;
            }

            float[] table = new float[256];
            for (int i = 0; i < 256; i++)
            {
                float value = i / 255f;
                table[i] = value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
            }

            _srgbToLinear = table;
            return table;
        }

        private static float LinearToSrgb(float value)
        {
            return value <= 0.0031308f ? value * 12.92f : 1.055f * Mathf.Pow(value, 1f / 2.4f) - 0.055f;
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

            AddRigLight(rig.transform, "Key", KeyLight);
            AddRigLight(rig.transform, "Fill", FillLight);
            AddRigLight(rig.transform, "Rim", RimLight);

            return rig;
        }

        private static void AddRigLight(Transform rig, string name, RigLight settings)
        {
            if (settings.Intensity <= 0f)
            {
                return;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(rig, false);
            go.transform.localRotation = Quaternion.Euler(settings.Pitch, settings.Yaw, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = settings.Color;
            light.intensity = settings.Intensity;
            light.shadows = settings.CastsShadows ? ShadowMode : LightShadows.None;
            light.shadowStrength = Mathf.Clamp01(ShadowStrength);

            // A head is roughly a fifth of a metre across, so the default biases - sized for terrain -
            // push the shadow far enough off the surface to detach it from what casts it.
            light.shadowBias = ShadowBias;
            light.shadowNormalBias = ShadowNormalBias;
            light.shadowNearPlane = 0.05f;
        }

        /// <summary>
        /// Named lighting rigs, for the portrait_lightpreset console command.
        /// </summary>
        internal static bool ApplyLightPreset(string preset)
        {
            switch (preset)
            {
                // What the mod rendered before this pass: heavy flat ambient, no shadows anywhere.
                case "flat":
                    Set(KeyLight, 15f, 205f, new Color(1f, 0.96f, 0.90f), 1.05f, false);
                    Set(FillLight, 5f, 150f, new Color(0.78f, 0.82f, 0.92f), 0.55f, false);
                    Set(RimLight, 0f, 20f, new Color(0.90f, 0.93f, 1f), 0.20f, false);
                    AmbientIntensity = 1f;
                    ShadowMode = LightShadows.None;
                    ShadowStrength = 0.75f;
                    return true;

                // Default: a proper key/fill/rim with the key casting, ambient pulled well down.
                case "studio":
                    Set(KeyLight, 28f, 205f, new Color(1f, 0.96f, 0.90f), 1.25f, true);
                    Set(FillLight, 5f, 150f, new Color(0.78f, 0.82f, 0.92f), 0.35f, false);
                    Set(RimLight, 0f, 20f, new Color(0.90f, 0.93f, 1f), 0.45f, false);
                    AmbientIntensity = 0.30f;
                    ShadowMode = LightShadows.Soft;
                    ShadowStrength = 0.75f;
                    return true;

                // Ambient at zero, exactly as SquadMemberScrollerController renders the tactical
                // squad portraits, with a strong rim to carry the silhouette.
                case "vanilla":
                    Set(KeyLight, 25f, 210f, new Color(1f, 0.97f, 0.92f), 1.35f, true);
                    Set(FillLight, 8f, 145f, new Color(0.72f, 0.78f, 0.90f), 0.30f, false);
                    Set(RimLight, 5f, 15f, new Color(0.85f, 0.90f, 1f), 0.55f, false);
                    AmbientIntensity = 0f;
                    ShadowMode = LightShadows.Soft;
                    ShadowStrength = 1f;
                    return true;

                // Hard side key, almost no fill - the shadow side of the face goes nearly black.
                case "dramatic":
                    Set(KeyLight, 32f, 230f, new Color(1f, 0.94f, 0.86f), 1.6f, true);
                    Set(FillLight, 5f, 140f, new Color(0.55f, 0.62f, 0.80f), 0.12f, false);
                    Set(RimLight, 8f, 10f, new Color(0.80f, 0.88f, 1f), 0.65f, false);
                    AmbientIntensity = 0.10f;
                    ShadowMode = LightShadows.Soft;
                    ShadowStrength = 1f;
                    return true;

                default:
                    return false;
            }
        }

        private static void Set(RigLight light, float pitch, float yaw, Color color, float intensity, bool castsShadows)
        {
            light.Pitch = pitch;
            light.Yaw = yaw;
            light.Color = color;
            light.Intensity = intensity;
            light.CastsShadows = castsShadows;
        }

        /// <summary>Named rig light, for the portrait_light console command.</summary>
        internal static RigLight FindRigLight(string name)
        {
            switch (name)
            {
                case "key": return KeyLight;
                case "fill": return FillLight;
                case "rim": return RimLight;
                default: return null;
            }
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

        /// <summary>
        /// Hands every encounter a leader slot in its shipped state.
        ///
        /// ShowEncounter is the single entry point the game displays a geoscape event through, and it
        /// runs before the event's own leader artwork is assigned, so resetting here neither fights
        /// the game for the slot nor undoes the portrait an incident applies afterwards.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleSiteEncounters), "ShowEncounter")]
        internal static class UIModuleSiteEncounters_ShowEncounter_ResetLeaderSlot_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static void Prefix(UIModuleSiteEncounters __instance)
            {
                ResetLeaderSlot(__instance);
            }
        }
    }
}
