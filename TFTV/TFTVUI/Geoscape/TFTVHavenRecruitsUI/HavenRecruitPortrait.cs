using PhoenixPoint.Geoscape.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVHavenRecruitsUI
{
    /// <summary>
    /// The rendered head shown in the recruit details panel.
    ///
    /// The render itself is the incident portrait generator's - the same subject build, framing and
    /// lighting the incident leader picture uses, fed a GeoUnitDescriptor instead of a GeoCharacter.
    /// What lives here is everything that keeps that render from being expensive:
    ///
    ///  - nothing renders for the list, only for the recruit whose details are open;
    ///  - a selection has to hold still for <see cref="DebounceSeconds"/> before it is paid for, so
    ///    walking the list with a stick or the arrow keys renders the recruit stopped on, not each
    ///    one passed over;
    ///  - one render at a time, and a second request while one is in flight replaces the target
    ///    rather than queueing behind it;
    ///  - every result is cached for the session, including the failures, so a recruit is never
    ///    rendered twice and a Scarab is never attempted twice.
    ///
    /// A render is a rig build plus a camera pass plus a GPU readback: single-digit-to-tens of
    /// milliseconds, once per recruit the player actually looks at, and zero per frame after that.
    /// </summary>
    internal static class HavenRecruitPortrait
    {
        private const string LogPrefix = "[TFTV][HavenRecruitPortrait]";

        /// <summary>Name of the slot GameObject, used to find one a previous panel left behind.</summary>
        private const string SlotName = "RecruitPortrait";

        // Where the slot sits, as a fraction of the detail panel: upper right, clear of the header
        // above it and of the faction, name and level rows to its left. Anchors rather than pixels,
        // because the panel is sized against the screen and the canvas it hangs on is scaled - a
        // fixed size would be right at one resolution and wrong at the next. The slot is outside
        // every layout group in the panel, so its presence cannot move any of those rows around.
        private const float PortraitAnchorMinX = 0.60f;
        private const float PortraitAnchorMaxX = 0.905f;
        private const float PortraitAnchorMinY = 0.735f;
        private const float PortraitAnchorMaxY = 0.905f;

        /// <summary>
        /// How long a recruit has to stay selected before its portrait is rendered. Long enough that
        /// scrolling the list with a controller costs nothing, short enough not to be felt on a click.
        /// </summary>
        private const float DebounceSeconds = 0.2f;

        /// <summary>
        /// Rendered portraits kept at once. Twelve heads at this slot size is under two megabytes,
        /// and no haven list the player walks through in one sitting is much longer.
        /// </summary>
        private const int CacheCapacity = 12;

        /// <summary>
        /// Ceiling on the rendered portrait's pixel size. The slot is small, so this only bites on
        /// very high-DPI displays, where a head rendered at the full slot size would cost more than
        /// the extra sharpness is worth.
        /// </summary>
        private const int MaxPortraitResolution = 512;

        /// <summary>
        /// Used when the slot cannot be measured, which is a slot with no layout pass behind it yet.
        /// </summary>
        private const int FallbackResolution = 256;

        private static GameObject _holder;
        private static RectTransform _holderRect;
        private static Image _portraitImage;

        /// <summary>
        /// The slot's rect, so the panel can keep its own text clear of it. Valid whether or not a
        /// portrait is currently shown - the header text should stop at the same column either way,
        /// rather than running wider on the recruits that happen to have no portrait.
        /// </summary>
        internal static RectTransform SlotRect => _holderRect;

        // Rendered portraits by recruit. A null value is a recruit that has been tried and cannot be
        // rendered (a vehicle has no face bone) - cached so it is not attempted again.
        private static readonly Dictionary<GeoUnitDescriptor, Sprite> Cache =
            new Dictionary<GeoUnitDescriptor, Sprite>(ReferenceComparer.Instance);

        // Cache keys oldest first, so the least recently shown portrait is the one evicted.
        private static readonly List<GeoUnitDescriptor> CacheOrder = new List<GeoUnitDescriptor>();

        // The recruit the panel currently wants shown. The render loop reads this every time it is
        // about to commit to work, so a newer selection always wins over an older one.
        private static GeoUnitDescriptor _requested;
        private static bool _renderLoopRunning;

        /// <summary>
        /// The recruit detail panel, or null if there is not one right now. Unity's own null test,
        /// not C#'s: a destroyed GameObject is still a live reference, and ?. would happily walk into
        /// it and throw.
        /// </summary>
        private static Transform DetailPanelTransform()
        {
            GameObject panel = HavenRecruitsDetailsPanel._detailPanel;
            return panel != null ? panel.transform : null;
        }

        /// <summary>
        /// Adds the portrait slot to the detail panel, as the panel is built and again on demand if
        /// it ever goes missing. Does nothing when there is already a slot.
        /// </summary>
        internal static void Create(Transform detailPanel)
        {
            try
            {
                if (detailPanel == null || _holder != null)
                {
                    return;
                }

                // A slot left over from a previous panel that outlived its reference - otherwise
                // rebuilding would stack a second one on top of it.
                Transform stale = detailPanel.Find(SlotName);
                if (stale != null)
                {
                    UnityEngine.Object.Destroy(stale.gameObject);
                }

                var (holderGO, holderRT) = RecruitOverlayManagerHelpers.NewUI(SlotName, detailPanel);

                // Stretched between its anchors rather than laid out, so nothing in the panel's
                // vertical stack shifts when a portrait appears - or when one never does.
                holderRT.anchorMin = new Vector2(PortraitAnchorMinX, PortraitAnchorMinY);
                holderRT.anchorMax = new Vector2(PortraitAnchorMaxX, PortraitAnchorMaxY);
                holderRT.pivot = new Vector2(0.5f, 0.5f);
                holderRT.offsetMin = Vector2.zero;
                holderRT.offsetMax = Vector2.zero;

                _portraitImage = holderGO.AddComponent<Image>();
                _portraitImage.raycastTarget = false;

                // The slot's shape follows the panel's; the head keeps the shape it was rendered at
                // and is centred in whatever the slot turns out to be.
                _portraitImage.preserveAspect = true;

                _holder = holderGO;
                _holderRect = holderRT;
                _holder.SetActive(false);

                TFTVLogger.Always($"{LogPrefix} Portrait slot created.");

                // A fresh panel means a fresh session. Reaching here proves there was no slot, so any
                // loop still believing it is running belongs to a level that is gone.
                _renderLoopRunning = false;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Shows the given recruit's portrait, rendering it if this is the first time it is asked for.
        /// </summary>
        internal static void Show(GeoUnitDescriptor recruit)
        {
            try
            {
                // Rebuild the slot if it has gone missing. The detail panel is torn down and put back
                // on every load, and this class is static, so the two can get out of step: the slot
                // reference is cleared while the panel that was meant to carry the replacement is
                // never rebuilt, and portraits stop appearing for the rest of the session. Asking for
                // the slot here rather than trusting it was created makes that unable to happen,
                // whatever order the panel's own lifecycle runs in.
                if (_holder == null)
                {
                    Create(DetailPanelTransform());
                }

                if (_holder == null)
                {
                    TFTVLogger.Always($"{LogPrefix} No slot to show {recruit?.GetName() ?? "nobody"} in, and no panel to build one on.");
                    return;
                }

                _requested = recruit;

                if (recruit == null)
                {
                    Detach();
                    return;
                }

                if (Cache.TryGetValue(recruit, out Sprite cached))
                {
                    Touch(recruit);
                    Apply(cached);
                    return;
                }

                // Nothing to show yet, and the previous recruit's head must not stand in for it.
                Detach();

                if (!_renderLoopRunning)
                {
                    // Set before starting: StartCoroutine runs the loop up to its first yield
                    // synchronously, and that stretch can reach the loop's own finally. Assigning
                    // the result afterwards would then flag a loop that has already exited as running,
                    // and no later selection would ever be rendered.
                    _renderLoopRunning = true;

                    if (PortraitGenerator.RunPortraitCoroutine(RenderLoop()) == null)
                    {
                        _renderLoopRunning = false;
                        TFTVLogger.Always($"{LogPrefix} Could not start the render loop for {recruit.GetName()}.");
                    }
                }
                else
                {
                    TFTVLogger.Always($"{LogPrefix} {recruit.GetName()} queued behind a render loop already running.");
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Clears the slot and abandons any render still to come. Called when the details panel is
        /// hidden - the panel is shared by every recruit, so a stale head must not survive into the
        /// next one.
        /// </summary>
        internal static void Hide()
        {
            try
            {
                _requested = null;
                Detach();
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Frees every rendered portrait and forgets the slot. Called when the overlay tears its
        /// panels down, which is also when the GameObjects these sprites are shown on go away.
        /// </summary>
        internal static void ResetState()
        {
            try
            {
                _requested = null;

                // The loop runs on a coroutine runner that lives on the geoscape level, and a load
                // destroys that level without the loop's own finally ever getting to clear this. Left
                // set, it makes every later request believe a loop is already on the way, and no
                // portrait renders again for the rest of the session.
                _renderLoopRunning = false;

                if (_portraitImage != null)
                {
                    _portraitImage.sprite = null;
                }

                foreach (Sprite sprite in Cache.Values)
                {
                    DestroySprite(sprite);
                }

                Cache.Clear();
                CacheOrder.Clear();

                // Destroyed, not merely forgotten: the panel that carries it is usually torn down
                // with it, but when it is not, a forgotten slot would linger as an orphan and the
                // rebuild would stack a second one beside it.
                if (_holder != null)
                {
                    UnityEngine.Object.Destroy(_holder);
                }

                _holder = null;
                _holderRect = null;
                _portraitImage = null;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Renders whatever recruit is selected, one at a time, until nothing is left wanting a
        /// portrait. Runs only while there is work; it is not a per-frame cost of having the panel open.
        /// </summary>
        private static IEnumerator RenderLoop()
        {
            try
            {
                while (true)
                {
                    GeoUnitDescriptor target = _requested;
                    if (target == null || _holder == null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Render loop stopping: " +
                            $"{(target == null ? "nothing selected" : "no slot")}.");
                        yield break;
                    }

                    if (Cache.TryGetValue(target, out Sprite cached))
                    {
                        Touch(target);
                        Apply(cached);
                        yield break;
                    }

                    // Let the selection settle. A player running down the list with a controller
                    // passes over every recruit in it, and none of those are worth a render.
                    float deadline = Time.realtimeSinceStartup + DebounceSeconds;
                    while (Time.realtimeSinceStartup < deadline && _requested == target)
                    {
                        yield return null;
                    }

                    if (_requested != target)
                    {
                        continue;
                    }

                    Sprite portrait = null;
                    yield return PortraitGenerator.RenderPortrait(target, ResolveResolution(), s => portrait = s);

                    // The overlay can be torn down while a render is in flight, and ResetState has
                    // then already freed everything it knew about. Nothing would ever free this one.
                    if (_holder == null)
                    {
                        DestroySprite(portrait);
                        yield break;
                    }

                    // Cache the failure too: a recruit with no face bone would otherwise be
                    // re-rendered from scratch every time it is selected.
                    Store(target, portrait);

                    if (_requested == target)
                    {
                        Apply(portrait);
                        yield break;
                    }
                }
            }
            finally
            {
                _renderLoopRunning = false;
            }
        }

        /// <summary>
        /// Render size for the slot as it is actually drawn on this display, so the portrait is
        /// neither magnified nor rendered into pixels the Image throws away.
        /// </summary>
        private static Vector2Int ResolveResolution()
        {
            Vector2Int measured = PortraitGenerator.ResolveSlotResolution(_holderRect, 1f, MaxPortraitResolution);
            if (measured.x > 0 && measured.y > 0)
            {
                return measured;
            }

            return new Vector2Int(FallbackResolution, FallbackResolution);
        }

        private static void Apply(Sprite portrait)
        {
            if (_portraitImage == null || _holder == null)
            {
                return;
            }

            _portraitImage.sprite = portrait;
            _holder.SetActive(portrait != null);
        }

        /// <summary>
        /// Empties the slot and hides it. The sprite itself stays in the cache - only the Image's
        /// reference to it is dropped.
        /// </summary>
        private static void Detach()
        {
            Apply(null);
        }

        private static void Store(GeoUnitDescriptor recruit, Sprite portrait)
        {
            if (recruit == null)
            {
                DestroySprite(portrait);
                return;
            }

            Cache[recruit] = portrait;
            Touch(recruit);

            while (CacheOrder.Count > CacheCapacity)
            {
                GeoUnitDescriptor oldest = CacheOrder[0];
                CacheOrder.RemoveAt(0);

                if (Cache.TryGetValue(oldest, out Sprite evicted))
                {
                    Cache.Remove(oldest);

                    // Never free what the panel is drawing: a destroyed texture left on an Image
                    // renders as garbage until something replaces it.
                    if (_portraitImage == null || _portraitImage.sprite != evicted)
                    {
                        DestroySprite(evicted);
                    }
                }
            }
        }

        /// <summary>
        /// Moves a recruit to the young end of the eviction order.
        /// </summary>
        private static void Touch(GeoUnitDescriptor recruit)
        {
            CacheOrder.Remove(recruit);
            CacheOrder.Add(recruit);
        }

        private static void DestroySprite(Sprite sprite)
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
        /// Keys the cache on the descriptor object itself. Two recruits can be identical in every
        /// field a value comparison would look at, and a haven's recruit is one persistent object
        /// until that haven rolls a new one, which is exactly the lifetime a portrait is valid for.
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<GeoUnitDescriptor>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(GeoUnitDescriptor x, GeoUnitDescriptor y) => ReferenceEquals(x, y);

            public int GetHashCode(GeoUnitDescriptor obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
