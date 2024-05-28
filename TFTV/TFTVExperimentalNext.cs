using Base.Defs;
using Base.Rendering.ObjectRendering;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVExperimentalNext
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


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
            try { 
            




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
                     foreach (RectTransform rectTransform in __instance.GetComponentsInChildren<RectTransform>())
                     {
                         TFTVLogger.Always($"{rectTransform.name}");
                         if (rectTransform.name.Contains("UIElement") ||
                             rectTransform.name.Contains("Gradient") || rectTransform.name.Contains("StreaksGroup") || rectTransform.name.Contains("Line"))
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
                             text.text = "Hello! My name is Alistair Ashby, and you may remember me from other quotes";
                             text.color = Color.white;
                             text.resizeTextMaxSize = 100;
                             text.alignment = TextAnchor.UpperLeft;
                             text.fontSize = 200;  // Increased font size for bigger text


                            // textRectTransform.anchoredPosition = new Vector2(500, 0); // Set the position of the text box

                             // Move text to the right to make space for the image
                             text.rectTransform.Translate(-50, 0, 0);  // Adjust this value if needed

                             // Create and position the new image to the left of the text
                             Image newImage = UnityEngine.Object.Instantiate(__instance.StatusReportButton.GetComponentInChildren<Image>(), text.transform);
                             newImage.sprite = Helper.CreateSpriteFromImageFile("alistair.jpg");

                             // Adjust the scale and position of the new image
                             RectTransform imageRectTransform = newImage.GetComponent<RectTransform>();
                             imageRectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);  // Adjust the scale for a bigger image
                             imageRectTransform.anchoredPosition = new Vector2(-520, 0);  // Position the image to the left of the text

                             RectTransform textRectTransform = text.GetComponent<RectTransform>();
                             textRectTransform.sizeDelta = new Vector2(150, 150); // Set the size of the text box
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
         }
        */

        /*  [HarmonyPatch(typeof(UIModuleCorruptionReport), "Init")]
          public static class UIModuleCorruptionReport_Init_patch
          {
              public static void Postfix(UIModuleCorruptionReport __instance, GeoscapeViewContext context)
              {
                  try
                  {
                      foreach (RectTransform rectTransform in __instance.GetComponentsInChildren<RectTransform>())
                      {
                          TFTVLogger.Always($"{rectTransform.name}");
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
                              text.text = "Hello! My name is Alistair Ashby, and you may remember me from other quotes";
                              text.color = Color.white;
                              text.resizeTextMaxSize = 100;
                              text.alignment = TextAnchor.UpperLeft;
                              text.fontSize = 200;  // Increased font size for bigger text


                              // textRectTransform.anchoredPosition = new Vector2(500, 0); // Set the position of the text box

                              // Move text to the right to make space for the image
                              text.rectTransform.Translate(-50, 0, 0);  // Adjust this value if needed

                              // Create and position the new image to the left of the text
                              Image newImage = UnityEngine.Object.Instantiate(__instance.StatusReportButton.GetComponentInChildren<Image>(), text.transform);
                              newImage.sprite = Helper.CreateSpriteFromImageFile("alistair.jpg");

                              // Adjust the scale and position of the new image
                              RectTransform imageRectTransform = newImage.GetComponent<RectTransform>();
                              imageRectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);  // Adjust the scale for a bigger image
                              imageRectTransform.anchoredPosition = new Vector2(-520, 0);  // Position the image to the left of the text

                              RectTransform textRectTransform = text.GetComponent<RectTransform>();
                              textRectTransform.sizeDelta = new Vector2(150, 150); // Set the size of the text box
                              newImage.color = new Color(newImage.color.r, newImage.color.g, newImage.color.b, 0); // Set initial alpha to 0

                              // Start the fade-in coroutine
                              __instance.StartCoroutine(FadeIn(text, newImage, 5f)); // Fade in over 2 seconds

                              // Start the fade-out coroutine after a delay
                            //  __instance.StartCoroutine(FadeOut(text, newImage, 2f, 5f)); // Fade out over 2 seconds after a delay of 5 seconds
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

              private static IEnumerator FadeIn(Text text, Image image, float duration)
              {
                  float elapsedTime = 0f;
                  Color textColor = text.color;
                  Color imageColor = image.color;

                  while (elapsedTime < duration)
                  {
                      elapsedTime += Time.deltaTime;
                      float alpha = Mathf.Clamp01(elapsedTime / duration);
                      text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                      image.color = new Color(imageColor.r, imageColor.g, imageColor.b, alpha);
                      yield return null;
                  }
              }

              private static IEnumerator FadeOut(Text text, Image image, float duration, float delay)
              {
                  yield return new WaitForSeconds(delay);

                  float elapsedTime = 0f;
                  Color textColor = text.color;
                  Color imageColor = image.color;

                  while (elapsedTime < duration)
                  {
                      elapsedTime += Time.deltaTime;
                      float alpha = Mathf.Clamp01(1 - (elapsedTime / duration));
                      text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                      image.color = new Color(imageColor.r, imageColor.g, imageColor.b, alpha);
                      yield return null;
                  }
              }
          }*/



    }
}
