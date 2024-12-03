using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.EditorTools
{
    public class TwentyFiveSliceEditor : EditorWindow
    {
        private Sprite _targetSprite;
        private TwentyFiveSliceData _sliceData;
        private Texture2D _spriteTexture;

        // Adjusted to 4 border lines
        private float[] _verticalBorders = { 20f, 40f, 60f, 80f };
        private float[] _horizontalBorders = { 20f, 40f, 60f, 80f };
        private bool _bordersLoaded = false;

        [MenuItem("Window/2D/25-Slice Editor")]
        public static void ShowWindow()
        {
            GetWindow<TwentyFiveSliceEditor>("25-Slice Editor");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("25-Slice Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetSprite = (Sprite)EditorGUILayout.ObjectField("Target Sprite", _targetSprite, typeof(Sprite), false);

            if (_targetSprite == null)
            {
                EditorGUILayout.HelpBox("Please select a Sprite to slice.", MessageType.Info);
                return;
            }

            if (!_bordersLoaded)
            {
                LoadBorders();
            }

            _spriteTexture = AssetPreview.GetAssetPreview(_targetSprite);

            if (_spriteTexture != null)
            {
                // Adjust sprite preview size to remove top and bottom padding
                float aspectRatio = (float)_spriteTexture.width / _spriteTexture.height;
                float previewHeight = 400;
                float previewWidth = previewHeight * aspectRatio;

                // Sprite preview with transparency support
                Rect spriteRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false),
                    GUILayout.ExpandHeight(false));
                GUI.DrawTexture(spriteRect, _spriteTexture, ScaleMode.ScaleToFit, true);

                if (_sliceData != null)
                {
                    // Border adjustment (4 borders)
                    EditorGUILayout.LabelField("Adjust Borders (0-100%)", EditorStyles.boldLabel);

                    for (int i = 0; i < 4; i++)
                    {
                        _verticalBorders[i] = EditorGUILayout.Slider($"Vertical Border {i + 1}", _verticalBorders[i],
                            i > 0 ? _verticalBorders[i - 1] : 0, i < 3 ? _verticalBorders[i + 1] : 100);
                        _horizontalBorders[i] = EditorGUILayout.Slider($"Horizontal Border {i + 1}",
                            _horizontalBorders[i], i > 0 ? _horizontalBorders[i - 1] : 0,
                            i < 3 ? _horizontalBorders[i + 1] : 100);
                    }

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Save Borders", GUILayout.Height(30)))
                    {
                        SaveBorders();
                    }
                }
                else
                {
                    if (GUILayout.Button("Create Borders", GUILayout.Height(30)))
                    {
                        _sliceData = new TwentyFiveSliceData
                        {
                            verticalBorders = _verticalBorders,
                            horizontalBorders = _horizontalBorders
                        };
                    }
                }

                // Draw borders
                DrawBorders(spriteRect);
            }
        }

        private void DrawBorders(Rect spriteRect)
        {
            Handles.BeginGUI();

            for (int i = 0; i < 4; i++)
            {
                // Vertical borders
                float x = spriteRect.x + (spriteRect.width * _verticalBorders[i] / 100f);
                Handles.DrawLine(new Vector3(x, spriteRect.y), new Vector3(x, spriteRect.y + spriteRect.height));
                Handles.Label(new Vector3(x - 10, spriteRect.y - 20), $"V{i + 1}");

                // Horizontal borders
                float y = spriteRect.y + (spriteRect.height * _horizontalBorders[i] / 100f);
                Handles.DrawLine(new Vector3(spriteRect.x, y), new Vector3(spriteRect.x + spriteRect.width, y));
                Handles.Label(new Vector3(spriteRect.x - 30, y - 10), $"H{i + 1}");
            }

            Handles.EndGUI();
        }

        private void LoadBorders()
        {
            if (_targetSprite == null) return;

            var hash = TfsHashGenerator.GenerateUniqueSpriteHash(_targetSprite);
            string filePath = $"{Application.dataPath}/Resources/TwentyFiveSliceData/{hash}.json";

            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                _sliceData = JsonUtility.FromJson<TwentyFiveSliceData>(json);
                if (_sliceData != null)
                {
                    _verticalBorders = _sliceData.verticalBorders;
                    _horizontalBorders = _sliceData.horizontalBorders;
                }
            }

            _bordersLoaded = true;

            Debug.Log($"Path: {filePath}, Name: {hash}.json");
        }

        private void SaveBorders()
        {
            if (_targetSprite == null) return;

            EnsureTextureIsReadable(_targetSprite.texture);

            var hash = TfsHashGenerator.GenerateUniqueSpriteHash(_targetSprite);
            string directoryPath = $"{Application.dataPath}/Resources/TwentyFiveSliceData";
            string filePath = $"{directoryPath}/{hash}.json";

            Debug.Log($"Saving slice data to: {filePath}");

            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            _sliceData = new TwentyFiveSliceData
            {
                verticalBorders = _verticalBorders,
                horizontalBorders = _horizontalBorders
            };

            string data = JsonUtility.ToJson(_sliceData);
            System.IO.File.WriteAllText(filePath, data);

            // Unity 에디터에 변경 사항 알리기
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            Debug.Log($"Path: {filePath}, Name: {hash}.json");
            Debug.Log("25-Slice data saved.");
        }

        /// <summary>
        /// Ensures the texture is readable by modifying the texture import settings if needed.
        /// </summary>
        private void EnsureTextureIsReadable(Texture2D texture)
        {
#if UNITY_EDITOR
            string texturePath = UnityEditor.AssetDatabase.GetAssetPath(texture);
            var importer = UnityEditor.AssetImporter.GetAtPath(texturePath) as UnityEditor.TextureImporter;

            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log($"Texture '{texture.name}' is now readable.");
            }
#endif
        }
    }
}