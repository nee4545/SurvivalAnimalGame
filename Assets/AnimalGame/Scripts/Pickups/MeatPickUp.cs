using UnityEngine;

public class MeatPickup : PickupBase
{
    [Header("Meat")]
    public float hungerRestore = 20f;
    public bool spin = true;
    public float spinSpeed = 180f;

    void Update()
    {
        if (spin) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    protected override bool TryCollect(CCActor player)
    {
        if (!player) return false;
        float added = player.AddHunger(hungerRestore);
        return added > 0.01f;
    }
}
