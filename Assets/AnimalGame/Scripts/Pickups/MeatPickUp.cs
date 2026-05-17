using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MeatPickup : MonoBehaviour
{
    public bool canBePickedUp = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp)
            return;

        PlayerMeatCarrier carrier = other.GetComponentInParent<PlayerMeatCarrier>();

        if (carrier == null)
            return;

        bool collected = carrier.TryCollectMeat(gameObject);

        if (collected)
        {
            canBePickedUp = false;
        }
    }
}