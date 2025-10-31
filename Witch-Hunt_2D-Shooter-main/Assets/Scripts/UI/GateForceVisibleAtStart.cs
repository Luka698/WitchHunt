using UnityEngine;

/// Đặt trên object Cổng đặt sẵn trong scene để chắc chắn:
/// - Active = true ngay từ Awake (không "mất cổng" khi vào game)
/// - (tuỳ chọn) ép Animator về state đóng ở frame đầu
public class GateForceVisibleAtStart : MonoBehaviour
{
    [Tooltip("Nếu có Animator, ép về state đóng khi bắt đầu.")]
    public bool resetAnimatorToClosed = true;

    [Tooltip("Tên state đóng trong Animator.")]
    public string closedStateName = "Closed";

    Animator anim;
    bool resetDone;

    void Awake()
    {
        // Ép Active ON ngay từ rất sớm
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (resetAnimatorToClosed && anim && !string.IsNullOrEmpty(closedStateName))
        {
            // Play về state Closed ở layer 0, normalizedTime=0
            anim.Play(closedStateName, 0, 0f);
            anim.Update(0f); // force evaluate để pose đúng ngay
            resetDone = true;
        }
    }

    // Optional: nếu cổng bị script khác "cãi lệnh" trong LateUpdate frame đầu,
    // bạn có thể bật đoạn này để ép lại 1 lần nữa rồi tự tắt:
    void LateUpdate()
    {
        if (resetAnimatorToClosed && anim && !resetDone)
        {
            anim.Play(closedStateName, 0, 0f);
            anim.Update(0f);
            resetDone = true;
        }
        if (resetDone) enabled = false; // xong việc → tự tắt script
    }
}
