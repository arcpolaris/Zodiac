using UnityEngine;

namespace Zodiac.Util
{
    [CreateAssetMenu(fileName = "CursorReference", menuName = "Create Reference")]
    public class CursorHelper : ScriptableObject
    {
        public static bool IsCursorLocked
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.visible = !value;
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }

        public static void LockCursor() => IsCursorLocked = true;
        public void UnlockCursor() => IsCursorLocked = false;
    }
}
