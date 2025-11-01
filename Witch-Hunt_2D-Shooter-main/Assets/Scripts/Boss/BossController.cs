using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossController : MonoBehaviour
{
    // =========================
    //   ENUMS
    // =========================
    public enum BossState { Dormant, Phase1, Phase2, Phase3, Dead }
    public enum ShootDirOverride { None = -1, Down = 0, Up = 1, Left = 2, Right = 3 }

    // Internal action guard (độc quyền 1 hành động tại 1 thời điểm)
    private enum ActionFlag { None, Prepare, Punch, Rocket, Cannon }
    private ActionFlag currentAction = ActionFlag.None;

    // =========================
    //   REFERENCES
    // =========================
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Collider2D hitCollider;
    public Animator bodyAnimator;
    public Animator move1Animator;
    public LayerMask playerLayer;
    public string playerTag = "Player";

    [Header("Audio & Camera FX")]
    public SFXLibrary sfx;
    public CameraShakeFlash camFx;

    // =========================
    //   GENERAL / MOVE
    // =========================
    [Header("General")]
    public bool facePlayer = false;
    public bool hoverIdle = false;
    public Vector2 hoverAmplitude = new Vector2(0.5f, 0.8f);
    public Vector2 hoverFrequency = new Vector2(1.2f, 0.9f);

    [Header("Movement Tuning")]
    public float baseMoveSpeed = 2.6f;
    [Min(0f)] public float speedMultiplier = 1f;
    public float stopDistance = 0.4f;

    // =========================
    //   AI / RNG CONTROL
    // =========================
    [Header("Free Brain (AI Random)")]
    public bool enableFreeBrain = false;
    public Vector2 dashCooldownRange = new Vector2(1.0f, 2.2f);
    [Range(0f, 1f)] public float dashChancePerSecond = 0.35f;
    public Vector2 rocketCooldownRange = new Vector2(3.5f, 6.0f);
    [Range(0f, 1f)] public float rocketChancePerSecond = 0.35f;
    public Vector2 cannonCooldownRange = new Vector2(2.5f, 5.0f);
    [Range(0f, 1f)] public float cannonChancePerSecond = 0.35f;
    public float rocketWeight = 1.0f;
    public float punchWeight = 1.0f;
    public float cannonWeight = 1.0f;

    // =========================
    //   PUNCH SETTINGS
    // =========================
    [Header("Two-Phase Punch (Dash)")]
    public float preCastDuration = 0.15f;
    public float postCastDelay = 0.50f;
    public float punchActiveDuration = 0.12f;
    public float punchDashSpeed = 8.5f;
    public float postPunchExtraHold = 0.18f;
    [Tooltip("Giữ tối thiểu pose Prepare để tránh mất hình (giây).")]
    public float minPrepareHold = 0.06f;
    [Range(0, 3)] public int safetyFrames = 1;

    [Header("Punch Hitbox (Math)")]
    public Vector2 punchHitboxSizeHorizontal = new Vector2(1.20f, 0.80f);
    public Vector2 punchHitboxSizeVertical = new Vector2(0.80f, 1.20f);
    public float punchReach = 0.8f;
    public int punchDamage = 1;

    [Header("Punch Hitbox (Runtime Object)")]
    [Tooltip("Bật để tạo GameObject + BoxCollider2D (isTrigger) làm hitbox trong thời gian Punch.")]
    public bool useRuntimePunchHitbox = true;
    [Tooltip("Nới thêm kích thước hitbox runtime (mỗi chiều).")]
    public float runtimeHitboxPadding = 0.04f;
    [Tooltip("Layer cho hitbox runtime (nên là 1 layer chỉ va chạm Player).")]
    public int runtimeHitboxLayer = 0; // 0 = Default; khuyến nghị tạo layer riêng

    [Header("Punch Close-Contact Auto-Hit")]
    [Tooltip("Nếu player đang đứng/rê rất sát Boss thì khi Punch sẽ auto trúng.")]
    public bool autoHitWhenVeryClose = true;
    [Tooltip("Bán kính kiểm tra sát thân (tính từ tâm collider của Boss).")]
    public float closeAutoHitRadius = 0.7f;

    [Header("Collision (Walls/Obstacles)")]
    public LayerMask obstacleLayers;
    public float skinWidth = 0.02f;
    public bool stopPunchOnObstacle = true;

    // =========================
    //   ANIMATOR PARAMS
    // =========================
    [Header("Animator Parameters (names)")]
    public string paramDir = "Dir"; // int: 0 D,1 U,2 L,3 R
    public string paramIsMoving = "IsMoving";
    public string paramIsPreparing = "IsPreparing";
    public string paramIsPunching = "IsPunching";
    public string paramIsRocketing = "IsRocketing";
    public string paramIsCannoning = "IsCannoning";

    // =========================
    //   ROCKET CONFIG / PREFABS / CANNON
    // =========================
    [Header("Rocket (Warning → Fall → Explosion)")]
    public float rocketCastDuration = 0.6f;
    public float postRocketHold = 0.1f;
    public Vector2Int rocketCountRange = new Vector2Int(4, 7);
    public float rocketFireInterval = 0.15f;
    public Vector2 landingRadiusRange = new Vector2(1.5f, 3.5f);
    public float warningTime = 0.8f;
    public float fallDuration = 0.45f;
    public float fallStartHeight = 12f;

    [Header("Rocket Prefabs")]
    public GameObject warningPrefab;
    public GameObject fallingMissilePrefab;
    public GameObject explosionPrefab;

    [Header("Rocket Muzzle (position tuning)")]
    public Transform muzzleOverride;
    public ShootDirOverride shootDirectionOverride = ShootDirOverride.None;
    public bool useLocalMuzzleOffset = true;
    public Vector2 muzzleNudgeGlobal = Vector2.zero;
    public Vector2 muzzleNudgeRight = new Vector2(0.7f, 0.2f);
    public Vector2 muzzleNudgeLeft = new Vector2(-0.7f, 0.2f);
    public Vector2 muzzleNudgeUp = new Vector2(0f, 0.9f);
    public Vector2 muzzleNudgeDown = new Vector2(0f, 0.2f);

    [Header("Rocket Launch VFX (speed control)")]
    public GameObject risingMissilePrefab;
    public GameObject muzzleSmokePrefab;
    public float riseDistance = 3f;
    public float riseSpeed = 12f;
    public float riseDuration = 0.25f;
    public AnimationCurve riseCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Directional Anchors — ROCKET (4 hướng)")]
    public bool useDirectionalAnchorsRocket = false;
    public Transform rocketAnchorRight;
    public Transform rocketAnchorLeft;
    public Transform rocketAnchorUp;
    public Transform rocketAnchorDown;

    [Header("Directional Anchors — CANNON (4 hướng, độc lập)")]
    public bool useDirectionalAnchorsCannon = true;
    public Transform cannonAnchorRight;
    public Transform cannonAnchorLeft;
    public Transform cannonAnchorUp;
    public Transform cannonAnchorDown;

    [Header("Flip Aware")]
    public bool flipAwareOffsets = true;
    public SpriteRenderer flipSource;

    [Header("Cannon (Stand & Shoot 4-dir)")]
    public float cannonCastDuration = 0.25f;
    public float postCannonHold = 0.08f;
    public int cannonShots = 4;
    public float cannonFireInterval = 0.14f;
    public float cannonBulletSpeed = 9.0f;
    public float cannonBulletLifetime = 2.5f;
    public LayerMask cannonHitObstacleLayers;

    [Header("Cannon Prefabs")]
    public GameObject cannonBulletPrefab;
    public GameObject cannonExplosionPrefab;
    public GameObject cannonMuzzleFlashPrefab;

    // =========================
    //   STATE / INTERNALS
    // =========================
    [Header("Debug/State")]
    public BossState state = BossState.Dormant;
    public bool autoFindPlayerByTag = true;

    private Coroutine freeBrainLoop;
    private Vector3 startPos;
    private float tHover;
    private ContactFilter2D obstacleFilter;
    private float nextDashAllowedAt = 0f;
    private float nextRocketAllowedAt = 0f;
    private float nextCannonAllowedAt = 0f;

    // Runtime punch hitbox refs
    private GameObject runtimeHitboxGO;
    private PunchHitboxRuntime runtimeHitbox;

    // =========================
    //   UNITY CORE
    // =========================
    void Reset() { rb = GetComponent<Rigidbody2D>(); hitCollider = GetComponent<Collider2D>(); }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!hitCollider) hitCollider = GetComponent<Collider2D>();

        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;

        obstacleFilter = new ContactFilter2D { useLayerMask = true, layerMask = obstacleLayers, useTriggers = false };
        startPos = transform.position;
    }

    void Start()
    {
        if (autoFindPlayerByTag && player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }

        nextDashAllowedAt = Time.time + Random.Range(dashCooldownRange.x, dashCooldownRange.y);
        nextRocketAllowedAt = Time.time + Random.Range(rocketCooldownRange.x, rocketCooldownRange.y);
        nextCannonAllowedAt = Time.time + Random.Range(cannonCooldownRange.x, cannonCooldownRange.y);
    }

    void Update()
    {
        if (facePlayer && player != null)
        {
            Vector2 dir = (player.position - transform.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            rb.SetRotation(ang);
        }

        if (hoverIdle && !enableFreeBrain && currentAction == ActionFlag.None)
        {
            tHover += Time.deltaTime;
            Vector3 off = new Vector3(
                Mathf.Sin(tHover * hoverFrequency.x) * hoverAmplitude.x,
                Mathf.Cos(tHover * hoverFrequency.y) * hoverAmplitude.y,
                0f
            );
            rb.MovePosition(startPos + off);
        }
    }

    // =========================
    //   PUBLIC CONTROL API
    // =========================
    public void StartFreeBrain()
    {
        enableFreeBrain = true;
        if (freeBrainLoop != null) StopCoroutine(freeBrainLoop);
        freeBrainLoop = StartCoroutine(FreeBrainLoop());
    }
    public void StopFreeBrain()
    {
        enableFreeBrain = false;
        if (freeBrainLoop != null) StopCoroutine(freeBrainLoop);
        freeBrainLoop = null;
        ClearAllActions();
    }

    // GỌI HÀM NÀY KHI BOSS CHẾT
    public void Die()
    {
        if (state == BossState.Dead) return;
        state = BossState.Dead;

        // Ngừng AI + hành động hiện tại
        StopAllCoroutines();
        StopFreeBrain();
        ClearAllActions();

        // Đóng băng Boss
        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // Hủy hitbox đang tồn tại
        DestroyRuntimePunchHitbox();

        // Dọn sạch đạn/effects cả Boss lẫn Player
        CleanupAllProjectilesAndFX();

        // (Nếu cần: phát SFX chết, animation... để nơi khác tự lo)
        Debug.Log("☠ Boss died → cleaned projectiles/FX and stopped AI.");
    }

    // =========================
    //   MAIN BRAIN LOOP
    // =========================
    IEnumerator FreeBrainLoop()
    {
        while (enableFreeBrain && state != BossState.Dead)
        {
            if (!player)
            {
                ClearAllActions();
                yield return null;
                continue;
            }

            Vector2 toPl = (player.position - transform.position);
            float dist = toPl.magnitude;

            Vector2 dir = Vector2.zero;
            int dirIndex = 0; // 0=Down,1=Up,2=Left,3=Right
            if (Mathf.Abs(toPl.x) > Mathf.Abs(toPl.y)) { dir.x = Mathf.Sign(toPl.x); dirIndex = (dir.x < 0f) ? 2 : 3; }
            else { dir.y = Mathf.Sign(toPl.y); dirIndex = (dir.y < 0f) ? 0 : 1; }

            int shootDirIdx = (shootDirectionOverride == ShootDirOverride.None) ? dirIndex : (int)shootDirectionOverride;
            SetAnimDir(dirIndex);

            if (currentAction != ActionFlag.None)
            {
                yield return null;
                continue;
            }

            if (dist > stopDistance)
            {
                SetExclusiveMoving(true);

                bool canPunch = Time.time >= nextDashAllowedAt;
                bool canRocket = Time.time >= nextRocketAllowedAt;
                bool canCannon = Time.time >= nextCannonAllowedAt;

                bool wantsPunch = canPunch && (Random.value < dashChancePerSecond * Time.deltaTime);
                bool wantsRocket = canRocket && (Random.value < rocketChancePerSecond * Time.deltaTime);
                bool wantsCannon = canCannon && (Random.value < cannonChancePerSecond * Time.deltaTime);

                if (wantsPunch || wantsRocket || wantsCannon)
                {
                    float wP = wantsPunch ? Mathf.Max(0.0001f, punchWeight) : 0f;
                    float wR = wantsRocket ? Mathf.Max(0.0001f, rocketWeight) : 0f;
                    float wC = wantsCannon ? Mathf.Max(0.0001f, cannonWeight) : 0f;
                    float sum = wP + wR + wC;

                    string chosen;
                    float roll = Random.value * sum;
                    if (roll < wR) chosen = "rocket";
                    else if (roll < wR + wC) chosen = "cannon";
                    else chosen = "punch";

                    if (chosen == "rocket")
                    {
                        yield return StartCoroutine(RocketRoutine(shootDirIdx));
                        nextRocketAllowedAt = Time.time + Random.Range(rocketCooldownRange.x, rocketCooldownRange.y);
                    }
                    else if (chosen == "cannon")
                    {
                        yield return StartCoroutine(CannonRoutine(shootDirIdx));
                        nextCannonAllowedAt = Time.time + Random.Range(cannonCooldownRange.x, cannonCooldownRange.y);
                    }
                    else
                    {
                        yield return StartCoroutine(DoTwoPhasePunchDash(dirIndex, dir.normalized));
                        nextDashAllowedAt = Time.time + Random.Range(dashCooldownRange.x, dashCooldownRange.y);
                    }
                }
                else
                {
                    float speed = Mathf.Max(0f, baseMoveSpeed) * Mathf.Max(0f, speedMultiplier);
                    CastAndMove(dir.normalized, speed, Time.deltaTime);
                }
            }
            else
            {
                SetExclusiveMoving(false);
                yield return new WaitForSeconds(0.05f);
            }

            yield return null;
        }
    }

    // =========================
    //   2-PHASE PUNCH (PREPARE → PUNCH) + AUTO-HIT + RUNTIME HITBOX
    // =========================
    IEnumerator DoTwoPhasePunchDash(int dirIndexStart, Vector2 dirNStart)
    {
        SetExclusiveMoving(false);

        // --- PREPARE ---
        EnterPrepare(dirIndexStart);
        for (int i = 0; i < Mathf.Max(0, safetyFrames); i++) yield return null;

        float hold = Mathf.Max(minPrepareHold, preCastDuration);
        float t = 0f;
        while (t < hold) { t += Time.deltaTime; yield return null; }
        ExitPrepare();

        // --- Delay trước khi Punch ---
        if (postCastDelay > 0f) yield return new WaitForSeconds(postCastDelay);

        // --- PUNCH ---
        EnterPunch(dirIndexStart);
        for (int i = 0; i < Mathf.Max(0, safetyFrames); i++) yield return null;

        t = 0f;
        bool hasDamaged = false;

        // Tạo hitbox runtime nếu bật
        if (useRuntimePunchHitbox)
        {
            Vector2 c0, s0; ComputePunchHitbox(dirNStart, out c0, out s0);
            SpawnOrUpdateRuntimePunchHitbox(c0, s0);
            runtimeHitbox?.Arm(playerLayer, playerTag, punchDamage, sfx, transform, this);
        }

        while (t < punchActiveDuration)
        {
            float dt = Time.deltaTime;
            t += dt;

            // (1) Auto-hit “rất gần”
            if (!hasDamaged && autoHitWhenVeryClose && CheckCloseContactAutoHit())
            {
                hasDamaged = true;
            }

            // (2) Cập nhật hướng, dash và hitbox runtime bám theo
            Vector2 dirN = dirNStart;
            int dirIdx = dirIndexStart;
            if (player)
            {
                Vector2 toPl = (player.position - transform.position);
                if (Mathf.Abs(toPl.x) > Mathf.Abs(toPl.y))
                {
                    dirN = new Vector2(Mathf.Sign(toPl.x), 0f);
                    dirIdx = (dirN.x < 0f) ? 2 : 3;
                }
                else
                {
                    dirN = new Vector2(0f, Mathf.Sign(toPl.y));
                    dirIdx = (dirN.y < 0f) ? 0 : 1;
                }
                SetAnimDir(dirIdx);
            }

            bool movedFull = CastAndMove(dirN.normalized, punchDashSpeed, dt);
            if (!movedFull && stopPunchOnObstacle) break;

            // (3) Cập nhật vị trí/size hitbox runtime
            if (useRuntimePunchHitbox)
            {
                Vector2 cc, ss; ComputePunchHitbox(dirN, out cc, out ss);
                SpawnOrUpdateRuntimePunchHitbox(cc, ss);
            }
            else
            {
                // (4) Fallback OverlapBox
                if (!hasDamaged && CheckPunchHitAndDamage_StrictFront(dirN))
                    hasDamaged = true;
            }

            yield return null;
        }

        if (postPunchExtraHold > 0f) yield return new WaitForSeconds(postPunchExtraHold);

        // Hủy hitbox runtime
        DestroyRuntimePunchHitbox();

        ExitPunch();
    }

    // Tính center/size hitbox punch theo hướng
    void ComputePunchHitbox(Vector2 dirN, out Vector2 center, out Vector2 size)
    {
        bool horizontal = Mathf.Abs(dirN.x) > Mathf.Abs(dirN.y);
        size = horizontal ? punchHitboxSizeHorizontal : punchHitboxSizeVertical;
        if (runtimeHitboxPadding > 0f) size += Vector2.one * (runtimeHitboxPadding * 2f);

        Vector2 half = size * 0.5f;

        Vector2 bossCenter =
            hitCollider ? (Vector2)hitCollider.bounds.center
                        : (rb ? rb.position : (Vector2)transform.position);

        center = bossCenter;
        if (horizontal)
            center += new Vector2(Mathf.Sign(dirN.x) * (punchReach + half.x), 0f);
        else
            center += new Vector2(0f, Mathf.Sign(dirN.y) * (punchReach + half.y));
    }

    void SpawnOrUpdateRuntimePunchHitbox(Vector2 center, Vector2 size)
    {
        if (!useRuntimePunchHitbox) return;

        if (runtimeHitboxGO == null)
        {
            runtimeHitboxGO = new GameObject("BossPunchHitbox_Runtime");
            runtimeHitboxGO.layer = runtimeHitboxLayer;
            runtimeHitboxGO.transform.SetParent(null); // không parent vào boss để tránh flip/scale ảnh hưởng
            var col = runtimeHitboxGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            runtimeHitbox = runtimeHitboxGO.AddComponent<PunchHitboxRuntime>();
        }

        runtimeHitboxGO.transform.position = center;
        var box = runtimeHitboxGO.GetComponent<BoxCollider2D>();
        if (box) box.size = size;
    }

    void DestroyRuntimePunchHitbox()
    {
        if (runtimeHitboxGO != null)
        {
            Destroy(runtimeHitboxGO);
            runtimeHitboxGO = null;
            runtimeHitbox = null;
        }
    }

    // =========================
    //   ROCKET / CANNON
    // =========================

    // Chọn ngẫu nhiên một điểm rơi quanh Player (fallback: quanh Boss nếu chưa tìm thấy player)
    Vector2 PickRandomLandingPointAroundPlayer()
    {
        Transform target = player ? player : transform;

        float minR = Mathf.Max(0.1f, landingRadiusRange.x);
        float maxR = Mathf.Max(minR, landingRadiusRange.y);

        float r = Random.Range(minR, maxR);
        float ang = Random.Range(0f, Mathf.PI * 2f);

        Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
        return (Vector2)target.position + offset;
    }

    IEnumerator RocketRoutine(int shootDirIdx)
    {
        SetExclusiveMoving(false);
        EnterRocket(shootDirIdx);
        for (int i = 0; i < Mathf.Max(0, safetyFrames); i++) yield return null;

        if (sfx) sfx.Play2D(sfx.rocketEnter);
        yield return new WaitForSeconds(Mathf.Max(0.05f, rocketCastDuration));

        int rocketCount = Random.Range(rocketCountRange.x, rocketCountRange.y + 1);
        for (int i = 0; i < rocketCount; i++)
        {
            Vector2 muzzlePos = GetMuzzleWorldPos(shootDirIdx, isCannon: false);
            StartCoroutine(LaunchVfxAtMuzzle(muzzlePos));
            if (sfx) sfx.Play3D(sfx.rocketFire, muzzlePos);
            StartCoroutine(HandleSingleRocketStrike(PickRandomLandingPointAroundPlayer()));
            if (i < rocketCount - 1) yield return new WaitForSeconds(rocketFireInterval);
        }

        if (postRocketHold > 0f) yield return new WaitForSeconds(postRocketHold);
        ExitRocket();
    }

    IEnumerator LaunchVfxAtMuzzle(Vector2 muzzlePosWorld)
    {
        if (muzzleSmokePrefab)
            Destroy(Instantiate(muzzleSmokePrefab, muzzlePosWorld, Quaternion.identity), 2f);

        if (risingMissilePrefab)
        {
            var rising = Instantiate(risingMissilePrefab, muzzlePosWorld, Quaternion.identity);
            Vector3 start = muzzlePosWorld;
            Vector3 end = muzzlePosWorld + Vector2.up * Mathf.Max(0.01f, riseDistance);

            float remain = Vector3.Distance(start, end);
            while (remain > 0.001f && rising)
            {
                float step = riseSpeed * Time.deltaTime;
                rising.transform.position = Vector3.MoveTowards(rising.transform.position, end, step);
                remain = Vector3.Distance(rising.transform.position, end);
                yield return null;
            }
            if (rising) Destroy(rising);
        }
    }

    IEnumerator HandleSingleRocketStrike(Vector2 landingPos)
    {
        if (warningPrefab)
        {
            var warn = Instantiate(warningPrefab, landingPos, Quaternion.identity);
            Destroy(warn, Mathf.Max(0.5f, warningTime + fallDuration + 1f));
        }

        yield return new WaitForSeconds(warningTime);

        if (fallingMissilePrefab)
        {
            Vector3 startPos = new Vector3(landingPos.x, landingPos.y + fallStartHeight, 0f);
            var missile = Instantiate(fallingMissilePrefab, startPos, Quaternion.identity);

            float t = 0f;
            while (t < fallDuration && missile)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fallDuration);
                missile.transform.position = Vector3.Lerp(startPos, new Vector3(landingPos.x, landingPos.y, 0f), a);
                yield return null;
            }
            if (missile) Destroy(missile);
        }
        else
        {
            yield return new WaitForSeconds(fallDuration);
        }

        if (explosionPrefab)
        {
            Destroy(Instantiate(explosionPrefab, landingPos, Quaternion.identity), 2.0f);
            if (sfx) sfx.Play3D(sfx.explosion, landingPos);
            if (camFx) camFx.ShakeFollowSafe();
        }
    }

    IEnumerator CannonRoutine(int shootDirIdx)
    {
        SetExclusiveMoving(false);
        EnterCannon(shootDirIdx);
        for (int i = 0; i < Mathf.Max(0, safetyFrames); i++) yield return null;

        if (sfx) sfx.Play2D(sfx.cannonEnter);
        yield return new WaitForSeconds(Mathf.Max(0.02f, cannonCastDuration));

        for (int i = 0; i < Mathf.Max(1, cannonShots); i++)
        {
            Vector2 muzzlePos = GetMuzzleWorldPos(shootDirIdx, isCannon: true);

            if (cannonMuzzleFlashPrefab)
                Destroy(Instantiate(cannonMuzzleFlashPrefab, muzzlePos, Quaternion.identity), 1.5f);
            if (muzzleSmokePrefab)
                Destroy(Instantiate(muzzleSmokePrefab, muzzlePos, Quaternion.identity), 2f);

            if (sfx) sfx.Play3D(sfx.cannonFire, muzzlePos);

            Vector2 dir = DirIndexToVector(shootDirIdx);
            StartCoroutine(HandleSingleCannonBullet(muzzlePos, dir));

            if (i < cannonShots - 1) yield return new WaitForSeconds(Mathf.Max(0f, cannonFireInterval));
        }

        if (postCannonHold > 0f) yield return new WaitForSeconds(postCannonHold);
        ExitCannon();
    }

    IEnumerator HandleSingleCannonBullet(Vector2 startPos, Vector2 dirN)
    {
        if (cannonBulletPrefab == null) yield break;

        var bullet = Instantiate(cannonBulletPrefab, startPos, Quaternion.identity);
        float speed = Mathf.Max(0.01f, cannonBulletSpeed);
        float life = Mathf.Max(0.01f, cannonBulletLifetime);

        float t = 0f;
        Vector2 pos = startPos;

        LayerMask hitMask = cannonHitObstacleLayers;
        if (playerLayer.value != 0) hitMask |= playerLayer;

        while (t < life && bullet)
        {
            float dt = Time.deltaTime; t += dt;
            float dist = speed * dt;
            Vector2 next = pos + dirN * dist;

            RaycastHit2D hit = Physics2D.Raycast(pos, dirN, dist, hitMask);
            if (hit.collider != null)
            {
                Vector2 p = hit.point;
                if (cannonExplosionPrefab)
                    Destroy(Instantiate(cannonExplosionPrefab, p, Quaternion.identity), 2.0f);
                if (sfx) sfx.Play3D(sfx.explosion, p);
                if (camFx) camFx.ShakeFollowSafe();
                Destroy(bullet);
                yield break;
            }

            bullet.transform.position = next;
            pos = next;
            yield return null;
        }

        if (bullet) Destroy(bullet);
    }

    Vector2 DirIndexToVector(int dirIdx)
    {
        switch (dirIdx)
        {
            case 1: return Vector2.up;
            case 2: return Vector2.left;
            case 3: return Vector2.right;
            default: return Vector2.down;
        }
    }

    // =========================
    //   MUZZLE HELPERS
    // =========================
    Transform GetRocketAnchorByDir(int dirIdx)
    {
        switch (dirIdx)
        {
            case 3: return rocketAnchorRight;
            case 2: return rocketAnchorLeft;
            case 1: return rocketAnchorUp;
            default: return rocketAnchorDown;
        }
    }
    Transform GetCannonAnchorByDir(int dirIdx)
    {
        switch (dirIdx)
        {
            case 3: return cannonAnchorRight;
            case 2: return cannonAnchorLeft;
            case 1: return cannonAnchorUp;
            default: return cannonAnchorDown;
        }
    }

    int FlipSignX()
    {
        if (!flipAwareOffsets) return 1;
        if (flipSource != null) return flipSource.flipX ? -1 : 1;
        var t = (move1Animator ? move1Animator.transform : transform);
        return (t.lossyScale.x < 0f) ? -1 : 1;
    }

    Vector2 GetMuzzleWorldPos(int shootDirIdx, bool isCannon)
    {
        Vector2 nudge = muzzleNudgeGlobal;
        switch (shootDirIdx)
        {
            case 3: nudge += muzzleNudgeRight; break;
            case 2: nudge += muzzleNudgeLeft; break;
            case 1: nudge += muzzleNudgeUp; break;
            default: nudge += muzzleNudgeDown; break;
        }
        nudge.x *= FlipSignX();

        if (isCannon && useDirectionalAnchorsCannon)
        {
            var a = GetCannonAnchorByDir(shootDirIdx);
            if (a) return useLocalMuzzleOffset ? (Vector2)a.TransformPoint((Vector3)nudge)
                                               : (Vector2)a.position + nudge;
        }
        else if (!isCannon && useDirectionalAnchorsRocket)
        {
            var a = GetRocketAnchorByDir(shootDirIdx);
            if (a) return useLocalMuzzleOffset ? (Vector2)a.TransformPoint((Vector3)nudge)
                                               : (Vector2)a.position + nudge;
        }

        if (muzzleOverride)
            return useLocalMuzzleOffset ? (Vector2)muzzleOverride.TransformPoint((Vector3)nudge)
                                        : (Vector2)muzzleOverride.position + nudge;

        return (Vector2)transform.position + nudge;
    }

    // =========================
    //   MOTION / COLLISION
    // =========================
    bool CastAndMove(Vector2 dir, float speed, float dt)
    {
        if (dir.sqrMagnitude < 1e-6f || speed <= 0f || dt <= 0f) return true;

        float distance = speed * dt;
        if (!hitCollider) { rb.MovePosition(rb.position + dir * distance); return true; }

        var results = new RaycastHit2D[8];
        int hitCount = hitCollider.Cast(dir, obstacleFilter, results, distance + skinWidth);

        if (hitCount > 0)
        {
            float minDist = distance;
            for (int i = 0; i < hitCount; i++)
            { if (results[i].collider == null) continue; minDist = Mathf.Min(minDist, results[i].distance - skinWidth); }
            if (minDist < 0f) minDist = 0f;

            rb.MovePosition(rb.position + dir * minDist);
            return false;
        }
        else
        {
            rb.MovePosition(rb.position + dir * distance);
            return true;
        }
    }

    // =========================
    //   PUNCH HIT CHECK (OverlapBox fallback)
    // =========================
    bool CheckPunchHitAndDamage_StrictFront(Vector2 dirN)
    {
        bool horizontal = Mathf.Abs(dirN.x) > Mathf.Abs(dirN.y);
        Vector2 size = horizontal ? punchHitboxSizeHorizontal : punchHitboxSizeVertical;
        Vector2 half = size * 0.5f;

        Vector2 bossCenter =
            hitCollider ? (Vector2)hitCollider.bounds.center
                        : (rb ? rb.position : (Vector2)transform.position);

        Vector2 center = bossCenter;
        if (horizontal)
            center += new Vector2(Mathf.Sign(dirN.x) * (punchReach + half.x), 0f);
        else
            center += new Vector2(0f, Mathf.Sign(dirN.y) * (punchReach + half.y));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody == rb) continue;

            if (playerLayer.value != 0)
            {
                if (((1 << col.gameObject.layer) & playerLayer.value) == 0) continue;
            }
            else
            {
                if (!string.IsNullOrEmpty(playerTag))
                {
                    if (!col.CompareTag(playerTag) && !col.transform.root.CompareTag(playerTag))
                        continue;
                }
            }

            Health health =
                col.GetComponent<Health>() ??
                col.GetComponentInParent<Health>() ??
                col.GetComponentInChildren<Health>();

            if (health != null)
            {
                health.TakeDamage(punchDamage);
                if (sfx) sfx.Play3D(sfx.punchHit, col.transform.position);
                return true;
            }
        }

        return false;
    }

    // =========================
    //   AUTO-HIT (cực gần)
    // =========================
    bool CheckCloseContactAutoHit()
    {
        Vector2 bossCenter =
            hitCollider ? (Vector2)hitCollider.bounds.center
                        : (rb ? rb.position : (Vector2)transform.position);

        float r = Mathf.Max(0.05f, closeAutoHitRadius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(bossCenter, r);

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody == rb) continue;

            if (playerLayer.value != 0)
            {
                if (((1 << col.gameObject.layer) & playerLayer.value) == 0) continue;
            }
            else if (!string.IsNullOrEmpty(playerTag))
            {
                if (!col.CompareTag(playerTag) && !col.transform.root.CompareTag(playerTag))
                    continue;
            }

            Health health =
                col.GetComponent<Health>() ??
                col.GetComponentInParent<Health>() ??
                col.GetComponentInChildren<Health>();

            if (health != null)
            {
                health.TakeDamage(punchDamage);
                if (sfx) sfx.Play3D(sfx.punchHit, col.transform.position);
                return true;
            }
        }
        return false;
    }

    // =========================
    //   ANIM HELPERS (EXCLUSIVE)
    // =========================
    Animator ActiveAnimator => move1Animator != null ? move1Animator : bodyAnimator;

    void SetAnimDir(int dirIdx) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramDir)) a.SetInteger(paramDir, dirIdx); }
    void SetAnimMoving(bool v) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramIsMoving)) a.SetBool(paramIsMoving, v); }
    void SetAnimPreparing(bool v) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramIsPreparing)) a.SetBool(paramIsPreparing, v); }
    void SetAnimPunching(bool v) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramIsPunching)) a.SetBool(paramIsPunching, v); }
    void SetAnimRocketing(bool v) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramIsRocketing)) a.SetBool(paramIsRocketing, v); }
    void SetAnimCannoning(bool v) { var a = ActiveAnimator; if (a && !string.IsNullOrEmpty(paramIsCannoning)) a.SetBool(paramIsCannoning, v); }

    void ClearAllActionBools()
    {
        SetAnimPreparing(false);
        SetAnimPunching(false);
        SetAnimRocketing(false);
        SetAnimCannoning(false);
    }

    void SetExclusiveMoving(bool moving)
    {
        if (moving)
        {
            currentAction = ActionFlag.None;
            ClearAllActionBools();
            SetAnimMoving(true);
        }
        else
        {
            SetAnimMoving(false);
        }
    }

    void EnterPrepare(int dirIdx)
    {
        currentAction = ActionFlag.Prepare;
        SetAnimMoving(false);
        ClearAllActionBools();
        SetAnimDir(dirIdx);
        SetAnimPreparing(true);
    }
    void ExitPrepare()
    {
        SetAnimPreparing(false);
    }

    void EnterPunch(int dirIdx)
    {
        currentAction = ActionFlag.Punch;
        SetAnimMoving(false);
        ClearAllActionBools();
        SetAnimDir(dirIdx);
        SetAnimPunching(true);
    }
    void ExitPunch()
    {
        SetAnimPunching(false);
        currentAction = ActionFlag.None;
    }

    void EnterRocket(int dirIdx)
    {
        currentAction = ActionFlag.Rocket;
        SetAnimMoving(false);
        ClearAllActionBools();
        SetAnimDir(dirIdx);
        SetAnimRocketing(true);
    }
    void ExitRocket()
    {
        SetAnimRocketing(false);
        currentAction = ActionFlag.None;
    }

    void EnterCannon(int dirIdx)
    {
        currentAction = ActionFlag.Cannon;
        SetAnimMoving(false);
        ClearAllActionBools();
        SetAnimDir(dirIdx);
        SetAnimCannoning(true);
    }
    void ExitCannon()
    {
        SetAnimCannoning(false);
        currentAction = ActionFlag.None;
    }

    void ClearAllActions()
    {
        currentAction = ActionFlag.None;
        SetAnimMoving(false);
        ClearAllActionBools();
        DestroyRuntimePunchHitbox();
    }

    // =========================
    //   CLEANUP SAU KHI BOSS CHẾT
    // =========================
    public void CleanupAllProjectilesAndFX()
    {
        Debug.Log("🧹 Boss died → clearing all projectiles and FX...");

        // Xoá đạn Boss
        var bossBullets = GameObject.FindGameObjectsWithTag("EnemyProjectile");
        foreach (var b in bossBullets) Destroy(b);

        // Xoá đạn Player
        var playerBullets = GameObject.FindGameObjectsWithTag("PlayerProjectile");
        foreach (var b in playerBullets) Destroy(b);

        // Xoá hiệu ứng Boss (nếu đã set tag)
        var bossEffects = GameObject.FindGameObjectsWithTag("BossFX");
        foreach (var e in bossEffects) Destroy(e);

        // Tắt các spawner/launcher còn chạy
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            var n = mb.GetType().Name;
            if (n.Contains("Spawner") || n.Contains("Shockwave") || n.Contains("Shoot") || n.Contains("Launch"))
                mb.enabled = false;
        }

        // Tắt audio FX kéo dài
        foreach (var audio in FindObjectsOfType<AudioSource>())
        {
            if (audio && audio.isPlaying && audio.gameObject.name.Contains("Boss"))
                audio.Stop();
        }
    }

    // =========================
    //   GIZMOS
    // =========================
    void OnDrawGizmosSelected()
    {
        // Punch preview (math)
        Vector2 dir = Vector2.down;
        if (Application.isPlaying && player != null)
        {
            Vector2 toPl = (player.position - transform.position);
            dir = (Mathf.Abs(toPl.x) > Mathf.Abs(toPl.y))
                ? new Vector2(Mathf.Sign(toPl.x), 0f)
                : new Vector2(0f, Mathf.Sign(toPl.y));
        }
        bool horizontal = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        Vector2 size = horizontal ? punchHitboxSizeHorizontal : punchHitboxSizeVertical;
        Vector2 half = size * 0.5f;
        Vector2 center = (Vector2)transform.position + (horizontal
            ? new Vector2(Mathf.Sign(dir.x) * (punchReach + half.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y) * (punchReach + half.y)));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);

        // Close-contact auto-hit radius
        if (closeAutoHitRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Vector3 c = hitCollider ? hitCollider.bounds.center : transform.position;
            Gizmos.DrawWireSphere(c, closeAutoHitRadius);
        }
    }
}

/// <summary>
/// Runtime punch hitbox: tự gây damage một lần khi va chạm Player trong thời gian tồn tại.
/// </summary>
public class PunchHitboxRuntime : MonoBehaviour
{
    private int damage;
    private LayerMask targetLayer;
    private string targetTag;
    private bool alreadyHit;
    private SFXLibrary sfx;
    private Transform ownerRoot;
    private BossController ownerBoss;

    public void Arm(LayerMask playerLayer, string playerTag, int dmg, SFXLibrary sfxLib, Transform owner, BossController boss)
    {
        targetLayer = playerLayer;
        targetTag = playerTag;
        damage = dmg;
        sfx = sfxLib;
        ownerRoot = owner;
        ownerBoss = boss;
        alreadyHit = false;
    }

    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnTriggerStay2D(Collider2D other) { TryHit(other); }

    void TryHit(Collider2D col)
    {
        if (alreadyHit || col == null) return;

        if (ownerRoot && (col.transform == ownerRoot || col.transform.IsChildOf(ownerRoot))) return;

        if (targetLayer.value != 0)
        {
            if (((1 << col.gameObject.layer) & targetLayer.value) == 0) return;
        }
        else if (!string.IsNullOrEmpty(targetTag))
        {
            if (!col.CompareTag(targetTag) && !col.transform.root.CompareTag(targetTag)) return;
        }

        Health h = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>() ?? col.GetComponentInChildren<Health>();
        if (h != null)
        {
            h.TakeDamage(damage);
            if (sfx) sfx.Play3D(sfx.punchHit, col.transform.position);
            alreadyHit = true;
        }
    }
}
