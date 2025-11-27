using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal class GeoCharacterFilter
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal static class HiddenOperativeTagFilter
        {
            internal const string HiddenTagName = "HiddenFromOperativesTagDef";

            private static GameTagDef _hiddenTag;
         
            internal static bool ShouldHide(GeoCharacter character)
            {
                GameTagDef hiddenTag = EnsureHiddenTag();
                return hiddenTag != null && character != null && character.GameTags.Contains(hiddenTag);
            }

            internal static void ApplyHiddenTag(GeoCharacter character)
            {
                GameTagDef hiddenTag = EnsureHiddenTag();
                if (hiddenTag != null && character != null && !character.GameTags.Contains(hiddenTag))
                {
                    character.GameTags.Add(hiddenTag);
                }
            }

            internal static void RemoveHiddenTag(GeoCharacter character)
            {
                GameTagDef hiddenTag = EnsureHiddenTag();
                if (hiddenTag != null && character != null && character.GameTags.Contains(hiddenTag))
                {
                    character.GameTags.Remove(hiddenTag);
                }
            }

            internal static IEnumerable<GeoCharacter> FilterCharacters(IEnumerable<GeoCharacter> characters)
            {
                GameTagDef hiddenTag = EnsureHiddenTag();
                if (hiddenTag == null)
                {
                    return characters ?? Enumerable.Empty<GeoCharacter>();
                }

                return (characters ?? Enumerable.Empty<GeoCharacter>()).Where(c => c != null && !c.GameTags.Contains(hiddenTag));
            }

            internal static void FilterList(List<GeoCharacter> characters)
            {
                GameTagDef hiddenTag = EnsureHiddenTag();
                if (hiddenTag == null || characters == null)
                {
                    return;
                }

                characters.RemoveAll(c => c != null && c.GameTags.Contains(hiddenTag));
            }

            private static GameTagDef EnsureHiddenTag()
            {
                if (_hiddenTag != null)
                {
                    return _hiddenTag;
                }
 
                try
                {
                    _hiddenTag = TFTVCommonMethods.CreateNewTag(HiddenTagName, "{1A975AB1-68B0-44F4-BC8B-9AE06898A10F}");
                
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"HiddenOperativeTagFilter failed to resolve tag '{HiddenTagName}': {ex}");
                }

                return _hiddenTag;
            }
        }

        [HarmonyPatch(typeof(GeoSite), nameof(GeoSite.GetAllCharacters))]
        internal static class GeoSite_GetAllCharacters_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                __result = HiddenOperativeTagFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), nameof(GeoVehicle.GetAllCharacters))]
        internal static class GeoVehicle_GetAllCharacters_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                __result = HiddenOperativeTagFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(UIStateGeoRoster), "FilterCharacters")] //VERIFIED
        internal static class UIStateGeoRoster_FilterCharacters_Patch
        {
            private static readonly AccessTools.FieldRef<UIStateGeoRoster, List<GeoCharacter>> CharactersField = AccessTools.FieldRefAccess<UIStateGeoRoster, List<GeoCharacter>>("_characters");

            private static void Postfix(UIStateGeoRoster __instance)
            {
                HiddenOperativeTagFilter.FilterList(CharactersField(__instance));
            }
        }

        [HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "Init", new Type[]
        {
        typeof(GeoscapeViewContext),
        typeof(List<IGeoCharacterContainer>),
        typeof(IGeoCharacterContainer),
        typeof(GeoRosterFilterMode),
        typeof(RosterSelectionMode)
        })]
        internal static class UIModuleGeneralPersonelRoster_Init_Patch
        {
            private static readonly FieldInfo UnitContainersField = AccessTools.Field(typeof(UIModuleGeneralPersonelRoster), "_unitContainers");
            private static readonly Type ContainerDataType = AccessTools.Inner(typeof(UIModuleGeneralPersonelRoster), "ContainerData");
            private static readonly FieldInfo GeoTacUnitsField = AccessTools.Field(ContainerDataType, "<GeoTacUnits>k__BackingField");

            private static void Postfix(UIModuleGeneralPersonelRoster __instance)
            {
                if (!(UnitContainersField.GetValue(__instance) is IList containerList))
                {
                    return;
                }

                foreach (object containerData in containerList)
                {
                    if (containerData == null)
                    {
                        continue;
                    }

                    if (GeoTacUnitsField.GetValue(containerData) is IEnumerable<GeoCharacter> units)
                    {
                        GeoTacUnitsField.SetValue(containerData, HiddenOperativeTagFilter.FilterCharacters(units).ToList());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GeoMission), nameof(GeoMission.GetDefaultDeploymentSetup), new Type[] { typeof(IEnumerable<GeoCharacter>) })]
        internal static class GeoMission_GetDefaultDeploymentSetup_FromEnumerable_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                __result = HiddenOperativeTagFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(GeoMission), nameof(GeoMission.GetDefaultDeploymentSetup), new Type[] { typeof(GeoFaction), typeof(IGeoCharacterContainer) })]
        internal static class GeoMission_GetDefaultDeploymentSetup_FromFaction_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                __result = HiddenOperativeTagFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(UIStateRosterDeployment), MethodType.Constructor, new Type[]
        {
        typeof(GeoMission),
        typeof(GeoFaction),
        typeof(IGeoCharacterContainer),
        typeof(bool)
        })]
        internal static class UIStateRosterDeployment_Ctor_Patch
        {
            private static readonly AccessTools.FieldRef<UIStateRosterDeployment, List<GeoCharacter>> SelectedDeploymentField = AccessTools.FieldRefAccess<UIStateRosterDeployment, List<GeoCharacter>>("_selectedDeployment");
            private static readonly AccessTools.FieldRef<UIStateRosterDeployment, GeoCharacter> InitialCharacterField = AccessTools.FieldRefAccess<UIStateRosterDeployment, GeoCharacter>("_initialCharacter");

            private static void Postfix(UIStateRosterDeployment __instance)
            {
                List<GeoCharacter> selectedDeployment = SelectedDeploymentField(__instance);
                HiddenOperativeTagFilter.FilterList(selectedDeployment);

                GeoCharacter initialCharacter = InitialCharacterField(__instance);
                if (HiddenOperativeTagFilter.ShouldHide(initialCharacter))
                {
                    InitialCharacterField(__instance) = null;
                }
            }
        }
    }
}
