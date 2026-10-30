using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVHarshAttrition
    {
        /// <summary>
        /// Every Pandoran rolls its toughness when it enters play: max HP (through Endurance - Health's maximum is
        /// recalculated from it on every UpdateStats, which wiped a roll put on Health itself), and each body part's
        /// max HP and armour, each an independent roll of 60-90% or 110-140%; Perception and Speed -3..+3; Willpower -5..+5.
        /// The rolls are modifications on the stats with the status def as source, so they are saved with the stats
        /// and never re-rolled.
        ///
        /// What the player sees is unlocked piece by piece (see <see cref="Insight"/>):
        /// - neither researched - "UNKNOWN SPECIMEN", no health bar and no stats, only the damage it has taken;
        /// - autopsy - Hit Points of templates, and HP and armour of body parts, killed or captured in an earlier mission;
        /// - vivisection - Will Points, movement, Perception, Stealth, Accuracy, abilities and its own statuses
        ///   (a species with no vivisection gets these from its autopsy);
        /// - study (a player unit got within half its vision range of this individual, as adjusted for the target's
        ///   size and stealth) - what is unlocked shows this individual's rolled values instead of the template's ("N?").
        /// Floating damage numbers and the status icons stay at every tier.
        /// </summary>
        internal static class PandoranVariance
        {
            internal static PandoranVarianceStatusDef StatusDef { get; set; }

            internal const float MinRoll = 0.6f;
            internal const float MaxRoll = 1.4f;
            /// <summary>The smallest a percentage roll moves a value, either way.</summary>
            internal const float MinDeviation = 0.1f;
            /// <summary>Rolled max HP moves in steps of this many points.</summary>
            internal const float HealthStep = 10f;
            /// <summary>Studied within this share of the vision range between the two actors.</summary>
            internal const float StudyRangeShare = 0.5f;

            private const string UnknownNameKey = "TFTV_UNKNOWN_SPECIMEN";
            private const string PlayerFactionCommandName = "px";

            /// <summary>What the player knows about one creature. Everything is known about anything that is not an enemy Pandoran.</summary>
            internal sealed class Insight
            {
                internal static readonly Insight Everything = new Insight(true, true, true, true, true, null);

                /// <summary>Autopsied or vivisected: its name and its body parts' names.</summary>
                internal readonly bool Identified;
                /// <summary>Autopsied: body parts killed or captured in an earlier mission give their HP and armour.</summary>
                internal readonly bool Autopsied;
                /// <summary>Autopsied, and this template killed or captured in an earlier mission: Hit Points, and every part.</summary>
                internal readonly bool Health;
                /// <summary>Vivisected: Will Points, movement, Perception, Stealth, Accuracy, abilities and its own statuses.</summary>
                internal readonly bool Stats;
                /// <summary>A soldier studied this individual up close: what is unlocked shows its own values, not the template's.</summary>
                internal readonly bool Studied;

                private readonly TacticalActor _actor;

                internal Insight(bool identified, bool autopsied, bool health, bool stats, bool studied, TacticalActor actor)
                {
                    Identified = identified;
                    Autopsied = autopsied;
                    Health = health;
                    Stats = stats;
                    Studied = studied;
                    _actor = actor;
                }

                /// <summary>Something is held back: anything else shows as vanilla.</summary>
                internal bool Gated => !(Health && Stats && Studied);

                /// <summary>Its real Hit Points (and so damage predictions and lethal-shot warnings).</summary>
                internal bool RealHealth => Health && Studied;

                /// <summary>The body part in this slot gives its HP and armour (every item in it met before).</summary>
                internal bool KnowsPart(ItemSlot slot)
                {
                    if (Health || slot == null)
                    {
                        return Health;
                    }

                    return Autopsied && slot.GetAllDirectItems(false).All(item => item?.TacticalItemDef == null || EncounteredParts.Contains(item.TacticalItemDef.name));
                }

                /// <summary>
                /// The same for the slot whose health stat this is (a free-aim bar shows one part); false when none matches.
                /// A known template knows every part - and <see cref="Everything"/> (humans, allies, the option off) has no
                /// actor to look the slot up on, so it must answer before the lookup.
                /// </summary>
                internal bool KnowsPartWithHealth(StatusStat health)
                {
                    if (Health)
                    {
                        return true;
                    }

                    ItemSlot slot = FindSlot(_actor, health);
                    return slot != null && KnowsPart(slot);
                }
            }

            private sealed class Species
            {
                internal readonly string ClassTag;
                internal readonly string Autopsy;
                /// <summary>Null for a species that cannot be vivisected (eggs, sentinels, nodes): its autopsy unlocks everything.</summary>
                internal readonly string Vivisection;

                internal Species(string classTag, string autopsy, string vivisection = null)
                {
                    ClassTag = classTag;
                    Autopsy = autopsy;
                    Vivisection = vivisection;
                }
            }

            /// <summary>
            /// Pandoran class tags with their autopsy and vivisection (live capture) researches. Eggs first: an actor
            /// takes the first entry whose tag it has.
            /// </summary>
            private static readonly Species[] AllSpecies =
            {
                new Species("Egg_Facehugger_ClassTagDef", "PX_Alien_MindfraggerEgg_ResearchDef"),
                new Species("Egg_Swarmer_ClassTagDef", "PX_Alien_SwarmerEgg_ResearchDef"),
                new Species("Egg_Acidworm_ClassTagDef", "PX_Alien_WormEgg_ResearchDef"),
                new Species("Egg_Poisonworm_ClassTagDef", "PX_Alien_WormEgg_ResearchDef"),
                new Species("Egg_Fireworm_ClassTagDef", "PX_Alien_WormEgg_ResearchDef"),
                new Species("Crabman_ClassTagDef", "PX_AlienCrabman_ResearchDef", "PX_Alien_LiveCrabman_ResearchDef"),
                new Species("SpawningPoolCrabman_ClassTagDef", "PX_Alien_Spawnery_ResearchDef"),
                new Species("Fishman_ClassTagDef", "PX_Alien_Fishman_ResearchDef", "PX_Alien_LiveFishman_ResearchDef"),
                new Species("Siren_ClassTagDef", "PX_Alien_Siren_ResearchDef", "PX_Alien_LiveSiren_ResearchDef"),
                new Species("Chiron_ClassTagDef", "PX_Alien_Chiron_ResearchDef", "PX_Alien_LiveChiron_ResearchDef"),
                new Species("Acheron_ClassTagDef", "PX_Alien_Acheron_ResearchDef", "PX_Alien_LiveAcheron_ResearchDef"),
                new Species("Queen_ClassTagDef", "PX_Alien_Queen_ResearchDef", "PX_Alien_LiveQueen_ResearchDef"),
                new Species("Swarmer_ClassTagDef", "PX_Alien_Swarmer_ResearchDef", "PX_Alien_LiveSwarmer_ResearchDef"),
                new Species("Facehugger_ClassTagDef", "PX_Alien_Mindfragger_ResearchDef", "PX_Alien_LiveMindfragger_ResearchDef"),
                new Species("Acidworm_ClassTagDef", "PX_Alien_Acidworm_ResearchDef", "PX_Alien_LiveAcidworm_ResearchDef"),
                new Species("Poisonworm_ClassTagDef", "PX_Alien_Poisonworm_ResearchDef", "PX_Alien_LivePoisonworm_ResearchDef"),
                new Species("Fireworm_ClassTagDef", "PX_Alien_Fireworm_ResearchDef", "PX_Alien_LiveFireworm_ResearchDef"),
                new Species("SentinelHatching_ClassTagDef", "PX_Alien_HatchingSentinel_ResearchDef"),
                new Species("SentinelMist_ClassTagDef", "PX_Alien_MistSentinel_ResearchDef"),
                new Species("SentinelTerror_ClassTagDef", "PX_Alien_TerrorSentinel_ResearchDef"),
                new Species("CorruptionNode_ClassTagDef", "PX_Alien_CorruptionNode_ResearchDef"),
            };

            /// <summary>
            /// Class tags whose autopsy the player has completed. Taken from the geoscape when it ends (tactical
            /// has no research), and saved with the tactical data for missions loaded from a tactical save.
            /// </summary>
            internal static List<string> AutopsiedClassTags = new List<string>();

            /// <summary>Class tags vivisected (or, for a species with no vivisection, autopsied). Kept like <see cref="AutopsiedClassTags"/>.</summary>
            internal static List<string> VivisectedClassTags = new List<string>();

            /// <summary>
            /// Templates (<see cref="ComputeTemplate"/>) and body-part item defs killed or captured in earlier missions.
            /// Saved with the campaign; a mission reads them as they stood when it started.
            /// </summary>
            internal static HashSet<string> EncounteredTemplates = new HashSet<string>();
            internal static HashSet<string> EncounteredParts = new HashSet<string>();

            /// <summary>
            /// Met for the first time this mission: they count from the next one. Carried back to the geoscape in memory
            /// (its own data is the pre-mission snapshot) and merged there; saved with the tactical data.
            /// </summary>
            internal static HashSet<string> NewTemplates = new HashSet<string>();
            internal static HashSet<string> NewParts = new HashSet<string>();

            private const char TemplateSeparator = '|';

            private static List<KeyValuePair<ClassTagDef, string>> _speciesTags;

            #region species and knowledge

            internal static void RecordAutopsies(GeoLevelController controller)
            {
                try
                {
                    List<string> autopsied = new List<string>();
                    List<string> vivisected = new List<string>();

                    if (controller?.PhoenixFaction?.Research == null)
                    {
                        return;
                    }

                    foreach (Species species in AllSpecies)
                    {
                        bool autopsy = controller.PhoenixFaction.Research.HasCompleted(species.Autopsy);

                        if (autopsy)
                        {
                            autopsied.Add(species.ClassTag);
                        }

                        if (species.Vivisection == null ? autopsy : controller.PhoenixFaction.Research.HasCompleted(species.Vivisection))
                        {
                            vivisected.Add(species.ClassTag);
                        }
                    }

                    AutopsiedClassTags = autopsied;
                    VivisectedClassTags = vivisected;
                    TFTVLogger.Always($"[HarshAttrition] Autopsied species: {string.Join(", ", autopsied)}; vivisected: {string.Join(", ", vivisected)};"
                        + $" {EncounteredTemplates.Count} templates and {EncounteredParts.Count} body parts killed or captured");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Back on the geoscape: the saved encounters (the pre-mission snapshot, or the loaded save's) plus whatever the
            /// mission just played killed or captured.
            /// </summary>
            internal static void RestoreEncounters(List<string> templates, List<string> parts)
            {
                EncounteredTemplates = new HashSet<string>(templates ?? new List<string>());
                EncounteredParts = new HashSet<string>(parts ?? new List<string>());

                int newTemplates = NewTemplates.Count(template => EncounteredTemplates.Add(template));
                int newParts = NewParts.Count(part => EncounteredParts.Add(part));

                if (newTemplates > 0 || newParts > 0)
                {
                    TFTVLogger.Always($"[HarshAttrition] Last mission added {newTemplates} templates and {newParts} body parts");
                }

                ClearNewEncounters();
            }

            /// <summary>On loading a save or restarting a mission: what an abandoned mission met does not count.</summary>
            internal static void ClearNewEncounters()
            {
                NewTemplates.Clear();
                NewParts.Clear();
            }

            /// <summary>A new campaign starts knowing nothing.</summary>
            internal static void ClearCampaignKnowledge()
            {
                AutopsiedClassTags = new List<string>();
                VivisectedClassTags = new List<string>();
                EncounteredTemplates = new HashSet<string>();
                EncounteredParts = new HashSet<string>();
                ClearNewEncounters();
            }

            private static List<KeyValuePair<ClassTagDef, string>> SpeciesTags
            {
                get
                {
                    if (_speciesTags == null)
                    {
                        _speciesTags = new List<KeyValuePair<ClassTagDef, string>>();

                        foreach (Species species in AllSpecies)
                        {
                            ClassTagDef tag = TFTVMain.Main.DefCache.GetDef<ClassTagDef>(species.ClassTag);

                            if (tag != null)
                            {
                                _speciesTags.Add(new KeyValuePair<ClassTagDef, string>(tag, species.ClassTag));
                            }
                        }
                    }

                    return _speciesTags;
                }
            }

            /// <summary>The Pandoran species (its class tag's name), or null for anything else.</summary>
            private static string GetSpecies(TacticalActorBase actor)
            {
                if (actor?.GameTags == null)
                {
                    return null;
                }

                foreach (KeyValuePair<ClassTagDef, string> pair in SpeciesTags)
                {
                    if (actor.GameTags.Contains(pair.Key))
                    {
                        return pair.Value;
                    }
                }

                return null;
            }

            /// <summary>Everything for anything that is not a Pandoran, for Pandorans the player controls, and with the option off.</summary>
            internal static Insight GetInsight(TacticalActorBase actorBase)
            {
                if (!IsEnabled || !(actorBase is TacticalActor actor))
                {
                    return Insight.Everything;
                }

                string species = GetSpecies(actor);

                if (species == null || actor.IsControlledByPlayer)
                {
                    return Insight.Everything;
                }

                bool autopsied = AutopsiedClassTags != null && AutopsiedClassTags.Contains(species);
                bool vivisected = VivisectedClassTags != null && VivisectedClassTags.Contains(species);
                PandoranVarianceStatus status = actor.Status?.GetStatus<PandoranVarianceStatus>();
                bool health = autopsied && EncounteredTemplates.Contains(GetTemplate(actor, status));

                return new Insight(autopsied || vivisected, autopsied, health, vivisected, status != null && status.Studied, actor);
            }

            /// <summary>
            /// What a "template" is: the species and the body-part item defs it spawned with, sorted - so the same
            /// mutations make the same template whichever spawn path brought it in, and its Hit Points follow from them.
            /// </summary>
            private static string ComputeTemplate(TacticalActor actor)
            {
                IEnumerable<string> parts = (actor.BodyState?.GetHealthSlots() ?? Enumerable.Empty<ItemSlot>())
                    .SelectMany(slot => slot.GetAllDirectItems(false))
                    .Where(item => item?.TacticalItemDef != null)
                    .Select(item => item.TacticalItemDef.name)
                    .Distinct()
                    .OrderBy(name => name, StringComparer.Ordinal);

                return GetSpecies(actor) + TemplateSeparator + string.Join(TemplateSeparator.ToString(), parts);
            }

            /// <summary>The template taken when the creature was rolled; worked out (and kept) for one rolled before templates were.</summary>
            private static string GetTemplate(TacticalActor actor, PandoranVarianceStatus status)
            {
                if (!string.IsNullOrEmpty(status?.Template))
                {
                    return status.Template;
                }

                string template = ComputeTemplate(actor);

                if (status != null)
                {
                    status.Template = template;
                }

                return template;
            }

            private static ItemSlot FindSlot(TacticalActor actor, StatusStat health)
            {
                return health == null ? null : actor?.BodyState?.GetHealthSlots().FirstOrDefault(slot => slot.GetHealth() == health);
            }

            #endregion

            #region encounters

            /// <summary>A Pandoran killed or captured: its template and body parts count from the next mission.</summary>
            private static void RecordEncounter(TacticalActorBase actorBase, string how)
            {
                if (!IsEnabled || !(actorBase is TacticalActor actor) || GetSpecies(actor) == null)
                {
                    return;
                }

                string template = GetTemplate(actor, actor.Status?.GetStatus<PandoranVarianceStatus>());
                bool newTemplate = !EncounteredTemplates.Contains(template) && NewTemplates.Add(template);
                int newParts = template.Split(TemplateSeparator).Skip(1).Count(part => !EncounteredParts.Contains(part) && NewParts.Add(part));

                if (newTemplate || newParts > 0)
                {
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} {how}: {(newTemplate ? "new template" : "known template")}, {newParts} new body parts");
                }
            }

            /// <summary>
            /// At the end of a won mission, every capturable Pandoran still paralysed is taken (GeoMission.CaptureLiveAlien
            /// decides later whether containment has room; subdued counts either way).
            /// </summary>
            internal static void RecordCaptures(TacticalLevelController level)
            {
                try
                {
                    TacticalFaction player = level?.GetFactionByCommandName(PlayerFactionCommandName);

                    if (!IsEnabled || level?.Map == null || player == null || player.State != TacFactionState.Won)
                    {
                        return;
                    }

                    GameTagDef capturable = PhoenixPoint.Common.Core.CommonHelpers.GetSharedGameTags()?.CapturableTag;

                    foreach (TacticalActor actor in level.Map.GetActors<TacticalActor>(null).ToList())
                    {
                        if (actor != null && actor.IsAlive && actor.TacticalFaction != player
                            && actor.Status?.HasStatus<ParalysedStatus>() == true
                            && (capturable == null || actor.GameTags.Contains(capturable)))
                        {
                            RecordEncounter(actor, "captured");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            #endregion

            #region rolls

            private static void OnEnteredPlay(TacticalActorBase actorBase)
            {
                if (!IsEnabled || StatusDef == null || !(actorBase is TacticalActor actor) || actor.Status == null || actor.BodyState == null)
                {
                    return;
                }

                if (GetSpecies(actor) == null || actor.TacticalFaction?.Faction?.FactionDef == null)
                {
                    return;
                }

                // Pandorans that start on the player's side are left as they are.
                if (actor.TacticalFaction == actor.TacticalLevel?.GetFactionByCommandName(PlayerFactionCommandName))
                {
                    return;
                }

                // Already rolled: a loaded actor brings both the status and the modifications back. The status alone
                // is not enough - the study check or a hit on the frame it spawned can put it on first (Crabman_8 went
                // unrolled that way). Saves from before the Rolled flag are told apart by the modifications.
                PandoranVarianceStatus existing = actor.Status.GetStatus<PandoranVarianceStatus>();

                if (existing != null && existing.Rolled || HasRollModifications(actor))
                {
                    return;
                }

                float healthRoll = NextRoll();
                HashSet<TacticalItem> rolledItems = new HashSet<TacticalItem>();
                List<float> partHealthRolls = new List<float>();
                List<float> partArmourRolls = new List<float>();
                int perceptionRoll;
                int speedRoll;
                int willpowerRoll;
                StatusStat health = actor.CharacterStats?.Health;
                StatusStat willPoints = actor.CharacterStats?.WillPoints;
                float healthShare = Fullness(health);
                float willPointsShare = Fullness(willPoints);
                float healthMaxBefore = health == null ? 0f : (float)health.Max;

                // While the new maximums go on, Health can dip for a moment (and a low roll lowers it for good). Hurt
                // reactions listen to that - the Triton camo, eggs starting to hatch, mist - and the new armour and
                // part values float up as numbers over a creature already on the map (reinforcements), so both are
                // muted until the roll is done.
                _rolling = true;

                try
                {
                    PandoranVarianceStatus status = existing ?? actor.Status.ApplyStatus(StatusDef) as PandoranVarianceStatus;

                    if (status != null)
                    {
                        status.Template = ComputeTemplate(actor);
                    }

                    ApplyHealthRoll(actor, healthRoll);

                    foreach (ItemSlot slot in actor.BodyState.GetHealthSlots().ToList())
                    {
                        float partHealthRoll = NextRoll();
                        float partArmourRoll = NextRoll();
                        bool rolledPart = false;

                        foreach (TacticalItem item in slot.GetAllDirectItems(false))
                        {
                            // An item spanning several slots is rolled once.
                            if (item == null || !rolledItems.Add(item))
                            {
                                continue;
                            }

                            ApplyRoll(item.GetHealth(), partHealthRoll);
                            ApplyRoll(item.GetArmor(), partArmourRoll);
                            rolledPart = true;
                        }

                        if (rolledPart)
                        {
                            partHealthRolls.Add(partHealthRoll);
                            partArmourRolls.Add(partArmourRoll);
                        }
                    }

                    perceptionRoll = RollFlat(actor, PerceptionStatName, MaxFlatRoll);
                    speedRoll = RollFlat(actor, SpeedStatName, MaxFlatRoll);
                    willpowerRoll = RollFlat(actor, WillpowerStatName, MaxWillpowerRoll);

                    // New maximums for Health (from Endurance) and Will Points (from Willpower). The current values
                    // are only clamped, so a raised maximum would leave the creature short: it keeps the share it had.
                    actor.UpdateStats();
                    Refill(health, healthShare);
                    Refill(willPoints, willPointsShare);

                    if (status != null)
                    {
                        status.Rolled = true;
                    }
                }
                finally
                {
                    _rolling = false;
                }

                TFTVLogger.Always($"[HarshAttrition] Rolled {actor.name}: HP x{healthRoll:0.00}, {rolledItems.Count} body parts"
                    + $" (part HP {DescribeRolls(partHealthRolls)}, armour {DescribeRolls(partArmourRolls)}),"
                    + $" Perception {perceptionRoll:+0;-0;0}, Speed {speedRoll:+0;-0;0}, Willpower {willpowerRoll:+0;-0;0};"
                    + $" now HP {Describe(health)} (template max {healthMaxBefore:0}), WP {Describe(willPoints)}");
            }

            private static string Describe(StatusStat stat)
            {
                return stat == null ? "-" : $"{(float)stat:0}/{(float)stat.Max:0}";
            }

            /// <summary>How full the stat is, 0 to 1 (1 when it has no maximum).</summary>
            private static float Fullness(StatusStat stat)
            {
                float max = stat == null ? 0f : (float)stat.Max;
                return max > 0f ? Mathf.Clamp01((float)stat / max) : 1f;
            }

            private static void Refill(StatusStat stat, float share)
            {
                if (stat != null)
                {
                    stat.Set(Mathf.Round(share * (float)stat.Max));
                }
            }

            /// <summary>"x0.72-1.31, mean 1.04", for checking the spread in the log.</summary>
            private static string DescribeRolls(List<float> rolls)
            {
                return rolls.Count == 0 ? "none" : $"x{rolls.Min():0.00}-{rolls.Max():0.00}, mean {rolls.Average():0.00}";
            }

            /// <summary>Any modification the roll puts on: Health, a body part's health or armour, Perception or Speed.</summary>
            private static bool HasRollModifications(TacticalActor actor)
            {
                // Health: the roll lived there before it moved to Endurance.
                if (OwnDelta(actor.CharacterStats?.Endurance) != 0f || OwnDelta(actor.CharacterStats?.Health) != 0f
                    || FlatRoll(actor, PerceptionStatName) != 0f || FlatRoll(actor, SpeedStatName) != 0f || FlatRoll(actor, WillpowerStatName) != 0f)
                {
                    return true;
                }

                foreach (ItemSlot slot in actor.BodyState.GetHealthSlots())
                {
                    foreach (TacticalItem item in slot.GetAllDirectItems(false))
                    {
                        if (item != null && (OwnDelta(item.GetHealth()) != 0f || OwnDelta(item.GetArmor()) != 0f))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Set while a creature's stats are being rolled: its hurt reactions must not take that for damage, and
            /// the changes must not float up as numbers.
            /// </summary>
            private static bool _rolling;

            /// <summary>
            /// TacticalHurtReactionAbility listens to Health's StatChangeEvent and registers a reaction (Triton camo,
            /// eggs' StartPreparing, SpawnMist) on any drop while the creature is in play. The rolls run right after it
            /// enters play, so without this every rolled Triton vanished and every rolled egg started to hatch.
            /// </summary>
            [HarmonyPatch(typeof(PhoenixPoint.Tactical.Entities.Abilities.TacticalHurtReactionAbility), "OnActorDamaged")]
            internal static class TacticalHurtReactionAbility_OnActorDamaged_PandoranVariance_Patch
            {
                private static bool Prefix()
                {
                    return !_rolling;
                }
            }

            internal const int MaxFlatRoll = 3;
            internal const int MaxWillpowerRoll = 5;
            private const string PerceptionStatName = "Perception";
            private const string SpeedStatName = "Speed";
            private const string WillpowerStatName = "Willpower";

            /// <summary>
            /// A whole-number roll from -range to +range added to the stat, sourced from the status def like the HP rolls.
            /// A stat at 0 (an egg's or a sentinel's Speed) is left alone, and no roll takes a stat below 1.
            /// </summary>
            private static int RollFlat(TacticalActor actor, string statName, int range)
            {
                BaseStat stat = actor.Status.GetStat(statName);

                if (stat == null)
                {
                    return 0;
                }

                float current = stat;
                int roll = Rolls.Next(-range, range + 1);

                if (current <= 0f || roll == 0)
                {
                    return 0;
                }

                roll = Mathf.Max(roll, 1 - Mathf.RoundToInt(current));

                if (roll == 0)
                {
                    return 0;
                }

                // A stat with a maximum (Willpower, Speed) is often put back to that maximum (Set(Max)), which cancels
                // an Add on its value: the roll goes on the maximum, and the value follows it.
                if (stat is StatusStat statusStat && (float)statusStat.Max > 0f && !float.IsInfinity((float)statusStat.Max))
                {
                    statusStat.AddStatModification(new StatModification(StatModificationType.AddMax, statName, roll, StatusDef, roll), false);
                    statusStat.ReapplyModifications();
                    statusStat.Set(current + roll);
                    return roll;
                }

                stat.AddStatModification(new StatModification(StatModificationType.Add, statName, roll, StatusDef, roll), false);
                stat.ReapplyModifications();
                return roll;
            }

            /// <summary>The flat roll on this stat (0 when none): the species' base value is the stat less this.</summary>
            private static float FlatRoll(TacticalActor actor, string statName)
            {
                BaseStat stat = actor?.Status?.GetStat(statName);

                if (stat?.Modifications == null || StatusDef == null)
                {
                    return 0f;
                }

                float total = 0f;

                foreach (StatModification modification in stat.Modifications)
                {
                    if (modification.Source == (object)StatusDef)
                    {
                        total += modification.Value;
                    }
                }

                return total;
            }

            /// <summary>
            /// Its own generator: Unity's is seeded the same way for a reloaded mission, so the rolls repeated on every
            /// reload, and drawing from it also shifted every other random outcome the game takes from it afterwards.
            /// </summary>
            private static readonly System.Random Rolls = new System.Random(Environment.TickCount);

            /// <summary>
            /// 60-90% or 110-140%, either side equally likely and uniform within it: never within 10% of the template,
            /// so every individual is noticeably weaker or tougher where it was rolled.
            /// </summary>
            private static float NextRoll()
            {
                float deviation = MinDeviation + (float)Rolls.NextDouble() * (MaxRoll - 1f - MinDeviation);
                return Rolls.Next(2) == 0 ? 1f - deviation : 1f + deviation;
            }

            #endregion

            #region damage taken

            /// <summary>All the Hit Points an unknown specimen has lost so far: the one thing about its health the player sees.</summary>
            internal static float GetDamageTaken(TacticalActorBase actor)
            {
                return (actor as TacticalActor)?.Status?.GetStatus<PandoranVarianceStatus>()?.DamageTaken ?? 0f;
            }

            /// <summary>Counts what TacticalActorBase.ApplyDamage took off the creature's Hit Points, whatever dealt it.</summary>
            private static void CountDamage(TacticalActorBase actorBase, float healthBefore)
            {
                if (!IsEnabled || !(actorBase is TacticalActor actor) || StatusDef == null || actor.Status == null || GetSpecies(actor) == null)
                {
                    return;
                }

                float lost = healthBefore - (float)actor.CharacterStats.Health;

                if (lost <= 0f)
                {
                    return;
                }

                PandoranVarianceStatus status = actor.Status.GetStatus<PandoranVarianceStatus>()
                    ?? actor.Status.ApplyStatus(StatusDef) as PandoranVarianceStatus;

                if (status != null)
                {
                    status.DamageTaken += lost;
                }
            }

            #endregion

            #region roll helpers

            /// <summary>
            /// Max HP is Endurance x EnduranceToHealthMultiplier, so it is rolled through Endurance. The rolled max HP is
            /// rounded to a multiple of 10, and a roll never rounds away to nothing: it moves at least one step of 10 its
            /// way. Endurance takes fractions (Add/AddMax values are not rounded), so this holds whatever the multiplier.
            /// </summary>
            private static void ApplyHealthRoll(TacticalActor actor, float roll)
            {
                StatusStat endurance = actor.CharacterStats?.Endurance;
                StatusStat health = actor.CharacterStats?.Health;
                float multiplier = actor.TacticalActorDef?.EnduranceToHealthMultiplier ?? 0f;
                float enduranceMax = endurance == null ? 0f : (float)endurance.Max;

                if (endurance == null || health == null || multiplier <= 0f || enduranceMax <= 0f || float.IsInfinity(enduranceMax))
                {
                    ApplyRoll(endurance, roll);
                    return;
                }

                float healthMax = (float)health.Max;
                float target = Mathf.Round(healthMax * roll / HealthStep) * HealthStep;

                if (Mathf.Abs(target - healthMax) < HealthStep)
                {
                    target = healthMax + (roll < 1f ? -HealthStep : HealthStep);
                }

                target = Mathf.Max(HealthStep, target);
                float delta = (target - healthMax) / multiplier;

                if (Mathf.Abs(delta) < 0.001f)
                {
                    return;
                }

                float share = Fullness(endurance);
                endurance.AddStatModification(new StatModification(StatModificationType.AddMax, endurance.Name, delta, StatusDef, delta), false);
                endurance.ReapplyModifications();
                endurance.Set(share * (float)endurance.Max);
            }

            /// <summary>Scales the stat's maximum by the roll, keeping it as full (or as hurt) as it was.</summary>
            private static void ApplyRoll(StatusStat stat, float roll)
            {
                if (stat == null)
                {
                    return;
                }

                float max = (float)stat.Max;

                if (max <= 0f || float.IsInfinity(max))
                {
                    return;
                }

                float delta = Mathf.Round(max * (roll - 1f));

                if (Mathf.Abs(delta) < 1f)
                {
                    return;
                }

                float share = Mathf.Clamp01((float)stat / max);

                stat.AddStatModification(new StatModification(StatModificationType.AddMax, stat.Name, delta, StatusDef, delta), false);
                stat.ReapplyModifications();
                stat.Set(Mathf.Round(share * (float)stat.Max));
            }

            /// <summary>What the roll added to (or took from) this stat's maximum.</summary>
            private static float OwnDelta(StatusStat stat)
            {
                if (stat?.Modifications == null || StatusDef == null)
                {
                    return 0f;
                }

                float total = 0f;

                foreach (StatModification modification in stat.Modifications)
                {
                    if (modification.Source == (object)StatusDef)
                    {
                        total += modification.Value;
                    }
                }

                return total;
            }

            /// <summary>The species' base value as a share of this individual's rolled one (1 when not rolled).</summary>
            private static float BaseShare(StatusStat stat)
            {
                float max = stat == null ? 0f : (float)stat.Max;
                return max > 0f ? Mathf.Max(0f, max - OwnDelta(stat)) / max : 1f;
            }

            /// <summary>The same over all the body parts' health or armour together - near enough for a "~" figure.</summary>
            private static float PartsBaseShare(TacticalActor actor, bool armour)
            {
                float total = 0f;
                float own = 0f;
                HashSet<TacticalItem> seen = new HashSet<TacticalItem>();

                foreach (ItemSlot slot in actor.BodyState?.GetHealthSlots() ?? Enumerable.Empty<ItemSlot>())
                {
                    foreach (TacticalItem item in slot.GetAllDirectItems(false))
                    {
                        if (item == null || !seen.Add(item))
                        {
                            continue;
                        }

                        StatusStat stat = armour ? item.GetArmor() : item.GetHealth();

                        if (stat != null)
                        {
                            total += (float)stat.Max;
                            own += OwnDelta(stat);
                        }
                    }
                }

                return total > 0f ? Mathf.Max(0f, total - own) / total : 1f;
            }

            #endregion

            #region perception check

            /// <summary>
            /// Studies every known, visible Pandoran that a player unit is close enough to: within half of
            /// vanilla's vision range between the two (perception x size and stealth). Only the moved actor's
            /// pairs are checked when one moved; everything at a turn start.
            /// </summary>
            private static void CheckStudy(TacticalLevelController level, TacticalActorBase moved)
            {
                if (!IsEnabled || level?.Map == null)
                {
                    return;
                }

                TacticalFaction player = level.GetFactionByCommandName(PlayerFactionCommandName);

                if (player?.Vision == null)
                {
                    return;
                }

                List<TacticalActor> viewers;
                IEnumerable<TacticalActor> targets;

                if (moved is TacticalActor movedActor && movedActor.TacticalFaction == player)
                {
                    viewers = new List<TacticalActor> { movedActor };
                    targets = level.Map.GetActors<TacticalActor>(null);
                }
                else
                {
                    viewers = player.TacticalActors.Where(actor => actor != null && actor.IsAlive && !actor.IsEvacuated).ToList();
                    targets = moved is TacticalActor movedTarget ? new[] { movedTarget } : level.Map.GetActors<TacticalActor>(null);
                }

                if (viewers.Count == 0)
                {
                    return;
                }

                foreach (TacticalActor target in targets.ToList())
                {
                    if (target == null || !target.IsAlive || !player.Vision.IsRevealed(target))
                    {
                        continue;
                    }

                    // Studying only matters once something about the species is unlocked.
                    Insight insight = GetInsight(target);

                    if (!insight.Gated || insight.Studied || !insight.Identified)
                    {
                        continue;
                    }

                    foreach (TacticalActor viewer in viewers)
                    {
                        if (viewer == null || !viewer.IsAlive || viewer == target)
                        {
                            continue;
                        }

                        float range = StudyRangeShare * TacticalFactionVision.GetPossibleVisionRangeBetweenActors(viewer, target);

                        if ((viewer.Pos - target.Pos).magnitude <= range)
                        {
                            Study(target, viewer);
                            break;
                        }
                    }
                }
            }

            private static void Study(TacticalActor target, TacticalActor viewer)
            {
                PandoranVarianceStatus status = target.Status.GetStatus<PandoranVarianceStatus>()
                    ?? target.Status.ApplyStatus(StatusDef) as PandoranVarianceStatus;

                if (status == null)
                {
                    return;
                }

                status.Studied = true;
                TFTVLogger.Always($"[HarshAttrition] {viewer.name} studied {target.name} up close");
                RefreshHealthbars(target);
            }

            #endregion

            #region healthbars

            /// <summary>Healthbars that hid something, per actor, so they can be redrawn the moment it is studied.</summary>
            private static readonly ConditionalWeakTable<TacticalActorBase, List<HealthbarUIActorElement>> GatedBars = new ConditionalWeakTable<TacticalActorBase, List<HealthbarUIActorElement>>();

            private static readonly MethodInfo UpdateHealthMethod = AccessTools.Method(typeof(HealthbarUIActorElement), "UpdateHealth");
            private static readonly MethodInfo UpdateArmorMethod = AccessTools.Method(typeof(HealthbarUIActorElement), "UpdateArmor");

            private static void RegisterBar(TacticalActorBase actor, HealthbarUIActorElement bar)
            {
                List<HealthbarUIActorElement> bars = GatedBars.GetOrCreateValue(actor);

                if (!bars.Contains(bar))
                {
                    bars.Add(bar);
                }
            }

            private static void RefreshHealthbars(TacticalActorBase actor)
            {
                if (!GatedBars.TryGetValue(actor, out List<HealthbarUIActorElement> bars))
                {
                    return;
                }

                foreach (HealthbarUIActorElement bar in bars.ToList())
                {
                    if (bar == null || bar.Actor != actor)
                    {
                        bars.Remove(bar);
                        continue;
                    }

                    // Virtual: the free-aim bar's overrides run for free-aim bars.
                    UpdateHealthMethod?.Invoke(bar, new object[] { false });
                    UpdateArmorMethod?.Invoke(bar, null);
                }
            }

            /// <summary>Health bars removed for an unknown specimen, to switch back on when the element shows someone else.</summary>
            private static readonly HashSet<ValueBarElement> HiddenHealthBars = new HashSet<ValueBarElement>();

            private static readonly AccessTools.FieldRef<HealthbarUIActorElement, StatusStat> HealthStatField =
                AccessTools.FieldRefAccess<HealthbarUIActorElement, StatusStat>("_healthStat");

            /// <summary>
            /// Whether a bar shows the creature (its Hit Points, known through its template) or one body part (known
            /// through that part), and whether it may show the real values.
            /// </summary>
            private static void ReadBar(HealthbarUIActorElement bar, Insight insight, out bool creatureBar, out bool known, out bool real)
            {
                TacticalActor actor = bar?.Actor as TacticalActor;
                StatusStat health = bar == null ? null : HealthStatField(bar);
                creatureBar = actor == null || health == null || health == actor.CharacterStats?.Health;
                known = creatureBar ? insight.Health : insight.KnowsPartWithHealth(health);
                real = known && insight.Studied;
            }

            /// <summary>
            /// Short of the real values: no number, no lethal-shot skull, no predicted loss. With its Hit Points (or this
            /// part) not unlocked there is no bar at all: a creature-level bar shows the damage taken instead, a part's nothing.
            /// </summary>
            private static void GateHealth(HealthbarUIActorElement bar)
            {
                TacticalActorBase actor = bar?.Actor as TacticalActorBase;
                Insight insight = GetInsight(actor);
                ReadBar(bar, insight, out bool creatureBar, out bool known, out bool real);

                // Healthbars are pooled: one that hid its bar for an unknown specimen may now show anyone.
                if (bar?.HealthBar != null && known && HiddenHealthBars.Remove(bar.HealthBar))
                {
                    bar.HealthBar.gameObject.SetActive(true);
                }

                if (bar == null || real)
                {
                    return;
                }

                RegisterBar(actor, bar);

                if (bar.FlashingSkull != null)
                {
                    bar.FlashingSkull.gameObject.SetActive(false);
                }

                if (!known)
                {
                    if (bar.HealthBar != null && bar.HealthBar.gameObject.activeSelf)
                    {
                        bar.HealthBar.gameObject.SetActive(false);
                        HiddenHealthBars.Add(bar.HealthBar);
                    }

                    if (bar.HealthAmountText != null)
                    {
                        bar.HealthAmountText.text = creatureBar ? $"−{Mathf.RoundToInt(GetDamageTaken(actor))}" : string.Empty;
                    }

                    return;
                }

                if (bar.HealthAmountText != null)
                {
                    bar.HealthAmountText.text = "?";
                }

                if (bar.HealthBar == null)
                {
                    return;
                }

                // The predicted loss is set straight on the bar when aiming (HealthbarUIActorElement.set_DamagePrediction).
                bar.HealthBar.PredictedValue = 0f;
                bar.HealthBar.PredictedPipsCount = 0;
                bar.HealthBar.PredictedChance = 0f;
                bar.HealthBar.UpdateValue(false);
            }

            /// <summary>No predicted armour loss; the template's armour marked "?", or no value at all while it is locked.</summary>
            private static void GateArmour(HealthbarUIActorElement bar, MaskValueBarElement alternate)
            {
                TacticalActorBase actor = bar?.Actor as TacticalActorBase;
                ReadBar(bar, GetInsight(actor), out bool creatureBar, out bool known, out bool real);

                if (bar == null || real)
                {
                    return;
                }

                RegisterBar(actor, bar);

                foreach (MaskValueBarElement armourBar in new[] { bar.ArmorBar, alternate })
                {
                    if (armourBar == null)
                    {
                        continue;
                    }

                    armourBar.PredictedChange = 0f;

                    if (!known)
                    {
                        armourBar.CurrentValue = armourBar.MaxValue;
                    }

                    armourBar.UpdateValue();
                }

                if (bar.ArmorAmountText == null || bar.ArmorBar == null)
                {
                    return;
                }

                if (!known)
                {
                    bar.ArmorAmountText.text = "?";
                    return;
                }

                // One part's own roll, or every part's together for the creature.
                float share = creatureBar
                    ? PartsBaseShare((TacticalActor)actor, armour: true)
                    : BaseShare(FindSlot((TacticalActor)actor, HealthStatField(bar))?.GetArmor());

                bar.ArmorAmountText.text = Approximately(Mathf.Ceil(bar.ArmorBar.CurrentValue * share));
            }

            #endregion

            #region info screen and tooltip

            private static Insight _statusScreenInsight = Insight.Everything;

            private static string UnknownName => TFTVCommonMethods.ConvertKeyToString(UnknownNameKey);

            private static string DamageTakenLabel => TFTVCommonMethods.ConvertKeyToString("TFTV_DAMAGE_TAKEN");

            private static UIModuleCharacterStatus.CharacterData.ValueBarData Scale(UIModuleCharacterStatus.CharacterData.ValueBarData bar, float share)
            {
                bar.Current = Mathf.Round(bar.Current * share);
                bar.Limit = Mathf.Round(bar.Limit * share);
                bar.Max = Mathf.Round(bar.Max * share);
                return bar;
            }

            /// <summary>
            /// The info screen. Not identified: its name and the statuses others put on it only. Otherwise each part is
            /// shown as far as it is unlocked: the template's Hit Points and stats marked "?" until it is studied, each
            /// body-part row on its own (a part never killed or captured reads "?"), abilities with the vivisection.
            /// </summary>
            private static void GateStatusScreenData(UIModuleCharacterStatus.CharacterData data)
            {
                // Before vanilla fills the screen: what was hidden for the last unknown specimen comes back, and
                // vanilla then sets it up as it would for anyone.
                RestoreStatusScreen();
                _statusScreenPartRow = 0;
                _statusScreenParts.Clear();

                TacticalActor actor = TFTVUI.Tactical.AcidReadout.UIStateCharacterStatus_SetData_TrackActor_Patch.Current;
                Insight insight = actor == null ? Insight.Everything : GetInsight(actor);
                _statusScreenInsight = insight;

                if (!insight.Gated || data == null)
                {
                    return;
                }

                if (!insight.Identified)
                {
                    data.Name = UnknownName;
                    data.Abilities?.Clear();
                    data.BodyParts?.Clear();
                    data.Armour?.Clear();
                    data.Equipment?.Clear();
                    return;
                }

                if (!insight.Stats)
                {
                    data.Abilities?.Clear();
                }

                // Health's maximum is Endurance x a multiplier, so it is rolled through Endurance. The bar keeps its fill.
                if (insight.Health && !insight.Studied)
                {
                    data.HitPoints = Scale(data.HitPoints, BaseShare(actor.CharacterStats?.Endurance));
                }

                _statusScreenHealthBase = Mathf.RoundToInt(data.HitPoints.Max);

                if (data.BodyParts != null)
                {
                    List<ItemSlot> slots = actor.BodyState?.GetHealthSlots().ToList() ?? new List<ItemSlot>();
                    int next = 0;

                    for (int i = 0; i < data.BodyParts.Count; i++)
                    {
                        UIModuleCharacterStatus.CharacterData.BodyPartData part = data.BodyParts[i];

                        // Rows are built from the health slots in order (empty optional slots skipped), named after the slot.
                        while (next < slots.Count && !ReferenceEquals(slots[next].ItemSlotDef?.DisplayName, part.Name))
                        {
                            next++;
                        }

                        ItemSlot slot = next < slots.Count ? slots[next++] : null;
                        bool known = slot != null && insight.KnowsPart(slot);
                        StatusStat partHealth = slot?.GetHealth();
                        StatusStat partArmour = slot?.GetArmor();

                        // Template values: this individual's maximum less its own roll.
                        _statusScreenParts.Add(new PartRow
                        {
                            Known = known,
                            Real = known && insight.Studied,
                            Health = partHealth == null ? part.HitPoints.Max : (float)partHealth.Max - OwnDelta(partHealth),
                            Armour = partArmour == null ? part.Armour.Limit : (float)partArmour.Max - OwnDelta(partArmour),
                        });

                        // A locked part's bars give nothing away either: drawn full.
                        if (!known)
                        {
                            part.HitPoints.Current = part.HitPoints.Max;
                            part.Armour.Current = part.Armour.Limit;
                            data.BodyParts[i] = part;
                        }
                    }
                }

                // The template's Perception, movement and Will Points for the texts, without this individual's rolls.
                if (insight.Stats && !insight.Studied)
                {
                    data.Perception -= FlatRoll(actor, PerceptionStatName);
                }

                _statusScreenMovementBase = Mathf.RoundToInt(data.ActionPoints.Max - FlatRoll(actor, SpeedStatName));
                _statusScreenWillBase = Mathf.RoundToInt(data.WillPoints.Max - FlatRoll(actor, WillpowerStatName));
            }

            private static int _statusScreenHealthBase;
            private static int _statusScreenMovementBase;
            private static int _statusScreenWillBase;

            private struct PartRow
            {
                internal bool Known;
                internal bool Real;
                internal float Health;
                internal float Armour;
            }

            /// <summary>What each body-part row may show, in row order; GateBodyPartRow takes them one by one.</summary>
            private static readonly List<PartRow> _statusScreenParts = new List<PartRow>();
            private static int _statusScreenPartRow;

            /// <summary>
            /// A body-part row: its template HP and armour with "?" (or its real ones once studied), or plain "?" for a part
            /// never killed or captured. Runs inside UIModuleCharacterStatus.SetData, so the insight set by its prefix applies.
            /// </summary>
            private static void GateBodyPartRow(PhoenixPoint.Tactical.View.ViewControllers.CharacterStatusBodyPartRowController row, UIModuleCharacterStatus.CharacterData.BodyPartData part)
            {
                if (!_statusScreenInsight.Gated || row == null)
                {
                    return;
                }

                PartRow info = _statusScreenPartRow < _statusScreenParts.Count ? _statusScreenParts[_statusScreenPartRow] : default;
                _statusScreenPartRow++;

                if (info.Real)
                {
                    return;
                }

                if (row.HitPoints != null)
                {
                    row.HitPoints.text = info.Known ? Approximately(info.Health) : "?";
                }

                // Vanilla hides the armour group when Limit is 0.
                if (row.Armour != null && part.Armour.Limit != 0f)
                {
                    row.Armour.text = info.Known ? Approximately(info.Armour) : "?";
                }
            }

            /// <summary>
            /// How an approximate value reads: the species' base with a "?" after it. A "~" in front looked like a minus
            /// sign in the game's font.
            /// </summary>
            private static string Approximately(float value)
            {
                return $"{Mathf.RoundToInt(value)}?";
            }

            private static void GateStatusScreenTexts(UIModuleCharacterStatus module)
            {
                Insight insight = _statusScreenInsight;
                _statusScreenInsight = Insight.Everything;

                if (!insight.Gated || module == null)
                {
                    return;
                }

                Transform[] keys = { module.HitPointsBar?.transform, module.WillPointsBar?.transform, module.ActionPointsBar?.transform, module.PerceptionText?.transform };

                if (!insight.Health)
                {
                    // No Hit Points: the bar goes, and the class line says how much damage it has taken.
                    if (module.CharacterClass != null)
                    {
                        TacticalActor actor = TFTVUI.Tactical.AcidReadout.UIStateCharacterStatus_SetData_TrackActor_Patch.Current;
                        module.CharacterClass.text = $"{DamageTakenLabel}: {Mathf.RoundToInt(GetDamageTaken(actor))}";
                    }

                    HideGroup(module, keys, module.HitPointsBar?.transform, module.HitPointsBarText?.transform);
                }
                else if (!insight.Studied && module.HitPointsBarText != null)
                {
                    // The template's maximum only: no current value, nothing this individual rolled.
                    module.HitPointsBarText.text = Approximately(_statusScreenHealthBase);
                }

                if (!insight.Stats)
                {
                    HideGroup(module, keys, module.WillPointsBar?.transform, module.WillPointsBarText?.transform);

                    if (module.ActionPointsGroup != null)
                    {
                        HideGroup(module, keys, module.ActionPointsGroup.transform, module.ActionPointsBarText?.transform);
                    }

                    HideGroup(module, keys, module.PerceptionText?.transform, module.StealthText?.transform);
                    HideGroup(module, keys, module.AccuracyText?.transform, null);
                }
                else if (!insight.Studied)
                {
                    if (module.ActionPointsBarText != null)
                    {
                        module.ActionPointsBarText.text = Approximately(_statusScreenMovementBase);
                    }

                    if (module.WillPointsBarText != null)
                    {
                        module.WillPointsBarText.text = Approximately(_statusScreenWillBase);
                    }

                    if (module.PerceptionText != null && float.TryParse(module.PerceptionText.text, out float perception))
                    {
                        module.PerceptionText.text = Approximately(perception);
                    }
                }
            }

            /// <summary>Hidden on the info screen for an unknown specimen, to switch back on for the next character.</summary>
            private static readonly List<GameObject> HiddenOnStatusScreen = new List<GameObject>();

            private static void RestoreStatusScreen()
            {
                foreach (GameObject hidden in HiddenOnStatusScreen)
                {
                    if (hidden != null)
                    {
                        hidden.SetActive(true);
                    }
                }

                HiddenOnStatusScreen.Clear();
            }

            private static void Hide(Transform transform)
            {
                if (transform != null && transform.gameObject.activeSelf)
                {
                    transform.gameObject.SetActive(false);
                    HiddenOnStatusScreen.Add(transform.gameObject);
                }
            }

            /// <summary>
            /// Hides the smallest group holding both elements (a bar and its number, say), which takes the row's label
            /// with it - unless that group also holds another row's key element, in which case only the two go.
            /// </summary>
            private static void HideGroup(UIModuleCharacterStatus module, Transform[] keys, Transform first, Transform second)
            {
                if (first == null)
                {
                    return;
                }

                Transform group = second == null ? first.parent : first;

                while (group != null && second != null && !second.IsChildOf(group))
                {
                    group = group.parent;
                }

                bool shared = group == null
                    || group == module.transform
                    || keys.Any(key => key != null && key != first && key != second && key.IsChildOf(group));

                if (shared)
                {
                    Hide(first);
                    Hide(second);
                    return;
                }

                Hide(group);
            }

            /// <summary>
            /// Statuses a creature has of its own (Regeneration, say) say what it can do: until it is vivisected only those
            /// put on it by someone else - the player's bleeding, poison, fire - stay listed. Every status list in the UI
            /// comes from these two methods: the info screen and the hover tooltip from the first, the healthbar from the second.
            /// </summary>
            private static void FilterInnateStatuses(TacticalActorViewBase view, ref List<TacticalActorViewBase.StatusInfo> infos)
            {
                if (infos == null || infos.Count == 0 || view == null)
                {
                    return;
                }

                TacticalActorBase actor = view.ActorBase;

                if (GetInsight(actor).Stats || actor.Status == null)
                {
                    return;
                }

                HashSet<TacStatusDef> innate = new HashSet<TacStatusDef>();
                HashSet<TacStatusDef> applied = new HashSet<TacStatusDef>();

                foreach (TacStatus status in actor.Status.Statuses.OfType<TacStatus>())
                {
                    TacticalActorBase source = TacUtil.GetSourceTacticalActorBase(status.Source);
                    (source != null && source != actor ? applied : innate).Add(status.TacStatusDef);
                }

                infos = infos.Where(info => info?.Def == null || !innate.Contains(info.Def) || applied.Contains(info.Def)).ToList();
            }

            #endregion

            #region aiming

            /// <summary>Body part labels around an unknown specimen in free aim read "?"; the creature itself, UNKNOWN SPECIMEN.</summary>
            private static void GateReceiverName(IDamageReceiver receiver, ref string name)
            {
                TacticalActorBase actor = receiver?.GetActor();

                if (GetInsight(actor).Identified)
                {
                    return;
                }

                name = ReferenceEquals(receiver, actor) ? UnknownName : "?";
            }

            /// <summary>
            /// The free-aim panel for the part under the crosshair: what it does and what disabling it costs comes with
            /// the vivisection; its name, with either research.
            /// </summary>
            private static void GateTargetPanel(UIModuleFreeFirstPersonShooting module, IDamageReceiver receiver)
            {
                Insight insight = GetInsight(receiver?.GetActor());

                if (module == null || insight.Stats)
                {
                    return;
                }

                if (module.TargetingInfoPanel != null)
                {
                    module.TargetingInfoPanel.SetActive(false);
                }

                if (module.NameText != null && !insight.Identified)
                {
                    module.NameText.text = UnknownName;
                }
            }

            private static readonly FieldInfo StatusBarsField = AccessTools.Field(typeof(UIModuleFreeFirstPersonShooting), "_statusBars");
            private static bool _statusBarsHidden;

            /// <summary>The free-aim part bars carry the damage prediction: hidden short of its real Hit Points.</summary>
            private static void GateTargetStatusBars(UIModuleFreeFirstPersonShooting module, IDamageReceiver receiver)
            {
                if (!(StatusBarsField?.GetValue(module) is Component bars) || bars == null)
                {
                    return;
                }

                if (!GetInsight(receiver?.GetActor()).RealHealth)
                {
                    bars.gameObject.SetActive(false);
                    _statusBarsHidden = true;
                }
                else if (_statusBarsHidden)
                {
                    bars.gameObject.SetActive(true);
                    _statusBarsHidden = false;
                }
            }

            private static readonly MethodInfo ShootTargetHealthBarModuleGetter = AccessTools.PropertyGetter(typeof(PhoenixPoint.Tactical.View.ViewStates.UIStateShoot), "_shootTargetHealthBarModule");

            /// <summary>The target's name over its healthbar when aiming.</summary>
            private static void GateShootTargetName(PhoenixPoint.Tactical.View.ViewStates.UIStateShoot state, PhoenixPoint.Tactical.Entities.Abilities.TacticalAbilityTarget target)
            {
                if (GetInsight(target?.GetTargetActor()).Identified)
                {
                    return;
                }

                if (ShootTargetHealthBarModuleGetter?.Invoke(state, null) is UIModuleShootTargetHealthbar module && module.TargetNameText != null)
                {
                    module.TargetNameText.text = UnknownName.ToUpper();
                }
            }

            /// <summary>
            /// The hover tooltip, as TFTV builds it (UICharacterSelectedVanillaFixes.GenerateData): name, Health,
            /// Will Points, movement, Perception, Stealth, Accuracy, then statuses. Each line as far as it is unlocked:
            /// the template's value with "?" until studied, "?" (or, for Health, the damage taken) while locked.
            /// </summary>
            internal static void GateTooltip(TacticalActor actor, List<ShortActorInfoTooltipDataEntry> entries)
            {
                try
                {
                    Insight insight = GetInsight(actor);

                    if (entries == null || entries.Count < 7 || !insight.Gated)
                    {
                        return;
                    }

                    if (!insight.Identified)
                    {
                        ShortActorInfoTooltipDataEntry name = entries[0];
                        name.TextContent = UnknownName;
                        entries[0] = name;
                    }

                    if (!insight.Health)
                    {
                        // Health becomes the damage taken so far.
                        ShortActorInfoTooltipDataEntry health = entries[1];
                        health.TextContent = DamageTakenLabel;
                        health.ValueContent = Mathf.RoundToInt(GetDamageTaken(actor)).ToString();
                        entries[1] = health;
                    }
                    else if (!insight.Studied)
                    {
                        StatusStat maxHealth = actor.CharacterStats?.Health;
                        SetValue(entries, 1, maxHealth == null ? "?" : Approximately((float)maxHealth.Max * BaseShare(actor.CharacterStats.Endurance)));
                    }

                    if (!insight.Stats)
                    {
                        for (int i = 2; i <= 6; i++)
                        {
                            SetValue(entries, i, "?");
                        }
                    }
                    else if (!insight.Studied)
                    {
                        // Will Points, movement and Perception at the template's, without this individual's roll.
                        SetValue(entries, 2, Approximate(entries[2].ValueContent, FlatRoll(actor, WillpowerStatName)));
                        SetValue(entries, 3, Approximate(entries[3].ValueContent, FlatRoll(actor, SpeedStatName)));
                        SetValue(entries, 4, Approximate(entries[4].ValueContent, FlatRoll(actor, PerceptionStatName)));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>"12/18" or "18" less the roll, as "15?"; anything else is left as it is.</summary>
            private static string Approximate(string value, float roll)
            {
                string last = value?.Split('/').LastOrDefault();

                return float.TryParse(last, out float number) ? Approximately(number - roll) : value;
            }

            private static void SetValue(List<ShortActorInfoTooltipDataEntry> entries, int index, string value)
            {
                ShortActorInfoTooltipDataEntry entry = entries[index];
                entry.ValueContent = value;
                entries[index] = entry;
            }

            #endregion

            #region patches

            /// <summary>Every Hit Point loss goes through here (TacticalActor.ApplyDamageInternal is only reached from it).</summary>
            [HarmonyPatch(typeof(TacticalActorBase), nameof(TacticalActorBase.ApplyDamage))]
            internal static class TacticalActorBase_ApplyDamage_PandoranVariance_Patch
            {
                private static void Prefix(TacticalActorBase __instance, out float __state)
                {
                    __state = __instance is TacticalActor actor && actor.CharacterStats?.Health != null ? (float)actor.CharacterStats.Health : 0f;
                }

                private static void Postfix(TacticalActorBase __instance, float __state)
                {
                    try
                    {
                        CountDamage(__instance, __state);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>
            /// The geoscape's "Pandoran intelligence report" introduces every new Pandoran mutation, model and stats
            /// included, as the Pandorans research it (GeoAlienFaction.UpdateResearch). Under Harsh Attrition new
            /// mutations are met in the field, so those pages go. The report also carries the diplomacy penalty for
            /// ignored alien bases, already applied by then: if there is one, the report still opens to say so; if
            /// not, HasAnyData is false and vanilla does not open it at all.
            /// </summary>
            [HarmonyPatch(typeof(PhoenixPoint.Geoscape.View.GeoscapeView), "OnNewAlienIntlligence")]
            internal static class GeoscapeView_OnNewAlienIntlligence_PandoranVariance_Patch
            {
                private static void Prefix(PhoenixPoint.Geoscape.View.ViewControllers.Modal.AlienIntelligenceBriefData data)
                {
                    try
                    {
                        if (IsEnabled && data != null)
                        {
                            data.Researches = Enumerable.Empty<PhoenixPoint.Geoscape.Entities.Research.ResearchElement>();
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>Set while a notification is being built for an unknown specimen: its body parts lose their names.</summary>
            private static bool _maskSlotNames;

            /// <summary>
            /// The floating "body part disabled" message reads the slot's DisplayName (TacticalActorViewBase.ShowNotification,
            /// called from TacticalActorNotificationsComp.OnBodySlotBroken/OnBodyPartBroken). For an unknown specimen it
            /// says "BODY PART" instead of naming the limb. Nothing is shown while a roll is applied: the new armour
            /// ("Armor", DamageReceiverImplementation.OnArmorChanged) and part health ("EquipmentDamaged",
            /// TacticalActor.OnEquipmentHealthChange) would float up as damage over a reinforcement.
            /// </summary>
            [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.ShowNotification))]
            internal static class TacticalActorViewBase_ShowNotification_PandoranVariance_Patch
            {
                private static bool Prefix(TacticalActorViewBase __instance)
                {
                    if (_rolling)
                    {
                        return false;
                    }

                    try
                    {
                        _maskSlotNames = !GetInsight(__instance?.ActorBase).Identified;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                    return true;
                }

                private static void Finalizer()
                {
                    _maskSlotNames = false;
                }
            }

            [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.DisplayName), MethodType.Getter)]
            internal static class ItemSlot_DisplayName_PandoranVariance_Patch
            {
                private static void Postfix(ref string __result)
                {
                    if (_maskSlotNames)
                    {
                        __result = TFTVCommonMethods.ConvertKeyToString("TFTV_UNKNOWN_BODY_PART");
                    }
                }
            }

            private static readonly Type NamePopupType = AccessTools.Inner(typeof(TacticalActorViewBase), "NamePopup");
            private static readonly FieldInfo NamePopupNameField = NamePopupType == null ? null : AccessTools.Field(NamePopupType, "Name");

            /// <summary>The name that floats over a creature when it is first seen or selected: UNKNOWN SPECIMEN for an unknown one.</summary>
            [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.ShowNameNotification))]
            internal static class TacticalActorViewBase_ShowNameNotification_PandoranVariance_Patch
            {
                private static bool Prefix(TacticalActorViewBase __instance)
                {
                    try
                    {
                        if (NamePopupNameField == null || GetInsight(__instance.ActorBase).Identified)
                        {
                            return true;
                        }

                        object popup = Activator.CreateInstance(NamePopupType);
                        NamePopupNameField.SetValue(popup, UnknownName);
                        __instance.ShowNotification("SelectedActor", new object[] { popup, __instance.ActorBase });
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalLevelController), nameof(TacticalLevelController.ActorEnteredPlay))]
            internal static class TacticalLevelController_ActorEnteredPlay_PandoranVariance_Patch
            {
                private static void Postfix(TacticalActorBase actor)
                {
                    try
                    {
                        OnEnteredPlay(actor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>A Pandoran killed: a prefix, while its status (and the template on it) is still there.</summary>
            [HarmonyPatch(typeof(TacticalLevelController), nameof(TacticalLevelController.ActorDied))]
            internal static class TacticalLevelController_ActorDied_PandoranVariance_Patch
            {
                private static void Prefix(DeathReport deathReport)
                {
                    try
                    {
                        RecordEncounter(deathReport?.Actor, "killed");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalLevelController), nameof(TacticalLevelController.ActorMoved))]
            internal static class TacticalLevelController_ActorMoved_PandoranVariance_Patch
            {
                private static void Postfix(TacticalLevelController __instance, TacticalActorBase actor)
                {
                    try
                    {
                        CheckStudy(__instance, actor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.StartTurn))]
            internal static class TacticalFaction_StartTurn_PandoranVariance_Patch
            {
                private static void Postfix(TacticalFaction __instance)
                {
                    try
                    {
                        CheckStudy(__instance?.TacticalLevel, null);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(HealthbarUIActorElement), "UpdateHealth")]
            internal static class HealthbarUIActorElement_UpdateHealth_PandoranVariance_Patch
            {
                private static void Postfix(HealthbarUIActorElement __instance)
                {
                    try
                    {
                        GateHealth(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(FreeAimTargetHealthbarUIActorElement), "UpdateHealth")]
            internal static class FreeAimTargetHealthbarUIActorElement_UpdateHealth_PandoranVariance_Patch
            {
                private static void Postfix(FreeAimTargetHealthbarUIActorElement __instance)
                {
                    try
                    {
                        GateHealth(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(HealthbarUIActorElement), "UpdateArmor")]
            internal static class HealthbarUIActorElement_UpdateArmor_PandoranVariance_Patch
            {
                private static void Postfix(HealthbarUIActorElement __instance)
                {
                    try
                    {
                        GateArmour(__instance, null);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(FreeAimTargetHealthbarUIActorElement), "UpdateArmor")]
            internal static class FreeAimTargetHealthbarUIActorElement_UpdateArmor_PandoranVariance_Patch
            {
                private static void Postfix(FreeAimTargetHealthbarUIActorElement __instance)
                {
                    try
                    {
                        GateArmour(__instance, __instance.ArmorBarAlternate);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>The "damage: min-max" line under the target's healthbar when aiming.</summary>
            [HarmonyPatch(typeof(UIModuleShootTargetHealthbar), nameof(UIModuleShootTargetHealthbar.SetDamagePredictionText))]
            internal static class UIModuleShootTargetHealthbar_SetDamagePredictionText_PandoranVariance_Patch
            {
                private static void Postfix(UIModuleShootTargetHealthbar __instance)
                {
                    try
                    {
                        if (__instance.TargetDamagePredictionText != null && !GetInsight(__instance.HealthBar?.Actor as TacticalActorBase).RealHealth)
                        {
                            __instance.TargetDamagePredictionText.text = string.Empty;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.GetCharacterStatusActorStatuses))]
            internal static class TacticalActorViewBase_GetCharacterStatusActorStatuses_PandoranVariance_Patch
            {
                private static void Postfix(TacticalActorViewBase __instance, ref List<TacticalActorViewBase.StatusInfo> __result)
                {
                    try
                    {
                        FilterInnateStatuses(__instance, ref __result);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.GetHealthbarStatuses))]
            internal static class TacticalActorViewBase_GetHealthbarStatuses_PandoranVariance_Patch
            {
                private static void Postfix(TacticalActorViewBase __instance, ref List<TacticalActorViewBase.StatusInfo> __result)
                {
                    try
                    {
                        FilterInnateStatuses(__instance, ref __result);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(FreeAimTargetHealthbarUIActorElement), "GetReceiverDisplayName")]
            internal static class FreeAimTargetHealthbarUIActorElement_GetReceiverDisplayName_PandoranVariance_Patch
            {
                private static void Postfix(IDamageReceiver recv, ref string __result)
                {
                    try
                    {
                        GateReceiverName(recv, ref __result);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting), nameof(UIModuleFreeFirstPersonShooting.OnNewDamageReceiverSelected))]
            internal static class UIModuleFreeFirstPersonShooting_OnNewDamageReceiverSelected_PandoranVariance_Patch
            {
                private static void Postfix(UIModuleFreeFirstPersonShooting __instance, IDamageReceiver damageReceiver)
                {
                    try
                    {
                        GateTargetPanel(__instance, damageReceiver);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting), nameof(UIModuleFreeFirstPersonShooting.UpdateInfoPanelStatusBars))]
            internal static class UIModuleFreeFirstPersonShooting_UpdateInfoPanelStatusBars_PandoranVariance_Patch
            {
                private static void Postfix(UIModuleFreeFirstPersonShooting __instance, IDamageReceiver damageReceiver)
                {
                    try
                    {
                        GateTargetStatusBars(__instance, damageReceiver);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>No part description or disabling effects before the vivisection (the panel is hidden as well).</summary>
            [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting), "UpdateSlotInfoPanel")]
            internal static class UIModuleFreeFirstPersonShooting_UpdateSlotInfoPanel_PandoranVariance_Patch
            {
                private static bool Prefix(ItemSlot __0)
                {
                    try
                    {
                        return GetInsight(__0?.GetActor()).Stats;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting), "UpdateItemInfoPanel")]
            internal static class UIModuleFreeFirstPersonShooting_UpdateItemInfoPanel_PandoranVariance_Patch
            {
                private static bool Prefix(TacticalItem __0)
                {
                    try
                    {
                        return GetInsight(__0?.TacticalActor).Stats;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            [HarmonyPatch(typeof(PhoenixPoint.Tactical.View.ViewStates.UIStateShoot), "SetShootTarget", new Type[] { typeof(PhoenixPoint.Tactical.Entities.Abilities.TacticalAbilityTarget), typeof(bool) })]
            internal static class UIStateShoot_SetShootTarget_PandoranVariance_Patch
            {
                private static void Postfix(PhoenixPoint.Tactical.View.ViewStates.UIStateShoot __instance, PhoenixPoint.Tactical.Entities.Abilities.TacticalAbilityTarget abilityTarget)
                {
                    try
                    {
                        GateShootTargetName(__instance, abilityTarget);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleCharacterStatus), nameof(UIModuleCharacterStatus.SetData))]
            internal static class UIModuleCharacterStatus_SetData_PandoranVariance_Patch
            {
                private static void Prefix(UIModuleCharacterStatus.CharacterData data)
                {
                    try
                    {
                        GateStatusScreenData(data);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void Postfix(UIModuleCharacterStatus __instance)
                {
                    try
                    {
                        GateStatusScreenTexts(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(PhoenixPoint.Tactical.View.ViewControllers.CharacterStatusBodyPartRowController), nameof(PhoenixPoint.Tactical.View.ViewControllers.CharacterStatusBodyPartRowController.SetData))]
            internal static class CharacterStatusBodyPartRowController_SetData_PandoranVariance_Patch
            {
                private static void Postfix(PhoenixPoint.Tactical.View.ViewControllers.CharacterStatusBodyPartRowController __instance, UIModuleCharacterStatus.CharacterData.BodyPartData bodyPart)
                {
                    try
                    {
                        GateBodyPartRow(__instance, bodyPart);
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
}
