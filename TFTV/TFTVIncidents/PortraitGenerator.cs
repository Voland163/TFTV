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
using UnityEngine.UI;

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

        // Ceiling on a supersampled render's size, which brings the factor down for large portraits.
        // Small ones - the crew cards, the recruit panel - stay at four times; the incident leader
        // picture, over 800px, drops to two times. At four times that picture is 3284px square and
        // its capture measured 450ms in a single frame, a visible freeze as the incident opens; at
        // two times it is a quarter of the pixels, still four samples per pixel along every edge.
        private const int MaxRenderDimension = 2048;

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
        /// Where the camera stands for one portrait.
        ///
        /// The incident leader picture and the recruit panel take different crops out of the same
        /// subject: the incident slot is large and wants a bust, with the armour and shoulders under
        /// the head reading as part of the picture, while the recruit slot is small enough that a
        /// bust reduces the face to a smudge - there it is the face that carries the portrait.
        /// </summary>
        internal struct PortraitFraming
        {
            internal float NoseDistance;
            internal float HeadDistance;
            internal float FieldOfView;
            internal float YawDegrees;
            internal float Height;
            internal float LookAtVerticalOffset;
        }

        /// <summary>
        /// The incident leader picture's framing - the live portrait_* tunables above, read fresh so
        /// a console change still takes effect on the next render.
        /// </summary>
        private static PortraitFraming IncidentFraming => new PortraitFraming
        {
            NoseDistance = NoseDistance,
            HeadDistance = HeadDistance,
            FieldOfView = CameraFoV,
            YawDegrees = CameraYawDegrees,
            Height = CameraHeight,
            LookAtVerticalOffset = LookAtVerticalOffset,
        };

        /// <summary>
        /// The recruit panel's framing: the same shot pulled in until the face fills the frame.
        ///
        /// At the incident distance of 1.00m a 25 degree lens covers 0.44m at the subject, which is a
        /// head and most of a chest - in a slot this small the head lands in the top third and the
        /// armour takes the rest. 0.60m covers 0.27m, and the 20 degree lens brings that to 0.21m:
        /// a head, a jaw and a collar.
        ///
        /// The crop is tightened by narrowing the lens rather than walking the camera in. Both fill
        /// the frame equally, but a camera closer to a face exaggerates whatever is nearest it - the
        /// nose and the brow - while a longer lens at the same distance flattens the face the way a
        /// portrait lens is meant to.
        /// </summary>
        internal static PortraitFraming RecruitFraming = new PortraitFraming
        {
            NoseDistance = 0.60f,
            HeadDistance = 0.68f,
            FieldOfView = 20f,
            YawDegrees = -20f,

            // Level with the point being looked at, so the lens is not tilted down into the top of
            // the skull - a tilt foreshortens the crown, which is the part with least room to spare.
            Height = 0.05f,

            // Aims above the nose, which drops the subject in the frame. The half-frame at this lens
            // and distance reaches 0.106m, so 0.045 puts the top of the frame about 0.15m above the
            // nose - clear of the crown - while the bottom still falls below the chin.
            LookAtVerticalOffset = 0.045f,
        };

        /// <summary>
        /// Writes every render to persistentDataPath as a PNG named after the settings that produced
        /// it, which is how framing and quality changes get compared side by side.
        ///
        /// A tuning aid, not something to ship on: encoding and writing a PNG happens inline with the
        /// render, so leaving it on costs every portrait a synchronous disk write and litters the
        /// player's data folder. Turn it on for a tuning session with portrait_dump on.
        /// </summary>
        internal static bool DumpRenderToDisk = false;

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

        // Where subjects stand while they are built and photographed - far from anything the
        // geoscape camera sees. Each subject in flight has its own slot along X, StagingSlotSpacing
        // apart; see AcquireStagingSlot.
        private static readonly Vector3 SubjectStagingPosition = new Vector3(1000f, 1000f, 1000f);

        /// <summary>
        /// Distance between staging slots. The portrait camera's far plane is 2.5m and its shadows
        /// reach 3m, so at this spacing no subject can appear in, or cast into, another's picture.
        /// </summary>
        private const float StagingSlotSpacing = 50f;

        private static readonly HashSet<int> StagingSlotsInUse = new HashSet<int>();

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

        // ── Crew card portraits ──────────────────────────────────────────────

        /// <summary>
        /// The operative cards' framing: a head and shoulders, between the leader picture's bust and
        /// the recruit panel's face crop.
        ///
        /// A card is a fraction of the leader slot's width, so the leader's bust would leave the face
        /// too small to tell two operatives apart - which is the only thing the row is there for. A
        /// bare face crop overcorrects: the armour under the chin is what makes the row read as a row
        /// of soldiers rather than a row of mugshots, and it is the shape the tactical squad portraits
        /// have.
        ///
        /// At this lens a half-frame reaches 0.60 * tan(9.5) = 0.10m, so the frame covers about 0.20m
        /// at the subject - a face and a jaw, with the collar only just in at the bottom.
        ///
        /// Closer than it first shipped, which framed a head and most of a chest: at card size that
        /// left the face a small part of a picture mostly made of armour, and the armour is the part
        /// every operative in a squad has in common.
        ///
        /// The aim point sits well above the nose, which drops the head in the frame. That is not
        /// only composition here - the affinity badge straddles the top edge of the card, and a head
        /// centred in the frame had its crown behind it.
        /// </summary>
        internal static PortraitFraming CardFraming = new PortraitFraming
        {
            NoseDistance = 0.60f,
            HeadDistance = 0.66f,
            FieldOfView = 19f,
            YawDegrees = -20f,
            Height = 0.05f,
            LookAtVerticalOffset = 0.055f,
        };

        // Rendered card portraits, kept across incidents for as long as the geoscape lasts. Kept
        // apart from the leader cache above because the two are rendered at different sizes and
        // different framings - a card is not a shrunk leader picture.
        //
        // Across incidents rather than per incident because nearly all of the wait is loading each
        // operative's armour into a rig, which is paid once per operative per render: the crew of
        // the aircraft that has just resolved one incident is usually the crew that opens the next,
        // and a cache that was dropped between the two made them sit through the same loads again.
        private static readonly Dictionary<int, CardEntry> CardCache = new Dictionary<int, CardEntry>();
        private static readonly List<int> CardCacheOrder = new List<int>();

        /// <summary>
        /// Rendered cards kept at once. Twenty-four heads at card size is a few megabytes, and covers
        /// every aircraft crew a campaign has in rotation at a time.
        /// </summary>
        private const int MaxCachedCards = 24;

        // The geoscape the cache belongs to. A new one - a load, or a return from tactical - means
        // every character object is new, so nothing the cache holds can be trusted to match.
        private static GeoLevelController _cardCacheLevel;

        // Cards still waiting for a render, oldest first, and the Image each one is for.
        private static readonly List<CardRequest> CardQueue = new List<CardRequest>();
        private static int _cardWorkers;

        /// <summary>
        /// Card renders in flight at once. Each is mostly waiting on its operative's armour to load,
        /// so running several overlaps those waits; four covers most crews in one pass without
        /// putting a whole aircraft's worth of rigs in memory at the same moment.
        /// </summary>
        private const int MaxCardWorkers = 4;

        private sealed class CardRequest
        {
            public GeoCharacter Character;
            public string Signature;
            public Image Target;
            public Vector2Int Resolution;
            public Action<bool> OnDone;
        }

        private sealed class CardEntry
        {
            public string Signature;
            public Sprite Sprite;
        }

        /// <summary>
        /// Shows the given operative's head in a crew card, rendering it if it is not cached yet.
        /// <paramref name="onDone"/> runs once the card has its picture, or once it is certain it
        /// will not get one - on the spot for a cached head, later for a render.
        ///
        /// A crew of eight is eight rigs to build, which is why renders are queued and drained one
        /// at a time rather than started together.
        /// </summary>
        internal static void RequestCardPortrait(GeoCharacter character, Image target, Vector2Int resolution, Action<bool> onDone)
        {
            try
            {
                if (character == null || character.Id <= 0 || target == null)
                {
                    onDone?.Invoke(false);
                    return;
                }

                EnsureCardCacheLevel();
                // The size is part of what makes a cached head reusable: after a resolution change
                // the old one would be drawn blurred or wastefully large.
                string signature = BuildCardSignature(character) + "@" + resolution.x + "x" + resolution.y;

                if (TryGetCachedCard(character.Id, signature, out Sprite cached))
                {
                    // A cached null is a render that was already tried and produced nothing.
                    // Asking again would rebuild the same rig for the same empty result.
                    ApplyCardPortrait(target, cached);
                    onDone?.Invoke(cached != null);
                    return;
                }

                CardQueue.Add(new CardRequest
                {
                    Character = character,
                    Signature = signature,
                    Target = target,
                    Resolution = resolution,
                    OnDone = onDone,
                });

                StartCardWorkers();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                onDone?.Invoke(false);
            }
        }

        /// <summary>
        /// Moves the given operative's card to the front of the render queue, if it is still waiting.
        /// The selected card is the one being looked at, and without this it can be last in line.
        /// </summary>
        internal static void PrioritiseCardPortrait(int characterId)
        {
            try
            {
                if (characterId <= 0 || CardQueue.Count < 2)
                {
                    return;
                }

                int index = CardQueue.FindIndex(r => r != null && r.Character != null && r.Character.Id == characterId);
                if (index <= 0)
                {
                    return;
                }

                CardRequest request = CardQueue[index];
                CardQueue.RemoveAt(index);
                CardQueue.Insert(0, request);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Drops the renders still waiting for cards that are going away, without touching what is
        /// already cached. A render already under way finishes and is kept - it is most of the way
        /// to being useful to the next incident, and abandoning it throws the load away.
        /// </summary>
        internal static void CancelPendingCardPortraits()
        {
            CardQueue.Clear();
        }

        /// <summary>
        /// Starts workers for waiting cards, up to <see cref="MaxCardWorkers"/> at once. A worker
        /// takes its first request before its first pause, so the queue shrinks as each one starts
        /// and this never starts more workers than there is work for.
        /// </summary>
        private static void StartCardWorkers()
        {
            while (_cardWorkers < MaxCardWorkers && CardQueue.Count > 0)
            {
                _cardWorkers++;
                if (RunPortraitCoroutine(CardWorker()) == null)
                {
                    // No geoscape to render on, so the worker never ran: take back the count it would
                    // have given up, and fail what is waiting rather than leave it waiting forever.
                    _cardWorkers--;
                    FailPendingCardPortraits();
                    return;
                }
            }
        }

        /// <summary>
        /// Renders waiting cards one after another until the queue is empty. Several run at once;
        /// each render stands in its own staging slot, so their loads overlap safely.
        /// </summary>
        private static IEnumerator CardWorker()
        {
            try
            {
                while (CardQueue.Count > 0)
                {
                    CardRequest request = CardQueue[0];
                    CardQueue.RemoveAt(0);

                    if (request?.Character == null)
                    {
                        continue;
                    }

                    int characterId = request.Character.Id;

                    // The same operative may have been asked for twice - two cards for one operative -
                    // and the first of them has already done the work.
                    if (TryGetCachedCard(characterId, request.Signature, out Sprite done))
                    {
                        ApplyCardPortrait(request.Target, done);
                        InvokeCardDone(request, done != null);
                        continue;
                    }

                    Sprite portrait = null;
                    yield return RenderPortrait(
                        new UnitDisplayData(request.Character, GameUtl.GameComponent<SharedData>()),
                        request.Character,
                        request.Resolution,
                        CardFraming,
                        requireFaceBone: false,
                        dumpToDisk: false,
                        onDone: s => portrait = s);

                    // Stored even when null, so a subject that cannot be rendered is not retried for
                    // every card that asks for it.
                    StoreCachedCard(characterId, request.Signature, portrait);

                    ApplyCardPortrait(request.Target, portrait);
                    InvokeCardDone(request, portrait != null);

                    // Any other request for the same operative that queued up behind this one.
                    for (int i = CardQueue.Count - 1; i >= 0; i--)
                    {
                        CardRequest waiting = CardQueue[i];
                        if (waiting?.Character != null && waiting.Character.Id == characterId && waiting.Signature == request.Signature)
                        {
                            CardQueue.RemoveAt(i);
                            ApplyCardPortrait(waiting.Target, portrait);
                            InvokeCardDone(waiting, portrait != null);
                        }
                    }
                }
            }
            finally
            {
                _cardWorkers--;
            }
        }

        private static void InvokeCardDone(CardRequest request, bool success)
        {
            try
            {
                request?.OnDone?.Invoke(success);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void FailPendingCardPortraits()
        {
            List<CardRequest> pending = new List<CardRequest>(CardQueue);
            CardQueue.Clear();

            foreach (CardRequest request in pending)
            {
                InvokeCardDone(request, false);
            }
        }

        /// <summary>
        /// What the head is made of, as a string: the armour on it, the appearance tags, and how far
        /// Delirium has marked the face. A cached head is only reused while this is unchanged, so an
        /// operative who has been re-armoured or recoloured since their last incident is rendered
        /// again rather than shown as they were.
        /// </summary>
        private static string BuildCardSignature(GeoCharacter character)
        {
            try
            {
                UnitDisplayData data = new UnitDisplayData(character, GameUtl.GameComponent<SharedData>());

                IEnumerable<string> armour = data.ArmourItems != null
                    ? data.ArmourItems.Where(i => i != null).Select(i => i.name).OrderBy(n => n, StringComparer.Ordinal)
                    : Enumerable.Empty<string>();

                IEnumerable<string> tags = data.GameTags != null
                    ? data.GameTags.Where(t => t != null).Select(t => t.name).OrderBy(n => n, StringComparer.Ordinal)
                    : Enumerable.Empty<string>();

                int corruption = character.CharacterStats != null
                    ? Mathf.RoundToInt(character.CharacterStats.Corruption)
                    : 0;

                return string.Join("|", armour) + "#" + string.Join("|", tags) + "#" + corruption;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

                // Unique, so a signature that could not be worked out never matches a cached head.
                return Guid.NewGuid().ToString();
            }
        }

        private static bool TryGetCachedCard(int characterId, string signature, out Sprite sprite)
        {
            sprite = null;

            if (!CardCache.TryGetValue(characterId, out CardEntry entry) || entry == null)
            {
                return false;
            }

            if (!string.Equals(entry.Signature, signature, StringComparison.Ordinal))
            {
                return false;
            }

            // Unity's own null test: a texture freed from under the sprite leaves a live reference
            // to a dead object, and applying it would draw garbage.
            if (entry.Sprite != null && entry.Sprite.texture == null)
            {
                return false;
            }

            sprite = entry.Sprite;
            CardCacheOrder.Remove(characterId);
            CardCacheOrder.Add(characterId);
            return true;
        }

        private static void StoreCachedCard(int characterId, string signature, Sprite sprite)
        {
            if (CardCache.TryGetValue(characterId, out CardEntry previous) && previous != null && previous.Sprite != sprite)
            {
                DestroyCardSprite(previous.Sprite);
            }

            CardCache[characterId] = new CardEntry { Signature = signature, Sprite = sprite };
            CardCacheOrder.Remove(characterId);
            CardCacheOrder.Add(characterId);

            while (CardCacheOrder.Count > MaxCachedCards)
            {
                int oldest = CardCacheOrder[0];
                CardCacheOrder.RemoveAt(0);

                if (CardCache.TryGetValue(oldest, out CardEntry evicted))
                {
                    CardCache.Remove(oldest);
                    DestroyCardSprite(evicted?.Sprite);
                }
            }
        }

        /// <summary>
        /// Drops the cache when the geoscape it was filled on is no longer the current one.
        /// </summary>
        private static void EnsureCardCacheLevel()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == _cardCacheLevel)
            {
                return;
            }

            ClearCardCache();
            _cardCacheLevel = level;
        }

        /// <summary>
        /// Unity's own null test, not C#'s: the card this render was started for may have been torn
        /// down while it ran, and a destroyed Image is still a live C# reference.
        /// </summary>
        private static void ApplyCardPortrait(Image target, Sprite portrait)
        {
            if (target == null || portrait == null)
            {
                return;
            }

            target.sprite = portrait;
            target.preserveAspect = true;
            target.enabled = true;
        }

        private static void DestroyCardSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (sprite.texture != null)
            {
                UnityEngine.Object.Destroy(sprite.texture);
            }

            UnityEngine.Object.Destroy(sprite);
        }

        /// <summary>
        /// Frees every rendered card and drops the queue. Called when the geoscape changes - not per
        /// incident, which is the whole point of the cache.
        /// </summary>
        internal static void ClearCardCache()
        {
            try
            {
                FailPendingCardPortraits();

                foreach (CardEntry entry in CardCache.Values)
                {
                    DestroyCardSprite(entry?.Sprite);
                }

                CardCache.Clear();
                CardCacheOrder.Clear();
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
            Vector2Int measured = ResolveSlotResolution(module?.EncounterLeaderImage?.rectTransform, DisplayScale, MaxPortraitResolution);
            if (measured.x > 0 && measured.y > 0)
            {
                return measured;
            }

            return new Vector2Int(FallbackPortraitResolution, FallbackPortraitResolution);
        }

        /// <summary>
        /// Portrait resolution for a UI slot: the slot's own on-screen pixel size, scaled down to fit
        /// <paramref name="maxResolution"/> without changing its aspect ratio. Rendering to anything
        /// else means the Image either throws rendered pixels away or magnifies them.
        ///
        /// Returns zero when the slot cannot be measured - a rect with no layout pass behind it yet -
        /// so the caller can decide between a fallback and waiting a frame.
        /// </summary>
        internal static Vector2Int ResolveSlotResolution(RectTransform rect, float displayScale, int maxResolution)
        {
            try
            {
                if (rect == null)
                {
                    return Vector2Int.zero;
                }

                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);

                float width;
                float height;

                Canvas canvas = rect.GetComponentInParent<Canvas>();
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

                if (width <= 1f || height <= 1f)
                {
                    return Vector2Int.zero;
                }

                // The slot may be drawn at a scale, and that is the size the portrait is actually
                // seen at - measure against that, not the unscaled rect.
                width *= displayScale;
                height *= displayScale;

                // Scale both axes by the same factor. Clamping them independently changes the
                // render's aspect ratio away from the slot's, and the Image then letterboxes the
                // result and throws the difference away.
                float fit = Mathf.Min(1f, maxResolution / Mathf.Max(width, height));
                return new Vector2Int(
                    Mathf.Max(MinPortraitResolution, Mathf.RoundToInt(width * fit)),
                    Mathf.Max(MinPortraitResolution, Mathf.RoundToInt(height * fit)));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return Vector2Int.zero;
            }
        }

        private static IEnumerator RenderPortrait(GeoCharacter character, Vector2Int resolution, Action<Sprite> onDone)
        {
            yield return RenderPortrait(
                new UnitDisplayData(character, GameUtl.GameComponent<SharedData>()),
                character,
                resolution,
                IncidentFraming,
                requireFaceBone: false,
                dumpToDisk: DumpRenderToDisk,
                onDone: onDone);
        }

        /// <summary>
        /// Renders a portrait of a recruit - a unit the player does not own yet, and which therefore
        /// exists only as a GeoUnitDescriptor rather than a GeoCharacter.
        ///
        /// The descriptor overload of UnitDisplayData is the one vanilla's own recruit screen builds
        /// its 3D model from (UIModuleActorCycle.Init), so the same appearance tags reach the builder
        /// here. Two things differ from the GeoCharacter path: there is no character to refresh tags
        /// on - the descriptor's identity carries them directly - and no Delirium, since a recruit has
        /// no corruption until they are hired.
        ///
        /// A descriptor with no face bone (a Scarab, an Aspida) renders nothing rather than a
        /// close-up of a hull: the framing below is a face framing and nothing else.
        ///
        /// The face, hair and eyes are the recruit's own; the armour is painted in the one fixed
        /// pair of colours every portrait uses. See <see cref="ApplyPortraitArmourColours"/> for why
        /// the recruit's own armour colours are not what a portrait wants.
        /// </summary>
        internal static IEnumerator RenderPortrait(GeoUnitDescriptor recruit, Vector2Int resolution, Action<Sprite> onDone)
        {
            if (recruit == null)
            {
                onDone?.Invoke(null);
                yield break;
            }

            UnitDisplayData displayData = new UnitDisplayData(recruit, GameUtl.GameComponent<SharedData>());
            displayData.GameTags = ApplyPortraitArmourColours(displayData.GameTags);

            yield return RenderPortrait(
                displayData,
                null,
                resolution,
                RecruitFraming,
                requireFaceBone: true,
                dumpToDisk: false,
                onDone: onDone);
        }


        /// <summary>
        /// The armour colours every recruit portrait is painted in.
        ///
        /// A recruit's own colours are randomised per recruit when their identity is first asked for
        /// (GeoUnitDescriptor.GenerateIdentity, under the randomized-initial-customization campaign
        /// option), so a haven's offers came out in a scatter of unrelated colours. One fixed pair
        /// makes the panel read as a set of portraits rather than a paint chart.
        ///
        /// Palette entries, by def name. Armour palette entry N supplies both a
        /// CustomizationColorTagDef_N for the primary channel and a
        /// CustomizationSecondaryColorTagDef_N for the secondary one, so these two names are entry 1
        /// for the primary and entry 2 for the secondary.
        /// </summary>
        private const string PortraitPrimaryColorDefName = "CustomizationColorTagDef_1";
        private const string PortraitSecondaryColorDefName = "CustomizationSecondaryColorTagDef_2";

        /// <summary>
        /// Swaps the armour colours in a recruit's tag list for the two fixed portrait colours.
        ///
        /// Only the armour channels: hair and eye colour are CustomizationColorTagDefs as well, but
        /// they are their own subtypes, so selecting on the primary and secondary subtypes leaves a
        /// recruit's own head colouring alone. The pattern is left alone too.
        ///
        /// Only the tags handed to the builder are changed - the recruit's identity is untouched, so
        /// hiring them still produces the operative the rest of the game agreed on.
        /// </summary>
        private static IEnumerable<GameTagDef> ApplyPortraitArmourColours(IEnumerable<GameTagDef> tags)
        {
            try
            {
                if (tags == null)
                {
                    return tags;
                }

                DefCache defCache = TFTVMain.Main.DefCache;
                CustomizationPrimaryColorTagDef primary = defCache.GetDef<CustomizationPrimaryColorTagDef>(PortraitPrimaryColorDefName);
                CustomizationSecondaryColorTagDef secondary = defCache.GetDef<CustomizationSecondaryColorTagDef>(PortraitSecondaryColorDefName);

                if (primary == null && secondary == null)
                {
                    TFTVLogger.Always($"{LogPrefix} Neither {PortraitPrimaryColorDefName} nor {PortraitSecondaryColorDefName} exists - " +
                        "portraits keep the recruit's own colours.");
                    return tags;
                }

                // Each channel is only dropped when there is something to put back in its place.
                List<GameTagDef> result = tags
                    .Where(tag => !(primary != null && tag is CustomizationPrimaryColorTagDef)
                        && !(secondary != null && tag is CustomizationSecondaryColorTagDef))
                    .ToList();

                if (primary != null)
                {
                    result.Add(primary);
                }

                if (secondary != null)
                {
                    result.Add(secondary);
                }

                return result;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return tags;
            }
        }

        /// <summary>
        /// The lowest staging slot no subject is standing in.
        ///
        /// Renders run side by side: most of one is waiting for that operative's armour to load, and
        /// those waits overlap instead of adding up. What must not overlap is two subjects occupying
        /// the same place - every portrait camera is aimed at its own subject, and a second character
        /// standing in the same spot is photographed along with it, which is how faces once came back
        /// wearing pieces of each other. A slot each keeps them apart.
        ///
        /// The capture itself needs no such care: it runs start to finish within one call, so two
        /// captures can never interleave, and the scene lighting it swaps out is put back before it
        /// returns.
        /// </summary>
        private static int AcquireStagingSlot()
        {
            int slot = 0;
            while (StagingSlotsInUse.Contains(slot))
            {
                slot++;
            }

            StagingSlotsInUse.Add(slot);
            return slot;
        }

        /// <summary>
        /// Builds the subject, renders it and hands back the sprite. The one place a portrait is
        /// actually made; every caller above is a way of describing the subject to it.
        /// </summary>
        private static IEnumerator RenderPortrait(
            UnitDisplayData displayData,
            GeoCharacter character,
            Vector2Int resolution,
            PortraitFraming framing,
            bool requireFaceBone,
            bool dumpToDisk,
            Action<Sprite> onDone)
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null || displayData == null)
            {
                onDone?.Invoke(null);
                yield break;
            }

            int slot = AcquireStagingSlot();

            AddonsCharacterBuilder builder = CreateSubjectBuilder(slot);
            try
            {
                bool built = false;
                yield return BuildSubject(builder, character, displayData, level, ok => built = ok);
                if (!built)
                {
                    onDone?.Invoke(null);
                    yield break;
                }

                Texture2D rendered = RenderSubject(builder, displayData, resolution, framing, requireFaceBone);
                if (rendered == null)
                {
                    onDone?.Invoke(null);
                    yield break;
                }

                // The render already carries mipmaps; trilinear + anisotropic filtering keeps it
                // clean when the UI displays it slightly scaled.
                rendered.filterMode = FilterMode.Trilinear;
                rendered.anisoLevel = 4;

                if (dumpToDisk)
                {
                    DumpRender(displayData.Name, rendered);
                }

                onDone?.Invoke(Sprite.Create(
                    rendered,
                    new Rect(0f, 0f, rendered.width, rendered.height),
                    new Vector2(0.5f, 0.5f),
                    100f));
            }
            finally
            {
                // Immediate, not deferred: the slot is handed straight to the next render, and a
                // deferred destroy would leave this subject standing in it for the rest of the frame
                // to be photographed alongside whoever takes it next.
                UnityEngine.Object.DestroyImmediate(builder.gameObject);
                StagingSlotsInUse.Remove(slot);
            }
        }

        /// <summary>
        /// Writes the render to persistentDataPath under a name carrying the settings that produced
        /// it, so a run of portrait_* variations leaves a directory that can be compared file by file.
        /// </summary>
        private static void DumpRender(string subjectName, Texture2D rendered)
        {
            try
            {
                string name = $"TFTV_Portrait_{SanitizeFileName(subjectName)}_{SettingsSlug()}.png";
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, name), rendered.EncodeToPNG());
                TFTVLogger.Always($"{LogPrefix} Wrote {name} ({rendered.width}x{rendered.height}).");
            }
            catch (Exception dumpError)
            {
                TFTVLogger.Error(dumpError);
            }
        }

        /// <summary>
        /// Operative names reach the dump file name, and they can carry anything the player typed.
        /// </summary>
        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "unnamed";
            }

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
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
        private static AddonsCharacterBuilder CreateSubjectBuilder(int slot)
        {
            // Inactive first: Awake runs on activation, and it must see the fields below.
            GameObject host = new GameObject("[TFTV]PortraitSubject");
            host.SetActive(false);
            host.transform.position = SubjectStagingPosition + (Vector3.right * (slot * StagingSlotSpacing));

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
        ///
        /// <paramref name="character"/> is null when the subject is a recruit: a GeoUnitDescriptor's
        /// display data already reads its appearance tags straight off the identity, so there is
        /// nothing to refresh and no corruption to apply.
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
                character?.RefreshTags();

                AddonsManager manager = builder.AddonsManager;
                manager.SetAutorefreshOnTagsChanged(false);
                manager.GameTags.Clear();
                manager.GameTags.AddRange(displayData.GameTags);

                // Helmets and head attachments hide the face the portrait is about; this is vanilla's
                // showHelmet: false. The body itself comes from the armour items, exactly as on the
                // personnel screen - an unarmoured operative carries their bare body parts there.
                List<ItemDef> armour = displayData.ArmourItems
                    .Where(item => item != null && !IsRemovableHeadCover(item))
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
                    TFTVLogger.Always($"{LogPrefix} Rebuild of {displayData.Name} timed out after {RebuildTimeoutSeconds}s.");
                    onDone?.Invoke(false);
                    yield break;
                }

                manager.SetAutorefreshOnTagsChanged(true);
                RefreshAddonTags(manager);
                HideCoveredAddonVisuals(manager);
                ApplyFaceCorruption(builder, character, level);
                CommonCharacterUtils.ResetCharacterAnimation(builder);

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
        private static Texture2D RenderSubject(AddonsCharacterBuilder builder, UnitDisplayData displayData, Vector2Int resolution, PortraitFraming framing, bool requireFaceBone)
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
                return CapturePortrait(builder, resolution, framing, requireFaceBone);
            }
            finally
            {
                if (lightRig != null)
                {
                    // Immediate: with renders running side by side, another capture can come in the
                    // same frame, and a light rig left standing until the frame ends would light that
                    // subject too.
                    UnityEngine.Object.DestroyImmediate(lightRig);
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
        private static Texture2D CapturePortrait(AddonsCharacterBuilder builder, Vector2Int resolution, PortraitFraming framing, bool requireFaceBone)
        {
            Transform target = builder.AddonsManager?.FindTransform("Nose", rigBonesOnly: true)
                ?? builder.AddonsManager?.FindTransform("Head", rigBonesOnly: true);

            if (target == null)
            {
                // Nothing with a face: a vehicle chassis, or a rig whose bones are named otherwise.
                // The framing below is a head-and-shoulders framing, so pointing it at the object
                // root gives a close-up of whatever happens to sit at the origin.
                if (requireFaceBone)
                {
                    return null;
                }

                target = builder.transform;
            }

            float distance = target.name == "Head" ? framing.HeadDistance : framing.NoseDistance;

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
                camera.fieldOfView = framing.FieldOfView;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 2.5f;

                // CopyFrom brings the scene camera's aspect across with it, and assigning a target
                // texture does not clear it. Set it from the render target, or a non-square portrait
                // comes out stretched.
                camera.aspect = (float)renderResolution.x / renderResolution.y;

                // Swing the camera around the head for a three-quarter view. The yaw is taken about
                // world up rather than the bone's, since rig bones do not carry a dependable up, and
                // the look-at levels the shot so the portrait is never tilted.
                Vector3 viewDirection = Quaternion.AngleAxis(framing.YawDegrees, Vector3.up) * target.forward;
                camera.transform.position = target.position
                    + viewDirection.normalized * distance
                    + Vector3.up * framing.Height;

                // Aiming below the face rather than straight at it lifts the head out of the middle
                // of the frame and leaves the shoulders under it.
                camera.transform.LookAt(target.position + Vector3.up * framing.LookAtVerticalOffset);

                camera.Render();

                // A power-of-two factor is averaged down on the GPU and only the finished portrait
                // is read back. Anything else - no supersampling at all, or an odd factor forced by
                // the render-size cap - takes the CPU path below.
                if (factor > 1 && (factor & (factor - 1)) == 0)
                {
                    return DownsampleOnGpu(renderTexture, resolution, factor);
                }

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

            // Halved rather than stepped down one at a time, so the cap lands on 2 rather than 3.
            // Only power-of-two factors can be averaged on the GPU; a 3 fell back to the CPU loop,
            // which is what every operative switch was paying for on the leader picture.
            while (factor > 1 && longest * factor > MaxRenderDimension)
            {
                factor /= 2;
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
        /// Averages a supersampled render down to the portrait's size on the GPU, then reads back
        /// only the finished portrait.
        ///
        /// The CPU version below reads the whole supersampled render back - sixteen samples per
        /// portrait pixel, about 43MB for the leader picture - and averages it in a managed loop on
        /// the main thread. That was most of what a render cost apart from building the character,
        /// and it bought nothing the GPU does not do for free: halving a texture with bilinear
        /// filtering, sampling each output pixel at the centre of a 2x2 block, is an exact 2x2 box
        /// filter, so two halvings are exactly the 4x4 box the CPU loop computed.
        ///
        /// The CPU loop's two refinements carry over:
        ///  - Linear-light averaging comes from the render textures themselves. In a linear-space
        ///    game they are sRGB, so sampling decodes to linear before filtering and writing encodes
        ///    again - the averaging happens in linear light without being asked for.
        ///  - The alpha weighting that keeps the black background from bleeding a dark fringe into
        ///    the silhouette. At full size every pixel is either character, fully opaque, or cleared
        ///    background, (0,0,0,0) - so the render is already premultiplied, and averaging it gives
        ///    premultiplied colour. Dividing by alpha afterwards turns it back into the straight
        ///    colour the CPU loop produced. Only edge pixels are partially transparent, and only
        ///    those need the division, which keeps the one CPU pass over the result cheap.
        /// </summary>
        private static Texture2D DownsampleOnGpu(RenderTexture source, Vector2Int resolution, int factor)
        {
            List<RenderTexture> halves = new List<RenderTexture>();
            RenderTexture previouslyActive = RenderTexture.active;
            RenderTextureReadWrite readWrite = source.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;

            try
            {
                RenderTexture current = source;
                current.filterMode = FilterMode.Bilinear;

                for (int remaining = factor; remaining > 1; remaining /= 2)
                {
                    RenderTexture next = RenderTexture.GetTemporary(
                        current.width / 2, current.height / 2, 0, source.format, readWrite);
                    next.filterMode = FilterMode.Bilinear;
                    halves.Add(next);

                    Graphics.Blit(current, next);
                    current = next;
                }

                RenderTexture.active = current;
                Texture2D portrait = new Texture2D(current.width, current.height, TextureFormat.RGBA32, mipChain: true);
                portrait.ReadPixels(new Rect(0f, 0f, current.width, current.height), 0, 0, recalculateMipMaps: false);

                UnpremultiplyEdges(portrait);
                portrait.Apply(updateMipmaps: true);
                return portrait;
            }
            finally
            {
                RenderTexture.active = previouslyActive;

                foreach (RenderTexture half in halves)
                {
                    RenderTexture.ReleaseTemporary(half);
                }
            }
        }

        /// <summary>
        /// Turns premultiplied colour back into straight colour on the pixels that need it - the
        /// partially transparent ones along the silhouette. Opaque and empty pixels are the same
        /// either way and are skipped, which is nearly all of them.
        ///
        /// In a linear-space game the stored bytes are sRGB-encoded linear light, and the division
        /// has to happen on the linear value, as the averaging did.
        /// </summary>
        private static void UnpremultiplyEdges(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            float[] toLinear = linear ? SrgbToLinearTable() : null;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a == 0 || p.a == 255)
                {
                    continue;
                }

                float alpha = p.a / 255f;
                pixels[i] = new Color32(
                    Unpremultiply(p.r, alpha, toLinear),
                    Unpremultiply(p.g, alpha, toLinear),
                    Unpremultiply(p.b, alpha, toLinear),
                    p.a);
            }

            texture.SetPixels32(pixels);
        }

        private static byte Unpremultiply(byte value, float alpha, float[] toLinear)
        {
            float straight = toLinear != null
                ? LinearToSrgb(Mathf.Min(1f, toLinear[value] / alpha))
                : Mathf.Min(1f, (value / 255f) / alpha);

            return (byte)Mathf.Clamp(Mathf.RoundToInt(straight * 255f), 0, 255);
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
        /// <summary>
        /// Whether an item covers the head and can simply be taken off for the portrait.
        ///
        /// A helmet can. A mutated or bionic head cannot: it is not worn over the face, it *is* the
        /// face, and stripping it leaves the portrait showing the plain human head the augment
        /// replaced - a face the operative does not have. Vanilla draws the same line, by whether the
        /// head slot holds a permanent augment (UIStateMemorial's showHelmet, and the customization
        /// screen's _hasMutatedHead, which greys out the hide-helmet toggle for exactly this case).
        ///
        /// Vanilla decides it once for the whole unit; this decides it per item, so an operative
        /// wearing a helmet over an augmented head still loses the helmet and keeps the augment.
        /// </summary>
        private static bool IsRemovableHeadCover(ItemDef armourItem)
        {
            if (armourItem?.RequiredSlotBinds == null || armourItem.IsPermanentAugment)
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

        /// <summary>
        /// Runs a portrait coroutine on the geoscape level's own runner, which is where every render
        /// in this file happens. Returns null when there is no geoscape to run it on.
        /// </summary>
        internal static Coroutine RunPortraitCoroutine(IEnumerator routine)
        {
            try
            {
                if (routine == null)
                {
                    return null;
                }

                CoroutineRunner runner = GetCoroutineRunner();
                if (runner == null)
                {
                    TFTVLogger.Always($"{LogPrefix} No coroutine runner - there is no geoscape to render on.");
                    return null;
                }

                return runner.StartCoroutine(routine);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static CoroutineRunner GetCoroutineRunner()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null)
            {
                return null;
            }

            // Deliberately not ??: a destroyed component is a live C# reference that only Unity's
            // own == knows is dead, so ?? would hand back the previous level's runner and every
            // StartCoroutine on it would throw.
            CoroutineRunner existing = level.GetComponent<CoroutineRunner>();
            return existing != null ? existing : level.gameObject.AddComponent<CoroutineRunner>();
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
