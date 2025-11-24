using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public int UnitId;
        public GeoCharacter Character;
        public PersonnelAssignment Assignment;
        public SpecializationDef TrainingSpec;
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class PersonnelAssignmentSave
    {
        public int PersonnelId;
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
        private static int _nextPersonnelId = 1;

        internal static IEnumerable<PersonnelInfo> CurrentPersonnel => _personnel;

        internal static void ClearAssignments()
        {
            _assignments.Clear();
            _personnel.Clear();
            _nextPersonnelId = 1;
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
            return info ?? _personnel.FirstOrDefault(p => p.UnitId == unitId);
        }

        private static PersonnelInfo CreatePersonnelRecord(GeoCharacter character)
        {
            var info = new PersonnelInfo
            {
                Id = _nextPersonnelId++,
                UnitId = character?.Id ?? 0,
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

        internal static PersonnelInfo EnsurePersonnelFromSave(int id, int unitId, string characterName, string mainSpec)
        {
            var info = GetPersonnelById(id);
            if (info == null)
            {
                info = new PersonnelInfo
                {
                    Id = id,
                    UnitId = unitId,
                    Assignment = PersonnelAssignment.Unassigned,
                    
                };
                _personnel.Add(info);
                _nextPersonnelId = Math.Max(_nextPersonnelId, id + 1);
            }

            if (info.UnitId == 0 && unitId > 0)
            {
                info.UnitId = unitId;
            }


            if (info.TrainingSpec == null && !string.IsNullOrEmpty(mainSpec))
            {
                try
                {
                    var spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(mainSpec);
                    if (spec != null) info.TrainingSpec = spec;
                }
                catch { }
            }

            return info;
        }

        internal static bool SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return false;

            bool changed = false;
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null) return false;

            foreach (var kv in phoenix.NakedRecruits.ToList())
            {

                GeoUnitDescriptor descriptor = kv.Key;
                if (descriptor == null) continue;

                GeoCharacter character = CreateHiddenCharacterFromDescriptor(level, phoenix, descriptor);
                if (character != null)
                {
                    AttachCharacter(character);
                    changed = true;
                    phoenix.NakedRecruits.Remove(descriptor);
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

            return changed;
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
            info.UnitId = character.Id;
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

            _assignments.Remove(person.UnitId);
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
                    PersonnelId = pi.Id,
                    GeoUnitId = pi.UnitId,
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

            TFTVLogger.Always($"[PersonnelData] Loading assignments snapshot for {phoenix?.Bases.Count()} bases.");

            _assignments.Clear();

            foreach (var save in snapshot)
            {
                var info = EnsurePersonnelFromSave(save.PersonnelId, save.GeoUnitId, save.CharacterName, save.MainSpecName);
                info.Assignment = save.Assignment;
                info.TrainingCompleteNotDeployed = save.TrainingCompleteNotDeployed;
                info.DeploymentUIOpened = save.DeploymentUIOpened;

                TFTVLogger.Always($"[PersonnelData] Restoring personnel id={info.Id} name={save.CharacterName} assignment={info.Assignment} unitId={save.GeoUnitId}");
            }

            SyncFromNakedRecruits(phoenix);

            ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Research));
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));

            TFTVLogger.Always($"[PersonnelData] After load: ResearchUsed={_personnel.Count(pi => pi.Assignment == PersonnelAssignment.Research)} ManufacturingUsed={_personnel.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing)} Total={_personnel.Count}");

            FlushPendingInfoBarUpdate(level);
        }
    }
}