using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class FoodCarrierPoolReset : MonoBehaviour, IPoolable
{
    private FoodCarrierAI carrierAI;

    private void Awake()
    {
        carrierAI = GetComponent<FoodCarrierAI>();
    }

    public void OnSpawned()
    {
        if (carrierAI)
        {
            carrierAI.enabled = true;
            carrierAI.ResetCarrierAI();
        }
    }

    public void OnDespawned()
    {
        if (carrierAI)
        {
            carrierAI.PrepareCarrierForPoolDespawn();
            carrierAI.enabled = false;
        }
    }
}