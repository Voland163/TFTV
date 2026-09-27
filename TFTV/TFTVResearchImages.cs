using HarmonyLib;
using PhoenixPoint.Common.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TFTV
{
    /// <summary>
    /// Custom research background images, loaded from JPGs in Assets/Textures (vanilla ones are 1024x512; JPG because these
    /// are opaque and a PNG of that size is several times heavier). A research whose file isn't there keeps its vanilla image.
    ///
    /// ResearchViewElementDef.ResearchIcon is an AssetReferenceSprite: a GUID into the game's addressable bundles, so a
    /// file on disk can't be assigned to it. GeoLevelController.PreloadAssets loads every research's reference, and every
    /// screen that shows the image (ResearchTooltip, GeoReseatchCompleteDataBind, GeoPhoenixpedia.AddResearchEntry,
    /// DiplomacyResearchRewardDataBind, AlienResearchBriefDataBind) then reads ResearchIcon.Asset. So each custom research
    /// gets a reference of its own, and the getter patch below answers with our image for exactly those references.
    /// </summary>
    internal static class TFTVResearchImages
    {
        private static readonly Dictionary<AssetReference, Sprite> _spritesByReference = new Dictionary<AssetReference, Sprite>();
        private static readonly Dictionary<ResearchViewElementDef, AssetReferenceSprite> _referencesByViewElement = new Dictionary<ResearchViewElementDef, AssetReferenceSprite>();
        private static readonly Dictionary<string, Sprite> _spritesByFile = new Dictionary<string, Sprite>();

        /// <summary>
        /// Shows imageFileName as the research's background. If the file is missing the research keeps the image it has.
        /// </summary>
        internal static void SetCustomImage(ResearchViewElementDef viewElement, string imageFileName)
        {
            try
            {
                if (viewElement == null || string.IsNullOrEmpty(imageFileName))
                {
                    return;
                }

                Sprite sprite = GetSprite(imageFileName);

                if (sprite == null)
                {
                    return;
                }

                // Defs can be set up again (CreateDefFromClone returns the existing def), so drop the reference registered last time.
                if (_referencesByViewElement.TryGetValue(viewElement, out AssetReferenceSprite oldReference))
                {
                    _spritesByReference.Remove(oldReference);
                }

                // Never register the def's current reference: defs made with CreateNewPXResearch share it with the vanilla research
                // the image was taken from, which would change that research too. Keeping its GUID lets the geoscape preload
                // handle the new reference like any other, and is what shows if the patch ever stops applying.
                AssetReferenceSprite reference = new AssetReferenceSprite(viewElement.ResearchIcon?.AssetGUID ?? string.Empty);
                viewElement.ResearchIcon = reference;

                _referencesByViewElement[viewElement] = reference;
                _spritesByReference[reference] = sprite;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static Sprite GetSprite(string imageFileName)
        {
            if (_spritesByFile.TryGetValue(imageFileName, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            if (!File.Exists(Path.Combine(TFTVMain.TexturesDirectory, imageFileName)))
            {
                TFTVLogger.Debug($"[ResearchImages] {imageFileName} not found in Assets/Textures, keeping the vanilla research image");
                return null;
            }

            // UI image shown at about its own size, so no mipmaps (vanilla's are imported the same way).
            sprite = Helper.CreateSpriteFromImageFile(imageFileName, mipChain: false);
            _spritesByFile[imageFileName] = sprite;

            return sprite;
        }

        [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.Asset), MethodType.Getter)]
        internal static class AssetReference_get_Asset_patch
        {
            public static bool Prefix(AssetReference __instance, ref UnityEngine.Object __result)
            {
                try
                {
                    if (_spritesByReference.Count == 0 || !_spritesByReference.TryGetValue(__instance, out Sprite sprite) || sprite == null)
                    {
                        return true;
                    }

                    __result = sprite;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
