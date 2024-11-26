using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.EditorTools
{
    [CustomEditor(typeof(Runtime.TwentyFiveSliceImage))]
    public class TwentyFiveSliceImageEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Runtime.TwentyFiveSliceImage myScript = (Runtime.TwentyFiveSliceImage)target;

            // Sprite field with large preview
            myScript.overrideSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", myScript.overrideSprite, typeof(Sprite), false);

            if (myScript.overrideSprite != null)
            {
                Texture2D spriteTexture = AssetPreview.GetAssetPreview(myScript.overrideSprite);
                if (spriteTexture != null)
                {
                    float aspectRatio = (float)spriteTexture.width / spriteTexture.height;
                    float previewHeight = 200;
                    float previewWidth = previewHeight * aspectRatio;

                    Rect spriteRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                    GUI.DrawTexture(spriteRect, spriteTexture, ScaleMode.ScaleToFit, true);
                }
            }

            myScript.DebuggingView = EditorGUILayout.Toggle("Debugging View", myScript.DebuggingView);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}