using UnityEngine;

public class TriggerZone_SwitchShieldAndMusic : MonoBehaviour
{
    public string playerTag = "Player";
    public ShieldBarrier shieldToDisable; // Kéo ShieldBarrier vào
    public bool disableTriggerAfterHit = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 1) Tắt khiên
        if (shieldToDisable != null) shieldToDisable.DeactivateShield();

        // 2) Đổi nhạc sau Trigger
        if (MusicManager.Instance != null) MusicManager.Instance.PlayAfterTrigger();

        if (disableTriggerAfterHit) gameObject.SetActive(false);
    }
}
