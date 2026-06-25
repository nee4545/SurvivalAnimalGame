using UnityEngine;

[System.Serializable]
public class StampedeRunConfig
{
    [Header("Name")]
    public string configName = "Easy Stampede";

    [Header("Mini Game")]
    public float miniGameDuration = 30f;
    public int maxLives = 3;

    [Header("Rewards")]
    public int successXPReward = 100;
    public int successCoinReward = 25;

    [Header("Hazard Prefabs")]
    public StampedeHazardAI[] hazardPrefabs;

    [Header("Spawn Position")]
    public float spawnDistanceFromLanes = 18f;
    public float spawnHeightOffset = 0f;

    [Header("Start Timing")]
    public float startDelay = 1f;

    [Header("Phased Spawn Tuning")]
    public bool usePhasedSpawnTuning = true;

    public StampedeHazardSpawner.SpawnPhase openingPhase =
        new StampedeHazardSpawner.SpawnPhase(
            "Opening 30%",
            1.2f,
            1.8f,
            7f,
            9f,
            1,
            1
        );

    public StampedeHazardSpawner.SpawnPhase middlePhase =
        new StampedeHazardSpawner.SpawnPhase(
            "Middle 40%",
            0.85f,
            1.35f,
            9f,
            12f,
            1,
            2
        );

    public StampedeHazardSpawner.SpawnPhase finalePhase =
        new StampedeHazardSpawner.SpawnPhase(
            "Final 30%",
            0.55f,
            1.0f,
            12f,
            16f,
            2,
            2
        );

    [Header("Lane Selection")]
    public bool allowSameLaneBackToBack = false;

    [Header("Pooling")]
    public int prewarmCount = 12;
}