using System.Collections.Generic;
using UnityEngine;

/// Sinh các "ô" rung chấn dạng lưới xung quanh tâm.
/// Ô trung tâm 3x3 (|dx|<=1,|dy|<=1) có collider gây damage; ô rìa chỉ hiển thị & sẽ tự fade rồi hủy.
public class ShockwaveSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab của 1 ô rung chấn (nếu null sẽ tạo rỗng runtime).")]
    public GameObject shockTilePrefab;

    [Tooltip("Sprite hiển thị cho mỗi ô (nếu prefab không có SpriteRenderer).")]
    public Sprite shockSprite;

    [Header("Damage Settings")]
    [Tooltip("Bật để vùng trung tâm 3x3 gây sát thương.")]
    public bool useDamage = true;

    [Tooltip("Lượng damage gây ra khi chạm.")]
    public int damageAmount = 1;

    [Tooltip("Tag mà shockwave sẽ gây sát thương (vd: Player, Enemy...).")]
    public string[] targetTags = new string[] { "Player" };

    [Header("Visual Settings")]
    [Tooltip("Độ cao sorting order (hiển thị trên nền).")]
    public int sortingOrder = 100;

    /// <summary>
    /// Sinh lưới ô trong phạm vi [-radiusTiles..radiusTiles] theo trục X/Y,
    /// mỗi ô cách nhau đúng bằng tileSize => tăng tileSize sẽ làm hiển thị to hơn
    /// và collider vùng trung tâm (3x3 ô) cũng rộng theo đơn vị thế giới.
    /// </summary>
    public void SpawnRing(Vector3 center, int radiusTiles, Vector2 tileSize)
    {
        if (radiusTiles < 1) radiusTiles = 1;
        if (tileSize.x <= 0f) tileSize.x = 0.1f;
        if (tileSize.y <= 0f) tileSize.y = 0.1f;

        var spawned = new List<GameObject>();

        // tạo lưới vuông [-r..r]x[-r..r]
        for (int gx = -radiusTiles; gx <= radiusTiles; gx++)
        {
            for (int gy = -radiusTiles; gy <= radiusTiles; gy++)
            {
                Vector3 pos = new Vector3(
                    center.x + gx * tileSize.x,
                    center.y + gy * tileSize.y,
                    0f
                );

                GameObject tile = shockTilePrefab
                    ? Instantiate(shockTilePrefab, pos, Quaternion.identity)
                    : new GameObject("ShockTile");

                tile.transform.position = pos;
                tile.transform.SetParent(transform, true);

                // SpriteRenderer (hiển thị)
                var sr = tile.GetComponent<SpriteRenderer>();
                if (!sr) sr = tile.AddComponent<SpriteRenderer>();
                if (shockSprite) sr.sprite = shockSprite;
                sr.sortingOrder = sortingOrder;

#if UNITY_2017_1_OR_NEWER
                // Dùng drawMode=Tiled để giữ đúng kích thước mà không scale collider
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = tileSize;
#endif
                tile.transform.localScale = Vector3.one;

                // hitbox cho vùng trung tâm 3x3: |dx|<=1, |dy|<=1
                bool central3x3 = Mathf.Abs(gx) <= 1 && Mathf.Abs(gy) <= 1;

                if (useDamage && central3x3)
                {
                    // Collider trigger vừa khít "một ô" theo world units
                    var col = tile.GetComponent<BoxCollider2D>();
                    if (!col) col = tile.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = tileSize;

                    // DamageOnTouch (sửa theo bản mới)
                    var dmg = tile.GetComponent<DamageOnTouch>();
                    if (!dmg) dmg = tile.AddComponent<DamageOnTouch>();
                    dmg.damage = damageAmount;
                    dmg.targetTags = targetTags; // <-- thay cho targetTag cũ
                    dmg.destroySelfOnHit = false; // shock không tự hủy
                    dmg.destroyEvenIfNoHealth = false;
                    dmg.ignoreSameRoot = true;
                }

                // hiệu ứng fade & tự hủy
                if (!tile.TryGetComponent<ShockTileLifetime>(out _))
                    tile.AddComponent<ShockTileLifetime>();

                spawned.Add(tile);
            }
        }
    }
}

/// Tự fade & phá hủy "ô" rung chấn sau thời gian life.
public class ShockTileLifetime : MonoBehaviour
{
    public float life = 0.6f;

    private void Start()
    {
        StartCoroutine(Co());
    }

    private System.Collections.IEnumerator Co()
    {
        var sr = GetComponent<SpriteRenderer>();
        float t = 0f;
        Color c0 = sr ? sr.color : Color.white;

        while (t < life)
        {
            t += Time.deltaTime;
            if (sr)
            {
                float k = 1f - Mathf.Clamp01(t / life);
                sr.color = new Color(c0.r, c0.g, c0.b, k);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
