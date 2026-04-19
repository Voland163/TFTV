using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TFTV.AgendaTracker
{
    internal static class AgendaConstants
    {
        // Localized site names
        internal static bool fetchedSiteNames = false;
        internal static string unexploredSiteName = "UNEXPLORED SITE";
        internal static string explorationSiteName = "EXPLORATION SITE";
        internal static string scavengingSiteName = "SCAVENGING SITE";
        internal static string ancientSiteName = "ANCIENT SITE";

        // Localized action labels
        internal static string actionExploring = "INVESTIGATES";
        internal static string actionTraveling = "TRAVELS TO";
        internal static string actionRepairing = "REPAIRING:";
        internal static string actionExcavating = "EXCAVATING:";
        internal static string actionAcquire = "SECURE";
        internal static string actionAttack = "WILL ATTACK:";
        internal static string actionAttackOnPX = "WILL COMPLETE ATTACK ON";
        internal static string actionResolvingIncident = "RESOLVING INCIDENT:";
        internal static string actionActivatingOutpost = "ACTIVATING OUTPOST:";
        internal static string actionActivatingBase = "ACTIVATING BASE:";
        internal static string actionUpgradingBase = "UPGRADING BASE:";
        internal static string actionTrainingOperative = "TRAINING:";

        // Tracker colors
        internal static readonly Color vehicleTrackerColor = new Color32(251, 191, 31, 255);
        internal static readonly Color manufactureTrackerColor = new Color32(235, 110, 42, 255);
        internal static readonly Color researchTrackerColor = new Color32(42, 245, 252, 255);
        internal static readonly Color excavationTrackerColor = new Color32(93, 153, 106, 255);
        internal static readonly Color baseAttackTrackerColor = new Color32(192, 32, 32, 255);
        internal static readonly Color facilityTrackerColor = new Color32(185, 185, 185, 255);
        internal static readonly Color defaultTrackerColor = new Color32(155, 155, 155, 255);
        internal static readonly Color incidentTrackerColor = new Color32(126, 214, 223, 255);
        internal static readonly Color baseActivationTrackerColor = new Color32(120, 196, 120, 255);
        internal static readonly Color trainingTrackerColor = new Color32(163, 214, 72, 255);

        // Sprites (fetched once at runtime)
        internal static Sprite aircraftSprite = null;
        internal static Sprite ancientSiteProbeSprite = null;
        internal static Sprite archeologyLabSprite = null;
        internal static Sprite phoenixFactionSprite = null;

        // Default element prefab for color resets
        internal static UIFactionDataTrackerElement trackerElementDefault = null;

        // Cached tracker instance
        internal static UIModuleFactionAgendaTracker factionTracker = null;

        internal static bool pendingRefreshAfterBaseReworkRestore = false;

        // Cached reflected methods on UIModuleFactionAgendaTracker
        internal static readonly MethodInfo GetFreeElement =
            typeof(UIModuleFactionAgendaTracker).GetMethod("GetFreeElement", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly MethodInfo OnAddedElement =
            typeof(UIModuleFactionAgendaTracker).GetMethod("OnAddedElement", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly MethodInfo OrderElements =
            typeof(UIModuleFactionAgendaTracker).GetMethod("OrderElements", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly MethodInfo UpdateData =
            typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        internal static readonly MethodInfo Dispose =
            typeof(UIModuleFactionAgendaTracker).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);

        // Cached reflected fields on GeoVehicle for exploration time
        internal static readonly FieldInfo ExplorationUpdateableField =
            typeof(GeoVehicle).GetField("_explorationUpdateable", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly PropertyInfo ExplorationUpdateableNextUpdateProperty =
            ExplorationUpdateableField?.FieldType?.GetProperty("NextUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Cached field for tracked elements list
        internal static readonly FieldInfo CurrentTrackedElementsField =
            AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements");

        internal static List<UIFactionDataTrackerElement> GetTrackedElements()
        {
            return factionTracker != null
                ? (List<UIFactionDataTrackerElement>)CurrentTrackedElementsField.GetValue(factionTracker)
                : null;
        }

        internal static void LocalizeExtendedAgendaUI()
        {
            try
            {
                unexploredSiteName = Localize("EXTENDED_AGENDA_KEY_UNEXPLORED_SITE");
                explorationSiteName = Localize("EXTENDED_AGENDA_KEY_EXPLORATION_SITE");
                scavengingSiteName = Localize("EXTENDED_AGENDA_KEY_SCAVENGING_SITE");
                ancientSiteName = Localize("EXTENDED_AGENDA_KEY_ANCIENT_SITE");
                actionExploring = Localize("EXTENDED_AGENDA_KEY_INVESTIGATES");
                actionTraveling = Localize("EXTENDED_AGENDA_KEY_TRAVELS_TO");
                actionRepairing = Localize("EXTENDED_AGENDA_KEY_REPAIRING");
                actionExcavating = Localize("EXTENDED_AGENDA_KEY_EXCAVATING");
                actionAcquire = Localize("EXTENDED_AGENDA_KEY_SECURE");
                actionAttack = Localize("EXTENDED_AGENDA_KEY_WILL_ATTACK");
                actionAttackOnPX = Localize("EXTENDED_AGENDA_KEY_WILL_COMPLETE_ATTACK_ON");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string Localize(string key)
        {
            return new LocalizedTextBind() { LocalizationKey = key }.Localize();
        }
    }
}