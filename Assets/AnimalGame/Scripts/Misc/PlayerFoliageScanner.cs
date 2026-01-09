using UnityEngine;

public class PlayerFoliageScanner : MonoBehaviour
{
    [Header("Scan Settings")]
    public float radius = 1.6f;
    public float scanInterval = 0.12f;

    float sqrRadius;
    int cellRadius;

    void Start()
    {
        sqrRadius = radius * radius;

        // How many cells we need to check around player
        cellRadius = Mathf.CeilToInt(radius / FoliageRegistry.Instance.cellSize);

        InvokeRepeating(nameof(Scan), 0f, scanInterval);
    }

    void Scan()
    {
        if (!FoliageRegistry.Instance)
            return;

        Vector3 p = transform.position;
        Vector2Int centerCell = FoliageRegistry.Instance.WorldToCell(p);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + z);

                if (!FoliageRegistry.Instance.TryGetCell(cell, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var foliage = list[i];
                    if (!foliage) continue;

                    Vector3 diff = foliage.transform.position - p;
                    diff.y = 0f;

                    if (diff.sqrMagnitude <= sqrRadius)
                    {
                        foliage.React(p);
                    }
                }
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
