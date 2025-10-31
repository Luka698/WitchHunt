using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// DamageOnTouch (Pro)
/// - Hỗ trợ nhiều tag hoặc LayerMask để chọn mục tiêu (Player, Boss, Enemy, v.v.)
/// - Tự tìm Health ở self/parent/children, ưu tiên gọi TakeDamage(int/float)
/// - Tùy chọn: tự hủy "chính mình" sau khi gây damage (cho projectile/đạn)
/// - Tránh tự bắn vào bản thân (bỏ qua cùng root)
/// - Hỗ trợ cả OnTriggerEnter2D và OnCollisionEnter2D
[RequireComponent(typeof(Collider2D))]
public class DamageOnTouch : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Lượng sát thương gây ra")]
    public int damage = 1;

    [Header("Legacy Compatibility (do not use new projects)")]
    [Tooltip("Giữ để tương thích với các script cũ như ShockwaveSpawner.cs")]
    public string targetTag = "Player";


    [Header("Target Filtering")]
    [Tooltip("Nếu có ít nhất 1 phần tử, sẽ lọc theo Tag trong danh sách này.")]
    public string[] targetTags = new string[] { "Player", "Boss" };

    [Tooltip("Nếu bật, sẽ lọc thêm theo LayerMask (song song với tag). Để trống = không lọc layer.")]
    public bool useLayerMask = false;
    public LayerMask targetLayers;

    [Header("Projectile Options")]
    [Tooltip("Tự hủy chính mình sau khi gây damage 1 lần (đạn).")]
    public bool destroySelfOnHit = true;

    [Tooltip("Nếu không tìm thấy Health để trừ máu, vẫn hủy đạn (đỡ xuyên người).")]
    public bool destroyEvenIfNoHealth = true;

    [Tooltip("Delay hủy (giúp chơi VFX/âm thanh trước khi biến mất).")]
    public float destroyDelay = 0f;

    [Header("Misc")]
    [Tooltip("Bỏ qua va chạm với các collider thuộc cùng root (tránh tự bắn trúng chính mình).")]
    public bool ignoreSameRoot = true;

    [Tooltip("Chặn spam nhiều hit trong cùng 1 frame với cùng 1 mục tiêu.")]
    public bool preventMultiHitPerTarget = true;

    private Transform _myRoot;
    private HashSet<Collider2D> _hitThisFrame = new HashSet<Collider2D>();

    void Awake()
    {
        _myRoot = transform.root;
        var col = GetComponent<Collider2D>();
        // Không ép IsTrigger ở đây vì script có thể gắn cho cả trigger VÀ collision thường.
        // Bạn chỉ cần đảm bảo 1 trong 2 path (trigger hoặc collision) được Unity gọi.
        if (!col.enabled)
            Debug.LogWarning($"[DamageOnTouch] Collider on {name} is disabled.");
    }

    void LateUpdate()
    {
        // reset danh sách chống spam theo frame
        if (preventMultiHitPerTarget)
            _hitThisFrame.Clear();
    }

    // Trigger path
    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    // Collision path (nếu bạn dùng collider non-trigger)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider) TryDamage(collision.collider);
    }

    void TryDamage(Collider2D other)
    {
        if (!other) return;

        if (preventMultiHitPerTarget && _hitThisFrame.Contains(other))
            return;

        // 1) Bỏ qua cùng root (tránh tự gây damage)
        if (ignoreSameRoot && other.transform.root == _myRoot)
            return;

        // 2) Lọc theo LayerMask (nếu bật)
        if (useLayerMask)
        {
            if (((1 << other.gameObject.layer) & targetLayers.value) == 0)
                return;
        }

        // 3) Lọc theo Tags (nếu có khai báo)
        if (targetTags != null && targetTags.Length > 0)
        {
            bool tagOk = false;
            for (int i = 0; i < targetTags.Length; i++)
            {
                if (!string.IsNullOrEmpty(targetTags[i]) && (other.CompareTag(targetTags[i]) || other.transform.root.CompareTag(targetTags[i])))
                {
                    tagOk = true; break;
                }
            }
            if (!tagOk) return;
        }

        // 4) Tìm Health ở self/parent/children
        var health = other.GetComponent<Health>()
                 ?? other.GetComponentInParent<Health>()
                 ?? other.GetComponentInChildren<Health>();

        bool damaged = false;

        if (health != null)
        {
            // Ưu tiên phương thức TakeDamage(int/float)
            var t = health.GetType();
            MethodInfo mInt = t.GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
            MethodInfo mFloat = t.GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);

            if (mInt != null)
            {
                mInt.Invoke(health, new object[] { damage });
                damaged = true;
            }
            else if (mFloat != null)
            {
                mFloat.Invoke(health, new object[] { (float)damage });
                damaged = true;
            }
            else
            {
                // fallback: tìm field phổ biến
                FieldInfo fCur = t.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? t.GetField("hp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? t.GetField("HealthPoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? t.GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? t.GetField("current", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (fCur != null)
                {
                    object v = fCur.GetValue(health);
                    if (v is int vi)
                    {
                        fCur.SetValue(health, Mathf.Max(0, vi - damage));
                        damaged = true;
                        // Nếu có method Die() sẽ do Health tự xử
                    }
                    else if (v is float vf)
                    {
                        fCur.SetValue(health, Mathf.Max(0f, vf - damage));
                        damaged = true;
                    }
                }
            }
        }

        if (preventMultiHitPerTarget) _hitThisFrame.Add(other);

        // 5) Tự hủy đạn sau khi gây damage hoặc khi chạm mục tiêu hợp lệ
        if (destroySelfOnHit && (damaged || destroyEvenIfNoHealth))
        {
            if (destroyDelay <= 0f) Destroy(gameObject);
            else Destroy(gameObject, destroyDelay);
        }
    }
}
