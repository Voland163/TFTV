using Base;
using Base.Core;
using Base.Defs;
using Base.Rendering.ObjectRendering;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using PhoenixPoint.Tactical.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewControllers.SquadMemberScrollerController;

namespace TFTV
{
    internal class TFTVExperimentalNext
    {

        



        /*   public class SpriteCombiner : MonoBehaviour
           {
               public Sprite faceSprite;  // Assign this in the inspector
               public Sprite helmetSprite;  // Assign this in the inspector

               void Start()
               {
                   Sprite combinedSprite = CombineSprites(faceSprite, helmetSprite);

                   // Example usage: assign the combined sprite to a sprite renderer
                   GetComponent<SpriteRenderer>().sprite = combinedSprite;
               }

               Sprite CombineSprites(Sprite face, Sprite helmet)
               {
                   // Ensure the sprites are the same size
                   if (face.rect.size != helmet.rect.size)
                   {
                       Debug.LogError("Sprites must be the same size!");
                       return null;
                   }

                   // Create a secondary sprite texture array
                   var secondarySpriteTexture = new[]
                   {
               new SecondarySpriteTexture()
               {
                   name = "_SecondaryTexture1",
                   texture = helmet.texture
               }
           };

                   int width = (int)face.rect.width;
                   int height = (int)face.rect.height;



                   // Create a new combined sprite
                  return Sprite.Create(face.texture, new Rect(0, 0, width, height), Vector2.zero, 100, 0, SpriteMeshType.FullRect, Vector4.zero, false, secondarySpriteTexture);
               }
           }*/

        // SoldierPortraitUtil

        public static Texture2D RenderSoldier(GameObject soldierToRender, Vector2Int dimensions)
        {
            try
            {

                Texture2D texture2D = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, mipChain: true);
                RenderingEnvironment renderingEnvironment = new RenderingEnvironment(new Vector2Int(dimensions.x, dimensions.y), RenderingEnvironmentOption.NoBackground);
                ObjectRenderer objectRenderer = new ObjectRenderer(renderingEnvironment);
                SoldierFrame cameraFrameLogic = new SoldierFrame(objectRenderer.StageObject(soldierToRender).transform, 5f, 5f, 2f);
                objectRenderer.Render(cameraFrameLogic);
                renderingEnvironment.WriteResultsToTexture(texture2D);
                return texture2D;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }




        /* [HarmonyPatch(typeof(UIModuleCorruptionReport), "Init")]
         public static class UIModuleCorruptionReport_Init_patch
         {
             public static void Postfix(UIModuleCorruptionReport __instance, GeoscapeViewContext context)
             {
                 try
                 {
                     UIModuleTimeControl uIModuleTimeControl = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TimeControlModule;

                 /    foreach(Transform transform in uIModuleTimeControl.GetComponentsInChildren<Transform>().Where(t=>t.GetComponent<Image>()!=null))

                     {
                         TFTVLogger.Always($"{transform.name}, image name: {transform.GetComponent<Image>().name}");

                     }/

                     foreach (RectTransform rectTransform in __instance.GetComponentsInChildren<RectTransform>())
                     {
                      //   TFTVLogger.Always($"{rectTransform.name}");
                         if (rectTransform.name.Contains("UIElement") || rectTransform.name.Contains("Gradient") || rectTransform.name.Contains("StreaksGroup") || rectTransform.name.Contains("Line"))
                         {
                             rectTransform.gameObject.SetActive(false);
                         }
                         else if (rectTransform.name.Contains("Title"))
                         {
                             // Adjust the title if necessary
                         }
                     }

                     foreach (Text text in __instance.GetComponentsInChildren<Text>())
                     {
                         TFTVLogger.Always($"text {text.name}");
                         if (text.name.Equals("Title"))
                         {

                             string folderPath = Path.Combine(TFTVMain.ModDirectory, "Assets", "Textures", "Portraits");
                             Texture2D portraitTexture = LoadTextureFromFile($"{folderPath}/f_1_1_12_0_9.png");
                             // Define the rectangle for cropping
                             Rect croppedRect = new Rect(44, 20, 86, 100);

                             // Create the cropped sprite
                             Sprite croppedSprite = Sprite.Create(portraitTexture, croppedRect, new Vector2(0.5f, 0.5f));


                             // Set the text properties
                             text.text = "Hello! My name is Alistair Ashby, and you may remember me from other quotes";
                             text.color = Color.white;
                             text.resizeTextMaxSize = 100;
                             text.alignment = TextAnchor.MiddleCenter;
                             text.fontSize = 100;
                             text.rectTransform.anchoredPosition = new Vector2(-250, -400);  // Adjust position
                             text.rectTransform.sizeDelta = new Vector2(400, 200);

                             // Create a container to hold the background, portrait, and text
                             GameObject container = new GameObject("Container");
                             container.transform.SetParent(text.transform.parent);
                             RectTransform containerRectTransform = container.AddComponent<RectTransform>();
                             containerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                             containerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                             containerRectTransform.pivot = new Vector2(0.5f, 0.5f);
                             containerRectTransform.anchoredPosition = text.rectTransform.anchoredPosition;  // Same position as text
                             containerRectTransform.sizeDelta = new Vector2(400, 200);  // Adjust size as needed

                             // Re-parent text to the container
                             text.transform.SetParent(container.transform);

                             // Create a black background panel for the text
                             GameObject background = new GameObject("Background");
                             background.transform.SetParent(container.transform);
                             Image backgroundImage = background.AddComponent<Image>();

                             // Load and check the background sprite
                             Sprite backgroundSprite = Helper.CreateSpriteFromImageFile("text_background.png");
                             backgroundImage.sprite = backgroundSprite;

                             // Adjust the size and position of the text background panel
                             RectTransform backgroundRectTransform = background.GetComponent<RectTransform>();
                             backgroundRectTransform.sizeDelta = new Vector2(400, 120);
                             backgroundRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                             backgroundRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                             backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
                             backgroundRectTransform.anchoredPosition = new Vector2(-50, 0);  // Adjust position as needed
                             backgroundRectTransform.sizeDelta = new Vector2(700, 120);  // Adjust size as needed
                             // Create the portrait
                             GameObject portrait = new GameObject("Portrait");
                             portrait.transform.SetParent(container.transform);
                             Image portraitImage = portrait.AddComponent<Image>();
                             portraitImage.sprite = croppedSprite;

                             // Adjust the scale and position of the portrait image
                             RectTransform portraitRectTransform = portrait.GetComponent<RectTransform>();
                             portraitRectTransform.sizeDelta = new Vector2(croppedRect.width, croppedRect.height);
                             portraitRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                             portraitRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                             portraitRectTransform.pivot = new Vector2(0.5f, 0.5f);
                             portraitRectTransform.anchoredPosition = new Vector2(-300, 0);  // Adjust position as needed

                             // Set sibling indexes to ensure correct rendering order
                             background.transform.SetSiblingIndex(0);
                             portrait.transform.SetSiblingIndex(1);
                             text.transform.SetSiblingIndex(2);

                             // Start the fade-in coroutine
                             // __instance.StartCoroutine(FadeIn(text, portraitImage, backgroundImage, 5f)); // Fade in over 2 seconds

                             // Start the fade-out coroutine after a delay
                             // __instance.StartCoroutine(FadeOut(text, portraitImage, backgroundImage, 2f, 10f)); // Fade out over 2 seconds after a delay of 5 seconds
                         }
                         else
                         {
                             text.gameObject.SetActive(false);
                         }
                     }

                     __instance.StatusReportButton.gameObject.SetActive(false);
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

             private static Texture2D LoadTextureFromFile(string filePath)
             {
                 byte[] fileData = File.ReadAllBytes(filePath);
                 Texture2D texture = new Texture2D(2, 2);
                 texture.LoadImage(fileData); // Automatically resizes the texture dimensions
                 return texture;
             }

             private static IEnumerator FadeIn(Text text, Image portrait, Image background, float duration)
             {
                 float elapsedTime = 0f;
                 Color textColor = text.color;
                 Color portraitColor = portrait.color;
                 Color backgroundColor = background.color;

                 while (elapsedTime < duration)
                 {
                     elapsedTime += Time.deltaTime;
                     float alpha = Mathf.Clamp01(elapsedTime / duration);
                     text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                     portrait.color = new Color(portraitColor.r, portraitColor.g, portraitColor.b, alpha);
                     background.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, alpha);
                     yield return null;
                 }
             }

             private static IEnumerator FadeOut(Text text, Image portrait, Image background, float duration, float delay)
             {
                 yield return new WaitForSeconds(delay);

                 float elapsedTime = 0f;
                 Color textColor = text.color;
                 Color portraitColor = portrait.color;
                 Color backgroundColor = background.color;

                 while (elapsedTime < duration)
                 {
                     elapsedTime += Time.deltaTime;
                     float alpha = Mathf.Clamp01(1 - (elapsedTime / duration));
                     text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                     portrait.color = new Color(portraitColor.r, portraitColor.g, portraitColor.b, alpha);
                     background.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, alpha);
                     yield return null;
                 }
             }
         }*/
    }
}
