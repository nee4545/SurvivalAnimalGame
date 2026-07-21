using UnityEngine;


public class StampedeDustTrail : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("Optional. If empty, the script uses this object's parent as the moving target.")]
    public Transform targetToFollow;

    [Tooltip("When following a target, this offset is applied in the target's local space. Usually behind the feet.")]
    public Vector3 localOffset = new Vector3(0f, 0.08f, -0.65f);

    public bool followTargetPosition = true;
    public bool alignToMovementDirection = true;

    [Header("Emission By Speed")]
    public bool autoControlEmissionBySpeed = true;
    public float minSpeedToEmit = 0.75f;
    public float fullEmissionSpeed = 8f;
    public float emissionMultiplier = 1f;

    [Tooltip("A small burst when the object starts moving again after being idle.")]
    public bool playStartBurst = true;
    public int startBurstParticles = 8;

    [Header("Ground Placement")]
    public bool snapToGround = true;
    public LayerMask groundMask = ~0;
    public float groundRaycastHeight = 2f;
    public float groundRaycastDistance = 6f;
    public float groundYOffset = 0.04f;

    [Header("Manual Override")]
    public bool forceEmissionOn;

    private ParticleSystem[] particleSystems;
    private float[] baseEmissionRates;

    private Transform followTarget;
    private Vector3 previousTargetPosition;
    private Vector3 movementDirection;
    private float currentSpeed;
    private bool wasEmitting;
    private bool initialized;

    private void Awake()
    {
        CacheSystems();
        ResolveTarget();
    }

    private void OnEnable()
    {
        ResolveTarget();

        if (followTarget != null)
            previousTargetPosition = followTarget.position;
        else
            previousTargetPosition = transform.position;

        SetEmissionEnabled(false);
        initialized = true;
    }

    private void CacheSystems()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        baseEmissionRates = new float[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            var emission = particleSystems[i].emission;
            baseEmissionRates[i] = emission.rateOverTimeMultiplier;
        }
    }

    private void ResolveTarget()
    {
        if (targetToFollow != null)
        {
            followTarget = targetToFollow;
            return;
        }

        if (transform.parent != null)
            followTarget = transform.parent;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (followTarget == null)
            ResolveTarget();

        UpdateSpeedAndDirection();
        UpdateFollowPosition();
        UpdateRotation();
        UpdateEmission();
    }

    private void UpdateSpeedAndDirection()
    {
        Vector3 currentPosition = followTarget != null ? followTarget.position : transform.position;
        Vector3 delta = currentPosition - previousTargetPosition;
        delta.y = 0f;

        currentSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        if (delta.sqrMagnitude > 0.0001f)
            movementDirection = delta.normalized;

        previousTargetPosition = currentPosition;
    }

    private void UpdateFollowPosition()
    {
        if (!followTargetPosition || followTarget == null)
            return;

        Vector3 targetPosition = followTarget.TransformPoint(localOffset);

        if (snapToGround)
        {
            Vector3 rayStart = targetPosition + Vector3.up * groundRaycastHeight;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
                targetPosition.y = hit.point.y + groundYOffset;
        }

        transform.position = targetPosition;
    }

    private void UpdateRotation()
    {
        if (!alignToMovementDirection)
            return;

        if (movementDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up);
    }

    private void UpdateEmission()
    {
        if (particleSystems == null || particleSystems.Length == 0)
            return;

        bool shouldEmit = forceEmissionOn;
        float speedT = 1f;

        if (!forceEmissionOn && autoControlEmissionBySpeed)
        {
            speedT = Mathf.InverseLerp(minSpeedToEmit, fullEmissionSpeed, currentSpeed);
            shouldEmit = speedT > 0.01f;
        }

        if (!autoControlEmissionBySpeed)
            shouldEmit = true;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            var emission = particleSystems[i].emission;
            emission.enabled = shouldEmit;
            emission.rateOverTimeMultiplier = baseEmissionRates[i] * emissionMultiplier * Mathf.Max(0.2f, speedT);

            if (shouldEmit && !particleSystems[i].isPlaying)
                particleSystems[i].Play(false);
        }

        if (shouldEmit && !wasEmitting && playStartBurst)
            PlayBurst(startBurstParticles);

        wasEmitting = shouldEmit;
    }

    private void SetEmissionEnabled(bool enabled)
    {
        if (particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            var emission = particleSystems[i].emission;
            emission.enabled = enabled;
        }

        wasEmitting = enabled;
    }

    public void PlayBurst(int particleCount = 12)
    {
        if (particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            int count = Mathf.Max(1, Mathf.RoundToInt(particleCount * GetBurstMultiplier(i)));
            particleSystems[i].Emit(count);
        }
    }

    private float GetBurstMultiplier(int systemIndex)
    {
        if (particleSystems == null || systemIndex < 0 || systemIndex >= particleSystems.Length)
            return 1f;

        string systemName = particleSystems[systemIndex].name.ToLowerInvariant();

        if (systemName.Contains("speck"))
            return 0.65f;

        if (systemName.Contains("mist"))
            return 0.45f;

        return 1f;
    }
}
