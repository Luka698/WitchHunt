using UnityEngine;

[DisallowMultipleComponent]
public class SFXLibrary : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Âm lượng mặc định cho tất cả one-shot.")]
    [Range(0f, 1f)] public float masterVolume = 0.9f;

    [Tooltip("Dao động pitch ± (random) cho one-shot, 0 = tắt.")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.05f;

    [Header("Rocket")]
    public AudioClip rocketEnter;   // lúc bắt đầu state Rocket
    public AudioClip rocketFire;    // mỗi quả bắn ra

    [Header("Cannon")]
    public AudioClip cannonEnter;   // lúc bắt đầu state Cannon
    public AudioClip cannonFire;    // mỗi viên đạn

    [Header("Explosion / Melee / Others")]
    public AudioClip explosion;     // nổ (rocket/cannon)
    public AudioClip punchHit;      // Punch_* trúng Player
    public AudioClip laugh;         // tiếng cười
    public AudioClip shockwave;     // sau JumpLand

    // ======= One-shot player (pool đơn giản) =======
    const int POOL = 10;
    AudioSource[] pool;
    int next;

    void Awake()
    {
        pool = new AudioSource[POOL];
        for (int i = 0; i < POOL; i++)
        {
            var go = new GameObject($"SFXOneShot_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;     // 2D
            src.loop = false;
            pool[i] = src;
        }
    }

    public void Play2D(AudioClip clip, float vol = 1f)
    {
        if (!clip) return;
        var src = pool[next];
        next = (next + 1) % POOL;

        src.clip = clip;
        src.volume = masterVolume * vol;
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.transform.position = Vector3.zero;
        src.spatialBlend = 0f;
        src.Play();
    }

    public void Play3D(AudioClip clip, Vector3 pos, float vol = 1f)
    {
        if (!clip) return;
        var src = pool[next];
        next = (next + 1) % POOL;

        src.clip = clip;
        src.volume = masterVolume * vol;
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.transform.position = pos;
        src.spatialBlend = 1f;     // 3D nhẹ
        src.minDistance = 5f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.Play();
    }
}
