using HarmonyLib;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFTV
{
    class TFTVHavenRecruitsScreen
    {

        // Patch the method that initializes the geoscape UI.
        // (Replace "GeoscapeUI" and "InitializeUI" with the actual type/method names.)
      /*  [HarmonyPatch(typeof(UIModuleTimeControl), "Awake")]
        public static class UIModuleTimeControlPatch
        {
            public static void Postfix(UIModuleTimeControl __instance)
            {
                try
                {
                    // Log the patch invocation
                    TFTVLogger.Always("UIModuleTimeControlPatch invoked!");

                    // Create a new GameObject for the button
                    GameObject buttonObj = new GameObject("RecruitOverlayButton");
                    buttonObj.AddComponent<RectTransform>();
                    Button button = buttonObj.AddComponent<Button>();
                    Image buttonImage = buttonObj.AddComponent<Image>();
                    buttonImage.color = Color.white; // Customize as needed

                    // Create a Text child for the button label
                    GameObject textObj = new GameObject("ButtonText");
                    textObj.transform.SetParent(buttonObj.transform, false);
                    Text label = textObj.AddComponent<Text>();
                    label.text = "Recruits";
                    label.alignment = TextAnchor.MiddleCenter;
                    label.color = Color.black;
                    label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                    // Set up RectTransform sizes/positions
                    RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                    buttonRect.sizeDelta = new Vector2(150, 40); // Adjust size as needed

                    // Parent the button to the same parent as the Pause button
                    Transform parentTransform = __instance.PauseTimeButton.transform.parent;
                    buttonObj.transform.SetParent(parentTransform, false);

                    // Position the button above the Pause button
                    RectTransform pauseButtonRect = __instance.PauseTimeButton.GetComponent<RectTransform>();
                    buttonRect.anchoredPosition = pauseButtonRect.anchoredPosition + new Vector2(0, 50); // Adjust offset as needed

                    // Assign the button callback to show the overlay panel
                    button.onClick.AddListener(() => RecruitOverlayManager.ToggleOverlay());
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }*/


        // This class manages the overlay panel that shows all known recruits.
        public static class RecruitOverlayManager
        {
            static GameObject overlayPanel;
            static bool isInitialized = false;

            // Call this method to show or hide the recruit overlay.
            public static void ToggleOverlay()
            {
                try
                {
                    if (!isInitialized)
                    {
                        CreateOverlay();
                        isInitialized = true;
                    }
                    // Toggle visibility
                    overlayPanel.SetActive(!overlayPanel.activeSelf);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            // Creates the overlay panel and populates it with UI elements.
            private static void CreateOverlay()
            {
                try
                {
                    // Create a new canvas if one doesn't already exist
                    Canvas overlayCanvas = Object.FindObjectOfType<Canvas>();
                    if (overlayCanvas == null || overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    {
                        GameObject canvasObj = new GameObject("OverlayCanvas");
                        overlayCanvas = canvasObj.AddComponent<Canvas>();
                        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObj.AddComponent<CanvasScaler>();
                        canvasObj.AddComponent<GraphicRaycaster>();
                    }


                    // Create the panel that will serve as the overlay background
                    overlayPanel = new GameObject("RecruitOverlayPanel");
                    overlayPanel.AddComponent<CanvasRenderer>();
                    Image panelImage = overlayPanel.AddComponent<Image>();
                    panelImage.color = new Color(0f, 0f, 0f, 0.75f); // Semi-transparent dark background

                    // Attach the panel to the canvas
                    overlayPanel.transform.SetParent(overlayCanvas.transform, false);

                    // Set panel to cover the full screen
                    RectTransform rectTransform = overlayPanel.GetComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;



                    // Create container for the recruit list
                    GameObject listContainer = new GameObject("RecruitListContainer");
                    listContainer.transform.SetParent(overlayPanel.transform, false);
                    RectTransform listRect = listContainer.AddComponent<RectTransform>();
                    listRect.anchorMin = new Vector2(0.1f, 0.1f);
                    listRect.anchorMax = new Vector2(0.9f, 0.9f);
                    listRect.offsetMin = Vector2.zero;
                    listRect.offsetMax = Vector2.zero;



                    // Populate the recruit list
                    PopulateRecruitList(listContainer);

                    // Optionally, add a close button on the overlay itself
                    AddCloseButton(overlayPanel);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }


            // Add placeholder entries for recruits; in a complete mod you'd query actual game data.
            private static void PopulateRecruitList(GameObject container)
            {
                try
                {
                    // This is a simplified example. You would likely be iterating over a list of recruit data.
                    for (int i = 0; i < 5; i++)
                    {
                        GameObject recruitItem = new GameObject($"RecruitItem_{i}");
                        recruitItem.transform.SetParent(container.transform, false);

                        // Add a button to represent the recruit
                        Button recruitButton = recruitItem.AddComponent<Button>();
                        Image itemBackground = recruitItem.AddComponent<Image>();
                        itemBackground.color = Color.gray;

                        // Create text for recruit information
                        GameObject recruitTextObj = new GameObject("RecruitInfo");
                        recruitTextObj.transform.SetParent(recruitItem.transform, false);
                        Text recruitInfo = recruitTextObj.AddComponent<Text>();
                        recruitInfo.text = $"Recruit #{i}";
                        recruitInfo.alignment = TextAnchor.MiddleLeft;
                        recruitInfo.color = Color.black;
                        recruitInfo.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                        // Set up RectTransforms (positions, sizes) as needed
                        RectTransform itemRect = recruitItem.GetComponent<RectTransform>();
                        itemRect.sizeDelta = new Vector2(400, 30); // width and height
                        itemRect.anchoredPosition = new Vector2(0, -35 * i);

                        // Add a callback on click: perhaps navigate to the haven where this recruit is located.
                        int recruitIndex = i; // capture current index for the lambda
                        recruitButton.onClick.AddListener(() => OnRecruitClicked(recruitIndex));
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            // This function would be called when a recruit entry is clicked.
            private static void OnRecruitClicked(int recruitIndex)
            {
                Debug.Log($"Recruit {recruitIndex} clicked. Transitioning to the corresponding haven...");
                // Insert the code that handles navigating to the recruit’s haven here.
                // This might involve invoking game methods via reflection or sending messages to the game.
            }

            // Adds a close button to the overlay for convenience.
            private static void AddCloseButton(GameObject parentPanel)
            {
                try
                {
                    GameObject closeButtonObj = new GameObject("CloseOverlayButton");
                    closeButtonObj.AddComponent<RectTransform>();
                    Button closeButton = closeButtonObj.AddComponent<Button>();
                    Image closeImage = closeButtonObj.AddComponent<Image>();
                    closeImage.color = Color.red; // differentiate close button

                    // Create text for the button
                    GameObject closeTextObj = new GameObject("CloseText");
                    closeTextObj.transform.SetParent(closeButtonObj.transform, false);
                    Text closeText = closeTextObj.AddComponent<Text>();
                    closeText.text = "Close";
                    closeText.alignment = TextAnchor.MiddleCenter;
                    closeText.color = Color.white;
                    closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                    // Set the button's size and position (adjust these values to suit your layout)
                    RectTransform closeRect = closeButtonObj.GetComponent<RectTransform>();
                    closeRect.sizeDelta = new Vector2(100, 40);
                    closeRect.anchorMin = new Vector2(1, 1);
                    closeRect.anchorMax = new Vector2(1, 1);
                    closeRect.anchoredPosition = new Vector2(-60, -30);

                    // Parent the close button to the overlay panel
                    closeButtonObj.transform.SetParent(parentPanel.transform, false);

                    // When clicked, hide the overlay panel.
                    closeButton.onClick.AddListener(() => { parentPanel.SetActive(false); });
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }
    }


}
