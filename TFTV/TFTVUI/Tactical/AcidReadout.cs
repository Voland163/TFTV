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

        private static string LimbBreakdown(List<TFTVAcid.LimbAcid> limbs)
        {
            return string.Join(
                " · ",
                limbs.Select(limb => $"{limb.DisplayName} {Mathf.RoundToInt(limb.Acid)}").ToArray());
        }

        /// <summary>
        /// The next-turn outcome, all limbs on one line.
        ///
        /// This lives in the chip's hover tooltip rather than the status card: the card is a fixed
        /// height with best-fit text, so every line there shrinks the whole card, while a tooltip is
        /// sized to its content. Only next turn is projected - turns beyond it depend on the decay
        /// rate, which the Acheron workshop module changes for bionics and vehicles.
        /// </summary>
        private static string Forecast(List<TFTVAcid.LimbAcid> limbs)
        {
            string[] lines = new[] { TFTVCommonMethods.ConvertKeyToString("TFTV_ACID_NEXT_TURN") }
                .Concat(limbs.Select(limb => limb.WillCostHealth
                    ? TFTVCommonMethods.FormatKey(
                        "TFTV_ACID_FORECAST_HEALTH",
                        limb.DisplayName,
                        Mathf.RoundToInt(limb.Acid),
                        Mathf.RoundToInt(limb.AcidAfter),
                        Mathf.RoundToInt(limb.HealthDamage))
                    : TFTVCommonMethods.FormatKey(
                        "TFTV_ACID_FORECAST_ARMOUR",
                        limb.DisplayName,
                        Mathf.RoundToInt(limb.Acid),
                        Mathf.RoundToInt(limb.AcidAfter),
                        Mathf.RoundToInt(limb.Armour),
                        Mathf.RoundToInt(limb.ArmourAfter))))
                .ToArray();

            return string.Join(Environment.NewLine, lines);
        }

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
            builder.AppendLine(TFTVCommonMethods.FormatKey(
                "TFTV_ACID_CARD_DECAY",
                Mathf.RoundToInt(TFTVAcid.GetAcidDecayPerTurn(actor))));

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
        /// as three more statuses.
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

                // The sum comes off the Acid row: with the limbs listed underneath, it was the only
                // number on screen corresponding to nothing the game actually tracks.
                ShortActorInfoTooltipDataEntry acidRow = entries[entries.Count - 1];
                acidRow.ValueContent = string.Empty;
                entries[entries.Count - 1] = acidRow;

                foreach (TFTVAcid.LimbAcid limb in limbs)
                {
                    entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        TextContent = $"- {limb.DisplayName}",
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
        /// tooltip is sized to its content and only appears when asked for.
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
                    TacticalActor actor = UIStateCharacterStatus_SetData_TrackActor_Patch.Current;

                    if (actor == null || __instance == null || __instance.Tooltip == null)
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

                    string text = StatusForecast.Build(actor, statusDef, status.Value);
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }

                    // TipKey wins over TipText whenever it carries a key, so it has to be cleared;
                    // and the default 140px would wrap the forecast into a column.
                    __instance.Tooltip.TipKey = null;
                    __instance.Tooltip.TipText = text;
                    __instance.Tooltip.MaxWidth = Mathf.Max(__instance.Tooltip.MaxWidth, 340);
                    __instance.Tooltip.UpdateText(text);

                    TraceOnce(ref _tracedChipTooltip,
                        $"chip tooltip: {text.Replace(Environment.NewLine, " | ")}");
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
