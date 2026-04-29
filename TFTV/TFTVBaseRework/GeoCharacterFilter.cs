using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVBaseRework
{
    internal class GeoCharacterFilter
    {
        private static bool Enabled => BaseReworkCheck.BaseReworkEnabled;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        private static bool ShouldIncludeInSoldiersResult(GeoCharacter character)
        {
            if (character == null)
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
            internal static class RosterFilterPolicy
            {
                // Customize this however you want.
                public static bool ShouldHide(GeoCharacter c)
                {
                    if (c == null) return true;


                    return HiddenOperativeMarkerFilter.ShouldHide(c);

                }
            }

            [HarmonyPatch(typeof(UIModuleGeneralPersonelRoster), "InitRosterSlots")]
            internal static class Patch_UIModuleGeneralPersonelRoster_InitRosterSlots_HideRows
            {
                static void Postfix(UIModuleGeneralPersonelRoster __instance)
                {
                    if (__instance?.Slots == null) return;

                    foreach (var slot in __instance.Slots)
                    {
                        var c = slot.Character;
                        if (c != null && RosterFilterPolicy.ShouldHide(c))
                        {
                            slot.gameObject.SetActive(false);
                        }
                    }

                    // private RefreshNavigation()
                    Traverse.Create(__instance).Method("RefreshNavigation").GetValue();
                }
            }

            [HarmonyPatch(typeof(GeoscapeView), "ToEditUnitState")]
            [HarmonyPatch(new Type[] { typeof(GeoCharacter), typeof(IEnumerable<GeoCharacter>), typeof(StateStackAction) })]
            internal static class Patch_GeoscapeView_ToEditUnitState_FilterCharacters
            {
                static void Prefix(
                    GeoscapeView __instance,
                    ref GeoCharacter initCharacter,
                    ref IEnumerable<GeoCharacter> characters)
                {
                    bool Visible(GeoCharacter c) => c != null && !RosterFilterPolicy.ShouldHide(c);

                    // Mirror original fallback, but filtered.
                    var source = characters ?? __instance.GetFactionCharacters(null);

                    // Materialize once.
                    var filtered = source.Where(Visible).Distinct().ToList();

                    // If currently selected initCharacter is hidden, pick first visible.
                    if (initCharacter == null || !Visible(initCharacter))
                    {
                        initCharacter = filtered.FirstOrDefault();
                    }

                    // Feed filtered list into original method.
                    characters = filtered;
                }
            }


            [HarmonyPatch(typeof(GeoSiteVisualsController), "RefreshSiteVisuals")]
            public static class GeoSiteVisualsController_BaseIconAbilityFilterPatch
            {

                private static readonly MethodInfo RefreshAvailableSoldiersCountMethod = AccessTools.Method(typeof(GeoSiteVisualsController), "RefreshAvailableSoldiersCount");

                public static void Postfix(GeoSiteVisualsController __instance, GeoSite site)
                {
                    try
                    {

                        if (!BaseReworkCheck.BaseReworkEnabled) return;

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

                    __result = __result.Where((GeoCharacter soldier) => ShouldIncludeInSoldiersResult(soldier));
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
                if (character == null)
                {
                    return;
                }

                PersonnelRestrictions.MarkHiddenFromOperatives(character);
            }

            internal static void RemoveHiddenMarker(GeoCharacter character)
            {
                if (character == null)
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

        [HarmonyPatch(typeof(SiteManagementRow), "ShowSiteStats")]
        internal static class Patch_SiteManagementRow_ShowSiteStats
        {
            private static void Postfix(SiteManagementRow __instance)
            {

                if (!Enabled)
                {
                    return;
                }

                //  TFTVLogger.Always($"Postfix: SiteManagementRow.ShowSiteStats called for site: {__instance?.Site?.Name} __instance.PersonnelNumber.text: {__instance?.PersonnelNumber?.text}");

                if (__instance?.Site == null || __instance.PersonnelNumber == null || __instance.Site.GetComponent<GeoPhoenixBase>() == null)
                    return;


                int modified = __instance.Site.GetComponent<GeoPhoenixBase>().SoldiersInBase.Where(c => c != null && c.TemplateDef.IsHuman && !HiddenOperativeMarkerFilter.ShouldHide(c)).Count();

                // TFTVLogger.Always($"Postfix: Calculated modified personnel count: {modified}");

                __instance.PersonnelNumber.text = modified.ToString();
            }
        }

        [HarmonyPatch(typeof(UIModuleBaseLayout), "SetLeftSideInfo")]
        internal static class Patch_UIModuleBaseLayout_SetLeftSideInfo
        {
            private static void Postfix(UIModuleBaseLayout __instance)
            {
                if (!Enabled)
                {
                    return;
                }


                if (__instance?.PxBase == null || __instance.PersonnelAtBaseText == null)
                    return;



                // Replace with your logic:
                int modified = __instance.PxBase.SoldiersInBase.Where(c => c != null && c.TemplateDef.IsHuman && !HiddenOperativeMarkerFilter.ShouldHide(c)).Count();

                __instance.PersonnelAtBaseText.text = modified.ToString();
            }


        }

    }
}
