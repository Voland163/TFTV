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
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class PersonnelAssignmentSave
    {
        public int GeoUnitId;
        public string CharacterName;
        public string MainSpecName;
        public PersonnelAssignment Assignment;
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;
    }

    internal static class PersonnelData
    {
        private const string LogPrefix = "[PersonnelData]";

        private static readonly Dictionary<int, PersonnelInfo> _assignments = new Dictionary<int, PersonnelInfo>();
        private static readonly List<PersonnelInfo> _personnel = new List<PersonnelInfo>();

        internal static IEnumerable<PersonnelInfo> CurrentPersonnel => _personnel;

        internal static void ClearAssignments()
        {
            _assignments.Clear();
            _personnel.Clear();
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
            return info ?? _personnel.FirstOrDefault(p => p.Id == unitId);
        }

        private static PersonnelInfo CreatePersonnelRecord(GeoCharacter character)
        {
            var info = new PersonnelInfo
            {
                Id = character.Id,
                Character = character,
                Assignment = PersonnelAssignment.Unassigned,
            };

            _personnel.Add(info);
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

        internal static PersonnelInfo GetPersonnelById(int id)
        {
            return _personnel.FirstOrDefault(p => p.Id == id);
        }

        internal static PersonnelInfo GetPersonnelByUnitId(int unitId)
        {
            return FindPersonnel(unitId);
        }

        internal static PersonnelInfo EnsurePersonnelFromSave(int id, string mainSpec)
        {
            var info = GetPersonnelById(id);
            if (info == null)
            {
                info = new PersonnelInfo
                {
                    Id = id,
                    Assignment = PersonnelAssignment.Unassigned,

                };

                _personnel.Add(info);
            }

            if (id > 0 && !_assignments.ContainsKey(id))
            {
                _assignments[id] = info;
            }


            if (info.TrainingSpec == null && !string.IsNullOrEmpty(mainSpec))
            {
                var spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(mainSpec);
                if (spec != null) info.TrainingSpec = spec;
            }

            return info;
        }

        internal static bool SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return false;

            bool changed = false;
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null) return false;

            RebuildAssignmentLookup(phoenix);

            foreach (var kv in phoenix.NakedRecruits.ToList())
            {
                GeoUnitDescriptor descriptor = kv.Key;
                if (descriptor == null) continue;

                GeoCharacter character = CreateHiddenCharacterFromDescriptor(level, phoenix, descriptor);
                if (character != null)
                {
                    AttachCharacter(character);
                    changed = true;
                }
            }

            foreach (var soldier in phoenix.Soldiers)
            {
                if (GeoCharacterFilter.HiddenOperativeTagFilter.ShouldHide(soldier))
                {
                    if (!_assignments.ContainsKey(soldier.Id))
                    {
                        AttachCharacter(soldier);
                        changed = true;
                    }
                }
            }

            CleanNakedRecruits(phoenix);
            return changed;
        }

        private static void CleanNakedRecruits(GeoPhoenixFaction phoenix)
        {
            FieldInfo fieldInfo = AccessTools.Field(typeof(GeoPhoenixFaction), "_nakedRecruits");

            var _nakedRecruits = (Dictionary<GeoUnitDescriptor, ResourcePack>)fieldInfo.GetValue(phoenix);
            _nakedRecruits.Clear();
            fieldInfo.SetValue(phoenix, _nakedRecruits);
        }

        private static void RebuildAssignmentLookup(GeoPhoenixFaction phoenix)
        {

            TFTVLogger.Always($"{LogPrefix} Rebuilding personnel assignment lookup. Personnel count: {_personnel.Count}, assingments: {_assignments.Count}");

            foreach (var info in _personnel)
            {
                TFTVLogger.Always($"{LogPrefix} Processing personnel id={info.Id} character={(info.Character != null ? info.Character.DisplayName : "null")} _assignments.ContainsKey(info.UnitId): {_assignments.ContainsKey(info.Id)}");

                if (info.Id <= 0 || _assignments.ContainsKey(info.Id))
                {
                    continue;
                }

                var soldier = phoenix.Soldiers.FirstOrDefault(s => s.Id == info.Id);

                TFTVLogger.Always($"soldier==null: {soldier == null}");

                if (soldier != null)
                {
                    info.Character = soldier;
                    _assignments[info.Id] = info;
                }
            }
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
            _personnel.Remove(person);
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
            foreach (var pi in _personnel)
            {
                list.Add(new PersonnelAssignmentSave
                {
                    GeoUnitId = pi.Id,
                    CharacterName = pi.Character?.DisplayName,
                    MainSpecName = pi.TrainingSpec?.name ?? pi.Character?.Progression?.MainSpecDef?.name,
                    Assignment = pi.Assignment,
                    TrainingCompleteNotDeployed = pi.TrainingCompleteNotDeployed,
                    DeploymentUIOpened = pi.DeploymentUIOpened
                });
            }
            return list;
        }

        internal static void LoadAssignmentsSnapshot(GeoLevelController level, IEnumerable<PersonnelAssignmentSave> snapshot)
        {
            if (level?.PhoenixFaction == null || snapshot == null) return;
            var phoenix = level.PhoenixFaction;

            TFTVLogger.Always($"[PersonnelData] Loading assignments snapshot.");

            RestorePersonnelAssignments(phoenix, snapshot);
            ResyncWorkSlots(phoenix);
            RefreshInfoBar(level);
        }

        private static void RestorePersonnelAssignments(GeoPhoenixFaction phoenix, IEnumerable<PersonnelAssignmentSave> snapshot)
        {

            _assignments.Clear();

            foreach (PersonnelAssignmentSave save in snapshot)
            {
                PersonnelInfo info = EnsurePersonnelFromSave(save.GeoUnitId, save.MainSpecName);
                info.Id = save.GeoUnitId;
                info.Character = phoenix.Soldiers.FirstOrDefault(s => s.Id == save.GeoUnitId);
                info.Assignment = save.Assignment;
                if (save.MainSpecName != null && save.MainSpecName != "")
                {
                    info.TrainingSpec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName);
                }
                info.TrainingCompleteNotDeployed = save.TrainingCompleteNotDeployed;
                info.DeploymentUIOpened = save.DeploymentUIOpened;

                TFTVLogger.Always($"[PersonnelData] Restoring personnel id={info.Id} name={info.Character.DisplayName} assignment={info.Assignment}");
            }

            SyncFromNakedRecruits(phoenix);

        }

        internal static void ResyncWorkSlots(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return;

            ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Research));
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));

            TFTVLogger.Always($"[PersonnelData] After load: ResearchUsed={_personnel.Count(pi => pi.Assignment == PersonnelAssignment.Research)} ManufacturingUsed={_personnel.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing)} Total={_personnel.Count}");
        }

        private static void RefreshInfoBar(GeoLevelController level)
        {
            FlushPendingInfoBarUpdate(level);
        }
    }
}