using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShieldBarrier : MonoBehaviour
{
    [Header("Projectile Settings")]
    public string projectileTag = "Projectile"; // Tag của đạn

    [Header("State")]
    public bool activeAtStart = true;          // Bật lúc đầu

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;                  // Dùng trigger để bắt đạn
        gameObject.SetActive(activeAtStart);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;

        if (other.CompareTag(projectileTag))
        {
            // Phá hủy đạn khi đụng khiên
            Destroy(other.gameObject);
        }
    }

    // Gọi từ Trigger để tắt khiên
    public void DeactivateShield()
    {
        gameObject.SetActive(false);
    }
}
