using UnityEngine;

public class MeatPickup : PickupBase
{
    [Header("Meat")]
    public int meatAmount = 1;
    public float hungerRestore = 20f;
    public float healthRestore = 2f;
    public bool restoreStatsOnCollect = true;

    public bool spin = true;
    public float spinSpeed = 180f;

    protected override void Update()
    {
        base.Update();
        if (spin) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    protected override bool TryCollect(CCActor player)
    {
        if (!player) return false;

        player.AddMeat(meatAmount);

        if (restoreStatsOnCollect)
        {
            player.AddHunger(hungerRestore);
            player.AddHealth(healthRestore);
        }

        return true;
    }
}
