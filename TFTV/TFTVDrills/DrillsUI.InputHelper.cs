using Base.Input;
using System;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private static class DrillInputHelper
        {
            public static bool TryGetCursorScreenPosition(InputController controller, out Vector2 position)
            {
                position = default;
                if (controller == null)
                {
                    return false;
                }

                Vector3 cursor = controller.GetCursorPosition();
                if (!IsValid(cursor))
                {
                    cursor = controller.GetCursorPosition(InputType.KeyboardMouse);
                }

                if (!IsValid(cursor))
                {
                    return false;
                }

                position = new Vector2(cursor.x, cursor.y);
                return true;
            }

            public static bool TryGetCursorScreenPosition(out Vector2 position)
            {
                return TryGetCursorScreenPosition(GameUtl.GameComponent<InputController>(), out position);
            }

            private static bool IsValid(Vector3 cursor)
            {
                return !float.IsNaN(cursor.x) && !float.IsNaN(cursor.y) && !float.IsInfinity(cursor.x) && !float.IsInfinity(cursor.y);
            }
        }

    }
}
