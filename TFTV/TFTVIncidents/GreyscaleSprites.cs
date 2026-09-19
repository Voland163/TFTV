using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// Greyscale copies of UI sprites, made once and kept.
    ///
    /// An Image's colour multiplies the sprite, so tinting can darken an orange affinity mark or fade
    /// it out, but never turn it grey - and faded, the mark for an approach the operative cannot bring
    /// was hard to tell apart from one they could. A disabled look needs the colour taken out of the
    /// pixels themselves, which means a copy of the sprite with the colour removed.
    ///
    /// The source textures are not readable from script - UI art is imported without a CPU copy - so
    /// the sprite's region is drawn into a render texture and read back from that instead, which works
    /// whatever the texture's import settings, atlas included.
    /// </summary>
    internal static class GreyscaleSprites
    {
        /// <summary>
        /// Brightness of the grey copy relative to the original's luminance. Below one, so a greyed
        /// mark also reads as receded rather than just as a differently coloured one.
        /// </summary>
        private const float Brightness = 0.8f;

        private static readonly Dictionary<Sprite, Sprite> Cache = new Dictionary<Sprite, Sprite>();

        /// <summary>
        /// A greyscale copy of <paramref name="source"/>, or null when one cannot be made - in which
        /// case the caller should fall back to the original sprite with a fade.
        /// </summary>
        internal static Sprite For(Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            if (Cache.TryGetValue(source, out Sprite cached))
            {
                // Unity's own null test: a copy freed since, e.g. by a scene change, is rebuilt.
                if (cached != null)
                {
                    return cached;
                }

                Cache.Remove(source);
            }

            Sprite grey = Build(source);
            if (grey != null)
            {
                Cache[source] = grey;
            }

            return grey;
        }

        private static Sprite Build(Sprite source)
        {
            Texture2D texture = source.texture;
            if (texture == null)
            {
                return null;
            }

            Rect region;
            try
            {
                // Where the sprite actually sits in its texture - its own rect is in sprite space,
                // which for a sprite packed into an atlas is not the same place.
                region = source.textureRect;
            }
            catch (Exception)
            {
                // A tightly packed atlas sprite has no rectangular region to copy.
                return null;
            }

            int width = Mathf.RoundToInt(region.width);
            int height = Mathf.RoundToInt(region.height);
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;

                Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(region.x, region.y, width, height), 0, 0);

                Color32[] pixels = copy.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 p = pixels[i];
                    float luminance = ((0.299f * p.r) + (0.587f * p.g) + (0.114f * p.b)) * Brightness;
                    byte grey = (byte)Mathf.Clamp(Mathf.RoundToInt(luminance), 0, 255);
                    pixels[i] = new Color32(grey, grey, grey, p.a);
                }

                copy.SetPixels32(pixels);
                copy.Apply(false, true);
                copy.filterMode = texture.filterMode;
                copy.wrapMode = TextureWrapMode.Clamp;

                Vector2 pivot = new Vector2(
                    source.rect.width > 0f ? source.pivot.x / source.rect.width : 0.5f,
                    source.rect.height > 0f ? source.pivot.y / source.rect.height : 0.5f);

                return Sprite.Create(copy, new Rect(0f, 0f, width, height), pivot, source.pixelsPerUnit);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
