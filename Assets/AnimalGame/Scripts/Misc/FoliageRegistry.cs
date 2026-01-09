using UnityEngine;
using System.Collections.Generic;

public class FoliageRegistry : MonoBehaviour
{
    public static FoliageRegistry Instance;

    [Header("Grid Settings")]
    public float cellSize = 3f;

    // Grid: cell -> foliage list
    private readonly Dictionary<Vector2Int, List<FoliageReactableBase>> grid
        = new Dictionary<Vector2Int, List<FoliageReactableBase>>(1024);

    void Awake()
    {
        Instance = this;
        BuildGrid();
    }

    void BuildGrid()
    {
        grid.Clear();

        var reactables = GetComponentsInChildren<FoliageReactableBase>(true);

        for (int i = 0; i < reactables.Length; i++)
        {
            var f = reactables[i];
            if (!f) continue;

            Vector2Int cell = WorldToCell(f.transform.position);

            if (!grid.TryGetValue(cell, out var list))
            {
                list = new List<FoliageReactableBase>(8);
                grid.Add(cell, list);
            }

            list.Add(f);
        }

        Debug.Log($"FoliageRegistry: Registered {reactables.Length} foliage objects into {grid.Count} cells.");
    }

    public Vector2Int WorldToCell(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    public bool TryGetCell(Vector2Int cell, out List<FoliageReactableBase> list)
    {
        return grid.TryGetValue(cell, out list);
    }
}
