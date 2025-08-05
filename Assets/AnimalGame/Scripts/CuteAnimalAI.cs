using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static CuteAnimalAI;

#region MAIN AI SCRIPT

public class CuteAnimalAI : MonoBehaviour
{
    public enum AIType { Passive, PassiveEasy, PassiveVeryEasy, Aggressive, AggressiveType1, AggressiveType2, Companion }
    public enum AnimalType { Zebra, Giraffe, Lion, Elephant, Hyena, Chick, Chicken, Deer, Moose, Hippo, Rhino, Koala, Platypus, Cat, Dog, Panda, Bear, Crane, Peacock, Ostrich }

    [Header("🧠 AI Behavior Type")]
    [Tooltip("Overall behavior pattern of this animal.")]
    public AIType aiType = AIType.Passive;

    [Tooltip("Species for grouping or flocking behavior.")]
    public AnimalType animalType = AnimalType.Zebra;

    [Tooltip("Enable flocking (alignment, cohesion, separation) with same-type neighbors.")]
    public bool enableFlocking = false;

    [Header("🚶 Movement Speeds")]
    [Tooltip("Wander speed (all AI types).")]
    public float wanderSpeed = 2f;

    [Tooltip("Chase speed (Aggressive types).")]
    public float chaseSpeed = 4f;

    [Tooltip("Flee speed (Passive types).")]
    public float fleeSpeed = 5f;

    [Tooltip("Turning speed in degrees/sec.")]
    public float rotationSpeed = 90f;

    [Header("📏 Detection & Ranges")]
    [Tooltip("Player detection radius (Aggressive types).")]
    public float detectionRange = 6f;

    [Tooltip("Flee trigger distance (Passive types).")]
    public float fleeRange = 5f;

    [Tooltip("Stop distance for NavMeshAgent.")]
    public float stopDistance = 2f;

    [Tooltip("Radius for random wander target selection.")]
    public float wanderRadius = 10f;

    [Header("🤝 Flocking Settings")]
    [Tooltip("Min spacing to avoid crowding.")]
    public float repulsionDistance = 2f;
    [Tooltip("Repulsion strength when too close.")]
    public float repulsionStrength = 1.5f;
    [Tooltip("Weight of neighbor direction alignment.")]
    public float alignmentWeight = 1f;
    [Tooltip("Weight of neighbor cohesion.")]
    public float cohesionWeight = 1f;

    [Header("⚔️ Combat Settings")]
    [Tooltip("Melee attack range.")]
    public float attackRange = 1.5f;
    [Tooltip("Cooldown between attacks.")]
    public float attackCooldown = 2f;
    [Tooltip("Damage per attack.")]
    public int attackDamage = 10;
    [Tooltip("Duration of attack animation/state.")]
    public float attackDuration = 0.6f;

    [Header("⏱️ Timing Settings")]
    [Tooltip("Wander decision interval.")]
    public float wanderInterval = 4f;
    [Tooltip("Knockback duration when damaged.")]
    public float knockbackDuration = 0.2f;
    [Tooltip("Minimum rest duration.")]
    public float restMinDuration = 5f;
    [Tooltip("Maximum rest duration.")]
    public float restMaxDuration = 10f;

    [Header("📍 Navigation")]
    [Tooltip("Optional patrol points for wander.")]
    public Transform[] wanderPoints;

    [Header("😱 Flee & Panic")]
    [Tooltip("Radius to alert herd to flee.")]
    public float herdPanicRadius = 10f;
    [Tooltip("Max attempts to find valid flee target.")]
    public int fleeAttempts = 6;
    [Tooltip("Min edge distance to avoid NavMesh edges.")]
    public float navMeshEdgeThreshold = 2f;
    [Tooltip("Prediction time for player movement.")]
    public float playerPredictionTime = 1.5f;
    [Tooltip("Forward projection distance when fleeing.")]
    public float playerForwardPredictionDistance = 2f;
    [Tooltip("Speed multiplier for initial flee burst.")]
    public float fleeBurstMultiplier = 1.5f;
    [Tooltip("Duration of initial flee burst.")]
    public float fleeBurstDuration = 0.5f;
    [Tooltip("Zigzag lateral speed.")]
    public float zigzagSpeed = 6f;
    [Tooltip("Zigzag lateral offset strength.")]
    public float zigzagStrength = 0.5f;

    [Header("🧓 Herding (Passive)")]
    [Tooltip("Radius to detect/join herd.")]
    public float herdJoinRadius = 15f;
    [Tooltip("Preferred spacing within herd.")]
    public float herdPreferredDistance = 5f;
    [Tooltip("Speed when regrouping.")]
    public float herdRegroupSpeed = 3f;

    [Header("🏡 Home Area")]
    [Tooltip("Home radius around spawn.")]
    public float homeRadius = 8f;

    [Header("🐗 Charge (AggressiveType1)")]
    [Tooltip("Windup duration before charge.")]
    public float windupDuration = 1.5f;
    [Tooltip("Charge speed.")]
    public float chargeSpeed = 8f;
    [Tooltip("Charge duration.")]
    public float chargeDuration = 2f;
    [Tooltip("Charge impact radius.")]
    public float chargeDamageRadius = 1.2f;
    [Tooltip("Charge damage.")]
    public int chargeDamage = 30;
    [Tooltip("Max charge attempts.")]
    public int maxChargeAttempts = 3;
    [Tooltip("Cooldown before charge attempts reset.")]
    public float chargeCooldownDuration = 4f;

    [Header("😤 Retaliation (AggressiveType2)")]
    [Tooltip("Delay to switch from flee to hunt after provoked.")]
    public float retaliationDelay = 3f;

    [Header("👤 Companion Settings")]
    [Tooltip("Distance at or above which Companion follows directly.")]
    public float companionFollowDistance = 3f;
    [Tooltip("Orbit radius around player when idling.")]
    public float companionCircleRadius = 2f;
    [Tooltip("Orbit speed in radians/sec.")]
    public float companionCircleSpeed = 1.5f;
    [Tooltip("Threshold squared for updating NavMesh destination to avoid jitter.")]
    public float companionJitterThresholdSq = 0.04f; // 0.2 units squared
    [Tooltip("Speed factor for smooth rotation.")]
    public float companionRotationLerp = 5f;

    [Header("👤 Companion Idle Tweaks")]
    [Tooltip("How often (sec) to pick a new idle spot.")]
    public float idleMoveInterval = 3f;
    [Tooltip("Min radius around player for idle moves.")]
    public float idleMinRadius = 1.5f;
    [Tooltip("Max radius around player for idle moves.")]
    public float idleMaxRadius = 3f;

    [Header("🐦 Companion Flocking")]
    [Tooltip("Neighbor radius for flocking.")]
    public float flockNeighborRadius = 2f;
    [Tooltip("Separation weight (avoid crowding).")]
    public float flockSeparationWeight = 1.5f;
    [Tooltip("Cohesion weight (stay together).")]
    public float flockCohesionWeight = 1f;
    [Tooltip("Alignment weight (match direction).")]
    public float flockAlignmentWeight = 1f;

    [Tooltip("Enable NavMeshAgent auto-braking.")]
    public bool enableAutoBraking = true;

    // Companion-specific runtime
    [HideInInspector] public Vector3 lastCompanionTarget;
    // Hidden runtime state
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Health health;
    [HideInInspector] public CuteAnimalAnimHandler animHandler;
    [HideInInspector] public Transform player;
    [HideInInspector] public StateMachine StateMachine;
    [HideInInspector] public Vector3 spawnPosition;
    [HideInInspector] public float wanderTimer;
    [HideInInspector] public float attackTimer;
    [HideInInspector] public float knockbackTimer;
    [HideInInspector] public Vector3 knockbackVector;
    [HideInInspector] public bool wasProvoked;
    [HideInInspector] public float provokedTimer;
    [HideInInspector] public int currentChargeAttempts;
    [HideInInspector] public bool isChargeCooldownActive;
    [HideInInspector] public float chargeCooldownTimer;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = enableAutoBraking;
        animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
        health = GetComponent<Health>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.autoBraking = true;

        spawnPosition = transform.position;
        wanderTimer = wanderInterval;

        StateMachine = new StateMachine();
        switch (aiType)
        {
            case AIType.Companion:
                StateMachine.ChangeState(new AICompanionFollowState(this));
                break;
            default:
                StateMachine.ChangeState(new AIWanderState(this));
                break;
        }

        if (health != null)
            health.onDeath.AddListener(OnDeath);
    }

    private void Update()
    {
        // 1) Death check
        if (health != null && health.IsDead)
        {
            StateMachine.ChangeState(new AIDeadState(this));
            return;
        }

        // 2) Knockback resolution
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            agent.Move(knockbackVector * Time.deltaTime);
            return;
        }

        // 3) Cool down your basic attack timer
        attackTimer -= Time.deltaTime;

        // 4) AggressiveType1 charge cooldown
        if (aiType == AIType.AggressiveType1 && isChargeCooldownActive)
        {
            chargeCooldownTimer -= Time.deltaTime;
            if (chargeCooldownTimer <= 0f)
            {
                isChargeCooldownActive = false;
                currentChargeAttempts = 0;
            }
        }

        // 5) AggressiveType2 “retaliation delay” logic
        if (aiType == AIType.AggressiveType2 && wasProvoked)
        {
            provokedTimer -= Time.deltaTime;
            if (provokedTimer <= 0f)
            {
                // --- STEP 4: reset the provoked flag so future hits re-trigger flee ---
                wasProvoked = false;

                // Now transition to chase or attack depending on range
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackRange)
                    StateMachine.ChangeState(new AIAttackState(this));
                else
                    StateMachine.ChangeState(new AIChaseState(this));
                return;
            }
        }

        // 6) Otherwise, let the normal state machine run
        StateMachine.Update();

    }

    public void SmoothRotate(Vector3 dir, float lerpSpeed)
    {
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * lerpSpeed);
    }

    public float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.position);
    }

    public void UpdateMovementAnimation()
    {
        bool arrived = !agent.hasPath || agent.remainingDistance <= (agent.stoppingDistance + 0.1f);
        float speed = agent.velocity.magnitude;

        if (arrived || speed < 0.05f)
        {
            animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
            return;
        }

        if (speed < wanderSpeed + 0.2f)
            animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
        else
            animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void RotateTowards(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public bool PlayerInRange(float range)
    {
        return DistanceToPlayer() <= range;
    }

    public void ApplyKnockback(Vector3 direction)
    {

        if (aiType == AIType.AggressiveType2 && !(StateMachine.CurrentState is AIAttackState))
        {
            wasProvoked = true;
            provokedTimer = retaliationDelay;
            StateMachine.ChangeState(new AIFleeThenHuntState(this));
        }

        if (aiType == AIType.AggressiveType1)
        {
            // immediately interrupt whatever we were doing and start re-charging
            StateMachine.ChangeState(new AIWindupState(this));
        }

        knockbackVector = direction;
        knockbackTimer = knockbackDuration;
        agent.ResetPath();
        //animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
    }

    void OnDeath()
    {
        StateMachine.ChangeState(new AIDeadState(this));
    }

    public Vector3 GetBoidTarget(Vector3 baseTarget)
    {
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 repulsion = Vector3.zero;
        int neighborCount = 0;

        Collider[] neighbors = Physics.OverlapSphere(transform.position, repulsionDistance * 2);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject) continue;
            CuteAnimalAI otherAI = neighbor.GetComponent<CuteAnimalAI>();
            if (otherAI != null && otherAI.aiType == aiType && otherAI.animalType == animalType)
            {
                alignment += otherAI.agent.velocity.normalized;
                cohesion += neighbor.transform.position;

                Vector3 away = transform.position - neighbor.transform.position;
                repulsion += away.normalized / (away.magnitude + 0.01f);

                neighborCount++;
            }
        }

        Vector3 direction = (baseTarget - transform.position).normalized * wanderRadius;

        if (neighborCount > 0)
        {
            alignment = alignment.normalized * alignmentWeight;
            cohesion = ((cohesion / neighborCount) - transform.position).normalized * cohesionWeight;
            repulsion *= repulsionStrength;
            direction += alignment + cohesion + repulsion;
        }

        Vector3 target = transform.position + direction;
        if (NavMesh.SamplePosition(target, out var hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }
}

#endregion



#region AI STATES

// ================== REST STATE ==================
public class AIRestState : IState
{
    private CuteAnimalAI ai;
    private float restTimer;
    private bool isEating;

    public AIRestState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        // 1) Random rest duration
        restTimer = Random.Range(5f, 10f);

        // 2) Pick eat vs rest
        if (ai.aiType == AIType.AggressiveType1 || ai.aiType == AIType.AggressiveType2)
            isEating = true;
        else
            isEating = Random.value > 0.5f;

        // 3) Halt the agent so it doesn't idle‑override our animation
        ai.agent.ResetPath();
        ai.agent.isStopped = true;

        // 4) Play the chosen animation
        if (isEating)
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.EAT);
        else
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.REST);
    }

    public void Update()
    {
        float dist = ai.DistanceToPlayer();

        // ——— Player logic ———
        if (ai.aiType == AIType.Passive || ai.aiType == AIType.PassiveEasy || ai.aiType == AIType.PassiveVeryEasy)
        {
            if (dist <= ai.fleeRange)
            {
                ai.agent.isStopped = false;
                ai.StateMachine.ChangeState(
                    ai.aiType == AIType.PassiveEasy
                        ? new AIFleeSimpleState(ai)
                        : ai.aiType == AIType.PassiveVeryEasy
                            ? new AIFleeVeryEasyState(ai)
                            : new AIFleeState(ai)
                );
                return;
            }
        }
        else if (ai.aiType == AIType.Aggressive)
        {
            if (dist <= ai.attackRange)
            {
                ai.agent.isStopped = false;
                ai.StateMachine.ChangeState(new AIAttackState(ai));
                return;
            }
            if (dist <= ai.detectionRange)
            {
                ai.agent.isStopped = false;
                ai.StateMachine.ChangeState(new AIChaseState(ai));
                return;
            }
        }
        else if (ai.aiType == AIType.AggressiveType2 && ai.wasProvoked)
        {
            if (dist <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIAttackState(ai));
                return;
            }
            else if (dist <= ai.detectionRange)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai));
                return;
            }
        }
        else if (ai.aiType == AIType.AggressiveType1)
        {
            if (!ai.isChargeCooldownActive && dist <= ai.attackRange)
            {
                ai.agent.isStopped = true;
                ai.StateMachine.ChangeState(new AIWindupState(ai));
                return;
            }
        }

        // ——— Spacing logic ———
        Vector3 spacing = ComputeRestSpacing();
        if (spacing.sqrMagnitude > 0.01f)
        {
            // micro‑step to avoid overlap
            ai.agent.isStopped = false;
            if (NavMesh.SamplePosition(
                  ai.transform.position + spacing.normalized * 1f,
                  out var hit, 1f, NavMesh.AllAreas))
            {
                ai.agent.SetDestination(hit.position);
                ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
                return;
            }
        }

        // ——— Otherwise stay in rest/eat pose ———
        ai.agent.isStopped = true;

        // ——— Timer to go back to wandering ———
        restTimer -= Time.deltaTime;
        if (restTimer <= 0f)
        {
            ai.agent.isStopped = false;
            ai.StateMachine.ChangeState(new AIWanderState(ai));
        }
    }


    public void Exit()
    {
        // Make sure movement is re‑enabled and we revert to walk anim
        ai.agent.isStopped = false;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
    }



    private Vector3 ComputeRestSpacing()
    {
        Vector3 totalPush = Vector3.zero;
        int count = 0;

        Collider[] neighbors = Physics.OverlapSphere(ai.transform.position, ai.repulsionDistance);
        foreach (var n in neighbors)
        {
            if (n.TryGetComponent(out CuteAnimalAI other) && other != ai)
            {
                if (other.animalType == ai.animalType)
                {
                    Vector3 away = ai.transform.position - other.transform.position;
                    float dist = away.magnitude;
                    if (dist < ai.repulsionDistance)
                    {
                        totalPush += away.normalized / (dist + 0.1f);
                        count++;
                    }
                }
            }
        }

        return (count > 0) ? totalPush / count : Vector3.zero;
    }

    private void UpdateMovementAnimation()
    {
        float speed = ai.agent.velocity.magnitude;
        if (!ai.agent.hasPath || ai.agent.isStopped || speed < 0.05f)
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
        else if (speed < ai.wanderSpeed + 0.2f)
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
        else
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }
}

// ================== WANDER STATE ==================
public class AIWanderState : IState
{
    private CuteAnimalAI ai;

    public AIWanderState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.speed = ai.wanderSpeed;
        ai.wanderTimer = ai.wanderInterval;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
    }

    public void Update()
    {
        float distanceToPlayer = ai.DistanceToPlayer();

        // ✅ Passive → flee if player too close
        if ((ai.aiType == AIType.Passive || ai.aiType == AIType.PassiveEasy || ai.aiType == AIType.PassiveVeryEasy)
     && distanceToPlayer <= ai.fleeRange)
        {
            ai.StateMachine.ChangeState(
                ai.aiType == AIType.PassiveEasy
                    ? new AIFleeSimpleState(ai)
                    : ai.aiType == AIType.PassiveVeryEasy
                        ? new AIFleeVeryEasyState(ai)
                        : new AIFleeState(ai)
            );
            return;
        }

        if (ai.aiType == AIType.AggressiveType1)
        {
            if (!ai.isChargeCooldownActive && distanceToPlayer <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIWindupState(ai));
                return;
            }
        }

        if (ai.aiType == AIType.AggressiveType2 && ai.wasProvoked)
        {
            if (distanceToPlayer <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIAttackState(ai));
                return;
            }
            else if (distanceToPlayer <= ai.detectionRange)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai));
                return;
            }
        }

        // ✅ Aggressive → chase or attack if close
        if (ai.aiType == CuteAnimalAI.AIType.Aggressive)
        {
            if (distanceToPlayer <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIAttackState(ai));
                return;
            }
            else if (distanceToPlayer <= ai.detectionRange)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai));
                return;
            }
        }

        float distToSpawn = Vector3.Distance(ai.transform.position, ai.spawnPosition);

        // ✅ Already in home area → REST
        if (distToSpawn <= ai.homeRadius * 0.8f)
        {
            ai.StateMachine.ChangeState(new AIRestState(ai));
            return;
        }

        ai.wanderTimer -= Time.deltaTime;

        // ✅ If agent stuck → pick a new target
        if (ai.agent.hasPath && ai.agent.remainingDistance > 0.5f && ai.agent.velocity.magnitude < 0.05f)
            ai.wanderTimer = 0.1f;

        if (ai.wanderTimer <= 0f || ai.agent.remainingDistance < 0.5f)
        {
            Vector3 target;

            if (distToSpawn > ai.homeRadius)
            {
                // ✅ Far → pick a random offset near spawn (waterhole)
                target = GetRandomHomeOffset();
            }
            else
            {
                // ✅ Still near home → small random wander
                target = GetRandomWanderTarget();
            }

            Vector3 finalTarget = ai.enableFlocking ? ai.GetBoidTarget(target) : target;
            ai.agent.SetDestination(finalTarget);

            ai.wanderTimer = ai.wanderInterval;
        }

        UpdateMovementAnimation();
    }

    private Vector3 GetRandomHomeOffset()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * ai.homeRadius;
            randomOffset.y = 0;
            Vector3 candidate = ai.spawnPosition + randomOffset;

            if (NavMesh.SamplePosition(candidate, out var hit, ai.homeRadius, NavMesh.AllAreas))
            {
                if (!IsSpotBlocked(hit.position))
                    return hit.position;
            }
        }
        return ai.spawnPosition; // fallback
    }

    private bool IsSpotBlocked(Vector3 pos)
    {
        Collider[] neighbors = Physics.OverlapSphere(pos, ai.repulsionDistance * 1.2f);
        foreach (var n in neighbors)
        {
            if (n.TryGetComponent(out CuteAnimalAI other) && other != ai)
            {
                if (other.StateMachine != null && other.StateMachine.GetType() == typeof(AIRestState))
                    return true; // blocked by resting herd member
            }
        }
        return false;
    }

    private Vector3 GetRandomWanderTarget()
    {
        Vector3 baseTarget;
        if (ai.wanderPoints != null && ai.wanderPoints.Length > 0)
        {
            baseTarget = ai.wanderPoints[Random.Range(0, ai.wanderPoints.Length)].position;
        }
        else
        {
            Vector3 randomDir = Random.insideUnitSphere * ai.wanderRadius;
            randomDir += ai.transform.position;
            if (NavMesh.SamplePosition(randomDir, out var hit, ai.wanderRadius, NavMesh.AllAreas))
                baseTarget = hit.position;
            else
                baseTarget = ai.transform.position;
        }
        return baseTarget;
    }

    private void UpdateMovementAnimation()
    {
        float speed = ai.agent.velocity.magnitude;
        if (!ai.agent.hasPath || ai.agent.isStopped || speed < 0.05f)
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
        else if (speed < ai.wanderSpeed + 0.2f)
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
        else
            ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void Exit() { }
}

// ================== CHASE STATE ==================
public class AIChaseState : IState
{
    private CuteAnimalAI ai;

    public AIChaseState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.speed = ai.chaseSpeed;
        ai.agent.stoppingDistance = ai.stopDistance;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void Update()
    {
        float distanceToPlayer = ai.DistanceToPlayer();

        if (distanceToPlayer > ai.detectionRange)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }
        if (distanceToPlayer <= ai.attackRange)
        {
            ai.StateMachine.ChangeState(new AIAttackState(ai));
            return;
        }

        ai.agent.SetDestination(ai.player.position);
    }

    public void Exit() { }
}

// ================== ATTACK STATE ==================
public class AIAttackState : IState
{
    private CuteAnimalAI ai;
    private float attackDuration = 0.6f;
    private float timer;

    public AIAttackState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.ResetPath();
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);
        timer = attackDuration;

        if (ai.player != null && ai.attackTimer <= 0f)
        {
            Health ph = ai.player.GetComponent<Health>();
            if (ph != null)
            {
                ph.TakeDamage(ai.attackDamage);
            }
            ai.attackTimer = ai.attackCooldown;
        }
    }

    public void Update()
    {
        if (ai.player != null)
        {
            Vector3 dir = (ai.player.position - ai.transform.position);
            dir.y = 0;
            ai.RotateTowards(dir);
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            float distanceToPlayer = ai.DistanceToPlayer();
            if (distanceToPlayer <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIAttackState(ai)); // repeat
            }
            else if (distanceToPlayer <= ai.detectionRange)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai));
            }
            else
            {
                ai.StateMachine.ChangeState(new AIWanderState(ai));
            }
        }
    }

    public void Exit() { }
}

// ================== FLEE STATE ==================
public class AIFleeState : IState
{
    private CuteAnimalAI ai;
    private float burstTimer;
    private float burstMultiplier = 1.5f;
    private float burstDuration = 0.5f;

    public AIFleeState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        burstTimer = burstDuration;
        ai.agent.speed = ai.fleeSpeed * burstMultiplier;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void Update()
    {
        if (ai.player == null)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        if (burstTimer > 0)
        {
            burstTimer -= Time.deltaTime;
            if (burstTimer <= 0)
                ai.agent.speed = ai.fleeSpeed;
        }

        float distanceToPlayer = ai.DistanceToPlayer();

        if (distanceToPlayer > ai.fleeRange * 2.5f)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        Vector3 fleeDir = ComputeSafeFleeDirection();
        Vector3 lateral = Vector3.Cross(Vector3.up, fleeDir).normalized;
        float zigzagOffset = Mathf.Sin(Time.time * ai.zigzagSpeed) * ai.zigzagStrength;
        Vector3 finalDir = (fleeDir + lateral * zigzagOffset).normalized;
        Vector3 candidate = ai.transform.position + finalDir * ai.fleeRange;
        Vector3 safeTarget = ValidateFleeTarget(candidate);

        ai.RotateTowards(finalDir);

        if (safeTarget != Vector3.zero)
            ai.agent.SetDestination(safeTarget);
    }

    private Vector3 ComputeSafeFleeDirection()
    {
        Vector3 predictedPlayerPos = ai.player.position;
        if (ai.player.TryGetComponent<Rigidbody>(out var rb))
            predictedPlayerPos += rb.velocity * ai.playerPredictionTime;
        else
            predictedPlayerPos += ai.player.forward * ai.playerForwardPredictionDistance;

        Vector3 fleeDir = (ai.transform.position - predictedPlayerPos).normalized;

        if (NavMesh.FindClosestEdge(ai.transform.position, out var edgeHit, NavMesh.AllAreas))
        {
            if (edgeHit.distance < ai.navMeshEdgeThreshold * 1.5f)
            {
                Vector3 inward = ai.transform.position - edgeHit.position;
                inward.y = 0;
                inward.Normalize();

                fleeDir = Vector3.Lerp(fleeDir, inward, 0.4f).normalized;
            }
        }

        return fleeDir;
    }

    private Vector3 ValidateFleeTarget(Vector3 candidate)
    {
        if (NavMesh.SamplePosition(candidate, out var hit, ai.fleeRange, NavMesh.AllAreas))
        {
            if (NavMesh.FindClosestEdge(hit.position, out var edgeHit, NavMesh.AllAreas))
            {
                if (edgeHit.distance > ai.navMeshEdgeThreshold)
                    return hit.position;
            }
        }

        for (int i = 0; i < ai.fleeAttempts; i++)
        {
            Vector3 randomDir = Quaternion.Euler(0, Random.Range(-90f, 90f), 0) * Vector3.forward;
            Vector3 retryPos = ai.transform.position + randomDir * (ai.fleeRange * 0.5f);
            if (NavMesh.SamplePosition(retryPos, out var retryHit, ai.fleeRange, NavMesh.AllAreas))
            {
                if (NavMesh.FindClosestEdge(retryHit.position, out var edgeHit, NavMesh.AllAreas))
                {
                    if (edgeHit.distance > ai.navMeshEdgeThreshold)
                        return retryHit.position;
                }
            }
        }

        return ai.transform.position;
    }

    public void Exit() { }
}

// ================== KNOCKBACK STATE ==================
public class AIKnockbackState : IState
{
    private CuteAnimalAI ai;
    private Vector3 knockDir;
    private float timer;

    public AIKnockbackState(CuteAnimalAI ai, Vector3 dir)
    {
        this.ai = ai;
        knockDir = dir.normalized;
    }

    public void Enter()
    {
        timer = ai.knockbackDuration;
        //ai.animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
        ai.agent.ResetPath();
    }

    public void Update()
    {
        timer -= Time.deltaTime;
        ai.agent.Move(knockDir * 5f * Time.deltaTime);

        if (timer <= 0f)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
        }
    }

    public void Exit() { }
}

// ================== DEAD STATE ==================
public class AIDeadState : IState
{
    private CuteAnimalAI ai;

    public AIDeadState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.isStopped = true;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.DIE);
    }

    public void Update() { }
    public void Exit() { }
}

#endregion


public class AIWindupState : IState
{
    private CuteAnimalAI ai;
    private float timer;

    public AIWindupState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        timer = ai.windupDuration;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
        ai.agent.ResetPath();
    }

    public void Update()
    {
        if (ai.player == null)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        Vector3 dir = ai.player.position - ai.transform.position;
        dir.y = 0;
        ai.RotateTowards(dir);

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ai.StateMachine.ChangeState(new AIChargeState(ai));
        }
    }

    public void Exit() { }
}

public class AIChargeState : IState
{
    private CuteAnimalAI ai;
    private float timer;
    private Vector3 chargeDirection;
    private bool hasHitPlayer;

    public AIChargeState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.currentChargeAttempts++;
        ai.agent.speed = ai.chargeSpeed;
        ai.agent.ResetPath();
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
        timer = ai.chargeDuration;

        if (ai.player != null)
        {
            chargeDirection = (ai.player.position - ai.transform.position).normalized;
            chargeDirection.y = 0;
            ai.RotateTowards(chargeDirection);
        }
    }

    public void Update()
    {
        timer -= Time.deltaTime;

        Vector3 worldDir = (ai.player.position - ai.transform.position).normalized; // charge
                                                                                    // OR your fleeDir…

        // 1) Make sure you rotate over time:
        ai.RotateTowards(worldDir);

        // 2) Then only move straight ahead:
        Vector3 forwardMove = ai.transform.forward * ai.chargeSpeed * Time.deltaTime;
        ai.agent.Move(forwardMove);

        // Collision check
        if (!hasHitPlayer && ai.player != null)
        {
            float distance = Vector3.Distance(ai.transform.position, ai.player.position);
            if (distance <= ai.chargeDamageRadius)
            {
                if (ai.player.TryGetComponent<Health>(out var hp))
                {
                    hp.TakeDamage(ai.chargeDamage);
                    hasHitPlayer = true;
                }
            }
        }

        if (ai.currentChargeAttempts >= ai.maxChargeAttempts)
        {
            ai.isChargeCooldownActive = true;
            ai.chargeCooldownTimer = ai.chargeCooldownDuration;
            ai.StateMachine.ChangeState(new AIReturnToBaseState(ai));
            return;
        }

        if (timer <= 0 || hasHitPlayer)
        {
            float distanceToPlayer = ai.DistanceToPlayer();

            if (distanceToPlayer <= ai.attackRange && ai.player != null)
            {
                ai.StateMachine.ChangeState(new AIWindupState(ai)); // follow-up attack
            }
            else if (distanceToPlayer <= ai.detectionRange && ai.player != null)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai)); // keep pursuing
            }
            else
            {
                ai.StateMachine.ChangeState(new AIRestState(ai)); // return to chill
            }
        }

    }

    public void Exit() { }
}


public class AIFleeSimpleState : IState
{
    private CuteAnimalAI ai;

    public AIFleeSimpleState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.speed = ai.fleeSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void Update()
    {
        if (ai.player == null)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        float distanceToPlayer = ai.DistanceToPlayer();
        if (distanceToPlayer > ai.fleeRange * 2f)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        Vector3 fleeDir = (ai.transform.position - ai.player.position).normalized;
        Vector3 candidate = ai.transform.position + fleeDir * ai.fleeRange;

        if (NavMesh.SamplePosition(candidate, out var hit, ai.fleeRange, NavMesh.AllAreas))
        {
            if (NavMesh.FindClosestEdge(hit.position, out var edgeHit, NavMesh.AllAreas) &&
                edgeHit.distance < ai.navMeshEdgeThreshold)
            {
                Vector3 inward = ai.transform.position - edgeHit.position;
                candidate = ai.transform.position + inward.normalized * ai.fleeRange;
            }

            ai.agent.SetDestination(hit.position);
            ai.RotateTowards(fleeDir);
        }
    }

    public void Exit() { }
}

public class AIFleeVeryEasyState : IState
{
    private CuteAnimalAI ai;
    private Vector3 fleeDirection;
    private bool directionChosen = false;

    public AIFleeVeryEasyState(CuteAnimalAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        ai.agent.speed = ai.fleeSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
        directionChosen = false;
    }

    public void Update()
    {
        if (ai.player == null)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        float distanceToPlayer = ai.DistanceToPlayer();
        if (distanceToPlayer > ai.fleeRange * 2.5f)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        // Pick opposite of player's forward direction (only once unless edge is hit)
        if (!directionChosen)
        {
            fleeDirection = -ai.player.forward.normalized;
            directionChosen = true;
        }

        Vector3 candidate = ai.transform.position + fleeDirection * ai.fleeRange;

        if (NavMesh.SamplePosition(candidate, out var hit, ai.fleeRange, NavMesh.AllAreas))
        {
            if (NavMesh.FindClosestEdge(hit.position, out var edgeHit, NavMesh.AllAreas))
            {
                if (edgeHit.distance < ai.navMeshEdgeThreshold)
                {
                    // Hit an edge, choose a new direction randomly
                    fleeDirection = Random.insideUnitSphere;
                    fleeDirection.y = 0;
                    fleeDirection.Normalize();

                    candidate = ai.transform.position + fleeDirection * ai.fleeRange;
                }

                ai.agent.SetDestination(candidate);
                ai.RotateTowards(fleeDirection);  // Always face direction of escape
            }
        }
    }

    public void Exit() { }
}

public class AIReturnToBaseState : IState
{
    private CuteAnimalAI ai;
    private float restTimer = 4f;

    public AIReturnToBaseState(CuteAnimalAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        ai.agent.SetDestination(ai.spawnPosition);
        ai.agent.speed = ai.wanderSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
    }

    public void Update()
    {
        float distanceToSpawn = Vector3.Distance(ai.transform.position, ai.spawnPosition);

        if (distanceToSpawn > ai.homeRadius * 0.5f)
            return; // Keep walking home

        ai.agent.isStopped = true;
        restTimer -= Time.deltaTime;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.EAT);

        if (restTimer <= 0f)
        {
            ai.StateMachine.ChangeState(new AIRestState(ai));
        }
    }

    public void Exit() { }
}

public class AIFleeThenHuntState : IState
{
    private CuteAnimalAI ai;
    private float timer;
    private Vector3 fleeDirection;
    private const float DESTINATION_THRESHOLD_SQ = 0.25f; // 0.5 units²

    public AIFleeThenHuntState(CuteAnimalAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        // 1) Initialize timers and movement
        timer = ai.retaliationDelay;
        ai.agent.speed = ai.fleeSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        // 2) Turn off obstacle avoidance to prevent collisions with player
        ai.agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // 3) Pick one fixed flee direction at the moment of provocation
        fleeDirection = (ai.transform.position - ai.player.position).normalized;
    }

    public void Update()
    {
        // 1) Countdown
        timer -= Time.deltaTime;

        // 2) While fleeing
        if (timer > 0f)
        {
            Vector3 candidate = ai.transform.position + fleeDirection * ai.fleeRange;
            if (NavMesh.SamplePosition(candidate, out var hit, ai.fleeRange, NavMesh.AllAreas))
            {
                // Only update path if the new target is significantly different
                if ((hit.position - ai.agent.destination).sqrMagnitude > DESTINATION_THRESHOLD_SQ)
                {
                    ai.agent.SetDestination(hit.position);
                }
            }
            ai.RotateTowards(fleeDirection);
            return;
        }

        // 3) Once timer expires and we're safely away, switch to chase
        if (ai.DistanceToPlayer() >= ai.fleeRange * 1.2f)
        {
            // Re-enable obstacle avoidance
            ai.agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            ai.StateMachine.ChangeState(new AIChaseState(ai));
        }
    }

    public void Exit()
    {
        // Ensure avoidance is back to default
        ai.agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }
}

public class AICompanionState : IState
{
    private CuteAnimalAI ai;
    private float angle = 0f;

    public AICompanionState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.agent.stoppingDistance = ai.companionCircleRadius;
        ai.agent.speed = ai.wanderSpeed;
    }

    public void Update()
    {
        if (ai.player == null)
        {
            ai.StateMachine.ChangeState(new AIWanderState(ai));
            return;
        }

        // advance the circling angle
        angle += Time.deltaTime * ai.companionCircleSpeed;

        // compute target position on the circle
        Vector3 offset = new Vector3(
          Mathf.Cos(angle),
          0,
          Mathf.Sin(angle)
        ) * ai.companionCircleRadius;

        Vector3 target = ai.player.position + offset;
        ai.agent.SetDestination(target);
        ai.UpdateMovementAnimation();
    }

    public void Exit() { }
}

public class AICompanionIdleState : IState
{
    private CuteAnimalAI ai;
    private float angle;
    private Vector3 currentTarget;
    private float idleTimer;
    private Vector3 idleTarget;

    public AICompanionIdleState(CuteAnimalAI ai) { this.ai = ai; }

    private void PickIdleTarget()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(ai.idleMinRadius, ai.idleMaxRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        idleTarget = ai.player.position + offset;
    }

    private Vector3 ComputeFlockOffset()
    {
        var sep = Vector3.zero; var coh = Vector3.zero; var ali = Vector3.zero;
        int count = 0;
        foreach (var col in Physics.OverlapSphere(ai.transform.position, ai.flockNeighborRadius))
            if (col.TryGetComponent<CuteAnimalAI>(out var other) && other.aiType == AIType.Companion && other != ai)
            {
                Vector3 toOther = ai.transform.position - other.transform.position;
                sep += toOther.normalized / toOther.magnitude;
                coh += other.transform.position;
                ali += other.agent.velocity.normalized;
                count++;
            }
        if (count == 0) return Vector3.zero;
        coh = ((coh / count) - ai.transform.position).normalized;
        ali = (ali / count).normalized;
        return sep * ai.flockSeparationWeight + coh * ai.flockCohesionWeight + ali * ai.flockAlignmentWeight;
    }


    public void Enter()
    {
        idleTimer = 0f;
        PickIdleTarget();
        ai.agent.stoppingDistance = ai.companionCircleRadius;
        ai.agent.speed = ai.wanderSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
    }

    public void Update()
    {
        // 1) switch back to Follow if too far
        if (ai.DistanceToPlayer() > ai.companionFollowDistance * 1.2f)
        { ai.StateMachine.ChangeState(new AICompanionFollowState(ai)); return; }

        // 2) pick a new idle point as timer elapses
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            PickIdleTarget();
            idleTimer = ai.idleMoveInterval * Random.Range(0.8f, 1.2f);
        }

        // 3) compute combined target with flocking
        Vector3 flockOffset = ComputeFlockOffset();
        Vector3 desired = idleTarget + flockOffset;

        // 4) jitter reduction & NavMesh set
        if ((desired - ai.lastCompanionTarget).sqrMagnitude > ai.companionJitterThresholdSq)
        {
            ai.agent.SetDestination(desired);
            ai.lastCompanionTarget = desired;
        }

        // 5) smooth rotation toward velocity
        ai.SmoothRotate(ai.agent.velocity, ai.companionRotationLerp);

    }
    public void Exit() { }
}

/// <summary>
/// Companion follows directly until within followDistance, then idles by circling.
/// </summary>
public class AICompanionFollowState : IState
{
    private CuteAnimalAI ai;
    public AICompanionFollowState(CuteAnimalAI ai) { this.ai = ai; }
    public void Enter() 
    { 
        ai.agent.stoppingDistance = ai.companionFollowDistance;
        ai.agent.speed = ai.chaseSpeed;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }
    public void Update()
    {
        float dist = ai.DistanceToPlayer();
        if (dist > ai.companionFollowDistance)
        {
            ai.agent.SetDestination(ai.player.position);
            ai.SmoothRotate(ai.player.position - ai.transform.position, ai.companionRotationLerp);
        }
        else
        {
            ai.StateMachine.ChangeState(new AICompanionIdleState(ai));
        }
    }
    public void Exit() { }
}



