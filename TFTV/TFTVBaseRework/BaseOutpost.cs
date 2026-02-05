using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV.TFTVBaseRework
{

    public static class BaseActivationModalPatchState
    {
        public static GeoPhoenixBase LastActivatedBase;
        internal static string TitleOutpostActivated = "";
        internal static string DescriptionOutpostActivated = "";

        internal static void LoadLocalizedStrings()
        {
            TitleOutpostActivated = TFTVCommonMethods.ConvertKeyToString("KEY_OUTPOST_ACTIVATED_TITLE");
            DescriptionOutpostActivated = TFTVCommonMethods.ConvertKeyToString("KEY_OUTPOST_ACTIVATED_DESCRIPTION");
        }
    }

    [HarmonyPatch(typeof(GeoscapeView), "PxFaction_OnBaseActivated")]
    public static class Patch_CaptureActivatedBase
    {
        static void Postfix(GeoPhoenixBase @base, bool activatedFromExploration)
        {
            if (!activatedFromExploration) return;
            BaseActivationModalPatchState.LastActivatedBase = @base;
          
            if (BaseActivationModalPatchState.TitleOutpostActivated == "" || BaseActivationModalPatchState.DescriptionOutpostActivated == "")
            {
                BaseActivationModalPatchState.LoadLocalizedStrings();
            }

        }
    }

    [HarmonyPatch(typeof(UIModuleModal), "Show")]
    public static class Patch_EditBaseOutcomeModalText
    {
        static void Postfix(UIModuleModal __instance, ModalType modal)
        {
            if (modal != ModalType.GeoPhoenixBaseOutcome) return;

            var pxBase = BaseActivationModalPatchState.LastActivatedBase;

            TFTVLogger.Always($"[Patch_EditBaseOutcomeModalText] Modal shown for {pxBase?.Site?.LocalizedSiteName}. Modal type: {modal}. Is Outpost? {pxBase?.Site?.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag)}");

            if (pxBase?.Site == null) return;

            // Example: string-based site tag check
            bool hasSpecialTag = pxBase.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);
            if (!hasSpecialTag) return;

            var modalData = __instance.AvailableModals.FirstOrDefault(m => m.Type == modal);
            var modalObj = modalData.Modal;
            if (modalObj == null) return;

            foreach (var t in modalObj.GetComponentsInChildren<Text>(true))
            {
                // TFTVLogger.Always($"[Patch_EditBaseOutcomeModalText] Checking text component: {t.name}. Current text: {t.text}");

                if (t.name == "Title") t.text = BaseActivationModalPatchState.TitleOutpostActivated;
                if (t.name == "DescriptionText") t.text = BaseActivationModalPatchState.DescriptionOutpostActivated;

                BaseActivationModalPatchState.LastActivatedBase = null;
            }
        }
    }


    internal class BaseOutpost
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(GeoPhoenixBase), "UpdateStats")]
        internal static class GeoPhoenixBase_UpdateStats_patch
        {
            private static void Postfix(GeoPhoenixBase __instance)
            {
                if (__instance.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                {
                    __instance.Site.SiteProduction = new ResourcePack();
                }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixBase), "BaseHourlyUpdate")]
        internal static class GeoPhoenixBase_BaseHourlyUpdate_patch
        {
            private static bool Prefix(GeoPhoenixBase __instance)
            {
                /* TFTVLogger.Always($"[GeoPhoenixBase.BaseHourlyUpdate] Running for {__instance?.Site?.LocalizedSiteName}. " +
                     $"is Outpost? {__instance.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag)}");*/

                if (!__instance.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                {
                    return true;
                }

                __instance.BaseAssaultProtectionHours -= 1;

                float hpHeal = DefCache.GetDef<HealFacilityComponentDef>("E_Heal [MedicalBay_PhoenixFacilityDef]").BaseHeal / 2;
                float staminaHeal = DefCache.GetDef<HealFacilityComponentDef>("E_Heal [LivingQuarters_PhoenixFacilityDef]").BaseStaminaHeal / 2;
                float mutogHeal = DefCache.GetDef<HealFacilityComponentDef>("E_Heal [MedicalBay_PhoenixFacilityDef]").BaseHeal / 2;
                int aircraftRepair = DefCache.GetDef<VehicleSlotFacilityComponentDef>("E_Element0 [VehicleBay_PhoenixFacilityDef]").AircraftHealAmount / 2;
                int vehicleRepair = DefCache.GetDef<VehicleSlotFacilityComponentDef>("E_Element0 [VehicleBay_PhoenixFacilityDef]").VehicleHealAmount / 2;

                List<GeoCharacter> characters = __instance.Site.GetAllCharacters().ToList();

                foreach (GeoVehicle vehicle in __instance.VehiclesAtBase)
                {
                    characters.AddRange(vehicle.Units);
                }

                foreach (GeoCharacter soldier in characters)
                {
                    //  TFTVLogger.Always($"[GeoPhoenixBase.BaseHourlyUpdate] Checking {soldier?.DisplayName}. HP Heal: {hpHeal}, Stamina Heal: {staminaHeal}, Mutog Heal: {mutogHeal}, Vehicle Repair: {vehicleRepair}, Aircraft Repair: {aircraftRepair}");  

                    if (soldier.TemplateDef.IsHuman && hpHeal > 0 && soldier.Health.Value != soldier.Health.Max)
                    {
                        soldier.Heal(hpHeal);
                    }
                    else if (soldier.TemplateDef.IsMutog && mutogHeal > 0 && soldier.Health.Value != soldier.Health.Max)
                    {
                        soldier.Heal(mutogHeal);
                    }
                    else if (soldier.TemplateDef.CheckIsVehicle() && vehicleRepair > 0 && soldier.Health.Value != soldier.Health.Max)
                    {
                        soldier.Heal(vehicleRepair);
                    }

                    if (soldier.Fatigue != null && staminaHeal > 0 && soldier.Fatigue.Stamina.Value != soldier.Fatigue.Stamina.Max)
                    {
                        soldier.Fatigue.Stamina.AddRestrictedToMax(staminaHeal);
                    }
                }

                if (aircraftRepair > 0)
                {
                    foreach (GeoVehicle vehicle in __instance.Site.Vehicles)
                    {
                        vehicle.RepairModules(aircraftRepair);
                        if (vehicle.Stats.HitPoints != vehicle.Stats.MaxHitPoints)
                        {
                            vehicle.RepairAircraftHp(aircraftRepair);
                        }
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(UIModuleBaseLayout), "SetupChangeBaseButtons")]
        internal static class UIModuleBaseLayout_SetupChangeBaseButtons_patch
        {
            private static void Postfix(UIModuleBaseLayout __instance)
            {
                try
                {
                    Transform container = Traverse.Create(__instance).Field("BasesContainer").GetValue<Transform>();
                    if (container == null)
                    {
                        return;
                    }

                    foreach (Transform child in container)
                    {
                        MonoBehaviour selector = child.GetComponent("PhoenixBaseSelectionController") as MonoBehaviour;
                        if (selector == null)
                        {
                            continue;
                        }

                        GeoPhoenixBase pxBase = Traverse.Create(selector).Field("_phoenixBase").GetValue<GeoPhoenixBase>();
                        if (pxBase != null && pxBase.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(UIStatePhoenixBaseLayout), "EnterState")]
        internal static class UIStatePhoenixBaseLayout_EnterState_patch
        {
            private static void Postfix(UIStatePhoenixBaseLayout __instance)
            {
                try
                {
                    GeoPhoenixBase current = Traverse.Create(__instance).Field("_base").GetValue<GeoPhoenixBase>();
                    if (current == null || !current.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag))
                    {
                        return;
                    }

                    GeoPhoenixFaction faction = current.Site.Owner as GeoPhoenixFaction;
                    GeoPhoenixBase next = faction?.Bases.FirstOrDefault(b => !b.Site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag));
                    if (next == null)
                    {
                        return;
                    }

                    Traverse.Create(__instance).Field("_base").SetValue(next);
                    Traverse.Create(__instance).Method("SelectBase", next).GetValue();
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

    }
}
