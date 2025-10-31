using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level4Boss_UIFix : MonoBehaviour
{
    [Header("Cursor")]
    public bool forceShowCursor = true;
    public CursorLockMode desiredLockMode = CursorLockMode.None;

    [Header("EventSystem / Input Module")]
    public bool ensureEventSystem = true;
    [Tooltip("Ưu tiên StandaloneInputModule (Old Input Manager). Nếu bạn dùng New Input System thì tắt ô này và tự dùng InputSystemUIInputModule.")]
    public bool preferStandaloneInputModule = true;

    [Header("Raycast & Canvas")]
    public bool enableGraphicRaycasterOnAllCanvas = true;

    [Header("Auto Unblock Transparent Overlays")]
    [Tooltip("Tự tắt raycastTarget (Image) hoặc blocksRaycasts (CanvasGroup) nếu alpha quá nhỏ (che mất UI).")]
    public bool autoUnblockTransparentCovers = true;
    [Range(0f, 0.2f)]
    public float alphaThreshold = 0.02f; // <= 2% coi như trong suốt

    [Header("Debug")]
    public bool debugPointer = false;
    public Color debugRayGizmoColor = new Color(0f, 1f, 1f, 0.6f);

    void Awake()
    {
        TryFixCursor();
        TryFixEventSystem();
        TryEnableGraphicRaycasters();
        TryUnblockTransparentCovers();
    }

    void TryFixCursor()
    {
        if (!forceShowCursor) return;
        Cursor.lockState = desiredLockMode;
        Cursor.visible = true;
    }

    void TryFixEventSystem()
    {
        if (!ensureEventSystem) return;

        var es = FindObjectOfType<EventSystem>();
        if (!es)
        {
            var go = new GameObject("EventSystem (Auto)");
            es = go.AddComponent<EventSystem>();
            if (preferStandaloneInputModule)
                go.AddComponent<StandaloneInputModule>();
            Debug.Log("[Level4Boss_UIFix] Created EventSystem.");
        }
        else
        {
            // đảm bảo có 1 input module hoạt động
            var standalone = es.GetComponent<StandaloneInputModule>();
            var inputSystemUI = es.GetComponent("InputSystemUIInputModule"); // tránh hard dep vào package mới

            if (preferStandaloneInputModule)
            {
                if (!standalone) standalone = es.gameObject.AddComponent<StandaloneInputModule>();
                // nếu có InputSystemUIInputModule → disable component đó
                if (inputSystemUI)
                {
                    var comp = (Behaviour)inputSystemUI;
                    comp.enabled = false;
                }
                standalone.enabled = true;
            }
            else
            {
                // dùng New Input System → bật InputSystemUIInputModule nếu có
                if (inputSystemUI)
                {
                    var comp = (Behaviour)inputSystemUI;
                    comp.enabled = true;
                }
                // và tắt Standalone nếu có
                if (standalone) standalone.enabled = false;
            }
        }
    }

    void TryEnableGraphicRaycasters()
    {
        if (!enableGraphicRaycasterOnAllCanvas) return;

        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            var gr = c.GetComponent<GraphicRaycaster>();
            if (!gr) gr = c.gameObject.AddComponent<GraphicRaycaster>();
            gr.enabled = true;
        }
        Debug.Log($"[Level4Boss_UIFix] Ensured GraphicRaycaster on {canvases.Length} Canvas(es).");
    }

    void TryUnblockTransparentCovers()
    {
        if (!autoUnblockTransparentCovers) return;

        int fixedImages = 0, fixedCanvasGroups = 0;

        // 1) Image trong suốt nhưng raycastTarget = true → tắt raycastTarget
        var images = FindObjectsOfType<Image>(true);
        foreach (var img in images)
        {
            // full-screen overlay hoặc ảnh trong suốt che phủ
            var col = img.color;
            bool transparent = col.a <= alphaThreshold;

            if (transparent && img.raycastTarget)
            {
                img.raycastTarget = false;
                fixedImages++;
                Debug.Log($"[Level4Boss_UIFix] Disable raycastTarget on transparent Image: {FullPath(img.transform)}");
            }
        }

        // 2) CanvasGroup alpha ~0 nhưng blocksRaycasts = true → tắt blocksRaycasts
        var groups = FindObjectsOfType<CanvasGroup>(true);
        foreach (var g in groups)
        {
            bool transparent = g.alpha <= alphaThreshold;
            if (transparent && g.blocksRaycasts)
            {
                g.blocksRaycasts = false;
                fixedCanvasGroups++;
                Debug.Log($"[Level4Boss_UIFix] Disable blocksRaycasts on transparent CanvasGroup: {FullPath(g.transform)}");
            }
        }

        if (fixedImages > 0 || fixedCanvasGroups > 0)
        {
            Debug.Log($"[Level4Boss_UIFix] Unblocked overlays → Images:{fixedImages}, CanvasGroups:{fixedCanvasGroups}");
        }
    }

    string FullPath(Transform t)
    {
        List<string> parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    // ===== Optional debug: xem UI nào đang dưới con trỏ =====
    void OnGUI()
    {
        if (!debugPointer) return;
        if (EventSystem.current == null) return;

        Vector2 mouse = Input.mousePosition;
        var es = EventSystem.current;
        var ped = new PointerEventData(es) { position = mouse };
        var results = new List<RaycastResult>();

        // gom tất cả GraphicRaycaster trong scene để raycast
        var grs = FindObjectsOfType<GraphicRaycaster>(true);
        foreach (var gr in grs) gr.Raycast(ped, results);

        if (results.Count > 0)
        {
            GUILayout.BeginArea(new Rect(10, 10, 600, 200), GUI.skin.box);
            GUILayout.Label("<b>UI under mouse:</b>");
            for (int i = 0; i < Mathf.Min(5, results.Count); i++)
            {
                var r = results[i];
                GUILayout.Label($"- {r.gameObject.name} (sortingOrder:{r.sortingOrder}, dist:{r.distance:F2})");
            }
            GUILayout.EndArea();
        }
    }

    void OnDrawGizmos()
    {
        if (!debugPointer) return;
        Gizmos.color = debugRayGizmoColor;
        // vẽ 1 tia mỏng nơi con trỏ (tham khảo)
        Vector3 m = Input.mousePosition;
        var cam = Camera.main;
        if (cam)
        {
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, Mathf.Abs(cam.transform.position.z)));
            Gizmos.DrawSphere(wp, 0.05f);
        }
    }
}
