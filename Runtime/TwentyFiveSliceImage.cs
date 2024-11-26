using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TwentyFiveSliceImage : Image
{
    private struct SliceRect
    {
        public Vector2 position;  // 좌하단 위치
        public Vector2 size;      // 너비, 높이
        public Vector2 uvMin;     // UV 좌하단
        public Vector2 uvMax;     // UV 우상단
    }

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
        Rect texRect = overrideSprite.rect;

        // 보더값들을 픽셀 단위로 변환
        float[] xBorders = new float[6];
        float[] yBorders = new float[6];
        float[] uvXBorders = new float[6];
        float[] uvYBorders = new float[6];

        // 경계값 초기화
        xBorders[0] = 0;
        yBorders[0] = 0;
        xBorders[5] = 100;
        yBorders[5] = 100;
        uvXBorders[0] = outer.x;
        uvYBorders[0] = outer.y;
        uvXBorders[5] = outer.z;
        uvYBorders[5] = outer.w;

        // 중간 경계값 설정
        for (int i = 0; i < 4; i++)
        {
            xBorders[i + 1] = sliceData.verticalBorders[i];
            yBorders[i + 1] = sliceData.horizontalBorders[i];
            uvXBorders[i + 1] = Mathf.Lerp(outer.x, outer.z, sliceData.verticalBorders[i] / 100f);
            uvYBorders[i + 1] = Mathf.Lerp(outer.y, outer.w, sliceData.horizontalBorders[i] / 100f);
        }

        // 각 슬라이스의 정보 계산
        SliceRect[,] slices = new SliceRect[5, 5];
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                slices[x, y] = new SliceRect
                {
                    uvMin = new Vector2(uvXBorders[x], uvYBorders[y]),
                    uvMax = new Vector2(uvXBorders[x + 1], uvYBorders[y + 1])
                };
            }
        }

        // 1. 고정 크기 슬라이스 배치 (크기 변경 불가능한 슬라이스)
        PlaceFixedSlices(slices, rect, xBorders, yBorders);

        // 2. 한 방향으로만 늘어나는 슬라이스 배치
        PlaceStretchableSlices(slices, rect, xBorders, yBorders);

        // 3. 모든 방향으로 늘어나는 슬라이스 배치
        PlaceFullyStretchableSlices(slices, rect, xBorders, yBorders);

        // 메시 생성
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                var slice = slices[x, y];
                AddQuad(
                    vh,
                    slice.position,
                    slice.position + slice.size,
                    slice.uvMin,
                    slice.uvMax,
                    color
                );
            }
        }
    }

    private void PlaceFixedSlices(SliceRect[,] slices, Rect targetRect, float[] xBorders, float[] yBorders)
    {
        // 원본 크기 계산
        float[] originalWidths = new float[5];
        float[] originalHeights = new float[5];
        for (int i = 0; i < 5; i++)
        {
            originalWidths[i] = (xBorders[i + 1] - xBorders[i]) * overrideSprite.rect.width / 100f;
            originalHeights[i] = (yBorders[i + 1] - yBorders[i]) * overrideSprite.rect.height / 100f;
        }

        // 코너 슬라이스 (1, 5, 21, 25)
        PlaceFixedSlice(slices, 0, 0, targetRect.min, originalWidths[0], originalHeights[0]); // 1
        PlaceFixedSlice(slices, 4, 0, new Vector2(targetRect.xMax - originalWidths[4], targetRect.yMin), originalWidths[4], originalHeights[0]); // 5
        PlaceFixedSlice(slices, 0, 4, new Vector2(targetRect.xMin, targetRect.yMax - originalHeights[4]), originalWidths[0], originalHeights[4]); // 21
        PlaceFixedSlice(slices, 4, 4, new Vector2(targetRect.xMax - originalWidths[4], targetRect.yMax - originalHeights[4]), originalWidths[4], originalHeights[4]); // 25

        // 가장자리 중앙 (3, 11, 15, 23)
        float centerX = targetRect.center.x;
        float centerY = targetRect.center.y;

        PlaceFixedSlice(slices, 2, 0, new Vector2(centerX - originalWidths[2] * 0.5f, targetRect.yMin), originalWidths[2], originalHeights[0]); // 3
        PlaceFixedSlice(slices, 0, 2, new Vector2(targetRect.xMin, centerY - originalHeights[2] * 0.5f), originalWidths[0], originalHeights[2]); // 11
        PlaceFixedSlice(slices, 4, 2, new Vector2(targetRect.xMax - originalWidths[4], centerY - originalHeights[2] * 0.5f), originalWidths[4], originalHeights[2]); // 15
        PlaceFixedSlice(slices, 2, 4, new Vector2(centerX - originalWidths[2] * 0.5f, targetRect.yMax - originalHeights[4]), originalWidths[2], originalHeights[4]); // 23

        // 중앙 (13)
        PlaceFixedSlice(slices, 2, 2, new Vector2(centerX - originalWidths[2] * 0.5f, centerY - originalHeights[2] * 0.5f), originalWidths[2], originalHeights[2]); // 13
    }

    private void PlaceFixedSlice(SliceRect[,] slices, int x, int y, Vector2 position, float width, float height)
    {
        slices[x, y].position = position;
        slices[x, y].size = new Vector2(width, height);
    }

    private void PlaceStretchableSlices(SliceRect[,] slices, Rect targetRect, float[] xBorders, float[] yBorders)
    {
        // 가로로만 늘어나는 슬라이스 (2, 4, 12, 14, 22, 24)
        for (int x = 1; x < 4; x++)
        {
            if (x == 2) continue; // 중앙 열 건너뛰기

            float height = (yBorders[1] - yBorders[0]) * overrideSprite.rect.height / 100f; // 원본 높이 유지
                
            // 상단 (2, 4)
            slices[x, 0].position = new Vector2(
                slices[x-1, 0].position.x + slices[x-1, 0].size.x,
                targetRect.yMin
            );
            slices[x, 0].size = new Vector2(
                slices[x+1, 0].position.x - slices[x, 0].position.x,
                height
            );

            // 중단 (12, 14)
            height = (yBorders[3] - yBorders[2]) * overrideSprite.rect.height / 100f;
            slices[x, 2].position = new Vector2(
                slices[x-1, 2].position.x + slices[x-1, 2].size.x,
                slices[2, 2].position.y
            );
            slices[x, 2].size = new Vector2(
                slices[x+1, 2].position.x - slices[x, 2].position.x,
                height
            );

            // 하단 (22, 24)
            height = (yBorders[5] - yBorders[4]) * overrideSprite.rect.height / 100f;
            slices[x, 4].position = new Vector2(
                slices[x-1, 4].position.x + slices[x-1, 4].size.x,
                slices[2, 4].position.y
            );
            slices[x, 4].size = new Vector2(
                slices[x+1, 4].position.x - slices[x, 4].position.x,
                height
            );
        }

        // 세로로만 늘어나는 슬라이스 (6, 8, 10, 16, 18, 20)
        for (int y = 1; y < 4; y++)
        {
            if (y == 2) continue; // 중앙 행 건너뛰기

            float width = (xBorders[1] - xBorders[0]) * overrideSprite.rect.width / 100f; // 원본 너비 유지
                
            // 좌측 (6, 16)
            slices[0, y].position = new Vector2(
                targetRect.xMin,
                slices[0, y-1].position.y + slices[0, y-1].size.y
            );
            slices[0, y].size = new Vector2(
                width,
                slices[0, y+1].position.y - slices[0, y].position.y
            );

            // 중앙 (8, 18)
            width = (xBorders[3] - xBorders[2]) * overrideSprite.rect.width / 100f;
            slices[2, y].position = new Vector2(
                slices[2, 2].position.x,
                slices[2, y-1].position.y + slices[2, y-1].size.y
            );
            slices[2, y].size = new Vector2(
                width,
                slices[2, y+1].position.y - slices[2, y].position.y
            );

            // 우측 (10, 20)
            width = (xBorders[5] - xBorders[4]) * overrideSprite.rect.width / 100f;
            slices[4, y].position = new Vector2(
                targetRect.xMax - width,
                slices[4, y-1].position.y + slices[4, y-1].size.y
            );
            slices[4, y].size = new Vector2(
                width,
                slices[4, y+1].position.y - slices[4, y].position.y
            );
        }
    }

    private void PlaceFullyStretchableSlices(SliceRect[,] slices, Rect targetRect, float[] xBorders, float[] yBorders)
    {
        // 모든 방향으로 늘어나는 슬라이스 (7, 9, 17, 19)
        for (int y = 1; y < 4; y += 2)
        {
            for (int x = 1; x < 4; x += 2)
            {
                if (x == 2 || y == 2) continue; // 중앙 행/열 건너뛰기

                slices[x, y].position = new Vector2(
                    slices[x-1, y].position.x + slices[x-1, y].size.x,
                    slices[x, y-1].position.y + slices[x, y-1].size.y
                );
                slices[x, y].size = new Vector2(
                    slices[x+1, y].position.x - slices[x, y].position.x,
                    slices[x, y+1].position.y - slices[x, y].position.y
                );
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