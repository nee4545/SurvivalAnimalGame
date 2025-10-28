using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public abstract class PickupBase : MonoBehaviour, IPoolable
{
    [Header("Colliders")]
    [Tooltip("Non-trigger collider used for ground collision/settling.")]
    public Collider physicsCollider;
    [Tooltip("Trigger used for collection. If null, falls back to GetComponent<Collider>().")]
    public Collider pickupTrigger;

    [Header("Lifetime")]
    public float lifetime = 12f;
    public float blinkDuration = 2.5f;
    public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("FX (optional)")]
    public ParticleSystem collectVFX;
    public AudioSource collectSFX;

    protected bool collected;
    protected Renderer[] rends;
    protected PooledObject pooled;
    Rigidbody rb;

    Vector3 _originalLocalScale;

    void Awake()
    {
        pooled = GetComponent<PooledObject>();
        rends = GetComponentsInChildren<Renderer>(true);
        if (rends == null)
          rends = GetComponents<Renderer>();
        rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = true;
        rb.isKinematic = true; // will be toggled during toss by the dropper
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _originalLocalScale = transform.localScale;

        // Fallbacks
        if (!physicsCollider) physicsCollider = GetComponent<Collider>(); // must be non-trigger
        if (!pickupTrigger)
        {
            // If user didn’t assign a separate trigger, try to find a child trigger
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (c != physicsCollider && c.isTrigger) { pickupTrigger = c; break; }
        }

        if (pickupTrigger) pickupTrigger.isTrigger = true;
        if (physicsCollider) physicsCollider.isTrigger = false;
    }

    void OnEnable() { transform.localScale = _originalLocalScale; ResetVisuals(); StartCoroutine(LifeRoutine()); }
    public void OnSpawned() { transform.localScale = _originalLocalScale; StopAllCoroutines(); ResetVisuals(); StartCoroutine(LifeRoutine()); }
    public void OnDespawned() { transform.localScale = _originalLocalScale; StopAllCoroutines(); foreach (var r in rends) if (r) r.enabled = true; transform.localScale = Vector3.one; }

    protected void ResetVisuals() { collected = false; foreach (var r in rends) if (r) r.enabled = true; }

    void OnTriggerEnter(Collider other)
    {
        // Only react if the trigger that fired is our pickupTrigger (ignore ground contacts)
        if (!pickupTrigger || other.transform == transform) return;
    }

    // This will be called by a helper on the trigger child (below),
    // or you can leave it and we’ll also catch trigger events on the root:
    public void TryCollectFrom(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        var player = other.GetComponentInParent<CCActor>();
        if (!player) return;

        if (TryCollect(player))
        {
            collected = true;
            if (collectSFX) collectSFX.Play();
            if (collectVFX) collectVFX.Play();
            StartCoroutine(DespawnNextFrame());
        }
    }

    IEnumerator DespawnNextFrame() { yield return null; pooled.Despawn(); }

    IEnumerator LifeRoutine()
    {
        float t = 0f;
        float blinkStart = Mathf.Max(0f, lifetime - blinkDuration);
        bool blinking = false;
        while (t < lifetime && !collected)
        {
            t += Time.deltaTime;
            if (!blinking && t >= blinkStart && blinkDuration > 0f)
            {
                blinking = true;
                StartCoroutine(BlinkRoutine(lifetime - t));
            }
            yield return null;
        }
        if (!collected) pooled.Despawn();
    }

    IEnumerator BlinkRoutine(float duration)
    {
        float t = 0f;
        while (t < duration && !collected)
        {
            t += Time.deltaTime;
            float x = blinkCurve.Evaluate(Mathf.InverseLerp(0f, duration, t));
            bool on = (Mathf.PingPong(x * 20f, 1f) > 0.5f);
            foreach (var r in rends) if (r) r.enabled = on;
            yield return null;
        }
        foreach (var r in rends) if (r) r.enabled = true;
    }

    protected abstract bool TryCollect(CCActor player);
}
