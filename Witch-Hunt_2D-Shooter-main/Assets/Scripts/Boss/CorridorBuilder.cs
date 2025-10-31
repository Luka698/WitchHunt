using UnityEngine;

/// <summary>
/// Xây hành lang bằng cách nhân bản floor plate và tường bao quanh,
/// rộng widthPlates, dài lengthPlates. Chừa lỗ mở ở đầu ra.
/// </summary>
public class CorridorBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPlatePrefab;   // prefab "Plates" bạn đang dùng ở Level3
    public GameObject wallPrefab;         // prefab tường có BoxCollider2D (Static)

    [Header("Size (in plates)")]
    [Min(1)] public int widthPlates = 2;
    [Min(1)] public int lengthPlates = 6;

    [Header("Orientation")]
    public bool alongPositiveY = true; // true: hành lang kéo dọc theo +Y; false: theo +X

    [Header("Build")]
    public bool clearChildrenBeforeBuild = true;
    public bool buildOnStart = true;

    [Header("Entrance/Exit")]
    public bool createExitTrigger = true;
    public Vector2 exitTriggerThicknessPlates = new Vector2(0.2f, 0.2f); // dày mỏng collider trigger theo đơn vị plate

    private Vector2 plateSize; // world units (tính từ SpriteRenderer)

    void Start()
    {
        if (buildOnStart) Build();
    }

    public void Build()
    {
        if (!floorPlatePrefab || !wallPrefab)
        {
            Debug.LogError("[CorridorBuilder] Thiếu prefab floorPlatePrefab hoặc wallPrefab");
            return;
        }

        var sr = floorPlatePrefab.GetComponentInChildren<SpriteRenderer>();
        if (!sr)
        {
            Debug.LogError("[CorridorBuilder] floorPlatePrefab không có SpriteRenderer để tính kích thước.");
            return;
        }

        plateSize = sr.bounds.size; // kích thước 1 plate theo world units

        if (clearChildrenBeforeBuild)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 1) Build floor grid
        for (int y = 0; y < (alongPositiveY ? lengthPlates : widthPlates); y++)
        {
            for (int x = 0; x < (alongPositiveY ? widthPlates : lengthPlates); x++)
            {
                Vector3 pos = LocalFromPlateXY(x, y);
                var tile = Instantiate(floorPlatePrefab, transform);
                tile.name = $"Floor_{x}_{y}";
                tile.transform.localPosition = pos;
            }
        }

        // 2) Build walls on perimeter (trừ đầu vào & đầu ra)
        BuildWallsPerimeter();

        // 3) Exit trigger ở cuối hành lang (mở sang phòng boss)
        if (createExitTrigger)
            CreateExitTrigger();
    }

    Vector3 LocalFromPlateXY(int px, int py)
    {
        // px theo bề rộng, py theo chiều dài
        float dx = plateSize.x * px;
        float dy = plateSize.y * py;

        if (alongPositiveY)
            return new Vector3(dx, dy, 0f);
        else
            return new Vector3(dy, dx, 0f); // hoán đổi nếu kéo theo +X
    }

    void BuildWallsPerimeter()
    {
        int L = lengthPlates;
        int W = widthPlates;

        for (int i = 0; i < L; i++)
        {
            bool isExitRow = (i == L - 1);

            // Bên trái
            var left = Instantiate(wallPrefab, transform);
            left.name = $"Wall_SideA_{i}";
            left.transform.localPosition = LocalFromPlateXY(-1, i);
            FitWallToPlate(left, vertical: alongPositiveY);

            // Bên phải
            var right = Instantiate(wallPrefab, transform);
            right.name = $"Wall_SideB_{i}";
            right.transform.localPosition = LocalFromPlateXY(W, i);
            FitWallToPlate(right, vertical: alongPositiveY);

            // Bỏ tường ở đầu vào (i == 0)
            if (i == 0)
                continue;

            // Bỏ tường ở đầu ra (isExitRow)
            if (isExitRow)
                continue;
        }
    }

    void FitWallToPlate(GameObject wall, bool vertical)
    {
        wall.transform.localScale = new Vector3(
            vertical ? plateSize.x * 0.9f : plateSize.x * (widthPlates + 2),
            vertical ? plateSize.y : plateSize.y * 0.9f,
            1f
        );
    }

    void CreateExitTrigger()
    {
        var trig = new GameObject("BossRoomTrigger");
        trig.transform.SetParent(transform, false);

        // Đặt trigger tại cuối hành lang
        Vector3 basePos = LocalFromPlateXY(0, lengthPlates - 1);
        float midX = (alongPositiveY ? (widthPlates - 1) * plateSize.x * 0.5f
                                     : (lengthPlates - 1) * plateSize.x * 0.5f);

        Vector3 localPos;
        if (alongPositiveY)
            localPos = new Vector3(midX, basePos.y + plateSize.y * 0.5f, 0f);
        else
            localPos = new Vector3(basePos.x + plateSize.x * 0.5f, midX, 0f);

        trig.transform.localPosition = localPos;

        var box = trig.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        // Kích thước bằng bề rộng hành lang × mỏng theo chiều dài (để vừa “cửa”)
        float triggerW = (alongPositiveY ? widthPlates : lengthPlates) * plateSize.x;
        float triggerH = (alongPositiveY ? exitTriggerThicknessPlates.y : exitTriggerThicknessPlates.x) * plateSize.y;
        box.size = alongPositiveY ? new Vector2(triggerW, triggerH)
                                  : new Vector2(triggerH, triggerW);
    }
}
