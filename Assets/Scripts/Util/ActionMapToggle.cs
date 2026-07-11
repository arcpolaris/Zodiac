using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zodiac.Util
{
    public sealed class ActionMapToggle : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField, LabelOverride("Action Map")] private string mapName;

        private InputActionMap actionMap;

        void OnValidate()
        {
            actionMap = null;
            if (inputActionAsset == null) return;
            if (string.IsNullOrEmpty(mapName)) return;

            actionMap = inputActionAsset.FindActionMap(mapName);
        }

        void Awake() => actionMap = inputActionAsset.FindActionMap(mapName, true);

        public void EnableActionMap() => actionMap.Enable();
        public void DisableActionMap() => actionMap.Disable();

        public bool IsEnabled
        {
            get => actionMap.enabled;
            set
            {
                if (value) EnableActionMap();
                else DisableActionMap();
            }
        }

        private void Deconstruct(out string str, out InputActionMap map){
            str = mapName;
            map = actionMap;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ActionMapToggle))]
        private class ActionMapToggleEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                ActionMapToggle target = (ActionMapToggle)base.target;
                
                if (string.IsNullOrEmpty(target.mapName)) return;
                if (target.actionMap != null) return;

                EditorGUILayout.HelpBox($"Could not find ActionMap \"{target.mapName}\" in provided asset", MessageType.Error);
            }
        }
#endif
    }
}