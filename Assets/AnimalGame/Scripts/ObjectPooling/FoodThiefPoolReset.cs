using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class FoodThiefPoolReset : MonoBehaviour, IPoolable
{
    private FoodThiefAI thiefAI;

    private void Awake()
    {
        thiefAI = GetComponent<FoodThiefAI>();
    }

    public void OnSpawned()
    {
        if (thiefAI)
        {
            thiefAI.enabled = true;
            thiefAI.ResetFoodThiefAI();
        }
    }

    public void OnDespawned()
    {
        if (thiefAI)
        {
            thiefAI.PrepareFoodThiefForPoolDespawn();
            thiefAI.enabled = false;
        }
    }
}