using UnityEngine;

public class RiverRideableWakeTrailController : MonoBehaviour
{
    [Header("References")]
    public RiverRideableObject rideable;
    public TrailRenderer[] wakeTrails;

    [Header("Water Height")]
    public Transform waterSurfaceReference;
    public float waterY = 0f;
    public float heightOffset = 0.05f;
    public bool lockTrailsToWaterY = true;

    [Header("Emission")]
    public bool emitOnlyWhenMoving = true;
    public float minSpeedToEmit = 0.4f;
    public bool emitWhenMounted = true;
    public bool emitWhenRetiring = true;
    public bool emitWhenUnridden = true;

    [Header("Trail Shape")]
    public float normalTrailTime = 0.45f;
    public float fastTrailTime = 0.75f;
    public float speedForFastTrail = 8f;
    public float widthMultiplier = 1f;

    [Header("Debug")]
    public bool debugLogs;

    private Vector3 lastPosition;
    private bool hasLastPosition;

    private void Awake()
    {
        if (rideable == null)
            rideable = GetComponentInParent<RiverRideableObject>();

        CacheTrails();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        hasLastPosition = true;
        SetTrailEmission(false, true);
    }

    private void OnDisable()
    {
        SetTrailEmission(false, true);
    }

    private void LateUpdate()
    {
        CacheTrails();
        LockTrailsToWater();

        float speed = GetSpeed();
        bool shouldEmit = ShouldEmit(speed);

        UpdateTrailTime(speed);
        SetTrailEmission(shouldEmit, false);
    }

    private void CacheTrails()
    {
        if (wakeTrails != null && wakeTrails.Length > 0)
            return;

        wakeTrails = GetComponentsInChildren<TrailRenderer>(true);
    }

    private float GetWaterY()
    {
        if (waterSurfaceReference != null)
            return waterSurfaceReference.position.y;

        return waterY;
    }

    private void LockTrailsToWater()
    {
        if (!lockTrailsToWaterY)
            return;

        if (wakeTrails == null)
            return;

        float targetY = GetWaterY() + heightOffset;

        for (int i = 0; i < wakeTrails.Length; i++)
        {
            TrailRenderer trail = wakeTrails[i];

            if (trail == null)
                continue;

            Vector3 position = trail.transform.position;
            position.y = targetY;
            trail.transform.position = position;
        }
    }

    private float GetSpeed()
    {
        if (!hasLastPosition)
        {
            lastPosition = transform.position;
            hasLastPosition = true;
            return 0f;
        }

        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        delta.y = 0f;

        lastPosition = currentPosition;

        if (Time.deltaTime <= 0.0001f)
            return 0f;

        return delta.magnitude / Time.deltaTime;
    }

    private bool ShouldEmit(float speed)
    {
        if (emitOnlyWhenMoving && speed < minSpeedToEmit)
            return false;

        if (rideable == null)
            return true;

        if (rideable.IsRetiring)
            return emitWhenRetiring;

        if (rideable.HasRider)
            return emitWhenMounted;

        return emitWhenUnridden;
    }

    private void UpdateTrailTime(float speed)
    {
        if (wakeTrails == null)
            return;

        float t = Mathf.InverseLerp(minSpeedToEmit, Mathf.Max(minSpeedToEmit + 0.01f, speedForFastTrail), speed);
        float targetTime = Mathf.Lerp(normalTrailTime, fastTrailTime, t);

        for (int i = 0; i < wakeTrails.Length; i++)
        {
            TrailRenderer trail = wakeTrails[i];

            if (trail == null)
                continue;

            trail.time = targetTime;
            trail.widthMultiplier = widthMultiplier;
        }
    }

    private void SetTrailEmission(bool emit, bool clear)
    {
        if (wakeTrails == null)
            return;

        for (int i = 0; i < wakeTrails.Length; i++)
        {
            TrailRenderer trail = wakeTrails[i];

            if (trail == null)
                continue;

            trail.emitting = emit;

            if (clear)
                trail.Clear();
        }
    }
}
