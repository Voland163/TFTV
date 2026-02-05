using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    internal static class BaseReworkUtils
    {
        internal static bool BaseReworkEnabled = true;//> TFTVNewGameOptions.BaseRework;
        internal static void ClearTransformChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
