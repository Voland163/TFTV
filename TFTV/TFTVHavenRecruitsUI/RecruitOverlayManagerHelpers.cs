using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using static TFTV.HavenRecruitsMain;

namespace TFTV
{
    internal static class RecruitOverlayManagerHelpers
    {
        internal static void ClearTransformChildren(Transform transform)
        {
            if (transform == null)
            {
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        internal static Image MakeFixedIcon(Transform parent, Sprite sp, int px, Sprite backgroundSprite = null)
        {
            // Frame with RectTransform + LayoutElement fixes size for layout
            var (frame, frt) = NewUI(backgroundSprite != null ? "IconFrameAbility" : "IconFrame", parent);
            var le = frame.AddComponent<LayoutElement>();
            le.preferredWidth = px; le.minWidth = px;
            le.preferredHeight = px; le.minHeight = px;
            frt.sizeDelta = new Vector2(px, px);

            // Padding used to make the background slightly larger and the image slightly smaller than the frame.
            int pad = Mathf.Max(2, Mathf.RoundToInt(px * 0.12f)); // ~12% of px, minimum 2px
            float inset = pad * 0.5f; // foreground inset (half of the background oversize)

            if (backgroundSprite != null)
            {
                var (bgGO, bgRT) = NewUI("Background", frame.transform);
                var bgImage = bgGO.AddComponent<Image>();
                bgImage.sprite = backgroundSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.raycastTarget = false;

                // Make the background slightly larger than the frame by expanding its offsets beyond the parent rect.
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = new Vector2(-pad, -pad); // extend left/bottom
                bgRT.offsetMax = new Vector2(pad, pad);   // extend right/top
            }

            // Child image inset a little so the background reads as larger
            var (imgGO, imgRT) = NewUI("Img", frame.transform);
            var img = imgGO.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;

            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = new Vector2(inset, inset);    // inward from left/bottom
            imgRT.offsetMax = new Vector2(-inset, -inset);  // inward from right/top

            var arf = imgGO.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (sp && sp.rect.height > 0f)
                arf.aspectRatio = sp.rect.width / sp.rect.height;

            return img;
        }


        internal static Image MakeMutationIcon(Transform parent, Sprite sp, int px)
        {
            var (frame, frt) = NewUI("MutationIconFrame", parent);
            var le = frame.AddComponent<LayoutElement>();
            le.preferredWidth = px; le.minWidth = px;
            le.preferredHeight = px; le.minHeight = px;
            frt.sizeDelta = new Vector2(px, px);

            if (_iconBackground != null)
            {
                var (bgGO, bgRT) = NewUI("Background", frame.transform);
                var bg = bgGO.AddComponent<Image>();
                bg.sprite = _iconBackground;
                // bg.color = CardBackgroundColor;
                bg.raycastTarget = false;
                bg.type = Image.Type.Sliced;
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            }

            var (imgGO, imgRT) = NewUI("Img", frame.transform);
            var img = imgGO.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;

            imgRT.anchorMin = new Vector2(0.1f, 0.1f);
            imgRT.anchorMax = new Vector2(0.9f, 0.9f);
            imgRT.offsetMin = Vector2.zero; imgRT.offsetMax = Vector2.zero;

            var arf = imgGO.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (sp && sp.rect.height > 0f)
                arf.aspectRatio = sp.rect.width / sp.rect.height;

            if (_mutationBound != null)
            {
                var (boundGO, boundRT) = NewUI("Bound", frame.transform);
                var bound = boundGO.AddComponent<Image>();
                bound.sprite = _mutationBound;
                bound.raycastTarget = false;
                bound.type = Image.Type.Sliced;
                boundRT.anchorMin = Vector2.zero; boundRT.anchorMax = Vector2.one;
                boundRT.offsetMin = Vector2.zero; boundRT.offsetMax = Vector2.zero;
            }

            return img;
        }

        internal static (GameObject go, RectTransform rt) NewUI(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (go, (RectTransform)go.transform);
        }

        internal static Sprite GetFactionIcon(FactionFilter filter)
        {
            if (_factionIconCache.TryGetValue(filter, out var cached) && cached != null)
            {
                return cached;
            }

            string fileName = null;
            switch (filter)
            {
                case FactionFilter.Anu:
                    fileName = "FactionIcons_Anu.png";
                    break;
                case FactionFilter.NewJericho:
                    fileName = "FactionIcons_NewJericho.png";
                    break;
                case FactionFilter.Synedrion:
                    fileName = "FactionIcons_Synedrion.png";
                    break;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var sprite = Helper.CreateSpriteFromImageFile(fileName);
            if (sprite != null)
            {
                _factionIconCache[filter] = sprite;
            }

            return sprite;
        }
    }
}