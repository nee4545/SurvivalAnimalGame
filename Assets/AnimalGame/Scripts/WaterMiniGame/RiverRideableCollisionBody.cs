using System.Collections.Generic;
using UnityEngine;

public class RiverRideableCollisionBody : MonoBehaviour
{
    private static readonly List<RiverRideableCollisionBody> Bodies =
        new List<RiverRideableCollisionBody>();

    [Header("References")]
    public RiverRideableObject rideable;

    [Header("Collision Shape")]
    [Tooltip("Flat XZ radius used for cheap collision resolution.")]
    public float radius = 0.75f;

    [Tooltip("Optional offset from transform position.")]
    public Vector3 localCenterOffset = Vector3.zero;

    [Header("Resolution")]
    public bool resolveCollisions = true;

    [Tooltip("How strongly animals push away from each other.")]
    public float pushStrength = 8f;

    [Tooltip("Maximum push applied per frame.")]
    public float maxPushPerFrame = 0.25f;

    [Tooltip("If ON, mounted rideable is harder to push than free rideables.")]
    public bool makeRiddenRideableHeavier = true;

    [Range(0f, 1f)]
    public float riddenPushMultiplier = 0.25f;

    [Header("Mounted Bump Jump")]
    public bool triggerPlayerJumpWhenRiddenAnimalHitsAnother = true;

    [Tooltip("Other rideable must be within this extra overlap amount before jump triggers.")]
    public float jumpTriggerExtraPadding = 0.05f;

    [Tooltip("Prevents repeated jump calls while overlapping.")]
    public float jumpTriggerCooldown = 0.35f;

    [Header("Debug")]
    public bool drawGizmos;

    private float lastJumpTriggerTime = -999f;

    private void Awake()
    {
        if (rideable == null)
            rideable = GetComponent<RiverRideableObject>();
    }

    private void OnEnable()
    {
        if (!Bodies.Contains(this))
            Bodies.Add(this);
    }

    private void OnDisable()
    {
        Bodies.Remove(this);
    }

    private void OnDestroy()
    {
        Bodies.Remove(this);
    }

    private void LateUpdate()
    {
        if (!resolveCollisions)
            return;

        ResolveAgainstOtherBodies();
    }

    private void ResolveAgainstOtherBodies()
    {
        if (!IsValidBody())
            return;

        for (int i = 0; i < Bodies.Count; i++)
        {
            RiverRideableCollisionBody other = Bodies[i];

            if (other == null)
                continue;

            if (other == this)
                continue;

            if (!other.IsValidBody())
                continue;

            ResolvePair(other);
        }
    }

    private bool IsValidBody()
    {
        if (!isActiveAndEnabled)
            return false;

        if (rideable == null)
            return false;

        if (!rideable.gameObject.activeInHierarchy)
            return false;

        if (rideable.IsRetiring)
            return false;

        return true;
    }

    private void ResolvePair(RiverRideableCollisionBody other)
    {
        Vector3 myPosition = GetWorldCenter();
        Vector3 otherPosition = other.GetWorldCenter();

        Vector3 flatDelta = myPosition - otherPosition;
        flatDelta.y = 0f;

        float distanceSqr = flatDelta.sqrMagnitude;

        float combinedRadius = radius + other.radius;
        float combinedRadiusSqr = combinedRadius * combinedRadius;

        if (distanceSqr >= combinedRadiusSqr)
            return;

        float distance = Mathf.Sqrt(Mathf.Max(distanceSqr, 0.0001f));

        Vector3 pushDirection;

        if (distance > 0.001f)
        {
            pushDirection = flatDelta / distance;
        }
        else
        {
            pushDirection = Random.insideUnitSphere;
            pushDirection.y = 0f;

            if (pushDirection.sqrMagnitude < 0.001f)
                pushDirection = Vector3.right;

            pushDirection.Normalize();
        }

        float overlap = combinedRadius - distance;

        TryTriggerMountedJump(other, overlap);

        ApplyPush(other, pushDirection, overlap);
    }

    private void TryTriggerMountedJump(
        RiverRideableCollisionBody other,
        float overlap
    )
    {
        if (!triggerPlayerJumpWhenRiddenAnimalHitsAnother)
            return;

        if (overlap < jumpTriggerExtraPadding)
            return;

        if (Time.time - lastJumpTriggerTime < jumpTriggerCooldown)
            return;

        if (rideable == null || other == null || other.rideable == null)
            return;

        RiverEscapePlayerController rider = rideable.CurrentRider;

        if (rider == null)
            return;

        bool jumped =
            rider.ForceJumpFromRideableCollision(other.rideable);

        if (jumped)
            lastJumpTriggerTime = Time.time;
    }

    private void ApplyPush(
        RiverRideableCollisionBody other,
        Vector3 pushDirection,
        float overlap
    )
    {
        if (rideable == null || other.rideable == null)
            return;

        bool iHaveRider = rideable.HasRider;
        bool otherHasRider = other.rideable.HasRider;

        float myPushWeight = 1f;
        float otherPushWeight = 1f;

        if (makeRiddenRideableHeavier)
        {
            if (iHaveRider)
                myPushWeight = riddenPushMultiplier;

            if (otherHasRider)
                otherPushWeight = other.riddenPushMultiplier;
        }

        float totalWeight = myPushWeight + otherPushWeight;

        if (totalWeight <= 0.001f)
            return;

        float pushAmount =
            Mathf.Min(
                overlap * pushStrength * Time.deltaTime,
                maxPushPerFrame
            );

        Vector3 myPush =
            pushDirection *
            pushAmount *
            (otherPushWeight / totalWeight);

        Vector3 otherPush =
            -pushDirection *
            pushAmount *
            (myPushWeight / totalWeight);

        if (!iHaveRider)
            ApplyFlatMove(myPush);

        if (!otherHasRider)
            other.ApplyFlatMove(otherPush);
    }

    private void ApplyFlatMove(Vector3 worldDelta)
    {
        worldDelta.y = 0f;
        transform.position += worldDelta;
    }

    private Vector3 GetWorldCenter()
    {
        return transform.TransformPoint(localCenterOffset);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetWorldCenter(), radius);
    }
#endif
}