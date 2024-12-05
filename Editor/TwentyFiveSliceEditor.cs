using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public class TwentyFiveSliceEditor : EditorWindow
    {
        private Sprite _targetSprite;
        private Texture2D _spriteTexture;

        private float[] _verticalBorders = { 20f, 40f, 60f, 80f };
        private float[] _horizontalBorders = { 20f, 40f, 60f, 80f };
        private bool _bordersLoaded = false;

        private Vector2 _scrollPosition;
        private float _zoom = 1.0f;
        private Rect _canvasRect;
        private bool _isDragging = false;
        private int _draggingBorderIndex = -1;
        private bool _isDraggingVertical = false;

        private const float CanvasSize = 800f;
        private const float PaddingRatio = 0.1f; // 여유 공간 비율

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

            _spriteTexture = _targetSprite.texture;

            if (_spriteTexture != null)
            {
                // 줌 제어
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.ScrollWheel)
                {
                    float zoomDelta = -currentEvent.delta.y * 0.05f;
                    _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.5f, 3.0f);
                    currentEvent.Use();
                }

                // 내부 스크롤 뷰
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                // 내부 캔버스
                // 캔버스를 중앙에 정렬
                float windowWidth = position.width;
                float windowHeight = position.height;
                float canvasWidth = CanvasSize * _zoom;
                float canvasHeight = CanvasSize * _zoom;

                float offsetX = (windowWidth - canvasWidth) / 2f;
                float offsetY = (windowHeight - canvasHeight) / 2f;

                _canvasRect = new Rect(offsetX, offsetY, canvasWidth, canvasHeight);

                // 모눈 배경
                DrawCanvasBackground(_canvasRect);


                // 스프라이트와 경계
                DrawSpritePreview(_canvasRect);

                if (Event.current.type == EventType.Repaint)
                {
                    DrawBorders(_canvasRect);
                }

                HandleInput(_canvasRect);

                EditorGUILayout.EndScrollView();

                // 팝업 창
                DrawPopupWindow();
            }
        }

        private void DrawCanvasBackground(Rect rect)
        {
            Handles.BeginGUI();

            // 배경 색상
            Color darkGray = new Color(0.2f, 0.2f, 0.2f);
            Color lightGray = new Color(0.4f, 0.4f, 0.4f);

            float gridSize = 20f * _zoom;
            int gridXCount = Mathf.CeilToInt(rect.width / gridSize);
            int gridYCount = Mathf.CeilToInt(rect.height / gridSize);

            for (int x = 0; x < gridXCount; x++)
            {
                for (int y = 0; y < gridYCount; y++)
                {
                    bool isDark = (x + y) % 2 == 0;
                    Rect gridRect = new Rect(rect.x + x * gridSize, rect.y + y * gridSize, gridSize, gridSize);
                    EditorGUI.DrawRect(gridRect, isDark ? darkGray : lightGray);
                }
            }

            Handles.EndGUI();
        }

        private void DrawSpritePreview(Rect canvasRect)
        {
            Rect spriteRect = GetSpriteRect(canvasRect);
            GUI.DrawTexture(spriteRect, _spriteTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawBorders(Rect canvasRect)
        {
            Handles.BeginGUI();

            Rect spriteRect = GetSpriteRect(canvasRect);

            for (int i = 0; i < 4; i++)
            {
                // Vertical borders
                float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                Handles.color = Color.green;
                Handles.DrawLine(new Vector3(x, spriteRect.y), new Vector3(x, spriteRect.y + spriteRect.height));
                Handles.Label(new Vector3(x - 5, spriteRect.y - 10), $"V{i + 1}");

                // Horizontal borders
                float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;
                Handles.color = Color.green;
                Handles.DrawLine(new Vector3(spriteRect.x, y), new Vector3(spriteRect.x + spriteRect.width, y));
                Handles.Label(new Vector3(spriteRect.x + spriteRect.width + 5, y), $"H{i + 1}");
            }

            Handles.EndGUI();
        }

        private void HandleInput(Rect canvasRect)
        {
            Event currentEvent = Event.current;
            Rect spriteRect = GetSpriteRect(canvasRect);

            bool cursorSet = false;

            for (int i = 0; i < 4; i++)
            {
                float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;

                Rect verticalHandleRect = new Rect(x - 5, spriteRect.y, 10, spriteRect.height);
                Rect horizontalHandleRect = new Rect(spriteRect.x, y - 5, spriteRect.width, 10);

                // 커서 변경을 항상 수행
                if (verticalHandleRect.Contains(currentEvent.mousePosition) && !cursorSet)
                {
                    EditorGUIUtility.AddCursorRect(verticalHandleRect, MouseCursor.ResizeHorizontal);
                    cursorSet = true;
                }
                else if (horizontalHandleRect.Contains(currentEvent.mousePosition) && !cursorSet)
                {
                    EditorGUIUtility.AddCursorRect(horizontalHandleRect, MouseCursor.ResizeVertical);
                    cursorSet = true;
                }
            }

            // 마우스 입력 처리
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                        float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;

                        float tolerance = 10f; // Tolerance value

                        Rect verticalHandleRect = new Rect(x - 5 - tolerance, spriteRect.y, 10 + 2 * tolerance, spriteRect.height);
                        Rect horizontalHandleRect = new Rect(spriteRect.x, y - 5 - tolerance, spriteRect.width, 10 + 2 * tolerance);

                        if (verticalHandleRect.Contains(currentEvent.mousePosition))
                        {
                            _isDragging = true;
                            _draggingBorderIndex = i;
                            _isDraggingVertical = true;
                            currentEvent.Use();
                            return;
                        }

                        if (horizontalHandleRect.Contains(currentEvent.mousePosition))
                        {
                            _isDragging = true;
                            _draggingBorderIndex = i;
                            _isDraggingVertical = false;
                            currentEvent.Use();
                            return;
                        }
                    }

                    break;

                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        Vector2 mousePosition = currentEvent.mousePosition;

                        if (_isDraggingVertical)
                        {
                            float minX = spriteRect.x + (_draggingBorderIndex > 0 ? _verticalBorders[_draggingBorderIndex - 1] / 100f * spriteRect.width : 0);
                            float maxX = spriteRect.x + (_draggingBorderIndex < _verticalBorders.Length - 1 ? _verticalBorders[_draggingBorderIndex + 1] / 100f * spriteRect.width : spriteRect.width);
                            float clampedX = Mathf.Clamp(mousePosition.x, minX, maxX);
                            _verticalBorders[_draggingBorderIndex] = (clampedX - spriteRect.x) / spriteRect.width * 100f;
                        }
                        else
                        {
                            float minY = spriteRect.y + (_draggingBorderIndex > 0 ? _horizontalBorders[_draggingBorderIndex - 1] / 100f * spriteRect.height : 0);
                            float maxY = spriteRect.y + (_draggingBorderIndex < _horizontalBorders.Length - 1 ? _horizontalBorders[_draggingBorderIndex + 1] / 100f * spriteRect.height : spriteRect.height);
                            float clampedY = Mathf.Clamp(mousePosition.y, minY, maxY);
                            _horizontalBorders[_draggingBorderIndex] = (clampedY - spriteRect.y) / spriteRect.height * 100f;
                        }

                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseUp:
                    _isDragging = false;
                    _draggingBorderIndex = -1;
                    break;

                case EventType.MouseMove:
                case EventType.Repaint:
                    // Repaint 및 MouseMove 이벤트에서 커서 모양 갱신
                    for (int i = 0; i < 4; i++)
                    {
                        float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                        float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;

                        float tolerance = 10f; // Tolerance value

                        Rect verticalHandleRect = new Rect(x - 5 - tolerance, spriteRect.y, 10 + 2 * tolerance, spriteRect.height);
                        Rect horizontalHandleRect = new Rect(spriteRect.x, y - 5 - tolerance, spriteRect.width, 10 + 2 * tolerance);

                        if (verticalHandleRect.Contains(currentEvent.mousePosition))
                        {
                            EditorGUIUtility.AddCursorRect(verticalHandleRect, MouseCursor.ResizeHorizontal);
                        }
                        else if (horizontalHandleRect.Contains(currentEvent.mousePosition))
                        {
                            EditorGUIUtility.AddCursorRect(horizontalHandleRect, MouseCursor.ResizeVertical);
                        }
                    }

                    break;
            }
        }

        private Rect GetSpriteRect(Rect canvasRect)
        {
            float aspectRatio = (float)_spriteTexture.width / _spriteTexture.height;

            float paddedHeight = canvasRect.height * (1 - 2 * PaddingRatio);
            float paddedWidth = paddedHeight * aspectRatio;

            float offsetX = (canvasRect.width - paddedWidth) / 2f;
            float offsetY = (canvasRect.height - paddedHeight) / 2f;

            return new Rect(canvasRect.x + offsetX, canvasRect.y + offsetY, paddedWidth, paddedHeight);
        }

        private void DrawPopupWindow()
        {
            // 팝업의 크기를 조정
            Rect popupRect = new Rect(position.width - 220, position.height - 200, 200, 250);

            GUILayout.BeginArea(popupRect, GUI.skin.box);
            GUILayout.Label("25-Slice Editor", EditorStyles.boldLabel);

            for (int i = 0; i < 4; i++)
            {
                GUILayout.Label($"V{i + 1}: {_verticalBorders[i]:F1}%", EditorStyles.label);
                GUILayout.Label($"H{i + 1}: {_horizontalBorders[i]:F1}%", EditorStyles.label);
            }

            // 버튼의 높이를 팝업 영역에 맞게 조정
            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
                SaveBorders();
            }

            GUILayout.EndArea();
        }

        private void LoadBorders()
        {
            // Load borders from SliceDataManager
            _bordersLoaded = SliceDataManager.Instance.TryGetSliceData(_targetSprite, out var sliceData);

            if (_bordersLoaded)
            {
                _verticalBorders = sliceData.verticalBorders;
                _horizontalBorders = sliceData.horizontalBorders;
            }
        }

        private void SaveBorders()
        {
            // Save borders to SliceDataManager
            TwentyFiveSliceData sliceData = new TwentyFiveSliceData
            {
                verticalBorders = _verticalBorders,
                horizontalBorders = _horizontalBorders
            };

            SliceDataManager.Instance.SaveSliceData(_targetSprite, sliceData);
        }
    }
}