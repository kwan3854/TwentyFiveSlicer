using UnityEngine;
using UnityEditor;
using TwentyFiveSlicer.Runtime;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    [CustomEditor(typeof(TwentyFiveSliceImage))]
    [CanEditMultipleObjects]
    public class TwentyFiveSliceImageComponentInspector : UnityEditor.Editor
    {
        // From UnityEngine.UI.Image
        private SerializedProperty _spSprite;
        private SerializedProperty _spColor;
        private SerializedProperty _spMaterial;
        private SerializedProperty _spRaycastTarget;
        private SerializedProperty _spRaycastPadding;

        // From TwentyFiveSliceImage
        private SerializedProperty _spDebuggingView;

        // For foldouts
        private bool _shouldShowRaycastPadding = false;
        private bool _shouldShowDebuggingMenus = false;

        // Help box messages
        private const string NoSliceDataWarning = 
            "The selected sprite does not have 25-slice data. Please slice the sprite in Window -> 2D -> 25-Slice Editor.";

        private void OnEnable()
        {
            // Find the serialized fields in the base Image class
            _spSprite         = serializedObject.FindProperty("m_Sprite");
            _spColor          = serializedObject.FindProperty("m_Color");
            _spMaterial       = serializedObject.FindProperty("m_Material");
            _spRaycastTarget  = serializedObject.FindProperty("m_RaycastTarget");
            _spRaycastPadding = serializedObject.FindProperty("m_RaycastPadding");

            // For the TwentyFiveSliceImage fields
            _spDebuggingView  = serializedObject.FindProperty("debuggingView"); 
        }

        public override void OnInspectorGUI()
        {
            // 1) Update the serialized object so we have the latest data
            serializedObject.Update();

            // 2) Draw the fields we want from the base Image
            EditorGUILayout.PropertyField(_spSprite,     new GUIContent("Source Image"));
            EditorGUILayout.PropertyField(_spColor,      new GUIContent("Color"));
            EditorGUILayout.PropertyField(_spMaterial,   new GUIContent("Material"));
            EditorGUILayout.PropertyField(_spRaycastTarget, new GUIContent("Raycast Target"));

            // 3) Raycast Padding foldout
            _shouldShowRaycastPadding = EditorGUILayout.Foldout(_shouldShowRaycastPadding, "Raycast Padding");
            if (_shouldShowRaycastPadding)
            {
                EditorGUI.indentLevel++;
                
                // Show Left, Bottom, Right, Top for a Vector4.
                Vector4 padding = _spRaycastPadding.vector4Value;
                padding.x = EditorGUILayout.FloatField("Left",   padding.x);
                padding.y = EditorGUILayout.FloatField("Bottom", padding.y);
                padding.z = EditorGUILayout.FloatField("Right",  padding.z);
                padding.w = EditorGUILayout.FloatField("Top",    padding.w);
                
                // Write back to the property
                _spRaycastPadding.vector4Value = padding;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // 4) TwentyFiveSliceImage-specific fields
            _shouldShowDebuggingMenus = EditorGUILayout.Foldout(_shouldShowDebuggingMenus, "Debugging");
            if (_shouldShowDebuggingMenus)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_spDebuggingView, new GUIContent("Debugging View"));
                EditorGUI.indentLevel--;
            }

            // 5) Show the 25-slice data warning if needed
            ShowSliceDataWarning();

            // 6) Apply changes
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowSliceDataWarning()
        {
            // If there's a sprite but no 25-slice data, show the warning
            Sprite spriteObj = _spSprite.objectReferenceValue as Sprite;
            if (spriteObj != null)
            {
                if (!SliceDataManager.Instance.TryGetSliceData(spriteObj, out _))
                {
                    EditorGUILayout.HelpBox(NoSliceDataWarning, MessageType.Warning);
                }
            }
        }
    }
}