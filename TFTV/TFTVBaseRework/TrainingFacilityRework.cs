using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVBaseRework
{
    // Made public so RecruitTrainingSessionSave is fully public (resolves CS0052 when referenced by public save fields).
    public static class TrainingFacilityRework
    {
        private const int BaseDurationDays = 6;
        private const int BaseTargetLevel = 4;
        private const int UpgradedTargetLevel = 5;
        private static readonly System.Collections.Generic.Dictionary<string, float> DurationReductionResearch =
            new System.Collections.Generic.Dictionary<string, float>
            {
                { "NJ_Training_ResearchDef", 0.15f },
                { "PX_AntediluvianArchaeology_ResearchDef", 0.10f },
            };
        private const string AdvancedLevelResearchId = "PX_EliteTraining_ResearchDef";

        public static int GetEffectiveDurationDays(GeoPhoenixFaction faction)
        {
            float days = BaseDurationDays;
            if (faction?.Research != null)
            {
                foreach (var kv in DurationReductionResearch)
                    if (faction.Research.HasCompleted(kv.Key))
                        days *= (1f - kv.Value);
            }
            return System.Math.Max(1, (int)System.Math.Ceiling(days));
        }

        public static int GetTargetLevel(GeoPhoenixFaction faction) =>
            faction?.Research?.HasCompleted(AdvancedLevelResearchId) == true ? UpgradedTargetLevel : BaseTargetLevel;

        public static System.Collections.Generic.IReadOnlyList<SpecializationDef> GetAvailableTrainingSpecializations(GeoPhoenixFaction faction)
        {
            var list = new System.Collections.Generic.List<SpecializationDef>();
            if (faction?.GeoLevel == null) return list;
            var dc = TFTVMain.Main.DefCache;

            void TryAdd(string id)
            {
                try { var def = dc.GetDef<SpecializationDef>(id); if (def != null && !list.Contains(def)) list.Add(def); }
                catch { }
            }

            // Always available core classes
            TryAdd("AssaultSpecializationDef");
            TryAdd("HeavySpecializationDef");
            TryAdd("SniperSpecializationDef");

            var r = faction.Research;
            if (r != null)
            {
                if (r.HasCompleted("SYN_InfiltratorTech_ResearchDef")) TryAdd("InfiltratorSpecializationDef");
                if (r.HasCompleted("ANU_AnuPriest_ResearchDef")) TryAdd("PriestSpecializationDef");
                if (r.HasCompleted("ANU_Berserker_ResearchDef")) TryAdd("BerserkerSpecializationDef");
                if (r.HasCompleted("NJ_Technician_ResearchDef")) TryAdd("TechnicianSpecializationDef");
            }
            return list;
        }
    }
}