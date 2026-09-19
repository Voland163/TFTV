using Base.Audio;
using Base.Core;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// The row of operative cards an incident is assigned from: one card per crew member, each a
    /// rendered head with the operative's rank, name, class and affinity on it.
    ///
    /// This replaces a two-column list of name rows. A name row asks the player to remember which
    /// handle belongs to which of their operatives and to click through the list to find out; a face
    /// does not, and it is how the game already presents a squad in tactical. One row also means the
    /// whole crew is visible at once, which is what makes the response icons below worth comparing -
    /// picking an operative is choosing between the affinities on offer, and that reads as a choice
    /// only when the alternatives are on screen together.
    ///
    /// The heads are rendered, so they arrive over the next few frames rather than with the row.
    /// Every card is complete without its portrait - rank, name, class and affinity are all on the
    /// card itself - so a card that is still waiting is a card missing its picture, not a blank.
    /// </summary>
    internal static class OperativeCards
    {
        internal const string RowName = "[Mod]IncidentCrewCardRow";

        private const string CardNamePrefix = "[Mod]CrewCard_";

        /// <summary>
        /// Card width when the row has room for it. Cards shrink below this to fit a full crew and
        /// never grow past it, so a four-man crew is a short row of normal cards rather than four
        /// oversized ones.
        ///
        /// These are layout units, not pixels - this canvas draws at roughly half a pixel per unit,
        /// so a 210-unit card is about 115 screen pixels on a 1080p display, which is the size the
        /// mockup's cards are.
        /// </summary>
        private const float MaxCardWidth = 210f;

        /// <summary>
        /// Below this a rendered head stops being a face, so a crew too large to fit is truncated
        /// instead - see <see cref="MaxCards"/>.
        /// </summary>
        private const float MinCardWidth = 120f;

        /// <summary>Portrait box proportions (width / height), the tactical squad portrait's shape.</summary>
        private const float PortraitAspect = 0.85f;

        private const float CardSpacing = 56f;

        /// <summary>Height of the rank-and-name line under each portrait.</summary>
        private const float NameRowHeight = 44f;

        /// <summary>Floor for the shrink a long name gets, below which the card stops being readable.</summary>
        private const int MinNameFontSize = 20;

        /// <summary>The name line's size relative to the encounter's own body text.</summary>
        private const float NameFontScale = 0.72f;

        /// <summary>
        /// Aircraft carry up to eight, which at the minimum card width still fits one row. The cap is
        /// what stops a crew larger than any the game can produce from squeezing the row to nothing.
        /// </summary>
        internal const int MaxCards = 8;

        /// <summary>
        /// The affinity badge's size as a fraction of the card's width. A fraction rather than a
        /// number of units, because the cards themselves shrink to fit a large crew and a badge that
        /// did not shrink with them would swallow a narrow card.
        /// </summary>
        private const float BadgeWidthFraction = 0.38f;

        private const float ClassIconSize = 69f;
        private const float ClassIconInset = 8f;

        /// <summary>
        /// The corner shade's size relative to the class mark it sits under - enough to reach past
        /// the mark on every side, not so much that it reads as a dark corner on the card.
        /// </summary>
        private const float CornerShadeScale = 1.9f;

        private const string CornerShadeImageName = "incident_corner_shade.png";
        private static Sprite _cornerShadeSprite;
        private static bool _cornerShadeResolved;

        private static UIButtonSounds _buttonSoundSource;
        private static GeoLevelController _buttonSoundLevel;

        /// <summary>
        /// Layout units per screen pixel on this canvas, measured at 1080p (it draws at about half a
        /// pixel per unit). Border widths are specified in pixels, which is how the design gives them.
        /// </summary>
        private const float UnitsPerPixel = 1.92f;

        // Borders, in screen pixels. The unselected one is the portrait's own frame; the selected one
        // is the same frame one pixel heavier and amber.
        private const float NormalBorderPixels = 2f;
        private const float SelectedBorderPixels = 3f;

        private const float SelectedBorderWidth = SelectedBorderPixels * UnitsPerPixel;
        private const float NormalBorderWidth = NormalBorderPixels * UnitsPerPixel;

        /// <summary>
        /// Gap between the bottom of the portrait and the name under it: about ten screen pixels,
        /// so the name reads as a caption rather than as part of the picture.
        /// </summary>
        private const float NameGap = 10f * UnitsPerPixel;

        /// <summary>
        /// Ceiling on a rendered card's pixel size. A card is small; past this the extra pixels are
        /// thrown away by the Image and paid for by the renderer anyway.
        /// </summary>
        private const int MaxCardRenderResolution = 512;

        private static readonly List<CardState> Cards = new List<CardState>();
        private static int _selectedCharacterId = -1;

        /// <summary>How long the row's heads take to fade in once they are all ready.</summary>
        private const float RevealFadeSeconds = 0.25f;

        /// <summary>
        /// The longest the row waits for its slowest head before showing the rest anyway. A cold
        /// first render of a crew can take several seconds; this is a backstop for one that never
        /// finishes, not a target.
        /// </summary>
        private const float MaxRevealWaitSeconds = 12f;

        // The row being revealed: its heads, how many are still rendering, and which row it is -
        // renders outlive the row they were started for and must not count against the next one.
        private static readonly List<Image> _heads = new List<Image>();
        private static int _pendingPortraits;
        private static int _rowGeneration;
        private static bool _building;
        private static RowReveal _reveal;

        /// <summary>
        /// Everything a card needs to be restyled when the selection moves, kept on the card itself
        /// rather than in a parallel list that a teardown could get out of step with.
        /// </summary>
        private sealed class CardState : MonoBehaviour
        {
            public int CharacterId;
            public Outline Border;
            public Text NameLabel;
            public Color NameNormal;
        }

        /// <summary>
        /// Builds the row under <paramref name="parent"/> and returns each card's Selectable in
        /// screen order, for the caller to hand to the controller navigation.
        /// </summary>
        internal static List<Selectable> Build(
            Transform parent,
            IList<GeoCharacter> crew,
            float panelWidth,
            Font font,
            int baseFontSize,
            Action<GeoCharacter> onSelected,
            Action onFacesShown)
        {
            List<Selectable> selectables = new List<Selectable>();
            Clear();

            if (parent == null || crew == null || crew.Count == 0)
            {
                return selectables;
            }

            int count = Mathf.Min(crew.Count, MaxCards);
            float cardWidth = ResolveCardWidth(count, panelWidth);
            float portraitHeight = cardWidth / PortraitAspect;
            float cardHeight = portraitHeight + NameGap + NameRowHeight;

            GameObject row = new GameObject(RowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            _reveal = row.AddComponent<RowReveal>();
            _reveal.FacesShown = onFacesShown;
            _building = true;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = CardSpacing;

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = cardHeight;
            rowLayout.preferredHeight = cardHeight;

            float pixelsPerUnit = MeasurePixelsPerUnit(parent);
            Vector2Int renderResolution = ResolveRenderResolution(cardWidth, portraitHeight, pixelsPerUnit);

            for (int i = 0; i < count; i++)
            {
                GeoCharacter character = crew[i];
                if (character == null)
                {
                    continue;
                }

                Selectable selectable = BuildCard(
                    row.transform,
                    character,
                    cardWidth,
                    portraitHeight,
                    cardHeight,
                    renderResolution,
                    font,
                    baseFontSize,
                    onSelected);

                if (selectable != null)
                {
                    selectables.Add(selectable);
                }
            }

            // Every head was cached, or every render failed straight away - nothing to wait for.
            _building = false;
            RevealIfSettled();

            return selectables;
        }

        /// <summary>
        /// One card's portrait has arrived, or will not. Ignored for a row that has since been
        /// replaced - its renders carry on after the row is gone, and report back to whatever is
        /// current by then.
        /// </summary>
        private static void OnPortraitSettled(int generation)
        {
            if (generation != _rowGeneration)
            {
                return;
            }

            _pendingPortraits = Mathf.Max(0, _pendingPortraits - 1);
            RevealIfSettled();
        }

        private static void RevealIfSettled()
        {
            // Not while the row is still being built: a cached head reports back on the spot, and
            // the count would reach zero after the first card with the rest still to be asked for.
            if (!_building && _pendingPortraits <= 0 && _reveal != null)
            {
                _reveal.Begin(_heads);
            }
        }

        /// <summary>
        /// Shows the row's heads all at once, with a short fade, when the last of them is ready.
        ///
        /// The renders take different times - most of each is loading that operative's armour, and
        /// what is already loaded from an earlier render costs nothing - so shown as they arrived the
        /// heads came in one at a time, in a different order and at a different pace every time the
        /// screen opened. Held back and shown together, the row appears as one thing. The total wait
        /// is the same: it is the last head that decides it either way.
        ///
        /// Unscaled time, because the geoscape clock is stopped while an event is on screen.
        /// </summary>
        private sealed class RowReveal : MonoBehaviour
        {
            private readonly List<Image> _images = new List<Image>();
            private float _builtAt;
            private bool _revealing;

            private void Awake()
            {
                _builtAt = Time.unscaledTime;
            }

            /// <summary>
            /// Run once, as the faces start to show. The incident screen holds the large leader
            /// picture back until then, so the row arrives first and the leader picture after it -
            /// and by then the selected operative's armour is already loaded, which makes the
            /// leader picture a fraction of a second instead of a load of its own. Lives on the row,
            /// so a row torn down before it is shown never fires it for the screen that replaced it.
            /// </summary>
            internal Action FacesShown;

            internal void Begin(List<Image> images)
            {
                if (_revealing)
                {
                    return;
                }

                _images.Clear();
                _images.AddRange(images);
                _revealing = true;

                Action shown = FacesShown;
                FacesShown = null;

                try
                {
                    shown?.Invoke();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private void Update()
            {
                try
                {
                    if (!_revealing)
                    {
                        // A render that never reports back must not hold the whole row hostage.
                        if (Time.unscaledTime - _builtAt < MaxRevealWaitSeconds)
                        {
                            return;
                        }

                        Begin(_heads);
                    }

                    float step = Time.unscaledDeltaTime / RevealFadeSeconds;
                    bool done = true;

                    foreach (Image image in _images)
                    {
                        if (image == null)
                        {
                            continue;
                        }

                        Color color = image.color;
                        color.a = Mathf.Min(1f, color.a + step);
                        image.color = color;
                        done &= color.a >= 1f;
                    }

                    // A head still missing when the timeout forced the reveal is already at full
                    // alpha by now, so it simply appears the moment its sprite does.
                    if (done)
                    {
                        enabled = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    enabled = false;
                }
            }
        }

        /// <summary>
        /// Moves the amber border to the given operative's card. Safe to call for an operative with
        /// no card - a crew larger than <see cref="MaxCards"/> has members the row does not show.
        /// </summary>
        internal static void SetSelected(GeoCharacter character)
        {
            _selectedCharacterId = character?.Id ?? -1;

            // The selected face is the one being looked at, so it goes to the front of whatever is
            // still waiting to render - on the way in, and again whenever the player moves along the
            // row faster than the heads arrive.
            PortraitGenerator.PrioritiseCardPortrait(_selectedCharacterId);

            foreach (CardState card in Cards)
            {
                if (card == null)
                {
                    continue;
                }

                ApplySelectionVisual(card, card.CharacterId == _selectedCharacterId);
            }
        }

        /// <summary>
        /// Forgets the cards of the encounter that is going away. The GameObjects themselves are
        /// destroyed with the panel they hang under; what has to be dropped here is this list, which
        /// would otherwise keep handing out destroyed components to the next encounter.
        /// </summary>
        internal static void Clear()
        {
            Cards.Clear();
            _selectedCharacterId = -1;

            // A new generation, so renders still reporting back for the old row are ignored.
            _rowGeneration++;
            _heads.Clear();
            _pendingPortraits = 0;
            _building = false;
            _reveal = null;
        }

        private static float ResolveCardWidth(int count, float panelWidth)
        {
            if (count <= 0)
            {
                return MaxCardWidth;
            }

            float available = panelWidth - (CardSpacing * (count - 1));
            float fitted = available / count;
            return Mathf.Clamp(fitted, MinCardWidth, MaxCardWidth);
        }

        /// <summary>
        /// Render size for one card: its layout size converted to the screen pixels it is actually
        /// drawn at.
        ///
        /// Layout units are not pixels on this canvas - it draws at about half a pixel per unit at
        /// 1080p - and this used to treat them as though they were, so every card was rendered at
        /// about twice the size it is shown at before the renderer's own four-times supersampling was
        /// applied on top: some sixty samples per displayed pixel where sixteen already smooth every
        /// edge. The texture keeps mipmaps, so a card drawn slightly smaller than it was rendered
        /// still filters cleanly.
        /// </summary>
        private static Vector2Int ResolveRenderResolution(float cardWidth, float portraitHeight, float pixelsPerUnit)
        {
            float width = cardWidth * pixelsPerUnit;
            float height = portraitHeight * pixelsPerUnit;

            float fit = Mathf.Min(1f, MaxCardRenderResolution / Mathf.Max(width, height));
            return new Vector2Int(
                Mathf.RoundToInt(width * fit),
                Mathf.RoundToInt(height * fit));
        }

        /// <summary>
        /// Screen pixels per layout unit where the row is drawn, measured by projecting a span of
        /// the row's parent onto the screen. The row has not been laid out yet, but its parent's
        /// scale already is what it will be, and that is all this needs. Works whether the canvas
        /// draws straight to the screen or through a camera.
        /// </summary>
        private static float MeasurePixelsPerUnit(Transform reference)
        {
            try
            {
                Canvas canvas = reference != null ? reference.GetComponentInParent<Canvas>() : null;
                if (canvas != null)
                {
                    Camera camera = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

                    const float span = 100f;
                    Vector2 from = RectTransformUtility.WorldToScreenPoint(camera, reference.TransformPoint(Vector3.zero));
                    Vector2 to = RectTransformUtility.WorldToScreenPoint(camera, reference.TransformPoint(new Vector3(span, 0f, 0f)));

                    float measured = Vector2.Distance(from, to) / span;
                    if (measured > 0.05f && measured < 20f)
                    {
                        return measured;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            // The ratio measured on this canvas at 1080p, scaled to the current screen.
            return (1f / UnitsPerPixel) * (Screen.height / 1080f);
        }

        private static Selectable BuildCard(
            Transform row,
            GeoCharacter character,
            float cardWidth,
            float portraitHeight,
            float cardHeight,
            Vector2Int renderResolution,
            Font font,
            int baseFontSize,
            Action<GeoCharacter> onSelected)
        {
            try
            {
                GameObject card = new GameObject(
                    CardNamePrefix + character.GetName(),
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image),
                    typeof(Button),
                    typeof(CardState));
                card.transform.SetParent(row, false);

                // An invisible plate over the whole card, so the name under the portrait selects the
                // operative as readily as the portrait does - the name is the part of a card a player
                // reads, and a card whose label is not part of the target reads as broken.
                Image hitArea = card.GetComponent<Image>();
                hitArea.color = Color.clear;
                hitArea.raycastTarget = true;

                LayoutElement cardLayout = card.GetComponent<LayoutElement>();
                cardLayout.preferredWidth = cardWidth;
                cardLayout.preferredHeight = cardHeight;
                cardLayout.minWidth = cardWidth;
                cardLayout.minHeight = cardHeight;

                CardState state = card.GetComponent<CardState>();
                state.CharacterId = character.Id;

                BuildPortraitBox(card.transform, character, state, cardWidth, portraitHeight, renderResolution, font);
                BuildNameLine(card.transform, character, state, cardWidth, portraitHeight, font, baseFontSize);

                Button button = card.GetComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hitArea;

                GeoCharacter captured = character;
                button.onClick.AddListener(() =>
                {
                    try
                    {
                        onSelected?.Invoke(captured);
                    }
                    catch (Exception ex) { TFTVLogger.Error(ex); }
                });

                AddButtonSounds(card);

                Cards.Add(state);
                ApplySelectionVisual(state, character.Id == _selectedCharacterId);
                return button;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static void BuildPortraitBox(
            Transform card,
            GeoCharacter character,
            CardState state,
            float cardWidth,
            float portraitHeight,
            Vector2Int renderResolution,
            Font font)
        {
            GameObject box = new GameObject("Portrait", typeof(RectTransform), typeof(Image), typeof(Outline));
            box.transform.SetParent(card, false);

            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 1f);
            boxRect.anchorMax = new Vector2(0.5f, 1f);
            boxRect.pivot = new Vector2(0.5f, 1f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(cardWidth, portraitHeight);

            // The plate is what the border is drawn around and what a card shows while its head is
            // still rendering, so it is the background rather than the portrait Image itself - a
            // portrait that never arrives would otherwise take the border with it.
            Image plate = box.GetComponent<Image>();
            plate.color = IncidentUIStyle.CardBackground;
            plate.raycastTarget = true;

            state.Border = box.GetComponent<Outline>();
            state.Border.useGraphicAlpha = false;

            GameObject portraitObject = new GameObject("Head", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(box.transform, false);

            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = Vector2.one;
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;

            Image portrait = portraitObject.GetComponent<Image>();
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;
            portrait.enabled = false;

            // Invisible until the whole row is ready - see RowReveal.
            portrait.color = new Color(1f, 1f, 1f, 0f);
            _heads.Add(portrait);

            _pendingPortraits++;
            int generation = _rowGeneration;
            PortraitGenerator.RequestCardPortrait(character, portrait, renderResolution, _ => OnPortraitSettled(generation));

            AddCornerShade(box.transform);
            AddClassIcon(box.transform, character);
            AddAffinityBadge(box.transform, character, font, cardWidth * BadgeWidthFraction);
        }

        /// <summary>
        /// Gives a card the game's own button sounds - the click, and the hover in and out - copied
        /// from a vanilla button rather than chosen here, so selecting an operative sounds like
        /// pressing any other button on the geoscape.
        ///
        /// The crew slot the cards replaced is the first choice of source, since selecting a soldier
        /// is what it was for; the encounter's own choice buttons and trade's exit button are there in
        /// case a patch ever moves the sounds off it.
        /// </summary>
        private static void AddButtonSounds(GameObject card)
        {
            try
            {
                UIButtonSounds source = ResolveButtonSoundSource();
                if (source == null)
                {
                    return;
                }

                // Added after the Button, which it finds on Awake and hooks its pointer events to.
                UIButtonSounds sounds = card.AddComponent<UIButtonSounds>();
                sounds.Click = source.Click;
                sounds.Enter = source.Enter;
                sounds.Exit = source.Exit;
                sounds.ClickDisabled = source.ClickDisabled;
                sounds.EnterDisabled = source.EnterDisabled;
                sounds.ExitDisabled = source.ExitDisabled;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static UIButtonSounds ResolveButtonSoundSource()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

            // Per geoscape: the source is a component on a prefab or a live module of that level.
            if (_buttonSoundSource != null && _buttonSoundLevel == level)
            {
                return _buttonSoundSource;
            }

            _buttonSoundLevel = level;
            _buttonSoundSource = null;

            GeoscapeModulesData modules = level?.View?.GeoscapeModules;
            if (modules == null)
            {
                return null;
            }

            Component[] candidates =
            {
                modules.SoldierEquipModule?.SoldierSlotPrefab,
                modules.SiteEncountersModule?.ChoiceButtonsContainer != null
                    ? modules.SiteEncountersModule.ChoiceButtonsContainer.transform
                    : null,
                modules.TradeModule?.ExitBtn,
            };

            foreach (Component candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                UIButtonSounds found = candidate.GetComponentInChildren<UIButtonSounds>(true);
                if (found != null && found.Click != null)
                {
                    _buttonSoundSource = found;
                    return found;
                }
            }

            TFTVLogger.Always("[IncidentUI] No vanilla button sounds found; crew cards will be silent.");
            return null;
        }

        /// <summary>
        /// A dark radial falloff out of the bottom right corner, under the class mark. The mark is a
        /// light glyph and the corner of a portrait is whatever armour happens to be there, often
        /// light too; this gives it something dark to sit on without boxing it in.
        /// </summary>
        private static void AddCornerShade(Transform box)
        {
            Sprite shade = ResolveCornerShadeSprite();
            if (shade == null)
            {
                return;
            }

            GameObject shadeObject = new GameObject("ClassIconShade", typeof(RectTransform), typeof(Image));
            shadeObject.transform.SetParent(box, false);

            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.anchorMin = new Vector2(1f, 0f);
            shadeRect.anchorMax = new Vector2(1f, 0f);
            shadeRect.pivot = new Vector2(1f, 0f);
            shadeRect.anchoredPosition = Vector2.zero;
            float size = ClassIconSize * CornerShadeScale;
            shadeRect.sizeDelta = new Vector2(size, size);

            Image image = shadeObject.GetComponent<Image>();
            image.sprite = shade;
            image.raycastTarget = false;
        }

        /// <summary>
        /// The corner shade, loaded once. A shipped image, since a UI Image has no gradient of its own.
        /// </summary>
        private static Sprite ResolveCornerShadeSprite()
        {
            if (!_cornerShadeResolved)
            {
                _cornerShadeResolved = true;
                _cornerShadeSprite = Helper.CreateSpriteFromImageFile(CornerShadeImageName);

                if (_cornerShadeSprite == null)
                {
                    TFTVLogger.Always($"[IncidentUI] {CornerShadeImageName} could not be loaded; class icons go unshaded.");
                }
            }

            return _cornerShadeSprite;
        }

        /// <summary>
        /// The operative's class mark, bottom right: the game's own icon element, so a dual-class
        /// operative gets the single split diamond the rest of the game gives them rather than two
        /// separate icons side by side.
        ///
        /// Scaled, not resized. The element is laid out for the wide slot it was authored in and its
        /// main, secondary and splitter images keep their own anchors inside it, so setting the root
        /// to card size leaves them hanging off the edge - which is exactly what happened the first
        /// time. Scaling the whole thing keeps the composition the artwork depends on and just makes
        /// it smaller.
        /// </summary>
        private static void AddClassIcon(Transform box, GeoCharacter character)
        {
            try
            {
                ActorClassIconElement prefab = ResolveClassIconPrefab();
                if (prefab == null)
                {
                    return;
                }

                RectTransform prefabRect = prefab.transform as RectTransform;
                float authoredHeight = prefabRect != null ? prefabRect.rect.height : 0f;
                if (authoredHeight <= 1f)
                {
                    return;
                }

                ActorClassIconElement icon = UnityEngine.Object.Instantiate(prefab, box, false);
                icon.gameObject.name = "ClassIcon";
                icon.gameObject.SetActive(true);

                // The prefab's own LayoutElement is sized for that slot, and this card places the
                // icon by anchors rather than by a layout group.
                LayoutElement prefabLayout = icon.GetComponent<LayoutElement>();
                if (prefabLayout != null)
                {
                    UnityEngine.Object.DestroyImmediate(prefabLayout);
                }

                RectTransform iconRect = icon.transform as RectTransform;
                if (iconRect != null)
                {
                    float scale = ClassIconSize / authoredHeight;

                    // Anchored and pivoted into the corner it sits in, so the scale shrinks it
                    // towards that corner and the inset stays the inset whatever the prefab's size.
                    iconRect.anchorMin = new Vector2(1f, 0f);
                    iconRect.anchorMax = new Vector2(1f, 0f);
                    iconRect.pivot = new Vector2(1f, 0f);
                    iconRect.anchoredPosition = new Vector2(-ClassIconInset, ClassIconInset);
                    iconRect.localScale = new Vector3(scale, scale, 1f);
                }

                icon.SetClassIcons(character.GetClassViewElementDefs());
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// The game's own crew slot prefab, which is where the class icon element comes from. Read
        /// off the live geoscape rather than passed in, so nothing else has to carry it here.
        /// </summary>
        private static ActorClassIconElement ResolveClassIconPrefab()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            SoldierSlotController slot = level?.View?.GeoscapeModules?.SoldierEquipModule?.SoldierSlotPrefab;
            return slot != null ? slot.IconElement : null;
        }

        /// <summary>
        /// The operative's current affinity and its rank, top left. Operatives with no affinity yet
        /// get no badge - an empty plate on most of the row would read as a missing icon rather than
        /// as "nothing to show".
        /// </summary>
        private static void AddAffinityBadge(Transform box, GeoCharacter character, Font font, float badgeSize)
        {
            if (!LeaderSelection.TryGetCurrentAffinity(character, out LeaderSelection.AffinityApproach approach, out int rank)
                || rank <= 0)
            {
                return;
            }

            Sprite icon = LeaderSelection.GetAffinityAbility(approach, rank)?.ViewElementDef?.SmallIcon;
            if (icon == null)
            {
                return;
            }

            GameObject plate = new GameObject("AffinityBadge", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(box, false);

            RectTransform plateRect = plate.GetComponent<RectTransform>();

            // Centred on the portrait's top edge, half above it and half over the picture. In the
            // corner it read as something stuck on the card; straddling the edge it reads as the
            // card's own label, and it is in the same place on every card, so a row of operatives
            // can be scanned for affinities without looking at each one in turn.
            plateRect.anchorMin = new Vector2(0.5f, 1f);
            plateRect.anchorMax = new Vector2(0.5f, 1f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = Vector2.zero;
            plateRect.sizeDelta = new Vector2(badgeSize, badgeSize);

            Image plateImage = plate.GetComponent<Image>();
            plateImage.color = IncidentUIStyle.BadgePlate;
            plateImage.raycastTarget = false;

            // The portrait's own frame, carried onto the badge that straddles it, so the badge
            // reads as part of the card rather than as a sticker on it.
            Outline plateFrame = plate.AddComponent<Outline>();
            plateFrame.effectColor = IncidentUIStyle.CardBorder;
            plateFrame.effectDistance = new Vector2(NormalBorderWidth, NormalBorderWidth);
            plateFrame.useGraphicAlpha = false;

            GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyphObject.transform.SetParent(plate.transform, false);

            RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = new Vector2(2f, 2f);
            glyphRect.offsetMax = new Vector2(-2f, -2f);

            Image glyph = glyphObject.GetComponent<Image>();
            glyph.sprite = icon;
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;

            AddRankNumeral(plate.transform, rank, font, badgeSize);
        }

        /// <summary>
        /// The affinity's rank, in the badge's corner. Rank is the difference between an operative
        /// who has touched an approach and one who is good at it, and it is the number the response
        /// icons' arrows below are promising to raise.
        /// </summary>
        private static void AddRankNumeral(Transform plate, int rank, Font font, float badgeSize)
        {
            GameObject numeralObject = new GameObject("Rank", typeof(RectTransform), typeof(Text), typeof(Outline));
            numeralObject.transform.SetParent(plate, false);

            RectTransform numeralRect = numeralObject.GetComponent<RectTransform>();
            numeralRect.anchorMin = new Vector2(1f, 0f);
            numeralRect.anchorMax = new Vector2(1f, 0f);
            numeralRect.pivot = new Vector2(1f, 0f);
            numeralRect.anchoredPosition = new Vector2(1f, -1f);
            numeralRect.sizeDelta = new Vector2(badgeSize * 0.6f, badgeSize * 0.6f);

            Text numeral = numeralObject.GetComponent<Text>();
            numeral.text = rank.ToString();
            numeral.alignment = TextAnchor.LowerRight;
            numeral.horizontalOverflow = HorizontalWrapMode.Overflow;
            numeral.verticalOverflow = VerticalWrapMode.Overflow;
            numeral.raycastTarget = false;
            numeral.color = Color.white;
            numeral.fontStyle = FontStyle.Bold;
            numeral.font = font;
            numeral.fontSize = Mathf.RoundToInt(badgeSize * 0.52f);

            // The glyph under the numeral is a bright icon, and a plain white digit on it is not
            // reliably legible whatever the affinity happens to look like.
            Outline shadow = numeralObject.GetComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = false;
        }

        /// <summary>
        /// Rank and name under the portrait, in the geoscape's own "7 Jacob Eber" form. The name is
        /// the shortened one the incident prose uses, so an operative is called the same thing here
        /// and in the outcome text.
        /// </summary>
        private static void BuildNameLine(
            Transform card,
            GeoCharacter character,
            CardState state,
            float cardWidth,
            float portraitHeight,
            Font font,
            int baseFontSize)
        {
            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameObject.transform.SetParent(card, false);

            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -portraitHeight - NameGap);
            nameRect.sizeDelta = new Vector2(cardWidth, NameRowHeight);

            int preferredSize = Mathf.Max(MinNameFontSize, Mathf.RoundToInt(baseFontSize * NameFontScale));

            Text label = nameObject.GetComponent<Text>();
            label.alignment = TextAnchor.UpperCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.font = font;
            label.fontSize = preferredSize;
            label.supportRichText = true;
            label.color = Color.white;

            // Names are shortened before they get here, but a single long surname still outruns a
            // card this narrow. Shrinking the line is better than wrapping it: a card is a fixed
            // height in a row of them, so a second line would be cut off rather than shown, and a
            // name cut off mid-word identifies nobody.
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = MinNameFontSize;
            label.resizeTextMaxSize = preferredSize;

            label.text = BuildNameText(character);

            state.NameLabel = label;
            state.NameNormal = label.color;
        }

        private static string BuildNameText(GeoCharacter character)
        {
            string shortName = LeaderSelection.ShortenOperativeName(character.DisplayName, character.Id);
            if (string.IsNullOrEmpty(shortName))
            {
                shortName = character.GetName() ?? string.Empty;
            }

            int level = character.LevelProgression != null ? character.LevelProgression.Level : 0;
            if (level <= 0)
            {
                return shortName;
            }

            string amber = ColorUtility.ToHtmlStringRGB(IncidentUIStyle.Amber);
            return $"<color=#{amber}>{level}</color> {shortName}";
        }

        private static void ApplySelectionVisual(CardState card, bool selected)
        {
            if (card == null)
            {
                return;
            }

            if (card.Border != null)
            {
                card.Border.effectColor = selected ? IncidentUIStyle.Amber : IncidentUIStyle.CardBorder;
                float width = selected ? SelectedBorderWidth : NormalBorderWidth;
                card.Border.effectDistance = new Vector2(width, width);
            }

            if (card.NameLabel != null)
            {
                card.NameLabel.color = selected ? IncidentUIStyle.SelectedName : card.NameNormal;
            }
        }
    }
}
