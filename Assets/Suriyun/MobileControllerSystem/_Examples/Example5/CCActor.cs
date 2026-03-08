using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Terresquall;
using System;

public enum PlayerStatType
{
    MoveSpeed,
    StaminaDrainRate,
    HungerDrainRate,
    MaxHealth,
    AttackDamage,
    CompnionLimit,
}

[System.Serializable]
public class PlayerStat
{
    public PlayerStatType type;

    public float baseValue;
    public float perLevelIncrease;
    public int level;
    public int maxLevel = 10;

    public float CurrentValue =>
        baseValue + level * perLevelIncrease;

    public float NextValue =>
        level < maxLevel
            ? baseValue + (level + 1) * perLevelIncrease
            : CurrentValue;

    public bool CanUpgrade => level < maxLevel;

    public void Upgrade()
    {
        if (CanUpgrade)
            level++;
    }
}



#region CCActor WITH FSM

[RequireComponent(typeof(CharacterController))]
public class CCActor : MonoBehaviour
{
    public StateMachine StateMachine { get; private set; }

    [Header("Input System")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference attackAction;

    [Header("Movement Settings")]
    [Tooltip("Speed when walking (joystick below run threshold)")]
    public float walkSpeed = 3f;
    [Tooltip("Speed when running (joystick beyond run threshold)")]
    public float runSpeed = 6f;
    [Tooltip("Joystick magnitude threshold to switch to run")]
    [Range(0f, 1f)] public float runThreshold = 0.7f;
    [Tooltip("Degrees per second when rotating")]
    public float rotationSpeed = 720f;

    [Header("⚙️ Attack Behavior")]
    [Tooltip("Disable auto-rotation towards target while attacking.")]
    public bool disableAttackRotation = false;

    [Header("Jump Settings")]
    public float gravity = -9.81f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;

    [Header("Attack Facing")]
    [Tooltip("If true, player must face the target (within a cone) to attack.")]
    public bool requireFacing = true;

    [Tooltip("Half-angle of the facing cone in degrees (e.g., 70 = 140° total).")]
    [Range(0f, 180f)] public float requiredFacingAngle = 70f;

    [Tooltip("Forward offset for the melee hit sphere from the player.")]
    public float hitForwardOffset = 1.0f; // replaces the hardcoded 1.0f


    private Vector3 currentVelocity;  // New: for smooth acceleration
    [Header("Advanced Movement")]
    public float acceleration = 10f;  // New: tweak this for snappier or slower response

    [Header("Auto Attack Settings")]
    public float autoAttackRadius = 2f;
    public float autoAttackCooldown = 2f;
    public LayerMask enemyLayer;

    [Header("Attack Sector (Runtime)")]
    public LineRenderer attackSector;
    public bool showAttackSectorRuntime = true;
    public int sectorSegments = 28;
    public Color sectorColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    public float sectorYOffset = 0.05f;

    [Header("Attack Target Arrow")]
    public GameObject targetArrowPrefab;
    public bool showAttackArrow = true;
    public float arrowHeight = 1.2f;
    public float arrowTravelCurve = 1.0f;

    private GameObject _activeArrow;
    private float _arrowT;

    [HideInInspector] public float autoAttackTimer;
    [HideInInspector] public float attackTimer;

    // Core components
    [HideInInspector] public CharacterController controller;
    [HideInInspector] public CuteAnimalAnimHandler animHandler;

    // Input & state
    [HideInInspector] public Vector2 inputVec;
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public float verticalVelocity;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool isRunning;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;        // set in Inspector (Terrain, Default, etc.)
    public float groundCheckRadius = 0.25f;  // ~ controller.radius * 0.8f
    public float groundCheckDistance = 0.6f; // ray length below feet
    public float groundSnapDistance = 0.3f;  // snap if we’re hovering within this range
    public float stickToGroundForce = 5f;    // keeps us glued when grounded
    public float slopeSlideSpeed = 4f;       // slide down steep slopes

    [Header("Controller Tweaks")]
    public float slopeLimitDeg = 40f;        // gentler than default 45–60
    public float maxStepOffset = 0.25f;      // top speed stepping
    public float runStepOffset = 0.05f;      // nearly zero when sprinting


    [Header("Stamina")]
    [Tooltip("Max stamina value.")]
    public float maxStamina = 100f;

    [Tooltip("Stamina drain per second while sprinting.")]
    public float runDrainPerSecond = 25f;

    [Tooltip("Stamina regen per second while walking (not sprinting).")]
    public float walkRegenPerSecond = 10f;

    [Tooltip("Stamina regen per second while idle (no input).")]
    public float idleRegenPerSecond = 16f;

    [Tooltip("Need at least this much stamina to START sprinting (prevents stutter at 0).")]
    public float minToStartRunning = 12f;

    [Tooltip("Delay before stamina starts regenerating after you stop sprinting.")]
    public float regenDelayAfterSprint = 0.5f;

    [HideInInspector] public float stamina;   // current stamina
    private float _regenResumeTime;

    public float Stamina01 => (maxStamina <= 0f) ? 0f : Mathf.Clamp01(stamina / maxStamina);

    [Header("Hunger")]
    [Tooltip("Max hunger value. 0 means disabled.")]
    public float maxHunger = 100f;

    [Tooltip("Hunger drain per second (always-on, unless paused).")]
    public float hungerDrainPerSecond = 2f;

    [Tooltip("HP damage per second while hunger is at 0.")]
    public float starvationDamagePerSecond = 5f;

    [Tooltip("Optional extra hunger drain while sprinting.")]
    public float sprintBonusHungerDrain = 0f;

    [Tooltip("Pause hunger drain (e.g., cutscenes).")]
    public bool pauseHungerDrain = false;

    [Header("Upgradeable Stats")]
    public List<PlayerStat> stats = new();

    [HideInInspector] public float hunger;   // current hunger (0..maxHunger)
    public float Hunger01 => (maxHunger <= 0f) ? 0f : Mathf.Clamp01(hunger / maxHunger);

    // Internals
    private float _starveDamageAccum; // accumulates fractional starvation damage
    [HideInInspector] public Health health;  // player health reference

    public bool isInParabola = false;

    public int companionLimit = 1;



    private float defaultStepOffset;

    private VirtualJoystick virtualJoystick;

    [Header("New Auto-Attack Loop")]
    public float attackRange = 1.6f;      // detection to keep attacking
    public int attackDamage = 10;         // per hit
    public float attackHitRadius = 1.0f;  // overlap radius in front of player

    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public bool isAttackingLoop; // true while enemies are in range

    struct GroundInfo { public bool grounded; public Vector3 normal; public float angleDeg; }
    GroundInfo _ground;

    [Header("Progression")]
    public int playerLevel = 1;
    public int currentXP = 0;
    public int baseXPToNextLevel = 50;
    public float xpGrowthMultiplier = 1.25f;
    public float levelScaleIncrease = 0.2f;
    public ParticleSystem levelupvfx;

    [Header("Economy")]
    public int storedMeat = 0;

    public event Action OnProgressChanged;


    public int XPToNextLevel
    {
        get
        {
            return Mathf.RoundToInt(baseXPToNextLevel * Mathf.Pow(xpGrowthMultiplier, playerLevel - 1));
        }
    }

    public float XPProgress01
    {
        get
        {
            int needed = XPToNextLevel;
            if (needed <= 0) return 0f;
            return Mathf.Clamp01((float)currentXP / needed);
        }
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;

        while (currentXP >= XPToNextLevel)
        {
            currentXP -= XPToNextLevel;
            LevelUp();
        }

        NotifyProgressChanged();
    }

    public void AddMeat(int amount)
    {
        if (amount <= 0) return;

        storedMeat += amount;
        NotifyProgressChanged();
    }

    public bool TrySpendMeat(int amount)
    {
        if (amount <= 0) return true;
        if (storedMeat < amount) return false;

        storedMeat -= amount;
        NotifyProgressChanged();
        return true;
    }

    private void LevelUp()
    {
        playerLevel++;

        Vector3 s = transform.localScale;
        s += Vector3.one * levelScaleIncrease;
        transform.localScale = s;

        if(levelupvfx != null) 
        {
            levelupvfx.Play();
        }

        NotifyProgressChanged();
    }

    public bool TryBuyAndSpawnCompanion(GameObject companionPrefab, int meatCost = 50)
    {
        if (companionPrefab == null)
            return false;

        if (!TrySpendMeat(meatCost))
            return false;

        Vector3[] offsets =
        {
        transform.right * 2f,
        -transform.right * 2f,
        transform.forward * 2f,
        -transform.forward * 2f,
        (transform.right + transform.forward).normalized * 2f
    };

        Vector3 spawnPosition = transform.position + offsets[0];

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 candidate = transform.position + offsets[i];

            if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            {
                spawnPosition = hit.point;
                break;
            }
        }

        Instantiate(companionPrefab, spawnPosition, Quaternion.identity);
        NotifyProgressChanged();
        return true;
    }

    private void NotifyProgressChanged()
    {
        OnProgressChanged?.Invoke();
    }

    void UpdateAttackSectorRuntime()
    {
        if (!showAttackSectorRuntime || !requireFacing || attackSector == null)
        {
            if (attackSector != null)
                attackSector.enabled = false;
            return;
        }

        attackSector.enabled = true;
        attackSector.startColor = sectorColor;
        attackSector.endColor = sectorColor;

        float radius = attackRange;
        float halfAngle = requiredFacingAngle;
        Vector3 origin = transform.position + Vector3.up * sectorYOffset;
        Vector3 forward = transform.forward;

        int pointCount = sectorSegments + 2; // center + arc
        attackSector.positionCount = pointCount;

        // Center
        attackSector.SetPosition(0, origin);

        for (int i = 0; i <= sectorSegments; i++)
        {
            float t = (float)i / sectorSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            attackSector.SetPosition(i + 1, origin + dir * radius);
        }
    }


    void UpdateTargetArrow()
    {
        if (!isAttackingLoop || currentTarget == null)
        {
            ResetArrow();
            return;
        }

        if (_activeArrow == null && targetArrowPrefab != null)
        {
            _activeArrow = Instantiate(targetArrowPrefab);
            _arrowT = 0f;
        }

        if (_activeArrow == null) return;

        // Progress synced to attack rate
        _arrowT += Time.deltaTime / autoAttackCooldown;
        _arrowT = Mathf.Clamp01(_arrowT);

        Vector3 start =
            transform.position +
            transform.forward * 0.6f +
            Vector3.up * arrowHeight;

        Vector3 end =
            currentTarget.position +
            Vector3.up * arrowHeight;

        // Curved motion (arc)
        Vector3 mid = Vector3.Lerp(start, end, 0.5f);
        mid.y += arrowTravelCurve;

        Vector3 p1 = Vector3.Lerp(start, mid, _arrowT);
        Vector3 p2 = Vector3.Lerp(mid, end, _arrowT);
        Vector3 pos = Vector3.Lerp(p1, p2, _arrowT);

        _activeArrow.transform.position = pos;
        _activeArrow.transform.LookAt(end);
    }

    void ResetArrow()
    {
        if (_activeArrow != null)
            Destroy(_activeArrow);

        _activeArrow = null;
        _arrowT = 0f;
    }


    void GroundCheck()
    {
        // Bottom of capsule near the feet:
        Vector3 feet = transform.position + controller.center
                     + Vector3.down * (controller.height * 0.5f - controller.radius + 0.02f);

        // Spherecast just below the feet
        if (Physics.SphereCast(feet + Vector3.up * 0.1f, controller.radius * 0.95f,
                               Vector3.down, out RaycastHit hit, groundCheckDistance,
                               groundMask, QueryTriggerInteraction.Ignore))
        {
            _ground.grounded = true;
            _ground.normal = hit.normal;
            _ground.angleDeg = Vector3.Angle(hit.normal, Vector3.up);

            // Optional ground snap when hovering a tiny bit above ground
            if (hit.distance > 0.02f && hit.distance <= groundSnapDistance
                && _ground.angleDeg <= controller.slopeLimit)
            {
                controller.Move(Vector3.down * (hit.distance - 0.02f));
            }
        }
        else
        {
            _ground.grounded = false;
            _ground.normal = Vector3.up;
            _ground.angleDeg = 0f;
        }
    }

    private bool IsFacing(Transform target)
    {
        if (!target) return false;
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;

        to.Normalize();
        float dot = Vector3.Dot(transform.forward, to);
        float cos = Mathf.Cos(requiredFacingAngle * Mathf.Deg2Rad);
        return dot >= cos;
    }

    bool IsMobilePlatform()
    {
#if UNITY_EDITOR
        return false;
#elif UNITY_ANDROID || UNITY_IOS
    return true;
#else
    return false;
#endif
    }



    void Awake()
    {
        // CharacterController
        controller = GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();

        defaultStepOffset = controller.stepOffset;
        controller.slopeLimit = slopeLimitDeg;
        controller.stepOffset = maxStepOffset;
        controller.skinWidth = 0.04f;      // helps grounding stability
        controller.minMoveDistance = 0f;

        health = health ?? GetComponent<Health>();

        // Anim handler
        animHandler = animHandler ?? GetComponentInChildren<CuteAnimalAnimHandler>();

        // FSM
        StateMachine = new StateMachine();
        stamina = maxStamina;

        hunger = Mathf.Clamp(maxHunger, 0f, Mathf.Max(0.0001f, maxHunger)); // start full if enabled
        _starveDamageAccum = 0f;

        InitializeStats();

        Debug.Log($"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]} | Shadows: {QualitySettings.shadows} | ShadowDist: {QualitySettings.shadowDistance}");

    }


    void ReadMovementInput()
    {
        if (IsMobilePlatform() && virtualJoystick != null && virtualJoystick.gameObject.activeInHierarchy)
        {
            inputVec = virtualJoystick.GetAxis();
        }
        else
        {
            inputVec = moveAction != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;
        }
    }


    void InitializeStats()
    {
        stats = new List<PlayerStat>
        {
            new PlayerStat {
                type = PlayerStatType.MoveSpeed,
                baseValue = walkSpeed,
                perLevelIncrease = 0.3f
            },
            new PlayerStat {
                type = PlayerStatType.StaminaDrainRate,
                baseValue = runDrainPerSecond,
                perLevelIncrease = -1.5f   // drain improves downward
            },
            new PlayerStat {
                type = PlayerStatType.HungerDrainRate,
                baseValue = hungerDrainPerSecond,
                perLevelIncrease = -0.3f
            },
            new PlayerStat {
                type = PlayerStatType.MaxHealth,
                baseValue = health.maxHealth,
                perLevelIncrease = 10f
            },
            new PlayerStat {
                type = PlayerStatType.AttackDamage,
                baseValue = attackDamage,
                perLevelIncrease = 3f
            },
             new PlayerStat {
                type = PlayerStatType.CompnionLimit,
                baseValue = companionLimit,
                perLevelIncrease = 1f
            }
        };

            ApplyStats();
    }

    public void ApplyStats()
    {
        foreach (var stat in stats)
        {
            switch (stat.type)
            {
                case PlayerStatType.MoveSpeed:
                    walkSpeed = stat.CurrentValue;
                    runSpeed = stat.CurrentValue * 2f;
                    break;

                case PlayerStatType.StaminaDrainRate:
                    runDrainPerSecond = stat.CurrentValue;
                    break;

                case PlayerStatType.HungerDrainRate:
                    hungerDrainPerSecond = stat.CurrentValue;
                    break;

                case PlayerStatType.MaxHealth:
                    health.SetMaxHealth((int)stat.CurrentValue);
                    break;

                case PlayerStatType.AttackDamage:
                    attackDamage = Mathf.RoundToInt(stat.CurrentValue);
                    break;
                case PlayerStatType.CompnionLimit:
                    companionLimit = Mathf.RoundToInt(stat.CurrentValue);
                    break;
            }
        }
    }

    public int GetUpgradeCost(PlayerStatType type)
    {
        var stat = GetStat(type);
        if (stat == null) return 0;

        // Example scaling cost
        return 10 + (stat.level * 5);
    }

    public bool UpgradeStat(PlayerStatType type)
    {
        var stat = stats.Find(s => s.type == type);
        if (stat == null || !stat.CanUpgrade)
            return false;

        int cost = GetUpgradeCost(type);
        if (!TrySpendMeat(cost))
            return false;

        stat.Upgrade();
        ApplyStats();
        NotifyProgressChanged();
        return true;
    }

    public PlayerStat GetStat(PlayerStatType type)
    {
        return stats.Find(s => s.type == type);
    }

    void Start()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        attackAction?.action.Enable();

        virtualJoystick = VirtualJoystick.GetInstance(0);

        if (IsMobilePlatform())
        {
            if (virtualJoystick != null)
                virtualJoystick.gameObject.SetActive(true);
        }
        else
        {
            if (virtualJoystick != null)
                virtualJoystick.gameObject.SetActive(false);
        }

        StateMachine.ChangeState(new PlayerIdleState(this));
    }



    private void UpdateHunger(bool sprinting)
    {
        if (maxHunger <= 0f || pauseHungerDrain || isDead) return; // disabled or paused

        float dt = Time.deltaTime;

        // Drain
        float drain = hungerDrainPerSecond + (sprinting ? sprintBonusHungerDrain : 0f);
        if (drain > 0f)
        {
            hunger -= drain * dt;
            if (hunger < 0f) hunger = 0f;
        }

        // Starvation
        if (hunger <= 0f && health != null && !health.IsDead)
        {
            // Convert DPS to integer damage events without changing Health API
            _starveDamageAccum += starvationDamagePerSecond * dt;
            int whole = (int)_starveDamageAccum;
            if (whole > 0)
            {
                _starveDamageAccum -= whole;
                health.TakeDamage(whole);
                if (health.IsDead)
                {
                    OnDeath();
                }
            }
        }
    }

    /// <summary>Adds hunger (e.g., when eating). Positive heals hunger. Returns actual added amount.</summary>
    public float AddHunger(float amount)
    {
        if (maxHunger <= 0f) return 0f;
        float before = hunger;
        hunger = Mathf.Clamp(hunger + amount, 0f, maxHunger);
        return hunger - before;
    }

    /// <summary>Fully refill hunger (utility).</summary>
    public void RefillHunger() => hunger = maxHunger;

    public void AddHealth(float amount)
    {
        health.Heal(amount);
    }


    private void UpdateStamina(bool sprinting, bool moving)
    {
        if (maxStamina <= 0f) return; // disabled

        float dt = Time.deltaTime;

        if (sprinting)
        {
            stamina -= runDrainPerSecond * dt;
            _regenResumeTime = Time.time + regenDelayAfterSprint;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isRunning = false; // will force walk next frame
            }
        }
        else
        {
            // Regen only after short cooldown
            if (Time.time >= _regenResumeTime)
            {
                float regen = moving ? walkRegenPerSecond : idleRegenPerSecond;
                stamina += regen * dt;
                if (stamina > maxStamina) stamina = maxStamina;
            }
        }
    }



    void Update()
    {
        if (isDead) return;
        if (isInParabola) return;

        ReadMovementInput();

        // Camera-relative movement direction (unchanged above)
        Vector3 camF = Camera.main.transform.forward;
        Vector3 camR = Camera.main.transform.right;
        camF.y = camR.y = 0f; camF.Normalize(); camR.Normalize();
        Vector3 inputDir = camF * inputVec.y + camR * inputVec.x;

        // —— Stamina-gated run vs walk (FIXED) ——
        bool wantsRun = inputVec.magnitude >= runThreshold;
        bool moving = inputDir.sqrMagnitude > 0.01f;

        bool lastIsRunning = isRunning;                  // remember previous state
        bool canStartRun = stamina >= minToStartRunning;

        // Only keep sprinting if player STILL wants to run, is MOVING, and has stamina
        bool keepSprinting = lastIsRunning && wantsRun && moving && stamina > 0.01f;

        // If not moving at all, force sprint off
        if (!moving) isRunning = false;

        // New sprint state
        isRunning = (wantsRun && moving && canStartRun) || keepSprinting;

        // If we just stopped sprinting, start regen cooldown now
        if (lastIsRunning && !isRunning)
            _regenResumeTime = Time.time + regenDelayAfterSprint;

        // Update stamina after deciding sprint state
        UpdateStamina(isRunning, moving);
        UpdateHunger(isRunning);

        // Speed & movement (unchanged)
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 targetVelocity = inputDir.normalized * currentSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        moveDirection = currentVelocity;

        float speed01 = Mathf.InverseLerp(walkSpeed, runSpeed, currentVelocity.magnitude);
        controller.stepOffset = Mathf.Lerp(maxStepOffset, runStepOffset, speed01);

        // Update timers
        attackTimer -= Time.deltaTime;
        autoAttackTimer -= Time.deltaTime;

        // FSM update
        StateMachine.Update();

        AutoAttackLoop();

        if(showAttackSectorRuntime)
        {
            UpdateAttackSectorRuntime();
        }
        if(showAttackArrow) 
        {
            UpdateTargetArrow();
        }

        // choose locomotion anim ONLY if not attacking
        UpdateLocomotionAnimation();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // If we bump into an enemy, do not allow stepping onto it
        if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            controller.stepOffset = 0f;
    }

    public void AutoAttackLoop()
    {
        // Pick nearest valid target in auto-aggro radius
        Transform t = FindNearestEnemy();

        if (t != null && t.TryGetComponent<Health>(out var hp) && !hp.IsDead)
        {
            float d = Vector3.Distance(transform.position, t.position);
            bool facingOk = !requireFacing || IsFacing(t);

            // Only keep/enter attack loop if within hard attack range AND facing (if required)
            if (d <= attackRange && facingOk)
            {
                currentTarget = t;
                isAttackingLoop = true;

                // Keep attack anim playing while target is valid
                animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);

                if (autoAttackTimer <= 0f)
                {
                    // Double-check the chosen target is also inside the actual hit area
                    Vector3 hitCenter = transform.position + transform.forward * hitForwardOffset;

                    Vector3 closestPoint = t.GetComponent<Collider>() != null
                        ? t.GetComponent<Collider>().ClosestPoint(hitCenter)
                        : t.position;

                    bool targetInsideHitRadius =
                        (closestPoint - hitCenter).sqrMagnitude <= attackHitRadius * attackHitRadius;

                    if (targetInsideHitRadius)
                    {
                        hp.TakeDamage(attackDamage);
                        _arrowT = 0f;

                        if (t.TryGetComponent<CuteAnimalAI>(out var ai))
                        {
                            Vector3 kb = (t.position - transform.position).normalized;
                            ai.ApplyKnockback(kb);
                        }
                    }

                    autoAttackTimer = autoAttackCooldown;
                }

                return;
            }

            // If in aggro radius but not yet facing, rotate toward target
            if (!disableAttackRotation && d <= autoAttackRadius)
            {
                Vector3 to = t.position - transform.position;
                to.y = 0f;
                RotateTowards(to);
            }
        }

        // No valid target
        currentTarget = null;
        isAttackingLoop = false;
    }


    public void UpdateLocomotionAnimation()
    {
        // If attacking, we deliberately keep ATTACK anim looping.
        if (isAttackingLoop) return;

        if (HasMovementInput())
            animHandler?.SetAnimation(isRunning ? eCuteAnimalAnims.RUN : eCuteAnimalAnims.WALK);
        else
            animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
    }

    // Helpers
    public bool HasMovementInput() => moveDirection.sqrMagnitude > 0.01f;
    public bool CanJump() => controller.isGrounded && jumpAction.action.triggered;
    public bool CanAttack() => attackAction.action.triggered && attackTimer <= 0f;
    public bool IsGrounded() => controller.isGrounded;

    public void RotateTowards(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion tgt = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, tgt, rotationSpeed * Time.deltaTime);
        }
    }

    public void Move(Vector3 desiredHorizontal)
    {
        // 1) Probe ground each frame
        GroundCheck();

        // 2) Gravity + stick
        if (_ground.grounded)
        {
            // keep a small downward bias so we don’t “float” over bumps
            if (verticalVelocity < 0f) verticalVelocity = -stickToGroundForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime; // gravity should be something like -25 to -35
        }

        // 3) Align horizontal motion to the ground plane (walk up/down slopes smoothly)
        Vector3 horizontal = desiredHorizontal;
        if (_ground.grounded)
            horizontal = Vector3.ProjectOnPlane(horizontal, _ground.normal);

        // 4) Slide down if on a slope steeper than the controller’s slope limit
        if (_ground.grounded && _ground.angleDeg > controller.slopeLimit + 0.1f)
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, _ground.normal).normalized;
            horizontal += slideDir * slopeSlideSpeed;
        }

        // 5) Compose final motion
        Vector3 motion = horizontal + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        // 6) Re-check quick: if we landed this frame, clamp fall
        if (_ground.grounded && verticalVelocity < -stickToGroundForce)
            verticalVelocity = -stickToGroundForce;
    }

    public void ApplyKnockback(Vector3 direction)
    {
        StateMachine.ChangeState(new PlayerKnockbackState(this, direction));
    }

    public void OnDeath()
    {
        isDead = true;
        StateMachine.ChangeState(new PlayerDeadState(this));
    }

    public Transform FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, autoAttackRadius, enemyLayer);
        Transform nearest = null;
        float minD = Mathf.Infinity;
        foreach (var h in hits)
        {
            if (h.TryGetComponent<Health>(out var hp) && !hp.IsDead)
            {
                float d = Vector3.Distance(transform.position, h.transform.position);
                if (d < minD)
                {
                    minD = d;
                    nearest = h.transform;
                }
            }
        }
        return nearest;
    }
}

#endregion

#region PLAYER STATES

public class PlayerIdleState : IState
{
    private CCActor actor;
    public PlayerIdleState(CCActor actor) { this.actor = actor; }
    public void Enter() { actor.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE); }
    public void Update()
    {
        // Transitions
        if (actor.HasMovementInput()) { actor.StateMachine.ChangeState(new PlayerMoveState(actor)); return; }
    }
    public void Exit() { }
}

public class PlayerMoveState : IState
{
    private CCActor actor;
    public PlayerMoveState(CCActor actor) { this.actor = actor; }
    public void Enter() { }
    public void Update()
    {
        // Movement + animation
        actor.RotateTowards(actor.moveDirection);
        actor.Move(actor.moveDirection);
        actor.animHandler?.SetAnimation(actor.HasMovementInput()
            ? (actor.isRunning ? eCuteAnimalAnims.RUN : eCuteAnimalAnims.WALK)
            : eCuteAnimalAnims.IDLE);
        // Transitions
        if (!actor.HasMovementInput()) { actor.StateMachine.ChangeState(new PlayerIdleState(actor)); return; }
    }
    public void Exit() { }
}

public class PlayerKnockbackState : IState
{
    private CCActor actor;
    private Vector3 horizontalDir;
    private float verticalVelocity;
    private float timer;

    // tuning
    private float knockDuration = 0.6f;
    private float horizontalSpeed = 26f;
    private float gravity = -35f;

    public PlayerKnockbackState(CCActor actor, Vector3 dir)
    {
        this.actor = actor;
        horizontalDir = dir.Flat().normalized;
    }

    public void Enter()
    {
        timer = knockDuration;

        // initial upward impulse
        verticalVelocity = 18f;

        // hard-disable movement input
        actor.inputVec = Vector2.zero;

        actor.animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
    }

    public void Update()
    {
        timer -= Time.deltaTime;

        // apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion =
            horizontalDir * horizontalSpeed +
            Vector3.up * verticalVelocity;

        actor.controller.Move(motion * Time.deltaTime);

        // early exit if grounded and falling
        if (actor.controller.isGrounded && verticalVelocity <= 0f)
        {
            EndKnockback();
            return;
        }

        if (timer <= 0f)
        {
            EndKnockback();
        }
    }

    void EndKnockback()
    {
        //SnapToGround();
        actor.StateMachine.ChangeState(
            actor.HasMovementInput()
                ? new PlayerMoveState(actor)
                : new PlayerIdleState(actor)
        );
    }

    void SnapToGround()
    {
        if (Physics.Raycast(actor.transform.position + Vector3.up * 0.2f,
                            Vector3.down, out RaycastHit hit,
                            2f, actor.groundMask))
        {
            Vector3 p = actor.transform.position;
            p.y = hit.point.y;
            actor.transform.position = p;
        }
    }

    public void Exit() { }
}


public class PlayerDeadState : IState
{
    private CCActor actor;
    public PlayerDeadState(CCActor a) { actor = a; }
    public void Enter()
    {
        actor.animHandler?.SetAnimation(eCuteAnimalAnims.DIE);
    }
    public void Update() { }
    public void Exit() { }
}




#endregion
