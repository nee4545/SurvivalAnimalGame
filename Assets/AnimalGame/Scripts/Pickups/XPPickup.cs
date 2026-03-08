using UnityEngine;

public class XPPickup : PickupBase
{
    [Header("XP")]
    public int xpAmount = 10;
    public float pulseSpeed = 5f;
    public float pulseScale = 1.15f;

    protected override bool TryCollect(CCActor player)
    {
        if (!player) return false;

        player.AddXP(xpAmount);
        return true;
    }
}
