using Base.Entities.Statuses;
using HarmonyLib;
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
        /// Every Pandoran rolls its toughness when it enters play: max HP, and each body part's max HP and
        /// armour, each an independent uniform 60-140%. The rolls are AddMax modifications on the stats with
        /// the status def as source, so they are saved with the stats and never re-rolled.
        ///
        /// What the player sees depends on knowledge of the species and of the individual:
        /// Unknown (species not autopsied) - "UNKNOWN SPECIMEN", no health bar and no stats, only the damage it has taken;
        /// Known (autopsied) - health as a bar only, armour and the info screen at the species' base values
        /// marked "~", no damage prediction;
        /// Studied (autopsied, and a player unit got within half its vision range of this individual, as
        /// adjusted for the target's size and stealth) - everything, as vanilla, for the rest of the mission.
        /// Floating damage numbers and the status icons stay at every tier.
        /// </summary>
        internal static class PandoranVariance
        {
            internal static PandoranVarianceStatusDef StatusDef { get; set; }

            internal const float MinRoll = 0.6f;
            internal const float MaxRoll = 1.4f;
            /// <summary>Studied within this share of the vision range between the two actors.</summary>
            internal const float StudyRangeShare = 0.5f;

            private const string UnknownNameKey = "TFTV_UNKNOWN_SPECIMEN";
            private const string PlayerFactionCommandName = "px";

            internal enum Knowledge
            {
                Unknown,
                Known,
                Studied,
            }

            private sealed class Species
            {
                internal readonly string ClassTag;
                internal readonly string[] Research;

                internal Species(string classTag, params string[] research)
                {
                    ClassTag = classTag;
                    Research = research;
                }
            }

            /// <summary>
            /// Pandoran class tags and the autopsy (or live capture) researches that make them known. Eggs first:
            /// an actor takes the first entry whose tag it has.
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

            private static List<KeyValuePair<ClassTagDef, string>> _speciesTags;

            #region species and knowledge

            internal static void RecordAutopsies(GeoLevelController controller)
            {
                try
                {
                    List<string> autopsied = new List<string>();

                    if (controller?.PhoenixFaction?.Research == null)
                    {
                        return;
                    }

                    foreach (Species species in AllSpecies)
                    {
                        if (species.Research.Any(id => controller.PhoenixFaction.Research.HasCompleted(id)))
                        {
                            autopsied.Add(species.ClassTag);
                        }
                    }

                    AutopsiedClassTags = autopsied;
                    TFTVLogger.Always($"[HarshAttrition] Autopsied species: {string.Join(", ", autopsied)}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
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

            /// <summary>Studied for anything that is not a Pandoran, for Pandorans the player controls, and with the option off.</summary>
            internal static Knowledge GetKnowledge(TacticalActorBase actorBase)
            {
                if (!IsEnabled || !(actorBase is TacticalActor actor))
                {
                    return Knowledge.Studied;
                }

                string species = GetSpecies(actor);

                if (species == null || actor.IsControlledByPlayer)
                {
                    return Knowledge.Studied;
                }

                if (AutopsiedClassTags == null || !AutopsiedClassTags.Contains(species))
                {
                    return Knowledge.Unknown;
                }

                PandoranVarianceStatus status = actor.Status?.GetStatus<PandoranVarianceStatus>();
                return status != null && status.Studied ? Knowledge.Studied : Knowledge.Known;
            }

            private static bool IsGated(TacticalActorBase actor, out Knowledge knowledge)
            {
                knowledge = GetKnowledge(actor);
                return knowledge != Knowledge.Studied;
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

                // Already rolled: a loaded actor brings both the status and the modifications back.
                if (actor.Status.GetStatus<PandoranVarianceStatus>() != null || OwnDelta(actor.CharacterStats?.Health) != 0f)
                {
                    return;
                }

                float healthRoll = NextRoll();
                HashSet<TacticalItem> rolledItems = new HashSet<TacticalItem>();
                int perceptionRoll;
                int speedRoll;

                // While the new maximums go on, Health can dip for a moment (and a low roll lowers it for good). Hurt
                // reactions listen to that - the Triton camo, eggs starting to hatch, mist - so they are muted until
                // the roll is done.
                _rolling = true;

                try
                {
                    actor.Status.ApplyStatus(StatusDef);
                    ApplyRoll(actor.CharacterStats?.Health, healthRoll);

                    foreach (ItemSlot slot in actor.BodyState.GetHealthSlots().ToList())
                    {
                        float partHealthRoll = NextRoll();
                        float partArmourRoll = NextRoll();

                        foreach (TacticalItem item in slot.GetAllDirectItems(false))
                        {
                            // An item spanning several slots is rolled once.
                            if (item == null || !rolledItems.Add(item))
                            {
                                continue;
                            }

                            ApplyRoll(item.GetHealth(), partHealthRoll);
                            ApplyRoll(item.GetArmor(), partArmourRoll);
                        }
                    }

                    perceptionRoll = RollFlat(actor, PerceptionStatName);
                    speedRoll = RollFlat(actor, SpeedStatName);
                    actor.UpdateStats();
                }
                finally
                {
                    _rolling = false;
                }

                TFTVLogger.Always($"[HarshAttrition] Rolled {actor.name}: HP x{healthRoll:0.00}, {rolledItems.Count} body parts, Perception {perceptionRoll:+0;-0;0}, Speed {speedRoll:+0;-0;0}");
            }

            /// <summary>Set while a creature's stats are being rolled: its hurt reactions must not take that for damage.</summary>
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
            private const string PerceptionStatName = "Perception";
            private const string SpeedStatName = "Speed";

            /// <summary>
            /// A whole-number roll from -3 to +3 added to the stat, sourced from the status def like the HP rolls. A stat
            /// at 0 (an egg's or a sentinel's Speed) is left alone, and no roll takes a stat below 1.
            /// </summary>
            private static int RollFlat(TacticalActor actor, string statName)
            {
                BaseStat stat = actor.Status.GetStat(statName);

                if (stat == null)
                {
                    return 0;
                }

                float current = stat;
                int roll = Rolls.Next(-MaxFlatRoll, MaxFlatRoll + 1);

                if (current <= 0f || roll == 0)
                {
                    return 0;
                }

                roll = Mathf.Max(roll, 1 - Mathf.RoundToInt(current));

                if (roll == 0)
                {
                    return 0;
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

            private static float NextRoll()
            {
                return MinRoll + (float)Rolls.NextDouble() * (MaxRoll - MinRoll);
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

            /// <summary>Scales the stat's maximum by the roll, keeping it as full (or as hurt) as it was.</summary>
            private static void ApplyRoll(StatusStat stat, float roll)
            {
                if (stat == null)
                {
                    return;
                }

                float max = (float)stat.Max;
                float delta = Mathf.Round(max * (roll - 1f));

                if (Mathf.Abs(delta) < 1f)
                {
                    return;
                }

                float share = max > 0f ? Mathf.Clamp01((float)stat / max) : 1f;

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
                    if (target == null || !target.IsAlive || GetKnowledge(target) != Knowledge.Known || !player.Vision.IsRevealed(target))
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
            /// Below Studied: no number, no lethal-shot skull, no predicted loss. An unknown specimen has no health bar
            /// at all; its creature-level bars show the damage it has taken instead, and its body-part bars nothing.
            /// </summary>
            private static void GateHealth(HealthbarUIActorElement bar)
            {
                TacticalActorBase actor = bar?.Actor as TacticalActorBase;
                bool gated = IsGated(actor, out Knowledge knowledge);

                // Healthbars are pooled: one that hid its bar for an unknown specimen may now show anyone.
                if (bar?.HealthBar != null && knowledge != Knowledge.Unknown && HiddenHealthBars.Remove(bar.HealthBar))
                {
                    bar.HealthBar.gameObject.SetActive(true);
                }

                if (!gated)
                {
                    return;
                }

                RegisterBar(actor, bar);

                if (bar.FlashingSkull != null)
                {
                    bar.FlashingSkull.gameObject.SetActive(false);
                }

                if (knowledge == Knowledge.Unknown)
                {
                    if (bar.HealthBar != null && bar.HealthBar.gameObject.activeSelf)
                    {
                        bar.HealthBar.gameObject.SetActive(false);
                        HiddenHealthBars.Add(bar.HealthBar);
                    }

                    if (bar.HealthAmountText != null)
                    {
                        bool creatureBar = HealthStatField(bar) == null || HealthStatField(bar) == ((TacticalActor)actor).CharacterStats?.Health;
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

            /// <summary>No predicted armour loss; the species' base armour marked "~", or nothing for an unknown species.</summary>
            private static void GateArmour(HealthbarUIActorElement bar, MaskValueBarElement alternate)
            {
                TacticalActorBase actor = bar?.Actor as TacticalActorBase;

                if (!IsGated(actor, out Knowledge knowledge))
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

                    if (knowledge == Knowledge.Unknown)
                    {
                        armourBar.CurrentValue = armourBar.MaxValue;
                    }

                    armourBar.UpdateValue();
                }

                if (bar.ArmorAmountText != null && bar.ArmorBar != null)
                {
                    bar.ArmorAmountText.text = knowledge == Knowledge.Unknown
                        ? "?"
                        : Approximately(Mathf.Ceil(bar.ArmorBar.CurrentValue * PartsBaseShare((TacticalActor)actor, armour: true)));
                }
            }

            #endregion

            #region info screen and tooltip

            private static Knowledge _statusScreenKnowledge = Knowledge.Studied;

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
            /// The info screen: an unknown species shows its name and statuses only; a known one shows the
            /// species' base health and armour (the body parts scaled together, so near enough) marked "~".
            /// </summary>
            private static void GateStatusScreenData(UIModuleCharacterStatus.CharacterData data)
            {
                // Before vanilla fills the screen: what was hidden for the last unknown specimen comes back, and
                // vanilla then sets it up as it would for anyone.
                RestoreStatusScreen();

                TacticalActor actor = TFTVUI.Tactical.AcidReadout.UIStateCharacterStatus_SetData_TrackActor_Patch.Current;
                _statusScreenKnowledge = actor == null ? Knowledge.Studied : GetKnowledge(actor);

                if (_statusScreenKnowledge == Knowledge.Studied || data == null)
                {
                    return;
                }

                if (_statusScreenKnowledge == Knowledge.Unknown)
                {
                    data.Name = UnknownName;
                    data.Abilities?.Clear();
                    data.BodyParts?.Clear();
                    data.Armour?.Clear();
                    data.Equipment?.Clear();
                    return;
                }

                data.HitPoints = Scale(data.HitPoints, BaseShare(actor.CharacterStats?.Health));

                if (data.BodyParts != null)
                {
                    float healthShare = PartsBaseShare(actor, armour: false);
                    float armourShare = PartsBaseShare(actor, armour: true);

                    for (int i = 0; i < data.BodyParts.Count; i++)
                    {
                        UIModuleCharacterStatus.CharacterData.BodyPartData part = data.BodyParts[i];
                        part.HitPoints = Scale(part.HitPoints, healthShare);
                        part.Armour = Scale(part.Armour, armourShare);
                        data.BodyParts[i] = part;
                    }
                }

                // The species' Perception, and the species' base Hit Points and movement for the texts (the bars keep
                // their real fill), without this individual's rolls.
                data.Perception -= FlatRoll(actor, PerceptionStatName);
                _statusScreenHealthBase = Mathf.RoundToInt(data.HitPoints.Max);
                _statusScreenMovementBase = Mathf.RoundToInt(data.ActionPoints.Max - FlatRoll(actor, SpeedStatName));
            }

            private static int _statusScreenHealthBase;
            private static int _statusScreenMovementBase;

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
                Knowledge knowledge = _statusScreenKnowledge;
                _statusScreenKnowledge = Knowledge.Studied;

                if (knowledge == Knowledge.Studied || module == null)
                {
                    return;
                }

                if (knowledge == Knowledge.Known)
                {
                    // The species' base maximums only: no current values, nothing this individual rolled.
                    if (module.HitPointsBarText != null)
                    {
                        module.HitPointsBarText.text = Approximately(_statusScreenHealthBase);
                    }

                    if (module.ActionPointsBarText != null)
                    {
                        module.ActionPointsBarText.text = Approximately(_statusScreenMovementBase);
                    }

                    if (module.PerceptionText != null && float.TryParse(module.PerceptionText.text, out float perception))
                    {
                        module.PerceptionText.text = Approximately(perception);
                    }

                    return;
                }

                // An unknown specimen: no bars and no stats at all - its name, the damage it has taken (in the class
                // line) and the statuses others put on it.
                if (module.CharacterClass != null)
                {
                    TacticalActor actor = TFTVUI.Tactical.AcidReadout.UIStateCharacterStatus_SetData_TrackActor_Patch.Current;
                    module.CharacterClass.text = $"{DamageTakenLabel}: {Mathf.RoundToInt(GetDamageTaken(actor))}";
                }

                Transform[] keys = { module.HitPointsBar?.transform, module.WillPointsBar?.transform, module.ActionPointsBar?.transform, module.PerceptionText?.transform };

                HideGroup(module, keys, module.HitPointsBar?.transform, module.HitPointsBarText?.transform);
                HideGroup(module, keys, module.WillPointsBar?.transform, module.WillPointsBarText?.transform);

                if (module.ActionPointsGroup != null)
                {
                    HideGroup(module, keys, module.ActionPointsGroup.transform, module.ActionPointsBarText?.transform);
                }

                HideGroup(module, keys, module.PerceptionText?.transform, module.StealthText?.transform);
                HideGroup(module, keys, module.AccuracyText?.transform, null);
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
            /// Statuses an unknown specimen has of its own (Regeneration, say) say what it is; only those put on it by
            /// someone else - the player's bleeding, poison, fire - stay listed. Every status list in the UI comes
            /// from these two methods: the info screen and the hover tooltip from the first, the healthbar from the second.
            /// </summary>
            private static void FilterInnateStatuses(TacticalActorViewBase view, ref List<TacticalActorViewBase.StatusInfo> infos)
            {
                if (infos == null || infos.Count == 0 || view == null)
                {
                    return;
                }

                TacticalActorBase actor = view.ActorBase;

                if (GetKnowledge(actor) != Knowledge.Unknown || actor.Status == null)
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

                if (GetKnowledge(actor) != Knowledge.Unknown)
                {
                    return;
                }

                name = ReferenceEquals(receiver, actor) ? UnknownName : "?";
            }

            /// <summary>
            /// The free-aim panel for the part under the crosshair: its name, what it does and what disabling it costs.
            /// None of it for an unknown specimen.
            /// </summary>
            private static void GateTargetPanel(UIModuleFreeFirstPersonShooting module, IDamageReceiver receiver)
            {
                if (module == null || GetKnowledge(receiver?.GetActor()) != Knowledge.Unknown)
                {
                    return;
                }

                if (module.TargetingInfoPanel != null)
                {
                    module.TargetingInfoPanel.SetActive(false);
                }

                if (module.NameText != null)
                {
                    module.NameText.text = UnknownName;
                }
            }

            private static readonly FieldInfo StatusBarsField = AccessTools.Field(typeof(UIModuleFreeFirstPersonShooting), "_statusBars");
            private static bool _statusBarsHidden;

            /// <summary>The free-aim part bars carry the damage prediction: hidden below Studied.</summary>
            private static void GateTargetStatusBars(UIModuleFreeFirstPersonShooting module, IDamageReceiver receiver)
            {
                if (!(StatusBarsField?.GetValue(module) is Component bars) || bars == null)
                {
                    return;
                }

                if (IsGated(receiver?.GetActor(), out _))
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
                if (GetKnowledge(target?.GetTargetActor()) != Knowledge.Unknown)
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
            /// Will Points, movement, Perception, Stealth, Accuracy, then statuses. Health is never shown below
            /// Studied; an unknown species shows nothing but its statuses.
            /// </summary>
            internal static void GateTooltip(TacticalActor actor, List<ShortActorInfoTooltipDataEntry> entries)
            {
                try
                {
                    if (entries == null || entries.Count < 7 || !IsGated(actor, out Knowledge knowledge))
                    {
                        return;
                    }

                    if (knowledge == Knowledge.Known)
                    {
                        SetValue(entries, 1, "?");

                        // Movement and Perception at the species' base, without this individual's roll.
                        SetValue(entries, 3, Approximate(entries[3].ValueContent, FlatRoll(actor, SpeedStatName)));
                        SetValue(entries, 4, Approximate(entries[4].ValueContent, FlatRoll(actor, PerceptionStatName)));
                        return;
                    }

                    // Health becomes the damage taken so far.
                    ShortActorInfoTooltipDataEntry health = entries[1];
                    health.TextContent = DamageTakenLabel;
                    health.ValueContent = Mathf.RoundToInt(GetDamageTaken(actor)).ToString();
                    entries[1] = health;

                    ShortActorInfoTooltipDataEntry name = entries[0];
                    name.TextContent = UnknownName;
                    entries[0] = name;

                    for (int i = 2; i <= 6; i++)
                    {
                        SetValue(entries, i, "?");
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
            /// says "BODY PART" instead of naming the limb.
            /// </summary>
            [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.ShowNotification))]
            internal static class TacticalActorViewBase_ShowNotification_PandoranVariance_Patch
            {
                private static void Prefix(TacticalActorViewBase __instance)
                {
                    try
                    {
                        _maskSlotNames = GetKnowledge(__instance?.ActorBase) == Knowledge.Unknown;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
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
                        if (NamePopupNameField == null || GetKnowledge(__instance.ActorBase) != Knowledge.Unknown)
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
                        if (__instance.TargetDamagePredictionText != null && IsGated(__instance.HealthBar?.Actor as TacticalActorBase, out _))
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

            /// <summary>No part description or disabling effects for an unknown specimen (the panel is hidden as well).</summary>
            [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting), "UpdateSlotInfoPanel")]
            internal static class UIModuleFreeFirstPersonShooting_UpdateSlotInfoPanel_PandoranVariance_Patch
            {
                private static bool Prefix(ItemSlot __0)
                {
                    try
                    {
                        return GetKnowledge(__0?.GetActor()) != Knowledge.Unknown;
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
                        return GetKnowledge(__0?.TacticalActor) != Knowledge.Unknown;
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

            #endregion
        }
    }
}
