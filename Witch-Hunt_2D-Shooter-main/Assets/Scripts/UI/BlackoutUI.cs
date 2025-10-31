using UnityEngine;

public class BlackoutUI : MonoBehaviour
{
    [Tooltip("Panel den full screen (GameObject)")]
    public GameObject blackoutPanel;

    static BlackoutUI _instance;

    void Awake()
    {
        _instance = this;
        if (blackoutPanel != null) blackoutPanel.SetActive(false);
    }

    public static void Show()
    {
        if (_instance == null || _instance.blackoutPanel == null) return;
        _instance.blackoutPanel.SetActive(true);
    }
}
