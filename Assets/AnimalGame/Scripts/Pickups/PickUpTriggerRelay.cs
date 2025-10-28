using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupTriggerRelay : MonoBehaviour
{
    public PickupBase pickup;
    void Reset() { pickup = GetComponentInParent<PickupBase>(); }
    void OnTriggerEnter(Collider other) { pickup?.TryCollectFrom(other); }
}

