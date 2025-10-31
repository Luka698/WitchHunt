using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource musicSource;          // 2D AudioSource (loop ON)

    [Header("Clips")]
    public AudioClip preTriggerClip;         // Nhạc ban đầu
    public AudioClip afterTriggerClip;       // Nhạc sau khi Trigger
    public AudioClip bossDeadClip;           // Nhạc sau khi Boss chết
    public AudioClip exitClip;               // Nhạc sau khi chạm Exit

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        if (preTriggerClip != null)
        {
            musicSource.clip = preTriggerClip;
            musicSource.Play();
        }
    }

    public void PlayClip(AudioClip clip, bool loop = true, float volume = 1f)
    {
        if (clip == null) return;
        musicSource.loop = loop;
        musicSource.volume = volume;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayPreTrigger() => PlayClip(preTriggerClip, true);
    public void PlayAfterTrigger() => PlayClip(afterTriggerClip, true);
    public void PlayBossDead() => PlayClip(bossDeadClip, true);
    public void PlayExit() => PlayClip(exitClip, true);
}
