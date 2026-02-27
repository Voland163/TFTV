using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVIncidents
{
    internal class GeoUIAdjustments
    {
        internal static class SiteContextMenuSpecialMissionPatch
        {
            private const string SpecialDeployTextKey = "KEY_INCIDENT_START_OPTION";
            private const string SpecialDeployTooltipKey = "KEY_INCIDENT_START_OPTION_TOOLTIP";
            private const string IncidentEventPrefix = "TFTV_INCIDENT_";
            private const string IncidentIntroSuffix = "_INTRO";

        

            [HarmonyPatch(typeof(UIModuleSiteContextualMenu), nameof(UIModuleSiteContextualMenu.SetMenuItems))]
            private static class UIModuleSiteContextualMenu_SetMenuItems_Patch
            {
                private static void Prefix(List<GeoAbility> rawAbilities)
                {


                    if (rawAbilities == null)
                    {
                        return;
                    }

                    rawAbilities.RemoveAll(a => a is ScanAbility);
                }


                private static void Postfix(UIModuleSiteContextualMenu __instance, GeoSite site)
                {
                    GeoSite selectedSite = site ?? __instance?.SelectedSite;
                    if (!SiteHasIncidentIntroEvent(selectedSite) || __instance?.ButtonsHolder == null)
                    {
                        return;
                    }

                    string deployText = LocalizeKeySafe(SpecialDeployTextKey);

                    foreach (object child in __instance.ButtonsHolder.transform)
                    {
                        SiteContextualMenuItem menuItem = ((UnityEngine.Transform)child).GetComponent<SiteContextualMenuItem>();
                        if (menuItem?.Ability == null)
                        {
                            continue;
                        }

                        if (IsIncidentStartAbility(menuItem.Ability))
                        {
                            menuItem.ItemText.text = deployText;
                            return;
                        }
                    }

                    foreach (object child in __instance.ButtonsHolder.transform)
                    {
                        SiteContextualMenuItem menuItem = ((UnityEngine.Transform)child).GetComponent<SiteContextualMenuItem>();
                        if (menuItem?.Ability != null && menuItem.ItemText != null)
                        {
                            menuItem.ItemText.text = deployText;
                            return;
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleSiteContextualMenu), "OnAbilityHover")]
            private static class UIModuleSiteContextualMenu_OnAbilityHover_Patch
            {
                public static void Postfix(UIModuleSiteContextualMenu __instance, bool isHovered, SiteContextualMenuItem menuItem)
                {
                    GeoSite site = __instance?.SelectedSite;
                    if (!isHovered || menuItem?.Ability == null || site == null || !SiteHasIncidentIntroEvent(site))
                    {
                        return;
                    }

                    if (!IsIncidentStartAbility(menuItem.Ability))
                    {
                        return;
                    }

                    GeoAbilityTarget target = new GeoAbilityTarget(site)
                    {
                        Faction = site.GeoLevel.ViewerFaction
                    };

                    bool isEnabled = menuItem.Ability.View != null && menuItem.Ability.View.CanActivate(target);
                    bool showResources = __instance.DescriptionBox.ResourcesContainer.activeSelf;
                    __instance.DescriptionBox.SetDescription(LocalizeKeySafe(SpecialDeployTooltipKey), showResources, isEnabled);
                }
            }

            private static bool SiteHasIncidentIntroEvent(GeoSite site)
            {
                if (site?.GeoLevel?.EventSystem == null || string.IsNullOrEmpty(site.EncounterID))
                {
                    TFTVLogger.Always($"[Incidents UI] Site {site?.name ?? "<null>"} has no encounter/event system.");
                    return false;
                }

                GeoscapeEventDef encounterEvent = site.GeoLevel.EventSystem.GetEventByID(site.EncounterID, true);
                if (encounterEvent == null || string.IsNullOrEmpty(encounterEvent.EventID))
                {
                    TFTVLogger.Always("[Incidents UI] Encounter event is null or has no EventID.");
                    return false;
                }

                if (IsIncidentIntroEventId(encounterEvent.EventID))
                {
                    TFTVLogger.Always($"[Incidents UI] Detected intro event: {encounterEvent.EventID}");
                    return true;
                }

                bool inDefinitions = GeoscapeEvents.IncidentDefinitions != null
                    && GeoscapeEvents.IncidentDefinitions.Any(i =>
                        i != null
                        && i.IntroEvent != null
                        && !string.IsNullOrEmpty(i.IntroEvent.EventID)
                        && string.Equals(i.IntroEvent.EventID, encounterEvent.EventID, StringComparison.OrdinalIgnoreCase));

                if (inDefinitions)
                {
                    TFTVLogger.Always($"[Incidents UI] Detected intro event from definitions: {encounterEvent.EventID}");
                }

                return inDefinitions;
            }

            private static bool IsIncidentIntroEventId(string eventId)
            {
                return !string.IsNullOrEmpty(eventId)
                    && eventId.StartsWith(IncidentEventPrefix, StringComparison.OrdinalIgnoreCase)
                    && eventId.EndsWith(IncidentIntroSuffix, StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsIncidentStartAbility(GeoAbility ability)
            {
                if (ability == null)
                {
                    return false;
                }

                if (ability is LaunchMissionAbility)
                {
                    return true;
                }

                string typeName = ability.GetType().Name;
                return typeName.IndexOf("TriggerGeoscapeEvent", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("TriggerEvent", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Encounter", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static string LocalizeKeySafe(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }

                try
                {
                    string localized = TFTVCommonMethods.ConvertKeyToString(key);
                    return string.IsNullOrEmpty(localized) ? key : localized;
                }
                catch
                {
                    return key;
                }
            }
        }
    }
}
