using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    [CustomEditor(typeof(Runtime.TwentyFiveSliceImage))]
    public class TwentyFiveSliceImageComponentInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var myScript = (TwentyFiveSliceImage)target;

            myScript.overrideSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", myScript.overrideSprite, typeof(Sprite), false);

            myScript.DebuggingView = EditorGUILayout.Toggle("Debugging Colored View", myScript.DebuggingView);

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