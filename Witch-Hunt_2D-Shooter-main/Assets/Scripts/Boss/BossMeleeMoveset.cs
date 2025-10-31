using System.Collections;
using UnityEngine;

/// <summary>
/// BossMeleeMoveset
/// - Trong quá trình áp sát player: chỉ phát Prepare_*
/// - Khi đã vào phạm vi stopDistance: chờ punchDelaySeconds rồi phát Punch_*
/// - Hướng (Left/Right/Up/Down) dựa vào vector tới player tại thời điểm áp sát
/// - Gọi StartMeleeAttack() để bắt đầu; Cancel() để huỷ.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossMeleeMoveset : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;            // Kéo Player vào
    public Animator animator;           // Animator của Boss
    private Rigidbody2D rb;

    [Header("Approach Settings")]
    [Tooltip("Tốc độ áp sát về phía Player trong giai đoạn Prepare.")]
    public float approachSpeed = 6f;

    [Tooltip("Khoảng cách coi như đã tiếp cận được.")]
    public float stopDistance = 2.4f;

    [Tooltip("Giới hạn thời gian áp sát (an toàn). Hết thời gian mà chưa đủ gần sẽ dừng lại và vẫn Punch.")]
    public float maxApproachTime = 2.5f;

    [Header("Punch Settings")]
    [Tooltip("Thời gian chờ (trễ) trước khi ra đòn sau khi tiếp cận thành công.")]
    public float punchDelaySeconds = 0.25f;

    [Tooltip("Thời gian chờ sau khi Punch để kết thúc đòn (tuỳ Animator).")]
    public float postPunchHold = 0.25f;

    [Header("Animator Parameters (Triggers)")]
    public string prepareLeftTrigger = "Prepare_Left";
    public string prepareRightTrigger = "Prepare_Right";
    public string prepareUpTrigger = "Prepare_Up";
    public string prepareDownTrigger = "Prepare_Down";

    public string punchLeftTrigger = "Punch_Left";
    public string punchRightTrigger = "Punch_Right";
    public string punchUpTrigger = "Punch_Up";
    public string punchDownTrigger = "Punch_Down";

    [Header("Flags/Debug")]
    public bool faceWhileApproach = true; // Đổi hướng Prepare trong lúc áp sát
    public bool inAttackRoutine;          // đang chạy coroutine
    public bool reachedPlayer;            // đã tiếp cận đủ gần

    Coroutine attackCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Bắt đầu một combo áp sát rồi Punch.
    /// Có thể gọi từ BossController hoặc AnimationHook.
    /// </summary>
    public void StartMeleeAttack()
    {
        if (inAttackRoutine) return;
        attackCo = StartCoroutine(ApproachThenPunch());
    }

    /// <summary>
    /// Hủy đòn (nếu đang áp sát). Dừng chuyển động và kết thúc state.
    /// </summary>
    public void Cancel()
    {
        if (attackCo != null) StopCoroutine(attackCo);
        attackCo = null;
        inAttackRoutine = false;
        reachedPlayer = false;
        rb.velocity = Vector2.zero;
    }

    IEnumerator ApproachThenPunch()
    {
        inAttackRoutine = true;
        reachedPlayer = false;

        // 1) Giai đoạn áp sát (chỉ Prepare_*)
        float t = 0f;
        Vector2 lastToPlayer = Vector2.right;

        while (t < maxApproachTime)
        {
            t += Time.deltaTime;

            if (player != null)
            {
                Vector2 toPlayer = (player.position - transform.position);
                lastToPlayer = toPlayer;

                // Nếu đã đủ gần → thoát vòng áp sát
                if (toPlayer.magnitude <= stopDistance)
                {
                    reachedPlayer = true;
                    rb.velocity = Vector2.zero;
                    break;
                }

                // Điều hướng và phát Prepare theo hướng
                if (faceWhileApproach)
                {
                    FirePrepareTriggerByDir(toPlayer);
                }

                // Tiến tới người chơi
                Vector2 dir = toPlayer.normalized;
                rb.velocity = dir * approachSpeed;
            }
            else
            {
                // Không có player → dừng
                rb.velocity = Vector2.zero;
                break;
            }

            yield return null;
        }

        // Dừng di chuyển khi kết thúc áp sát
        rb.velocity = Vector2.zero;

        // 2) Chờ trễ trước khi Punch (kể cả chưa đạt được khoảng cách vì hết thời gian)
        if (punchDelaySeconds > 0f)
        {
            float waited = 0f;
            while (waited < punchDelaySeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        // 3) Chọn hướng Punch theo vector tới player (tại thời điểm hiện tại để chính xác)
        Vector2 punchDir = (player != null)
            ? (Vector2)(player.position - transform.position)
            : lastToPlayer;

        FirePunchTriggerByDir(punchDir);

        // 4) Giữ một nhịp nhỏ để cho animation phát đòn
        if (postPunchHold > 0f)
            yield return new WaitForSeconds(postPunchHold);

        // Kết thúc
        inAttackRoutine = false;
        attackCo = null;
    }

    // ---------------- Animator helpers ----------------

    void FirePrepareTriggerByDir(Vector2 dir)
    {
        string trig = DirToPrepareTrigger(dir);
        if (!string.IsNullOrEmpty(trig))
            animator.SetTrigger(trig);
    }

    void FirePunchTriggerByDir(Vector2 dir)
    {
        string trig = DirToPunchTrigger(dir);
        if (!string.IsNullOrEmpty(trig))
            animator.SetTrigger(trig);
    }

    string DirToPrepareTrigger(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x >= 0f ? prepareRightTrigger : prepareLeftTrigger;
        }
        else
        {
            return dir.y >= 0f ? prepareUpTrigger : prepareDownTrigger;
        }
    }

    string DirToPunchTrigger(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x >= 0f ? punchRightTrigger : punchLeftTrigger;
        }
        else
        {
            return dir.y >= 0f ? punchUpTrigger : punchDownTrigger;
        }
    }

    // ---------------- Public setters (tuỳ chọn dùng từ Inspector/Controller) ----------------
    public void SetApproachSpeed(float v) => approachSpeed = Mathf.Max(0f, v);
    public void SetStopDistance(float v) => stopDistance = Mathf.Max(0.01f, v);
    public void SetPunchDelay(float v) => punchDelaySeconds = Mathf.Max(0f, v);
    public void SetPostPunchHold(float v) => postPunchHold = Mathf.Max(0f, v);
}
