using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public class TwentyFiveSliceEditor : EditorWindow
    {
        // Sprite and zoom data
        private Sprite _currentSpriteBackingField;
        private Texture2D _spriteTexture;

        // Borders data
        private float[] _verticalBorders = { 20f, 40f, 60f, 80f };
        private float[] _horizontalBorders = { 20f, 40f, 60f, 80f };
        private bool _bordersLoaded = false;

        // Scroll and zoom states
        private Vector2 _scrollPosition;
        private float _baseZoom = 1f;
        private float _zoomFactor = 1f;
        private bool _autoZoomApplied = false;
        private bool _recenterPending = false;

        private const float BaseCanvasSize = 2000f;
        private const float Margin = 50f;

        // Dragging
        private DraggingInfo _dragInfo = new DraggingInfo();

        [MenuItem("Window/2D/25-Slice Editor")]
        public static void ShowWindow()
        {
            GetWindow<TwentyFiveSliceEditor>("25-Slice Editor");
        }

        private Sprite CurrentSprite
        {
            get => _currentSpriteBackingField;
            set
            {
                if (_currentSpriteBackingField != value)
                {
                    _currentSpriteBackingField = value;
                    // Reset states when sprite changes
                    _bordersLoaded = false;
                    _autoZoomApplied = false;
                    _baseZoom = 1f;
                    _zoomFactor = 1f;
                    _recenterPending = true;
                }
            }
        }

        private void OnEnable()
        {
            this.wantsMouseMove = true;
        }

        private void OnGUI()
        {
            Rect localWindowRect = new Rect(0, 0, position.width, position.height);
            DrawMainBackground(localWindowRect);

            DrawToolbar();

            if (CurrentSprite == null)
            {
                EditorGUILayout.HelpBox("Please select a Sprite to slice.", MessageType.Info);
                return;
            }

            EnsureBordersLoaded();
            if (_spriteTexture == null) return;

            ApplyAutoZoomIfNeeded();

            HandleZoomEvents();

            DrawSpriteAndCanvas();

            Repaint();
        }

        /// <summary>
        /// Draws the top toolbar with editor title and sprite selection.
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            {
                GUILayout.Label("25-Slice Editor", EditorStyles.whiteLabel);

                GUILayout.Space(10);
                CurrentSprite =
                    (Sprite)EditorGUILayout.ObjectField(CurrentSprite, typeof(Sprite), false, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        /// <summary>
        /// Loads borders if not loaded yet.
        /// </summary>
        private void EnsureBordersLoaded()
        {
            if (!_bordersLoaded)
            {
                _bordersLoaded = SliceDataManager.Instance.TryGetSliceData(CurrentSprite, out var sliceData);
                if (_bordersLoaded)
                {
                    _verticalBorders = sliceData.verticalBorders;
                    _horizontalBorders = sliceData.horizontalBorders;
                }
            }

            _spriteTexture = CurrentSprite.texture;
        }

        /// <summary>
        /// Applies auto zoom if it was not applied yet.
        /// </summary>
        private void ApplyAutoZoomIfNeeded()
        {
            if (!_autoZoomApplied && _spriteTexture != null)
            {
                ApplyAutoZoomToFitSprite();
                _autoZoomApplied = true;
            }
        }

        /// <summary>
        /// Apply auto zoom so that the sprite fits the window perfectly at 100% (zoomFactor=1).
        /// Later, user can zoom in/out with scroll wheel.
        /// </summary>
        private void ApplyAutoZoomToFitSprite()
        {
            if (_spriteTexture == null) return;

            float usableWidth = position.width - Margin * 2f;
            float usableHeight = position.height - Margin * 2f;

            float zoomX = usableWidth / _spriteTexture.width;
            float zoomY = usableHeight / _spriteTexture.height;

            _baseZoom = Mathf.Min(zoomX, zoomY);
            _zoomFactor = 1f;
        }

        /// <summary>
        /// Handles zoom events using mouse scroll wheel.
        /// Allows zoomFactor between 0.5 and 2.0 times of base zoom.
        /// </summary>
        private void HandleZoomEvents()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.ScrollWheel)
            {
                float zoomDelta = -currentEvent.delta.y * 0.05f;
                _zoomFactor = Mathf.Clamp(_zoomFactor + zoomDelta, 0.5f, 2.0f);
                currentEvent.Use();
            }
        }

        /// <summary>
        /// Draw the sprite and its canvas (scrollable area).
        /// Handle recentering and popup window.
        /// </summary>
        private void DrawSpriteAndCanvas()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, true, true, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            float spriteWidth = _spriteTexture.width * (_baseZoom * _zoomFactor);
            float spriteHeight = _spriteTexture.height * (_baseZoom * _zoomFactor);

            float bigCanvasWidth = Mathf.Max(BaseCanvasSize, spriteWidth + Margin * 2f);
            float bigCanvasHeight = Mathf.Max(BaseCanvasSize, spriteHeight + Margin * 2f);

            Rect bigCanvasRect = GUILayoutUtility.GetRect(bigCanvasWidth, bigCanvasHeight);
            Rect spriteRect = CalculateSpriteRect(bigCanvasRect, spriteWidth, spriteHeight);

            DrawGridBackground(spriteRect);
            
            var outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(CurrentSprite);
            Rect uvRect = new Rect(
                outerUV.x,
                outerUV.y,
                outerUV.z - outerUV.x, // width in UV space
                outerUV.w - outerUV.y  // height in UV space
            );

            // Draw only the sprite portion of the atlas
            GUI.DrawTextureWithTexCoords(spriteRect, _spriteTexture, uvRect, true);
            
            DrawSpriteBoundary(spriteRect);
            DrawBorders(spriteRect);
            DrawIntersections(spriteRect);

            HandleInput(spriteRect);

            EditorGUILayout.EndScrollView();

            if (_recenterPending && Event.current.type == EventType.Repaint)
            {
                CenterSpriteInScroll(bigCanvasWidth, bigCanvasHeight, spriteRect);
                _recenterPending = false;
                Repaint();
            }

            DrawPopupWindow();
        }

        /// <summary>
        /// Calculate the rect in which the sprite should be drawn, centered within the given container.
        /// </summary>
        private Rect CalculateSpriteRect(Rect containerRect, float spriteWidth, float spriteHeight)
        {
            float offsetX = containerRect.x + (containerRect.width - spriteWidth) / 2f;
            float offsetY = containerRect.y + (containerRect.height - spriteHeight) / 2f;
            return new Rect(offsetX, offsetY, spriteWidth, spriteHeight);
        }

        /// <summary>
        /// Recenter the scroll view so that the sprite is displayed in the center of the window.
        /// </summary>
        private void CenterSpriteInScroll(float bigCanvasWidth, float bigCanvasHeight, Rect spriteRect)
        {
            float visibleWidth = position.width;
            float visibleHeight = position.height;

            Vector2 desiredScroll = new Vector2(
                spriteRect.center.x - (visibleWidth / 2f),
                spriteRect.center.y - (visibleHeight / 2f)
            );

            desiredScroll.x = Mathf.Clamp(desiredScroll.x, 0, Mathf.Max(0, bigCanvasWidth - visibleWidth));
            desiredScroll.y = Mathf.Clamp(desiredScroll.y, 0, Mathf.Max(0, bigCanvasHeight - visibleHeight));

            _scrollPosition = desiredScroll;
        }

        /// <summary>
        /// Draws the main background with a dark gray diagonal pattern.
        /// </summary>
        private void DrawMainBackground(Rect windowRect)
        {
            Color baseColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            EditorGUI.DrawRect(windowRect, baseColor);

            Handles.BeginGUI();
            Handles.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            float spacing = 20f;
            for (float startX = -windowRect.height; startX < windowRect.width; startX += spacing)
            {
                Vector3 start = new Vector3(startX, 0, 0);
                Vector3 end = new Vector3(startX + windowRect.height, windowRect.height, 0);
                Handles.DrawLine(start, end);
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// Draws a checkered grid background behind the sprite.
        /// </summary>
        private void DrawGridBackground(Rect rect)
        {
            Handles.BeginGUI();

            Color darkGray = new Color(0.2f, 0.2f, 0.2f);
            Color lightGray = new Color(0.4f, 0.4f, 0.4f);

            float gridSize = 20f * _baseZoom * _zoomFactor;
            int fullGridXCount = (int)(rect.width / gridSize);
            int fullGridYCount = (int)(rect.height / gridSize);

            float leftoverWidth = rect.width - (fullGridXCount * gridSize);
            float leftoverHeight = rect.height - (fullGridYCount * gridSize);

            int gridXCount = fullGridXCount + (leftoverWidth > 0.001f ? 1 : 0);
            int gridYCount = fullGridYCount + (leftoverHeight > 0.001f ? 1 : 0);

            for (int x = 0; x < gridXCount; x++)
            {
                for (int y = 0; y < gridYCount; y++)
                {
                    bool isDark = (x + y) % 2 == 0;
                    float cellWidth = (x == gridXCount - 1 && leftoverWidth > 0.001f) ? leftoverWidth : gridSize;
                    float cellHeight = (y == gridYCount - 1 && leftoverHeight > 0.001f) ? leftoverHeight : gridSize;

                    Rect gridRect = new Rect(rect.x + x * gridSize, rect.y + y * gridSize, cellWidth, cellHeight);
                    EditorGUI.DrawRect(gridRect, isDark ? darkGray : lightGray);
                }
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// Draws a cyan boundary around the sprite.
        /// </summary>
        private void DrawSpriteBoundary(Rect spriteRect)
        {
            Color outlineColor = new Color(56f / 255f, 118f / 255f, 1f, 1f);

            Handles.BeginGUI();
            Handles.color = outlineColor;
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, outlineColor);
            Handles.EndGUI();
        }

        /// <summary>
        /// Draws vertical and horizontal borders lines on the sprite.
        /// </summary>
        private void DrawBorders(Rect spriteRect)
        {
            Handles.BeginGUI();
            Handles.color = Color.green;

            // Vertical lines
            for (int i = 0; i < 4; i++)
            {
                float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                Handles.DrawLine(new Vector3(x, spriteRect.y), new Vector3(x, spriteRect.y + spriteRect.height));
                Handles.Label(new Vector3(x - 5, spriteRect.y - 20), $"V{i + 1}");
            }

            // Horizontal lines
            for (int i = 0; i < 4; i++)
            {
                float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;
                Handles.DrawLine(new Vector3(spriteRect.x, y), new Vector3(spriteRect.x + spriteRect.width, y));
                Handles.Label(new Vector3(spriteRect.x + spriteRect.width + 5, y), $"H{i + 1}");
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// Draws intersection points of vertical and horizontal lines.
        /// </summary>
        private void DrawIntersections(Rect spriteRect)
        {
            Handles.BeginGUI();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float x = spriteRect.x + spriteRect.width * _verticalBorders[j] / 100f;
                    float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;

                    Handles.color = Color.green;
                    Handles.DrawSolidRectangleWithOutline(new Rect(x - 2, y - 2, 4, 4), Color.green, Color.black);
                }
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// Handles mouse input for dragging lines and intersections.
        /// </summary>
        private void HandleInput(Rect spriteRect)
        {
            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            if (currentEvent.type == EventType.MouseDown)
            {
                TryBeginDrag(spriteRect, mousePos, currentEvent);
            }
            else if (currentEvent.type == EventType.MouseDrag && _dragInfo.IsDragging())
            {
                UpdateDrag(spriteRect, mousePos, currentEvent);
            }
            else if (currentEvent.type == EventType.MouseUp && _dragInfo.IsDragging())
            {
                EndDrag();
            }

            UpdateCursorIcon(spriteRect, mousePos);
        }

        /// <summary>
        /// Attempt to begin dragging a line or intersection handle.
        /// </summary>
        private void TryBeginDrag(Rect spriteRect, Vector2 mousePos, Event currentEvent)
        {
            if (CheckIntersectionHandleHover(spriteRect, mousePos, out int interH, out int interV))
            {
                _dragInfo.SetDraggingState(DraggingState.Intersection, interV, interH, MouseCursor.ResizeUpLeft);
                currentEvent.Use();
                return;
            }

            if (CheckVerticalHandleHover(spriteRect, mousePos, out int vIndex))
            {
                _dragInfo.SetDraggingState(DraggingState.Vertical, vIndex, -1, MouseCursor.ResizeHorizontal);
                currentEvent.Use();
                return;
            }

            if (CheckHorizontalHandleHover(spriteRect, mousePos, out int hIndex))
            {
                _dragInfo.SetDraggingState(DraggingState.Horizontal, -1, hIndex, MouseCursor.ResizeVertical);
                currentEvent.Use();
                return;
            }
        }

        /// <summary>
        /// Update dragging logic (vertical, horizontal, or intersection) while mouse is moved.
        /// </summary>
        private void UpdateDrag(Rect spriteRect, Vector2 mousePos, Event currentEvent)
        {
            (DraggingState state, int vIndex, int hIndex) = _dragInfo.GetDraggingState();

            if (state == DraggingState.Vertical)
                UpdateVerticalDrag(spriteRect, mousePos, vIndex);
            else if (state == DraggingState.Horizontal)
                UpdateHorizontalDrag(spriteRect, mousePos, hIndex);
            else if (state == DraggingState.Intersection)
                UpdateIntersectionDrag(spriteRect, mousePos, vIndex, hIndex);

            currentEvent.Use();
        }

        private void EndDrag()
        {
            _dragInfo.ClearDraggingState();
        }

        /// <summary>
        /// Update cursor icon depending on whether the mouse is over a handle or not.
        /// </summary>
        private void UpdateCursorIcon(Rect spriteRect, Vector2 mousePos)
        {
            if (_dragInfo.IsDragging())
            {
                Rect fullRect = new Rect(0, 0, position.width, position.height);
                EditorGUIUtility.AddCursorRect(fullRect, _dragInfo.GetCurrentDragCursor());
            }
            else
            {
                SetCursorIfOverHandle(spriteRect, mousePos);
            }
        }

        private void SetCursorIfOverHandle(Rect spriteRect, Vector2 mousePos)
        {
            if (CheckIntersectionHandleHover(spriteRect, mousePos, out _, out _))
            {
                EditorGUIUtility.AddCursorRect(new Rect(mousePos.x - 5, mousePos.y - 5, 10, 10),
                    MouseCursor.ResizeUpLeft);
                return;
            }

            if (CheckVerticalHandleHover(spriteRect, mousePos, out _))
            {
                EditorGUIUtility.AddCursorRect(new Rect(mousePos.x - 5, spriteRect.y, 10, spriteRect.height),
                    MouseCursor.ResizeHorizontal);
                return;
            }

            if (CheckHorizontalHandleHover(spriteRect, mousePos, out _))
            {
                EditorGUIUtility.AddCursorRect(new Rect(spriteRect.x, mousePos.y - 5, spriteRect.width, 10),
                    MouseCursor.ResizeVertical);
                return;
            }
        }

        private bool CheckIntersectionHandleHover(Rect spriteRect, Vector2 mousePos, out int hIndex, out int vIndex)
        {
            float tolerance = 10f;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float x = spriteRect.x + spriteRect.width * _verticalBorders[j] / 100f;
                    float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;
                    Rect intersectionHandleRect = new Rect(x - tolerance, y - tolerance, 2 * tolerance, 2 * tolerance);
                    if (intersectionHandleRect.Contains(mousePos))
                    {
                        hIndex = i;
                        vIndex = j;
                        return true;
                    }
                }
            }

            hIndex = -1;
            vIndex = -1;
            return false;
        }

        private bool CheckVerticalHandleHover(Rect spriteRect, Vector2 mousePos, out int vIndex)
        {
            float tolerance = 10f;
            for (int i = 0; i < 4; i++)
            {
                float x = spriteRect.x + spriteRect.width * _verticalBorders[i] / 100f;
                Rect verticalHandleRect =
                    new Rect(x - 5 - tolerance, spriteRect.y, 10 + 2 * tolerance, spriteRect.height);
                if (verticalHandleRect.Contains(mousePos))
                {
                    vIndex = i;
                    return true;
                }
            }

            vIndex = -1;
            return false;
        }

        private bool CheckHorizontalHandleHover(Rect spriteRect, Vector2 mousePos, out int hIndex)
        {
            float tolerance = 10f;
            for (int i = 0; i < 4; i++)
            {
                float y = spriteRect.y + spriteRect.height * _horizontalBorders[i] / 100f;
                Rect horizontalHandleRect =
                    new Rect(spriteRect.x, y - 5 - tolerance, spriteRect.width, 10 + 2 * tolerance);
                if (horizontalHandleRect.Contains(mousePos))
                {
                    hIndex = i;
                    return true;
                }
            }

            hIndex = -1;
            return false;
        }

        private void UpdateVerticalDrag(Rect spriteRect, Vector2 mousePos, int vIndex)
        {
            float clampedX = ClampVerticalPosition(spriteRect, mousePos.x, vIndex);
            _verticalBorders[vIndex] = (clampedX - spriteRect.x) / spriteRect.width * 100f;
        }

        private void UpdateHorizontalDrag(Rect spriteRect, Vector2 mousePos, int hIndex)
        {
            float clampedY = ClampHorizontalPosition(spriteRect, mousePos.y, hIndex);
            _horizontalBorders[hIndex] = (clampedY - spriteRect.y) / spriteRect.height * 100f;
        }

        private void UpdateIntersectionDrag(Rect spriteRect, Vector2 mousePos, int vIndex, int hIndex)
        {
            float clampedX = ClampVerticalPosition(spriteRect, mousePos.x, vIndex);
            float clampedY = ClampHorizontalPosition(spriteRect, mousePos.y, hIndex);

            _verticalBorders[vIndex] = (clampedX - spriteRect.x) / spriteRect.width * 100f;
            _horizontalBorders[hIndex] = (clampedY - spriteRect.y) / spriteRect.height * 100f;
        }

        private float ClampVerticalPosition(Rect spriteRect, float mouseX, int index)
        {
            float minX = spriteRect.x + (index > 0 ? (_verticalBorders[index - 1] / 100f) * spriteRect.width : 0);
            float maxX = spriteRect.x + (index < _verticalBorders.Length - 1
                ? (_verticalBorders[index + 1] / 100f) * spriteRect.width
                : spriteRect.width);
            return Mathf.Clamp(mouseX, minX, maxX);
        }

        private float ClampHorizontalPosition(Rect spriteRect, float mouseY, int index)
        {
            float minY = spriteRect.y + (index > 0 ? (_horizontalBorders[index - 1] / 100f) * spriteRect.height : 0);
            float maxY = spriteRect.y + (index < _horizontalBorders.Length - 1
                ? (_horizontalBorders[index + 1] / 100f) * spriteRect.height
                : spriteRect.height);
            return Mathf.Clamp(mouseY, minY, maxY);
        }

        private void DrawPopupWindow()
        {
            float popupWidth = 300;
            float popupHeight = 200;
            Rect popupRect = new Rect(position.width - popupWidth - 20, position.height - popupHeight - 20, popupWidth,
                popupHeight);

            // Semi-transparent dark gray background
            Color popupBgColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            EditorGUI.DrawRect(popupRect, popupBgColor);

            // Border
            Color borderColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            Handles.BeginGUI();
            Handles.color = borderColor;
            Handles.DrawSolidRectangleWithOutline(popupRect, Color.clear, borderColor);
            Handles.EndGUI();

            GUIStyle centerLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUIStyle boldCenterLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.BeginArea(popupRect);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("25-Slice Data", boldCenterLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            // ========== Vertical Borders ==========
            GUILayout.BeginVertical();
            GUILayout.Label("Vertical Borders", boldCenterLabel);

            float labelWidth = 30f;
            float fieldWidth = 80f;

            for (int i = 0; i < 4; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label($"V{i + 1}", GUILayout.Width(labelWidth));

                float newValue = EditorGUILayout.FloatField(
                    _verticalBorders[i],
                    GUILayout.Width(fieldWidth)
                );

                // Clamp
                float min = (i > 0) ? _verticalBorders[i - 1] : 0f;
                float max = (i < 3) ? _verticalBorders[i + 1] : 100f;
                newValue = Mathf.Clamp(newValue, min, max);

                _verticalBorders[i] = newValue;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.Space(20);

            // ========== Horizontal Borders ==========
            GUILayout.BeginVertical();
            GUILayout.Label("Horizontal Borders", boldCenterLabel);

            for (int i = 0; i < 4; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"H{i + 1}", GUILayout.Width(labelWidth));

                float newValue = EditorGUILayout.FloatField(
                    _horizontalBorders[i],
                    GUILayout.Width(fieldWidth)
                );

                float min = (i > 0) ? _horizontalBorders[i - 1] : 0f;
                float max = (i < 3) ? _horizontalBorders[i + 1] : 100f;
                newValue = Mathf.Clamp(newValue, min, max);

                _horizontalBorders[i] = newValue;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUIStyle saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", saveButtonStyle, GUILayout.Height(30), GUILayout.Width(250)))
            {
                SaveBorders();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndArea();
        }

        private void SaveBorders()
        {
            TwentyFiveSliceData sliceData = new TwentyFiveSliceData
            {
                verticalBorders = _verticalBorders,
                horizontalBorders = _horizontalBorders
            };
            SliceDataManager.Instance.SaveSliceData(CurrentSprite, sliceData);
        }
    }
}