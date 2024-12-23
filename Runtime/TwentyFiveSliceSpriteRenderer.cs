using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TwentyFiveSlicer.Runtime
{
    /// <summary>
    /// A 25-slice renderer that behaves similarly to a SpriteRenderer:
    /// - Based on MeshRenderer + MeshFilter.
    /// - Exposes sprite, color, flipX/flipY, pivot, sortingLayer, sortingOrder, etc.
    /// - The "size" property defines the final width and height. If (0,0), it uses the sprite's original pixel size.
    /// - It retrieves 25-slice data from SliceDataManager, then distributes the corner/edge/center areas accordingly.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class TwentyFiveSliceSpriteRenderer : MonoBehaviour
    {
        //========================================================
        // Inspector Fields
        //========================================================

        [FormerlySerializedAs("_sprite")] [Header("Sprite & 25-Slice Settings")] [SerializeField]
        private Sprite sprite;

        [FormerlySerializedAs("_debuggingView")]
        [Tooltip("If true, the 25-slice is displayed in a debug-style color distribution for each slice area.")]
        [SerializeField]
        private bool debuggingView;

        [FormerlySerializedAs("_color")] [Header("Renderer Basic Settings")] [SerializeField]
        private Color color = Color.white;

        [FormerlySerializedAs("_flipX")] [SerializeField]
        private bool flipX;

        [FormerlySerializedAs("_flipY")] [SerializeField]
        private bool flipY;

        [FormerlySerializedAs("_useSpritePivot")]
        [Header("Pivot & Size")]
        [Tooltip("If true, uses the sprite.pivot. If false, uses the customPivot field.")]
        [SerializeField]
        private bool useSpritePivot = true;

        [FormerlySerializedAs("_customPivot")]
        [Tooltip("If useSpritePivot=false, this pivot is used (local space).")]
        [SerializeField]
        private Vector2 customPivot = Vector2.zero;

        [FormerlySerializedAs("_pixelsPerUnit")]
        [Tooltip("Pixels per unit for world-space conversion. If 0, uses sprite.pixelsPerUnit.")]
        [SerializeField]
        private float pixelsPerUnit;

        [FormerlySerializedAs("_size")]
        [Tooltip("The final width and height of the 25-slice mesh. If (0,0), uses the sprite's original pixel size.")]
        [SerializeField]
        private Vector2 size = Vector2.zero;

        [FormerlySerializedAs("_sortingLayerName")] [Header("Sorting")] [SerializeField]
        private string sortingLayerName = "Default";

        [FormerlySerializedAs("_sortingOrder")] [SerializeField]
        private int sortingOrder;

        //========================================================
        // Internal Fields
        //========================================================

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private Mesh _generatedMesh;

        // These flags determine which columns/rows are fixed (e.g. corner/edges) vs. stretchable.
        private readonly bool[] _fixedColumns = { true, false, true, false, true };
        private readonly bool[] _fixedRows = { true, false, true, false, true };

        private bool _needMeshUpdate = true; // If true, triggers a mesh rebuild in RebuildMeshIfNeeded()

        //========================================================
        // MonoBehaviour Methods
        //========================================================

        private void OnEnable()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            if (_generatedMesh == null)
            {
                _generatedMesh = new Mesh { name = "TwentyFiveSliceMesh (Generated)" };
            }

            UpdateRendererSettings();
            RebuildMeshIfNeeded();
        }

        private void OnValidate()
        {
            // Called when inspector values change
            _needMeshUpdate = true;
            UpdateRendererSettings();
        }

        private void Update()
        {
            // In Edit Mode, rebuild if needed
            if (!Application.isPlaying)
            {
                RebuildMeshIfNeeded();
            }
        }

        //========================================================
        // Public Properties (similar to SpriteRenderer)
        //========================================================

        /// <summary>
        /// The sprite to be rendered in this 25-slice mesh.
        /// </summary>
        public Sprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite == value) return;
                sprite = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// Rendering color (applied to the material).
        /// </summary>
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                UpdateRendererSettings();
            }
        }

        /// <summary>
        /// Flip horizontally.
        /// </summary>
        public bool FlipX
        {
            get => flipX;
            set
            {
                if (flipX == value) return;
                flipX = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// Flip vertically.
        /// </summary>
        public bool FlipY
        {
            get => flipY;
            set
            {
                if (flipY == value) return;
                flipY = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// If true, shows a debug color distribution in the 25-slice (for each slice area).
        /// </summary>
        public bool DebuggingView
        {
            get => debuggingView;
            set
            {
                if (debuggingView == value) return;
                debuggingView = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// Whether to use the sprite's built-in pivot or a custom pivot.
        /// </summary>
        public bool UseSpritePivot
        {
            get => useSpritePivot;
            set
            {
                if (useSpritePivot == value) return;
                useSpritePivot = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// A user-defined pivot if UseSpritePivot=false.
        /// </summary>
        public Vector2 CustomPivot
        {
            get => customPivot;
            set
            {
                customPivot = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// Pixels Per Unit override. If 0, uses sprite.pixelsPerUnit.
        /// </summary>
        public float PixelsPerUnit
        {
            get => pixelsPerUnit;
            set
            {
                pixelsPerUnit = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// The final width/height of the 25-slice. If (0,0), uses sprite's original size.
        /// Similar to SpriteRenderer drawMode = Sliced's 'size' property.
        /// </summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                if (size == value) return;
                size = value;
                _needMeshUpdate = true;
            }
        }

        /// <summary>
        /// Local-space bounds of the generated mesh. For world-space, transform.TransformPoint is needed.
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                if (_generatedMesh != null)
                    return _generatedMesh.bounds;
                return new Bounds();
            }
        }

        //========================================================
        // Private Methods
        //========================================================

        /// <summary>
        /// Updates basic settings related to MeshRenderer, such as sorting and material color.
        /// </summary>
        private void UpdateRendererSettings()
        {
            if (_meshRenderer == null) return;

            // Set the sorting layer and order
            _meshRenderer.sortingLayerName = sortingLayerName;
            _meshRenderer.sortingOrder = sortingOrder;

            // Ensure a material is assigned
            if (_meshRenderer.sharedMaterial == null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.name = "TwentyFiveSliceMaterial";
                _meshRenderer.sharedMaterial = mat;
            }

            // Update material color
            _meshRenderer.sharedMaterial.color = color;
        }

        /// <summary>
        /// Rebuild the mesh if the flag is set.
        /// </summary>
        private void RebuildMeshIfNeeded()
        {
            if (!_needMeshUpdate) return;
            _needMeshUpdate = false;

            if (sprite == null)
            {
                // If no sprite, clear the mesh
                if (_generatedMesh != null) _generatedMesh.Clear();
                if (_meshFilter) _meshFilter.sharedMesh = _generatedMesh;
                return;
            }

            BuildTwentyFiveSliceMesh(_generatedMesh, sprite);
            _meshFilter.sharedMesh = _generatedMesh;
        }

        /// <summary>
        /// Core logic to build the 25-slice mesh.
        /// </summary>
        private void BuildTwentyFiveSliceMesh(Mesh mesh, Sprite targetSprite)
        {
            mesh.Clear();

            // 1) Get 25-slice data from SliceDataManager
            if (!SliceDataManager.Instance.TryGetSliceData(targetSprite, out var sliceData))
            {
                // If no slice data, just build a single quad
                BuildSimpleQuad(mesh, targetSprite);
                return;
            }

            Rect spriteRect = targetSprite.rect;
            Vector4 outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(targetSprite);
            float realPpu = (pixelsPerUnit > 0f) ? pixelsPerUnit : targetSprite.pixelsPerUnit;
            if (realPpu <= 0f) realPpu = 100f;

            // 2) Determine the final size
            Vector2 finalSize;
            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                // Use the inspector-specified size
                finalSize = size;
            }
            else
            {
                // Use sprite's pixel dimensions
                finalSize = new Vector2(spriteRect.width / realPpu, spriteRect.height / realPpu);
            }

            // Borders
            float[] xBordersPercent = GetXBordersPercent(sliceData);
            float[] yBordersPercent = GetYBordersPercent(sliceData);

            float[] uvXBorders = GetUVBorders(outerUV.x, outerUV.z, xBordersPercent);
            float[] uvYBorders = GetUVBorders(outerUV.y, outerUV.w, yBordersPercent);

            // Original pixel-based sizes
            float[] originalWidths = GetOriginalSizes(xBordersPercent, spriteRect.width);
            float[] originalHeights = GetOriginalSizes(yBordersPercent, spriteRect.height);

            // Distribute corners/edges (fixed) vs center (stretchable)
            float[] widths = GetAdjustedSizes(finalSize.x, originalWidths.Select(o => o / realPpu).ToArray(),
                _fixedColumns);
            float[] heights = GetAdjustedSizes(finalSize.y, originalHeights.Select(o => o / realPpu).ToArray(),
                _fixedRows);

            // 3) pivot
            Vector2 pivotOffset = useSpritePivot
                ? (targetSprite.pivot / realPpu)
                : customPivot;

            // Position arrays
            float[] xPositions = GetPositions(0, widths);
            float[] yPositions = GetPositions(0, heights);

            // Subtract pivot
            for (int i = 0; i < xPositions.Length; i++)
                xPositions[i] -= pivotOffset.x;
            for (int i = 0; i < yPositions.Length; i++)
                yPositions[i] -= pivotOffset.y;

            // flipX/flipY
            if (flipX)
                uvXBorders = uvXBorders.Reverse().ToArray();
            if (flipY)
                uvYBorders = uvYBorders.Reverse().ToArray();

            // 4) Build the mesh (25 slices max)
            var vertices = new Vector3[5 * 5 * 4];
            var uv = new Vector2[5 * 5 * 4];
            var colors = new Color[5 * 5 * 4];
            var triangles = new int[5 * 5 * 6];

            int vertIndex = 0;
            int triIndex = 0;

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    float w = widths[col];
                    float h = heights[row];
                    if (w < 0.0001f || h < 0.0001f)
                        continue;

                    float xMin = xPositions[col];
                    float yMin = yPositions[row];
                    float xMax = xMin + w;
                    float yMax = yMin + h;

                    // uv
                    float uMin = uvXBorders[col];
                    float uMax = uvXBorders[col + 1];
                    float vMin = uvYBorders[row];
                    float vMax = uvYBorders[row + 1];

                    // 4 vertices
                    vertices[vertIndex + 0] = new Vector3(xMin, yMin, 0);
                    vertices[vertIndex + 1] = new Vector3(xMin, yMax, 0);
                    vertices[vertIndex + 2] = new Vector3(xMax, yMax, 0);
                    vertices[vertIndex + 3] = new Vector3(xMax, yMin, 0);

                    uv[vertIndex + 0] = new Vector2(uMin, vMin);
                    uv[vertIndex + 1] = new Vector2(uMin, vMax);
                    uv[vertIndex + 2] = new Vector2(uMax, vMax);
                    uv[vertIndex + 3] = new Vector2(uMax, vMin);

                    // Debug color if needed
                    Color sliceColor = debuggingView
                        ? new Color(col / 4f, row / 4f, (col + row) / 8f, 1f)
                        : Color.white;
                    colors[vertIndex + 0] = sliceColor;
                    colors[vertIndex + 1] = sliceColor;
                    colors[vertIndex + 2] = sliceColor;
                    colors[vertIndex + 3] = sliceColor;

                    // Indices
                    triangles[triIndex + 0] = vertIndex + 0;
                    triangles[triIndex + 1] = vertIndex + 1;
                    triangles[triIndex + 2] = vertIndex + 2;
                    triangles[triIndex + 3] = vertIndex + 0;
                    triangles[triIndex + 4] = vertIndex + 2;
                    triangles[triIndex + 5] = vertIndex + 3;

                    vertIndex += 4;
                    triIndex += 6;
                }
            }

            // Trim arrays to the used portion
            var finalVerts = new Vector3[vertIndex];
            var finalUVs = new Vector2[vertIndex];
            var finalCols = new Color[vertIndex];
            var finalTris = new int[triIndex];

            System.Array.Copy(vertices, finalVerts, vertIndex);
            System.Array.Copy(uv, finalUVs, vertIndex);
            System.Array.Copy(colors, finalCols, vertIndex);
            System.Array.Copy(triangles, finalTris, triIndex);

            mesh.vertices = finalVerts;
            mesh.uv = finalUVs;
            mesh.colors = finalCols;
            mesh.triangles = finalTris;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            // Assign the sprite texture to the material
            if (_meshRenderer != null && _meshRenderer.sharedMaterial != null)
            {
                _meshRenderer.sharedMaterial.mainTexture = targetSprite.texture;
            }
        }

        /// <summary>
        /// Fallback when 25-slice data does not exist (just a single quad).
        /// </summary>
        private void BuildSimpleQuad(Mesh mesh, Sprite targetSprite)
        {
            mesh.Clear();

            Rect spriteRect = targetSprite.rect;
            Vector4 uvRect = UnityEngine.Sprites.DataUtility.GetOuterUV(targetSprite);

            float realPpu = (pixelsPerUnit > 0f) ? pixelsPerUnit : targetSprite.pixelsPerUnit;
            if (realPpu <= 0f) realPpu = 100f;

            // If size is zero, use the original sprite size
            float w, h;
            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                w = size.x;
                h = size.y;
            }
            else
            {
                w = spriteRect.width / realPpu;
                h = spriteRect.height / realPpu;
            }

            Vector2 pivotOffset = useSpritePivot
                ? (targetSprite.pivot / realPpu)
                : customPivot;

            float xMin = -pivotOffset.x;
            float yMin = -pivotOffset.y;
            float xMax = xMin + w;
            float yMax = yMin + h;

            // Handle flip
            float uMin = flipX ? uvRect.z : uvRect.x;
            float uMax = flipX ? uvRect.x : uvRect.z;
            float vMin = flipY ? uvRect.w : uvRect.y;
            float vMax = flipY ? uvRect.y : uvRect.w;

            var verts = new Vector3[4];
            var uvs = new Vector2[4];
            var tris = new int[] { 0, 1, 2, 2, 3, 0 };

            verts[0] = new Vector3(xMin, yMin, 0);
            verts[1] = new Vector3(xMin, yMax, 0);
            verts[2] = new Vector3(xMax, yMax, 0);
            verts[3] = new Vector3(xMax, yMin, 0);

            uvs[0] = new Vector2(uMin, vMin);
            uvs[1] = new Vector2(uMin, vMax);
            uvs[2] = new Vector2(uMax, vMax);
            uvs[3] = new Vector2(uMax, vMin);

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            if (_meshRenderer != null && _meshRenderer.sharedMaterial != null)
            {
                _meshRenderer.sharedMaterial.mainTexture = targetSprite.texture;
            }
        }

        //========================================================
        // 25-slice helper methods
        //========================================================

        private float[] GetXBordersPercent(TwentyFiveSliceData sliceData)
        {
            return new[]
            {
                0f,
                sliceData.verticalBorders[0],
                sliceData.verticalBorders[1],
                sliceData.verticalBorders[2],
                sliceData.verticalBorders[3],
                100f
            };
        }

        private float[] GetYBordersPercent(TwentyFiveSliceData sliceData)
        {
            return new[]
            {
                0f,
                100f - sliceData.horizontalBorders[3],
                100f - sliceData.horizontalBorders[2],
                100f - sliceData.horizontalBorders[1],
                100f - sliceData.horizontalBorders[0],
                100f
            };
        }

        private float[] GetUVBorders(float min, float max, float[] bordersPercent)
        {
            return new[]
            {
                min,
                Mathf.Lerp(min, max, bordersPercent[1] / 100f),
                Mathf.Lerp(min, max, bordersPercent[2] / 100f),
                Mathf.Lerp(min, max, bordersPercent[3] / 100f),
                Mathf.Lerp(min, max, bordersPercent[4] / 100f),
                max
            };
        }

        private float[] GetOriginalSizes(float[] bordersPercent, float totalSize)
        {
            return new[]
            {
                (bordersPercent[1] - bordersPercent[0]) * totalSize / 100f,
                (bordersPercent[2] - bordersPercent[1]) * totalSize / 100f,
                (bordersPercent[3] - bordersPercent[2]) * totalSize / 100f,
                (bordersPercent[4] - bordersPercent[3]) * totalSize / 100f,
                (bordersPercent[5] - bordersPercent[4]) * totalSize / 100f
            };
        }

        /// <summary>
        /// Distributes corner/edge vs. middle areas, setting which parts are fixed or stretched.
        /// </summary>
        private float[] GetAdjustedSizes(float totalSize, float[] originalSizes, bool[] fixedSizes)
        {
            float totalFixedSize = 0f;
            float stretchableSizeRatio = 0f;

            for (int i = 0; i < 5; i++)
            {
                if (fixedSizes[i]) totalFixedSize += originalSizes[i];
                else stretchableSizeRatio += originalSizes[i];
            }

            float[] adjustedSizes = new float[5];
            float totalStretchableSize = Mathf.Max(0, totalSize - totalFixedSize);

            if (totalSize < totalFixedSize)
            {
                // If total final size < sum of fixed corners/edges, scale them down proportionally
                float scaleRatio = (totalFixedSize < 0.0001f) ? 0f : (totalSize / totalFixedSize);
                for (int i = 0; i < 5; i++)
                {
                    adjustedSizes[i] = fixedSizes[i] ? originalSizes[i] * scaleRatio : 0f;
                }
            }
            else
            {
                // Otherwise, distribute the leftover to the middle
                for (int i = 0; i < 5; i++)
                {
                    if (fixedSizes[i])
                    {
                        adjustedSizes[i] = originalSizes[i];
                    }
                    else
                    {
                        if (stretchableSizeRatio > 0.0001f)
                            adjustedSizes[i] = totalStretchableSize * (originalSizes[i] / stretchableSizeRatio);
                        else
                            adjustedSizes[i] = 0f;
                    }
                }
            }

            return adjustedSizes;
        }

        /// <summary>
        /// Calculates the positions of each slice segment from 'start' onward.
        /// </summary>
        private float[] GetPositions(float start, float[] sizes)
        {
            float[] positions = new float[6];
            positions[0] = start;

            for (int i = 1; i <= 5; i++)
            {
                positions[i] = positions[i - 1] + sizes[i - 1];
            }

            return positions;
        }
    }
}