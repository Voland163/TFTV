using Base;
using Base.Core;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Destruction;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{



    public class OverlayQuipManager : MonoBehaviour
    {
        private static OverlayQuipManager _instance;
        public static OverlayQuipManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a new GameObject for the manager if needed.
                    GameObject managerGO = new GameObject("OverlayQuipManager");
                    _instance = managerGO.AddComponent<OverlayQuipManager>();
                    DontDestroyOnLoad(managerGO);
                }
                return _instance;
            }
        }

        private Coroutine autoClearCoroutine;
        // Set the delay (in seconds) after which lingering quips should be cleared.
        public float autoClearDelay = 4f;

        /// <summary>
        /// Schedules (or resets) the auto-clear timer.
        /// </summary>
        public void ScheduleAutoClear()
        {
            try
            {
                // If a timer is already running, cancel it.
                if (autoClearCoroutine != null)
                {
                    StopCoroutine(autoClearCoroutine);
                }
                autoClearCoroutine = StartCoroutine(AutoClearCoroutine());
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private IEnumerator AutoClearCoroutine()
        {
            // Wait for the specified delay.
            yield return new WaitForSeconds(autoClearDelay);
            // Then clear all lingering OverlayQuips.
            OverlayQuip.ClearAll();
        }
    }




    /// <summary>
    /// OverlayQuip creates a UI widget that shows a portrait and quip text in a black panel.
    /// It fades in, stays for a set duration, and then fades out.
    /// Multiple OverlayQuips will stack vertically.
    /// </summary>
    public class OverlayQuip : MonoBehaviour
    {
        // Components on this widget
        public CanvasGroup canvasGroup;
        public Image portraitImage;
        public Text quipText;

        // Duration settings (in seconds)
        public float fadeDuration = 0.5f;
        public float displayDuration = 3f;

        // A static list to keep track of active quips so they can be stacked properly.
        private static readonly List<OverlayQuip> activeQuips = new List<OverlayQuip>();

        // Vertical offset (in pixels) for stacking multiple quips (scaled by 3).
        private const float verticalOffset = 280f; // (originally 110, scaled 3×)

        // Base vertical offset for initial placement (to move the quip further down).
        private const float baseYOffset = -300f;

        /// <summary>
        /// Creates and displays an OverlayQuip.
        /// </summary>
        /// <param name="portrait">The sprite to display as the character portrait.</param>
        /// <param name="text">The quip text.</param>
        /// <param name="vignetteSprite">The sprite to use for the vignette overlay on the portrait.</param>
        /// <returns>The OverlayQuip instance (or null if a parent Canvas wasn’t found).</returns>
        public static OverlayQuip Create(Sprite portrait, string text)
        {
            try
            {
                // Find the TacticalLevelController and its navigation module to use as parent.
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                Transform parent = uIModuleNavigation.transform;

                // Create the main GameObject for the quip.
                GameObject quipGO = new GameObject("OverlayQuip");
                quipGO.transform.SetParent(parent, false);

                // Add a RectTransform and configure its anchors/pivot so that it appears on the right side.
                RectTransform rt = quipGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(1, 0.5f);
                // Scale up the panel 3× in width, but reduce the height to 80% of its former size:
                rt.sizeDelta = new Vector2(1000, 150); // was (1200, 450)
                                                       // Position it inside the right edge and lower it using baseYOffset.
                rt.anchoredPosition = new Vector2(-20, baseYOffset - (activeQuips.Count * verticalOffset));

                // Add an Image component to serve as the black background panel.
                Image backgroundImage = quipGO.AddComponent<Image>();
                backgroundImage.color = Color.black;

                // Add a CanvasGroup so we can easily fade the whole widget.
                CanvasGroup cg = quipGO.AddComponent<CanvasGroup>();
                cg.alpha = 0f; // Start transparent

                // Add the OverlayQuip component (this script) and set up its values.
                OverlayQuip overlay = quipGO.AddComponent<OverlayQuip>();
                overlay.canvasGroup = cg;

                // Create a child for the portrait image.
                GameObject portraitGO = new GameObject("Portrait");
                portraitGO.transform.SetParent(quipGO.transform, false);
                RectTransform portraitRT = portraitGO.AddComponent<RectTransform>();
                // Anchor the portrait to the left center of the panel.
                portraitRT.anchorMin = new Vector2(0, 0.5f);
                portraitRT.anchorMax = new Vector2(0, 0.5f);
                portraitRT.pivot = new Vector2(0, 0.5f);
                // Scale portrait 3× (width stays 300, height 300)
                portraitRT.sizeDelta = new Vector2(300, 300);
                // Move the portrait slightly to the left (from 30 to 20)
                portraitRT.anchoredPosition = new Vector2(-140, 5);

                Image portraitImage = portraitGO.AddComponent<Image>();
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
                overlay.portraitImage = portraitImage;
                portraitImage.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                // --- Add the vignette overlay on top of the portrait ---
                GameObject vignetteGO = new GameObject("Vignette");
                vignetteGO.transform.SetParent(portraitGO.transform, false);
                RectTransform vignetteRT = vignetteGO.AddComponent<RectTransform>();
                // Make the vignette fill the entire portrait area.
                vignetteRT.anchorMin = new Vector2(0, 0);
                vignetteRT.anchorMax = new Vector2(1, 1);
                vignetteRT.offsetMin = Vector2.zero;
                vignetteRT.offsetMax = Vector2.zero;
                // Add the Image component for the vignette.
                Image vignetteImage = vignetteGO.AddComponent<Image>();
                vignetteImage.sprite = Helper.CreateSpriteFromImageFile("BG_operative_vignette.png");
                vignetteImage.preserveAspect = true;
                // Ensure the vignette is drawn on top.
                vignetteGO.transform.SetAsLastSibling();
                // ---------------------------------------------------------

                // Create a child for the quip text.
                GameObject textGO = new GameObject("QuipText");
                textGO.transform.SetParent(quipGO.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                // Stretch the text to fill the space to the right of the portrait.
                textRT.anchorMin = new Vector2(0, 0);
                textRT.anchorMax = new Vector2(1, 1);
                // Adjust offsets: since we moved the portrait left, reduce the left offset slightly.
                textRT.offsetMin = new Vector2(150, 30); // left and bottom padding from 340
                textRT.offsetMax = new Vector2(-30, -30); // right and top padding

                Text quipText = textGO.AddComponent<Text>();
                quipText.text = text;
                // Use a built‑in font (e.g., Arial)
                quipText.font = TFTVUITactical.PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                // Set the font size to 60% of the previous 72 → approx 43.
                quipText.fontSize = 43;
                quipText.color = Color.white;
                quipText.alignment = TextAnchor.MiddleLeft;
                quipText.horizontalOverflow = HorizontalWrapMode.Wrap;
                quipText.verticalOverflow = VerticalWrapMode.Overflow;
                overlay.quipText = quipText;

                // Add this quip to the active list for stacking.
                activeQuips.Add(overlay);

                // Reset the auto-clear timer every time a new quip is created.
                OverlayQuipManager.Instance.ScheduleAutoClear();

                // Start the sequence (fade in, display, fade out).
                overlay.StartCoroutine(overlay.ShowSequence());

                return overlay;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        /// <summary>
        /// The coroutine that fades the quip in, waits, then fades it out.
        /// </summary>
        private IEnumerator ShowSequence()
        {
            // Fade in.
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

            // Wait for the display duration.
            yield return new WaitForSeconds(displayDuration);

            // Start fading out and scaling down.
            StartCoroutine(Fade(1f, 0f, fadeDuration));
            yield return StartCoroutine(Scale(Vector3.one, Vector3.zero, fadeDuration));

            // Remove this quip from the active list and adjust positions of any remaining quips.
            activeQuips.Remove(this);
          //  AdjustActiveQuipsPositions();

            // Destroy this GameObject.
            Destroy(gameObject);
        }

        /// <summary>
        /// Fades the widget’s CanvasGroup alpha from startAlpha to endAlpha over the given duration.
        /// </summary>
        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
            if (canvasGroup != null)
                canvasGroup.alpha = endAlpha;
        }

        /// <summary>
        /// Scales the widget’s RectTransform from startScale to endScale over the given duration.
        /// </summary>
        private IEnumerator Scale(Vector3 startScale, Vector3 endScale, float duration)
        {
            float elapsed = 0f;
            RectTransform rt = GetComponent<RectTransform>();
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (rt != null)
                    rt.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            if (rt != null)
                rt.localScale = endScale;
        }

        /// <summary>
        /// Adjusts the vertical positions of all active quips so they remain properly stacked.
        /// </summary>
        private static void AdjustActiveQuipsPositions()
        {
            try
            {
                for (int i = 0; i < activeQuips.Count; i++)
                {
                    if (activeQuips[i] != null)
                    {
                        RectTransform rt = activeQuips[i].GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseYOffset - (i * verticalOffset));
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

        /// <summary>
        /// Clears all active OverlayQuips by stopping their animations and destroying their GameObjects.
        /// Call this method when you know that the current UI state should be reset (for example, when the player takes a certain action).
        /// </summary>
        public static void ClearAll()
        {
            try
            {
                // Create a temporary copy of the active quips list,
                // so that modifications (removals) during iteration don’t cause issues.
                List<OverlayQuip> quipsToClear = new List<OverlayQuip>(activeQuips);
                foreach (OverlayQuip quip in quipsToClear)
                {
                    if (quip != null)
                    {
                        // Stop any ongoing fade or sequence coroutines.
                        quip.StopAllCoroutines();
                        // Destroy the quip's GameObject.
                        GameObject.Destroy(quip.gameObject);
                    }
                }
                // Clear the static list.
                activeQuips.Clear();
                TFTVLogger.Always($"Clear all called! TFTVNJQuestline.IntroMission.MissionQuips.QuipJustRun: {TFTVNJQuestline.IntroMission.MissionQuips.QuipJustRun}");
                TFTVNJQuestline.IntroMission.MissionQuips.QuipJustRun = false;
                TFTVLogger.Always($"after: TFTVNJQuestline.IntroMission.MissionQuips.QuipJustRun: {TFTVNJQuestline.IntroMission.MissionQuips.QuipJustRun}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
    }








    internal class TFTVTauntsAndQuips
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static List<Quip> Quips = new List<Quip>();

        public static QuipManager quipManager = null;

        private static Dictionary<TacticalActor, List<Quip>> _collectedQuips = new Dictionary<TacticalActor, List<Quip>>();

        public static void CollectQuips(TacticalActor quipper,
            QuipTrigger quipTrigger = QuipTrigger.NewTurn, QuipContext quipContext = QuipContext.None, QuipCondition quipCondition = QuipCondition.NoCondition,
            TacticalItemDef BodypartOrItemDef = null, TacStatusDef StatusDef = null, DamageKeywordDef DamageKeywordDef = null)
        {
            try
            {
                PPFactionDef pPFactionDef = quipper.TacticalFaction.TacticalFactionDef.FactionDef;
                int level = 0;
                bool evolved = quipper.BodyState.GetAllBodyparts().Any(ba => (bool)(ba?.OwnerItem?.TacticalItemDef?.Tags.Contains(Shared.SharedGameTags.EvolvedBodypartTag)));

                List<Quip> possibleQuips = Quips.Where(q => q.TacticalFaction == pPFactionDef && q.Trigger == quipTrigger &&
                (q.Context == QuipContext.Any || q.Context == quipContext) && q.Evolved == evolved && q.StatusDef == StatusDef &&
                q.BodypartOrItemDef == BodypartOrItemDef && q.Condition == quipCondition && q.DamageKeywordDef == DamageKeywordDef && quipper.HasGameTag(q.QuipperRelevantTag) && q.QuipperLevel == level).ToList();

                if (possibleQuips.Count > 0)
                {
                    foreach (var possibleQuip in possibleQuips)
                    {

                        if (!_collectedQuips.ContainsKey(quipper))
                        {
                            _collectedQuips.Add(quipper, new List<Quip> { possibleQuip });
                            TFTVLogger.Always($"added quip {possibleQuip.Text} to list of possible quips for {quipper?.DisplayName}");
                        }
                        else
                        {
                            if (!_collectedQuips[quipper].Contains(possibleQuip))
                            {
                                _collectedQuips[quipper].Add(possibleQuip);
                                TFTVLogger.Always($"added quip {possibleQuip.Text} to list of possible quips for {quipper?.DisplayName}");
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

        public static void SelectAndShowQuip(TacticalActor tacticalActor)
        {
            try
            {
                if (_collectedQuips.ContainsKey(tacticalActor) && _collectedQuips[tacticalActor].Count > 0)
                {
                    Quip quip = _collectedQuips[tacticalActor].GetRandomElement();

                    if (quipManager == null)
                    {
                        quipManager = new QuipManager();
                    }
                    if (quipManager.CanShowQuip())
                    {
                        FloatingTextManager.ShowFloatingText(tacticalActor, quip.Text, Color.white);

                    }
                    _collectedQuips.Clear();

                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }


        public static void PopulateQuipList()
        {
            try
            {
                TFTVLogger.Always($"running PopulateQuipList");

                bool keysRemain = true;

                for (int x = 0; keysRemain; x++)
                {
                    string keyId = $"Q-{x}";
                    string key = "";

                    foreach (var source in LocalizationManager.Sources)
                    {
                        var term = source.GetTermsList().FirstOrDefault(t => t.StartsWith(keyId, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(term))
                        {
                            // TFTVLogger.Always($"found {term}");
                            key = term;
                        }
                    }

                    if (key != "")
                    {
                        Quips.Add(new Quip(key));
                    }
                    else
                    {
                        keysRemain = false;
                    }
                }

                foreach (Quip quip in Quips)
                {
                    TFTVLogger.Always($"QUIP: {quip?.Key} {quip?.TacticalFaction?.GetName()} {quip?.Priority} Tag: {quip?.QuipperRelevantTag?.name}  {quip?.Trigger} {quip?.Context} {quip?.Condition} EVOLVED {quip?.Evolved}\n{quip?.Text}\n ID: {quip.Id}", false);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public class Quip
        {
            private string QuipperTagCode = "QTag";
            private string EvolvedCode = "Evolved";


            //Q_ALN_3_1_1_0_0

            public string Key { get; set; }
            public int Id { get; set; }
            public PPFactionDef TacticalFaction { get; set; }
            public QuipPriority Priority { get; set; }

            public QuipTrigger Trigger { get; set; }
            public QuipContext Context { get; set; }
            public QuipCondition Condition { get; set; }
            public bool Evolved { get; set; }
            public TacticalItemDef BodypartOrItemDef { get; set; }
            public TacStatusDef StatusDef { get; set; }
            public DamageKeywordDef DamageKeywordDef { get; set; }
            //   TacticalItemDef QuipperTacticalItemDef { get; set; }
            public GameTagDef QuipperRelevantTag { get; set; }
            public int QuipperLevel { get; set; }
            public string Text { get; set; }



            public Quip(string key)
            {
                Key = key;
                Text = TFTVCommonMethods.ConvertKeyToString(key);
                Id = ExtractIdFromKey(key);
                TacticalFaction = ExtractFactionFromKey(key);
                Priority = ExtractPriorityFromKey(key);
                Context = ExtractContextFromKey(key);
                Trigger = ExtractTriggerFromKey(key);
                Condition = ExtractConditionFromKey(key);
                Evolved = key.Contains(EvolvedCode);

                TFTVLogger.Always($"{key} has {QuipperTagCode}? {key.Contains(QuipperTagCode)}");

                if (key.Contains(QuipperTagCode))
                {
                    QuipperRelevantTag = ExtractRelevantGameTag(key);
                }

            }


            // Existing properties and constructor...


            private int ExtractIdFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    return int.TryParse(parts[1], out int id) ? id : 0;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private PPFactionDef ExtractFactionFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    string factionCode = parts[2];

                    switch (factionCode)
                    {
                        case "ALN":
                            return DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                        case "FO":
                            return DefCache.GetDef<PPFactionDef>("AN_FallenOnes_FactionDef");
                        case "ANC":
                            return DefCache.GetDef<PPFactionDef>("Ancients_FactionDef");
                        case "ANU":
                            return DefCache.GetDef<PPFactionDef>("Anu_FactionDef");
                        case "BAN":
                            return DefCache.GetDef<PPFactionDef>("NEU_Bandits_FactionDef");
                        case "NJ":
                            return DefCache.GetDef<PPFactionDef>("NJ_Purists_FactionDef");
                        case "PX":
                            return DefCache.GetDef<PPFactionDef>("Phoenix_FactionDef");
                        case "SYN":
                            return DefCache.GetDef<PPFactionDef>("Synedrion_FactionDef");
                        default:
                            return null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public QuipPriority ExtractPriorityFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    return Enum.TryParse(parts[3], out QuipPriority priority) ? priority : QuipPriority.Flavor;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public QuipTrigger ExtractTriggerFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    return Enum.TryParse(parts[4], out QuipTrigger trigger) ? trigger : QuipTrigger.Movement;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public QuipContext ExtractContextFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    return Enum.TryParse(parts[5], out QuipContext context) ? context : QuipContext.None;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public QuipCondition ExtractConditionFromKey(string key)
            {
                try
                {
                    string[] parts = key.Split('-');
                    return Enum.TryParse(parts[6], out QuipCondition condition) ? condition : QuipCondition.NoCondition;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public GameTagDef ExtractRelevantGameTag(string key)
            {
                try
                {
                    string[] parts = key.Split('-');

                    string gameTagDefName = parts[8];

                    TFTVLogger.Always($"{gameTagDefName}");

                    return DefCache.GetDef<GameTagDef>(parts[8]);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


        }


        public enum QuipPriority
        {
            Story,
            Hint,
            Flavor,
            EasterEgg
        }

        public enum QuipTrigger
        {
            NewTurn,
            Movement,
            Attack,
            Damage, //if different from standard damage, quip text must inlcude the Def of the damagekeyword in []
            Death,
            Panic,
            Status, //if status, quip text must include the Def of the status in [] to find it in the text.
            Detection,
            Ability,
            AnotherQuip
        }

        public enum QuipContext
        {
            None,
            Self,
            Friendly,
            Target,
            Enemy,
            Any
        }

        public enum QuipCondition
        {
            NoCondition,
            HighDamageDealt,
            LowDamageDealt,
            NoDamageDealt,
            DisabledBodyPart, //quip text must include the Def of the TacticalItemDef in []
            WeaponDestroyed,
            LowMorale,
            CharacterLevel,
            CharacterClass
        }

        public class QuipManager
        {
            private float lastQuipTime = 0f;
            private const float quipCooldown = 5f;  // 10-second cooldown

            public bool CanShowQuip()
            {
                try
                {
                    float currentTime = Time.time;  // Time.time gives elapsed time in seconds since the game started

                    if (currentTime - lastQuipTime >= quipCooldown)
                    {
                        TFTVLogger.Always($"Can show quip! currentTime: {currentTime} lastQuipTime: {lastQuipTime}");
                        lastQuipTime = currentTime;
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




        }



        public class FloatingText : MonoBehaviour
        {
            public Text TextComponent;  // Standard Text component
            public float Duration = 10f;
            public float FadeInTime = 0.5f;
            public float FadeOutTime = 0.5f;
            public Vector3 Offset = new Vector3(0, 2, 0); // Offset above UI or world position

            private float elapsedTime = 0f;
            private CanvasGroup canvasGroup;

            void Start()
            {
                try
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0; // Ensure it starts invisible
                    StartCoroutine(FadeInAndOut());
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public void SetText(string message, Color color)
            {
                try
                {
                    TextComponent.text = message;
                    TextComponent.color = color;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            IEnumerator FadeInAndOut()
            {
                float fadeDuration = FadeInTime + Duration + FadeOutTime;
                while (elapsedTime < fadeDuration)
                {
                    elapsedTime += Time.deltaTime;

                    if (elapsedTime <= FadeInTime)
                    {
                        canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / FadeInTime);
                    }
                    else if (elapsedTime > FadeInTime + Duration)
                    {
                        canvasGroup.alpha = Mathf.Lerp(1, 0, (elapsedTime - FadeInTime - Duration) / FadeOutTime);
                    }
                    yield return null;
                }
                Destroy(gameObject); // Remove text after animation
            }
        }

        public static class FloatingTextManager
        {
            public class FaceCamera : MonoBehaviour
            {
                void Update()
                {
                    if (Camera.main != null)
                    {
                        // Make the object face the camera but maintain its upright orientation
                        Vector3 direction = transform.position - Camera.main.transform.position;
                        direction.y = 0; // Lock the Y axis to prevent tilting
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
            }

            private static bool stopLoop = false;

            public static void StartFloatingTextLoop(DestructableBase destructable, List<string> messages, float interval, int randomStart)
            {
                stopLoop = false;
                GameUtl.CurrentLevel().StartCoroutine(ShowFloatingTextLoop(destructable, messages, interval, randomStart));
            }

            public static void StopFloatingTextLoop()
            {
                stopLoop = true;
            }

            private static IEnumerator ShowFloatingTextLoop(DestructableBase destructable, List<string> messages, float interval, int randomStart)
            {
                int index = randomStart;
                while (!stopLoop)
                {
                    ShowFloatingTextDestructableBase(destructable, messages[index]);
                    index = (index + 1) % messages.Count;
                    yield return new WaitForSeconds(interval);
                }
            }

            public static void ShowFloatingText(TacticalActor actor, string message, Color color)
            {
                try
                {
                    TFTVLogger.Always($"{actor.DisplayName}");

                    UIModuleNavigation uIModuleNavigation = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.NavigationModule;
                    ActorClassIconElement actorClassIconElement = actor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;

                    Transform transform = actorClassIconElement.MainClassIcon.transform;

                    TFTVLogger.Always($"actor.transform.parent: {actorClassIconElement.transform}");

                    GameObject textObject = CreateTextObjectForActors(transform);

                    FloatingText floatingText = textObject.GetComponent<FloatingText>();
                    floatingText.SetText(message, color); // Set text immediately
                    floatingText.transform.SetAsLastSibling();

                    //  Vector3 screenPosition = Camera.main.WorldToScreenPoint(actor.transform.position);
                    //   RectTransform rectTransform = textObject.GetComponent<RectTransform>();
                    //   rectTransform.sizeDelta = new Vector2(200, 100); // Set size for better visibility
                    //  rectTransform.anchoredPosition = new Vector2(0, rectTransform.sizeDelta.y); // Position above the parent


                    // Ensure the text object is active and visible
                    textObject.SetActive(true);
                    // textObject.transform.position = screenPosition;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            public static void ShowFloatingTextDestructableBase(DestructableBase destructable, string message)
            {
                try
                {
                    //  TFTVLogger.Always($"{destructable.name}");

                    Color color = Color.cyan;


                    Transform transform = destructable.transform;

                    //  TFTVLogger.Always($"actor.transform.parent: {actorClassIconElement.transform}");

                    GameObject textObject = CreateTextObjectForEnvironment(transform);

                    FloatingText floatingText = textObject.GetComponent<FloatingText>();
                    floatingText.SetText(message, color); // Set text immediately

                    RectTransform rectTransform = textObject.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(200, 100); // Set size for better visibility
                                                                     // rectTransform.anchoredPosition = new Vector2(0, rectTransform.sizeDelta.y); // Position above the parent


                    // Ensure the text object is active and visible
                    textObject.SetActive(true);
                    //textObject.transform.position = screenPosition;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static GameObject CreateTextObjectForEnvironment(Transform hologramTransform)
            {
                try
                {
                    // Ensure there's a Canvas in the scene
                    GameObject canvasObj = GameObject.Find("FloatingTextCanvas");
                    if (canvasObj == null)
                    {
                        canvasObj = new GameObject("FloatingTextCanvas");
                        Canvas canvas = canvasObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.WorldSpace; // Use WorldSpace for positioning near 3D objects
                        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                        scaler.dynamicPixelsPerUnit = 1000; // Adjust as needed
                        canvasObj.AddComponent<GraphicRaycaster>();

                        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
                        canvasGroup.interactable = false; // Prevent blocking UI input
                        canvasGroup.blocksRaycasts = false; // Allow clicks to pass through
                        canvasGroup.ignoreParentGroups = true;
                    }

                    // Create the text object
                    GameObject textObj = new GameObject("FloatingText");
                    RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                    rectTransform.SetParent(canvasObj.transform, false); // Parent to the Canvas

                    // Set the size of the RectTransform
                    rectTransform.sizeDelta = new Vector2(200, 100);

                    // Set the text object position near the hologram in World Space
                    Vector3 worldPosition = hologramTransform.position + new Vector3(0, 4, 0); // Offset above the hologram
                    rectTransform.position = worldPosition;

                    // Ensure the text faces the camera
                    //   rectTransform.rotation = Quaternion.LookRotation(rectTransform.position - Camera.main.transform.position);//new Quaternion(0, 0, 0, -0.5f);//

                    // Add and configure the Text component
                    Text textComponent = textObj.AddComponent<Text>();
                    textComponent.font = TFTVUITactical.PuristaSemiboldFontCache ?? UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
                    textComponent.fontSize = 7;
                    textComponent.alignment = TextAnchor.MiddleCenter;
                    textComponent.color = Color.white; // Ensure text color is visible
                    textComponent.text = "Floating Text"; // Default text

                    // Add an Outline component for the black outline
                    Outline outline = textObj.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(0.001f, -0.001f);

                    // Optional: Add a FloatingText script for additional behavior
                    FloatingText floatingText = textObj.AddComponent<FloatingText>();
                    floatingText.TextComponent = textComponent;

                    textObj.AddComponent<FaceCamera>();
                    // Ensure the text object is active and visible
                    textObj.SetActive(true);

                    return textObj;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static GameObject CreateTextObjectForActors(Transform parent)
            {
                try
                {
                    GameObject textObj = new GameObject("FloatingText");



                    RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                    rectTransform.SetParent(parent, false);
                    rectTransform.sizeDelta = new Vector2(980, 200);
                    rectTransform.anchoredPosition += new Vector2(75, 100);
                    rectTransform.localScale = new Vector3(0.5f, 0.5f, 0.5f);


                    Text textComponent = textObj.AddComponent<Text>();
                    textComponent.font = TFTVUITactical.PuristaSemiboldFontCache ?? UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
                    textComponent.fontSize = 76;
                    textComponent.alignment = TextAnchor.UpperCenter;
                    textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                    textComponent.color = TFTVUITactical.WhiteColor; // Ensure text color is visible

                    /* Outline outline = textObj.AddComponent<Outline>();
                     outline.effectColor = Color.black;
                     outline.effectDistance = new Vector2(2, -2);*/


                    FloatingText floatingText = textObj.AddComponent<FloatingText>();
                    floatingText.TextComponent = textComponent;

                    // Ensure the text object is active and visible
                    textObj.SetActive(true);

                    return textObj;
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



