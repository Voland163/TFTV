using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.AI;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Input;
using Base.Levels;
using Base.Rendering.ObjectRendering;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.SiteEncounters;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using SETUtil.Common.Extend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PhoenixPoint.Tactical.Entities.Effects.DamageEffect;
using static PhoenixPoint.Tactical.Entities.SquadPortraitsDef;


namespace TFTV
{
    internal class TFTVVanillaFixes
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        internal class Stealth
        {
            private static bool _usingEchoHead = false;

            //Prevents targeting body parts with Destiny and similar of unrevealed characters.

            [HarmonyPatch(typeof(ShootAbility), "GetShootTarget")]
            public static class ShootAbility_GetShootTarget_Patch
            {
                public static void Postfix(ShootAbility __instance,
                    TacticalAbilityTarget target, ref TacticalAbilityTarget __result)// Vector3? sourcePosition = null, TacticalTargetData targetData = null, )
                {
                    try
                    {
                        if (__instance.ShootAbilityDef.SnapToBodyparts)
                        {
                            TacticalActor tacticalActor = target.Actor as TacticalActor;
                            if (tacticalActor != null && !tacticalActor.IsRevealedToViewer)
                            {
                                __result = null;

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


            [HarmonyPatch(typeof(TacticalAbility), "get_EquipmentWithTags")]
            public static class TFTV_TacticalAbility_get_EquipmentWithTags
            {
                public static void Postfix(TacticalAbility __instance, ref Equipment __result)
                {
                    try
                    {
                        if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                        {
                            if (__instance.SelectedEquipment != null && __instance.SelectedEquipment.GameTags.Contains(DefCache.GetDef<GameTagDef>("SilencedWeapon_TagDef")))
                            {
                                __result = null;
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


            [HarmonyPatch(typeof(ShootAbility), "Activate")]
            public static class TFTV_ShootAbility_Activate
            {
                public static void Prefix(ShootAbility __instance)
                {
                    try
                    {
                        if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                        {
                            _usingEchoHead = true;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(Weapon), "IsAttackSilent")]
            public static class TFTV_Weapon_IsAttackSilent
            {
                public static void Postfix(Weapon __instance, ref bool __result)
                {
                    try
                    {
                        if (_usingEchoHead)
                        {
                            __result = true;
                            _usingEchoHead = false;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(TacticalFactionVision), "LocateRandomEnemyIfNeeded")]
            public static class TFTV_TacticalFactionVision_LocateRandomEnemyIfNeeded
            {
                public static bool Prefix(TacticalFactionVision __instance)
                {
                    try
                    {
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            //Prevents AI from consindering unseen enemies when evaluating attacks with explosives/cone weapons
            private static bool CheckVisibility(TacticalActorBase tacticalActorBase, TacticalActor tacticalActor, DamagePayload damagePayload)
            {
                try
                {
                    if (damagePayload.DamageDeliveryType == DamageDeliveryType.Sphere || damagePayload.DamageDeliveryType == DamageDeliveryType.Cone)
                    {
                        if (tacticalActor.TacticalFaction == tacticalActorBase.TacticalFaction)
                        {
                            return true;
                        }

                        if (tacticalActor.TacticalFaction.GetAllAliveFriendlyActors<TacticalActorBase>(tacticalActor).Contains(tacticalActorBase))
                        {
                            return true;
                        }

                        if (tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(ActorType.All, true).Contains(tacticalActorBase))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(AIUtil), "GetAffectedTargetsByShooting")]
            public static class TFTV_AIUtil_GetAffectedTargetsByShooting_patch
            {
                private static IEnumerable<TacticalActorBase> Postfix(IEnumerable<TacticalActorBase> results, Vector3 shootPos, TacticalActor sourceActor, Weapon sourceWeapon, TacticalAbilityTarget target, ShootAbilityDef shootAbility = null)
                {

                    DamagePayload damagePayload = sourceWeapon.GetDamagePayload();

                    foreach (TacticalActorBase actorBase in results)
                    {
                        if (CheckVisibility(actorBase, sourceActor, damagePayload))
                        {
                            yield return actorBase;
                        }

                    }
                }

            }



            //Prevents evacuated characters from spotting enemies

            [HarmonyPatch(typeof(TacticalFactionVision), "ReUpdateVisibilityTowardsActorImpl")]
            public static class TFTV_TacticalFactionVision_ReUpdateVisibilityTowardsActorImpl_patch
            {
                private static bool Prefix(TacticalActorBase fromActor, TacticalActorBase targetActor, float basePerceptionRange, ref bool __result)
                {
                    try
                    {
                        if (fromActor is TacticalActor tacticalActor && tacticalActor.IsEvacuated)
                        {
                            __result = false;
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
            }

        }

        internal class UI
        {
            //Fixes scanner showing colony detected for Palace
            [HarmonyPatch(typeof(SiteSurroundingsScanner), "AlienBasesAvailableInRange")]
            public static class SiteSurroundingsScanner_AlienBasesAvailableInRange_patch
            {

                public static void Postfix(SiteSurroundingsScanner __instance, GeoSite ____site, ref bool __result)
                {
                    try
                    {
                        Func<GeoSite, bool> querry = (GeoSite s) => s.GetComponent<GeoAlienBase>() != null && !s.GetComponent<GeoAlienBase>().IsPalace && s.GetInspected(____site.Owner) && s.State == GeoSiteState.Functioning;
                        MethodInfo methodInfo = typeof(SiteSurroundingsScanner).GetMethod("QuerryForAlienBases", BindingFlags.NonPublic | BindingFlags.Instance);
                        IEnumerable<GeoSite> eligibleSites = (IEnumerable<GeoSite>)methodInfo.Invoke(__instance, new object[] { querry });

                        __result = eligibleSites.Any();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static MethodInfo _drawAllEnemyVisionMarkersMethodInfo = null;


            //Fixes size of ground marker for eggs/sentinels etc.
            public static void FixSurveillanceAbilityGroundMarker(Harmony harmony)
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
                        _drawAllEnemyVisionMarkersMethodInfo = internalType.GetMethod("DrawAllEnemyVisionMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
                        //   MethodInfo updateStateInfoMethod = internalType.GetMethod("UpdateState", BindingFlags.NonPublic | BindingFlags.Instance);
                        MethodInfo onInputEvenMethodInfo = internalType.GetMethod("OnInputEvent", BindingFlags.NonPublic | BindingFlags.Instance);


                        if (zoneOfControlMarkerCreatorMethod != null)
                        {
                            harmony.Patch(zoneOfControlMarkerCreatorMethod, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(PatchResizeGroundMarker)));
                        }
                        if (prepareShortActorInfoMethod != null)
                        {
                            // TFTVLogger.Always($"patch should be running");
                            harmony.Patch(prepareShortActorInfoMethod, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(PrepareShortActorInfo)));
                        }
                        if (selectCharacterInfoMethod != null)
                        {
                            //  TFTVLogger.Always($"updateStateInfoMethod patch should be running");
                            harmony.Patch(selectCharacterInfoMethod, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(PatchShowEnemyVisionMarkers)));
                        }
                        //  if(EnemyVisionMarkerCreatorMethodInfo != null) 
                        //  {
                        //  harmony.Patch(EnemyVisionMarkerCreatorMethodInfo, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(PatchEnemyVisionMarkerCreator)));
                        //   }

                        if (onInputEvenMethodInfo != null)
                        {
                            //  TFTVLogger.Always($"patch should be running");
                            harmony.Patch(onInputEvenMethodInfo, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(OnInputEvent)));
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


            private static void PatchShowEnemyVisionMarkers(object __instance, MethodBase __originalMethod, TacticalActor character)
            {
                try
                {

                    if (character != null && (_showBoolCircles == 1 && CheckCharacterInfiltratorOrLazarus(character) || _showBoolCircles == 2))
                    {
                        _drawAllEnemyVisionMarkersMethodInfo.Invoke(__instance, new object[] { });
                    }

                    // TFTVUITactical.CaptureTacticalWidget.UpdateCaptureUIPosition(character.TacticalLevel);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static int _showBoolCircles = 1;

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

            [HarmonyPatch(typeof(InputController), "GetDefaultAction", typeof(int))]
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

            //   private static List<InputAction> _missingInputActions = new List<InputAction>();

            public static InputAction ShowPerceptionCircles = new InputAction();

            /*  [HarmonyPatch(typeof(InputController), "RefreshActions")]
              public static class InputController_Init_patch
              {
                  public static void Postfix(InputController __instance, InputAction[] ____activeActionsMap)
                  {
                      try
                      {
                          _missingInputActions.Clear();

                          if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null && !____activeActionsMap.Any(a => a != null && a.Name != null && a.Name == ShowPerceptionCircles.Name))
                          {
                              TFTVLogger.Always($"{ShowPerceptionCircles.Name} not found! adding to the list");
                              _missingInputActions.Add(ShowPerceptionCircles);
                              // __instance.ApplyKeybinding(ShowPerceptionCircles);
                          }
                          else if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                          {
                              List<InputAction> aircraftSelectKeys = TFTVDragandDropFunctionality.VehicleRoster.ActionsAircraftHotkeys;

                              foreach (InputAction inputAction in aircraftSelectKeys.Where(ia => !____activeActionsMap.Contains(ia)))
                              {
                                  TFTVLogger.Always($"{inputAction.Name} not found! adding to the list");
                                  _missingInputActions.Add(inputAction);

                                  //  __instance.ApplyKeybinding(inputAction);

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

            public static bool ShowPerceptionCirclesBindingApplied = false;
            public static void OnInputEvent(object __instance, InputEvent ev)
            {
                try
                {

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (!ShowPerceptionCirclesBindingApplied)
                    {

                        FieldInfo tacticalViewContextFieldInfo = typeof(TacticalView).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
                        TacticalViewContext tacticalViewContext = (TacticalViewContext)tacticalViewContextFieldInfo.GetValue(controller.View);
                        //   TFTVLogger.Always($"tacticalViewContext.Input.GetActiveAction() null? {tacticalViewContext.Input.GetActiveAction("DisplayPerceptionCircles") == null}");
                        //  InputActionState inputKey = tacticalViewContext.Input.GetKey("DisplayPerceptionCircles");
                        InputController inputController = tacticalViewContext.Input;
                        FieldInfo field = inputController.GetType().GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);

                        InputAction[] inputActions = (InputAction[])field.GetValue(inputController);

                        if (!inputActions.Any(a => a != null && a.Name != null && a.Name == ShowPerceptionCircles.Name))
                        {
                            // TFTVLogger.Always($"{ShowPerceptionCircles.Name} not found! adding to the list");
                            inputController.ApplyKeybinding(ShowPerceptionCircles);
                        }
                        ShowPerceptionCirclesBindingApplied = true;
                    }

                    if (ev.Type == InputEventType.Pressed)
                    {

                        TacticalActor selectedCharacter = controller.View.SelectedActor;

                        // TFTVLogger.Always($"tacticalViewContext.Input.GetActiveAction() null? {tacticalViewContext.Input.GetActiveAction("DisplayPerceptionCircles") == null}"); 
                        //  InputActionState inputKey = tacticalViewContext.Input.GetKey("DisplayPerceptionCircles");
                        /*  InputController inputController = tacticalViewContext.Input;
                          FieldInfo field = inputController.GetType().GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);

                          InputAction[] inputActions = (InputAction[])field.GetValue(inputController);

                          if (!inputActions.Any(a => a != null && a.Name != null && a.Name == ShowPerceptionCircles.Name))
                          {
                              TFTVLogger.Always($"{ShowPerceptionCircles.Name} not found! adding to the list");
                              inputController.ApplyKeybinding(ShowPerceptionCircles);
                          }*/


                        //   TFTVLogger.Always($"evName: {ev.Name}");
                        if (ev.Name == ShowPerceptionCircles.Name)
                        {
                            _showBoolCircles += 1;

                            if (_showBoolCircles > 2)
                            {
                                _showBoolCircles = 0;
                            }

                            if (selectedCharacter != null && (_showBoolCircles == 1 && CheckCharacterInfiltratorOrLazarus(selectedCharacter) || _showBoolCircles == 2))
                            {
                                _drawAllEnemyVisionMarkersMethodInfo.Invoke(__instance, new object[] { });
                            }
                            else
                            {
                                controller.View.Markers.ClearGroundMarkers();
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

            [HarmonyPatch(typeof(TacticalGroundMarkers), "ClearGroundMarkers", new Type[] { typeof(GroundMarkerGroup) })]
            public static class TacticalGroundMarkers_ClearGroundMarkers_patch
            {

                public static void Postfix(GroundMarkerGroup group, TacticalGroundMarkers __instance)
                {
                    try
                    {

                        if (group != GroundMarkerGroup.Selection)
                        {
                            return;
                        }

                        if (GameUtl.CurrentLevel() == null || GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() == null)
                        {
                            return;
                        }


                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        TacticalActor selectedActor = controller.View.SelectedActor;

                        if (selectedActor != null && (_showBoolCircles == 1 && CheckCharacterInfiltratorOrLazarus(selectedActor) || _showBoolCircles == 2))
                        {

                            IEnumerable<TacticalActor> tacticalActors = from a in selectedActor.TacticalFaction.Vision.GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false).OfType<TacticalActor>()
                                                                        where a.InPlay && a.Interactable
                                                                        select a;

                            foreach (TacticalActor tacticalActor in tacticalActors)
                            {
                                GroundMarker groundMarker = new GroundMarker(GroundMarkerType.EnemyVisionSphere, tacticalActor.VisionPoint, 0f)
                                {
                                    StartScale = 2f * selectedActor.GetPossibleVisionRangeTowardsMe(tacticalActor) * Vector3.one
                                };
                                controller.View.Markers.AddGroundMarker(GroundMarkerGroup.Selection, groundMarker, false);
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

            [HarmonyPatch(typeof(TacticalActor), "GetPossibleVisionRangeTowardsMe")]
            public static class TacticalActor_GetPossibleVisionRangeTowardsMe_patch
            {

                public static void Postfix(ref float __result, TacticalActor fromActor, TacticalActor __instance)
                {
                    try
                    {
                        if (__result < fromActor.CharacterStats.HearingRange.Value.EndValue || __result < __instance.TacticalLevel.TacticalLevelControllerDef.DetectionRange)
                        {
                            __result = Math.Max(fromActor.CharacterStats.HearingRange.Value.EndValue, __instance.TacticalLevel.TacticalLevelControllerDef.DetectionRange);
                        }

                        if (fromActor.GetAbility<SurveillanceAbility>() != null && fromActor.GetAbility<SurveillanceAbility>().SurveillanceAbilityDef.TargetingDataDef.Origin.Range > __result)
                        {
                            __result = fromActor.GetAbility<SurveillanceAbility>().SurveillanceAbilityDef.TargetingDataDef.Origin.Range;
                        }
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

            private static int GetAdjustedSpeedValueForParalyisDamage(TacticalActor actor, int value)
            {
                try
                {
                    ParalysisDamageOverTimeStatus status = actor.Status.GetStatus<ParalysisDamageOverTimeStatus>();

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
                    ShortActorInfoTooltipData shortActorInfoTooltipData = default;

                    shortActorInfoTooltipData.Entries = new List<ShortActorInfoTooltipDataEntry>();
                    shortActorInfoTooltipData.TrackRoot = actor.gameObject;

                    shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        TextContent = actor.DisplayName.ToUpper(),
                        ValueContent = string.Empty
                    });

                    shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        TextContent = uIModuleShortActorInfoTooltip.HealthTextKey.Localize(null),
                        ValueContent = string.Format("{0}/{1}", actor.CharacterStats.Health.IntValue, actor.CharacterStats.Health.IntMax)
                    });
                    shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        TextContent = uIModuleShortActorInfoTooltip.WillpointsTextKey.Localize(null),
                        ValueContent = string.Format("{0}/{1}", actor.CharacterStats.WillPoints.IntValue, actor.CharacterStats.WillPoints.IntMax)
                    });

                    int maxActionPoints = GetAdjustedSpeedValueForParalyisDamage(actor, actor.CharacterStats.ActionPoints.IntMax);

                    string value = string.Format("{0}/{1}", maxActionPoints, maxActionPoints);
                    if (actor.TacticalLevel.CurrentFaction == actor.TacticalFaction)
                    {
                        value = string.Format("{0}/{1}", Mathf.Min(actor.CharacterStats.ActionPoints.IntValue, maxActionPoints), maxActionPoints);
                    }

                    shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
                    {
                        TextContent = TFTVCommonMethods.ConvertKeyToString("KEY_MOVEMENT"),
                        ValueContent = value
                    });

                    string perceptionDescription = TFTVCommonMethods.ConvertKeyToString("KEY_PROGRESSION_PERCEPTION");
                    string perceptionValue = Mathf.CeilToInt(actor.GetAdjustedPerceptionValue()).ToString(); //Perception.IntValue.ToString();

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

                    shortActorInfoTooltipData.Entries.Add(perception);
                    shortActorInfoTooltipData.Entries.Add(stealth);
                    shortActorInfoTooltipData.Entries.Add(accuracy);

                    TacticalActor selectedActor = actor.TacticalLevel.View.SelectedActor;

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

                                shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
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

                            shortActorInfoTooltipData.Entries.Add(new ShortActorInfoTooltipDataEntry
                            {
                                Icon = _moonIcon,
                                IconColor = new Color(1, 1, 1, 1),
                                TextContent = TFTVCommonMethods.ConvertKeyToString("TFTV_MOONPROJECT_SHORT_INFO"),
                                ValueContent = $"{moonProject.Multiplier * 100 - 100}% {TFTVCommonMethods.ConvertKeyToString("TFTV_VIVISECTED_SHORT_INFO_DAMAGE")}" // Adjust based on the actual multiplier field
                            });
                        }

                    }

                    foreach (TacticalActorViewBase.StatusInfo statusInfo in actor.TacticalActorView.GetCharacterStatusActorStatuses())
                    {


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
                                shortActorInfoTooltipData.Entries.Add(item);
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

                                shortActorInfoTooltipData.Entries.Add(item);

                            }
                        }
                    }

                    return shortActorInfoTooltipData;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(UIModuleTacticalContextualMenu), "OnAbilityHover")]
            public static class UIModuleTacticalContextualMenu_OnAbilityHover_patch
            {

                public static void Postfix(bool isHovered, TacticalContextualMenuItem menuItem, UIModuleTacticalContextualMenu __instance)
                {
                    try
                    {
                        if (menuItem.InfoButton && __instance.SelectionInfo.Actor is TacticalActor tacticalActor && tacticalActor.IsControlledByPlayer)
                        {
                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            TacticalActor selectedActor = controller.View.SelectedActor;

                            if (selectedActor != null && selectedActor.TacticalFaction == controller.View.ViewerFaction)
                            {
                                ShowShortInfoTooltipSelectedActor(selectedActor, controller);
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

            //Codemite's solution to pink portrait backgrounds on Macs
            [HarmonyPatch(typeof(RenderingEnvironment), MethodType.Constructor, new Type[]
    {
    typeof(Vector2Int),
    typeof(RenderingEnvironmentOption),
    typeof(Color),
    typeof(Camera)
      })]
            public static class RenderingEnvironmentPatch
            {

                public static bool Prefix(ref RenderingEnvironment __instance, ref Transform ____origin, ref bool ____isExternalCamera, ref Camera ____camera,
                    Vector2Int resolution, RenderingEnvironmentOption option, Color? backgroundColor, Camera cam)
                {
                    try
                    {

                        ____origin = new GameObject("_RenderingEnvironmentOrigin_").transform;
                        ____origin.position = new Vector3(0f, 1500f, 0f);
                        ____origin.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                        ____isExternalCamera = cam != null;

                        ____camera = cam != null ? cam : new GameObject("_RenderingCamera_").AddComponent<Camera>();
                        ____camera.gameObject.SetActive(false);
                        ____camera.clearFlags = option.ContainsFlag(RenderingEnvironmentOption.NoBackground) && backgroundColor == null
                            ? CameraClearFlags.Depth
                            : (backgroundColor != null ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox);
                        ____camera.fieldOfView = 60;
                        ____camera.orthographicSize = 2;
                        ____camera.orthographic = option.ContainsFlag(RenderingEnvironmentOption.Orthographic);
                        ____camera.farClipPlane = 100;
                        ____camera.renderingPath = RenderingPath.Forward;
                        ____camera.enabled = false;
                        ____camera.allowHDR = false;
                        ____camera.allowDynamicResolution = false;
                        ____camera.usePhysicalProperties = false;

                        // Use reflection to set the value of RenderTexture
                        var renderTextureField = typeof(RenderingEnvironment).GetField("RenderTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (renderTextureField != null)
                        {
                            renderTextureField.SetValue(__instance, RenderTexture.GetTemporary(resolution.x, resolution.y, 1, RenderTextureFormat.ARGB32));
                        }

                        ____camera.targetTexture = (RenderTexture)renderTextureField.GetValue(__instance);

                        if (backgroundColor != null)
                        {
                            ____camera.backgroundColor = (Color)backgroundColor;
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


            [HarmonyPatch(typeof(SoldierPortraitUtil), "RenderSoldierNoCopy")]
            public static class SoldierPortraitUtil_RenderSoldierNoCopy_patch
            {

                public static bool Prefix(GameObject soldierToRender, RenderPortraitParams renderParams, Camera usedCamera, ref Texture2D __result)
                {
                    try
                    {

                        if (Application.platform == RuntimePlatform.OSXPlayer ||
                     Application.platform == RuntimePlatform.OSXEditor)
                        {

                            var outputImage = new Texture2D(renderParams.RenderedPortraitsResolution.x, renderParams.RenderedPortraitsResolution.y, TextureFormat.RGBA32, true);
                            using (var renderingEnvironment = new RenderingEnvironment(renderParams.RenderedPortraitsResolution, RenderingEnvironmentOption.NoBackground, Color.black, usedCamera))
                            {
                                Transform headTransform = soldierToRender.transform.FindTransformInChildren(renderParams.TargetBoneName) ?? soldierToRender.transform;
                                var t = soldierToRender.transform;
                                var initialPosition = t.position;
                                var initialRotation = t.rotation;
                                t.position = renderingEnvironment.OriginPosition;
                                t.rotation = renderingEnvironment.OriginRotation;

                                SoldierFrame soldierFrame = new SoldierFrame(headTransform,
                                    renderParams.CameraFoV, renderParams.CameraDistance, renderParams.CameraHeight, renderParams.CameraSide);
                                renderingEnvironment.Render(soldierFrame, false);
                                renderingEnvironment.WriteResultsToTexture(outputImage);

                                t.position = initialPosition;
                                t.rotation = initialRotation;
                            }

                            __result = outputImage;

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
            }
        }

        //Prevents items with 0HP from manifesting themselves in tactical
        //And causes AI to lockup.

        /*  [HarmonyPatch(typeof(TacticalItem), "get_IsHealthAboveMinThreshold")]
          public static class TFTV_TacticalItem_get_IsHealthAboveMinThreshold
          {
              public static void Postfix(TacticalItem __instance, ref bool __result)
              {
                  try
                  {
                      if (!((float)__instance.GetHealth() >= 1))
                      {
                          __result = (float)__instance.GetHealth().Max < 1E-05f;
                      }

                      __result = true;
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/




        //Prevents multiple instancing of mission briefings when several aircraft arrive simultaneously at the mission site
        private static TimeUnit _arrivalTime;

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteVisited")]
        public static class UIStateVehicleSelected_OnVehicleSiteVisitedt_patch
        {
            public static bool Prefix(UIStateVehicleSelected __instance, GeoVehicle vehicle)
            {
                try
                {
                    TimeUnit currentTime = vehicle.GeoLevel.Timing.Now;

                    if (_arrivalTime != null && _arrivalTime == currentTime && vehicle?.CurrentSite.Vehicles.Count() > 1)
                    {
                        TFTVLogger.Always($"more than 1 vehicle arriving at {vehicle?.CurrentSite?.LocalizedSiteName} simultaneously; cancelling stuff for all vehicles except the first");
                        return false;
                    }

                    _arrivalTime = currentTime;

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        //Prevent melee attacks at targets at high elevation
        public static void FixMeleeTooHighAttack(TacticalAbility __instance, ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor,
                       TacticalTargetData targetData, Vector3 sourcePosition)
        {
            try
            {

                if ((__instance is ShootAbility || __instance is BashAbility || __instance is RetrieveDeployedItemAbility) && targetData.Range < 4f)
                {
                    List<TacticalAbilityTarget> list = new List<TacticalAbilityTarget>();

                    foreach (TacticalAbilityTarget target in __result)
                    {
                        if (target.Actor is TacticalActor targetActor) //&& sourceActor is TacticalActor actingActor)
                        {
                            if (targetActor.Pos.y == sourcePosition.y || (targetActor.Pos.y > sourcePosition.y && targetActor.Pos.y - sourcePosition.y < targetData.Range)
                                || (targetActor.Pos.y < sourcePosition.y && sourcePosition.y - targetActor.Pos.y < targetData.Range))
                            {
                                // TFTVLogger.Always($"sourcePos: {sourcePosition} for {actingActor.name} at pos {actingActor.Pos}, targetActor.Pos.y {targetActor.Pos.y}, targetData.Range / 2 {targetData.Range}");
                                list.Add(target);

                            }
                        }
                        else
                        {
                            list.Add(target);
                        }
                    }
                    __result = list;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        //fixes requiring killing actor required for research even when it is already captured

        [HarmonyPatch(typeof(ActorResearchRequirement), "OnMissionEnd")]
        public static class TFTV_ActorResearchRequirement_OnMissionEnd
        {
            public static bool Prefix(ActorResearchRequirement __instance, GeoFaction faction, GeoMission mission, GeoSite site, GeoFaction ____faction)
            {
                try
                {
                    _ = site.GeoLevel;
                    ActorResearchRequirementDef actorResearchRequirementDef = __instance.ActorResearchRequirementDef;
                    foreach (FactionResult factionResult in mission.Result.FactionResults)
                    {
                        if (factionResult.FactionDef == ____faction.Def.PPFactionDef || (__instance.ActorResearchRequirementDef.Faction != null && factionResult.FactionDef != actorResearchRequirementDef.Faction))
                        {
                            continue;
                        }

                        foreach (TacActorUnitResult item in from t in factionResult.UnitResults.Select((UnitResult s) => s.Data).OfType<TacActorUnitResult>()
                                                            where !t.IsAlive || t.Statuses.Any(s => s.Def.EffectName == "Paralysed")
                                                            select t)
                        {

                            //  if(item.Statuses.Any(s => s.Def.EffectName == "Paralysed") && )

                            // TFTVLogger.Always($"considering {item.SourceTemplate.name}");

                            if (__instance.IsValidUnit(item))
                            {
                                MethodInfo updateProgressMethod = typeof(ResearchRequirement).GetMethod("UpdateProgress", BindingFlags.Instance | BindingFlags.NonPublic);

                                updateProgressMethod.Invoke(__instance, new object[] { 1 });

                                if (__instance.IsCompleted)
                                {
                                    break;
                                }
                            }
                        }
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


        //fixes events reducing health to 0 and killing soldiers
        [HarmonyPatch(typeof(GeoFactionReward), "AddInjuriesToAllSoldiers")]
        public static class TFTV_GeoFactionReward_AddInjuriesToAllSoldiers
        {
            public static bool Prefix(GeoFactionReward __instance, GeoFaction faction)
            {
                try
                {
                    if (__instance.AddAllSoldiersTiredness != 0)
                    {
                        foreach (GeoCharacter character in faction.Characters)
                        {
                            if (character.Fatigue != null)
                            {
                                character.Fatigue.Stamina.AddRestrictedToMax(-__instance.AddAllSoldiersTiredness);
                            }
                        }

                        __instance.ApplyResult.AllSoldiersTiredness += __instance.AddAllSoldiersTiredness;
                    }

                    if (__instance.AddAllSoldiersDamage == 0)
                    {
                        return false;
                    }

                    foreach (GeoCharacter character2 in faction.Characters)
                    {
                        if ((float)character2.Health > 1f && character2.TemplateDef.IsHuman)
                        {
                            int addAllSoldiersDamage = Math.Min(__instance.AddAllSoldiersDamage, (int)character2.Health - 1);
                            character2.Health.AddRestrictedToMax(-addAllSoldiersDamage);
                            TFTVLogger.Always($"applied {addAllSoldiersDamage} damage to {character2.DisplayName}");
                        }
                    }

                    __instance.ApplyResult.AllSoldiersDamage += __instance.AddAllSoldiersDamage;

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }






        //Madskunky's replacement of a trig function to reduce AI processing time 
        [HarmonyPatch(typeof(Weapon), "GetDamageModifierForDistance")]
        public static class Weapon_GetDamageModifierForDistance_patch
        {

            public static bool Prefix(Weapon __instance, TacticalActorBase targetActor, ref float __result, float distance)
            {
                try
                {
                    float num = (float)Math.PI / 180f * __instance.WeaponDef.SpreadDegrees;
                    if (num < Mathf.Epsilon)
                    {
                        __result = 1f;
                        return false;
                    }

                    TacticalPerceptionBase tacticalPerceptionBase = targetActor.TacticalPerceptionBase;
                    float num2 = tacticalPerceptionBase.Height / 2f;
                    float r = tacticalPerceptionBase.GetCapsuleLocal().r;
                    float time = Mathf.Clamp(((num2 + r) / 2f / distance) / num, 0f, 1f);
                    __result = time < 0.5 ? 0f : time;
                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Prevents ammo from disappearing on pressing replinish ammo if the class of the soldier is not proficient with the weapon and ALL filter is switched off 
        [HarmonyPatch(typeof(UIStateEditSoldier), "SoldierSlotItemChangedHandler")]
        public static class UIStateEditSoldier_SoldierSlotItemChangedHandler_patch
        {

            public static bool Prefix(UIStateEditSoldier __instance, UIInventorySlot slot)
            {
                try
                {
                    if (slot == null)
                    {
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
        }




        //MadSkunky's OW, fumble and Throwing range fix copied over from BC

        /// <summary> 
        /// Fix:
        /// The UI icon element of overwatch abilities does not take AP modifications for the default shooting ability into account.
        /// </summary>
        [HarmonyPatch(typeof(TacticalAbility), "FractActionPointCost", MethodType.Getter)]
        internal class TacticalAbility_get_FractActionPointCost_OWFix
        {
            public static bool Prefix(TacticalAbility __instance, ref float __result)
            {
                try
                {
                    if (!(__instance is OverwatchAbility overwatchAbility))
                    {
                        // do nothing and call the patched method if this is not an overwatch ability
                        return true;
                    }

                    // copy-pasted from the original get_FractActionPointCost()
                    float baseFract;
                    if (overwatchAbility.TacticalAbilityDef.ActionPointCost >= 0f)
                    {
                        baseFract = overwatchAbility.TacticalAbilityDef.ActionPointCost;
                    }
                    else
                    {
                        Equipment equipment = overwatchAbility.Equipment;
                        baseFract = ((equipment != null) ? equipment.FractApToUse : 0f);
                    }
                    // copy-pasted from the original and private method GetActionPointFractCost(baseFract);
                    if (!baseFract.IsZero(1E-05f) && overwatchAbility.IsTacticalActor)
                    {
                        baseFract = overwatchAbility.TacticalActor.CalcFractActionPointCost(baseFract, overwatchAbility);
                    }

                    // adding comparison with default shooting ability and take the minimum of both
                    __result = Mathf.Min(baseFract, overwatchAbility.GetDefaultShootAbility().FractActionPointCost);

                    // skip the patched method
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Fix:
        /// The method TacticalAbility.EnqueueAction() is an alternative to TacticalAbility.PlayAction().
        /// The assumption is that this method will play an action synchronously with a currently running action, as opposed to playing it directly as with PlayAction().
        /// This method appears to be called only by ShootAbility.Activate().
        /// In contrast to PlayAction(), however, a possible fumble (property FumbledAction = true) is not checked with this method and therefore no fumble will happen.
        /// This patch fixes the problem and queues the FumbleAction() to play when the FumbledAction property is set (= true).
        /// </summary>
        [HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.EnqueueAction))]
        public static class TacticalAbility_EnqueueAction_FumbleFix
        {
            public static bool Prefix(TacticalAbility __instance,
                                      ref PlayingAction ____nextAction,
                                      Func<PlayingAction, IEnumerator<NextUpdate>> action,
                                      object parameter,
                                      bool soloAfterCurrent = false)
            {
                try
                {
                    // Change action to play FumbleAction dependent on the property FumbledAction
                    if (__instance.FumbledAction)
                    {
                        action = GetDelegate<Func<PlayingAction, IEnumerator<NextUpdate>>>(__instance, "FumbleAction");
                        TFTVLogger.Always($"TacticalAbility_EnqueueAction_FumbleFixPatch: FumbledAction for {__instance} is true, changed action to play to TacticalAbility.FumbleAction().");
                    }

                    // Get delegate for CreateWaitingForCameraBlendingAction method
                    Func<Func<PlayingAction, IEnumerator<NextUpdate>>, Func<PlayingAction, IEnumerator<NextUpdate>>> instance_CreateWaitingForCameraBlendingAction =
                        GetDelegate<Func<Func<PlayingAction, IEnumerator<NextUpdate>>, Func<PlayingAction, IEnumerator<NextUpdate>>>>(__instance, "CreateWaitingForCameraBlendingAction");
                    // Go on with original code
                    ____nextAction = __instance.ActionComponent.PlayActionAfterCurrent(ActionChannel.ActorActions,
                                                                                       soloAfterCurrent,
                                                                                       parameter,
                                                                                       instance_CreateWaitingForCameraBlendingAction(action),
                                                                                       GetDelegate<Action<PlayingAction>>(__instance, "StartPlayingAction"),
                                                                                       GetDelegate<Action<PlayingAction>>(__instance, "ClearPlayingAction"));

                    __instance.TacticalActorBase.OnAbilityEnqueued(__instance);
                    return false; // Don't execute (skip) original method, this is a fix for the original one in the end ;-)
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;  // Execute original method
                }
            }

            private static DelegateType GetDelegate<DelegateType>(object instance, string name) where DelegateType : Delegate
            {
                return AccessTools.MethodDelegate<DelegateType>(AccessTools.Method(instance.GetType(), name), instance); ;
            }
        }

        /// <summary>
        /// Harmony patch that fixes the vanilla throw range calculation.
        /// The attenuation tag allows Harmony to find the targeted class/object method and apply the patch from the following class.
        /// </summary>
        [HarmonyPatch(typeof(Weapon), "GetThrowingRange")]
        internal class Weapon_GetThrowingRange_Fix
        {
            /// Using Postfix patch to be guaranteed to get executed.
            public static void Postfix(ref float __result, Weapon __instance, float rangeMultiplier)
            {
                try
                {
                    float num = __instance.TacticalActor.CharacterStats.Endurance * __instance.TacticalActor.TacticalActorDef.EnduranceToThrowMultiplier;
                    float num2 = __instance.TacticalActor.CharacterStats.BonusAttackRange.CalcModValueBasedOn(num);
                    // MadSkunky: Extension of calculation with range multiplier divided by 12 for normalization and multiplier from configuration.
                    num *= __instance.GetDamagePayload().Range / 12f;
                    float multiplier = 1f; // (Main.Config as GrenadeThrowRangeFixConfig).ThrowRangeMultiplier / 100f;
                    __result = ((num / __instance.Weight * rangeMultiplier) + num2) * multiplier;
                    // End of changes
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        /// <summary>
        /// Fix to prevent characters given in events from spawning with wrong faction origin
        /// </summary>

        private static List<string> _eventsRewardingNJCharacters = new List<string>() { "AN11", "EX7", "SY22" };

        [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]
        public static class GeoEventChoiceOutcome_GenerateFactionReward_patch
        {

            public static void Postfix(GeoEventChoiceOutcome __instance, string eventID, ref GeoFactionReward __result)
            {
                try
                {
                    GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (eventID == "PROG_PU4_WIN" && __result.Units.Count > 0)
                    {
                        __result.Units.Clear();
                        GeoFaction faction2 = level.AnuFaction;
                        GeoUnitDescriptor geoUnitDescriptor = level.CharacterGenerator.GenerateUnit(faction2, __instance.Units[0]);
                        level.CharacterGenerator.ApplyRecruitDifficultyParameters(geoUnitDescriptor);
                        GeoCharacter item2 = geoUnitDescriptor.SpawnAsCharacter();
                        __result.Units.Add(item2);

                    }
                    else if (_eventsRewardingNJCharacters.Contains(eventID) && __result.Units.Count > 0)
                    {
                        __result.Units.Clear();
                        GeoFaction faction2 = level.NewJerichoFaction;
                        GeoUnitDescriptor geoUnitDescriptor = level.CharacterGenerator.GenerateUnit(faction2, __instance.Units[0]);
                        level.CharacterGenerator.ApplyRecruitDifficultyParameters(geoUnitDescriptor);
                        GeoCharacter item2 = geoUnitDescriptor.SpawnAsCharacter();
                        __result.Units.Add(item2);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        /// <summary>
        /// Fix to prevent last item being removed in Marketplace when number of offers > 7 
        /// No try/catch because harmless error on buying item
        /// </summary>

        [HarmonyPatch(typeof(UIModuleTheMarketplace), "UpdateList")]
        public static class UIModuleTheMarketplace_UpdateList_patch
        {
            public static bool Prefix(UIModuleTheMarketplace __instance, GeoscapeEvent geoEvent, bool ____isInit,
                List<TheMarketplaceChoiceButton> ____marketplaceChoiceButtons, GeoMarketplace ____geoMarketplace)
            {
                //  try
                //  {
                MethodInfo setChoiceMethod = typeof(TheMarketplaceChoicesController).GetMethod("SetChoice", BindingFlags.NonPublic | BindingFlags.Instance);

                if (____isInit)
                {
                    __instance.ListScrollRect.Scroll.verticalNormalizedPosition = 1f;
                }

                ____marketplaceChoiceButtons.Clear();

                //    TFTVLogger.Always($"____geoMarketplace.MarketplaceChoices.Count {____geoMarketplace.MarketplaceChoices.Count}");

                int count = ____geoMarketplace.MarketplaceChoices.Count;

                if (____geoMarketplace.MarketplaceChoices.Count > 7) //&& !TFTVChangesToDLC5.TFTVMarketPlaceUI.MarketplaceOfferListAdjustedOnce)
                {
                    count = ____geoMarketplace.MarketplaceChoices.Count + 1;
                }



                __instance.ListScrollRect.InitVertical(__instance.MarketplaceChoiceButtonPrefab.GetComponent<TheMarketplaceChoiceButton>(), count, delegate (int index, UnityEngine.Component element)
                {
                    TheMarketplaceChoiceButton component = element.GetComponent<TheMarketplaceChoiceButton>();
                    setChoiceMethod.Invoke(__instance.TheMarketplaceChoicesController, new object[] { __instance.Context.ViewerFaction, ____geoMarketplace.MarketplaceChoices[index], component, geoEvent.Context });
                    ____marketplaceChoiceButtons.Add(component);
                });

                // TFTVLogger.Always($"____marketplaceChoiceButtons.Count {____marketplaceChoiceButtons.Count}");

                return false;
                //  }
                /*  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }*/
            }
        }


        /// <summary>
        /// Not strictly a bug, but once partial magazines become visible, without this patch they can be scrapped for the same price as a full one.
        /// This patch ensures that scrapping ammo is never profitable.
        /// </summary>

        [HarmonyPatch(typeof(ItemDef), "get_ScrapPrice")]
        public static class ItemDef_get_ScrapPrice_patch
        {
            public static void Postfix(ItemDef __instance, ref ResourcePack __result, ResourcePack ____scrapPrice)
            {
                try
                {

                    if (__instance.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                    {
                        TacticalItemDef tacticalItemDef = __instance as TacticalItemDef;

                        WeaponDef weaponDef = (WeaponDef)AmmoWeaponDatabase.AmmoToWeaponDictionary[tacticalItemDef][0];

                        float costMultiplier = Math.Max(__instance.ChargesMax / Math.Max(weaponDef.DamagePayload.AutoFireShotCount, weaponDef.DamagePayload.ProjectilesPerShot), 2);




                        __result = new ResourcePack(new ResourceUnit[]
                             {
                        new ResourceUnit(ResourceType.Tech, Mathf.Max(Mathf.FloorToInt(__instance.ManufactureTech / costMultiplier), Mathf.FloorToInt(__instance.ManufactureTech/10))),
                        new ResourceUnit(ResourceType.Materials, Mathf.Max(Mathf.CeilToInt(__instance.ManufactureMaterials / costMultiplier), Mathf.CeilToInt(__instance.ManufactureMaterials/10))),
                        new ResourceUnit(ResourceType.Mutagen, Mathf.Floor(__instance.ManufactureMutagen / costMultiplier)),
                        new ResourceUnit(ResourceType.LivingCrystals, Mathf.Floor(__instance.ManufactureLivingCrystals / costMultiplier)),
                        new ResourceUnit(ResourceType.Orichalcum, Mathf.Floor(__instance.ManufactureOricalcum / costMultiplier)),
                        new ResourceUnit(ResourceType.ProteanMutane, Mathf.Floor(__instance.ManufactureProteanMutane / costMultiplier))
                             });


                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        //Code provided by Codemite
        [HarmonyPatch(typeof(UIInventorySlot), "UpdateItem")]
        public static class UIInventorySlot_UpdateItem_patch
        {
            public static void Postfix(UIInventorySlot __instance, ICommonItem ____item)
            {
                try
                {
                    if (____item == null || ____item.CommonItemData.Count == 1 && (____item.CommonItemData.CurrentCharges == ____item.ItemDef.ChargesMax || ____item.CommonItemData.CurrentCharges == 0))
                    {
                        __instance.NumericBackground.gameObject.SetActive(false);
                    }
                    else
                    {
                        __instance.NumericBackground.gameObject.SetActive(true);

                        if (____item.CommonItemData.CurrentCharges == ____item.ItemDef.ChargesMax)
                        {
                            __instance.NumericField.text = ____item.CommonItemData.Count.ToString();
                        }
                        else
                        {
                            string ammoCount = $"{____item.CommonItemData.CurrentCharges}/{____item.ItemDef.ChargesMax}";
                            string textToShow;
                            string greyColor = "<color=#b6b6b6>";

                            if (____item.CommonItemData.Count - 1 == 0)
                            {
                                if (____item.ItemDef.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                                {
                                    textToShow = $"{greyColor}(1) {ammoCount}</color>";
                                }
                                else
                                {
                                    textToShow = $"{greyColor} {ammoCount}</color>";
                                }
                            }
                            else if (____item.ItemDef is WeaponDef weaponDef)
                            {
                                textToShow = ____item.CommonItemData.Count.ToString();
                            }
                            else
                            {

                                textToShow = $"{____item.CommonItemData.Count - 1} {greyColor}+ {____item.CommonItemData.CurrentCharges}/{____item.ItemDef.ChargesMax}</color>";
                            }

                            __instance.NumericField.text = textToShow;
                            __instance.NumericField.alignment = TextAnchor.MiddleLeft;
                        }
                    }

                    // return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        //Factions attacking Phoenix bases fix
        //Method by Dimitar "Codemite" Evtimov from Snapshot Games
        public static void PatchInAllBaseDefenseDefs()
        {
            try
            {

                CustomMissionTypeDef alienDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                CustomMissionTypeDef anuDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAnu_CustomMissionTypeDef");
                CustomMissionTypeDef njDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseNJ_CustomMissionTypeDef");
                CustomMissionTypeDef syDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseSY_CustomMissionTypeDef");
                CustomMissionTypeDef infestationDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseInfestationAlien_CustomMissionTypeDef");

                TacMissionTypeDef[] defenseMissions = { alienDef, anuDef, njDef, syDef, infestationDef };

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded)
                        continue;

                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetTransformsInChildrenStable())
                        {
                            var objActivator = transform.GetComponent<TacMissionObjectActivator>();
                            if (objActivator && objActivator.Missions.Length == 1 && objActivator.Missions.Contains(alienDef))
                            {
                                objActivator.Missions = defenseMissions;
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(TacMission), "PrepareMissionActivators")]

        public static class TacMission_PrepareMissionActivators_Experiment_patch
        {
            public static void Prefix(TacMission __instance)
            {
                try
                {

                    TFTVLogger.Always("PrepareMissionActivators");
                    PatchInAllBaseDefenseDefs();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        /// <summary>
        /// This replaces the original Vanilla method, which contained several bugs. 
        /// The bugs resulted from not considering how damage multipliers / armor stack multipliers (in TFTV, the special revenant resistance, in Vanilla, the Orichalcum shielding)
        /// reduced incoming damage to limbs.
        /// </summary>
        [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
        public static class TFTV_DamageAccumulation_GenerateStandardDamageTargetData
        {

            public static bool Prefix(DamageAccumulation __instance, ref DamageAccumulation.TargetData __result,
                IDamageReceiver target, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit, out float damageAmountLeft)
            {
                try
                {

                    MethodInfo GetPureDamageBonusForMethod = typeof(DamageAccumulation).GetMethod("GetPureDamageBonusFor", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetSourceDamageMultiplierMethod = typeof(DamageAccumulation).GetMethod("GetSourceDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetEffectiveArmorMethod = typeof(DamageAccumulation).GetMethod("GetEffectiveArmor", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetArmorStackMultiplierMethod = typeof(DamageAccumulation).GetMethod("GetArmorStackMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);

                    float armorStackMultiplier = (float)GetArmorStackMultiplierMethod.Invoke(__instance, new object[] { target as IDamageBlocker, __instance.Source });

                    float amount = __instance.Amount;
                    damageAmountLeft = __instance.Amount;
                    float num = amount + (float)GetPureDamageBonusForMethod.Invoke(__instance, new object[] { target });
                    float num2 = __instance.Amount * (float)GetSourceDamageMultiplierMethod.Invoke(__instance, new object[] { __instance.DamageTypeDef, target }) - __instance.Amount;//12
                    float damageMultiplierFor = target.GetDamageMultiplierFor(__instance.DamageTypeDef, __instance.Source);
                    float damageMultiplier = __instance.GetDamageMultiplier(target.GetApplicationType());
                    float effectiveArmor = (float)GetEffectiveArmorMethod.Invoke(__instance, new object[] { target });

                    float totalDamageMultiplier = damageMultiplier * damageMultiplierFor * armorStackMultiplier;

                    float num3 = effectiveArmor / totalDamageMultiplier;

                    float num4 = Mathf.Max(0f, num - num3) + num2; //max amount of damage that can be dealt by the attack. missing damageMultiplierFor?
                    float num5 = target.GetHealth() / totalDamageMultiplier; //max amount of damage target can take
                    float num6 = Mathf.Min(num4, num5); //choosing lower of the two
                    if (!__instance.IsFireDamageType)
                    {
                        float num7 = Mathf.Min(b: ((float)target.GetHealth().Max + effectiveArmor) / totalDamageMultiplier, a: damageAmountLeft);//50
                        damageAmountLeft -= num7;

                    }

                    if (num5 < 1E-05f && target.IsAccessoryBodyPart())
                    {
                        num4 = 0f;
                    }

                    float num8 = num6 * totalDamageMultiplier;

                    __result = new DamageAccumulation.TargetData
                    {
                        Target = target,
                        AmountApplied = num4,
                        DamageResult = new DamageResult
                        {
                            Source = __instance.Source,
                            ArmorDamage = __instance.ArmorShred,
                            ArmorMitigatedDamage = Mathf.Min(amount, num3),
                            HealthDamage = num8,
                            ImpactForce = impactForce,
                            ImpactHit = impactHit,
                            DamageOrigin = damageOrigin,
                            DamageTypeDef = __instance.DamageTypeDef,
                            RelatedDamageTypeDefs = ((__instance.DamageKeywords != null) ? __instance.DamageKeywords.Select((DamageKeywordPair x) => x.DamageKeywordDef.DamageTypeDef).ToList() : null)
                        }
                    };


                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        /*    [HarmonyPatch(typeof(DamageAccumulation), "AddTarget")]
            public static class TFTV_DamageAccumulation_AddTarget
            {

                public static bool Prefix(DamageAccumulation __instance, ref List<DamageAccumulation.TargetData> ____targetsData,
         IDamageReceiver target, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit, DamagePredictor predictor = null)
                {
                    try
                    {
                        MethodInfo methodInfoProcessDamageKeyword = typeof(DamageAccumulation).GetMethod("ProcessDamageKeyword", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo methodInfoIsJunkData = typeof(DamageAccumulation).GetMethod("IsJunkData", BindingFlags.Static | BindingFlags.NonPublic);

                        DamageAccumulation.TargetData data = null;

                        if (predictor == null)
                        {
                            predictor = TacUtil.GetSourceOfType<ProjectileLogic>(__instance.Source)?.Predictor;
                        }

                        if (__instance.Attenuating)
                        {
                            IDamageDealer sourceOfType = TacUtil.GetSourceOfType<IDamageDealer>(__instance.Source);
                            if (sourceOfType != null)
                            {
                                float magnitude = (damageOrigin - impactHit.Point).magnitude;
                                __instance.Amount *= Utl.AttenuationMultiplier(magnitude, sourceOfType.GetMaxRange());
                                __instance.Amount = (Utl.GreaterThanOrEqualTo(__instance.Amount, 1f) ? Mathf.Floor(__instance.Amount) : Mathf.Ceil(__instance.Amount));
                            }
                        }

                        // Process standard damage first
                         if (__instance.DamageKeywords == null || !__instance.DamageKeywords.Any())
                          {
                              TFTVLogger.Always($"got here2");
                              data = __instance.GenerateStandardDamageTargetData(target, damageOrigin, impactForce, impactHit, out __instance.Amount);
                              ____targetsData.Add(data);


                          }
                          else
                          {


                        // Separate standard damage keyword from other keywords
                        var standardDamageKeyword = __instance.DamageKeywords.FirstOrDefault(k => k.DamageKeywordDef.AppliesStandardDamage);
                        var otherKeywords = __instance.DamageKeywords.Where(k => !k.DamageKeywordDef.AppliesStandardDamage).ToList();

                        // Process standard damage keyword first
                        if (standardDamageKeyword != null)
                        {
                            TFTVLogger.Always($"standardDamageKeyword: {standardDamageKeyword.DamageKeywordDef.name}");
                            object[] parameters = { standardDamageKeyword, target, damageOrigin, impactForce, impactHit, predictor, data };
                            methodInfoProcessDamageKeyword.Invoke(__instance, parameters);
                            data = (DamageAccumulation.TargetData)parameters[6];
                            TFTVLogger.Always($"data.AmountApplied: {data.AmountApplied}, HealthDamage: {data.DamageResult.HealthDamage}, data.DamageResult.DamageTypeDef.name: {data.DamageResult.DamageTypeDef.name}");
                            data?.ApplyToTarget();
                        }

                        // Process other keywords (including shock damage) after standard damage
                        foreach (var damageKeyword in otherKeywords)
                        {
                            TFTVLogger.Always($"otherDamageKeyword: {damageKeyword.DamageKeywordDef.name}");
                            object[] parameters = { damageKeyword, target, damageOrigin, impactForce, impactHit, predictor, data };
                            methodInfoProcessDamageKeyword.Invoke(__instance, parameters);
                            data = (DamageAccumulation.TargetData)parameters[6];
                            TFTVLogger.Always($"data.AmountApplied: {data.AmountApplied}, HealthDamage: {data.DamageResult.HealthDamage}," +
                                $" data.DamageResult.DamageTypeDef.name: {data.DamageResult.DamageTypeDef.name}");
                            if(data.DamageResult.ApplyStatuses!=null && data.DamageResult.ApplyStatuses.Count > 0)
                            {
                                TFTVLogger.Always($"data.DamageResult.ApplyStatuses.Count: {data.DamageResult.ApplyStatuses.Count}");
                                foreach(var status in data.DamageResult.ApplyStatuses)
                                {
                                    TFTVLogger.Always($"status: {status.StatusDef.name}");
                                }
                            }

                        }

                        bool junkData = (bool)methodInfoIsJunkData.Invoke(__instance, new object[] { data });
                        TFTVLogger.Always($"data null? {data == null} junkData? {junkData}");


                        if (data != null && !junkData)
                        {
                            ____targetsData.Add(data);
                        }

                        // Apply the accumulated damage to the target


                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }*/


        //REPLACED ABOVE
        //Strates fix for bloodlust
        /*   [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
           class DamageAccumulation_GenerateStandardDamageTargetData_VanillaBugFix
           {
               static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
               {
                   List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                   IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
               {
                   new CodeInstruction(OpCodes.Ldloc_3),
                   new CodeInstruction(OpCodes.Div)
               };

                   // insert after each of the first 3 divide opcodes
                   int divs = 0;
                   for (int index = 0; index < instructions.Count(); index++)
                   {
                       if (listInstructions[index].opcode == OpCodes.Div)
                       {
                           listInstructions.InsertRange(index + 1, insert);
                           index += 2;
                           divs++;
                           if (divs == 3)
                           {
                               break;
                           }
                       }
                   }

                   if (divs != 3)
                   {
                       return instructions; // didn't find three, function signature changed, abort
                   }
                   return listInstructions;
               }

           }*/


        /// <summary>
        /// Need to ensure that if ammo is less than full, it gets reloaded even if the difference is rounded down to 0
        /// </summary>
        [HarmonyPatch(typeof(GeoItem), "ReloadForFree")]
        public static class TFTV_CharacterFatigue_ReloadForFree_patch
        {

            public static bool Prefix(GeoItem __instance)
            {
                try
                {
                    ItemDef _def = __instance.ItemDef;


                    if (_def.ChargesMax <= 0)
                    {
                        return false;
                    }

                    TacticalItemDef tacticalItemDef = _def as TacticalItemDef;

                    if (__instance.CommonItemData.Ammo == null || tacticalItemDef == null || !tacticalItemDef.CompatibleAmmunition.Any())
                    {
                        __instance.CommonItemData.SetChargesToMax();
                        return false;
                    }

                    TacticalItemDef tacticalItemDef2 = tacticalItemDef.CompatibleAmmunition[0];
                    int num = (_def.ChargesMax - __instance.CommonItemData.Ammo.CurrentCharges) / tacticalItemDef2.ChargesMax;

                    //Added to make sure that if ammo is less than full, it gets reloaded even if rounded to 0
                    if (_def.ChargesMax - __instance.CommonItemData.Ammo.CurrentCharges > 0 && num == 0)
                    {
                        num = 1;
                    }

                    for (int i = 0; i < num; i++)
                    {

                        __instance.CommonItemData.Ammo.LoadMagazine(new GeoItem(tacticalItemDef2));

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


        //Reduce population by 1 when recruiting at havens

        [HarmonyPatch(typeof(GeoHaven), "TakeRecruit")]

        public static class TFTV_GeoHaven_TakeRecruit_VanillaBugBix_patch
        {
            public static void Postfix(GeoHaven __instance, IGeoCharacterContainer __result, ref int ____population)
            {
                try
                {
                    if (__result != null)
                    {
                        ____population -= 1;
                        HavenInfoController havenInfo = (HavenInfoController)UnityEngine.Object.FindObjectOfType(typeof(HavenInfoController));


                        int populationChange = __instance.GetPopulationChange(__instance.ZonesStats.GetTotalHavenOutput());
                        if (populationChange > 0)
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationPositiveTextPattern, __instance.Population.ToString(), populationChange);
                        }
                        else if (populationChange == 0)
                        {
                            havenInfo.PopulationValueText.text = __instance.Population.ToString();
                        }
                        else
                        {
                            havenInfo.PopulationValueText.text = string.Format(havenInfo.PopulationNegativeTextPattern, __instance.Population.ToString(), populationChange);
                        }


                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Remove negative damage notices with very large numbers when character with elemental immunity hit by elemental damage
        [HarmonyPatch(typeof(HealthbarUIActorElement), "AddNotificationMessage")]
        public class HealthbarUIActorElement_AddNotificationMessage_VanillaBugFix_Patch
        {
            static bool Prefix(int? val = null)
            {
                try
                {
                    // Check if val is outside the specified range
                    if (val.HasValue && (val.Value > 1000000 || val.Value < -1000000))
                    {
                        //TFTVLogger.Always("it worked");
                        // Return false to cancel the original method call
                        return false;
                    }

                    // Return true to allow the original method call
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }


        //Ensure facilities are working after repairing Power Generator
        [HarmonyPatch(typeof(GeoPhoenixFacility), "SetFacilityFunctioning")]
        public static class GeoPhoenixFacility_SetFacilityFunctioning_AfterGenRepairedVanillaBugFix_Patch
        {
            public static void Postfix(GeoPhoenixFacility __instance)
            {
                try
                {

                    //  TFTVLogger.Always($"SetFacilityFunctioning {__instance.ViewElementDef.name}");

                    if (__instance.GetComponent<PowerFacilityComponent>() != null)
                    {
                        CheckFacilitesNotWorking(__instance.PxBase);
                        //  __instance.PxBase.RoutePower();
                    }

                    //

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Ensure facilities are working after repairing Power Generator
        [HarmonyPatch(typeof(GeoPhoenixBase), "RoutePower")]
        public static class GeoPhoenixFacility_RoutePower_ForceStatsUpdate_Patch
        {
            public static void Postfix(GeoPhoenixBase __instance)
            {
                try
                {
                    __instance.UpdateStats();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void CheckFacilitesNotWorking(GeoPhoenixBase phoenixBase)
        {
            try
            {
                phoenixBase.RoutePower();
                TFTVUIGeoMap.UnpoweredFacilitiesInfo.CheckUnpoweredBasesOnGeoscapeStart();

                /*   foreach (GeoPhoenixFacility baseFacility in phoenixBase.Layout.Facilities)
                   {

                       if (baseFacility.IsPowered && baseFacility.GetComponent<PrisonFacilityComponent>() == null)
                       {
                           baseFacility.SetPowered(false);
                           baseFacility.SetPowered(true);
                       }
                       // TFTVLogger.Always($"{baseFacility.ViewElementDef.name} at {phoenixBase.name} is working? {baseFacility.IsWorking}. is it powered? {baseFacility.IsPowered} ");
                   }*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// Fixes crash w weird interception screen 
        /// </summary>
        [HarmonyPatch(typeof(InterceptionBriefDataBind), "ModalShowHandler")]
        public static class InterceptionBriefDataBind_ModalShowHandler_patch
        {
            public static bool Prefix(InterceptionBriefDataBind __instance, UIModal modal)
            {
                try
                {

                    InterceptionInfoData data = (InterceptionInfoData)modal.Data;

                    if (data.CurrentPlayerAircraft == null && data.GetDefaultPlayerAircraft() == null || data.CurrentEnemyAircraft == null && data.GetDefaultEnemyAircraft() == null)
                    {
                        modal.Close();
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
        }

        /// <summary>
        /// Fixes Umbra appearing when host had fire status
        /// </summary>
        /// <param name="tacticalActorBase"></param>
        /// <returns></returns>
        private static bool CheckUmbraEffectAndFire(TacticalActorBase tacticalActorBase)
        {
            try
            {
                DeathBelcherAbilityDef umbraCrabDeathBelcher = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef");
                DeathBelcherAbilityDef umbraFishDeathBelcher = DefCache.GetDef<DeathBelcherAbilityDef>("Oilfish_Die_DeathBelcher_AbilityDef");


                if (tacticalActorBase is TacticalActor tacticalActor &&
                    tacticalActor.Status != null && (
                    tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(umbraCrabDeathBelcher) != null
                    || tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(umbraFishDeathBelcher) != null) &&
                    tacticalActor.Status.HasStatus<FireStatus>())
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




        [HarmonyPatch(typeof(TacticalActorBase), "Die")]
        public static class TacticalActorBase_Die_patch
        {
            public static void Prefix(TacticalActorBase __instance)
            {
                try
                {
                    if (CheckUmbraEffectAndFire(__instance))
                    {
                        PropertyInfo propertyInfo = typeof(TacticalActorBase).GetProperty("LastDamageType", BindingFlags.Public | BindingFlags.Instance);

                        propertyInfo.SetValue(__instance, DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef"));
                        // TFTVLogger.Always($"Last damage source set to fire for {__instance.name}, check {__instance.LastDamageType.name}");                       
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithAbility")]
        public static class AIAttackPositionConsideration_EvaluateWithAbilityPatch
        {
            public static bool Prefix(AIAttackPositionConsideration __instance, IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {

                    MethodInfo getDamagePayloadMethodInfo = typeof(AIAttackPositionConsideration).GetMethod("GetDamagePayload", BindingFlags.NonPublic | BindingFlags.Instance);

                    TacticalActor tacActor = (TacticalActor)actor;
                    TacAITarget tacAITarget = (TacAITarget)target;
                    Weapon weapon = (Weapon)tacAITarget.Equipment;
                    ShootAbility shootAbility = weapon?.DefaultShootAbility;
                    if (weapon == null || shootAbility == null)
                    {
                        Debug.LogError($"{__instance.Def.name} has invalid target weapon {weapon} for {tacActor}", tacActor);
                        __result = 0f;
                        return false;
                    }

                    IgnoredAbilityDisabledStatesFilter ignoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints = IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints;
                    if (!shootAbility.IsEnabled(ignoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints))
                    {
                        __result = 0f;
                        return false;
                    }

                    if (__instance.Def.IsOverwatch)
                    {
                        OverwatchAbility ability = tacActor.GetAbility<OverwatchAbility>(weapon);
                        if (ability == null || !ability.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreEquipmentNotSelected))
                        {
                            __result = 0f;
                            return false;
                        }

                        tacAITarget.AngleInRadians = __instance.Def.OverwatchFOV / 2f * ((float)Math.PI / 180f);
                    }

                    TacticalAbilityTarget tacticalAbilityTarget = new TacticalAbilityTarget();
                    List<TacticalActorBase> list = new List<TacticalActorBase>(5);
                    float num = 1f;
                    if (tacAITarget.Actor == null)
                    {
                        __result = 0f;
                        return false;
                    }

                    tacticalAbilityTarget.Actor = tacAITarget.Actor;
                    tacticalAbilityTarget.ActorGridPosition = tacAITarget.Actor.Pos;
                    int num2 = 1;
                    if (tacAITarget.Actor is TacticalActor)
                    {
                        num2 = ((TacticalActor)tacAITarget.Actor).BodyState.GetHealthSlots().Count();
                    }

                    list.Clear();
                    List<KeyValuePair<TacticalAbilityTarget, float>> shootTargetsWithScores = tacActor.TacticalFaction.AIBlackboard.GetShootTargetsWithScores(weapon, tacticalAbilityTarget, tacAITarget.Pos);
                    TacticalAbilityTarget key = shootTargetsWithScores.FirstOrDefault().Key;
                    if (key != null)
                    {
                        list.AddRange(AIUtil.GetAffectedTargetsByShooting(key.ShootFromPos, tacActor, weapon, key));
                        if (list.Count == 0)
                        {
                            __result = 0f;
                            return false;
                        }

                        int num3 = 0;
                        foreach (TacticalActorBase item in list)
                        {
                            if (tacActor.RelationTo(item) == FactionRelation.Friend)
                            {
                                num *= __instance.Def.FriendlyHitScoreMultiplier;
                            }
                            else if (tacActor.RelationTo(item) == FactionRelation.Neutral)
                            {
                                num *= __instance.Def.NeutralHitScoreMultiplier;
                            }
                            else if (tacActor.RelationTo(item) == FactionRelation.Enemy)
                            {

                                if (item.Status.HasStatus<MindControlStatus>() && item.Status.GetStatus<MindControlStatus>().OriginalFaction == tacActor.TacticalFaction.TacticalFactionDef)
                                {
                                }
                                else
                                {
                                    num3++;
                                }
                            }
                        }

                        if (num < Mathf.Epsilon || num3 == 0)
                        {
                            __result = 0f;
                            return false;
                        }



                        object[] parameters = new object[] { tacAITarget.Pos, weapon.GetDamagePayload(), list.Where((TacticalActorBase ac) => tacActor.RelationTo(ac) == FactionRelation.Enemy), weapon };

                        float damagePayLoadResult = (float)getDamagePayloadMethodInfo.Invoke(__instance, parameters);

                        float num4 = damagePayLoadResult.ClampHigh(__instance.Def.MaxDamage);

                        num *= num4 / __instance.Def.MaxDamage;
                        if (num < Mathf.Epsilon)
                        {
                            __result = 0f;
                            return false;
                        }

                        DamageDeliveryType damageDeliveryType = weapon.GetDamagePayload().DamageDeliveryType;
                        if (damageDeliveryType != DamageDeliveryType.Parabola && damageDeliveryType != DamageDeliveryType.Sphere && AIUtil.CheckActorType(tacAITarget.Actor, ActorType.Civilian | ActorType.Combatant))
                        {
                            Vector3 vector = key.ShootFromPos - tacAITarget.Actor.Pos;
                            if ((!vector.x.IsZero() || !vector.z.IsZero()) && !__instance.Def.IsOverwatch)
                            {
                                num *= Mathf.Clamp((float)shootTargetsWithScores.Count / (float)num2, 0f, 1f);
                            }
                        }

                        num *= AIUtil.GetEnemyWeight(tacActor.TacticalFaction.AIBlackboard, tacAITarget.Actor);
                        __result = Mathf.Clamp(num, 0f, 1f);
                        return false;

                    }

                    __result = 0f;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        /// <summary>
        /// Fix no SP no XP when evacuating rescue vehicle/soldier last
        /// </summary>
        /// <param name="actor"></param>

        private static void FixRescueMissionEvac(TacticalActor actor)
        {
            try
            {

                TacticalFaction phoenixFaction = actor.TacticalLevel.GetFactionByCommandName("px");

                if (!phoenixFaction.Objectives.Any(obj => obj is RescueSoldiersFactionObjective))
                {
                    return;
                }

                //        TFTVLogger.Always($"phoenixFaction.TacticalActors.Any(a => a.IsAlive && !a.IsEvacuated && a!=actor) {phoenixFaction.TacticalActors.Any(a => a.IsAlive && !a.IsEvacuated && a != actor && !a.IsMounted)}");

                if (phoenixFaction.TacticalActors.Any(a => a.IsAlive && !a.IsEvacuated && a != actor && !a.IsMounted))
                {
                    return;
                }

                RescueSoldiersFactionObjective objective = (RescueSoldiersFactionObjective)phoenixFaction.Objectives.FirstOrDefault(obj => obj is RescueSoldiersFactionObjective);

                //  TFTVLogger.Always($"got here! actor.TacticalFaction.Faction.FactionDef == objective.RescuedFaction {actor.TacticalFaction.Faction.FactionDef.name} {objective.RescuedFaction.name}");

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


        [HarmonyPatch(typeof(EvacuateMountedActorsAbility), "Activate")]
        public static class EvacuateMountedActorsAbility_Activate_patch
        {
            public static void Prefix(EvacuateMountedActorsAbility __instance)
            {
                try
                {
                    // TFTVLogger.Always($"running activate exit mission ability for {__instance.TacticalActor.DisplayName}, {__instance.TacticalActor.TacticalFaction.Faction.FactionDef.name} {__instance.TacticalActor.Status?.HasStatus<MindControlStatus>()}");

                    FixRescueMissionEvac(__instance.TacticalActor);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(ExitMissionAbility), "Activate")]
        public static class ExitMissionAbility_Activate_patch
        {
            public static void Prefix(ExitMissionAbility __instance)
            {
                try
                {
                    //  TFTVLogger.Always($"running activate exit mission ability for {__instance.TacticalActor.DisplayName}, {__instance.TacticalActor.TacticalFaction.Faction.FactionDef.name} {__instance.TacticalActor.Status?.HasStatus<MindControlStatus>()}");

                    FixRescueMissionEvac(__instance.TacticalActor);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void ClearDataActorsParalysisDamage() 
        {
            try 
            {
               // TFTVLogger.Always($"_actorsWithAppliedParalysisDamage.Clear()");
                _actorsWithAppliedParalysisDamage.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static Dictionary<TacticalActor, float> _actorsWithAppliedParalysisDamage = new Dictionary<TacticalActor, float>();

        private static bool CheckActorsWithAppliedParalysisDict(TacticalActor actor, float apLost)
        {
            try
            {
                if (_actorsWithAppliedParalysisDamage.ContainsKey(actor) && _actorsWithAppliedParalysisDamage[actor] >= apLost)
                {
                    TFTVLogger.Always($"{actor.DisplayName} already lost {apLost} from PD application this turn");
                    return true;
                }

                if (_actorsWithAppliedParalysisDamage.ContainsKey(actor))
                {
                    _actorsWithAppliedParalysisDamage[actor] = apLost;
                }
                else
                {
                    _actorsWithAppliedParalysisDamage.Add(actor, apLost);
                }

                return false;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        /// <summary>
        /// Fixes inconsistency in paralysis damage application
        /// </summary>
        [HarmonyPatch(typeof(ParalysisDamageEffect), "AddTarget")]
        public static class TFTV_ParalysisDamageEffect_AddTarget_Patch
        {

            public static bool Prefix(ParalysisDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {
                    TacticalActor tacticalActor = (__instance.IsSimulation(target) ? (target.GetParam<Params>().Predictor.GetPredictingReceiver(recv.GetActor()) as TacticalActor) : (recv.GetActor() as TacticalActor));
                    if (tacticalActor == null)
                    {
                        return false;
                    }

                    //added
                    bool attackNotSoT = false; //flag to check that the PD application is coming from an attack, not the SoT effect
                    //added

                    DamageOverTimeStatus status = tacticalActor.Status.GetStatus<DamageOverTimeStatus>(__instance.ParalysisDamageEffectDef.ParalysisStacksStatus);
                    bool flag = false;
                    float num = accum.Amount;

                    TFTVLogger.Always($"num: {num}; fullDamageValue: {status?.FullDamageValue}");

                    if (status != null && status != __instance.Source) //this triggers only if the PD is added as an attack. The other case would be if status==null
                    {
                        flag = true;
                        num += status.FullDamageValue;
                        TFTVLogger.Always($"it's an attack, not SoT effect! status.FullDamageValue: {status.FullDamageValue}, so num {num}");
                    }

                    //added
                    if (flag || status == null || status != null && status.FullDamageValue == 0)
                    {
                        attackNotSoT = true;
                    }

                    float currentPD = (float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString());

                    if (!attackNotSoT)
                    {
                        num -= 1; //As this SoT effect, but the application is carried before  
                        TFTVLogger.Always($"As this SoT effect, num reduced by 1 to: {num}");
                    }
                    //added

                    float a = num / (float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString());


                    TFTVLogger.Always($"(float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString()): {(float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString())}" +
                       $"\nso, a: {a}");

                    if (Utl.GreaterThanOrEqualTo(a, 1f))
                    {
                        TFTVLogger.Always($"1 or more");

                        tacticalActor.CharacterStats.ActionPoints.Subtract(tacticalActor.CharacterStats.ActionPoints.Max);
                        if (flag || Utl.GreaterThan(a, 1f))
                        {
                            TacticalActorBase sourceTacticalActorBase = TacUtil.GetSourceTacticalActorBase(status?.Source ?? __instance.Source);
                            tacticalActor.Status.ApplyStatus(__instance.ParalysisDamageEffectDef.ParalysedStatus, sourceTacticalActorBase);
                        }
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.75f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.75f))
                    {
                        TFTVLogger.Always($"0.75 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.75f * (float)tacticalActor.CharacterStats.ActionPoints.Max);

                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.5f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.5f))
                    {
                        TFTVLogger.Always($"0.5 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.5f * (float)tacticalActor.CharacterStats.ActionPoints.Max);
                    }
                    else if (Utl.GreaterThanOrEqualTo(a, 0.25f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.25f))
                    {
                        TFTVLogger.Always($"0.25 or more");
                        tacticalActor.CharacterStats.ActionPoints.Subtract(0.25f * (float)tacticalActor.CharacterStats.ActionPoints.Max);
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


        /*  [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithAbility")]
          public static class AIAttackPositionConsideration_EvaluateWithAbilityPatch
          {
              static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
              {
                  var codes = new List<CodeInstruction>(instructions);
                  var targetMethod = AccessTools.Method(typeof(TacticalFactionVision), "IsRevealed");
                  var statusCheckMethod = AccessTools.Method(typeof(StatusComponent), "HasStatus")
                                               .MakeGenericMethod(typeof(MindControlStatus));

                  for (int i = 0; i < codes.Count; i++)
                  {
                      // Look for the IsRevealed call
                      if (codes[i].Calls(targetMethod))
                      {
                          // Insert additional check for !a.Status.HasStatus<MindControlStatus>()
                          // This modifies the condition for filtering "a" (actors)
                          codes.InsertRange(i + 1, new[]
                          {
                      new CodeInstruction(OpCodes.Ldloc_3), // Load the actor (local variable 3)
                      new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TacticalActorBase), "Status")),
                      new CodeInstruction(OpCodes.Callvirt, statusCheckMethod), // Call HasStatus<MindControlStatus>()
                      new CodeInstruction(OpCodes.Ldc_I4_0), // Load "false" (0)
                      new CodeInstruction(OpCodes.Ceq), // Check if HasStatus is false
                      new CodeInstruction(OpCodes.And) // Combine with existing IsRevealed result
                  });
                          break;
                      }
                  }

                  return (IEnumerable<CodeInstruction>)codes.AsEnumerable();
              }
          }*/




        /*  [HarmonyPatch(typeof(ApplyDamageEffectAbility), "GetCharactersToIgnore")]
          public static class ApplyDamageEffectAbility_GetCharactersToIgnore_patch
          {
              public static void Postfix(ApplyDamageEffectAbility __instance, ref IEnumerable<TacticalActorBase> __result)
              {
                  try
                  {
                      if (!__instance.ApplyDamageEffectAbilityDef.IgnoreFriendlies) 
                      {
                          List<TacticalActorBase> adjustedList = __result.ToList();

                          foreach (TacticalActorBase tacticalActorBase in __result)
                          {
                              if(tacticalActorBase.Status!=null && tacticalActorBase.Status.HasStatus<MindControlStatus>()) 
                              {
                                  adjustedList.Remove(tacticalActorBase);
                              }
                          }

                          __result = adjustedList;
                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/



    }
}
