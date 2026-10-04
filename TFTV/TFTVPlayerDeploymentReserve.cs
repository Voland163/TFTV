using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Input;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV
{
    /// <summary>
    /// The deployment phase's reserve: squad members who are not on the map, either because vanilla found no room
    /// for them at level start (a second vehicle, typically) or because the player benched them.
    ///
    /// A reserve unit's deploy entry stays in the player spawn's own queue while the phase runs, so bringing it in
    /// is vanilla's DoSpawnActor at a spot the zones accept for its size. Benching is vanilla's
    /// ActorSpawner.DestroyActor - the path RemoveActorEffect and MorphIntoActorAbility take on living actors - with
    /// the entry put back in the queue. At DEPLOY the entries still in reserve leave the queue, so those units sit
    /// the mission out instead of arriving on a later turn. A unit that never spawned has no tactical result, which
    /// the geoscape treats as alive and unharmed (GeoMission.GetDeadSquadMembers only kills units it has a result
    /// for on the wrong side).
    ///
    /// Each reserve unit shows as a copy of its own squad-bar card, greyed out. The card is cloned from the bar
    /// before the unit leaves the map; units vanilla never placed are spawned briefly at the start of the phase so
    /// the bar renders theirs, then benched.
    ///
    /// Controls: left click on a card picks the unit up (the next map select brings it in on a free spot, or swaps
    /// it with the deployed unit pointed at); right click benches the selected unit. A pad has no free cursor in the
    /// main tactical view, so it cycles the picked-up unit - and, last, a bench tool - with an unused B, the same
    /// hook TacticalTooltipCycler uses, registered just ahead of it.
    /// </summary>
    internal static partial class TFTVPlayerDeploymentPhase
    {
        private const float ReserveSnapRange = 1.6f;
        private const int ReserveInputPriority = 40;
        private const float CardRenderTimeout = 6f;
        private const int CardColumns = 4;

        private static readonly MethodInfo DoSpawnActorMethod = AccessTools.Method(typeof(TacParticipantSpawn), "DoSpawnActor");
        private static readonly AccessTools.FieldRef<TacParticipantSpawn, List<ActorDeployData>> ActorDeployDataList =
            AccessTools.FieldRefAccess<TacParticipantSpawn, List<ActorDeployData>>("_actorDeployData");
        private static readonly AccessTools.FieldRef<SquadMemberScrollerController, Dictionary<TacticalActor, SquadMemberScrollerController.PortraitSprites>> SoldierPortraits =
            AccessTools.FieldRefAccess<SquadMemberScrollerController, Dictionary<TacticalActor, SquadMemberScrollerController.PortraitSprites>>("_soldierPortraits");

        // The End Turn label's font size, read when it is relabelled; the strip's text is sized from it.
        private static int EndTurnFontSize = 0;
        private const int FallbackReferenceFontSize = 32;

        private static GameObject _reserveStrip;
        private static GameObject _cardTemplates;
        private static readonly List<Texture> _cardTextures = new List<Texture>();
        private static readonly Dictionary<ActorDeployData, Vector2> _cardSizes = new Dictionary<ActorDeployData, Vector2>();
        private static int _reserveVersion;
        private static int _reserveStripVersion = -1;
        private static bool _reserveInputRegistered;
        private static bool _reserveStripLogged;
        private static Action _deferredReserveAction;
        private static int _deferredReserveFrame;

        // Never-placed units waiting to be spawned for a moment so the squad bar renders their card.
        private static readonly Queue<ActorDeployData> _cardRenderQueue = new Queue<ActorDeployData>();
        private static TacticalActor _cardRenderActor;
        private static float _cardRenderStarted;
        private static int _cardRenderReadyFrame = -1;

        /// <summary>
        /// Set when the phase spawned base defense's security guards itself; TFTVBaseDefenseTactical's first-turn
        /// setup then skips its own spawn. Reset at every phase start and teardown.
        /// </summary>
        internal static bool SecurityGuardsSpawnedDuringDeployment;

        // ---------------------------------------------------------------- state

        private static bool TryGetArmedReserve(out ActorDeployData data)
        {
            data = null;

            if (_pending == null || _pending.Releasing || _pending.Armed < 0 || _pending.Armed >= _pending.Reserve.Count)
            {
                return false;
            }

            data = _pending.Reserve[_pending.Armed];
            return true;
        }

        private static bool IsBenchArmed()
        {
            return _pending != null && !_pending.Releasing && _pending.Armed == _pending.Reserve.Count;
        }

        private static void Arm(int index)
        {
            if (_pending == null)
            {
                return;
            }

            _pending.Armed = index < 0 || index > _pending.Reserve.Count ? -1 : index;
            _reserveVersion++;
        }

        private static void Disarm()
        {
            Arm(-1);
        }

        /// <summary>
        /// Pad B: none, each reserve unit in turn, bench, then none again.
        /// </summary>
        private static void CycleArmed()
        {
            int next = _pending.Armed + 1;
            Arm(next > _pending.Reserve.Count ? -1 : next);
        }

        private static void ReserveChanged()
        {
            if (_pending != null && _pending.Armed > _pending.Reserve.Count)
            {
                _pending.Armed = -1;
            }

            _reserveVersion++;
            MarkSpotsDirty();
        }

        private static string ReserveName(ActorDeployData data)
        {
            try
            {
                if (data.InstanceData is TacActorBaseInstanceData instance && instance.OverrideName != null)
                {
                    string name = instance.OverrideName.Localize();

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return data.ComponentSetDef?.name ?? "?";
        }

        private static Sprite ReserveIcon(ActorDeployData data)
        {
            try
            {
                if (!(data.InstanceData is TacActorBaseInstanceData instance))
                {
                    return null;
                }

                foreach (ClassTagDef classTag in instance.AdditionalGameTags?.OfType<ClassTagDef>() ?? Enumerable.Empty<ClassTagDef>())
                {
                    SpecializationDef specialization = GameUtl.GameComponent<DefRepository>().GetAllDefs<SpecializationDef>()
                        .FirstOrDefault(s => s.ClassTag == classTag);

                    if (specialization?.ViewElementDef?.SmallIcon != null)
                    {
                        return specialization.ViewElementDef.SmallIcon;
                    }
                }

                return instance.OverrideViewElement?.SmallIcon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// After the squad's level-start spawn: everyone still queued is someone vanilla found no room for. They get
        /// a card rendered once the HUD is up.
        /// </summary>
        private static void CollectInitialReserve()
        {
            try
            {
                _cardRenderQueue.Clear();
                _cardRenderActor = null;

                if (_pending.PlayerSpawn == null)
                {
                    return;
                }

                foreach (ActorDeployData data in _pending.PlayerSpawn.ActorDeployData)
                {
                    if (data.Unique && data.InstanceData is TacActorBaseInstanceData instance && instance.GeoUnitId != 0)
                    {
                        _pending.Reserve.Add(data);
                        _cardRenderQueue.Enqueue(data);
                        TFTVLogger.Always($"[DeploymentPhase] {ReserveName(data)} did not fit at level start; in reserve");
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// At DEPLOY: whoever is still in reserve sits the mission out, so their entries leave the queue that vanilla
        /// would otherwise retry at every Phoenix turn start.
        /// </summary>
        private static void CloseReserve(PendingDeployment pending)
        {
            try
            {
                _deferredReserveAction = null;
                _reserveStripLogged = false;
                _cardRenderQueue.Clear();
                _cardRenderActor = null;
                pending.Armed = -1;
                DestroyReserveStrip();
                DestroyCards();

                if (pending.PlayerSpawn == null)
                {
                    return;
                }

                List<ActorDeployData> queue = ActorDeployDataList(pending.PlayerSpawn);

                foreach (ActorDeployData data in pending.Reserve)
                {
                    queue.Remove(data);
                    TFTVLogger.Always($"[DeploymentPhase] {ReserveName(data)} sits this mission out");
                }

                pending.Reserve.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // ---------------------------------------------------------------- cards

        private static SquadMemberScrollerController SquadBar()
        {
            return Resources.FindObjectsOfTypeAll<SquadMemberScrollerController>().FirstOrDefault(s => s != null && s.gameObject.scene.IsValid());
        }

        private static SquadMemberScrollerElement SquadBarElement(SquadMemberScrollerController squadBar, TacticalActor actor)
        {
            return squadBar?.GetComponentsInChildren<SquadMemberScrollerElement>(true).FirstOrDefault(e => e != null && e.Actor == actor);
        }

        /// <summary>
        /// The bar has finished this unit's portrait: a rendered one (soldiers) or a fixed one (vehicles, Mutogs).
        /// </summary>
        private static bool PortraitReady(SquadMemberScrollerController squadBar, TacticalActor actor)
        {
            return squadBar != null
                && SoldierPortraits(squadBar).TryGetValue(actor, out SquadMemberScrollerController.PortraitSprites sprites)
                && sprites != null && (sprites.RenderedPortrait != null || sprites.Portrait != null);
        }

        /// <summary>
        /// Copies the unit's squad-bar card before it leaves the map: the element cloned, its scripts stripped so
        /// nothing in it reacts to the game any more, and its rendered portrait copied, since the bar may release
        /// the original with the actor. Kept inactive as a template; the strip instantiates it.
        /// </summary>
        private static void CaptureCard(TacticalActor actor, ActorDeployData data)
        {
            try
            {
                SquadMemberScrollerElement element = SquadBarElement(SquadBar(), actor);

                if (element == null)
                {
                    TFTVLogger.Always($"[DeploymentPhase] no squad bar card found for {actor.DisplayName}");
                    return;
                }

                if (_cardTemplates == null)
                {
                    _cardTemplates = new GameObject("TFTV_DeploymentReserveCards");
                    _cardTemplates.SetActive(false);
                }

                GameObject card = UnityEngine.Object.Instantiate(element.gameObject, _cardTemplates.transform, false);
                card.name = "ReserveCard";
                StripToVisuals(card);

                foreach (RawImage raw in card.GetComponentsInChildren<RawImage>(true))
                {
                    if (raw.texture != null)
                    {
                        raw.texture = CopyTexture(raw.texture);
                    }
                }

                if (_pending.Cards.TryGetValue(data, out GameObject previous) && previous != null)
                {
                    UnityEngine.Object.Destroy(previous);
                }

                _pending.Cards[data] = card;
                _cardSizes[data] = ((RectTransform)element.transform).rect.size;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Leaves only what draws and lays out: UGUI graphics, masks, layout and canvas groups. Everything else -
        /// the game's element scripts, tooltips, buttons, animators - goes, scripts before the components they
        /// might require.
        /// </summary>
        private static void StripToVisuals(GameObject card)
        {
            List<Behaviour> doomed = card.GetComponentsInChildren<Behaviour>(true).Where(b => b != null && !IsVisualComponent(b)).ToList();

            foreach (Behaviour behaviour in doomed.Where(b => !(b is Selectable)))
            {
                UnityEngine.Object.DestroyImmediate(behaviour);
            }

            foreach (Behaviour behaviour in doomed.Where(b => b != null))
            {
                UnityEngine.Object.DestroyImmediate(behaviour);
            }
        }

        private static bool IsVisualComponent(Behaviour behaviour)
        {
            return behaviour is Graphic || behaviour is Mask || behaviour is RectMask2D || behaviour is LayoutElement
                || behaviour is LayoutGroup || behaviour is ContentSizeFitter || behaviour is AspectRatioFitter
                || behaviour is BaseMeshEffect || behaviour is CanvasGroup;
        }

        private static Texture CopyTexture(Texture source)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                copy.Apply();
                _cardTextures.Add(copy);
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void DestroyCards()
        {
            if (_cardTemplates != null)
            {
                UnityEngine.Object.Destroy(_cardTemplates);
            }

            foreach (Texture texture in _cardTextures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _cardTemplates = null;
            _cardTextures.Clear();
            _cardSizes.Clear();
        }

        /// <summary>
        /// One never-placed unit at a time: spawn it anywhere in the zones (overlapping is fine for the moment it
        /// stands there), wait for the bar to finish its portrait, then bench it, which captures the card.
        /// </summary>
        private static void RenderNextCard(TacticalLevelController controller)
        {
            SquadMemberScrollerController squadBar = SquadBar();

            if (squadBar == null)
            {
                return;
            }

            if (_cardRenderActor != null)
            {
                bool ready = PortraitReady(squadBar, _cardRenderActor) && SquadBarElement(squadBar, _cardRenderActor) != null;

                if (ready && _cardRenderReadyFrame < 0)
                {
                    // The bar updates the element from the portrait on a later frame; give it a couple.
                    _cardRenderReadyFrame = Time.frameCount;
                }

                bool timedOut = Time.realtimeSinceStartup - _cardRenderStarted > CardRenderTimeout;

                if ((ready && Time.frameCount > _cardRenderReadyFrame + 2) || timedOut)
                {
                    TacticalActor actor = _cardRenderActor;
                    _cardRenderActor = null;
                    TFTVLogger.Always($"[DeploymentPhase] {(timedOut ? "timed out rendering" : "rendered")} a reserve card for {actor.DisplayName}");
                    TryBench(controller, actor);
                }

                return;
            }

            if (_cardRenderQueue.Count == 0)
            {
                return;
            }

            ActorDeployData data = _cardRenderQueue.Dequeue();

            if (!_pending.Reserve.Contains(data))
            {
                return;
            }

            List<DeploySpot> spots = GetFreeDeploySpots(data);
            DeploySpot spot = spots.Count > 0 ? spots[0] : AnyZoneSpot();

            if (spot.Zone == null)
            {
                return;
            }

            _cardRenderActor = Spawn(data, spot);
            _cardRenderStarted = Time.realtimeSinceStartup;
            _cardRenderReadyFrame = -1;
            ReserveChanged();
        }

        private static DeploySpot AnyZoneSpot()
        {
            foreach (TacticalDeployZone zone in PlayerZones())
            {
                foreach (Vector3 pos in zone.GetOrderedSpawnPositions(false))
                {
                    return new DeploySpot { Zone = zone, Pos = pos };
                }
            }

            return default;
        }

        // ---------------------------------------------------------------- security guards

        /// <summary>
        /// Base defense's guards under Phoenix control are spawned by TFTV's first-turn setup, which the phase defers
        /// until after DEPLOY. Spawn them as the phase opens instead, so they can be placed too.
        /// </summary>
        private static void SpawnSecurityGuardsEarly(TacticalLevelController controller)
        {
            try
            {
                if (_pending.GuardsRequested || !IsBaseDefense())
                {
                    return;
                }

                _pending.GuardsRequested = true;
                TFTVBaseDefenseTactical.StartingDeployment.GoldShiftSetup();
                SecurityGuardsSpawnedDuringDeployment = true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// The guards arrive asynchronously (their assets load first). Any Phoenix unit on the map that is not a
        /// squad member is one; give it a deploy entry from the guard template, so placement can size its spots.
        /// </summary>
        private static void RegisterSecurityGuards(TacticalLevelController controller)
        {
            try
            {
                if (_pending == null || !_pending.GuardsRequested || _pending.PlayerSpawn == null)
                {
                    return;
                }

                foreach (TacticalActor actor in _pending.PlayerSpawn.TacticalFaction.TacticalActors)
                {
                    if (actor == null || !actor.IsAlive || _pending.SquadDeployData.ContainsKey(actor) || actor == _cardRenderActor)
                    {
                        continue;
                    }

                    TacCharacterDef template = TFTVBaseDefenseTactical.StartingDeployment.UseLevel2SecurityGuards && TFTVBaseDefenseTactical.Defs.SecurityGuardLevel2 != null
                        ? TFTVBaseDefenseTactical.Defs.SecurityGuardLevel2
                        : TFTVBaseDefenseTactical.Defs.SecurityGuard;

                    if (template == null)
                    {
                        return;
                    }

                    ActorDeployData data = template.GenerateActorDeployData();
                    data.InitializeInstanceData();

                    _pending.SquadDeployData[actor] = data;
                    _pending.Guards.Add(actor);
                    MarkSpotsDirty();
                    TFTVLogger.Always($"[DeploymentPhase] security guard {actor.DisplayName} can be placed");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // ---------------------------------------------------------------- actions

        private static TacticalActor Spawn(ActorDeployData data, DeploySpot spot)
        {
            TacticalActor actor = DoSpawnActorMethod.Invoke(_pending.PlayerSpawn, new object[] { 0, data, spot.Zone, spot.Pos }) as TacticalActor;

            if (actor != null)
            {
                // DoSpawnActor's ActorSpawned took the entry out of the queue; the capture patch has it in SquadDeployData.
                _pending.Reserve.Remove(data);

                // Phoenix's turn start, which refills AP, ran before this unit existed. ForceRestartTurn is what vanilla
                // gives a unit joining a side mid-turn (MindControlStatus.AfterApply): abilities restarted, AP to max.
                actor.ForceRestartTurn();
            }

            return actor;
        }

        /// <summary>
        /// Brings the reserve unit in at the free spot nearest the click.
        /// </summary>
        private static void TryBringIn(TacticalLevelController controller, ActorDeployData data, Vector3 clicked)
        {
            try
            {
                if (!TryNearestSpot(GetFreeDeploySpots(data), clicked, ReserveSnapRange, out DeploySpot spot))
                {
                    return;
                }

                TacticalActor actor = Spawn(data, spot);

                if (actor == null)
                {
                    TFTVLogger.Always($"[DeploymentPhase] {ReserveName(data)} could not be spawned at {spot.Pos}");
                    return;
                }

                // The selection stays where it was: the new unit's HUD side is not set up until it is selected
                // through the view state, which the player does by clicking it.
                Disarm();
                ReserveChanged();
                FinishMoves(controller);
                TFTVLogger.Always($"[DeploymentPhase] brought {actor.DisplayName} in from the reserve at {actor.Pos}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Sends a deployed unit to the reserve, keeping a copy of its squad-bar card. Refuses security guards (not
        /// part of the roster, so they could not come back) and the last squad member on the map (the turn needs
        /// someone to play it).
        /// </summary>
        private static bool TryBench(TacticalLevelController controller, TacticalActor actor)
        {
            try
            {
                if (!_pending.SquadDeployData.TryGetValue(actor, out ActorDeployData data))
                {
                    return false;
                }

                if (_pending.Guards.Contains(actor))
                {
                    TFTVLogger.Always($"[DeploymentPhase] {actor.DisplayName} is a security guard and stays on the map");
                    return false;
                }

                TacticalActor stays = _pending.SquadDeployData.Keys
                    .FirstOrDefault(a => a != actor && a != null && a.IsAlive && a != _cardRenderActor && !_pending.Guards.Contains(a));

                if (stays == null)
                {
                    TFTVLogger.Always($"[DeploymentPhase] {actor.DisplayName} is the last squad member on the map and stays");
                    return false;
                }

                if (controller.View.SelectedActor == actor && !TrySelectThroughState(controller, stays))
                {
                    TFTVLogger.Always($"[DeploymentPhase] could not move the selection off {actor.DisplayName}; not benched");
                    return false;
                }

                string name = actor.DisplayName;
                CaptureCard(actor, data);
                _pending.SquadDeployData.Remove(actor);
                ActorSpawner.DestroyActor(actor);

                List<ActorDeployData> queue = ActorDeployDataList(_pending.PlayerSpawn);

                if (!queue.Contains(data))
                {
                    queue.Add(data);
                }

                // ActorSpawned charged the entry's cost when it spawned; it will again if it comes back in.
                _pending.PlayerSpawn.DeploymentPointsUsed -= data.DeployCost;

                _pending.Reserve.Add(data);
                ReserveChanged();
                FinishMoves(controller);
                TFTVLogger.Always($"[DeploymentPhase] benched {name}");
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Swaps the picked-up reserve unit with a deployed one: the deployed unit goes to the reserve, and the
        /// reserve unit takes its place if it fits there. If it does not (a vehicle where a soldier stood), the
        /// benched unit comes straight back and the reserve unit stays picked up. The placement runs a frame later,
        /// once the benched unit's GameObject - and its colliders - are really gone.
        /// </summary>
        private static void TrySwapIn(TacticalLevelController controller, ActorDeployData incoming, TacticalActor outgoing)
        {
            try
            {
                if (!_pending.SquadDeployData.TryGetValue(outgoing, out ActorDeployData outgoingData))
                {
                    return;
                }

                Vector3 spot = outgoing.Pos;
                string outgoingName = outgoing.DisplayName;

                if (!TryBench(controller, outgoing))
                {
                    return;
                }

                _deferredReserveFrame = Time.frameCount;
                _deferredReserveAction = () =>
                {
                    if (TryNearestSpot(GetFreeDeploySpots(incoming), spot, ReserveSnapRange, out DeploySpot incomingSpot)
                        && Spawn(incoming, incomingSpot) is TacticalActor actor)
                    {
                        Disarm();
                        ReserveChanged();
                        FinishMoves(controller);
                        TFTVLogger.Always($"[DeploymentPhase] swapped {outgoingName} out for {actor.DisplayName}");
                        return;
                    }

                    TFTVLogger.Always($"[DeploymentPhase] {ReserveName(incoming)} does not fit where {outgoingName} stood; {outgoingName} returns");

                    if (TryNearestSpot(GetFreeDeploySpots(outgoingData), spot, ReserveSnapRange, out DeploySpot backSpot)
                        && Spawn(outgoingData, backSpot) is TacticalActor back)
                    {
                        ReserveChanged();
                        FinishMoves(controller);
                    }
                    else
                    {
                        TFTVLogger.Always($"[DeploymentPhase] {outgoingName} could not be put back and stays in reserve");
                    }
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // ---------------------------------------------------------------- per frame

        private static void UpdateReserve(TacticalLevelController controller)
        {
            EnsureReserveInput();

            if (_deferredReserveAction != null && Time.frameCount > _deferredReserveFrame)
            {
                Action action = _deferredReserveAction;
                _deferredReserveAction = null;

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            try
            {
                RenderNextCard(controller);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                _cardRenderQueue.Clear();
                _cardRenderActor = null;
            }

            if (_reserveStrip == null || _reserveStripVersion != _reserveVersion)
            {
                BuildReserveStrip(controller);
            }
        }

        private static void EnsureReserveInput()
        {
            if (_reserveInputRegistered)
            {
                return;
            }

            InputController input = GameUtl.GameComponent<InputController>();

            if (input == null)
            {
                return;
            }

            // After the view states (0), so B only arrives here when nothing else wanted it; before the tooltip
            // cycler (50), so during deployment the reserve gets it first.
            input.EventHandlers.AddUnique(HandleReserveCancel, ReserveInputPriority);
            _reserveInputRegistered = true;
        }

        private static bool HandleReserveCancel(InputEvent ev)
        {
            try
            {
                if (ev.Type != InputEventType.Pressed || ev.Name != "Cancel" || ev.InputType != InputType.Joystick)
                {
                    return false;
                }

                TacticalLevelController controller = _pending?.Controller;

                if (controller == null || _pending.Releasing || controller.CurrentFaction == null
                    || !controller.CurrentFaction.IsPlayingTurn || !controller.CurrentFaction.IsControlledByPlayer)
                {
                    return false;
                }

                CycleArmed();
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        // ---------------------------------------------------------------- strip

        private static readonly Color StripBackground = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color EntryBackground = new Color(0.12f, 0.14f, 0.17f, 0.9f);
        private static readonly Color ArmedBackground = new Color(0.55f, 0.42f, 0.05f, 0.95f);
        private static readonly Color ReserveTextColor = new Color(0.62f, 0.66f, 0.72f);
        private const float GreyedOut = 0.45f;

        private static int _stripTitleSize = FallbackReferenceFontSize;
        private static int _stripEntrySize = FallbackReferenceFontSize;
        private static int _stripHintSize = FallbackReferenceFontSize;

        private static void DestroyReserveStrip()
        {
            if (_reserveStrip != null)
            {
                _reserveStrip.transform.SetParent(null);
                UnityEngine.Object.Destroy(_reserveStrip);
            }

            _reserveStrip = null;
            _reserveStripVersion = -1;
        }

        private static Font ReserveFont()
        {
            return TFTVUI.Tactical.Data.PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// The strip sits on the HUD canvas, left of centre: a title, the reserve cards in rows of four like the
        /// squad bar, a line naming the unit picked up, and a hint. Rebuilt whole whenever the reserve or the tool
        /// changes.
        /// </summary>
        private static void BuildReserveStrip(TacticalLevelController controller)
        {
            try
            {
                Transform hudModule = controller.View.TacticalModules?.NavigationModule?.transform;
                Canvas canvas = hudModule?.GetComponentInParent<Canvas>();

                if (canvas == null)
                {
                    return;
                }

                DestroyReserveStrip();

                // The HUD modules sit under scaling of their own inside the root canvas; match it, so the strip
                // reads at the same size as the vanilla HUD around it.
                Transform rootCanvas = canvas.rootCanvas.transform;
                float hudScale = rootCanvas.lossyScale.x > 0f ? hudModule.lossyScale.x / rootCanvas.lossyScale.x : 1f;

                if (float.IsNaN(hudScale) || float.IsInfinity(hudScale) || hudScale <= 0f)
                {
                    hudScale = 1f;
                }

                int reference = EndTurnFontSize > 0 ? EndTurnFontSize : FallbackReferenceFontSize;
                _stripTitleSize = reference;
                _stripEntrySize = Mathf.RoundToInt(reference * 0.8f);
                _stripHintSize = Mathf.RoundToInt(reference * 0.55f);

                if (!_reserveStripLogged)
                {
                    _reserveStripLogged = true;
                    TFTVLogger.Always($"[DeploymentPhase] reserve strip: HUD scale {hudScale:0.##}, reference font {reference}");
                }

                _reserveStrip = new GameObject("TFTV_DeploymentReserve", typeof(RectTransform));
                RectTransform root = _reserveStrip.GetComponent<RectTransform>();
                root.SetParent(rootCanvas, false);
                root.localScale = Vector3.one * hudScale;
                root.anchorMin = new Vector2(0f, 0.5f);
                root.anchorMax = new Vector2(0f, 0.5f);
                root.pivot = new Vector2(0f, 0.5f);
                root.anchoredPosition = new Vector2(reference, reference * 4f) * hudScale;

                _reserveStrip.AddComponent<Image>().color = StripBackground;

                int padding = Mathf.RoundToInt(reference * 0.4f);
                VerticalLayoutGroup layout = _reserveStrip.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(padding, padding, padding, padding);
                layout.spacing = reference * 0.25f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = _reserveStrip.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                AddStripText(root, TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_RESERVE_TITLE"), _stripTitleSize, Color.white);

                if (_pending.Reserve.Count == 0)
                {
                    AddStripText(root, TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_RESERVE_EMPTY"), _stripEntrySize, ReserveTextColor);
                }
                else
                {
                    AddCards(root);
                }

                if (TryGetArmedReserve(out ActorDeployData armed))
                {
                    AddStripText(root, ReserveName(armed), _stripEntrySize, Color.white);
                }
                else if (IsBenchArmed())
                {
                    // Only the pad reaches this; the mouse benches with a right click.
                    AddStripEntry(root, null, TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_RESERVE_BENCH"), true, _pending.Reserve.Count);
                }

                AddStripText(root, TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_RESERVE_HINT"), _stripHintSize, ReserveTextColor, reference * 11f);

                _reserveStripVersion = _reserveVersion;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                DestroyReserveStrip();
                _reserveStripVersion = _reserveVersion;
            }
        }

        private static void AddCards(Transform parent)
        {
            Vector2 cell = _cardSizes.Values.FirstOrDefault(s => s.x > 0f && s.y > 0f);

            if (cell == default)
            {
                cell = new Vector2(_stripEntrySize * 4f, _stripEntrySize * 4.5f);
            }

            GameObject grid = new GameObject("Cards", typeof(RectTransform));
            grid.transform.SetParent(parent, false);

            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = cell;
            layout.spacing = new Vector2(_stripEntrySize * 0.25f, _stripEntrySize * 0.25f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Min(CardColumns, _pending.Reserve.Count);

            for (int i = 0; i < _pending.Reserve.Count; i++)
            {
                ActorDeployData data = _pending.Reserve[i];
                bool armed = _pending.Armed == i;

                if (_pending.Cards.TryGetValue(data, out GameObject template) && template != null)
                {
                    AddCard(grid.transform, template, armed, i);
                }
                else
                {
                    AddStripEntry(grid.transform, ReserveIcon(data), ReserveName(data), armed, i);
                }
            }
        }

        private static void AddCard(Transform parent, GameObject template, bool armed, int index)
        {
            GameObject card = UnityEngine.Object.Instantiate(template, parent, false);
            card.SetActive(true);

            // Off the map, so greyed out until picked up.
            if (!armed)
            {
                foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
                {
                    Color color = graphic.color;
                    graphic.color = new Color(color.r * GreyedOut, color.g * GreyedOut, color.b * GreyedOut, color.a);
                }
            }
            else
            {
                Outline outline = card.AddComponent<Outline>();
                outline.effectColor = ArmedBackground;
                outline.effectDistance = new Vector2(4f, 4f);
            }

            // Something on the card's root has to catch the click for the button.
            Graphic target = card.GetComponent<Graphic>();

            if (target == null)
            {
                Image catcher = card.AddComponent<Image>();
                catcher.color = new Color(0f, 0f, 0f, 0f);
                target = catcher;
            }

            AddArmButton(card, target, index);
        }

        private static Text AddStripText(Transform parent, string content, int size, Color color, float preferredWidth = -1f)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = ReserveFont();
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (preferredWidth > 0f)
            {
                textObject.AddComponent<LayoutElement>().preferredWidth = preferredWidth;
            }

            return text;
        }

        /// <summary>
        /// The plain row, for a unit whose card could not be captured, and for the pad's bench tool.
        /// </summary>
        private static void AddStripEntry(Transform parent, Sprite icon, string label, bool armed, int index)
        {
            GameObject row = new GameObject("Entry", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            Image background = row.AddComponent<Image>();
            background.color = armed ? ArmedBackground : EntryBackground;

            int padding = Mathf.RoundToInt(_stripEntrySize * 0.3f);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding * 2, padding, padding);
            layout.spacing = _stripEntrySize * 0.4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            row.AddComponent<LayoutElement>().minHeight = _stripEntrySize * 2f;

            if (icon != null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
                iconObject.transform.SetParent(row.transform, false);

                Image iconImage = iconObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = armed ? Color.white : ReserveTextColor;

                LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = _stripEntrySize * 1.6f;
                iconLayout.preferredHeight = _stripEntrySize * 1.6f;
            }

            AddStripText(row.transform, label, _stripEntrySize, armed ? Color.white : ReserveTextColor);
            AddArmButton(row, background, index);
        }

        private static void AddArmButton(GameObject target, Graphic graphic, int index)
        {
            Button button = target.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (_pending == null || _pending.Releasing)
                {
                    return;
                }

                Arm(_pending.Armed == index ? -1 : index);
            });
        }
    }
}
