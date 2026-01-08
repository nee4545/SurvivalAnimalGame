using UnityEngine;
using System.Collections.Generic;

public class FoliageRegistry : MonoBehaviour
{
    public static readonly List<FoliageReactableBase> All = new();

    void Awake()
    {
        RegisterChildren();
    }

    void RegisterChildren()
    {
        All.Clear();

        var reactables = GetComponentsInChildren<FoliageReactableBase>(true);

        for (int i = 0; i < reactables.Length; i++)
        {
            All.Add(reactables[i]);
        }

        Debug.Log($"FoliageRegistry: Registered {All.Count} foliage objects.");
    }
}
