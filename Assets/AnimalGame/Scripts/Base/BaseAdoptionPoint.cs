using System.Collections;
using UnityEngine;

public class BaseAdoptionPoint : MonoBehaviour
{
    [Header("References")]
    public BaseCubManager baseCubManager;
    public GameObject normalCubPrefab;
    public Transform cubSpawnPoint;
    public Transform adoptionWalkPoint;

    [Header("Adoption")]
    public float adoptDelay = 0.15f;

    private bool isAdopting;

    private void OnTriggerEnter(Collider other)
    {
        PlayerCubCarrier carrier = other.GetComponentInChildren<PlayerCubCarrier>();

        if (!carrier)
            return;

        if (!isAdopting)
            StartCoroutine(AdoptRoutine(carrier));
    }

    private IEnumerator AdoptRoutine(PlayerCubCarrier carrier)
    {
        isAdopting = true;

        while (carrier.HasCub && baseCubManager != null && baseCubManager.HasSpace)
        {
            OrphanCubAI orphanCub = carrier.RemoveTopCub();

            if (!orphanCub)
                break;

            Transform walkTarget = adoptionWalkPoint ? adoptionWalkPoint : transform;

            bool reachedPoint = false;

            orphanCub.StartMovingToAdoptionPoint(
            walkTarget,
            carrier.transform,
            cub =>
            {
                reachedPoint = true;
            }
            );

            while (!reachedPoint)
                yield return null;

            Vector3 spawnPos = cubSpawnPoint
                ? cubSpawnPoint.position
                : walkTarget.position;

            Quaternion spawnRot = cubSpawnPoint
                ? cubSpawnPoint.rotation
                : walkTarget.rotation;

            SpawnNormalCub(spawnPos, spawnRot);
            DespawnOrphan(orphanCub);

            yield return new WaitForSeconds(adoptDelay);
        }

        isAdopting = false;
    }

    private void SpawnNormalCub(Vector3 position, Quaternion rotation)
    {
        if (!normalCubPrefab)
            return;

        GameObject cubObj = PoolManager.Spawn(normalCubPrefab, position, rotation);

        AnimalCubAI cubAI = cubObj.GetComponent<AnimalCubAI>();

        if (cubAI != null && baseCubManager != null)
        {
            baseCubManager.RegisterCub(cubAI);
        }
    }

    private void DespawnOrphan(OrphanCubAI orphanCub)
    {
        if (!orphanCub)
            return;

        PooledObject pooledObject = orphanCub.GetComponent<PooledObject>();

        if (pooledObject)
            pooledObject.Despawn();
        else
            Destroy(orphanCub.gameObject);
    }
}