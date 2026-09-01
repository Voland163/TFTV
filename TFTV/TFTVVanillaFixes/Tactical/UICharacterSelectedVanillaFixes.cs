using Base;
using Base.Core;
using Base.Defs;
using Base.Input;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class UICharacterSelectedVanillaFixes
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static InputAction ShowPerceptionCircles = new InputAction();
        public static bool ShowPerceptionCirclesBindingApplied;

        private static int _showBoolCircles = 1;
        private static bool _updatingPerceptionCircles;

        public static void OnInputEvent(object __instance, InputEvent ev)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (!ShowPerceptionCirclesBindingApplied)
                {
                    FieldInfo contextField = typeof(TacticalView).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
                    TacticalViewContext viewContext = (TacticalViewContext)contextField.GetValue(controller.View);
                    InputController inputController = viewContext.Input;
                    FieldInfo actionsField = inputController.GetType().GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    InputAction[] inputActions = (InputAction[])actionsField.GetValue(inputController);

                    if (!inputActions.Any(action => action != null && action.Name == ShowPerceptionCircles.Name))
                    {
                        inputController.ApplyKeybinding(ShowPerceptionCircles);
                    }

                    ShowPerceptionCirclesBindingApplied = true;
                }

                if (ev.Type != InputEventType.Pressed || ev.Name != ShowPerceptionCircles.Name)
                {
                    return;
                }

                _showBoolCircles = (_showBoolCircles + 1) % 3;
                RefreshPerceptionCircles(controller);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool ShouldShowPerceptionCircles(TacticalLevelController controller, TacticalActor selectedActor)
        {
            if (selectedActor == null || controller.CurrentFaction == null)
            {
                return false;
            }

            // CurrentFaction is already assigned while the first player character
            // is selected, but IsPlayingTurn/IsViewerFaction may not be finalized
            // until later in tactical-view initialization. Requiring those flags
            // makes the first selection fail and the second selection succeed.
            // Matching the selected actor to the player-controlled current faction
            // is sufficient; the turn-end patch below removes the markers before
            // another faction starts moving.
            if (!controller.CurrentFaction.IsControlledByPlayer
                || selectedActor.TacticalFaction != controller.CurrentFaction)
            {
                return false;
            }

            return _showBoolCircles == 2
                || (_showBoolCircles == 1 && CheckCharacterInfiltratorOrLazarus(selectedActor));
        }

        private static float GetPerceptionCircleRange(TacticalActor selectedActor, TacticalActor enemyActor)
        {
            // Keep the expanded detection calculation local to perception-circle
            // UI. In particular, do not change ReturnFireMarkerCreator, which also
            // calls GetPossibleVisionRangeTowardsMe.
            float range = selectedActor.GetPossibleVisionRangeTowardsMe(enemyActor);
            range = Math.Max(range, enemyActor.CharacterStats.HearingRange.Value.EndValue);
            range = Math.Max(range, selectedActor.TacticalLevel.TacticalLevelControllerDef.DetectionRange);

            SurveillanceAbility surveillance = enemyActor.GetAbility<SurveillanceAbility>();
            if (surveillance != null)
            {
                range = Math.Max(
                    range,
                    surveillance.SurveillanceAbilityDef.TargetingDataDef.Origin.Range);
            }

            return range;
        }

        [HarmonyPatch]
        private static class UIStateCharacterSelected_EnemyVisionMarkerCreator_Patch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    "PhoenixPoint.Tactical.View.ViewStates.UIStateCharacterSelected:EnemyVisionMarkerCreator");
            }

            private static void Postfix(object context, ref GroundMarker __result)
            {
                try
                {
                    TacticalActor enemyActor = context as TacticalActor;
                    TacticalLevelController controller = enemyActor?.TacticalLevel;
                    TacticalActor selectedActor = controller?.View.SelectedActor;

                    if (__result == null
                        || enemyActor == null
                        || selectedActor == null)
                    {
                        return;
                    }

                    // Patch only the enemy-perception hover marker. The return-fire
                    // marker keeps the unmodified possible-vision result.
                    __result.StartScale = 2.05f
                        * GetPerceptionCircleRange(selectedActor, enemyActor)
                        * Vector3.one;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static void RefreshPerceptionCircles(TacticalLevelController controller)
        {
            if (_updatingPerceptionCircles)
            {
                return;
            }

            try
            {
                _updatingPerceptionCircles = true;

                // Enemy vision spheres share the Selection group with the vanilla
                // implementation. Replace that group before every draw so the
                // vanilla and mod paths cannot leave two spheres per enemy. This
                // also avoids ClearGroundMarkers(), which clears every UI group.
                controller.View.Markers.ClearGroundMarkers(GroundMarkerGroup.Selection);

                TacticalActor selectedActor = controller.View.SelectedActor;
                if (!ShouldShowPerceptionCircles(controller, selectedActor))
                {
                    return;
                }

                IEnumerable<TacticalActor> enemies = selectedActor.TacticalFaction.Vision
                    .GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                    .OfType<TacticalActor>()
                    .Where(actor => actor.InPlay && actor.Interactable)
                    .Distinct();

                foreach (TacticalActor enemy in enemies)
                {
                    GroundMarker marker = new GroundMarker(GroundMarkerType.EnemyVisionSphere, enemy.VisionPoint, 0f)
                    {
                        // Match UIStateCharacterSelected.DrawAllEnemyVisionMarkers.
                        StartScale = 2.05f * GetPerceptionCircleRange(selectedActor, enemy) * Vector3.one
                    };

                    controller.View.Markers.AddGroundMarker(GroundMarkerGroup.Selection, marker, false);
                }
            }
            finally
            {
                _updatingPerceptionCircles = false;
            }
        }

        [HarmonyPatch(
            typeof(TacticalGroundMarkers),
            nameof(TacticalGroundMarkers.ClearGroundMarkers),
            new Type[] { typeof(GroundMarkerGroup) })]
        private static class TacticalGroundMarkers_ClearGroundMarkers_Patch
        {
            private static void Postfix(GroundMarkerGroup group)
            {
                try
                {
                    // Tactical-view initialization performs another Selection clear
                    // after the first character-selection postfix. Restore the
                    // circles after that clear instead of letting them flash once
                    // and disappear. The refresh guard prevents its own Selection
                    // clear from recursively drawing a second set of circles.
                    if (group != GroundMarkerGroup.Selection || _updatingPerceptionCircles)
                    {
                        return;
                    }

                    TacticalLevelController controller = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();
                    if (controller != null
                        && ShouldShowPerceptionCircles(controller, controller.View.SelectedActor))
                    {
                        RefreshPerceptionCircles(controller);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static void PatchShowEnemyVisionMarkers(object __instance, MethodBase __originalMethod, TacticalActor character)
        {
            try
            {
                TacticalLevelController controller = character?.TacticalLevel;
                if (controller != null)
                {
                    // This is the sole mod drawing path. It first removes any
                    // spheres drawn by the vanilla SelectCharacter implementation.
                    RefreshPerceptionCircles(controller);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(TacticalView), "OnViewerFactionEndedTurn")]
        private static class TacticalView_OnViewerFactionEndedTurn_Patch
        {
            private static void Prefix(TacticalFaction prevFaction)
            {
                try
                {
                    if (!prevFaction.IsViewerFaction)
                    {
                        return;
                    }

                    TacticalLevelController controller = prevFaction.TacticalLevel;
                    controller.View.Markers.ClearGroundMarkers(GroundMarkerGroup.Selection);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        public static void PatchResizeGroundMarker(MethodBase __originalMethod, object context, ref GroundMarker __result)
        {
            try
            {
                if (__result != null)
                {
                    __result.StartScale /= 2.05f;
                    __result.StartScale *= 1.6f;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        //Shows correct movement info and more info in tactical tooltip; also shows info for currently selected character

        private static Sprite _vivisectionIcon = null;
        private static Sprite _moonIcon = null;



        [HarmonyPatch(typeof(UIStateCharacterStatus), "GetActionPoints")] //VERIFIED
        public static class UIStateCharacterStatus_GetActionPoints_patch
        {

            public static bool Prefix(ref UIModuleCharacterStatus.CharacterData.ValueBarData __result, TacticalActor character)
            {
                try
                {

                    int maxActionPoints = GetAdjustedSpeedValueForParalyisDamage(character);

                    __result = new UIModuleCharacterStatus.CharacterData.ValueBarData
                    {
                        Max = maxActionPoints,
                        Limit = maxActionPoints,
                        Current = Mathf.Min(character.CharacterStats.ActionPoints.IntValue, maxActionPoints),
                        Overcharge = 0f
                    };

                    if (character.TacticalLevel.CurrentFaction != character.TacticalFaction)
                    {
                        __result.Current = maxActionPoints;
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


        private static int GetAdjustedSpeedValueForParalyisDamage(TacticalActor actor)
        {
            try
            {
                ParalysisDamageOverTimeStatus status = actor.Status.GetStatus<ParalysisDamageOverTimeStatus>();

                int value = (int)actor.MaxActionPoints;

                if (status == null)
                {
                    return value;
                }
                else
                {
                    float paralysisDamage = status.FullDamageValue;
                    float actorStrength = (float)actor.Status.GetStat(StatModificationTarget.Endurance.ToString());
                    float a = paralysisDamage / actorStrength;

                    if (Utl.GreaterThanOrEqualTo(a, 1f))
                    {
                        return 0;
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.75f))
                    {
                        return (int)(value * 0.25f);
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.5f))
                    {
                        return (int)(value * 0.5f);
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.25f))
                    {
                        return (int)(value * 0.75f);
                    }
                    else
                    {
                        return value;
                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static ShortActorInfoTooltipData GenerateData(TacticalActor actor, UIModuleShortActorInfoTooltip uIModuleShortActorInfoTooltip)
        {
            try
            {
                ShortActorInfoTooltipData data = default;

                data.Entries = new List<ShortActorInfoTooltipDataEntry>();
                data.TrackRoot = actor?.gameObject;

                if (actor == null)
                {
                    TFTVLogger.Always("[GenerateData] actor == null");
                    data.Entries.Add(new ShortActorInfoTooltipDataEntry { TextContent = "UNKNOWN", ValueContent = string.Empty });
                    return data;
                }
                if (uIModuleShortActorInfoTooltip == null)
                {
                    //  TFTVLogger.Always($"[GenerateData] tooltip module == null for actor={actor.DisplayName}");
                }


                data.Entries.Add(new ShortActorInfoTooltipDataEntry
                {
                    TextContent = actor.DisplayName.ToUpper(),
                    ValueContent = string.Empty
                });

                data.Entries.Add(new ShortActorInfoTooltipDataEntry
                {
                    TextContent = uIModuleShortActorInfoTooltip.HealthTextKey.Localize(null),
                    ValueContent = string.Format("{0}/{1}", actor.CharacterStats.Health.IntValue, actor.CharacterStats.Health.IntMax)
                });
                data.Entries.Add(new ShortActorInfoTooltipDataEntry
                {
                    TextContent = uIModuleShortActorInfoTooltip.WillpointsTextKey.Localize(null),
                    ValueContent = string.Format("{0}/{1}", actor.CharacterStats.WillPoints.IntValue, actor.CharacterStats.WillPoints.IntMax)
                });

                int maxActionPoints = GetAdjustedSpeedValueForParalyisDamage(actor); //actor.CharacterStats.ActionPoints.IntMax);

                string value = $"{maxActionPoints}";//string.Format("{0}/{1}", maxActionPoints, maxActionPoints);
                if (actor.TacticalLevel.CurrentFaction == actor.TacticalFaction)
                {
                    value = string.Format("{0}/{1}", Mathf.Min(actor.CharacterStats.ActionPoints.IntValue, maxActionPoints), maxActionPoints);
                }

                data.Entries.Add(new ShortActorInfoTooltipDataEntry
                {
                    TextContent = TFTVCommonMethods.ConvertKeyToString("KEY_MOVEMENT"),
                    ValueContent = value
                });

                string perceptionDescription = TFTVCommonMethods.ConvertKeyToString("KEY_PROGRESSION_PERCEPTION");
                string perceptionValue = Mathf.RoundToInt(actor.GetAdjustedPerceptionValue()).ToString(); //Perception.IntValue.ToString();

                ShortActorInfoTooltipDataEntry perception = new ShortActorInfoTooltipDataEntry()
                {
                    TextContent = perceptionDescription,
                    ValueContent = perceptionValue
                };

                string stealthDescription = TFTVCommonMethods.ConvertKeyToString("KEY_ROSTER_STAT_STEALTH");
                float stealthFloatValue = actor.CharacterStats.Stealth.Value.EndValue * 100;
                string stealthValue = $"{(stealthFloatValue > 0 ? "+" : string.Empty)}{Mathf.Round(stealthFloatValue)}%";

                ShortActorInfoTooltipDataEntry stealth = new ShortActorInfoTooltipDataEntry()
                {
                    TextContent = stealthDescription,
                    ValueContent = stealthValue
                };

                string accuracyDescription = TFTVCommonMethods.ConvertKeyToString("KEY_PROGRESSION_ACCURACY");
                float accuracyFloatValue = actor.CharacterStats.Accuracy.Value.EndValue * 100;
                string accuracyValue = $"{(accuracyFloatValue > 0 ? "+" : string.Empty)}{Mathf.Round(accuracyFloatValue)}%";

                ShortActorInfoTooltipDataEntry accuracy = new ShortActorInfoTooltipDataEntry()
                {
                    TextContent = accuracyDescription,
                    ValueContent = accuracyValue
                };

                data.Entries.Add(perception);
                data.Entries.Add(stealth);
                data.Entries.Add(accuracy);

                TacticalActor selectedActor = actor.TacticalLevel.View.SelectedActor;

                var view = actor.TacticalLevel?.View;
                // TFTVLogger.Always($"[GenerateData] actor={actor.DisplayName}, view={(view != null)}, selectedActor={(selectedActor != null)}, actorView={(actor.TacticalActorView != null)}");

                if (selectedActor != null && selectedActor.Status != null)
                {
                    DamageMultiplierStatusDef moonProject = DefCache.GetDef<DamageMultiplierStatusDef>("E_Status [DamageBonusToAliens_FactionEffectDef]");
                    //   Sprite moonProjectIcon = DefCache.GetDef<ViewElementDef>("E_ViewElement [MoonLaunch_GeoHavenZoneDef]").SmallIcon;

                    DamageMultiplierStatus damageMultiplierStatusVivisection =
 selectedActor.Status.Statuses
 .OfType<DamageMultiplierStatus>()
 .FirstOrDefault(d =>
     d.DamageMultiplierStatusDef.OutgoingDamageTargetTags.Count() > 0
     && d.DamageMultiplierStatusDef.OutgoingDamageTargetTags[0] is ClassTagDef classTag
     && actor.HasGameTag(classTag));


                    if (damageMultiplierStatusVivisection != null)
                    {
                        float multiplier = damageMultiplierStatusVivisection.DamageMultiplierStatusDef.Multiplier;

                        if (multiplier > 0)
                        {
                            if (_vivisectionIcon == null)
                            {
                                _vivisectionIcon = Helper.CreateSpriteFromImageFile("vivisection_icon.png");

                            }

                            //  TFTVLogger.Always($"adding vivisection status to {actor.name} from {damageMultiplierStatusVivisection.DamageMultiplierStatusDef.name}");

                            data.Entries.Add(new ShortActorInfoTooltipDataEntry
                            {
                                Icon = _vivisectionIcon,
                                IconColor = new Color(1, 1, 1, 1),
                                TextContent = TFTVCommonMethods.ConvertKeyToString("TFTV_VIVISECTED_SHORT_INFO"),
                                ValueContent = $"{multiplier * 100 - 100}% {TFTVCommonMethods.ConvertKeyToString("TFTV_VIVISECTED_SHORT_INFO_DAMAGE")}" // Adjust based on the actual multiplier field
                            });
                        }
                    }

                    if (selectedActor.HasStatus(moonProject) && actor.HasGameTag(Shared.SharedGameTags.AlienTag))
                    {

                        if (_moonIcon == null)
                        {
                            _moonIcon = Helper.CreateSpriteFromImageFile("moon_icon.png");
                        }

                        data.Entries.Add(new ShortActorInfoTooltipDataEntry
                        {
                            Icon = _moonIcon,
                            IconColor = new Color(1, 1, 1, 1),
                            TextContent = TFTVCommonMethods.ConvertKeyToString("TFTV_MOONPROJECT_SHORT_INFO"),
                            ValueContent = $"{moonProject.Multiplier * 100 - 100}% {TFTVCommonMethods.ConvertKeyToString("TFTV_VIVISECTED_SHORT_INFO_DAMAGE")}" // Adjust based on the actual multiplier field
                        });
                    }

                }

                foreach (TacticalActorViewBase.StatusInfo statusInfo in SafeGetStatusInfos(actor, "GenerateData"))
                {

                    if (statusInfo == null)
                    {
                        TFTVLogger.Always($"[GenerateData] statusInfo == null (actor={actor.DisplayName})");
                        continue;
                    }
                    if (statusInfo.Def == null)
                    {
                        TFTVLogger.Always($"[GenerateData] statusInfo.Def == null (actor={actor.DisplayName})");
                        continue;
                    }

                    if (statusInfo.Def.VisibleOnHealthbar != TacStatusDef.HealthBarVisibility.Hidden)
                    {
                        if (statusInfo.Def != TFTVRevenant.RevenantResistanceStatus)
                        {

                            ShortActorInfoTooltipDataEntry item = new ShortActorInfoTooltipDataEntry
                            {
                                Icon = statusInfo.Def.Visuals.SmallIcon,
                                IconColor = statusInfo.Def.Visuals.Color,
                                TextContent = statusInfo.Def.Visuals.DisplayName1.Localize(null),
                                ValueContent = string.Format("{0}/{1}", statusInfo.Value, statusInfo.Limit)
                            };
                            if (float.IsNaN(statusInfo.Value) && float.IsNaN(statusInfo.Limit) || statusInfo.Def is ArmorStackStatusDef)
                            {
                                item.ValueContent = string.Empty;
                            }
                            else if (float.IsNaN(statusInfo.Limit))
                            {
                                item.ValueContent = string.Format("{0}", statusInfo.Value);
                            }
                            data.Entries.Add(item);

                            // Acid is a sum across body parts; break it out under its own row so the
                            // single number does not read as one pool.
                            TFTVUI.Tactical.AcidReadout.AppendAcidBreakdown(data.Entries, actor, statusInfo.Def);
                        }
                        else
                        {
                            string displayName = statusInfo.Def.Visuals.DisplayName1.Localize(null);

                            // TFTVLogger.Always($"displayName: {displayName}");

                            string[] parts = displayName.Split(new char[] { '-' }, 2);

                            string title = parts[0]; // "part1"
                                                     // TFTVLogger.Always($"title: {title}");
                            string description = parts.Length > 1 ? parts[1] : ""; // "part2"
                            description = $"-50%\n{description.Trim()}";
                            //   TFTVLogger.Always($"description: {description}, statusInfo.Value: {statusInfo.Value}");

                            ShortActorInfoTooltipDataEntry item = new ShortActorInfoTooltipDataEntry
                            {
                                Icon = statusInfo.Def.Visuals.SmallIcon,
                                IconColor = statusInfo.Def.Visuals.Color,
                                TextContent = title,
                                ValueContent = description //string.Format("{0}/{1}", statusInfo.Value, statusInfo.Limit)
                            };
                            if (float.IsNaN(statusInfo.Value) && float.IsNaN(statusInfo.Limit) || statusInfo.Def is ArmorStackStatusDef)
                            {
                                //item.ValueContent = string.Empty;
                            }
                            else
                            {
                                item.ValueContent = $"{description} {statusInfo.Value}";
                            }

                            data.Entries.Add(item);

                        }
                    }
                }

                return data;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static IEnumerable<TacticalActorViewBase.StatusInfo> SafeGetStatusInfos(TacticalActor actor, string callerTag)
        {
            try
            {
                if (actor == null)
                {
                    TFTVLogger.Always($"[StatusInfos:{callerTag}] actor == null");
                    return Enumerable.Empty<TacticalActorViewBase.StatusInfo>();
                }
                if (actor.TacticalActorView == null)
                {
                    TFTVLogger.Always($"[StatusInfos:{callerTag}] actor.TacticalActorView == null (actor={actor.DisplayName})");
                    return Enumerable.Empty<TacticalActorViewBase.StatusInfo>();
                }

                var infos = actor.TacticalActorView.GetCharacterStatusActorStatuses();
                if (infos == null)
                {
                    TFTVLogger.Always($"[StatusInfos:{callerTag}] GetCharacterStatusActorStatuses returned null (actor={actor.DisplayName})");
                    return Enumerable.Empty<TacticalActorViewBase.StatusInfo>();
                }

                return infos;
            }
            catch (Exception ex)
            {
                TFTVLogger.Always($"[StatusInfos:{callerTag}] Exception while fetching statuses for actor={actor?.DisplayName}: {ex}");
                return Enumerable.Empty<TacticalActorViewBase.StatusInfo>();
            }
        }

        [HarmonyPatch(typeof(UIModuleTacticalContextualMenu), "OnAbilityHover")] //VERIFIED
        public static class UIModuleTacticalContextualMenu_OnAbilityHover_patch
        {
            public static void Postfix(bool isHovered, TacticalContextualMenuItem menuItem, UIModuleTacticalContextualMenu __instance)
            {
                try
                {
                    var ctrl = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();
                    var actor = __instance?.SelectionInfo.Actor as TacticalActor;
                    // TFTVLogger.Always($"[OnAbilityHover] hovered={isHovered}, infoButton={menuItem?.InfoButton ?? false}, ctrl={(ctrl != null)}, actor={(actor != null)}, actorView={(actor?.TacticalActorView != null)}");

                    if (!isHovered || menuItem == null || !menuItem.InfoButton || ctrl == null || actor == null)
                    {
                        return;
                    }

                    var view = ctrl.View;
                    if (view == null || view.TacticalModules == null || view.SelectedActor == null)
                    {
                        //   TFTVLogger.Always($"[OnAbilityHover] view/modules/selectedActor not ready (view={(view != null)}, modules={(view?.TacticalModules != null)}, selectedActor={(view?.SelectedActor != null)})");
                        return;
                    }

                    if (!actor.IsControlledByPlayer || view.ViewerFaction != actor.TacticalFaction)
                    {
                        // TFTVLogger.Always($"[OnAbilityHover] actor not player-controlled or viewer mismatch (player={actor.IsControlledByPlayer}, viewerMatch={view.ViewerFaction == actor.TacticalFaction})");
                        return;
                    }

                    ShowShortInfoTooltipSelectedActor(actor, ctrl);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void ShowShortInfoTooltipSelectedActor(TacticalActor actor, TacticalLevelController controller)
        {
            try
            {

                UIModuleShortActorInfoTooltip uIModuleShortActorInfoTooltip = controller.View.TacticalModules.ShortActorTooltipModule;

                // uIModuleShortActorInfoTooltip.InitTooltip(controller.GetComponent<UIObjectTrackersController>());

                uIModuleShortActorInfoTooltip.SetData(GenerateData(actor, uIModuleShortActorInfoTooltip));

                if (!uIModuleShortActorInfoTooltip.IsShown)
                {
                    uIModuleShortActorInfoTooltip.Show();
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }





        public static void PrepareShortActorInfo(TacticalActor actor, ref ShortActorInfoTooltipData __result)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                UIModuleShortActorInfoTooltip uIModuleShortActorInfoTooltip = controller.View.TacticalModules.ShortActorTooltipModule;

                __result = GenerateData(actor, uIModuleShortActorInfoTooltip);
                // TFTVLogger.Always($"{GenerateData(actor, uIModuleShortActorInfoTooltip).TrackRoot.name}");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
        

        //Fixes size of ground marker for eggs/sentinels etc.
        public static void PatchInternalClassUIStateCharacterSelecter(Harmony harmony)
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly assembly = null;
                foreach (Assembly a in assemblies)
                {
                    if (a.GetName().Name.Contains("Assembly-CSharp"))
                    {
                        assembly = a;
                    }
                }
                Type internalType = assembly.GetType("PhoenixPoint.Tactical.View.ViewStates.UIStateCharacterSelected");

                if (internalType != null)
                {
                    MethodInfo zoneOfControlMarkerCreatorMethod = internalType.GetMethod("ZoneOfControlMarkerCreator", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo prepareShortActorInfoMethod = internalType.GetMethod("PrepareShortActorInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo selectCharacterInfoMethod = internalType.GetMethod("SelectCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                   

                    MethodInfo activateAttackAbilityState = internalType.GetMethod("ActivateAttackAbilityState", BindingFlags.NonPublic | BindingFlags.Instance);

                    //   MethodInfo updateStateInfoMethod = internalType.GetMethod("UpdateState", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo onInputEvenMethodInfo = internalType.GetMethod("OnInputEvent", BindingFlags.NonPublic | BindingFlags.Instance);


                    if (zoneOfControlMarkerCreatorMethod != null)
                    {
                        harmony.Patch(zoneOfControlMarkerCreatorMethod, postfix: new HarmonyMethod(typeof(UICharacterSelectedVanillaFixes), nameof(PatchResizeGroundMarker)));
                    }
                    if (prepareShortActorInfoMethod != null)
                    {
                        // TFTVLogger.Always($"patch should be running");
                        harmony.Patch(prepareShortActorInfoMethod, postfix: new HarmonyMethod(typeof(UICharacterSelectedVanillaFixes), nameof(PrepareShortActorInfo)));
                    }
                    if (selectCharacterInfoMethod != null)
                    {
                        //  TFTVLogger.Always($"updateStateInfoMethod patch should be running");
                        harmony.Patch(selectCharacterInfoMethod, postfix: new HarmonyMethod(typeof(UICharacterSelectedVanillaFixes), nameof(PatchShowEnemyVisionMarkers)));
                    }

                    if (activateAttackAbilityState != null)
                    {
                        harmony.Patch(activateAttackAbilityState, postfix: new HarmonyMethod(typeof(UICharacterSelectedVanillaFixes), nameof(ActivateAttackAbilityState)));
                    }

                    //  if(EnemyVisionMarkerCreatorMethodInfo != null) 
                    //  {
                    //  harmony.Patch(EnemyVisionMarkerCreatorMethodInfo, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(PatchEnemyVisionMarkerCreator)));
                    //   }

                    if (onInputEvenMethodInfo != null)
                    {
                        //  TFTVLogger.Always($"patch should be running");
                        harmony.Patch(onInputEvenMethodInfo, postfix: new HarmonyMethod(typeof(UICharacterSelectedVanillaFixes), nameof(OnInputEvent)));
                    }
                    /*   if (selectCharacterInfoMethod != null)
                       {
                           // TFTVLogger.Always($"patch should be running");
                           harmony.Patch(selectCharacterInfoMethod, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes), nameof(SelectCharacter)));
                       }*/
                }
                else
                {

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static TacticalActorBase _enemyActorTargeted = null;


        public static bool CheckIfEnemyActorTargeted()
        {
            try
            {
                if (_enemyActorTargeted != null)
                {
                    //TFTVLogger.Always($"Chasing {__instance?.name} param: {chaseTransform} {lockInput} {instant} {chaseOnlyOutsideFrame}");
                    _enemyActorTargeted = null;
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        private static void ActivateAttackAbilityState(object __instance, bool fps, TacticalActorBase targetActor = null)
        {
            try
            {
                // TFTVLogger.Always($"fps {fps} actor? {targetActor?.name}");

                if (targetActor != null)
                {
                    _enemyActorTargeted = targetActor;
                }
                else
                {
                    _enemyActorTargeted = null;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

      
        private static bool CheckCharacterInfiltratorOrLazarus(TacticalActor character)
        {
            try
            {
                bool lazarusScarab = false;
                if (character.BodyState != null && character.BodyState.GetVehicleModules() != null && character.BodyState.GetVehicleModules().Any(e => e.TacticalItemDef == (GroundVehicleModuleDef)Repo.GetDef("983eb90b-29bf-15e4-fa76-d7f731069bd1")))
                {
                    lazarusScarab = true;
                }

                foreach (TacticalFaction faction in character.TacticalLevel.Factions.Where(f => f.GetRelationTo(character.TacticalFaction) == FactionRelation.Enemy))
                {
                    if (faction.Vision.IsRevealed(character))
                    {
                        return false;
                    }
                }

                if (character.GameTags.Contains(DefCache.GetDef<GameTagDef>("Infiltrator_ClassTagDef"))
                    || lazarusScarab)
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(InputController), "GetDefaultAction", typeof(int))] //VERIFIED
        public static class InputController_GetDefaultAction_patch
        {
            public static bool Prefix(InputController __instance, int hash, ref InputAction __result)
            {
                try
                {
                    /*   if (_missingInputActions.Count > 0) 
                       { 
                       TFTVLogger.Always($"_missingInputActions.Count: {_missingInputActions.Count}");

                       }*/


                    if (__instance.AllActionMap.IsEmpty<InputAction>())
                    {
                        __instance.AllActionMap.Clear();
                        __instance.AllActionMap.AddRange(__instance.DefaultInputMap.Actions);

                        /*   if (_missingInputActions.Count > 0) 
                           {
                               TFTVLogger.Always($"adding _missingInputActions to AllActionMap");
                           __instance.AllActionMap.AddRange(_missingInputActions);
                           }*/

                    }
                    if (hash < __instance.AllActionMap.Count && hash != InputCache.InvalidHash)
                    {
                        __result = __instance.AllActionMap[hash];
                        return false;
                    }

                    InputRebindingComponent inputRebindingComponent = GameUtl.GameComponent<PhoenixGame>().GetComponent<InputRebindingComponent>();

                    List<InputAction> overrides = new List<InputAction>();
                    foreach (object obj in inputRebindingComponent.BindingsOverrides.Values.Values)
                    {
                        if (obj is InputAction inputAction)
                        {
                            overrides.Add(inputAction);
                        }
                    }

                    /*  if (_missingInputActions.Count > 0)
                      {
                          TFTVLogger.Always($"adding _missingInputActions to overrides");
                          overrides.AddRange(_missingInputActions);

                      }*/


                    __instance.ApplyKeybindings(overrides);

                    if (hash < __instance.AllActionMap.Count && hash != InputCache.InvalidHash)
                    {
                        __result = __instance.AllActionMap[hash];
                        return false;
                    }

                    __result = null;

                    TFTVLogger.Always($"{hash} is null!, __instance.AllActionMap.Count: {__instance.AllActionMap.Count} ");

                    foreach (InputAction inputAction in __instance.AllActionMap)
                    {
                        TFTVLogger.Always($"__instance.AllActionMap: {inputAction.Name}, {inputAction.Hash}, {inputAction.Chords[0]?.Keys[0]?.Name}");
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

    }
}
