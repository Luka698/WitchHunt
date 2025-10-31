using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// UI thoại: nền + ảnh + text chạy từng chữ, tạm dừng game trong lúc thoại.
/// Nhấn chuột trái/Space/Enter: nếu đang chạy chữ -> hiện full; nếu đã full -> sang câu tiếp.
/// Hết thoại: tự ẩn panel, khôi phục timeScale và gọi callback.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI Refs")]
    public CanvasGroup panel;      // CanvasGroup của khung thoại
    public Image portrait;        // Ảnh chân dung (có thể để trống)
    public TMP_Text textLabel;    // Văn bản thoại (TextMeshPro)

    [Header("Behavior")]
    [Min(1f)] public float charsPerSecond = 40f;
    public float fadeDuration = 0.2f;
    public KeyCode[] advanceKeys = new KeyCode[] { KeyCode.Space, KeyCode.Return };

    bool isActive;
    bool isTyping;
    float prevTimeScale = 1f;

    readonly List<string> lines = new List<string>();
    int index = -1;
    Coroutine typeCo;
    Action onComplete;

    void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (!isActive) return;

        // input dùng thời gian thực, không phụ thuộc timeScale
        bool click = Input.GetMouseButtonDown(0);
        bool key = false;
        for (int i = 0; i < advanceKeys.Length; i++)
        {
            if (Input.GetKeyDown(advanceKeys[i])) { key = true; break; }
        }

        if (click || key)
        {
            if (isTyping) ShowFullCurrent();
            else NextLine();
        }
    }

    // ===== API =====
    public void Show(string[] dialogueLines, Sprite portraitSprite = null, Action onDone = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f; // pause game

        lines.Clear();
        lines.AddRange(dialogueLines);
        index = -1;
        onComplete = onDone;

        if (portrait != null) portrait.sprite = portraitSprite;

        StartCoroutine(FadeCanvas(1f, fadeDuration));
        isActive = true;
        NextLine();
    }

    // ===== Core =====
    void NextLine()
    {
        index++;
        if (index >= lines.Count)
        {
            Close();
            return;
        }
        StartTyping(lines[index]);
    }

    void StartTyping(string text)
    {
        if (textLabel == null) return;
        if (typeCo != null) StopCoroutine(typeCo);
        typeCo = StartCoroutine(TypeCo(text));
    }

    IEnumerator TypeCo(string text)
    {
        isTyping = true;
        textLabel.text = text;
        textLabel.ForceMeshUpdate();
        int total = textLabel.textInfo.characterCount;
        textLabel.maxVisibleCharacters = 0;

        if (total <= 0) { isTyping = false; typeCo = null; yield break; }

        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i < total; i++)
        {
            textLabel.maxVisibleCharacters = i + 1;
            yield return new WaitForSecondsRealtime(delay); // vì timeScale=0
        }

        isTyping = false;
        typeCo = null;
    }

    void ShowFullCurrent()
    {
        if (textLabel == null) return;
        if (typeCo != null) StopCoroutine(typeCo);
        textLabel.ForceMeshUpdate();
        textLabel.maxVisibleCharacters = textLabel.textInfo.characterCount;
        isTyping = false;
        typeCo = null;
    }

    void Close()
    {
        isActive = false;
        Time.timeScale = prevTimeScale; // resume game

        StartCoroutine(FadeCanvas(0f, fadeDuration));
        var cb = onComplete; onComplete = null;
        if (cb != null) cb.Invoke();
    }

    IEnumerator FadeCanvas(float target, float dur)
    {
        if (panel == null) yield break;
        float start = panel.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // không bị ảnh hưởng bởi timeScale=0
            panel.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / dur));
            yield return null;
        }
        panel.alpha = target;
        bool on = target > 0.5f;
        panel.interactable = on;
        panel.blocksRaycasts = on;
    }
}
