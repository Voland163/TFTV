using Base.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVTauntsAndQuips
    {

        public class Quip
        {
            public string Key { get; set; }
            public int Id { get; set; }
            public string TacticalFaction { get; set; }
            public QuipPriority Priority { get; set; }
            public QuipTrigger Trigger { get; set; }
            public TacticalActor Speaker { get; set; }
            public TacticalActor Target { get; set; }
            public TacticalItem Item { get; set; }
            public string Text { get; set; }

            public Quip(string key, string text)
            {
                Key = key;
                Text = text;
                Id = ExtractIdFromKey(key);
                TacticalFaction = ExtractFactionFromKey(key);
            }

            private int ExtractIdFromKey(string key)
            {
                // Implement logic to extract Id from key
                // Example: if key is "Faction_123", extract 123
                string[] parts = key.Split('_');
                return int.TryParse(parts[1], out int id) ? id : 0;
            }

            private string ExtractFactionFromKey(string key)
            {
                // Implement logic to extract TacticalFaction from key
                // Example: if key is "Faction_123", extract "Faction"
                string[] parts = key.Split('_');
                return parts[0];
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
            Damage,
            Death,
            Panic,
            Status,
            Detection,
            AnotherQuip
        }

        public enum QuipContext 
        { 
        None,
        Self, 
        Friendly, 
        Target,
        Enemy 
        }

        public enum QuipConditions 
        { 
            NoCondition,
            HighDamageDealt, 
            LowDamageDealt,
            NoDamageDealt,
            DisabledHead,
            DisabledArm,
            DisabledLeg,
            HighValueCharacterKilled,
            LowValueCharacterKilled,
            LowMorale


        } 

        public class QuipManager
        {
            private float lastQuipTime = 0f;
            private const float quipCooldown = 5f;  // 10-second cooldown

            public bool CanShowQuip(string quip)
            {
                try
                {
                    float currentTime = Time.time;  // Time.time gives elapsed time in seconds since the game started

                    if (currentTime - lastQuipTime >= quipCooldown)
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


        }



        public class FloatingText : MonoBehaviour
        {
            public Text TextComponent;  // Standard Text component
            public float Duration = 4f;
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
            public static void ShowFloatingText(TacticalActor actor, string message, Color color)
            {
                try
                {
                    TFTVLogger.Always($"{actor.DisplayName}");

                    UIModuleNavigation uIModuleNavigation = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.NavigationModule;
                    ActorClassIconElement actorClassIconElement = actor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;

                    Transform transform = actorClassIconElement.MainClassIcon.transform;

                    TFTVLogger.Always($"actor.transform.parent: {actorClassIconElement.transform}");

                    GameObject textObject = CreateTextObject(transform);

                    FloatingText floatingText = textObject.GetComponent<FloatingText>();
                    floatingText.SetText(message, color); // Set text immediately

                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(actor.transform.position);
                    RectTransform rectTransform = textObject.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(200, 100); // Set size for better visibility
                                                                     // rectTransform.anchoredPosition = new Vector2(0, rectTransform.sizeDelta.y); // Position above the parent


                    // Ensure the text object is active and visible
                    textObject.SetActive(true);
                    // textObject.transform.position = screenPosition;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static GameObject CreateTextObject(Transform parent)
            {
                try
                {
                    GameObject textObj = new GameObject("FloatingText");
                    RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                    rectTransform.SetParent(parent, false);

                    // Set the size of the RectTransform
                    rectTransform.sizeDelta = new Vector2(200, 100);

                    // Set the anchors to the middle of the parent
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    // Set the position above the parent
                    rectTransform.anchoredPosition += new Vector2(-50, -200);

                    Text textComponent = textObj.AddComponent<Text>();
                    textComponent.font = TFTVUITactical.PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    textComponent.fontSize = 36;
                    textComponent.alignment = TextAnchor.MiddleCenter;
                    textComponent.color = Color.white; // Ensure text color is visible

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



