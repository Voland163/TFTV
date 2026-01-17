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

        public static GameTagDef AlwaysDeployTag;

        private static readonly Dictionary<string, List<EnemyLimit>> _undesirablesLimits = new Dictionary<string, List<EnemyLimit>>();
        public static Dictionary<string, int> UndesirablesSpawned = new Dictionary<string, int>();

        /// <summary>
        /// Difficulty level considers scaling, so Story Mode and Rookie == 1, Etermes == 5.
        /// DifficultyLevel in EnemyLimit means: this limit applies on difficulties <= DifficultyLevel.
        /// Use -1 to apply on all difficulties.
        /// </summary>
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

        private static void AddUndesirableLimit(string objectGuid, EnemyLimit limit)
        {
            if (!_undesirablesLimits.TryGetValue(objectGuid, out List<EnemyLimit> limits))
            {
                limits = new List<EnemyLimit>();
                _undesirablesLimits.Add(objectGuid, limits);
            }

            limits.Add(limit);
        }

        private static EnemyLimit GetApplicableLimit(string objectGuid, TacticalFaction tacticalFaction)
        {
            int currentDifficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(tacticalFaction.TacticalLevel.Difficulty.Order);

            if (!_undesirablesLimits.TryGetValue(objectGuid, out List<EnemyLimit> limits) || limits.Count == 0)
            {
                return null;
            }

            // Applies if -1 (all) OR currentDifficulty <= DifficultyLevel.
            // Choose the most specific matching limit: smallest DifficultyLevel that still matches,
            // with -1 treated as "least specific".
            EnemyLimit best = null;

            foreach (EnemyLimit candidate in limits)
            {
                bool applies = candidate.DifficultyLevel == -1 || currentDifficulty <= candidate.DifficultyLevel;
                if (!applies)
                {
                    continue;
                }

                if (best == null)
                {
                    best = candidate;
                    continue;
                }

                int bestKey = best.DifficultyLevel == -1 ? int.MaxValue : best.DifficultyLevel;
                int candidateKey = candidate.DifficultyLevel == -1 ? int.MaxValue : candidate.DifficultyLevel;

                if (candidateKey < bestKey)
                {
                    best = candidate;
                }
            }

            return best;
        }

        public static void PopulateLimitsForUndesirables()
        {
            try
            {
                AddUndesirableLimit("0b8be047-fa18-3ec4-3be6-7dc3462fb07d", new EnemyLimit(1, -1, -1)); //AN_Berserker_Shooter_Torso_BodyPartDef
                AddUndesirableLimit("9ac7ada0-bdec-a9b4-d982-1debc04d8fff", new EnemyLimit(3, -1, -1)); //Acheron_ClassTagDef
                AddUndesirableLimit("4eab7f81-c27d-eef4-6a80-300960fb5160", new EnemyLimit(3, -1, -1)); //Chiron_ClassTagDef

                // Raiders with grenades: global cap 2, but cap 1 on difficulties <= 2.
                AddUndesirableLimit("9ed5b03f-120b-d0c4-0ac4-4b30b5312af8", new EnemyLimit(2, 2, -1));
                AddUndesirableLimit("9ed5b03f-120b-d0c4-0ac4-4b30b5312af8", new EnemyLimit(1, 1, 2));

                // Snipers: global cap 2, but cap 1 on difficulties <= 2.
                AddUndesirableLimit("5ea5ff74-8494-4554-6a31-73bc06dc8fab", new EnemyLimit(2, 2, -1));
                AddUndesirableLimit("5ea5ff74-8494-4554-6a31-73bc06dc8fab", new EnemyLimit(1, 1, 2));

                // Heavies: global cap 2, but cap 1 on difficulties <= 2.
                AddUndesirableLimit("c8629efc-4f67-b664-eaf7-d338a5b5b3a3", new EnemyLimit(2, 2, -1));
                AddUndesirableLimit("c8629efc-4f67-b664-eaf7-d338a5b5b3a3", new EnemyLimit(1, 1, 2));
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

        private static int CountUndesirables(string objectGuid, TacticalFaction tacticalFaction, int turnNumber = 0, EnemyLimit limit = null)
        {
            try
            {
                int count = 0;

                if (UndesirablesSpawned.ContainsKey(objectGuid))
                {
                    count = UndesirablesSpawned[objectGuid];
                }

                if (limit != null && limit.SimultaneousMax != -1 && turnNumber > 0)
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

        private static int GetLimitForUndesirable(EnemyLimit limit, int turnNumber = 0)
        {
            try
            {
                if (limit == null)
                {
                    return int.MaxValue;
                }

                if (turnNumber == 0)
                {
                    return limit.InitialMax;
                }

                return limit.SimultaneousMax;
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
                if (tacticalFaction.IsControlledByPlayer || tacCharacterDef.Data.GameTags.Contains(AlwaysDeployTag))
                {
                    return false;
                }

                string undesirableObjectGuid = null;
                EnemyLimit applicableLimit = null;

                foreach (string objectGuid in _undesirablesLimits.Keys)
                {
                    var def = Repo.GetDef(objectGuid);

                    bool isMatch = false;
                    if (def is ItemDef itemDef)
                    {
                        isMatch = CheckCharacterDefForItemDef(tacCharacterDef, itemDef);
                    }
                    else if (def is GameTagDef gameTagDef)
                    {
                        isMatch = CheckCharacterDefForGameTagDef(tacCharacterDef, gameTagDef);
                    }

                    if (!isMatch)
                    {
                        continue;
                    }

                    EnemyLimit limit = GetApplicableLimit(objectGuid, tacticalFaction);
                    if (limit == null)
                    {
                        // No limit applies at this difficulty -> treat as not undesirable for limiting purposes.
                        // (Actor remains eligible.)
                        return false;
                    }

                    undesirableObjectGuid = objectGuid;
                    applicableLimit = limit;
                    break;
                }

                if (undesirableObjectGuid == null)
                {
                    return false;
                }

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

                int count = CountUndesirables(undesirableObjectGuid, tacticalFaction, turnNumber, applicableLimit);
                int limitValue = GetLimitForUndesirable(applicableLimit, turnNumber);

                if (count >= limitValue)
                {
                    TFTVLogger.Always($"{tacCharacterDef.name} reached limit of allowed spawns!");
                }

                return count >= limitValue;
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

        [HarmonyPatch(typeof(TacParticipantSpawn), "GetEligibleActorDeployments")] //VERIFIED
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
