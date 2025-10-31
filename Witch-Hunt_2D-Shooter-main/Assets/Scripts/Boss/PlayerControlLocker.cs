using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Locker “ăn chắc” cho Player:
/// - Aggressive Lock: tự động disable toàn bộ MonoBehaviour (trừ whitelist)
/// - Vô hiệu PlayerInput (Input System) nếu có
/// - Dọn vận tốc Rigidbody
/// - Tuỳ chọn Freeze Rigidbody
/// - Tuỳ chọn ghim transform mỗi frame (pin) để chặn mọi set trực tiếp
/// </summary>
public class PlayerControlLocker : MonoBehaviour
{
    [Header("Aggressive Lock")]
    [Tooltip("Nếu bật: tự động disable tất cả MonoBehaviour trên Player (và con), trừ whitelist.")]
    public bool aggressiveLock = true;

    [Tooltip("Danh sách script cho phép chạy khi bị khoá (giữ nguyên enabled).")]
    public List<MonoBehaviour> whitelist = new List<MonoBehaviour>();

    [Header("Specific scripts to disable (optional)")]
    [Tooltip("Nếu bạn muốn kiểm soát thủ công, liệt kê các script điều khiển tại đây. (Không bắt buộc nếu bật Aggressive Lock)")]
    public MonoBehaviour[] componentsToDisable;

    [Header("Rigidbody control")]
    public Rigidbody2D rb;
    [Tooltip("Dọn vận tốc ngay khi khoá.")]
    public bool zeroVelocityOnLock = true;
    [Tooltip("Mỗi FixedUpdate khi đang khoá sẽ set velocity/angVel về 0.")]
    public bool continuouslyZeroVelocityWhileLocked = true;
    [Tooltip("Đóng băng Rigidbody khi khoá (Freeze Position X/Y + Rotation). TẮT mặc định để còn auto-step trong intro.")]
    public bool freezeRigidbodyWhenLocked = false;

    [Header("Transform Pin (đề phòng script đặt transform trực tiếp)")]
    [Tooltip("Nếu bật: mỗi frame sẽ ghim Player về vị trí/rotation khoá lại. KHÔNG nên bật khi đang auto-move bằng script.")]
    public bool pinTransformWhileLocked = false;

    [Header("Input System")]
    [Tooltip("Nếu dùng Input System (PlayerInput), sẽ bị disable khi khoá.")]
    public bool disableUnityPlayerInput = true;

    // ---- internal state
    bool isLocked;
    Vector3 pinnedPosition;
    float pinnedRotationZ;
    RigidbodyConstraints2D originalConstraints;
    float originalGravityScale;

#if ENABLE_INPUT_SYSTEM
    PlayerInput cachedPlayerInput;
#endif

    // Lưu lại những component bị disable tạm thời để bật lại khi unlock
    readonly List<MonoBehaviour> disabledByAggressive = new List<MonoBehaviour>();
    readonly List<MonoBehaviour> disabledByManual = new List<MonoBehaviour>();

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            originalConstraints = rb.constraints;
            originalGravityScale = rb.gravityScale;
        }
#if ENABLE_INPUT_SYSTEM
        if (disableUnityPlayerInput)
            cachedPlayerInput = GetComponentInChildren<PlayerInput>();
#endif
    }

    void OnEnable()
    {
        if (isLocked) ApplyLockState(); // đề phòng reload
    }

    void Update()
    {
        if (!isLocked || !pinTransformWhileLocked) return;

        // Ghim transform mỗi frame (update/latedUpdate tránh bị script khác set)
        transform.position = new Vector3(pinnedPosition.x, pinnedPosition.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, pinnedRotationZ);
    }

    void LateUpdate()
    {
        if (!isLocked || !pinTransformWhileLocked) return;

        // Lặp lại để chắc ăn trước khi render
        transform.position = new Vector3(pinnedPosition.x, pinnedPosition.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, pinnedRotationZ);
    }

    void FixedUpdate()
    {
        if (!isLocked || !rb) return;

        if (continuouslyZeroVelocityWhileLocked)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // ===== PUBLIC API =====
    public void LockControls()
    {
        if (isLocked) return;
        isLocked = true;

        // Chụp pose để pin (nếu bật)
        pinnedPosition = transform.position;
        pinnedRotationZ = transform.eulerAngles.z;

        ApplyLockState();
    }

    public void UnlockControls()
    {
        if (!isLocked) return;
        isLocked = false;

        RestoreLockState();
    }

    public bool IsLocked() => isLocked;

    // ===== CORE =====
    void ApplyLockState()
    {
        // 0) Manual list (nếu có)
        if (componentsToDisable != null && componentsToDisable.Length > 0)
        {
            foreach (var c in componentsToDisable)
            {
                if (!c) continue;
                if (c.enabled)
                {
                    c.enabled = false;
                    disabledByManual.Add(c);
                }
            }
        }

        // 1) Aggressive: disable TẤT CẢ MonoBehaviour (trừ whitelist & bản thân)
        if (aggressiveLock)
        {
            var all = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in all)
            {
                if (mb == null) continue;
                if (mb == (MonoBehaviour)this) continue;                // đừng tắt chính mình
                if (whitelist != null && whitelist.Contains(mb)) continue;

                // Một số component nên giữ nguyên:
                if (mb is Animator) continue;                           // thường muốn giữ animation còn chạy
                if (mb is SpriteRenderer) continue;
                // Nếu bạn có script UI gắn trên player, add vào whitelist để giữ

                if (mb.enabled)
                {
                    mb.enabled = false;
                    disabledByAggressive.Add(mb);
                }
            }
        }

        // 2) Rigidbody
        if (rb)
        {
            if (zeroVelocityOnLock)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (freezeRigidbodyWhenLocked)
            {
                originalConstraints = rb.constraints;
                originalGravityScale = rb.gravityScale;
                rb.constraints = RigidbodyConstraints2D.FreezePositionX
                               | RigidbodyConstraints2D.FreezePositionY
                               | RigidbodyConstraints2D.FreezeRotation;
            }
        }

        // 3) Input System
#if ENABLE_INPUT_SYSTEM
        if (disableUnityPlayerInput)
        {
            if (!cachedPlayerInput) cachedPlayerInput = GetComponentInChildren<PlayerInput>();
            if (cachedPlayerInput) cachedPlayerInput.enabled = false;
        }
#endif
    }

    void RestoreLockState()
    {
        // 0) Manual list
        for (int i = 0; i < disabledByManual.Count; i++)
        {
            if (disabledByManual[i]) disabledByManual[i].enabled = true;
        }
        disabledByManual.Clear();

        // 1) Aggressive
        for (int i = 0; i < disabledByAggressive.Count; i++)
        {
            if (disabledByAggressive[i]) disabledByAggressive[i].enabled = true;
        }
        disabledByAggressive.Clear();

        // 2) Rigidbody
        if (rb && freezeRigidbodyWhenLocked)
        {
            rb.constraints = originalConstraints;
            rb.gravityScale = originalGravityScale;
        }

        // 3) Input System
#if ENABLE_INPUT_SYSTEM
        if (disableUnityPlayerInput && cachedPlayerInput)
            cachedPlayerInput.enabled = true;
#endif
    }
}
