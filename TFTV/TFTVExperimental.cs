using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace TFTV
{


    internal class TFTVExperimental
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        [HarmonyPatch(typeof(GeoAlienFaction), "OnResearchUpdated")]
        public static class AlienResearchQueueSeeder
        {
            private const int MaxSeedAttempts = 5;

            public static void Postfix(GeoAlienFaction __instance)
            {
               // TFTVLogger.Always("[AlienResearchCadence] OnResearchUpdated postfix called.");

                if (__instance == null || __instance.Research == null || __instance.Research.Paused)
                {
                    return;
                }

            //    TFTVLogger.Always("[AlienResearchCadence] Alien faction research is not paused.");

                Research research = __instance.Research;
                if (research.Count > 0)
                {
                    return;
                }

                TFTVLogger.Always("[AlienResearchCadence] Research queue is empty, attempting to seed.");

                int attempts = 0;
                while (attempts < MaxSeedAttempts)
                {
                    ResearchElement candidate = research.Researchable.FirstOrDefault();

                TFTVLogger.Always(string.Format("[AlienResearchCadence] Seed attempt {0}, candidate: {1}", attempts + 1, candidate != null ? candidate.ResearchDef.name : "<null>"));

                    if (candidate == null)
                    {
                        break;
                    }

                    try
                    {
                        research.AddResearchToQueue(candidate);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Always(string.Format("[AlienResearchCadence] Failed to seed research {0}: {1}", candidate.ResearchDef.name, ex.Message));
                        break;
                    }

                    if (research.Current != null)
                    {
                        TFTVLogger.Always(string.Format("[AlienResearchCadence] Successfully seeded research queue with {0}", research.Current.ResearchDef.name));

                        return;
                    }

                    attempts++;
                }

                if (research.Current == null && research.Researchable.Any())
                {
                    TFTVLogger.Always(string.Format("[AlienResearchCadence] Research queue still empty after seeding attempts. Researchable={0}", research.Researchable.Count()));
                }
            }
        }





        /*[HarmonyPatch(typeof(GeoAlienFaction), "UpdateResearch")]
        public static class AlienResearchCadenceLogger
        {
            internal struct CadenceSnapshot
            {
                public bool ShouldUpdate;
                public float WalletResearch;
                public float IncomePerHour;
                public int CompletedCount;
                public string[] CompletedIds;
                public string CurrentResearchName;
                public int CurrentResearchCost;
                public float CurrentResearchProgress;
                public bool ResearchPaused;
                public int ResearchableCount;
                public int HiddenCount;
                public int RevealedCount;
                public int UnlockedCount;
                public int CompletedStateCount;
                public TimeUnit Now;
                public TimeUnit NextUpdate;
                public int UpdateHours;
            }

            public static void Prefix(GeoAlienFaction __instance, ref CadenceSnapshot __state)
            {
                if (__instance == null || __instance.GeoLevel == null)
                {
                    return;
                }

                Research research = __instance.Research;
                ResearchElement currentResearch = research != null ? research.Current : null;
                int hiddenCount = 0;
                int revealedCount = 0;
                int unlockedCount = 0;
                int completedCount = 0;
                int researchableCount = 0;
                if (research != null)
                {
                    hiddenCount = research.AllResearchesArray.Count(r => r.State == ResearchState.Hidden);
                    revealedCount = research.AllResearchesArray.Count(r => r.State == ResearchState.Revealed);
                    unlockedCount = research.AllResearchesArray.Count(r => r.State == ResearchState.Unlocked);
                    completedCount = research.AllResearchesArray.Count(r => r.State == ResearchState.Completed);
                    researchableCount = research.Researchable.Count();
                }
                __state = new CadenceSnapshot
                {
                    Now = __instance.GeoLevel.Timing.Now,
                    NextUpdate = __instance.NextResearchUpdate,
                    UpdateHours = __instance.Def.ResearchUpdateTimeHours,
                    WalletResearch = __instance.Wallet[ResourceType.Research].Value,
                    IncomePerHour = __instance.ResourceIncome.GetTotalResouce(ResourceType.Research).Value,
                    CompletedCount = research != null ? research.Completed.Count() : 0,
                    CompletedIds = research != null ? research.Completed.Select(r => r.ResearchDef.name).ToArray() : Array.Empty<string>(),
                    CurrentResearchName = currentResearch != null ? currentResearch.ResearchDef.name : "<none>",
                    CurrentResearchCost = currentResearch != null ? currentResearch.ResearchCost : 0,
                    CurrentResearchProgress = currentResearch != null ? currentResearch.ResearchProgress : 0f,
                    ResearchPaused = research != null && research.Paused,
                    ResearchableCount = researchableCount,
                    HiddenCount = hiddenCount,
                    RevealedCount = revealedCount,
                    UnlockedCount = unlockedCount,
                    CompletedStateCount = completedCount,
                    ShouldUpdate = research != null && __instance.NextResearchUpdate <= __instance.GeoLevel.Timing.Now
                };
            }

            public static void Postfix(GeoAlienFaction __instance, CadenceSnapshot __state)
            {
                if (__instance == null || __instance.GeoLevel == null)
                {
                    return;
                }

                Research research = __instance.Research;
                int completedAfter = research != null ? research.Completed.Count() : 0;
                int completedDelta = completedAfter - __state.CompletedCount;
                TimeUnit now = __instance.GeoLevel.Timing.Now;
                float walletAfter = __instance.Wallet[ResourceType.Research].Value;
                float incomePerHour = __instance.ResourceIncome.GetTotalResouce(ResourceType.Research).Value;
                ResearchElement currentResearch = research != null ? research.Current : null;
                string message = string.Format(
                    "[AlienResearchCadence] now={0} nextBefore={1} nextAfter={2} updateHours={3} shouldUpdate={4} walletBefore={5:0.##} walletAfter={6:0.##} incomePerHour={7:0.##} completedDelta={8} current={9} progress={10:0.##}/{11} paused={12} researchable={13} stateCounts(H={14},R={15},U={16},C={17})",
                    now,
                    __state.NextUpdate,
                    __instance.NextResearchUpdate,
                    __state.UpdateHours,
                    __state.ShouldUpdate,
                    __state.WalletResearch,
                    walletAfter,
                    incomePerHour,
                    completedDelta,
                    currentResearch != null ? currentResearch.ResearchDef.name : "<none>",
                    currentResearch != null ? currentResearch.ResearchProgress : 0f,
                    currentResearch != null ? currentResearch.ResearchCost : 0,
                    __state.ResearchPaused,
                    __state.ResearchableCount,
                    __state.HiddenCount,
                    __state.RevealedCount,
                    __state.UnlockedCount,
                    __state.CompletedStateCount);
                TFTVLogger.Always(message, false);

                if (research == null)
                {
                    return;
                }

                HashSet<string> completedBefore = new HashSet<string>(__state.CompletedIds ?? Array.Empty<string>());
                List<ResearchElement> newlyCompleted = research.Completed.Where(r => !completedBefore.Contains(r.ResearchDef.name)).ToList();
                if (newlyCompleted.Count == 0)
                {
                    return;
                }

                StringBuilder details = new StringBuilder();
                details.Append("[AlienResearchCadence] newlyCompleted=");
                for (int i = 0; i < newlyCompleted.Count; i++)
                {
                    ResearchElement element = newlyCompleted[i];
                    if (i > 0)
                    {
                        details.Append(" | ");
                    }
                    details.Append(element.ResearchDef.name);
                    details.AppendFormat(" cost={0}", element.ResearchCost);
                    List<string> rewardNames = new List<string>();
                    foreach (ResearchReward reward in element.Rewards)
                    {
                        if (reward is UnitTemplateResearchReward unitTemplateReward)
                        {
                            rewardNames.Add(string.Format("UnitTemplate:{0} add={1}", unitTemplateReward.RewardDef.Template.name, unitTemplateReward.RewardDef.Add));
                        }
                        else
                        {
                            rewardNames.Add(reward.BaseDef.name);
                        }
                    }
                    if (rewardNames.Count > 0)
                    {
                        details.AppendFormat(" rewards=[{0}]", string.Join(", ", rewardNames));
                    }
                }
                TFTVLogger.Always(details.ToString(), false);
            }
        }*/
        public static class ResearchCalendarUtility
        {
            public sealed class ResearchCalendarEntry
            {
                public sealed class TemplateUnlockInfo
                {
                    public string TemplateName { get; private set; }
                    public string LevelText { get; private set; }

                    public TemplateUnlockInfo(string templateName, string levelText)
                    {
                        this.TemplateName = templateName;
                        this.LevelText = levelText;
                    }
                }

                public ResearchDef ResearchDef { get; private set; }
                public int Priority { get; private set; }
                public int SecondaryPriority { get; private set; }
                public int ResearchCost { get; private set; }
                public float RemainingCost { get; private set; }
                public float DaysToComplete { get; private set; }
                public float DaysFromNow { get; private set; }
                public IReadOnlyList<TemplateUnlockInfo> TemplateUnlocks { get; private set; }

                public ResearchCalendarEntry(ResearchDef researchDef, int priority, int secondaryPriority, int researchCost, float remainingCost, float daysToComplete, float daysFromNow, IReadOnlyList<TemplateUnlockInfo> templateUnlocks)
                {
                    this.ResearchDef = researchDef;
                    this.Priority = priority;
                    this.SecondaryPriority = secondaryPriority;
                    this.ResearchCost = researchCost;
                    this.RemainingCost = remainingCost;
                    this.DaysToComplete = daysToComplete;
                    this.DaysFromNow = daysFromNow;
                    this.TemplateUnlocks = templateUnlocks;
                }
            }

            public sealed class FactionResearchCalendar
            {
                public GeoFaction Faction { get; private set; }
                public float HourlyResearchOutput { get; private set; }
                public IReadOnlyList<ResearchCalendarEntry> Entries { get; private set; }

                public FactionResearchCalendar(GeoFaction faction, float hourlyResearchOutput, IReadOnlyList<ResearchCalendarEntry> entries)
                {
                    this.Faction = faction;
                    this.HourlyResearchOutput = hourlyResearchOutput;
                    this.Entries = entries;
                }
            }

            public static IReadOnlyList<FactionResearchCalendar> BuildCalendars(GeoLevelController level)
            {
                if (level == null)
                {
                    throw new ArgumentNullException("level");
                }
                DefRepository defRepository = level.GameController.GetComponent<DefRepository>();
                if (defRepository == null)
                {
                    throw new InvalidOperationException("DefRepository not found on the game controller.");
                }
                List<GeoFaction> factions = new List<GeoFaction>
            {
                level.AnuFaction,
                level.NewJerichoFaction,
                level.SynedrionFaction
            };

                List<FactionResearchCalendar> calendars = new List<FactionResearchCalendar>();
                foreach (GeoFaction faction in factions)
                {
                    if (faction == null)
                    {
                        continue;
                    }
                    ResearchDbDef researchDb = null;
                    if (faction == faction.GeoLevel.AnuFaction)
                    {
                        researchDb = DefCache.GetDef<ResearchDbDef>("anu_ResearchDB");
                    }
                    else if (faction == faction.GeoLevel.NewJerichoFaction)
                    {

                        researchDb = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                    }
                    else if (faction == faction.GeoLevel.SynedrionFaction)
                    {
                        researchDb = DefCache.GetDef<ResearchDbDef>("syn_ResearchDB");
                    }

                    if (researchDb == null)
                    {
                        continue;
                    }

                    List<ResearchCalendarEntry> entries = BuildFactionCalendarEntries(faction, researchDb.Researches);
                    float hourlyOutput = GetHourlyResearchOutput(faction);
                    calendars.Add(new FactionResearchCalendar(faction, hourlyOutput, entries));
                }

                return calendars;
            }

            public static void LogCalendars(GeoLevelController level)
            {
                foreach (FactionResearchCalendar calendar in BuildCalendars(level))
                {
                    TFTVLogger.Always(FormatCalendar(calendar));
                }
            }

            private static float GetHourlyResearchOutput(GeoFaction faction)
            {
                return (float)faction.ResourceIncome.GetTotalResouce(ResourceType.Research).RoundedValue;
            }

            private static List<ResearchCalendarEntry> BuildFactionCalendarEntries(GeoFaction faction, IEnumerable<ResearchDef> researchDefs)
            {
                Research research = faction.Research;
                List<ResearchDef> orderedResearches = researchDefs
                    .Where((ResearchDef def) => def != null)
                    .Where((ResearchDef def) =>
                    {
                        ResearchElement researchElement = research.GetByDef(def);
                        return researchElement != null && !researchElement.IsCompleted;
                    })
                    .OrderByDescending((ResearchDef def) => def.Priority)
                    .ThenByDescending((ResearchDef def) => def.SecodaryPriority)
                    .ThenBy((ResearchDef def) => def.name)
                    .ToList();

                float hourlyOutput = GetHourlyResearchOutput(faction);
                float daysFromNow = 0f;
                List<ResearchCalendarEntry> entries = new List<ResearchCalendarEntry>();

                foreach (ResearchDef researchDef in orderedResearches)
                {
                    ResearchElement researchElement = research.GetByDef(researchDef);
                    float remainingCost = (float)researchDef.ResearchCost;
                    if (researchElement != null)
                    {
                        remainingCost = Mathf.Max(0f, (float)researchDef.ResearchCost - researchElement.ResearchProgress);
                    }

                    float daysToComplete = (hourlyOutput <= 0f) ? float.PositiveInfinity : remainingCost / hourlyOutput / 24f;
                    daysFromNow += daysToComplete;

                    IReadOnlyList<ResearchCalendarEntry.TemplateUnlockInfo> templateUnlocks = GetTemplateUnlocks(researchDef);
                    entries.Add(new ResearchCalendarEntry(
                        researchDef,
                        researchDef.Priority,
                        researchDef.SecodaryPriority,
                        researchDef.ResearchCost,
                        remainingCost,
                        daysToComplete,
                        daysFromNow,
                        templateUnlocks));
                }

                return entries;
            }

            private static string FormatCalendar(FactionResearchCalendar calendar)
            {
                if (calendar == null)
                {
                    return "Research Calendar: (null)";
                }

                string header = string.Format("Research Calendar for {0} (Hourly Output: {1})", calendar.Faction.Name, calendar.HourlyResearchOutput);
                List<string> lines = new List<string> { header };

                if (calendar.Entries.Count == 0)
                {
                    lines.Add("  (no remaining research)");
                    return string.Join("\n", lines);
                }

                foreach (ResearchCalendarEntry entry in calendar.Entries)
                {
                    string daysToComplete = float.IsPositiveInfinity(entry.DaysToComplete) ? "∞" : entry.DaysToComplete.ToString("0.00");
                    string daysFromNow = float.IsPositiveInfinity(entry.DaysFromNow) ? "∞" : entry.DaysFromNow.ToString("0.00");
                    lines.Add(string.Format("  {0} | Priority {1}/{2} | Cost {3} | Remaining {4:0.##} | +{5} days -> {6} days from now",
                        entry.ResearchDef.name,
                        entry.Priority,
                        entry.SecondaryPriority,
                        entry.ResearchCost,
                        entry.RemainingCost,
                        daysToComplete,
                        daysFromNow));

                    if (entry.TemplateUnlocks.Count > 0)
                    {
                        string templates = string.Join(", ", entry.TemplateUnlocks.Select((ResearchCalendarEntry.TemplateUnlockInfo t) => string.Format("{0} ({1})", t.TemplateName, t.LevelText)));
                        lines.Add(string.Format("    Templates: {0}", templates));
                    }
                }

                return string.Join("\n", lines);
            }

            private static IReadOnlyList<ResearchCalendarEntry.TemplateUnlockInfo> GetTemplateUnlocks(ResearchDef researchDef)
            {
                List<ResearchCalendarEntry.TemplateUnlockInfo> templates = new List<ResearchCalendarEntry.TemplateUnlockInfo>();
                if (researchDef.Unlocks == null)
                {
                    return templates;
                }

                foreach (UnitTemplateResearchRewardDef rewardDef in researchDef.Unlocks.OfType<UnitTemplateResearchRewardDef>())
                {
                    TacCharacterDef template = rewardDef.Template;
                    if (template == null || !IsFactionTemplateName(template.name))
                    {
                        continue;
                    }

                    templates.Add(new ResearchCalendarEntry.TemplateUnlockInfo(template.name, GetTemplateLevelText(template)));
                }

                return templates;
            }

            private static bool IsFactionTemplateName(string templateName)
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    return false;
                }

                return templateName.StartsWith("AN_", StringComparison.OrdinalIgnoreCase)
                    || templateName.StartsWith("NJ_", StringComparison.OrdinalIgnoreCase)
                    || templateName.StartsWith("SY_", StringComparison.OrdinalIgnoreCase);
            }

            private static string GetTemplateLevelText(TacCharacterDef template)
            {
                if (template == null || template.Data == null)
                {
                    return "Level ?";
                }

                if (template.Data.LevelProgression != null && template.Data.LevelProgression.IsValid)
                {
                    return string.Format("Level {0}", template.Data.LevelProgression.Level);
                }

                return "Level ?";
            }
        }





        /*  [HarmonyPatch(typeof(HealAbility), "ShouldReturnTarget")]
          internal static class TechnicianRepairTargetLoggingPatch
          {
              private const string TechnicianRepairAbilityName = "TechnicianRepair_AbilityDef";
              private const string TargetDisplayName = "JUNKER";

              private static void Postfix(HealAbility __instance, TacticalActor healer, TacticalActor targetActor, ref bool __result)
              {
                  if (__result || __instance?.HealAbilityDef == null || targetActor == null)
                  {
                      return;
                  }

                  if (!string.Equals(__instance.HealAbilityDef.name, TechnicianRepairAbilityName, StringComparison.Ordinal))
                  {
                      return;
                  }

                  if (!string.Equals(targetActor.DisplayName, TargetDisplayName, StringComparison.OrdinalIgnoreCase))
                  {
                      return;
                  }

                  HealAbilityDef healAbilityDef = __instance.HealAbilityDef;
                  bool suppressedHealing = targetActor.HasGameTags(healAbilityDef.SuppressHealingOnTargetTags, false);
                  bool needsGeneralHeal = __instance.GeneralHealAmount > 0f && targetActor.Health.Value < targetActor.Health.Max;
                  bool hasHealableBodyParts = healAbilityDef.HealBodyParts && targetActor.BodyState.GetHealthSlots().Any((ItemSlot slot) => HasHealableBodyPart(healAbilityDef, slot));
                  bool repairsArmor = healAbilityDef.RestoresArmour;
                  bool hasDamagedArmor = repairsArmor && targetActor.BodyState.GetHealthSlots().Any((ItemSlot slot) => slot.GetArmor().Value < slot.GetArmor().Max);
                  bool conditionalEffectMet = healAbilityDef.HealEffects != null && healAbilityDef.HealEffects.Any((HealAbilityDef.ConditionalHealEffect effect) => ConditionalEffectApplies(effect, healer, targetActor));

                  string reason;
                  if (suppressedHealing && !conditionalEffectMet)
                  {
                      reason = "healing is suppressed on the target and no conditional heal effect matched.";
                  }
                  else if (!needsGeneralHeal && !hasHealableBodyParts && (!repairsArmor || !hasDamagedArmor) && !conditionalEffectMet)
                  {
                      reason = "nothing to heal or repair and conditional heal effects did not match.";
                  }
                  else
                  {
                      reason = "failed an unspecified heal targeting requirement.";
                  }

                  Debug.LogWarning($"[TechnicianRepair diagnostics] Ability '{healAbilityDef.name}' skipped target '{targetActor.DisplayName}': suppressed={suppressedHealing}, needsGeneralHeal={needsGeneralHeal}, healableBodyParts={hasHealableBodyParts}, armourDamaged={hasDamagedArmor}, conditionalEffectMet={conditionalEffectMet}. Reason: {reason}");
              }

              private static bool ConditionalEffectApplies(HealAbilityDef.ConditionalHealEffect healEffect, TacticalActor healer, TacticalActor targetActor)
              {
                  if (healEffect == null)
                  {
                      return false;
                  }

                  return !(healEffect.HealerConditions?.Any((EffectConditionDef condition) => condition != null && !condition.ConditionMet(healer)) ?? false) && !(healEffect.TargetGenerationConditions?.Any((EffectConditionDef condition) => condition != null && !condition.ConditionMet(targetActor)) ?? false);
              }

              private static bool HasHealableBodyPart(HealAbilityDef healAbilityDef, ItemSlot slot)
              {
                  if (slot == null)
                  {
                      return false;
                  }

                  if (!healAbilityDef.IgnoreDisabledSlots && !slot.Enabled)
                  {
                      return false;
                  }

                  if (healAbilityDef.BlockedBodypartsTagDef != null && slot.HasDirectGameTag(healAbilityDef.BlockedBodypartsTagDef, true))
                  {
                      return false;
                  }

                  if (healAbilityDef.ExclusiveBodypartsTagDef != null && !slot.HasDirectGameTag(healAbilityDef.ExclusiveBodypartsTagDef, true))
                  {
                      return false;
                  }

                  return slot.GetHealth().Value < slot.GetHealth().Max;
              }
          }*/

        /*  private static bool HasLineOfSight(
              TacticalTargetData targetData,
              TacticalActorBase sourceActor,
              Vector3 sourcePosition,
              TacticalActorBase targetActor)
          {
              if (sourceActor.CheckVisibleLineBetweenActors(sourcePosition, targetActor, true))
              {
                  return true;
              }

              if (!targetData.CanPeekFromEdge)
              {
                  return false;
              }

              var floorCast = sourceActor.GetFloorCast();
              floorCast.Ray.direction = Vector3.down;
              floorCast.MaxDistance = 1f;

              float agentRadius = sourceActor.NavigationComponent.AgentNavSettings.AgentRadius;
              foreach (Vector3 peekPos in TacticalMap.GetPositionsInRange(sourcePosition, agentRadius, agentRadius + 1f))
              {
                  Vector3 dir = (peekPos - sourcePosition).normalized;
                  if (sourceActor.Map.GetCoverInfoInDirection(sourcePosition, dir, sourceActor.TacticalPerceptionBase.VisionHeight).CoverType != CoverType.None)
                  {
                      continue;
                  }

                  floorCast.Ray.origin = peekPos + Vector3.up * 0.05f;
                  if (!floorCast.Cast() &&
                      sourceActor.CheckVisibleLineBetweenActors(peekPos, targetActor, false))
                  {
                      return true;
                  }
              }

              return false;
          }*/



        /*  [HarmonyPatch(typeof(UIStateVehicleSelected), "OnSelect")]
          internal static class UIStateVehicleSelected_OnSelect_Patch
          {
              private static readonly AccessTools.FieldRef<UIStateVehicleSelected, bool> SuppressGamepadSelectEventRef = AccessTools.FieldRefAccess<UIStateVehicleSelected, bool>("_suppressGamepadSelectEvent");

              private static readonly AccessTools.FieldRef<UIStateVehicleSelected, UIModuleActionsBar> ActionsBarModuleRef = AccessTools.FieldRefAccess<UIStateVehicleSelected, UIModuleActionsBar>("_actionsBarModule");

              private static readonly AccessTools.FieldRef<UIStateVehicleSelected, UIModuleSiteContextualMenu> ContextualMenuModuleRef = AccessTools.FieldRefAccess<UIStateVehicleSelected, UIModuleSiteContextualMenu>("_contextualMenuModule");

              private static readonly AccessTools.FieldRef<UIStateVehicleSelected, GeoscapeCamera> GeoscapeCameraRef = AccessTools.FieldRefAccess<UIStateVehicleSelected, GeoscapeCamera>("_geoscapeCamera");

              private static readonly MethodInfo SelectedVehicleGetter = AccessTools.PropertyGetter(typeof(UIStateVehicleSelected), "SelectedVehicle");

              private static readonly MethodInfo SelectVehicleMethod = AccessTools.Method(typeof(UIStateVehicleSelected), "SelectVehicle", new[] { typeof(GeoVehicle), typeof(bool) });

              private static readonly MethodInfo ContextGetter = AccessTools.PropertyGetter(typeof(GeoscapeViewState), "Context");

              private static readonly MethodInfo CursorOverGuiGetter = AccessTools.PropertyGetter(typeof(GeoscapeViewState), "CursorOverGui");

              public static void Postfix(UIStateVehicleSelected __instance)
              {
                  GeoscapeViewContext context = (GeoscapeViewContext)ContextGetter.Invoke(__instance, null);
                  if (context == null || context.Input.InputType != Base.Input.InputType.Joystick)
                  {
                      return;
                  }

                  if ((bool)CursorOverGuiGetter.Invoke(__instance, null))
                  {
                      return;
                  }

                  GeoscapeCamera geoscapeCamera = GeoscapeCameraRef(__instance);
                  if (geoscapeCamera != null && geoscapeCamera.IsCursorOverGUI)
                  {
                      return;
                  }

                  if (SuppressGamepadSelectEventRef(__instance))
                  {
                      SuppressGamepadSelectEventRef(__instance) = false;
                      return;
                  }

                  UIModuleActionsBar actionsBarModule = ActionsBarModuleRef(__instance);
                  if (actionsBarModule != null && actionsBarModule.GamepadAbilityScrolling)
                  {
                      return;
                  }

                  GeoscapeSelectionInfo geoscapeSelectionInfo = context.View.SelectAtCursor(true);
                  GeoVehicle geoVehicle = geoscapeSelectionInfo.Actor as GeoVehicle;
                  if (geoVehicle == null)
                  {
                      return;
                  }

                  GeoVehicle selectedVehicle = (GeoVehicle)SelectedVehicleGetter.Invoke(__instance, null);
                  if (selectedVehicle != null)
                  {
                      selectedVehicle.Animator.SetBool("IsSelected", false);
                  }

                  SelectVehicleMethod.Invoke(__instance, new object[] { geoVehicle, false });

                  UIModuleSiteContextualMenu contextualMenuModule = ContextualMenuModuleRef(__instance);
                  contextualMenuModule?.HideContextualMenu();
              }
          }*/




        /* [HarmonyPatch(typeof(FactionCharacterGenerator), "GeneratePersonalAbilities")]
            internal static class Debug_GenerateUnit_Patches
            {
                // Called before 'GenerateUnit' -> PREFIX.
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                private static void Prefix(FactionCharacterGenerator __instance, int abilitiesCount, LevelProgressionDef levelDef, List<TacticalAbilityDef> ____personalAbilityPool)
                {
                    try
                    {
                        TFTVLogger.Always($"GeneratePersonalAbilities {abilitiesCount} {levelDef?.name} ____personalAbilityPool==null: {____personalAbilityPool==null}");

                      foreach (var ability in ____personalAbilityPool)
                      {
                          TFTVLogger.Always($"ability null? {ability==null} {ability?.name}");


                      }

                    }
                    catch (Exception e)
                    {
                        PRMLogger.Error(e);

                    }
                }
            }*/


        /*
                [HarmonyPatch(typeof(UnitTemplateResearchReward), "GiveReward")]
                internal static class UnitTemplateResearchReward_GiveReward_Patch
                {
                    private static void Prefix(UnitTemplateResearchReward __instance, GeoFaction faction)
                    {
                        try
                        {
                            if (faction == faction.GeoLevel.NewJerichoFaction && __instance.RewardDef.Template.Data.LevelProgression.Level == 1) 
                            {
                                TFTVLogger.Always($"PREFIX {__instance.RewardDef.name} : {__instance.RewardDef.Template.name} add: {__instance.RewardDef.Add} already unlocked: {faction.UnlockedUnitTemplates.Contains(__instance.RewardDef.Template)}"); 
                            }                   
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }

                    private static void Postfix(UnitTemplateResearchReward __instance, GeoFaction faction)
                    {
                        try
                        {
                            if (faction == faction.GeoLevel.NewJerichoFaction && __instance.RewardDef.Template.Data.LevelProgression.Level == 1)
                            {
                                TFTVLogger.Always($"POSTFIX {__instance.RewardDef.name} : {__instance.RewardDef.Template.name} add: {__instance.RewardDef.Add} already unlocked: {faction.UnlockedUnitTemplates.Contains(__instance.RewardDef.Template)}");
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }
                }

                [HarmonyPatch(typeof(UnitTemplateResearchReward), "RemoveReward")]
                internal static class UnitTemplateResearchReward_RemoveReward_Patch
                {
                    private static void Prefix(UnitTemplateResearchReward __instance, GeoFaction faction)
                    {
                        try
                        {
                            if (faction == faction.GeoLevel.NewJerichoFaction && __instance.RewardDef.Template.Data.LevelProgression.Level == 1)
                            {
                                TFTVLogger.Always($"PREFIX {__instance.RewardDef.name} : {__instance.RewardDef.Template.name} add: {__instance.RewardDef.Add} already unlocked {faction.UnlockedUnitTemplates.Contains(__instance.RewardDef.Template)}");
                            }


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }

                    private static void Postfix(UnitTemplateResearchReward __instance, GeoFaction faction)
                    {
                        try
                        {
                            if (faction == faction.GeoLevel.NewJerichoFaction && __instance.RewardDef.Template.Data.LevelProgression.Level == 1)
                            {
                                TFTVLogger.Always($"POSTFIX {__instance.RewardDef.name} : {__instance.RewardDef.Template.name} add: {__instance.RewardDef.Add} already unlocked: {faction.UnlockedUnitTemplates.Contains(__instance.RewardDef.Template)}");
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }
                }*/

        //increase level of recruits
        //level 2: +4 +4 +1 
        //level 3: +6 +6 +2
        //level 4: +8 +8 +3
        //level 5: +10 +10 +4
        //level 6: +12 +12 +5
        //plan would be to modify  public static GeoEventChoice GenerateItemChoice(ItemDef itemDef, float price) to change name to show LVL of merc

        public static int MaxHavenRecruitLevel()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                int templateMaxXP = controller.AnuFaction.UnlockedUnitTemplates.OrderByDescending(
                    tem => tem.Data.LevelProgression.Experience).FirstOrDefault().Data.LevelProgression.Experience;

                int maxLevel = 1;
                int[] stats = new int[] { 0, 0, 0 };

                if (templateMaxXP > 2000)
                {
                    maxLevel = 7;
                    stats = new int[] { 14, 14, 6 };
                }
                else if (templateMaxXP > 1500)
                {
                    maxLevel = 6;
                    stats = new int[] { 12, 12, 5 };
                }
                else if (templateMaxXP > 900)
                {
                    maxLevel = 5;
                    stats = new int[] { 10, 10, 4 };
                }
                else if (templateMaxXP > 500)
                {
                    maxLevel = 4;
                    stats = new int[] { 8, 8, 3 };
                }
                else if (templateMaxXP > 250)
                {
                    maxLevel = 3;
                    stats = new int[] { 6, 6, 2 };
                }
                else if (templateMaxXP > 100)
                {
                    maxLevel = 2;
                    stats = new int[] { 4, 4, 1 };
                }

                return maxLevel;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void PrintInfoHavenRecruits(GeoLevelController controller)
        {
            try
            {

                foreach (GeoSite geoSite in controller.Map.AllSites.Where(s => s.Type == GeoSiteType.Haven && s.GetComponent<GeoHaven>().AvailableRecruit != null))
                {
                    TFTVLogger.Always($"{geoSite.GetComponent<GeoHaven>().AvailableRecruit.GetName()} at {geoSite.LocalizedSiteName}");
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void PrintAvailableTemplates(GeoLevelController controller)
        {
            try
            {



                foreach (TacCharacterDef tacCharacterDef in controller.NewJerichoFaction.UnlockedUnitTemplates)
                {
                    TFTVLogger.Always($"Template: {tacCharacterDef.name}, Level: {tacCharacterDef.Data.LevelProgression.Level}, ClassTag: {tacCharacterDef.ClassTag}");
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }



        /*  

               public GeoUnitDescriptor GenerateRandomUnit(CharacterGenerationContext context)
          {
             
          }*/

        //need to add: 
        /* if (geoUnitDescriptor.Progression != null)
         {
             geoUnitDescriptor.Progression.LearnPrimaryAbilities = true;
         }*/



        /* [HarmonyPatch(typeof(GeoVehicle), "get_Speed")]
         public static class GeoVehicle_get_Speed_patch
         {
             public static void Postfix(GeoVehicle __instance, EarthUnits __result)
             {
                 try
                 {
                     TFTVLogger.Always($"GeoVehicle.get_Speed: {__instance.name}, {__result} {__instance.GlobePosition} {__instance.GeoLevel.Map.}");



                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /* public static void AdjustMusicLevelAncientMaps(TacticalLevelController controller)
         {
             try
             {

                 MissionTypeTagDef ancientsMission = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSite_MissionTagDef");

                 AudioManager audioManager = GameUtl.GameComponent<PhoenixGame>().GetComponent<AudioManager>();

                 TFTVLogger.Always($"current music setting is: {audioManager.GetAudioLevel(MixerKey.Music)}");

                 if (TFTVAncients.CheckIfAncientsPresent(controller))
                 {
                     TFTVLogger.Always($"Ancients Map. Adjusting music");

                     audioManager.SetAudioLevel(MixerKey.Music, Mathf.Min(0.25f, _playerSetMusicVolume));
                 }


             }
             catch (Exception e)
             {
                 TFTVLogger.Error(e);
                 throw;
             }



         }*/





        /*  [HarmonyPatch(typeof(InterceptionBriefDataBind), "DisplayData")]
          public static class InterceptionBriefDataBind_DisplayData_patch
          {
              public static bool Prefix(InterceptionBriefDataBind __instance, InterceptionInfoData data)
              {
                  try
                  {
                      MethodInfo displayDataMethodInfo = typeof(InterceptionBriefDataBind).GetMethod("DisplayData", BindingFlags.Instance | BindingFlags.NonPublic);
                      MethodInfo setAircraftImageMethodInfo = typeof(InterceptionBriefDataBind).GetMethod("SetAircraftImage", BindingFlags.Instance | BindingFlags.NonPublic);
                      MethodInfo setAircraftButtonAndEquipmentMethodInfo = typeof(InterceptionBriefDataBind).GetMethod("SetAircraftImage", BindingFlags.Instance | BindingFlags.NonPublic);
                      MethodInfo setEnemyButtonAndEquipmentMethodInfo = typeof(InterceptionBriefDataBind).GetMethod("SetEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
                      MethodInfo setWeatherConditionButtonAndEquipmentMethodInfo = typeof(InterceptionBriefDataBind).GetMethod("SetWeatherCondition", BindingFlags.Instance | BindingFlags.NonPublic);

                      TFTVLogger.Always($"running InterceptionBriefDataBind.DisplayData. Data is null? {data == null}");

                      if (data.CurrentPlayerAircraft == null)
                      {
                          TFTVLogger.Always($"currentplayeraircraft is null");
                          data.CurrentPlayerAircraft = data.GetDefaultPlayerAircraft();
                          TFTVLogger.Always($"got the default player aircraft. is it null though? {data.CurrentPlayerAircraft == null}");
                      }

                      if (data.CurrentEnemyAircraft == null)
                      {
                          TFTVLogger.Always($"currentenemyaircraft is null");
                          data.CurrentEnemyAircraft = data.GetDefaultEnemyAircraft();
                          TFTVLogger.Always($"got the currentenemyaircraft. is it null though? {data.CurrentEnemyAircraft == null}");
                      }

                      GeoVehicle currentPlayerAircraft = data.CurrentPlayerAircraft;
                      GeoVehicle currentEnemyAircraft = data.CurrentEnemyAircraft;
                      __instance.PlayerAircraftName.text = currentPlayerAircraft.Name;
                      setAircraftImageMethodInfo.Invoke(__instance, new object[] { __instance.PlayerAircraftImagesRoot, currentPlayerAircraft.VehicleDef.WorldVisuals.WorldObjectSprite });
                      __instance.EnemyAircraftName.text = currentEnemyAircraft.Name;
                      setAircraftImageMethodInfo.Invoke(__instance, new object[] { __instance.EnemyAircraftImagesRoot, currentEnemyAircraft.VehicleDef.WorldVisuals.WorldObjectSprite });

                      TFTVLogger.Always($"got here 0");

                      setAircraftButtonAndEquipmentMethodInfo.Invoke(__instance, new object[] { __instance.PlayerHull, __instance.PlayerAircraftEquipment, __instance.FesteringSkiesSettingsDef.UISettings.ActionsToSelectEquipments, currentPlayerAircraft, true });
                      setAircraftButtonAndEquipmentMethodInfo.Invoke(__instance, new object[] { __instance.EnemyHull, __instance.EnemyAircraftEquipment, __instance.FesteringSkiesSettingsDef.UISettings.ActionsToTargetEnemy, currentEnemyAircraft, false });
                      __instance.PlayerAircraftArmour.text = $"{currentPlayerAircraft.Stats.Armor}";
                      __instance.EnemyAircraftArmour.text = $"{currentEnemyAircraft.Stats.Armor}";

                      TFTVLogger.Always($"got here 1");

                      displayAvailableAircrafts(__instance.PlayerAircraftSelectionButtons, __instance.PlayerAircraftSelectionPages, data.PlayerAircrafts, currentPlayerAircraft, delegate (GeoVehicle vehicle)
                      {
                          data.CurrentPlayerAircraft = vehicle;
                          displayDataMethodInfo.Invoke(__instance, new object[] { data });
                      });
                      displayAvailableAircrafts(__instance.EnemyAircraftSelectionButtons, __instance.EnemyAircraftSelectionPages, data.EnemyAircrafts, currentEnemyAircraft, delegate (GeoVehicle vehicle)
                      {
                          data.CurrentEnemyAircraft = vehicle;
                          displayDataMethodInfo.Invoke(__instance, new object[] { data });
                      });

                      TFTVLogger.Always($"got here 2");

                      __instance.CrewContainer.SetCrew(currentPlayerAircraft.Soldiers, currentPlayerAircraft.MaxCharacterSpace);
                      setEnemyButtonAndEquipmentMethodInfo.Invoke(__instance, new object[] { currentEnemyAircraft });
                      setWeatherConditionButtonAndEquipmentMethodInfo.Invoke(__instance, new object[] { data });

                      TFTVLogger.Always($"got here 3");

                      void displayAvailableAircrafts(ButtonTabbingController aircraftsContainer, ButtonTabbingController pagesContainer, IEnumerable<GeoVehicle> allAircrafts, GeoVehicle selectedAircraft, Action<GeoVehicle> onClickCallback)
                      {
                          IEnumerable<IEnumerable<GeoVehicle>> slices = createSlices();
                          List<PhoenixGeneralButton> createdPageButtons = new List<PhoenixGeneralButton>();
                          PhoenixGeneralButton selectedPageButton = null;
                          pagesContainer.gameObject.SetActive(slices.Count() > 1);
                          UIUtil.EnsureActiveComponentsInContainer(pagesContainer.transform, __instance.PageChangeButton, slices, delegate (PhoenixGeneralButton button, IEnumerable<GeoVehicle> slice)
                          {
                              button.PointerClicked = delegate
                              {
                                  displayAircraftSlice(slice);
                                  pagesContainer.SelectedButton = button;
                              };
                              createdPageButtons.Add(button);
                              if (slice.Contains(selectedAircraft))
                              {
                                  displayAircraftSlice(slice);
                                  selectedPageButton = button;
                              }
                          });
                          pagesContainer.TabbingList = createdPageButtons.ToArray();
                          pagesContainer.SelectedButton = selectedPageButton;
                          IEnumerable<IEnumerable<GeoVehicle>> createSlices()
                          {
                              IEnumerable<GeoVehicle> vehiclesLeft = allAircrafts;
                              while (vehiclesLeft.Any())
                              {
                                  yield return vehiclesLeft.Take(__instance.MaxSelectionButtons);
                                  vehiclesLeft = vehiclesLeft.Skip(__instance.MaxSelectionButtons);
                              }
                          }

                          void displayAircraftSlice(IEnumerable<GeoVehicle> slice)
                          {
                              PhoenixGeneralButton selectedButton = null;
                              List<PhoenixGeneralButton> createdButtons = new List<PhoenixGeneralButton>();
                              UIUtil.EnsureActiveComponentsInContainer(aircraftsContainer.transform, __instance.AircraftSelectionButtonPrefab, slice, delegate (PhoenixGeneralButton button, GeoVehicle vehicle)
                              {
                                  button.GetComponent<UIButtonIconController>().Icon.sprite = vehicle.VehicleDef.ViewElement.SmallIcon;
                                  button.PointerClicked = delegate
                                  {
                                      onClickCallback(vehicle);
                                  };
                                  createdButtons.Add(button);
                                  if (vehicle == selectedAircraft)
                                  {
                                      selectedButton = button;
                                  }
                              });
                              aircraftsContainer.TabbingList = createdButtons.ToArray();
                              aircraftsContainer.SelectedButton = selectedButton;
                              aircraftsContainer.gameObject.SetActive(value: true);
                              if (slices.Count() == 1 && createdButtons.Count == 1)
                              {
                                  aircraftsContainer.gameObject.SetActive(value: false);
                              }
                          }
                      }

                      TFTVLogger.Always($"got here final");




                      return false;


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /* [HarmonyPatch(typeof(ActorLastDamageTypeEffectConditionDef), "ActorChecks")]
         public static class ActorLastDamageTypeEffectConditionDef_ActorChecks_patch
         {
             public static void Prefix(TacticalActorBase actor, ActorLastDamageTypeEffectConditionDef __instance)
             {
                 try
                 {


                     TFTVLogger.Always($"ActorCheck: {__instance.name} {__instance.DamageType.name}, {actor.name}, {actor.LastDamageType.name}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

             public static void Postfix(TacticalActorBase actor, ActorLastDamageTypeEffectConditionDef __instance, bool __result)
             {
                 try
                 {


                     TFTVLogger.Always($"ActorCheck: {__instance.name}, {actor.name}, {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /* [HarmonyPatch(typeof(Weapon), "GetTargetBodyParts")]
         public static class Weapon_GetTargetBodyParts_patch
         {
             public static void Postfix(Weapon __instance, TacticalActorBase targetActor)
             {
                 try
                 {
                     CharacterBodyState component = targetActor.GetComponent<CharacterBodyState>();
                     if (component != null)
                     {
                         foreach (BodyPartAspect bodyPartAspect in component.GetAllBodyparts())
                         {
                             TFTVLogger.Always($"Component: {bodyPartAspect.BodyPartAspectDef.name}");
                         }

                         IEnumerable<BodyPartAspect> enumerable = component.GetVisibleBodyparts();

                         foreach(BodyPartAspect bodyPartAspect in enumerable) 
                         {
                             TFTVLogger.Always($"Visible Bodyparts: {bodyPartAspect.BodyPartAspectDef.name}, {bodyPartAspect.OwnerItem?.DisplayName}");                   
                         } 
                     }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/






        /*  [HarmonyPatch(typeof(UIStateEditSoldier), "ExitState")]
          public static class UIStateEditSoldier_ExitState_patch
          {
              public static void Postfix(UIStateEditSoldier __instance)
              {
                  try
                  {



                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/


        /*  [HarmonyPatch(typeof(GeoscapeView), "OpenModal")]
          public static class GeoscapeView_OpenModal_patch
          {
              public static void Postfix(GeoscapeView __instance, ModalType modalType, DialogCallback callback, object modalData, int priority, ref bool forceOnTop, ref bool replaceTop)
              {
                  try
                  {
                      TFTVLogger.Always($"GeoscapeView.OpenModal: {modalType.GetName()}");

                      if (modalType == ModalType.CharacterProgressionConfirmCharacter || modalType == ModalType.DualClassPicker)
                      {



                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/



        /*  [HarmonyPatch(typeof(UnusableHandStatus), "AfterApply")]
          public static class UnusableHandStatus_AfterApply_patch
          {
              public static void Postfix(UnusableHandStatus __instance)
              {
                  try
                  {
                      TFTVLogger.Always($"{__instance.TacticalActor.name}, usable hands: {__instance.TacticalActor.GetUsableHands()}");

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/








        /* [HarmonyPatch(typeof(UIStateEditSoldier), "OnSelectSecondaryClass")]
         public static class UIStateEditSoldier_OnSelectSecondaryClass_patch
         {
             public static bool Prefix(UIStateEditSoldier __instance, ref bool ____confirmationDialogRequest, GeoCharacter ____currentCharacter)
             {
                 try
                 {

                     if (____currentCharacter.Progression.MainSpecDef != TFTVChangesToDLC5.TFTVMercenaries.SlugSpecialization)
                     {
                         return true;
                     }

                     GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                     SpecializationDef techSpec = DefCache.GetDef<SpecializationDef>("TechnicianSpecializationDef");

                     ____confirmationDialogRequest = true;
                     List<SpecializationDef> availableSpecs = controller.ViewerFaction.AvailableCharacterSpecializations.Where((SpecializationDef p) => p != ____currentCharacter.Progression.MainSpecDef && !p.NotSecondClassSpecialization).ToList();

                     if (availableSpecs.Contains(techSpec))
                     {
                         availableSpecs.Remove(techSpec);
                     }

                     SelectSpecializationDataBind.Data modalData = new SelectSpecializationDataBind.Data
                     {
                         AvailableSpecs = availableSpecs,
                         SelectedSpec = null
                     };

                     // Get the MethodInfo object for the private method
                     MethodInfo methodInfo = typeof(UIStateEditSoldier).GetMethod("OnDualClassPickerClosed", BindingFlags.NonPublic | BindingFlags.Instance);

                     // Create a delegate to invoke the private method
                     Action<ModalResult, SelectSpecializationDataBind.Data> onDualClassPickerClosed = (Action<ModalResult, SelectSpecializationDataBind.Data>)Delegate.CreateDelegate(typeof(Action<ModalResult, SelectSpecializationDataBind.Data>), __instance, methodInfo);

                     controller.View.OpenModal(ModalType.DualClassPicker, delegate (ModalResult res)
                     {
                         onDualClassPickerClosed.Invoke(res, modalData);
                     }, modalData, 100, forceOnTop: true);

                     return false;

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/






        /*  [HarmonyPatch(typeof(TacticalNavigationComponent), "WaitForAnimation")]
          public static class TFTV_TacticalNavigationComponent_WaitForAnimation
          {
              public static void Prefix(TacticalNavigationComponent __instance, AnimationClip animation)
              {
                  try
                  {
                      TFTVLogger.Always($"{__instance?.TacticalActor?.name}: {animation?.name} current anim? {Utils.GetCurrentAnim(__instance.Animator).name}");

                      if(Utils.GetCurrentAnim(__instance.Animator).name== "FF_ShotLoopNoRecoil_SN") 
                      {
                          TFTVLogger.Always($"{__instance?.TacticalActor?.name} passed the if");

                          __instance.Animator.Play("High Idle");

                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /* [HarmonyPatch(typeof(TacticalNavigationComponent), "WaitForAnimation")]
         public static class TFTV_TacticalNavigationComponent_WaitForAnimation
         {
             public static IEnumerable<NextUpdate> Postfix (TacticalNavigationComponent __instance, AnimationClip animation, IEnumerable<NextUpdate> results)
             {

                 foreach (NextUpdate nextUpdate in results)
                 {
                     TFTVLogger.Always($"{__instance.TacticalActor.name}: {animation.name}");
                     yield return nextUpdate;
                 }
             }
         }*/





















        /* 

         [HarmonyPatch(typeof(TacticalFactionVision), "IncrementKnownCounterToAll")]
         public static class TFTV_TacticalFactionVision_IncrementKnownCounterToAll
         {
             public static void Prefix(TacticalFactionVision __instance, TacticalActorBase actor, KnownState type, int counterValue, bool notifyChange)
             {
                 try
                 {
                     TFTVLogger.Always($"IncrementKnownCounterToAll run {actor.DisplayName}, {type}, {counterValue}, {notifyChange}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }
        */

        /* [HarmonyPatch(typeof(CaptureActorResearchRequirement), "IsValidUnit", typeof(GeoUnitDescriptor))]
         public static class TFTV_CaptureActorResearchRequirement_IsValidUnit
         {
             public static void Postfix(CaptureActorResearchRequirement __instance, GeoUnitDescriptor unit, bool __result)
             {
                 try
                 {
                     TFTVLogger.Always($"{__instance.CaptureRequirementDef.name} for {unit.GetName()}, valid? {__result}");


                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /* [HarmonyPatch(typeof(MoveAbility), "GetTargetDataFor")]
         public static class TFTV_MoveAbility_GetTargetDataFor
         {
             public static void Prefix(MoveAbility __instance, TacticalPathRequest pathRequest)
             {
                 try
                 {
                     TFTVLogger.Always($"MoveAbility.GetTargetDataFor {__instance.TacticalActor.DisplayName}, pathRequest null? {pathRequest==null}. " +
                         $"Controlled by player? {__instance.TacticalActor.IsControlledByPlayer}. Is vehicle? {__instance.TacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag)}");

                     if (pathRequest != null && __instance.TacticalActor.IsControlledByPlayer && __instance.TacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                     {
                         TacticalNavigationComponent component = __instance.TacticalActor.TacticalNav;
                         component.CurrentPath = component.CreatePathRequest();

                         TFTVLogger.Always($"Creating path request for {__instance.TacticalActor.DisplayName}");
                     }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*   bool IsValidActorForTag(TacticalActorDef actorDef, IEnumerable<TacticalItemDef> bodyparts, TacticalActorDef actorRequirement, GameTagDef tagRequirement)

          // CaptureActorResearchRequirement

                 [HarmonyPatch(typeof(ActorResearchRequirementDef), "GetDisabledStateText", typeof(GeoAbilityTarget))]
           public static class TFTV_ActorResearchRequirementDef_GetDisabledStateText
           {
               public static void Postfix(ActorResearchRequirementDef __instance, ref string __result)
               {
                   try
                   {
                       if (__instance.GeoAbility is LaunchBehemothMissionAbility)
                       {
                           __result = "Behemoth is submerged!";
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/


        /*    public void WireResearchEvents()
        {
            GameUtl.GameComponent<DefRepository>().GetAllDefs<ResearchDbDef>();
            IEnumerable<ResearchElement> completed = this.Completed;
            foreach (ResearchElement researchElement in this.AllResearchesArray.GetEnumerator<ResearchElement>())
            {
                bool flag = true;
                string[] invalidatedBy = researchElement.ResearchDef.InvalidatedBy;
                for (int i = 0; i < invalidatedBy.Length; i++)
                {
                    string invalidateID = invalidatedBy[i];
                    if (completed.Any((ResearchElement r) => string.Equals(r.ResearchID, invalidateID, StringComparison.OrdinalIgnoreCase)))
                    {
                        flag = false;
                        researchElement.State = ResearchState.Hidden;
                        break;
                    }
                }
                if (flag)
                {
                    researchElement.InitializeRequirements(researchElement.ResearchDef.RequirementsDefs);
                }
            }
        }*/













        /*   [HarmonyPatch(typeof(BreachEntrance), "OnAllPlotParcelsLoaded")]
           public static class TFTV_BreachEntrance_OnAllPlotParcelsLoaded_patch
           {

               public static void Prefix(BreachEntrance __instance, List<Transform> parcels, Transform myParcel, ref MapPlot plot)
               {
                   try
                   {                 
                       plot.RemainingBreachPoints = 1;
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/







        //.GetDamageModifierForDistance

        // AmbushOutcomeDataBind

        //GeoMission














        /* [HarmonyPatch(typeof(UIInventorySlotSideButton), "OnSideButtonPressed")]
         public static class UIInventorySlotSideButton_OnSideButtonPressed_patch
         {

             public static void Prefix(UIInventorySlotSideButton __instance, GeneralState ____currentState, UIModuleSoldierEquip ____parentModule, ItemDef ____itemToProduce)
             {
                 try
                 {
                     ICommonItem commonItem = null;
                     TFTVLogger.Always($"OnSideButtonPressed, current state {____currentState.State}, action {____currentState.Action}");

                     TFTVLogger.Always($"parent module Storage list is {____parentModule.StorageList} and the count of Unfiltered items is " +
                         $"{____parentModule.StorageList.UnfilteredItems.Count}");

                     TFTVLogger.Always($"item to produce {____itemToProduce.name}");

                     commonItem = ____parentModule.StorageList.UnfilteredItems.Where((ICommonItem item) => item.ItemDef == ____itemToProduce).FirstOrDefault().GetSingleItem();
                     TFTVLogger.Always($"common item null? {commonItem==null}");

                    // commonItem = _parentModule.StorageList.UnfilteredItems.Where((ICommonItem item) => item.ItemDef == _itemToProduce).FirstOrDefault().GetSingleItem();

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/

        /*  GeoPhoenixFaction

               private void OnMaxDiplomacyStateChanged(GeoFaction faction, GeoFaction towards, PartyDiplomacyState state, PartyDiplomacyState previousState)
          {
              PhoenixDiplomacySettings diplomacySetting = FactionDef.FactionsDiplomacySettings.FirstOrDefault((PhoenixDiplomacySettings s) => s.FactionDef == faction.Def.PPFactionDef);
              if (diplomacySetting != null)
              {
                  PhoenixDiplomacySettings.DiplomacyStateSettings diplomacyStateSettings = diplomacySetting.StateSettings.FirstOrDefault((PhoenixDiplomacySettings.DiplomacyStateSettings s) => s.State == diplomacySetting.StartingState);
                  if (diplomacyStateSettings?.Tag != null && !base.GameTags.Contains(diplomacyStateSettings.Tag))
                  {
                      AddTag(diplomacyStateSettings.Tag);
                  }
              }

              _level.AchievmentTracker.CheckDiplomacyProgress(state, faction);
          }


          protected void ShareResearchWithAllies(ResearchElement research)
          {
              if (!Def.CanShareResearch || !(research.ResearchDef.Faction == Def))
              {
                  return;
              }

              foreach (GeoFaction item in GetFactionsWithMinDiplomacyState(PartyDiplomacyState.Allied))
              {
                  if (item.Research != null && research.IsAvailableToFaction(item) && !item.Research.Completed.Any((ResearchElement r) => r.ResearchID == research.ResearchID))
                  {
                      item.Research.GiveResearch(research);
                      Research.SendOnResearchObtainedTelemetricsEvent(research.ResearchDef, item, "Diplomacy");
                  }
              }
          }


          */

        //  internal static Color purple = new Color32(149, 23, 151, 255);




        /*    [HarmonyPatch(typeof(TacCharacterDef), "ApplyPogression")]
            public static class GeoMission_ApplyPogression_patch
            {

                public static void Prefix(TacCharacterDef __instance, TacCharacterData data)
                {
                    try
                    {
                        TFTVLogger.Always($"looking at template {__instance.name}");

                        SpecializationDef specializationDef = GameUtl.GameComponent<DefRepository>().GetAllDefs<SpecializationDef>().FirstOrDefault((SpecializationDef s) => s.ClassTag == __instance.ClassTag);

                        TFTVLogger.Always($"specializationDef is {specializationDef.name}");

                        if (!(specializationDef == null))
                        {

                            List<TacticalAbilityDef> second = specializationDef.GetAbilitiesTillLevel(data.LevelProgression.Level).ToList();
                            TFTVLogger.Always($"second count is {second.Count}");
                            data.Abilites = data.Abilites.Concat(second).Distinct().ToArray();
                        }

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/
        /*  [HarmonyPatch(typeof(GeoMission), "DistributeActorDeploymentWeight")]
          public static class GeoMission_DistributeActorDeploymentWeight_patch
          {

              public static bool Prefix(GeoMission __instance, List<IDeployableUnit> units, List<MissionDeployParams> deployLimits, TacMissionTypeParticipantData.DeploymentRuleData deploymentRule, ref List<ActorDeployData> __result)
              {
                  try
                  {
                      TFTVLogger.Always($"Running DistributeActorDeploymentWeight");

                      Dictionary<ClassTagDef, List<IDeployableUnit>> dictionary = new Dictionary<ClassTagDef, List<IDeployableUnit>>();
                      Dictionary<ClassTagDef, float> dictionary2 = new Dictionary<ClassTagDef, float>();
                      foreach (IDeployableUnit unit2 in units)
                      {
                          if (unit2.ClassTag == null)
                          {
                              Debug.LogError($"Unit '{unit2}' does not have a ClassTagDef, skipping!");
                              continue;
                          }

                          foreach (ClassTagDef classTag in unit2.ClassTags)
                          {
                              if (!dictionary.ContainsKey(classTag))
                              {
                                  dictionary.Add(classTag, new List<IDeployableUnit>());
                              }
                              TFTVLogger.Always($"adding class {classTag.name}");
                              dictionary[classTag].Add(unit2);
                          }
                      }

                      TFTVLogger.Always($"got here; deployLimits count: {deployLimits.Count()}");

                      foreach (MissionDeployParams deployLimit in deployLimits)
                      {
                          ClassTagDef actorTag = deployLimit.Limit.ActorTag;

                          TFTVLogger.Always($"actorTag is {actorTag.name}");                  
                      }


                      foreach (MissionDeployParams deployLimit in deployLimits)
                      {
                          ClassTagDef actorTag = deployLimit.Limit.ActorTag;

                          TFTVLogger.Always($"actorTag is {actorTag.name}");

                          dictionary.TryGetValue(actorTag, out var value);
                          int num = value?.Count ?? 1;
                          dictionary2.Add(actorTag, deployLimit.Weight / (float)num);
                      }

                      TFTVLogger.Always($"got here2");
                      List<ActorDeployData> list = new List<ActorDeployData>();
                      foreach (IDeployableUnit unit in units)
                      {
                          float num2 = 0f;
                          foreach (ClassTagDef classTag2 in unit.ClassTags)
                          {
                              dictionary2.TryGetValue(classTag2, out var value2);
                              num2 = Mathf.Max(num2, value2);
                          }

                          ActorDeployData actorDeployData = unit.GenerateActorDeployData();
                          TacMissionTypeParticipantData.DeploymentRuleData.UnitDeploymentOverride unitDeploymentOverride = deploymentRule.OverrideUnitDeployment.FirstOrDefault((TacMissionTypeParticipantData.DeploymentRuleData.UnitDeploymentOverride t) => unit.ClassTags.Contains(t.ClassTag));
                          if (unitDeploymentOverride != null)
                          {
                              actorDeployData.DeployCost = unitDeploymentOverride.OverrideDeployment;
                          }

                          if (!actorDeployData.Unique)
                          {
                              actorDeployData.ChanceWeight = num2;
                          }

                          list.Add(actorDeployData);
                      }

                      __result = list;

                      TFTVLogger.Always($"result count: {__result.Count}");

                      return false;


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /*  [HarmonyPatch(typeof(EncounterVarResearchReward), "GiveReward")]
          public static class EncounterVarResearchReward_GiveReward_patch
          {

              public static void Postfix(EncounterVarResearchReward __instance, GeoFaction faction)
              {
                  try
                  {
                      EncounterVarResearchRewardDef def = __instance.BaseDef as EncounterVarResearchRewardDef;

                      TFTVLogger.Always($"{def.name} {def.VariableName} {faction.Name.Localize()}");
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/












        /*   [HarmonyPatch(typeof(GeoPhoenixpedia), "AddItemEntry")]
           public static class GeoPhoenixpedia_ProcessGeoscapeInstanceData_patch
           {

               public static void Prefix(GeoPhoenixpedia __instance, ItemDef item)
               {
                   try
                   {
                       TFTVLogger.Always($"Running GeoPhoenixpedia.AddItemEntry");
                       TFTVLogger.Always($"{item}");


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/







        //   









        /*   [HarmonyPatch(typeof(EnterBaseAbility), "GetTargetDisabledStateInternal")]
           public static class EnterBaseAbility_GetValidTargets_patch
           {

               public static void Postfix(EnterBaseAbility __instance, GeoAbilityTarget target, GeoAbilityTargetDisabledState __result)
               {
                   try
                   {
                       TFTVLogger.Always($"GetTargetDisabledStateInternal for ability {__instance.GeoscapeAbilityDef.name}");


                       if (__result==GeoAbilityTargetDisabledState.NotDisabled && target.Actor is GeoSite site && site.ActiveMission != null && site.CharactersCount > 0)
                       {
                           UIModuleSiteContextualMenu uIModuleSiteContextualMenu = __instance.GeoLevel.View.GeoscapeModules.SiteContextualMenuModule;


                           FieldInfo fieldInfoListSiteContextualMenuItem = typeof(UIModuleSiteContextualMenu).GetField("_menuItems", BindingFlags.NonPublic | BindingFlags.Instance);
                           List<SiteContextualMenuItem> menuItems = fieldInfoListSiteContextualMenuItem.GetValue(uIModuleSiteContextualMenu) as List<SiteContextualMenuItem>;

                           foreach(SiteContextualMenuItem menuItem in menuItems) 
                           { 
                           if(menuItem.ItemText.text == __instance.View.ViewElementDef.DisplayName1.Localize()) 
                               {
                                   menuItem.ItemText.text = "DEPLOY TO DEFEND BASE";


                               }

                           }


                       }


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/





        /*  public static int KludgeStartingWeight = 0;
          public static int KludgeCurrentWeight = 0;


          [HarmonyPatch(typeof(UIStateInventory), "RefreshUI")]
          public static class UIStateInventory_RefreshUI_patch
          {
              public static void Postfix(UIStateInventory __instance, TacticalActor ____secondaryActor)
              {
                  try
                  {
                      TFTVLogger.Always($"RefreshUI");

                      MethodInfo refreshCostMessageMethod = typeof(UIStateInventory).GetMethod("RefreshCostMessage", BindingFlags.Instance | BindingFlags.NonPublic);

                      ApplyStatusAbilityDef rfAAbility = DefCache.GetDef<ApplyStatusAbilityDef>("ReadyForAction_AbilityDef");
                      ChangeAbilitiesCostStatusDef rfAStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_ReadyForActionStatus [ReadyForAction_AbilityDef]");

                      if (KludgeStartingWeight == KludgeCurrentWeight && ____secondaryActor!=null)
                      {                       
                          if (__instance.PrimaryActor.GetAbilityWithDef<ApplyStatusAbility>(rfAAbility) != null && !__instance.PrimaryActor.HasStatus(rfAStatus))
                          {
                              __instance.PrimaryActor.Status.ApplyStatus(rfAStatus);
                              TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA ability but is missing the status, personal inventory case");
                              refreshCostMessageMethod.Invoke(__instance, null);

                          }
                      }
                      else if(KludgeStartingWeight > KludgeCurrentWeight && ____secondaryActor != null && __instance.PrimaryActor.HasStatus(rfAStatus)) 
                      {

                          __instance.PrimaryActor.Status.UnapplyStatus(__instance.PrimaryActor.Status.GetStatusByName(rfAStatus.EffectName));
                          TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA status, but is taking items away from inventory");
                          refreshCostMessageMethod.Invoke(__instance, null);

                      }


                      //  TFTVLogger.Always($"KludgeCurrentWeight is {KludgeCurrentWeight}, will set to 0");
                      //  KludgeCurrentWeight = 0;
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }
        */


        /*    [HarmonyPatch(typeof(UIStateInventory), "InitInventory")]
            public static class UIStateInventory_InitInventory_patch
            {

                public static bool Prefix(UIStateInventory __instance, TacticalActor ____secondaryActor)
                {
                    try
                    {
                        if (____secondaryActor == KludgeActor)
                        {

                            TFTVLogger.Always($"InitInventory prefix {____secondaryActor?.DisplayName}");

                            return false;
                        }
                        else
                        {

                            return true;

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(UIStateInventory), "InitVehicleInventory")]
            public static class UIStateInventory_InitVehicleInventory_patch
            {

                public static bool Prefix(UIStateInventory __instance, TacticalActor ____secondaryActor)
                {
                    try
                    {
                        if (____secondaryActor == KludgeActor)
                        {

                            TFTVLogger.Always($"InitVehicleInventory prefix {____secondaryActor?.DisplayName}");

                            return false;
                        }
                        else 
                        {

                            return true;

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/

        /*   [HarmonyPatch(typeof(UIStateInventory), "ExitState")]
           public static class UIStateInventory_RefreshCostMessage_patch
           {
               public static void Postfix(UIStateInventory __instance)
               {
                   try
                   {
                      // TFTVLogger.Always($"Exit State");
                       ApplyStatusAbilityDef rfAAbility = DefCache.GetDef<ApplyStatusAbilityDef>("ReadyForAction_AbilityDef");
                       ChangeAbilitiesCostStatusDef rfAStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_ReadyForActionStatus [ReadyForAction_AbilityDef]");

                       if (__instance.PrimaryActor.GetAbilityWithDef<ApplyStatusAbility>(rfAAbility) != null && !__instance.PrimaryActor.HasStatus(rfAStatus))
                       {
                           __instance.PrimaryActor.Status.ApplyStatus(rfAStatus);
                           TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA ability but is missing the status");
                       }

                    //   KludgeCurrentWeight = 0;
                    //   KludgeStartingWeight = 0;

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*  [HarmonyPatch(typeof(UIModuleSoldierEquip), "GetPrimaryWeight")]
          public static class UIModuleSoldierEquip_GetPrimaryWeight_patch
          {
              public static void Postfix(UIModuleSoldierEquip __instance, int __result)
              {
                  try

                  {
                      TFTVLogger.Always($"GetPrimaryWeight");
                      TFTVLogger.Always($"kludgeWeight is {KludgeStartingWeight} and result is {__result}");

                      if (KludgeStartingWeight != 0)
                      {
                          KludgeCurrentWeight = __result;
                          TFTVLogger.Always($"setting from GetPrimaryWeight KludgeCurrentWeight to {KludgeCurrentWeight}");
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/




        /*
        [HarmonyPatch(typeof(UIStateInventory), "EnterState")]
         public static class UIStateInventory_EnterState_patch
         {
             public static void Postfix(UIStateInventory __instance, TacticalActor ____secondaryActor, InventoryComponent ____groundInventory, bool ____isSecondaryVehicleInventory)
             {
                 try
                 {

                     TFTVLogger.Always($"primary actor is {__instance.PrimaryActor.DisplayName}, groundInventory actor is {____groundInventory?.Actor?.name}, is secondary vehicle inventory: {____isSecondaryVehicleInventory}");

                     if(____groundInventory.Actor is TacticalActor actor && actor == ____secondaryActor) 
                     {
                        MethodInfo methodCreateGroundInventory = typeof(UIStateInventory).GetMethod("CreateGroundInventory", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodResetInventoryQueries = typeof(UIStateInventory).GetMethod("ResetInventoryQueries", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodRefreshStorageLabel = typeof(UIStateInventory).GetMethod("RefreshStorageLabel", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodInitInitialItems = typeof(UIStateInventory).GetMethod("InitInitialItems", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodSetupGroundMarkers = typeof(UIStateInventory).GetMethod("SetupGroundMarkers", BindingFlags.Instance | BindingFlags.NonPublic);


                        ____groundInventory = (InventoryComponent)methodCreateGroundInventory.Invoke(__instance, null);

                        methodResetInventoryQueries.Invoke(__instance, null);
                        methodSetupGroundMarkers.Invoke(__instance, null);
                        methodRefreshStorageLabel.Invoke(__instance, null);
                        methodInitInitialItems.Invoke(__instance, null);


                        //
                        // __instance.ResetInventoryQueries();
                        TFTVLogger.Always($"ground inventory set active to false");

                     }

                     TFTVLogger.Always($"{__instance.PrimaryActor.GetAbility<InventoryAbility>()?.TacticalAbilityDef?.name}");

                     foreach (TacticalAbilityTarget target in __instance.PrimaryActor.GetAbility<InventoryAbility>().GetTargets())
                     {
                         InventoryComponent inventoryComponent = target.InventoryComponent;

                         TFTVLogger.Always($"inventory component {inventoryComponent?.name}");

                         if (inventoryComponent.GetType() != typeof(EquipmentComponent))
                         {
                             TFTVLogger.Always($" {inventoryComponent?.name} is no equipmentComponent");

                             TacticalActor tacticalActor = inventoryComponent.Actor as TacticalActor;
                             if (tacticalActor != null && TacUtil.CanTradeWith(__instance.PrimaryActor, tacticalActor))
                             {
                                 TFTVLogger.Always($"{__instance.PrimaryActor.DisplayName} can trade with {tacticalActor.DisplayName}");
                                 InventoryAbility ability = tacticalActor.GetAbility<InventoryAbility>();
                                 if (ability != null && !(ability.GetDisabledState() != AbilityDisabledState.NotDisabled))
                                 {
                                     TFTVLogger.Always($"{tacticalActor.DisplayName} inventory ability is not null");
                                 }
                             }


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
        */











        /*   [HarmonyPatch(typeof(UIModuleWeaponSelection), "HandleEquipments")]
            public static class UIModuleWeaponSelection_HandleEquipments_patch
           {
               public static void Postfix(UIModuleWeaponSelection __instance, Equipment ____selectedEquipment)
               {
                   try
                   {
                       EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");

                       if (____selectedEquipment.EquipmentDef==repairKit) 
                       {
                           TFTVLogger.Always("");
                           __instance.DamageTypeVisualsTemplate.DamageTypeIcon.gameObject.SetActive(false);
                           __instance.DamageTypeVisualsTemplate.DamageText.gameObject.SetActive(false);


                       }



                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/





        // UIModuleAbilities




        /* [HarmonyPatch(typeof(TacticalActorBase), "GetDamageMultiplierFor")]
         public static class TacticalActorBase_GetDamageMultiplierFor_patch
         {
             public static void Postfix(TacticalActorBase __instance, ref float __result, DamageTypeBaseEffectDef damageType)
             {
                 try
                 {
                     AcidDamageTypeEffectDef acidDamageTypeEffectDef = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                     if (damageType == acidDamageTypeEffectDef)
                     {
                         TFTVLogger.Always($"GetDamageMultiplierFor  {__instance.name} and result is {__result}");
                         __result = 1;
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetPureDamageBonusFor")]
          public static class DamageAccumulation_GetPureDamageBonusFor_patch
          {
              public static void Postfix(DamageAccumulation __instance, IDamageReceiver target, float __result)
              {
                  try
                  {
                      if (__result != 0)
                      {

                          TFTVLogger.Always($"GetPureDamageBonusFor {target.GetDisplayName()}, result is {__result}");
                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /*     [HarmonyPatch(typeof(AddStatusDamageKeywordData), "ProcessKeywordDataInternal")]
          public static class AddStatusDamageKeywordData_ProcessKeywordDataInternal_Patch
          {
              public static void Postfix(AddStatusDamageKeywordData __instance, DamageAccumulation.TargetData data)
              {
                  try
                  {
                      if (__instance.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                      {
                          TFTVLogger.Always($"target {data.Target.GetSlotName()}");

                          if (data.Target is ItemSlot)
                          {

                              ItemSlot itemSlot = (ItemSlot) data.Target;

                              if (itemSlot.DisplayName == "LEG")
                              {
                                  TacticalActor tacticalActor = data.Target.GetActor() as TacticalActor;

                                  itemSlot = tacticalActor.BodyState.GetSlot("Legs");
                                  TFTVLogger.Always($"itemslot name now {itemSlot.GetSlotName()}");
                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }
                              else
                              {
                                  TFTVLogger.Always($"target {data.Target.GetSlotName()} is itemslot {itemSlot.DisplayName}");

                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }

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

          */




        /*   [HarmonyPatch(typeof(DamageKeyword), "AddKeywordStatus")]
             public static class DamageOverTimeResistanceStatus_ApplyResistance_Patch
             {
                 public static void Postfix(IDamageReceiver recv, DamageAccumulation.TargetData data, StatusDef statusDef, int value, object customStatusTarget = null)
                 {
                     try
                     {


                       TFTVLogger.Always($"AddKeywordStatus value {value}");


                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }
             }*/




        /* [HarmonyPatch(typeof(DamageAccumulation), "AddTargetStatus")]
         public static class DamageAccumulation_AddTargetStatus_Patch
         {
             public static void Prefix(DamageAccumulation __instance, StatusDef statusDef, int tacStatusValue, IDamageReceiver target)
             {
                 try
                 {


                     if (statusDef == DefCache.GetDef<AcidStatusDef>("Acid_StatusDef"))
                     {




                         TFTVLogger.Always($"tacstatusvalue is {tacStatusValue}");
                     }


                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*  [HarmonyPatch(typeof(DamageOverTimeStatus), "GetDamageMultiplier")]
          public static class DamageOverTimeStatus_GetDamageMultiplier_Patch
          {
              public static void Postfix(DamageOverTimeStatus __instance, ref float __result)
              {
                  try
                  {
                      TFTVLogger.Always($"GetDamageMultiplier for {__instance.DamageOverTimeStatusDef.name} and result is {__result}");

                      AcidStatusDef acidDamage = DefCache.GetDef<AcidStatusDef>("Acid_StatusDef");

                      if (__instance.DamageOverTimeStatusDef == acidDamage) 
                      {
                          TFTVLogger.Always($"dot status acid {__result}");
                          __result = 1;
                          TFTVLogger.Always($"new dot status acid {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetSourceDamageMultiplier")]
          public static class DamageAccumulation_GetSourceDamageMultiplier_Patch
          {
              public static void Postfix(DamageAccumulation __instance, DamageTypeBaseEffectDef damageType, float __result)
              {
                  try
                  {
                      if (!damageType.name.Equals("Projectile_StandardDamageTypeEffectDef"))
                          {

                          TFTVLogger.Always($"source actor {__instance?.SourceActor?.name} damageType is {damageType.name} and multiplier is {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /* [HarmonyPatch(typeof(Equipment), "SetActive")]
         public static class Equipment_RemoveAbilitiesFromSource_Patch
         {
             public static void Postfix(Equipment __instance, bool active)
             {
                 try
                 {
                     TFTVLogger.Always($"equipment is {__instance.DisplayName}, and is it active? {active}");




                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*   [HarmonyPatch(typeof(TacticalItem), "RemoveAbilitiesFromActor")]
           public static class TacticalItem_RemoveAbilitiesFromActor_patch
           {
               public static void Prefix(TacticalItem __instance)
               {
                   try
                   {
                       TFTVLogger.Always($"RemoveAbilitiesFromActor from item {__instance.ItemDef.name}");


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*   [HarmonyPatch(typeof(ActorComponent), "RemoveAbilitiesFromSource")]
           public static class ActorComponent_RemoveAbilitiesFromSource_patch
           {
               public static void Prefix(ActorComponent __instance, object source)
               {
                   try
                   {
                       TFTVLogger.Always($"RemoveAbilitiesFromSource from {__instance.name} with source {source}");

                       foreach (Ability item in __instance.GetAbilities<Ability>().Where((Ability a) => a.Source == source).ToList())
                       {
                           TFTVLogger.Always($"ability is {item.AbilityDef.name} and it's source is {item.Source}, while parameter source is {source}");
                       }

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*
                [HarmonyPatch(typeof(SlotStateStatus), "SetAbilitiesState")]
                public static class SlotStateStatus_GetDamageMultiplierFor_patch
                {
                    public static void Prefix(SlotStateStatus __instance, ItemSlot ____targetSlot)
                    {
                        try
                        {
                            TFTVLogger.Always($"Gets at least to here {__instance.Source}");

                            foreach (TacticalItem allDirectItem in ____targetSlot.GetAllDirectItems(onlyBodyparts: true))
                            {
                                if (__instance.SlotStateStatusDef.BodypartsEnabled && !allDirectItem.Enabled)
                                {
                                    TFTVLogger.Always($"landed here: looking at {allDirectItem.ItemDef.name}");
                                }
                                else if (!__instance.SlotStateStatusDef.BodypartsEnabled && allDirectItem.Enabled)
                                {
                                    TFTVLogger.Always($"landed in the else if: looking at {allDirectItem.ItemDef.name}");
                                }


                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }*/

        /*    [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbilityDef")]
            public static class AbilitySummaryData_ProcessHealAbilityDef_Patch
            {
                public static void Postfix(AbilitySummaryData __instance, HealAbilityDef healAbilityDef)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbilityDef running");
                        if ((bool)healAbilityDef.GeneralHealSummary && healAbilityDef.GeneralHealAmount > 0f)
                        {
                            TFTVLogger.Always($"{healAbilityDef.GeneralHealSummary} and {healAbilityDef.GeneralHealAmount}");

                        }

                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbility")]
            public static class AbilitySummaryData_ProcessHealAbility_Patch
            {
                public static void Prefix(AbilitySummaryData __instance, HealAbility healAbility)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbility running");
                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                        if (__instance.Keywords.Count() > 0)
                        {
                            KeywordData keywordData = __instance.Keywords.First((KeywordData kd) => kd.Id == "GeneralHeal");

                            if (keywordData == null)
                            {
                                TFTVLogger.Always("somehow null!");
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
        */


        /*   internal virtual void TriggerHurt(DamageResult damageResult)
           {
               var hurtReactionAbility = GetAbility<TacticalHurtReactionAbility>();
               if (IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
               {
                   return;
               }

               bool useModFlinching = true; // Use a global flag for the mod 
               if (useModFlinching && _ragdollDummy != null && _ragdollDummy.CanFlinch)
               {
                   DoTriggerHurt(damageResult, damageResult.forceHurt);
                   return;
               }

               _pendingHurtDamage = damageResult;
               if (_waitingForHurtReactionCrt == null || _waitingForHurtReactionCrt.Stopped)
               {
                   _waitingForHurtReactionCrt = Timing.Start(PollForPendingHurtReaction(damageResult.forceHurt));
               }
           }*/


        /*
        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static bool Prefix(TacticalActor __instance, DamageResult damageResult, RagdollDummy ____ragdollDummy, IUpdateable ____waitingForHurtReactionCrt,
                DamageResult ____pendingHurtDamage)
            {
                try
                {


                    MethodInfo doTriggerHurtMethod = typeof(TacticalActor).GetMethod("DoTriggerHurt", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo pollForPendingHurtReaction = typeof(TacticalActor).GetMethod("PollForPendingHurtReaction", BindingFlags.NonPublic | BindingFlags.Instance); 



                 var hurtReactionAbility = __instance.GetAbility<TacticalHurtReactionAbility>();



                    if (__instance.IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
                    {
                        TFTVLogger.Always("Early exit triggers");
                        return true;
                    }

                    bool useModFlinching = true; // Use a global flag for the mod 
                    if (useModFlinching && ____ragdollDummy != null && ____ragdollDummy.CanFlinch)
                    {
                        doTriggerHurtMethod.Invoke(__instance, new object[] { damageResult, damageResult.forceHurt });
                        TFTVLogger.Always("Takes to do trigger hurt method");

                        return false;
                    }

                    ____pendingHurtDamage = damageResult;
                    if (____waitingForHurtReactionCrt == null || ____waitingForHurtReactionCrt.Stopped)
                    {
                        TFTVLogger.Always("waiting for hurt reaction or it is stopped");
                        object[] parameters = new object[] { damageResult.forceHurt };
                        //Timing timingInstance = new Timing();
                        ____waitingForHurtReactionCrt = __instance.Timing.Start((IEnumerator<NextUpdate>)pollForPendingHurtReaction.Invoke(__instance, parameters));

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




        [HarmonyPatch(typeof(TacticalActor), "SetFlinchingEnabled")]
        public static class TacticalActor_AddFlinch_Patch
        {
            public static void Postfix(TacticalActor __instance, ref RagdollDummy ____ragdollDummy)
            {
                try
                {
                    TFTVLogger.Always($"SetFlinchingEnabled invoked");
                    ____ragdollDummy.SetFlinchingEnabled(true);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(RagdollDummy), "AddFlinch")]
        public static class RagdollDummy_AddFlinch_Patch
        {
            public static void Prefix(RagdollDummy __instance, float ____ragdollBlendTimeTotal)
            {
                try
                {
                    TFTVLogger.Always($"AddFlinch invoked prefix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            public static void Postfix(RagdollDummy __instance, float ____ragdollBlendTimeTotal, Vector3 force, CastHit hit)
            {
                try
                {
                    RagdollDummyDef ragdollDummyDef = DefCache.GetDef<RagdollDummyDef>("Generic_RagdollDummyDef");
                    TFTVLogger.Always($"AddFlinch invoked postfix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}. original force is {force}, the hit body part is {hit.Collider?.attachedRigidbody?.name}" +
                        $" mass is {hit.Collider?.attachedRigidbody?.mass}, force applied on first hit is {force*ragdollDummyDef.FlinchForceMultiplier}");







                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(RagdollDummy), "get_CanFlinch")]
        public static class RagdollDummy_SetFlinchingEnabled_Patch
        {
            public static void Postfix(RagdollDummy __instance, ref bool __result)
            {
                try
                {
                    TFTVLogger.Always($"get_CanFlinch invoked for {__instance?.Actor?.name} and result is {__result}");

                    __result = true;

                    TFTVLogger.Always($"And now result is {__result}");




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
        */




        /*private bool OnProjectileHit(CastHit hit, Vector3 dir)
        {
            if (hit.Collider.gameObject.CompareTag("WindowPane"))
            {
                return false;
            }

            if (Projectile != null)
            {
                Projectile.OnProjectileHit(hit);
            }

            AffectTarget(hit, dir);
            if (DamagePayload.StopOnFirstHit)
            {
                return true;
            }

            if (DamagePayload.StopWhenNoRemainingDamage)
            {
                DamageAccumulation damageAccum = _damageAccum;
                return damageAccum == null || !damageAccum.HasRemainingDamage;
            }

            _damageAccum?.ResetToInitalAmount();
            return false;
        }*/







        public static Vector3 FindPushToTile(TacticalActor attacker, TacticalActor defender, int numTiles)
        {

            try
            {


                Vector3 diff = defender.Pos - attacker.Pos;
                Vector3 pushToPosition = defender.Pos + numTiles * diff.normalized;

                // TFTVLogger.Always($"attacker position is {attacker.Pos} and defender position is {defender.Pos}, so difference is {diff} and pushtoposition is {pushToPosition}");



                return pushToPosition;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        /*  [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

          public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
          {
              public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {
                              TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                              if (tacticalActor != null)
                              {
                                  tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                     TFTVLogger.Always($", added {knockBackAbility.name} to {tacticalActor.name}");
                              }
                          }
                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

              public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                             TFTVLogger.Always($", ability is {ability.TacticalAbilityDef.name}");

                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {

                              TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                              if (tacticalActor != null && tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                              {
                                  RepositionAbility knockBack = tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility);

                                  IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                  TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                  TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                  foreach (TacticalAbilityTarget target in targets)
                                  {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                      if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 2)).magnitude <= 1f)
                                      {
                                          TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                          pushPosition = target;

                                      }
                                  }


                                  //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                  //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                  //  moveAbility.Activate(pushPosition);

                                  knockBack.Activate(pushPosition);



                                  TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                              }
                          }
                      }

                      if (ability.TacticalAbilityDef == knockBackAbility)
                      {
                          __instance.RemoveAbility(ability);

                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

          }

          */


        /* [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

           public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
           {
               public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {
                      // TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                       if (ability.TacticalAbilityDef!=null && ability.TacticalAbilityDef == strikeAbility)
                       {
                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                           //    TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                               if (tacticalActor != null)
                               {
                                   tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                //   TFTVLogger.Always($", added {knockBackAbility.name} to {tacticalActor.name}");
                               }
                           }
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

               public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {


                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                       if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                       {
                        //   TFTVLogger.Always($", ability is {ability.TacticalAbilityDef.name}");

                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                              // TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                               if (tacticalActor != null && tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                               {
                                   JetJumpAbility knockBack = tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility);

                                   IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                   TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                   TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                   foreach (TacticalAbilityTarget target in targets)  
                                   {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                       if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 1)).magnitude <= 1f) 
                                       {
                                           TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                           pushPosition = target;

                                       }
                                   }


                                   //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                   //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                   //  moveAbility.Activate(pushPosition);

                                   knockBack.Activate(pushPosition);



                                   TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                               }
                           }
                       }

                       if (ability.TacticalAbilityDef == knockBackAbility)
                       {
                           __instance.RemoveAbility(ability);

                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

           }


        */
    }
}













