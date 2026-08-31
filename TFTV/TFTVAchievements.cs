using Base.Achievements;
using Base.Core;
using Base.Defs;
using Base.Platforms;
using Base.Utils.GameConsole;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Achievements;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    /// <summary>
    /// Keeps the game's collection achievements winnable in a modded campaign.
    ///
    /// Three of them are checklists of fixed contents baked into a def at build time:
    /// ResearchAllProjects holds research Ids, ManufactureAllItems holds ItemDefs and
    /// CaptureAllAlienTypes holds ClassTagDefs. Each completes on an exact count match
    /// (ListAchievement.Completed is Progress.Count == AllowedValues.Length), so a single entry
    /// the mod has made unreachable locks the achievement out for good - which is what has
    /// happened to Master Scientist and Master Manufacturer, since TFTV drops researches from the
    /// research DBs and strips the manufacturable tag off items that are still on those lists.
    ///
    /// The checklists are therefore pruned down to what a TFTV campaign can actually deliver.
    /// Pruning only ever removes entries that are provably unobtainable - it never adds TFTV's own
    /// content, which would make the achievements harder than the ones players are chasing - and
    /// it refuses to act at all if its own reachability check looks wrong, since over-pruning
    /// would hand out a Steam achievement nobody earned.
    /// </summary>
    internal static class TFTVAchievements
    {
        private const string LogPrefix = "[TFTV][Achievements] ";

        private const string ResearchAllProjectsId = "ResearchAllProjects";
        private const string ManufactureAllItemsId = "ManufactureAllItems";
        private const string CaptureAllAlienTypesId = "CaptureAllAlienTypes";

        /// <summary>
        /// A checklist that loses more than this share of its entries says the reachability check
        /// below is broken rather than the game's data, so the achievement is left alone.
        /// </summary>
        private const float MaxPrunedFraction = 0.5f;

        /// <summary>
        /// Share of a checklist the manufacturing unlock-route model has to account for before its
        /// verdict on the remainder is acted on. A model that cannot explain the entries the game
        /// plainly does deliver has no business declaring the rest undeliverable.
        /// </summary>
        private const float MinModelAgreement = 0.9f;

        /// <summary>
        /// Checklist entries removed by hand rather than by the reachability checks below.
        ///
        /// Everything else this class drops is dropped because the game's own data proves it cannot
        /// be delivered. These are a judgement call about TFTV instead - reachable in principle, but
        /// not something a player can reasonably be asked for - so they are kept apart from the
        /// inferred prunes and each one carries its reason.
        /// </summary>
        private static readonly Dictionary<string, string> ManuallyExcludedResearchIds =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Gated behind a config option, so for most players it never appears at all.
                { "PX_MutagenHarvesting_ResearchDef", "config-dependent; most campaigns never reveal it" },
            };

        /// <summary>
        /// See <see cref="ManuallyExcludedResearchIds"/>.
        ///
        /// The turret magazines are here because TFTV does not currently let the player manufacture
        /// turret ammunition. If that ever comes back these three should come back onto the
        /// checklist with it.
        /// </summary>
        private static readonly Dictionary<string, string> ManuallyExcludedManufactureItems =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "NJ_PRCRTechTurretGun_AmmoClip_ItemDef", "turret ammo is not manufacturable in TFTV" },
                { "NJ_TechTurretGun_AmmoClip_ItemDef", "turret ammo is not manufacturable in TFTV" },
                { "PX_LaserTechTurretGun_AmmoClip_ItemDef", "turret ammo is not manufacturable in TFTV" },
            };

        private static readonly FieldInfo AchievementsField =
            AccessTools.Field(typeof(AchievementTracker), "_achievements");

        private static bool _reconciled;

        /// <summary>
        /// Prunes the checklist achievements to what this mod's defs can actually deliver.
        ///
        /// Has to run after every def change TFTV makes, and cannot simply edit the defs: the
        /// achievements are built in PhoenixGame.StartGame, on the very first frame, whereas mods
        /// are not loaded until a second or so later - so by the time this runs the tracker is
        /// already holding Achievement objects built from the unmodded lists, and they have to be
        /// rebuilt rather than merely corrected at the source.
        /// </summary>
        internal static void ReconcileWithModdedDefs()
        {
            try
            {
                if (_reconciled)
                {
                    return;
                }

                AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
                if (tracker == null)
                {
                    TFTVLogger.Always(LogPrefix + "No AchievementTracker yet; skipping reconciliation.");
                    return;
                }

                _reconciled = true;

                PruneResearchAllProjects(tracker);
                PruneManufactureAllItems(tracker);

                // Explicitly, not by way of the read postfix: mods load long after the tracker has
                // finished reading, so nothing else will bring these back this session. Every list
                // achievement, since the clearing branch is not choosy about which ones it hits.
                if (AchievementsField?.GetValue(tracker) is Dictionary<string, Achievement> all)
                {
                    foreach (Achievement achievement in all.Values.Where(IsShadowed))
                    {
                        int before = CountProgress(achievement);
                        SyncShadow(achievement);
                        int after = CountProgress(achievement);

                        if (after != before)
                        {
                            TFTVLogger.Always(LogPrefix + $"{achievement.Id}: {before} entries at load, {after} after restoring from our own copy.");
                        }
                    }
                }

                PushCompletedAchievementsToPlatform(tracker);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static readonly FieldInfo OldNormalizedProgressField =
            AccessTools.Field(typeof(Achievement), "_OldNormalizedProgress");

        /// <summary>
        /// Tells the platform about achievements that are already finished locally but still locked
        /// on its side.
        ///
        /// The tracker only ever pushes an achievement whose progress <em>changed</em> during the
        /// session:
        ///
        ///   bool num = item._OldNormalizedProgress != item.NormalizedProgress;
        ///
        /// Restoring a checklist - by pruning it to what is reachable, or by putting back a list the
        /// boot cleared - leaves those two equal, because the progress was read rather than earned.
        /// So a player can sit at a complete checklist that is never handed to Steam. Vanilla solves
        /// the same problem the same way, forcing a re-push by setting _OldNormalizedProgress to -1,
        /// and this borrows that.
        ///
        /// Deliberately limited to achievements that are Completed locally: this is the one place
        /// that can cause a Steam achievement to be awarded, so it must never fire on anything the
        /// player has not actually finished.
        /// </summary>
        private static void PushCompletedAchievementsToPlatform(AchievementTracker tracker)
        {
            try
            {
                if (!(AchievementsField?.GetValue(tracker) is Dictionary<string, Achievement> all)) return;

                PlatformAchievements platformAchievements = GameUtl.GameComponent<PlatformComponent>()
                    ?.Platform?.GetPlatformAchievements();

                if (platformAchievements?.Achievements == null) return;

                foreach (Achievement achievement in all.Values)
                {
                    if (achievement == null || !achievement.Completed) continue;

                    PlatformAchievements.Achievement onPlatform = platformAchievements.Achievements
                        .FirstOrDefault(a => a.AchievementData?.AchievementDef?.Id == achievement.Id);

                    if (onPlatform == null || onPlatform.ProgressPercent >= 100) continue;

                    // Makes the tracker see a change where there was none, so its own push runs.
                    OldNormalizedProgressField?.SetValue(achievement, -1);
                    tracker.StoreAchievementProgress(achievement);

                    TFTVLogger.Always(LogPrefix + $"{achievement.Id}: complete locally ({achievement.GetSimplifiedProgressText()}) " +
                        $"but the platform still reads {onPlatform.ProgressPercent}% - asking for it to be awarded.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #region Master Scientist - research all projects

        /// <summary>
        /// A research Id that no research DB carries any more cannot be completed by anyone: the
        /// faction's research list is built purely from ResearchDbDef.Researches, and the
        /// achievement is only ever fed a ResearchElement that came from that list. TFTV removes a
        /// number of vanilla researches from those DBs (the whole aircraft tree, among others), and
        /// each one left on the checklist is a permanent block.
        /// </summary>
        private static void PruneResearchAllProjects(AchievementTracker tracker)
        {
            try
            {
                StringListAchievementDef def = TFTVMain.Main.DefCache.GetDef<StringListAchievementDef>("ResearchAllProjects_AchievementDef");
                if (def?.Values == null)
                {
                    TFTVLogger.Always(LogPrefix + "ResearchAllProjects def or its Values missing; skipping.");
                    return;
                }

                HashSet<string> reachableIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (ResearchDbDef db in GameUtl.GameComponent<DefRepository>().GetAllDefs<ResearchDbDef>())
                {
                    if (db?.Researches == null) continue;

                    foreach (ResearchDef research in db.Researches)
                    {
                        if (research != null && !string.IsNullOrEmpty(research.Id))
                        {
                            reachableIds.Add(research.Id);
                        }
                    }
                }

                bool Obtainable(string id)
                {
                    return id != null && reachableIds.Contains(id) && !ManuallyExcludedResearchIds.ContainsKey(id);
                }

                string[] kept = def.Values.Where(Obtainable).ToArray();
                IEnumerable<string> dropped = def.Values.Select(id => id ?? "<null>").Where(id => !Obtainable(id));

                if (!ShouldApplyPruning(ResearchAllProjectsId, def.Values.Length, kept.Length, dropped))
                {
                    return;
                }

                def.Values = kept;
                RebuildAchievement(tracker, ResearchAllProjectsId, def);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #endregion

        #region Master Manufacturer - manufacture all items

        /// <summary>
        /// Drops items that no route can ever put in the manufacturing list.
        ///
        /// Two criteria, both needed. The first is a proof: every route in - a faction's starting
        /// items, a ManufactureResearchReward, the items a specialisation brings with it - gates on
        /// the ManufacturableTag, so an item that has lost it can never be built by anyone, and TFTV
        /// strips it from items outright (see TFTVMercenaries).
        ///
        /// The second walks those three routes and drops what none of them reach. That is a model of
        /// the game's bookkeeping rather than a proof, so it is only trusted when it has just
        /// demonstrated itself on the same data: see <see cref="MinModelAgreement"/>. On the live
        /// list it accounts for 175 of 177 entries, and the two it rejects are the legacy resistance
        /// vests whose unlocking researches TFTV deletes outright - the same two researches this
        /// mod's own checklist prune drops - so the two independent signals agree.
        /// </summary>
        private static void PruneManufactureAllItems(AchievementTracker tracker)
        {
            try
            {
                DefListAchievementDef def = TFTVMain.Main.DefCache.GetDef<DefListAchievementDef>("ManufactureAllItems_AchievementDef");
                if (def?.Values == null)
                {
                    TFTVLogger.Always(LogPrefix + "ManufactureAllItems def or its Values missing; skipping.");
                    return;
                }

                GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;
                HashSet<ItemDef> reachable = CollectManufacturableItems();

                bool StillTagged(BaseDef value)
                {
                    return value is ItemDef item && item.Tags != null && item.Tags.Contains(manufacturableTag);
                }

                // How much of the checklist the route model explains. Below the threshold the model
                // is not describing this game's data properly and only the tag proof is used.
                int taggedCount = def.Values.Count(StillTagged);
                int modelledCount = def.Values.Count(v => v is ItemDef item && reachable.Contains(item));
                float agreement = taggedCount > 0 ? (float)modelledCount / taggedCount : 0f;
                bool trustRouteModel = agreement >= MinModelAgreement;

                TFTVLogger.Always(LogPrefix + $"{ManufactureAllItemsId}: unlock-route model accounts for {modelledCount}/{taggedCount} " +
                    $"still-manufacturable entries ({agreement:P1}); {(trustRouteModel ? "using it" : "below threshold, using the tag check alone")}.");

                bool Obtainable(BaseDef value)
                {
                    if (!StillTagged(value)) return false;
                    if (value != null && ManuallyExcludedManufactureItems.ContainsKey(value.name)) return false;
                    return !trustRouteModel || reachable.Contains((ItemDef)value);
                }

                BaseDef[] kept = def.Values.Where(Obtainable).ToArray();
                IEnumerable<string> dropped = def.Values
                    .Where(v => !Obtainable(v))
                    .Select(v => v != null ? v.name : "<null>");

                if (!ShouldApplyPruning(ManufactureAllItemsId, def.Values.Length, kept.Length, dropped))
                {
                    return;
                }

                def.Values = kept;
                RebuildAchievement(tracker, ManufactureAllItemsId, def);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static HashSet<ItemDef> CollectManufacturableItems()
        {
            DefRepository repo = GameUtl.GameComponent<DefRepository>();
            GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;
            HashSet<ItemDef> reachable = new HashSet<ItemDef>();

            void AddIfManufacturable(ItemDef item)
            {
                if (item?.Tags != null && item.Tags.Contains(manufacturableTag))
                {
                    reachable.Add(item);
                }
            }

            // Whatever a faction starts able to build. Taken across every faction rather than just
            // Phoenix: erring towards keeping an entry costs nothing, dropping one wrongly does.
            foreach (GeoFactionDef faction in repo.GetAllDefs<GeoFactionDef>())
            {
                foreach (ItemDef item in faction.StartingManufacturableItems ?? Array.Empty<ItemDef>())
                {
                    AddIfManufacturable(item);
                }

                foreach (GeoFactionDef.DLCStartItems dlcItems in faction.AdditionalDLCItems ?? Array.Empty<GeoFactionDef.DLCStartItems>())
                {
                    foreach (ItemDef item in dlcItems?.StartingManufacturableItems ?? Array.Empty<ItemDef>())
                    {
                        AddIfManufacturable(item);
                    }
                }
            }

            // Whatever a research still in a DB unlocks.
            foreach (ResearchDbDef db in repo.GetAllDefs<ResearchDbDef>())
            {
                foreach (ResearchDef research in db?.Researches ?? new List<ResearchDef>())
                {
                    foreach (ResearchRewardDef reward in research?.Unlocks ?? Array.Empty<ResearchRewardDef>())
                    {
                        if (!(reward is ManufactureResearchRewardDef manufactureReward)) continue;

                        foreach (ItemDef item in manufactureReward.Items ?? Array.Empty<ItemDef>())
                        {
                            AddIfManufacturable(item);
                        }
                    }
                }
            }

            // Whatever arrives with a specialisation (ItemManufacturing.OnSpecializationAdded).
            HashSet<ClassTagDef> specTags = new HashSet<ClassTagDef>();
            foreach (SpecializationDef spec in repo.GetAllDefs<SpecializationDef>())
            {
                if (spec?.ClassTag != null)
                {
                    specTags.Add(spec.ClassTag);
                }
            }

            if (specTags.Count > 0)
            {
                foreach (ItemDef item in repo.GetAllDefs<ItemDef>())
                {
                    if (item?.Tags == null || !item.Tags.Contains(manufacturableTag)) continue;

                    foreach (ClassTagDef specTag in specTags)
                    {
                        if (item.Tags.Contains(specTag))
                        {
                            reachable.Add(item);
                            break;
                        }
                    }
                }
            }

            return reachable;
        }

        #endregion

        #region Capture all alien types

        /// <summary>
        /// Credits every class tag the captured Pandoran carries, not just its first one.
        ///
        /// GeoUnitDescriptor.ClassTag is GetClassTags().FirstOrDefault(), i.e. the first entry of
        /// the unit template's ClassTags, so a Pandoran whose checklist tag is not the first one it
        /// declares never registers however many times it is caught. AddNotContained ignores
        /// anything outside the checklist, so widening the match can only ever credit a capture the
        /// player genuinely made against a type the achievement already asks for.
        /// </summary>
        [HarmonyPatch(typeof(GeoAchievementTracker), "CaptureAllAlienTypesProgress")]
        internal static class GeoAchievementTracker_CaptureAllAlienTypesProgress_Patch
        {
            public static void Postfix(GeoUnitDescriptor captured)
            {
                try
                {
                    if (captured == null) return;

                    AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
                    DefListAchievement achievement = tracker?.GetAchievement<DefListAchievement>(CaptureAllAlienTypesId);
                    if (achievement == null || achievement.Completed) return;

                    bool added = false;
                    foreach (ClassTagDef classTag in captured.ClassTags)
                    {
                        if (classTag != null && achievement.AddNotContained(classTag))
                        {
                            added = true;
                            TFTVLogger.Always(LogPrefix + $"{CaptureAllAlienTypesId}: credited {classTag.name} (not the descriptor's primary tag).");
                        }
                    }

                    if (added)
                    {
                        tracker.StoreAchievementProgress(achievement);
                        TFTVLogger.Always(LogPrefix + $"{CaptureAllAlienTypesId}: now {achievement.GetSimplifiedProgressText()}.");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        #region Checklist dumps

        /// <summary>
        /// The full checklists and the template scan behind them - a few hundred lines, so on demand
        /// rather than at every boot. This is the command to reach for when an achievement is asking
        /// for something it should not, or when a Pandoran is not being credited.
        /// </summary>
        [ConsoleCommand(Command = "tftv_achievements_dump", Description = "Writes the full achievement checklists and capture-template scan to TFTV.log.")]
        public static void DumpChecklists(IConsole console)
        {
            AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
            if (tracker == null)
            {
                console.WriteLine("No AchievementTracker available.");
                return;
            }

            DumpResearchChecklist(tracker);
            DumpManufactureChecklist(tracker);
            DumpCaptureTemplates(tracker);
            console.WriteLine("Written to TFTV.log.");
        }

        /// <summary>
        /// Every research the achievement still asks for, by def name and by the name the player sees,
        /// so the list can be read against what a TFTV campaign is actually meant to contain.
        /// </summary>
        private static void DumpResearchChecklist(AchievementTracker tracker)
        {
            try
            {
                StringListAchievement achievement = tracker.GetAchievement<StringListAchievement>(ResearchAllProjectsId);
                if (achievement == null) return;

                Dictionary<string, ResearchDef> byId = new Dictionary<string, ResearchDef>(StringComparer.Ordinal);
                foreach (ResearchDef research in GameUtl.GameComponent<DefRepository>().GetAllDefs<ResearchDef>())
                {
                    if (research != null && !string.IsNullOrEmpty(research.Id) && !byId.ContainsKey(research.Id))
                    {
                        byId[research.Id] = research;
                    }
                }

                TFTVLogger.Always(LogPrefix + $"=== {ResearchAllProjectsId}: {achievement.AllowedValues.Count} required, {achievement.NumericalProgress} done ===");

                foreach (string id in achievement.AllowedValues.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                {
                    byId.TryGetValue(id, out ResearchDef research);
                    string uiName = SafeName(() => research?.ViewElementDef?.GetName());
                    string faction = research?.Faction != null ? research.Faction.name : "?";
                    string done = achievement.Progress.Contains(id) ? "x" : " ";

                    TFTVLogger.Always(LogPrefix + $"  [{done}] {id} | {uiName} | faction {faction}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Every item the achievement still asks the player to build, by def name and UI name.
        /// </summary>
        private static void DumpManufactureChecklist(AchievementTracker tracker)
        {
            try
            {
                DefListAchievement achievement = tracker.GetAchievement<DefListAchievement>(ManufactureAllItemsId);
                if (achievement == null) return;

                TFTVLogger.Always(LogPrefix + $"=== {ManufactureAllItemsId}: {achievement.AllowedValues.Count} required, {achievement.NumericalProgress} done ===");

                foreach (BaseDef value in achievement.AllowedValues.OrderBy(v => v != null ? v.name : string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    string uiName = SafeName(() => (value as ItemDef)?.GetDisplayName()?.Localize());
                    string done = achievement.Progress.Contains(value) ? "x" : " ";

                    TFTVLogger.Always(LogPrefix + $"  [{done}] {(value != null ? value.name : "<null>")} | {uiName}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Which character templates can actually satisfy the capture checklist.
        ///
        /// TacCharacterDef.ClassTags is not authored directly - it is Data.GameTags filtered to
        /// ClassTagDef and then cached - so a template whose GameTags carry no ClassTagDef ends up
        /// with an empty list, and GeoUnitDescriptor.ClassTag (its FirstOrDefault) is null. A
        /// Pandoran like that can be captured all day and register nothing, because AddNotContained
        /// is being handed null. This dump names every template in that state, and for each of the
        /// nine wanted tags lists the templates that carry it, flagging any where the tag is not
        /// first - the only position vanilla's tracking ever looks at.
        /// </summary>
        private static void DumpCaptureTemplates(AchievementTracker tracker)
        {
            try
            {
                DefListAchievement achievement = tracker.GetAchievement<DefListAchievement>(CaptureAllAlienTypesId);
                if (achievement == null) return;

                List<TacCharacterDef> templates = GameUtl.GameComponent<DefRepository>()
                    .GetAllDefs<TacCharacterDef>()
                    .Where(t => t != null)
                    .ToList();

                GameTagDef alienRaceTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AlienTag;

                TFTVLogger.Always(LogPrefix + $"=== {CaptureAllAlienTypesId}: scanning {templates.Count} TacCharacterDefs ===");

                // Templates with no ClassTagDef at all. For an alien one this is fatal to the
                // achievement: the captured descriptor's ClassTag comes out null.
                List<string> untagged = new List<string>();
                foreach (TacCharacterDef template in templates)
                {
                    List<ClassTagDef> classTags = SafeClassTags(template);
                    if (classTags.Count > 0) continue;

                    bool isAlien = alienRaceTag != null && template.Data?.GameTags != null && template.Data.GameTags.Contains(alienRaceTag);
                    untagged.Add($"{template.name}{(isAlien ? " (ALIEN)" : string.Empty)}");
                }

                TFTVLogger.Always(LogPrefix + $"Templates with no ClassTagDef at all ({untagged.Count}):");
                foreach (string name in untagged.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    TFTVLogger.Always(LogPrefix + "  " + name);
                }

                // Stems of the wanted tags ("Fireworm_ClassTagDef" -> "Fireworm"), used below to spot a
                // template wearing another creature's tag.
                Dictionary<ClassTagDef, string> wantedStems = new Dictionary<ClassTagDef, string>();
                foreach (BaseDef wanted in achievement.AllowedValues)
                {
                    if (wanted is ClassTagDef wantedTag)
                    {
                        wantedStems[wantedTag] = wantedTag.name.Replace("_ClassTagDef", string.Empty);
                    }
                }

                // For each wanted tag, who carries it and in what position.
                foreach (KeyValuePair<ClassTagDef, string> wanted in wantedStems)
                {
                    ClassTagDef wantedTag = wanted.Key;
                    string stem = wanted.Value;
                    string done = achievement.Progress.Contains(wantedTag) ? "x" : " ";
                    List<string> carriers = new List<string>();

                    foreach (TacCharacterDef template in templates)
                    {
                        List<ClassTagDef> classTags = SafeClassTags(template);
                        int index = classTags.IndexOf(wantedTag);
                        if (index < 0) continue;

                        List<string> notes = new List<string>();

                        // Only the first tag is what GeoUnitDescriptor.ClassTag returns, which is all
                        // vanilla's tracking ever looks at.
                        if (index != 0)
                        {
                            notes.Add($"tag at index {index}, vanilla tracking reads index 0 only");
                        }

                        // A template named after one creature but wearing another's tag: capturing it
                        // would credit the wrong type, and leave its own type uncreditable.
                        string impostorStem = wantedStems.Values.FirstOrDefault(other =>
                            !string.Equals(other, stem, StringComparison.OrdinalIgnoreCase)
                            && template.name.IndexOf(other, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (impostorStem != null)
                        {
                            notes.Add($"SUSPECT: name says {impostorStem}, tag says {stem}");
                        }

                        carriers.Add(notes.Count > 0
                            ? $"{template.name}  <-- {string.Join("; ", notes)}"
                            : template.name);
                    }

                    TFTVLogger.Always(LogPrefix + $"[{done}] {wantedTag.name}: {carriers.Count} template(s)");
                    foreach (string carrier in carriers.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                    {
                        TFTVLogger.Always(LogPrefix + "    " + carrier);
                    }
                }

                // The reverse view: templates named after a wanted creature that do not carry that
                // creature's tag at all. This is what a swapped or forgotten tag looks like from the
                // other side, and it catches the case where the tag went missing rather than moving.
                TFTVLogger.Always(LogPrefix + "Templates named after a wanted type but not carrying its tag:");
                foreach (TacCharacterDef template in templates.OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase))
                {
                    List<ClassTagDef> classTags = SafeClassTags(template);

                    foreach (KeyValuePair<ClassTagDef, string> wanted in wantedStems)
                    {
                        if (template.name.IndexOf(wanted.Value, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        if (classTags.Contains(wanted.Key)) continue;

                        string carried = classTags.Count > 0
                            ? string.Join(", ", classTags.Select(t => t.name))
                            : "<none>";

                        TFTVLogger.Always(LogPrefix + $"    {template.name}: expected {wanted.Key.name}, carries [{carried}]");
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static List<ClassTagDef> SafeClassTags(TacCharacterDef template)
        {
            try
            {
                return template.ClassTags?.ToList() ?? new List<ClassTagDef>();
            }
            catch
            {
                return new List<ClassTagDef>();
            }
        }

        /// <summary>
        /// Localisation can throw on a def with a half-built view element, and a dump that dies
        /// part-way is worse than one with a gap in it.
        /// </summary>
        private static string SafeName(Func<string> resolve)
        {
            try
            {
                string name = resolve();
                return string.IsNullOrEmpty(name) ? "<no name>" : name;
            }
            catch
            {
                return "<name failed>";
            }
        }

        #endregion

        #region Console command

        /// <summary>
        /// Reports what each checklist achievement is still waiting for, on demand.
        ///
        /// The boot-time dump runs when this mod loads, which is not necessarily after the tracker
        /// has finished pulling progress in: AchievementTracker only loads it once both the options
        /// store and the platform's own achievement list are ready, and Steam answers when it
        /// answers. Asking from the console removes that doubt - by the time anyone can type, every
        /// source has reported in.
        /// </summary>
        [ConsoleCommand(Command = "tftv_achievements", Description = "Lists what the checklist achievements are still missing.")]
        public static void PrintAchievementProgress(IConsole console)
        {
            try
            {
                AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
                if (tracker == null)
                {
                    console.WriteLine("No AchievementTracker available.");
                    return;
                }

                ReportList(console, tracker.GetAchievement<DefListAchievement>(CaptureAllAlienTypesId), CaptureAllAlienTypesId,
                    v => v != null ? v.name : "<null>");

                ReportList(console, tracker.GetAchievement<StringListAchievement>(ResearchAllProjectsId), ResearchAllProjectsId,
                    v => v ?? "<null>");

                ReportList(console, tracker.GetAchievement<DefListAchievement>(ManufactureAllItemsId), ManufactureAllItemsId,
                    v => v != null ? v.name : "<null>");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                console.WriteLine("Failed - see TFTV.log.");
            }
        }

        /// <summary>
        /// What the platform layer thinks of every achievement: which Steam achievement each def is
        /// bound to, which stat backs its progress bar, and the percentage Steam is reporting back.
        ///
        /// Two questions this answers. First, whether "cannot set stat 'stat_ach_35'" is one broken
        /// stat or all of them - a single bad entry is an app-configuration mistake on Snapshot's
        /// side, whereas every stat failing points at stats not having been received yet. Second,
        /// what ProgressPercent each achievement reports, because that is the value the tracker
        /// compares against local progress on load, and the branch it picks is what decides whether a
        /// checklist survives the boot.
        /// </summary>
        [ConsoleCommand(Command = "tftv_steam_achievements", Description = "Dumps how each achievement is wired to Steam and what Steam reports for it.")]
        public static void PrintSteamAchievementWiring(IConsole console)
        {
            try
            {
                PlatformComponent platform = GameUtl.GameComponent<PlatformComponent>();
                List<AchievementDef> defs = platform?.PlatformComponentDef?.Achievements;
                if (defs == null)
                {
                    console.WriteLine("No achievement defs available.");
                    return;
                }

                PlatformAchievements platformAchievements = platform.Platform?.GetPlatformAchievements();
                List<PlatformAchievements.Achievement> known = platformAchievements?.Achievements;

                Report(console, $"Platform achievements: {(platformAchievements == null ? "<none - achievements are local only>" : platformAchievements.GetType().Name)}, " +
                    $"{(known == null ? "not yet received" : known.Count + " received")}, " +
                    $"HasPlatformProgress={platformAchievements?.HasPlatformProgress()}, UseLocalStorage={platformAchievements?.UseLocalStorage()}");

                AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();

                foreach (AchievementDef def in defs.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
                {
                    PlatformAchievements.Achievement platformAchievement = known?
                        .FirstOrDefault(a => a.AchievementData?.AchievementDef == def);

                    Achievement local = tracker?.GetAchievement(def.Id);

                    string statId = string.IsNullOrEmpty(def.SteamProgressStatId) ? "<no progress stat>" : def.SteamProgressStatId;
                    string steamSide = platformAchievement == null
                        ? "not matched to any Steam achievement"
                        : $"steam {platformAchievement.ProgressPercent}%";
                    string localSide = local == null ? "no local achievement" : $"local {local.NormalizedProgress}%";

                    // The comparison the tracker makes on load. "steam ahead" is the case that clears
                    // a checklist, so anything listed that way is losing its progress every boot.
                    string verdict = platformAchievement != null && local != null
                        ? (platformAchievement.ProgressPercent > local.NormalizedProgress
                            ? "  <-- STEAM AHEAD: this one gets cleared on load"
                            : string.Empty)
                        : string.Empty;

                    Report(console, $"{def.Id} | steamId {def.SteamId} | stat {statId} | step {def.SteamProgressNotifyStep} | {steamSide} | {localSide}{verdict}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                console.WriteLine("Failed - see TFTV.log.");
            }
        }

        private static void ReportList<TItem>(IConsole console, ListAchievement<TItem> achievement, string id, Func<TItem, string> describe)
            where TItem : class
        {
            if (achievement == null)
            {
                Report(console, id + ": not found.");
                return;
            }

            List<string> missing = achievement.AllowedValues
                .Where(v => !achievement.Progress.Contains(v))
                .Select(describe)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Report(console, $"{id}: {achievement.GetSimplifiedProgressText()}" +
                $"{(achievement.Completed ? " (complete)" : string.Empty)}");

            if (missing.Count > 0)
            {
                Report(console, $"  missing: {string.Join(", ", missing)}");
            }

            // The credited side matters too: a type credited twice under one tag, or a tag credited
            // that no Pandoran should carry, both show up here and nowhere else.
            List<string> have = achievement.Progress
                .Select(describe)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Report(console, $"  credited: {(have.Count > 0 ? string.Join(", ", have) : "<nothing>")}");
        }

        private static void Report(IConsole console, string line)
        {
            console.WriteLine(line);
            TFTVLogger.Always(LogPrefix + line);
        }

        #endregion

        #region Shared plumbing

        private static bool ShouldApplyPruning(string achievementId, int originalCount, int keptCount, IEnumerable<string> dropped)
        {
            List<string> droppedNames = dropped.ToList();

            TFTVLogger.Always(LogPrefix + $"{achievementId}: {originalCount} entries, {keptCount} reachable, {droppedNames.Count} unreachable.");

            if (droppedNames.Count == 0)
            {
                return false;
            }

            TFTVLogger.Always(LogPrefix + $"{achievementId}: unreachable entries [{string.Join(", ", droppedNames)}]");

            if (keptCount == 0)
            {
                TFTVLogger.Always(LogPrefix + $"{achievementId}: reachability check found nothing at all - leaving the achievement untouched.");
                return false;
            }

            if (originalCount > 0 && (float)droppedNames.Count / originalCount > MaxPrunedFraction)
            {
                TFTVLogger.Always(LogPrefix + $"{achievementId}: would drop over {MaxPrunedFraction:P0} of the list, which reads as a broken check rather than modded data - leaving the achievement untouched.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Swaps in an achievement built from the corrected def and restores the player's stored
        /// progress onto it.
        /// </summary>
        private static void RebuildAchievement(AchievementTracker tracker, string achievementId, AchievementDef def)
        {
            try
            {
                if (!(AchievementsField?.GetValue(tracker) is IDictionary achievements))
                {
                    TFTVLogger.Always(LogPrefix + $"{achievementId}: cannot reach the tracker's achievement list; the def was corrected but the live achievement was not rebuilt.");
                    return;
                }

                Achievement rebuilt = def.CreateAchievement();
                achievements[achievementId] = rebuilt;

                tracker.ReadAchievemntValue(rebuilt);
                TrimProgressToAllowedValues(rebuilt);

                TFTVLogger.Always(LogPrefix + $"{achievementId}: rebuilt, progress now {rebuilt.GetSimplifiedProgressText()}.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Drops stored progress entries that are no longer on the checklist, and any duplicates.
        ///
        /// ListAchievement.Completed is an exact count match, so a save carrying progress for an
        /// entry that has since been pruned would push the count past the list's length and lock
        /// the achievement out just as thoroughly as the unreachable entry did.
        /// </summary>
        private static void TrimProgressToAllowedValues(Achievement achievement)
        {
            switch (achievement)
            {
                case StringListAchievement stringList:
                    Trim(stringList.Progress, new HashSet<string>(stringList.AllowedValues, StringComparer.Ordinal));
                    break;
                case DefListAchievement defList:
                    Trim(defList.Progress, new HashSet<BaseDef>(defList.AllowedValues));
                    break;
            }
        }

        private static void Trim<T>(List<T> progress, HashSet<T> allowed)
        {
            if (progress == null) return;

            HashSet<T> seen = new HashSet<T>();
            progress.RemoveAll(item => item == null || !allowed.Contains(item) || !seen.Add(item));
        }

        /// <summary>
        /// The tracker reloads stored progress whenever the platform hands its achievements over,
        /// which can land after the checklists have been pruned - so the same trim has to be applied
        /// on every load, not just the one this mod performs itself.
        /// </summary>
        [HarmonyPatch(typeof(AchievementTracker), nameof(AchievementTracker.ReadAchievemntValue))]
        internal static class AchievementTracker_ReadAchievemntValue_Patch
        {
            public static void Postfix(Achievement achievement)
            {
                try
                {
                    if (achievement == null) return;

                    TrimProgressToAllowedValues(achievement);
                    SyncShadow(achievement);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Where the tracker reconciles the stored list against the platform's percentage - and where
        /// a list achievement's progress can be destroyed:
        ///
        ///   if (platform.ProgressPercent > value.NormalizedProgress)
        ///       value.NormalizedProgress = (int)platform.ProgressPercent;
        ///
        /// ListAchievement's setter has no way to turn a percentage back into "which entries", so
        /// for anything short of 100 it simply clears the list. Steam only ever accepts an increase,
        /// so from the outside the percentage stands still while the local checklist has silently
        /// restarted. Restoring afterwards from our own copy is what keeps a cold start from losing
        /// everything the player has collected.
        /// </summary>
        [HarmonyPatch(typeof(AchievementTracker), "LoadAchievemntsProgress")]
        internal static class AchievementTracker_LoadAchievemntsProgress_Patch
        {
            // Reads the field through the FieldInfo this class already holds rather than through
            // Harmony's "___field" injection: the field is itself named with a leading underscore,
            // so the injected parameter would need four of them, and getting that count wrong is a
            // patch-time exception that takes the whole mod down with it.
            public static void Postfix(AchievementTracker __instance)
            {
                try
                {
                    if (!(AchievementsField?.GetValue(__instance) is Dictionary<string, Achievement> achievements)) return;

                    foreach (Achievement achievement in achievements.Values)
                    {
                        if (!IsShadowed(achievement)) continue;

                        int before = CountProgress(achievement);
                        SyncShadow(achievement);
                        int after = CountProgress(achievement);

                        if (after != before)
                        {
                            TFTVLogger.Always(LogPrefix + $"{achievement.Id}: platform reconcile left {before} entries, {after} after restoring from our own copy.");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        #region Recovering capture progress from the campaign's own records

        /// <summary>
        /// Credits every Pandoran type this campaign's statistics say has been captured.
        ///
        /// PhoenixStatisticsManager keeps CapturedAliens as a ClassTagDef-keyed tally, and unlike the
        /// achievement's own progress that lives in the savegame - so it survives the restart that
        /// loses the achievement list, and it is a record of captures the player genuinely made.
        /// Seeding from it recovers a campaign's worth of captures that would otherwise have to be
        /// done again, and since it only ever adds tags the checklist already asks for, it cannot
        /// credit anything unearned.
        ///
        /// The statistics are per-campaign while the achievement is game-wide, so this only ever adds
        /// to the total; nothing is removed on loading a different save. Loading a save therefore
        /// gives the player their own copy's running total first and this campaign's captures on top
        /// of it, which is what keeps the achievement's "not necessarily in the same playthrough"
        /// promise: the campaign statistics are a backfill for history the wipe destroyed, not the
        /// mechanism that carries progress forward.
        /// </summary>
        internal static void SeedCaptureProgressFromCampaignStats()
        {
            try
            {
                AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
                DefListAchievement achievement = tracker?.GetAchievement<DefListAchievement>(CaptureAllAlienTypesId);
                if (achievement == null || achievement.Completed) return;

                // First, everything credited in any earlier playthrough. The achievement is game-wide
                // and its own store is wiped on each boot, so this is what carries a capture made in
                // one campaign into the next - without it, loading a save would leave the player with
                // only that campaign's captures, and the checklist would in effect have to be
                // completed in a single playthrough.
                SyncShadow(achievement);

                Dictionary<ClassTagDef, int> captured = GameUtl.GameComponent<PhoenixStatisticsManager>()
                    ?.CurrentGameStats?.GeoscapeStats?.CapturedAliens;

                if (captured == null || captured.Count == 0)
                {
                    return;
                }

                List<string> credited = new List<string>();
                foreach (KeyValuePair<ClassTagDef, int> entry in captured)
                {
                    if (entry.Key != null && achievement.AddNotContained(entry.Key))
                    {
                        credited.Add($"{entry.Key.name} (x{entry.Value})");
                    }
                }

                if (credited.Count > 0)
                {
                    tracker.StoreAchievementProgress(achievement);
                    TFTVLogger.Always(LogPrefix + $"{CaptureAllAlienTypesId}: recovered [{string.Join(", ", credited)}] from this campaign's statistics; now {achievement.GetSimplifiedProgressText()}.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Seeds capture progress the moment the campaign's statistics are restored.
        ///
        /// OnLevelStart is too early - the statistics arrive with the savegame through this method,
        /// so asking before it has run finds an empty tally. This is the point at which the campaign's
        /// own record of what has been captured actually exists.
        /// </summary>
        [HarmonyPatch(typeof(PhoenixStatisticsManager), nameof(PhoenixStatisticsManager.SetStatistics))]
        internal static class PhoenixStatisticsManager_SetStatistics_Patch
        {
            public static void Postfix()
            {
                SeedCaptureProgressFromCampaignStats();
            }
        }

        #endregion

        #region Keeping def-list progress across restarts

        private const string ShadowKeyPrefix = "TFTV_AchievementDefNames_";

        private static readonly FieldInfo GameValueStoreField =
            AccessTools.Field(typeof(AchievementTracker), "_gameValueStore");

        /// <summary>
        /// Every checklist achievement, not just this mod's three.
        ///
        /// The clearing branch in LoadAchievemntsProgress hits any ListAchievement whose platform
        /// percentage has run ahead of the local list, and on this profile that is AllClassCombo and
        /// UseAllAbilities as well - both plain vanilla achievements losing their progress on every
        /// boot for the same reason. Keeping a copy of all of them costs nothing and fixes those too.
        /// </summary>
        private static bool IsShadowed(Achievement achievement)
        {
            return achievement is DefListAchievement || achievement is StringListAchievement;
        }

        private static bool IsShadowed(string achievementId)
        {
            AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
            return IsShadowed(tracker?.GetAchievement(achievementId));
        }

        private static NamedValueStore GetValueStore()
        {
            AchievementTracker tracker = GameUtl.GameComponent<AchievementTracker>();
            return tracker != null ? GameValueStoreField?.GetValue(tracker) as NamedValueStore : null;
        }

        /// <summary>
        /// Keeps our own copy of a def-list achievement's progress in step with the tracker's, in
        /// both directions and without either side ever losing an entry.
        ///
        /// The copy is a list of plain def names, not def references, on purpose: the research
        /// checklist stores strings and comes back intact across a restart, while both def-list
        /// checklists come back empty, so strings are the format demonstrably surviving this store.
        ///
        /// A union rather than an overwrite, so that whichever side happens to be read first is not
        /// the one that wins. On the first run after this code is added the copy is empty and the
        /// tracker's list is what seeds it; on a later cold start the tracker's list is the empty one
        /// and the copy puts it back. Progress in these achievements is only ever earned, never
        /// spent, so a union can never be the wrong answer.
        /// </summary>
        private static void SyncShadow(Achievement achievement)
        {
            try
            {
                if (!IsShadowed(achievement)) return;

                NamedValueStore store = GetValueStore();
                if (store == null) return;

                string key = ShadowKeyPrefix + achievement.Id;

                HashSet<string> union = store.GetValue(key) is IEnumerable<string> stored
                    ? new HashSet<string>(stored, StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);

                switch (achievement)
                {
                    case DefListAchievement defList:
                        foreach (BaseDef earned in defList.Progress)
                        {
                            if (earned != null) union.Add(earned.name);
                        }

                        if (union.Count == 0) return;
                        store.SetValue(key, union.ToList());

                        // Back into the achievement, filtered through the checklist, so a name the
                        // checklist no longer carries cannot creep in and push the count past the
                        // exact match that completion requires.
                        foreach (BaseDef allowed in defList.AllowedValues)
                        {
                            if (allowed != null && union.Contains(allowed.name)) defList.AddNotContained(allowed);
                        }
                        break;

                    case StringListAchievement stringList:
                        foreach (string earned in stringList.Progress)
                        {
                            if (earned != null) union.Add(earned);
                        }

                        if (union.Count == 0) return;
                        store.SetValue(key, union.ToList());

                        foreach (string allowed in stringList.AllowedValues)
                        {
                            if (allowed != null && union.Contains(allowed)) stringList.AddNotContained(allowed);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Every route that records progress ends here, so this is the one place our copy has to be
        /// kept in step with the tracker's.
        /// </summary>
        [HarmonyPatch(typeof(AchievementTracker), nameof(AchievementTracker.StoreAchievementProgress))]
        internal static class AchievementTracker_StoreAchievementProgress_Patch
        {
            public static void Postfix(Achievement achievement)
            {
                SyncShadow(achievement);
            }
        }

        private static int CountProgress(Achievement achievement)
        {
            switch (achievement)
            {
                case StringListAchievement stringList: return stringList.Progress?.Count ?? -1;
                case DefListAchievement defList: return defList.Progress?.Count ?? -1;
                default: return -1;
            }
        }

        #endregion
    }
}
