using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueRunnerSimple : MonoBehaviour
{
    [Header("UI Look")]
    public int sortingOrder = 2000;
    public bool boxAnchorBottom = true; // true: hộp thoại ở dưới; false: trên

    [Header("Typewriter")]
    public float charsPerSecond = 40f; // tốc độ gõ (ký tự/giây)

    [Header("Style (manual overrides)")]
    public Font customFont;                  // Kéo font .ttf/.otf đã import vào đây (Inspector)
    public int customFontSize = 28;          // Cỡ chữ
    public Color textColor = new Color(1f, 1f, 1f, 0.95f); // Màu chữ
    public Sprite defaultPortrait;           // Avatar mặc định

    [Header("Life-cycle")]
    public bool persistAfterClose = true;    // TRUE: không phá hủy sau khi xong; chỉ ẩn để dùng lại
    public bool startHidden = true;          // TRUE: ẩn lúc mới tạo (CanvasGroup alpha = 0)

    // runtime
    Canvas canvas;
    CanvasGroup cgRoot;
    Image dimPanel;

    RectTransform box;
    Image boxBg;
    Image portrait;
    Text textLabel;

    string[] lines;
    int lineIndex;
    Action onDone;

    bool typing;
    bool skipToEnd;
    Coroutine typeCo;

    bool built;

    void Awake()
    {
        if (!built) BuildUI();
        if (startHidden) HideImmediate();
    }

    // ======== Public API ========
    public void Show(string[] dialogueLines, Action onDoneCallback)
    {
        if (!built) BuildUI();

        lines = dialogueLines ?? Array.Empty<string>();
        onDone = onDoneCallback;
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    public void Hide()
    {
        // Ẩn CanvasGroup
        if (cgRoot != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(cgRoot, cgRoot.alpha, 0f, 0.15f));
        }
    }

    public void SetFont(Font f) { customFont = f; if (textLabel != null && f != null) textLabel.font = f; }
    public void SetFontSize(int size) { customFontSize = size; if (textLabel != null) textLabel.fontSize = size; }
    public void SetTextColor(Color c) { textColor = c; if (textLabel != null) textLabel.color = c; }
    public void SetPortrait(Sprite s) { defaultPortrait = s; if (portrait != null) { portrait.sprite = s; portrait.enabled = (s != null); } }

    // ======== Flow ========
    IEnumerator Run()
    {
        // hiện panel mờ + hộp thoại
        yield return StartCoroutine(FadeCanvasGroup(cgRoot, cgRoot.alpha, 1f, 0.2f));

        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            yield return StartCoroutine(ShowLine(lines[lineIndex]));
        }

        // xong -> ẩn hoặc phá hủy tùy chọn
        if (persistAfterClose)
        {
            yield return StartCoroutine(FadeCanvasGroup(cgRoot, 1f, 0f, 0.15f));
        }
        else
        {
            yield return StartCoroutine(FadeCanvasGroup(cgRoot, 1f, 0f, 0.15f));
            if (canvas != null) Destroy(canvas.gameObject);
            Destroy(this);
        }

        onDone?.Invoke();
    }

    IEnumerator ShowLine(string content)
    {
        // setup trạng thái
        textLabel.text = "";
        if (defaultPortrait != null)
        {
            portrait.sprite = defaultPortrait;
            portrait.enabled = true;
        }
        else
        {
            portrait.enabled = false;
        }

        typing = true;
        skipToEnd = false;

        typeCo = StartCoroutine(Typewriter(content));

        // input điều khiển: Space / Enter / Mouse1
        while (true)
        {
            if (typing && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                skipToEnd = true; // tua full dòng
            }
            else if (!typing && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                break; // sang dòng kế
            }
            yield return null;
        }
    }

    IEnumerator Typewriter(string content)
    {
        float interval = (charsPerSecond <= 0f) ? 0.0001f : (1f / charsPerSecond);
        float acc = 0f;
        int shown = 0;

        while (shown < content.Length)
        {
            if (skipToEnd)
            {
                textLabel.text = content;
                shown = content.Length;
                break;
            }

            acc += Time.unscaledDeltaTime;
            if (acc >= interval)
            {
                acc -= interval;
                shown = Mathf.Clamp(shown + 1, 0, content.Length);
                textLabel.text = content.Substring(0, shown);
            }
            yield return null;
        }

        typing = false;
        typeCo = null;
    }

    // ======== UI Build ========
    void BuildUI()
    {
        if (built) return;

        // Canvas thoại riêng
        var goCanvas = new GameObject("_DialogueCanvas_Simple", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        cgRoot = goCanvas.GetComponent<CanvasGroup>();
        cgRoot.alpha = 1f; // sẽ bị set lại theo startHidden
        cgRoot.interactable = true;
        cgRoot.blocksRaycasts = true;

        var scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        // Panel mờ đen
        var goDim = new GameObject("DimPanel", typeof(Image));
        goDim.transform.SetParent(goCanvas.transform, false);
        dimPanel = goDim.GetComponent<Image>();
        dimPanel.color = new Color(0f, 0f, 0f, 0.6f);

        var rtDim = goDim.GetComponent<RectTransform>();
        rtDim.anchorMin = Vector2.zero;
        rtDim.anchorMax = Vector2.one;
        rtDim.offsetMin = Vector2.zero;
        rtDim.offsetMax = Vector2.zero;

        // Hộp thoại
        var goBox = new GameObject("Box", typeof(Image));
        goBox.transform.SetParent(goCanvas.transform, false);
        boxBg = goBox.GetComponent<Image>();
        boxBg.color = new Color(0f, 0f, 0f, 0.8f);

        box = goBox.GetComponent<RectTransform>();
        float boxHeight = 280f;
        float margin = 40f;
        if (boxAnchorBottom)
        {
            box.anchorMin = new Vector2(0f, 0f);
            box.anchorMax = new Vector2(1f, 0f);
            box.offsetMin = new Vector2(margin, margin);
            box.offsetMax = new Vector2(-margin, margin + boxHeight);
        }
        else
        {
            box.anchorMin = new Vector2(0f, 1f);
            box.anchorMax = new Vector2(1f, 1f);
            box.offsetMin = new Vector2(margin, -margin - boxHeight);
            box.offsetMax = new Vector2(-margin, -margin);
        }

        // Avatar (trái)
        var goPortrait = new GameObject("Portrait", typeof(Image));
        goPortrait.transform.SetParent(goBox.transform, false);
        portrait = goPortrait.GetComponent<Image>();
        portrait.color = Color.white;
        portrait.preserveAspect = true;

        var rtPortrait = goPortrait.GetComponent<RectTransform>();
        rtPortrait.anchorMin = new Vector2(0f, 0f);
        rtPortrait.anchorMax = new Vector2(0f, 1f);
        rtPortrait.sizeDelta = new Vector2(220f, -40f);
        rtPortrait.anchoredPosition = new Vector2(120f, 0f);

        // Text (phải)
        var goText = new GameObject("Text", typeof(Text));
        goText.transform.SetParent(goBox.transform, false);
        textLabel = goText.GetComponent<Text>();
        textLabel.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        textLabel.fontSize = customFontSize;
        textLabel.alignment = TextAnchor.UpperLeft;
        textLabel.color = textColor;
        textLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        textLabel.verticalOverflow = VerticalWrapMode.Truncate;
        textLabel.supportRichText = true; // cho phép <b>, <i>, <color=...>

        var rtText = goText.GetComponent<RectTransform>();
        rtText.anchorMin = new Vector2(0f, 0f);
        rtText.anchorMax = new Vector2(1f, 1f);
        rtText.offsetMin = new Vector2(220f + 40f, 30f);
        rtText.offsetMax = new Vector2(-30f, -30f);

        built = true;
    }

    void HideImmediate()
    {
        if (cgRoot != null) cgRoot.alpha = 0f;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float duration)
    {
        float t = 0f;
        g.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }
}
