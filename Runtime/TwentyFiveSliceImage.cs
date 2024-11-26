using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwentyFiveSlicer.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class TwentyFiveSliceImage : Image
    {
        private struct SliceRect
        {
            public Vector2 Position; // 좌하단 위치
            public Vector2 Size; // 너비, 높이
            public Vector2 UVMin; // UV 좌하단
            public Vector2 UVMax; // UV 우상단
        }

        public bool DebuggingView
        {
            get => _debuggingView;
            set
            {
                if (_debuggingView != value)
                {
                    _debuggingView = value;
                    SetVerticesDirty();
                }
            }
        }
        private bool _debuggingView = false;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (overrideSprite == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            var sliceData = LoadTwentyFiveSliceData(overrideSprite);
            if (sliceData == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Rect rect = GetPixelAdjustedRect();
            Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(overrideSprite);
            Rect spriteRect = overrideSprite.rect;

            // Borders in percentages
            float[] xBordersPercent = new float[6];
            float[] yBordersPercent = new float[6];

            xBordersPercent[0] = 0f;
            yBordersPercent[0] = 0f;
            xBordersPercent[5] = 100f;
            yBordersPercent[5] = 100f;

            for (int i = 0; i < 4; i++)
            {
                xBordersPercent[i + 1] = sliceData.verticalBorders[i];
                yBordersPercent[4 - i] = 100f - sliceData.horizontalBorders[i]; // y축 방향 반전
            }

            // UV borders
            float[] uvXBorders = new float[6];
            float[] uvYBorders = new float[6];

            uvXBorders[0] = outer.x;
            uvYBorders[0] = outer.y;
            uvXBorders[5] = outer.z;
            uvYBorders[5] = outer.w;

            for (int i = 0; i < 4; i++)
            {
                uvXBorders[i + 1] = Mathf.Lerp(outer.x, outer.z, xBordersPercent[i + 1] / 100f);
                uvYBorders[i + 1] = Mathf.Lerp(outer.y, outer.w, yBordersPercent[i + 1] / 100f);
            }

            // Original widths and heights
            float[] originalWidths = new float[5];
            float[] originalHeights = new float[5];

            for (int i = 0; i < 5; i++)
            {
                originalWidths[i] = (xBordersPercent[i + 1] - xBordersPercent[i]) * spriteRect.width / 100f;
                originalHeights[i] = (yBordersPercent[i + 1] - yBordersPercent[i]) * spriteRect.height / 100f;
            }

            // Determine fixed and stretchable columns and rows
            bool[] fixedColumns = new bool[5] { true, false, true, false, true }; // columns 0,2,4 are fixed
            bool[] fixedRows = new bool[5] { true, false, true, false, true }; // rows 0,2,4 are fixed

            // Compute total fixed width and height
            float totalFixedWidth = 0f;
            float totalFixedHeight = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedColumns[i])
                    totalFixedWidth += originalWidths[i];
                if (fixedRows[i])
                    totalFixedHeight += originalHeights[i];
            }

            // Compute total stretchable width and height
            float totalStretchableWidth = Mathf.Max(0, rect.width - totalFixedWidth);
            float totalStretchableHeight = Mathf.Max(0, rect.height - totalFixedHeight);

            // Compute widths
            float[] widths = new float[5];
            float stretchableWidthRatio = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedColumns[i])
                {
                    widths[i] = originalWidths[i];
                }
                else
                {
                    stretchableWidthRatio += originalWidths[i];
                }
            }

            for (int i = 0; i < 5; i++)
            {
                if (!fixedColumns[i])
                {
                    widths[i] = totalStretchableWidth * (originalWidths[i] / stretchableWidthRatio);
                }
            }

            // Compute heights
            float[] heights = new float[5];
            float stretchableHeightRatio = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedRows[i])
                {
                    heights[i] = originalHeights[i];
                }
                else
                {
                    stretchableHeightRatio += originalHeights[i];
                }
            }

            for (int i = 0; i < 5; i++)
            {
                if (!fixedRows[i])
                {
                    heights[i] = totalStretchableHeight * (originalHeights[i] / stretchableHeightRatio);
                }
            }

            // Compute positions
            float[] xPositions = new float[6];
            xPositions[0] = rect.xMin;
            for (int i = 1; i <= 5; i++)
            {
                xPositions[i] = xPositions[i - 1] + widths[i - 1];
            }

            float[] yPositions = new float[6];
            yPositions[0] = rect.yMin;
            for (int i = 1; i <= 5; i++)
            {
                yPositions[i] = yPositions[i - 1] + heights[i - 1];
            }

            // Build slices
            SliceRect[,] slices = new SliceRect[5, 5];
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    slices[x, y] = new SliceRect
                    {
                        Position = new Vector2(xPositions[x], yPositions[y]),
                        Size = new Vector2(widths[x], heights[y]),
                        UVMin = new Vector2(uvXBorders[x], uvYBorders[y]),
                        UVMax = new Vector2(uvXBorders[x + 1], uvYBorders[y + 1])
                    };
                }
            }

            // Build the mesh
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var slice = slices[x, y];
                    Color sliceColor = DebuggingView ? new Color((float)x / 4, (float)y / 4, 0.5f) : color;

                    // 튀어나온 부분을 투명하게 처리
                    if (slice.Size.x > 0 && slice.Size.y > 0)
                    {
                        AddQuad(
                            vh,
                            slice.Position,
                            slice.Position + slice.Size,
                            slice.UVMin,
                            slice.UVMax,
                            sliceColor
                        );
                    }
                }
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topRight,
            Vector2 uvBottomLeft, Vector2 uvTopRight, Color color)
        {
            int vertexIndex = vh.currentVertCount;

            vh.AddVert(new Vector3(bottomLeft.x, bottomLeft.y), color,
                new Vector2(uvBottomLeft.x, uvBottomLeft.y));
            vh.AddVert(new Vector3(bottomLeft.x, topRight.y), color,
                new Vector2(uvBottomLeft.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, topRight.y), color,
                new Vector2(uvTopRight.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, bottomLeft.y), color,
                new Vector2(uvTopRight.x, uvBottomLeft.y));

            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }

        private TwentyFiveSliceData LoadTwentyFiveSliceData(Sprite targetSprite)
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(targetSprite);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                return JsonUtility.FromJson<TwentyFiveSliceData>(importer.userData);
            }
#endif

            return new TwentyFiveSliceData
            {
                verticalBorders = new float[] { 20f, 40f, 60f, 80f },
                horizontalBorders = new float[] { 20f, 40f, 60f, 80f }
            };
        }
    }
}