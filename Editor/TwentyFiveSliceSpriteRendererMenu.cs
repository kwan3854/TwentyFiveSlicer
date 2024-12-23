using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    /// <summary>
    /// Provides a right-click menu item under
    ///  GameObject -> 2D Object -> Sprites -> 25-Sliced
    /// that creates a new GameObject with a TwentyFiveSliceSpriteRenderer.
    /// </summary>
    public static class TwentyFiveSliceSpriteRendererMenu
    {
        [MenuItem("GameObject/2D Object/Sprites/25-Sliced", false, 300)]
        public static void Create25SlicedSprite()
        {
            // Create a new GameObject
            GameObject go = new GameObject("25-Sliced");

            // Attach the TwentyFiveSliceSpriteRenderer component
            go.AddComponent<TwentyFiveSliceSpriteRenderer>();

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create 25-Sliced Sprite");

            // Automatically select the new object in the Hierarchy
            Selection.activeGameObject = go;
        }
    }
}
