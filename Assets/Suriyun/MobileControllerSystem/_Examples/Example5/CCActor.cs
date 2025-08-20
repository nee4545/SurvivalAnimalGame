using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Terresquall;

#region FSM CORE

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

public class StateMachine
{
    private IState currentState;

    public IState CurrentState => currentState;

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}

#endregion

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

    private Vector3 currentVelocity;  // New: for smooth acceleration
    [Header("Advanced Movement")]
    public float acceleration = 10f;  // New: tweak this for snappier or slower response

    [Header("Auto Attack Settings")]
    public float autoAttackRadius = 2f;
    public float autoAttackCooldown = 2f;
    public LayerMask enemyLayer;

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


    void Awake()
    {
        // CharacterController
        controller = GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();

        defaultStepOffset = controller.stepOffset;
        controller.slopeLimit = slopeLimitDeg;
        controller.stepOffset = maxStepOffset;
        controller.skinWidth = 0.04f;      // helps grounding stability
        controller.minMoveDistance = 0f;

        // Anim handler
        animHandler = animHandler ?? GetComponentInChildren<CuteAnimalAnimHandler>();

        // FSM
        StateMachine = new StateMachine();
        stamina = maxStamina;

    }



    void Start()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        attackAction?.action.Enable();

        // Always enable virtual joystick for testing
        virtualJoystick = VirtualJoystick.GetInstance(0);
        if (virtualJoystick != null)
            virtualJoystick.gameObject.SetActive(true);

        // Init FSM
        StateMachine.ChangeState(new PlayerIdleState(this));
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

        // Update input
        if (virtualJoystick != null)
            inputVec = virtualJoystick.GetAxis();
        else if (moveAction != null)
            inputVec = moveAction.action.ReadValue<Vector2>();

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
        // pick nearest valid target in range
        Transform t = FindNearestEnemy();
        if (t != null && t.TryGetComponent<Health>(out var hp) && !hp.IsDead)
        {
            float d = Vector3.Distance(transform.position, t.position);
            if (d <= attackRange)
            {
                currentTarget = t;
                isAttackingLoop = true;

                // keep attack anim playing while enemies are present
                animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);

                // cooldown tick
                if (autoAttackTimer <= 0f)
                {
                    // simple melee cone/arc in front
                    Vector3 hitCenter = transform.position + transform.forward * 1.0f;
                    Collider[] hits = Physics.OverlapSphere(hitCenter, attackHitRadius, enemyLayer);
                    foreach (var h in hits)
                    {
                        if (h.TryGetComponent<Health>(out var eh) && !eh.IsDead)
                        {
                            eh.TakeDamage(attackDamage);

                            // optional: light push
                            if (h.TryGetComponent<CuteAnimalAI>(out var ai))
                            {
                                Vector3 kb = (h.transform.position - transform.position).normalized;
                                ai.ApplyKnockback(kb);
                            }
                        }
                    }

                    autoAttackTimer = autoAttackCooldown;
                }

                return; // keep attacking, do not touch movement/rotation
            }
        }

        // no valid target -> stop attack loop
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
    private Vector3 dir;
    private float timer;
    public PlayerKnockbackState(CCActor a, Vector3 d)
    {
        actor = a;
        dir = d.normalized;
    }
    public void Enter()
    {
        timer = 0.2f;
        actor.animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
    }
    public void Update()
    {
        timer -= Time.deltaTime;
        actor.Move(dir * 5f);
        if (timer <= 0)
            actor.StateMachine.ChangeState(
                actor.HasMovementInput() ? new PlayerMoveState(actor) : new PlayerIdleState(actor));
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
