using System;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Common.Core;
using Base.UI;
using PhoenixPoint.Geoscape.Entities.Sites;
using UnityEngine;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using HarmonyLib;

namespace TFTV.PortedAATweaks.UIEnhancements
{
    /// <summary>
    /// Ported patches from Assorted Adjustment mod.
    /// Original Author: mad2342
    /// Github page: https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments
    /// </summary>
    internal static class ExtendedHavenInfo
    {
        // Show recruit class on haven popup
        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetHaven")]
        public static class UIModuleSelectionInfoBox_SetHaven_Patch
        {
            internal static string recruitAvailableText = "";

            //public static bool Prepare()
            //{
            //    return AssortedAdjustments.Settings.EnableUIEnhancements && AssortedAdjustments.Settings.ShowExtendedHavenInfo;
            //}

            public static void Postfix(UIModuleSelectionInfoBox __instance, GeoSite ____site, bool showRecruits)
            {
                try
                {
                    //Logger.Debug($"[UIModuleSelectionInfoBox_SetHaven_POSTFIX] Haven: {____site.Name}");

                    if (!showRecruits)
                    {
                        return;
                    }

                    GeoUnitDescriptor recruit = ____site.GetComponent<GeoHaven>()?.AvailableRecruit;
                    if (recruit == null)
                    {
                        return;
                    }

                    if (recruit.UnitType.IsHuman)
                    {
                        string className = recruit.Progression.MainSpecDef.ViewElementDef.DisplayName1.Localize();
                        string level = recruit.Level.ToString();
                        IEnumerable<ViewElementDef> abilityViews = recruit.GetPersonalAbilityTrack().AbilitiesByLevel?.Select(a => a?.Ability?.ViewElementDef).Where(e => e != null);
                        string abilities = abilityViews?.Select(v => v.DisplayName1.Localize()).Join(null, "\n");

                        if (string.IsNullOrEmpty(recruitAvailableText))
                        {
                            recruitAvailableText = Utilities.ToTitleCase(__instance.RecruitAvailableText.text.Split((char)32).First() + ":");
                        }
                        __instance.RecruitAvailableText.fontSize = 24;
                        __instance.RecruitAvailableText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        __instance.RecruitAvailableText.text = $"<size=30>{recruitAvailableText} <color=#f4a22c>{className}</color> (Level {level})</size>\n<color=#ecba62>{abilities}</color>";
                    }
                    else
                    {
                        string recruitName = recruit.GetName();
                        if (string.IsNullOrEmpty(recruitAvailableText))
                        {
                            recruitAvailableText = Utilities.ToTitleCase(__instance.RecruitAvailableText.text.Split((char)32).First() + ":");
                        }
                        __instance.RecruitAvailableText.fontSize = 24;
                        __instance.RecruitAvailableText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        __instance.RecruitAvailableText.text = $"<size=30>{recruitAvailableText} <color=#f4a22c>{recruitName}</color></size>";
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        // Show trading info on haven popup
        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetHaven")]
        public static class UIModuleSelectionInfoBox_SetHaven_Patch2
        {
            private static string GetResourceName(ResourceType type)
            {
                switch (type)
                {
                    case ResourceType.Materials: return new LocalizedTextBind("Geoscape/KEY_GEOSCAPE_MATERIALS").Localize();
                    case ResourceType.Supplies: return new LocalizedTextBind("Geoscape/KEY_GEOSCAPE_FOOD").Localize();
                    case ResourceType.Tech: return new LocalizedTextBind("Geoscape/KEY_GEOSCAPE_TECH").Localize();
                }
                return type.ToString();
            }

            private static string GetResourceEntry(int quantity, ResourceType type, int substring = 1)
            {
                string name = GetResourceName(type);
                if (name.Length > substring)
                {
                    name = name.Substring(0, substring);
                }
                if (name.Length > 0)
                {
                    name = $" {Utilities.ToTitleCase(name)}";
                }
                switch (type)
                {
                    case ResourceType.Materials:
                        return $"<color=#ed6e2b>{quantity}{name}</color>";

                    case ResourceType.Supplies:
                        return $"<color=#3def1b>{quantity}{name}</color>";

                    case ResourceType.Tech:
                        return $"<color=#1893e1>{quantity}{name}</color>";
                }
                return $"<color=#FFFFFF>{quantity}{name}</color>";
            }



            //public static bool Prepare()
            //{
            //    return AssortedAdjustments.Settings.EnableUIEnhancements && AssortedAdjustments.Settings.ShowExtendedHavenInfo;
            //}

            public static void Postfix(UIModuleSelectionInfoBox __instance, GeoSite ____site)
            {
                try
                {
                    //Logger.Debug($"[UIModuleSelectionInfoBox_SetHaven_POSTFIX] Haven: {____site.Name}");

                    List<HavenTradingEntry> resourcesAvailable = ____site.GetComponent<GeoHaven>()?.GetResourceTrading();
                    Text textAnchor = __instance.SitePopulationText;

                    if (resourcesAvailable?.Count > 0 && textAnchor != null && ____site.GeoLevel.PhoenixFaction.Research.HasCompleted("PX_HavenTrade_ResearchDef"))
                    {
                        string format = "<size=26>Exchange {0} for {1} ({2})</size>\n";

                        textAnchor.horizontalOverflow = HorizontalWrapMode.Overflow;
                        textAnchor.lineSpacing = 0.8f;

                        textAnchor.text += "\n\n" + string.Concat(resourcesAvailable.Select(e => string.Format(format, GetResourceEntry(e.HavenReceiveQuantity, e.HavenWants, 99), GetResourceEntry(e.HavenOfferQuantity, e.HavenOffers, 99), e.ResourceStock)));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        // Optimize haven defense visuals
        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetHaven")]
        public static class UIModuleSelectionInfoBox_SetHaven_Patch3
        {
            //public static bool Prepare()
            //{
            //    return AssortedAdjustments.Settings.EnableUIEnhancements && AssortedAdjustments.Settings.ShowExtendedHavenInfo;
            //}

            private static string GetColorHexCodeForFaction(object o)
            {
                if (o is GeoFaction geoFaction)
                {
                    return GetColorHexCodeForFaction(geoFaction);
                }
                else if (o is GeoSubFaction geoSubFaction)
                {
                    return GetColorHexCodeForFaction(geoSubFaction);
                }
                else
                {
                    return "#ff0000";
                }
            }

            private static string GetColorHexCodeForFaction(GeoFaction faction)
            {
                return $"#{ColorUtility.ToHtmlStringRGB(faction.Def.FactionColor)}";
            }

            private static string GetColorHexCodeForFaction(GeoSubFaction subFaction)
            {
                return $"#{ColorUtility.ToHtmlStringRGB(subFaction.SubFactionDef.ViewDef.FactionColor)}";
            }

            public static void Postfix(UIModuleSelectionInfoBox __instance, GeoSite ____site)
            {
                try
                {
                    if (!(____site.ActiveMission is GeoHavenDefenseMission mission))
                    {
                        return;
                    }
                    TFTVLogger.Info($"[UIModuleSelectionInfoBox_SetHaven_POSTFIX] Haven: {____site.LocalizedSiteName}, Mission: {mission.MissionName.Localize()}, Zone: {mission.AttackedZone}");

                    GeoFaction defendingFaction = ____site.Owner;
                    object attackingFaction = null;
                    if (mission.Site.GeoLevel.GetFaction(mission.AttackerFaction, true) != null)
                    {
                        attackingFaction = mission.Site.GeoLevel.GetFaction(mission.AttackerFaction, false);
                    }
                    else if (mission.Site.GeoLevel.GetSubFaction(mission.AttackerFaction, true) != null)
                    {
                        attackingFaction = mission.Site.GeoLevel.GetSubFaction(mission.AttackerFaction, false);
                    }

                    string defenderColorHex = GetColorHexCodeForFaction(defendingFaction);
                    string attackerColorHex = GetColorHexCodeForFaction(attackingFaction);

                    string defender = defendingFaction.Name.Localize();
                    string attacker = mission.AttackerName;
                    defender = defender.PadLeft(20, ' ');
                    attacker = attacker.PadRight(20, ' ');
                    defender = $"<color={defenderColorHex}>{defender}</color>";
                    attacker = $"<color={attackerColorHex}>{attacker}</color>";

                    string defenderStrength = $"{mission.FriendlyDefenderDeploymentPoints}";
                    string attackerStrength = $"{mission.FriendlyAttackerDeploymentPoints}";
                    defenderStrength = defenderStrength.PadLeft(2, '0').PadLeft(5, ' ');
                    attackerStrength = attackerStrength.PadLeft(2, '0').PadRight(5, ' ');
                    defenderStrength = $"<size=52><color={defenderColorHex}>{defenderStrength}</color></size>";
                    attackerStrength = $"<size=52><color={attackerColorHex}>{attackerStrength}</color></size>";

                    string attackedZone = mission.AttackedZone.Def.ViewElementDef.DisplayName1.Localize();
                    //string havenDestructionChance = Mathf.Ceil(mission.MissionProgress * 100).ToString();

                    __instance.SiteAttackedByText.fontSize = 26;
                    __instance.SiteAttackedByText.lineSpacing = 0.8f;
                    __instance.SiteAttackedByText.horizontalOverflow = HorizontalWrapMode.Overflow;

                    string battleInfo = "\n";
                    battleInfo += $"<size=30>{attackedZone} UNDER ATTACK</size>\n";
                    battleInfo += $"{defender}  vs  {attacker} \n";
                    battleInfo += $"{defenderStrength}{new string(' ', 15)}{attackerStrength}";

                    // Set
                    __instance.SiteAttackedByText.text = battleInfo;



                    // Hide superfluous stuff
                    __instance.SiteAttackingForceText.gameObject.SetActive(false);
                    __instance.SiteDefendingForceText.gameObject.SetActive(false);
                    __instance.AlienBaseOperationRangeText.gameObject.SetActive(false);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        // Add alertness
        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetHaven")]
        public static class UIModuleSelectionInfoBox_SetHaven_Patch4
        {
            //public static bool Prepare()
            //{
            //    return AssortedAdjustments.Settings.EnableUIEnhancements && AssortedAdjustments.Settings.ShowExtendedHavenInfo;
            //}

            public static void Postfix(UIModuleSelectionInfoBox __instance, GeoSite ____site)
            {
                try
                {
                    GeoHaven haven = ____site.GetComponent<GeoHaven>();
                    if (haven == null)
                    {
                        return;
                    }

                    string alertnessLabel = "Alertness";
                    string alertnessLevel;
                    switch (haven.AlertLevel)
                    {
                        case GeoHaven.HavenAlertLevel.Alert:
                            alertnessLevel = "High";
                            break;
                        case GeoHaven.HavenAlertLevel.HighAlert:
                            alertnessLevel = "Extreme";
                            break;
                        default:
                            alertnessLevel = "Normal";
                            break;
                    }
                    string alertnessText = "\n";
                    alertnessText += $"{alertnessLabel}: {alertnessLevel}";

                    __instance.AlienBaseOperationRangeText.text += $"{alertnessText}";
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
