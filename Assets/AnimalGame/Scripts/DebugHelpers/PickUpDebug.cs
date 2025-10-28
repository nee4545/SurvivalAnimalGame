using UnityEngine;

public class PickupDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Spawned: {name} localScale={transform.localScale} lossyScale={transform.lossyScale}");
        var sc = GetComponent<SphereCollider>();
        var cc = GetComponent<CapsuleCollider>();
        if (sc != null) Debug.Log($"SphereCollider radius(local)={sc.radius} worldRadius={sc.radius * transform.lossyScale.x}");
        if (cc != null) Debug.Log($"CapsuleCollider r={cc.radius} h={cc.height} worldR={cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z)}");
    }
}
