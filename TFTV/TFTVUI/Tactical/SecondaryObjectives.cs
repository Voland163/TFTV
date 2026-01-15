using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using hoenixPoint.Tactical.View.ViewControllers;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;
using static TFTV.TFTVCapturePandorans;

namespace TFTV.TFTVUI.Tactical
{
    internal class SecondaryObjectivesTactical
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static Transform _secondaryObjectives = null;
        private static Transform _secondaryObjectivesTitle = null;

        private static ActorHasStatusFactionObjectiveDef _captureAnyRevenant;
        private static ActorHasStatusFactionObjectiveDef _captureScylla;
        private static ActorHasStatusFactionObjectiveDef _captureSiren;
        private static ActorHasStatusFactionObjectiveDef _captureGooAlien;
        private static ActorHasStatusFactionObjectiveDef _captureViralAlien;
        private static ActorHasStatusFactionObjectiveDef _captureMistAlien;
        private static ActorHasStatusFactionObjectiveDef _capturePsychicAlien;

        private static KillActorFactionObjectiveDef _killRevenantMimic;
        private static KillActorFactionObjectiveDef _killRevenantDybbuk;
        private static KillActorFactionObjectiveDef _killRevenantNemesis;
        private static KillActorFactionObjectiveDef _killScylla;

        public static List<string> AvailableSecondaryObjectivesTactical = new List<string>();
        private static List<FactionObjectiveDef> _secondaryObjectiveDefsInPlay = new List<FactionObjectiveDef>();
        private static List<FactionObjectiveDef> _secondaryObjectiveDefsALL = new List<FactionObjectiveDef>();

        private static Dictionary<FactionObjective, List<TacticalActor>> _objectivesTargetsDictionary = new Dictionary<FactionObjective, List<TacticalActor>>();
        private static NewObjectiveUI _newObjectiveWidget = null;

        public static void AddAdditionalSecondaryObjective(FactionObjectiveDef objectiveDef)
        {
            try
            {
                _secondaryObjectiveDefsInPlay.Add(objectiveDef);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        public class NewObjectiveUI : MonoBehaviour
        {
            public Sprite IconSprite;

            public GameObject uiObject;
            private float fadeDuration = 1f; // Duration for fade in/out
            private float displayDuration = 6f; // Duration to display at full opacity
            public CanvasGroup CanvasGroup;
            private List<string> descriptions;

            private float timer = 0f;
            private enum FadeState { None, FadingIn, Displaying, FadingOut, FadingInText }
            private FadeState fadeState = FadeState.None;
            private Text _description;
            Color greenColor = new Color(0.4f, 0.729f, 0.416f);

            public void ShowNewObjective(string title, List<string> descriptions, Sprite icon, bool isRevenant = false, bool victoryObjective = false, float displayDuration = 3f)
            {
                try
                {
                    this.descriptions = descriptions;
                    this.displayDuration = displayDuration;
                    currentDescriptionIndex = 0; // Reset index for each new objective
                    descriptionDisplayTime = displayDuration / descriptions.Count; // Time per description

                    CreateUIElement(title, descriptions[currentDescriptionIndex], icon, victoryObjective, isRevenant);
                    StartFadeIn();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private int currentDescriptionIndex = 0;
            private float descriptionDisplayTime;


            private void CreateUIElement(string title, string initialDescription, Sprite classIcon, bool victoryObjective = false, bool isRevenant = false)
            {
                try
                {
                    UIModuleNavigation uIModuleNavigation = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.NavigationModule;

                    uiObject = new GameObject("NewObjectiveUI", typeof(RectTransform));
                    RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
                    rectTransform.SetParent(uIModuleNavigation.transform, false);
                    rectTransform.sizeDelta = new Vector2(900, 140);
                    rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.position = new Vector3(Screen.width / 2, Screen.height * 4 / 5, 0);

                    CanvasGroup = uiObject.AddComponent<CanvasGroup>();
                    CanvasGroup.alpha = 0;

                    // _newObjectiveWidget = uiObject;

                    GameObject background = new GameObject("Background", typeof(RectTransform));
                    background.transform.SetParent(uiObject.transform);
                    Image bgImage = background.AddComponent<Image>();
                    background.GetComponent<RectTransform>().sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y - 40);
                    bgImage.sprite = Helper.CreateSpriteFromImageFile("pp_obj_update_background.png");
                    bgImage.color = new Color(0, 0, 0, 0.75f); // Adjusted alpha for background only
                    RectTransform bgRect = background.GetComponent<RectTransform>();
                    bgRect.anchorMin = new Vector2(0, 0);
                    bgRect.anchorMax = new Vector2(1, 1);
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;


                    // Top Border Line with Icon in the Middle
                    GameObject topLine = new GameObject("TopLine");
                    topLine.transform.SetParent(uiObject.transform);
                    RectTransform lineRect = topLine.AddComponent<RectTransform>();
                    lineRect.sizeDelta = new Vector2(500, 1);
                    lineRect.anchorMin = new Vector2(0.5f, 1);
                    lineRect.anchorMax = new Vector2(0.5f, 1);
                    lineRect.pivot = new Vector2(0.5f, 1);
                    lineRect.anchoredPosition = new Vector2(0, 65);

                    Image lineImage = topLine.AddComponent<Image>();
                    lineImage.sprite = Helper.CreateSpriteFromImageFile("pp_obj_update_line.png");

                    if (victoryObjective)
                    {
                        lineImage.color = greenColor;
                    }


                    // Icon on the Top Border
                    GameObject icon = new GameObject("Icon");
                    icon.transform.SetParent(uiObject.transform);
                    Image iconImage = icon.AddComponent<Image>();
                    iconImage.sprite = IconSprite;
                    RectTransform iconRect = icon.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(60, 60);
                    iconRect.anchorMin = new Vector2(0.5f, 1);
                    iconRect.anchorMax = new Vector2(0.5f, 1);
                    iconRect.pivot = new Vector2(0.5f, 1);
                    iconRect.anchoredPosition = new Vector2(0, 130); // Moved higher

                    AddOutlineToIcon addOutlineToObjectiveIcon = icon.GetComponent<AddOutlineToIcon>() ?? icon.AddComponent<AddOutlineToIcon>();
                    addOutlineToObjectiveIcon.icon = icon;
                    addOutlineToObjectiveIcon.InitOrUpdate();

                    // Title Text
                    GameObject titleTextObj = new GameObject("TitleText");
                    titleTextObj.transform.SetParent(uiObject.transform);
                    Text titleText = titleTextObj.AddComponent<Text>();
                    titleText.text = title;
                    titleText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    titleText.fontSize = 80; // Doubled font size
                    titleText.color = Color.gray;
                    titleText.alignment = TextAnchor.MiddleCenter;
                    RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
                    titleRect.sizeDelta = new Vector2(1500, 160);
                    titleRect.anchorMin = new Vector2(0.5f, 0.8f);
                    titleRect.anchorMax = new Vector2(0.5f, 0.8f);
                    titleRect.pivot = new Vector2(0.5f, 1);
                    titleRect.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Scaling down to improve text clarity
                    titleRect.anchoredPosition = new Vector2(0, 60);


                    // Description Text
                    GameObject descriptionTextObj = new GameObject("DescriptionText");
                    descriptionTextObj.transform.SetParent(uiObject.transform);
                    Text descriptionText = descriptionTextObj.AddComponent<Text>();
                    descriptionText.text = initialDescription;
                    descriptionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                    descriptionText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    descriptionText.fontSize = 120; // Doubled font size
                    descriptionText.color = WhiteColor;
                    descriptionText.alignment = TextAnchor.MiddleCenter;
                    RectTransform descRect = descriptionTextObj.GetComponent<RectTransform>();
                    descRect.sizeDelta = new Vector2(3000, 240);
                    descRect.anchorMin = new Vector2(0.5f, 0.6f);
                    descRect.anchorMax = new Vector2(0.5f, 0.6f);
                    descRect.pivot = new Vector2(0.5f, 1);
                    descRect.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Scaling down to improve text clarity
                    descRect.anchoredPosition = new Vector2(0, 40);


                    // Class Icon below
                    GameObject targetIcon = new GameObject("TargetIcon");
                    targetIcon.transform.SetParent(uiObject.transform);
                    Image targetIconImage = targetIcon.AddComponent<Image>();
                    targetIconImage.sprite = classIcon;
                    targetIconImage.color = NegativeColor;

                    if (isRevenant)
                    {
                        targetIconImage.color = LeaderColor;
                    }

                    RectTransform targetIconRect = targetIcon.GetComponent<RectTransform>();
                    targetIconRect.sizeDelta = new Vector2(120, 120);
                    targetIconRect.anchorMin = new Vector2(0.5f, 1);
                    targetIconRect.anchorMax = new Vector2(0.5f, 1);
                    targetIconRect.pivot = new Vector2(0.5f, 1);
                    targetIconRect.anchoredPosition = new Vector2(0, -500); // Moved higher

                    AddOutlineToIcon addOutlineToIcon = targetIcon.GetComponent<AddOutlineToIcon>() ?? targetIcon.AddComponent<AddOutlineToIcon>();
                    addOutlineToIcon.icon = targetIcon;
                    addOutlineToIcon.InitOrUpdate();

                    if (victoryObjective)
                    {
                        descriptionText.color = greenColor;
                    }

                    // Set display time for each description segment
                    //  descriptionDisplayTime = displayDuration / descriptions.Length;


                    _description = descriptionText;


                    AdjustAlphaElements(0);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private void Update()
            {
                try
                {
                    if (fadeState == FadeState.FadingIn)
                    {
                        timer += Time.deltaTime;
                        AdjustAlphaElements(Mathf.Lerp(0, 1, timer / fadeDuration));

                        if (timer >= fadeDuration)
                        {
                            timer = 0f;
                            fadeState = FadeState.Displaying;
                        }
                    }
                    else if (fadeState == FadeState.Displaying)
                    {
                        timer += Time.deltaTime;

                        if (timer >= descriptionDisplayTime)
                        {
                            timer = 0f;
                            fadeState = FadeState.FadingOut;
                        }
                    }
                    else if (fadeState == FadeState.FadingOut)
                    {
                        timer += Time.deltaTime;
                        AdjustAlphaElements(Mathf.Lerp(1, 0, timer / fadeDuration));

                        if (timer >= fadeDuration)
                        {
                            currentDescriptionIndex++;
                            if (currentDescriptionIndex >= descriptions.Count)
                            {
                                fadeState = FadeState.None;
                                AdjustAlphaElements(0); // Ensure alpha is fully 0 before destroying
                                DestroyUIObject();
                            }
                            else
                            {
                                _description.text = descriptions[currentDescriptionIndex];
                                StartFadeIn(); // Restart fade-in for the next description
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


            private void AdjustAlphaElements(float alpha)
            {
                try
                {
                    if (CanvasGroup != null)
                    {
                        CanvasGroup.alpha = alpha;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private void DestroyUIObject()
            {
                if (uiObject != null)
                {
                    Destroy(uiObject);
                    uiObject = null;
                }
            }

            private void StartFadeIn()
            {
                try
                {
                    timer = 0.0f;
                    fadeState = FadeState.FadingIn;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static List<TacticalActor> GetTargetsForObjective(FactionObjective objective, bool onlyAlive = true, bool onlyRevealed = true)
        {
            try
            {
                GameTagDef relevantTag = null;

                TacticalLevelController controller = objective.Level;

                /*  if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                  {
                      return null;
                  }*/

                if (objective is KillActorFactionObjective killObjective)
                {
                    FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                    relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                }
                else if (objective is ActorHasStatusFactionObjective captureObjective)
                {
                    FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);
                    relevantTag = (GameTagDef)fieldInfo.GetValue(captureObjective);
                }

                //   TacticalFaction aliens = controller.GetFactionByCommandName("aln");
                TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                List<TacticalActor> enemyActors = controller.Factions
                                     .Where(f => f.GetRelationTo(phoenix) == FactionRelation.Enemy)
                                     .SelectMany(f => f.TacticalActors)
                                     .ToList();




                List<TacticalActor> relevantActors = enemyActors.
                    Where(ta =>
                    (ta.HasGameTag(relevantTag) || ta.BodyState.GetAllBodyparts().Any(b => b.OwnerItem.TacticalItemDef.Tags.Contains(relevantTag)) || ta.Equipments.GetWeapons().Any(w => w.GameTags.Contains(relevantTag)))).ToList().
                    Concat(phoenix.TacticalActors.Where(ta =>
                    (ta.HasGameTag(relevantTag) || ta.Equipments.GetWeapons().Any(w => w.GameTags.Contains(relevantTag))) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())).ToList();

                if (onlyAlive)
                {
                    relevantActors = relevantActors.Where(ta => ta.IsAlive).ToList();
                }

                if (onlyRevealed)
                {
                    relevantActors = relevantActors.Where(ta => phoenix.Vision.IsRevealed(ta)).ToList();
                }

                //  TFTVLogger.Always($"objective {objective.Description.Localize()} relevantActors count: {relevantActors.Count}");

                return relevantActors;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static void AddChaseTargetOnClick(GameObject parent, FactionObjective factionObjective)
        {
            try
            {

                if (!parent.GetComponent<EventTrigger>())
                {
                    parent.AddComponent<EventTrigger>();
                }

                EventTrigger eventTrigger = parent.GetComponent<EventTrigger>();
                eventTrigger.triggers.Clear();
                EventTrigger.Entry click = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };

                click.callback.AddListener((eventData) =>
                {
                    TacticalActor target = _objectivesTargetsDictionary[factionObjective].FirstOrDefault();

                    target.TacticalActorView.DoCameraChase();
                    if (_objectivesTargetsDictionary[factionObjective].Count > 0)
                    {
                        _objectivesTargetsDictionary[factionObjective].Remove(target);
                        _objectivesTargetsDictionary[factionObjective].Add(target);
                    }

                });

                eventTrigger.triggers.Add(click);

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        private static UIHider _uIHider = null;
        private static bool _objectiveVictoryAnnouncementRunning = false;

        public static void CreateSecondaryObjectiveAnnouncement(List<string> descriptions, Sprite icon, bool isRevenant = false, bool victoryObjective = false)
        {
            try
            {
                //  if (_newObjectiveWidget == null)
                //   {
                TFTVLogger.Always($"Running objective announcement: {descriptions.First()}, _objectiveVictoryAnnouncementRunning? {_objectiveVictoryAnnouncementRunning}");

                string title = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_NEW_SECONDARY_OBJECTIVE");

                if (victoryObjective)
                {
                    title = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_OBJECTIVE_ACCOMPLISHED");
                }

                if (!_objectiveVictoryAnnouncementRunning)
                {
                    GameObject gameObject = new GameObject();
                    NewObjectiveUI newObjectiveUI = gameObject.AddComponent<NewObjectiveUI>();
                    newObjectiveUI.IconSprite = Helper.CreateSpriteFromImageFile("objective.png");
                    newObjectiveUI.ShowNewObjective(title, descriptions, icon, isRevenant, victoryObjective);
                    _newObjectiveWidget = newObjectiveUI;
                    //  TacticalUIOverlayController.ToggleUI();

                    _uIHider = new GameObject("UIHider").AddComponent<UIHider>();
                    _uIHider.HideUI();
                    ExecuteAfterDelay(2f, RestoreUI);
                }
                if (victoryObjective)
                {
                    _objectiveVictoryAnnouncementRunning = true;
                }

                //ExecuteAfterDelay(10f, );
                //  }
                /* else
                 {
                     _objectiveDescription = description.ToUpper();
                     ExecuteAfterDelay(2f, ChangeDescription);
                 }*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void AdjustSecondaryObjective(FactionObjective objective, ObjectiveElement objectiveElement)
        {
            try
            {

                if (!IsSecondaryObjective(objective))
                {
                    return;
                }


                List<TacticalActor> targets = GetTargetsForObjective(objective);

                if (targets.Count == 0)
                {
                    return;
                }

                AdjustSecondaryObjectiveTextRevenant(objective, objectiveElement);

                if (_objectivesTargetsDictionary.ContainsKey(objective))
                {
                    if (objective is ActorHasStatusFactionObjective)
                    {
                        _objectivesTargetsDictionary[objective] = targets;
                    }
                }
                else
                {
                    _objectivesTargetsDictionary.Add(objective, targets);
                }

                AddChaseTargetOnClick(objectiveElement.Description.gameObject, objective);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static string GetTextForRevenantObjective(FactionObjective objective)
        {
            try
            {

                string input = objective.GetDescription();
                string result = "";
                if (TFTVRevenant.TFTVRevenantResearch.ProjectOsiris)
                {
                    if (input.Contains("."))
                    {
                        result = input.Split('.')[0];

                    }
                }
                else
                {
                    int revenantPoints = TFTVRevenant.TFTVRevenantResearch.PreviousRevenantPoints + TFTVRevenant.TFTVRevenantResearch.RevenantPoints;
                    result = input + $" {revenantPoints}/{TFTVRevenant.TFTVRevenantResearch.RequieredRevenantPoints}";
                }

                return result;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }


        public static void AdjustSecondaryObjectiveTextRevenant(FactionObjective objective, ObjectiveElement objectiveElement)
        {
            try
            {

                if (IsKillOrCaptureRevenantObjective(objective))
                {
                    objectiveElement.Description.text = GetTextForRevenantObjective(objective);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        public class UIHider : MonoBehaviour
        {
            private CanvasGroup mainCanvasGroup = _secondaryObjectives.GetComponentInParent<CanvasGroup>();            // Main CanvasGroup containing the UI

            // Dictionary to store original alpha values
            private Dictionary<Graphic, float> originalAlphas = new Dictionary<Graphic, float>();

            public void HideUI()
            {
                try
                {
                    if (mainCanvasGroup == null)
                    {
                        TFTVLogger.Always("Cannot hide UI. Main CanvasGroup is missing.");
                        return;
                    }

                    // Ensure the main CanvasGroup alpha is set to 1 to keep the UI interactable
                    mainCanvasGroup.alpha = 1f;


                    UIModuleSquadManagement uIModuleSquadManagement = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.SquadManagementModule;

                    uIModuleSquadManagement.WillpointsBar.enabled = false;

                    foreach (UIColorController uIColorController in uIModuleSquadManagement.GetComponentsInChildren<UIColorController>(true))
                    {
                        uIColorController.Enabled = false;
                    }

                    // Loop through all Image, Text, and RawImage components in the main CanvasGroup
                    foreach (Graphic graphic in mainCanvasGroup.GetComponentsInChildren<Graphic>(true))
                    {
                        //  TFTVLogger.Always($"{graphic.name}");

                        if (_newObjectiveWidget.CanvasGroup.GetComponentsInChildren<Graphic>(true).Contains(graphic))
                        {
                            continue;
                        }

                        // Store the original alpha if not already stored
                        if (!originalAlphas.ContainsKey(graphic))
                        {
                            originalAlphas[graphic] = graphic.color.a;
                        }

                        // Set alpha to 0 to make invisible
                        Color color = graphic.color;
                        color.a = 0f;
                        graphic.color = color;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private IEnumerator FadeInGraphic(Graphic graphic, float targetAlpha, float duration)
            {
                if (graphic == null) yield break;

                Color color = graphic.color;
                float startAlpha = color.a;
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                    color.a = newAlpha;
                    graphic.color = color;
                    yield return null; // Wait for the next frame
                }

                // Ensure the final alpha is set
                color.a = targetAlpha;
                graphic.color = color;
            }

            public void RestoreUIVisibility(float fadeDuration = 0.5f)
            {
                try
                {
                    if (originalAlphas == null || originalAlphas.Count == 0)
                    {
                        return;
                    }

                    foreach (var kvp in originalAlphas)
                    {

                        Graphic graphic = kvp.Key;
                        float originalAlpha = kvp.Value;

                        if (graphic != null && _uIHider != null)
                        {
                            // Start a coroutine to fade in this graphic

                            _uIHider.StartCoroutine(FadeInGraphic(graphic, originalAlpha, fadeDuration));

                        }
                    }

                    // Clear the dictionary after restoring
                    originalAlphas.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }

        public class CoroutineHelper : MonoBehaviour { }

        public static void ExecuteAfterDelay(float delay, Action callback)
        {
            var helper = new GameObject("CoroutineHelper").AddComponent<CoroutineHelper>();
            helper.StartCoroutine(ExecuteAfterDelayCoroutine(delay, callback, helper));
        }

        private static IEnumerator ExecuteAfterDelayCoroutine(float delay, Action callback, CoroutineHelper helper)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
            GameObject.Destroy(helper.gameObject); // Clean up helper
        }



        public static void RestoreUI()
        {
            try
            {
                TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                tacticalLevelController.View.SelectedActor?.TacticalActorView?.DoCameraChase();
                UIModuleObjectives uIModuleObjectives = tacticalLevelController.View.TacticalModules.ObjectivesModule;
                //  TFTVLogger.Always($"_uIHider null? {_uIHider == null}");

                _uIHider?.RestoreUIVisibility();
                _objectiveVictoryAnnouncementRunning = false;
                InitObjectivesTFTV(uIModuleObjectives);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }// Code to execute after 3 seconds
        }

        private static bool CheckCapacityForCaptureObjective(GeoPhoenixFaction phoenixFaction, int spaceRequired)
        {
            try
            {
                int captureCapacity = AircraftCaptureCapacity;
                int availableContainmentSpace = phoenixFaction.ContaimentCapacity - phoenixFaction.ContaimentUsage;
                bool limitedCaptureSetting = TFTVNewGameOptions.LimitedCaptureSetting;

                if (limitedCaptureSetting && Mathf.Min(captureCapacity, availableContainmentSpace) > spaceRequired || !limitedCaptureSetting && availableContainmentSpace > spaceRequired)
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

        public static void PopulateAvailableObjectives(GeoLevelController controller)
        {
            try
            {
                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;



                if (TFTVRevenant.DeadSoldiersDelirium.Count > 0 && controller.EventSystem.GetVariable(TFTVRevenant.TFTVRevenantResearch.RevenantCapturedVariable) == 0
                    && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureAnyRevenant.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureAnyRevenant.Guid);
                        //  TFTVLogger.Always($"_captureAnyRevenant available");
                    }
                }

                if (phoenixFaction.Research.HasCompleted("PX_Aircraft_EscapePods_ResearchDef") && phoenixFaction.Research.GetResearchById("PX_Alien_LiveQueen_ResearchDef").IsRevealed
                    && CheckCapacityForCaptureObjective(phoenixFaction, 8))
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureScylla.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureScylla.Guid);
                        //  TFTVLogger.Always($"_captureScylla available");
                    }
                }

                if (phoenixFaction.Research.GetResearchById("PX_Alien_LiveSiren_ResearchDef").IsRevealed)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureSiren.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 3))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureSiren.Guid);
                        //  TFTVLogger.Always($"_captureSiren available");
                    }
                }

                if (phoenixFaction.Research.GetResearchById("PX_GooRepeller_ResearchDef").IsRevealed)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureGooAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 4))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureGooAlien.Guid);
                        // TFTVLogger.Always($"_captureGooAlien available");
                    }
                }

                if (phoenixFaction.Research.GetResearchById("PX_AlienVirusInfection_ResearchDef").IsRevealed)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureViralAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureViralAlien.Guid);
                        TFTVLogger.Always($"_captureViralAlien available, Aircraft capture capacity is {AircraftCaptureCapacity}");
                    }
                }

                if (phoenixFaction.Research.GetResearchById(TFTVChangesToDLC4Events.MistResearch.Id).IsRevealed)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_captureMistAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_captureMistAlien.Guid);
                        TFTVLogger.Always($"_captureMistAlien available, Aircraft capture capacity is {AircraftCaptureCapacity}");
                    }
                }


                if (phoenixFaction.Research.GetResearchById("PX_PyschicAttack_ResearchDef").IsRevealed && CheckCapacityForCaptureObjective(phoenixFaction, 3))
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_capturePsychicAlien.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_capturePsychicAlien.Guid);
                        // TFTVLogger.Always($"_captureViralAlien available");
                    }
                }


                if (phoenixFaction.Research.GetResearchById("PX_Alien_Queen_ResearchDef").IsHidden)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_killScylla.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_killScylla.Guid);
                        // TFTVLogger.Always($"_killScylla available");
                    }
                }

                if (controller.EventSystem.GetVariable(TFTVRevenant.TFTVRevenantResearch.RevenantCapturedVariable) > 0)
                {
                    if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantMimic.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_killRevenantMimic.Guid);
                        //TFTVLogger.Always($"_killMimic available");
                    }

                    if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantDybbuk.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_killRevenantDybbuk.Guid);
                        // TFTVLogger.Always($"_killDybbuk available");
                    }

                    if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantNemesis.Guid))
                    {
                        AvailableSecondaryObjectivesTactical.Add(_killRevenantNemesis.Guid);
                        //TFTVLogger.Always($"_killNemesis available");
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void AddAllAvailableSecondaryObjectivesToMission(TacMissionTypeDef missionType)
        {
            try
            {
                if (!missionType.FinalMission && !missionType.name.Contains("Tutorial") && missionType.ParticipantsData.Any(pd => pd.FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef")
                    && AvailableSecondaryObjectivesTactical != null && AvailableSecondaryObjectivesTactical.Count > 0))
                {
                    //  TFTVLogger.Always($"Add {AvailableSecondaryObjectivesTactical.Count} SecondaryObjectives");

                    List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                    listOfFactionObjectives.RemoveAll(obj => _secondaryObjectiveDefsALL.Contains(obj));

                    missionType.CustomObjectives = listOfFactionObjectives.ToArray();

                    foreach (string factionObjectiveDefGUID in AvailableSecondaryObjectivesTactical)
                    {
                        FactionObjectiveDef factionObjectiveDef = (FactionObjectiveDef)Repo.GetDef(factionObjectiveDefGUID);
                        //     TFTVLogger.Always($"Considering Adding Secondary Objective {factionObjectiveDef.name}");

                        if (!missionType.CustomObjectives.Contains(factionObjectiveDef))
                        {
                            TFTVLogger.Always($"Adding Secondary Objective {factionObjectiveDef.name}");
                            listOfFactionObjectives.Add(factionObjectiveDef);
                            if (!_secondaryObjectiveDefsInPlay.Contains(factionObjectiveDef))
                            {
                                _secondaryObjectiveDefsInPlay.Add(factionObjectiveDef);
                            }
                        }
                    }



                    missionType.CustomObjectives = listOfFactionObjectives.ToArray();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal class Defs
        {

            public static void CreateDefs()
            {
                try
                {
                    CreateCaptureRevenantObjective();
                    CreateCaptureGooAlienObjective();
                    CreateCaptureScyllaObjective();
                    CreateCaptureSirenObjective();
                    CreateCapturePsychicAlienObjective();
                    CreateCaptureViralAlienObjective();
                    CreateCaptureMistPandoranObjective();

                    CreateKillScyllaObjective();
                    CreateKillOrCaptureMimic();
                    CreateKillOrCaptureDybbuk();
                    CreateKillOrCaptureNemesis();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureMistPandoranObjective()
            {
                try
                {
                    string name = "CaptureMistAlien";
                    string guid = "{E90CF725-1C02-4D48-B2A0-766266FDC132}";
                    GameTagDef tag = TFTVChangesToDLC4Events.MistTag;
                    string descLocKey = "TFTV_KEY_CAPTURE_MIST_OBJECTIVE";
                    int expReward = 300;

                    _captureMistAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void CreateKillScyllaObjective()
            {
                try
                {

                    string name = "KilLScylla";
                    string guid = "{CB92D1A0-0D3F-49D1-BA26-BE1AE7EA1F03}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("Queen_ClassTagDef");
                    string descLocKey = "TFTV_KEY_KILL_SCYLLA_OBJECTIVE";
                    int expReward = 500;

                    _killScylla = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateKillOrCaptureMimic()
            {
                try
                {
                    string name = "KillMimic";
                    string guid = "{32568945-79C5-41A4-8168-90D1C238BAFE}";

                    GameTagDef tag = TFTVRevenant.RevenantTier1GameTag;
                    string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                    string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                    int expReward = 200;

                    _killRevenantMimic = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateKillOrCaptureDybbuk()
            {
                try
                {
                    string name = "KillDybbuk";
                    string guid = "{F2D70D32-D15C-422E-A1EA-E8ED9A8B5334}";

                    GameTagDef tag = TFTVRevenant.RevenantTier2GameTag;
                    string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                    string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                    int expReward = 400;

                    _killRevenantDybbuk = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateKillOrCaptureNemesis()
            {
                try
                {
                    string name = "KillNemesis";
                    string guid = "{EF91C6AB-82F1-48E0-868C-421E036A376A}";

                    GameTagDef tag = TFTVRevenant.RevenantTier3GameTag;
                    string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                    string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                    int expReward = 600;

                    _killRevenantNemesis = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureRevenantObjective()
            {
                try
                {
                    string name = "CaptureRevenant";
                    string guid = "{1C220EB6-F02C-467F-8565-B85130B2A641}";

                    GameTagDef tag = TFTVRevenant.AnyRevenantGameTag;
                    string descLocKey = "TFTV_KEY_CAPTURE_REVENANT_OBJECTIVE";
                    int expReward = 300;

                    _captureAnyRevenant = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureScyllaObjective()
            {
                try
                {
                    string name = "CaptureScylla";
                    string guid = "{85DF80E8-DEB0-4719-86CA-86B1BD5B215A}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("Queen_ClassTagDef");
                    string descLocKey = "TFTV_KEY_CAPTURE_SCYLLA_OBJECTIVE";
                    int expReward = 300;

                    _captureScylla = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureSirenObjective()
            {
                try
                {
                    string name = "CaptureSiren";
                    string guid = "{7FA442A3-EC6E-4272-A4A3-F12457176028}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("Siren_ClassTagDef");
                    string descLocKey = "TFTV_KEY_CAPTURE_SIREN_OBJECTIVE";
                    int expReward = 300;

                    _captureSiren = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCapturePsychicAlienObjective()
            {
                try
                {
                    string name = "CapturePsychicAlien";
                    string guid = "{88732F4A-D028-4A79-B5BF-DB414368DB26}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("PsychicBodypart_TagDef");
                    string descLocKey = "TFTV_KEY_CAPTURE_PSYCHIC_OBJECTIVE";
                    int expReward = 300;

                    _capturePsychicAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureGooAlienObjective()
            {
                try
                {
                    string name = "CaptureGooAlien";
                    string guid = "{E1B7F1FE-F431-469B-B164-414872758FDA}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("GooBodypart_TagDef");
                    string descLocKey = "TFTV_KEY_CAPTURE_GOO_OBJECTIVE";
                    int expReward = 300;

                    _captureGooAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureViralAlienObjective()
            {
                try
                {
                    string name = "CaptureViralAlien";
                    string guid = "{780CF0CB-6C7F-43DE-BF54-B421EC039243}";
                    GameTagDef tag = DefCache.GetDef<GameTagDef>("ViralBodypart_TagDef");
                    string descLocKey = "TFTV_KEY_CAPTURE_VIRUS_OBJECTIVE";
                    int expReward = 300;

                    _captureViralAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static ActorHasStatusFactionObjectiveDef CreateSecondaryObjectiveCapture(string name, string guid, GameTagDef targetTag, string descLocKey, int expReward)
            {
                try
                {
                    ActorHasStatusFactionObjectiveDef source =
                        (ActorHasStatusFactionObjectiveDef)Repo.GetDef("2f3ea3b1-49b1-7cbe-ef82-07a75d820259"); //E_CaptureAcheron [StoryCH1_CustomMissionTypeDef]
                    ActorHasStatusFactionObjectiveDef newObjective = Helper.CreateDefFromClone(source, guid, name);
                    newObjective.TargetGameTag = targetTag;
                    newObjective.IsVictoryObjective = false;
                    newObjective.CanRegress = true;
                    newObjective.IsDefeatObjective = false;
                    newObjective.IsUiHidden = true;
                    newObjective.IsUiSummaryHidden = true;
                    newObjective.MissionObjectiveData.Description.LocalizationKey = descLocKey;
                    newObjective.MissionObjectiveData.Summary.LocalizationKey = descLocKey;
                    newObjective.MissionObjectiveData.ExperienceReward = expReward;

                    _secondaryObjectiveDefsALL.Add(newObjective);

                    return newObjective;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static KillActorFactionObjectiveDef CreateSecondaryObjectiveKill(string name, string guid, GameTagDef targetTag, string descLocKey, int expReward, bool paralyzedCounts, string summaryKey = null)
            {
                try
                {
                    KillActorFactionObjectiveDef source =
                        (KillActorFactionObjectiveDef)Repo.GetDef("bd7a9d69-fbdf-3424-db3e-6566c3595d47"); //KillChiron_CustomMissionObjective]
                    KillActorFactionObjectiveDef newObjective = Helper.CreateDefFromClone(source, guid, name);
                    newObjective.KillTargetGameTag = targetTag;
                    newObjective.IsVictoryObjective = false;
                    newObjective.IsDefeatObjective = false;
                    newObjective.IsUiHidden = false;
                    newObjective.IsUiSummaryHidden = true;
                    newObjective.MissionObjectiveData.Description.LocalizationKey = descLocKey;
                    newObjective.MissionObjectiveData.Summary.LocalizationKey = summaryKey ?? descLocKey;
                    newObjective.MissionObjectiveData.ExperienceReward = expReward;
                    newObjective.ParalysedCounts = paralyzedCounts;

                    _secondaryObjectiveDefsALL.Add(newObjective);

                    return newObjective;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }


        public static bool IsKillOrCaptureRevenantObjective(FactionObjective objective)
        {
            try
            {
                if (objective.Description.LocalizationKey == _killRevenantMimic.MissionObjectiveData.Description.LocalizationKey)
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

        public static void ClearDataOnGameLoadAndStateChange()
        {
            try
            {
                _secondaryObjectivesTitle = null;
                _secondaryObjectives = null;
                AvailableSecondaryObjectivesTactical?.Clear();
                _secondaryObjectiveDefsInPlay?.Clear();
                _objectivesTargetsDictionary?.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static bool IsSecondaryObjective(FactionObjective objective)
        {
            try
            {
                if (_secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == objective.Description.LocalizationKey))
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

        private static bool IsNeverRelevantObjective(FactionObjective objective)
        {
            try
            {
                GameTagDef relevantTag = null;

                if (objective == null || objective.Faction == null)
                {
                    return false;
                }

                TacticalLevelController controller = objective.Level;

                if (objective is KillActorFactionObjective killObjective)
                {
                    TacticalFaction pxFaction = controller.GetFactionByCommandName("px");

                    FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (fieldInfo != null)
                    {
                        relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                    }

                    //    if (relevantTag != null && !controller.GetFactionByCommandName("aln").TacticalActors.Any(ta => ta.HasGameTag(relevantTag))
                    //        && !controller.GetFactionByCommandName("px").TacticalActors.Any(ta => ta.HasGameTag(relevantTag) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>()))

                    if (relevantTag != null && !controller.Factions.Any(f => f.GetRelationTo(pxFaction) == FactionRelation.Enemy && f.TacticalActors.Any(ta => ta.HasGameTag(relevantTag))
                       && !pxFaction.TacticalActors.Any(ta => ta.HasGameTag(relevantTag) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                    {
                        TFTVLogger.Always($"Not adding {objective.Description.Localize()} because it's never relevant");
                        objective.IsUiSummaryHidden = true;

                        PropertyInfo propertyInfo = typeof(FactionObjective).GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
                        propertyInfo.SetValue(objective, FactionObjectiveState.InProgress);

                        return true;
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

        private static Dictionary<TacticalActor, List<FactionObjective>> _pendingObjectivesTargets = new Dictionary<TacticalActor, List<FactionObjective>>();

        private static void RunPendingObjectives()
        {
            try
            {
                if (_pendingObjectivesTargets.Count > 0)
                {
                    _pendingObjectivesTargets.Keys.First().StartCoroutine(RunPendingObjectivesCoroutine());
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        // private static UIActorElement _targetUIActorElement = null;

        private static IEnumerator RunPendingObjectivesCoroutine()
        {
            for (int x = 0; x < _pendingObjectivesTargets.Keys.Count; x++)
            {
                // Wait for x * 3f seconds
                yield return new WaitForSeconds(x * 5f);

                TacticalActor target = _pendingObjectivesTargets.Keys.ElementAt(x);
                target.TacticalActorView.DoCameraChase(true);

                /*  _targetUIActorElement = target.TacticalActorViewBase.UIActorElement;

                  //  _targetUIActorElement.SetVisible(true);
                  _targetUIActorElement.GetComponent<HealthbarUIActorElement>().HealthBar.gameObject.SetActive(false);
                  _targetUIActorElement.GetComponent<HealthbarUIActorElement>().ArmorBar.gameObject.SetActive(false);
                  _targetUIActorElement.SetHighlighted(true);*/


                List<FactionObjective> factionObjectives = _pendingObjectivesTargets[target];

                List<string> description = new List<string>();

                bool isRevenant = false;

                foreach (FactionObjective factionObjective in factionObjectives)
                {
                    string descriptionToAdd = "";

                    if (factionObjective.Description.LocalizationKey == _captureAnyRevenant.MissionObjectiveData.Description.LocalizationKey)
                    {
                        isRevenant = true;
                    }

                    if (IsKillOrCaptureRevenantObjective(factionObjective))
                    {
                        descriptionToAdd = GetTextForRevenantObjective(factionObjective);
                        isRevenant = true;
                    }
                    else
                    {
                        descriptionToAdd = factionObjective.GetDescription();
                    }

                    description.Add(descriptionToAdd);

                    // TFTVLogger.Always($"RunPendingObjectivesCoroutine: {factionObjective.GetDescription()}, {description.Count()}");
                }

                _pendingObjectivesTargets.Clear();

                Sprite icon = target.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement.MainClassIcon.sprite;



                //  TFTVLogger.Always($"target.ViewElementDef.name: {target.ViewElementDef.name}", false);
                //target.ViewElementDef.SmallIcon
                CreateSecondaryObjectiveAnnouncement(description, icon, isRevenant);
            }
        }

        private static bool CheckTargetIsNotToCaptureUncapturableScylla(TacticalActor tacticalActor, FactionObjective objective)
        {
            try
            {
                ClassTagDef scyllaTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

                return !TFTVNewGameOptions.LimitedCaptureSetting
                    || !tacticalActor.HasGameTag(scyllaTag)
                    || !_secondaryObjectiveDefsInPlay.Contains(_killScylla)
                    || objective.Description.LocalizationKey == _killScylla.MissionObjectiveData.Description.LocalizationKey;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static bool IsRelevantObjective(FactionObjective objective)
        {
            try
            {
                GameTagDef relevantTag = null;

                TacticalLevelController controller = objective.Level;

                if (objective is KillActorFactionObjective killObjective)
                {
                    FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                    relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                }
                else if (objective is ActorHasStatusFactionObjective captureObjective)
                {
                    FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);
                    relevantTag = (GameTagDef)fieldInfo.GetValue(captureObjective);
                }

                //  TacticalFaction aliens = controller.GetFactionByCommandName("aln");
                TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                /* if (relevantTag != null)
                 {
                     TFTVLogger.Always($"relevantTag: {relevantTag.name}");

                     foreach (TacticalActor actor in controller.GetFactionByCommandName("aln").TacticalActors)
                     {
                         TFTVLogger.Always($"Alien: {actor.name}");

                         foreach (BodyPartAspect bpa in actor.BodyState.GetAllBodyparts())
                         {
                             TFTVLogger.Always($"bpa: {bpa?.OwnerItem?.TacticalItemDef?.name}");

                             foreach (GameTagDef gameTagDef in bpa.OwnerItem.TacticalItemDef.Tags)
                             {
                                 TFTVLogger.Always($"Tag: {gameTagDef.name}");
                             }
                         }
                     }
                 }*/
                List<TacticalActor> enemyActors = controller.Factions
                                    .Where(f => f.GetRelationTo(phoenix) == FactionRelation.Enemy)
                                    .SelectMany(f => f.TacticalActors)
                                    .ToList();


                List<TacticalActor> relevantActors = enemyActors.
                    Where(ta =>
                    ta.IsAlive && phoenix.Vision.IsRevealed(ta)
                    && TacticalActorHasTagAnywhere(ta, relevantTag)
                    && CheckTargetIsNotToCaptureUncapturableScylla(ta, objective)
                    ).ToList().
                    Concat(phoenix.TacticalActors.Where(ta =>
                    ta.IsAlive
                    && TacticalActorHasTagAnywhere(ta, relevantTag)
                    && CheckTargetIsNotToCaptureUncapturableScylla(ta, objective)
                    && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())).ToList();


                List<TacticalActor> notCapturableActors = new List<TacticalActor>();

                if (objective is ActorHasStatusFactionObjective && TFTVNewGameOptions.LimitedCaptureSetting)
                {

                    //   TFTVLogger.Always($"AircraftCaptureCapacity, ContainmentSpaceAvailable {AircraftCaptureCapacity} {ContainmentSpaceAvailable}");

                    int availableSpace = Math.Min(AircraftCaptureCapacity, ContainmentSpaceAvailable);

                    foreach (TacticalActor tacticalActor in relevantActors)
                    {
                        int requiredSlots = CalculateCaptureSlotCost(tacticalActor.GameTags.ToList());
                        //     TFTVLogger.Always($"required slots is {requiredSlots} for {tacticalActor.name}");
                        if (requiredSlots > availableSpace)
                        {
                            notCapturableActors.Add(tacticalActor);
                        }
                    }
                }

                relevantActors.RemoveRange(notCapturableActors);

                if (relevantTag != null && relevantActors.Count > 0)
                {
                    if (objective.IsUiSummaryHidden)
                    {
                        TFTVLogger.Always($"considering summary hidden objective {objective.GetDescription()}", false);

                        TacticalActor tacticalActor = relevantActors.FirstOrDefault();

                        if (_pendingObjectivesTargets.ContainsKey(tacticalActor))
                        {
                            _pendingObjectivesTargets[tacticalActor].Add(objective);
                        }
                        else
                        {
                            _pendingObjectivesTargets.Add(tacticalActor, new List<FactionObjective>() { objective });
                        }

                        objective.IsUiHidden = false;
                        objective.IsUiSummaryHidden = false;
                        return false;
                    }

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


        [HarmonyPatch(typeof(ActorHasStatusFactionObjective), "GetTargets")] //VERIFIED
        public static class ActorHasStatusFactionObjective_GetTargets_Patch
        {
            public static bool Prefix(ActorHasStatusFactionObjective __instance, GameTagDef ____targetsGameTag, ref IEnumerable<TacticalActorBase> __result)
            {
                try
                {
                    if (__instance.Faction == null || !__instance.Level.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        return true;
                    }


                    if (_secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey)
                        && ____targetsGameTag is ItemTypeTagDef)
                    {
                        List<TacticalActorBase> allValidTargets = new List<TacticalActorBase>();

                        foreach (TacticalActor tacticalActor in __instance.Level.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null).Concat
                            (__instance.Level.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                        {
                            if (TacticalActorHasTagAnywhere(tacticalActor, ____targetsGameTag))
                            {
                                allValidTargets.Add(tacticalActor);
                            }
                        }

                        __result = allValidTargets;

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

        private static FactionObjectiveState CaptureObjectiveWinEvaluate(ActorHasStatusFactionObjective objective)
        {
            try
            {

                if (!objective.Faction.HasTacActorsThatCanWin() && !objective.Faction.HasUndeployedTacActors())
                {
                    return FactionObjectiveState.Failed;
                }

                List<TacticalActor> targets = new List<TacticalActor>();

                FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                GameTagDef targetTag = fieldInfo.GetValue(objective) as GameTagDef;


                foreach (TacticalActor tacticalActor in objective.Level.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null).
                    Concat(objective.Level.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                {
                    if (TacticalActorHasTagAnywhere(tacticalActor, targetTag))
                    {
                        targets.Add(tacticalActor);
                    }
                }

                if (!targets.Any())
                {
                    TFTVLogger.Always($"returning fail state for {objective.Description.Localize()}");
                    return FactionObjectiveState.Failed;
                }

                if (targets.Any((TacticalActorBase x) => x.Status.HasStatus<ParalysedStatus>() && (CaptureTacticalWidget.CaptureUI == null || CaptureTacticalWidget.CaptureUI.capturedAliens.Contains(x))))
                {
                    TFTVLogger.Always($"returning achieved state for {objective.Description.Localize()}");
                    return FactionObjectiveState.Achieved;
                }

                TFTVLogger.Always($"returning inprogress state for {objective.Description.Localize()}");
                return FactionObjectiveState.InProgress;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void ShowObjectiveCompletePanel(FactionObjective objective)
        {
            try
            {
                //  TFTVLogger.Always($"ShowObjectiveCompletePanel Running");

                if ((objective is KillActorFactionObjective || objective is KillActorAlienBaseFactionObjective) && objective.State == FactionObjectiveState.Achieved)
                {
                    // TFTVLogger.Always($"{objective.GetDescription()} {objective.State}");

                    var targets = GetTargetsForObjective(objective, false, false);

                    if (targets == null)
                    {
                        return;
                    }

                    TacticalActor target = targets.FirstOrDefault();

                    if (target == null)
                    {
                        return;
                    }

                    ClassTagDef scyllaTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

                    if (_secondaryObjectiveDefsInPlay.Any(def => def.MissionObjectiveData.Description.LocalizationKey == _killScylla.MissionObjectiveData.Description.LocalizationKey)
                        && target.HasGameTag(scyllaTag) && objective.Description.LocalizationKey != _killScylla.MissionObjectiveData.Description.LocalizationKey)
                    {
                        TFTVLogger.Always($"not announcing {objective.Description.Localize()}");
                        return;

                    }

                    target.TacticalActorView.DoCameraChase(true);

                    string descriptionToAdd = objective.GetDescription().ToUpper();
                    bool isRevenant = false;

                    if (IsKillOrCaptureRevenantObjective(objective))
                    {
                        descriptionToAdd = GetTextForRevenantObjective(objective);
                    }

                    List<string> description = new List<string>() { descriptionToAdd };

                    CreateSecondaryObjectiveAnnouncement(description, target.ViewElementDef.SmallIcon, isRevenant, true);

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool TacticalActorHasTagAnywhere(TacticalActor ta, GameTagDef relevantTag)
        {
            try
            {
                List<GameTagDef> excludTags = new List<GameTagDef>() { DefCache.GetDef<GameTagDef>("SentinelTerror_ClassTagDef"), DefCache.GetDef<GameTagDef>("SentinelMist_ClassTagDef") };


                if (!ta.GameTags.Any(t => excludTags.Contains(t)) && (ta.HasGameTag(relevantTag)
                        || ta.Equipments.GetWeapons().Any(w => w.WeaponDef.Tags.Contains(relevantTag))
                        || ta.BodyState.GetAllBodyparts().Any(b => b.OwnerItem.TacticalItemDef.Tags.Contains(relevantTag))))
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


        private static bool CheckCaptureObjectiveCompleted(ActorHasStatusFactionObjective captureObjective)
        {
            try
            {
                TacticalFaction alienFaction = captureObjective.Faction.TacticalLevel.GetFactionByCommandName("aln");

                var captureTargetsGameTagField = AccessTools.Field(typeof(ActorHasStatusFactionObjective), "_targetsGameTag");

                GameTagDef captureTag = (GameTagDef)captureTargetsGameTagField.GetValue(captureObjective);




                if (!alienFaction.TacticalActors.Any(ta => TacticalActorHasTagAnywhere(ta, captureTag)
                && !ta.IsEvacuated && ta.Status != null
                && ta.Status.HasStatus<ParalysedStatus>()))
                {
                    return false;
                }

                List<TacticalActor> eligibleAliens = alienFaction.TacticalActors.Where(ta => TacticalActorHasTagAnywhere(ta, captureTag)
                && !ta.IsEvacuated && ta.Status != null
                && ta.Status.HasStatus<ParalysedStatus>()).ToList();

                if (CaptureTacticalWidget.CaptureUI == null)
                {
                    return true;
                }
                else if (eligibleAliens.Any(ta => CaptureTacticalWidget.CaptureUI.capturedAliens.Contains(ta)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(ActorHasStatusFactionObjective), "EvaluateObjective")] //VERIFIED
        public static class ActorHasStatusFactionObjective_EvaluateObjective_Patch
        {
            public static void Postfix(ActorHasStatusFactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    if (_secondaryObjectiveDefsInPlay.Any(o =>
                    o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey) && __result == FactionObjectiveState.Failed)
                    {
                        // TFTVLogger.Always($"{__instance.GetDescription()} {__result}");

                        if (CheckCaptureObjectiveCompleted(__instance))
                        {
                            __result = FactionObjectiveState.Achieved;
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


        [HarmonyPatch(typeof(FactionObjective), nameof(FactionObjective.Evaluate))]
        public static class FactionObjective_Evaluate_Patch
        {
            public static bool Prefix(FactionObjective __instance, out FactionObjectiveState __state)
            {
                try
                {
                    // Capture the initial state before Evaluate runs
                    __state = __instance.State;

                    // TFTVLogger.Always($"Objective {__instance.Description.Localize()} state {__instance.State}");

                    if (__instance.Faction != null && __instance.Faction.State == TacFactionState.Won)
                    {
                        TFTVLogger.Always($"Objective {__instance.Description.Localize()} state {__instance.State}");

                        if (__instance is ActorHasStatusFactionObjective captureObjective &&
                                _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                        {
                            TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");

                            if (!CheckCaptureObjectiveCompleted(captureObjective))
                            {
                                TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                return false;
                            }
                        }
                        else if (__instance is KillActorFactionObjective killObjective &&
                               _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                        {
                            // TFTVLogger.Always($"Not going to evaluate {__instance.Description.Localize()}");
                            TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");
                            var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                            GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective);

                            if (!CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                            {
                                TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                return false;
                            }
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

            public static void Postfix(FactionObjective __instance, ref FactionObjectiveState __result, FactionObjectiveState __state)
            {
                try
                {
                    if (__instance is EvacuateFactionObjective && __instance.State == FactionObjectiveState.Achieved)
                    {
                        TFTVLogger.Always($"EvacuateFactionObjective: Objective {__instance.Description.Localize()} state {__instance.State}");

                        foreach (FactionObjective factionObjective in __instance.Faction.Objectives)
                        {
                            //  TFTVLogger.Always($"examining in Postfix  {factionObjective?.Description?.Localize()}");

                            if (factionObjective is KillActorFactionObjective killObjective2 &&
                                _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == killObjective2.Description.LocalizationKey))
                            {
                                // TFTVLogger.Always($"Evac! Got here and examining {factionObjective.Description.Localize()}");

                                var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                                GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective2);

                                if (CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                                {
                                    var property = typeof(FactionObjective).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                    property.SetValue(killObjective2, FactionObjectiveState.Achieved);

                                    TFTVLogger.Always($"Objective {killObjective2.Description.Localize()} should succeed, state is {killObjective2.State}");

                                }
                            }
                        }
                    }


                    ModifySecondaryObjectivesEvaluateBehavior(__instance, ref __result, __state);
                    // TFTVVanillaFixes.ModifyVehicleRescueEvaluateBehavior(__instance, ref __result);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void ModifySecondaryObjectivesEvaluateBehavior(FactionObjective __instance, ref FactionObjectiveState __result, FactionObjectiveState __state)
        {
            try
            {


                if (__instance.Faction != null &&
                    __instance.Level.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {


                    if (__state != FactionObjectiveState.Achieved)
                    {
                        ShowObjectiveCompletePanel(__instance);
                    }

                    if (__instance.Faction != null && __instance.Faction.State == TacFactionState.Won)

                    {
                        if (__instance is ActorHasStatusFactionObjective captureObjective &&
                            _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                        {
                            TFTVLogger.Always($"{__instance.Description.Localize()} Initial State: {__state}, faction state: {__instance.Faction.State}");

                            if (__instance.Faction.State == TacFactionState.Won)
                            {
                                // Check if this specific objective should be marked as Achieved
                                __result = CaptureObjectiveWinEvaluate(captureObjective);
                            }

                            if (__result == FactionObjectiveState.Failed)
                            {
                                __result = FactionObjectiveState.InProgress;
                            }
                        }
                        else if (__instance is KillActorFactionObjective killObjective &&
                            _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                        {

                            if (__instance.Faction.State == TacFactionState.Won)
                            {
                                TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");
                                var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                                GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective);

                                if (CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                                {
                                    TFTVLogger.Always($"Objective {__instance.Description.Localize()} achieved!");
                                    __result = FactionObjectiveState.Achieved;
                                }
                                else
                                {
                                    TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                    __result = FactionObjectiveState.Failed;
                                }

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

        private static bool CheckIfActorDeadNotEvaced(TacticalLevelController controller, GameTagDef killTag)
        {
            try
            {
                //TacticalFaction alienFaction = controller.GetFactionByCommandName("aln");

                List<TacticalActor> enemyActors = controller.Factions
                                    .Where(f => f.GetRelationTo(controller.GetFactionByCommandName("px")) == FactionRelation.Enemy)
                                    .SelectMany(f => f.TacticalActors)
                                    .ToList();

                TFTVLogger.Always($"paralyzed enemy actor with killTag? " +
                    $"{enemyActors.Any(ta => ta.HasGameTag(killTag) && !ta.IsEvacuated && (ta.IsDead || ta.Status != null && ta.Status.HasStatus<ParalysedStatus>()))}");

                return enemyActors.Any(ta => ta.HasGameTag(killTag) && !ta.IsEvacuated && (ta.IsDead || ta.Status != null && ta.Status.HasStatus<ParalysedStatus>()));

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static void InitObjectivesTFTV(UIModuleObjectives __instance)
        {
            try
            {
                if (_secondaryObjectives == null)
                {
                    CreateSecondaryObjectivesWidget(__instance);
                }

                TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                // Get the objectives from the context
                ObjectivesManager objectives = tacticalLevelController.CurrentFaction.Objectives;

                objectives.Evaluate();

                // Separate primary and secondary objectives
                List<FactionObjective> primaryObjectives = objectives.Where(p => !p.IsUiHidden && !IsSecondaryObjective(p)).ToList();
                List<FactionObjective> secondaryObjectives = objectives.Where(p => IsSecondaryObjective(p) && IsRelevantObjective(p)).ToList();

                RunPendingObjectives();

                if (secondaryObjectives.Count == 0)
                {
                    _secondaryObjectivesTitle.gameObject.SetActive(false);
                }
                else
                {
                    _secondaryObjectivesTitle.gameObject.SetActive(true);
                }

                // Update primary objectives
                UIUtil.EnsureActiveComponentsInContainer(__instance.ObjectivesContainer, __instance.ObjectivePrefab, primaryObjectives, (element, objective) =>
                {
                    element.SetObjective(objective);
                });

                // Update secondary objectives
                UIUtil.EnsureActiveComponentsInContainer(_secondaryObjectives, __instance.ObjectivePrefab, secondaryObjectives, (element, objective) =>
                {
                    element.SetObjective(objective);
                });



                // FailsafeRestoreUI();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        private static void CreateSecondaryObjectivesWidget(UIModuleObjectives moduleObjectives)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                Resolution resolution = Screen.currentResolution;
                float resolutionFactorWidth = (float)resolution.width / 1920f;
                float resolutionFactorHeight = (float)resolution.height / 1080f;

                Transform primaryObjectivesContainer = moduleObjectives.ObjectivesContainer;
                Vector3 primaryPosition = primaryObjectivesContainer.position;

                //   TFTVLogger.Always($"primaryPosition: {primaryPosition}");

                _secondaryObjectivesTitle = UnityEngine.Object.Instantiate(moduleObjectives.transform.Find("Objectives_Text"), uIModuleNavigation.transform);
                _secondaryObjectivesTitle.position = new Vector3(144 * resolutionFactorWidth, 900 * resolutionFactorHeight, 0.0f);

                _secondaryObjectives = UnityEngine.Object.Instantiate(moduleObjectives.ObjectivesContainer, _secondaryObjectivesTitle);

                //  TFTVLogger.Always($"_secondaryObjectives.GetComponent<RectTransform>().position: {_secondaryObjectives.GetComponent<RectTransform>().position}" +
                //      $"_secondaryObjectives.GetComponent<RectTransform>().anchoredPosition: {_secondaryObjectives.GetComponent<RectTransform>().anchoredPosition}");



                _secondaryObjectives.GetComponent<RectTransform>().position = new Vector3(primaryPosition.x, _secondaryObjectivesTitle.position.y + 35 * resolutionFactorHeight, 0);

                // _secondaryObjectives.transform.position = new Vector3(primaryPosition.x, primaryPosition.y - 50 * resolutionFactorHeight, primaryPosition.z);

                foreach (Component component in _secondaryObjectives.GetComponentsInChildren<Component>())
                {
                    if (component is Image image && image.name.Equals("Image"))
                    {
                        image.color = new Color(0, 0, 0, 0);
                    }

                    //  TFTVLogger.Always($"{component.name} {component.GetType()}");
                }


                foreach (Component component in _secondaryObjectivesTitle.GetComponents<Component>())
                {
                    if (component is Localize localize)
                    {
                        localize.enabled = false;
                    }
                    if (component is Text text)
                    {
                        /*   GlowEffect glowEffect = component.gameObject.AddComponent<GlowEffect>();
                           //  TFTVLogger.Always($"glowEffect==null? {glowEffect == null}");
                           glowEffect.Start();*/

                        text.font = moduleObjectives.ObjectivePrefab.GetComponentInChildren<Text>().font;
                        text.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_SECONDARY_OBJECTIVES");
                    }
                    if (component is UIColorController colorController)
                    {
                        colorController.Enabled = false;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void VerifyObjectiveListUIPrefix(UIModuleObjectives uIModulesObjectives, TacticalViewContext Context)
        {
            try
            {
                ObjectivesManager objectives = Context.LevelController.CurrentFaction.Objectives;

                List<FactionObjective> objectivesToRemove = new List<FactionObjective>();

                foreach (FactionObjective factionObjective in objectives)
                {
                    if (IsSecondaryObjective(factionObjective) && IsNeverRelevantObjective(factionObjective))
                    {
                        objectivesToRemove.Add(factionObjective);
                    }
                }

                foreach (FactionObjective factionObjective1 in objectivesToRemove)
                {
                    objectives.Remove(factionObjective1);

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
