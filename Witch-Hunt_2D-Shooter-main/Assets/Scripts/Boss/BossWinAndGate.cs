using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Win khi Boss chết + Mở cổng + Cộng điểm theo máu còn lại của Player.
/// ĐÃ THÊM: đảm bảo cổng hiện diện & ở trạng thái "Closed" ngay từ Start (tuỳ chọn).
/// </summary>
public class BossWinAndGate : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Health của BOSS (kéo component Health của Boss vào đây).")]
    public Component bossHealth; // Health.cs trên Boss

    [Tooltip("Health của PLAYER (kéo Health của Player vào để tính điểm).")]
    public Component playerHealth;

    [Header("Gate / Exit on Win")]
    [Tooltip("Các object gate/block sẽ bị TẮT (SetActive=false) khi THẮNG.")]
    public List<GameObject> gateObjectsToDisableOnWin = new List<GameObject>();

    [Tooltip("Các Animator của cổng (nếu muốn phát trigger mở).")]
    public List<Animator> gateAnimators = new List<Animator>();

    [Tooltip("Tên Trigger để MỞ cổng khi THẮNG (vd: \"Open\").")]
    public string openTriggerName = "Open";

    [Tooltip("Bật collider/trigger thoát khi thắng.")]
    public Collider2D exitColliderToEnableOnWin;
    public bool enableExitColliderOnWin = false;

    [Header("Gate State at Start (sửa lỗi cổng 'mất' khi vào game)")]
    [Tooltip("Nếu bật: đảm bảo tất cả gateObjectsToDisableOnWin được SetActive(true) ở Start.")]
    public bool ensureGateVisibleOnStart = true;

    [Tooltip("Nếu bật: ép Animator của cổng về state đóng khi Start.")]
    public bool resetGateAnimatorAtStart = true;

    [Tooltip("Tên state đóng trên Animator (để Play về đúng state). Để trống thì bỏ qua.")]
    public string closedStateName = "Closed"; // đổi theo controller của bạn

    [Header("Scoring (điểm theo số tim còn lại)")]
    public int pointsIfHearts3 = 1000;
    public int pointsIfHearts2 = 600;
    public int pointsIfHearts1 = 300;
    public int pointsElse = 0;
    [Tooltip("Số tim tối đa được coi là '3 tim' (dùng để clamp).")]
    public int clampHeartsMax = 3;

    [Header("Debug/Options")]
    public bool verboseLog = true;

    bool didWin;

    void Start()
    {
        // Auto-find (nếu quên kéo)
        if (!playerHealth)
        {
            var pl = GameObject.FindGameObjectWithTag("Player");
            if (pl) playerHealth = pl.GetComponentInChildren<Component>();
        }

        // ❶ ĐẢM BẢO CỔNG HIỆN & Ở TRẠNG THÁI ĐÓNG NGAY TỪ START (nếu bạn bật 2 option trên)
        if (ensureGateVisibleOnStart)
        {
            for (int i = 0; i < gateObjectsToDisableOnWin.Count; i++)
            {
                var go = gateObjectsToDisableOnWin[i];
                if (go) go.SetActive(true);
            }
        }
        if (resetGateAnimatorAtStart && !string.IsNullOrEmpty(closedStateName))
        {
            for (int i = 0; i < gateAnimators.Count; i++)
            {
                var an = gateAnimators[i];
                if (an) an.Play(closedStateName, 0, 0f);
            }
        }

        // Exit ban đầu nên tắt (nếu bạn cấu hình bật khi thắng)
        if (enableExitColliderOnWin && exitColliderToEnableOnWin)
            exitColliderToEnableOnWin.enabled = false;

        // Bắt đầu theo dõi Boss chết
        StartCoroutine(WatchBossDeath());
    }

    IEnumerator WatchBossDeath()
    {
        // chờ có tham chiếu bossHealth
        while (!bossHealth) yield return null;

        while (!didWin)
        {
            int hp = ReadIntFromHealth(bossHealth);
            if (hp <= 0)
            {
                didWin = true;
                OnBossDefeated();
                yield break;
            }
            yield return null;
        }
    }

    void OnBossDefeated()
    {
        if (verboseLog) Debug.Log("[BossWinAndGate] Boss defeated → opening gates & scoring.");

        // ❷ MỞ CỔNG / TẮT VẬT CẢN
        for (int i = 0; i < gateObjectsToDisableOnWin.Count; i++)
        {
            var go = gateObjectsToDisableOnWin[i];
            if (go) go.SetActive(false);
        }
        for (int i = 0; i < gateAnimators.Count; i++)
        {
            var an = gateAnimators[i];
            if (an && !string.IsNullOrEmpty(openTriggerName))
                an.SetTrigger(openTriggerName);
        }

        // ❸ BẬT EXIT (nếu có)
        if (enableExitColliderOnWin && exitColliderToEnableOnWin)
            exitColliderToEnableOnWin.enabled = true;

        // ❹ TÍNH ĐIỂM THEO MÁU PLAYER
        int hearts = Mathf.Clamp(ReadIntFromHealth(playerHealth), 0, Mathf.Max(1, clampHeartsMax));
        int add = ScoreForHearts(hearts);
        if (verboseLog) Debug.Log($"[BossWinAndGate] Player hearts={hearts} → +{add} points");
        if (add > 0) TryAddScore(add);
    }

    int ScoreForHearts(int hearts)
    {
        if (hearts >= 3) return pointsIfHearts3;
        if (hearts == 2) return pointsIfHearts2;
        if (hearts == 1) return pointsIfHearts1;
        return pointsElse;
    }

    // ===== Helpers: đọc HP từ mọi kiểu Health bằng reflection =====
    static readonly string[] FieldCandidates = new[]
    {
        "current", "currentHealth", "health", "hp", "value", "Health", "HP", "Current"
    };

    int ReadIntFromHealth(Component c)
    {
        if (!c) return 0;
        var t = c.GetType();

        // Fields
        for (int i = 0; i < FieldCandidates.Length; i++)
        {
            var f = t.GetField(FieldCandidates[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                object v = f.GetValue(c);
                if (v is int vi) return vi;
                if (v is float vf) return Mathf.RoundToInt(vf);
            }
        }
        // Properties
        for (int i = 0; i < FieldCandidates.Length; i++)
        {
            var p = t.GetProperty(FieldCandidates[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead)
            {
                object v = p.GetValue(c, null);
                if (v is int vi) return vi;
                if (v is float vf) return Mathf.RoundToInt(vf);
            }
        }
        // Methods
        var m1 = t.GetMethod("GetCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m1 != null) { object v = m1.Invoke(c, null); if (v is int vi) return vi; if (v is float vf) return Mathf.RoundToInt(vf); }
        var m2 = t.GetMethod("GetHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m2 != null) { object v = m2.Invoke(c, null); if (v is int vi) return vi; if (v is float vf) return Mathf.RoundToInt(vf); }

        return 0;
    }

    // ===== Helpers: cộng điểm vào hệ thống sẵn có =====
    void TryAddScore(int add)
    {
        if (add <= 0) return;

        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            var m = gm.GetType().GetMethod("AddScore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) { m.Invoke(gm, new object[] { add }); return; }

            var f = gm.GetType().GetField("score", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                int cur = (int)f.GetValue(gm);
                f.SetValue(gm, cur + add);
                return;
            }
        }

        var scoreDisplay = FindObjectOfType<ScoreDisplay>();
        if (scoreDisplay != null)
        {
            var m = scoreDisplay.GetType().GetMethod("AddScore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (m != null)
            {
                if (m.IsStatic) m.Invoke(null, new object[] { add });
                else m.Invoke(scoreDisplay, new object[] { add });
                return;
            }
        }

        if (verboseLog) Debug.LogWarning($"[BossWinAndGate] Không tìm thấy GameManager/ScoreDisplay để cộng điểm (+{add}).");
    }
}
