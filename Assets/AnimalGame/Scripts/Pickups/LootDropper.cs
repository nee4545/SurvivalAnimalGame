using System.Collections;
using UnityEngine;

public class LootDropper : MonoBehaviour
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

    private void Awake()
    {
        _hasDropped = false;
    }

    public void OnDeathDrop()
    {
        if (_hasDropped) return;
        _hasDropped = true;
        StartCoroutine(DropRoutine());
    }

    IEnumerator DropRoutine()
    {
        yield return new WaitForSeconds(dropDelay);

        Vector3 center = transform.position + Vector3.up * spawnHeight;

        // XP
        if (xpPrefab)
        {
            
            Vector3 xpPos = FindGroundedPoint(center);
            float yOffset = 0.5f;
            xpPos.y += yOffset;
            var xp = PoolManager.Spawn(xpPrefab, xpPos, Random.rotation);
            SetLifetime(xp, xpLifetime);
            //Toss(xp);
        }

        // Meat
        int count = Random.Range(meatMin, meatMax + 1);
        for (int i = 0; i < count; i++)
        {
            if (meatPrefabs == null || meatPrefabs.Length == 0) break;
            var meatPrefab = meatPrefabs[Random.Range(0, meatPrefabs.Length)];

            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(scatterRadius * 0.35f, scatterRadius);
            Vector3 pos = center + new Vector3(r.x, 0f, r.y);
            pos = FindGroundedPoint(pos);

            var go = PoolManager.Spawn(meatPrefab, pos, Random.rotation);
            SetLifetime(go, meatLifetime);
            Toss(go);

            yield return null; // stagger for organic feel
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
        rb.AddForce(dir * outwardForce + Vector3.up * upwardForce, ForceMode.VelocityChange);
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
