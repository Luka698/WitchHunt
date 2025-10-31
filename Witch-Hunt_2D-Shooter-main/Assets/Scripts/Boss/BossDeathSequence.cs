using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossDeathSequence : MonoBehaviour
{
    [Header("Boss & Player")]
    public Health bossHealth;                   // Nếu để trống và Boss có Tag="Boss" sẽ auto-find
    public GameObject player;                  // Kéo Player vào (để auto-lock)
    public bool usePlayerControlLocker = true; // Nếu có PlayerControlLocker sẽ gọi lock/unlock
    public bool pauseTimeScale = true;         // Dừng Time.timeScale trong cutscene (khóa gameplay theo thời gian)

    [Header("Fade")]
    public float fadeDuration = 1.25f;         // Thời gian fade đen/sáng (unscaled)
    public float blackHoldBeforeDialogue = 2f; // GIỮ đen trước khi hiện thoại
    public int fadeSortingOrder = 1500;        // Layer của Fade (nên thấp hơn Dialogue)

    [Header("Dialogue")]
    public DialogueRunnerSimple runner;        // Nếu để trống, sẽ tự tạo (persist, tái sử dụng)
    public int runnerSortingOrder = 2000;      // Đặt cao hơn fade để thoại nằm trên nền đen
    public Sprite bossPortrait;                // Avatar Boss (tùy chọn)
    [TextArea(1, 3)] public string line1 = "Được rồi... Lần này ngươi may mắn.";
    [TextArea(1, 3)] public string line2 = "Đi qua cổng đi.";

    [Header("After Death Actions")]
    public GameObject objectToDisable;         // Prefab / cổng / barrier cần ẩn
    public bool destroyInstead = false;        // Nếu bật, Destroy thay vì SetActive(false)
    public float disableDelay = 0.5f;          // Trễ 0.5s sau khi thoại xong rồi mới ẩn

    [Header("Debug")]
    public bool logDebug = true;

    // runtime
    private bool hasTriggered;
    private float prevTimeScale = 1f;
    private GameObject fadeCanvasGO;
    private CanvasGroup fadeGroup;
    private Component cachedLocker;

    void OnValidate()
    {
        if (bossHealth == null)
        {
            var boss = GameObject.FindWithTag("Boss");
            if (boss != null) bossHealth = boss.GetComponent<Health>();
        }
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p;
        }
    }

    void Update()
    {
        if (hasTriggered) return;
        if (bossHealth == null) return;

        if (bossHealth.currentHealth <= 0)
        {
            StartSequence();
        }
    }

    [ContextMenu("TEST: Start Sequence Now")]
    public void StartSequence()
    {
        if (hasTriggered)
        {
            if (logDebug) Debug.Log("[BossDeathCutscene] Already running.");
            return;
        }

        hasTriggered = true;
        if (logDebug) Debug.Log("[BossDeathCutscene] Start sequence.");
        StartCoroutine(Co_Sequence());
    }

    IEnumerator Co_Sequence()
    {
        // 1️⃣ Khóa gameplay input
        LockGameplay();

        // 2️⃣ Fade đen
        EnsureFadeCanvas();
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 3️⃣ Giữ đen 2s
        if (blackHoldBeforeDialogue > 0f)
            yield return new WaitForSecondsRealtime(blackHoldBeforeDialogue);

        // 4️⃣ Hiện thoại
        yield return StartCoroutine(ShowDialogue());

        // 5️⃣ Sau khi thoại xong → ẩn / xoá object (nếu có)
        if (objectToDisable != null)
        {
            yield return new WaitForSecondsRealtime(disableDelay);
            if (destroyInstead)
            {
                Destroy(objectToDisable);
                if (logDebug) Debug.Log("[BossDeathCutscene] Destroyed object: " + objectToDisable.name);
            }
            else
            {
                objectToDisable.SetActive(false);
                if (logDebug) Debug.Log("[BossDeathCutscene] Disabled object: " + objectToDisable.name);
            }
        }

        // 6️⃣ Fade sáng lại
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // 7️⃣ Trả control gameplay
        UnlockGameplay();

        if (logDebug) Debug.Log("[BossDeathCutscene] Sequence complete.");
    }

    // === Fade ===
    void EnsureFadeCanvas()
    {
        if (fadeCanvasGO != null) return;

        fadeCanvasGO = new GameObject("_FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = fadeCanvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = fadeSortingOrder;

        var scaler = fadeCanvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        var panel = new GameObject("Fade", typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(fadeCanvasGO.transform, false);

        var img = panel.GetComponent<Image>();
        img.color = Color.black;

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeGroup = panel.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.interactable = true;
        fadeGroup.blocksRaycasts = true;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeGroup == null) yield break;
        float t = 0f;
        fadeGroup.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeGroup.alpha = to;
    }

    // === Dialogue ===
    IEnumerator ShowDialogue()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<DialogueRunnerSimple>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<DialogueRunnerSimple>();
                runner.persistAfterClose = true;
                runner.startHidden = true;
            }
        }

        runner.sortingOrder = runnerSortingOrder;
        runner.boxAnchorBottom = true;
        runner.charsPerSecond = 40f;
        runner.SetPortrait(bossPortrait);

        string[] lines = string.IsNullOrWhiteSpace(line2)
            ? new[] { line1 }
            : new[] { line1, line2 };

        bool done = false;
        runner.Show(lines, () => { done = true; });

        while (!done) yield return null;
    }

    // === Gameplay Lock ===
    void LockGameplay()
    {
        if (usePlayerControlLocker && player != null)
        {
            var locker = player.GetComponent("PlayerControlLocker");
            if (locker != null)
            {
                cachedLocker = locker;
                var mi = locker.GetType().GetMethod("LockControls");
                if (mi != null) mi.Invoke(locker, null);
            }
        }

        if (pauseTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    void UnlockGameplay()
    {
        if (cachedLocker != null)
        {
            var mi = cachedLocker.GetType().GetMethod("UnlockControls");
            if (mi != null) mi.Invoke(cachedLocker, null);
            cachedLocker = null;
        }

        if (pauseTimeScale)
        {
            Time.timeScale = prevTimeScale;
        }
    }
}
