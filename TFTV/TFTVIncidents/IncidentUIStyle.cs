using UnityEngine;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// The colours the incident resolution screen is drawn in, in one place so the crew cards, the
    /// approach icons and the response text agree about what "selected" and "matching" look like.
    ///
    /// Two accents carry the whole screen. Amber is selection - the card the player is on, the
    /// approach that will be taken - and is the accent the geoscape uses everywhere else. Green is
    /// the operative's own affinity paying off: a response they are suited to, and the rank they
    /// would come back with. Nothing else is coloured, so neither accent has to compete to be seen.
    /// </summary>
    internal static class IncidentUIStyle
    {
        /// <summary>Selection: the chosen card's border, the chosen approach's outline.</summary>
        internal static readonly Color Amber = new Color(1f, 0.72f, 0.15f, 1f);

        /// <summary>
        /// The frame every portrait carries, and the affinity badge on top of it: #56606e, the
        /// designer's slate. Opaque - a frame that lets the artwork behind show through changes
        /// colour with every incident.
        /// </summary>
        internal static readonly Color CardBorder = new Color(0x56 / 255f, 0x60 / 255f, 0x6e / 255f, 1f);

        /// <summary>
        /// The selected operative's name. Colour rather than weight marks the selection: bold
        /// changes the name's width, so the selected label reflowed against its neighbours.
        /// </summary>
        internal static readonly Color SelectedName = new Color(1f, 0.86f, 0.25f, 1f);

        /// <summary>
        /// Behind a card while its head is still rendering, and behind every head after.
        ///
        /// Fully opaque, and black rather than merely dark. The portraits are rendered on a
        /// transparent background, so whatever is behind the card shows through them - and what is
        /// behind is the incident's artwork, which is often a bright sky. At any alpha below one the
        /// heads sat on a patch of washed-out grey that changed colour with the picture behind them.
        /// </summary>
        internal static readonly Color CardBackground = new Color(0f, 0f, 0f, 1f);

        /// <summary>The affinity badge's plate, dark enough to hold a coloured glyph.</summary>
        internal static readonly Color BadgePlate = new Color(0f, 0f, 0f, 0.72f);

        /// <summary>
        /// The plate behind an approach icon inside a response button.
        ///
        /// Black and opaque, because the affinity artwork is itself an orange mark on a black plate:
        /// drawn on this the two blacks merge, so the icon reads as one tall plate with a small mark
        /// on it rather than as a small plate floating in a button.
        /// </summary>
        internal static readonly Color ApproachPlate = new Color(0f, 0f, 0f, 1f);

        /// <summary>A response the selected operative's affinity applies to, and the level-up arrow.</summary>
        internal static readonly Color AffinityMatch = new Color(0.45f, 0.92f, 0.35f, 1f);

        /// <summary>An approach on offer that this operative cannot bring an affinity to.</summary>
        internal static readonly Color ApproachInactive = new Color(1f, 1f, 1f, 0.32f);

        /// <summary>An approach that is not chosen but could be - a rookie picking what to develop.</summary>
        internal static readonly Color ApproachSelectable = new Color(1f, 1f, 1f, 0.7f);

        /// <summary>Hours, and the payoff icons under a response: present, but not competing with the text.</summary>
        internal static readonly Color Payoff = new Color(1f, 1f, 1f, 0.78f);
    }
}
