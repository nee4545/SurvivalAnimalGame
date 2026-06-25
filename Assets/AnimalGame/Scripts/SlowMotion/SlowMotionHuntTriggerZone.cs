using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlowMotionHuntTriggerZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public bool allowSlowMotionHunt = true;

    [Tooltip("If enabled, hunt activation only works while player is inside this zone.")]
    public bool requirePlayerInside = true;

    [Tooltip("Optional override for max leap distance while inside this zone.")]
    public float zoneMaxLeapDistance = 16f;

    [Header("Hunt Target Area")]
    [Tooltip("Only animals inside this spherical area will be considered for slow motion hunt targeting.")]
    public bool useHuntTargetArea = true;

    [Tooltip("Local offset from this hunt zone object. Use this to place the targeting sphere near a spawner.")]
    public Vector3 targetAreaLocalCenter = Vector3.zero;

    [Tooltip("Radius of the spherical animal targeting area.")]
    public float targetAreaRadius = 12f;

    [Header("Gizmo")]
    public Color targetAreaGizmoColor = new Color(1f, 0.7f, 0f, 0.22f);
    public Color targetAreaWireColor = new Color(1f, 0.55f, 0f, 1f);

    public Vector3 TargetAreaWorldCenter
    {
        get
        {
            return transform.TransformPoint(targetAreaLocalCenter);
        }
    }

    public float TargetAreaWorldRadius
    {
        get
        {
            float maxScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z)
            );

            return targetAreaRadius * maxScale;
        }
    }

    public bool IsPointInsideTargetArea(Vector3 worldPoint)
    {
        if (!useHuntTargetArea)
            return true;

        float radius = TargetAreaWorldRadius;
        return (worldPoint - TargetAreaWorldCenter).sqrMagnitude <= radius * radius;
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        targetAreaRadius = 12f;
        targetAreaLocalCenter = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        CCActor actor = other.GetComponentInParent<CCActor>();   
        if (!actor) return;

        SlowMotionHuntController hunt = actor.GetComponent<SlowMotionHuntController>();
        if (!hunt) return;

        hunt.EnterHuntZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        CCActor actor = other.GetComponentInParent<CCActor>();
        if (!actor) return;

        SlowMotionHuntController hunt = actor.GetComponent<SlowMotionHuntController>();
        if (!hunt) return;

        hunt.ExitHuntZone(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!useHuntTargetArea)
            return;

        Vector3 center = TargetAreaWorldCenter;
        float radius = TargetAreaWorldRadius;

        Gizmos.color = targetAreaGizmoColor;
        Gizmos.DrawSphere(center, radius);

        Gizmos.color = targetAreaWireColor;
        Gizmos.DrawWireSphere(center, radius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useHuntTargetArea)
            return;

        Vector3 center = TargetAreaWorldCenter;
        float radius = TargetAreaWorldRadius;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, radius);

        Gizmos.DrawLine(transform.position, center);
    }
#endif
}