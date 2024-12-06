using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public static class TwentyFiveSliceImageMenu
    {
        [MenuItem("GameObject/UI/Image - 25 Slice", false, 2000)]
        public static void CreateTwentyFiveSliceImage()
        {
            // Find or create a Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();

                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }

            // Create the TwentyFiveSliceImage GameObject
            var imageGo = new GameObject("TwentyFiveSliceImage", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(imageGo, "Create TwentyFiveSliceImage");

            imageGo.transform.SetParent(canvas.transform, false);

            var image = imageGo.AddComponent<TwentyFiveSliceImage>();

            var rt = imageGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);

            Selection.activeGameObject = imageGo;
        }
    }
}