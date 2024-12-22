using System.Linq;
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
        private readonly bool[] _fixedColumns = { true, false, true, false, true };
        private readonly bool[] _fixedRows = { true, false, true, false, true };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            if (!SliceDataManager.Instance.TryGetSliceData(sprite, out var sliceData))
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Rect rect = GetPixelAdjustedRect();
            Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            Rect spriteRect = sprite.rect;

            float[] xBordersPercent = GetXBordersPercent(sliceData);
            float[] yBordersPercent = GetYBordersPercent(sliceData);

            float[] uvXBorders = GetUVBorders(outer.x, outer.z, xBordersPercent);
            float[] uvYBorders = GetUVBorders(outer.y, outer.w, yBordersPercent);

            float[] originalWidths = GetOriginalSizes(xBordersPercent, spriteRect.width);
            float[] originalHeights = GetOriginalSizes(yBordersPercent, spriteRect.height);

            float[] widths = GetAdjustedSizes(rect.width, originalWidths, _fixedColumns);
            float[] heights = GetAdjustedSizes(rect.height, originalHeights, _fixedRows);

            float[] xPositions = GetPositions(rect.xMin, widths, true);
            float[] yPositions = GetPositions(rect.yMin, heights, false);

            SliceRect[,] slices = GetSlices(xPositions, yPositions, uvXBorders, uvYBorders, widths, heights);

            DrawSlices(vh, slices);
        }

        private float[] GetXBordersPercent(TwentyFiveSliceData sliceData)
        {
            return new float[]
            {
                0f, sliceData.verticalBorders[0], sliceData.verticalBorders[1], sliceData.verticalBorders[2],
                sliceData.verticalBorders[3], 100f
            };
        }

        private float[] GetYBordersPercent(TwentyFiveSliceData sliceData)
        {
            return new float[]
            {
                0f, 100f - sliceData.horizontalBorders[3], 100f - sliceData.horizontalBorders[2],
                100f - sliceData.horizontalBorders[1], 100f - sliceData.horizontalBorders[0], 100f
            };
        }

        private float[] GetUVBorders(float min, float max, float[] bordersPercent)
        {
            return new float[]
            {
                min, Mathf.Lerp(min, max, bordersPercent[1] / 100f), Mathf.Lerp(min, max, bordersPercent[2] / 100f),
                Mathf.Lerp(min, max, bordersPercent[3] / 100f), Mathf.Lerp(min, max, bordersPercent[4] / 100f), max
            };
        }

        private float[] GetOriginalSizes(float[] bordersPercent, float totalSize)
        {
            return new float[]
            {
                (bordersPercent[1] - bordersPercent[0]) * totalSize / 100f,
                (bordersPercent[2] - bordersPercent[1]) * totalSize / 100f,
                (bordersPercent[3] - bordersPercent[2]) * totalSize / 100f,
                (bordersPercent[4] - bordersPercent[3]) * totalSize / 100f,
                (bordersPercent[5] - bordersPercent[4]) * totalSize / 100f
            };
        }

        private float[] GetAdjustedSizes(float totalSize, float[] originalSizes, bool[] fixedSizes)
        {
            float totalFixedSize = 0f;
            float stretchableSizeRatio = 0f;

            // Calculate total fixed size and stretchable size ratio
            for (int i = 0; i < 5; i++)
            {
                if (fixedSizes[i])
                {
                    totalFixedSize += originalSizes[i];
                }
                else
                {
                    stretchableSizeRatio += originalSizes[i];
                }
            }

            float totalStretchableSize = Mathf.Max(0, totalSize - totalFixedSize);
            float[] adjustedSizes = new float[5];

            // If total size is less than total fixed size, scale down the fixed sizes
            if (totalSize < totalFixedSize)
            {
                float scaleRatio = totalSize / totalFixedSize;
                for (int i = 0; i < 5; i++)
                {
                    adjustedSizes[i] = fixedSizes[i] ? originalSizes[i] * scaleRatio : 0;
                }
            }
            // Otherwise, distribute the remaining size proportionally to the stretchable sizes
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    adjustedSizes[i] = fixedSizes[i]
                        ? originalSizes[i]
                        : totalStretchableSize * (originalSizes[i] / stretchableSizeRatio);
                }
            }

            return adjustedSizes;
        }

        private float[] GetPositions(float start, float[] sizes, bool isXPosition)
        {
            float[] positions = new float[6];
            positions[0] = start;

            Rect rect = GetPixelAdjustedRect();
            float maxDelta = isXPosition ? rect.width : rect.height;
            float totalSize = sizes.Sum();
            float[] normalizedSizeRatios = sizes.Select(size => size / totalSize).ToArray();

            for (int i = 1; i <= 5; i++)
            {
                positions[i] = positions[i - 1] + normalizedSizeRatios[i - 1] * maxDelta;
            }

            return positions;
        }

        private SliceRect[,] GetSlices(float[] xPositions, float[] yPositions, float[] uvXBorders, float[] uvYBorders,
            float[] widths, float[] heights)
        {
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

            return slices;
        }

        private void DrawSlices(VertexHelper vh, SliceRect[,] slices)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var slice = slices[x, y];
                    Color sliceColor = DebuggingView ? new Color((float)x / 4, (float)y / 4, (float)(x + y) / 8) : color;
                    if (slice.Size.x > 0 && slice.Size.y > 0)
                    {
                        AddQuad(vh, slice.Position, slice.Position + slice.Size, slice.UVMin, slice.UVMax, sliceColor);
                    }
                }
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topRight, Vector2 uvBottomLeft,
            Vector2 uvTopRight, Color color)
        {
            int vertexIndex = vh.currentVertCount;
            vh.AddVert(new Vector3(bottomLeft.x, bottomLeft.y), color, new Vector2(uvBottomLeft.x, uvBottomLeft.y));
            vh.AddVert(new Vector3(bottomLeft.x, topRight.y), color, new Vector2(uvBottomLeft.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, topRight.y), color, new Vector2(uvTopRight.x, uvTopRight.y));
            vh.AddVert(new Vector3(topRight.x, bottomLeft.y), color, new Vector2(uvTopRight.x, uvBottomLeft.y));
            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }
    }
}