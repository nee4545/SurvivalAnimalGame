using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static CuteAnimalAI;

#region MAIN AI SCRIPT

public class CuteAnimalAI : MonoBehaviour
{
    public enum AIType { Passive, PassiveEasy, PassiveVeryEasy, Aggressive } // ✅ Added PassiveVeryEasy
    public enum AnimalType { Zebra, Giraffe, Lion, Elephant, Hyena }

    [Header("⚙️ General AI Settings")]
    [Tooltip("Type of AI - Passive animals flee, Aggressive animals chase/attack.")]
    public AIType aiType = AIType.Passive;

    [Tooltip("Animal species (used for grouping/herding behavior).")]
    public AnimalType animalType = AnimalType.Zebra;

    [Tooltip("Enable flocking behavior for group movement.")]
    public bool enableFlocking = false;

    [Header("🚶 Movement Speeds")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 4f;
    public float fleeSpeed = 5f;
    public float rotationSpeed = 90f;

    [Header("📏 Ranges & Detection")]
    public float detectionRange = 6f;
    public float fleeRange = 5f;
    public float stopDistance = 2f;
    public float wanderRadius = 10f;

    [Header("🤝 Herding & Flocking")]
    public float repulsionDistance = 2f;
    public float repulsionStrength = 1.5f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.0f;

    [Header("⚔️ Combat Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;

    [Header("⏳ Behavior Timers")]
    public float wanderInterval = 4f;
    public float knockbackDuration = 0.2f;

    [Header("🏜️ Navigation & Waypoints")]
    public Transform[] wanderPoints;

    [Header("😱 Panic & Flee Settings")]
    public float herdPanicRadius = 10f;
    public int fleeAttempts = 6;
    public float navMeshEdgeThreshold = 2.0f;
    public float playerPredictionTime = 1.5f;
    public float playerForwardPredictionDistance = 2f;
    public float zigzagSpeed = 6f;
    public float zigzagStrength = 0.5f;

    [Header("🧓 Passive Herd Settings")]
    public float herdJoinRadius = 15f;
    public float herdPreferredDistance = 5f;
    public float herdRegroupSpeed = 3f;

    [Header("🏡 Home Area Settings")]
    [Tooltip("Radius around the spawn point where animals can rest/eat.")]
    public float homeRadius = 8f;

    // Components
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Health health;
    [HideInInspector] public CuteAnimalAnimHandler animHandler;
    [HideInInspector] public Transform player;
    [HideInInspector] public StateMachine StateMachine;

    // Internal Timers
    [HideInInspector] public float wanderTimer;
    [HideInInspector] public float attackTimer;

    // Knockback state
    [HideInInspector] public Vector3 knockbackVector;
    [HideInInspector] public float knockbackTimer;

    // ✅ Remember spawn point (home area)
    [HideInInspector] public Vector3 spawnPosition;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
        health = GetComponent<Health>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.autoBraking = true;

        spawnPosition = transform.position;
        wanderTimer = wanderInterval;

        StateMachine = new StateMachine();
        StateMachine.ChangeState(new AIWanderState(this));

        if (health != null)
            health.onDeath.AddListener(OnDeath);
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StateMachine.ChangeState(new AIDeadState(this));
            return;
        }

        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            agent.Move(knockbackVector * Time.deltaTime);
            return;
        }

        attackTimer -= Time.deltaTime;
        StateMachine.Update();
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
        knockbackVector = direction;
        knockbackTimer = knockbackDuration;
        agent.ResetPath();
        animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
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
        if ((ai.aiType == AIType.Passive || ai.aiType == AIType.PassiveEasy || ai.aiType == AIType.PassiveVeryEasy) && dist <= ai.fleeRange)
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
        if (ai.aiType == CuteAnimalAI.AIType.Aggressive)
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
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
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


