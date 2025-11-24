using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
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
        public GeoUnitDescriptor Descriptor;
        public PersonnelAssignment Assignment;
        public SpecializationDef TrainingSpec;
        public GeoCharacter CreatedCharacter;
        public GeoPhoenixFacility TrainingFacility;
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;

        public string SavedDescriptorName;
        public string SavedIdentityName;
        public GeoCharacterSex SavedIdentitySex;
        public string SavedMainSpecName;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class PersonnelAssignmentSave
    {
        public int PersonnelId;
        public string DescriptorName;
        public string IdentityName;
        public GeoCharacterSex IdentitySex;
        public string MainSpecName;
        public PersonnelAssignment Assignment;
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;
    }

    internal static class PersonnelData
    {
        private const string LogPrefix = "[PersonnelData]";

        private static readonly Dictionary<GeoUnitDescriptor, PersonnelInfo> _assignments = new Dictionary<GeoUnitDescriptor, PersonnelInfo>();
        private static readonly List<PersonnelInfo> _personnel = new List<PersonnelInfo>();
        private static int _nextPersonnelId = 1;

        internal static IEnumerable<PersonnelInfo> CurrentPersonnel => _personnel;

        internal static void ClearAssignments()
        {
            _assignments.Clear();
            _personnel.Clear();
            _nextPersonnelId = 1;
        }

        private static PersonnelInfo FindPersonnel(GeoUnitDescriptor descriptor)
        {
            if (descriptor == null) return null;
            _assignments.TryGetValue(descriptor, out var info);
            return info;
        }

        private static PersonnelInfo FindPersonnelByIdentity(GeoUnitDescriptor descriptor)
        {
            if (descriptor?.Identity == null) return null;
            return _personnel.FirstOrDefault(p => string.Equals(p.SavedIdentityName, descriptor.Identity.Name, StringComparison.Ordinal)
                                                  && p.SavedIdentitySex == descriptor.Identity.Sex);
        }

        private static PersonnelInfo CreatePersonnelRecord(GeoUnitDescriptor descriptor)
        {
            var info = new PersonnelInfo
            {
                Id = _nextPersonnelId++,
                Descriptor = descriptor,
                Assignment = PersonnelAssignment.Unassigned,
                SavedDescriptorName = descriptor?.GetName(),
                SavedIdentityName = descriptor?.Identity?.Name,
                SavedIdentitySex = descriptor?.Identity?.Sex ?? GeoCharacterSex.None,
                SavedMainSpecName = descriptor?.Progression?.MainSpecDef?.name
            };

            _personnel.Add(info);
            if (descriptor != null && !_assignments.ContainsKey(descriptor))
            {
                _assignments[descriptor] = info;
            }

            return info;
        }

        private static void AttachDescriptor(GeoUnitDescriptor descriptor)
        {
            if (descriptor == null) return;

            var info = FindPersonnel(descriptor) ?? FindPersonnelByIdentity(descriptor);
            if (info == null)
            {
                info = CreatePersonnelRecord(descriptor);
            }
            else
            {
                info.Descriptor = descriptor;
                info.SavedDescriptorName = descriptor.GetName();
                info.SavedIdentityName = descriptor.Identity?.Name;
                info.SavedIdentitySex = descriptor.Identity?.Sex ?? GeoCharacterSex.None;
                if (!_assignments.ContainsKey(descriptor))
                {
                    _assignments[descriptor] = info;
                }
            }
        }

        internal static int GetOrCreatePersonnelId(GeoUnitDescriptor descriptor)
        {
            var info = FindPersonnel(descriptor) ?? CreatePersonnelRecord(descriptor);
            return info.Id;
        }

        internal static PersonnelInfo GetPersonnelById(int id)
        {
            return _personnel.FirstOrDefault(p => p.Id == id);
        }

        internal static PersonnelInfo GetPersonnelByDescriptor(GeoUnitDescriptor descriptor)
        {
            return FindPersonnel(descriptor);
        }

        internal static PersonnelInfo EnsurePersonnelFromSave(int id, string descriptorName, string identityName, GeoCharacterSex identitySex, string mainSpec)
        {
            var info = GetPersonnelById(id);
            if (info == null)
            {
                info = new PersonnelInfo
                {
                    Id = id,
                    Assignment = PersonnelAssignment.Unassigned,
                    SavedDescriptorName = descriptorName,
                    SavedIdentityName = identityName,
                    SavedIdentitySex = identitySex,
                    SavedMainSpecName = mainSpec
                };
                _personnel.Add(info);
                _nextPersonnelId = Math.Max(_nextPersonnelId, id + 1);
            }
            return info;
        }

        internal static bool EnsureDescriptorInPool(GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            if (faction == null || descriptor == null) return false;

            var nakedRecruits = faction.NakedRecruits;
            if (nakedRecruits is IDictionary dict)
            {
                if (!dict.Contains(descriptor))
                {
                    dict[descriptor] = null;
                }
            }

            return SyncFromNakedRecruits(faction);
        }

        internal static bool SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return false;

            bool changed = false;
            foreach (var kv in phoenix.NakedRecruits)
            {
                if (kv.Key == null) continue;
                if (!_assignments.ContainsKey(kv.Key))
                {
                    changed = true;
                }
                AttachDescriptor(kv.Key);
            }

            var activeDescriptors = new HashSet<GeoUnitDescriptor>(phoenix.NakedRecruits.Keys);
            var mappedDescriptors = _assignments.Keys.ToList();

            foreach (var descriptor in mappedDescriptors)
            {
                if (!activeDescriptors.Contains(descriptor))
                {
                    _assignments.Remove(descriptor);
                    changed = true;
                }
            }

            return changed;
        }

        internal static void RemoveDescriptorFromNakedPool(GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            if (faction?.NakedRecruits.ContainsKey(descriptor) == true)
            {
                faction.NakedRecruits.Remove(descriptor);
            }
            _assignments.Remove(descriptor);
        }

        internal static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            var faction = level?.PhoenixFaction;
            if (faction == null) return new List<SpecializationDef>();
            return TrainingFacilityRework.GetAvailableTrainingSpecializations(faction).ToList();
        }

        internal static void AssignWorker(PersonnelInfo person, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (person?.Descriptor == null || faction == null) return;
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
                    DescriptorName = pi.Descriptor?.GetName() ?? pi.SavedDescriptorName,
                    IdentityName = pi.Descriptor?.Identity?.Name ?? pi.SavedIdentityName,
                    IdentitySex = pi.Descriptor?.Identity?.Sex ?? pi.SavedIdentitySex,
                    MainSpecName = pi.TrainingSpec?.name ?? pi.Descriptor?.Progression?.MainSpecDef?.name ?? pi.SavedMainSpecName,
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

            _assignments.Clear();

            foreach (var save in snapshot)
            {
                var info = EnsurePersonnelFromSave(save.PersonnelId, save.DescriptorName, save.IdentityName, save.IdentitySex, save.MainSpecName);
                info.Assignment = save.Assignment;
                info.TrainingCompleteNotDeployed = save.TrainingCompleteNotDeployed;
                info.DeploymentUIOpened = save.DeploymentUIOpened;

                if (!string.IsNullOrEmpty(save.MainSpecName))
                {
                    try
                    {
                        var spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName);
                        if (spec != null) info.TrainingSpec = spec;
                    }
                    catch { }
                }
            }

            SyncFromNakedRecruits(phoenix);

            ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Research));
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                _personnel.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));
        }
    }
}
