using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BossDeathMusicRelay : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip preTriggerClip;      // Nhạc nền trước khi Boss Trigger
    public AudioClip afterTriggerClip;    // Nhạc sau khi Trigger
    public AudioClip bossDeadClip;        // Nhạc sau khi Boss chết

    [Header("Refs")]
    public Health bossHealth;             // Kéo Health của Boss vào (tự động nếu để trống)
    public Collider2D triggerZone;        // Vùng Trigger (khi Player chạm vào, đổi nhạc)
    public string playerTag = "Player";   // Tag của Player

    [Header("Debug")]
    public bool logDebug = true;

    private AudioSource audioSource;
    private bool triggered = false;
    private bool bossDead = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; // Cho phép lặp lại nhạc
        audioSource.playOnAwake = false;

        if (bossHealth == null)
        {
            var boss = GameObject.FindWithTag("Boss");
            if (boss != null)
                bossHealth = boss.GetComponent<Health>();
        }

        if (preTriggerClip != null)
        {
            audioSource.clip = preTriggerClip;
            audioSource.Play();
            if (logDebug) Debug.Log("[MusicRelay] Playing preTriggerClip (loop).");
        }
    }

    void Update()
    {
        if (bossDead || bossHealth == null) return;

        // Khi Boss chết → đổi nhạc
        if (bossHealth.currentHealth <= 0)
        {
            bossDead = true;
            PlayMusic(bossDeadClip);
            if (logDebug) Debug.Log("[MusicRelay] Boss dead → switched to bossDeadClip.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Khi Player vào vùng trigger → đổi nhạc afterTrigger
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;
            PlayMusic(afterTriggerClip);
            if (logDebug) Debug.Log("[MusicRelay] Player entered trigger → switched to afterTriggerClip.");
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
