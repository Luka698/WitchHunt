using UnityEngine;

/// <summary>
/// Gắn vào một GameObject trong scene Level4_Boss.
/// Nhiệm vụ: Khi panel thoại tắt (kết thúc Intro), lập tức chuyển sang nhạc Boss.
/// Không cần sửa DialogueUI/BossIntroTrigger.
/// </summary>
public class BossBGMSwitcher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel (GameObject) của UI thoại Intro. Khi nó ẩn (inactive) => đã kết thúc.")]
    public GameObject dialoguePanel; // Kéo đúng panel mà bạn đang dùng để hiển thị đoạn thoại mở đầu

    [Tooltip("AudioSource dùng làm BGM (nhạc nền).")]
    public AudioSource bgmSource;    // Kéo AudioSource phát nhạc nền trong scene

    [Tooltip("Clip nhạc Boss muốn bật ngay sau khi thoại kết thúc.")]
    public AudioClip bossMusic;      // Kéo file nhạc boss vào đây

    [Header("Options")]
    [Tooltip("Crossfade (giây). 0 = cắt ngay, >0 = mượt hơn.")]
    public float crossfadeSeconds = 0f;

    bool switched = false;
    bool wasOpen = false;

    void Start()
    {
        if (dialoguePanel)
            wasOpen = dialoguePanel.activeInHierarchy; // đang mở thoại khi bắt đầu
    }

    void Update()
    {
        if (switched || dialoguePanel == null || bgmSource == null || bossMusic == null)
            return;

        bool isOpen = dialoguePanel.activeInHierarchy;

        // Khi vừa chuyển từ mở -> tắt (đối thoại kết thúc)
        if (wasOpen && !isOpen)
        {
            SwitchToBossMusic();
            switched = true;
        }

        wasOpen = isOpen;
    }

    void SwitchToBossMusic()
    {
        if (crossfadeSeconds <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = bossMusic;
            bgmSource.loop = true;
            bgmSource.Play();
            return;
        }

        // Crossfade đơn giản
        StartCoroutine(CrossfadeTo(bossMusic, crossfadeSeconds));
    }

    System.Collections.IEnumerator CrossfadeTo(AudioClip target, float time)
    {
        float t = 0f;
        float startVol = bgmSource.volume;
        // Fade-out
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / time);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.clip = target;
        bgmSource.loop = true;
        bgmSource.Play();

        // Fade-in
        t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVol, t / time);
            yield return null;
        }
        bgmSource.volume = startVol;
    }
}
