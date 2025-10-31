using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CameraShakeFlash : MonoBehaviour
{
    [Header("Shake")]
    public float defaultShakeAmplitude = 0.2f;
    public float defaultShakeDuration = 0.15f;

    [Header("Flash")]
    public Image whiteOverlay;             // Image trắng full-screen trên Canvas Overlay
    [Range(0f, 1f)] public float defaultFlashAlpha = 0.35f;
    public float defaultFlashDuration = 0.12f;

    Vector3 baseLocalPos;
    Coroutine co;

    void OnEnable() { baseLocalPos = transform.localPosition; }

    // ===== API cũ (nếu nơi khác đang dùng) =====
    public void ShakeAndFlash(
        float shakeAmp = -1f, float shakeDur = -1f,
        float flashAlpha = -1f, float flashDur = -1f)
    {
        float amp = shakeAmp < 0 ? defaultShakeAmplitude : shakeAmp;
        float dur = shakeDur < 0 ? defaultShakeDuration : shakeDur;
        float fal = flashAlpha < 0 ? defaultFlashAlpha : flashAlpha;
        float fdu = flashDur < 0 ? defaultFlashDuration : flashDur;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoShake(amp, dur, fal, fdu, useEndOfFrame: false));
    }

    // ===== API mới: follow-safe (rung theo local, chạy sau camera follow) =====
    public void ShakeFollowSafe() =>
        ShakeFollowSafe(defaultShakeAmplitude, defaultShakeDuration, defaultFlashAlpha, defaultFlashDuration);

    public void ShakeFollowSafe(float amp, float dur, float flashAlpha, float flashDur)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoShake(amp, dur, flashAlpha, flashDur, useEndOfFrame: true));
    }

    IEnumerator CoShake(float amp, float dur, float fAlpha, float fDur, bool useEndOfFrame)
    {
        Vector3 startLocal = transform.localPosition;

        if (whiteOverlay && fAlpha > 0f && fDur > 0f)
            StartCoroutine(CoFlash(fAlpha, fDur));

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - t / dur; // fade-out
            Vector2 rnd = Random.insideUnitCircle * amp * k;
            transform.localPosition = startLocal + (Vector3)rnd;

            if (useEndOfFrame) yield return new WaitForEndOfFrame(); // sau follow / cinemachine
            else yield return null;
        }

        transform.localPosition = startLocal;
        co = null;
    }

    IEnumerator CoFlash(float peakAlpha, float dur)
    {
        if (!whiteOverlay) yield break;

        // up
        float t = 0f;
        while (t < dur * 0.5f)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(0f, peakAlpha, t / (dur * 0.5f)));
            yield return null;
        }
        // down
        t = 0f;
        while (t < dur * 0.5f)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(peakAlpha, 0f, t / (dur * 0.5f)));
            yield return null;
        }
        SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        if (!whiteOverlay) return;
        var c = whiteOverlay.color; c.a = a; whiteOverlay.color = c;
    }
}
