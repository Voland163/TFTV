using Base;
using Base.Core;
using Base.Defs;
using Base.Rendering.ObjectRendering;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
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

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        public class CharacterPortrait
        {

            public static Dictionary<int, string> characterPics = new Dictionary<int, string>();


            private static List<string> _portraitFileList = new List<string>();

            public static void PopulateCharacterPics(GeoLevelController controller)
            {
                try
                {
                    if (!config.CustomPortraits)
                    {
                        return;
                    }

                    characterPics.Clear();

                    foreach (GeoCharacter geoCharacter in controller.PhoenixFaction.HumanSoldiers.Where(gc => CheckCharacterHumanHead(gc) && !characterPics.ContainsKey(gc.Id)))
                    {
                        if (characterPics.Count() == _portraitFileList.Count())
                        {
                            return;
                        }

                        if (geoCharacter.Identity.FaceTag == DefCache.GetDef<FaceTagDef>("Sophia_FaceTagDef"))
                        {
                            characterPics.Add(geoCharacter.Id, "Sophia_Brown.jpg");
                            continue;
                        }
                        else if (geoCharacter.Identity.FaceTag == DefCache.GetDef<FaceTagDef>("Jacob_FaceTagDef"))
                        {
                            characterPics.Add(geoCharacter.Id, "Jacob_Eber.jpg");
                            continue;
                        }

                        string portraitFilename = FindBestMatchingPortrait(geoCharacter.GameTags.ToList(), ExtractGenderFromCharater(geoCharacter.Identity.SexTag));
                        if (portraitFilename != null)
                        {
                            characterPics.Add(geoCharacter.Id, portraitFilename);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static void PopulatePortraitFileList()
            {
                try
                {
                    if (!config.CustomPortraits)
                    {
                        return;
                    }

                    string folderPath = Path.Combine(TFTVMain.ModDirectory, "Assets", "Textures", "Portraits");

                    // Get all .jpg files in the specified folder
                    string[] files = Directory.GetFiles(folderPath, "*.jpg");

                    // Clear the list before adding new filenames
                    _portraitFileList.Clear();

                    // Add each filename to the _portraitFileList
                    foreach (string file in files)
                    {
                        _portraitFileList.Add(Path.GetFileName(file));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static int[] ExtractPortraitDataFromTags(List<GameTagDef> tags)
            {
                try
                {
                    HumanCustomizationDef humanCustomizationDef = DefCache.GetDef<HumanCustomizationDef>("HumanCustomizationDef");
                    int[] portraitData = new int[5];

                    foreach (GameTagDef tagDef in tags)
                    {
                        if (humanCustomizationDef.RaceTags.Contains(tagDef))
                        {
                            portraitData[0] = humanCustomizationDef.RaceTags.IndexOf(tagDef) + 1;
                        }
                        else if (humanCustomizationDef.FaceTags.Contains(tagDef))
                        {
                            portraitData[1] = humanCustomizationDef.FaceTags.IndexOf(tagDef) + 1;
                        }
                        else if (humanCustomizationDef.HairTags.Contains(tagDef))
                        {
                            portraitData[2] = humanCustomizationDef.HairTags.IndexOf(tagDef) + 1;
                        }
                        else if (humanCustomizationDef.FacialHairTags.Contains(tagDef))
                        {
                            portraitData[3] = humanCustomizationDef.FacialHairTags.IndexOf(tagDef) + 1;
                        }
                        else if (humanCustomizationDef.HairColorTags.Contains(tagDef))
                        {
                            portraitData[4] = humanCustomizationDef.HairColorTags.IndexOf(tagDef) + 1;
                        }
                    }

                    return portraitData;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool CheckCharacterHumanHead(GeoCharacter geoCharacter)
            {
                try
                {
                    if (geoCharacter == null)
                    {
                        return false;
                    }

                    if (!geoCharacter.TemplateDef.IsHuman)
                    {
                        return false;
                    }


                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                    GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                    ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

                    if (geoCharacter.ArmourItems.Any(item => item.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot
                    && (item.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || item.CommonItemData.ItemDef.Tags.Contains(mutationTag))))
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

            public static string ExtractGenderFromCharater(GameTagDef tag)
            {
                try
                {


                    if (tag.name.Equals("Female_GenderTagDef"))
                    {
                        return "f";
                    }
                    else
                    {
                        return "m";
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static string FindBestMatchingPortrait(List<GameTagDef> tags, string gender)
            {
                try
                {
                    HumanCustomizationDef humanCustomizationDef = DefCache.GetDef<HumanCustomizationDef>("HumanCustomizationDef");
                    int[] providedTags = ExtractPortraitDataFromTags(tags);

                    string bestMatch = null;
                    int bestMatchScore = int.MaxValue;

                    foreach (string filename in _portraitFileList.Where(fn => fn.StartsWith(gender) && !characterPics.ContainsValue(fn)))
                    {
                        string[] parts = Path.GetFileNameWithoutExtension(filename).Split('_');
                        if (parts.Length != 6) continue;

                        int[] fileTags = parts.Skip(1).Take(5).Select(int.Parse).ToArray();
                        int currentScore = CalculateTagScore(providedTags, fileTags);

                        if (currentScore < bestMatchScore)
                        {
                            bestMatchScore = currentScore;
                            bestMatch = filename;
                        }
                    }

                    return bestMatch;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static int CalculateTagScore(int[] providedTags, int[] fileTags)
            {
                try
                {
                    // Define weights for each tag type
                    int[] weights = { 5, 4, 3, 2, 1 };
                    int score = 0;

                    for (int i = 0; i < providedTags.Length; i++)
                    {
                        score += weights[i] * Math.Abs(providedTags[i] - fileTags[i]);
                    }

                    return score;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }




        public static bool NewCharacterPortraitInSetupProperPortrait(TacticalActor actor,
                Dictionary<TacticalActor, PortraitSprites> _soldierPortraits, SquadMemberScrollerController squadMemberScrollerController)
        {
            try
            {

                if (config.CustomPortraits)
                {
                    MethodInfo tryFindFakePortraitMethod = typeof(SquadMemberScrollerController).GetMethod("TryFindFakePortrait", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo updatePortraitForSoldierMethod = typeof(SquadMemberScrollerController).GetMethod("UpdatePortraitForSoldier", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (CharacterPortrait.characterPics.ContainsKey(actor.GeoUnitId))
                    {
                        PortraitSprites portraitSprites = _soldierPortraits[actor];
                        TFTVLogger.Always($"ForceSpecialCharacterPortraitInSetupProperPortrait actor is {actor.name}");

                        Sprite sprite = Helper.CreatePortraitFromImageFile(CharacterPortrait.characterPics[actor.GeoUnitId]);

                        portraitSprites.Portrait = sprite;
                        updatePortraitForSoldierMethod.Invoke(squadMemberScrollerController, new object[] { actor });

                        return false;

                    }
                }
                return TFTVPalaceMission.MissionObjectives.ForceSpecialCharacterPortraitInSetupProperPortrait(actor, _soldierPortraits, squadMemberScrollerController);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


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
