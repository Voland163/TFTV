using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVBaseRework
{
    partial class BaseActivation
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static class PhoenixBaseReworkState
        {
            public const string OutpostTag = "PX_REWORK_OUTPOST";
            public const string LootedTag = "PX_REWORK_LOOTED";
            public const string ManticoreCountTagPrefix = "PX_REWORK_MANTICORE_COUNT:";
            public const string ScarabCountTagPrefix = "PX_REWORK_SCARAB_COUNT:";
            public const string PendingOutpostTag = "PX_REWORK_PENDING_OUTPOST";
            public const string PendingBaseTag = "PX_REWORK_PENDING_BASE";
            public const string PendingBaseUpgradeTag = "PX_REWORK_PENDING_BASE_UPGRADE";
            public const string FirstVisitPreviewTagPrefix = "PX_REWORK_FIRST_VISIT_PREVIEW:";
            public const string LootResultTagPrefix = "PX_REWORK_LOOT_RESULT:";
        }

        internal enum PendingBaseAction
        {
            Outpost,
            FullBase,
            UpgradeToBase
        }

        internal class HideVanillaUIPatches
        {
            public static bool PreventEnterBaseAbilityForOutpost(EnterBaseAbility __instance, GeoAbilityTarget target)
            {
                try
                {
                    if (BaseReworkUtils.BaseReworkEnabled && target.Actor is GeoSite site && site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                    {
                        return true;
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch]
            public static class HideDestroyedPhoenixBaseTooltipPatch
            {
                // Patch both states that can display the short Phoenix base tooltip.
                static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
                {
                    yield return AccessTools.Method(typeof(UIStateNothingSelected), "OnSiteMouseHover");
                    yield return AccessTools.Method(typeof(UIStateVehicleSelected), "OnSiteMouseHover");
                }

                // If hovering a destroyed Phoenix Base, force handler to behave as "no site hovered".
                static void Prefix(ref GeoSite site)
                {
                    if (site != null &&
                        site.Type == GeoSiteType.PhoenixBase &&
                        site.State == GeoSiteState.Destroyed)
                    {
                        site = null;
                    }
                }
            }

            [HarmonyPatch(typeof(ActivateBaseAbility), "GetDisabledStateInternal")]
            internal static class ActivateBaseAbility_GetDisabledStateInternal_patch
            {
                public static bool Prefix(ActivateBaseAbility __instance, ref GeoAbilityDisabledState __result)
                {
                    try
                    {
                        if (!BaseReworkUtils.BaseReworkEnabled)
                        {
                            return true;
                        }


                        GeoSite geoSite = __instance.GeoActor as GeoSite;
                        if (geoSite == null || geoSite.Type != GeoSiteType.PhoenixBase || geoSite.ExpiringTimerAt > 0) //added expiring timer check to prevent activating while pending action is in progress
                        {
                            __result = GeoAbilityDisabledState.InvalidAbilityActor;
                            return false;
                        }

                        if (geoSite.State != GeoSiteState.Abandoned && !geoSite.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag)) //added case for outpost since it uses the same ability but should be activatable
                        {
                            __result = GeoAbilityDisabledState.RequirementsNotMet;
                            return false;
                        }

                        if (!geoSite.Owner.IsEnvironmentFaction && !geoSite.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag)) //added case for outpost since it uses the same ability but should be activatable
                        {
                            __result = GeoAbilityDisabledState.PhoenixBaseIsAlreadyActivated;
                            return false;
                        }

                        if (geoSite.HasActiveMission)
                        {
                            __result = GeoAbilityDisabledState.PhoenixBaseIsAlreadyActivated;
                            return false;
                        }

                        __result = GeoAbilityDisabledState.NotDisabled;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }
            }



        }
       

        [HarmonyPatch(typeof(GeoVehicle), "OnArrivedAtDestination")]
        internal static class GeoVehicle_OnArrivedAtDestination_OpenActivationUI_patch
        {
         
            public static void Postfix(GeoVehicle __instance, bool justPassing)
            {
                try
                {
                    if (!BaseReworkUtils.BaseReworkEnabled || justPassing || __instance == null)
                    {
                        return;
                    }

                    if (BaseInitialLoot.JustLootedManticore !=null && BaseInitialLoot.JustLootedManticore== __instance)
                    {
                        TFTVLogger.Always($"[GeoVehicle.OnArrivedAtDestination] JustLootedManticore arrived at destination: {__instance.Name}");
                        BaseInitialLoot.JustLootedManticore = null;
                        return;
                    }

                    GeoSite site = __instance.CurrentSite;

                    if (!(__instance.Owner is GeoPhoenixFaction faction) || site == null || site.Type != GeoSiteType.PhoenixBase)
                    {
                        return;
                    }

                    bool isAbandonedPhoenixSite = site.State == GeoSiteState.Abandoned && site.Owner != null && site.Owner.IsEnvironmentFaction;
                    bool isOutpost = site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);

                    if (!isAbandonedPhoenixSite && !isOutpost)
                    {
                        return;
                    }

                    ActivateBaseAbility activateAbility = site.GetAbilities<GeoAbility>()
                        .OfType<ActivateBaseAbility>()
                        .FirstOrDefault();

                    if (activateAbility == null)
                    {
                        return;
                    }

                    GeoAbilityTarget target = new GeoAbilityTarget(site) { Faction = faction };
                    if (!activateAbility.CanActivate(target))
                    {
                        return;
                    }

                    if (activateAbility.View != null && activateAbility.View.HasActivationUI)
                    {
                        activateAbility.View.ShowActivationUI(target);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }


        internal static class PhoenixBaseVisitFlow
        {

            internal static bool HasPhoenixVehicleAtSite(GeoSite site, GeoPhoenixFaction faction)
            {
                try
                {
                    return site?.Vehicles != null && faction != null && site.Vehicles.Any(v => v != null && v.Owner == faction);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }



            private static readonly ResourcePack OutpostCost = new ResourcePack(new[]
                {
                new ResourceUnit(ResourceType.Materials, 200f),
                new ResourceUnit(ResourceType.Tech, 50f)
            });

            private static readonly ResourcePack FullBaseAdditionalCost = new ResourcePack(new[]
            {
                new ResourceUnit(ResourceType.Materials, 400f),
                new ResourceUnit(ResourceType.Tech, 100f)
            });



            internal static bool TryQueueFullBaseFromActivationUI(GeoSite site, GeoPhoenixFaction faction, bool fromOutpost)
            {
                try
                {
                    if (site == null || faction == null || HasPendingAction(site))
                    {
                        return false;
                    }

                    return EstablishBase(site, faction, fromOutpost);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            internal static bool TrySetOutpostFromActivationUI(GeoSite site, GeoPhoenixFaction faction)
            {
                try
                {
                    if (site == null || faction == null || HasPendingAction(site))
                    {
                        return false;
                    }

                    return SetOutpost(site, faction);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            internal static bool TryRansackFromActivationUI(GeoSite site, GeoPhoenixFaction faction)
            {
                try
                {
                    if (site == null || faction == null || HasPendingAction(site))
                    {
                        return false;
                    }

                    BaseRansack.Ransack(site, faction);
                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }


            private static bool SetOutpost(GeoSite site, GeoPhoenixFaction faction)
            {
                try
                {
                    if (!TryPayAndConsumePersonnel(faction, site, OutpostCost, 1))
                    {
                        return false;
                    }

                    StartPendingAction(site, faction, PendingBaseAction.Outpost, 24);
                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            private static bool EstablishBase(GeoSite site, GeoPhoenixFaction faction, bool fromOutpost)
            {
                try
                {
                    int personnel = fromOutpost ? 3 : 4;
                    ResourcePack cost = fromOutpost ? OutpostCost : FullBaseAdditionalCost;
                    if (!TryPayAndConsumePersonnel(faction, site, cost, personnel))
                    {
                        return false;
                    }

                    StartPendingAction(site, faction, fromOutpost ? PendingBaseAction.UpgradeToBase : PendingBaseAction.FullBase, fromOutpost ? 48 : 72);
                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            private static bool TryPayAndConsumePersonnel(GeoPhoenixFaction faction, GeoSite site, ResourcePack cost, int requiredPersonnel)
            {
                try
                {
                    if (!faction.Wallet.HasResources(cost))
                    {
                        return false;
                    }

                    if (BaseReworkUtils.BaseReworkEnabled)
                    {
                        if (!PersonnelData.TryConsumePersonnelForBaseActivation(faction, requiredPersonnel))
                        {
                            return false;
                        }

                        faction.Wallet.Take(cost, OperationReason.Purchase);
                        return true;
                    }

                    List<GeoCharacter> personnel = site.Units
                        .Concat(site.Vehicles.Where(v => v.Owner == faction).SelectMany(v => v.Units))
                        .Where(c => c?.TemplateDef != null && c.TemplateDef.IsHuman)
                        .Take(requiredPersonnel)
                        .ToList();

                    if (personnel.Count < requiredPersonnel)
                    {
                        return false;
                    }

                    faction.Wallet.Take(cost, OperationReason.Purchase);
                    foreach (GeoCharacter person in personnel)
                    {
                        faction.KillCharacter(person, CharacterDeathReason.Dismissed);
                    }


                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            private static bool HasPendingAction(GeoSite site)
            {
                return site.SiteTags.Contains(PhoenixBaseReworkState.PendingOutpostTag)
                    || site.SiteTags.Contains(PhoenixBaseReworkState.PendingBaseTag)
                    || site.SiteTags.Contains(PhoenixBaseReworkState.PendingBaseUpgradeTag);
            }
            public static bool HasPendingActionPublic(GeoSite site)
            {
                return HasPendingAction(site);
            }

            public static float GetPendingDurationHours(GeoSite site)
            {
                if (site.SiteTags.Contains(PhoenixBaseReworkState.PendingOutpostTag))
                {
                    return 24f;
                }

                if (site.SiteTags.Contains(PhoenixBaseReworkState.PendingBaseUpgradeTag))
                {
                    return 48f;
                }

                return 72f;
            }



            private static void StartPendingAction(GeoSite site, GeoPhoenixFaction faction, PendingBaseAction action, int hours)
            {
                GeoLevelController level = site?.GeoLevel;
                GeoscapeEventSystem eventSystem = level?.EventSystem;
                if (level == null || eventSystem == null)
                {
                    return;
                }

                TimeUnit duration = TimeUnit.FromHours(hours);
                string timerId = BaseConstructionVisuals.BuildPendingTimerId(site, action);
                GeoEventTimer timer = eventSystem.StartTimer(timerId, duration);

                TFTVLogger.Always($"[BaseActivation] StartPendingAction: site={site?.SiteId}, action={action}, hours={hours}, owner={site?.Owner?.Name}");
                TFTVLogger.Always($"[BaseActivation] Pending timer set: site={site.SiteId}, start={timer.StartAt}, end={timer.EndAt}, id={timerId}");

                ClearPendingTags(site);
                site.SiteTags.Add(action == PendingBaseAction.Outpost
                    ? PhoenixBaseReworkState.PendingOutpostTag
                    : (action == PendingBaseAction.UpgradeToBase
                        ? PhoenixBaseReworkState.PendingBaseUpgradeTag
                        : PhoenixBaseReworkState.PendingBaseTag));

                site.ExpiringTimerAt = timer.EndAt;

                BaseConstructionVisuals.ActivePendingByTimerId[timerId] = new BaseConstructionVisuals.PendingActionInfo
                {
                    TimerId = timerId,
                    SiteId = site.SiteId,
                    Action = action,
                    StartAt = timer.StartAt,
                    EndAt = timer.EndAt
                };

                site.RefreshVisuals();
                BaseConstructionVisuals.RefreshPendingConstructionVisuals(level);
            }

            internal static NextUpdate CompletePendingAction(GeoSite site, GeoPhoenixFaction faction, PendingBaseAction action)
            {
                try
                {
                    if (site == null || faction == null)
                    {
                        return NextUpdate.StopScheduler;
                    }

                    TFTVLogger.Always($"[BaseActivation] CompletePendingAction: site={site.SiteId}, action={action}, owner={site.Owner?.Name}");

                    if (action == PendingBaseAction.Outpost)
                    {
                        if (site.Owner.IsEnvironmentFaction)
                        {
                            faction.ActivateBaseFromExploration(site);
                        }
                        site.SiteTags.Add(PhoenixBaseReworkState.OutpostTag);
                        site.SiteProduction = new ResourcePack();
                    }
                    else
                    {
                        if (site.Owner.IsEnvironmentFaction)
                        {
                            faction.ActivateBaseFromExploration(site);
                        }
                        site.SiteTags.Remove(PhoenixBaseReworkState.OutpostTag);
                    }

                    ClearPendingTags(site);
                    site.ExpiringTimerAt = TimeUnit.Zero;
                    site.RefreshVisuals();
                    BaseConstructionVisuals.RefreshPendingConstructionVisuals(site.GeoLevel);
                    return NextUpdate.StopScheduler;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return NextUpdate.StopScheduler;
                }
            }

            private static void ClearPendingTags(GeoSite site)
            {
                site.SiteTags.Remove(PhoenixBaseReworkState.PendingOutpostTag);
                site.SiteTags.Remove(PhoenixBaseReworkState.PendingBaseTag);
                site.SiteTags.Remove(PhoenixBaseReworkState.PendingBaseUpgradeTag);
            }

            internal static ResourcePack GetOutpostCostPack()
            {
                return OutpostCost;
            }

            internal static int GetOutpostPersonnelCost()
            {
                return 1;
            }

            internal static ResourcePack GetBaseQueueCostPack(bool fromOutpost)
            {
                return fromOutpost ? OutpostCost : FullBaseAdditionalCost;
            }

            internal static int GetBaseQueuePersonnelCost(bool fromOutpost)
            {
                return fromOutpost ? 3 : 4;
            }

            internal static bool CanAffordOutpost(GeoPhoenixFaction faction)
            {
                return HasResourcesAndPersonnel(faction, OutpostCost, GetOutpostPersonnelCost());
            }

            internal static bool CanAffordBaseQueue(GeoPhoenixFaction faction, bool fromOutpost)
            {
                return HasResourcesAndPersonnel(faction, GetBaseQueueCostPack(fromOutpost), GetBaseQueuePersonnelCost(fromOutpost));
            }

            private static bool HasResourcesAndPersonnel(GeoPhoenixFaction faction, ResourcePack cost, int personnelRequired)
            {
                if (faction == null)
                {
                    return false;
                }

                if (cost != null && !faction.Wallet.HasResources(cost))
                {
                    return false;
                }

                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return true;
                }

                if (personnelRequired <= 0)
                {
                    return true;
                }

                return PersonnelData.GetAvailablePersonnelCount(faction) >= personnelRequired;
            }
        }
    }
}