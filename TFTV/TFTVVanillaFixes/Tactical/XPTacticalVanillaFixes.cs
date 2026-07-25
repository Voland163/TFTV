using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PRMBetterClasses.SkillModifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class XPTacticalVanillaFixes
    {
        private static bool CheckActorIsInPhoenixRecords(TacticalActor tacticalActor)
        {
            try
            {
                // TFTVLogger.Always($"looking at {tacticalActor?.DisplayName} geoUnitId {tacticalActor?.GeoUnitId}");

                if (tacticalActor.GeoUnitId != null && tacticalActor.GeoUnitId != 0)
                {
                    if (tacticalActor.TacticalLevel.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(tacticalActor.GeoUnitId))
                    {
                        FactionPerks.EnsureDieHardActorHasAtLeast1HP(tacticalActor);
                        TFTVLogger.Always($"{tacticalActor?.DisplayName} with geoUnitId {tacticalActor?.GeoUnitId} found in the Phoenix Records! Should receive XP");

                        return true;
                    }
                }

                TFTVLogger.Always($"{tacticalActor?.DisplayName} with geoUnitId {tacticalActor?.GeoUnitId} is not in the Phoenix Records. No XP for you!");

                return false;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.GiveExperienceForObjectives))]
        public static class TacticalFaction_GiveExperienceForObjectives_patch
        {
            public static bool Prefix(TacticalFaction __instance)
            {
                try
                {
                    if (__instance.Objectives.Count == 0)
                    {
                        return false;
                    }

                    __instance.Objectives.Evaluate();
                    int num = 0;
                    int num2 = 0;
                    foreach (FactionObjective objective in __instance.Objectives)
                    {
                        if (objective.State == FactionObjectiveState.Achieved)
                        {
                            int actualExperienceReward = objective.GetActualExperienceReward();
                            num2 += actualExperienceReward;
                            int actualSkillPointsReward = objective.GetActualSkillPointsReward();
                            num += actualSkillPointsReward;
                        }
                    }

                    GameDifficultyLevelDef difficulty = __instance.TacticalLevel.Difficulty;
                    if (num2 > 0 && difficulty != null)
                    {
                        float expConvertedToSkillpoints = difficulty.ExpConvertedToSkillpoints;
                        int num3 = Mathf.RoundToInt((float)num2 * expConvertedToSkillpoints);
                        num += num3;
                    }

                    GameTagDef vehicleTag = CommonHelpers.GetSharedGameTags().VehicleTag;
                    List<TacticalActor> list = (from p in __instance.GetOwnedActors<TacticalActor>()
                                                where p.LevelProgression != null && p.IsAlive
                                                && !p.GameTags.Contains(vehicleTag) && CheckActorIsInPhoenixRecords(p)
                                                orderby p.Contribution.Contribution descending
                                                select p).ToList();

                    MethodInfo skillpointsMethodInfo = typeof(TacticalFaction).GetMethod("set_Skillpoints", BindingFlags.Instance | BindingFlags.NonPublic);

                    skillpointsMethodInfo.Invoke(__instance, new object[] { __instance.Skillpoints + num });

                    if (__instance.State == TacFactionState.Won && difficulty != null)
                    {
                        foreach (TacticalActor item in list)
                        {
                            if (item.LevelProgression.Def.UsesSkillPoints)
                            {
                                item.CharacterProgression.AddSkillPoints(difficulty.SoldierSkillPointsPerMission);
                            }
                        }
                    }

                    if (!list.Any() || num2 <= 0)
                    {
                        return false;
                    }

                    Dictionary<TacticalActor, int> xpAwards = list.ToDictionary(actor => actor, actor => 0);
                    DistributeExperience(num2, list, difficulty, xpAwards);

                    // --- Mentor Protocol (new behaviour) ---
                    // Build xpAwards first so we know who earned the most XP before redistribution.
                    // 1. Find the highest single XP award among ALL squad members (before mentor zeroing).
                    int highestXpInSquad = xpAwards.Count > 0 ? xpAwards.Values.Max() : 0;

                    // 2. Gather mentors and eligible non-mentor recipients (alive, in squad, no mentor ability).
                    List<TacticalActor> mentors = list.Where(a => TFTVDrills.DrillsHarmony.MentorProtocol.CheckForMentorProtocolAbility(a)).ToList();
                    List<TacticalActor> nonMentorRecipients = list
                        .Where(a => !TFTVDrills.DrillsHarmony.MentorProtocol.CheckForMentorProtocolAbility(a) && a.LevelProgression.Level < 7)
                        .OrderBy(a => a.LevelProgression.Level)
                        .ThenBy(a => a.LevelProgression.Experience) // tie-break: least XP toward next level
                        .ToList();

                    // Track which non-mentor operatives have already been assigned a mentor's XP.
                    HashSet<TacticalActor> claimedRecipients = new HashSet<TacticalActor>();
                    int mentorSpBonus = 0;

                    foreach (TacticalActor mentor in mentors)
                    {
                        int mentorXp = xpAwards.ContainsKey(mentor) ? xpAwards[mentor] : 0;

                        // Check BEFORE zeroing: does this mentor have the highest XP in the squad?
                        if (mentorXp >= highestXpInSquad && highestXpInSquad > 0)
                        {
                            mentorSpBonus += 2;
                        }

                        // Zero out the mentor's own XP.
                        if (xpAwards.ContainsKey(mentor))
                        {
                            xpAwards[mentor] = 0;
                        }

                        if (mentorXp <= 0)
                        {
                            continue;
                        }

                        // Find the lowest-level non-mentor operative not yet claimed.
                        TacticalActor target = nonMentorRecipients.FirstOrDefault(a => !claimedRecipients.Contains(a));
                        if (target == null)
                        {
                            continue; // No eligible recipient; XP is lost (mentor keeps nothing).
                        }

                        claimedRecipients.Add(target);
                        if (xpAwards.ContainsKey(target))
                        {
                            xpAwards[target] += mentorXp;
                        }
                        else
                        {
                            xpAwards[target] = mentorXp;
                        }
                    }

                    // Apply SP bonus to the common pool.
                    if (mentorSpBonus > 0)
                    {
                        MethodInfo skillpointsMethodInfoBonus = typeof(TacticalFaction).GetMethod("set_Skillpoints", BindingFlags.Instance | BindingFlags.NonPublic);
                        skillpointsMethodInfoBonus.Invoke(__instance, new object[] { __instance.Skillpoints + mentorSpBonus });
                    }

                    foreach (KeyValuePair<TacticalActor, int> award in xpAwards)
                    {
                        if (award.Value <= 0)
                        {
                            continue;
                        }

                        /*    if (award.Key.LevelProgression.Level >= 7)
                            {
                                continue;
                            }*/

                        award.Key.LevelProgression.AddExperience(award.Value);
                    }


                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static void DistributeExperience(int experiencePool, List<TacticalActor> recipients, GameDifficultyLevelDef difficulty, Dictionary<TacticalActor, int> xpAwards)
        {
            if (experiencePool <= 0 || recipients == null || recipients.Count == 0)
            {
                return;
            }

            int remainingExperience = experiencePool;
            float equalDistributionPart = (difficulty != null) ? difficulty.ExpEqualDistributionPart : 0f;
            int equalShare = Mathf.RoundToInt((float)experiencePool * equalDistributionPart) / recipients.Count;
            if (equalShare > 0)
            {
                foreach (TacticalActor tacticalActor in recipients)
                {
                    remainingExperience -= equalShare;
                    xpAwards[tacticalActor] += equalShare;
                }
            }

            if (remainingExperience > 0)
            {
                int totalContribution = recipients.Sum(actor => actor.Contribution.Contribution);
                if (totalContribution > 0)
                {
                    int contributionBase = remainingExperience;
                    foreach (TacticalActor tacticalActor2 in recipients)
                    {
                        float ratio = (float)tacticalActor2.Contribution.Contribution / (float)totalContribution;
                        int share = Mathf.FloorToInt((float)contributionBase * ratio);
                        if (share > 0)
                        {
                            remainingExperience -= share;
                            xpAwards[tacticalActor2] += share;
                        }
                    }
                }
                else
                {
                    int equalContributionShare = remainingExperience / recipients.Count;
                    if (equalContributionShare > 0)
                    {
                        foreach (TacticalActor tacticalActor3 in recipients)
                        {
                            remainingExperience -= equalContributionShare;
                            xpAwards[tacticalActor3] += equalContributionShare;
                        }
                    }
                }
                for (int i = 0; i < remainingExperience && i < recipients.Count; i++)
                {
                    xpAwards[recipients[i]] += 1;
                }
            }
        }

        /// <summary>
        /// Fix no SP no XP when evacuating rescue vehicle/soldier last
        /// </summary>
        /// <param name="actor"></param>

        public static void FixRescueMissionEvac(TacticalActor actor)
        {
            try
            {

                TFTVLogger.Always($"[FixRescueMissionEvac] running for {actor.DisplayName}.");

                TacticalFaction phoenixFaction = actor.TacticalLevel.GetFactionByCommandName("px");

                if (!phoenixFaction.Objectives.Any(obj => obj is RescueSoldiersFactionObjective))
                {
                    return;
                }


                if (phoenixFaction.TacticalActors.Any(a => a.IsAlive && !a.IsEvacuated && a != actor && !a.IsMounted
                && (a.Status == null || a.Status != null && !a.Status.HasStatus<MindControlStatus>() && !a.Status.HasStatus<MinionStatus>())))
                {
                    return;
                }

                RescueSoldiersFactionObjective objective = (RescueSoldiersFactionObjective)phoenixFaction.Objectives.FirstOrDefault(obj => obj is RescueSoldiersFactionObjective);

                TFTVLogger.Always($"got here! actor.TacticalFaction.Faction.FactionDef == objective.RescuedFaction {actor.TacticalFaction.Faction.FactionDef.name} {objective.RescuedFaction.name}");

                MindControlStatus status = actor.Status?.GetStatus<MindControlStatus>();

                if (actor.TacticalFaction.Faction.FactionDef == objective.RescuedFaction || status != null && status.OriginalFaction.FactionDef == objective.RescuedFaction)
                {
                    int rescuedActors = objective.RescuedPeople + 1;

                    TFTVLogger.Always($"{actor.DisplayName} is an objective for the Rescue mission! Total RescuedActors: {rescuedActors}");

                    PropertyInfo propertyInfoState = typeof(FactionObjective).GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
                    propertyInfoState.SetValue(objective, FactionObjectiveState.Achieved);

                    PropertyInfo propertyInfoRescuedPeople = typeof(RescueSoldiersFactionObjective).GetProperty("RescuedPeople", BindingFlags.Instance | BindingFlags.Public);
                    propertyInfoRescuedPeople.SetValue(objective, rescuedActors);

                    //  VehicleEvaced = true;
                    //  TFTVLogger.Always($"objective.State: {objective.State}");
                    phoenixFaction.Objectives.Evaluate();
                    //  TFTVLogger.Always($"objective.State: {objective.State}");
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
