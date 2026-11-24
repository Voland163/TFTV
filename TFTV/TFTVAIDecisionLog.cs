using Base.AI;
using HarmonyLib;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// For the playtest logs: every decision of an AI actor, with each action's best option and its score, so
    /// "why did it overwatch instead of shooting" can be read from TFTV.log instead of guessed.
    ///
    /// The game scores every action of the actor's template against its targets (weight x considerations), keeps
    /// each action's few best (AIFaction.GetTopTargets) and hands them all to AIFaction.SelectTarget, which picks
    /// among those within AISettingsDef's share of the best at random. This postfix logs that list, one line per
    /// decision: "[AI decide] Actor (AP left, position): picks X; options: A 41.2 (target, spot) | B 30.0 (...) ...".
    /// </summary>
    internal static class TFTVAIDecisionLog
    {
        internal static bool Enabled = true;

        private const int OptionsShown = 6;

        [HarmonyPatch(typeof(AIFaction), "SelectTarget")]
        internal static class TFTV_AIFaction_SelectTarget_DecisionLog_patch
        {
            public static void Postfix(IList<AIScoredTarget> scoredTargets, AIScoredTarget __result)
            {
                try
                {
                    if (!Enabled || scoredTargets == null || scoredTargets.Count == 0)
                    {
                        return;
                    }

                    TacticalActor actor = (__result?.Actor ?? scoredTargets[0]?.Actor) as TacticalActor;

                    if (actor == null || actor.TacticalFaction == null || actor.TacticalFaction.IsControlledByPlayer)
                    {
                        return;
                    }

                    List<AIScoredTarget> best = scoredTargets
                        .Where(t => t?.Action?.ActionDef != null)
                        .GroupBy(t => t.Action.ActionDef)
                        .Select(g => g.OrderByDescending(t => t.Score).First())
                        .OrderByDescending(t => t.Score)
                        .Take(OptionsShown)
                        .ToList();

                    string options = string.Join(" | ", best.Select(t => Describe(actor, t)));

                    TFTVLogger.Always($"[AI decide] {actor.DisplayName} ({(float)actor.CharacterStats.ActionPoints:0.#}/{actor.CharacterStats.ActionPoints.Max:0} AP at " +
                        $"{Flat(actor.Pos)}): picks {(__result != null ? Describe(actor, __result) : "nothing")}; options: {options}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static string Describe(TacticalActor actor, AIScoredTarget scored)
        {
            string name = scored.Action?.ActionDef?.name?.Replace("_AIActionDef", "").Replace("TFTV_", "") ?? "?";
            string detail = "";

            if (scored.Target is TacAITarget target)
            {
                List<string> parts = new List<string>();

                if (target.Actor != null && target.Actor != actor)
                {
                    parts.Add($"{target.Actor.DisplayName} {Vector3.Distance(target.Pos, target.Actor.Pos):0}m");
                }

                if ((target.Pos - actor.Pos).sqrMagnitude > 0.25f)
                {
                    parts.Add($"to {Flat(target.Pos)}, {target.PathLength:0.#} AP");
                }

                if (target.Equipment is TacticalItem item && item.ItemDef != null)
                {
                    parts.Add(item.ItemDef.name.Replace("_WeaponDef", ""));
                }

                if (parts.Count > 0)
                {
                    detail = $" ({string.Join(", ", parts)})";
                }
            }

            return $"{name} {scored.Score.ToString("0.##", CultureInfo.InvariantCulture)}{detail}";
        }

        private static string Flat(Vector3 pos)
        {
            return $"({pos.x:0.#}, {pos.y:0.#}, {pos.z:0.#})";
        }
    }
}
