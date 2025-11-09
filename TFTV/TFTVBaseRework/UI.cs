using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.TrainingFacilityRework;
using static TFTV.TFTVBaseRework.Workers;

namespace TFTV.TFTVBaseRework
{
    internal class UI
    {
        /// <summary>
        /// Turns the vanilla Base Recruit (Personnel) tab into a Personnel Management screen.
        /// Displays "unassigned personnel" (raw GeoUnitDescriptor prototypes) and lets the player:
        /// 1) Convert into level 1 operative (chosen class).
        /// 2) Assign as a worker to Research or Manufacturing (consumes a global slot).
        /// 3) Create operative and send directly to Training Facility (chosen class).
        /// 
        /// Pool: 4–5 new descriptors every 4 days (configurable).
        /// </summary>
        internal static class PersonnelManagementUI
        {
            private const int DaysBetweenRefresh = 4;
            private const int MinNewPersonnel = 4;
            private const int MaxNewPersonnel = 5;

            private static readonly List<GeoUnitDescriptor> _unassigned = new List<GeoUnitDescriptor>();
            private static int _lastGenerationDay = -1;
            private static System.Random _rng = new System.Random();

            internal static IReadOnlyList<GeoUnitDescriptor> Unassigned => _unassigned;

            internal static void DailyTick(GeoLevelController level)
            {
                try
                {
                    if (level?.PhoenixFaction == null) return;
                    int day = level.Timing.Now.TimeSpan.Days;
                    if (_lastGenerationDay < 0)
                    {
                        _lastGenerationDay = day;
                        GenerateBatch(level);
                        return;
                    }
                    if (day - _lastGenerationDay >= DaysBetweenRefresh)
                    {
                        _lastGenerationDay = day;
                        GenerateBatch(level);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void GenerateBatch(GeoLevelController level)
            {
                try
                {
                    CleanupInvalid(level);
                    int count = _rng.Next(MinNewPersonnel, MaxNewPersonnel + 1);
                    var phoenix = level.PhoenixFaction;
                    for (int i = 0; i < count; i++)
                    {
                        var descriptor = GenerateDescriptor(level, phoenix);
                        if (descriptor != null)
                        {
                            _unassigned.Add(descriptor);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CleanupInvalid(GeoLevelController level)
            {
                _unassigned.RemoveAll(d =>
                    d == null ||
                    level == null ||
                    level.PhoenixFaction == null);
            }

            // Adjusted to match GeoPhoenixFaction.RegenerateNakedRecruits flow
            private static GeoUnitDescriptor GenerateDescriptor(GeoLevelController level, GeoPhoenixFaction phoenix)
            {
                try
                {
                    if (level == null || phoenix == null) return null;

                    CharacterGenerationContext context = level.CharacterGenerator.GenerateCharacterGeneratorContext(phoenix);
                    GeoUnitDescriptor descriptor = level.CharacterGenerator.GenerateRandomUnit(context);
                    if (descriptor == null)
                    {
                        return null;
                    }

                    // Apply the same difficulty tuning as vanilla naked recruits
                    level.CharacterGenerator.ApplyRecruitDifficultyParameters(descriptor);

                    return descriptor;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return null;
                }
            }

            internal static void RemoveDescriptor(GeoUnitDescriptor descriptor)
            {
                _unassigned.Remove(descriptor);
            }

            #region UI Patching

            [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
            internal static class GeoLevelController_DailyUpdate_PersonnelPool
            {
                private static void Postfix(GeoLevelController __instance)
                {
                    DailyTick(__instance);
                }
            }

            [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
            internal static class UIStateRosterRecruits_EnterState_PersonnelManagement
            {
                private static void Postfix(UIStateRosterRecruits __instance)
                {
                    try
                    {
                        GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        if(geoLevelController== null)
                        {
                            TFTVLogger.Always("geoLevelController null in UIStateRosterRecruits EnterState.");
                            return;
                        }


                        Transform listRoot = geoLevelController.View.GeoscapeModules.RecruitsListModule.transform;
                            
                        if (listRoot == null)
                        {
                            TFTVLogger.Always("PersonnelManagementUI: Could not find recruit list root.");
                            return;
                        }

                        ClearChildren(listRoot);



                        var phoenix = geoLevelController.PhoenixFaction;
                        if (phoenix == null)
                        {
                            return;
                        }

                        foreach (var proto in Unassigned)
                        {
                            CreatePersonnelCard(listRoot, proto, phoenix.GeoLevel, phoenix);
                        }

                        AddSummaryHeader(__instance, listRoot, phoenix);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void ClearChildren(Transform root)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                {
                    var c = root.GetChild(i);
                    if (c != null)
                    {
                        UnityEngine.Object.Destroy(c.gameObject);
                    }
                }
            }

            private static void AddSummaryHeader(UIStateRosterRecruits state, Transform parent, GeoPhoenixFaction phoenix)
            {
                var go = new GameObject("TFTV_Personnel_Header");
                go.transform.SetParent(parent, false);
                var txt = go.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 18;
                var research = TrainingFacilityRework.GetWorkSlotUsage(phoenix, FacilitySlotType.Research);
                var manuf = TrainingFacilityRework.GetWorkSlotUsage(phoenix, FacilitySlotType.Manufacturing);
                txt.text =
                    $"Unassigned Personnel: {Unassigned.Count}\n" +
                    $"Research Workers: {research.used}/{research.provided}  Manufacturing Workers: {manuf.used}/{manuf.provided}";
                txt.color = Color.cyan;
            }

            private static void CreatePersonnelCard(Transform parent, GeoUnitDescriptor proto, GeoLevelController level, GeoPhoenixFaction phoenix)
            {
                var card = new GameObject($"Personnel_{proto.GetName()}");
                card.transform.SetParent(parent, false);
                var bg = card.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

                var layout = card.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 4;
                layout.padding = new RectOffset(6, 6, 6, 6);

                AddLabel(card.transform, proto.GetName(), 16, Color.white);
                AddLabel(card.transform, $"Template Level: {proto.Level}", 12, Color.gray);

                var classRoot = new GameObject("ClassSelect");
                classRoot.transform.SetParent(card.transform, false);
                var classText = classRoot.AddComponent<Text>();
                classText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                classText.fontSize = 12;
                classText.color = Color.yellow;

                var availableSpecs = ResolveAvailableMainSpecs(level);
                int specIndex = _rng.Next(availableSpecs.Count);
                SpecializationDef currentSpec = availableSpecs[specIndex];
                classText.text = $"Chosen Class: {currentSpec?.name}";

                AddActionButton(card.transform, "Make Operative",
                    () => MakeOperative(proto, currentSpec, phoenix, level));

                AddActionButton(card.transform, "Assign Research",
                    () => AssignWorker(proto, phoenix, FacilitySlotType.Research));

                AddActionButton(card.transform, "Assign Manufacturing",
                    () => AssignWorker(proto, phoenix, FacilitySlotType.Manufacturing));

                AddActionButton(card.transform, "Start Training",
                    () => AssignTraining(proto, currentSpec, level));

                AddActionButton(card.transform, "Cycle Class",
                    () =>
                    {
                        specIndex = (specIndex + 1) % availableSpecs.Count;
                        currentSpec = availableSpecs[specIndex];
                        classText.text = $"Chosen Class: {currentSpec?.name}";
                    });
            }

            private static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
            {
                try
                {
                    var cache = TFTVMain.Main.DefCache;
                    var names = new[]
                    {
                            "AssaultSpecializationDef",
                            "HeavySpecializationDef",
                            "SniperSpecializationDef",
                            "PriestSpecializationDef",
                            "BerserkerSpecializationDef",
                            "InfiltratorSpecializationDef",
                            "TechnicianSpecializationDef"
                        };
                    var list = new List<SpecializationDef>();
                    foreach (var n in names)
                    {
                        var spec = cache.GetDef<SpecializationDef>(n);
                        if (spec != null)
                        {
                            list.Add(spec);
                        }
                    }
                   
                    return list;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return new List<SpecializationDef>();
                }
            }

            private static void MakeOperative(GeoUnitDescriptor proto, SpecializationDef spec, GeoPhoenixFaction faction, GeoLevelController level)
            {
                try
                {
                    if (proto == null || spec == null) return;
                    var baseToUse = faction.Bases.FirstOrDefault();
                    if (baseToUse == null) return;
                    var character = TrainingFacilityRework.CreateOperativeFromDescriptor(level, proto, baseToUse, spec);
                    if (character != null)
                    {
                        PersonnelManagementUI.RemoveDescriptor(proto);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AssignWorker(GeoUnitDescriptor proto, GeoPhoenixFaction faction, FacilitySlotType slotType)
            {
                try
                {
                    if (proto == null) return;
                    if (TrainingFacilityRework.TryAssignToWork(faction, slotType))
                    {
                        PersonnelManagementUI.RemoveDescriptor(proto);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AssignTraining(GeoUnitDescriptor proto, SpecializationDef spec, GeoLevelController level)
            {
                try
                {
                    if (proto == null || spec == null) return;
                    var phoenix = level.PhoenixFaction;
                    var facility = FindAnyValidTrainingFacility(phoenix);
                    if (facility == null)
                    {
                        MakeOperative(proto, spec, phoenix, level);
                        return;
                    }

                    bool success = TrainingFacilityRework.TryAssignDescriptorToTraining(level, proto, facility, spec);
                    if (success)
                    {
                        PersonnelManagementUI.RemoveDescriptor(proto);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static GeoPhoenixFacility FindAnyValidTrainingFacility(GeoPhoenixFaction phoenix)
            {
                foreach (var b in phoenix.Bases)
                {
                    foreach (var f in b.Layout.Facilities)
                    {
                        if (f != null &&
                            f.GetComponent<ExperienceFacilityComponent>() != null &&
                            f.IsPowered &&
                            f.State == GeoPhoenixFacility.FacilityState.Functioning)
                        {
                            return f;
                        }
                    }
                }
                return null;
            }

            private static void AddLabel(Transform parent, string text, int size, Color color)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(parent, false);
                var t = go.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.text = text;
                t.fontSize = size;
                t.color = color;
            }

            private static void AddActionButton(Transform parent, string caption, Action onClick)
            {
                var go = new GameObject($"Btn_{caption}");
                go.transform.SetParent(parent, false);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.25f, 0.35f, 0.55f, 0.9f);

                var btn = go.AddComponent<Button>();
                btn.onClick.AddListener(() => onClick?.Invoke());

                var txtGO = new GameObject("Text");
                txtGO.transform.SetParent(go.transform, false);
                var txt = txtGO.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.text = caption;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontSize = 12;

                var layout = go.AddComponent<LayoutElement>();
                layout.minHeight = 26;
                layout.minWidth = 140;
            }
            #endregion
        }
    }
}




