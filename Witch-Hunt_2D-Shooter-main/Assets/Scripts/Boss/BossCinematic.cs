using System.Collections;
using UnityEngine;

public class BossCinematic : MonoBehaviour
{
    [Header("Boss References")]
    public Transform boss;          // Doraemon (root)
    public Animator bossAnimator;   // Animator trên Doraemon (cinematic Laugh/Jump/JumpLand)
    public Rigidbody2D bossRb;

    [Header("Gate (Khóa đường)")]
    public GameObject gatePrefab;
    public Transform gateSpawnPoint;

    [Header("Jump Settings")]
    public Transform landingPoint;
    public float jumpStartDelay = 0.40f;
    public float jumpDuration = 0.90f;
    public float jumpArcHeight = 2.50f;
    public float landPause = 0.15f;

    [Header("Laugh")]
    public float laughDelay = 0.80f;

    [Header("Shockwave")]
    public ShockwaveSpawner shockwaveSpawner;
    public int shockwaveRadiusTiles = 3;
    public Vector2 tileSize = new Vector2(1f, 1f);

    [Header("Visual (Renderer-Only Switch)")]
    [Tooltip("Doraemon (root) – nhánh cinematic dùng BodyAnimator + SpriteRenderer riêng.")]
    public GameObject bodyVisual;   // ← kéo chính GameObject Doraemon (root)
    [Tooltip("Child Move1 – Animator/SpriteRenderer cho Move/Prepare/Punch.")]
    public GameObject move1Visual;  // ← kéo child Move1

    [Header("Audio & Camera FX")]
    [Tooltip("SFXLibrary trong scene (clip Laugh/ Shockwave).")]
    public SFXLibrary sfx;
    [Tooltip("CameraShakeFlash để rung + flash sau Shockwave.")]
    public CameraShakeFlash camFx;

    [Header("Options")]
    public bool activateBossAfter = true;

    // ==== Renderer-only switchers (không SetActive trên root) ====
    public void ShowBody_RendererOnly()
    {
        ToggleRenderers(bodyVisual, true);
        ToggleRenderers(move1Visual, false);
    }
    public void ShowMove1_RendererOnly()
    {
        ToggleRenderers(bodyVisual, false);
        ToggleRenderers(move1Visual, true);
    }
    public void HideAll_RendererOnly()
    {
        ToggleRenderers(bodyVisual, false);
        ToggleRenderers(move1Visual, false);
    }
    private void ToggleRenderers(GameObject go, bool on)
    {
        if (!go) return;
        // Bật/tắt TẤT CẢ SpriteRenderer & Animator bên dưới go (kể cả con cháu)
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.enabled = on;

        var anims = go.GetComponentsInChildren<Animator>(true);
        foreach (var a in anims) a.enabled = on;
    }

    public void RunSequence()
    {
        Debug.Log("[Cinematic] RunSequence() được gọi");
        StartCoroutine(CoRun());
    }

    private IEnumerator CoRun()
    {
        if (boss == null) { Debug.LogError("[Cinematic] ❌ boss = null"); yield break; }
        if (landingPoint == null) { Debug.LogError("[Cinematic] ❌ landingPoint = null"); yield break; }

        // Ban đầu chỉ hiển thị body (cinematic), ẩn Move1
        ShowBody_RendererOnly();

        BossController bc = boss.GetComponent<BossController>();

        // --- Gate ---
        if (gatePrefab != null && gateSpawnPoint != null)
            Instantiate(gatePrefab, gateSpawnPoint.position, Quaternion.identity);

        // --- Laugh ---
        if (bossAnimator != null) bossAnimator.SetTrigger("Laugh");
        if (sfx) sfx.Play2D(sfx.laugh);
        if (laughDelay > 0f) yield return new WaitForSeconds(laughDelay);

        // --- JumpStart ---
        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger("JumpStart");
            bossAnimator.ResetTrigger("JumpAir");
            bossAnimator.ResetTrigger("JumpLand");
            bossAnimator.SetTrigger("JumpStart");
        }
        if (jumpStartDelay > 0f) yield return new WaitForSeconds(jumpStartDelay);

        // --- JumpAir + tween ---
        if (bossAnimator != null) bossAnimator.SetTrigger("JumpAir");

        bool restoreKinematic = false;
        if (bossRb != null && !bossRb.isKinematic) { bossRb.isKinematic = true; restoreKinematic = true; }
        if (bossRb != null) { bossRb.velocity = Vector2.zero; bossRb.angularVelocity = 0f; }

        Vector3 start = boss.position;
        Vector3 end = landingPoint.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, jumpDuration);
            float k = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(start, end, k);
            float arc = 1f - Mathf.Pow(2f * k - 1f, 2f);
            pos.y += arc * jumpArcHeight;

            boss.position = pos;
            yield return null;
        }
        boss.position = end;

        // --- JumpLand ---
        if (bossAnimator != null) bossAnimator.SetTrigger("JumpLand");
        if (landPause > 0f) yield return new WaitForSeconds(landPause);

        // --- Shockwave ---
        if (shockwaveSpawner != null)
            shockwaveSpawner.SpawnRing(end, shockwaveRadiusTiles, tileSize);

        if (sfx) sfx.Play2D(sfx.shockwave);
        if (camFx) camFx.ShakeAndFlash(0.3f, 0.2f, 0.25f, 0.18f);

        // === Chuyển hiển thị: ẩn body, bật Move1 (renderer only) ===
        ShowMove1_RendererOnly();

        if (bossRb != null && restoreKinematic) bossRb.isKinematic = false;

        // --- Cho boss vào FreeBrain sau cinematic ---
        if (bc != null)
        {
            bc.facePlayer = false;   // luôn thẳng đứng
            bc.StartFreeBrain();
        }

        if (activateBossAfter && bc != null)
        {
            // bc.Activate(); // nếu bạn có hệ thống khác
        }
    }
}
