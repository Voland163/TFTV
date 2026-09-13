using Base.Core;
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

        private const float SelectedBorderWidth = 4f;
        private const float NormalBorderWidth = 1.5f;

        /// <summary>
        /// Ceiling on a rendered card's pixel size. A card is small; past this the extra pixels are
        /// thrown away by the Image and paid for by the renderer anyway.
        /// </summary>
        private const int MaxCardRenderResolution = 512;

        private static readonly List<CardState> Cards = new List<CardState>();
        private static int _selectedCharacterId = -1;

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
            Action<GeoCharacter> onSelected)
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
            float cardHeight = portraitHeight + NameRowHeight;

            GameObject row = new GameObject(RowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

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

            Vector2Int renderResolution = ResolveRenderResolution(cardWidth, portraitHeight);

            TFTVLogger.Always(
                $"[IncidentUI] Crew row: {count} cards, panel {panelWidth:0}w, card {cardWidth:0}x{cardHeight:0}, " +
                $"render {renderResolution.x}x{renderResolution.y}, base font {baseFontSize}.");

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

            return selectables;
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
        /// Render size for one card, from its layout size rather than its on-screen size: the row is
        /// built before it has had a layout pass, so there is nothing on screen to measure yet. The
        /// factor covers the card being drawn on a display denser than the layout's own units.
        ///
        /// No oversampling on top of that. The renderer supersamples every portrait four times over
        /// already, so asking it for more pixels than the card is drawn at buys nothing and is paid
        /// for sixteen times.
        /// </summary>
        private static Vector2Int ResolveRenderResolution(float cardWidth, float portraitHeight)
        {
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            float width = cardWidth * scale;
            float height = portraitHeight * scale;

            float fit = Mathf.Min(1f, MaxCardRenderResolution / Mathf.Max(width, height));
            return new Vector2Int(
                Mathf.RoundToInt(width * fit),
                Mathf.RoundToInt(height * fit));
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

            PortraitGenerator.RequestCardPortrait(character, portrait, renderResolution);

            AddClassIcon(box.transform, character);
            AddAffinityBadge(box.transform, character, font, cardWidth * BadgeWidthFraction);
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
            nameRect.anchoredPosition = new Vector2(0f, -portraitHeight - 2f);
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
                card.NameLabel.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
