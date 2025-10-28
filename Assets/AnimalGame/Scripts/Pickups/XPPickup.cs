using UnityEngine;

public class XPPickup : PickupBase
{
    [Header("XP")]
    public int xpAmount = 10;
    public float pulseSpeed = 5f;
    public float pulseScale = 1.15f;


    protected override bool TryCollect(CCActor player)
    {
        // TODO: Hook your XP system later, e.g. player.AddXP(xpAmount);
        return true;
    }
}
