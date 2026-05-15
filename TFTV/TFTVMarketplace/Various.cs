using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using Base.Defs;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TFTV.TFTVBaseRework;
using TFTV.Vehicles.Ammo;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static GameTagDef MercenaryTag;

        private static bool AmbushOrScavTemp = false;



        /// <summary>
        /// Allows to buy in Marketplace without an aircraft at the site.
        /// </summary>

        [HarmonyPatch(typeof(MarketplaceAbility), "GetTargetDisabledStateInternal")] //VERIFIED
        public static class MarketplaceAbility_GetTargetDisabledStateInternal_patch
        {

            public static void Postfix(ref GeoAbilityTargetDisabledState __result, MarketplaceAbility __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("NumberOfDLC5MissionsCompletedVariable") > 0)
                    {

                        __result = GeoAbilityTargetDisabledState.NotDisabled;

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.AddRecruit))]
        public static class GeoPhoenixFaction_AddRecruit_patch
        {
            public static bool Prefix(GeoPhoenixFaction __instance, GeoCharacter recruit, IGeoCharacterContainer toContainer, IGeoCharacterContainer __result)
            {
                try
                {
                    TFTVLogger.Always($"{recruit.DisplayName} {toContainer?.Name} toContainer geosite? {toContainer is GeoSite} toContainer is PhoenixBase? {toContainer is GeoPhoenixBase}.");

                    /* foreach(GameTagDef gameTagDef in recruit.TemplateDef.GetGameTags()) 
                     {
                         TFTVLogger.Always($"{gameTagDef.name}");
                     }*/

                    if ((recruit.TemplateDef != null && recruit.TemplateDef.GetGameTags().Contains(MercenaryTag)
                        || recruit.GameTags.Contains(DefCache.GetDef<GameTagDef>("KaosBuggy_ClassTagDef"))
                        || recruit.GameTags.Contains(TFTVProjectOsiris.OCPProductTag))
                        && toContainer != null && toContainer is GeoSite)
                    {
                        if (recruit.GameTags.Contains(TFTVProjectOsiris.OCPProductTag))
                        {
                            recruit.LevelProgression.SetLevel(__instance.GeoLevel.DeadSoldiers[TFTVProjectOsiris.IdProjectOsirisCandidate].Level);
                        }

                        __instance.GeoLevel.View.PrepareDeployAsset(__instance, recruit, null, null, manufactured: false, spaceFull: false);
                        __result = null;
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

            public static void Postfix(GeoPhoenixFaction __instance, GeoCharacter recruit, IGeoCharacterContainer toContainer)
            {
                try
                {
                   

                    if (recruit == null || !TFTVAircraftReworkMain.AircraftReworkOn) return;

                    if (recruit.GameTags.Contains(DefCache.GetDef<GameTagDef>("KaosBuggy_ClassTagDef")))
                    {
                        TFTVLogger.Always($"[GeoPhoenixFaction.AddRecruit] Got a Junker, will try to load its guns");
                        TryLoadPurchasedVehicleJunkerWeapons(recruit);

                    }

                    if (!BaseReworkCheck.BaseReworkEnabled)
                    {
                        return;
                    }

                    // Apply deferred stat gains for recruits finalized via UI path.
                    var tfType = typeof(TFTV.TFTVBaseRework.TrainingFacilityRework);
                    var pendingField = tfType.GetField("_pendingPostRecruitStatApply", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var applyMethod = tfType.GetMethod("ApplyCumulativeLevelGains", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                    if (pendingField != null && applyMethod != null)
                    {
                        var dict = pendingField.GetValue(null) as System.Collections.IDictionary;
                        if (dict != null && dict.Contains(recruit.Id))
                        {
                            int level = (int)dict[recruit.Id];
                            applyMethod.Invoke(null, new object[] { recruit, level });
                            dict.Remove(recruit.Id);
                            TFTVLogger.Always($"[Training] Post-AddRecruit stat gains applied to {recruit.DisplayName} (Level {level}).");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void TryLoadPurchasedVehicleJunkerWeapons(GeoCharacter junker)
            {
                try
                {
                    if (junker == null)
                    {
                        return;
                    }


                    TFTVLogger.Always($"[GeoPhoenixFaction.AddRecruit] [TryLoadPurchasedVehicleJunkerWeapons] " +
                        $"Looking through the junkers Equipment items; should be {junker.ArmourItems.Count()} items ");


                    foreach (GeoItem geoItem in junker.ArmourItems)
                    {
                        TFTVLogger.Always($"geoitem {geoItem.ItemDef.name}");

                        GroundVehicleModuleDef moduleDef = geoItem?.ItemDef as GroundVehicleModuleDef;
                        if (moduleDef == null)
                        {
                            continue;
                        }

                        // Only modules that actually have sub-weapons (Junker weapons case)
                        if (!moduleDef.GetSubWeapons().Any())
                        {
                            continue;
                        }

                        // Reload all ammo types the module supports to full capacity (creates magazines inside module.CommonItemData.Ammo)
                        foreach (WeaponDef w in moduleDef.GetSubWeapons())
                        {
                            TacticalItemDef ammoDef = w?.CompatibleAmmunition?.FirstOrDefault();
                            if (ammoDef == null)
                            {
                                continue;
                            }

                            TFTVLogger.Always($"[GeoPhoenixFaction.AddRecruit] [TryLoadPurchasedVehicleJunkerWeapons] Reloading ammo for weapon {w.name} using ammo {ammoDef.name}");
                            VehicleModuleAmmoHarmonyPatches.ReloadModuleAmmo(geoItem, ammoDef);

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        //Patch necesarry to remove Slug filter from UI to avoid duplicate tech filters
        //Transpiler magic from LucusTheDestroyer (all hail Lucus!)



        [HarmonyPatch(typeof(UIStateEditSoldier), "OnSelectSecondaryClass")] //VERIFIED
        public static class UIStateEditSoldier_OnSelectSecondaryClass_patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0), //this (__instance in normal patch terms)
            new CodeInstruction(OpCodes.Ldloc_1), //Storage index of the list of SpecializationDefs
            new CodeInstruction(OpCodes.Call, typeof(UIStateEditSoldier_OnSelectSecondaryClass_patch).GetMethod("RemoveTech"))

        };
                for (int i = 0; i < instructions.Count(); i++)
                {
                    if (listInstructions[i].opcode == OpCodes.Stloc_1 && listInstructions[i + 1].opcode == OpCodes.Ldloc_0 && listInstructions[i + 2].opcode == OpCodes.Newobj)
                    {
                        listInstructions.InsertRange(i + 1, insert);
                        return listInstructions;
                    }
                }
                return instructions;
            }
            public static void RemoveTech(UIStateEditSoldier state, List<SpecializationDef> list)
            {
                try
                {
                    FieldInfo fieldInfo = state.GetType().GetField("_currentCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                    GeoCharacter character = fieldInfo.GetValue(state) as GeoCharacter;
                    if (character.Progression.MainSpecDef != TFTVChangesToDLC5.TFTVMercenaries.SlugSpecialization)
                    {
                        return;
                    }
                    SpecializationDef techSpec = DefCache.GetDef<SpecializationDef>("TechnicianSpecializationDef");
                    if (list.Contains(techSpec))
                    {
                        list.Remove(techSpec);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIStateEditSoldier), "InitFilters")] //VERIFIED
        public static class UIStateEditSoldier_InitFilters_patch
        {
            public static bool Prefix(UIStateEditSoldier __instance, GeoCharacter ____initCharacter)
            {
                try
                {
                    GeoPhoenixFaction faction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                    UIModuleActorCycle actorCycleModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                    UIModuleSoldierEquip soldierEquipModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.SoldierEquipModule;

                    IReadOnlyList<SpecializationDef> availableCharacterSpecializations = faction.AvailableCharacterSpecializations;
                    List<SpecializationDef> list = new List<SpecializationDef>();
                    foreach (GeoCharacter geoCharacter in actorCycleModule.Characters)
                    {
                        if (geoCharacter.Progression != null)
                        {
                            foreach (SpecializationDef item in geoCharacter.Progression.GetSpecializations())
                            {
                                if (!list.Contains(item) && item != TFTVMercenaries.SlugSpecialization)
                                {
                                    list.Add(item);
                                }
                            }
                        }
                    }

                    soldierEquipModule.SetupClassFilters(availableCharacterSpecializations, list, ____initCharacter);

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
        /// Ensures that rescue vehicle missions will not contain faction vehicles if they haven't been researched by the faction yet.
        /// commented out 2/1 because 1) less variety in vehicles gained, and 2) introduced weird meta to wait until vehicles researched
        /// </summary>

        /*   [HarmonyPatch(typeof(GeoMissionGenerator), "GetRandomMission", new Type[] { typeof(IEnumerable<MissionTagDef>), typeof(ParticipantFilter), typeof(Func<TacMissionTypeDef, bool>) })]
           public static class GeoMissionGenerator_GetRandomMission_patch
           {

               public static void Prefix(IEnumerable<MissionTagDef> tags, out List<CustomMissionTypeDef> __state, GeoLevelController ____level)
               {
                   try
                   {
                       ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                       ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");

                       MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                       __state = new List<CustomMissionTypeDef>();


                       if (tags.Contains(requiresVehicle) && ____level != null)
                       {
                           TFTVLogger.Always($"Generating rescue Vehicle scav; checking if factions have researched Aspida/Armadillo");
                           GeoLevelController controller = ____level;

                           if (controller.NewJerichoFaction.Research != null && !controller.NewJerichoFaction.Research.HasCompleted("NJ_VehicleTech_ResearchDef"))
                           {
                               TFTVLogger.Always($"Armadillo not researched by New Jericho");

                               foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                               {
                                   if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == armadillo)
                                   {
                                       __state.Add(customMissionTypeDef);
                                   }
                               }
                           }
                           if (controller.SynedrionFaction.Research != null && !controller.SynedrionFaction.Research.HasCompleted("SYN_Rover_ResearchDef"))
                           {
                               TFTVLogger.Always($"Aspida not researched by Synedrion");

                               foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                               {
                                   if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == aspida)
                                   {
                                       __state.Add(customMissionTypeDef);
                                   }
                               }
                           }

                           if (__state.Count > 0)
                           {
                               TFTVLogger.Always($"Removing rescue vehicle missions with not researched vehicles from generation pool");

                               foreach (CustomMissionTypeDef mission in __state)
                               {
                                   mission.Tags.Remove(requiresVehicle);
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


               public static void Postfix(IEnumerable<MissionTagDef> tags, in List<CustomMissionTypeDef> __state)
               {
                   try
                   {
                       ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                       ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");


                       MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                       if (tags.Contains(DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef")) && __state.Count > 0)
                       {
                           TFTVLogger.Always($"Adding back missions that were removed from the pool");

                           foreach (CustomMissionTypeDef mission in __state)
                           {

                               if (!mission.Tags.Contains(requiresVehicle))
                               {
                                   mission.Tags.Add(requiresVehicle);
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
           }*/


        public static void ForceMarketPlaceUpdate()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                GeoMarketplace geoMarketplace = controller.Marketplace;
                MethodInfo updateOptionsWithRespectToTimeMethod = typeof(GeoMarketplace).GetMethod("UpdateOptionsWithRespectToTime", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo updateOptionsNextTimeField = typeof(GeoMarketplace).GetField("_updateOptionsNextTime", BindingFlags.NonPublic | BindingFlags.Instance);

                updateOptionsNextTimeField.SetValue(geoMarketplace, controller.Timing.Now);
                TFTVLogger.Always($"Forced Marketplace options update; changing next update time to now, {controller.Timing.Now.DateTime}");

                updateOptionsWithRespectToTimeMethod.Invoke(geoMarketplace, null);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(GeoMarketplace), "OnSiteVisited")] //VERIFIED
        public static class GeoMarketplace_OnSiteVisited_MarketPlace_patch
        {
            public static void Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings)
            {
                try
                {
                    if (____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) == 0)
                    {
                        TFTVLogger.Always($"Marketplace visited for the first time");

                        ____level.EventSystem.SetVariable(____settings.NumberOfDLC5MissionsCompletedVariable, 4);
                        ____level.EventSystem.SetVariable(____settings.DLC5IntroCompletedVariable, 1);
                        ____level.EventSystem.SetVariable(____settings.DLC5FinalMovieCompletedVariable, 1);
                        ForceMarketPlaceUpdate();

                        GeoscapeTutorialStepType stepType = GeoscapeTutorialStepType.AlienInfestHavenRaid;

                        GeoscapeTutorial geoscapeTutorial = ____level.Tutorial;

                        geoscapeTutorial.ShowTutorialStep(stepType);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}


