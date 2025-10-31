using UnityEngine;

public class BossDeathRelay : MonoBehaviour
{
    [Header("Refs")]
    public BossDeathSequence director; // trỏ tới object “director”
    public Health health;                       // Health của Boss

    [Header("Options")]
    public bool logDebug = true;

    bool fired;

    void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (director == null) director = FindObjectOfType<BossDeathSequence>();
    }

    void Update()
    {
        if (!fired && health != null && health.currentHealth <= 0)
        {
            fired = true;
            if (logDebug) Debug.Log("[BossDeathRelay] Health <= 0 -> Trigger director.");
            if (director != null) director.StartSequence(); // gọi hàm công khai trong Cutscene
        }
    }

    void OnDestroy()
    {
        // Phòng khi Boss bị Destroy trước khi Update kịp bắt
        if (!fired && director != null)
        {
            fired = true;
            if (logDebug) Debug.Log("[BossDeathRelay] OnDestroy -> Trigger director as fallback.");
            director.StartSequence();
        }
    }
}
