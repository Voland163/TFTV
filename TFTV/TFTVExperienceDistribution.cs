using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
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

        [HarmonyPatch(typeof(PhoenixStatisticsManager), "ActorKilledInGeoscape")]
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

                phoenixFaction.Skillpoints += refund;

               TFTVLogger.Always($"Refunded {refund} shared skill points after the death of {charater.DisplayName} ({missionsParticipated} missions).");
            }
        }

        [HarmonyPatch(typeof(TacticalContribution), "AddContribution")]
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
