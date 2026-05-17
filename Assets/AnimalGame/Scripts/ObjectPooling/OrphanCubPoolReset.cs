using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class OrphanCubPoolReset : MonoBehaviour, IPoolable
{
    private OrphanCubAI orphanAI;

    private void Awake()
    {
        orphanAI = GetComponent<OrphanCubAI>();
    }

    public void OnSpawned()
    {
        if (orphanAI)
        {
            orphanAI.enabled = true;
            orphanAI.ResetOrphanCub();
        }
    }

    public void OnDespawned()
    {
        if (orphanAI)
        {
            orphanAI.StopAllCoroutines();
            orphanAI.enabled = false;
        }
    }
}