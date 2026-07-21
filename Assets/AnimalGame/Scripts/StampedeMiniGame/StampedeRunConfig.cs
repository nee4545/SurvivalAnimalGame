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

    [Header("Rock Hazards")]
    public bool enableRockHazards = true;
    public GameObject rockHazardPrefab;

    public float rockSpawnIntervalMin = 1.5f;
    public float rockSpawnIntervalMax = 2.5f;

    public float rockNormalSpawnDistanceFromPlayer = 32f;
    public float rockInvertedSpawnDistanceFromPlayer = 45f;

    [Header("Animal Clusters")]
    public bool enableAnimalClusters = true;
    public StampedeClusterAnimal[] clusterAnimalPrefabs;

    public float clusterSpawnIntervalMin = 2.5f;
    public float clusterSpawnIntervalMax = 4.5f;

    public float clusterNormalSpawnDistanceFromPlayer = 28f;
    public float clusterInvertedSpawnDistanceFromPlayer = 45f;

    public int minAnimalsPerCluster = 3;
    public int maxAnimalsPerCluster = 5;

    public float clusterWidth = 2.2f;
    public float clusterDepth = 2.8f;

    public bool allowMultiLaneCluster = false;
    public int maxLanesPerCluster = 1;

    [Range(0f, 1f)]
    public float doubleRockChance = 0.15f;

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