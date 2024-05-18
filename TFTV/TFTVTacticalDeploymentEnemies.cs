using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVTacticalDeploymentEnemies
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static Dictionary<string, EnemyLimit> _undesirablesLimits = new Dictionary<string, EnemyLimit>();
        public static Dictionary<string, int> UndesirablesSpawned = new Dictionary<string, int>();

        public class EnemyLimit
        {
            public int InitialMax { get; set; }
            public int SimultaneousMax { get; set; }
            public int DifficultyLevel { get; set; }

            public EnemyLimit(int initialMax, int simultaneousMax, int difficultyLevel)
            {
                InitialMax = initialMax;
                SimultaneousMax = simultaneousMax;
                DifficultyLevel = difficultyLevel;
            }
        }

        public static void PopulateLimitsForUndesirables()
        {
            try
            {
                _undesirablesLimits.Add("0b8be047-fa18-3ec4-3be6-7dc3462fb07d", new EnemyLimit(1, -1, -1)); //AN_Berserker_Shooter_Torso_BodyPartDef
                _undesirablesLimits.Add("9ac7ada0-bdec-a9b4-d982-1debc04d8fff", new EnemyLimit(3, -1, -1)); //Acheron_ClassTagDef
                _undesirablesLimits.Add("4eab7f81-c27d-eef4-6a80-300960fb5160", new EnemyLimit(3, -1, -1)); //Chiron_ClassTagDef
                _undesirablesLimits.Add("9ed5b03f-120b-d0c4-0ac4-4b30b5312af8", new EnemyLimit(2, 2, 3)); //NEU_Heavy_Torso_BodyPartDef raiders with grenades
                _undesirablesLimits.Add("5ea5ff74-8494-4554-6a31-73bc06dc8fab", new EnemyLimit(2, 2, 3)); //Sniper_ClassTagDef
                _undesirablesLimits.Add("c8629efc-4f67-b664-eaf7-d338a5b5b3a3", new EnemyLimit(2, 2, 3)); //Heavy_ClassTagDef
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckCharacterDefForItemDef(TacCharacterDef tacCharacterDef, ItemDef itemDef)
        {
            try
            {
                return tacCharacterDef.Data.EquipmentItems.Contains(itemDef) || tacCharacterDef.Data.BodypartItems.Contains(itemDef);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckCharacterDefForGameTagDef(TacCharacterDef tacCharacterDef, GameTagDef gameTagDef)
        {
            try
            {
                return tacCharacterDef.ClassTag == gameTagDef;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int CountUndesirables(string objectGuid, TacticalFaction tacticalFaction, int turnNumber = 0)
        {
            try
            {
                int count = 0;

                if (UndesirablesSpawned.ContainsKey(objectGuid))
                {
                    count = UndesirablesSpawned[objectGuid];
                }

                if (_undesirablesLimits[objectGuid].SimultaneousMax != -1 && turnNumber > 0)
                {
                    var def = Repo.GetDef(objectGuid);

                    if (def is ItemDef itemDef)
                    {
                        count = CountUndesirableActorsWithItemDefAlive(itemDef, tacticalFaction);
                    }
                    else if (def is GameTagDef gameTagDef)
                    {
                        count = CountUndesirableActorsWithClassTagAlive(gameTagDef, tacticalFaction);
                    }
                }

                return count;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int GetLimitForUndesirable(string objectGUID, TacticalLevelController controller, int turnNumber = 0)
        {
            try
            {
                if (turnNumber == 0)
                {
                    return _undesirablesLimits[objectGUID].InitialMax;
                }
                else
                {
                    return _undesirablesLimits[objectGUID].SimultaneousMax;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static bool UnDesirableActorCheck(TacCharacterDef tacCharacterDef, TacticalFaction tacticalFaction, int turnNumber = 0, bool spawned = false)
        {
            try
            {
                if (tacticalFaction.IsControlledByPlayer)
                {
                    return false;
                }

                string undesirableObjectGuid = "";
                bool undesirableActor = false;

                foreach (string objectGuid in _undesirablesLimits.Keys.Where(k => _undesirablesLimits[k].DifficultyLevel<=tacticalFaction.TacticalLevel.Difficulty.Order))
                {
                    var def = Repo.GetDef(objectGuid);

                    if (def is ItemDef itemDef)
                    {
                        undesirableActor = CheckCharacterDefForItemDef(tacCharacterDef, itemDef);
                        // TFTVLogger.Always($"{itemDef.name}");
                    }
                    else if (def is GameTagDef gameTagDef)
                    {
                        undesirableActor = CheckCharacterDefForGameTagDef(tacCharacterDef, gameTagDef);
                        // TFTVLogger.Always($"{gameTagDef.name}");
                    }

                    if (undesirableActor)
                    {
                        undesirableObjectGuid = objectGuid;
                        break;
                    }
                }

                if (undesirableActor)
                {
                    if (spawned)
                    {
                        if (UndesirablesSpawned.ContainsKey(undesirableObjectGuid))
                        {
                            UndesirablesSpawned[undesirableObjectGuid] += 1;
                        }
                        else
                        {
                            UndesirablesSpawned.Add(undesirableObjectGuid, 1);
                        }
                    }

                    int count = CountUndesirables(undesirableObjectGuid, tacticalFaction, turnNumber);

                    int limit = GetLimitForUndesirable(undesirableObjectGuid, tacticalFaction.TacticalLevel, turnNumber);

                    if (count >= limit)
                    {
                        TFTVLogger.Always($"{tacCharacterDef.name} reached limit of allowed spawns!");
                    }

                    return count >= limit;
                }

                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int CountUndesirableActorsWithItemDefAlive(ItemDef itemDef, TacticalFaction tacticalFaction)
        {
            try
            {
                int count = 0;

                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.IsAlive && !ta.IsEvacuated))
                {
                    if (tacticalActor.Equipments.Equipments.Any(e => e.ItemDef == itemDef))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int CountUndesirableActorsWithClassTagAlive(GameTagDef classTagDef, TacticalFaction tacticalFaction)
        {
            try
            {
                int count = 0;

                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.IsAlive && !ta.IsEvacuated))
                {
                    if (tacticalActor.GameTags.Contains(classTagDef))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
        /*  private static bool DesirableActorCheck(TacActorDef tacActorDef, TacticalFaction tacticalFaction)
          {
              try
              {


                  if (tacActorDef != null && Repo.GetDef(tacActorDef.Guid) is TacCharacterDef tacCharacterDef)
                  {
                      TFTVLogger.Always($"{tacCharacterDef?.name}");

                      foreach (string itemDefGuid in _undesirablesLimits.Keys)
                      {
                          ItemDef itemDef = (ItemDef)Repo.GetDef(itemDefGuid);
                          TFTVLogger.Always($"{itemDef}");
                          if (tacCharacterDef.Data.EquipmentItems.Contains(itemDef) || tacCharacterDef.Data.BodypartItems.Contains(itemDef))
                          {
                              int limit = _undesirablesLimits[itemDef.Guid];

                              int count = 0;

                              if (tacticalFaction.TacticalLevel.CurrentFaction == tacticalFaction)
                              {
                                  TFTVLogger.Always($"current faction: {tacticalFaction.TacticalLevel.CurrentFaction} so must be reinforcements");
                                  count = CountUndesirableActorsAlive(itemDef, tacticalFaction);
                              }
                              else
                              {
                                  if (_undesirablesSpawned.ContainsKey(itemDef.Guid))
                                  {
                                      count = _undesirablesSpawned[itemDef.Guid];
                                  }
                              }

                              if (count >= limit)
                              {
                                  TFTVLogger.Always($"{tacCharacterDef?.name} removed from list of possible deployments");
                                  return false;
                              }

                              if (_undesirablesSpawned.ContainsKey(itemDef.Guid))
                              {
                                  _undesirablesSpawned[itemDef.Guid] += 1;
                              }
                              else
                              {
                                  _undesirablesSpawned.Add(itemDef.Guid, 1);
                              }
                          }
                      }
                  }

                  return true;

              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
                  throw;
              }
          }*/


        [HarmonyPatch(typeof(TacParticipantSpawn), "GetEligibleActorDeployments")]
        public static class TFTV_TacParticipantSpawn_GetEligibleActorDeployments
        {
            public static IEnumerable<ActorDeployData> Postfix(IEnumerable<ActorDeployData> results, TacParticipantSpawn __instance)
            {

                foreach (ActorDeployData actorDeployData in results)
                {
                    if (actorDeployData.InstanceData != null)
                    {

                        TacticalFaction tacticalFaction = __instance.TacticalFaction;

                        if (actorDeployData.InstanceDef is TacCharacterDef tacCharacterDef)
                        {
                            if (!UnDesirableActorCheck(tacCharacterDef, tacticalFaction))
                            {
                                yield return actorDeployData;
                            }
                            else
                            {
                              //  TFTVLogger.Always($"{actorDeployData.InstanceDef.name} excluded from eligible actors for deployment!");
                            }
                        }
                        else
                        {
                            yield return actorDeployData;

                        }
                    }
                    else
                    {
                        yield return actorDeployData;

                    }
                }

            }
        }

        [HarmonyPatch(typeof(TacParticipantSpawn), "DoSpawnActor")]
        public static class TFTV_TacParticipantSpawn_DoSpawnActor
        {
            public static void Postfix(TacParticipantSpawn __instance, int turnNumber, ActorDeployData deploymentData, TacticalActorBase __result)
            {
                try
                {
                    if (deploymentData.InstanceDef != null && deploymentData.InstanceDef is TacCharacterDef tacCharacterDef)
                    {
                        TFTVLogger.Always($"DoSpawnActor: {deploymentData?.InstanceDef?.name}, Turn number:{turnNumber} Faction: {__instance?.TacticalFaction}");

                        UnDesirableActorCheck(tacCharacterDef, __instance.TacticalFaction, turnNumber, true);
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
