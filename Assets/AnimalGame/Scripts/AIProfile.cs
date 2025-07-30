using UnityEngine;

// ====================================
// AIProfile: ScriptableObject Data Container
// ====================================
[CreateAssetMenu(fileName = "AIProfile", menuName = "AI/Profiles/AIProfile", order = 1)]
public class AIProfile : ScriptableObject
{
    [Header("⚙️ General AI Settings")]
    public CuteAnimalAI.AIType aiType = CuteAnimalAI.AIType.Passive;
    public CuteAnimalAI.AnimalType animalType = CuteAnimalAI.AnimalType.Zebra;
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

    [Header("🏞️ Navigation & Waypoints")]
    public Transform[] wanderPoints;

    [Header("😱 Panic & Flee Settings")]
    public float herdPanicRadius = 10f;
    public int fleeAttempts = 6;
    public float navMeshEdgeThreshold = 2.0f;
    public float playerPredictionTime = 1.5f;
    public float playerForwardPredictionDistance = 2f;
    public float zigzagSpeed = 6f;
    public float zigzagStrength = 0.5f;

    [Header("🦓 Passive Herd Settings")]
    public float herdJoinRadius = 15f;
    public float herdPreferredDistance = 5f;
    public float herdRegroupSpeed = 3f;

    [Header("🚀 Flee Burst Settings")]
    public float fleeBurstMultiplier = 1.5f;
    public float fleeBurstDuration = 0.5f;

    [Header("🔄 Edge Awareness")]
    [Range(0f, 1f)] public float edgeBlendStrength = 0.4f;
    public float edgeAwarenessBuffer = 1.5f;
}
