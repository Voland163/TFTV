using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;
using static TFTV.TFTVCapturePandorans;

namespace TFTV.TFTVUI.Tactical
{
    internal class CaptureTacticalWidget
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public class AircraftUI : MonoBehaviour
        {


            public Transform uiParent;  // Parent object for the entire UI panel
            private GameObject _spacesPanel;
            public Sprite aircraftSprite;  // Background image of the aircraft
            public Sprite aircraftTypeIcon;  // Icon for the type of aircraft
            public string aircraftName;
            public int totalCapacity;  // Example total capacity
            public int filledSpaces;    // Example filled spaces
                                        //private readonly Font _font = ; // Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name.Equals("Purista Semibold"));
            public bool showCaptureDetails = true;
            // List of currently captured aliens (to be updated dynamically)
            public List<TacticalActor> capturedAliens = new List<TacticalActor>();

            private readonly Color _dimWhite = new Color(0.757f, 0.988f, 1.0f);
            private GameObject _alienPanel1;
            private GameObject _alienPanel2;
            private GameObject _alienPanel3;// Reference to the alien panel
            private GameObject _alienPanel4;
            List<GameObject> _alienPanels = new List<GameObject>();
            private GameObject freeSpaceTextObject; // Reference to the free space text object

            public void Init()
            {
                try
                {
                    CreateAircraftUI();
                    UpdateUI();  // Initial UI update         
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            // Method to create the entire UI for the aircraft
            void CreateAircraftUI()
            {
                try
                {
                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    // Create a parent panel for the entire UI
                    GameObject aircraftPanel = new GameObject("AircraftPanel");
                    aircraftPanel.transform.SetParent(uiParent, false);
                    RectTransform panelRect = aircraftPanel.AddComponent<RectTransform>();

                    if (aircraftSprite != null)
                    {
                        panelRect.position = new Vector3(1750 * resolutionFactorWidth, 830 * resolutionFactorHeight, 0.0f);
                    }
                    else
                    {
                        panelRect.position = new Vector3(1750 * resolutionFactorWidth, 930 * resolutionFactorHeight, 0.0f);

                    }
                    panelRect.sizeDelta = new Vector2(600, 500);
                    aircraftPanel.AddComponent<CanvasRenderer>();

                    // 1. Background Image (aircraft picture)
                    Image backgroundImage = aircraftPanel.AddComponent<Image>();
                    backgroundImage.sprite = aircraftSprite;  // Set the aircraft image
                    backgroundImage.type = Image.Type.Simple;
                    backgroundImage.preserveAspect = true;  // Make sure the image is not stretched
                                                            //  backgroundImage.rectTransform.sizeDelta = new Vector2(600 * resolutionFactorWidth, 330 * resolutionFactorHeight);
                    if (aircraftSprite != null)
                    {
                        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0.4f);
                    }
                    else
                    {
                        backgroundImage.color = new Color(0, 0, 0, 0);
                        panelRect.sizeDelta = new Vector2(600, 300);
                    }

                    GameObject backgroundContainer = new GameObject("BackgroundContainer");
                    backgroundContainer.transform.SetParent(aircraftPanel.transform, false);
                    RectTransform containerRect = backgroundContainer.AddComponent<RectTransform>();
                    containerRect.sizeDelta = backgroundImage.rectTransform.sizeDelta;

                    // Step 2: Move backgroundImage as a child of this container
                    backgroundImage.transform.SetParent(backgroundContainer.transform, false);

                    // Step 3: Flip the background image by setting localScale on x-axis
                    containerRect.localScale = new Vector3(-1, 1, 1);

                    if (TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        string tooltipText = AircraftReworkTacticalModules.BuildTacticalModulesTooltip();
                        if (!string.IsNullOrWhiteSpace(tooltipText))
                        {
                            UITooltipText tooltip = backgroundImage.gameObject.GetComponent<UITooltipText>() ?? backgroundImage.gameObject.AddComponent<UITooltipText>();
                            tooltip.TipText = tooltipText;
                            tooltip.Position = UITooltip.Position.Bottom;
                            tooltip.enabled = true;
                        }
                    }

                    // 2. Aircraft Name Text
                    GameObject nameTextObject = new GameObject("AircraftName");
                    nameTextObject.transform.SetParent(aircraftPanel.transform, false);
                    Text nameText = nameTextObject.AddComponent<Text>();
                    nameText.text = aircraftName;
                    nameText.font = PuristaSemiboldFontCache;
                    nameText.fontSize = 35;
                    nameText.color = WhiteColor;
                    nameText.alignment = TextAnchor.MiddleLeft;
                    RectTransform nameTextRect = nameText.GetComponent<RectTransform>();
                    nameTextRect.sizeDelta = new Vector2(290, 100);
                    nameTextRect.anchoredPosition = new Vector2(100, -70);

                    // 3. Aircraft Type Icon
                    GameObject iconObject = new GameObject("AircraftIcon");
                    iconObject.transform.SetParent(aircraftPanel.transform, false);
                    Image iconImage = iconObject.AddComponent<Image>();
                    iconImage.sprite = aircraftTypeIcon;  // Set the aircraft type icon
                    RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(60, 60);  // Example size
                    iconRect.anchoredPosition = new Vector2(-80, -70);  // Adjusted position to the left



                    AddOutlineToIcon addOutlineToIcon = iconObject.GetComponent<AddOutlineToIcon>() ?? iconObject.AddComponent<AddOutlineToIcon>();
                    addOutlineToIcon.icon = iconObject;
                    addOutlineToIcon.InitOrUpdate();

                    if (!showCaptureDetails)
                    {
                        return;
                    }

                    // 4. Free Space Text
                    freeSpaceTextObject = new GameObject("FreeSpaceText");
                    freeSpaceTextObject.transform.SetParent(aircraftPanel.transform, false);
                    Text freeSpaceText = freeSpaceTextObject.AddComponent<Text>();
                    freeSpaceText.font = PuristaSemiboldFontCache; //Resources.GetBuiltinResource<Font>("Arial.ttf");
                    freeSpaceText.fontSize = 30;
                    freeSpaceText.color = WhiteColor;
                    RectTransform freeSpaceTextRect = freeSpaceText.GetComponent<RectTransform>();
                    freeSpaceTextRect.sizeDelta = new Vector2(350, 50);  // Increased width for text
                    freeSpaceTextRect.anchoredPosition = new Vector2(70, -130);  // Moved further down and to the right

                    // 5. Create a Horizontal Layout Group for filled/empty spaces
                    GameObject spacesPanel = new GameObject("SpacesPanel");
                    spacesPanel.transform.SetParent(aircraftPanel.transform, false);
                    RectTransform spacesRect = spacesPanel.AddComponent<RectTransform>();
                    spacesRect.sizeDelta = new Vector2(350, 20);  // Increased size for more space
                    spacesRect.anchoredPosition = new Vector2(70, -160);  // Moved down and to the right
                    HorizontalLayoutGroup layoutGroupSpacePanel = spacesPanel.AddComponent<HorizontalLayoutGroup>();
                    layoutGroupSpacePanel.spacing = 5;  // Set spacing between icons
                    layoutGroupSpacePanel.childAlignment = TextAnchor.MiddleLeft;
                    _spacesPanel = spacesPanel;

                    // 6. Add the alien capture panel (new section)

                    int alienPanelsNeeded = (totalCapacity + 5) / 6;

                    for (int x = 0; x < alienPanelsNeeded; x++)
                    {
                        CreateAlienCapturePanel(aircraftPanel.transform, x);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            // Method to create a space icon
            private void CreateSpaceIcon(Transform parent, Color color, bool hasOutline = false)
            {
                try
                {

                    GameObject icon = new GameObject("SpaceIcon");
                    icon.transform.SetParent(parent, false);
                    Image iconImage = icon.AddComponent<Image>();

                    iconImage.preserveAspect = true;

                    // Add and configure the Outline component

                    iconImage.color = color;  // Red for filled, white for empty

                    AddOutlineToIcon addOutlineToIcon = icon.GetComponent<AddOutlineToIcon>() ?? icon.AddComponent<AddOutlineToIcon>();
                    addOutlineToIcon.icon = icon;
                    addOutlineToIcon.InitOrUpdate();

                    RectTransform rt = icon.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(20, 20);  // Adjust size for the icon
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            // Method to create the alien capture panel
            private void CreateAlienCapturePanel(Transform parent, int rowNumber)
            {
                try
                {
                    //  TFTVLogger.Always($"creating alien panel {rowNumber}");

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    if(resolutionFactorWidth>1.4f)
                    {
                        resolutionFactorWidth = 1.4f;
                    }
             //   TFTVLogger.Always($"resolutionFactorWidth: {resolutionFactorWidth} for {resolution.width}");


                    GameObject alienPanel = new GameObject($"AlienCapturePanel{rowNumber}");
                    alienPanel.transform.SetParent(parent, false);
                    RectTransform alienPanelRect = alienPanel.AddComponent<RectTransform>();
                    alienPanelRect.sizeDelta = new Vector2(370 * resolutionFactorWidth, 85);  // Adjust as necessary
                    alienPanelRect.anchoredPosition = new Vector2(75, -90 * rowNumber - 220);  // Adjust position

                    // Add a black background image to the alien capture panel
                    Image background = alienPanel.AddComponent<Image>();
                    background.color = new Color(0, 0, 0, 0.5f);
                    background.type = Image.Type.Sliced;  // Optionally make it sliced for better scaling
                    background.rectTransform.sizeDelta = new Vector2(370 * resolutionFactorWidth, 70);

                    if (rowNumber == 0)
                    {
                        _alienPanel1 = alienPanel;
                    }
                    else if (rowNumber == 1)
                    {
                        _alienPanel2 = alienPanel;
                    }
                    else if (rowNumber == 2)
                    {
                        _alienPanel3 = alienPanel;
                    }
                    else
                    {
                        _alienPanel4 = alienPanel;

                    }

                    alienPanel.SetActive(false);

                    _alienPanels.Add(alienPanel);
                    // Adjust size
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            // Method to update the UI dynamically
            public void UpdateUI()
            {
                try
                {
                    if (!showCaptureDetails)
                    {
                        return;
                    }

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;
                    float alienRowScale = Mathf.Min(resolutionFactorWidth, 1.4f);

                    foreach (GameObject alienPanel in _alienPanels)
                    {
                        alienPanel.SetActive(false);

                        // Clear existing alien icons and space squares
                        foreach (Transform child in alienPanel.transform)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    foreach (Transform child in _spacesPanel.transform)
                    {
                        if (child.gameObject.name.Contains("SpaceIcon"))
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    for (int i = 0; i < filledSpaces; i++)
                    {
                        CreateSpaceIcon(_spacesPanel.transform, _dimWhite); //new Color(0.329f, 0.376f, 0.455f));  // grey for filled
                    }
                    for (int i = filledSpaces; i < Mathf.Max(16, totalCapacity); i++)
                    {
                        if (i < totalCapacity)
                        {
                            CreateSpaceIcon(_spacesPanel.transform, Color.gray);
                        }
                        else
                        {
                            CreateSpaceIcon(_spacesPanel.transform, Color.clear);
                        }
                        // white for empty
                    }

                    // Loop through captured aliens and create the icons and squares
                    for (int i = 0; i < capturedAliens.Count; i++)
                    {
                        TacticalActor alien = capturedAliens[i];

                        // Alien Icon
                        GameObject alienIconObject = new GameObject($"AlienIcon_{i}");
                        int position = i;
                        if (i < 6)
                        {
                            _alienPanel1.SetActive(true);
                            alienIconObject.transform.SetParent(_alienPanel1.transform, false);
                        }
                        else if (i >= 6 && i < 13)
                        {
                            _alienPanel2.SetActive(true);
                            alienIconObject.transform.SetParent(_alienPanel2.transform, false);
                            position -= 6;
                        }
                        else if (i >= 13 && i < 19)
                        {
                            _alienPanel3.SetActive(true);
                            alienIconObject.transform.SetParent(_alienPanel3.transform, false);
                            position -= 12;
                        }
                        else
                        {
                            _alienPanel3.SetActive(true);
                            alienIconObject.transform.SetParent(_alienPanel3.transform, false);
                            position -= 18;
                        }


                        Image alienIconImage = alienIconObject.AddComponent<Image>();
                        alienIconImage.sprite = alien.ViewElementDef.SmallIcon;  // Use the alien sprite
                        RectTransform alienIconRect = alienIconObject.GetComponent<RectTransform>();
                        alienIconRect.sizeDelta = new Vector2(50, 50);  // Adjust size for the alien icon
                        alienIconRect.anchoredPosition = new Vector2((position * 60 - 150) * alienRowScale, 0);


                        AddOutlineToIcon addOutlineToIcon = alienIconObject.GetComponent<AddOutlineToIcon>() ?? alienIconObject.AddComponent<AddOutlineToIcon>();
                        addOutlineToIcon.icon = alienIconObject;
                        addOutlineToIcon.InitOrUpdate();


                        if (!alienIconObject.GetComponent<EventTrigger>())
                        {
                            alienIconObject.AddComponent<EventTrigger>();
                        }


                        EventTrigger eventTrigger = alienIconObject.GetComponent<EventTrigger>();
                        eventTrigger.triggers.Clear();
                        EventTrigger.Entry click = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerClick
                        };

                        click.callback.AddListener((eventData) =>
                        {
                            alien.TacticalActorView.DoCameraChase();
                        });

                        eventTrigger.triggers.Add(click);


                        // Alien Space Squares - two rows if slots > 4
                        int spaceUsage = TFTVCapturePandorans.CalculateCaptureSlotCost(alien.GameTags.ToList());  // Retrieve the space usage for the alien
                        int maxPerRow = 4;  // Max number of squares per row
                        for (int j = 0; j < spaceUsage; j++)
                        {
                            GameObject spaceSquare = new GameObject($"AlienSpace_{i}_{j}");
                            spaceSquare.transform.SetParent(alienIconObject.transform, false);

                            // Add and configure the Image component
                            Image spaceSquareImage = spaceSquare.AddComponent<Image>();
                            spaceSquareImage.color = _dimWhite;  // Space color

                            // Add and configure the Outline component
                            Outline outline = spaceSquare.AddComponent<Outline>();
                            outline.effectColor = Color.black;     // Outline color
                            outline.effectDistance = new Vector2(4, -4);  // Outline thickness (adjust as needed)

                            // Configure the RectTransform
                            RectTransform spaceRect = spaceSquare.GetComponent<RectTransform>();
                            spaceRect.sizeDelta = new Vector2(8, 8);  // Adjust size

                            // Row and column logic (Start from bottom-left corner)
                            int row = j / maxPerRow;     // Row (resets after 4 squares)
                            int column = j % maxPerRow;  // Column

                            // Adjust position to start from bottom-left of the icon
                            spaceRect.anchoredPosition = new Vector2(column * 12 - (alienIconRect.sizeDelta.x / 3), -row * 12 - (alienIconRect.sizeDelta.y / 2));  // Align from bottom-left
                        }

                    }

                    // Update free space text
                    int freeSpaces = totalCapacity - filledSpaces;

                    //   TFTVLogger.Always($"Free capture slots: {freeSpaces.ToString()}/{totalCapacity.ToString()}");

                    freeSpaceTextObject.GetComponent<Text>().text = $"{TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_CAPTURE_FREE_SLOTS")} {freeSpaces}/{totalCapacity}";

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        public static void ClearData()
        {
            try
            {
                CaptureUI = null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static AircraftUI CaptureUI = null;


        /// <summary>
        /// This is run on UIModuleObjectives.Init(). It's aborted if Limite Capture is off, no capture is possible due to lack of space/capability, or if there are no aliens.
        /// </summary>
        public static void CreateCaptureTacticalWidget(UIModuleObjectives moduleObjectives)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
              //  TFTVLogger.Always($"[CreateCaptureTacticalWidget] Aircraft name: {AircraftName} viewElement: {AircraftViewElement}");
                //  TFTVLogger.Always($"CreatCaptureTacticalWidget: {TFTVNewGameOptions.LimitedCaptureSetting} {ContainmentFacilityPresent} {AircraftCaptureCapacity}");

                bool captureAvailable = TFTVNewGameOptions.LimitedCaptureSetting
                   && (ContainmentFacilityPresent || AircraftCaptureCapacity > 0);
                bool hasAliens = controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln"));
                bool showCaptureDetails = captureAvailable && hasAliens;
                if (!showCaptureDetails && !TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                if (CaptureUI == null)
                {

                    // TFTVLogger.Always($"got here");

                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    // Create the aircraft UI panel as a child of uIModuleNavigation
                    GameObject aircraftUIPanel = new GameObject("AircraftCaptureWidget", typeof(RectTransform));
                    RectTransform aircraftRectTransform = aircraftUIPanel.GetComponent<RectTransform>();

                    // Set it as a child of uIModuleNavigation
                    aircraftRectTransform.SetParent(uIModuleNavigation.transform, false);

                    // Set anchors to ensure proper scaling and positioning relative to parent
                    aircraftRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Center the UI element
                    aircraftRectTransform.anchorMax = new Vector2(0.5f, 0.5f); // Center the UI element
                    aircraftRectTransform.pivot = new Vector2(0.5f, 0.5f);     // Set pivot to center

                    // Add your UI logic (AircraftUI)
                    AircraftUI uiScript = aircraftUIPanel.AddComponent<AircraftUI>();
                    uiScript.transform.SetParent(aircraftUIPanel.transform, false);
                    uiScript.uiParent = aircraftUIPanel.transform;
                    uiScript.showCaptureDetails = showCaptureDetails;
                    //   uiScript.transform.position = new Vector3(550 * resolutionFactorWidth, 1045 * resolutionFactorHeight, 0.0f);
                    // Parent the UI

                    if (AircraftViewElement != null && AircraftViewElement.Length > 5)
                    {
                        //  TFTVLogger.Always($"AircraftName: {AircraftName}, {AircraftViewElement}");

                        ViewElementDef viewElementDef = (ViewElementDef)Repo.GetDef(AircraftViewElement);

                        uiScript.aircraftSprite = viewElementDef.InventoryIcon ?? null;

                        if (viewElementDef.InventoryIcon == null)
                        {
                            uiScript.aircraftTypeIcon = viewElementDef.SmallIcon;
                        }
                        else
                        {
                            uiScript.aircraftTypeIcon = viewElementDef.LargeIcon;
                        }
                    }
                    else
                    {
                        uiScript.aircraftSprite = Helper.CreateSpriteFromImageFile("Manticore_transparent_uinomipmaps.png");
                        uiScript.aircraftTypeIcon = DefCache.GetDef<ViewElementDef>("PP_Manticore_View").LargeIcon;
                    }

                    uiScript.aircraftName = AircraftName;  // Set dynamically if needed

                    if (showCaptureDetails && AircraftCaptureCapacity != -1)
                    {
                        uiScript.totalCapacity = Math.Min(ContainmentSpaceAvailable, AircraftCaptureCapacity);
                    }
                    else if (showCaptureDetails)
                    {
                        uiScript.totalCapacity = ContainmentSpaceAvailable;
                    }
                    else
                    {
                        uiScript.totalCapacity = 0;
                    }

                    uiScript.filledSpaces = 0;    // Example, adjust accordingly

                    CaptureUI = uiScript;

                    //  TFTVLogger.Always($"widget setup");
                    // Call the UI creation method to build the interface
                    uiScript.Init();
                }

                if (showCaptureDetails)
                {
                    UpdateCaptureUI();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// This is run when a Pandoran is chosen or removed from priority extraction list and on UIModuleObjectives.Init()
        /// </summary>
        public static void UpdateCaptureUI()
        {
            try
            {
                if (CaptureUI == null || !CaptureUI.showCaptureDetails)
                {
                    return;
                }

                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    return;
                }

                TacticalFaction aliens = controller.GetFactionByCommandName("aln");

                GameTagDef captureTag = Shared.SharedGameTags.CapturableTag;

                List<TacticalActor> priorityAliens = aliens.TacticalActors.Where(a => a.Status.HasStatus<ReadyForCapturesStatus>()).ToList();
                List<TacticalActor> paralyzedAliens = aliens.TacticalActors.Where(a =>
                a.IsAlive
                && a.Status.HasStatus<ParalysedStatus>()
                && a.GameTags.Contains(captureTag)
                && !priorityAliens.Contains(a))
                    .ToList();

                CaptureUI.capturedAliens.Clear();
                CaptureUI.filledSpaces = 0;
                int availableCaptureslotsCounter = CaptureUI.totalCapacity;//CachedACC;

                foreach (TacticalActor priorityCaptureAlien in priorityAliens)
                {
                    int slotCost = CalculateCaptureSlotCost(priorityCaptureAlien.GameTags.ToList());

                    availableCaptureslotsCounter -= slotCost;
                    CaptureUI.capturedAliens.Add(priorityCaptureAlien);
                    CaptureUI.filledSpaces += slotCost;
                }

                paralyzedAliens = paralyzedAliens.OrderByDescending(taur => CalculateCaptureSlotCost(taur.GameTags.ToList())).ToList();

                foreach (TacticalActor otherParalyzedAlien in paralyzedAliens)
                {
                    //  TFTVLogger.Always($"paralyzed {otherParalyzedAlien.TacticalActorBaseDef.name}, aircraftCaptureCapacity is {availableCaptureslotsCounter}");
                    int slotCost = CalculateCaptureSlotCost(otherParalyzedAlien.GameTags.ToList());

                    if (availableCaptureslotsCounter >= slotCost)
                    {
                        //  TFTVLogger.Always($"{tacActorUnitResult1.TacticalActorBaseDef.name} added to capture list; available slots before that {availableCaptureslotsCounter}");
                        CaptureUI.capturedAliens.Add(otherParalyzedAlien);
                        CaptureUI.filledSpaces += slotCost;
                        availableCaptureslotsCounter -= slotCost;
                        //  TFTVLogger.Always($"{otherParalyzedAlien.TacticalActorBaseDef.name} added to capture list; available slots after {availableCaptureslotsCounter}");
                    }
                }

                CaptureUI.UpdateUI();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }
    }
}
