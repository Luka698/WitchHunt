using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossIntroTrigger : MonoBehaviour
{
    [Header("Refs")]
    public CameraController cameraController;
    public Transform player;
    public Rigidbody2D playerRb;
    public PlayerControlLocker playerLocker;
    public Transform boss;
    public Animator bossAnimator;

    // ⭐ Các Object sẽ biến mất sau khi Dialogue kết thúc
    [Header("Objects to Hide After Dialogue")]
    public List<GameObject> objectsToHideAfterDialogue = new List<GameObject>();

    [Header("Cinematic")]
    [Tooltip("Kéo GameObject có BossCinematic (ví dụ BossDirector) vào đây.")]
    public BossCinematic cinematic;

    [Header("Camera move")]
    public float cameraMoveDuration = 1.2f;

    [Header("Player auto step")]
    [Tooltip("Khoảng cách mong muốn giữa Player và Boss sau khi auto-step.")]
    public float desiredDistanceFromBoss = 6f;
    [Tooltip("Thời gian tween bước ngắn của Player.")]
    public float playerMoveDuration = 0.8f;
    [Tooltip("Giới hạn quãng đường Player tự bước (để không kéo quá xa).")]
    public float playerMaxStep = 4f;

    [Header("Lock controls")]
    [Tooltip("Khóa điều khiển ngay khi chạm trigger và chỉ mở sau khi thoại kết thúc.")]
    public bool lockControls = true;
    [Tooltip("Giữ thêm 1 khoảng ngắn sau khi camera/step xong trước khi mở thoại.")]
    public float extraLockDuration = 0.4f;

    [Header("Dialogue")]
    public Sprite portrait;
    [TextArea(2, 4)] public string line1 = "Ngươi đã tới…";
    [TextArea(2, 4)] public string line2 = "Đây sẽ là nơi ngươi bỏ mạng!";
    [TextArea(2, 4)] public string line3 = "Chuẩn bị đi!";
    [Tooltip("Sau intro, trả camera về chế độ Overhead.")]
    public bool returnCameraOverhead = true;

    // ⭐ NEW: BGM switch ngay khi thoại kết thúc
    [Header("BGM Switch (ngay khi thoại kết thúc)")]
    [Tooltip("AudioSource đang phát nhạc nền trong scene.")]
    public AudioSource bgmSource;
    [Tooltip("Clip nhạc Boss sẽ phát ngay sau khi thoại đóng.")]
    public AudioClip bossMusic;
    [Tooltip("0 = đổi ngay. >0 = crossfade mượt (giây).")]
    public float bgmCrossfadeSeconds = 0f;

    bool hasTriggered;

    void Reset()
    {
        // Bảo đảm collider là trigger
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        // Thử auto-find các tham chiếu phổ biến
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody2D>();
        if (!playerLocker && player) playerLocker = player.GetComponent<PlayerControlLocker>();
    }

    void OnValidate()
    {
        // Bảo đảm collider là trigger (khi chỉnh Inspector)
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        hasTriggered = true;

        // KHÓA CONTROL NGAY LẬP TỨC
        if (lockControls)
        {
            if (!playerLocker) playerLocker = other.GetComponent<PlayerControlLocker>() ?? other.GetComponentInParent<PlayerControlLocker>();
            if (playerLocker != null)
            {
                playerLocker.LockControls();
                if (playerRb != null) playerRb.velocity = Vector2.zero; // tránh trôi
            }
            else
            {
                Debug.LogWarning("[BossIntroTrigger] playerLocker chưa được gán → không thể LockControls.");
            }
        }

        // Dừng animator boss để chờ thoại
        if (bossAnimator != null) bossAnimator.speed = 0f;

        Debug.Log("[IntroTrigger] Player vào trigger → LockControls & bắt đầu intro");
        StartCoroutine(DoIntro());
    }

    IEnumerator DoIntro()
    {
        // Player bước ngắn tới vị trí mong muốn
        if (player != null && boss != null)
        {
            Vector3 dir = (player.position - boss.position);
            float d = dir.magnitude;
            dir = d > 0.0001f ? dir / d : Vector3.right;
            Vector3 desiredPos = boss.position + dir * desiredDistanceFromBoss;

            Vector3 delta = desiredPos - player.position;
            if (delta.magnitude > playerMaxStep) delta = delta.normalized * playerMaxStep;

            Vector3 target = player.position + delta;
            float t = 0f;
            Vector3 start = player.position;

            if (playerRb != null) playerRb.velocity = Vector2.zero;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, playerMoveDuration);
                Vector3 p = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
                if (playerRb != null) playerRb.MovePosition(p);
                else player.position = p;
                yield return null;
            }
            if (playerRb != null) playerRb.MovePosition(target);
            else player.position = target;

            Debug.Log("[IntroTrigger] Player auto-step done");
        }

        // Camera trượt vào giữa Boss–Player
        if (cameraController != null && player != null && boss != null)
        {
            Vector3 mid = (player.position + boss.position) * 0.5f;
            Vector3 camStart = cameraController.transform.position;
            Vector3 camTarget = new Vector3(mid.x, mid.y, cameraController.cameraZCoordinate);

            var prev = cameraController.cameraMovementStyle;
            cameraController.cameraMovementStyle = CameraController.CameraStyles.Locked;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, cameraMoveDuration);
                cameraController.transform.position = Vector3.Lerp(camStart, camTarget, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            cameraController.transform.position = camTarget;
            Debug.Log("[IntroTrigger] Camera move done");
        }

        if (extraLockDuration > 0f)
            yield return new WaitForSeconds(extraLockDuration);

        // Mở thoại (game sẽ pause theo DialogueUI).
        string[] lines = BuildLines();
        if (DialogueUI.Instance != null && lines.Length > 0)
        {
            Debug.Log("[IntroTrigger] Mở thoại");
            DialogueUI.Instance.Show(
                lines,
                portrait,
                () =>
                {
                    Debug.Log("[IntroTrigger] Thoại KẾT THÚC → switch BGM, ẩn vật thể & chạy cinematic");

                    // ✅ ĐỔI NHẠC BOSS NGAY LẬP TỨC (không chờ gì)
                    TrySwitchToBossBGM();

                    // ✅ ẨN CÁC OBJECT SAU THOẠI
                    HideObjectsAfterDialogue();

                    // Khôi phục camera
                    if (returnCameraOverhead && cameraController != null)
                        cameraController.cameraMovementStyle = CameraController.CameraStyles.Overhead;

                    // Cho boss hoạt ảnh lại
                    if (bossAnimator != null) bossAnimator.speed = 1f;

                    // MỞ KHÓA CONTROL SAU THOẠI
                    if (playerLocker != null) playerLocker.UnlockControls();

                    // Chạy cinematic
                    if (cinematic != null) cinematic.RunSequence();
                    else Debug.LogError("[IntroTrigger] ❌ Chưa gán reference 'cinematic' trong Inspector!");
                }
            );
        }
        else
        {
            Debug.LogWarning("[IntroTrigger] Không có DialogueUI hoặc lines rỗng → bỏ qua thoại, chạy cinematic luôn.");

            // Dù không có thoại, vẫn bảo đảm nhạc boss bật ngay (nếu bạn muốn)
            TrySwitchToBossBGM();

            // Ẩn các object ngay cả khi không có thoại
            HideObjectsAfterDialogue();

            if (returnCameraOverhead && cameraController != null)
                cameraController.cameraMovementStyle = CameraController.CameraStyles.Overhead;

            if (bossAnimator != null) bossAnimator.speed = 1f;

            if (playerLocker != null) playerLocker.UnlockControls();

            if (cinematic != null) cinematic.RunSequence();
            else Debug.LogError("[IntroTrigger] ❌ Chưa gán reference 'cinematic' trong Inspector!");
        }

        gameObject.SetActive(false);
    }

    string[] BuildLines()
    {
        var ls = new System.Collections.Generic.List<string>(3);
        if (!string.IsNullOrWhiteSpace(line1)) ls.Add(line1);
        if (!string.IsNullOrWhiteSpace(line2)) ls.Add(line2);
        if (!string.IsNullOrWhiteSpace(line3)) ls.Add(line3);
        return ls.ToArray();
    }

    // ====== BGM SWITCH ======
    void TrySwitchToBossBGM()
    {
        if (bgmSource == null || bossMusic == null)
        {
            // Không fail game nếu thiếu — chỉ báo để bạn kéo tham chiếu
            Debug.LogWarning("[BossIntroTrigger] Thiếu 'bgmSource' hoặc 'bossMusic' → không thể đổi nhạc Boss.");
            return;
        }

        if (bgmCrossfadeSeconds <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = bossMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            StartCoroutine(CrossfadeTo(bossMusic, bgmCrossfadeSeconds));
        }
    }

    IEnumerator CrossfadeTo(AudioClip target, float time)
    {
        float startVol = bgmSource.volume;
        float t = 0f;

        // Fade-out
        while (t < time)
        {
            t += Time.unscaledDeltaTime; // không phụ thuộc timeScale
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

    void HideObjectsAfterDialogue()
    {
        if (objectsToHideAfterDialogue == null) return;

        foreach (var obj in objectsToHideAfterDialogue)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log("[BossIntroTrigger] Hide object: " + obj.name);
            }
        }
    }
}
