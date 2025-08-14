using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static CuteAnimalAI;

#region MAIN AI SCRIPT

[RequireComponent(typeof(NavMeshAgent))]
public class CuteAnimalAI : MonoBehaviour
{
    public enum AIType { Passive, PassiveEasy, PassiveVeryEasy, Aggressive, AggressiveType1, AggressiveType2, AggressiveType3, Companion , AggressiveJumping}
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

    [Header("🔍 Companion Targeting")]
    [Tooltip("Tag used to identify other AI to chase/attack")]
    public string targetTag = "Animal";

    [Header("🔍 Companion Combat")]
    [Tooltip("How far the companion can see and start chasing other animals.")]
    public float companionDetectionRange = 8f;

    [Header("🔗 Companion Tether")]
    [Tooltip("Max distance from player the companion is allowed to stray for chasing.")]
    public float maxChaseDistance = 8f;

    [Header("🐺 Pack Hunting (AggressiveType3)")]
    [Tooltip("Radius to call pack members for coordinated attack.")]
    public float packCallRadius = 15f;

    [Tooltip("Max pack members to coordinate with.")]
    public int maxPackSize = 4;

    [Tooltip("Time to coordinate before synchronized attack.")]
    public float coordinationTime = 2f;
    [Header("🐺 Pack Hunting (AggressiveType3)")]
    [Tooltip("Radius of circle around player for intercept points.")]
    public float packChaseRadius = 6f;

    [Header("🪵 Jumping (AggressiveJumping & PassiveJumping)")]
    [Tooltip("Peak height of jump arc (world units).")]
    public float jumpHeight = 3f;
    [Tooltip("Time to complete a jump (sec).")]
    public float jumpDuration = 0.7f;
    [Tooltip("How far around the target to search for a valid landing spot on NavMesh.")]
    public float jumpLandingProbeRadius = 2f;
    [Tooltip("How long the aggressive jumper chases before returning home.")]
    public float chaseDuration = 5f;

    [Header("Passive Jumping")]
    [Tooltip("Tag used for designer-placed perches (trees/rocks).")]
    public string jumpSpotTag = "JumpSpot";

    [Tooltip("How close to the foot of the JumpSpot before jumping up.")]
    public float jumpSpotApproachRadius = 1.25f;
    [Header("Jumping Cooldowns")]
    [Tooltip("How long to chill on the perch before allowed to jump down again (AggressiveJumping).")]
    public float perchCooldown = 1.0f;

    [Tooltip("Horizontal speed used to scale jump duration for long jumps upward.")]
    public float jumpHorizontalSpeed = 5f; // try 4–7
    public float minJumpDuration = 0.65f;
    public float maxJumpDuration = 1.8f;

    [Header("🐺 Pack Collapse & Lunge")]
    [Tooltip("Seconds to shrink ring from packChaseRadius down to near attack range.")]
    public float packCollapseSeconds = 3.0f;           // time to tighten the ring
    [Tooltip("Minimum radius factor of packChaseRadius when fully collapsed (e.g., 0.4 = 40%)")]
    public float packMinRadiusFactor = 0.45f;          // how tight the ring gets
    [Tooltip("Attack commit cone around an agent’s slot angle (deg).")]
    public float packAttackWindowDeg = 25f;            // commit window
    [Tooltip("Cooldown before this agent can lunge again.")]
    public float packLungeCooldown = 2.2f;             // per-wolf stagger
    [Tooltip("Dash speed when lunging.")]
    public float packLungeSpeed = 1.5f;                // multiplier on chaseSpeed
    [Tooltip("How long a lunge lasts max (sec).")]
    public float packLungeDuration = 0.9f;
    [Tooltip("Distance past player to aim during lunge (overshoot)")]
    public float packLungeOvershoot = 1.0f;

    [Header("🐺 Pack Orbiting")]
    public bool packOrbitingEnabled = true;
    [Tooltip("How fast slot angles drift around the player (deg/sec).")]
    public float packOrbitAngularSpeedDeg = 35f;
    [Tooltip("How often to repath toward the moving slot (sec).")]
    public float packRepathInterval = 0.25f;

    [Header("🐺 Rear Overtake Boost")]
    [Tooltip("Speed multiplier when a wolf is mostly behind the player.")]
    public float packRearBoost = 1.35f;
    [Tooltip("Rear wedge around 180° where boost applies.")]
    public float packRearAngleWindowDeg = 60f;


    [HideInInspector] public List<Transform> jumpSpots = new();
    [HideInInspector] public Transform currentJumpSpot;

    [HideInInspector] public bool jumpSessionActive;
    [HideInInspector] public float jumpSessionDeadline;

    [HideInInspector] public float nextPackAttackReadyTime; // per-wolf CD

    private bool _oneTimeInitDone;

    // runtime
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Color _originalTint;
    private string _colorProp = "_BaseColor";

    // Runtime cache
    [HideInInspector] public List<Transform> potentialTargets;

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

    [SerializeField] private string playerTag = "Player";
    private float _playerLookupCooldown = 0f;

    private bool TryResolvePlayer(bool force = false)
    {
        Transform candidate = null;

        // 1) Prefer PlayerLocator.Instance if it points to a live scene object
        var inst = PlayerLocator.Instance;
        if (inst != null && inst.gameObject.activeInHierarchy && inst.gameObject.scene.IsValid())
            candidate = inst;

        // 2) Otherwise pick the *closest active* Player-tagged object (not a prefab / inactive)
        if (candidate == null)
        {
            var gos = GameObject.FindGameObjectsWithTag(playerTag);
            Transform best = null;
            float bestSq = float.PositiveInfinity;

            foreach (var go in gos)
            {
                if (!go || !go.activeInHierarchy || !go.scene.IsValid()) continue;
                float sq = (go.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = go.transform; }
            }

            candidate = best;
        }

        if (candidate == null) return false;

        if (force || player == null || player != candidate)
            player = candidate;

        return true;
    }

    private void Awake()
    {
        StateMachine = new StateMachine();

        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
        health = GetComponent<Health>();
        player = PlayerLocator.Instance;

        // Gather all renderers (SkinnedMeshRenderer/MeshRenderer)
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();

        // Choose the right color property based on the first material we find
        if (_renderers.Length > 0 && _renderers[0] != null && _renderers[0].sharedMaterial != null)
        {
            _colorProp = _renderers[0].sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            _originalTint = _renderers[0].sharedMaterial.GetColor(_colorProp);
        }
    }


    private void Start()
    {
        if (_oneTimeInitDone) return;
        _oneTimeInitDone = true;

        // Agent defaults (once is enough)
        agent.autoBraking = enableAutoBraking;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.autoBraking = true;

        // Jump spots list can be built once
        if (aiType == AIType.AggressiveJumping)
        {
            var spots = GameObject.FindGameObjectsWithTag(jumpSpotTag);
            jumpSpots = new List<Transform>(spots.Select(s => s.transform));
        }
      

        if (health != null)
            health.onDeath.AddListener(OnDeath);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        TryResolvePlayer();

        // (Re)subscribe events for pooled objects
        if (health == null) health = GetComponent<Health>();
        if (health != null) health.onDeath.AddListener(OnDeath);

        // Bring this AI back to life in a safe way
        ResetRuntimeForSpawn();
    }

    private void OnDisable()
    {
        // During editor teardown, NavMesh/agents may already be invalid; do nothing.
        if (!Application.isPlaying) return;

        // Stop any running coroutines safely
        StopAllCoroutines();
        if (_spawnInitCo != null) { StopCoroutine(_spawnInitCo); _spawnInitCo = null; }

        // Unhook events (so pooling doesn’t stack listeners)
        if (health != null) health.onDeath.RemoveListener(OnDeath);

        // Touch agent only if truly valid
        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                // Safe only in this case:
                agent.ResetPath();
                agent.isStopped = true;
            }

            // Optional: disable to avoid stray callbacks during pooling
            // agent.enabled = false;
        }
    }


    private Coroutine _spawnInitCo;



    private void ClearAllPropertyBlocks()
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i]) _renderers[i].SetPropertyBlock(null);
    }

    private void SetTintOnAll(Color c)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r || !r.sharedMaterial) continue;

            // If some meshes use different color props, you can detect per-renderer:
            // var prop = r.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            // _mpb.Clear(); _mpb.SetColor(prop, c); r.SetPropertyBlock(_mpb);

            _mpb.Clear();
            _mpb.SetColor(_colorProp, c);
            r.SetPropertyBlock(_mpb);
        }
    }


    private bool EnsureAgentOnNavMesh(float sampleRadius = 6f)
    {
        if (agent == null) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null) return false;

        if (!agent.enabled) agent.enabled = true;

        // Already on mesh?
        if (agent.isOnNavMesh) return true;

        // Try to snap near current position
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, sampleRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private IEnumerator WaitForNavMeshAndFinishSpawn()
    {
        // Try for a short time (e.g. 0.5s) to find/snap to NavMesh
        float end = Time.time + 0.5f;
        while (Time.time < end)
        {
            if (EnsureAgentOnNavMesh(6f)) break;
            yield return null;
        }

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
        {
            // Give up safely — disable to avoid errors; idle anim so it doesn't look broken
            if (agent) agent.enabled = false;
            animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
            _spawnInitCo = null;
            yield break;
        }

        // Now it's safe to clear paths & proceed
        agent.ResetPath();
        FinishSpawnState();
        _spawnInitCo = null;
    }

    private void FinishSpawnState()
    {
        // Pick the correct initial state per spawn
        if (aiType == AIType.Companion)
            StateMachine.ChangeState(new AICompanionFollowState(this));
        else if (aiType == AIType.AggressiveJumping)
            StateMachine.ChangeState(new AIPerchRestState(this));  // perched until triggered
        else
            StateMachine.ChangeState(new AIWanderState(this));
    }

    private void ResetRuntimeForSpawn()
    {
        // Stop any old work (jump arcs, deferred inits, etc.)
        StopAllCoroutines();
        if (_spawnInitCo != null) { StopCoroutine(_spawnInitCo); _spawnInitCo = null; }

        // New “home” for this spawn
        spawnPosition = transform.position;

        // Reset timers/flags
        wanderTimer = wanderInterval;
        attackTimer = 0f;
        knockbackTimer = 0f;
        knockbackVector = Vector3.zero;

        wasProvoked = false; provokedTimer = 0f;
        currentChargeAttempts = 0;
        isChargeCooldownActive = false; chargeCooldownTimer = 0f;

        jumpSessionActive = false; jumpSessionDeadline = 0f; currentJumpSpot = null;
        nextPackAttackReadyTime = 0f;

        // Agent sanity (configure, but don't touch pathing until on NavMesh)
        if (agent == null) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            agent.updateRotation = true;
            agent.autoBraking = enableAutoBraking;
            agent.avoidancePriority = Random.Range(30, 70);
            agent.stoppingDistance = stopDistance;
            agent.speed = wanderSpeed;
            // DO NOT set isStopped or ResetPath yet.
        }

        // Health: full restore
        if (health == null) health = GetComponent<Health>();
        if (health != null) health.ResetHealth();

        // Visuals — restore tint & clear overrides
        ClearAllPropertyBlocks();

        // Companion retarget cache (optional)
        if (aiType == AIType.Companion)
        {
            var objs = GameObject.FindGameObjectsWithTag(targetTag);
            potentialTargets = new List<Transform>(objs.Select(o => o.transform));
        }

        // Ensure we’re on the NavMesh before calling ResetPath / setting state
        if (EnsureAgentOnNavMesh(6f))
        {
            agent.isStopped = false;  // safe now
            agent.ResetPath();        // safe now
            FinishSpawnState();       // choose initial state and (if needed) SetDestination
        }
        else
        {
            // Defer finishing until we can get onto the mesh
            _spawnInitCo = StartCoroutine(WaitForNavMeshAndFinishSpawn());
        }
    }


    public Transform GetClosestAnimalTarget()
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (var t in potentialTargets)
        {
            if (t == null) continue;

            // Skip dead animals
            if (t.TryGetComponent<Health>(out var h) && h.IsDead)
                continue;

            float d = Vector3.Distance(transform.position, t.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return (best != null && bestDist <= companionDetectionRange) ? best : null;
    }

    public IEnumerator DespawnOrDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        var po = GetComponent<PooledObject>();
        if (po) po.Despawn(); else Destroy(gameObject);
    }

    public void Death()
    {
        StartCoroutine(DespawnOrDestroyAfter(0.25f));
    }

    public Transform GetNearestJumpSpot(Vector3 from)
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;
        foreach (var t in jumpSpots)
        {
            if (t == null) continue;
            float d = Vector3.SqrMagnitude(t.position - from);
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }

    public Vector3 GetJumpSpotFoot(Transform spot)
    {
        // NavMesh point near the JumpSpot to stand before jumping up
        return SampleOnNavmesh(spot.position, jumpLandingProbeRadius * 4f);
    }

    public Vector3 SampleOnNavmesh(Vector3 desired, float maxDistance)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        if (NavMesh.SamplePosition(desired, out hit, maxDistance * 3f, NavMesh.AllAreas))
            return hit.position;
        if (NavMesh.SamplePosition(transform.position, out hit, maxDistance * 3f, NavMesh.AllAreas))
            return hit.position;

        // last resort: keep the current agent position if it’s valid, else leave as-is
        if (agent.enabled && agent.isOnNavMesh)
            return agent.nextPosition;
        return transform.position;
    }

    public IEnumerator JumpArc(Vector3 from, Vector3 to, float height, float duration, bool landOnNavMesh = true)
    {
        // Disable while we animate the arc
        if (agent.enabled) agent.enabled = false;

        // Face jump direction
        Vector3 flatDir = (to - from); flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flatDir.normalized);

        // Animate parabola
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            // smooth horizontal/vertical blend
            Vector3 pos = Vector3.Lerp(from, to, t);
            // nice hump (0..π)
            pos.y = Mathf.Lerp(from.y, to.y, t) + height * Mathf.Sin(Mathf.PI * t);

            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!landOnNavMesh)
        {
            // Stay exactly at 'to' (perch), keep agent disabled
            yield return null;
            yield break;
        }


        // Find a safe landing on the NavMesh (spiral search)
        Vector3 land;
        if (!TryFindNavMeshPoint(to, jumpLandingProbeRadius, jumpLandingProbeRadius * 6f, out land))
        {
            // last resort: around current position
            TryFindNavMeshPoint(transform.position, jumpLandingProbeRadius * 6f, jumpLandingProbeRadius * 12f, out land);
        }

        // Re-enable & warp; give it one frame to register on the mesh
        agent.enabled = true;
#if UNITY_2022_1_OR_NEWER
        bool warped = agent.Warp(land);
#else
    bool warped = true; agent.Warp(land);
#endif
        if (!warped)
        {
            // if warp somehow failed, keep trying for a couple frames
            for (int i = 0; i < 5 && !agent.isOnNavMesh; i++)
            {
                if (NavMesh.SamplePosition(land, out var hit, jumpLandingProbeRadius * 8f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                yield return null;
            }
        }

        // Let the agent settle on the mesh this frame
        yield return null;
    }

    // Spiral/ring search so we never return off-mesh
    private bool TryFindNavMeshPoint(Vector3 center, float startRadius, float maxRadius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(center, out var hit, startRadius, NavMesh.AllAreas))
        {
            result = hit.position; return true;
        }

        float r = Mathf.Max(0.5f, startRadius);
        while (r <= maxRadius)
        {
            // 12 points around the ring
            for (int i = 0; i < 12; i++)
            {
                float ang = i * 30f;
                Vector3 p = center + Quaternion.Euler(0f, ang, 0f) * (Vector3.forward * r);
                if (NavMesh.SamplePosition(p, out hit, 1.0f, NavMesh.AllAreas))
                {
                    result = hit.position; return true;
                }
            }
            r *= 1.6f;
        }

        result = center;
        return false;
    }


    public Vector3 GetPerchFootPointForReturn()
    {
        // Prefer a designer perch if we have one
        Transform perch = currentJumpSpot ?? GetNearestJumpSpot(spawnPosition);
        if (perch != null)
        {
            // Push outward from the perch toward where we came from
            Vector3 outward = (transform.position - perch.position).Flat();
            if (outward.sqrMagnitude < 0.01f) outward = transform.forward;
            outward.Normalize();

            // Try a few radii; prefer a point lower than the perch (so we don’t “climb up” first)
            float[] radii = { jumpSpotApproachRadius * 1.2f, jumpSpotApproachRadius * 1.8f, jumpSpotApproachRadius * 2.5f, jumpSpotApproachRadius * 3.5f };
            Vector3 fallback = Vector3.zero;
            foreach (float r in radii)
            {
                Vector3 candidate = perch.position + outward * r;
                if (NavMesh.SamplePosition(candidate, out var hit, 1.0f, NavMesh.AllAreas))
                {
                    if (hit.position.y <= perch.position.y - 0.1f) // prefer ground below the perch
                        return hit.position;
                    fallback = hit.position; // keep something usable
                }
            }

            // Fallback: generic foot near perch
            if (fallback != Vector3.zero) return fallback;
            return GetJumpSpotFoot(perch);
        }

        // No perch transform? sample around spawn
        return SampleOnNavmesh(spawnPosition, jumpLandingProbeRadius * 4f);
    }



    private void Update()
    {

        var inst = PlayerLocator.Instance;
        if (inst != null && inst != player && inst.gameObject.activeInHierarchy && inst.gameObject.scene.IsValid())
            player = inst;


        // Keep trying to bind to the player for pre-placed AIs
        if (player == null)
        {
            _playerLookupCooldown -= Time.deltaTime;
            if (_playerLookupCooldown <= 0f)
            {
                TryResolvePlayer();
                _playerLookupCooldown = 1f; // try once per second
            }
        }

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

        if (StateMachine.CurrentState is AIChargeState)
        {
            StateMachine.Update();
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

        float distance = DistanceToPlayer();

        if (aiType == AIType.AggressiveJumping
            && jumpSessionActive
            && Time.time >= jumpSessionDeadline)
        {
            var s = StateMachine.CurrentState;
            if (!(s is AIJumpReturnHomeState) && !(s is AIPerchRestState))
            {
                StateMachine.ChangeState(new AIJumpReturnHomeState(this));
                return;
            }
        }


        // --- Only allow jump re-triggers when NOT in a session ---
        // New (only trigger from perch state)
        if (!jumpSessionActive && StateMachine.CurrentState is AIPerchRestState)
        {
            if (aiType == AIType.AggressiveJumping && distance <= detectionRange)
            {
                StateMachine.ChangeState(new AIJumpAttackState(this));
                return;
            }
        }

        // 6) Otherwise, let the normal state machine run
        StateMachine.Update();

    }

    public bool HasCompletePath(Vector3 target)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;
        var path = new NavMeshPath();
        agent.CalculatePath(target, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }

    // Returns the farthest point toward 'target' that has a complete path (on this island),
    // stepping back along the segment. Returns 'current' if nothing works.
    public Vector3 FindReachableToward(Vector3 target, int steps = 6)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return transform.position;
        Vector3 start = agent.transform.position;
        for (int i = steps; i >= 1; i--)
        {
            float t = (float)i / steps;                    // 1.0 .. 1/steps
            Vector3 probe = Vector3.Lerp(start, target, t);
            if (NavMesh.SamplePosition(probe, out var hit, jumpLandingProbeRadius * 2f, NavMesh.AllAreas))
            {
                if (HasCompletePath(hit.position)) return hit.position;
            }
        }
        return start;
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

        if (_renderers != null)
            TriggerFlashRed();

        knockbackVector = direction;
        knockbackTimer = knockbackDuration;
        agent.ResetPath();
        //animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);
    }

    private Coroutine _flashCo;

    public void TriggerFlashRed(float seconds = 0.2f)
    {
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(FlashRedAll(seconds));
    }

    private IEnumerator FlashRedAll(float seconds)
    {
        if (_renderers == null || _renderers.Length == 0) yield break;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // 1) Tint all renderers red
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r || !r.sharedMaterial) continue;

            // Pick the right color property per material
            string prop = r.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor"
                        : r.sharedMaterial.HasProperty("_Color") ? "_Color"
                        : null;

            if (prop == null) continue;

            _mpb.Clear();
            _mpb.SetColor(prop, Color.red);
            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(seconds);

        // 2) Clear all overrides so materials go back to their own tints
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i]) _renderers[i].SetPropertyBlock(null);

        _flashCo = null;
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
        // ——— Timer to go back to wandering ———
        restTimer -= Time.deltaTime;

        float dist = ai.DistanceToPlayer();

        Debug.Log("DistanceToPlayer" + dist);

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
            if (!ai.isChargeCooldownActive && dist <= ai.detectionRange)
            {
                ai.agent.isStopped = true;
                ai.StateMachine.ChangeState(new AIWindupState(ai));
                return;
            }
        }
        else if(ai.aiType == AIType.AggressiveType3)
        {
            if (dist <= ai.detectionRange)
            {
                // start pack call
                ai.StateMachine.ChangeState(new AIPackCallState(ai));
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

        if(ai.aiType ==AIType.AggressiveType3)
        {
            if (distanceToPlayer <= ai.detectionRange)
            {
                // start pack call
                ai.StateMachine.ChangeState(new AIPackCallState(ai));
                return;
            }
        }

        if (ai.aiType == AIType.AggressiveType1)
        {
            if (!ai.isChargeCooldownActive && distanceToPlayer <= ai.detectionRange)
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
        ai.Death();
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
    private Vector3 overshootPoint;
    private bool hasHitPlayer;

    // How far past the player we want to go (in units)
    private float overshootDistance => ai.chargeSpeed * ai.chargeDuration * 1f;

    public AIChargeState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        // 1) Increase attempt count & set up agent
        ai.currentChargeAttempts++;
        ai.agent.speed = ai.chargeSpeed;
        ai.agent.ResetPath();
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
        ai.agent.updateRotation = false;  // we'll rotate ourselves

        timer = ai.chargeDuration;
        hasHitPlayer = false;

        // 2) Compute a one‐time direction & overshoot point
        if (ai.player != null)
        {
            chargeDirection = (ai.player.position - ai.transform.position)
                                .Flat()                   // extension method to zero Y
                                .normalized;
            // target a point just past the player's current pos:
            overshootPoint = ai.player.position + chargeDirection * overshootDistance;
        }
        else
        {
            chargeDirection = ai.transform.forward;
            overshootPoint = ai.transform.position + chargeDirection * overshootDistance;
        }

        ai.transform.rotation = Quaternion.LookRotation(chargeDirection);

        // 3) Tell the NavMeshAgent to run full‐speed toward that overshoot point
        ai.agent.stoppingDistance = 0f;
        ai.agent.SetDestination(overshootPoint);
    }

    public void Update()
    {
        timer -= Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(chargeDirection);
        ai.transform.rotation = Quaternion.RotateTowards(
            ai.transform.rotation,
            targetRot,
            ai.companionRotationLerp * Time.deltaTime
        );


        // 2) Collision check on the way through
        if (!hasHitPlayer && ai.player != null)
        {
            float distToPlayer = Vector3.Distance(ai.transform.position, ai.player.position);
            if (distToPlayer <= ai.chargeDamageRadius)
            {
                if (ai.player.TryGetComponent<Health>(out var hp))
                    hp.TakeDamage(ai.chargeDamage);
                hasHitPlayer = true;
            }
        }

        // 3) Termination: either we've timed out, reached the overshoot, or hit the player
        bool reachedOvershoot = !ai.agent.pathPending
                                && ai.agent.remainingDistance <= 0.1f;
        if (timer <= 0f || reachedOvershoot || hasHitPlayer)
        {
            // If we still have more charges available and close enough, we can wind up again...
            if (ai.currentChargeAttempts < ai.maxChargeAttempts
                && Vector3.Distance(ai.transform.position, ai.player.position) <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIWindupState(ai));
            }
            // Otherwise fall back to a normal chase
            else if (Vector3.Distance(ai.transform.position, ai.player.position) <= ai.detectionRange)
            {
                ai.StateMachine.ChangeState(new AIChaseState(ai));
            }
            else
            {
                ai.StateMachine.ChangeState(new AIRestState(ai));
            }
        }
    }

    public void Exit()
    {
        // Restore auto‐rotation so other states can use it
        ai.agent.updateRotation = true;
    }
}

public static class Vector3Extensions
{
    public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);
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

        // Detection → Chase
        if (ai.DistanceToPlayer() <= ai.maxChaseDistance)
        {
            var prey = ai.GetClosestAnimalTarget();
            if (prey != null)
            {
                ai.StateMachine.ChangeState(new CompanionChaseState(ai, prey));
                return;
            }
        }

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

        if (ai.DistanceToPlayer() <= ai.maxChaseDistance)
        {
            var prey = ai.GetClosestAnimalTarget();
            if (prey != null)
            {
                ai.StateMachine.ChangeState(new CompanionChaseState(ai, prey));
                return;
            }
        }

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


public class CompanionChaseState : IState
{
    private CuteAnimalAI ai;
    private Transform target;

    public CompanionChaseState(CuteAnimalAI ai, Transform target)
    {
        this.ai = ai;
        this.target = target;
    }

    public void Enter()
    {
        ai.agent.speed = ai.chaseSpeed;
        ai.agent.stoppingDistance = ai.attackRange;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
    }

    public void Update()
    {
        if (target == null
        || (target.TryGetComponent<Health>(out var ht) && ht.IsDead)
        || ai.DistanceToPlayer() > ai.maxChaseDistance)
        {
            ai.StateMachine.ChangeState(new AICompanionFollowState(ai));
            return;
        }

        if (target == null)
        {
            ai.StateMachine.ChangeState(new AICompanionFollowState(ai));
            return;
        }

        float dist = Vector3.Distance(ai.transform.position, target.position);

        // Too far? drop chase
        if (dist > ai.companionDetectionRange)
        {
            ai.StateMachine.ChangeState(new AICompanionFollowState(ai));
            return;
        }

        // In attack range? transition to attack (we’ll build this next)
        if (dist <= ai.attackRange)
        {
            ai.StateMachine.ChangeState(new CompanionAttackState(ai, target));
            return;
        }

        // Otherwise keep chasing
        ai.agent.SetDestination(target.position);
        ai.SmoothRotate(target.position - ai.transform.position, ai.companionRotationLerp);
    }

    public void Exit() { }
}

public class CompanionAttackState : IState
{
    private CuteAnimalAI ai;
    private Transform target;
    private float timer;

    public CompanionAttackState(CuteAnimalAI ai, Transform target)
    {
        this.ai = ai;
        this.target = target;
    }

    public void Enter()
    {
        // Stop moving and turn off NavMeshAgent auto-rotation
        ai.agent.ResetPath();
        ai.agent.updateRotation = false;

        // Play attack anim & deal damage immediately
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);
        if (target != null && target.TryGetComponent<Health>(out var h))
            h.TakeDamage(ai.attackDamage);

        timer = ai.attackCooldown;
    }

    public void Update()
    {

        if (target == null
       || (target.TryGetComponent<Health>(out var h) && h.IsDead)
       || ai.DistanceToPlayer() > ai.maxChaseDistance)
        {
            ai.StateMachine.ChangeState(new AICompanionFollowState(ai));
            return;
        }

        // Always smoothly face the target (if it still exists)
        if (target != null)
        {
            var dir = (target.position - ai.transform.position).normalized;
            ai.SmoothRotate(dir, ai.companionRotationLerp);
        }

        // Bail if target gone or too far from player
        if (target == null ||
            ai.DistanceToPlayer() > ai.maxChaseDistance)
        {
            ExitAndFollow();
            return;
        }

        float dist = Vector3.Distance(ai.transform.position, target.position);

        // If out of melee but still near, go back to chase
        if (dist > ai.attackRange && dist <= ai.companionDetectionRange)
        {
            ai.StateMachine.ChangeState(new CompanionChaseState(ai, target));
            return;
        }

        // Cooldown countdown
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (dist <= ai.attackRange)
                ai.StateMachine.ChangeState(new CompanionAttackState(ai, target));
            else if (dist <= ai.companionDetectionRange)
                ai.StateMachine.ChangeState(new CompanionChaseState(ai, target));
            else
                ExitAndFollow();
        }
    }

    public void Exit()
    {
        // Re-enable NavMeshAgent rotation so later states can use it
        ai.agent.updateRotation = true;
    }

    private void ExitAndFollow()
    {
        Exit();
        ai.StateMachine.ChangeState(new AICompanionFollowState(ai));
    }
}

public class AIPackCallState : IState
{
    private CuteAnimalAI ai;
    private float timer;

    public AIPackCallState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        timer = ai.coordinationTime;
        // Broadcast to neighbors:
        Collider[] hits = Physics.OverlapSphere(ai.transform.position, ai.packCallRadius);
        foreach (var c in hits)
        {
            if (c.TryGetComponent<CuteAnimalAI>(out var other)
             && other.animalType == ai.animalType
             && other.aiType == AIType.AggressiveType3
             && !(other.StateMachine.CurrentState is AIPackCoordinateState))
            {
                other.StateMachine.ChangeState(new AIPackCoordinateState(other, ai));
            }
        }
        // Self also coordinates:
        ai.StateMachine.ChangeState(new AIPackCoordinateState(ai, ai));
    }

    public void Update() { /* instantaneous—Enter does all work */ }

    public void Exit() { }
}

public class AIPackCoordinateState : IState
{
    private CuteAnimalAI ai;
    private CuteAnimalAI caller;
    private float timer;
    private List<CuteAnimalAI> pack = new();

    public AIPackCoordinateState(CuteAnimalAI ai, CuteAnimalAI caller)
    {
        this.ai = ai;
        this.caller = caller;
    }

    public void Enter()
    {
        timer = ai.coordinationTime;
        // Build pack list (max N)
        var hits = Physics.OverlapSphere(caller.transform.position, ai.packCallRadius);
        foreach (var c in hits)
        {
            if (pack.Count >= ai.maxPackSize) break;
            if (c.TryGetComponent<CuteAnimalAI>(out var other)
             && other.animalType == ai.animalType
             && other.aiType == AIType.AggressiveType3)
            {
                pack.Add(other);
            }
        }
        // Face toward player:
        ai.agent.ResetPath();
        ai.transform.LookAt(caller.player.position.Flat());
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
    }

    public void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        // 2) Gather your pack members (including yourself)
        var pack = GameObject
            .FindGameObjectsWithTag(ai.targetTag)          // or however you cache your pack
            .Select(go => go.GetComponent<CuteAnimalAI>())
            .Where(a => a != null && a.aiType == AIType.AggressiveType3)
            .ToList();

        // 3) Figure out your index in the pack
        int total = pack.Count;
        int idx = pack.IndexOf(ai);
        if (idx < 0) idx = 0; // fallback

        // 4) Transition into the coordinated chase, handing off your angle slot
        ai.StateMachine.ChangeState(
            new AIPackChaseState(ai, ai.player, idx, total)
        );
    }

    public void Exit() { }
}

public class AIPackAttackState : IState
{
    private CuteAnimalAI ai;
    private Vector3 flankDirection;

    public AIPackAttackState(CuteAnimalAI ai, Vector3 flankDirection)
    {
        this.ai = ai;
        this.flankDirection = flankDirection;
    }

    public void Enter()
    {
        ai.agent.speed = ai.chaseSpeed;
        ai.agent.stoppingDistance = ai.attackRange;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        // Compute flank target point just off to the side of player
        Vector3 sideOffset = Vector3.Cross(Vector3.up, (ai.player.position - ai.transform.position).Flat().normalized);
        Vector3 target = ai.player.position + flankDirection + sideOffset * 2f;
        ai.agent.SetDestination(target);
    }

    public void Update()
    {
        // Standard chase→attack logic, but each will approach from their assigned flank
        float dist = Vector3.Distance(ai.transform.position, ai.player.position);
        if (dist <= ai.attackRange)
            ai.StateMachine.ChangeState(new AIAttackState(ai));
        else
            ai.agent.SetDestination(ai.player.position);
    }

    public void Exit() { }
}

public class AIPackChaseState : IState
{
    private CuteAnimalAI ai;
    private Transform player;
    private int index, total;
    private Vector3 interceptPoint;
    float lastAngleDeg;

    private float engageStartTime;
    private float orbitAngleDeg;
    private bool orbitClockwise;
    private float nextRepathTime;
    private float baseSpeed;
    private float burstTimer = 0f;
    private float currentRadius;

    private const float burstDuration = 1f;            // how long the burst lasts
    private const float burstMultiplier = 1.5f;



    public AIPackChaseState(CuteAnimalAI ai, Transform player, int index, int total)
    {
        this.ai = ai;
        this.player = player;
        this.index = index;
        this.total = total;
    }

    private float CollapseFactor()
    {
        float t = (Time.time - engageStartTime) / Mathf.Max(0.01f, ai.packCollapseSeconds);
        t = Mathf.Clamp01(t);
        float f = Mathf.Lerp(1f, ai.packMinRadiusFactor, t);
        currentRadius = ai.packChaseRadius * f;
        return currentRadius;
    }

    private bool AngleWithinAttackWindow()
    {
        // 0° means directly in front of player; we used lastAngleDeg for our slot
        return Mathf.Abs(lastAngleDeg) <= ai.packAttackWindowDeg;
    }

    public void Enter()
    {
        engageStartTime = Time.time;
        baseSpeed = ai.chaseSpeed;
        ai.agent.stoppingDistance = 0f;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        // Start from assigned slot angle
        ComputeIntercept();                 // sets lastAngleDeg, interceptPoint
        orbitAngleDeg = lastAngleDeg;

        // Randomize spin direction a bit so the pack doesn’t all choose the same
        orbitClockwise = (ai.GetInstanceID() & 1) == 0;

        // Stagger repaths so they don't sync
        nextRepathTime = Time.time + Random.Range(0f, ai.packRepathInterval * 0.5f);

        // Optional initial burst you already had
        bool isRearFlanker = (total >= 4 && index == 0);
        bool isDirectChaser = Mathf.Abs(lastAngleDeg) < 10f;
        if (isRearFlanker || isDirectChaser) { ai.agent.speed = baseSpeed * 1.5f; burstTimer = 1f; }
        else ai.agent.speed = baseSpeed;

        ai.agent.SetDestination(interceptPoint);
    }


    public void Update()
    {
        if (burstTimer > 0f) { burstTimer -= Time.deltaTime; if (burstTimer <= 0f) ai.agent.speed = baseSpeed; }

        // Make the slot angle drift → produces circling
        if (ai.packOrbitingEnabled)
        {
            float dir = orbitClockwise ? 1f : -1f;
            orbitAngleDeg += dir * ai.packOrbitAngularSpeedDeg * Time.deltaTime;
            orbitAngleDeg = Mathf.Repeat(orbitAngleDeg + 360f, 360f);
        }

        // Periodic repath (also catches arrival or big player motion)
        bool needRepath = Time.time >= nextRepathTime
                       || (!ai.agent.pathPending && ai.agent.remainingDistance <= 0.25f)
                       || (!ai.agent.pathPending && (player.position - interceptPoint).sqrMagnitude > 1.2f * 1.2f);

        if (needRepath)
        {
            // Use orbiting angle if enabled, otherwise the static slot
            ComputeIntercept(ai.packOrbitingEnabled ? (float?)orbitAngleDeg : null);
            ai.agent.SetDestination(interceptPoint);
            nextRepathTime = Time.time + ai.packRepathInterval;
        }

        // Rear overtake boost: wolves behind the player get a speed bump
        Vector3 toAgent = (ai.transform.position - player.position).Flat().normalized;
        float dot = Vector3.Dot(player.forward.Flat().normalized, toAgent); // < 0 means agent is behind player
        bool inRearWedge = Mathf.Abs(Mathf.DeltaAngle(orbitAngleDeg, 180f)) <= ai.packRearAngleWindowDeg;
        bool isBehind = dot < 0f || inRearWedge;

        float targetSpeed = isBehind ? baseSpeed * ai.packRearBoost : baseSpeed;
        ai.agent.speed = Mathf.Lerp(ai.agent.speed, targetSpeed, 8f * Time.deltaTime); // smooth

        // If close enough, bite; otherwise allow your lunge commit logic as before
        float distToPlayer = Vector3.Distance(ai.transform.position, player.position);
        if (distToPlayer <= ai.attackRange * 1.05f)
        {
            ai.StateMachine.ChangeState(new AIAttackState(ai));
            return;
        }

        bool canLunge = Time.time >= ai.nextPackAttackReadyTime;
        bool nearEnough = distToPlayer <= ai.attackRange * 1.8f;
        bool inWindow = Mathf.Abs(lastAngleDeg) <= ai.packAttackWindowDeg; // relies on ComputeIntercept setting lastAngleDeg

        if ((inWindow && canLunge) || nearEnough)
        {
            ai.nextPackAttackReadyTime = Time.time + ai.packLungeCooldown;
            ai.StateMachine.ChangeState(new AIPackLungeState(ai));
            return;
        }
    }

    public void Exit() { }

    private void ComputeIntercept(float? angleOverride = null)
    {
        if (total <= 0) { interceptPoint = player.position; return; }
        index = Mathf.Clamp(index, 0, total - 1);

        float angleDeg;
        if (angleOverride.HasValue) angleDeg = angleOverride.Value;
        else
        {
            // your slot logic
            if (total >= 4 && index == 0) angleDeg = 180f;
            else
            {
                float slots = total >= 4 ? total - 1 : total;
                float sectorSize = 360f / (total >= 4 ? slots : total);
                int slotIndex = total >= 4 ? index - 1 : index;
                float startOffset = total >= 4 ? -90f : 0f;
                angleDeg = startOffset + sectorSize * slotIndex;
            }
        }

        lastAngleDeg = angleDeg;  // used by attack window check

        float r = CollapseFactor();
        Vector3 dir = Quaternion.Euler(0, angleDeg, 0) * Vector3.forward;
        Vector3 rawTarget = player.position + dir * r;

        if (player.TryGetComponent<Rigidbody>(out var rb))
            rawTarget += rb.velocity.Flat() * 0.25f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(rawTarget, out hit, 2f, NavMesh.AllAreas))
            interceptPoint = hit.position;
        else
            interceptPoint = player.position;
    }


}



public class AIPackLungeState : IState
{
    private CuteAnimalAI ai;
    private float timer;
    private Vector3 targetPoint;
    private bool hit;

    public AIPackLungeState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);
        ai.agent.stoppingDistance = 0f;
        ai.agent.speed = ai.chaseSpeed * ai.packLungeSpeed;

        // aim slightly past the player along our current facing
        Vector3 dir = (ai.player.position - ai.transform.position).Flat().normalized;
        targetPoint = ai.player.position + dir * ai.packLungeOvershoot;

        ai.agent.SetDestination(targetPoint);
        timer = ai.packLungeDuration;
        hit = false;
    }

    public void Update()
    {
        timer -= Time.deltaTime;

        // hit check
        if (!hit && Vector3.Distance(ai.transform.position, ai.player.position) <= ai.attackRange)
        {
            if (ai.player.TryGetComponent<Health>(out var hp)) hp.TakeDamage(ai.attackDamage);
            hit = true;
        }

        // end conditions: time up or reached target
        bool arrived = !ai.agent.pathPending && ai.agent.remainingDistance <= 0.1f;
        if (timer <= 0f || arrived || hit)
        {
            // if still close, do a quick bite; else resume chase
            if (Vector3.Distance(ai.transform.position, ai.player.position) <= ai.attackRange * 0.95f)
                ai.StateMachine.ChangeState(new AIAttackState(ai));
            else
                ai.StateMachine.ChangeState(new AIPackChaseState(ai, ai.player, 0, 0)); // idx/total will be rebuilt
            return;
        }
    }

    public void Exit() { }
}


// Aggressive jumper: perch -> jump down -> chase for a while -> jump back to perch
public class AIJumpAttackState : IState
{
    private CuteAnimalAI ai;
    public AIJumpAttackState(CuteAnimalAI ai) { this.ai = ai; }
    private Coroutine jumpRoutine;

    public void Enter()
    {
        // ✅ Arm the session immediately so no other triggers fire mid-air
        ai.jumpSessionActive = true;
        ai.jumpSessionDeadline = Time.time + ai.chaseDuration;

        ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);

        Vector3 start = ai.transform.position;
        Vector3 desiredLand = ai.player ? ai.player.position : start;
        Vector3 land = ai.SampleOnNavmesh(desiredLand, ai.jumpLandingProbeRadius);

        // Optional: stop any previous jump routine just in case
        if (jumpRoutine != null) ai.StopCoroutine(jumpRoutine);
        jumpRoutine = ai.StartCoroutine(JumpAndChase(start, land));
    }

    private IEnumerator JumpAndChase(Vector3 start, Vector3 land)
    {
        // Jump down
        yield return ai.StartCoroutine(ai.JumpArc(start, land, ai.jumpHeight, ai.jumpDuration));

        // Give the agent a few frames to settle onto the mesh
        int settleTries = 10;
        while (settleTries-- > 0 && (!ai.agent.enabled || !ai.agent.isOnNavMesh))
        {
            if (NavMesh.SamplePosition(ai.transform.position, out var snap, ai.jumpLandingProbeRadius * 8f, NavMesh.AllAreas))
                ai.agent.Warp(snap.position);

            yield return null; // <-- important: wait a frame after Warp
        }

        if (!ai.agent.enabled || !ai.agent.isOnNavMesh)
        {
            ai.StateMachine.ChangeState(new AIJumpReturnHomeState(ai));
            yield break;
        }

        ai.jumpSessionActive = true;
        ai.jumpSessionDeadline = Time.time + ai.chaseDuration;

        // Chase for a while
        ai.agent.speed = ai.chaseSpeed;

        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        float timer = ai.chaseDuration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            // If state changed while we were mid-coroutine, stop cleanly
            if (!(ai.StateMachine.CurrentState is AIJumpAttackState))
                yield break;

            if (ai.player == null) break;

            // If not safely on the mesh, try to snap and skip this frame
            if (!ai.agent.enabled || !ai.agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(ai.transform.position, out var snap, 5f, NavMesh.AllAreas))
                    ai.agent.Warp(snap.position);

                yield return null; // <-- skip SetDestination this frame
                continue;
            }

            // Final guard before calling SetDestination
            ai.agent.SetDestination(ai.player.position);

            if (Vector3.Distance(ai.transform.position, ai.player.position) <= ai.attackRange)
            {
                ai.StateMachine.ChangeState(new AIAttackState(ai));
                yield break;
            }

            yield return null;
        }

        ai.StateMachine.ChangeState(new AIJumpReturnHomeState(ai));
    }


    public void Update() { }
    public void Exit()
    {
        if (jumpRoutine != null) ai.StopCoroutine(jumpRoutine);
        jumpRoutine = null;
    }
}

public class AIJumpReturnHomeState : IState
{
    private CuteAnimalAI ai;
    private Vector3 footPoint;
    private bool startedJump;
    private float noProgressTimer;

    private const float JumpTriggerRadius = 1.5f;   // a bit looser
    private const float NoProgressTimeout = 1.25f;  // seconds

    public AIJumpReturnHomeState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        ai.jumpSessionActive = true;

        footPoint = ai.GetPerchFootPointForReturn();

        if (!ai.agent.enabled) ai.agent.enabled = true;
        ai.agent.updateRotation = true;
        ai.agent.isStopped = false;
        ai.agent.stoppingDistance = 0f;
        ai.agent.speed = ai.chaseSpeed;

        if (!ai.agent.isOnNavMesh &&
            NavMesh.SamplePosition(ai.transform.position, out var snap, ai.jumpLandingProbeRadius * 4f, NavMesh.AllAreas))
        {
            ai.agent.Warp(snap.position);
        }

        ai.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        // ✅ If there’s no complete path to the foot, try a reachable point along the way
        if (!ai.HasCompletePath(footPoint))
        {
            Vector3 reachable = ai.FindReachableToward(footPoint);
            if ((reachable - ai.transform.position).sqrMagnitude > 0.2f)
                ai.agent.SetDestination(reachable);
            else
            {
                // Nothing reachable → jump straight back to perch from here
                startedJump = true;
                ai.agent.ResetPath();
                ai.StartCoroutine(JumpUpThenRest());
                return;
            }
        }
        else
        {
            ai.agent.SetDestination(footPoint);
        }

        noProgressTimer = NoProgressTimeout;
        startedJump = false;
    }

    public void Update()
    {
        if (startedJump) return;

        if (!ai.agent.enabled) { ai.agent.enabled = true; return; }
        if (!ai.agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(ai.transform.position, out var snap, ai.jumpLandingProbeRadius * 4f, NavMesh.AllAreas))
                ai.agent.Warp(snap.position);
            return;
        }

        // If our path is invalid/partial at any point → try reachable, else jump up
        if (!ai.agent.hasPath || ai.agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Vector3 reachable = ai.FindReachableToward(footPoint);
            if ((reachable - ai.transform.position).sqrMagnitude > 0.2f)
                ai.agent.SetDestination(reachable);
            else
            {
                startedJump = true;
                ai.agent.ResetPath();
                ai.StartCoroutine(JumpUpThenRest());
                return;
            }
        }

        // “No progress” fail-safe: velocity near zero while far from target
        if (ai.agent.remainingDistance > 2f && ai.agent.velocity.sqrMagnitude < 0.01f)
        {
            noProgressTimer -= Time.deltaTime;
            if (noProgressTimer <= 0f)
            {
                // Try a fresh reachable point; if still nothing, jump up
                Vector3 reachable = ai.FindReachableToward(footPoint);
                if (ai.HasCompletePath(reachable))
                {
                    ai.agent.SetDestination(reachable);
                    noProgressTimer = NoProgressTimeout;
                }
                else
                {
                    startedJump = true;
                    ai.agent.ResetPath();
                    ai.StartCoroutine(JumpUpThenRest());
                    return;
                }
            }
        }
        else
        {
            noProgressTimer = NoProgressTimeout;
        }

        // Normal “close to foot” trigger
        float dist = Vector3.Distance(ai.transform.position, footPoint);
        if (dist <= JumpTriggerRadius || (!ai.agent.pathPending && ai.agent.remainingDistance <= JumpTriggerRadius))
        {
            startedJump = true;
            ai.agent.ResetPath();
            ai.StartCoroutine(JumpUpThenRest());
        }
    }

    private IEnumerator JumpUpThenRest()
    {
        Vector3 from = ai.transform.position;
        Vector3 to = ai.spawnPosition;

        if (ai.agent.enabled) ai.agent.enabled = false;
        yield return ai.StartCoroutine(ai.JumpArc(from, to, ai.jumpHeight, ai.jumpDuration, landOnNavMesh: false));

        ai.StateMachine.ChangeState(new AIPerchRestState(ai));
    }

    public void Exit() { }
}




// Perched, chill until player gets close again.
public class AIPerchRestState : IState
{
    private CuteAnimalAI ai;
    public AIPerchRestState(CuteAnimalAI ai) { this.ai = ai; }

    public void Enter()
    {
        // Optional: if your perch is off NavMesh, you can disable the agent here.
        // But since we warped onto the mesh at the foot of the perch above, we keep it enabled.
        if (ai.agent.enabled) ai.agent.ResetPath();
        ai.agent.enabled = false;
        ai.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);
        ai.jumpSessionActive = false;

    }

    public void Update()
    {
        float dist = ai.DistanceToPlayer();

        //if (ai.aiType == CuteAnimalAI.AIType.AggressiveJumping)
        //{
        //    if (dist <= ai.detectionRange)
        //    {
        //        ai.StateMachine.ChangeState(new AIJumpAttackState(ai));
        //        return;
        //    }
        //}
    }

    public void Exit() {  }
}










