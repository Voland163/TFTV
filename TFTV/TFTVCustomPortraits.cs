using Base;
using Base.Core;
using Base.Defs;
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
    internal class TFTVCustomPortraits
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        public class CharacterPortrait
        {

            public static void ClearPortraitData()
            {
                try
                {
                    characterPics.Clear();
                    TFTVLogger.Always($"Portraits list cleared");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static Dictionary<int, string> characterPics = new Dictionary<int, string>();
            private static List<string> _portraitFileList = new List<string>();

            [HarmonyPatch(typeof(UIModuleUnitCustomization), "ChangeCustomization")]
            public static class UIModuleUnitCustomization_ChangeCustomization_patch
            {

                public static void Postfix(UIModuleUnitCustomization __instance, CustomizationTagDef newTag)
                {
                    try
                    {
                        if (!config.CustomPortraits)
                        {
                            return;
                        }

                        UIModuleActorCycle uIModuleActorCycle = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                        GeoCharacter geoCharacter = uIModuleActorCycle.CurrentCharacter;




                        if (geoCharacter != null) //&& CheckCharacterHumanHead(geoCharacter))
                        {
                            if (characterPics.ContainsKey(geoCharacter.Id))
                            {
                                if (!_portraitFileList.Contains(characterPics[geoCharacter.Id]))
                                {
                                    _portraitFileList.Add(characterPics[geoCharacter.Id]);
                                }

                                characterPics.Remove(geoCharacter.Id);
                            }

                            if (CheckCharacterHumanHead(geoCharacter))
                            {
                                FindPictureForCharacter(geoCharacter);
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

            private static void FindPictureForCharacter(GeoCharacter geoCharacter)
            {
                try
                {
                    if (geoCharacter.Identity.FaceTag == DefCache.GetDef<FaceTagDef>("Sophia_FaceTagDef") && CheckCharacterHumanHead(geoCharacter))
                    {
                        characterPics.Add(geoCharacter.Id, "Sophia_Brown.png");
                        return;
                    }
                    else if (geoCharacter.Identity.FaceTag == DefCache.GetDef<FaceTagDef>("Jacob_FaceTagDef") && CheckCharacterHumanHead(geoCharacter))
                    {
                        characterPics.Add(geoCharacter.Id, "Jacob_Eber.png");
                        return;
                    }

                    string portraitFilename = FindBestMatchingPortrait(geoCharacter.GameTags.ToList(), ExtractGenderFromCharater(geoCharacter.Identity.SexTag));
                    if (portraitFilename != null)
                    {
                        characterPics.Add(geoCharacter.Id, portraitFilename);
                        _portraitFileList.Remove(portraitFilename);
                        string tags = string.Join(", ", ExtractPortraitDataFromTags(geoCharacter.GameTags.ToList()));

                        TFTVLogger.Always($"{geoCharacter.DisplayName} with tags {tags} got the pic {portraitFilename}");
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

            public static void PopulateCharacterPics(GeoLevelController controller)
            {
                try
                {
                    if (!config.CustomPortraits)
                    {
                        return;
                    }

                    if (characterPics == null)
                    {
                        characterPics = new Dictionary<int, string>();
                    }

                    //    TFTVLogger.Always($"characterPics.Keys.Count {characterPics.Keys.Count}");

                    // characterPics.Clear();

                    foreach (GeoCharacter geoCharacter in controller.PhoenixFaction.HumanSoldiers.Where(gc => CheckCharacterHumanHead(gc) && !characterPics.ContainsKey(gc.Id)))
                    {
                        if (characterPics.Count() == _portraitFileList.Count())
                        {
                            return;
                        }

                        FindPictureForCharacter(geoCharacter);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CheckForAlreadyTakenPortraits()
            {
                try
                {
                    List<string> portraitsAlreadyTaken = new List<string>();

                    if (characterPics != null && characterPics.Values.Count > 0)
                    {
                        portraitsAlreadyTaken.AddRange(characterPics.Values);

                        foreach (string portrait in portraitsAlreadyTaken)
                        {
                            _portraitFileList.Remove(portrait);
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
                    string[] files = Directory.GetFiles(folderPath, "*.png");// "*.jpg");

                    // Clear the list before adding new filenames
                    _portraitFileList.Clear();

                    // Add each filename to the _portraitFileList
                    foreach (string file in files)
                    {
                        _portraitFileList.Add(Path.GetFileName(file));
                    }

                    CheckForAlreadyTakenPortraits();

                    TFTVLogger.Always($"There are {_portraitFileList.Count} picture files available");
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

                    TFTVLogger.Always($"{geoCharacter.DisplayName} can get portrait");

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
                        int currentScore = CalculateTagScore(providedTags, fileTags, gender);

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

            private static int CalculateTagScore(int[] providedTags, int[] fileTags, string gender)
            {
                try
                {
                    // Define weights for each tag type
                    int[] weights = { 5, 4, 3, 2, 1 };
                    int score = 0;

                    //if character is female, facial hair tag doesn't matter
                    if (gender.StartsWith("f"))
                    {
                        weights[3] = 0;
                    }

                    //if character has no hair, hair color doesn't matter
                    if (providedTags[2] == 1 && ((providedTags[3] == 1 || gender.StartsWith("f"))))
                    {
                        weights[4] = 0;
                    }

                    //female hair 3 and 14 is the same one
                    if (gender.StartsWith("f") && (providedTags[2] == 3 || providedTags[2] == 14) && (fileTags[2] == 3 || fileTags[2] == 14))
                    {
                        fileTags[2] = providedTags[2];
                    }

                    for (int i = 0; i < providedTags.Length; i++)
                    {

                        //best grade is 0;
                        //a grade of 1 means "similar" tags;
                        //a grade of 2 means tags are different;
                        //a grade of 3 means tags are very different; 

                        int grade = 2;


                        if (Math.Abs(providedTags[i] - fileTags[i]) == 0) //same tag
                        {
                            grade = 0;
                        }
                        else if (i == 2 && providedTags[i] != 1 && fileTags[i] == 1) //character has hair, and the pic has no hair, double penalty
                        {
                            grade += 1;
                        }
                        else if (i == 2 && gender.StartsWith("f")) //female 5 and 6 are pretty close; 8 and 9; 10, 11 and 12; 7 and 16
                        {
                            if (
                                ((providedTags[2] == 5 || providedTags[2] == 6) && (fileTags[2] == 5 || fileTags[2] == 6)) ||
                                ((providedTags[2] == 8 || providedTags[2] == 9) && (fileTags[2] == 8 || fileTags[2] == 9)) ||
                                ((providedTags[2] == 7 || providedTags[2] == 16) && (fileTags[2] == 7 || fileTags[2] == 16)) ||
                                ((providedTags[2] == 10 || providedTags[2] == 11 || providedTags[2] == 12)
                                && (fileTags[2] == 10 || fileTags[2] == 11 || fileTags[2] == 12))
                                )

                            {
                                grade = 1;
                            }
                        }
                        else if (i == 2 && gender.StartsWith("m")) //male 2 and 3; 6, 9 and 10; 7, 11, 12 and 13; 
                        {
                            if (
                                ((providedTags[2] == 2 || providedTags[2] == 3) && (fileTags[2] == 2 || fileTags[2] == 3)) ||
                                ((providedTags[2] == 6 || providedTags[2] == 9 || providedTags[2] == 10)
                                && (fileTags[2] == 6 || fileTags[2] == 9 || fileTags[2] == 10)) ||
                                ((providedTags[2] == 7 || providedTags[2] == 11 || providedTags[2] == 12 || providedTags[2] == 13)
                                && (fileTags[2] == 7 || fileTags[2] == 11 || fileTags[2] == 12 || fileTags[2] == 13))
                                )

                            {
                                grade = 1;
                            }
                        }
                        else if (i == 3 && gender.StartsWith("m"))  //facial hair: 13, 14, and 15; 8, 9, 10 and 16; 6 and 7; 
                        {
                            if (
                                ((providedTags[3] == 6 || providedTags[3] == 7) && (fileTags[3] == 6 || fileTags[3] == 7)) ||
                                ((providedTags[3] == 13 || providedTags[3] == 14 || providedTags[3] == 15)
                                && (fileTags[3] == 13 || fileTags[3] == 14 || fileTags[3] == 15)) ||
                                 ((providedTags[3] == 8 || providedTags[3] == 9 || providedTags[3] == 10 || providedTags[3] == 16)
                                && (fileTags[3] == 8 || fileTags[3] == 9 || fileTags[3] == 10 || fileTags[3] == 16))
                                )

                            {
                                grade = 1;
                            }
                        }
                        else if (i == 4) //colors: 1, 4, and 6; 2, 5 and 7; 
                        {
                            if (((providedTags[i] == 1 || providedTags[i] == 4 || providedTags[i] == 6)
                            && (fileTags[i] == 1 || fileTags[i] == 4 || fileTags[i] == 6)) ||
                            ((providedTags[i] == 2 || providedTags[i] == 5 || providedTags[i] == 7)
                            && (fileTags[i] == 2 || fileTags[i] == 5 || fileTags[i] == 7)))
                            {
                                grade = 1;
                            }
                        }

                        score += weights[i] * grade;
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

                    PortraitSprites portraitSprites = _soldierPortraits[actor];


                    if (CharacterPortrait.characterPics.ContainsKey(actor.GeoUnitId))
                    {

                        // TFTVLogger.Always($"ForceSpecialCharacterPortraitInSetupProperPortrait actor is {actor.name}");


                        //  Sprite sprite = Helper.CreatePortraitFromImageFile($"{folderPath}/test.png");

                        Sprite sprite = Helper.CreatePortraitFromImageFile(CharacterPortrait.characterPics[actor.GeoUnitId]);

                        portraitSprites.Portrait = sprite;
                        updatePortraitForSoldierMethod.Invoke(squadMemberScrollerController, new object[] { actor });

                        return false;

                    }

                    if (actor.CameFromSourceTemplate(DefCache.GetDef<TacCharacterDef>("S_Helena_TacCharacterDef")))
                    {
                        Sprite sprite = Helper.CreatePortraitFromImageFile("helena.png");

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

    }
}
