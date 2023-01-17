using Base;
using Base.Defs;
using Base.Entities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static PhoenixPoint.Geoscape.Levels.GeoMissionGenerator;

namespace TFTV
{
    internal class TFTVExperimental
    {

        internal static Color purple = new Color32(149, 23, 151, 255);
        private static readonly DefRepository Repo = TFTVMain.Repo;
      //  public static PPFactionDef FactionAttackingPhoenixBase = new PPFactionDef();
      //  public static PhoenixBaseAttacker phoenixBaseAttacker;
      //  public static string PhoenixBaseUnderAttack = "";


        public static void MakeCopyOfAlienAttackOnPhoenixBase()
        {
            try
            {
                string name = "CloneOfPXBaseAlien_CustomMissionTypeDef";
                CustomMissionTypeDef baseDefenseVsAlien = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                CustomMissionTypeDef copyOfAlienAttack = Helper.CreateDefFromClone(
                    baseDefenseVsAlien,
                    "584FF8D3-61C2-4361-8589-BEEF439712D7",
                    name);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AdjustBaseAttack(PPFactionDef faction)
        {
            try
            {
                CustomMissionTypeDef baseDefenseVsAlien = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                CustomMissionTypeDef baseDefenseVsAlienSafeCopy = DefCache.GetDef<CustomMissionTypeDef>("CloneOfPXBaseAlien_CustomMissionTypeDef");

                PPFactionDef aliens = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                PPFactionDef anu = DefCache.GetDef<PPFactionDef>("Anu_FactionDef");
                PPFactionDef nj = DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef");
                PPFactionDef synedrion = DefCache.GetDef<PPFactionDef>("Synedrion_FactionDef");


                CustomMissionTypeDef baseDefenseVsAnu = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAnu_CustomMissionTypeDef");
                CustomMissionTypeDef baseDefenseVsNJ = DefCache.GetDef<CustomMissionTypeDef>("PXBaseNJ_CustomMissionTypeDef");
                CustomMissionTypeDef baseDefenseVsSyn = DefCache.GetDef<CustomMissionTypeDef>("PXBaseSY_CustomMissionTypeDef");

                List<MissionDeployParams> actorDeployDataSynedrion =
                    new List<MissionDeployParams>(baseDefenseVsSyn.ParticipantsData[0].ActorDeployParams);
                List<MissionDeployParams> actorDeployDataAnu =
                      new List<MissionDeployParams>(baseDefenseVsAnu.ParticipantsData[0].ActorDeployParams);
                List<MissionDeployParams> actorDeployDataNJ =
                      new List<MissionDeployParams>(baseDefenseVsNJ.ParticipantsData[0].ActorDeployParams);
                List<MissionDeployParams> actorDeployDataAliens =
                     new List<MissionDeployParams>(baseDefenseVsAlienSafeCopy.ParticipantsData[0].ActorDeployParams);

                baseDefenseVsAlien.ParticipantsData[0].FactionDef = faction;
                if (faction == aliens)
                {
                    baseDefenseVsAlien.ParticipantsData[0].ActorDeployParams = actorDeployDataAliens;
                    baseDefenseVsAlien.DifficultyThreatLevel = baseDefenseVsAlienSafeCopy.DifficultyThreatLevel;
                    baseDefenseVsAlien.ThreatLevelProvider = baseDefenseVsAlienSafeCopy.ThreatLevelProvider;
                    baseDefenseVsAlien.Outcomes[0].Outcomes = baseDefenseVsAlienSafeCopy.Outcomes[0].Outcomes;

                }
                else if (faction == anu)
                {
                    baseDefenseVsAlien.ParticipantsData[0].ActorDeployParams = actorDeployDataAnu;
                    baseDefenseVsAlien.DifficultyThreatLevel = DifficultyThreatLevel.Extreme;
                    baseDefenseVsAlien.ThreatLevelProvider.AddThreatLevelModifier = false;
                    baseDefenseVsAlien.Outcomes[0].Outcomes = new PhoenixPoint.Geoscape.Entities.Missions.Outcomes.MissionOutcomeDef[] { };

                }
                else if (faction == nj)
                {
                    baseDefenseVsAlien.ParticipantsData[0].ActorDeployParams = actorDeployDataNJ;
                    baseDefenseVsAlien.DifficultyThreatLevel = DifficultyThreatLevel.Extreme;
                    baseDefenseVsAlien.ThreatLevelProvider.AddThreatLevelModifier = false;
                    baseDefenseVsAlien.Outcomes[0].Outcomes = new PhoenixPoint.Geoscape.Entities.Missions.Outcomes.MissionOutcomeDef[] { };
                }
                else if (faction == synedrion)
                {
                    baseDefenseVsAlien.ParticipantsData[0].ActorDeployParams = actorDeployDataSynedrion;
                    baseDefenseVsAlien.DifficultyThreatLevel = DifficultyThreatLevel.Extreme;
                    baseDefenseVsAlien.ThreatLevelProvider.AddThreatLevelModifier = false;
                    baseDefenseVsAlien.Outcomes[0].Outcomes = new PhoenixPoint.Geoscape.Entities.Missions.Outcomes.MissionOutcomeDef[] { };

                }
   
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        [HarmonyPatch(typeof(GeoMissionGenerator), "GetRandomMission", new Type[] { typeof(MissionTypeTagDef), typeof(ParticipantFilter) })]

        public static class GetRandomMission_InitializeInstanceData_Experiment_patch
        {
            public static void Postfix(ref TacMissionTypeDef __result, ParticipantFilter enemy, MissionTypeTagDef type)
            {
                try
                {
                    MissionTypeTagDef baseDefence = DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");
                    if (type == baseDefence)
                    {
                        PPFactionDef aliens = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                        PPFactionDef anu = DefCache.GetDef<PPFactionDef>("Anu_FactionDef");
                        PPFactionDef nj = DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef");
                        PPFactionDef synedrion = DefCache.GetDef<PPFactionDef>("Synedrion_FactionDef");

                        CustomMissionTypeDef baseDefenseVsAlien = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");

                        AdjustBaseAttack(enemy.Faction);
                        __result = baseDefenseVsAlien;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        /* [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]

         public static class TacMission_InitDeployZones_Experiment_patch
         {
             public static void Prefix(UIModal modal)
             {
                 try
                 {
                     TFTVLogger.Always("ModalShowHandler invoked");
                     GeoMission geoMission = (GeoMission)modal.Data;
                     GeoSite geoSite = geoMission.Site;
                     geoMission.Cancel();
                //     geoSite.CreatePhoenixBaseDefenseMission(FactionAttackingPhoenixBase)



                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }


         }*/




        /*  [HarmonyPatch(typeof(TacMission), "InitDeployZones")]

          public static class TacMission_InitDeployZones_Experiment_patch
          {
              public static void Prefix(TacMission __instance)
              {
                  try
                  {
                      foreach (TacticalDeployZone item in __instance.TacticalLevel.Map.GetActors<TacticalDeployZone>().ToList()) 
                      {
                          TFTVLogger.Always("The deploy zone is " + item.name);

                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/


        /*  [HarmonyPatch(typeof(GeoSite), "CreatePhoenixBaseDefenseMission")]

          public static class LevelSceneBinding_InitializeInstanceData_Experiment_patch
          {
              public static void Postfix(PhoenixBaseAttacker attacker, GeoSite __instance)
              {
                  try
                  {
                      //phoenixBaseAttacker = attacker;
                      PhoenixBaseUnderAttack = __instance.LocalizedSiteName;

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/

        /*   public static void ClearAndCreateBaseDefenseMission(GeoLevelController controller)
           {
               try

               {
                   PPFactionDef nj = DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef");
                   CustomMissionTypeDef baseDefenseVsNJ = DefCache.GetDef<CustomMissionTypeDef>("PXBaseNJ_CustomMissionTypeDef");
                   List<MissionDeployParams> actorDeployDataNJ =
                  new List<MissionDeployParams>(baseDefenseVsNJ.ParticipantsData[0].ActorDeployParams);
                   TFTVLogger.Always("At least the method is inovked");
                   if (PhoenixBaseUnderAttack != null)
                   {
                       TFTVLogger.Always("Base under attack found");

                       foreach(GeoSite geoSite in controller.PhoenixFaction.Sites)

                       if (geoSite.LocalizedSiteName == PhoenixBaseUnderAttack && geoSite.HasActiveMission)
                       {
                           TFTVLogger.Always("Active mission found");

                               geoSite.DisableSite();
                             //  geoSite.ActiveMission = null;

                             //  geoSite.ActiveMission.MissionDef.ParticipantsData[0].FactionDef = nj;
                             //  geoSite.ActiveMission.MissionDef.ParticipantsData[0].ActorDeployParams = actorDeployDataNJ;


                           // PhoenixBaseUnderAttack.CreatePhoenixBaseDefenseMission(phoenixBaseAttacker);
                           TFTVLogger.Always("New defense mission should have been created, with attacker ");  //phoenixBaseAttacker.Faction.Name);   
                       }

                   }


               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
               }

           }*/

        /*
            [HarmonyPatch(typeof(TacParticipantSpawn), "GetMissionSpawnZones")]
            public static class TacParticipantSpawn_GetMissionSpawnZones_patch
            {
                public static void Postfix(TacParticipantSpawn __instance)
                {
                    try
                    {
                        CustomMissionTypeDef missionTypeDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");



                        List<TacticalDeployZone> list = __instance.TacticalFaction.DeployZones.Where((TacticalDeployZone z) => z.IsMissionZone).ToList();

                        foreach (TacticalDeployZone tacticalDeployZone in list)
                        {
                            TFTVLogger.Always("tacticalDeploy zone is " + tacticalDeployZone.name);

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


            }*/

        //Unlock Project Glory when player activates 3rd base. Commented out for release #13



        /*  [HarmonyPatch(typeof(GeoPhoenixFaction), "ActivatePhoenixBase")]
          public static class GeoPhoenixFaction_ActivatePhoenixBase_GiveGlory_Patch
          {
              public static void Postfix(GeoPhoenixFaction __instance)
              {
                  try
                  {
                      if (__instance.GeoLevel.EventSystem.GetVariable("Photographer") != 1 && __instance.Bases.Count() > 2)
                      {
                          GeoscapeEventContext eventContext = new GeoscapeEventContext(__instance.GeoLevel.ViewerFaction, __instance);
                          __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaLotaStart", eventContext);
                          __instance.GeoLevel.EventSystem.SetVariable("Photographer", 1);
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/


        public static TacticalActor mutoidReceivingHealing = null;

        [HarmonyPatch(typeof(HealAbility), "HealTargetCrt")]

        public static class HealAbility_HealTargetCrt_Mutoid_Patch
        {
            public static void Prefix(PlayingAction action)
            {
                try
                {
                    if ((TacticalActor)((TacticalAbilityTarget)action.Param).Actor != null)
                    {
                        TacticalActor actor = (TacticalActor)((TacticalAbilityTarget)action.Param).Actor;
                        if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef")))
                        {
                            mutoidReceivingHealing = actor;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "get_GeneralHealAmount")]

        public static class HealAbility_Mutoid_Patch
        {

            public static void Postfix(HealAbility __instance, ref float __result)
            {
                try
                {

                    if (mutoidReceivingHealing != null)
                    {
                        __result = 0;
                        mutoidReceivingHealing = null;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
        class DamageMultiplier_BugFix
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Div)
            };

                // insert after each of the first 3 divide opcodes
                int divs = 0;
                for (int index = 0; index < instructions.Count(); index++)
                {
                    if (listInstructions[index].opcode == OpCodes.Div)
                    {
                        listInstructions.InsertRange(index + 1, insert);
                        index += 2;
                        divs++;
                        if (divs == 3)
                        {
                            break;
                        }
                    }
                }

                if (divs != 3)
                {
                    return instructions; // didn't find three, function signature changed, abort
                }
                return listInstructions;
            }

        }

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
        public static class GeoMission_ModifyMissionData_AddVOObjectives_Patch
        {
            public static void Postfix(TacMissionData missionData, GeoMission __instance)
            {
                try
                {
                    TFTVLogger.Always("ModifyMissionData invoked");
                    GeoLevelController controller = __instance.Level;
                    List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                    foreach (int vo in voidOmens)
                    {
                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                        {
                            TFTVLogger.Always("VO " + vo + " found");
                            List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                            if (!listOfFactionObjectives.Contains(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo)))
                            {
                                listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                                missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /*public static void CheckObjectives()
        {
            try 
            {
                List<ObjectiveElement> objectiveElementsList = UnityEngine.Object.FindObjectsOfType<ObjectiveElement>().ToList();
                if (objectiveElementsList.Count > 0)
                {
                    TFTVLogger.Always("There are elements in the list");

                }
                foreach (ObjectiveElement objectiveElement in objectiveElementsList)
                {
                    TFTVLogger.Always("ObjectiveElement name " + objectiveElement.name);
                    TFTVLogger.Always("ObjectiveElement " + objectiveElement.StatusText.text);

                    if (objectiveElement.Description.text.Contains("VOID"))
                    {
                        TFTVLogger.Always("FactionObjective check passed");
                        objectiveElement.Description.color = purple;

                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }*/


        [HarmonyPatch(typeof(ObjectivesManager), "Add")]
        public static class FactionObjective_ModifyObjectiveColor_Patch
        {

            public static void Postfix(ObjectivesManager __instance, FactionObjective objective)
            {
                try
                {
                    //  TFTVLogger.Always("FactionObjective Invoked");
                    if (objective.Description.LocalizationKey.Contains("VOID"))
                    {
                        objective.Description = new LocalizedTextBind(objective.Description.Localize().ToUpper(), true);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(KeepSoldiersAliveFactionObjective), "EvaluateObjective")]
        public static class KeepSoldiersAliveFactionObjective_EvaluateObjective_Patch
        {

            public static void Postfix(KeepSoldiersAliveFactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    //TFTVLogger.Always("FactionObjective Evaluate");
                    if (__instance.Description.LocalizationKey.Contains("VOID"))
                    {
                        __result = FactionObjectiveState.InProgress;

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


