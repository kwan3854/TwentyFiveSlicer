using UnityEngine;
using UnityEngine.UI;

namespace TwentyFiveSlicer.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class TwentyFiveSliceImage : Image
    {
        private struct SliceRect
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector2 UVMin;
            public Vector2 UVMax;
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
        private string _cachedHash = null;
        private Sprite _cachedSprite = null;
        private TwentyFiveSliceData _cachedSliceData = null;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (overrideSprite == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            var sliceData = GetSliceDataWithCache(overrideSprite);
            if (sliceData == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Rect rect = GetPixelAdjustedRect();
            Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(overrideSprite);
            Rect spriteRect = overrideSprite.rect;

            float[] xBordersPercent = { 0f, sliceData.verticalBorders[0], sliceData.verticalBorders[1], sliceData.verticalBorders[2], sliceData.verticalBorders[3], 100f };
            float[] yBordersPercent = { 0f, 100f - sliceData.horizontalBorders[3], 100f - sliceData.horizontalBorders[2], 100f - sliceData.horizontalBorders[1], 100f - sliceData.horizontalBorders[0], 100f };

            float[] uvXBorders = { outer.x, Mathf.Lerp(outer.x, outer.z, xBordersPercent[1] / 100f), Mathf.Lerp(outer.x, outer.z, xBordersPercent[2] / 100f), Mathf.Lerp(outer.x, outer.z, xBordersPercent[3] / 100f), Mathf.Lerp(outer.x, outer.z, xBordersPercent[4] / 100f), outer.z };
            float[] uvYBorders = { outer.y, Mathf.Lerp(outer.y, outer.w, yBordersPercent[1] / 100f), Mathf.Lerp(outer.y, outer.w, yBordersPercent[2] / 100f), Mathf.Lerp(outer.y, outer.w, yBordersPercent[3] / 100f), Mathf.Lerp(outer.y, outer.w, yBordersPercent[4] / 100f), outer.w };

            float[] originalWidths = { (xBordersPercent[1] - xBordersPercent[0]) * spriteRect.width / 100f, (xBordersPercent[2] - xBordersPercent[1]) * spriteRect.width / 100f, (xBordersPercent[3] - xBordersPercent[2]) * spriteRect.width / 100f, (xBordersPercent[4] - xBordersPercent[3]) * spriteRect.width / 100f, (xBordersPercent[5] - xBordersPercent[4]) * spriteRect.width / 100f };
            float[] originalHeights = { (yBordersPercent[1] - yBordersPercent[0]) * spriteRect.height / 100f, (yBordersPercent[2] - yBordersPercent[1]) * spriteRect.height / 100f, (yBordersPercent[3] - yBordersPercent[2]) * spriteRect.height / 100f, (yBordersPercent[4] - yBordersPercent[3]) * spriteRect.height / 100f, (yBordersPercent[5] - yBordersPercent[4]) * spriteRect.height / 100f };

            bool[] fixedColumns = { true, false, true, false, true };
            bool[] fixedRows = { true, false, true, false, true };

            float totalFixedWidth = 0f, totalFixedHeight = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedColumns[i]) totalFixedWidth += originalWidths[i];
                if (fixedRows[i]) totalFixedHeight += originalHeights[i];
            }

            float totalStretchableWidth = Mathf.Max(0, rect.width - totalFixedWidth);
            float totalStretchableHeight = Mathf.Max(0, rect.height - totalFixedHeight);

            float[] widths = new float[5];
            float stretchableWidthRatio = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedColumns[i]) widths[i] = originalWidths[i];
                else stretchableWidthRatio += originalWidths[i];
            }
            for (int i = 0; i < 5; i++)
            {
                if (!fixedColumns[i]) widths[i] = totalStretchableWidth * (originalWidths[i] / stretchableWidthRatio);
            }

            float[] heights = new float[5];
            float stretchableHeightRatio = 0f;
            for (int i = 0; i < 5; i++)
            {
                if (fixedRows[i]) heights[i] = originalHeights[i];
                else stretchableHeightRatio += originalHeights[i];
            }
            for (int i = 0; i < 5; i++)
            {
                if (!fixedRows[i]) heights[i] = totalStretchableHeight * (originalHeights[i] / stretchableHeightRatio);
            }

            float[] xPositions = new float[6];
            xPositions[0] = rect.xMin;
            for (int i = 1; i <= 5; i++) xPositions[i] = xPositions[i - 1] + widths[i - 1];

            float[] yPositions = new float[6];
            yPositions[0] = rect.yMin;
            for (int i = 1; i <= 5; i++) yPositions[i] = yPositions[i - 1] + heights[i - 1];

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

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var slice = slices[x, y];
                    Color sliceColor = DebuggingView ? new Color((float)x / 4, (float)y / 4, 0.5f) : color;
                    if (slice.Size.x > 0 && slice.Size.y > 0)
                    {
                        AddQuad(vh, slice.Position, slice.Position + slice.Size, slice.UVMin, slice.UVMax, sliceColor);
                    }
                }
            }
        }

        private TwentyFiveSliceData GetSliceDataWithCache(Sprite sprite)
        {
            if (sprite == _cachedSprite) return _cachedSliceData;
            _cachedSprite = sprite;
            _cachedHash = TfsHashGenerator.GenerateUniqueSpriteHash(sprite);
            _cachedSliceData = LoadTwentyFiveSliceData(sprite);
            return _cachedSliceData;
        }

        private void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topRight, Vector2 uvBottomLeft, Vector2 uvTopRight, Color color)
        {
            int vertexIndex = vh.currentVertCount;
            vh.AddVert(new Vector3(bottomLeft.x, bottomLeft.y), color, new Vector2(uvBottomLeft.x, uvBottomLeft.y));
            vh.AddVert(new Vector3(bottomLeft.x, topRight.y), color, new Vector2(uvBottomLeft.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, topRight.y), color, new Vector2(uvTopRight.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, bottomLeft.y), color, new Vector2(uvTopRight.x, uvBottomLeft.y));
            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }

        private TwentyFiveSliceData LoadTwentyFiveSliceData(Sprite targetSprite)
        {
            if (string.IsNullOrEmpty(_cachedHash)) return null;
            TextAsset jsonAsset = Resources.Load<TextAsset>($"TwentyFiveSliceData/{_cachedHash}");
            if (jsonAsset != null) return JsonUtility.FromJson<TwentyFiveSliceData>(jsonAsset.text);
            Debug.LogWarning($"Slice data not found for sprite {targetSprite.name}. Hash: {_cachedHash}");
            return new TwentyFiveSliceData
            {
                verticalBorders = new float[] { 20f, 40f, 60f, 80f },
                horizontalBorders = new float[] { 20f, 40f, 60f, 80f }
            };
        }
    }
}