using System.Collections.Generic;
using UnityEngine;

public class BaseCubManager : MonoBehaviour
{
    [Header("Cub Limit")]
    public int maxCubCount = 5;

    [Header("Base References")]
    public Transform baseCenter;
    public Transform player;
    public HomeFoodPoint foodPoint;

    private readonly HashSet<AnimalCubAI> registeredCubs = new();

    public int CurrentCubCount => registeredCubs.Count;
    public bool HasSpace => CurrentCubCount < maxCubCount;

    private void Awake()
    {
        RegisterExistingSceneCubs();
    }

    private void RegisterExistingSceneCubs()
    {
        AnimalCubAI[] sceneCubs = FindObjectsByType<AnimalCubAI>(FindObjectsSortMode.None);

        foreach (AnimalCubAI cub in sceneCubs)
        {
            RegisterCub(cub);
        }
    }

    public bool RegisterCub(AnimalCubAI cub)
    {
        if (cub == null)
            return false;

        if (registeredCubs.Contains(cub))
            return true;

        if (!HasSpace)
            return false;

        registeredCubs.Add(cub);

        cub.AssignBaseManager(this);

        return true;
    }

    public bool TryRegisterCub()
    {
        return HasSpace;
    }

    public void UnregisterCub(AnimalCubAI cub)
    {
        if (cub == null)
            return;

        registeredCubs.Remove(cub);
    }

    public void UnregisterCub()
    {
        // Kept only for older code compatibility.
        // Prefer UnregisterCub(AnimalCubAI cub).
    }
}