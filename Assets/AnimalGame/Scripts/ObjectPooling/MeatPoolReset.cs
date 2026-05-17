using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class MeatPoolReset : MonoBehaviour, IPoolable
{
    private Collider meatCollider;
    private MeatPickup meatPickup;

    private void Awake()
    {
        meatCollider = GetComponent<Collider>();
        meatPickup = GetComponent<MeatPickup>();
    }

    public void OnSpawned()
    {
        transform.DOKill();
        transform.SetParent(null, true);

        if (meatCollider)
            meatCollider.enabled = true;

        if (meatPickup)
            meatPickup.canBePickedUp = true;
    }

    public void OnDespawned()
    {
        transform.DOKill();

        if (meatCollider)
            meatCollider.enabled = false;

        if (meatPickup)
            meatPickup.canBePickedUp = false;
    }
}