using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// The stock alert state (TacAIActor.IsAlerted) and TFTV's AI. An AI unit with alert behaviour
        /// (TacAIActorDef.HasAlertBehavior) that is not alerted has its enemy mask cut down to
        /// AISettingsDef.NonAlertingEnemiesMask (civilians, key structures, crates): soldiers, aliens and bandits
        /// are in none of its target lists even when its side sees them. TFTV code that reads the side's vision
        /// directly asks here whether the unit may go for an enemy, so it doesn't plan for attacks the unit
        /// can't make (audit 2026-11-19).
        ///
        /// Also: haven defenders and rescuees start every mission alerted (user, 2026-11-19). Vanilla alerts
        /// every AI side at deployment on haven defences (TacMissionTypeDef.IsAiAlertedInitially), not on
        /// rescue missions; this makes sure of both at tactical start and again before each of their decisions.
        /// </summary>
        internal static class AlertState
        {
            private static GameTagDef _havenDefenseTag;

            /// <summary>The unit goes for soldiers, aliens and bandits: alerted, no alert behaviour, or not run by the AI.</summary>
            internal static bool FightsCombatants(TacticalActor actor)
            {
                if (actor == null || actor.IsControlledByPlayer || actor.AIActor == null)
                {
                    return true;
                }

                return actor.AIActor.GetEnemyMask(ActorType.Combatant) != 0;
            }

            /// <summary>Whether the AI would count <paramref name="enemy"/> as a target of this unit at all.</summary>
            internal static bool CanTarget(TacticalActor actor, TacticalActorBase enemy)
            {
                if (enemy == null || FightsCombatants(actor))
                {
                    return true;
                }

                return actor.TacticalFaction?.AIBlackboard != null
                    && actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.All), false).Contains(enemy);
            }

            /// <summary>Enemies the unit's side has revealed, whether or not the unit itself is alerted.</summary>
            internal static IEnumerable<TacticalActorBase> SeenBySide(TacticalActor actor)
            {
                if (actor?.TacticalFaction?.Vision == null)
                {
                    return Enumerable.Empty<TacticalActorBase>();
                }

                return actor.TacticalFaction.Vision.GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                    .Where(e => e != null && e.IsAlive);
            }

            /// <summary>Haven defenders (the residents of a haven defence) and the faction Phoenix came to rescue.</summary>
            internal static bool StartsAlerted(TacticalFaction faction, out string why)
            {
                why = IsHavenDefenders(faction) ? "haven defender" : IsAwaitingRescue(faction) ? "awaiting rescue" : null;
                return why != null;
            }

            /// <summary>The residents of a haven defence, run by the AI.</summary>
            internal static bool IsHavenDefenders(TacticalFaction faction)
            {
                return faction != null && !faction.IsControlledByPlayer && faction.ParticipantKind == TacMissionParticipant.Residents
                    && IsHavenDefense(faction.TacticalLevel);
            }

            /// <summary>A faction some player faction has come to rescue (RescueSoldiersFactionObjective).</summary>
            internal static bool IsAwaitingRescue(TacticalFaction faction)
            {
                return faction != null && !faction.IsControlledByPlayer && faction.TacticalLevel?.Factions != null
                    && faction.TacticalLevel.Factions.Any(f => f != faction && f.IsControlledByPlayer && f.Objectives != null
                        && f.Objectives.OfType<RescueSoldiersFactionObjective>().Any(o => o.RescuedFaction == faction.Faction?.FactionDef));
            }

            private static bool IsHavenDefense(TacticalLevelController level)
            {
                if (_havenDefenseTag == null)
                {
                    _havenDefenseTag = TFTVMain.Main.DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");
                }

                TacMissionTypeDef missionType = level?.TacMission?.MissionData?.MissionType;

                return missionType != null && _havenDefenseTag != null
                    && (missionType.MissionTypeTag == _havenDefenseTag || (missionType.MissionTags != null && missionType.MissionTags.Contains(_havenDefenseTag)));
            }

            private static bool TryAlert(TacticalActor actor)
            {
                if (actor == null || !actor.IsAlive || actor.AIActor == null || actor.AIActor.IsAlerted
                    || (actor.Status != null && actor.Status.HasStatus<MindControlStatus>()))
                {
                    return false;
                }

                actor.AIActor.IsAlerted = true;
                return actor.AIActor.IsAlerted;
            }

            /// <summary>Called from TFTVTactical.OnTacticalStart, after every side has deployed.</summary>
            internal static void AlertOnTacticalStart(TacticalLevelController controller)
            {
                try
                {
                    if (controller?.Factions == null)
                    {
                        return;
                    }

                    foreach (TacticalFaction faction in controller.Factions)
                    {
                        if (!StartsAlerted(faction, out string why))
                        {
                            continue;
                        }

                        int alerted = faction.TacticalActors.Count(TryAlert);
                        TFTVLogger.Always($"TFTV AI alert: {faction.TacticalFactionDef?.name} ({why}) starts alerted: {alerted} of {faction.TacticalActors.Count(a => a.IsAlive)} units alerted now");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Backstop for units that enter play later or come back from mind control: before one of them is
            /// evaluated, it is alerted.
            /// </summary>
            [HarmonyPatch(typeof(AIBlackboard), nameof(AIBlackboard.BeforeAIActorEvaluation))]
            internal static class AIBlackboard_BeforeAIActorEvaluation_StartsAlerted_Patch
            {
                private static void Postfix(TacAIActor aiActor)
                {
                    try
                    {
                        TacticalActor actor = aiActor?.TacticalActor;

                        if (actor == null || aiActor.IsAlerted || !StartsAlerted(actor.TacticalFaction, out string why) || !TryAlert(actor))
                        {
                            return;
                        }

                        TFTVLogger.Always($"TFTV AI alert: {actor.DisplayName} ({why}) alerted before its decision");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }
    }
}
