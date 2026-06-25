using System.Collections;
using UnityEngine;

public class LootDropper : MonoBehaviour, IPoolable
{
    [Header("Prefabs (pooled)")]
    public GameObject[] meatPrefabs;

    [Header("Counts")]
    public int meatMin = 2;
    public int meatMax = 5;

    [Header("Drop Behavior")]
    public float spawnHeight = 1.2f;        // start loot ABOVE the body
    private float arcHeightMin = 0.8f;
    private float arcHeightMax = 2f;
    public float arcDurationMin = 0.35f;
    public float arcDurationMax = 0.55f;
    public float scatterRadius = 1.4f;      // how far around the body loot can land

    [Tooltip("Extra delay AFTER landing before pickup is allowed")]
    private float pickupDelay = 0.7f;

    private LayerMask groundMask;
    [Header("Grounding")]
    public float groundCheckDistance = 10f;

    [Header("Lifetime")]
    public float xpLifetime = 14f;
    public float meatLifetime = 12f;

    [Header("Drop Delay")]
    public float dropDelay = 0.10f;

    [Header("Kill Rewards")]
    public int xpReward = 25;
    public int coinReward = 5;

    [Header("Reward UI")]
    public bool playXPUIEffect = true;
    public bool playCoinUIEffect = true;
    public Vector3 rewardUIWorldOffset = new Vector3(0f, 1.5f, 0f);

    public Transform lootOrigin;

    private bool hasDropped = false;

    public void Awake()
    {
        hasDropped = false;
        groundMask = LayerMask.GetMask("Terrain");
    }
    public void OnSpawned() => hasDropped = false;
    public void OnDespawned() { }

    // Called by AnimalAI on death
    public void DropAt(Vector3 deathPos, Quaternion deathRot)
    {
        if (hasDropped) return;
        hasDropped = true;

        Vector3 origin = lootOrigin ? lootOrigin.position : deathPos;
        origin.y += spawnHeight; // ensure we start above the body

        CoroutineRunner.I.StartCoroutine(DropRoutine(origin));
    }

    IEnumerator DropRoutine(Vector3 origin)
    {
        if (dropDelay > 0f)
            yield return new WaitForSeconds(dropDelay);

        CCActor player = FindObjectOfType<CCActor>();

        Vector3 rewardWorldPos = origin + rewardUIWorldOffset;

        if (player != null)
        {
            if (xpReward > 0)
            {
                player.AddXP(xpReward);

                Vector3 finalXpPosition = player.transform.position + new Vector3(0, 1, 0);

                if (playXPUIEffect && UIXPFlyEffect.Instance != null)
                {
                    UIXPFlyEffect.Instance.PlayXPReward(finalXpPosition, xpReward);
                }
            }

            if (coinReward > 0)
            {
                player.AddCoins(coinReward);

                // Replace UICoinFlyEffect with your actual coin UI effect script name/method.
                if (playCoinUIEffect && UICoinFlyEffect.Instance != null)
                {
                    UICoinFlyEffect.Instance.PlayCoinReward(rewardWorldPos, coinReward);
                }
            }
        }

        // ---------------- MEAT DROPS ----------------
        int dropCount = Random.Range(meatMin, meatMax + 1);

        for (int i = 0; i < dropCount; i++)
        {
            if (meatPrefabs == null || meatPrefabs.Length == 0)
                break;

            var prefab = meatPrefabs[Random.Range(0, meatPrefabs.Length)];

            // Start position (slightly above origin)
            Vector3 startPos = origin;

            // Random direction and distance around the origin
            float distance = Random.Range(0.5f, scatterRadius);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            // Flat target position around the body
            Vector3 flatTarget = origin + dir * distance;
            Vector3 targetPos = GetGroundPoint(flatTarget);

            // Spawn loot at start position
            var loot = PoolManager.Spawn(prefab, startPos, Quaternion.identity);
            SetupPickup(loot, meatLifetime);

            // Run manual parabolic arc
            CoroutineRunner.I.StartCoroutine(ParabolicDrop(loot, startPos, targetPos));

            yield return null;
        }
    }

    // --------------------------------------------
    // Pickup setup (no enabling here; we do that after the arc)
    // --------------------------------------------
    void SetupPickup(GameObject obj, float lifetime)
    {
        if (!obj) return;

        if (obj.TryGetComponent(out PickupBase pb))
        {
            pb.lifetime = lifetime;
            //pb.pickupEnabled = true; // block pickup until arc completes
        }

    }

    // --------------------------------------------
    // Manual parabolic motion (no physics)
    // --------------------------------------------
    IEnumerator ParabolicDrop(GameObject obj, Vector3 startPos, Vector3 targetPos)
    {
        if (!obj) yield break;

        // Different arc each time
        float duration = Random.Range(arcDurationMin, arcDurationMax);
        float arcHeight = Random.Range(arcHeightMin, arcHeightMax);

        float elapsed = 0f;

        // Split into horizontal and vertical components
        Vector2 startXZ = new Vector2(startPos.x, startPos.z);
        Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
        float startY = startPos.y;
        float targetY = targetPos.y;

        // If start is below target, raise it so the arc looks nicer
        if (startY < targetY)
            startY = targetY + 0.2f;

        while (elapsed < duration)
        {
            if (!obj) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Horizontal lerp
            Vector2 xz = Vector2.Lerp(startXZ, targetXZ, t);

            // Base vertical (lerp between start & target Y)
            float y = Mathf.Lerp(startY, targetY, t);

            // Add a parabolic "hump": sin(pi * t) gives 0 → 1 → 0
            float hump = Mathf.Sin(t * Mathf.PI) * arcHeight;
            y += hump;

            obj.transform.position = new Vector3(xz.x, y, xz.y);

            yield return null;
        }

        if (!obj) yield break;

        Vector3 final = obj.transform.position;
        Vector3 ground = GetGroundPoint(final);

        float yOffset = 0f;

        // If it's a meat pickup, use the visible mesh bounds instead of the trigger collider
        if (obj.TryGetComponent<MeatPickup>(out var meat))
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                yOffset = rend.bounds.extents.y;
            }
        }
        else if(obj.TryGetComponent<XPPickup>(out var xp))
        {
            yOffset = 0.5f;
        }
        else
        {
            // Fallback for other pickups (XP, etc.)
            if (obj.TryGetComponent<Collider>(out var col))
            {
                yOffset = col.bounds.extents.y;
            }
        }

        obj.transform.position = ground + Vector3.up * yOffset;

        if (obj.TryGetComponent(out PickupBase pb))
        {
            if (pickupDelay > 0f)
                yield return new WaitForSeconds(pickupDelay);

            pb.readyForMagnet = true;
        }
    }

    // --------------------------------------------
    // Grounding Helper
    // --------------------------------------------
    Vector3 GetGroundPoint(Vector3 start)
    {
        // Cast from above downwards to find terrain/ground
        Vector3 rayStart = start + Vector3.up * 2f;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out var hit,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            // Return the real ground point (no offset here)
            return hit.point;
        }

        // Fallback: if no ground hit, just keep original Y
        return start;
    }

}
