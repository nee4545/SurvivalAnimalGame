using UnityEngine;

public class StampedeTweenReactionEmitter : MonoBehaviour
{
    public enum SpeedMode
    {
        TransformMovement,
        WorldScroller,
        AlwaysPulse
    }

    [Header("Mode")]
    public SpeedMode speedMode = SpeedMode.TransformMovement;

    [Tooltip("Assign this when using WorldScroller mode.")]
    public StampedeWorldScroller worldScroller;

    [Header("Detection")]
    public float reactionRadius = 2.2f;
    public float emitInterval = 0.12f;
    public int maxChecksPerTick = 60;

    [Header("Reaction Strength")]
    public float reactionStrength = 1f;
    public float minMoveSpeedToReact = 0.5f;

    [Header("Debug")]
    public bool debugDrawRadius;

    private float nextEmitTime;
    private Vector3 lastPosition;
    private int scanIndex;

    private void OnEnable()
    {
        lastPosition = transform.position;
        nextEmitTime = 0f;
        scanIndex = 0;
    }

    private void Update()
    {
        float speed = GetReactionSpeed();

        if (speed < minMoveSpeedToReact)
            return;

        if (Time.time < nextEmitTime)
            return;

        nextEmitTime = Time.time + emitInterval;

        PulseNearbyReactables();
    }

    private float GetReactionSpeed()
    {
        if (speedMode == SpeedMode.AlwaysPulse)
            return minMoveSpeedToReact + 1f;

        if (speedMode == SpeedMode.WorldScroller)
        {
            if (worldScroller != null)
                return worldScroller.GetCurrentScrollSpeed();

            return minMoveSpeedToReact + 1f;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        float speed =
            (transform.position - lastPosition).magnitude / dt;

        lastPosition = transform.position;

        return speed;
    }

    private void PulseNearbyReactables()
    {
        var reactables = StampedeTweenReactable.All;

        if (reactables == null || reactables.Count == 0)
            return;

        float radiusSqr = reactionRadius * reactionRadius;

        int total = reactables.Count;
        int checks = Mathf.Min(maxChecksPerTick, total);

        for (int i = 0; i < checks; i++)
        {
            if (total <= 0)
                return;

            scanIndex %= total;

            StampedeTweenReactable reactable = reactables[scanIndex];
            scanIndex++;

            if (reactable == null || !reactable.isActiveAndEnabled)
                continue;

            float distanceSqr =
                (reactable.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr <= radiusSqr)
            {
                reactable.React(transform.position, reactionStrength);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawRadius)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, reactionRadius);
    }
}