using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVEvacAll
    {
        /// <summary>
        /// Adapted from Mad's AssortedAdjustments. https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments
        /// All hail Mad!
        /// </summary>

        private static PhoenixGeneralButton _evacAll = null;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static class SmartEvacuation
        {
            [HarmonyPatch(typeof(TacticalView), "OnAbilityExecuted")]
            public static class TacticalView_OnAbilityExecuted_Patch
            {
                internal static IEnumerable<TacticalActor> allActiveSquadmembers;

                // Override!
                public static bool Prefix(TacticalView __instance, TacticalAbility ability, TacticalActor ____selectedActor)
                {
                    // Callback Helper
                    void OnEvacuateSquadConfirmationResult(MessageBoxCallbackResult res)
                    {
                        if (res.DialogResult != MessageBoxResult.Yes)
                        {
                            InitEvacAll(__instance.TacticalLevel.View.TacticalModules.EndTurnContainer, res.UserData);
                            return;
                        }

                        // Evacuate current actor
                        TacticalAbility tacticalAbility = res.UserData as TacticalAbility;
                        TacticalAbilityTarget tacticalAbilityTarget = tacticalAbility?.GetTargets().FirstOrDefault();
                        if (tacticalAbilityTarget != null)
                        {
                            tacticalAbility.Activate(tacticalAbilityTarget);
                        }

                        // Evacuate squadmembers
                        foreach (TacticalActor tActor in allActiveSquadmembers)
                        {
                            TacticalAbility tAbility = tActor.GetAbility<ExitMissionAbility>();
                            if (tAbility == null)
                            {
                                tAbility = tActor.GetAbility<EvacuateMountedActorsAbility>();
                            }
                            TacticalAbilityTarget taTarget = tAbility?.GetTargets().FirstOrDefault();
                            //Logger.Info($"[GeoFaction_ShowExitMissionPrompt_PREFIX] ActorGridPosition: {taTarget.ActorGridPosition}");

                            if (taTarget != null)
                            {
                                tAbility.Activate(taTarget);
                            }
                        }
                        __instance.ResetViewState();
                    }

                    void InitEvacAll(UIModuleEndTurnContainer uIModuleEndTurnContainer, object tacticalAbility)
                    {
                        try
                        {
                            Resolution resolution = Screen.currentResolution;
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            float resolutionFactorHeight = (float)resolution.height / 1080f;

                            TacticalView uIModule = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View;

                            if (_evacAll == null)
                            {
                                _evacAll = UnityEngine.Object.Instantiate(uIModuleEndTurnContainer.Button, uIModule.TacticalModules.NavigationModule.transform);
                                _evacAll.transform.position = (new Vector3(960 * resolutionFactorWidth, 270 * resolutionFactorHeight, 0.0f));
                                //  TFTVLogger.Always($"{_evacAll.transform.position}, {_evacAll.transform.localPosition}");

                                Text text = _evacAll.GetComponentInChildren<Text>(); //UIText3Big
                                Image image = _evacAll.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name.Equals("UI_ArrowRight"));
                                image.sprite = DefCache.GetDef<TacticalAbilityViewElementDef>("E_View [ExitMission_AbilityDef]").SmallIcon;
                                image.rectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                                image.color = new Color32(210, 138, 48, 255);
                                Image image2 = _evacAll.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name.Equals("UI_ArrowRight (1)"));
                                //  image.gameObject.SetActive(false);
                                image2.gameObject.SetActive(false);
                                // image2.gameObject.SetActive(false);

                                text.text = $"{TFTVCommonMethods.ConvertKeyToString("TFTV_EVACUATE_ALL")}  ";
                                text.color = new Color32(210, 138, 48, 255);
                                text.alignment = TextAnchor.MiddleCenter;
                                _evacAll.PointerClicked += () => OnEvacuateSquadConfirmationResult(new MessageBoxCallbackResult { DialogResult = MessageBoxResult.Yes, UserData = tacticalAbility });



                                foreach (Component component in _evacAll.GetComponentsInChildren<Component>().Where(c => c is Image))
                                {
                                    TFTVLogger.Always($"{component.name}");
                                }
                            }
                            else
                            {
                                _evacAll.gameObject.SetActive(true);
                                _evacAll.RemoveAllClickedDelegates();
                                _evacAll.PointerClicked += () => OnEvacuateSquadConfirmationResult(new MessageBoxCallbackResult { DialogResult = MessageBoxResult.Yes, UserData = tacticalAbility });
                            }
                            // _evacAll.transform.position += new Vector3(0, 0, 0);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }



                    try
                    {
                        if (!__instance.ViewerFaction.IsPlayingTurn || (ability.TacticalActorBase && ability.TacticalActorBase.TacticalFaction != __instance.ViewerFaction) || ability is IdleAbility)
                        {
                            _evacAll?.gameObject.SetActive(false);
                            return false;
                        }

                        bool isExitMissionAbilityEnabled = ability?.TacticalActorBase?.GetAbility<ExitMissionAbility>()?.IsEnabled(null) == true;
                        bool isEvacuateMountedActorsAbilityEnabled = ability?.TacticalActorBase?.GetAbility<EvacuateMountedActorsAbility>()?.IsEnabled(null) == true;
                        bool shouldOverridePrompt = isExitMissionAbilityEnabled || isEvacuateMountedActorsAbilityEnabled;
                        //Logger.Debug($"[TacticalView_OnAbilityExecuted_PREFIX] isExitMissionAbilityEnabled: {isExitMissionAbilityEnabled}");
                        //Logger.Debug($"[TacticalView_OnAbilityExecuted_PREFIX] isEvacuateMountedActorsAbilityEnabled: {isEvacuateMountedActorsAbilityEnabled}");
                        //Logger.Debug($"[TacticalView_OnAbilityExecuted_PREFIX] shouldOverridePrompt: {shouldOverridePrompt}");

                        if (ability is IMoveAbility && ability.TacticalActor == ____selectedActor && shouldOverridePrompt)
                        {
                            //Logger.Debug($"[TacticalView_OnAbilityExecuted_PREFIX] Overriding exit mission prompt to only trigger if the whole squad is in some exit zone.");



                            // Always called by original method, needed?
                            typeof(TacticalView).GetMethod("UpdateApPool", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
                            //__instance.UpdateApPool(false);



                            TacticalAbility evacuateAbility = ____selectedActor.GetAbility<ExitMissionAbility>();
                            if (evacuateAbility == null)
                            {
                                evacuateAbility = ____selectedActor.GetAbility<EvacuateMountedActorsAbility>();
                            }

                            allActiveSquadmembers = __instance.TacticalLevel.CurrentFaction.TacticalActors.Where(a => a != ____selectedActor && a.IsActive);
                            //   Logger.Info($"[TacticalView_OnAbilityExecuted_PREFIX] allActiveSquadmembers: {allActiveSquadmembers.Select(a => a.DisplayName).ToArray().Join(null, ", ")}");

                            bool isSquadInExitZone = true;
                            foreach (TacticalActor tActor in allActiveSquadmembers)
                            {
                                TacticalAbility tAbility = tActor.GetAbility<ExitMissionAbility>();
                                if (tAbility == null)
                                {
                                    tAbility = tActor.GetAbility<EvacuateMountedActorsAbility>();
                                }
                                if (tAbility == null)
                                {
                                    // Has no relevant ability, most likely a turret
                                    //  Logger.Info($"[TacticalView_OnAbilityExecuted_PREFIX] actor: {tActor.DisplayName} has no exit/evacuate ability (IsMetallic: {tActor.IsMetallic}, GameTags: {tActor.TacticalActorBaseDef.GameTags})");
                                    continue;
                                }
                                //    Logger.Info($"[TacticalView_OnAbilityExecuted_PREFIX] actor: {tActor.DisplayName}, canEvacuate: {tAbility?.HasValidTargets}");

                                if (!tAbility.HasValidTargets)
                                {
                                    isSquadInExitZone = false;
                                    // Don't break to test for a while
                                    //break;
                                }
                            }
                            //  Logger.Info($"[TacticalView_OnAbilityExecuted_PREFIX] isSquadInExitZone: {isSquadInExitZone}");

                            if (isSquadInExitZone)
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_EVACUATE_SQUAD"), MessageBoxIcon.Question, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnEvacuateSquadConfirmationResult), null, evacuateAbility);
                            }



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
                        return true;
                    }
                }
            }
        }

    }
}
