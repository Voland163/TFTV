using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.GameOverConditions;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Player-chosen deployment (config option PlayerDeploymentPhase): hold back every non-player participant at
    /// mission start and let the player reposition the squad inside its deploy zones first (placement and the
    /// placement-only ability lock live in TFTVPlayerDeploymentPlacement.cs).
    ///
    /// Vanilla spawns every participant in one go inside TacMission.OnLevelStart, and the later sides' spawn rules
    /// (InitialEnemyClearanceRange, the initial LOS checks) avoid whoever is already on the map. Here only the
    /// Phoenix squad spawns at level start; enemies, residents and the environment wait until the player confirms
    /// with the End Turn button (relabelled DEPLOY), so they spawn around the squad's positions at that moment.
    /// Confirming does not end the turn: Phoenix's first turn simply carries on with everyone on the map.
    ///
    /// While held:
    /// - held factions report undeployed actors, so kill-all objectives are set up for them, the wipe objective
    ///   stays in progress and their turns are not skipped;
    /// - every objective is frozen at its current state and game-over evaluation is suspended - with the targets
    ///   not spawned a kill objective reads as achieved, and an achieved objective sticks;
    /// - saving is disabled (a save would store a half-spawned mission);
    /// - TFTV's own OnTacticalStart and OnNewTurn wait, then run in vanilla's order after the release.
    /// If the turn passes to any other faction before the key is pressed, the held sides are released first.
    ///
    /// Per-mission state is tied to the level controller, so a new level never sees a previous mission's hold.
    /// </summary>
    internal static partial class TFTVPlayerDeploymentPhase
    {
        private sealed class PendingDeployment
        {
            public TacticalLevelController Controller;
            public readonly List<TacParticipantSpawn> HeldSpawns = new List<TacParticipantSpawn>();
            public bool SaveWasEnabled = true;

            // The game-wide save manager the phase switched off; it outlives the level, so the teardown can still
            // reach it when the controller is going away.
            public PhoenixSaveManager SaveManager;
            public TFTVTactical DeferredMod;
            public bool TacticalStartDeferred;
            public int? DeferredTurnNumber;

            // The player's spawn and each soldier's deploy entry, captured as they spawn: vanilla deletes unique
            // entries once spawned, and placement needs them to ask the zones where that soldier may stand.
            public TacParticipantSpawn PlayerSpawn;
            public readonly Dictionary<TacticalActor, ActorDeployData> SquadDeployData = new Dictionary<TacticalActor, ActorDeployData>();

            // Squad members not on the map: the ones vanilla could not place, and any the player benched. Their
            // entries stay in the player spawn's own queue while held, so DoSpawnActor can bring them in; at the
            // release they are taken out of it, so they sit the mission out rather than arriving later.
            public readonly List<ActorDeployData> Reserve = new List<ActorDeployData>();

            // Reserve cards: a copy of each unit's squad-bar card, taken before it left the map (inactive templates).
            public readonly Dictionary<ActorDeployData, GameObject> Cards = new Dictionary<ActorDeployData, GameObject>();

            // Base defense security guards under Phoenix control: placeable like the squad, but never benched -
            // they are not part of the roster and could not be brought back.
            public readonly HashSet<TacticalActor> Guards = new HashSet<TacticalActor>();
            public bool GuardsRequested;

            // The reserve tool in hand (see TFTVPlayerDeploymentReserve.cs): -1 none, an index into Reserve, or
            // Reserve.Count for bench.
            public int Armed = -1;

            // Set while the held spawns deploy: the hold (objective freeze, undeployed actors) stays on until
            // every held side is on the map, since spawning raises events that re-check objectives.
            public bool Releasing;
        }

        private static PendingDeployment _pending;

        // True only while TacMission.OnLevelStart runs for a mission that will hold its other participants.
        private static bool _holdingDuringLevelStart;

        private static readonly AccessTools.FieldRef<GameOverCondition, TacticalLevelController> GameOverConditionController =
            AccessTools.FieldRefAccess<GameOverCondition, TacticalLevelController>("_tacticalLevelController");

        internal static bool IsPending(TacticalLevelController controller)
        {
            return _pending != null && controller != null && _pending.Controller == controller;
        }

        private static bool IsHeld(TacticalFaction faction)
        {
            return faction != null && IsPending(faction.TacticalLevel) && _pending.HeldSpawns.Any(s => s.TacticalFaction == faction);
        }

        private static bool IsEligibleMission(TacMission mission)
        {
            try
            {
                TacMissionTypeDef missionType = mission.MissionData?.MissionType;

                if (missionType == null || mission.TacticalLevel == null)
                {
                    return false;
                }

                // The tutorials are a scripted sequence that drives the squad itself. Every other mission, base
                // defense and the Palace included, gets the phase: their scripted setup is TFTV's own turn-0 and
                // tactical-start work, which the phase defers until after the release, and their per-team zone
                // rules come through GetEligibleDeployZones, which placement uses.
                return !missionType.name.Contains("Tutorial");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static void BeginPhase(TacticalLevelController controller)
        {
            try
            {
                // First, so the phase always gets its prompt and DEPLOY button even if anything below fails.
                controller.gameObject.AddComponent<DeploymentPhaseListener>().Controller = controller;

                foreach (TacParticipantSpawn spawn in _pending.HeldSpawns)
                {
                    TFTVLogger.Always($"[DeploymentPhase] holding back {spawn.ParticipantKind} ({spawn.TacticalFaction?.TacticalFactionDef?.name})");
                }

                SecurityGuardsSpawnedDuringDeployment = false;
                ResetPlacementCaches();

                _pending.SaveManager = controller.GameController.SaveManager;
                _pending.SaveWasEnabled = _pending.SaveManager.IsSaveEnabled;
                _pending.SaveManager.IsSaveEnabled = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Called from TFTVTactical.OnTacticalStart. True when the call was deferred until the release.
        /// </summary>
        internal static bool TryDeferTacticalStart(TFTVTactical mod, TacticalLevelController controller)
        {
            if (!IsPending(controller))
            {
                return false;
            }

            _pending.DeferredMod = mod;
            _pending.TacticalStartDeferred = true;

            // The level may have re-enabled saving since BeginPhase; if so remember that and hold it off again.
            // Never read a false here as the original: BeginPhase is what set it.
            if (_pending.SaveManager == null)
            {
                _pending.SaveManager = controller.GameController.SaveManager;
            }

            _pending.SaveWasEnabled |= _pending.SaveManager.IsSaveEnabled;
            _pending.SaveManager.IsSaveEnabled = false;

            TFTVLogger.Always($"[DeploymentPhase] OnTacticalStart deferred until deployment is confirmed");
            return true;
        }

        /// <summary>
        /// Called from TFTVTactical.OnNewTurn. True when the call was deferred until the release.
        /// </summary>
        internal static bool TryDeferNewTurn(TFTVTactical mod, TacticalLevelController controller, int turnNumber)
        {
            if (!IsPending(controller))
            {
                return false;
            }

            _pending.DeferredMod = mod;
            _pending.DeferredTurnNumber = turnNumber;
            TFTVLogger.Always($"[DeploymentPhase] OnNewTurn({turnNumber}) for {controller.CurrentFaction?.TacticalFactionDef?.name} deferred until deployment is confirmed");
            return true;
        }

        internal static void Release(TacticalLevelController controller, string reason)
        {
            if (!IsPending(controller) || _pending.Releasing)
            {
                return;
            }

            PendingDeployment pending = _pending;
            pending.Releasing = true;

            try
            {
                TFTVLogger.Always($"[DeploymentPhase] releasing held participants ({reason}); restoring saving enabled = {pending.SaveWasEnabled}");

                ClearMarkers();
                RestoreEndTurnButton();
                CloseReserve(pending);

                try
                {
                    foreach (TacParticipantSpawn spawn in pending.HeldSpawns)
                    {
                        int before = spawn.TacticalFaction.TacticalActors.Count();
                        spawn.DeployForTurn(0);
                        int after = spawn.TacticalFaction.TacticalActors.Count();
                        TFTVLogger.Always($"[DeploymentPhase] {spawn.ParticipantKind} ({spawn.TacticalFaction.TacticalFactionDef.name}) spawned {after - before} actor(s); {DescribeSpawnRules(spawn)}");
                    }
                }
                finally
                {
                    // Only now, with every side on the map, may objectives be judged again.
                    _pending = null;
                    RestoreSaving(pending);
                    ResetPlacementCaches();
                }

                LogEnemyPlacement(controller);
                LogObjectiveStates(controller);

                if (pending.DeferredMod != null)
                {
                    if (pending.TacticalStartDeferred)
                    {
                        pending.DeferredMod.OnTacticalStart();
                    }

                    if (pending.DeferredTurnNumber.HasValue)
                    {
                        pending.DeferredMod.OnNewTurn(pending.DeferredTurnNumber.Value);
                    }
                }

                // Rebuild the objectives panel with everyone on the map: entering the character-selected state runs
                // UIModuleObjectives.Init, which evaluates and redraws. A frame later, since this can run from inside
                // that state's own End Turn handling.
                controller.StartCoroutine(ResetViewStateNextFrame(controller));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Gives the save manager back the state the phase found it in. Every way out of the phase - DEPLOY, and
        /// the level going away before it - comes through here.
        /// </summary>
        private static void RestoreSaving(PendingDeployment pending)
        {
            try
            {
                if (pending?.SaveManager != null)
                {
                    pending.SaveManager.IsSaveEnabled = pending.SaveWasEnabled;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static System.Collections.IEnumerator ResetViewStateNextFrame(TacticalLevelController controller)
        {
            yield return null;

            try
            {
                if (controller != null)
                {
                    controller.View.ResetViewState();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string DescribeSpawnRules(TacParticipantSpawn spawn)
        {
            try
            {
                Traverse rules = Traverse.Create(spawn).Property("Rules");
                return $"InitialEnemyClearanceRange {rules.Field("InitialEnemyClearanceRange").GetValue<float>()}, " +
                    $"DoInitialLOSChecks {rules.Field("DoInitialLOSChecks").GetValue<bool>()}";
            }
            catch (Exception e)
            {
                return $"rules unreadable ({e.Message})";
            }
        }

        /// <summary>
        /// For checking the prototype: how close each newly spawned hostile is to the squad, and whether the
        /// squad can already see it.
        /// </summary>
        private static void LogEnemyPlacement(TacticalLevelController controller)
        {
            try
            {
                TacticalFaction phoenix = controller.GetTacticalFaction(TacMissionParticipant.Player);
                List<TacticalActor> squad = phoenix.TacticalActors.Where(a => a.IsAlive).ToList();

                foreach (TacticalFaction faction in controller.Factions.Where(f => f != phoenix && f.GetRelationTo(phoenix) == FactionRelation.Enemy))
                {
                    foreach (TacticalActor enemy in faction.TacticalActors.Where(a => a.IsAlive))
                    {
                        float nearest = squad.Count > 0 ? squad.Min(s => (s.Pos - enemy.Pos).magnitude) : -1f;
                        TFTVLogger.Always($"[DeploymentPhase] {faction.TacticalFactionDef.name} {enemy.DisplayName} at {enemy.Pos}, nearest soldier {nearest:0.0}m, revealed to Phoenix {phoenix.Vision.IsRevealed(enemy)}");
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void LogObjectiveStates(TacticalLevelController controller)
        {
            try
            {
                foreach (TacticalFaction faction in controller.Factions)
                {
                    foreach (FactionObjective objective in faction.Objectives)
                    {
                        TFTVLogger.Always($"[DeploymentPhase] after release: {faction.TacticalFactionDef.name} {objective.GetType().Name} {objective.GetDescription()} - {objective.State}");
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Drives the phase's on-screen side once the level is visible: the opening prompt, the DEPLOY label on the
        /// End Turn button and the spot markers. Lives on the level controller's GameObject, so it goes away with
        /// the level; tearing down restores the button and clears the markers.
        /// </summary>
        private class DeploymentPhaseListener : MonoBehaviour
        {
            public TacticalLevelController Controller;
            private bool _shown;
            private bool _labelled;
            private bool _failed;

            // The relabel searches every loaded object; try it a few times a second, and not forever.
            private float _nextLabelTry;
            private int _labelTries;
            private const int MaxLabelTries = 80;

            private void Update()
            {
                try
                {
                    if (!IsPending(Controller))
                    {
                        Destroy(this);
                        return;
                    }

                    // After an error the phase carries on without its visuals: DEPLOY still works, since the
                    // RequestEndTurn patch does not depend on this component.
                    if (_failed)
                    {
                        return;
                    }

                    TacticalFaction current = Controller.CurrentFaction;

                    if (current == null || !current.IsPlayingTurn || !current.IsControlledByPlayer)
                    {
                        return;
                    }

                    if (!_labelled && _labelTries < MaxLabelTries && Time.unscaledTime >= _nextLabelTry)
                    {
                        _nextLabelTry = Time.unscaledTime + 0.25f;
                        _labelled = TryRelabelEndTurnButton();

                        if (!_labelled && ++_labelTries == MaxLabelTries)
                        {
                            TFTVLogger.Always($"[DeploymentPhase] End Turn button not found; it keeps its label");
                        }
                    }

                    if (!_shown)
                    {
                        _shown = true;
                        SpawnSecurityGuardsEarly(Controller);
                        GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_PHASE_PROMPT"),
                            MessageBoxIcon.Information, MessageBoxButtons.OK, null);
                    }

                    RegisterSecurityGuards(Controller);
                    UpdateReserve(Controller);
                    UpdateMarkers(Controller);

                    // Right-click on a unit benches it (left click places, brings in, swaps).
                    if (Input.GetMouseButtonDown(1) && !(UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() ?? false))
                    {
                        if (Controller.View.SelectAtCursor().Actor is TacticalActor pointedAt && IsSquadMember(pointedAt))
                        {
                            TryBench(Controller, pointedAt);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    _failed = true;
                    ClearMarkers();
                }
            }

            private void OnDestroy()
            {
                ClearMarkers();
                RestoreEndTurnButton();
                DestroyReserveStrip();

                // Only Update destroys this once the phase is over, so still pending here means the level is
                // going away mid-phase (a load, a restart, a quit): drop the hold with it, and give back the saving
                // the phase took from the game-wide save manager, or the next level could not save. Compared by
                // reference: mid-teardown, Unity's null check may already call the controller destroyed.
                if (_pending != null && ReferenceEquals(_pending.Controller, Controller))
                {
                    PendingDeployment pending = _pending;
                    _pending = null;
                    SecurityGuardsSpawnedDuringDeployment = false;
                    RestoreSaving(pending);
                    ResetPlacementCaches();
                    TFTVLogger.Always($"[DeploymentPhase] level left before deployment; restored saving enabled = {pending.SaveWasEnabled}");
                }
            }
        }

        // Text components keyed to their original string; the label may be UGUI Text or TextMeshPro, so both are
        // handled through their shared string "text" property.
        private static readonly Dictionary<Component, string> _endTurnLabels = new Dictionary<Component, string>();
        private static readonly List<Behaviour> _pausedLocalizers = new List<Behaviour>();

        private static System.Reflection.PropertyInfo TextProperty(Component component)
        {
            System.Reflection.PropertyInfo property = component.GetType().GetProperty("text", typeof(string));
            return property != null && property.CanRead && property.CanWrite ? property : null;
        }

        /// <summary>
        /// Shows DEPLOY on the End Turn button, whose press confirms deployment during the phase (see the
        /// RequestEndTurn patch). Only the button's own label changes; the hotkey glyph is left alone. Returns false
        /// while the HUD is not built yet, so the caller retries next frame. FindObjectsOfTypeAll, because the HUD
        /// can still be inactive when the turn starts.
        /// </summary>
        private static bool TryRelabelEndTurnButton()
        {
            UIModuleEndTurnContainer endTurn = Resources.FindObjectsOfTypeAll<UIModuleEndTurnContainer>()
                .FirstOrDefault(m => m != null && m.gameObject.scene.IsValid());

            if (endTurn?.Button == null)
            {
                return false;
            }

            string label = TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_PHASE_CONFIRM");
            Transform hotkey = endTurn.Hotkey != null ? endTurn.Hotkey.transform : null;

            foreach (Component component in endTurn.Button.GetComponentsInChildren<Component>(true))
            {
                if (component == null || (hotkey != null && component.transform.IsChildOf(hotkey)))
                {
                    continue;
                }

                // An I2 Localize component would write the old term straight back; pause it while relabelled.
                if (component is Behaviour behaviour && behaviour.enabled && component.GetType().Name == "Localize")
                {
                    behaviour.enabled = false;
                    _pausedLocalizers.Add(behaviour);
                    continue;
                }

                System.Reflection.PropertyInfo textProperty = TextProperty(component);

                if (textProperty == null || _endTurnLabels.ContainsKey(component))
                {
                    continue;
                }

                _endTurnLabels[component] = (string)textProperty.GetValue(component);
                textProperty.SetValue(component, label);

                // The reserve strip sizes its text from this, so it reads at the HUD's own scale.
                if (component is UnityEngine.UI.Text uiText && uiText.fontSize > 0)
                {
                    EndTurnFontSize = uiText.fontSize;
                }
            }

            if (_endTurnLabels.Count == 0)
            {
                TFTVLogger.Always($"[DeploymentPhase] End Turn button has no text to relabel; it holds: " +
                    string.Join(", ", endTurn.Button.GetComponentsInChildren<Component>(true).Where(c => c != null).Select(c => $"{c.name}:{c.GetType().Name}")));
                return true;
            }

            TFTVLogger.Always($"[DeploymentPhase] End Turn button relabelled: " +
                string.Join("; ", _endTurnLabels.Select(l => $"{l.Key.GetType().Name} on {l.Key.name} was '{l.Value}'")) +
                (_pausedLocalizers.Count > 0 ? $"; paused {_pausedLocalizers.Count} localizer(s)" : ""));

            return true;
        }

        private static void RestoreEndTurnButton()
        {
            foreach (KeyValuePair<Component, string> label in _endTurnLabels)
            {
                if (label.Key != null)
                {
                    TextProperty(label.Key)?.SetValue(label.Key, label.Value);
                }
            }

            foreach (Behaviour localizer in _pausedLocalizers)
            {
                if (localizer != null)
                {
                    localizer.enabled = true;
                }
            }

            _endTurnLabels.Clear();
            _pausedLocalizers.Clear();
        }

        [HarmonyPatch(typeof(TacMission), "OnLevelStart")]
        internal static class TacMission_OnLevelStart_HoldOtherParticipants_Patch
        {
            public static void Prefix(TacMission __instance)
            {
                try
                {
                    // ParticipantSpawns is null only on a fresh mission; a loaded save just re-initializes them.
                    if (!TFTVMain.Main.Config.PlayerDeploymentPhase || __instance.ParticipantSpawns != null || !IsEligibleMission(__instance))
                    {
                        return;
                    }

                    _pending = new PendingDeployment { Controller = __instance.TacticalLevel };
                    _holdingDuringLevelStart = true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void Postfix(TacMission __instance)
            {
                try
                {
                    if (!_holdingDuringLevelStart || !IsPending(__instance.TacticalLevel))
                    {
                        return;
                    }

                    _holdingDuringLevelStart = false;

                    if (_pending.HeldSpawns.Count == 0)
                    {
                        _pending = null;
                        return;
                    }

                    CollectInitialReserve();
                    BeginPhase(__instance.TacticalLevel);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static Exception Finalizer(Exception __exception)
            {
                if (_holdingDuringLevelStart)
                {
                    // OnLevelStart threw before the postfix: spawn normally rather than leave sides held.
                    _holdingDuringLevelStart = false;
                    _pending = null;
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(TacParticipantSpawn), "DeployForTurn")]
        internal static class TacParticipantSpawn_DeployForTurn_HoldOtherParticipants_Patch
        {
            public static bool Prefix(TacParticipantSpawn __instance)
            {
                try
                {
                    if (_pending == null || !IsPending(__instance.TacMission.TacticalLevel))
                    {
                        return true;
                    }

                    if (_holdingDuringLevelStart)
                    {
                        if (__instance.ParticipantKind == TacMissionParticipant.Player)
                        {
                            // Even if none of the squad fits, the reserve needs the spawn to bring them in from.
                            _pending.PlayerSpawn = __instance;
                            return true;
                        }

                        _pending.HeldSpawns.Add(__instance);
                        return false;
                    }

                    if (_pending.Releasing)
                    {
                        return true;
                    }

                    // Anything else asking a held side to deploy before the release (its own turn start is
                    // handled by the OnNewTurn patch below, which releases first).
                    return !_pending.HeldSpawns.Contains(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(TacParticipantSpawn), "DoSpawnActor")]
        internal static class TacParticipantSpawn_DoSpawnActor_CaptureSquadDeployData_Patch
        {
            public static void Postfix(TacParticipantSpawn __instance, ActorDeployData deploymentData, TacticalActorBase __result)
            {
                try
                {
                    // At level start, and later for reserve units brought in during the phase.
                    if (_pending == null || __instance.ParticipantKind != TacMissionParticipant.Player || !IsPending(__instance.TacMission.TacticalLevel)
                        || _pending.Releasing || !(__result is TacticalActor actor) || deploymentData == null)
                    {
                        return;
                    }

                    _pending.PlayerSpawn = __instance;
                    _pending.SquadDeployData[actor] = deploymentData;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// End Turn during the phase means DEPLOY: release the held sides and keep Phoenix's turn going. Every end
        /// turn route (button, hotkey, pad, each view state) goes through RequestEndTurn.
        /// </summary>
        [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.RequestEndTurn))]
        internal static class TacticalFaction_RequestEndTurn_ConfirmDeployment_Patch
        {
            public static bool Prefix(TacticalFaction __instance)
            {
                try
                {
                    TacticalLevelController controller = __instance.TacticalLevel;

                    if (IsPending(controller) && !_pending.Releasing && __instance == controller.GetTacticalFaction(TacMissionParticipant.Player))
                    {
                        Release(controller, "deployment confirmed");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(TacMission), "OnNewTurn")]
        internal static class TacMission_OnNewTurn_ReleaseBeforeOtherFactionPlays_Patch
        {
            public static void Prefix(TacMission __instance, TacticalFaction __1)
            {
                try
                {
                    TacticalLevelController controller = __instance.TacticalLevel;

                    if (IsPending(controller) && __1 != null && __1 != controller.GetTacticalFaction(TacMissionParticipant.Player))
                    {
                        Release(controller, $"turn passed to {__1.TacticalFactionDef?.name}");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.HasUndeployedTacActors))]
        internal static class TacticalFaction_HasUndeployedTacActors_HeldParticipants_Patch
        {
            public static void Postfix(TacticalFaction __instance, ref bool __result)
            {
                try
                {
                    if (!__result && _pending != null && IsHeld(__instance))
                    {
                        __result = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static readonly MethodInfo ObjectiveStateSetter = AccessTools.PropertySetter(typeof(FactionObjective), nameof(FactionObjective.State));

        /// <summary>
        /// While sides are held, objectives evaluate as usual - so running figures like SurviveTurnsFactionObjective's
        /// TurnsRemaining, which only EvaluateObjective sets, are right on the panel - but never finish. With the
        /// targets not spawned a kill objective reads as achieved, and Evaluate() would make that stick; so any
        /// finished result is put back to in progress before ObjectivesManager.Evaluate reads it (it chains the next
        /// objectives off a finished state). The state setter is a plain field write, so nothing fires on the way.
        /// </summary>
        [HarmonyPatch(typeof(FactionObjective), nameof(FactionObjective.Evaluate))]
        internal static class FactionObjective_Evaluate_KeepInProgressWhileHeld_Patch
        {
            public static void Postfix(FactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    if (__result != FactionObjectiveState.InProgress && IsPending(__instance.Faction?.TacticalLevel))
                    {
                        ObjectiveStateSetter.Invoke(__instance, new object[] { FactionObjectiveState.InProgress });
                        __result = FactionObjectiveState.InProgress;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GameOverCondition), "EvaluateObjectives")]
        internal static class GameOverCondition_EvaluateObjectives_SuspendWhileHeld_Patch
        {
            public static bool Prefix(GameOverCondition __instance, ref bool __result)
            {
                try
                {
                    if (_pending != null && IsPending(GameOverConditionController(__instance)))
                    {
                        __result = false;
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                return true;
            }
        }
    }
}
