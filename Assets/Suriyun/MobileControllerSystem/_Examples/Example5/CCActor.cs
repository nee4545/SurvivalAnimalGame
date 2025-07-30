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
    public float jumpHeight = 3f;
    public float gravity = -9.81f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;

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

    private VirtualJoystick virtualJoystick;

    void Start()
    {
        controller = gameObject.AddComponent<CharacterController>();
        animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        attackAction?.action.Enable();

        // Always enable virtual joystick for testing
        virtualJoystick = VirtualJoystick.GetInstance(0);
        if (virtualJoystick != null)
            virtualJoystick.gameObject.SetActive(true);

        // Init FSM
        StateMachine = new StateMachine();
        StateMachine.ChangeState(new PlayerIdleState(this));
    }

    void Update()
    {
        if (isDead) return;

        // Update input
        if (virtualJoystick != null)
            inputVec = virtualJoystick.GetAxis();
        else if (moveAction != null)
            inputVec = moveAction.action.ReadValue<Vector2>();

        // Determine run vs walk
        isRunning = inputVec.magnitude >= runThreshold;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Camera-relative moveDirection
        Vector3 camF = Camera.main.transform.forward;
        Vector3 camR = Camera.main.transform.right;
        camF.y = camR.y = 0f;
        camF.Normalize(); camR.Normalize();
        Vector3 inputDir = camF * inputVec.y + camR * inputVec.x;
        moveDirection = inputDir.normalized * currentSpeed;

        // Update timers
        attackTimer -= Time.deltaTime;
        autoAttackTimer -= Time.deltaTime;

        // FSM update
        StateMachine.Update();
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

    public void Move(Vector3 vel)
    {
        // Apply gravity
        if (!controller.isGrounded)
            verticalVelocity += gravity * Time.deltaTime;
        else if (verticalVelocity < 0f)
            verticalVelocity = -2f;

        Vector3 motion = vel * Time.deltaTime;
        motion.y = verticalVelocity * Time.deltaTime;
        controller.Move(motion);
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
        // Auto-attack
        if (actor.autoAttackTimer <= 0f)
        {
            var tgt = actor.FindNearestEnemy();
            if (tgt != null)
            {
                actor.autoAttackTimer = actor.autoAttackCooldown;
                actor.StateMachine.ChangeState(new PlayerAttackState(actor, tgt));
                return;
            }
        }
        // Transitions
        if (actor.HasMovementInput()) { actor.StateMachine.ChangeState(new PlayerMoveState(actor)); return; }
        if (actor.CanJump()) { actor.StateMachine.ChangeState(new PlayerJumpState(actor)); return; }
        if (actor.CanAttack()) { actor.StateMachine.ChangeState(new PlayerAttackState(actor)); return; }
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

        // Auto-attack
        if (actor.autoAttackTimer <= 0f)
        {
            var tgt = actor.FindNearestEnemy();
            if (tgt != null)
            {
                actor.autoAttackTimer = actor.autoAttackCooldown;
                actor.StateMachine.ChangeState(new PlayerAttackState(actor, tgt));
                return;
            }
        }
        // Transitions
        if (!actor.HasMovementInput()) { actor.StateMachine.ChangeState(new PlayerIdleState(actor)); return; }
        if (actor.CanJump()) { actor.StateMachine.ChangeState(new PlayerJumpState(actor)); return; }
        if (actor.CanAttack()) { actor.StateMachine.ChangeState(new PlayerAttackState(actor)); return; }
    }
    public void Exit() { }
}

public class PlayerJumpState : IState
{
    private CCActor actor;
    private float jumpVel;
    public PlayerJumpState(CCActor actor) { this.actor = actor; }
    public void Enter()
    {
        jumpVel = Mathf.Sqrt(actor.jumpHeight * -2f * actor.gravity);
        actor.verticalVelocity = jumpVel;
        actor.animHandler?.SetAnimation(eCuteAnimalAnims.JUMP);
    }
    public void Update()
    {
        actor.Move(actor.moveDirection);
        if (actor.IsGrounded() && actor.verticalVelocity < 0f)
        {
            actor.StateMachine.ChangeState(
                actor.HasMovementInput() ? (IState)new PlayerMoveState(actor) : new PlayerIdleState(actor));
        }
    }
    public void Exit() { }
}

public class PlayerAttackState : IState
{
    private CCActor actor;
    private Transform tgt;
    private float timer;

    public PlayerAttackState(CCActor actor, Transform target = null)
    {
        this.actor = actor;
        this.tgt = target;
    }

    public void Enter()
    {
        actor.animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);
        timer = 0.4f;

        if (!actor.disableAttackRotation && tgt != null)
        {
            Vector3 faceDir = tgt.position - actor.transform.position;
            faceDir.y = 0;
            actor.RotateTowards(faceDir.normalized);
        }

        Collider[] hits = Physics.OverlapSphere(actor.transform.position + actor.transform.forward, 1.5f);
        foreach (var h in hits)
            if (h.TryGetComponent<Health>(out var hp))
                hp.TakeDamage(10);

        actor.attackTimer = actor.attackCooldown;
    }

    public void Update()
    {
        timer -= Time.deltaTime;
        actor.Move(actor.moveDirection);

        if (!actor.disableAttackRotation)
        {
            if (tgt != null)
            {
                Vector3 dir = tgt.position - actor.transform.position;
                dir.y = 0;
                actor.RotateTowards(dir.normalized);
            }
            else if (actor.HasMovementInput())
            {
                actor.RotateTowards(actor.moveDirection.normalized);
            }
        }

        if (timer <= 0f)
        {
            actor.StateMachine.ChangeState(
                actor.HasMovementInput() ? (IState)new PlayerMoveState(actor) : new PlayerIdleState(actor));
        }
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
