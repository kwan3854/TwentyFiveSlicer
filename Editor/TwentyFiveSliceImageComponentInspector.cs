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
        
        public override void OnInspectorGUI()
        {
            var myScript = (TwentyFiveSliceImage)target;
            
            myScript.overrideSprite = (Sprite)EditorGUILayout.ObjectField("Source Image", myScript.overrideSprite, typeof(Sprite),
                        false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            myScript.color = EditorGUILayout.ColorField("Color", myScript.color);
            myScript.material = (Material)EditorGUILayout.ObjectField("Material", myScript.material, typeof(Material), false);
            myScript.raycastTarget = EditorGUILayout.Toggle("Raycast Target", myScript.raycastTarget);
            
            _showRaycastPadding = EditorGUILayout.Foldout(_showRaycastPadding, "Raycast Padding");
            if (_showRaycastPadding)
            {
                EditorGUI.indentLevel++;
                myScript.raycastPadding = EditorGUILayout.Vector4Field("Padding", myScript.raycastPadding);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            
            _showDebuggingMenus = EditorGUILayout.Foldout(_showDebuggingMenus, "Debugging");
            if (_showDebuggingMenus)
            {
                EditorGUI.indentLevel++;
                myScript.DebuggingView = EditorGUILayout.Toggle("Debugging Colored View", myScript.DebuggingView);
                EditorGUI.indentLevel--;
            }

            if (myScript.overrideSprite != null && !SliceDataManager.Instance.TryGetSliceData(myScript.overrideSprite, out _))
            {
                EditorGUILayout.HelpBox("The selected sprite does not have 25-slice data. Please slice the sprite in Window -> 2D -> 25-Slice Editor.", MessageType.Warning);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}