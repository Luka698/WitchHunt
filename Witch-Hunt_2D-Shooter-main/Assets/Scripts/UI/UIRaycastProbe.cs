using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastProbe : MonoBehaviour
{
    [Header("Input Module")]
    public bool preferNewInputSystem = false; // bật nếu project dùng New Input System
    public bool ensureEventSystem = true;

    [Header("Cursor")]
    public bool forceShowCursor = true;

    [Header("Raycast Unblock")]
    [Tooltip("Tắt raycast của Image alpha thấp & CanvasGroup alpha thấp.")]
    public bool autoUnblockTransparentOverlays = true;
    [Range(0f, 0.1f)] public float alphaThreshold = 0.02f;

    void Awake()
    {
        TryFixEventSystem();
        TryFixCursor();
        TryUnblockTransparent();
    }

    void TryFixEventSystem()
    {
        if (!ensureEventSystem) return;

        var es = FindObjectOfType<EventSystem>();
        if (!es)
        {
            var go = new GameObject("EventSystem (Auto)");
            es = go.AddComponent<EventSystem>();
        }

        // Tìm 2 module
        var standalone = es.GetComponent<StandaloneInputModule>();
        var inputSystemUI = es.GetComponent("InputSystemUIInputModule") as Behaviour;

        bool hasNewInputPackage = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") != null;

        if (preferNewInputSystem && hasNewInputPackage)
        {
            if (inputSystemUI == null) inputSystemUI = es.gameObject.AddComponent(Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule")) as Behaviour;
            if (standalone) standalone.enabled = false;
            if (inputSystemUI) inputSystemUI.enabled = true;
            Debug.Log("[UIInputAutoFix] Using InputSystemUIInputModule.");
        }
        else
        {
            if (standalone == null) standalone = es.gameObject.AddComponent<StandaloneInputModule>();
            if (inputSystemUI) inputSystemUI.enabled = false;
            standalone.enabled = true;
            Debug.Log("[UIInputAutoFix] Using StandaloneInputModule.");
        }
    }

    void TryFixCursor()
    {
        if (!forceShowCursor) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void TryUnblockTransparent()
    {
        if (!autoUnblockTransparentOverlays) return;

        int offImg = 0, offCg = 0;

        foreach (var img in FindObjectsOfType<Image>(true))
        {
            if (img.raycastTarget && img.color.a <= alphaThreshold)
            {
                img.raycastTarget = false;
                offImg++;
                Debug.Log($"[UIInputAutoFix] Disable raycastTarget on transparent Image: {FullPath(img.transform)}");
            }
        }

        foreach (var cg in FindObjectsOfType<CanvasGroup>(true))
        {
            if (cg.blocksRaycasts && cg.alpha <= alphaThreshold)
            {
                cg.blocksRaycasts = false;
                offCg++;
                Debug.Log($"[UIInputAutoFix] Disable blocksRaycasts on transparent CanvasGroup: {FullPath(cg.transform)}");
            }
        }

        if (offImg > 0 || offCg > 0)
            Debug.Log($"[UIInputAutoFix] Unblocked overlays → Images:{offImg}, CanvasGroups:{offCg}");
    }

    string FullPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
