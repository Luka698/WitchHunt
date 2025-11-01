using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelCompleteClickFix : MonoBehaviour
{
    [Header("Optional refs")]
    [Tooltip("Kéo PlayerControlLocker (nếu có) để mở control sau khi win.")]
    public MonoBehaviour playerControlLocker; // expected to have UnlockControls()

    [Header("Sorting/Canvas")]
    public int topSortingOrder = 6000;
    public bool forceOverrideSorting = true;

    [Header("CanvasGroup (panel thắng)")]
    public bool ensureCanvasGroup = true;
    public bool makeInteractable = true;
    public bool makeBlocksRaycasts = true;

    [Header("Unblock overlays (toàn scene)")]
    public bool autoUnblockTransparentOverlays = true;
    [Range(0f, 0.1f)] public float alphaThreshold = 0.02f; // <=2% coi như trong suốt

    [Header("Cursor & Input")]
    public bool forceShowCursor = true;
    public bool recenterPointerFocus = true;

    [Header("First Selected (Enter/Space vẫn OK)")]
    [Tooltip("Kéo nút mặc định (Main Menu/Retry) vào đây để SetSelected.")]
    public GameObject firstSelected;

    void OnEnable()
    {
        // 1) Mở control & hiện chuột
        TryUnlockControls();
        TryEnableCursor();

        // 2) Đảm bảo GraphicRaycaster + Canvas ưu tiên
        var canvas = EnsureTopCanvas();

        // 3) Đảm bảo CanvasGroup của panel thắng nhận raycast
        EnsurePanelCanvasGroup();

        // 4) Gỡ các overlay trong suốt đang che (toàn scene)
        if (autoUnblockTransparentOverlays) UnblockTransparentOverlays();

        // 5) Đặt focus vào nút mặc định (để phím Enter/Space vẫn chạy) nhưng vẫn cho chuột click
        if (recenterPointerFocus) SetFirstSelected();

        Debug.Log("[LevelCompleteClickFix] Win UI is now clickable.");
    }

    void TryUnlockControls()
    {
        if (!playerControlLocker) return;
        var m = playerControlLocker.GetType().GetMethod("UnlockControls");
        if (m != null) m.Invoke(playerControlLocker, null);
    }

    void TryEnableCursor()
    {
        if (!forceShowCursor) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    Canvas EnsureTopCanvas()
    {
        // Tìm Canvas gần nhất trên panel thắng
        var c = GetComponent<Canvas>();
        if (!c) c = gameObject.AddComponent<Canvas>();

        if (forceOverrideSorting) c.overrideSorting = true;
        c.sortingOrder = Mathf.Max(c.sortingOrder, topSortingOrder);

        // GraphicRaycaster để nhận click
        var gr = GetComponent<GraphicRaycaster>();
        if (!gr) gr = gameObject.AddComponent<GraphicRaycaster>();
        gr.enabled = true;

        return c;
    }

    void EnsurePanelCanvasGroup()
    {
        if (!ensureCanvasGroup) return;

        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        if (makeInteractable) cg.interactable = true;
        if (makeBlocksRaycasts) cg.blocksRaycasts = true;
        // nếu panel này dùng alpha = 0 → vẫn cho click: tuỳ bạn,
        // mặc định ta không động đến alpha của panel win
    }

    void UnblockTransparentOverlays()
    {
        int offImg = 0, offCg = 0;

        // Tắt raycast trên Image trong suốt (không thuộc panel win)
        foreach (var img in FindObjectsOfType<Image>(true))
        {
            if (img.transform.IsChildOf(transform)) continue; // giữ raycast của panel win
            if (img.raycastTarget && img.color.a <= alphaThreshold)
            {
                img.raycastTarget = false;
                offImg++;
            }
        }

        // Tắt blocksRaycasts trên CanvasGroup alpha nhỏ (không thuộc panel win)
        foreach (var cg in FindObjectsOfType<CanvasGroup>(true))
        {
            if (cg.transform.IsChildOf(transform)) continue; // giữ panel win
            if (cg.blocksRaycasts && cg.alpha <= alphaThreshold)
            {
                cg.blocksRaycasts = false;
                offCg++;
            }
        }

        if (offImg > 0 || offCg > 0)
            Debug.Log($"[LevelCompleteClickFix] Unblocked overlays → Images:{offImg}, CanvasGroups:{offCg}");
    }

    void SetFirstSelected()
    {
        if (EventSystem.current == null)
        {
            // tạo EventSystem tối thiểu
            var go = new GameObject("EventSystem (Auto)");
            var es = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // Xoá selection cũ rồi set lại nút mặc định
        EventSystem.current.SetSelectedGameObject(null);
        if (firstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}
