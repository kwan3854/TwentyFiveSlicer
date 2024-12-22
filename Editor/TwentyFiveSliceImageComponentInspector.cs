using System.Reflection;
using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    [CustomEditor(typeof(Runtime.TwentyFiveSliceImage))]
    public class TwentyFiveSliceImageComponentInspector : UnityEditor.Editor
    {
        private bool _showRaycastPadding = false;
        private bool _showDebuggingMenus = false;
        
        // Labels
        private const string SourceImageLabel = "Source Image";
        private const string ColorLabel = "Color";
        private const string MaterialLabel = "Material";
        private const string RaycastTargetLabel = "Raycast Target";
        private const string RaycastPaddingLabel = "Raycast Padding";
        private const string PaddingLabel = "Padding";
        private const string DebuggingLabel = "Debugging";
        private const string DebuggingColoredViewLabel = "Debugging Colored View";
        
        // Help box messages
        private const string NoSliceDataWarning = "The selected sprite does not have 25-slice data. Please slice the sprite in Window -> 2D -> 25-Slice Editor.";
        
        public override void OnInspectorGUI()
        {
            var myScript = (TwentyFiveSliceImage)target;
            
            myScript.sprite = (Sprite)EditorGUILayout.ObjectField(SourceImageLabel, myScript.sprite, typeof(Sprite),
                        false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            myScript.color = EditorGUILayout.ColorField(ColorLabel, myScript.color);
            myScript.material = (Material)EditorGUILayout.ObjectField(MaterialLabel, myScript.material, typeof(Material), false);
            myScript.raycastTarget = EditorGUILayout.Toggle(RaycastTargetLabel, myScript.raycastTarget);
            
            _showRaycastPadding = EditorGUILayout.Foldout(_showRaycastPadding, RaycastPaddingLabel);
            if (_showRaycastPadding)
            {
                EditorGUI.indentLevel++;
                myScript.raycastPadding = EditorGUILayout.Vector4Field(PaddingLabel, myScript.raycastPadding);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            
            _showDebuggingMenus = EditorGUILayout.Foldout(_showDebuggingMenus, DebuggingLabel);
            if (_showDebuggingMenus)
            {
                EditorGUI.indentLevel++;
                myScript.DebuggingView = EditorGUILayout.Toggle(DebuggingColoredViewLabel, myScript.DebuggingView);
                EditorGUI.indentLevel--;
            }

            if (myScript.sprite != null && !SliceDataManager.Instance.TryGetSliceData(myScript.sprite, out _))
            {
                EditorGUILayout.HelpBox(NoSliceDataWarning, MessageType.Warning);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}