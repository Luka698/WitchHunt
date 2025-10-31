using UnityEngine;
using TMPro;
using System.Collections;

public class SimpleDialoguePopup : MonoBehaviour
{
    public static SimpleDialoguePopup Instance;

    [Header("UI")]
    public CanvasGroup panel;        // Panel có CanvasGroup (alpha, interactable)
    public TMP_Text textLabel;       // TMP_Text để hiện thoại
    public float fade = 0.2f;        // thời gian fade in/out

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

    public static void Show(string msg, float seconds)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.CoShow(msg, seconds));
    }

    IEnumerator CoShow(string msg, float seconds)
    {
        if (textLabel != null) textLabel.text = msg;

        // fade in
        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            if (panel != null) panel.alpha = Mathf.Clamp01(t / fade);
            yield return null;
        }
        if (panel != null)
        {
            panel.alpha = 1f;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, seconds));

        // fade out
        t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            if (panel != null) panel.alpha = 1f - Mathf.Clamp01(t / fade);
            yield return null;
        }
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }
}
