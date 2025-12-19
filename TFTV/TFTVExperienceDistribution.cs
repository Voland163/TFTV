using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV
{
    internal class TFTVExperienceDistribution
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        // Pending SP refunds for Project Osiris candidates (keyed by dead soldier id).
        internal static readonly Dictionary<int, int> PendingDeathSkillPointRefunds = new Dictionary<int, int>();

        internal static void PayPendingDeathRefunds(GeoPhoenixFaction phoenixFaction, string reason)
        {
            try
            {
                if (phoenixFaction == null || PendingDeathSkillPointRefunds.Count == 0)
                {
                    return;
                }

                int total = PendingDeathSkillPointRefunds.Values.Sum();
                if (total <= 0)
                {
                    PendingDeathSkillPointRefunds.Clear();
                    return;
                }

                phoenixFaction.Skillpoints += total;
                TFTVLogger.Always($"Paid {total} deferred shared skill points ({PendingDeathSkillPointRefunds.Count} deaths). Reason: {reason}");

                PendingDeathSkillPointRefunds.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CancelPendingRefund(GeoTacUnitId deadSoldierId, string reason)
        {
            try
            {
                if (PendingDeathSkillPointRefunds.Remove(deadSoldierId))
                {
                    TFTVLogger.Always($"Cancelled deferred shared skill points refund for {deadSoldierId}. Reason: {reason}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(PhoenixStatisticsManager), "ActorKilledInGeoscape")] //VERIFIED
        internal static class PhoenixStatisticsManager_ActorKilledInGeoscape_Patch
        {
            private static readonly HashSet<CharacterDeathReason> MissionDeathReasons = new HashSet<CharacterDeathReason>
            {
                CharacterDeathReason.DiedOnMission,
                CharacterDeathReason.MindControlledOnMission
            };

            static void Postfix(PhoenixStatisticsManager __instance, GeoFaction faction, GeoCharacter charater, IGeoCharacterContainer container, CharacterDeathReason reason)
            {
                if (__instance?.CurrentGameStats == null)
                {
                    return;
                }

                if (charater == null || charater.TemplateDef == null || !charater.TemplateDef.IsHuman)
                {
                    return;
                }

                if (!MissionDeathReasons.Contains(reason))
                {
                    return;
                }

                GeoPhoenixFaction phoenixFaction = faction as GeoPhoenixFaction;
                if (phoenixFaction == null)
                {
                    return;
                }

                SoldierStats soldierStats = __instance.CurrentGameStats.GetSoldierStat(charater.Id, true) ?? __instance.CurrentGameStats.GetSoldierStat(charater.Id, false);
                if (soldierStats == null)
                {
                    return;
                }

                GeoLevelController geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                GameDifficultyLevelDef difficulty = geoLevel?.CurrentDifficultyLevel;
                if (difficulty == null)
                {
                    return;
                }

                int missionsParticipated = Math.Max(0, soldierStats.MissionsParticipated);
                int skillPointsPerMission = Math.Max(0, difficulty.SoldierSkillPointsPerMission);
                if (missionsParticipated == 0 || skillPointsPerMission == 0)
                {
                    return;
                }

                int refund = missionsParticipated * skillPointsPerMission / 2;
                if (refund <= 0)
                {
                    return;
                }

            
                // Defer refund if this death is an Osiris-eligible candidate.
                // (Candidate enrollment happens in TFTVRevenantResearch.RecordStatsOfDeadSoldier, keyed by GeoUnitId.)
                if (TFTVRevenant.TFTVRevenantResearch.ProjectOsirisStats != null
                    && TFTVRevenant.TFTVRevenantResearch.ProjectOsirisStats.ContainsKey(charater.Id))
                {
                    PendingDeathSkillPointRefunds[charater.Id] = refund;
                    TFTVLogger.Always($"Deferred {refund} shared skill points refund for {charater.DisplayName} (Project Osiris candidate).");
                    return;
                }

                phoenixFaction.Skillpoints += refund;
                TFTVLogger.Always($"Refunded {refund} shared skill points after the death of {charater.DisplayName} ({missionsParticipated} missions).");
            }
        }

        [HarmonyPatch(typeof(TacticalContribution), "AddContribution")] //VERIFIED
        public static class TFTV_TacticalContribution_AddContribution
        {
            public static void Postfix(TacticalContribution __instance, int cp, TacticalActorBase ____actor)
            {
                try
                {

                    if (cp <= 0)
                    {
                        return;
                    }

                    if (!____actor.Status.HasStatus<MindControlStatus>() || ____actor.Status.GetStatus<MindControlStatus>().ControllerActor == null)
                    {
                        return;
                    }

                    TacticalActor controllingActor = ____actor.Status.GetStatus<MindControlStatus>().ControllerActor;

                    // TFTVLogger.Always($"{controllingActor.name} has {controllingActor.Contribution.Contribution} CP");

                    FieldInfo contributionFieldInfo = typeof(TacticalContribution).GetField("_contribution", BindingFlags.NonPublic | BindingFlags.Instance);

                    TacticalContribution controllingActorContribution = controllingActor.Contribution;

                    int controllingActorContributionValue = controllingActorContribution.Contribution + cp / 2;

                    contributionFieldInfo.SetValue(controllingActorContribution, controllingActorContributionValue);

                    // TFTVLogger.Always($"{controllingActor.name} now has {controllingActor.Contribution.Contribution} CP");

                    Debug.Log($"+{cp} cp for {controllingActor.name} (through Mind Controlled Unit).");


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
