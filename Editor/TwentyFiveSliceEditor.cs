using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public class TwentyFiveSliceEditor : EditorWindow
    {
        private Sprite _targetSprite;
        private TwentyFiveSliceData _sliceData;
        private Texture2D _spriteTexture;

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
                float aspectRatio = (float)_spriteTexture.width / _spriteTexture.height;
                float previewHeight = 400;
                float previewWidth = previewHeight * aspectRatio;

                Rect spriteRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                GUI.DrawTexture(spriteRect, _spriteTexture, ScaleMode.ScaleToFit, true);

                if (_sliceData != null)
                {
                    EditorGUILayout.LabelField("Adjust Borders (0-100%)", EditorStyles.boldLabel);

                    for (int i = 0; i < 4; i++)
                    {
                        _verticalBorders[i] = EditorGUILayout.Slider($"Vertical Border {i + 1}", _verticalBorders[i], i > 0 ? _verticalBorders[i - 1] : 0, i < 3 ? _verticalBorders[i + 1] : 100);
                        _horizontalBorders[i] = EditorGUILayout.Slider($"Horizontal Border {i + 1}", _horizontalBorders[i], i > 0 ? _horizontalBorders[i - 1] : 0, i < 3 ? _horizontalBorders[i + 1] : 100);
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

                DrawBorders(spriteRect);
            }
        }

        private void DrawBorders(Rect spriteRect)
        {
            Handles.BeginGUI();

            for (int i = 0; i < 4; i++)
            {
                float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                Handles.DrawLine(new Vector3(x, spriteRect.y), new Vector3(x, spriteRect.y + spriteRect.height));
                Handles.Label(new Vector3(x - 10, spriteRect.y - 20), $"V{i + 1}");

                float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;
                Handles.DrawLine(new Vector3(spriteRect.x, y), new Vector3(spriteRect.x + spriteRect.width, y));
                Handles.Label(new Vector3(spriteRect.x - 30, y - 10), $"H{i + 1}");
            }

            Handles.EndGUI();
        }

        private void LoadBorders()
        {
            if (SliceDataManager.Instance.TryGetSliceData(_targetSprite, out _sliceData))
            {
                _verticalBorders = _sliceData.verticalBorders;
                _horizontalBorders = _sliceData.horizontalBorders;
            }
            else
            {
                Debug.LogWarning("Slice data not found. Creating new borders.");
                _sliceData = new TwentyFiveSliceData
                {
                    verticalBorders = _verticalBorders,
                    horizontalBorders = _horizontalBorders
                };
                SliceDataManager.Instance.SaveSliceData(_targetSprite, _sliceData);
            }

            _bordersLoaded = true;
        }

        private void SaveBorders()
        {
            if (_targetSprite == null) return;

            _sliceData = new TwentyFiveSliceData
            {
                verticalBorders = _verticalBorders,
                horizontalBorders = _horizontalBorders
            };

            SliceDataManager.Instance.SaveSliceData(_targetSprite, _sliceData);
        }
    }
}