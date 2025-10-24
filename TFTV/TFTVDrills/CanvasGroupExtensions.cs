using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal static class CanvasGroupExtensions
    {
        public static void SetAlpha(this CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}
