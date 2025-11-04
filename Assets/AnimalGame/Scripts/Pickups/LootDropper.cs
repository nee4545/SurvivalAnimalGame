using System.Collections;
using UnityEngine;

public class LootDropper : MonoBehaviour, IPoolable
{
    [Header("Prefabs (pooled)")]
    public GameObject xpPrefab;         // always one
    public GameObject[] meatPrefabs;    // your 2 meat prefabs here

    [Header("Counts")]
    public int meatMin = 2;
    public int meatMax = 5;

    [Header("Scatter")]
    public float spawnHeight = 0.5f;
    public float scatterRadius = 1.5f;
    public float upwardForce = 5f;
    public float outwardForce = 3.5f;
    public float torqueForce = 2f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public float groundCheckDown = 2f;

    [Header("Lifetime overrides")]
    public float meatLifetime = 12f;
    public float xpLifetime = 14f;

    [Header("Drop timing")]
    public float dropDelay = 0.15f;

    bool _hasDropped = false;

    public Transform lootOrigin;

    private void Awake()
    {
        _hasDropped = false;
    }
    public void OnSpawned() { _hasDropped = false; }
    public void OnDespawned() { }

    public void DropAt(Vector3 deathPos, Quaternion deathRot)
    {
        if (_hasDropped) return;
        _hasDropped = true;

        Vector3 originPos = lootOrigin ? lootOrigin.position : deathPos;
        // keep Y from deathPos or use originPos.y, your call; I'd use originPos.y:
        originPos.y = lootOrigin ? lootOrigin.position.y : deathPos.y;

        CoroutineRunner.I.StartCoroutine(DropRoutineAt(originPos, deathRot));
    }

    IEnumerator DropRoutineAt(Vector3 deathPos, Quaternion deathRot)
    {
        if (dropDelay > 0f)
            yield return new WaitForSeconds(dropDelay);

        // Use the captured death position, NOT transform.position
        Vector3 center = deathPos + Vector3.up * spawnHeight;

        // XP
        if (xpPrefab)
        {
            Vector3 xpPos = FindGroundedPoint(center);
            // (optional) remove the artificial lift to reduce drift
            xpPos.y += 0.5f;  // <- comment this out if you don't want the lift
            var xp = PoolManager.Spawn(xpPrefab, xpPos, Random.rotation);
            SetLifetime(xp, xpLifetime);
            // Toss(xp); // optional tiny settle pop
        }

        // Meat
        int count = Random.Range(meatMin, meatMax + 1);
        for (int i = 0; i < count; i++)
        {
            if (meatPrefabs == null || meatPrefabs.Length == 0) break;

            var meatPrefab = meatPrefabs[Random.Range(0, meatPrefabs.Length)];
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(scatterRadius * 0.35f, scatterRadius);
            Vector3 pos = center + new Vector3(r.x, 0f, r.y);
            pos.y += 1.5f;
            pos = FindGroundedPoint(pos);

            var go = PoolManager.Spawn(meatPrefab, pos, Random.rotation);
            SetLifetime(go, meatLifetime);
            Toss(go);

            yield return null; // organic staggering
        }
    }

    void SetLifetime(GameObject go, float seconds)
    {
        if (go && go.TryGetComponent<PickupBase>(out var p))
            p.lifetime = seconds;
    }

    void Toss(GameObject go)
    {
        if (!go) return;

        var rb = go.GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // your PoolManager already zeroes velocity on Spawn
        rb.isKinematic = false;

        Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y); dir.Normalize();
        rb.AddForce(dir * outwardForce + Vector3.up * (upwardForce), ForceMode.VelocityChange);
        rb.AddTorque(Random.onUnitSphere * torqueForce, ForceMode.VelocityChange);

        // settle then stop rolling forever
        StartCoroutine(MakeKinematicAfter(rb, 0.6f));
    }

    IEnumerator MakeKinematicAfter(Rigidbody rb, float t)
    {
        yield return new WaitForSeconds(t);
        if (rb) rb.isKinematic = true;
    }

    Vector3 FindGroundedPoint(Vector3 start)
    {
        if (Physics.Raycast(start + Vector3.up * 0.25f, Vector3.down, out var hit, groundCheckDown + 0.25f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.05f;
        return start;
    }
}
