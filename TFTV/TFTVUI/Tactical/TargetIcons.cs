using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;
using static TFTV.TFTVUI.Tactical.OpposingHumanoidForceWidget;

namespace TFTV.TFTVUI.Tactical
{
    internal class TargetIcons
    {

        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;


        private static Sprite _umbraArthronIcon = null;
        private static Sprite _umbraTritonIcon = null;

      

        [HarmonyPatch(typeof(MindControlStatus), nameof(MindControlStatus.OnUnapply))]
        internal static class TFTV_MindControlStatus_OnUnapply_RefreshIcons_Patch
        {
            public static void Postfix(MindControlStatus __instance)
            {
                try
                {
                    TacticalActor actor = __instance.TacticalActor;
                    if (actor?.TacticalActorViewBase?.UIActorElement == null)
                    {
                        return;
                    }

                    HealthbarUIActorElement hb = actor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>();
                    if (hb?.ActorClassIconElement == null)
                    {
                        return;
                    }

                    ChangeHealthBarIcon(hb.ActorClassIconElement, actor);

                    // Optional: if your spotted-enemy list caches colors too, you need to refresh it as well.
                    // Most robust approach is to let the existing SetActorClassIcon patch run again (rebuild/refresh list),
                    // but that depends on how your spotted UI is updated in your mod.
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void SetUmbraIcons()
        {
            try
            {
                _umbraArthronIcon = Helper.CreateSpriteFromImageFile("umbra_arthron_icon.png");
                _umbraTritonIcon = Helper.CreateSpriteFromImageFile("umbra_triton_icon.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static Sprite GetUmbraArthronIcon()
        {
            try
            {
                return _umbraArthronIcon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static Sprite GetUmbraTritonIcon()
        {
            try
            {
                return _umbraTritonIcon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        /// <summary>
        /// This is used to add rank triangles in the info panel and color leaders gold
        /// </summary>
        /// <param name="actorClassIconElement"></param>
        /// <param name="rankTag"></param>
        /// <param name="friendly"></param>
        public static void AdjustIconInfoPanel(ActorClassIconElement actorClassIconElement, GameTagDef rankTag, bool friendly)
        {

            try
            {

                //the ranks are a mess... because I made tags that go from 4 to 1 from rookie to boss, but for the rank triangles 4 actually means boss and 1 means rookie.
                //I'm an idiot, what can say? but fixing it too much work...

                RankIconCreator rankIconCreator = new RankIconCreator();

                int rank = 1;


                if (rankTag == TFTVHumanEnemies.HumanEnemyTier1GameTag)
                {
                    rank = 4;
                    actorClassIconElement.MainClassIcon.color = LeaderColor;
                }
                else
                {
                    // actorClassIconElement.MainClassIcon.color = NegativeColor;

                    if (rankTag == TFTVHumanEnemies.HumanEnemyTier2GameTag)
                    {
                        rank = 3;
                    }
                    else if (rankTag == TFTVHumanEnemies.HumanEnemyTier3GameTag)
                    {
                        rank = 2;
                    }
                }

                rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject, actorClassIconElement.MainClassIcon.sprite, rank, true, false, false, friendly);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        /// <summary>
        /// This is used to remove rank triangles from the info panel, necessary for cleanup
        /// </summary>
        /// <param name="actorClassIconElement"></param>
        public static void RemoveRankFromInfoPanel(ActorClassIconElement actorClassIconElement)
        {

            try
            {
                RankIconCreator rankIconCreator = new RankIconCreator();
                rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }




        public class RankIconCreator : MonoBehaviour
        {
            public Sprite topLeftTriangleSprite = Helper.CreateSpriteFromImageFile("rank_1.png");     // Triangle for the top-left corner
            public Sprite topRightTriangleSprite = Helper.CreateSpriteFromImageFile("rank_2.png");       // Triangle for the top-right corner
            public Sprite bottomLeftTriangleSprite = Helper.CreateSpriteFromImageFile("rank_4.png");     // Triangle for the bottom-left corner
            public Sprite bottomRightTriangleSprite = Helper.CreateSpriteFromImageFile("rank_3.png");    // Triangle for the bottom-right corner

            public void AddRankTriangles(GameObject iconObject, int rank, bool bigCorners = false, bool noLOSColor = false, bool shootState = false, bool friendly = false)
            {
                try
                {
                    Color color = NegativeColor;

                    if (rank == 4)
                    {
                        color = LeaderColor;
                    }

                    if (noLOSColor)
                    {
                        color = Color.gray;
                    }

                    if (friendly)
                    {
                        color = WhiteColor;
                    }


                    Sprite[] cornerSprites = {
            topLeftTriangleSprite,    // Rank 1: Top-left corner
            topRightTriangleSprite,   // Rank 2: Top-right corner
            bottomLeftTriangleSprite, // Rank 3: Bottom-left corner
            bottomRightTriangleSprite // Rank 4: Bottom-right corner
        };

                    Vector2[] cornerPositions = {
            new Vector2(0, 1), // Top-left
            new Vector2(1, 1), // Top-right
            new Vector2(0, 0), // Bottom-left
            new Vector2(1, 0)  // Bottom-right
        };



                    Vector2[] offsetPositions = new Vector2[4];

                    if (!bigCorners && !shootState)
                    {
                        offsetPositions = new Vector2[] {
            new Vector2(7, -7),   // Offset for top-left
            new Vector2(-7, -7),  // Offset for top-right
            new Vector2(7, 7),    // Offset for bottom-left
            new Vector2(-7, 7)    // Offset for bottom-right
                                  
                        };
                    }

                    if (shootState)
                    {
                        offsetPositions = new Vector2[] {
            new Vector2(35, -70),   // Offset for top-left
            new Vector2(-35, -70),  // Offset for top-right
            new Vector2(35, 70),    // Offset for bottom-left
            new Vector2(-35, 70)    // Offset for bottom-right
                            };

                    }
                    else if (bigCorners)
                    {

                        offsetPositions = new Vector2[] {
            new Vector2(14, -14),   // Offset for top-left
            new Vector2(-14, -14),  // Offset for top-right
            new Vector2(14, 14),    // Offset for bottom-left
            new Vector2(-14, 14)    // Offset for bottom-right
        };
                    }




                    for (int i = 0; i < rank; i++)
                    {
                        CreateTriangleSpriteAtCorner(iconObject, cornerSprites[i], cornerPositions[i], offsetPositions[i], color, bigCorners, shootState);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private void CreateTriangleSpriteAtCorner(GameObject parentIcon, Sprite triangleSprite, Vector2 anchorPosition, Vector2 offset, Color color, bool bigCorners = false, bool shootState = false)
            {
                try
                {
                    GameObject triangleIcon = new GameObject("RankTriangle");
                    triangleIcon.transform.SetParent(parentIcon.transform, false);

                    // Add Image component and assign the specific corner sprite
                    Image triangleImage = triangleIcon.AddComponent<Image>();
                    triangleImage.sprite = triangleSprite;
                    triangleImage.color = color; // Set color if needed


                    if (bigCorners)
                    {

                        AddOutlineToIcon addOutlineToIcon = triangleIcon.GetComponent<AddOutlineToIcon>() ?? triangleIcon.AddComponent<AddOutlineToIcon>();
                        addOutlineToIcon.icon = triangleIcon;
                        addOutlineToIcon.InitOrUpdate();
                    }



                    // Set RectTransform to position the triangle in the specified corner with an offset
                    RectTransform rectTransform = triangleIcon.GetComponent<RectTransform>();
                    rectTransform.anchorMin = anchorPosition;
                    rectTransform.anchorMax = anchorPosition;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);


                    if (shootState)
                    {
                        rectTransform.sizeDelta = new Vector2(30, 30);

                    }
                    else if (bigCorners)
                    {
                        rectTransform.sizeDelta = new Vector2(14, 14);
                    }
                    else
                    {
                        rectTransform.sizeDelta = new Vector2(7, 7);
                    }

                    rectTransform.anchoredPosition = offset;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public void SetIconWithRank(GameObject iconObject, Sprite iconSprite, int rank, bool biggerCorner = false, bool noLOSColor = false, bool shootState = false, bool friendly = false)
            {
                try
                {
                    Image iconImage = iconObject.GetComponent<Image>();
                    if (iconImage == null)
                    {
                        iconImage = iconObject.AddComponent<Image>();
                    }
                    iconImage.sprite = iconSprite;

                    AddRankTriangles(iconObject, rank, biggerCorner, noLOSColor, shootState, friendly);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public void RemoveRankTriangles(GameObject iconObject)
            {
                try
                {
                    // Find all child objects named "RankTriangle" and remove them
                    foreach (Transform child in iconObject.transform)
                    {
                        if (child.name == "RankTriangle")
                        {
                            Destroy(child.gameObject);
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

        /// <summary>
        /// This adjusts how class icon is displayed next to the HP bar, also during FPS aiming
        /// </summary>
        /// <param name="actorClassIconElement"></param>
        /// <param name="tacticalActorBase"></param>
        /// 

        public static bool IsReallyEnemy(TacticalActorBase tacticalActorBase)
        {
            try
            {


                MindControlStatus mindControlStatus = tacticalActorBase.Status?.GetStatus<MindControlStatus>();

                if (mindControlStatus != null)
                {
                    TacticalFaction originalFaction = tacticalActorBase.TacticalLevel.GetFactionByCommandName(mindControlStatus.OriginalFaction.FactionDef.ShortName);

                    if (originalFaction.GetRelationTo(tacticalActorBase.TacticalLevel.GetFactionByCommandName("px")) == FactionRelation.Enemy
                        && !tacticalActorBase.GameTags.Any(t => t is ClassTagDef classTagDef && classTagDef.name.Contains("Mindfragged")))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (tacticalActorBase.TacticalFaction.GetRelationTo(tacticalActorBase.TacticalLevel.GetFactionByCommandName("px")) == FactionRelation.Enemy
                        && !tacticalActorBase.GameTags.Any(t => t is ClassTagDef classTagDef && classTagDef.name.Contains("Mindfragged")))
                    {
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


        private static int GetAncientsChargeLevelFromWP(TacticalActor tacticalActor)
        {
            try
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

                int rank = 0;

                if (tacticalActor.HasGameTag(hopliteTag))
                {

                    if (tacticalActor.CharacterStats.WillPoints >= 30)
                    {
                        rank = 4;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 25)
                    {
                        rank = 3;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 20)
                    {
                        rank = 2;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 15)
                    {
                        rank = 1;
                    }
                }

                if (tacticalActor.HasGameTag(cyclopsTag))
                {

                    if (tacticalActor.CharacterStats.WillPoints >= 40)
                    {
                        rank = 4;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 30)
                    {
                        rank = 3;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 20)
                    {
                        rank = 2;
                    }
                    else if (tacticalActor.CharacterStats.WillPoints >= 15)
                    {
                        rank = 1;
                    }

                }

                return rank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void ImplementAncientsChargeLevel(ActorClassIconElement actorClassIconElement, TacticalActorBase tacticalActorBase, bool hasNoLos = false)
        {
            try
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

                TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                if (tacticalActor == null || (!tacticalActor.HasGameTag(cyclopsTag) && !tacticalActor.HasGameTag(hopliteTag)))
                {
                    return;
                }

                bool shootState = actorClassIconElement.MainClassIcon.rectTransform.sizeDelta.x > 100;

                RankIconCreator rankIconCreator = new RankIconCreator();
                rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                int rank = GetAncientsChargeLevelFromWP(tacticalActor);

                if (rank > 0)
                {
                    rankIconCreator.SetIconWithRank(
                        actorClassIconElement.MainClassIcon.gameObject,
                        actorClassIconElement.MainClassIcon.sprite,
                        rank,
                        true,
                        hasNoLos,
                        shootState,
                        friendly: !IsReallyEnemy(tacticalActorBase));
                }

                // Color selection:
                // - No LOS: gray
                // - Friendly (player-owned): WhiteColor
                // - Rank 4 (max charge): LeaderColor
                // - Otherwise: NegativeColor (enemy warning)
                if (hasNoLos)
                {
                    actorClassIconElement.MainClassIcon.color = RegularNoLOSColor;
                }
                else if (!IsReallyEnemy(tacticalActorBase))
                {
                    actorClassIconElement.MainClassIcon.color = WhiteColor;
                }
                else if (rank == 4)
                {
                    actorClassIconElement.MainClassIcon.color = LeaderColor;
                }
                else
                {
                    actorClassIconElement.MainClassIcon.color = NegativeColor;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        private static int GetRankFromHumanEnemyTag(TacticalActorBase tacticalActorBase)
        {
            try
            {
                int rank = 4;

                if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier2GameTag))
                {
                    rank = 3;
                }
                else if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier3GameTag))
                {
                    rank = 2;
                }
                else if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier4GameTag))
                {
                    rank = 1;
                }

                return rank;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        /// <summary>
        /// This method is for adjusting the actor's icon NOT in the targets panel
        /// </summary>
        /// <param name="actorClassIconElement"></param>
        /// <param name="tacticalActorBase"></param>
        public static void ChangeHealthBarIcon(ActorClassIconElement actorClassIconElement, TacticalActorBase tacticalActorBase)
        {
            try
            {
                RankIconCreator rankIconCreator = new RankIconCreator();
                rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.humanEnemyTagDef))
                {
                    int rank = GetRankFromHumanEnemyTag(tacticalActorBase);

                    bool shootState = actorClassIconElement.MainClassIcon.rectTransform.sizeDelta.x > 100;
                    bool isFriendly = false;

                    Color color = NegativeColor;

                    if (rank == 4)
                    {
                        color = LeaderColor;
                    }

                    if (IsReallyEnemy(tacticalActorBase))
                    {
                        actorClassIconElement.MainClassIcon.color = color;
                    }
                    else
                    {
                        actorClassIconElement.MainClassIcon.color = WhiteColor;
                        isFriendly = true;
                    }

                    rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject,
                     actorClassIconElement.MainClassIcon.sprite, rank, true, false, shootState, isFriendly);

                    // actorClassIconElement.MainClassIcon.color = color;

                }
                else if (tacticalActorBase.GameTags.Contains(TFTVRevenant.AnyRevenantGameTag))
                {
                    actorClassIconElement.MainClassIcon.color = LeaderColor;
                    actorClassIconElement.SecondaryClassIcon.color = LeaderColor;
                }
                else
                {
                    if (IsReallyEnemy(tacticalActorBase))
                    {
                        if (tacticalActorBase.ActorDef.name.Equals("Oilcrab_ActorDef") || tacticalActorBase.ActorDef.name.Equals("Oilfish_ActorDef"))
                        {
                            actorClassIconElement.MainClassIcon.color = VoidColor;
                        }
                        else
                        {
                            actorClassIconElement.MainClassIcon.color = NegativeColor;
                        }
                    }
                    else
                    {
                        actorClassIconElement.MainClassIcon.color = WhiteColor;
                    }
                }

                ImplementAncientsChargeLevel(actorClassIconElement, tacticalActorBase);


                AddOutlineToIcon addOutlineToIcon = actorClassIconElement.MainClassIcon.GetComponent<AddOutlineToIcon>() ?? actorClassIconElement.MainClassIcon.gameObject.AddComponent<AddOutlineToIcon>();
                addOutlineToIcon.icon = actorClassIconElement.MainClassIcon.gameObject;
                addOutlineToIcon.InitOrUpdate();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        public static void ManageRankIconToSpottedEnemies(SpottedTargetsElement spottedTargetsElement, GameObject obj, TacticalActorBase target)
        {
            try
            {
                ActorClassIconElement actorClassIconElement = obj.GetComponentInChildren<ActorClassIconElement>();
                RankIconCreator rankIconCreator = new RankIconCreator();
                rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                bool hasNoLOS = false;

                if (spottedTargetsElement.UiSpottedEnemyNoLosButton.isActiveAndEnabled)
                {
                    hasNoLOS = true;
                }

                if (target.HasGameTag(TFTVHumanEnemies.humanEnemyTagDef))
                {
                    int rank = GetRankFromHumanEnemyTag(target);

                    Color color = actorClassIconElement.MainClassIcon.color;

                    bool friendly = false;

                    if (hasNoLOS)
                    {
                        color = RegularNoLOSColor;
                    }
                    else
                    {
                        if (rank == 4)
                        {
                            color = LeaderColor;
                        }
                        else
                        {
                            if (IsReallyEnemy(target))
                            {
                                color = NegativeColor;
                            }
                            else
                            {
                                color = WhiteColor;
                                friendly = true;
                            }

                            // color = NegativeColor;
                        }

                    }

                    rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject,
                      actorClassIconElement.MainClassIcon.sprite, rank, true, hasNoLOS, false, friendly);

                    actorClassIconElement.MainClassIcon.color = color;
                }
                else if (target.GameTags.Contains(TFTVRevenant.AnyRevenantGameTag))
                {
                    Color color = actorClassIconElement.MainClassIcon.color;

                    if (hasNoLOS)
                    {
                        color = RegularNoLOSColor;
                    }
                    else
                    {

                        color = LeaderColor;
                    }

                    actorClassIconElement.MainClassIcon.color = color;

                }
                else if (IsReallyEnemy(target))
                {

                    Color color = actorClassIconElement.MainClassIcon.color;

                    if (hasNoLOS)
                    {
                        color = RegularNoLOSColor;
                    }
                    else
                    {
                        if (target.ActorDef.name.Equals("Oilcrab_ActorDef") || target.ActorDef.name.Equals("Oilfish_ActorDef"))
                        {

                            color = VoidColor;
                        }
                        else
                        {
                            color = NegativeColor;
                        }
                    }

                    actorClassIconElement.MainClassIcon.color = color;

                }
                else
                {
                    actorClassIconElement.MainClassIcon.color = WhiteColor;

                }

                ImplementAncientsChargeLevel(actorClassIconElement, target, hasNoLOS);

                AddOutlineToIcon addOutlineToIcon = actorClassIconElement.MainClassIcon.GetComponent<AddOutlineToIcon>() ?? actorClassIconElement.MainClassIcon.gameObject.AddComponent<AddOutlineToIcon>();
                addOutlineToIcon.icon = actorClassIconElement.MainClassIcon.gameObject;
                addOutlineToIcon.InitOrUpdate();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }



        [HarmonyPatch(typeof(HealthbarUIActorElement), "InitHealthbar")] //VERIFIED
        public static class HealthbarUIActorElement_InitHealthbar_patch
        {
            public static void Postfix(HealthbarUIActorElement __instance, TacticalActorBase ____tacActorBase)
            {
                try
                {
                    ChangeHealthBarIcon(__instance.ActorClassIconElement, ____tacActorBase);
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
