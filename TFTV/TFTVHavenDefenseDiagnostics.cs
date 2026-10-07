using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    /// <summary>
    /// Logs, at the start of a mission, every template the Residents participant (a haven's defenders and
    /// civilians) can draw from, with its cost, and how many points it spent.
    ///
    /// Kept while haven defense balance is being looked at. What it established: every vanilla HavenDef
    /// mission def gives the defenders a fixed 360 points (DeploymentRuleType.PresetDeployment), and the
    /// haven's geoscape defense strength (GeoHavenDefenseMission.DefenderDeployment) never reaches tactical.
    /// 360 buys one Synedrion soldier (170-340 each); civilians cost nothing.
    /// </summary>
    internal static class TFTVHavenDefenseDiagnostics
    {
        private const string Tag = "[HavenDefenseDiag]";

        [HarmonyPatch(typeof(TacParticipantSpawn), nameof(TacParticipantSpawn.DeployForTurn))]
        internal static class TacParticipantSpawn_DeployForTurn_Patch
        {
            private static readonly AccessTools.FieldRef<TacParticipantSpawn, List<ActorDeployLimit>> DeployLimitsRef =
                AccessTools.FieldRefAccess<TacParticipantSpawn, List<ActorDeployLimit>>("_actorDeployLimit");

            private static void Prefix(TacParticipantSpawn __instance, int turnNumber, out float __state)
            {
                __state = __instance?.DeploymentPointsUsed ?? 0f;

                try
                {
                    if (turnNumber == 0 && __instance?.MissionFactionData != null && __instance.ParticipantKind == TacMissionParticipant.Residents)
                    {
                        LogCandidates(__instance);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Every template this participant can draw from, with what it costs, and the class limits that cap
            /// how many of each it may field.
            /// </summary>
            private static void LogCandidates(TacParticipantSpawn spawn)
            {
                string missionType = spawn.TacMission?.MissionData?.MissionType?.name ?? "?";
                List<ActorDeployData> candidates = spawn.ActorDeployData?.Where(c => c != null).ToList() ?? new List<ActorDeployData>();

                TFTVLogger.Always($"{Tag} DEFENDER CANDIDATES {missionType}: {spawn.MissionFactionData.FactionDef?.name} has {spawn.MissionFactionData.InitialDeploymentPoints} points, " +
                    $"points used so far {spawn.DeploymentPointsUsed}, {candidates.Count} candidate templates (cheapest first):");

                foreach (ActorDeployData candidate in candidates.OrderBy(c => c.DeployCost))
                {
                    GameTagDef[] tags = candidate.ActorInstance?.GameTags ?? (candidate.InstanceDef as TacCharacterDef)?.Data?.GameTags;
                    string classTags = tags == null ? "?" : string.Join("/", tags.OfType<ClassTagDef>().Select(t => t.name));
                    bool civilian = tags != null && tags.Contains(TFTVMain.Shared.SharedGameTags.CivilianTag);

                    TFTVLogger.Always($"{Tag}   {candidate.GetName()}: cost {candidate.DeployCost}, weight {candidate.ChanceWeight}, " +
                        $"initial {candidate.CanBeInitialDeployment}, reinforcement {candidate.CanBeReinforcement}, unique {candidate.Unique}, " +
                        $"priority {candidate.DeployWithPriority}, classes {classTags}{(civilian ? " [CIVILIAN]" : string.Empty)}");
                }

                List<ActorDeployLimit> limits = DeployLimitsRef(spawn);

                if (limits == null || limits.Count == 0)
                {
                    TFTVLogger.Always($"{Tag}   class limits: none");
                    return;
                }

                foreach (ActorDeployLimit limit in limits.Where(l => l != null))
                {
                    TFTVLogger.Always($"{Tag}   class limit {limit.ActorTag?.name}: {limit.ActorLimit.Min}-{limit.ActorLimit.Max}, spawned {limit.SpawnedCount}");
                }
            }

            private static void Postfix(TacParticipantSpawn __instance, int turnNumber, float __state)
            {
                try
                {
                    if (__instance?.MissionFactionData == null || __instance.ParticipantKind != TacMissionParticipant.Residents)
                    {
                        return;
                    }

                    TFTVLogger.Always($"{Tag} DEFENDERS SPAWNED turn {turnNumber}: {__instance.MissionFactionData.FactionDef?.name} " +
                        $"initial points {__instance.MissionFactionData.InitialDeploymentPoints}, points used {__state} -> {__instance.DeploymentPointsUsed}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
