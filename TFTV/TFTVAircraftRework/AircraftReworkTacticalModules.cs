using Base;
using Base.Core;
using Base.Entities.Statuses;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVAircraftRework;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.Statuses.ItemSlotStatsModifyStatusDef;
using static TFTV.AircraftReworkHelpers;
using static TFTV.TFTVAircraftReworkMain;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;

namespace TFTV
{

    internal class AircraftReworkTacticalModules
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static int _thunderBirdScannerPresent = 0;
        private static int _captureDronesPresent = 0;
        private static int _mistRepellerPresent = 0;
        private static int _heliosStealthPresent = 0;
        private static int _blimpMistPresent = 0;
        private static int _heliosPresent = 0;
        private static int _heliosNanotechPresent = 0;
        private static int _thunderbirdGroundAttackWeaponPresent = 0;
        private static int _blimpMutationLabFrenzyPresent = 0;
        private static int _blimpPriestResearch = 0;
        private static int _heliosVestBuff = 0;
        private static int _heliosStealthModulePerceptionBuff = 0;
        private static int _thunderbirdWorkshopPresent = 0;
        private static int _nestResearched = 0;
        private static int _lairResearched = 0;

        internal static string ReportModulesPresent()
        {
            try
            {
                string report = "";

                if (_thunderBirdScannerPresent > 0)
                {
                    report += "Scanner Module Present\n";
                }

                if (_captureDronesPresent > 0)
                {
                    report += "Capture Drones Module Present\n";
                }

                if (_mistRepellerPresent > 0)
                {
                    report += "Mist Repeller Module Present\n";
                }

                if (_heliosStealthPresent > 0)
                {
                    report += $"Helios Stealth Module Present, level {_heliosStealthPresent}\n";
                }

                if (_blimpMistPresent > 0)
                {
                    report += $"Blimp WP Module Present, level {_blimpMistPresent}\n";
                }

                if (_heliosPresent > 0)
                {
                    report += "Helios Present\n";
                }

                if (_heliosNanotechPresent > 0)
                {
                    report += "Helios Statis Chamber Present\n";
                }

                if (_thunderbirdGroundAttackWeaponPresent > 0)
                {
                    report += $"Thunderbird Ground Attack Weapon Present, level {_thunderbirdGroundAttackWeaponPresent}\n";
                }

                if (_blimpMutationLabFrenzyPresent > 0)
                {
                    report += "Blimp Mutation Lab Frenzy Module Present\n";
                }

                if (_blimpPriestResearch > 0)
                {
                    report += "Blimp Mutation Lab Priest Research Module Present\n";
                }

                if (_heliosVestBuff > 0)
                {
                    report += "Helios Vest Buff Present\n";
                }

                if (_heliosStealthModulePerceptionBuff > 0)
                {
                    report += $"Helios Stealth Module Perception Buff Present, level {_heliosStealthModulePerceptionBuff}\n";
                }
                if (_thunderbirdWorkshopPresent > 0)
                {
                    report += "Thunderbird Workshop Present\n";
                }
                if (_nestResearched > 0)
                {
                    report += "Nest Researched\n";
                }
                if (_lairResearched > 0)
                {
                    report += "Lair Researched\n";
                }

                return report;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void LoadInternalDataForTactical()
        {
            try
            {
                _thunderBirdScannerPresent = InternalData.ModulesInTactical[0];
                _captureDronesPresent = InternalData.ModulesInTactical[1];
                _mistRepellerPresent = InternalData.ModulesInTactical[2];
                _heliosStealthPresent = InternalData.ModulesInTactical[3];
                _blimpMistPresent = InternalData.ModulesInTactical[4];
                _heliosPresent = InternalData.ModulesInTactical[5];
                _heliosNanotechPresent = InternalData.ModulesInTactical[6];
                _thunderbirdGroundAttackWeaponPresent = InternalData.ModulesInTactical[7];
                _blimpMutationLabFrenzyPresent = InternalData.ModulesInTactical[8];
                _blimpPriestResearch = InternalData.ModulesInTactical[9];
                _heliosVestBuff = InternalData.ModulesInTactical[10];
                _heliosStealthModulePerceptionBuff = InternalData.ModulesInTactical[11];
                _thunderbirdWorkshopPresent = InternalData.ModulesInTactical[12];
                _nestResearched = InternalData.ModulesInTactical[13];
                _lairResearched = InternalData.ModulesInTactical[14];

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void SaveInternalDataForTactical()
        {
            try
            {
                InternalData.ModulesInTactical[0] = _thunderBirdScannerPresent;
                InternalData.ModulesInTactical[1] = _captureDronesPresent;
                InternalData.ModulesInTactical[2] = _mistRepellerPresent;
                InternalData.ModulesInTactical[3] = _heliosStealthPresent;
                InternalData.ModulesInTactical[4] = _blimpMistPresent;
                InternalData.ModulesInTactical[5] = _heliosPresent;
                InternalData.ModulesInTactical[6] = _heliosNanotechPresent;
                InternalData.ModulesInTactical[7] = _thunderbirdGroundAttackWeaponPresent;
                InternalData.ModulesInTactical[8] = _blimpMutationLabFrenzyPresent;
                InternalData.ModulesInTactical[9] = _blimpPriestResearch;
                InternalData.ModulesInTactical[10] = _heliosVestBuff;
                InternalData.ModulesInTactical[11] = _heliosStealthModulePerceptionBuff;
                InternalData.ModulesInTactical[12] = _thunderbirdWorkshopPresent;
                InternalData.ModulesInTactical[13] = _nestResearched;
                InternalData.ModulesInTactical[14] = _lairResearched;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void ClearTacticalDataOnLoad()
        {
            try
            {
                _thunderBirdScannerPresent = 0;
                _captureDronesPresent = 0;
                _mistRepellerPresent = 0;
                _heliosStealthPresent = 0;
                _blimpMistPresent = 0;
                _heliosPresent = 0;
                _heliosNanotechPresent = 0;
                _thunderbirdGroundAttackWeaponPresent = 0;
                _blimpMutationLabFrenzyPresent = 0;
                _blimpPriestResearch = 0;
                _heliosVestBuff = 0;
                _heliosStealthModulePerceptionBuff = 0;
                _thunderbirdWorkshopPresent = 0;
                _nestResearched = 0;
                _lairResearched = 0;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void CheckTacticallyRelevantModulesOnVehicle(GeoVehicle geoVehicle, GeoMission geoMission = null)
        {
            try
            {

                ClearTacticalDataOnLoad();

                if (!AircraftReworkOn || geoVehicle == null)
                {
                    return;
                }

                Research phoenixResearch = geoVehicle.GeoLevel.PhoenixFaction.Research;

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule))
                {
                    _thunderBirdScannerPresent = 0;

                    if (phoenixResearch.HasCompleted("PX_Alien_Colony_ResearchDef"))
                    {
                        _nestResearched = 1;
                    }

                    if (phoenixResearch.HasCompleted("PX_Alien_Lair_ResearchDef"))
                    {
                        _lairResearched = 1;
                    }

                    if (phoenixResearch.HasCompleted("NJ_NeuralTech_ResearchDef"))
                    {
                        _thunderBirdScannerPresent = 1;
                    }

                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _captureDronesModule))
                {
                    _captureDronesPresent = 1;
                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosMistRepellerModule))
                {
                    _mistRepellerPresent = 1;
                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosStealthModule))
                {
                    _heliosStealthPresent = 1;
                    _heliosStealthModulePerceptionBuff = 1;

                    if (phoenixResearch.HasCompleted("SYN_SafeZoneProject_ResearchDef"))
                    {
                        _heliosStealthPresent += 1;
                    }

                    if (phoenixResearch.HasCompleted("SYN_InfiltratorTech_ResearchDef"))
                    {
                        _heliosStealthPresent += 1;
                    }

                    if (phoenixResearch.HasCompleted("SYN_NightVision_ResearchDef"))
                    {
                        _heliosStealthModulePerceptionBuff += 1;
                    }

                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMistModule))
                {
                    _blimpMistPresent = Tiers.GetMistModuleBuffLevel();
                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosPanaceaModule))
                {
                    if (phoenixResearch.HasCompleted("SYN_NanoTech_ResearchDef"))
                    {
                        _heliosNanotechPresent = 1;
                    }

                    if (phoenixResearch.HasCompleted("SYN_NanoHealing_ResearchDef"))
                    {
                        _heliosVestBuff = 1;
                    }
                }

                if (geoVehicle.VehicleDef == helios)
                {
                    _heliosPresent = 1;
                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdGroundAttackModule))
                {
                    //must exclude, Palace, Alien Colony, Base Defense, 

                    TFTVLogger.Always($"Checking Thunderbird Ground Attack module for mission {geoMission.MissionDef?.name}");

                    if (geoMission.MissionDef.MissionTags.Contains(Shared.SharedGameTags.BaseDefenseMissionTag) ||
                        geoMission.MissionDef.MissionTags.Contains(Shared.SharedGameTags.BaseInfestationMissionTag) ||
                        geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienCitadelAssault_MissionTagDef")) ||
                        geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienLairAssault_MissionTagDef")) ||
                        geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienNestAssault_MissionTagDef")) ||
                        (geoMission.MissionDef.MapPlotDef != null && geoMission.MissionDef.MapPlotDef.name.Contains("ALN_PLT")))

                    {
                    }
                    else
                    {
                        TFTVLogger.Always($"Setting _thunderbirdGroundAttackWeapon to true");

                        _thunderbirdGroundAttackWeaponPresent = Tiers.GetGWABuffLevel();
                    }

                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutationLabModule) && phoenixResearch.HasCompleted("ANU_StimTech_ResearchDef"))
                {
                    _blimpMutationLabFrenzyPresent = 1;
                }
                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMistModule) && phoenixResearch.HasCompleted("ANU_AnuPriest_ResearchDef"))
                {
                    _blimpPriestResearch = 1;
                }

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule))
                {
                    //pending acid res implementation, + self-repair implementation
                    _thunderbirdWorkshopPresent = 1;

                    if (phoenixResearch.HasCompleted("PX_BlastResistanceVest_ResearchDef"))
                    {

                        _thunderbirdWorkshopPresent = 2;
                    }
                    // TFTVLogger.Always($"_thunderbirdWorkshopPresent: {_thunderbirdWorkshopPresent} ");
                }

                HeliosStatisChamber.ImplementVestBuff();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal class WorkshopModule
        {

            public static void WorkshopModuleLowerAcidApplyAffectCheck(DamageOverTimeStatus damageOverTimeStatus)
            {
                try 
                {

                    TacticalActor tacticalActor = damageOverTimeStatus.TacticalActor;
                    //   TFTVLogger.Always($"running ApplyEffect. ta null? {tacticalActor == null} {__instance.DamageOverTimeStatusDef.name}");

                    if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2 || damageOverTimeStatus.TacticalActor == null || !tacticalActor.IsControlledByPlayer)
                    {
                        return;
                    }

                    ItemSlot itemSlot = damageOverTimeStatus.Target as ItemSlot;

                    if (itemSlot == null)
                    {
                        return;
                    }


                    if (itemSlot.GetAllDirectItems(false) != null && (itemSlot.GetAllDirectItems(false).
                        Any(ti => ti.GameTags.Contains(Shared.SharedGameTags.BionicalTag) ||
                        ti.GetTopMainAddon() != null && ti.GetTopMainAddon().GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                        || tacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag)))
                    {
                        TFTVLogger.Always($"Lowering acid status for {damageOverTimeStatus.TacticalActor.DisplayName}.");

                        damageOverTimeStatus.LowerDamageOverTimeLevel(damageOverTimeStatus.DamageOverTimeStatusDef.LowerLevelPerTurn);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            } 




            /* [HarmonyPatch(typeof(DamageOverTimeStatus), "LowerDamageOverTimeLevelProportional")]
             public static class DamageOverTimeStatus_LowerDamageOverTimeLevelProportional_Patch
             {
                 public static void Prefix(DamageOverTimeStatus __instance, ref float multiplier)
                 {
                     try
                     {

                         TacticalActor tacticalActor = __instance.TacticalActor;
                         TFTVLogger.Always($"running LowerDamageOverTimeLevelProportional. ta null? {tacticalActor == null} multiplier: {multiplier}");

                         if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2)
                         {
                             return;
                         }

                         TFTVLogger.Always($"affected bodypart?  {__instance.Target?.GetType()}; {__instance.Target?.ToString()}; {__instance.GetTargetSlotsNames()?.FirstOrDefault()}; {__instance.GetTargetSlotsNames()?.Count()}");


                         if (tacticalActor != null && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.BionicalTag)))
                         {
                             TFTVLogger.Always($"Lowering acid status for {__instance.TacticalActor.DisplayName}.");

                             multiplier = 2;
                         }

                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                     }
                 }
             }



             [HarmonyPatch(typeof(DamageOverTimeStatus), "LowerDamageOverTimeLevel")]
             public static class DamageOverTimeStatus_LowerDamageOverTimeLevel_Patch
             {
                 public static void Prefix(DamageOverTimeStatus __instance, ref float amount)
                 {
                     try
                     {

                         TacticalActor tacticalActor = __instance.TacticalActor;
                         TFTVLogger.Always($"running LowerDamageOverTimeLevel. ta null? {tacticalActor == null}");

                         if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2)
                         {
                             return;
                         }




                         TFTVLogger.Always($"affected bodypart?  {__instance.Target?.GetType()}; {__instance.Target?.ToString()}; {__instance.GetTargetSlotsNames()?.FirstOrDefault()}; {__instance.GetTargetSlotsNames()?.Count()}");


                         if (tacticalActor != null && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.BionicalTag)))
                         {
                             TFTVLogger.Always($"Lowering acid status for {__instance.TacticalActor.DisplayName}.");

                             amount = 20;
                         }

                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                     }
                 }
             }*/

        }

        internal class FirstTurn
        {

            public static void ImplementModuleEffectsOnFirstTurn(TacticalLevelController controller)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }


                    ImplementHeliosStealthModule(controller);
                    //  ImplementBlimpWPModule(controller);
                    HeliosStatisChamber.ImplementPanaceaNanotechTactical(controller);

                    ImplementMutationLabFrenzy(controller);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void ImplementMutationLabFrenzy(TacticalLevelController controller)
            {
                try
                {
                    if (_blimpMutationLabFrenzyPresent == 0)
                    {
                        return;
                    }

                    FrenzyStatusDef frenzyStatusDef = DefCache.GetDef<FrenzyStatusDef>("Frenzy_StatusDef");

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        //  TFTVLogger.Always($"{tacticalActor.DisplayName} has {tacticalActor.BodyState.CharacterAddonsManager.RootAddon.Count()} addon items.");

                        foreach (Addon item in tacticalActor.BodyState.CharacterAddonsManager.RootAddon)
                        {
                            if (item is TacticalItem tacticalItem)
                            {
                                // TFTVLogger.Always($"tacticalItem: {tacticalItem.DisplayName} {tacticalItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)}" +
                                //     $";{tacticalItem.GetTopMainAddon()?.AddonDef?.name}"); //.

                                if (tacticalItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag) ||
                                    tacticalItem.GameTags.Contains(DefCache.GetDef<ItemMaterialTagDef>("MutatedTissue_ItemMaterialTagDef")))
                                {
                                    if (tacticalActor.Status != null && !tacticalActor.HasStatus(frenzyStatusDef))
                                    {
                                        tacticalActor.Status.ApplyStatus(frenzyStatusDef);
                                    }


                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            //Stealth module
            private static void ImplementHeliosStealthModule(TacticalLevelController controller)
            {
                try
                {
                    if (_heliosStealthPresent == 0)
                    {
                        return;
                    }

                    if (_heliosStealthPresent == 3)
                    {
                        _heliosStealthModuleStatus.StatModifications[0].Value = 0.5f;
                    }
                    else if (_heliosStealthPresent == 2)
                    {
                        _heliosStealthModuleStatus.StatModifications[0].Value = 0.3f;
                    }
                    else
                    {
                        _heliosStealthModuleStatus.StatModifications[0].Value = 0.1f;

                    }

                    if (_heliosStealthModulePerceptionBuff == 2)
                    {
                        _heliosStealthModuleStatus.StatModifications[1].Value = 15;
                    }
                    else
                    {
                        _heliosStealthModuleStatus.StatModifications[1].Value = 5;
                    }

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        if (!tacticalActor.Status.HasStatus(_heliosStealthModuleStatus))
                        {
                            tacticalActor.Status.ApplyStatus(_heliosStealthModuleStatus);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            //Blimp WP Module
            private static void ImplementBlimpWPModule(TacticalLevelController controller)
            {
                try
                {
                    if (_blimpMistPresent == 0)
                    {
                        return;
                    }

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        if (tacticalActor.CharacterStats.Willpower != null && !tacticalActor.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                        {
                            TFTVLogger.Always($"{tacticalActor.DisplayName} has {tacticalActor.CharacterStats.WillPoints} WPs");
                            tacticalActor.CharacterStats.Willpower.SetOverchargeCapacity(3);
                            tacticalActor.CharacterStats.Willpower.Add(3);
                            TFTVLogger.Always($"after module is applied, has {tacticalActor.CharacterStats.WillPoints} WPs");
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
        internal class EveryTurn
        {

            public static void ImplementModuleEffectsOnEveryPhoenixTurn(TacticalFaction tacticalFaction)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    if (tacticalFaction.IsControlledByPlayer)
                    {
                        ImplementScannerTacticalAbility(tacticalFaction.TacticalLevel);
                    }


                    ImplementArgusEye(tacticalFaction);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ImplementArgusEye(TacticalFaction faction)
            {
                try
                {
                    if (_thunderBirdScannerPresent == 0)
                    {
                        return;
                    }


                    TacticalFaction phoenixFaction = faction.TacticalLevel.GetFactionByCommandName("px");
                    if (faction != phoenixFaction)
                    {
                        foreach (TacticalActor tacticalActor in phoenixFaction.TacticalActors)
                        {
                            if (!tacticalActor.Status.HasStatus(_argusEyeStatus))
                            {
                                TFTVLogger.Always($"applying {_argusEyeStatus.name} to {tacticalActor.DisplayName}. " +
                                    $"\ncurrent accuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue} " +
                                    $"{tacticalActor.CharacterStats.Perception.Value.EndValue}", false);
                                tacticalActor.Status.ApplyStatus(_argusEyeStatus);
                                TFTVLogger.Always($"new accuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue} " +
                                     $"{tacticalActor.CharacterStats.Perception.Value.EndValue}", false);

                            }
                        }

                    }

                    if (faction == phoenixFaction)
                    {
                        foreach (TacticalActor tacticalActor in phoenixFaction.TacticalActors)
                        {
                            if (tacticalActor.Status.HasStatus(_argusEyeStatus))
                            {
                                tacticalActor.Status.UnapplyStatus(tacticalActor.Status.GetStatusByName(_argusEyeStatus.EffectName));
                                TFTVLogger.Always($"removing {_argusEyeStatus.name} to " +
                                    $"{tacticalActor.DisplayName} \naccuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue}", false);
                            }
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            //Thunderbird Range module big Panda detection
            private static void ImplementScannerTacticalAbility(TacticalLevelController controller)
            {
                try
                {
                    if (_nestResearched == 0 && _lairResearched == 0)
                    {
                        return;
                    }

                    //  TFTVLogger.Always($"ImplementScannerTacticalAbility: {_thunderBirdScannerPresent}");

                    ClassTagDef hatchingSentinelTag = DefCache.GetDef<ClassTagDef>("SentinelHatching_ClassTagDef");
                    ClassTagDef spawneryTag = DefCache.GetDef<ClassTagDef>("SpawningPoolCrabman_ClassTagDef");

                    List<ClassTagDef> tags = new List<ClassTagDef>();

                    if (_nestResearched == 1)
                    {
                        tags.Add(hatchingSentinelTag);
                    }

                    if (_lairResearched == 1)
                    {
                        tags.Add(spawneryTag);
                    }

                    List<TacticalActor> list = (from a in controller.Map.GetTacActors<TacticalActor>(controller.CurrentFaction, FactionRelation.Enemy)
                                                where !controller.CurrentFaction.Vision.KnownActors.ContainsKey(a)
                                                && a.IsActive
                                                && a.GameTags.Any(t => tags.Contains(t))
                                                select a).ToList();
                    if (list.Count > 0)
                    {
                        foreach (TacticalActor a in list)
                        {
                            TFTVLogger.Always($"actor spotted by scanner: {a.DisplayName}");
                            controller.CurrentFaction.Vision.IncrementKnownCounter(a, KnownState.Revealed, 1, true);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        internal class GroundAttackWeapon
        {

            internal static void ImplementGroundAttackWeaponModule(TacticalLevelController controller)
            {
                try
                {
                    if (_thunderbirdGroundAttackWeaponPresent == 0)
                    {
                        return;
                    }

                    Sprite icon = null;
                    if (_groundAttackAbility.LevelIcons != null && _thunderbirdGroundAttackWeaponPresent - 1 < _groundAttackAbility.LevelIcons.Length)
                    {
                        icon = _groundAttackAbility.LevelIcons[Math.Max(_thunderbirdGroundAttackWeaponPresent - 1, 0)];
                    }


                    if (icon != null)
                    {
                        _groundAttackAbility.ViewElementDef.SmallIcon = icon;
                        _groundAttackAbility.ViewElementDef.LargeIcon = icon;
                    }

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        GroundAttackWeaponAbility ability = tacticalActor.GetAbilityWithDef<GroundAttackWeaponAbility>(_groundAttackAbility) ?? (GroundAttackWeaponAbility)tacticalActor.AddAbility(_groundAttackAbility, tacticalActor);
                        ability.ConfigureForLevel(_thunderbirdGroundAttackWeaponPresent);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static void RemoveGroundAttackWeaponModuleAbility()
            {
                try
                {
                    if (_thunderbirdGroundAttackWeaponPresent == 0)
                    {
                        return;
                    }

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        if (tacticalActor.GetAbilityWithDef<GroundAttackWeaponAbility>(_groundAttackAbility) != null)
                        {
                            tacticalActor.RemoveAbility(_groundAttackAbility);
                        }
                    }

                    _thunderbirdGroundAttackWeaponPresent = 0;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
        internal class AnuMistModule
        {
            /// <summary>
            /// Modifying the perception range cost of the mist blob
            /// </summary>
            [HarmonyPatch(typeof(TacticalPerceptionBase), "get_MistBlobPerceptionRangeCost")]
            public static class TacticalPerceptionBase_get_MistBlobPerceptionRangeCost_Patch
            {
                public static void Postfix(TacticalPerceptionBase __instance, ref float __result)
                {
                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (TFTVVoidOmens.VoidOmensCheck[7] && config.MoreMistVO && AircraftReworkOn)
                        {
                            __result /= 3;
                        }

                        TacticalActor tacticalActor = __instance.TacActorBase as TacticalActor;

                        if (tacticalActor == null)
                        {
                            return;
                        }

                        int mistSymbiosisLevel = CheckForAnuBlimpMistModule(tacticalActor.TacticalFaction);

                        //if aircraft rework is on and player has researched Mutations2 or 3:

                        if (tacticalActor != null && mistSymbiosisLevel > 1
                            && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                        {

                            if (mistSymbiosisLevel == 2)
                            {
                                __result /= 2;
                            }
                            else
                            {
                                __result = 0;
                            }
                        }

                        // TFTVLogger.Always($"MistBlobPerceptionRangeCost for {tacticalActor.DisplayName} is {__result}");


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            private static int CheckForAnuBlimpMistModule(TacticalFaction tacticalFaction)
            {
                try
                {
                    return AircraftReworkOn && _blimpMistPresent > 0 && tacticalFaction.IsControlledByPlayer ? _blimpMistPresent : 0;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            /// <summary>
            /// Mist effects on WP
            /// </summary>
            [HarmonyPatch(typeof(TacticalActor), "ApplyMistEffects")]
            public static class TacticalActor_ApplyMistEffects_Patch
            {
                public static bool Prefix(TacticalActor __instance)
                {
                    try
                    {
                        //if aircraft rework is on and the Mist module is present and the actor has the Anu mutation tag,
                        //if the actor is in mist and player has research Mutations2, apply WP regen
                        //else, don't subtract WP

                        int mistSymbiosisLevel = CheckForAnuBlimpMistModule(__instance.TacticalFaction);

                        if (mistSymbiosisLevel == 0 ||
                            !__instance.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                        {
                            return true;
                        }

                        if (!__instance.TacticalPerception.IsTouchingVoxel(TacticalVoxelType.Mist))
                        {
                            return false;
                        }

                        TacticalVoxelMatrixDataDef voxelMatrixData = __instance.TacticalLevel.VoxelMatrix.VoxelMatrixData;

                        if (mistSymbiosisLevel > 1)
                        {
                            __instance.CharacterStats.WillPoints.Add(voxelMatrixData.MistRecoverWillPointsValue);
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


            /// <summary>
            /// These 2 patches reveal non-Pandoran actors in mist to the player with the Anu blimp module and Priest Research.
            /// </summary>
            [HarmonyPatch(typeof(TacticalFactionVision))]
            public static class TacticalFactionVision_PrefixPatch
            {
                //────────────────────────────────────────────────────────────
                // 1. Replacement for GatherKnowableActors
                //────────────────────────────────────────────────────────────
                [HarmonyPrefix]
                [HarmonyPatch("GatherKnowableActors")]
                public static bool GatherKnowableActorsPrefix(TacticalFactionVision __instance,
                    TacticalActorBase fromActor,
                    Vector3 fromActorPos,
                    float basePerceptionRange,
                    ICollection<TacticalActorBase> visible,
                    ICollection<TacticalActorBase> located)
                {
                    try
                    {

                        //also checks for AircraftRework


                        int mistSymbiosisLevel = CheckForAnuBlimpMistModule(fromActor.TacticalFaction);


                        if (mistSymbiosisLevel == 0 || _blimpPriestResearch == 0)
                        {
                            return true;
                        }

                        //skip patch if fromActor tactical faction is Pandoran.

                        TacticalFaction fromActorTacticalFaction = fromActor.TacticalFaction;

                        if (fromActorTacticalFaction.TacticalFactionDef.MatchesShortName("ALN"))
                        {
                            return true;
                        }


                        foreach (TacticalActorBase actor in fromActor.Map.GetActors<TacticalActorBase>())
                        {

                            // Skip if the actor is the same as fromActor or if they are on the same faction, if the actor has null PerceptionBase, or actor is evaced
                            if (actor == fromActor ||
                                actor.TacticalFaction == fromActorTacticalFaction ||
                                actor.TacticalPerceptionBase == null ||
                                (actor.Status != null && actor.Status.HasStatus<EvacuatedStatus>()))
                            {
                                continue;
                            }


                            //if fromActor faction is Phoenix, the actor is in mist, the actor is not Pandoran, should be revealed to Player if _blimpPriestResearch!=0
                            //note that will be revealed to Pandoran fromActor too because og method will run
                            if (fromActorTacticalFaction.IsControlledByPlayer && actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist) &&
                                actor.TacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef != actor.TacticalFaction.TacticalFactionDef)
                            {
                                visible.Add(actor);
                                // TFTVLogger.Always($"{actor.DisplayName} touching Mist, so revealing to {fromActor.DisplayName}");
                            }

                            else if ((bool)TacticalFactionVision.CheckVisibleLineBetweenActors(
                                         fromActor, fromActorPos, actor,
                                         true, null,
                                         1, null))
                            {
                                visible.Add(actor);
                                //   TFTVLogger.Always($"{actor.DisplayName} in LOS at a distance of {(fromActorPos - actor.Pos).magnitude}, so revealing to {fromActor.DisplayName}");
                            }
                            else if (actor is TacticalActor && actor.IsAlive && !actor.IsCloaked)
                            {
                                TacticalLevelController tacticalLevel = fromActor.TacticalLevel;
                                if ((fromActorPos - actor.Pos).magnitude <= tacticalLevel.TacticalLevelControllerDef.DetectionRange)
                                {
                                    located.Add(actor);
                                }
                            }
                        }
                        // Returning false skips the original method.
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                //────────────────────────────────────────────────────────────
                // 2. Replacement for ReUpdateVisibilityTowardsActorImpl
                //────────────────────────────────────────────────────────────
                [HarmonyPrefix]
                [HarmonyPatch("ReUpdateVisibilityTowardsActorImpl")]
                public static bool ReUpdateVisibilityTowardsActorImplPrefix(
                    TacticalFactionVision __instance,
                    TacticalActorBase fromActor,
                    TacticalActorBase targetActor,
                    float basePerceptionRange,
                    bool notifyChange,
                    ref bool __result)
                {
                    try
                    {
                        //if the viewing actor is evaced, early exit to fix Vanilla bug
                        if (fromActor is TacticalActor tacticalActor && tacticalActor.IsEvacuated)
                        {
                            __result = false;
                            return false;
                        }

                        if (targetActor == null || !targetActor.InPlay)
                        {
                            return true; // let OG handle; it's safer during enter-play
                        }


                        int mistSymbiosisLevel = CheckForAnuBlimpMistModule(fromActor.TacticalFaction);


                        // If Aircratf rework is off/the module is not present/Priest is not researched, use og method
                        if (mistSymbiosisLevel == 0 || _blimpPriestResearch == 0)
                        {
                            return true;
                        }

                        //If viewing faction is not Phoenix, use og method
                        if (!fromActor.TacticalFaction.TacticalFactionDef.MatchesShortName("px"))
                        {
                            return true;
                        }

                        //If viewing actor is dead, early exit as per OG method
                        if (fromActor.IsDead)
                        {
                            __result = false;
                            return false;
                        }

                        //condition to reveal targetactor to viewingfaction
                        bool condition = false;

                        //condition is true if the target actor is in mist and not Pandoran 

                        if (targetActor.TacticalPerceptionBase != null && targetActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist)
                            && targetActor.TacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef != targetActor.TacticalFaction.TacticalFactionDef)
                        {

                            // TFTVLogger.Always($"{targetActor.DisplayName} in Mist revealed to {fromActor.DisplayName}");
                            condition = true;
                        }
                        else if (TacticalFactionVision.CheckVisibleLineBetweenActors(fromActor, fromActor.Pos, targetActor, true, null, 1, null))
                        {
                            // TFTVLogger.Always($"{targetActor.DisplayName} revealed to {fromActor.DisplayName} because LOS");
                            condition = true;
                        }



                        if (condition)
                        {
                            // Call IncrementKnownCounterImpl on targetActor.
                            MethodInfo mIncrement = AccessTools.Method(__instance.GetType(), "IncrementKnownCounterImpl");
                            __result = (bool)mIncrement.Invoke(__instance, new object[] { targetActor, KnownState.Revealed, 1, notifyChange, null });
                        }
                        else
                        {
                            __result = false;
                        }



                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                /// <summary>
                /// This patch hides player Mutants from factions other than Pandorans when they enter Mist
                /// Removed, because actors are still located, like Pandorans. 
                /// </summary> 
                //────────────────────────────────────────────────────────────
                // 3. Replacement for OnActorMoved(TacticalActorBase)
                //────────────────────────────────────────────────────────────
                /*  [HarmonyPrefix]
                  [HarmonyPatch("OnActorMoved", new[] { typeof(TacticalActorBase) })]
                  public static bool OnActorMovedPrefix(TacticalFactionVision __instance, TacticalActorBase movedActor)
                  {
                      try
                      {

                          TacticalLevelController tacticalLevel = __instance.Faction.TacticalLevel;

                          //Should only work if movedActor is controlled by Phoenix, blimp Mist module and Priest Research are present,
                          //and if the Faction doing the viewing isn't Pandoran 
                          if (!AircraftReworkOn || _blimpMistPresent == 0 || _blimpPriestResearch == 0 || !movedActor.TacticalFaction.IsControlledByPlayer
                              || tacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef == __instance.Faction.TacticalFactionDef)
                          {
                              return true;
                          }

                          TacticalActor tacticalActor = movedActor as TacticalActor;

                          //only works for mutants
                          if (tacticalActor == null || !tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                          {
                              TFTVLogger.Always($"{tacticalActor.DisplayName} not a mutant, so early exit for OnActorMoved");
                              return true;
                          } 

                          // Early out if we are not in turn or the actor should not be processed.
                          if (!tacticalLevel.TurnIsPlaying ||
                              !movedActor.InPlay ||
                              (movedActor.Status != null && movedActor.Status.HasStatus<EvacuatedStatus>()))
                          {
                              return false;
                          }

                          // Update faction knowledge on actor movement
                          if (movedActor.TacticalFaction == __instance.Faction)
                          {
                              MethodInfo methodInfoUpdateVisibilityForImpl = typeof(TacticalFactionVision).GetMethod("UpdateVisibilityForImpl", BindingFlags.Instance | BindingFlags.NonPublic);
                              bool changed = (bool)methodInfoUpdateVisibilityForImpl.Invoke(__instance, new object[] { movedActor, tacticalLevel.TacticalLevelControllerDef.DetectionRange, true });
                              if (changed)
                              {
                                  tacticalLevel.FactionKnowledgeChanged(__instance.Faction);
                              }
                              return false;
                          }

                          bool flag = false;
                          bool flag2 = false;
                          bool flag3 = false;

                          //Should hide moved actor if moves into Mist
                          if (movedActor.TacticalPerceptionBase != null &&
                              movedActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist)) 
                          {
                              TFTVLogger.Always($"hiding {movedActor.DisplayName} in Mist");
                              MethodInfo methodInfoResetKnownCounterImpl = typeof(TacticalFactionVision).GetMethod("ResetKnownCounterImpl", BindingFlags.Instance | BindingFlags.NonPublic);
                              flag2 = (bool)methodInfoResetKnownCounterImpl.Invoke(__instance, new object[] { movedActor, KnownState.Revealed, false, null });
                          }

                          // Process each actor in the faction.
                          foreach (TacticalActorBase actor in __instance.Faction.Actors)
                          {
                              if (actor.TacticalPerceptionBase != null &&
                                 (tacticalLevel.CurrentFaction == __instance.Faction ||
                                  actor.TacticalPerceptionBase.TacticalPerceptionBaseDef.UpdateOnOthersTurn))
                              {

                                  MethodInfo mReUpdateVis = AccessTools.Method(__instance.GetType(), "ReUpdateVisibilityTowardsActorImpl");

                                  bool res1 = (bool)mReUpdateVis.Invoke(__instance, new object[]
                                  { actor, movedActor, tacticalLevel.TacticalLevelControllerDef.DetectionRange, !flag2 });
                                  flag |= res1;

                                  MethodInfo mReUpdateHear = AccessTools.Method(__instance.GetType(), "ReUpdateHearingImpl");

                                  //  TFTVLogger.Always($"mReUpdateHear null? {mReUpdateHear == null}");

                                  bool res2 = (bool)mReUpdateHear.Invoke(__instance, new object[] { actor, movedActor, true });
                                  flag3 |= res2;
                              }
                          }
                          if ((flag ^ flag2) || flag3)
                          {
                              tacticalLevel.FactionKnowledgeChanged(__instance.Faction);
                          }
                          return false;
                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                          throw;
                      }
                  }*/







            }
        }
        internal class CaptureDrones
        {

            [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
            public static class UIStateRosterDeployment_EnterState_Patch
            {
                public static void Prefix(UIStateRosterDeployment __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        GeoMission mission = __instance.Mission;
                        GeoSite geoSite = mission.Site;
                        GeoVehicle geoVehicle = geoSite.GetPlayerVehiclesOnSite()?.FirstOrDefault();

                        if (geoVehicle != null)
                        {
                            if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _captureDronesModule) && mission.MissionDef.DontRecoverItems)
                            {
                                _captureDronesPresent = 1;
                                mission.MissionDef.DontRecoverItems = false;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                public static void Postfix(UIStateRosterDeployment __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        GeoMission mission = __instance.Mission;

                        if (_captureDronesPresent > 0)
                        {
                            mission.MissionDef.DontRecoverItems = true;
                            // CaptureDronesModulePresent = false;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /*  [HarmonyPatch(typeof(GeoMission), "GetItemsOnTheGround")]
              public static class GeoMission_GetItemsOnTheGround2_Patch
              {
                  public static void Postfix(GeoMission __instance, TacMissionResult result, ref IEnumerable<GeoItem> __result)
                  {
                      try
                      {


                          if (!AircraftReworkOn)
                          {
                              return;
                          }

                          TFTVLogger.Always($"__result==null? {__result==null}");

                          foreach(GeoItem geoItem in __result)
                          {
                              TFTVLogger.Always($"item on the ground: {geoItem?.ItemDef?.name}");
                          }

                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                          throw;

                      }
                  }

              }*/






            [HarmonyPatch(typeof(GeoMission), "ManageGear")]
            public static class GeoMission_ManageGear_Patch
            {
                public static void Prefix(GeoMission __instance, TacMissionResult result, GeoSquad squad, out bool __state)
                {
                    try
                    {
                        __state = false;

                        if (!AircraftReworkOn)
                        {
                            return;
                        }


                        if (_captureDronesPresent > 0 && __instance.MissionDef.DontRecoverItems)
                        {
                            TFTVLogger.Always($"got here; salvage drone module changing missionDef {__instance.MissionDef.name} to recover items");

                            __instance.MissionDef.DontRecoverItems = false;
                            __state = true;
                        }



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                public static void Postfix(GeoMission __instance, in bool __state)
                {
                    try
                    {
                        if (!AircraftReworkOn || !__state)
                        {
                            return;
                        }



                        if (_captureDronesPresent > 0)
                        {
                            TFTVLogger.Always($"changing missionDef {__instance.MissionDef.name} back to not recover items");
                            __instance.MissionDef.DontRecoverItems = true;
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }


            [HarmonyPatch(typeof(TacticalLevelController), "GetMissionResult")]
            internal static class TacticalLevelController_GetMissionResult_Prefix
            {
                // Prefix fully replaces original; __result is set and we return false.
                static void Prefix(TacticalLevelController __instance)
                {
                    try
                    {
                        //  TFTVLogger.Always($"Running GetMissionResult");

                        foreach (TacticalActorBase actor in __instance.Map.GetActors<TacticalActorBase>().
                            Where(tab => tab is CrateItemContainer crateItemContainer && crateItemContainer.GetComponent<CrateComponent>() != null
                            && !crateItemContainer.GetComponent<CrateComponent>().IsOpen()))
                        {
                            // TFTVLogger.Always($"Unopened crate: {actor?.name}");

                            CrateItemContainer crate = actor as CrateItemContainer;

                            TFTVLogger.Always($"container not open, contains {actor?.Inventory?.Items?.Count} items");
                            actor.Inventory.Items.Clear();
                            TFTVLogger.Always($"emptied! new count: {actor?.Inventory?.Items?.Count} items");

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }

            }

            /*      [HarmonyPatch(typeof(GeoMission), "GetItemsOnTheGround")]
              internal static class GeoMission_GetItemsOnTheGround_Prefix
              {
                  // Prefix fully replaces original; __result is set and we return false.
                  static bool Prefix(GeoMission __instance, TacMissionResult result, ref IEnumerable<GeoItem> __result)
                  {
                      try
                      {
                          var site = __instance.Site;
                          var level = site.GeoLevel;

                          // Re-derive the private ManufactureTag the original uses
                          var manufactureTag = __instance.GameController
                              .GetComponent<SharedData>()
                              .SharedGameTags
                              .ManufacturableTag;

                          // Alien "items" tag used to exclude alien-only gear
                          var alienItemsRaceTag = level.AlienFaction.FactionDef.RaceTagDef;

                          // This is how the original gets the Environment faction's results
                          var envResult = result.GetResultByFacionDef(level.EnvironmentFactionDef.PPFactionDef);

                          // Defensive guard
                          if (envResult == null)
                          {
                              TFTVLogger.Always("[TFTV] GetItemsOnTheGround: No environment faction result found.");
                              __result = Enumerable.Empty<GeoItem>();
                              return false;
                          }

                          // Pull every ItemContainerResult the mission produced for the environment faction
                          var containers = envResult.UnitResults
                              .Select(u => u.Data)
                              .OfType<ItemContainerResult>()
                              .ToList();

                          // ---- Custom logging per ItemContainerResult (requested) ----
                          // We don't know extra metadata on ItemContainerResult beyond InventoryItems,
                          // so we log index + counts + a few item names for quick inspection.
                          for (int i = 0; i < containers.Count; i++)
                          {


                              var c = containers[i];


                              int count = c?.InventoryItems?.Count() ?? 0;
                              var sampleNames = (c?.InventoryItems ?? Enumerable.Empty<ItemData>())
                                  .Take(5)
                                  .Select(it => it?.ItemDef?.name ?? "<null>")
                                  .ToArray();



                              TFTVLogger.Always($"[TFTV] ItemContainerResult[{i}] -> items: {count}, sample: {string.Join(", ", sampleNames)}");
                          }
                          // ------------------------------------------------------------

                          var picked = new List<GeoItem>();

                          foreach (var container in containers)
                          {
                              TFTVLogger.Always($"container count: {container.InventoryItems.Count}");

                              if (container.InventoryItems == null)
                                  continue;

                              foreach (var invItem in container.InventoryItems)
                              {
                                  // Mirror original filters:
                                  // 1) Must be manufacturable (has manufactureTag)
                                  // 2) Must NOT be alien-only (own-tags include alienItemsRaceTag)
                                  // 3) Exclude permanent augments (permanent TacticalItemDef augments)
                                  bool isManufacturable = (invItem.ItemDef.Tags?.Contains(manufactureTag) ?? false);
                                  bool isAlienOnly = (invItem.OwnTags?.Contains(alienItemsRaceTag) ?? false);

                                  bool isPermanentAugment =
                                      invItem.ItemDef is TacticalItemDef tItem &&
                                      tItem.IsPermanentAugment;

                                  if (isManufacturable && !isAlienOnly && !isPermanentAugment)
                                  {
                                      picked.Add(new GeoItem(invItem));
                                  }
                                  else
                                  {
                                      // Detailed log on excluded items to help validate filters
                                      TFTVLogger.Always($"[TFTV] Excluded ground item '{invItem?.ItemDef?.name}': " +
                                                $"Manufacturable={isManufacturable}, AlienOnly={isAlienOnly}, PermanentAugment={isPermanentAugment}");
                                  }
                              }
                          }

                          __result = picked;
                          return false; // skip original
                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Always($"[TFTV] GetItemsOnTheGround Prefix failed: {e}");
                          // On error, let original run to avoid breaking gameplay
                          return true;
                      }
                  }
              }*/

        }
        //Helios:
        internal class HeliosStatisChamber
        {

            private static bool _vestBuffApplied;
            private static float _ablativeResistanceBase = -1f;
            private static float _hazmatResistanceBase = -1f;
            private static float _ablativeHealthBase = -1f;
            private static float _hazmatArmorBase = -1f;


            internal static void ImplementPanaceaNanotechTactical(TacticalLevelController controller)
            {
                try
                {
                    if (_heliosNanotechPresent == 0)
                    {
                        return;
                    }

                    DamageOverTimeResistanceStatusDef damageOverTimeResistanceStatusDef = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                    {
                        if (tacticalActor.Status != null && !tacticalActor.HasStatus(damageOverTimeResistanceStatusDef))
                        {
                            tacticalActor.Status.ApplyStatus(damageOverTimeResistanceStatusDef);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ResetVestDefs()
            {
                try
                {
                    if (!_vestBuffApplied)
                    {
                        return;
                    }

                    CacheVestBaseValuesIfNeeded();

                    UpdateResistanceMultiplier(TFTVVests.AblativeResistancesDef, _ablativeResistanceBase);
                    UpdateResistanceMultiplier(TFTVVests.HazmatResistancesDef, _hazmatResistanceBase);
                    UpdateSlotStatModifier(TFTVVests.HealthBuffStatusDef, StatType.Health, _ablativeHealthBase);
                    UpdateSlotStatModifier(TFTVVests.ArmorBuffStatusDef, StatType.Armour, _hazmatArmorBase);

                    _vestBuffApplied = false;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void SetVestDefsForVestBuff()
            {
                try
                {
                    CacheVestBaseValuesIfNeeded();

                    UpdateResistanceMultiplier(TFTVVests.AblativeResistancesDef, _ablativeResistanceBase * 1.5f);
                    UpdateResistanceMultiplier(TFTVVests.HazmatResistancesDef, _hazmatResistanceBase * 1.5f);
                    UpdateSlotStatModifier(TFTVVests.HealthBuffStatusDef, StatType.Health, _ablativeHealthBase * 1.5f);
                    UpdateSlotStatModifier(TFTVVests.ArmorBuffStatusDef, StatType.Armour, _hazmatArmorBase * 1.5f);

                    _vestBuffApplied = true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CacheVestBaseValuesIfNeeded()
            {
                if (_ablativeResistanceBase < 0f && TFTVVests.AblativeResistancesDef != null)
                {
                    _ablativeResistanceBase = TFTVVests.AblativeResistancesDef.Multiplier;
                }

                if (_hazmatResistanceBase < 0f && TFTVVests.HazmatResistancesDef != null)
                {
                    _hazmatResistanceBase = TFTVVests.HazmatResistancesDef.Multiplier;
                }

                if (_ablativeHealthBase < 0f)
                {
                    _ablativeHealthBase = ReadSlotStatModifierValue(TFTVVests.HealthBuffStatusDef, StatType.Health);
                }

                if (_hazmatArmorBase < 0f)
                {
                    _hazmatArmorBase = ReadSlotStatModifierValue(TFTVVests.ArmorBuffStatusDef, StatType.Armour);
                }
            }

            private static void UpdateResistanceMultiplier(DamageMultiplierStatusDef statusDef, float multiplier)
            {
                if (statusDef == null || multiplier < 0f)
                {
                    return;
                }

                statusDef.Multiplier = multiplier;
            }

            private static void UpdateSlotStatModifier(ItemSlotStatsModifyStatusDef statusDef, StatType statType, float value)
            {
                if (statusDef == null || value < 0f)
                {
                    return;
                }

                foreach (ItemSlotModification modification in statusDef.StatsModifications)
                {
                    if (modification.Type == statType)
                    {
                        modification.Value = value;
                    }
                }
            }

            private static float ReadSlotStatModifierValue(ItemSlotStatsModifyStatusDef statusDef, StatType statType)
            {
                if (statusDef == null || statusDef.StatsModifications == null)
                {
                    return -1f;
                }

                foreach (ItemSlotModification modification in statusDef.StatsModifications)
                {
                    if (modification.Type == statType)
                    {
                        return modification.Value;
                    }
                }

                return -1f;
            }


            internal static void ImplementVestBuff()
            {
                try
                {
                    if (!AircraftReworkOn || _heliosVestBuff == 0)
                    {
                        ResetVestDefs();
                        return;
                    }

                    SetVestDefsForVestBuff();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            [HarmonyPatch(typeof(DamageOverTimeStatus), "OnApply")]
            public static class DamageOverTimeStatus_OnApply_Patch
            {
                public static void Postfix(DamageOverTimeStatus __instance, StatusComponent statusComponent)
                {
                    try
                    {
                        if (!AircraftReworkOn || _heliosNanotechPresent == 0)
                        {
                            return;
                        }

                        TacticalActor tacticalActor = __instance.TacticalActor;

                        DamageOverTimeResistanceStatusDef nanotechStatus = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");

                        if (nanotechStatus.StatusDefs.Contains(__instance.DamageOverTimeStatusDef))
                        {
                            if (tacticalActor != null)
                            {
                                if (tacticalActor.HasStatus(nanotechStatus))
                                {
                                    DamageOverTimeResistanceStatus status = (DamageOverTimeResistanceStatus)tacticalActor.Status.GetStatusByName(nanotechStatus.EffectName);
                                    tacticalActor.Status.UnapplyStatus(status);
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

            [HarmonyPatch(typeof(FireStatus), "CalculateFireDamage")]
            public static class FireStatus_CalculateFireDamage_Patch
            {
                public static void Postfix(FireStatus __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn || _heliosNanotechPresent == 0)
                        {
                            return;
                        }

                        TacticalActor tacticalActor = __instance.TacticalActor;
                        DamageOverTimeResistanceStatusDef nanotechStatus = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");


                        if (tacticalActor != null)
                        {
                            if (tacticalActor.HasStatus(nanotechStatus))
                            {
                                DamageOverTimeResistanceStatus status = (DamageOverTimeResistanceStatus)tacticalActor.Status.GetStatusByName(nanotechStatus.EffectName);
                                tacticalActor.Status.UnapplyStatus(status);
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

            /*  [HarmonyPatch(typeof(DamageOverTimeResistanceStatus), "ApplyResistance")]
              public static class DamageOverTimeResistanceStatus_ApplyResistance_Patch
              {
                  static void Postfix(DamageOverTimeResistanceStatus __instance)
                  {
                      try
                      {
                          if (!AircraftReworkOn || !HeliosStatisChamberPresent)
                          {
                              return;
                          }

                          if (__instance.DamageOverTimeResistanceStatusDef == DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef"))
                          {
                              TacticalActor tacticalActor = __instance.TacticalActor;

                              if (tacticalActor != null && tacticalActor.Status != null)
                              {
                                  tacticalActor.Status.UnapplyStatus(__instance);
                              }
                          }
                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                          throw;
                      }
                  }
              }*/
        }
        //Mist repeller effects

        internal class MistRepeller
        {

            [HarmonyPatch(typeof(TacticalLevelController), "OnLevelStart")]//OnLevelStateChanged")]
            public static class OnLevelStart_Patch
            {
                public static void Postfix(TacticalLevelController __instance) //Level.State prevState, Level.State state, )
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        if (_mistRepellerPresent > 0)
                        {
                            TFTVLogger.Always("Mist repeller module present");

                            __instance.TacticalGameParams.IsCorruptionActive = false;
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static void ImplementMistRepellerTurnStart(TacticalVoxelMatrix tacticalVoxelMatrix, TacticalVoxel[] _voxels)
            {
                try
                {
                    if (!AircraftReworkOn || _mistRepellerPresent == 0)
                    {
                        return;
                    }


                    var mistVoxels = _voxels.Where(v => v != null && v.GetVoxelType() == TacticalVoxelType.Mist).ToList();

                    // Calculate the number of voxels to remove
                    int voxelsToRemove = mistVoxels.Count / 2;

                    TFTVLogger.Always($"Activating Mist Repeller! Current mist voxels: {mistVoxels.Count}. Mist voxels to remove {voxelsToRemove}");

                    // Shuffle the list to randomize which voxels are removed
                    mistVoxels = mistVoxels.OrderBy(v => UnityEngine.Random.value).ToList();

                    // Remove mist from half of the voxels
                    for (int i = 0; i < voxelsToRemove; i++)
                    {
                        mistVoxels[i].SetVoxelType(TacticalVoxelType.Empty);
                    }

                    // Update the voxel matrix to reflect the changes
                    tacticalVoxelMatrix.UpdateVoxelMatrix();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }
        //Helios advantage
        [HarmonyPatch(typeof(SurviveTurnsFactionObjectiveDef), "GenerateObjective")]
        public static class SurviveTurnsFactionObjectiveDef_GenerateObjective_Patch
        {
            public static void Prefix(SurviveTurnsFactionObjectiveDef __instance, TacticalLevelController level, TacticalFaction faction, out int? __state)//, int ____squadMaxDeployment)
            {
                try
                {
                    __state = __instance.SurviveTurns;

                    if (AircraftReworkOn && _heliosPresent > 0)
                    {
                        //   TFTVLogger.Always($"got here, {__instance.SurviveTurns}");

                        __instance.SurviveTurns -= 1;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void Postfix(SurviveTurnsFactionObjectiveDef __instance, TacticalLevelController level, TacticalFaction faction, int? __state)//, int ____squadMaxDeployment)
            {
                try
                {
                    if (AircraftReworkOn && _heliosPresent > 0)
                    {
                        __instance.SurviveTurns = __state.Value;
                    }
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

