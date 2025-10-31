using UnityEngine;

[DisallowMultipleComponent]
public class KeepUpright2D : MonoBehaviour
{
    [Tooltip("Giữ Z = 0 mỗi frame (chặn animator/logic vô tình xoay)")]
    public bool forceIdentityRotation = true;

    [Tooltip("Đặt vận tốc góc = 0 để tránh quay do vật lý")]
    public bool zeroAngularVelocity = true;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Khóa xoay do vật lý
        if (rb)
        {
            // Dành cho Unity các phiên bản khác nhau – dùng cả hai cho chắc
            rb.freezeRotation = true; // property trên Rigidbody2D
            rb.constraints |= RigidbodyConstraints2D.FreezeRotation; // flags
        }

        // Đảm bảo bắt đầu game là đứng thẳng
        if (forceIdentityRotation)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        // Mỗi frame ép về thẳng đứng, tránh mọi nguồn xoay khác
        if (forceIdentityRotation)
        {
            transform.rotation = Quaternion.identity;
        }

        // Chặn quay do va chạm vật lý còn đọng lại
        if (rb && zeroAngularVelocity)
        {
            rb.angularVelocity = 0f;
        }
    }
}
