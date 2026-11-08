using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVVoidOmensExpansion
{
    /// <summary>Which missions a Void Omen matters in, for the tactical flag and the ODI widget.</summary>
    internal enum VoidOmenTacticalScope
    {
        None,
        Always,
        PandoransPresent,
        HavenDefense
    }

    /// <summary>One Void Omen: when it can roll, what it clashes with, and what it does to defs.</summary>
    internal sealed class VoidOmenEntry
    {
        internal int Id;

        /// <summary>The system this omen needs exists in the campaign. Null means always.</summary>
        internal Func<GeoLevelController, bool> Gate;

        /// <summary>Earlier rolls this campaign before it enters the pool.</summary>
        internal int MinEarlierRolls;

        /// <summary>Never active together: if one of these is in play, this one is left out of the pool.</summary>
        internal int[] ExcludedWith = new int[0];

        internal VoidOmenTacticalScope TacticalScope = VoidOmenTacticalScope.None;

        /// <summary>The omen is in play but switched off by a config option, for the tactical flag only.</summary>
        internal Func<bool> ConfigAllows;

        /// <summary>Runs on every ImplementVoidOmens while in play, so it must be safe to repeat.</summary>
        internal Action<GeoLevelController> Apply;

        /// <summary>Runs once when the omen leaves play.</summary>
        internal Action<GeoLevelController> Revert;
    }

    /// <summary>
    /// The one table of Void Omens. The pool, ImplementVoidOmens, the tactical flags and the ODI widget all read it,
    /// so a new omen is one entry here plus its patches. Omens 38-40 are free: the flags and events already reach them.
    /// </summary>
    internal static class VoidOmenCatalog
    {
        /// <summary>Highest id the flag array and the events have room for.</summary>
        internal const int HighestId = 40;

        internal static readonly List<VoidOmenEntry> Entries = Build();

        internal static VoidOmenEntry Get(int id) => Entries.FirstOrDefault(e => e.Id == id);

        private static bool Config(Func<TFTVConfig, bool> option) => option(TFTVMain.Main.Config);

        private static List<VoidOmenEntry> Build()
        {
            return new List<VoidOmenEntry>
            {
                new VoidOmenEntry { Id = 1, Apply = TFTVVoidOmens.ApplyVO1, Revert = TFTVVoidOmens.RevertVO1 },
                new VoidOmenEntry { Id = 2, ExcludedWith = new[] { 27 }, Apply = TFTVVoidOmens.ApplyVO2, Revert = TFTVVoidOmens.RevertVO2 },
                new VoidOmenEntry { Id = 3, MinEarlierRolls = 8, TacticalScope = VoidOmenTacticalScope.Always },
                new VoidOmenEntry { Id = 4, Gate = _ => Config(c => c.LimitedDeploymentVO) && !TFTVAircraftReworkMain.AircraftReworkOn },
                new VoidOmenEntry { Id = 5, TacticalScope = VoidOmenTacticalScope.HavenDefense, Apply = TFTVVoidOmens.ApplyVO5, Revert = TFTVVoidOmens.RevertVO5 },
                new VoidOmenEntry { Id = 6, Apply = TFTVVoidOmens.ApplyVO6, Revert = TFTVVoidOmens.RevertVO6 },
                new VoidOmenEntry
                {
                    Id = 7,
                    Gate = _ => Config(c => c.MoreMistVO),
                    ExcludedWith = new[] { 21 },
                    TacticalScope = VoidOmenTacticalScope.Always,
                    ConfigAllows = () => Config(c => c.MoreMistVO)
                },
                new VoidOmenEntry { Id = 8, Apply = TFTVVoidOmens.ApplyVO8, Revert = TFTVVoidOmens.RevertVO8 },
                new VoidOmenEntry { Id = 9, Apply = TFTVVoidOmens.ApplyVO9, Revert = TFTVVoidOmens.RevertVO9 },
                // only add unlimited Delirium if max Delirium is not reached
                new VoidOmenEntry { Id = 10, Gate = c => c.EventSystem.GetVariable("BC_SDI") * 5 < 30, TacticalScope = VoidOmenTacticalScope.Always },
                new VoidOmenEntry
                {
                    Id = 11,
                    Gate = c => c.EventSystem.GetVariable("BehemothEggHatched") == 1 && c.AlienFaction.Behemoth != null,
                    Apply = TFTVVoidOmens.ApplyVO11,
                    Revert = TFTVVoidOmens.RevertVO11
                },
                new VoidOmenEntry { Id = 12 },
                new VoidOmenEntry { Id = 13, Apply = TFTVVoidOmens.ApplyVO13, Revert = TFTVVoidOmens.RevertVO13 },
                new VoidOmenEntry { Id = 14, TacticalScope = VoidOmenTacticalScope.Always, Apply = TFTVVoidOmens.ApplyVO14, Revert = TFTVVoidOmens.RevertVO14 },
                new VoidOmenEntry { Id = 15, Gate = UmbraResearched, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 16, Gate = UmbraResearched, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 17, Gate = c => c.EventSystem.GetVariable("Infestation_Encounter_Variable") == 1 },
                new VoidOmenEntry { Id = 18 },
                new VoidOmenEntry
                {
                    Id = 19,
                    MinEarlierRolls = 5,
                    Gate = _ => TFTVRevenant.DeadSoldiersDelirium.Keys.Count == 0,
                    TacticalScope = VoidOmenTacticalScope.PandoransPresent
                },

                new VoidOmenEntry { Id = 20, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 21, ExcludedWith = new[] { 7 }, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 22, Gate = _ => TFTVAircraftReworkMain.AircraftReworkOn },
                new VoidOmenEntry { Id = 23, Apply = GeoscapeOmens.ApplyVO23, Revert = GeoscapeOmens.RevertVO23 },
                new VoidOmenEntry { Id = 24, Gate = GeoscapeOmens.ContainmentGate },
                new VoidOmenEntry { Id = 25 },
                new VoidOmenEntry { Id = 26, Gate = WorldOmens.AtWarGate },
                new VoidOmenEntry { Id = 27, ExcludedWith = new[] { 2 }, Apply = WorldOmens.ApplyVO27, Revert = WorldOmens.RevertVO27 },
                new VoidOmenEntry { Id = 28, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 29, Gate = WorldOmens.AlignedGate },
                new VoidOmenEntry { Id = 30, Gate = _ => TFTVRevenant.DeadSoldiersDelirium.Keys.Count >= 2, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
                new VoidOmenEntry { Id = 31 },
                new VoidOmenEntry { Id = 32, TacticalScope = VoidOmenTacticalScope.Always },
                new VoidOmenEntry { Id = 33, Gate = GeoscapeOmens.AugmentGate },
                new VoidOmenEntry { Id = 34, Gate = _ => TFTVAircraftReworkMain.AircraftReworkOn },
                new VoidOmenEntry { Id = 35 },
                new VoidOmenEntry { Id = 36, Gate = TacticalOmens.DeliriumPerkGate, TacticalScope = VoidOmenTacticalScope.Always },
                new VoidOmenEntry { Id = 37, TacticalScope = VoidOmenTacticalScope.PandoransPresent },
            };
        }

        private static bool UmbraResearched(GeoLevelController controller)
        {
            return controller.AlienFaction.Research.HasCompleted("ALN_CrabmanUmbra_ResearchDef");
        }

        /// <summary>The omens that can roll now: gates and earlier rolls met, not in play, no clash with one in play.</summary>
        internal static List<int> GetEligible(GeoLevelController controller)
        {
            int earlierRolls = TFTVVoidOmens.CheckForAlreadyRolledVoidOmens(controller).Count;
            int[] inPlay = TFTVVoidOmens.CheckFordVoidOmensInPlay(controller);

            return Entries
                .Where(e => (e.Gate == null || e.Gate(controller)) && earlierRolls >= e.MinEarlierRolls)
                .Where(e => !inPlay.Contains(e.Id) && !e.ExcludedWith.Any(inPlay.Contains))
                .Where(e => !IsBanned(controller, e.Id))
                .Select(e => e.Id)
                .ToList();
        }

        /// <summary>
        /// Omens the player took out of this playthrough for good (console void_omen_remove ... ban). Kept as geoscape
        /// event variables, so they are saved with the campaign; void_omen_force lifts the ban.
        /// </summary>
        private const string BannedVariable = "TFTV_VoidOmenBanned_";

        internal static bool IsBanned(GeoLevelController controller, int id)
        {
            return controller?.EventSystem != null && controller.EventSystem.GetVariable(BannedVariable + id) > 0;
        }

        internal static void SetBanned(GeoLevelController controller, int id, bool banned)
        {
            controller.EventSystem.SetVariable(BannedVariable + id, banned ? 1 : 0);
        }

        /// <summary>The omens the ODI widget lists on this mission.</summary>
        internal static IEnumerable<int> GetForMission(bool pandoransPresent, bool havenDefense)
        {
            return Entries
                .Where(e => e.TacticalScope == VoidOmenTacticalScope.Always
                    || (e.TacticalScope == VoidOmenTacticalScope.PandoransPresent && pandoransPresent)
                    || (e.TacticalScope == VoidOmenTacticalScope.HavenDefense && havenDefense))
                .Select(e => e.Id);
        }
    }
}
