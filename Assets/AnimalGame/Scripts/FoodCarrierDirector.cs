using System.Collections.Generic;
using UnityEngine;

public class FoodCarrierDirector : MonoBehaviour
{
    public static FoodCarrierDirector Instance { get; private set; }

    private readonly List<GameObject> availableMeat = new();
    private readonly HashSet<GameObject> claimedMeat = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterMeat(GameObject meat)
    {
        if (meat == null)
            return;

        if (!availableMeat.Contains(meat))
            availableMeat.Add(meat);
    }

    public void UnregisterMeat(GameObject meat)
    {
        if (meat == null)
            return;

        availableMeat.Remove(meat);
        claimedMeat.Remove(meat);
    }

    public bool TryClaimNearestMeat(Vector3 fromPosition, float maxDistance, out GameObject meat)
    {
        meat = null;

        float bestDistanceSqr = maxDistance * maxDistance;

        for (int i = availableMeat.Count - 1; i >= 0; i--)
        {
            GameObject candidate = availableMeat[i];

            if (candidate == null || !candidate.activeInHierarchy)
            {
                availableMeat.RemoveAt(i);
                continue;
            }

            if (claimedMeat.Contains(candidate))
                continue;

            float distanceSqr = (candidate.transform.position - fromPosition).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                meat = candidate;
            }
        }

        if (meat == null)
            return false;

        claimedMeat.Add(meat);
        return true;
    }

    public void ReleaseClaim(GameObject meat)
    {
        if (meat == null)
            return;

        claimedMeat.Remove(meat);
    }
}