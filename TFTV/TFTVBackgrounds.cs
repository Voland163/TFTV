using Base.Core;
using Base.Defs;
using Base.Lighting;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Geoscape.Levels.GeoSceneReferences;

namespace TFTV
{
    internal class TFTVBackgrounds
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static class TFTVBackgroundDeploymentSelector
        {
            private const float SquadBayBackgroundScale = 1f;//1.05f;

            internal enum LightCondition
            {
                Day,
                Night
            }

            /// Whether a painting is tied to a time of day. Files ending "_daynight" or "_dayornight",
            /// and files with no time suffix at all, are usable under either sky.
            private enum LightMatch
            {
                Day,
                Night,
                Either
            }

            private struct BackgroundEntry
            {
                public string FileName;
                public string Slot;
                public LightMatch Light;
            }

            private const string BackgroundsFolder = "TFTVMissionDeploymentBackgrounds";

            private static readonly string[] EitherSuffixes = { "_dayornight", "_dayandnight", "_daynight", "_anytime", "_any" };

            /// Explicit per-mission-def overrides, for pinning one mission to one image.
            private static readonly Dictionary<string, Dictionary<LightCondition, Sprite>> MissionBackgrounds =
                new Dictionary<string, Dictionary<LightCondition, Sprite>>(StringComparer.OrdinalIgnoreCase);

            /// Relative path to sprite, including nulls for art we do not have.
            private static readonly Dictionary<string, Sprite> LoadedBackgrounds =
                new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            /// Haven zone defs to the file name fragment their artwork uses.
            private static readonly Dictionary<string, string> ZoneCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Research_GeoHavenZoneDef", "research" },
                { "Factory_GeoHavenZoneDef", "factory" },
                { "FoodProduction_GeoHavenZoneDef", "farm" },
                { "Residential_GeoHavenZoneDef", "living" },
                { "ResidentialElite_GeoHavenZoneDef", "eliteliving" },
                { "Training_GeoHavenZoneDef", "training" },
                { "TrainingElite_GeoHavenZoneDef", "elitetraining" },
                { "Energy_GeoHavenZoneDef", "energy" },
                { "MistRepeller_GeoHavenZoneDef", "repeller" },
                { "SatelliteUplink_GeoHavenZoneDef", "uplink" },
                { "MissionaryCentre_GeoHavenZoneDef", "missionary" },
                { "LeviathanChamber_GeoHavenZoneDef", "leviathan" },
                { "MoonLaunch_GeoHavenZoneDef", "moonlaunch" },
                { "TWFortress_GeoHavenZoneDef", "fortress" },
            };

            private static Dictionary<string, List<BackgroundEntry>> _bySlot;
            private static Dictionary<string, List<BackgroundEntry>> _byFaction;
            private static List<BackgroundEntry> _infested;

            internal static void Register(string missionDefName, LightCondition lightCondition, Sprite background)
            {
                if (string.IsNullOrEmpty(missionDefName) || background == null)
                {
                    return;
                }

                Dictionary<LightCondition, Sprite> backgrounds;
                if (!MissionBackgrounds.TryGetValue(missionDefName, out backgrounds))
                {
                    backgrounds = new Dictionary<LightCondition, Sprite>();
                    MissionBackgrounds.Add(missionDefName, backgrounds);
                }

                backgrounds[lightCondition] = background;
            }

            internal static Sprite Select(GeoMission mission)
            {
                LightCondition lightCondition = GetLightCondition(mission);
                Sprite background;
                Dictionary<LightCondition, Sprite> missionBackgrounds;
                string missionDefName = mission != null && mission.MissionDef != null ? mission.MissionDef.name : null;

                if (!string.IsNullOrEmpty(missionDefName)
                    && MissionBackgrounds.TryGetValue(missionDefName, out missionBackgrounds)
                    && missionBackgrounds.TryGetValue(lightCondition, out background))
                {
                    return background;
                }

                BuildIndex();

                string chosen = Resolve(mission, lightCondition);

                // One line per deployment screen, so the log shows which artwork a mission resolved to.
                TFTVLogger.Always($"Deployment background for {missionDefName ?? "unknown mission"} ({lightCondition}): {chosen ?? "no match"}");

                return LoadBackground(chosen) ?? DefaultFor(lightCondition);
            }

            /// Picks the file name for a mission, or null if the folder has nothing for it.
            private static string Resolve(GeoMission mission, LightCondition lightCondition)
            {
                if (mission == null || mission.MissionDef == null)
                {
                    return null;
                }

                LightMatch want = lightCondition == LightCondition.Night ? LightMatch.Night : LightMatch.Day;
                HashSet<string> tags = GetTagNames(mission);

                string alienBase = GetAlienBaseCode(tags);

                if (alienBase != null)
                {
                    // Pandoran interiors have no sky, so they are shared between day and night.
                    return PickFromSlot("aln_" + alienBase, want, allowOtherLight: true, mission: mission);
                }

                GeoHaven haven = mission.Site != null ? mission.Site.GetComponent<GeoHaven>() : null;
                string faction = GetFactionCode(mission, haven);

                if (tags.Contains("HavenInfestation_MissionTypeTagDef"))
                {
                    return PickInfested(faction, want, mission);
                }

                if (tags.Any(tag => tag.IndexOf("MissionTypeAncientSite", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    string ancients = PickFromSlot("ancients", want, allowOtherLight: true, mission: mission);

                    if (ancients != null)
                    {
                        return ancients;
                    }
                }

                if (haven != null && faction != null)
                {
                    string zone = GetZoneCode(mission, tags);

                    // Right place under the right sky first, then anything else this faction builds
                    // under that sky, and only then the right place under the wrong one. A haven never
                    // falls back to wasteland: a settlement should not look like open ground.
                    return PickZone(faction, zone, want, allowOtherLight: false, mission: mission)
                        ?? PickFromFaction(faction, want, allowOtherLight: false, mission: mission)
                        ?? PickZone(faction, zone, want, allowOtherLight: true, mission: mission)
                        ?? PickFromFaction(faction, want, allowOtherLight: true, mission: mission);
                }

                return PickWasteland(tags, want, mission);
            }

            /// An infested haven always looks infested. Factions with no infested artwork of their own
            /// borrow Synedrion's, which reads as overgrown ruin more than as anyone's architecture.
            private static string PickInfested(string faction, LightMatch want, GeoMission mission)
            {
                string own = faction != null
                    ? PickFromSlot(faction + "_infested", want, allowOtherLight: true, mission: mission)
                    : null;

                if (own != null)
                {
                    return own;
                }

                List<BackgroundEntry> synedrion = _infested
                    .Where(entry => entry.Slot.StartsWith("sy_", StringComparison.Ordinal))
                    .ToList();

                return Choose(synedrion.Count > 0 ? synedrion : _infested, mission);
            }

            /// Open ground, for missions that are not at a settlement. Overgrown and bare wasteland are
            /// close enough that plain terrain under the right sky beats the right terrain under the wrong one.
            private static string PickWasteland(HashSet<string> tags, LightMatch want, GeoMission mission)
            {
                bool overgrown = tags.Any(tag => tag.IndexOf("Overgrown", StringComparison.OrdinalIgnoreCase) >= 0);

                return (overgrown ? PickFromSlot("wasteland_overgrown", want, allowOtherLight: false, mission: mission) : null)
                    ?? PickFromSlot("wasteland", want, allowOtherLight: false, mission: mission)
                    ?? (overgrown ? PickFromSlot("wasteland_overgrown", want, allowOtherLight: true, mission: mission) : null)
                    ?? PickFromSlot("wasteland", want, allowOtherLight: true, mission: mission);
            }

            private static string PickZone(string faction, string zone, LightMatch want, bool allowOtherLight, GeoMission mission)
            {
                return zone != null ? PickFromSlot(faction + "_" + zone, want, allowOtherLight, mission) : null;
            }

            private static string PickFromSlot(string slot, LightMatch want, bool allowOtherLight, GeoMission mission)
            {
                List<BackgroundEntry> entries;

                return _bySlot.TryGetValue(slot, out entries) ? PickFrom(entries, want, allowOtherLight, mission) : null;
            }

            private static string PickFromFaction(string faction, LightMatch want, bool allowOtherLight, GeoMission mission)
            {
                List<BackgroundEntry> entries;

                return _byFaction.TryGetValue(faction, out entries) ? PickFrom(entries, want, allowOtherLight, mission) : null;
            }

            private static string PickFrom(List<BackgroundEntry> entries, LightMatch want, bool allowOtherLight, GeoMission mission)
            {
                List<BackgroundEntry> lit = entries
                    .Where(entry => entry.Light == want || entry.Light == LightMatch.Either)
                    .ToList();

                if (lit.Count > 0)
                {
                    return Choose(lit, mission);
                }

                return allowOtherLight ? Choose(entries, mission) : null;
            }

            /// Several paintings can serve one slot. Which one a mission gets is derived from its site,
            /// so re-entering the deployment screen does not reshuffle the art.
            private static string Choose(List<BackgroundEntry> entries, GeoMission mission)
            {
                if (entries == null || entries.Count == 0)
                {
                    return null;
                }

                int siteId = mission != null && mission.Site != null ? mission.Site.SiteId : 0;

                return entries[(siteId & int.MaxValue) % entries.Count].FileName;
            }

            /// Reads the artwork folder once and sorts every file into the slot it can fill. Going by
            /// what is actually on disk keeps the matching working as the art is renamed and extended.
            private static void BuildIndex()
            {
                if (_bySlot != null)
                {
                    return;
                }

                _bySlot = new Dictionary<string, List<BackgroundEntry>>(StringComparer.OrdinalIgnoreCase);
                _byFaction = new Dictionary<string, List<BackgroundEntry>>(StringComparer.OrdinalIgnoreCase);
                _infested = new List<BackgroundEntry>();

                string folder = Path.Combine(TFTVMain.TexturesDirectory, BackgroundsFolder);

                if (!Directory.Exists(folder))
                {
                    TFTVLogger.Always($"No deployment background folder at {folder}");
                    return;
                }

                string[] files = Directory.GetFiles(folder, "*.jpg");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                foreach (string file in files)
                {
                    BackgroundEntry entry = Parse(Path.GetFileNameWithoutExtension(file));

                    Add(_bySlot, entry.Slot, entry);

                    if (entry.Slot.EndsWith("_infested", StringComparison.Ordinal))
                    {
                        _infested.Add(entry);
                        continue;
                    }

                    string faction = FactionOf(entry.Slot);

                    if (faction != null)
                    {
                        Add(_byFaction, faction, entry);
                    }
                }
            }

            /// Splits a file name into the slot it fills and the sky it was painted under.
            private static BackgroundEntry Parse(string fileName)
            {
                string key = fileName.ToLowerInvariant();

                // A second painting for the same slot is marked "_alt".
                if (key.EndsWith("_alt", StringComparison.Ordinal))
                {
                    key = key.Substring(0, key.Length - "_alt".Length);
                }

                LightMatch light = LightMatch.Either;

                if (key.LastIndexOf('_') > 0 && EitherSuffixes.Any(suffix => key.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    key = key.Substring(0, key.LastIndexOf('_'));
                }
                else if (key.EndsWith("_day", StringComparison.Ordinal))
                {
                    light = LightMatch.Day;
                    key = key.Substring(0, key.Length - "_day".Length);
                }
                else if (key.EndsWith("_night", StringComparison.Ordinal))
                {
                    light = LightMatch.Night;
                    key = key.Substring(0, key.Length - "_night".Length);
                }

                // "wasteland_normal" and a bare "wasteland" name the same open ground.
                if (key == "wasteland_normal")
                {
                    key = "wasteland";
                }

                return new BackgroundEntry { FileName = fileName, Slot = key, Light = light };
            }

            private static string FactionOf(string slot)
            {
                int separator = slot.IndexOf('_');
                string prefix = separator > 0 ? slot.Substring(0, separator) : slot;

                return prefix == "anu" || prefix == "nj" || prefix == "sy" ? prefix : null;
            }

            private static void Add(Dictionary<string, List<BackgroundEntry>> index, string key, BackgroundEntry entry)
            {
                List<BackgroundEntry> entries;

                if (!index.TryGetValue(key, out entries))
                {
                    entries = new List<BackgroundEntry>();
                    index.Add(key, entries);
                }

                entries.Add(entry);
            }

            /// Absolute last resort. Every faction currently has both day and night artwork, so this
            /// only fires if the folder is missing or has been emptied.
            private static Sprite DefaultFor(LightCondition lightCondition)
            {
                bool night = lightCondition == LightCondition.Night;
                LightMatch want = night ? LightMatch.Night : LightMatch.Day;

                return LoadBackground(PickFromSlot("wasteland", want, allowOtherLight: true, mission: null))
                    ?? LoadBackground(null, night ? "deployment_b" : "deployment_a");
            }

            private static HashSet<string> GetTagNames(GeoMission mission)
            {
                HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (mission.MissionDef.Tags != null)
                {
                    foreach (GameTagDef tag in mission.MissionDef.Tags)
                    {
                        if (tag != null && !string.IsNullOrEmpty(tag.name))
                        {
                            tags.Add(tag.name);
                        }
                    }
                }

                return tags;
            }

            private static string GetAlienBaseCode(HashSet<string> tags)
            {
                if (tags.Contains("MissionTypeAlienNestAssault_MissionTagDef")) return "nest";
                if (tags.Contains("MissionTypeAlienLairAssault_MissionTagDef")) return "lair";
                if (tags.Contains("MissionTypeAlienCitadelAssault_MissionTagDef")) return "citadel";

                return null;
            }

            /// Havens keep their original owner's architecture once the Pandorans move in, so an
            /// infested haven is matched on who built it rather than on who holds it now.
            private static string GetFactionCode(GeoMission mission, GeoHaven haven)
            {
                GeoSite site = mission.Site;

                if (site == null || site.GeoLevel == null)
                {
                    return null;
                }

                GeoFaction owner = haven != null ? haven.UninfestedOwner : site.Owner;

                if (owner == null)
                {
                    return null;
                }

                if (owner == site.GeoLevel.AnuFaction) return "anu";
                if (owner == site.GeoLevel.NewJerichoFaction) return "nj";
                if (owner == site.GeoLevel.SynedrionFaction) return "sy";

                return null;
            }

            private static string GetZoneCode(GeoMission mission, HashSet<string> tags)
            {
                // Stealing an aircraft is fought over the landing pad whatever zone holds it.
                if (tags.Contains("MissionTypeStealAircraft_MissionTagDef"))
                {
                    return "aircraft";
                }

                GeoHavenZoneDef zoneDef = mission.MissionData?.Targets?.OfType<GeoHavenZoneDef>().FirstOrDefault();
                string zone;

                return zoneDef != null && ZoneCodes.TryGetValue(zoneDef.name, out zone) ? zone : null;
            }

            /// Backgrounds are full-screen artwork, so they are decoded the first time a mission asks
            /// for one rather than all at once on load. Misses are cached too.
            private static Sprite LoadBackground(string fileName)
            {
                return LoadBackground(BackgroundsFolder, fileName);
            }

            private static Sprite LoadBackground(string folder, string fileName)
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                string relativePath = string.IsNullOrEmpty(folder) ? fileName + ".jpg" : Path.Combine(folder, fileName + ".jpg");
                Sprite background;

                if (LoadedBackgrounds.TryGetValue(relativePath, out background))
                {
                    return background;
                }

                background = File.Exists(Path.Combine(TFTVMain.TexturesDirectory, relativePath))
                    ? Helper.CreateSpriteFromImageFile(relativePath)
                    : null;

                LoadedBackgrounds.Add(relativePath, background);

                return background;
            }

            internal static void FitFullHeight(RectTransform imageTransform, Sprite background)
            {
                if (imageTransform == null || background == null || background.texture == null)
                {
                    return;
                }

                // The image is rendered on a transformed object in the 3D squad-bay scene. Its parent is
                // not a screen-sized clipping viewport and can be much larger than the visible display.
                // Using the parent's height therefore makes the artwork enormous.
                float availableHeight = imageTransform.rect.height;

                if (availableHeight <= 0f)
                {
                    availableHeight = imageTransform.rect.height;
                }

                float aspect = (float)background.texture.width / background.texture.height;
                imageTransform.sizeDelta = new Vector2(availableHeight * aspect, availableHeight);

                // This is a world-space display viewed through the squad-bay camera, not a normal
                // screen-space Image. A unit scale leaves it as the small rectangle seen in the roster.
                // Retain the scene's established background magnification while preserving scale Z.
                float displayScale = aspect * SquadBayBackgroundScale;
                imageTransform.localScale = new Vector3(
                    displayScale,
                    displayScale,
                    imageTransform.localScale.z);
                // In particular, do not set anchoredPosition3D.z to zero. The inherited Z position places
                // this display correctly in the squad-bay camera; zero moves it into the foreground.
            }

            private static LightCondition GetLightCondition(GeoMission mission)
            {
                if (mission == null || mission.Site == null)
                {
                    return LightCondition.Day;
                }

                return IsDayTime(mission.Site.LocalTime.DateTime.Hour) ? LightCondition.Day : LightCondition.Night;
            }

            /// Shared with the sun/moon icon on the same screen (TFTVUI.Geoscape.MissionDeployment), so
            /// the icon and the artwork behind it never disagree about whether it is night.
            internal static bool IsDayTime(int hourOfDay)
            {
                return hourOfDay >= 6 && hourOfDay <= 20;
            }
        }



        private static Sprite _backgroundSquadDeploy = null;
        private static Sprite _backgroundContainment = null;
        private static Sprite _backgroundBionics = null;
        private static Sprite _activeBackground = null;
        private static Sprite _backgroundMutation = null;
        private static Sprite _backgroundCustomization = null;
        private static Sprite _backgroundMemorial = null;
        private static Sprite _backgroundAirForce = null;
        private static Sprite _backgroundDeployment = null;

        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayMain = null;

        private static void ModifyLightningAndPlatform(Transform transform)
        {
            try
            {
                var sceneLightingDef = DefCache.GetDef<SceneLightingDef>("EditSoldier_LightingDef");
                if (sceneLightingDef == null || transform == null)
                {
                    return; // nothing to modify safely
                }

                if (_activeBackground == _backgroundContainment)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundBionics)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 1.0f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.0f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else if (_activeBackground == _backgroundCustomization)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.9f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.8f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.7f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(false);
                }
                else if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundMemorial)
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.3f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.5f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.3f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(true);
                }
                else
                {
                    sceneLightingDef.LightingData.ambientEquatorColor.b = 0.06f;
                    sceneLightingDef.LightingData.ambientEquatorColor.g = 0.14f;
                    sceneLightingDef.LightingData.ambientEquatorColor.r = 0.49f;
                    if (transform.gameObject != null) transform.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        [HarmonyPatch(typeof(CharacterClassWorldDisplay), nameof(CharacterClassWorldDisplay.SetDisplay))]
        public static class TFTV_CharacterClassWorldDisplay_SetDisplay_patch
        {

            public static bool Prefix(CharacterClassWorldDisplay __instance)
            {
                try
                {
                    __instance.gameObject.SetActive(false);

                    return false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        [HarmonyPatch(typeof(UIStateRosterAliens), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterAliens_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    //   UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                    //   uIModuleGeneralPersonelRoster.RosterList.gameObject.SetActive(true);

                    _activeBackground = _backgroundContainment;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")] //VERIFIED
        public static class TFTV_UIStateEditSoldier_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoCharacterStatus), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoCharacterStatus_EnterState_patch
        {

            public static void Prefix(UIStateGeoCharacterStatus __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoCharacterStatus ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateGeoRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateGeoRoster_EnterState_patch
        {

            public static void Prefix(UIStateGeoRoster __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    // TFTVLogger.Always($"entering UIStateGeoRoster");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateInitial), "EnterState")] //VERIFIED
        public static class TFTV_UIStateInitial_EnterState_patch
        {

            public static void Prefix(UIStateInitial __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateInitial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static bool MemorialPushStateRunning = false;

        [HarmonyPatch(typeof(UIStateMemorial), "PushState")] //VERIFIED
        public static class TFTV_UIStateMemorial_PushState_patch
        {

            public static void Prefix(UIStateMemorial __instance)
            {
                try
                {




                    _activeBackground = _backgroundMemorial;

                    // TFTVLogger.Always($"entering UIStateMemorial");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }


        [HarmonyPatch(typeof(UIStateEditVehicle), "PushState")] //VERIFIED
        public static class TFTV_UIStateEditVehicle_PushState_patch
        {

            public static void Prefix(UIStateEditVehicle __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;
                    //  TFTVLogger.Always($"entering UIStateEditVehicle");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateMutate), "PushState")] //VERIFIED
        public static class TFTV_UIStateMutate_PushState_patch
        {

            public static void Prefix(UIStateMutate __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    // TFTVLogger.Always($"entering UIStateMutate");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateBuyMutoid), "PushState")] //VERIFIED
        public static class TFTV_UIStateBuyMutoid_PushState_patch
        {

            public static void Prefix(UIStateBuyMutoid __instance)
            {
                try
                {
                    _activeBackground = _backgroundMutation;
                    //  TFTVLogger.Always($"entering UIStateBuyMutoid");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateBionics), "PushState")] //VERIFIED
        public static class TFTV_UIStateBionics_PushState_patch
        {

            public static void Prefix(UIStateBionics __instance)
            {
                try
                {
                    _activeBackground = _backgroundBionics;
                    // TFTVLogger.Always($"entering UIStateBionics");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_EnterState_patch
        {

            public static void Prefix(UIStateRosterDeployment __instance)
            {
                try
                {
                    _backgroundDeployment = TFTVBackgroundDeploymentSelector.Select(__instance.Mission);
                    _activeBackground = _backgroundDeployment;

                    // _activeBackground = _backgroundSquadDeploy;
                    //TFTVLogger.Always($"entering UIStateRosterDeployment ");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "PushState")] //VERIFIED
        public static class TFTV_UIStateRosterRecruits_PushState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundSquadDeploy;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")] //VERIFIED
        public static class TFTV_UIStateSoldierCustomization_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundCustomization;
                    // TFTVLogger.Always($"entering UIStateSoldierCustomization ");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void LoadTFTVBackgrounds()
        {
            try
            {
                _backgroundSquadDeploy = Helper.CreateSpriteFromImageFile("squadbay.jpg");
                _backgroundContainment = Helper.CreateSpriteFromImageFile("containment.jpg");
                _backgroundMutation = Helper.CreateSpriteFromImageFile("scenemutation.jpg");
                _backgroundCustomization = Helper.CreateSpriteFromImageFile("scenecustomization.jpg");
                _backgroundBionics = Helper.CreateSpriteFromImageFile("scenebionics.jpg");
                _backgroundMemorial = Helper.CreateSpriteFromImageFile("scenememorial.jpg");
                _backgroundAirForce = Helper.CreateSpriteFromImageFile("sceneairforce.jpg");


                // The per-mission backgrounds are picked and decoded on demand by the selector, which
                // also falls back to deployment_a/b if the artwork folder is missing. Decoding those
                // two here as well would keep a pair of full-size textures resident for nothing.

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void RemoveSceneDoF()
        {
            try
            {
                FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);

                LightingManager lightingManager = context.LightingManager;
                OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                OptionsManager.GraphicsQualityPreset preset = optionsManager.CurrentGraphicsPreset;

                preset.DepthOfField = false;

                MethodInfo methodInfo = typeof(LightingManager).GetMethod("ApplyPostProcessOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(lightingManager, new object[] { preset });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void ChangeSceneBackgroundSquadDeploy(GeoSceneReferences geoSceneReferences)
        {
            try
            {
                if (geoSceneReferences == null || geoSceneReferences.SquadBay == null)
                {
                    return;
                }



                if (_copyCharacterClassWorldDisplayMain != null)
                {
                    if (_copyCharacterClassWorldDisplayMain.gameObject == null) return;

                    _copyCharacterClassWorldDisplayMain.gameObject.SetActive(true);

                    if (_activeBackground == null && _backgroundSquadDeploy == null) return;

                    var bg = _activeBackground ?? _backgroundSquadDeploy;
                    _copyCharacterClassWorldDisplayMain.SingleClassImage.sprite = bg;

                    var backgroundPicRT = _copyCharacterClassWorldDisplayMain.SingleClassImage.GetComponent<RectTransform>();
                    if (backgroundPicRT == null || bg.texture == null) return;

                    float imageAspectCurrentBackground = (float)bg.texture.width / bg.texture.height;

                    // Adjustments per background with guards
                    backgroundPicRT.sizeDelta = new Vector2(backgroundPicRT.rect.height * imageAspectCurrentBackground, backgroundPicRT.rect.height);

                    if (_activeBackground == _backgroundDeployment)
                    {
                        TFTVBackgroundDeploymentSelector.FitFullHeight(backgroundPicRT, bg);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMutation || _activeBackground == _backgroundBionics)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.08f, imageAspectCurrentBackground * 1.08f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                    }
                    else if (_activeBackground == _backgroundCustomization)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.1f, imageAspectCurrentBackground * 1.1f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }
                    else if (_activeBackground == _backgroundMemorial)
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.15f, imageAspectCurrentBackground * 1.15f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, backgroundPicRT.anchoredPosition3D.z + 20);
                        RemoveSceneDoF();
                    }
                    else
                    {
                        backgroundPicRT.localScale = new Vector2(imageAspectCurrentBackground * 1.31f, imageAspectCurrentBackground * 1.31f);
                        backgroundPicRT.anchoredPosition3D = new Vector3(backgroundPicRT.anchoredPosition3D.x, backgroundPicRT.anchoredPosition3D.y, 0);
                        RemoveSceneDoF();
                    }

                    return;
                }

                // Create copy safely
                var sourceDisplay = geoSceneReferences.SquadBay.ClassWorldDisplay;
                if (sourceDisplay == null || sourceDisplay.gameObject == null)
                {
                    return;
                }

                GameObject copy = UnityEngine.Object.Instantiate(sourceDisplay.gameObject, sourceDisplay.transform.parent);
                var copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                if (copyDisplay == null) return;

                _copyCharacterClassWorldDisplayMain = copyDisplay;
                var bg2 = _activeBackground ?? _backgroundSquadDeploy;

                copyDisplay.SingleClassImage.sprite = bg2;

                var rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                if (rt != null && _backgroundSquadDeploy != null && _backgroundSquadDeploy.texture != null)
                {
                    float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                    rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                    rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);
                    rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                    rt.eulerAngles = new Vector3(2.8f, 346, 0);
                }

                copyDisplay.SingleClassImage.gameObject.SetActive(true);
                if (copyDisplay.RightClassImage != null) copyDisplay.RightClassImage.gameObject.SetActive(false);
                if (copyDisplay.LeftClassImage != null) copyDisplay.LeftClassImage.gameObject.SetActive(false);

                // Remove DOF only if geoscape view exists
                RemoveSceneDoF();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // private static Sprite _airForceBackground = null;



        [HarmonyPatch(typeof(UIStateVehicleRoster), "EnterState")] //VERIFIED
        public static class TFTV_UIStateVehicleRoster_EnterState_patch
        {

            public static void Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    _activeBackground = _backgroundAirForce;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static CharacterClassWorldDisplay _copyCharacterClassWorldDisplayVehicleRoster = null;

        [HarmonyPatch(typeof(GeoSceneReferences), nameof(GeoSceneReferences.ActivateScene))]
        public static class TFTV_GeoSceneReferences_ActivateScene_patch
        {
            public static void Prefix(GeoSceneReferences __instance, ActiveSceneReference activeScene, Dictionary<ActiveSceneReference, Transform> ____scenes)
            {
                try
                {

                    // Safety: bail out if the scene reference or scenes map is not usable.
                    if (__instance == null || ____scenes == null)
                    {
                        return;
                    }

                    // Only proceed if the requested scene exists in the map
                    if (!____scenes.ContainsKey(activeScene) || ____scenes[activeScene] == null)
                    {
                        // Still attempt to cleanup UI panel, but don’t touch scene-specific objects
                        TFTVUI.Geoscape.ContainmentScreen.RemoveContainmentInfoPanel();
                        return;
                    }

                    if (activeScene == ActiveSceneReference.SquadBay)
                    {
                        // Guard against missing SquadBay sub-refs in some transitions
                        if (__instance.SquadBay != null)
                        {
                            ChangeSceneBackgroundSquadDeploy(__instance);

                            if (__instance.SquadBay.CharBuilderPlatform != null)
                            {
                                ModifyLightningAndPlatform(__instance.SquadBay.CharBuilderPlatform);
                            }
                        }
                    }
                    else if (activeScene == ActiveSceneReference.VehicleBay)
                    {
                        // Vehicle roster background; make sure base display exists before using it
                        if (_copyCharacterClassWorldDisplayVehicleRoster != null)
                        {
                            if (_copyCharacterClassWorldDisplayVehicleRoster.gameObject != null)
                            {
                                _copyCharacterClassWorldDisplayVehicleRoster.gameObject.SetActive(true);
                            }
                        }
                        else if (__instance.SquadBay != null && __instance.VehicleBay != null)
                        {
                            // Create a copy safely only when the source exists
                            var source = __instance.SquadBay.ClassWorldDisplay;
                            if (source != null && source.gameObject != null)
                            {
                                GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, __instance.VehicleBay.transform);
                                var copyDisplay = copy.GetComponent<CharacterClassWorldDisplay>();
                                if (copyDisplay != null)
                                {
                                    copy.SetActive(true);
                                    copyDisplay.SingleClassImage.sprite = _backgroundAirForce;

                                    RectTransform rt = copyDisplay.SingleClassImage.GetComponent<RectTransform>();
                                    if (rt != null && _backgroundSquadDeploy != null && _backgroundSquadDeploy.texture != null)
                                    {
                                        float imageAspect = (float)_backgroundSquadDeploy.texture.width / _backgroundSquadDeploy.texture.height;
                                        rt.sizeDelta = new Vector2(rt.rect.height * imageAspect, rt.rect.height);
                                        rt.localScale = new Vector2(imageAspect * 1.31f, imageAspect * 1.31f);
                                        rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x - 45, rt.anchoredPosition3D.y - 25, rt.anchoredPosition3D.z);
                                        rt.eulerAngles = new Vector3(2.8f, 346, 0);
                                    }

                                    copyDisplay.SingleClassImage.gameObject.SetActive(true);
                                    if (copyDisplay.RightClassImage != null) copyDisplay.RightClassImage.gameObject.SetActive(false);
                                    if (copyDisplay.LeftClassImage != null) copyDisplay.LeftClassImage.gameObject.SetActive(false);

                                    _copyCharacterClassWorldDisplayVehicleRoster = copyDisplay;
                                }
                            }
                        }
                    }

                    // Cleanup info panel safely every time
                    TFTVUI.Geoscape.ContainmentScreen.RemoveContainmentInfoPanel();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

    }
}
