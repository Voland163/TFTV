using Base.Defs;
using Base.Utils.GameConsole;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.UI;
using PhoenixPoint.Tactical.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVUI.Tactical
{
    internal class MeleeRangePrediction
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(MoveAbilitySceneViewElement), nameof(MoveAbilitySceneViewElement.DrawHoverMarker))]
        public static class KnownMeleeThreatMoveHoverPatch
        {
            [ConsoleVariable(Alias = "move_melee_threat_range_tolerance", Description = "Extra distance tolerance for known melee enemy threat tile markers.")]
            public static float RangeTolerance = 0.25f;

            private const string MeleeWeaponTagDefName = "MeleeWeapon_TagDef";
            private const float MovePositionMatchTolerance = 0.2f;
            private static readonly Color ThreatIconColor = new Color(1f, 0.145f, 0.286f, 0.05f);
            private static GameTagDef _meleeWeaponTagDef;

            private static void Postfix(MoveAbilitySceneViewElement __instance, TacticalViewContext context, bool __result)
            {
                if (!TFTVMain.Main.Config.ShowMeleeThreatMarkers || !__result || __instance == null || context == null)
                {
                    return;
                }

                MoveAbility selectedMoveAbility = __instance.Ability as MoveAbility;
                TacticalActor selectedActor = selectedMoveAbility?.TacticalActor;
                GameObject hoverVisual = __instance.HoverMarker?.VisualObject;
                if (selectedMoveAbility == null || selectedActor == null || hoverVisual == null)
                {
                    return;
                }

                List<MoveAbilityTargetData> selectedMoves = selectedMoveAbility.GetTargetsData(null).ToList();
                if (selectedMoves.Count == 0)
                {
                    return;
                }

                Vector3 hoveredPosition = hoverVisual.transform.position;
                MoveAbilityTargetData hoveredMove = selectedMoves.FirstOrDefault(move => SameMovePosition(move.Position, hoveredPosition));
                if (hoveredMove == null)
                {
                    return;
                }

                DrawKnownMeleeThreatMarkers(context, selectedActor, hoveredMove.Position);
            }

            private static void DrawKnownMeleeThreatMarkers(TacticalViewContext context, TacticalActor selectedActor, Vector3 hoveredMovePosition)
            {
                Dictionary<Vector3, List<TacticalActor>> threatsByPosition =
                    new Dictionary<Vector3, List<TacticalActor>>(Vector3EqualityComparer.Default);

                foreach (TacticalActor threatActor in GetKnownMeleeEnemies(selectedActor))
                {
                    ShootAbility meleeAbility = GetBestMeleeAbility(threatActor);
                    if (meleeAbility == null)
                    {
                        continue;
                    }

                    float meleeRange = meleeAbility.Weapon.GetMaxRange();
                    float maxMoveRange = GetFullTurnMoveRange(threatActor);
                    float moveAndAttackRange = GetFullTurnMoveAndAttackRange(threatActor, meleeAbility);
                    if (moveAndAttackRange < 0f)
                    {
                        continue;
                    }

                    foreach (MoveAbilityTargetData threatMove in GetThreatActorMoveTargets(threatActor, maxMoveRange))
                    {
                        if (!threatMove.IsPositionInRange(moveAndAttackRange) ||
                            !CanMeleeAttackFrom(threatMove.Position, hoveredMovePosition, meleeRange))
                        {
                            continue;
                        }

                        List<TacticalActor> actors;
                        if (!threatsByPosition.TryGetValue(threatMove.Position, out actors))
                        {
                            actors = new List<TacticalActor>();
                            threatsByPosition.Add(threatMove.Position, actors);
                        }

                        if (!actors.Contains(threatActor))
                        {
                            actors.Add(threatActor);
                        }
                    }
                }

                foreach (KeyValuePair<Vector3, List<TacticalActor>> tile in threatsByPosition)
                {
                    TacticalActor firstThreat = tile.Value[0];
                    GroundMarker marker = new GroundMarker(GroundMarkerType.MeleeAttackPosition, tile.Key, 0f)
                    {
                        Areas = firstThreat.TacticalNav.NavAreas
                    };

                    context.View.Markers.AddGroundMarker(GroundMarkerGroup.HoverSelection, marker, false);

                    Sprite[] icons = tile.Value
                        .SelectMany(actor => actor.ClassViewElementDefs)
                        .Select(viewElement => viewElement.SmallIcon)
                        .Where(icon => icon != null)
                        .ToArray();

                    MeleeThreatMarkerVisual visual = marker.VisualObject.GetComponent<MeleeThreatMarkerVisual>();
                    if (visual == null)
                    {
                        visual = marker.VisualObject.AddComponent<MeleeThreatMarkerVisual>();
                    }

                    visual.ShowIcons(icons, ThreatIconColor);
                    Utils.TiltForTerrain(context, marker, firstThreat.TacticalNav.FloorLayers);
                }
            }

            private static IEnumerable<TacticalActor> GetKnownMeleeEnemies(TacticalActor selectedActor)
            {
                return from actor in selectedActor.TacticalFaction.Vision
                           .GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                           .OfType<TacticalActor>()
                       where CanThreatenNextTurn(actor) && GetBestMeleeAbility(actor) != null
                       select actor;
            }

            private static bool CanThreatenNextTurn(TacticalActor actor)
            {
                return actor.InPlay &&
                       actor.Interactable &&
                       !actor.IsDisabled &&
                       !actor.IsDeadNextTurn() &&
                       actor.Status.GetStatus<PanicStatus>() == null &&
                       GetNextTurnActionPoints(actor) > 0f;
            }

            private static ShootAbility GetBestMeleeAbility(TacticalActor actor)
            {
                GameTagDef meleeWeaponTagDef = GetMeleeWeaponTagDef();
                if (meleeWeaponTagDef == null)
                {
                    return null;
                }

                return (from ability in actor.GetAbilities<ShootAbility>()
                        let weapon = ability.Weapon
                        where weapon != null &&
                              weapon.IsUsable &&
                              weapon.WeaponDef.Tags.Contains(meleeWeaponTagDef)
                        orderby weapon.GetMaxRange() descending
                        select ability).FirstOrDefault();
            }

            private static GameTagDef GetMeleeWeaponTagDef()
            {
                if (_meleeWeaponTagDef == null)
                {
                    _meleeWeaponTagDef = DefCache.GetDef<ItemClassificationTagDef>(MeleeWeaponTagDefName);
                }

                return _meleeWeaponTagDef;
            }

            private static float GetFullTurnMoveRange(TacticalActor actor)
            {
                MoveAbility moveAbility = actor.GetAbility<MoveAbility>();
                if (actor.Status.GetStatusByName("Gooed") != null)
                {
                    return 0f;
                }

                float distanceToApFactor = moveAbility != null
                    ? moveAbility.DistanceToAPFactor
                    : actor.TacticalNav.DistanceToAPFactor;
                return GetNextTurnActionPoints(actor) * distanceToApFactor;
            }

            private static float GetFullTurnMoveAndAttackRange(TacticalActor actor, ShootAbility meleeAbility)
            {
                MoveAbility moveAbility = actor.GetAbility<MoveAbility>();
                if (moveAbility == null)
                {
                    return 0f;
                }

                float nextTurnActionPoints = GetNextTurnActionPoints(actor);
                float attackApCost = actor.CalcActionPointCost(
                    actor.CalcFractActionPointCost(meleeAbility.FractActionPointCost, meleeAbility));
                if (Utl.LesserThan(nextTurnActionPoints, attackApCost, 1E-05f))
                {
                    return -1f;
                }

                float remainingApForMovement = Mathf.Max(
                    0f,
                    nextTurnActionPoints - attackApCost);

                if (actor.Status.GetStatusByName("Gooed") != null)
                {
                    return 0f;
                }

                return remainingApForMovement * moveAbility.DistanceToAPFactor;
            }

            private static float GetNextTurnActionPoints(TacticalActor actor)
            {
                float maxActionPoints = actor.CharacterStats.ActionPoints.Max;
                float actionPoints = maxActionPoints;

                StunStatus stunStatus = actor.Status.GetStatus<StunStatus>();
                if (stunStatus != null)
                {
                    actionPoints -= maxActionPoints * stunStatus.StunStatusDef.ActionPointsReduction;
                }

                DamageOverTimeStatus paralysis =
                    actor.Status.GetStatusByName("Paralysis") as DamageOverTimeStatus;
                if (paralysis != null)
                {
                    float endurance = Mathf.Max(1E-05f, actor.CharacterStats.Endurance);
                    float paralysisRatio = paralysis.FullDamageValue / endurance;
                    actionPoints -= maxActionPoints * GetParalysisActionPointReduction(paralysisRatio);
                }

                return Mathf.Max(0f, actionPoints);
            }

            private static float GetParalysisActionPointReduction(float paralysisRatio)
            {
                if (Utl.GreaterThanOrEqualTo(paralysisRatio, 1f, 1E-05f))
                {
                    return 1f;
                }

                if (Utl.GreaterThanOrEqualTo(paralysisRatio, 0.75f, 1E-05f))
                {
                    return 0.75f;
                }

                if (Utl.GreaterThanOrEqualTo(paralysisRatio, 0.5f, 1E-05f))
                {
                    return 0.5f;
                }

                return Utl.GreaterThanOrEqualTo(paralysisRatio, 0.25f, 1E-05f)
                    ? 0.25f
                    : 0f;
            }

            private static IEnumerable<MoveAbilityTargetData> GetThreatActorMoveTargets(TacticalActor actor, float maxMoveRange)
            {
                yield return new MoveAbilityTargetData(actor.Pos, 0f);

                MoveAbility moveAbility = actor.GetAbility<MoveAbility>();
                if (moveAbility == null || maxMoveRange <= 0f)
                {
                    yield break;
                }

                TacticalPathRequest pathRequest = actor.TacticalNav.CreatePathRequest() as TacticalPathRequest;
                if (pathRequest == null)
                {
                    yield break;
                }

                pathRequest.MaxPathLength = maxMoveRange;
                foreach (MoveAbilityTargetData moveTarget in moveAbility.GetTargetsData(pathRequest))
                {
                    yield return moveTarget;
                }
            }

            private static bool CanMeleeAttackFrom(Vector3 attackerPosition, Vector3 targetPosition, float meleeRange)
            {
                return Utl.LesserThanOrEqualTo(
                    Vector3.Distance(attackerPosition, targetPosition),
                    meleeRange + RangeTolerance,
                    1E-05f);
            }

            private static bool SameMovePosition(Vector3 a, Vector3 b)
            {
                return Utl.Equals(a, b, MovePositionMatchTolerance);
            }
        }
    }

    internal sealed class MeleeThreatMarkerVisual : MonoBehaviour
    {
        private const string IconObjectName = "TFTV_MeleeThreatClassIcon";
        private const float IconHeight = 0.03f;
        private const float IconSpacing = 0.48f;
        private const float DesiredSingleIconSize = 0.8f;

        private readonly List<SpriteRenderer> _icons = new List<SpriteRenderer>();
        private Renderer[] _stockRenderers;
        private bool[] _stockRendererEnabledStates;

        private void Awake()
        {
            _stockRenderers = GetComponentsInChildren<Renderer>(true);
            _stockRendererEnabledStates = _stockRenderers
                .Select(renderer => renderer.enabled)
                .ToArray();
        }

        public void ShowIcons(IList<Sprite> sprites, Color color)
        {
            HideIconsAndRestoreMarker();
            if (sprites == null || sprites.Count == 0)
            {
                return;
            }

            // Delete this loop if you want the icons to overlay the original marker.
            foreach (Renderer stockRenderer in _stockRenderers)
            {
                if (stockRenderer != null)
                {
                    stockRenderer.enabled = false;
                }
            }

            int columns = Mathf.CeilToInt(Mathf.Sqrt(sprites.Count));
            int rows = Mathf.CeilToInt((float)sprites.Count / columns);
            float countScale = 1f / Mathf.Max(columns, rows);

            for (int i = 0; i < sprites.Count; i++)
            {
                SpriteRenderer iconRenderer = GetOrCreateIcon(i);
                Sprite sprite = sprites[i];
                int column = i % columns;
                int row = i / columns;

                float width = Mathf.Max(0.01f, sprite.bounds.size.x);
                float height = Mathf.Max(0.01f, sprite.bounds.size.y);
                float normalizedScale = DesiredSingleIconSize / Mathf.Max(width, height);

                iconRenderer.sprite = sprite;
                iconRenderer.color = color;
                iconRenderer.transform.localPosition = new Vector3(
                    (column - (columns - 1) * 0.5f) * IconSpacing,
                    IconHeight,
                    (row - (rows - 1) * 0.5f) * IconSpacing);
                iconRenderer.transform.localScale = Vector3.one * normalizedScale * countScale;
                iconRenderer.gameObject.SetActive(true);
            }
        }

        private SpriteRenderer GetOrCreateIcon(int index)
        {
            while (_icons.Count <= index)
            {
                GameObject iconObject = new GameObject(IconObjectName);
                iconObject.transform.SetParent(transform, false);
                iconObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
                iconRenderer.sortingOrder = 100;
                _icons.Add(iconRenderer);
            }

            return _icons[index];
        }

        private void OnDisable()
        {
            HideIconsAndRestoreMarker();
        }

        private void HideIconsAndRestoreMarker()
        {
            foreach (SpriteRenderer icon in _icons)
            {
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.gameObject.SetActive(false);
                }
            }

            if (_stockRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _stockRenderers.Length; i++)
            {
                Renderer stockRenderer = _stockRenderers[i];
                if (stockRenderer != null)
                {
                    stockRenderer.SetPropertyBlock(null);
                    stockRenderer.enabled = _stockRendererEnabledStates[i];
                }
            }
        }
    }
}