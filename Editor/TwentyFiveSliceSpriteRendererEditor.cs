using System.Linq;
using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    /// <summary>
    /// A custom editor for TwentyFiveSliceSpriteRenderer:
    /// - Inspector-only editing of size/pivot/sorting, etc.
    /// - Scene-view handles for direct mouse-based resizing are omitted.
    /// </summary>
    [CustomEditor(typeof(Runtime.TwentyFiveSliceSpriteRenderer))]
    [CanEditMultipleObjects]
    public class TwentyFiveSliceSpriteRendererEditor : UnityEditor.Editor
    {
        // SerializedProperty references
        private SerializedProperty _spSprite;
        private SerializedProperty _spDebugView;
        private SerializedProperty _spColor;
        private SerializedProperty _spFlipX;
        private SerializedProperty _spFlipY;
        private SerializedProperty _spUseSpritePivot;
        private SerializedProperty _spCustomPivot;
        private SerializedProperty _spPixelsPerUnit;
        private SerializedProperty _spSize;
        private SerializedProperty _spSortingLayerName;
        private SerializedProperty _spSortingOrder;
        
        // HelpBox message
        private const string NoSliceDataWarning = 
            "The selected sprite does not have 25-slice data. Please slice the sprite in Window -> 2D -> 25-Slice Editor.";

        private void OnEnable()
        {
            // Match the field names in TwentyFiveSliceSpriteRenderer
            _spSprite           = serializedObject.FindProperty("sprite");
            _spDebugView        = serializedObject.FindProperty("debuggingView");
            _spColor            = serializedObject.FindProperty("color");
            _spFlipX            = serializedObject.FindProperty("flipX");
            _spFlipY            = serializedObject.FindProperty("flipY");
            _spUseSpritePivot   = serializedObject.FindProperty("useSpritePivot");
            _spCustomPivot      = serializedObject.FindProperty("customPivot");
            _spPixelsPerUnit    = serializedObject.FindProperty("pixelsPerUnit");
            _spSize             = serializedObject.FindProperty("size");
            _spSortingLayerName = serializedObject.FindProperty("sortingLayerName");
            _spSortingOrder     = serializedObject.FindProperty("sortingOrder");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Sprite & Debug
            EditorGUILayout.PropertyField(_spSprite,     new GUIContent("Sprite"));
            EditorGUILayout.PropertyField(_spDebugView,  new GUIContent("Debug View"));
            EditorGUILayout.PropertyField(_spColor,      new GUIContent("Color"));
            EditorGUILayout.PropertyField(_spFlipX,      new GUIContent("Flip X"));
            EditorGUILayout.PropertyField(_spFlipY,      new GUIContent("Flip Y"));

            // Pivot & Size
            EditorGUILayout.PropertyField(_spUseSpritePivot, new GUIContent("Use Sprite Pivot"));
            if (!_spUseSpritePivot.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_spCustomPivot, new GUIContent("Custom Pivot"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_spPixelsPerUnit, new GUIContent("Pixels Per Unit"));
            EditorGUILayout.PropertyField(_spSize,          new GUIContent("Size"));

            // Sorting Layer & Order
            SortingLayerField(_spSortingLayerName, _spSortingOrder);

            // --- 25-Slice Data Warning ---
            // If there's a sprite but no slice data, show a HelpBox
            var spriteObj = _spSprite.objectReferenceValue as Sprite;
            if (spriteObj != null)
            {
                if (!SliceDataManager.Instance.TryGetSliceData(spriteObj, out _))
                {
                    EditorGUILayout.HelpBox(NoSliceDataWarning, MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


        /// <summary>
        /// Displays a popup for sorting layers like the SpriteRenderer,
        /// plus an "Add Layer" button to open the Tags and Layers project settings.
        /// </summary>
        private void SortingLayerField(SerializedProperty layerNameProp, SerializedProperty orderProp)
        {
            // Current sorting layer name
            string currentLayerName = layerNameProp.stringValue;

            // Retrieve all sorting layers
            var layers = SortingLayer.layers;
            var layerNames = layers.Select(l => l.name).ToArray();

            // Find the current index
            int currentIndex = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                if (layerNames[i] == currentLayerName)
                {
                    currentIndex = i;
                    break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Sorting Layer", currentIndex, layerNames);
            if (EditorGUI.EndChangeCheck())
            {
                // If changed, assign the new layer name
                layerNameProp.stringValue = layerNames[newIndex];
            }

            // "Add..." button
            if (GUILayout.Button("Add Layer", GUILayout.MaxWidth(80)))
            {
                // Open Project Settings > Tags and Layers
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            EditorGUILayout.EndHorizontal();

            // Show "Order in Layer"
            EditorGUILayout.PropertyField(orderProp, new GUIContent("Order in Layer"));
        }
    }
}