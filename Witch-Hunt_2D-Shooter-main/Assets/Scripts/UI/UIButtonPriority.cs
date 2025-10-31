using UnityEngine;
using UnityEngine.UI;

public class UIButtonPriority : MonoBehaviour
{
    [Header("Set UI Sorting Order")]
    public int uiSortingOrder = 5000; // rất cao để vượt Boss & Dialogue

    [Header("Force Add Canvas")]
    public bool forceAddCanvas = true;

    [Header("Force Add Raycaster")]
    public bool forceAddRaycaster = true;

    void Awake()
    {
        EnsureCanvasPriority();
    }

    void EnsureCanvasPriority()
    {
        // Tìm Canvas cha — cách cũ để support Unity cũ
        Canvas parentCanvas = null;
        Transform p = transform.parent;
        while (p != null && parentCanvas == null)
        {
            parentCanvas = p.GetComponent<Canvas>();
            p = p.parent;
        }

        // Nếu không có, tạo mới
        if (parentCanvas == null && forceAddCanvas)
        {
            parentCanvas = gameObject.AddComponent<Canvas>();
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = uiSortingOrder;
        }
        else if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = uiSortingOrder;
        }

        // GraphicRaycaster để UI nhận chuột
        if (forceAddRaycaster && parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Debug.Log("[UIButtonPriority] UI boosted! Sorting Order = " + uiSortingOrder);
    }
}
