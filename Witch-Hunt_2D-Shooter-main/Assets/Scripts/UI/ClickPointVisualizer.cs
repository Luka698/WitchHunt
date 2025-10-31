using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickPointVisualizer : MonoBehaviour
{
    [Header("Trigger")]
    public bool leftClick = true;
    public bool rightClick = false;
    public bool middleClick = false;

    [Header("Pulse Style")]
    public Color pulseColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    public float startSize = 24f;
    public float endSize = 72f;
    [Range(0f, 0.9f)] public float ringThickness = 0.25f;
    public float duration = 0.35f;
    [Range(0, 3)] public int extraRipples = 1;
    public float rippleDelay = 0.08f;

    [Header("Sprite (optional)")]
    [Tooltip("Để trống thì sẽ tự tạo sprite 1x1 trắng bằng code.")]
    public Sprite markerSprite;

    [Header("Canvas Host (auto create if null)")]
    public Canvas overlayCanvas; // nếu để trống sẽ tự tạo Canvas Overlay

    [Header("Misc")]
    public bool useUnscaledTime = true;
    public bool logUIUnderPointer = false;

    const string AUTO_CANVAS_NAME = "ClickDebugCanvas (Auto)";
    RectTransform overlayRoot;
    Sprite _fallbackSprite; // sprite tạo bằng code nếu không có markerSprite

    void Awake()
    {
        EnsureOverlayCanvas();
        EnsureMarkerSprite();
    }

    void Update()
    {
        bool fire =
            (leftClick && Input.GetMouseButtonDown(0)) ||
            (rightClick && Input.GetMouseButtonDown(1)) ||
            (middleClick && Input.GetMouseButtonDown(2));

        if (!fire) return;

        Vector2 screenPos = GetMouseOnCorrectDisplay();

        if (logUIUnderPointer)
        {
            bool onUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            Debug.Log((onUI ? "🎯 On UI" : "⬜ World") + " | " + screenPos);
        }

        StartCoroutine(SpawnPulse(screenPos, 0f));
        for (int i = 1; i <= extraRipples; i++)
            StartCoroutine(SpawnPulse(screenPos, rippleDelay * i));
    }

    void EnsureOverlayCanvas()
    {
        if (overlayCanvas == null)
        {
            var found = GameObject.Find(AUTO_CANVAS_NAME);
            if (found) overlayCanvas = found.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                var go = new GameObject(AUTO_CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                overlayCanvas = go.GetComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 32760;

                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                var cg = go.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false; // không chặn click UI khác
            }
        }

        overlayRoot = overlayCanvas.GetComponent<RectTransform>();
        if (!overlayRoot) overlayRoot = overlayCanvas.gameObject.AddComponent<RectTransform>();
        overlayRoot.anchorMin = overlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        overlayRoot.anchoredPosition = Vector2.zero;
    }

    void EnsureMarkerSprite()
    {
        if (markerSprite != null) return;

        // Tạo 1x1 trắng bằng code (ổn định mọi phiên bản Unity)
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
    }

    Vector2 GetMouseOnCorrectDisplay()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        Vector3 rel = Display.RelativeMouseAt(Input.mousePosition);
        if (rel.z != 0) return new Vector2(rel.x, rel.y);
#endif
        return Input.mousePosition;
    }

    IEnumerator SpawnPulse(Vector2 screenPos, float delay)
    {
        if (delay > 0f)
        {
            float tDelay = 0f;
            while (tDelay < delay)
            {
                tDelay += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        Vector2 localPos;
        Camera cam = (overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null :
                     (overlayCanvas.worldCamera ? overlayCanvas.worldCamera : Camera.main);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot, screenPos, cam, out localPos
        );

        // Marker gốc
        var go = new GameObject("ClickPulse", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(overlayRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(startSize, startSize);

        var img = go.GetComponent<Image>();
        img.color = pulseColor;
        img.raycastTarget = false;
        img.sprite = markerSprite != null ? markerSprite : _fallbackSprite;
        img.type = Image.Type.Sliced; // với sprite 1x1 sẽ là hình vuông/màu, vẫn ok cho pulse

        // Inner ring (tuỳ chọn)
        Image inner = null;
        if (ringThickness > 0f)
        {
            var innerGO = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            var innerRT = innerGO.GetComponent<RectTransform>();
            innerRT.SetParent(rt, false);
            innerRT.anchorMin = innerRT.anchorMax = new Vector2(0.5f, 0.5f);
            innerRT.pivot = new Vector2(0.5f, 0.5f);
            innerRT.sizeDelta = Vector2.one * startSize * (1f - ringThickness);

            inner = innerGO.GetComponent<Image>();
            inner.color = pulseColor;
            inner.raycastTarget = false;
            inner.sprite = img.sprite;
            inner.type = Image.Type.Sliced;
        }

        // Animate
        float t = 0f;
        Color c0 = pulseColor;
        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float a = Mathf.Clamp01(t / duration);

            float size = Mathf.Lerp(startSize, endSize, EaseOutCubic(a));
            rt.sizeDelta = new Vector2(size, size);
            if (inner) inner.rectTransform.sizeDelta = Vector2.one * size * (1f - ringThickness);

            // fade out
            Color c = c0; c.a = Mathf.Lerp(c0.a, 0f, a);
            img.color = c;
            if (inner) inner.color = c;

            yield return null;
        }

        Destroy(go);
    }

    float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
}
