using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static TFTV.TFTVBaseRework.Workers;

namespace TFTV.TFTVBaseRework
{
    public enum PersonnelAssignment
    {
        Unassigned,
        Research,
        Manufacturing,
        Training
    }

    internal class PersonnelInfo
    {
        public int Id;
        public GeoCharacter Character;
        public PersonnelAssignment Assignment;
        public SpecializationDef TrainingSpec;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class PersonnelAssignmentSave
    {
        public int GeoUnitId;
        public string MainSpecName;
        public PersonnelAssignment Assignment;
    }

    internal static class PersonnelData
    {
        private const string LogPrefix = "[PersonnelData]";

        private static readonly Dictionary<int, PersonnelInfo> _assignments = new Dictionary<int, PersonnelInfo>();
        public static Dictionary<int, PersonnelInfo> Assignments => _assignments;

        internal static void ClearAssignments()
        {
            _assignments.Clear();
            TFTVLogger.Always("[PersonnelData] Cleared assignments and personnel pool.");
        }

        private static PersonnelInfo FindPersonnel(GeoCharacter character)
        {
            if (character == null) return null;
            _assignments.TryGetValue(character.Id, out var info);
            return info;
        }

        private static PersonnelInfo FindPersonnel(int unitId)
        {
            if (unitId <= 0) return null;
            _assignments.TryGetValue(unitId, out var info);
            return info;
        }

        private static PersonnelInfo CreatePersonnelRecord(GeoCharacter character)
        {
            var info = new PersonnelInfo
            {
                Id = character.Id,
                Character = character,
                Assignment = PersonnelAssignment.Unassigned,
            };

            if (character != null && !_assignments.ContainsKey(character.Id))
            {
                _assignments[character.Id] = info;
            }

            return info;
        }

        internal static int GetOrCreatePersonnelId(GeoCharacter character)
        {
            var info = FindPersonnel(character) ?? CreatePersonnelRecord(character);
            return info.Id;
        }


        internal static PersonnelInfo GetPersonnelByUnitId(int unitId)
        {
            return _assignments.TryGetValue(unitId, out var info) ? info : null;
        }




        private static GeoCharacter CreateHiddenCharacterFromDescriptor(GeoLevelController level, GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            try
            {
                GeoPhoenixBase targetBase = faction?.Bases?.FirstOrDefault();
                GeoSite site = targetBase?.Site;
                if (level == null || faction == null || descriptor == null || site == null)
                {
                    return null;
                }

                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null)
                {
                    return null;
                }

                GeoCharacterFilter.HiddenOperativeTagFilter.ApplyHiddenTag(character);
                character.LevelProgression?.SetLevel(1);
                faction.AddRecruit(character, site);
                return character;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }

        }

        private static void AttachCharacter(GeoCharacter character)
        {
            if (character == null) return;

            var info = FindPersonnel(character) ?? FindPersonnel(character.Id) ?? CreatePersonnelRecord(character);
            info.Character = character;
            info.Id = character.Id;
            if (!_assignments.ContainsKey(character.Id))
            {
                _assignments[character.Id] = info;
            }
        }

        internal static void RemovePersonnel(GeoPhoenixFaction faction, PersonnelInfo person)
        {
            if (person == null) return;

            if (person.Assignment == PersonnelAssignment.Research || person.Assignment == PersonnelAssignment.Manufacturing)
            {
                ReleaseWorkSlotIfNeeded(faction, person.Assignment);
            }

            _assignments.Remove(person.Id);
        }

        internal static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            var faction = level?.PhoenixFaction;
            if (faction == null) return new List<SpecializationDef>();
            return TrainingFacilityRework.GetAvailableTrainingSpecializations(faction).ToList();
        }

        internal static void AssignWorker(PersonnelInfo person, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (person?.Character == null || faction == null) return;
            ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            PersonnelAssignment desired = slotType == FacilitySlotType.Research
                ? PersonnelAssignment.Research
                : PersonnelAssignment.Manufacturing;

            if (person.Assignment == desired) return;
            var previous = person.Assignment;

            bool slotAdded = ResearchManufacturingSlotsManager.IncrementUsedSlot(faction, slotType);
            if (!slotAdded)
            {
                TFTVLogger.Always($"{LogPrefix} No free {slotType} slots available (used >= provided).");
                return;
            }

            ReleaseWorkSlotIfNeeded(faction, previous);
            person.Assignment = desired;

            GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
            UIModuleInfoBar infoBar = level.View.GeoscapeModules.ResourcesModule;
            var update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
            update.Invoke(infoBar, new object[] { faction, false });
        }

        private static void ReleaseWorkSlotIfNeeded(GeoPhoenixFaction faction, PersonnelAssignment assignment)
        {
            if (faction == null) return;
            switch (assignment)
            {
                case PersonnelAssignment.Research:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(faction, FacilitySlotType.Research);
                    break;
                case PersonnelAssignment.Manufacturing:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(faction, FacilitySlotType.Manufacturing);
                    break;
            }
        }

        internal static List<PersonnelAssignmentSave> CreateAssignmentsSnapshot()
        {
            var list = new List<PersonnelAssignmentSave>();
            foreach (var pi in _assignments.Values)
            {
                list.Add(new PersonnelAssignmentSave
                {
                    GeoUnitId = pi.Id,
                    MainSpecName = pi.TrainingSpec?.name ?? pi.Character?.Progression?.MainSpecDef?.name,
                    Assignment = pi.Assignment,

                });
            }
            return list;
        }

        internal static void LoadAssignmentsSnapshot(GeoLevelController level, IEnumerable<PersonnelAssignmentSave> snapshot)
        {
            try
            {
                if (level?.PhoenixFaction == null || snapshot == null) return;
                var phoenix = level.PhoenixFaction;

                TFTVLogger.Always($"[PersonnelData] Loading assignments snapshot.");

                foreach (PersonnelAssignmentSave save in snapshot)
                {
                    PersonnelInfo info = new PersonnelInfo
                    {
                        Id = save.GeoUnitId,
                        Character = phoenix.Soldiers.FirstOrDefault(s => s.Id == save.GeoUnitId),
                        Assignment = save.Assignment
                    };

                    if (save.MainSpecName != null && save.MainSpecName != "")
                    {
                        info.TrainingSpec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName);
                    }

                    _assignments.Add(info.Id, info);

                    TFTVLogger.Always($"[PersonnelData] Restoring personnel id={info.Id} name={info.Character.DisplayName} assignment={info.Assignment} MainSpecName: {info?.TrainingSpec?.name}");
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void RestoreAssignments(GeoLevelController level)
        {
            try
            {
                ResyncWorkSlots(level.PhoenixFaction);
                RefreshInfoBar(level);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ResyncWorkSlots(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return;

            ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Research));
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));

            TFTVLogger.Always($"[PersonnelData] After load: ResearchUsed={_assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Research)} ManufacturingUsed={_assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing)} Total={_assignments.Values.Count}");
        }

        private static void RefreshInfoBar(GeoLevelController level)
        {
            FlushPendingInfoBarUpdate(level);
        }

        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_PersonnelPool
        {
            private static void Postfix(GeoLevelController __instance) => TFTVBaseRework.PersonnelManagementUI.DailyTick(__instance);
        }


        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.RegenerateNakedRecruits))]
        internal static class GeoPhoenixFaction_RegenerateNakedRecruits_PersonnelSync
        {
            private static void Postfix(GeoPhoenixFaction __instance, ref TimeUnit ____lastNakedRecruitRefresh)
            {

                TFTVLogger.Always($"GeoPhoenixFaction.RegenerateNakedRecruits running");

                try { SyncFromNakedRecruits(__instance); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        internal static void SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            try
            {

                GeoLevelController level = phoenix.GeoLevel;

                foreach (var kv in phoenix.NakedRecruits.ToList())
                {
                    GeoUnitDescriptor descriptor = kv.Key;
                    if (descriptor == null) continue;

                    GeoCharacter character = CreateHiddenCharacterFromDescriptor(level, phoenix, descriptor);

                    if (!_assignments.ContainsKey(character.Id))
                    {
                        AttachCharacter(character);

                    }
                }
                CleanNakedRecruits(phoenix);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static void CleanNakedRecruits(GeoPhoenixFaction phoenix)
        {
            try
            {
                FieldInfo fieldInfo = AccessTools.Field(typeof(GeoPhoenixFaction), "_nakedRecruits");

                var _nakedRecruits = (Dictionary<GeoUnitDescriptor, ResourcePack>)fieldInfo.GetValue(phoenix);
                _nakedRecruits.Clear();
                fieldInfo.SetValue(phoenix, _nakedRecruits);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}