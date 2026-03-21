using Base.Defs;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVBaseRework
{
    internal class GeoCharacterFilter
    {
        private static bool Enabled => BaseReworkUtils.BaseReworkEnabled;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        private static bool ShouldIncludeInSoldiersResult(GeoPhoenixFaction faction, GeoCharacter character)
        {
            if (character == null)
            {
                return false;
            }

            if (!Enabled)
            {
                return true;
            }

            if (faction != null && character.Faction != faction)
            {
                return false;
            }

            if (!HiddenOperativeMarkerFilter.ShouldHide(character))
            {
                return true;
            }

            PersonnelInfo personnel = PersonnelData.GetPersonnelByUnitId(character.Id);
            if (personnel == null)
            {
                return false;
            }

            switch (personnel.Assignment)
            {
                case PersonnelAssignment.Research:
                case PersonnelAssignment.Manufacturing:
                case PersonnelAssignment.Training:
                    return true;

                default:
                    return false;
            }
        }

        internal class PersonnelCountersAdjustments
        {
            [HarmonyPatch(typeof(GeoSiteVisualsController), "RefreshSiteVisuals")]
            public static class GeoSiteVisualsController_BaseIconAbilityFilterPatch
            {
               
                private static readonly MethodInfo RefreshAvailableSoldiersCountMethod = AccessTools.Method(typeof(GeoSiteVisualsController), "RefreshAvailableSoldiersCount");

                public static void Postfix(GeoSiteVisualsController __instance, GeoSite site)
                {
                    try
                    {

                        if (!BaseReworkUtils.BaseReworkEnabled) return;

                        if (__instance == null || site == null || site.Type != GeoSiteType.PhoenixBase || site.State != GeoSiteState.Functioning)
                        {
                            return;
                        }

                        GeoPhoenixBase phoenixBase = site.GetComponent<GeoPhoenixBase>();
                        if (phoenixBase == null || phoenixBase.Site == null)
                        {
                            return;
                        }

                        int filteredCount = phoenixBase.SoldiersInBase.Count((GeoCharacter character) => character != null && !HiddenOperativeMarkerFilter.ShouldHide(character));
                        RefreshAvailableSoldiersCountMethod?.Invoke(__instance, new object[]
                        {
                filteredCount != 0,
                filteredCount
                        });
                    }
                    catch (Exception ex) { TFTVLogger.Error(ex); }
                }
            }

            [HarmonyPatch(typeof(GeoPhoenixFaction), "get_Soldiers")]
            private static class GeoPhoenixFaction_GetSoldiers_Patch
            {
                private static void Postfix(GeoPhoenixFaction __instance, ref IEnumerable<GeoCharacter> __result)
                {
                    if (!Enabled || __result == null)
                    {
                        return;
                    }

                    __result = __result.Where((GeoCharacter soldier) => ShouldIncludeInSoldiersResult(__instance, soldier));
                }
            }

        }


        internal static class HiddenOperativeMarkerFilter
        {
            internal static bool ShouldHide(GeoCharacter character)
            {
                if (!Enabled)
                {
                    return false;
                }

                return PersonnelRestrictions.IsHiddenFromOperatives(character);
            }

            internal static void ApplyHiddenMarker(GeoCharacter character)
            {
                if (!Enabled || character == null)
                {
                    return;
                }

                PersonnelRestrictions.MarkHiddenFromOperatives(character);
            }

            internal static void RemoveHiddenMarker(GeoCharacter character)
            {
                if (!Enabled || character == null)
                {
                    return;
                }

                PersonnelRestrictions.ClearHiddenFromOperatives(character);
            }

            internal static IEnumerable<GeoCharacter> FilterCharacters(IEnumerable<GeoCharacter> characters)
            {
                if (!Enabled)
                {
                    return characters ?? Enumerable.Empty<GeoCharacter>();
                }

                return (characters ?? Enumerable.Empty<GeoCharacter>())
                    .Where(c => c != null && !ShouldHide(c));
            }

            internal static void FilterList(List<GeoCharacter> characters)
            {
                if (!Enabled || characters == null)
                {
                    return;
                }

                characters.RemoveAll(c => c != null && ShouldHide(c));
            }
        }

        [HarmonyPatch(typeof(UIStateEditSoldier), "OnDismissSoldierDialogCallback")]
        internal static class UIStateEditSoldier_OnDismissSoldierDialogCallback_HiddenOperativeCleanup_Patch
        {
            private static void Postfix(UIStateEditSoldier __instance, MessageBoxCallbackResult msgResult, ref List<GeoCharacter> ____characters)
            {
                if (msgResult.DialogResult != MessageBoxResult.Yes)
                {
                    return;
                }

                ____characters = HiddenOperativeMarkerFilter.FilterCharacters(____characters).ToList();

                foreach (GeoCharacter character in ____characters)
                {
                    TFTVLogger.Always($"[UIStateEditSoldier] Remaining character: {character.DisplayName}");

                }

            }
        }




        [HarmonyPatch(typeof(GeoSite), nameof(GeoSite.GetAllCharacters))]
        internal static class GeoSite_GetAllCharacters_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                if (!Enabled)
                {
                    return;
                }

                __result = HiddenOperativeMarkerFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), nameof(GeoVehicle.GetAllCharacters))]
        internal static class GeoVehicle_GetAllCharacters_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                if (!Enabled)
                {
                    return;
                }

                __result = HiddenOperativeMarkerFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(UIStateGeoRoster), "FilterCharacters")] //VERIFIED
        internal static class UIStateGeoRoster_FilterCharacters_Patch
        {
            private static readonly AccessTools.FieldRef<UIStateGeoRoster, List<GeoCharacter>> CharactersField = AccessTools.FieldRefAccess<UIStateGeoRoster, List<GeoCharacter>>("_characters");

            private static void Postfix(UIStateGeoRoster __instance)
            {
                if (!Enabled)
                {
                    return;
                }

                HiddenOperativeMarkerFilter.FilterList(CharactersField(__instance));
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
                if (!Enabled)
                {
                    return;
                }

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
                        GeoTacUnitsField.SetValue(containerData, HiddenOperativeMarkerFilter.FilterCharacters(units).ToList());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GeoMission), nameof(GeoMission.GetDefaultDeploymentSetup), new Type[] { typeof(IEnumerable<GeoCharacter>) })]
        internal static class GeoMission_GetDefaultDeploymentSetup_FromEnumerable_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                if (!Enabled)
                {
                    return;
                }

                __result = HiddenOperativeMarkerFilter.FilterCharacters(__result);
            }
        }

        [HarmonyPatch(typeof(GeoMission), nameof(GeoMission.GetDefaultDeploymentSetup), new Type[] { typeof(GeoFaction), typeof(IGeoCharacterContainer) })]
        internal static class GeoMission_GetDefaultDeploymentSetup_FromFaction_Patch
        {
            private static void Postfix(ref IEnumerable<GeoCharacter> __result)
            {
                if (!Enabled)
                {
                    return;
                }

                __result = HiddenOperativeMarkerFilter.FilterCharacters(__result);
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
                if (!Enabled)
                {
                    return;
                }

                List<GeoCharacter> selectedDeployment = SelectedDeploymentField(__instance);
                HiddenOperativeMarkerFilter.FilterList(selectedDeployment);

                GeoCharacter initialCharacter = InitialCharacterField(__instance);
                if (HiddenOperativeMarkerFilter.ShouldHide(initialCharacter))
                {
                    InitialCharacterField(__instance) = null;
                }
            }
        }
    }
}
