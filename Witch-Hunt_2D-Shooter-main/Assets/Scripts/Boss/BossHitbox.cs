using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    public Health bossHealth; // tham chiếu tới Health của boss

    private void Start()
    {
        // tự động tìm Health trong cha
        if (bossHealth == null)
            bossHealth = GetComponentInParent<Health>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // nếu đạn chạm vào
        if (collision.CompareTag("Projectile"))
        {
            // trừ máu boss
            if (bossHealth != null)
                bossHealth.TakeDamage(1); // số 1 = sát thương, bạn có thể đổi tuỳ ý

            // phá huỷ viên đạn
            Destroy(collision.gameObject);
        }
    }
}
