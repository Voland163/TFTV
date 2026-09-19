using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Tactical
{
    /// <summary>
    /// Acid shows up as a single number on three screens - the healthbar chips, the hover tooltip and
    /// the status card - and that number is a sum the game itself never uses. A soldier reading
    /// "ACID 50" is carrying, say, 20 on his torso and 30 on an arm: two separate AcidStatus
    /// instances, each corroding its own plate on its own clock, each billing his health separately
    /// once that plate is gone.
    ///
    /// Everything needed to say so is already in hand. TacticalActorViewBase.StatusInfo carries
    /// TargetSlots alongside the summed Value, and all three consumers throw it away; the per-turn
    /// outcome comes from TFTVAcid, so the readout resolves a tick exactly the way the damage path
    /// does.
    ///
    /// Resistance gets its own line because it is otherwise invisible by construction: it does not
    /// change the acid value applied, and it does not reduce armor corrosion, so a resistant soldier
    /// and a bare one render identically until health starts ticking.
    /// </summary>
    internal static class AcidReadout
    {
        private const string LogPrefix = "[AcidReadout]";

        // One line each per session: enough to tell from a log whether a surface ever ran and what
        // it saw, without spamming a file that is written on every healthbar refresh.
        private static bool _tracedHealthbar;
        private static bool _tracedHealthbarRow;
        private static bool _tracedChipTooltip;
        private static bool _tracedTooltip;
        private static bool _tracedCard;

        private static void TraceOnce(ref bool flag, string message)
        {
            if (flag)
            {
                return;
            }

            flag = true;
            TFTVLogger.Always($"{LogPrefix} {message}");
        }

        #region text

        /// <summary>
        /// Named separately from the body text because it only appears when the character actually
        /// carries a multiplier, and because several sources compound: two at 0.5 make 0.25. The
        /// doubled burn-off does not compound with them - the engine tests the multiplier against 1
        /// rather than scaling by it - so the many-source wording says so rather than implying a
        /// second vest speeds anything up.
        /// </summary>
        private static string ResistanceNote(TacticalActor actor)
        {
            float resistance = TFTVAcid.GetAcidResistance(actor);

            if (Mathf.Approximately(resistance, 1f))
            {
                return null;
            }

            string multiplier = resistance.ToString("0.##");

            if (resistance > 1f)
            {
                return TFTVCommonMethods.FormatKey("TFTV_ACID_VULNERABILITY_NOTE", multiplier);
            }

            int sources = TFTVAcid.GetAcidResistanceSourceCount(actor);

            return sources > 1
                ? TFTVCommonMethods.FormatKey("TFTV_ACID_RESISTANCE_NOTE_MANY", multiplier, sources)
                : TFTVCommonMethods.FormatKey("TFTV_ACID_RESISTANCE_NOTE_ONE", multiplier);
        }

        private static string BuildCardDescription(TacticalActor actor, List<TFTVAcid.LimbAcid> limbs)
        {
            // No blank separator lines: the Text is best-fit against a fixed height, so an empty
            // line costs exactly as much font size as a full one.
            StringBuilder builder = new StringBuilder();

            // No limb breakdown here: the body-part rows on the same screen already show it per
            // limb, and a character with acid on many parts would not fit.
            builder.Append(TFTVCommonMethods.ConvertKeyToString("TFTV_ACID_CARD_BODY"));
            builder.Append(" ");

            // Limbs can decay at different rates - two workshop modules halve the burn-off time on
            // bionics only - so the sentence commits to a number only when they all agree.
            float decay = TFTVAcid.GetUniformAcidDecay(actor);

            builder.AppendLine(float.IsNaN(decay)
                ? TFTVCommonMethods.ConvertKeyToString("TFTV_ACID_CARD_DECAY_VARIES")
                : TFTVCommonMethods.FormatKey("TFTV_ACID_CARD_DECAY", Mathf.RoundToInt(decay)));

            string note = ResistanceNote(actor);
            if (note != null)
            {
                builder.AppendLine(note);
            }

            return builder.ToString().TrimEnd();
        }

        #endregion

        #region healthbar chips

        /// <summary>
        /// The chip row has no room for a breakdown, so it gets the two smallest honest additions: a
        /// limb count, and a sink to the end of the row when acid will cost no Hit Points next turn
        /// because every affected limb still has armor. Acid otherwise leads the row at its most
        /// eye-catching exactly when it is doing nothing at all.
        ///
        /// Written as a postfix that re-walks what vanilla just built rather than a reimplementation
        /// of UpdateStatuses, so the element pooling, the two visibility lists and the layout rebuild
        /// all stay vanilla.
        /// </summary>
        [HarmonyPatch(typeof(HealthbarUIActorElement), "UpdateStatuses")]
        internal static class HealthbarUIActorElement_UpdateStatuses_AcidDetail_Patch
        {
            private static void Postfix(HealthbarUIActorElement __instance, TacticalActorViewBase ____viewComponent)
            {
                try
                {
                    if (____viewComponent == null)
                    {
                        return;
                    }

                    if (!(____viewComponent.ActorBase is TacticalActor actor))
                    {
                        return;
                    }

                    List<TFTVAcid.LimbAcid> limbs = TFTVAcid.GetLimbAcid(actor);
                    if (limbs.Count == 0)
                    {
                        return;
                    }

                    bool harmless = !limbs.Any(limb => limb.WillCostHealth);
                    DamageOverTimeStatusDef acidDef = actor.Status
                        .GetStatuses<AcidStatus>()
                        .FirstOrDefault()?.DamageOverTimeStatusDef;

                    if (acidDef == null)
                    {
                        return;
                    }

                    List<TacticalActorViewBase.StatusInfo> shown = ____viewComponent.GetHealthbarStatuses(stackAsSingle: true);

                    TraceOnce(ref _tracedHealthbar,
                        $"healthbar: {limbs.Count} acid limb(s), harmless={harmless}, chips={shown.Count}");

                    DecorateContainer(__instance.StatusesList, shown, acidDef, limbs.Count, harmless,
                        TacStatusDef.HealthBarVisibility.VisibleWhenSelected);
                    DecorateContainer(__instance.StatusesListAlwaysVisible, shown, acidDef, limbs.Count, harmless,
                        TacStatusDef.HealthBarVisibility.AlwaysVisible);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Vanilla fills each container by sibling order, one element per status in list order.
            ///
            /// Acid that will cost no health this turn is sunk to the end of that list, but by
            /// rewriting what each element shows rather than by moving the elements: vanilla assigns
            /// positionally, so reparenting a chip would make the next refresh hand its data to a
            /// different GameObject and the row would flicker between the two orders. Reordering the
            /// data instead is idempotent - the same statuses always produce the same row.
            ///
            /// Only acid is reordered. Burning and poison would need their own per-limb models to
            /// project, and guessing at them here would be inventing numbers.
            /// </summary>
            private static void DecorateContainer(
                RectTransform container,
                List<TacticalActorViewBase.StatusInfo> shown,
                DamageOverTimeStatusDef acidDef,
                int limbCount,
                bool harmless,
                TacStatusDef.HealthBarVisibility visibility)
            {
                if (container == null)
                {
                    return;
                }

                List<TacticalActorViewBase.StatusInfo> ordered = shown
                    .Where(status => status.Def.VisibleOnHealthbar == visibility)
                    .ToList();

                int acidIndex = ordered.FindIndex(status => status.Def == acidDef);
                if (acidIndex < 0)
                {
                    return;
                }

                // Vanilla fills the container with "foreach (Transform item in container)" - direct
                // children only. GetComponentsInChildren recurses and picked up a nested element,
                // so the count never matched and every refresh bailed at the guard below.
                List<HealthbarStatusElement> elements = new List<HealthbarStatusElement>();
                foreach (Transform child in container)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    HealthbarStatusElement chip = child.GetComponent<HealthbarStatusElement>();
                    if (chip != null)
                    {
                        elements.Add(chip);
                    }
                }

                TraceOnce(ref _tracedHealthbarRow,
                    $"healthbar container: {ordered.Count} status(es), {elements.Count} element(s), acidIndex={acidIndex}");

                if (elements.Count != ordered.Count)
                {
                    // Vanilla and this postfix disagree about what is on screen; leave it alone
                    // rather than write the wrong status into a chip.
                    return;
                }

                if (harmless && acidIndex != ordered.Count - 1)
                {
                    TacticalActorViewBase.StatusInfo acid = ordered[acidIndex];
                    ordered.RemoveAt(acidIndex);
                    ordered.Add(acid);
                    acidIndex = ordered.Count - 1;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        elements[i].SetStatus(ordered[i].Def.GetHealthBarVisuals(), ordered[i].Value, ordered[i].Limit);
                    }
                }

                HealthbarStatusElement element = elements[acidIndex];

                // "×2" reads as a multiplier on the 50. A bracketed count reads as "on 2 limbs".
                if (limbCount > 1 && element.StatusValue != null)
                {
                    element.StatusValue.text = $"{element.StatusValue.text} ({limbCount})";
                }
            }
        }

        #endregion

        #region hover tooltip

        /// <summary>
        /// One row per acid'd limb, inserted under the Acid row of the short actor tooltip.
        ///
        /// Called from UICharacterSelectedVanillaFixes.GenerateData rather than bolted on with a
        /// patch: that method is TFTV's own replacement for the vanilla tooltip and it overwrites
        /// __result wholesale, so a second postfix on PrepareShortActorInfo is discarded no matter
        /// which order Harmony runs them in.
        ///
        /// The rows carry no icon, so they indent under Acid and read as its breakdown rather than
        /// as three more statuses; a single line down their left edge groups them.
        /// </summary>
        internal static void AppendAcidBreakdown(
            List<ShortActorInfoTooltipDataEntry> entries,
            TacticalActor actor,
            TacStatusDef statusDef)
        {
            try
            {
                if (entries == null || actor == null || !(statusDef is DamageOverTimeStatusDef))
                {
                    return;
                }

                DamageOverTimeStatusDef acidDef = actor.Status?
                    .GetStatuses<AcidStatus>()
                    .FirstOrDefault()?.DamageOverTimeStatusDef;

                if (acidDef == null || statusDef != acidDef)
                {
                    return;
                }

                List<TFTVAcid.LimbAcid> limbs = TFTVAcid.GetLimbAcid(actor);

                // A single affected limb already reads correctly from the Acid row itself.
                if (limbs.Count < 2)
                {
                    return;
                }

                // The sum stays on the Acid row but is muted once drawn (see
                // UIModuleShortActorInfoTooltip_SetData_LimbBracket_Patch): it is a total the game
                // never applies as such, and the limbs underneath carry the real numbers.
                _breakdownEntries = entries;
                _breakdownSumIndex = entries.Count - 1;
                _breakdownCount = limbs.Count;

                foreach (TFTVAcid.LimbAcid limb in limbs)
                {
                    entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        // Indented past the bracket line drawn beside these rows.
                        TextContent = $"{LimbIndent}{limb.DisplayName}",
                        ValueContent = Mathf.RoundToInt(limb.Acid).ToString(),
                    });
                }

                TraceOnce(ref _tracedTooltip, $"tooltip: added {limbs.Count} limb row(s) under Acid");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private const string LimbIndent = "   ";
        private const string BracketObjectName = "TFTV_LimbBracket";
        private const float BracketLineWidth = 2f;
        private static readonly Color BracketColor = new Color(0.85f, 0.85f, 0.85f, 0.8f);
        private static readonly Color MutedValueColor = new Color(0.55f, 0.55f, 0.58f, 1f);

        // Which entry list last had a breakdown appended, and where. The list is created afresh by
        // every GenerateData, so matching it by reference tells the postfix whether the data being
        // drawn is the one the breakdown was added to.
        private static List<ShortActorInfoTooltipDataEntry> _breakdownEntries;
        private static int _breakdownSumIndex;
        private static int _breakdownCount;

        /// <summary>
        /// Draws the limb rows' bracket and mutes the acid total once vanilla has laid the entries
        /// out. The row objects are pooled across actors, so every row is reset first; a row that
        /// carried the bracket for one soldier must not keep it for the next.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleShortActorInfoTooltip), nameof(UIModuleShortActorInfoTooltip.SetData))]
        internal static class UIModuleShortActorInfoTooltip_SetData_LimbBracket_Patch
        {
            private static Color? _defaultValueColor;

            private static void Postfix(UIModuleShortActorInfoTooltip __instance, ShortActorInfoTooltipData actorData)
            {
                try
                {
                    if (!(Traverse.Create(__instance).Field("_textEntries").GetValue() is System.Collections.IList pooled))
                    {
                        return;
                    }

                    List<TextValueSlotController> rows = new List<TextValueSlotController>();
                    foreach (object item in pooled)
                    {
                        if (item is TextValueSlotController row && row != null)
                        {
                            rows.Add(row);
                        }
                    }

                    if (rows.Count == 0)
                    {
                        return;
                    }

                    if (_defaultValueColor == null && rows[0].Value != null)
                    {
                        _defaultValueColor = rows[0].Value.color;
                    }

                    bool ours = actorData.Entries != null
                        && ReferenceEquals(actorData.Entries, _breakdownEntries)
                        && _breakdownSumIndex + _breakdownCount < rows.Count
                        && _breakdownSumIndex + _breakdownCount < actorData.Entries.Count;

                    // Vanilla draws entries into rows in order; if the titles disagree, this is not
                    // the layout the breakdown was recorded for, so decorate nothing.
                    if (ours)
                    {
                        for (int i = _breakdownSumIndex; i <= _breakdownSumIndex + _breakdownCount; i++)
                        {
                            // Compared loosely: the row may case or trim the text it was given.
                            if (rows[i].Title == null
                                || !string.Equals(
                                    rows[i].Title.text?.Trim(),
                                    actorData.Entries[i].TextContent?.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                ours = false;
                                break;
                            }
                        }
                    }

                    RectTransform entriesRoot = __instance.EntriesRoot != null
                        ? __instance.EntriesRoot.transform as RectTransform
                        : null;
                    VerticalLayoutGroup entriesLayout = entriesRoot != null ? entriesRoot.GetComponent<VerticalLayoutGroup>() : null;
                    float gap = entriesLayout != null ? entriesLayout.spacing : 0f;
                    bool layoutSetsHeight = entriesLayout != null && entriesLayout.childControlHeight;

                    // Pooled rows keep whatever height they were given for the last soldier, so
                    // everything goes back to vanilla before this soldier's limbs are tightened.
                    foreach (RectTransform item in _originalHeights.Keys.ToList())
                    {
                        RestoreHeight(item, layoutSetsHeight);
                    }

                    Text sumTitle = ours ? rows[_breakdownSumIndex].Title : null;
                    float limbPitch = sumTitle != null
                        ? Mathf.Max(sumTitle.fontSize * LimbRowLineHeight, MinLimbRowHeight + gap)
                        : 0f;

                    if (ours && entriesRoot != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(entriesRoot);

                        // Row text is pinned to the top of its row, so the distance from one
                        // row's text to the next is that row's height plus the layout's spacing.
                        //
                        //  - limb to limb: about one line of text, so they read as one block;
                        //  - Acid to the first limb: a little more, so the block sits under Acid;
                        //  - the last limb to the next status: more again, so the breakdown is
                        //    clearly closed off before the next status starts.
                        //
                        // Nothing is measured here. The tooltip is filled while it is still hidden
                        // and Unity does not lay out hidden objects, so every rect read at this
                        // point is left over from the last time it was shown - including rows this
                        // patch already shrank. Both distances therefore come from the font size
                        // alone.
                        int lastLimb = _breakdownSumIndex + _breakdownCount;
                        bool hasNext = lastLimb + 1 < rows.Count && lastLimb + 1 < actorData.Entries.Count;

                        if (limbPitch > 0f)
                        {
                            float topPitch = limbPitch * TopPitchRatio;
                            float bottomPitch = limbPitch * BottomPitchRatio;

                            SetHeight(LayoutItem(rows[_breakdownSumIndex], entriesRoot),
                                Mathf.Max(topPitch - gap, MinLimbRowHeight), layoutSetsHeight);

                            for (int i = _breakdownSumIndex + 1; i <= lastLimb; i++)
                            {
                                float pitch = i == lastLimb && hasNext ? bottomPitch : limbPitch;
                                SetHeight(LayoutItem(rows[i], entriesRoot),
                                    Mathf.Max(pitch - gap, MinLimbRowHeight), layoutSetsHeight);
                            }

                            TraceOnce(ref _tracedLimbLayout,
                                $"limb rows: gap={gap}, layoutSetsHeight={layoutSetsHeight}, " +
                                $"pitch limb={limbPitch} top={topPitch} bottom={bottomPitch}");
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(entriesRoot);

                        if (__instance.TooltipRoot != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.TooltipRoot.transform as RectTransform);
                        }
                    }

                    for (int i = 0; i < rows.Count; i++)
                    {
                        bool isLimb = ours && i > _breakdownSumIndex && i <= _breakdownSumIndex + _breakdownCount;
                        bool isSum = ours && i == _breakdownSumIndex;

                        if (rows[i].Value != null && _defaultValueColor.HasValue)
                        {
                            rows[i].Value.color = isSum ? MutedValueColor : _defaultValueColor.Value;
                        }

                        SetBracket(
                            rows[i],
                            ours && i == _breakdownSumIndex + 1 && limbPitch > 0f,
                            _breakdownCount,
                            limbPitch);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>A limb row's pitch, in lines of its own text.</summary>
            private const float LimbRowLineHeight = 1.25f;
            /// <summary>The Acid-to-first-limb distance, as a multiple of the limb-to-limb one.</summary>
            private const float TopPitchRatio = 1.2f;
            /// <summary>
            /// The last-limb-to-next-status distance, as a multiple of the limb-to-limb one: wider
            /// than the top, so the breakdown closes off clearly before the next status.
            /// </summary>
            private const float BottomPitchRatio = 1.8f;
            private const float MinLimbRowHeight = 2f;

            private static bool _tracedLimbLayout;

            private struct OriginalHeight
            {
                internal float SizeY;
                internal bool HadElement;
                internal float MinHeight;
                internal float PreferredHeight;
            }

            private static readonly Dictionary<RectTransform, OriginalHeight> _originalHeights =
                new Dictionary<RectTransform, OriginalHeight>();

            /// <summary>
            /// The object the entries' layout group actually sizes: the row itself when it is a
            /// direct child of the entries root, otherwise the wrapper it sits in. Sizing the row
            /// inside a wrapper changes nothing the layout looks at.
            /// </summary>
            private static RectTransform LayoutItem(TextValueSlotController row, RectTransform entriesRoot)
            {
                Transform current = row.transform;

                while (current != null && current.parent != entriesRoot)
                {
                    current = current.parent;
                }

                return current as RectTransform;
            }

            /// <summary>
            /// Sets an item's height through a LayoutElement when the entries' layout group sizes its
            /// children, or through the rect when it does not, remembering the original first.
            /// </summary>
            private static void SetHeight(RectTransform item, float height, bool layoutSetsHeight)
            {
                if (item == null)
                {
                    return;
                }

                LayoutElement element = item.GetComponent<LayoutElement>();

                if (!_originalHeights.ContainsKey(item))
                {
                    _originalHeights[item] = new OriginalHeight
                    {
                        SizeY = item.sizeDelta.y,
                        HadElement = element != null,
                        MinHeight = element != null ? element.minHeight : -1f,
                        PreferredHeight = element != null ? element.preferredHeight : -1f,
                    };
                }

                if (layoutSetsHeight)
                {
                    if (element == null)
                    {
                        element = item.gameObject.AddComponent<LayoutElement>();
                    }

                    element.minHeight = height;
                    element.preferredHeight = height;
                }
                else
                {
                    item.sizeDelta = new Vector2(item.sizeDelta.x, height);
                }
            }

            private static void RestoreHeight(RectTransform item, bool layoutSetsHeight)
            {
                if (!_originalHeights.TryGetValue(item, out OriginalHeight original))
                {
                    return;
                }

                _originalHeights.Remove(item);

                // A destroyed row is still a dictionary key until it is removed here.
                if (item == null)
                {
                    return;
                }

                LayoutElement element = item.GetComponent<LayoutElement>();
                if (element != null)
                {
                    // An element this patch added goes back to -1, which lays out as if absent.
                    element.minHeight = original.HadElement ? original.MinHeight : -1f;
                    element.preferredHeight = original.HadElement ? original.PreferredHeight : -1f;
                }

                if (!layoutSetsHeight)
                {
                    item.sizeDelta = new Vector2(item.sizeDelta.x, original.SizeY);
                }
            }

            /// <summary>
            /// A single line beside the limb rows, from the top of the first limb's text to the
            /// bottom of the last one's. It hangs off the first limb row; every other row, and the
            /// first one when there is no breakdown, has its line hidden.
            ///
            /// It is anchored to the row's top edge and sized from the limb pitch rather than from
            /// the other rows' positions, which are stale while the tooltip is hidden. The title's
            /// offset from its own row's top is safe to read: it does not depend on the row's height.
            /// </summary>
            private static void SetBracket(TextValueSlotController row, bool show, int limbCount, float limbPitch)
            {
                RectTransform rowRect = row.transform as RectTransform;
                Transform existing = row.transform.Find(BracketObjectName);

                if (!show || rowRect == null || row.Title == null)
                {
                    if (existing != null)
                    {
                        existing.gameObject.SetActive(false);
                    }
                    return;
                }

                RectTransform line;
                if (existing == null)
                {
                    GameObject go = new GameObject(BracketObjectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    go.transform.SetParent(row.transform, false);
                    go.GetComponent<LayoutElement>().ignoreLayout = true;

                    Image image = go.GetComponent<Image>();
                    image.color = BracketColor;
                    image.raycastTarget = false;

                    line = go.GetComponent<RectTransform>();
                    line.pivot = new Vector2(0f, 1f);
                }
                else
                {
                    line = (RectTransform)existing;
                    line.gameObject.SetActive(true);
                }

                RectTransform title = row.Title.rectTransform;
                Vector3 titleCentre = rowRect.InverseTransformPoint(title.TransformPoint(title.rect.center));
                float x = rowRect.InverseTransformPoint(title.TransformPoint(new Vector3(title.rect.xMin, 0f, 0f))).x
                    - rowRect.rect.xMin;

                // Relative to the row's top edge.
                float centreFromTop = titleCentre.y - rowRect.rect.yMax;
                float halfText = row.Title.fontSize * 0.5f;
                float top = centreFromTop + halfText;
                float bottom = centreFromTop - (limbCount - 1) * limbPitch - halfText;

                line.anchorMin = new Vector2(0f, 1f);
                line.anchorMax = new Vector2(0f, 1f);
                line.offsetMin = new Vector2(x, bottom);
                line.offsetMax = new Vector2(x + BracketLineWidth, top);
            }
        }

        #endregion

        #region status card

        /// <summary>
        /// The status screen builds its CharacterData from the actor and then hands it to the module,
        /// which no longer knows who it belongs to. The actor is parked here across that call so the
        /// description can be composed for this character rather than read from the def.
        ///
        /// It is cleared in the postfix so a geoscape status screen - which goes through the same
        /// module with no tactical actor - can never pick up a stale one.
        /// </summary>
        [HarmonyPatch(typeof(UIStateCharacterStatus), "SetData")]
        internal static class UIStateCharacterStatus_SetData_TrackActor_Patch
        {
            internal static TacticalActor Current;

            private static void Prefix(TacticalActor character) => Current = character;

            private static void Postfix() => Current = null;
        }

        /// <summary>
        /// Replaces the acid card's stock description, which reports the decay rate as the corrosion
        /// rate ("corrodes 10 armor per turn") and so contradicts the mod's own Phoenixpedia entry.
        /// In its place: which limbs carry the acid, what happens to each of them next turn, and what
        /// the character's acid resistance is and is not doing.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleCharacterStatus), "SetData")]
        internal static class UIModuleCharacterStatus_SetData_AcidDescription_Patch
        {
            private static void Prefix(UIModuleCharacterStatus.CharacterData data)
            {
                try
                {
                    TacticalActor actor = UIStateCharacterStatus_SetData_TrackActor_Patch.Current;

                    if (actor == null || data?.Statuses == null)
                    {
                        return;
                    }

                    List<TFTVAcid.LimbAcid> limbs = TFTVAcid.GetLimbAcid(actor);
                    if (limbs.Count == 0)
                    {
                        return;
                    }

                    DamageOverTimeStatusDef acidDef = actor.Status
                        .GetStatuses<AcidStatus>()
                        .FirstOrDefault()?.DamageOverTimeStatusDef;

                    if (acidDef == null)
                    {
                        return;
                    }

                    string description = BuildCardDescription(actor, limbs);

                    TraceOnce(ref _tracedCard,
                        $"card: {description.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length} line(s), {description.Length} chars");

                    for (int i = 0; i < data.Statuses.Count; i++)
                    {
                        if (data.Statuses[i].Icon != acidDef.Visuals.SmallIcon)
                        {
                            continue;
                        }

                        UIModuleCharacterStatus.CharacterData.StatusData status = data.Statuses[i];
                        status.Description = new LocalizedTextBind(
                            description,
                            TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
                        data.Statuses[i] = status;
                        return;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        #region status-screen chip tooltip

        /// <summary>
        /// The status chips on the character status screen have hover tooltips that say nothing but
        /// the status name. They are the natural home for a next-turn forecast: the status cards are
        /// a fixed height with best-fit text, so every line there shrinks the whole card, whereas a
        /// tooltip is sized to its content and only appears when asked for. The forecast is drawn
        /// by StatusForecastTooltip in place of the vanilla one.
        ///
        /// The same row controller also draws the little droplets on the body-part rows, which are
        /// already per-limb and must not be given a whole-character forecast. Those live under a
        /// CharacterStatusBodyPartRowController, so the parent chain tells the two apart.
        /// </summary>
        [HarmonyPatch(typeof(CharacterStatusStatusRowController), "SetData")]
        internal static class CharacterStatusStatusRowController_SetData_Forecast_Patch
        {
            private static void Postfix(
                CharacterStatusStatusRowController __instance,
                UIModuleCharacterStatus.CharacterData.StatusData status)
            {
                try
                {
                    if (__instance == null || __instance.Tooltip == null)
                    {
                        return;
                    }

                    // The controllers are pooled, so a chip that showed a forecast last time may now
                    // hold a status without one: start every call from the vanilla tooltip.
                    StatusForecastTrigger trigger = __instance.Tooltip.GetComponent<StatusForecastTrigger>();
                    if (trigger != null)
                    {
                        trigger.Forecast = null;
                        trigger.enabled = false;
                    }

                    __instance.Tooltip.enabled = true;

                    TacticalActor actor = UIStateCharacterStatus_SetData_TrackActor_Patch.Current;

                    if (actor == null)
                    {
                        return;
                    }

                    // A droplet on a body-part row: already per-limb, leave it alone.
                    if (__instance.GetComponentInParent<CharacterStatusBodyPartRowController>() != null)
                    {
                        return;
                    }

                    TacStatusDef statusDef = actor.Status?.Statuses?
                        .OfType<TacStatus>()
                        .Select(st => st.TacStatusDef)
                        // Visuals is optional on a status def - only ones shown somewhere have it -
                        // so it has to be checked before the icon is compared.
                        .FirstOrDefault(def => def != null && def.Visuals != null && def.Visuals.SmallIcon == status.Icon);

                    if (statusDef == null)
                    {
                        return;
                    }

                    StatusForecast.Forecast forecast = StatusForecast.Build(actor, statusDef, status.Value);
                    if (forecast == null)
                    {
                        return;
                    }

                    // The vanilla tooltip is one Text and cannot draw the icon or the value column,
                    // so it steps aside for the forecast panel. A disabled behaviour receives no
                    // pointer events, so the two never show together.
                    if (trigger == null)
                    {
                        trigger = __instance.Tooltip.gameObject.AddComponent<StatusForecastTrigger>();
                    }

                    trigger.Forecast = forecast;
                    trigger.Font = __instance.Value != null ? __instance.Value.font : null;
                    trigger.enabled = true;
                    __instance.Tooltip.enabled = false;

                    TraceOnce(ref _tracedChipTooltip,
                        $"chip tooltip: {forecast.Title}, {forecast.Rows.Count} row(s)");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion
    }
}
