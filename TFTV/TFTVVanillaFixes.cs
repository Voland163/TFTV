using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.AI;
using Base.Audio;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Eventus;
using Base.Input;
using Base.Levels;
using Base.Rendering.ObjectRendering;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsSharedData;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
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
using PhoenixPoint.Tactical.View.ViewStates;
using PRMBetterClasses.SkillModifications;
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
            //Put in Aircraft rework, Anu Mist Module
            /*    [HarmonyPatch(typeof(TacticalFactionVision), "ReUpdateVisibilityTowardsActorImpl")]
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
                }*/

        }

        internal class UI
        {




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

                        MethodInfo activateAttackAbilityState = internalType.GetMethod("ActivateAttackAbilityState", BindingFlags.NonPublic | BindingFlags.Instance);

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

                        if (activateAttackAbilityState != null)
                        {
                            harmony.Patch(activateAttackAbilityState, postfix: new HarmonyMethod(typeof(TFTVVanillaFixes.UI), nameof(ActivateAttackAbilityState)));
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



            [HarmonyPatch(typeof(UIStateCharacterStatus), "GetActionPoints")]
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

                    int maxActionPoints = GetAdjustedSpeedValueForParalyisDamage(actor); //actor.CharacterStats.ActionPoints.IntMax);

                    string value = $"{maxActionPoints}";//string.Format("{0}/{1}", maxActionPoints, maxActionPoints);
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
        }

        internal class Tactical
        {
            internal class TacticalSavesAIBug
            {
                [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.StartTurn))]
                internal static class TacticalActorStartTurnPatch
                {
                    private static readonly FieldInfo AbilityUsesThisTurnField = AccessTools.Field(typeof(TacticalActor), "__abilityUsesThisTurn");


                    public static void Prefix(TacticalActor __instance, bool ____currentlyDeserializing)
                    {
                        try
                        {



                            if (__instance == null)
                            {
                                return;
                            }

                            TFTVLogger.Always($"Start Turn for TacticalActor {__instance?.DisplayName}");

                            if (__instance.AbilityTraits.Contains(TacticalActor.DoNotResetThisTurnTrait))
                            {
                                TFTVLogger.Always($"TacticalActor has DoNotResetThisTurnTrait");

                                return;
                            }



                            if (!____currentlyDeserializing)
                            {
                                TFTVLogger.Always($"TacticalActor is not deserializing");
                                return;
                            }

                            TFTVLogger.Always($"AbilityUsesThisTurnField null? {AbilityUsesThisTurnField == null}");

                            if (AbilityUsesThisTurnField.GetValue(__instance) is Dictionary<TacticalAbilityDef, int> abilityUses)
                            {
                                abilityUses.Clear();
                            }


                            return;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }
            }

            internal class DecimalWillpoints
            {


                [HarmonyPatch(typeof(StatusStat), "ApplyStatModification")]
                public static class StatusStat_ApplyStatModification_patch
                {
                    public static void Prefix(StatusStat __instance, ref StatModification statMod)
                    {
                        try
                        {
                            //TFTVLogger.Always($"ApplyStatModification: {__instance.Name}");

                            if (__instance.Name == "WillPoints")
                            {
                                float roundedF = Mathf.CeilToInt(statMod.Value);

                                if (roundedF != statMod.Value)
                                {
                                    TFTVLogger.Always($"ApplyStatModification: rounding WP from {statMod.Value} to {roundedF}");
                                    statMod.Value = roundedF;
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




                [HarmonyPatch(typeof(StatusStat), "Set")]
                public static class StatusStat_Set_patch
                {
                    public static void Prefix(StatusStat __instance, ref float f)
                    {
                        try
                        {
                            // TFTVLogger.Always($"{__instance.Name}");

                            if (__instance.Name == "WillPoints")
                            {
                                float roundedF = Mathf.CeilToInt(f);

                                if (roundedF != f)
                                {
                                    TFTVLogger.Always($"rounding WP from {f} to {roundedF}");
                                    f = roundedF;
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
            }

            internal class Audio
            {
                private static bool _musicVolumeAncientMapAdjusted = false;

                [HarmonyPatch(typeof(AudioManager), "PlayEvent")]
                public static class AudioManager_PlayEvent_patch
                {
                    public static void Prefix(AudioManager __instance, AudioEventData eventData, BaseEventContext context)
                    {
                        try
                        {
                            if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null)
                            {
                                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                                if (TFTVAncients.CheckIfAncientMap(controller) && !_musicVolumeAncientMapAdjusted)
                                {
                                    //if (eventData.Event.Name == "TacticalMusicEnemyTurn" || eventData.Event.Name == "TacticalMusicPlayerTurn")
                                    //  {
                                    if (__instance.MasterVolumeRTPC.GetGlobalValue() > 0.25f && __instance.MusicVolumeRTPC.GetGlobalValue() > 0.25f)
                                    {
                                        __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 0.25f);
                                        _musicVolumeAncientMapAdjusted = true;

                                        //  AKRESULT result = AkSoundEngine.SetRTPCValue(eventData.Event.Id, 0.01f, __instance.MusicVolumeRTPC.Id);
                                        // AKRESULT aKRESULT = AkSoundEngine.SetRTPCValue("", eventData.Event.Id, 0.01f);

                                        TFTVLogger.Always($"Ancients map: music reduced to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
                                    }//AKRESULT: {result}");
                                }
                                else if (!TFTVAncients.CheckIfAncientMap(controller) && _musicVolumeAncientMapAdjusted)
                                {
                                    __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 4f);

                                    _musicVolumeAncientMapAdjusted = false;

                                    TFTVLogger.Always($"resetting music to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
                                }

                                //  AkSoundEngine
                                //  }
                                return;
                            }

                            if (_musicVolumeAncientMapAdjusted)
                            {
                                __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 4f);

                                _musicVolumeAncientMapAdjusted = false;
                                TFTVLogger.Always($"resetting music to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
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

            internal class UmbraFire
            {
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

            }

            internal class AI
            {


                [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithAbility")]
                public static class AIAttackPositionConsideration_EvaluateWithAbilityPatch
                {
                    public static bool Prefix(AIAttackPositionConsideration __instance, IAIActor actor, IAITarget target, TacticalAbilityDef abilityDef, ref float __result)
                    {
                        try
                        {

                            MethodInfo getDamagePayloadMethodInfo = typeof(AIAttackPositionConsideration).GetMethod("GetPayloadMaxDamage", BindingFlags.NonPublic | BindingFlags.Instance);

                            //TFTVLogger.Always($"getDamagePayloadMethodInfo null {getDamagePayloadMethodInfo==null}");

                            TacticalActor tacActor = (TacticalActor)actor;
                            TacAITarget tacAITarget = (TacAITarget)target;
                            float eps = 0.01f;
                            if (abilityDef == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            TacticalAbility abilityWithDef = tacActor.GetAbilityWithDef<TacticalAbility>(abilityDef);
                            if (abilityWithDef == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            if (!abilityWithDef.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                            {
                                __result = 0f;
                                return false;
                            }

                            float maxMoveAndActRange = tacActor.GetMaxMoveAndActRange(abilityWithDef, tacAITarget.MoveAbility);
                            if (Utl.GreaterThan(tacAITarget.PathLength, maxMoveAndActRange, eps))
                            {
                                __result = 0f;
                                return false;
                            }

                            DamagePayload damagePayload = (abilityWithDef as IDamageDealer)?.GetDamagePayload();
                            if (damagePayload == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            IEnumerable<TacticalActorBase> enemies = from a in tacActor.TacticalFaction.AIBlackboard.GetEnemies(tacActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask), checkKnowledge: false)
                                                                     where tacActor.TacticalFaction.Vision.IsRevealed(a)
                                                                     select a;
                            IEnumerable<TacticalAbilityTarget> enumerable = abilityWithDef.GetTargetsAt(tacAITarget.Pos);
                            if (!__instance.Def.InclideAlliesAsTargets)
                            {
                                enumerable = enumerable.Where((TacticalAbilityTarget x) => enemies.Contains(x.Actor));
                            }

                            List<TacticalActorBase> list = new List<TacticalActorBase>(10);
                            float num = 0f;
                            foreach (TacticalAbilityTarget item in enumerable)
                            {
                                float num2 = 1f;
                                list.Clear();
                                if (abilityWithDef.OriginTargetData.TargetSelf)
                                {
                                    list.AddRange(AIUtil.GetAffectedTargetsByDamageAbility(tacActor, tacAITarget.Pos, abilityWithDef as IDamageDealer));
                                }
                                else
                                {
                                    list.AddRange(AIUtil.GetAffectedTargetsByDamageAbility(tacActor, item.Actor.Pos, abilityWithDef as IDamageDealer));
                                }

                                list.RemoveWhere(t => tacActor.RelationTo(t) == FactionRelation.Enemy && !tacActor.TacticalFaction.Vision.IsLocated(t) && !tacActor.TacticalFaction.Vision.IsRevealed(t));

                                int num3 = 0;
                                foreach (TacticalActorBase item2 in list)
                                {
                                    if (tacActor.RelationTo(item2) == FactionRelation.Friend)
                                    {
                                        if ((__instance.Def.IgnoreDamageOnSelf && item2 as TacticalActor != tacActor) || !__instance.Def.IgnoreDamageOnSelf)
                                        {
                                            num2 *= __instance.Def.FriendlyHitScoreMultiplier;
                                        }
                                    }
                                    else if (tacActor.RelationTo(item2) == FactionRelation.Neutral)
                                    {
                                        num2 *= __instance.Def.NeutralHitScoreMultiplier;
                                    }

                                    if (tacActor.RelationTo(item2) == FactionRelation.Enemy)
                                    {
                                        if (item2.Status != null && item2.Status.HasStatus<MindControlStatus>() && item2.Status.GetStatus<MindControlStatus>().OriginalFaction == tacActor.TacticalFaction.TacticalFactionDef)
                                        {
                                        }
                                        else
                                        {
                                            num3++;
                                        }


                                    }
                                }

                                if (num2 < Mathf.Epsilon || num3 == 0)
                                {
                                    continue;
                                }

                                object[] parameters = new object[] { tacAITarget.Pos, damagePayload, list.Where((TacticalActorBase ac) => tacActor.RelationTo(ac) == FactionRelation.Enemy), null };

                                float payloadMaxDamage = (float)getDamagePayloadMethodInfo.Invoke(__instance, parameters);
                                if (!(payloadMaxDamage < 10f))
                                {
                                    float num4 = payloadMaxDamage.ClampHigh(__instance.Def.MaxDamage);
                                    num2 *= num4 / __instance.Def.MaxDamage;
                                    num2 *= AIUtil.GetEnemyWeight(tacActor.TacticalFaction.AIBlackboard, item.Actor);
                                    num2 = Mathf.Clamp(num2, 0f, 1f);
                                    if (num < num2)
                                    {
                                        tacAITarget.Actor = item.Actor;
                                        num = num2;
                                    }
                                }
                            }

                            __result = num;
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

            internal class XP
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


                [HarmonyPatch(typeof(TacticalFaction), "GiveExperienceForObjectives")]
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

                            int mentorCount = list.Count(p => TFTVDrills.DrillsHarmony.MentorProtocol.CheckForMentorProtocolAbility(p));
                            if (mentorCount > 0)
                            {
                                num += mentorCount * 2;
                            }

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

                            int mentorPool = 0;
                            foreach (TacticalActor actor in list)
                            {
                                if (TFTVDrills.DrillsHarmony.MentorProtocol.CheckForMentorProtocolAbility(actor))
                                {
                                    mentorPool += xpAwards[actor];
                                    xpAwards[actor] = 0;
                                }
                            }

                            List<TacticalActor> mentorRecipients = list.Where(a => !TFTVDrills.DrillsHarmony.MentorProtocol.CheckForMentorProtocolAbility(a) && a.LevelProgression.Level < 7).ToList();
                            if (mentorPool > 0 && mentorRecipients.Any())
                            {
                                DistributeExperience(mentorPool, mentorRecipients, difficulty, xpAwards);
                            }

                            foreach (KeyValuePair<TacticalActor, int> award in xpAwards)
                            {
                                if (award.Value <= 0)
                                {
                                    continue;
                                }

                                if (award.Key.LevelProgression.Level >= 7)
                                {
                                    continue;
                                }

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
            }

            internal class ParalysisDamage
            {
                private static Dictionary<TacticalActor, float> _actorsWithAppliedParalysisDamage = new Dictionary<TacticalActor, float>();

                public static void ClearDataActorsParalysisDamage()
                {
                    try
                    {
                        //  TFTVLogger.Always($"Clearing paralysis damage");
                        _actorsWithAppliedParalysisDamage.Clear();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static bool CheckActorsWithAppliedParalysisDict(TacticalActor actor, float apLost)
                {
                    try
                    {

                        //   TFTVLogger.Always($"actor: {actor.DisplayName} ap value: {actor.CharacterStats.ActionPoints.Value} max value: {actor.CharacterStats.ActionPoints.IntMax}");

                        if (_actorsWithAppliedParalysisDamage.ContainsKey(actor) && _actorsWithAppliedParalysisDamage[actor] >= apLost)
                        {
                            //  TFTVLogger.Always($"{actor.DisplayName} already lost {apLost} from PD application this turn");
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

                            //   TFTVLogger.Always($"{tacticalActor.DisplayName} num: {num}; fullDamageValue: {status?.FullDamageValue}");

                            if (status != null && status != __instance.Source) //this triggers only if the PD is added as an attack. The other case would be if status==null
                            {
                                flag = true;
                                num += status.FullDamageValue;
                                //  TFTVLogger.Always($"it's an attack, not SoT effect! status.FullDamageValue: {status.FullDamageValue}, so num {num}");
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
                                          // TFTVLogger.Always($"As this SoT effect, num reduced by 1 to: {num}");
                            }
                            //added

                            float a = num / (float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString());


                            //  TFTVLogger.Always($"{tacticalActor.DisplayName}, STR: {(float)tacticalActor.Status.GetStat(__instance.ParalysisDamageEffectDef.TargetStat.ToString())}, num: {num}, a: {a}"); 

                            if (Utl.GreaterThanOrEqualTo(a, 1f))
                            {
                                //    TFTVLogger.Always($"1 or more");

                                tacticalActor.CharacterStats.ActionPoints.Subtract(tacticalActor.CharacterStats.ActionPoints.Max);
                                // if (flag || Utl.GreaterThan(a, 1f))
                                // {
                                //   TFTVLogger.Always($"greater than 1");

                                TacticalActorBase sourceTacticalActorBase = TacUtil.GetSourceTacticalActorBase(status?.Source ?? __instance.Source);
                                tacticalActor.Status.ApplyStatus(__instance.ParalysisDamageEffectDef.ParalysedStatus, sourceTacticalActorBase);
                                //  }
                            }
                            else if (Utl.GreaterThanOrEqualTo(a, 0.75f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.75f))
                            {
                                //  TFTVLogger.Always($"0.75 or more");
                                tacticalActor.CharacterStats.ActionPoints.Subtract(0.75f * (float)tacticalActor.CharacterStats.ActionPoints.Max);

                            }
                            else if (Utl.GreaterThanOrEqualTo(a, 0.5f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.5f))
                            {
                                //   TFTVLogger.Always($"0.5 or more");
                                tacticalActor.CharacterStats.ActionPoints.Subtract(0.5f * (float)tacticalActor.CharacterStats.ActionPoints.Max);
                            }
                            else if (Utl.GreaterThanOrEqualTo(a, 0.25f) && !CheckActorsWithAppliedParalysisDict(tacticalActor, 0.25f))
                            {
                                //   TFTVLogger.Always($"0.25 or more");
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

            }

            internal class MindControl
            {
                [HarmonyPatch(typeof(MindControlStatus), "WillBreakControl")]
                public static class MindControlStatus_WillBreakControl_patch
                {
                    public static bool Prefix(MindControlStatus __instance, bool shouldApplyControlCost, float ____minUpkeepCost, ref bool __result)
                    {
                        try
                        {
                            if (!shouldApplyControlCost)
                            {
                                __result = false;
                            }
                            else
                            {
                                StatusStat willPoints = __instance.ControllerActor.CharacterStats.WillPoints;
                                float num = Mathf.Max(____minUpkeepCost, __instance.TacticalActor.TacticalActorDef.WillPointWorth);

                                if (TFTVDrills.DrillsHarmony.VirulentGrip.CheckForVirulentGripAbility(__instance.ControllerActor, __instance.TacticalActor))
                                {
                                    num /= 2;
                                    TFTVLogger.Always($"halving cost of MCing {__instance.TacticalActor.name} because infected and {__instance.ControllerActor.DisplayName} has VirulentGrip ability");
                                }

                                bool flag = (willPoints - num) < 0;
                                if (flag)
                                {
                                    __instance.RequestUnapply(__instance.TacticalActorBase.Status);
                                }
                                else
                                {
                                    willPoints.Subtract(num);
                                }
                                __result = flag;
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

            internal class UI
            {

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

                private static void AdjustPortraitLights(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
                {
                    try
                    {
                        // --- Key Light (e.g., DirLighjt2) ---
                        // Aim for a warm, flattering tone.
                        keyLight.useColorTemperature = true;
                        keyLight.colorTemperature = 5500; // Warmer than 6570K for a cozy feel
                                                          // Optionally, adjust the color to emphasize warmth:
                        keyLight.color = new Color(1.0f, 0.9f, 0.8f, 1.0f);
                        keyLight.intensity = 1.0f; // Adjust intensity as needed

                        // --- Fill Light (e.g., DirLighjt3) ---
                        // Use a softer light to reduce harsh shadows.
                        fillLight.useColorTemperature = true;
                        fillLight.colorTemperature = 5500; // Consistent with the key light
                                                           // Slightly modify the color to be neutral but warm
                        fillLight.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                        fillLight.intensity = 0.5f; // Lower intensity for subtle filling

                        // --- Rim/Hair Light (e.g., DirLighjt1) ---
                        // This light provides a cool accent to separate the character from the background.
                        rimLight.useColorTemperature = true;
                        rimLight.colorTemperature = 6500; // Keep it cooler for contrast
                        rimLight.color = new Color(0.2f, 0.825f, 1.0f, 1.0f);
                        rimLight.intensity = 0.7f; // Adjust intensity so it provides a gentle highlight

                        // --- Ambient Light (e.g., AmbienceLight) ---
                        // A lower-intensity, soft warm light to ensure overall balance.
                        ambientLight.useColorTemperature = true;
                        ambientLight.colorTemperature = 5500; // Warmer ambient tone
                                                              // Slightly tinted to avoid a flat look
                        ambientLight.color = new Color(1.0f, 1.0f, 0.95f, 1.0f);
                        ambientLight.intensity = 0.3f; // Lower intensity to prevent wash-out
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustLovecraftianPortraitLights(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
                {
                    // --- Key Light ---
                    // Use a directional key light with a dark, cold cyan tint.
                    // A relatively high intensity with a hard edge creates stark shadows.
                    keyLight.useColorTemperature = true;
                    keyLight.colorTemperature = 6500; // Cold tone.
                    keyLight.color = new Color(0.25f, 0.45f, 0.55f, 1.0f); // Dark cyan.
                    keyLight.intensity = 1.2f; // Strong key light for dramatic shadows.
                                               // Optionally, if your Light supports a spot or angle, narrow the beam.

                    // --- Fill Light ---
                    // Use a very low-intensity fill to let the shadows dominate.
                    fillLight.useColorTemperature = true;
                    fillLight.colorTemperature = 6500;
                    fillLight.color = new Color(0.1f, 0.15f, 0.2f, 1.0f); // Extremely muted, dark fill.
                    fillLight.intensity = 0.3f; // Low intensity to maintain deep shadows.

                    // --- Rim/Hair Light ---
                    // A rim light can help define the edges of the character.
                    // Use a cooler tone (even slightly bluer) for an eerie outline.
                    rimLight.useColorTemperature = true;
                    rimLight.colorTemperature = 7000;
                    rimLight.color = new Color(0.2f, 0.35f, 0.6f, 1.0f); // Cool blue.
                    rimLight.intensity = 0.8f; // Sufficient to outline without overpowering.

                    // --- Ambient Light ---
                    // Keep ambient light very low to prevent flattening the mood.
                    ambientLight.useColorTemperature = true;
                    ambientLight.colorTemperature = 6500;
                    ambientLight.color = new Color(0.05f, 0.05f, 0.1f, 1.0f); // Nearly dark with a hint of blue.
                    ambientLight.intensity = 0.2f; // Low overall illumination.
                }



                private static void AdjustPortraitLightsCold(Light rimLight, Light keyLight, Light fillLight, Light ambientLight)
                {
                    try
                    {
                        // --- Key Light (Primary Illumination) ---
                        // Use a cool blue-tinted key light.
                        keyLight.useColorTemperature = true;
                        keyLight.colorTemperature = 7000; // A cooler temperature for a cold look.
                        keyLight.color = new Color(0.8f, 0.9f, 1.0f, 1.0f); // Slight blue tint.
                        keyLight.intensity = 1.0f; // Adjust intensity as needed.

                        // --- Fill Light (Subtle Shadow Reduction) ---
                        // Use a softer fill with a similar cool tone.
                        fillLight.useColorTemperature = true;
                        fillLight.colorTemperature = 7000;
                        fillLight.color = new Color(0.7f, 0.8f, 0.9f, 1.0f); // A slightly softer blue tint.
                        fillLight.intensity = 0.5f; // Lower intensity to gently fill shadows.

                        // --- Rim/Hair Light (Accent/Edge Highlight) ---
                        // Use an even cooler tone to create a distinct rim effect.
                        rimLight.useColorTemperature = true;
                        rimLight.colorTemperature = 7500; // Even cooler for a crisp rim.
                        rimLight.color = new Color(0.3f, 0.5f, 0.9f, 1.0f); // A deeper blue.
                        rimLight.intensity = 0.7f; // Adjust so it provides a subtle edge highlight.

                        // --- Ambient Light (Overall Scene Illumination) ---
                        // A soft, low-intensity ambient light to round out the scene.
                        ambientLight.useColorTemperature = true;
                        ambientLight.colorTemperature = 7000;
                        ambientLight.color = new Color(0.9f, 0.9f, 1.0f, 1.0f); // Light blue tint.
                        ambientLight.intensity = 0.3f; // Keep it low to avoid washing out details.
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



                private static bool CheckOSX()
                {
                    try
                    {
                        if (Application.platform == RuntimePlatform.OSXPlayer ||
                           Application.platform == RuntimePlatform.OSXEditor)
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


                [HarmonyPatch(typeof(SoldierPortraitUtil), "RenderSoldierNoCopy")]
                public static class SoldierPortraitUtil_RenderSoldierNoCopy_patch
                {

                    public static bool Prefix(GameObject soldierToRender, RenderPortraitParams renderParams, Camera usedCamera, ref Texture2D __result)
                    {
                        try
                        {

                            /* List<Light> lights = new List<Light>()
                             {
                                 soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt2")),
                                 soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt3")),
                                 soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("DirLighjt1")),
                                 soldierToRender.transform.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name.Contains("AmbienceLight")),
                             };*/

                            // AdjustPortraitLights(lights[0], lights[1], lights[2], lights[3]);
                            //   AdjustPortraitLightsCold(lights[0], lights[1], lights[2], lights[3]);
                            //  AdjustLovecraftianPortraitLights(lights[0], lights[1], lights[2], lights[3]);

                            Texture2D texture2D = new Texture2D(renderParams.RenderedPortraitsResolution.x, renderParams.RenderedPortraitsResolution.y, TextureFormat.RGBA32, mipChain: true);
                            texture2D.filterMode = FilterMode.Trilinear;


                            RenderingEnvironment renderingEnvironment = new RenderingEnvironment(renderParams.RenderedPortraitsResolution, RenderingEnvironmentOption.NoBackground, null, usedCamera);

                            if (CheckOSX())
                            {
                                renderingEnvironment = new RenderingEnvironment(renderParams.RenderedPortraitsResolution, RenderingEnvironmentOption.NoBackground, Color.black, usedCamera);
                            }

                            //  TFTVLogger.Always($"QualitySettings.antiAliasing: {QualitySettings.antiAliasing}");

                            //  renderingEnvironment.RenderTexture.antiAliasing = (QualitySettings.antiAliasing > 0) ? QualitySettings.antiAliasing : 4;

                            float cameraDistance = renderParams.CameraDistance;

                            Transform transform = soldierToRender.transform.FindTransformInChildren("Nose");
                            if (transform == null)
                            {
                                transform = soldierToRender.transform.FindTransformInChildren("Jaw");//Head");
                                cameraDistance = 0.63f;

                                if (transform == null)
                                {
                                    transform = soldierToRender.transform;
                                }
                                ;
                            }

                            Transform transform2 = soldierToRender.transform;
                            Vector3 position = transform2.position;
                            Quaternion rotation = transform2.rotation;
                            transform2.position = renderingEnvironment.OriginPosition;
                            transform2.rotation = renderingEnvironment.OriginRotation;

                            SoldierFrame cameraFrameLogic = new SoldierFrame(transform, renderParams.CameraFoV, cameraDistance, renderParams.CameraHeight, renderParams.CameraSide);
                            renderingEnvironment.Render(cameraFrameLogic, useOrigin: false);
                            renderingEnvironment.WriteResultsToTexture(texture2D);
                            texture2D.Apply(updateMipmaps: true);

                            transform2.position = position;
                            transform2.rotation = rotation;
                            __result = texture2D;

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
                /// Removes the empty target icons from destroyed vehicles
                /// </summary>
                [HarmonyPatch(typeof(UIModuleSpottedEnemies), "SetAllEnemies")]
                public static class UIModuleSpottedEnemies_SetAllEnemies_patch
                {
                    public static void Prefix(UIModuleSpottedEnemies __instance, ref IList<TacticalAbilityTarget> allSortedKnownTargets)
                    {
                        try
                        {

                            List<TacticalAbilityTarget> targetsToRemove = new List<TacticalAbilityTarget>();

                            foreach (TacticalAbilityTarget target in allSortedKnownTargets)
                            {

                                if (target.Actor != null)
                                {
                                    TacticalActorBase tacticalActorBase = target.Actor;

                                    if (tacticalActorBase.ViewElementDef == null || tacticalActorBase.ViewElementDef.SmallIcon == null)
                                    {
                                        targetsToRemove.Add(target);
                                        //TFTVLogger.Always($"{tacticalActorBase.name} has no viewelement");
                                    }
                                }

                            }

                            allSortedKnownTargets.RemoveRange(targetsToRemove);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
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
            }

            internal class Damage
            {
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

            /// <summary>
            /// Codemite's solution to Acheron's junk transforms (part of the Acheron bad targeting problem). All hail Codemite!
            /// </summary>
            [HarmonyPatch(typeof(TacticalItem), "SetupAimPoint")]
            public static class TFTV_TacticalItem_SetupAimPoint_patch
            {
                public static bool Prefix(TacticalItem __instance, ref Transform ____aimPoint)
                {
                    try
                    {

                        ____aimPoint = __instance.FindTransform(__instance.TacticalItemDef.AimPoint);


                        if (__instance.TacticalActor != null && __instance.TacticalActor.GameTags != null && __instance.TacticalActor.HasGameTag(DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef")))
                        {
                            // The junk data in Acheron's legs is under the  DEF-shin.L transform
                            bool isAcheronJunkData = true && // instead of true check for TacticalActor != null && [is this acheron class]
                                       ____aimPoint != null &&
                                       ____aimPoint.parent.name.Contains("DEF-shin.L") && ____aimPoint.parent.childCount == 1;


                            if (isAcheronJunkData)
                            {
                                //  TFTVLogger.Always($"Junk Acheron part: {____aimPoint.name} {____aimPoint.parent.name} {____aimPoint.position}");

                                // Cache the transform just in case - we don't want a body part without any aim point
                                var tempPoint = ____aimPoint;

                                // Remove the transform from the registered list.
                                __instance.OwnedTransforms.Remove(____aimPoint);

                                // The FindTransform function goes through all the OwnedTransforms and finds the first one with the supplied name
                                ____aimPoint = __instance.FindTransform(__instance.TacticalItemDef.AimPoint);

                                // In case no other aim point is found restore the old one, even if it is a bit sus
                                if (____aimPoint == null)
                                {
                                    // TFTVLogger.Always($"keeping the junk!!");
                                    __instance.OwnedTransforms.Add(tempPoint);
                                    ____aimPoint = tempPoint;
                                }
                            }
                            /* else
                             {

                                 TFTVLogger.Always($"NOT Junk Acheron part: {____aimPoint.name} {____aimPoint.parent.name} {____aimPoint.position}");

                             }*/
                        }
                        if (____aimPoint == null)
                        {
                            Debug.LogError($"Item {__instance} has no AimPoint!", __instance.VisualRoot);
                            ____aimPoint = __instance.VisualRoot;
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

        internal class Geoscape
        {

            /// <summary>
            /// Fixes softlock if game picks a turret deployed by a haven defender as a recruit
            /// </summary>
            [HarmonyPatch(typeof(HavenMissionUtil), "GenerateHavenMissionRecruitmentReward")]
            public static class GenerateHavenMissionRecruitmentRewardPatch
            {
                static bool Prefix(GeoMission mission, ref GeoFactionReward __result)
                {
                    try
                    {
                        GeoFactionReward geoFactionReward = new GeoFactionReward();
                        GeoSite site = mission.Site;
                        GeoFaction uninfestedOwner = site.GetComponent<GeoHaven>().UninfestedOwner;
                        if (mission.GetMissionOutcomeState() == TacFactionState.Won)
                        {
                            int diplomacy = site.Owner.Diplomacy.GetDiplomacy(site.GeoLevel.ViewerFaction);
                            int diplomacy2 = site.GetComponent<GeoHaven>().Leader.Diplomacy.GetDiplomacy(site.GeoLevel.ViewerFaction);
                            if (HavenMissionUtil.FactionSoldierAlwaysJoin || (diplomacy > 0 && diplomacy2 > 0))
                            {
                                SharedGameTagsDataDef tags = site.GeoLevel.SharedData.SharedGameTags;
                                List<TacActorUnitResult> list = (from u in (from s in (from u in HavenMissionUtil.GetHavenUnitsFromMission(mission)
                                                                                       where u.MissionHistoryResult.HasItemType(UnitHistoryItemType.ControlledByPlayer)
                                                                                       select u).ToList()
                                                                            select s.Data).OfType<TacActorUnitResult>()
                                                                 where u.IsAlive && !u.HasTag(tags.CivilianTag) && u.SourceTemplate != null
                                                                 select u).ToList();

                                /*  foreach(TacActorUnitResult tacActorUnitResult in list) 
                                  {
                                      TFTVLogger.Always($"{tacActorUnitResult?.TacticalActorBaseDef?.name} {tacActorUnitResult?.SourceTemplate?.name}");

                                  }*/

                                if (site.GeoLevel.PhoenixFaction.LivingQuarterFreeSpace > 0 && list.Any())
                                {
                                    TacActorUnitResult randomElement = list.GetRandomElement();
                                    int num = UnityEngine.Random.Range(0, 100);
                                    if (HavenMissionUtil.FactionSoldierAlwaysJoin || num < site.GeoLevel.CurrentDifficultyLevel.HavenRescueSoldierJoinChance)
                                    {
                                        randomElement.Statuses.RemoveAll((StatusResult s) => s.Def is MindControlStatusDef);
                                        GeoCharacter geoCharacter = site.GeoLevel.CharacterGenerator.GenerateUnit(uninfestedOwner, randomElement).SpawnAsCharacter();
                                        geoCharacter.ApllyTacticalResult(randomElement);
                                        geoFactionReward.Units.Add(geoCharacter);
                                    }
                                }
                            }
                        }
                        __result = geoFactionReward;

                        return false;
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



            [HarmonyPatch(typeof(GeoHaven), "get_RecruitCorruption")]
            public static class TFTV_GeoHaven_get_RecruitCorruption_VanillaBugBix_patch
            {
                public static void Postfix(GeoHaven __instance, ref int __result)
                {
                    try
                    {
                        if (__result > 0 &&
                            (__instance.AvailableRecruit.GetGameTags().Contains(Shared.SharedGameTags.VehicleTag)
                            || __instance.AvailableRecruit.ClassTags.Contains(Shared.SharedGameTags.VehicleClassTag)))
                        {
                            __result = 0;
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
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
        }

        internal class Research
        {

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

                        //TFTVLogger.Always($"actorResearchRequirementDef: {actorResearchRequirementDef.name}");

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
                                // TFTVLogger.Always($"item: {item?.TacticalActorBaseDef?.name} is valid? {__instance.IsValidUnit(item)}");

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

            /// <summary>
            /// Fixes not recognizing required research tags
            /// </summary>
            [HarmonyPatch(typeof(ActorResearchRequirementDef), "IsValidActor")]
            public static class TFTV_ActorResearchRequirementDef_IsValidActor
            {
                public static bool Prefix(ActorResearchRequirementDef __instance, GeoUnitDescriptor unit, TacticalActorDef actorRequirement, GameTagDef tagRequirement, ref bool __result)
                {
                    try
                    {

                        if (unit == null)
                        {
                            // TFTVLogger.Always("early exit 1");
                            __result = false;
                            return false;
                        }

                        TacticalActorBaseDef tacticalActorBaseDef = unit.UnitType.TemplateDef.TacticalActorBaseDef;
                        if (actorRequirement != null && actorRequirement != tacticalActorBaseDef)
                        {
                            //  TFTVLogger.Always("early exit 2");
                            __result = false;
                            return false;
                        }

                        if (tagRequirement != null)
                        {
                            // TFTVLogger.Always("got here");

                            bool flag = tacticalActorBaseDef.GameTags.Contains(tagRequirement);
                            if (!flag)
                            {
                                List<TacticalItemDef> enumerable = unit.ArmorItems;
                                List<TacticalItemDef> equipment = unit.Equipment;
                                if (enumerable == null)
                                {
                                    enumerable = new List<TacticalItemDef>();
                                }

                                if (equipment != null)
                                {
                                    enumerable.Concat(equipment);
                                }

                                enumerable.AddRange(unit.UnitType.TemplateDef.GetTemplateBodyparts());



                                flag = enumerable.Any(b => b.Tags.Contains(tagRequirement));

                            }

                            if (!flag)
                            {
                                __result = false;
                                return false;
                            }

                            __result = true;
                        }

                        __result = true;
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











































        /*[HarmonyPatch(typeof(BaseStat), "CalcModValueBasedOn", typeof(float), typeof(StatModification), typeof (StatRoundingMode))]
        public static class BaseStat_CalcModValueBasedOn_patch
        {
            public static void Prefix(BaseStat __instance, StatModification statMod, ref StatRoundingMode roundingMode)
            {
                try
                {


                    TFTVLogger.Always($"statMod.StatName: {statMod.StatName} {statMod.Modification}");

                    if (statMod.StatName == "WillPoints" && statMod.Modification == StatModificationType.AddRestrictedToBounds)
                    {
                        
                        roundingMode = StatRoundingMode.Ceil;

                        TFTVLogger.Always($"set rounding mode to Ceil");


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
