using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
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

        private const string MistAcheronsKey = "group:MistAcherons";
        private const string WormChironsKey = "group:WormChirons";

        /// <summary>
        /// Caps keyed by a group name instead of a def GUID: a template belongs to the group when it carries any
        /// of the listed items (body parts or equipment). Items are looked up by def name on first use.
        /// </summary>
        private static readonly Dictionary<string, string[]> _groupItemNames = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, HashSet<ItemDef>> _groupItems = new Dictionary<string, HashSet<ItemDef>>();

        private static void AddGroupLimit(string groupKey, string[] itemDefNames, EnemyLimit limit)
        {
            _groupItemNames[groupKey] = itemDefNames;
            AddUndesirableLimit(groupKey, limit);
        }

        private static HashSet<ItemDef> GetGroupItems(string groupKey)
        {
            if (_groupItems.TryGetValue(groupKey, out HashSet<ItemDef> items))
            {
                return items;
            }

            items = new HashSet<ItemDef>();
            foreach (string name in _groupItemNames[groupKey])
            {
                ItemDef itemDef = DefCache.GetDef<ItemDef>(name);
                if (itemDef != null)
                {
                    items.Add(itemDef);
                }
                else
                {
                    TFTVLogger.Always($"[DeploymentCaps] {groupKey}: item {name} not found");
                }
            }

            _groupItems[groupKey] = items;
            return items;
        }

        private static string DescribeCap(string objectGuid)
        {
            if (_groupItemNames.ContainsKey(objectGuid))
            {
                return objectGuid;
            }

            return Repo.GetDef(objectGuid)?.name ?? objectGuid;
        }

        private static bool CheckCharacterDefForGroup(TacCharacterDef tacCharacterDef, string groupKey)
        {
            HashSet<ItemDef> items = GetGroupItems(groupKey);
            return tacCharacterDef.Data.EquipmentItems.Any(i => items.Contains(i)) || tacCharacterDef.Data.BodypartItems.Any(i => items.Contains(i));
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
                // Big Pandorans, per mission (reinforcements included): at most 2 Acherons, only 1 of them
                // a Mist thrower (Acheron, Acheron Prime); at most 3 Chirons, only 2 of them worm throwers.
                AddUndesirableLimit("9ac7ada0-bdec-a9b4-d982-1debc04d8fff", new EnemyLimit(2, -1, -1)); //Acheron_ClassTagDef
                AddUndesirableLimit("4eab7f81-c27d-eef4-6a80-300960fb5160", new EnemyLimit(3, -1, -1)); //Chiron_ClassTagDef
                AddGroupLimit(MistAcheronsKey, new[] { "Acheron_Arms_WeaponDef", "AcheronPrime_Arms_WeaponDef" }, new EnemyLimit(1, -1, -1));
                AddGroupLimit(WormChironsKey, new[] { "Chiron_Abdomen_FireWorm_Launcher_WeaponDef", "Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef", "Chiron_Abdomen_AcidWorm_Launcher_WeaponDef" }, new EnemyLimit(2, -1, -1));

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
                return gameTagDef is ClassTagDef classTagDef && tacCharacterDef.ClassTags.Contains(classTagDef);
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

                // A template can fall under several caps (a worm Chiron counts as a Chiron and as a worm
                // thrower): every matching cap is counted, and the template is blocked if any one is full.
                bool blocked = false;

                foreach (string objectGuid in _undesirablesLimits.Keys)
                {
                    bool isMatch = false;

                    if (_groupItemNames.ContainsKey(objectGuid))
                    {
                        isMatch = CheckCharacterDefForGroup(tacCharacterDef, objectGuid);
                    }
                    else
                    {
                        var def = Repo.GetDef(objectGuid);

                        if (def is ItemDef itemDef)
                        {
                            isMatch = CheckCharacterDefForItemDef(tacCharacterDef, itemDef);
                        }
                        else if (def is GameTagDef gameTagDef)
                        {
                            isMatch = CheckCharacterDefForGameTagDef(tacCharacterDef, gameTagDef);
                        }
                    }

                    if (!isMatch)
                    {
                        continue;
                    }

                    // No limit at this difficulty: this cap doesn't restrict the template.
                    EnemyLimit applicableLimit = GetApplicableLimit(objectGuid, tacticalFaction);
                    if (applicableLimit == null)
                    {
                        continue;
                    }

                    if (spawned)
                    {
                        UndesirablesSpawned.TryGetValue(objectGuid, out int spawnedSoFar);
                        UndesirablesSpawned[objectGuid] = spawnedSoFar + 1;
                    }

                    int count = CountUndesirables(objectGuid, tacticalFaction, turnNumber, applicableLimit);
                    int limitValue = GetLimitForUndesirable(applicableLimit, turnNumber);

                    if (limitValue >= 0 && count >= limitValue)
                    {
                        TFTVLogger.Always($"{tacCharacterDef.name} reached limit of allowed spawns ({DescribeCap(objectGuid)}: {count} of {limitValue})");
                        blocked = true;
                    }
                }

                return blocked;
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

        private static MissionTagDef _havenDefenseTag;

        /// <summary>The haven's own side (defenders and civilians) in a haven defense mission.</summary>
        private static bool IsHavenDefenseResidents(TacParticipantSpawn spawn)
        {
            if (spawn == null || spawn.ParticipantKind != TacMissionParticipant.Residents)
            {
                return false;
            }

            if (_havenDefenseTag == null)
            {
                _havenDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");
            }

            var missionTags = spawn.TacMission?.MissionData?.MissionType?.MissionTags;
            return _havenDefenseTag != null && missionTags != null && missionTags.Contains(_havenDefenseTag);
        }

        /// <summary>
        /// Haven defenders on Phoenix's side. The undesirables limits are there to keep enemy squads
        /// fair, so they don't apply to them. Under Void Omen #5 haven defenders are hostile, and the
        /// limits apply as to any enemy.
        /// </summary>
        private static bool IsFriendlyHavenDefenders(TacParticipantSpawn spawn)
        {
            return IsHavenDefenseResidents(spawn) && !TFTVVoidOmens.VoidOmensCheck[5];
        }

        [HarmonyPatch(typeof(TacParticipantSpawn), "GetEligibleActorDeployments")] //VERIFIED
        public static class TFTV_TacParticipantSpawn_GetEligibleActorDeployments
        {
            public static IEnumerable<ActorDeployData> Postfix(IEnumerable<ActorDeployData> results, TacParticipantSpawn __instance)
            {
                bool havenDefense = IsHavenDefenseResidents(__instance);
                bool ignoreLimits = havenDefense && IsFriendlyHavenDefenders(__instance);

                foreach (ActorDeployData actorDeployData in results)
                {
                    // Haven defenders are people: no vehicles (Aspida, Armadillo...) and no Mutogs.
                    if (havenDefense && actorDeployData.InstanceDef is TacCharacterDef havenUnit && (havenUnit.IsVehicle || havenUnit.IsMutog))
                    {
                        continue;
                    }

                    if (ignoreLimits)
                    {
                        yield return actorDeployData;
                        continue;
                    }

                    if (actorDeployData.InstanceData != null)
                    {
                        TacticalFaction tacticalFaction = __instance.TacticalFaction;

                        // NEW: never block unique deployments (story/mission-specific uniques)
                        if (actorDeployData.Unique)
                        {
                            yield return actorDeployData;
                            continue;
                        }

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

                        // Friendly haven defenders neither use nor count toward the enemy's undesirables limits.
                        if (!IsFriendlyHavenDefenders(__instance))
                        {
                            UnDesirableActorCheck(tacCharacterDef, __instance.TacticalFaction, turnNumber, true);
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
    }
}
