using Base.Core;
using Base.Defs;
using Base.Serialization.General;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVCapturePandorans
    {
        //   PhoenixPoint.Geoscape.Levels.Factions.GeoPhoenixFaction.<> c.< get_ContaimentUsage > b__42_0(GeoUnitDescriptor) : int @0600C599

        // ResourceGeneratorFacilityComponent

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
        public class ReadyForCapturesStatus : TacStatus
        {


        }

        [CreateAssetMenu(fileName = "ReadyForCapturesStatusDef", menuName = "Defs/Statuses/")]
        [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
        public class ReadyForCapturesStatusDef : TacStatusDef
        {

        }

        public static int AircraftCaptureCapacity = -1;
        public static bool ContainmentFacilityPresent = false;
        public static bool ScyllaCaptureModulePresent = false;
        public static int ContainmentSpaceAvailable = 0;
        //   public static int CachedACC = 0;


        public static void ModifyCapturePandoransTacticalObjectives(TacMissionTypeDef missionType)
        {
            try
            {
                TFTVLogger.Always("ModifyCapturePandoransObjectives");

                List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                KeepSoldiersAliveFactionObjectiveDef containmentPresent = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_BASE");
                KeepSoldiersAliveFactionObjectiveDef aircraftCapture = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_AIRCRAFT");

                if (ContainmentFacilityPresent)
                {
                    if (!listOfFactionObjectives.Contains(containmentPresent))
                    {
                        listOfFactionObjectives.Add(containmentPresent);
                    }
                }
                else
                {
                    if (listOfFactionObjectives.Contains(containmentPresent))
                    {
                        listOfFactionObjectives.Remove(containmentPresent);
                    }
                }
                if (AircraftCaptureCapacity < 0)
                {
                    TFTVLogger.Always($"AircraftCaptureCapacity is {AircraftCaptureCapacity}");

                    if (listOfFactionObjectives.Contains(aircraftCapture))
                    {
                        listOfFactionObjectives.Remove(aircraftCapture);
                    }
                }
                else
                {
                    if (!listOfFactionObjectives.Contains(aircraftCapture))
                    {
                        listOfFactionObjectives.Add(aircraftCapture);
                        TFTVLogger.Always("AircraftCapture capacity objective added");
                    }

                }

                missionType.CustomObjectives = listOfFactionObjectives.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        [HarmonyPatch(typeof(ObjectivesManager), "Add")]
        public static class FactionObjective_ModifyObjectiveColor_CapturePandorans_Patch
        {

            public static void Postfix(ObjectivesManager __instance, FactionObjective objective)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {

                        KeepSoldiersAliveFactionObjectiveDef aircraftCapture = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_AIRCRAFT");

                      //  TFTVLogger.Always($"FactionObjective Invoked, {objective.Summary.LocalizationKey}");
                        if (objective.Summary.LocalizationKey.Contains("CAPACITY_AIRCRAFT"))
                        {
                            LocalizedTextBind aircraftCapacityText = new LocalizedTextBind("CAPTURE_CAPACITY_AIRCRAFT");
                            LocalizedTextBind totalCapacityText = new LocalizedTextBind("CAPTURE_CAPACITY_TOTAL");
                            string aircraftContainment = $"{aircraftCapacityText.Localize()} {AircraftCaptureCapacity}. {totalCapacityText.Localize()} {ContainmentSpaceAvailable}";

                            // TFTVLogger.Always("FactionObjective Invoked check passed");
                            objective.Description = new LocalizedTextBind(aircraftContainment, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(KeepSoldiersAliveFactionObjective), "EvaluateObjective")]
        public static class KeepSoldiersAliveFactionObjective_EvaluateObjective_CapturePandorans_Patch
        {

            public static void Postfix(KeepSoldiersAliveFactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;


                    KeepSoldiersAliveFactionObjectiveDef containmentPresent = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_BASE");
                    KeepSoldiersAliveFactionObjectiveDef aircraftCapture = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_AIRCRAFT");

                    //   TFTVLogger.Always($"Evaluating objective {__instance.Summary.LocalizationKey}");


                    if (__instance.Summary.LocalizationKey.Contains("CAPACITY_AIRCRAFT"))//Localize() == aircraftCapture.MissionObjectiveData.Description.Localize())
                    {

                        LocalizedTextBind aircraftCapacityText = new LocalizedTextBind("CAPTURE_CAPACITY_AIRCRAFT");
                        LocalizedTextBind totalCapacityText = new LocalizedTextBind("CAPTURE_CAPACITY_TOTAL");
                        string aircraftContainment = $"{aircraftCapacityText.Localize()} {AircraftCaptureCapacity}. {totalCapacityText.Localize()} {ContainmentSpaceAvailable}";
                        __instance.Description = new LocalizedTextBind(aircraftContainment, true);
                        // TFTVLogger.Always($"FactionObjective check passed; description: {__instance.Description},{aircraftCapture.MissionObjectiveData.Description.Localize()} {AircraftCaptureCapacity} ");
                        __result = FactionObjectiveState.InProgress;
                    }

                    if (__instance.Summary.LocalizationKey.Contains("CAPACITY_BASE"))//__instance.Description.Localize() == containmentPresent.MissionObjectiveData.Description.Localize()) 
                    {
                        //    TFTVLogger.Always("FactionObjective check passed");
                        __result = FactionObjectiveState.InProgress;

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void CheckCaptureCapability(GeoMission geoMission)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (TFTVNewGameOptions.LimitedCaptureSetting && !geoMission.MissionDef.FinalMission)
                {

                    if (geoMission.Site.GeoLevel.PhoenixFaction.Research.HasCompleted("PX_CaptureTech_ResearchDef"))
                    {

                        PhoenixFacilityDef containmentFacility = DefCache.GetDef<PhoenixFacilityDef>("AlienContainment_PhoenixFacilityDef");

                        // TFTVLogger.Always($"are we here?");

                        if (geoMission.MissionDef.ParticipantsData.Any(tcpd => tcpd.FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef")))
                        {

                            if (geoMission.MissionDef.Tags.Contains(Shared.SharedGameTags.BaseDefenseMissionTag) || geoMission.MissionDef.Tags.Contains(Shared.SharedGameTags.InfestHavenMissionTag))
                            {
                                if (geoMission.Site.GetComponent<GeoPhoenixBase>() is GeoPhoenixBase pxBase &&
                                    pxBase.Layout.Facilities.
                                    Where(f => f.Def.Equals(containmentFacility)).
                                    Any(f => f.State == GeoPhoenixFacility.FacilityState.Functioning && f.State != GeoPhoenixFacility.FacilityState.Damaged))
                                {

                                    TFTVLogger.Always($"This is a Phoenix base mission, and there is a functioning Containment Facility, so capture capacity is not limited");
                                    ContainmentFacilityPresent = true;
                                    AircraftCaptureCapacity = -1;
                                    return;
                                }

                            }

                            if (!ContainmentFacilityPresent)
                            {
                                GeoVehicleDef manticore6slots = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def_6_Slots");
                                GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
                                GeoVehicleDef helios5slots = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def_5_Slots");
                                GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
                                GeoVehicleDef thunderbird7slots = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def_7_Slots");
                                GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
                                GeoVehicleDef blimp12slots = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def_12_Slots");
                                GeoVehicleDef blimp8slots = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");
                                GeoVehicleDef maskedManticore8slots = DefCache.GetDef<GeoVehicleDef>("PP_ManticoreMasked_Def_8_Slots");
                                GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

                                List<GeoVehicle> geoVehicles = geoMission.Site.GetPlayerVehiclesOnSite().ToList();

                                if (geoVehicles.Any(gv => gv.VehicleDef.Equals(blimp12slots) || gv.VehicleDef.Equals(blimp8slots) || gv.VehicleDef.Equals(maskedManticore) || gv.VehicleDef.Equals(maskedManticore8slots)))
                                {
                                    AircraftCaptureCapacity = 8;
                                }
                                else if (geoVehicles.Any(gv => gv.VehicleDef.Equals(thunderbird) || gv.VehicleDef.Equals(thunderbird7slots)))
                                {
                                    AircraftCaptureCapacity = 7;

                                }
                                else if (geoVehicles.Any(gv => gv.VehicleDef.Equals(manticore6slots) || gv.VehicleDef.Equals(manticore)))
                                {
                                    AircraftCaptureCapacity = 6;
                                }
                                else if (geoVehicles.Any(gv => gv.VehicleDef.Equals(helios) || gv.VehicleDef.Equals(helios5slots)))
                                {
                                    AircraftCaptureCapacity = 5;
                                }

                                if (geoVehicles.Any(gv => gv.Modules.Any(m => m != null && m.ModuleDef != null && m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.SurvivalOdds)))
                                {
                                    ScyllaCaptureModulePresent = true;
                                    AircraftCaptureCapacity += 8;
                                    TFTVLogger.Always($"Scylla Capture Module present!");
                                }

                                ContainmentSpaceAvailable = geoMission.Site.GeoLevel.PhoenixFaction.GetTotalContaimentCapacity() - geoMission.Site.GeoLevel.PhoenixFaction.ContaimentUsage;

                                if (ContainmentSpaceAvailable == 0)
                                {
                                    AircraftCaptureCapacity = -1;

                                }

                                TFTVLogger.Always($"There is an aircraft with {AircraftCaptureCapacity} slots available for capture and there is {ContainmentSpaceAvailable} containment capacity");
                                return;
                            }
                        }
                    }
                }
                AircraftCaptureCapacity = -1;
                TFTVLogger.Always($"No capture capacity; aircraft capture capacity {AircraftCaptureCapacity}");
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        // }

        internal static int CalculateCaptureSlotCost(List<GameTagDef> gameTagDefs)
        {
            try
            {
                ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
                ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                ClassTagDef wormTag = DefCache.GetDef<ClassTagDef>("Worm_ClassTagDef");
                ClassTagDef facehuggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");
                ClassTagDef swarmerTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");

                List<ClassTagDef> smallAndGruntsTags = new List<ClassTagDef>() { wormTag, facehuggerTag, swarmerTag };

                int captureSlots = 0;

                if (gameTagDefs.Contains(queenTag))
                {
                    if (ScyllaCaptureModulePresent)
                    {
                        captureSlots = 8;
                    }
                    else
                    {
                        captureSlots = 16;

                    }
                    // TFTVLogger.Always($"{tacticalActor.name} has {largeDeploymentTag.name}, captureSlots set to {captureSlots}");
                }
                else if (gameTagDefs.Contains(chironTag) || gameTagDefs.Contains(acheronTag))
                {
                    captureSlots = 4;
                    // TFTVLogger.Always($"{tacticalActor.name} has {mediumDeploymentTag.name}, captureSlots set to {captureSlots}");
                }
                else if (gameTagDefs.Contains(sirenTag))
                {
                    captureSlots = 3;
                    // TFTVLogger.Always($"{tacticalActor.name} has {eliteDeploymentTag.name}, captureSlots set to {captureSlots}");
                }
                else if (gameTagDefs.Contains(crabTag) || gameTagDefs.Contains(fishTag))
                {
                    captureSlots = 2;

                }
                else if (gameTagDefs.Any(gt => smallAndGruntsTags.Contains(gt)))
                {
                    captureSlots = 1;
                    // TFTVLogger.Always($"{tacticalActor.name} has {gruntDeploymentTag.name} or {tinyDeploymentTag.name}, captureSlots set to {captureSlots}");
                }

                //  TFTVLogger.Always($"Checking: {tacticalActor.name} takes up {captureSlots} capture slots");

                return captureSlots;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        [HarmonyPatch(typeof(UIModuleTacticalContextualMenu), "OnAbilitySelected")]
        public static class UIModuleTacticalContextualMenu_OnAbilitySelected_CapturePandorans_patch
        {

            public static bool Prefix(UIModuleTacticalContextualMenu __instance, TacticalAbility ability, out int __state)
            {
                try
                {
                    __state = 0;
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {


                        if (ability != null && (ContainmentFacilityPresent || AircraftCaptureCapacity >= 0))
                        {

                            ApplyStatusAbilityDef capturePandoranAbility = DefCache.GetDef<ApplyStatusAbilityDef>("CapturePandoran_Ability");
                            ReadyForCapturesStatusDef readyForCaptureStatus = DefCache.GetDef<ReadyForCapturesStatusDef>("CapturePandoran_Status");
                            ApplyStatusAbilityDef cancelCaptureAbility = DefCache.GetDef<ApplyStatusAbilityDef>("RemoveCapturePandoran_Ability");
                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            KeepSoldiersAliveFactionObjectiveDef aircraftCapture = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("CAPTURE_CAPACITY_AIRCRAFT");

                            if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == capturePandoranAbility)
                            {

                                TacticalActor tacticalActor = __instance.SelectionInfo.Actor as TacticalActor;


                                if (!ContainmentFacilityPresent && tacticalActor != null)
                                {
                                    TacticalFaction pxFaction = controller.GetFactionByCommandName("px");
                                    FactionObjective captureSlotsRemainingReminder = pxFaction.Objectives.FirstOrDefault(o => o.Summary.LocalizationKey.Contains("CAPACITY_AIRCRAFT"));

                                    tacticalActor.Status.ApplyStatus(readyForCaptureStatus);

                                    TFTVLogger.Always($"Capacity {AircraftCaptureCapacity} slot cost {CalculateCaptureSlotCost(tacticalActor.GameTags.ToList())}");

                                    AircraftCaptureCapacity -= CalculateCaptureSlotCost(tacticalActor.GameTags.ToList());
                                    TFTVLogger.Always($"Capacity now {AircraftCaptureCapacity}");


                                    if (captureSlotsRemainingReminder != null)
                                    {

                                        captureSlotsRemainingReminder.Description = new LocalizedTextBind($"{aircraftCapture.MissionObjectiveData.Description.Localize()} {AircraftCaptureCapacity}", true);
                                        TFTVLogger.Always($"{captureSlotsRemainingReminder.Description.LocalizeEnglish()}");


                                        UIModuleObjectives uIModuleObjectives = controller.View.TacticalModules.ObjectivesModule;
                                        TacticalViewContext context = Traverse.Create(uIModuleObjectives).Field("_context").GetValue<TacticalViewContext>();


                                        uIModuleObjectives.Init(context);

                                    }



                                }
                                __state = 1;
                                return false;
                            }
                            else if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == cancelCaptureAbility && __instance.SelectionInfo.Actor != null && __instance.SelectionInfo.Actor.Status.HasStatus(readyForCaptureStatus))
                            {
                                TacticalActor tacticalActor = __instance.SelectionInfo.Actor as TacticalActor;

                                if (tacticalActor != null)
                                {
                                    TacticalFaction pxFaction = controller.GetFactionByCommandName("px");
                                    FactionObjective captureSlotsRemainingReminder = pxFaction.Objectives.FirstOrDefault(o => o.Summary.LocalizationKey.Contains("CAPACITY_AIRCRAFT"));

                                    TFTVLogger.Always($"Capacity {AircraftCaptureCapacity} slot cost {CalculateCaptureSlotCost(tacticalActor.GameTags.ToList())}");
                                    tacticalActor.Status.UnapplyStatus(tacticalActor.Status.GetStatusByName(readyForCaptureStatus.EffectName));

                                    if (!ContainmentFacilityPresent)
                                    {

                                        AircraftCaptureCapacity += CalculateCaptureSlotCost(tacticalActor.GameTags.ToList());

                                        TFTVLogger.Always($"Capacity now {AircraftCaptureCapacity}");

                                        if (captureSlotsRemainingReminder != null)
                                        {

                                            captureSlotsRemainingReminder.Description = new LocalizedTextBind($"{aircraftCapture.MissionObjectiveData.Description.Localize()} {AircraftCaptureCapacity}", true);
                                            TFTVLogger.Always($"{captureSlotsRemainingReminder.Description.LocalizeEnglish()}");
                                            UIModuleObjectives uIModuleObjectives = controller.View.TacticalModules.ObjectivesModule;
                                            TacticalViewContext context = Traverse.Create(uIModuleObjectives).Field("_context").GetValue<TacticalViewContext>();


                                            uIModuleObjectives.Init(context);
                                        }

                                        // controller.GetFactionByCommandName("px").Objectives.FirstOrDefault(o => o.Summary.LocalizationKey.Contains("CAPACITY_AIRCRAFT")).Evaluate();
                                    }

                                    __state = 2;
                                    return false;
                                }
                            }

                        }
                        return true;
                    }
                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void Postfix(UIModuleTacticalContextualMenu __instance, TacticalAbility ability, in int __state, ref List<TacticalContextualMenuItem> ____menuItems)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {

                        ApplyStatusAbilityDef capturePandoranAbility = DefCache.GetDef<ApplyStatusAbilityDef>("CapturePandoran_Ability");
                        ReadyForCapturesStatusDef readyForCaptureStatus = DefCache.GetDef<ReadyForCapturesStatusDef>("CapturePandoran_Status");
                        ApplyStatusAbilityDef cancelCaptureAbility = DefCache.GetDef<ApplyStatusAbilityDef>("RemoveCapturePandoran_Ability");

                        if (__state == 1)
                        {
                            // TFTVLogger.Always("OnAbilitySelected ran postfix");
                            if (ability.TacticalAbilityDef == capturePandoranAbility)
                            {

                                TacticalContextualMenuItem tacticalContextualMenuItem = ____menuItems.Where(tcm => tcm.Ability == ability).FirstOrDefault();
                                tacticalContextualMenuItem.gameObject.SetActive(value: false);

                            }
                        }
                        else if (__state == 2)
                        {
                            if (ability.TacticalAbilityDef == cancelCaptureAbility)
                            {

                                TacticalContextualMenuItem tacticalContextualMenuItem = ____menuItems.Where(tcm => tcm.Ability == ability).FirstOrDefault();


                                tacticalContextualMenuItem.gameObject.SetActive(value: false);

                            }


                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }


        [HarmonyPatch(typeof(UIModuleTacticalContextualMenu), "SetMenuItems")]
        public static class UIModuleTacticalContextualMenu_SetMenuItems_CapturePandorans_patch
        {

            public static void Prefix(SelectionInfo selectionInfo, ref List<TacticalAbility> rawAbilities, out int __state)
            {
                try
                {
                    __state = 0;

                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {

                        if (ContainmentFacilityPresent || AircraftCaptureCapacity >= 0)
                        {

                            ParalysedStatusDef paralysedStatusDef = DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef");
                            ApplyStatusAbilityDef capturePandoranAbility = DefCache.GetDef<ApplyStatusAbilityDef>("CapturePandoran_Ability");
                            ReadyForCapturesStatusDef readyForCaptureStatus = DefCache.GetDef<ReadyForCapturesStatusDef>("CapturePandoran_Status");

                            ApplyStatusAbilityDef cancelCaptureAbility = DefCache.GetDef<ApplyStatusAbilityDef>("RemoveCapturePandoran_Ability");

                            if (selectionInfo.Actor is TacticalActor Pandoran && Pandoran.HasGameTag(Shared.SharedGameTags.CapturableTag)
                                && Pandoran.HasGameTag(Shared.SharedGameTags.AlienTag)
                                && Pandoran.HasStatus(paralysedStatusDef))
                            {
                                TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                                TacticalActor selectedActor = tacticalLevelController.View.SelectedActor;

                                if (!Pandoran.HasStatus(readyForCaptureStatus))
                                {
                                    if (AircraftCaptureCapacity >= CalculateCaptureSlotCost(Pandoran.GameTags.ToList()))
                                    {

                                        TFTVLogger.Always($"the Pandoran is {Pandoran.name}, aircraft capture Capacity is {AircraftCaptureCapacity}, required space is {CalculateCaptureSlotCost(Pandoran.GameTags.ToList())}");
                                        selectedActor.AddAbility(capturePandoranAbility, selectedActor);
                                        ApplyStatusAbility markForCapture = selectedActor.GetAbilityWithDef<ApplyStatusAbility>(capturePandoranAbility);
                                        rawAbilities.Add(markForCapture);
                                        __state = 1;
                                    }
                                }
                                else
                                {
                                    selectedActor.AddAbility(cancelCaptureAbility, selectedActor);
                                    ApplyStatusAbility cancelCapture = selectedActor.GetAbilityWithDef<ApplyStatusAbility>(cancelCaptureAbility);
                                    rawAbilities.Add(cancelCapture);
                                    __state = 2;


                                }
                            }
                            // TFTVLogger.Always($"ability should be added");
                        }

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static void Postfix(in int __state)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {

                        if (__state == 1)
                        {
                            ApplyStatusAbilityDef capturePandoranAbility = DefCache.GetDef<ApplyStatusAbilityDef>("CapturePandoran_Ability");
                            TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            TacticalActor selectedActor = tacticalLevelController.View.SelectedActor;
                            selectedActor.RemoveAbility(capturePandoranAbility);
                            //  TFTVLogger.Always($"ability should be removed");
                        }
                        else if (__state == 2)
                        {
                            ApplyStatusAbilityDef cancelCaptureAbility = DefCache.GetDef<ApplyStatusAbilityDef>("RemoveCapturePandoran_Ability");
                            TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            TacticalActor selectedActor = tacticalLevelController.View.SelectedActor;
                            selectedActor.RemoveAbility(cancelCaptureAbility);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }

        internal static List<TacActorUnitResult> GetCaptureList(IEnumerable<TacActorUnitResult> tacActorUnitResults)
        {
            try
            {  
                
                if (!ContainmentFacilityPresent)
                {
                    int availableCaptureslotsCounter = AircraftCaptureCapacity;//CachedACC;

                    List<TacActorUnitResult> paralyzedList = tacActorUnitResults.ToList();
                    List<TacActorUnitResult> captureList = new List<TacActorUnitResult>(tacActorUnitResults.Where(taur => taur.HasStatus<ReadyForCapturesStatusDef>()));
                    
                    foreach(TacActorUnitResult tacActorUnitResult in captureList) 
                    {
                        availableCaptureslotsCounter -= CalculateCaptureSlotCost(tacActorUnitResult.GameTags);  
                    }

                    paralyzedList = paralyzedList.OrderByDescending(taur => CalculateCaptureSlotCost(taur.GameTags)).ToList();

                    foreach (TacActorUnitResult tacActorUnitResult1 in paralyzedList.Where(taur => !taur.HasStatus<ReadyForCapturesStatusDef>()))
                    {
                        TFTVLogger.Always($"paralyzed {tacActorUnitResult1.TacticalActorBaseDef.name}, aircraftCaptureCapacity is {availableCaptureslotsCounter}, space required is {CalculateCaptureSlotCost(tacActorUnitResult1.GameTags)}");

                        if (availableCaptureslotsCounter >= CalculateCaptureSlotCost(tacActorUnitResult1.GameTags))
                        {
                            //  TFTVLogger.Always($"{tacActorUnitResult1.TacticalActorBaseDef.name} added to capture list; available slots before that {availableCaptureslotsCounter}");
                            captureList.Add(tacActorUnitResult1);
                            availableCaptureslotsCounter -= CalculateCaptureSlotCost(tacActorUnitResult1.GameTags);
                            TFTVLogger.Always($"{tacActorUnitResult1.TacticalActorBaseDef.name} added to capture list; available slots after {availableCaptureslotsCounter}");

                        }
                    }


                    return captureList;
                }
                else
                {

                    return tacActorUnitResults.ToList();
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        [HarmonyPatch(typeof(GeoMission), "CaptureLiveAlien")]
        public static class GeoMission_CaptureLiveAlien_patch
        {

            public static bool Prefix(GeoMission __instance, TacMissionResult result)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    CheckCaptureCapability(__instance);

                    if (TFTVNewGameOptions.LimitedCaptureSetting)
                    {
                        TFTVLogger.Always($"CaptureLiveAlienRunning");

                        GeoLevelController geoLevel = __instance.Site.GeoLevel;
                        _ = geoLevel.PhoenixFaction;

                        //  ParalysedStatusDef captureStatus = DefCache.GetDef<ParalysedStatusDef>("CapturePandoran_Status");

                        if (result.GetResultByFacionDef(geoLevel.PhoenixFaction.FactionDef.PPFactionDef).State != TacFactionState.Won)
                        {
                            return false;
                        }

                        FactionResult resultByFacionDef = result.GetResultByFacionDef(geoLevel.AlienFaction.FactionDef.PPFactionDef);
                        if (resultByFacionDef == null)
                        {
                            return false;
                        }

                        __instance.GameController.GetComponent<DefRepository>().GetAllDefs<ComponentSetDef>();
                        GameTagDef captureTag = __instance.GameController.GetComponent<SharedData>().SharedGameTags.CapturableTag;
                        IEnumerable<TacActorUnitResult> enumerable = from a in resultByFacionDef.UnitResults.Select((UnitResult t) => t.Data).OfType<TacActorUnitResult>()
                                                                     where a.IsAlive && a.HasStatus<ParalysedStatusDef>()
                                                                     where a.GameTags.Contains(captureTag)
                                                                     select a;
                        List<GeoUnitDescriptor> list = new List<GeoUnitDescriptor>();

                        TFTVLogger.Always($"enumerable count is {enumerable.Count()}");

                        IEnumerable<TacActorUnitResult> finalCaptureList = GetCaptureList(enumerable);

                        foreach (TacActorUnitResult item in finalCaptureList)
                        {
                            GeoUnitDescriptor geoUnitDescriptor = null;
                            if ((int)item.GeoUnitId > 0)
                            {
                                IGeoTacUnit tacUnitById = geoLevel.GetTacUnitById(item.GeoUnitId);
                                geoUnitDescriptor = GeoUnitDescriptor.FromGeoTacUnit(tacUnitById);
                                geoUnitDescriptor.Faction = geoLevel.AlienFaction;
                                geoLevel.KillTacUnit(tacUnitById, CharacterDeathReason.Captured);
                            }
                            else if (item.SourceTemplate != null)
                            {
                                if (item.SourceTemplate is TacCharacterDef)
                                {
                                    geoUnitDescriptor = new GeoUnitDescriptor(geoLevel.AlienFaction, (TacCharacterDef)item.SourceTemplate);
                                }
                            }
                            else if (item.TacticalActorBaseDef != null)
                            {
                                TacCharacterDef templateForTacticalActorDef = __instance.Site.GeoLevel.CharacterGenerator.GetTemplateForTacticalActorDef(item.TacticalActorBaseDef);
                                if (templateForTacticalActorDef != null)
                                {
                                    geoUnitDescriptor = new GeoUnitDescriptor(geoLevel.AlienFaction, templateForTacticalActorDef);
                                }

                                Debug.LogWarning($"Tactical unit for capture '{item.TacticalActorBaseDef}' is missing SourceTemplate, used generic template instead!");
                            }
                            else
                            {
                                Debug.LogError("Invalid tac unit - no GeoUnitId and no actor def");
                            }

                            if (geoUnitDescriptor != null && geoUnitDescriptor.GetGameTags().Contains(geoLevel.AlienFaction.FactionDef.RaceTagDef))
                            {
                                list.Add(geoUnitDescriptor);
                            }
                        }

                        __instance.Reward.CapturedAliens = list.ToList();

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

    }
}
