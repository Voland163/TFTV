using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Characters.CharacterTemplates;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsSharedData;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.SiteEncounters;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TFTV.LaserWeapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    internal class GeoscapeVanillaFixes
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        /// <summary>
        /// Fixes not getting SP from Training Facilities
        /// </summary>

        internal static void ApplyDailyUpdate(GeoLevelController level)
        {
            try
            {
                foreach (GeoFaction geoFaction in level.Factions)
                {
                    if (geoFaction != null && geoFaction.Def != null && geoFaction.Def.UpdateFaction)
                    {
                        if (geoFaction is GeoPhoenixFaction geoPhoenixFaction)
                        {
                            geoPhoenixFaction.UpdateBasesDaily();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }




        [HarmonyPatch(typeof(UIInventoryDropArea), "InitForItem", new Type[]
    {
        typeof(ICommonItem)
    })]
        public static class UIInventoryDropArea_HarmonyPatch
        {
            private static void Postfix(UIInventoryDropArea __instance, ICommonItem item)
            {
                if (!__instance.IsScrap || item == null)
                {
                    return;
                }
                float nextScrapRefundMultiplier = ScrapRefundUtils.GetNextScrapRefundMultiplier(item);
                ResourcePack resourcePack = item.ItemDef.ScrapPrice * nextScrapRefundMultiplier;
                __instance.MaterialsCost.SetResource(resourcePack.ByResourceType(ResourceType.Materials), true, false, true);
                __instance.TechCost.SetResource(resourcePack.ByResourceType(ResourceType.Tech), true, false, true);
                __instance.MutagenCost.SetResource(resourcePack.ByResourceType(ResourceType.Mutagen), true, true, true);
                __instance.CrystalCost.SetResource(resourcePack.ByResourceType(ResourceType.LivingCrystals), true, true, true);
                __instance.OrichalcumCost.SetResource(resourcePack.ByResourceType(ResourceType.Orichalcum), true, true, true);
                __instance.PropaneCost.SetResource(resourcePack.ByResourceType(ResourceType.ProteanMutane), true, true, true);
            }
        }




        [HarmonyPatch(typeof(GeoFaction), "ScrapItem")]
        public static class GeoFaction_ScrapItem_HarmonyPatch
        {
            private static readonly AccessTools.FieldRef<GeoFaction, GeoLevelController> _levelRef = AccessTools.FieldRefAccess<GeoFaction, GeoLevelController>("_level");

            private static bool Prefix(GeoFaction __instance, GeoItem geoItem, int amount = 1)
            {
                for (int i = 0; i < amount; i++)
                {
                    float num = ScrapRefundUtils.GetNextScrapRefundMultiplier(geoItem);
                    ResourcePack resourcePack = geoItem.ItemDef.ScrapPrice * num;
                    __instance.Wallet.Give(resourcePack, OperationReason.Scrap);
                    PhoenixTelemetry instance = PhoenixTelemetry.Instance;
                    if (instance.Enabled)
                    {
                        instance.OnResourcesGiven(resourcePack, "ItemScrap");
                    }
                    geoItem.CommonItemData.Subtract(geoItem.GetSingleItem());
                }
                _levelRef(__instance).AchievmentTracker.ScrapItemsProgress(geoItem, amount);
                return false;
            }
        }

        internal static class ScrapRefundUtils
        {
            public static bool ShouldProrateScrapRefund(ICommonItem item)
            {
                return item.ItemDef.ChargesMax > 0 && item.CommonItemData.IsAmmo();
            }

            public static float GetNextScrapRefundMultiplier(ICommonItem item)
            {
                if (!ShouldProrateScrapRefund(item))
                {
                    return 1f;
                }
                if (item.ItemDef.CombineWhenStacking && item.CommonItemData.Count > 1)
                {
                    return 1f;
                }
                return Mathf.Clamp01((float)item.CommonItemData.CurrentCharges / (float)item.ItemDef.ChargesMax);
            }
        }

        [HarmonyPatch(typeof(GeoInventoryItemScrap), "UpdateScrapInfo")]
        public static class GeoInventoryItemScrap_HarmonyPatch
        {
            private static readonly AccessTools.FieldRef<GeoInventoryItemScrap, UIInventorySlot> _slotRef = AccessTools.FieldRefAccess<GeoInventoryItemScrap, UIInventorySlot>("_slot");
            private static readonly AccessTools.FieldRef<GeoInventoryItemScrap, int> _scrappedItemsAmountRef = AccessTools.FieldRefAccess<GeoInventoryItemScrap, int>("_scrappedItemsAmount");
            private static readonly AccessTools.FieldRef<GeoInventoryItemScrap, int> _itemsRemainingAmountRef = AccessTools.FieldRefAccess<GeoInventoryItemScrap, int>("_itemsRemainingAmount");

            private static bool Prefix(GeoInventoryItemScrap __instance)
            {
                int scrappedItemsAmount = _scrappedItemsAmountRef(__instance);
                int itemsRemainingAmount = _itemsRemainingAmountRef(__instance);
                ICommonItem item = _slotRef(__instance).Item;
                __instance.ItemAmount.text = scrappedItemsAmount.ToString();
                __instance.IncreaseAmountButton.enabled = (itemsRemainingAmount > scrappedItemsAmount);
                __instance.DecreaseAmountButton.enabled = (scrappedItemsAmount > 1);
                ResourcePack resourcePack = GetPreviewScrapPack(item, scrappedItemsAmount, itemsRemainingAmount);
                __instance.MaterialsCost.SetResource(resourcePack.ByResourceType(ResourceType.Materials), true, false, true);
                __instance.TechCost.SetResource(resourcePack.ByResourceType(ResourceType.Tech), true, false, true);
                __instance.MutagenCost.SetResource(resourcePack.ByResourceType(ResourceType.Mutagen), true, true, true);
                __instance.CrystalCost.SetResource(resourcePack.ByResourceType(ResourceType.LivingCrystals), true, true, true);
                __instance.OrichalcumCost.SetResource(resourcePack.ByResourceType(ResourceType.Orichalcum), true, true, true);
                __instance.PropaneCost.SetResource(resourcePack.ByResourceType(ResourceType.ProteanMutane), true, true, true);
                return false;
            }

            private static ResourcePack GetPreviewScrapPack(ICommonItem item, int amount, int itemsRemainingAmount)
            {
                float num = 0f;
                float num2 = ScrapRefundUtils.ShouldProrateScrapRefund(item) ? Mathf.Clamp01((float)item.CommonItemData.CurrentCharges / (float)item.ItemDef.ChargesMax) : 1f;
                for (int i = 0; i < amount; i++)
                {
                    if (ScrapRefundUtils.ShouldProrateScrapRefund(item) && item.ItemDef.CombineWhenStacking)
                    {
                        num += ((itemsRemainingAmount - i > 1) ? 1f : num2);
                    }
                    else
                    {
                        num += ScrapRefundUtils.GetNextScrapRefundMultiplier(item);
                    }
                }
                return item.ItemDef.ScrapPrice * num;
            }
        }




        [HarmonyPatch(typeof(GeoFaction), "OnCharacterAdded")]
        public static class GeoFaction_OnCharacterAdded_RefreshTagsPatch
        {
            static void Postfix(GeoCharacter character)
            {
                if (character == null || character.Identity == null) return;
                if (!character.TemplateDef.IsHuman) return;

                // critical line: sync Identity -> GameTags immediately
                character.RefreshTags();
            }
        }

        /// <summary>
        /// Fixes doubled HP in Geoscape alien template/intel displays for certain units (e.g. worms/eggs).
        ///
        /// Root cause in vanilla:
        /// - TacCharacterDef.GenerateDummyCharacterStats() uses GetBodypartAspects()
        /// - GetBodypartAspects() comes from GetTemplateBodyparts(true)
        /// - Some templates can surface duplicate bodypart aspects through merged part sources/subaddons
        /// - duplicate aspect stat modifiers stack twice -> doubled HP shown in report UI
        ///
        /// This postfix deduplicates aspects by def identity before they are consumed by stat generation.
        /// </summary>
        [HarmonyPatch(typeof(CharacterTemplateExtension), nameof(CharacterTemplateExtension.GetBodypartAspects))]
        internal static class TemplateHpDoubleCountFix
        {
            private static void Postfix(TacCharacterDef def, ref IEnumerable<BodyPartAspectDef> __result)
            {
                if (__result == null)
                {
                    return;
                }

                // Distinct() is sufficient here because duplicate entries are usually the same def instance.
                // Materialize to avoid re-enumerating deferred pipelines multiple times downstream.
                __result = __result.Where(a => a != null).Distinct().ToList();
            }
        }


        [HarmonyPatch(typeof(PXBaseActivationDataBind), "SetFacilities")]
        internal static class PXBaseActivationDataBind_SetFacilities_Patch
        {
            private static readonly Action<PXBaseActivationDataBind, PhoenixFacilityDef, bool> ToggleFacilityTooltipInvoker =
                AccessTools.MethodDelegate<Action<PXBaseActivationDataBind, PhoenixFacilityDef, bool>>(
                    AccessTools.Method(typeof(PXBaseActivationDataBind), "ToggleFacilityTooltip"));

            private static void Postfix(PXBaseActivationDataBind __instance, GeoPhoenixBase pxBase)
            {
                if (__instance == null || pxBase == null || pxBase.Layout == null)
                {
                    return;
                }

                GeoPhoenixFacility[] facilities = pxBase.Layout.BasicFacilities.ToArray();
                PhoenixGeneralButton[] buttons = UIUtil
                    .EnsureActiveComponentsInContainer<PhoenixGeneralButton>(__instance.FacilityContainer, __instance.FacilityContainerPrefab, facilities.Length)
                    .ToArray();

                for (int i = 0; i < facilities.Length && i < buttons.Length; i++)
                {
                    GeoPhoenixFacility facility = facilities[i];
                    PhoenixGeneralButton button = buttons[i];
                    PhoenixFacilityDef facilityDef = facility.Def;

                    button.PointerHoverUnfiltered = null;
                    button.PointerHoverUnfiltered = (PhoenixGeneralButton.HoverEventHandler)Delegate.Combine(button.PointerHoverUnfiltered, new PhoenixGeneralButton.HoverEventHandler(delegate (bool active)
                    {
                        ToggleFacilityTooltipInvoker(__instance, facilityDef, active);
                    }));

                    Image damagedMarker = button.GetComponentsInChildren<Image>(true).FirstOrDefault((Image image) => image.name == "DamagedFacility");
                    if (damagedMarker != null)
                    {
                        damagedMarker.gameObject.SetActive(facility.IsDamaged);
                    }
                }
            }
        }


        /// <summary>
        /// Fixes losing modules when ground vehicle scrapped
        /// </summary>
        [HarmonyPatch(typeof(GeoFaction), nameof(GeoFaction.KillCharacter))]
        public static class GeoFaction_KillCharacter_Patch
        {

            private static void Prefix(GeoFaction __instance, GeoCharacter unit, CharacterDeathReason reason)
            {
                try
                {
                    if (reason != CharacterDeathReason.Dismissed)
                    {
                        return;
                    }

                    if (!(__instance is GeoPhoenixFaction phoenixFaction))
                    {
                        return;
                    }

                    // TFTVLogger.Always($"!unit.GameTags.Contains(Shared.SharedGameTags.VehicleTag) {!unit.GameTags.Contains(Shared.SharedGameTags.VehicleTag)}");

                    if (!unit.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                    {
                        return;
                    }

                    TransferGroundVehicleModules(phoenixFaction, unit);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Always($"GroundVehicleScrapFix encountered an error while handling ground vehicle scrap: {ex}");
                }
            }

            private static void TransferGroundVehicleModules(GeoPhoenixFaction faction, GeoCharacter vehicle)
            {
                try
                {
                    if (faction?.ItemStorage == null || vehicle == null)
                    {
                        return;
                    }

                    List<GeoItem> itemsToTransfer = new List<GeoItem>();
                    AddUsableItems(vehicle.InventoryItems, itemsToTransfer, false);
                    AddUsableItems(vehicle.EquipmentItems, itemsToTransfer);
                    AddUsableItems(vehicle.ArmourItems, itemsToTransfer);

                    if (itemsToTransfer.Count == 0)
                    {
                        return;
                    }

                    foreach (GeoItem item in itemsToTransfer)
                    {
                        faction.ItemStorage.AddItem(item);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void AddUsableItems(IEnumerable<GeoItem> source, List<GeoItem> destination, bool emptyAmmoFirst = true)
            {
                if (source == null)
                {
                    return;
                }

                foreach (GeoItem geoItem in source)
                {
                    try
                    {
                        if (geoItem == null)
                        {
                            continue;
                        }

                        if (!emptyAmmoFirst)
                        {
                            destination.Add(geoItem);
                            TFTVLogger.Always($"item should be added {geoItem.ItemDef.name}");
                            continue;
                        }

                        TFTVLogger.Always($"geoItem.ItemDef {geoItem?.ItemDef?.name} {geoItem?.CommonItemData?.Ammo?.CurrentCharges}");

                        TryEmptyAmmoBeforeTransfer(geoItem);

                        destination.Add(geoItem);
                        TFTVLogger.Always($"item should be added {geoItem.ItemDef.name}");
                    }
                    catch (Exception e)
                    {
                        // Don't let one bad item (e.g. a vehicle module with an inconsistent
                        // ammo/magazine state) abort the whole transfer/scrap flow.
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void TryEmptyAmmoBeforeTransfer(GeoItem geoItem)
            {
                AmmoManager ammo = geoItem.CommonItemData.Ammo;

                if (ammo == null || ammo.CurrentCharges <= 0)
                {
                    return;
                }

                // Vehicle module items can report CurrentCharges > 0 without having any
                // backing magazine objects. In that case AmmoManager.ConsumeCharges indexes
                // into an empty LoadedMagazines collection and throws
                // ArgumentOutOfRangeException when the vehicle is scrapped, so only attempt
                // to consume charges when there is an actual magazine to consume from.
                if (ammo.LoadedMagazines == null || ammo.LoadedMagazines.Count == 0)
                {
                    TFTVLogger.Always($"{geoItem.ItemDef?.name} has CurrentCharges {ammo.CurrentCharges} but no loaded magazines; skipping ModifyCharges to avoid ConsumeCharges index error");
                    return;
                }

                // AmmoManager.ConsumeCharges also throws ArgumentOutOfRangeException for
                // some vehicle module items even when LoadedMagazines is non-empty (its
                // internal per-magazine charge accounting doesn't line up for these items).
                // Since all we need here is for the ammo to be emptied before the item is
                // stored, bypass ConsumeCharges entirely and clear the loaded magazines
                // directly instead of going through ModifyCharges.
                try
                {
                    ammo.LoadedMagazines.Clear();
                }
                catch (Exception e)
                {
                    TFTVLogger.Always($"Failed to directly clear LoadedMagazines for {geoItem.ItemDef?.name}: {e.Message}. Falling back to ModifyCharges.");

                    try
                    {
                        geoItem.CommonItemData.ModifyCharges(-ammo.CurrentCharges, false);
                    }
                    catch (Exception e2)
                    {
                        // Emptying ammo is best-effort here; the item is still transferred
                        // to storage with its ammo intact rather than crashing the whole
                        // vehicle-scrap/kill-character flow.
                        TFTVLogger.Always($"ModifyCharges fallback also failed for {geoItem.ItemDef?.name}: {e2.Message}");
                    }
                }
            }
        }



        /// <summary>
        /// Fixes softlock if game picks a turret deployed by a haven defender as a recruit
        /// </summary>
        [HarmonyPatch(typeof(HavenMissionUtil), nameof(HavenMissionUtil.GenerateHavenMissionRecruitmentReward))]
        public static class GenerateHavenMissionRecruitmentRewardPatch
        {
            static bool Prefix(GeoMission mission, ref GeoFactionReward __result)
            {
                try
                {
                    GeoFactionReward geoFactionReward = new GeoFactionReward();
                    GeoSite site = mission.Site;
                    GeoFaction uninfestedOwner = site.GetComponent<GeoHaven>().UninfestedOwner;
                    if (mission.GetMissionOutcomeState() == TacFactionState.Won)
                    {
                        int diplomacy = site.Owner.Diplomacy.GetDiplomacy(site.GeoLevel.ViewerFaction);
                        int diplomacy2 = site.GetComponent<GeoHaven>().Leader.Diplomacy.GetDiplomacy(site.GeoLevel.ViewerFaction);
                        if (HavenMissionUtil.FactionSoldierAlwaysJoin || (diplomacy > 0 && diplomacy2 > 0))
                        {
                            SharedGameTagsDataDef tags = site.GeoLevel.SharedData.SharedGameTags;
                            List<TacActorUnitResult> list = (from u in (from s in (from u in HavenMissionUtil.GetHavenUnitsFromMission(mission)
                                                                                   where u.MissionHistoryResult.HasItemType(UnitHistoryItemType.ControlledByPlayer)
                                                                                   select u).ToList()
                                                                        select s.Data).OfType<TacActorUnitResult>()
                                                             where u.IsAlive && !u.HasTag(tags.CivilianTag) && u.SourceTemplate != null
                                                             select u).ToList();

                            /*  foreach(TacActorUnitResult tacActorUnitResult in list) 
                              {
                                  TFTVLogger.Always($"{tacActorUnitResult?.TacticalActorBaseDef?.name} {tacActorUnitResult?.SourceTemplate?.name}");

                              }*/

                            if (site.GeoLevel.PhoenixFaction.LivingQuarterFreeSpace > 0 && list.Any())
                            {
                                TacActorUnitResult randomElement = list.GetRandomElement();
                                int num = UnityEngine.Random.Range(0, 100);
                                if (HavenMissionUtil.FactionSoldierAlwaysJoin || num < site.GeoLevel.CurrentDifficultyLevel.HavenRescueSoldierJoinChance)
                                {
                                    randomElement.Statuses.RemoveAll((StatusResult s) => s.Def is MindControlStatusDef);
                                    GeoCharacter geoCharacter = site.GeoLevel.CharacterGenerator.GenerateUnit(uninfestedOwner, randomElement).SpawnAsCharacter();
                                    geoCharacter.ApllyTacticalResult(randomElement);
                                    geoFactionReward.Units.Add(geoCharacter);
                                }
                            }
                        }
                    }
                    __result = geoFactionReward;

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        //Ensure facilities are working after repairing Power Generator
        [HarmonyPatch(typeof(GeoPhoenixFacility), "SetFacilityFunctioning")] //VERIFIED
        public static class GeoPhoenixFacility_SetFacilityFunctioning_AfterGenRepairedVanillaBugFix_Patch
        {
            public static void Postfix(GeoPhoenixFacility __instance)
            {
                try
                {

                    //  TFTVLogger.Always($"SetFacilityFunctioning {__instance.ViewElementDef.name}");

                    if (__instance.GetComponent<PowerFacilityComponent>() != null)
                    {
                        CheckFacilitesNotWorking(__instance.PxBase);
                        //  __instance.PxBase.RoutePower();
                    }

                    //

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Ensure facilities are working after repairing Power Generator
        [HarmonyPatch(typeof(GeoPhoenixBase), nameof(GeoPhoenixBase.RoutePower))]
        public static class GeoPhoenixFacility_RoutePower_ForceStatsUpdate_Patch
        {
            public static void Postfix(GeoPhoenixBase __instance)
            {
                try
                {
                    __instance.UpdateStats();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void CheckFacilitesNotWorking(GeoPhoenixBase phoenixBase)
        {
            try
            {
                phoenixBase.RoutePower();
                TFTVUI.Geoscape.Facilities.CheckUnpoweredBasesOnGeoscapeStart();

                /*   foreach (GeoPhoenixFacility baseFacility in phoenixBase.Layout.Facilities)
                   {

                       if (baseFacility.IsPowered && baseFacility.GetComponent<PrisonFacilityComponent>() == null)
                       {
                           baseFacility.SetPowered(false);
                           baseFacility.SetPowered(true);
                       }
                       // TFTVLogger.Always($"{baseFacility.ViewElementDef.name} at {phoenixBase.name} is working? {baseFacility.IsWorking}. is it powered? {baseFacility.IsPowered} ");
                   }*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// Fixes crash w weird interception screen 
        /// </summary>
        [HarmonyPatch(typeof(InterceptionBriefDataBind), nameof(InterceptionBriefDataBind.ModalShowHandler))]
        public static class InterceptionBriefDataBind_ModalShowHandler_patch
        {
            public static bool Prefix(InterceptionBriefDataBind __instance, UIModal modal)
            {
                try
                {

                    InterceptionInfoData data = (InterceptionInfoData)modal.Data;

                    if (data.CurrentPlayerAircraft == null && data.GetDefaultPlayerAircraft() == null || data.CurrentEnemyAircraft == null && data.GetDefaultEnemyAircraft() == null)
                    {
                        modal.Close();
                        return false;
                    }

                    return true;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Need to ensure that if ammo is less than full, it gets reloaded even if the difference is rounded down to 0
        /// </summary>
        [HarmonyPatch(typeof(GeoItem), nameof(GeoItem.ReloadForFree))]
        public static class TFTV_CharacterFatigue_ReloadForFree_patch
        {

            public static bool Prefix(GeoItem __instance)
            {
                try
                {
                    ItemDef _def = __instance.ItemDef;


                    if (_def.ChargesMax <= 0)
                    {
                        return false;
                    }

                    TacticalItemDef tacticalItemDef = _def as TacticalItemDef;

                    if (__instance.CommonItemData.Ammo == null || tacticalItemDef == null || !tacticalItemDef.CompatibleAmmunition.Any())
                    {
                        __instance.CommonItemData.SetChargesToMax();
                        return false;
                    }

                    if (__instance?.ItemDef is WeaponDef weaponDef && LaserWeaponsMain.LaserAmmoShareHelper.TryGetEntry(weaponDef, out _))
                    {
                        int targetMax = __instance.ItemDef.ChargesMax;
                        int current = Math.Max(0, __instance.CommonItemData.CurrentCharges);
                        int missing = Math.Max(0, targetMax - current);
                        if (missing <= 0)
                        {
                            return false;
                        }

                        if (__instance.CommonItemData.Ammo != null)
                        {
                            __instance.CommonItemData.Ammo.ReloadCharges(missing, true);
                        }
                        else
                        {
                            __instance.CommonItemData.SetChargesToMax();
                        }
                        return false;
                    }

                    TacticalItemDef tacticalItemDef2 = tacticalItemDef.CompatibleAmmunition[0];
                    int num = (_def.ChargesMax - __instance.CommonItemData.Ammo.CurrentCharges) / tacticalItemDef2.ChargesMax;

                    //Added to make sure that if ammo is less than full, it gets reloaded even if rounded to 0
                    if (_def.ChargesMax - __instance.CommonItemData.Ammo.CurrentCharges > 0 && num == 0)
                    {
                        num = 1;
                    }

                    for (int i = 0; i < num; i++)
                    {

                        __instance.CommonItemData.Ammo.LoadMagazine(new GeoItem(tacticalItemDef2));

                    }

                    return false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(GeoHaven), "get_RecruitCorruption")] //VERIFIED
        public static class TFTV_GeoHaven_get_RecruitCorruption_VanillaBugBix_patch
        {
            public static void Postfix(GeoHaven __instance, ref int __result)
            {
                try
                {
                    if (__result > 0 &&
                        (__instance.AvailableRecruit.GetGameTags().Contains(Shared.SharedGameTags.VehicleTag)
                        || __instance.AvailableRecruit.ClassTags.Contains(Shared.SharedGameTags.VehicleClassTag)))
                    {
                        __result = 0;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        //Reduce population by 1 when recruiting at havens

        [HarmonyPatch(typeof(GeoHaven), nameof(GeoHaven.TakeRecruit))]

        public static class TFTV_GeoHaven_TakeRecruit_VanillaBugBix_patch
        {
            public static void Postfix(GeoHaven __instance, IGeoCharacterContainer __result, ref int ____population)
            {
                try
                {
                    if (__result != null)
                    {
                        ____population -= 1;
                        HavenInfoController havenInfo = (HavenInfoController)UnityEngine.Object.FindObjectOfType(typeof(HavenInfoController));


                        int populationChange = __instance.GetPopulationChange(__instance.ZonesStats.GetTotalHavenOutput());
                        if (populationChange > 0)
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationPositiveTextPattern, __instance.Population.ToString(), populationChange);
                        }
                        else if (populationChange == 0)
                        {
                            havenInfo.PopulationValueText.text = __instance.Population.ToString();
                        }
                        else
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationNegativeTextPattern, __instance.Population.ToString(), populationChange);
                        }


                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


       


        /// <summary>
        /// Fix to prevent characters given in events from spawning with wrong faction origin
        /// </summary>

        private static List<string> _eventsRewardingNJCharacters = new List<string>() { "AN11", "EX7", "SY22" };

        internal static void ApplyGenerateFactionReward(GeoEventChoiceOutcome __instance, string eventID, ref GeoFactionReward __result)
        {
            try
            {
                GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (eventID == "PROG_PU4_WIN" && __result.Units.Count > 0)
                {
                    __result.Units.Clear();
                    GeoFaction faction2 = level.AnuFaction;
                    GeoUnitDescriptor geoUnitDescriptor = level.CharacterGenerator.GenerateUnit(faction2, __instance.Units[0]);
                    level.CharacterGenerator.ApplyRecruitDifficultyParameters(geoUnitDescriptor);
                    GeoCharacter item2 = geoUnitDescriptor.SpawnAsCharacter();
                    __result.Units.Add(item2);

                }
                else if (_eventsRewardingNJCharacters.Contains(eventID) && __result.Units.Count > 0)
                {
                    __result.Units.Clear();
                    GeoFaction faction2 = level.NewJerichoFaction;
                    GeoUnitDescriptor geoUnitDescriptor = level.CharacterGenerator.GenerateUnit(faction2, __instance.Units[0]);
                    level.CharacterGenerator.ApplyRecruitDifficultyParameters(geoUnitDescriptor);
                    GeoCharacter item2 = geoUnitDescriptor.SpawnAsCharacter();
                    __result.Units.Add(item2);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        /// <summary>
        /// Fix to prevent last item being removed in Marketplace when number of offers > 7 
        /// No try/catch because harmless error on buying item
        /// </summary>

        [HarmonyPatch(typeof(UIModuleTheMarketplace), "UpdateList")] //VERIFIED
        public static class UIModuleTheMarketplace_UpdateList_patch
        {
            public static bool Prefix(UIModuleTheMarketplace __instance, GeoscapeEvent geoEvent, bool ____isInit,
                List<TheMarketplaceChoiceButton> ____marketplaceChoiceButtons, GeoMarketplace ____geoMarketplace)
            {
                //  try
                //  {
                MethodInfo setChoiceMethod = typeof(TheMarketplaceChoicesController).GetMethod("SetChoice", BindingFlags.NonPublic | BindingFlags.Instance);

                if (____isInit)
                {
                    __instance.ListScrollRect.Scroll.verticalNormalizedPosition = 1f;
                }

                ____marketplaceChoiceButtons.Clear();

                //    TFTVLogger.Always($"____geoMarketplace.MarketplaceChoices.Count {____geoMarketplace.MarketplaceChoices.Count}");

                int count = ____geoMarketplace.MarketplaceChoices.Count;

                if (____geoMarketplace.MarketplaceChoices.Count > 7) //&& !TFTVChangesToDLC5.TFTVMarketPlaceUI.MarketplaceOfferListAdjustedOnce)
                {
                    count = ____geoMarketplace.MarketplaceChoices.Count + 1;
                }



                __instance.ListScrollRect.InitVertical(__instance.MarketplaceChoiceButtonPrefab.GetComponent<TheMarketplaceChoiceButton>(), count, delegate (int index, UnityEngine.Component element)
                {
                    TheMarketplaceChoiceButton component = element.GetComponent<TheMarketplaceChoiceButton>();
                    setChoiceMethod.Invoke(__instance.TheMarketplaceChoicesController, new object[] { __instance.Context.ViewerFaction, ____geoMarketplace.MarketplaceChoices[index], component, geoEvent.Context });
                    ____marketplaceChoiceButtons.Add(component);
                });

                // TFTVLogger.Always($"____marketplaceChoiceButtons.Count {____marketplaceChoiceButtons.Count}");

                return false;
                //  }
                /*  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }*/
            }
        }



        //Prevents ammo from disappearing on pressing replinish ammo if the class of the soldier is not proficient with the weapon and ALL filter is switched off 
        [HarmonyPatch(typeof(UIStateEditSoldier), "SoldierSlotItemChangedHandler")] //VERIFIED
        public static class UIStateEditSoldier_SoldierSlotItemChangedHandler_patch
        {

            public static bool Prefix(UIStateEditSoldier __instance, UIInventorySlot slot)
            {
                try
                {
                    if (slot == null)
                    {
                        return false;

                    }

                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //fixes events reducing health to 0 and killing soldiers
        [HarmonyPatch(typeof(GeoFactionReward), "AddInjuriesToAllSoldiers")] //VERIFIED
        public static class TFTV_GeoFactionReward_AddInjuriesToAllSoldiers
        {
            public static bool Prefix(GeoFactionReward __instance, GeoFaction faction)
            {
                try
                {
                    if (__instance.AddAllSoldiersTiredness != 0)
                    {
                        foreach (GeoCharacter character in faction.Characters)
                        {
                            if (character.Fatigue != null)
                            {
                                character.Fatigue.Stamina.AddRestrictedToMax(-__instance.AddAllSoldiersTiredness);
                            }
                        }

                        __instance.ApplyResult.AllSoldiersTiredness += __instance.AddAllSoldiersTiredness;
                    }

                    if (__instance.AddAllSoldiersDamage == 0)
                    {
                        return false;
                    }

                    foreach (GeoCharacter character2 in faction.Characters)
                    {
                        if ((float)character2.Health > 1f && character2.TemplateDef.IsHuman)
                        {
                            //TFTVLogger.Always($"{character2?.DisplayName} has {character2.Health.Value}");
                            int addAllSoldiersDamage = Math.Min(__instance.AddAllSoldiersDamage, (int)character2.Health - 1);
                            character2.Health.AddRestrictedToMax(-addAllSoldiersDamage);
                            TFTVLogger.Always($"applied {addAllSoldiersDamage} damage to {character2.DisplayName}, so now has {character2.Health.Value}");
                        }
                    }

                    __instance.ApplyResult.AllSoldiersDamage += __instance.AddAllSoldiersDamage;

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        //Prevents multiple instancing of mission briefings when several aircraft arrive simultaneously at the mission site
        private static TimeUnit _arrivalTime;

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteVisited")] //VERIFIED
        public static class UIStateVehicleSelected_OnVehicleSiteVisitedt_patch
        {
            public static bool Prefix(UIStateVehicleSelected __instance, GeoVehicle vehicle)
            {
                try
                {
                    TimeUnit currentTime = vehicle.GeoLevel.Timing.Now;

                    if (_arrivalTime != null && _arrivalTime == currentTime && vehicle?.CurrentSite.Vehicles.Count() > 1)
                    {
                        TFTVLogger.Always($"more than 1 vehicle arriving at {vehicle?.CurrentSite?.LocalizedSiteName} simultaneously; cancelling stuff for all vehicles except the first");
                        return false;
                    }

                    _arrivalTime = currentTime;

                    return true;
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
