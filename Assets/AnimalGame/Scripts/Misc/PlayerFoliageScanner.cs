using UnityEngine;

public class PlayerFoliageScanner : MonoBehaviour
{
    [Header("Scan Settings")]
    public float radius = 1.6f;
    public float scanInterval = 0.12f;
    public LayerMask foliageLayer;
    float sqrRadius;


    void Start()
    {
        sqrRadius = radius * radius;
        InvokeRepeating(nameof(Scan), 0f, scanInterval);
    }

    void Scan()
    {
        Vector3 p = transform.position;

        for (int i = 0; i < FoliageRegistry.All.Count; i++)
        {
            var foliage = FoliageRegistry.All[i];
            if (!foliage) continue;

            Vector3 diff = foliage.transform.position - p;
            diff.y = 0f;

            if (diff.sqrMagnitude <= sqrRadius)
            {
                foliage.React(p);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
