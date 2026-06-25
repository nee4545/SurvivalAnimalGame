using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PooledObject))]
public class CubPoolReset : MonoBehaviour, IPoolable
{
    private AnimalCubAI cubAI;
    private Health health;
    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        cubAI = GetComponent<AnimalCubAI>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    public void OnSpawned()
    {
        if (health)
            health.ResetHealth();

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (agent)
        {
            Vector3 intendedSpawnPosition = transform.position;
            Quaternion intendedSpawnRotation = transform.rotation;

            agent.updatePosition = false;
            agent.updateRotation = false;

            if (!agent.enabled)
                agent.enabled = true;

            if (NavMesh.SamplePosition(
                    intendedSpawnPosition,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.nextPosition = hit.position;

                transform.SetPositionAndRotation(
                    hit.position,
                    intendedSpawnRotation
                );
            }
            else
            {
                transform.SetPositionAndRotation(
                    intendedSpawnPosition,
                    intendedSpawnRotation
                );
            }

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = false;
            }

            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        if (cubAI)
        {
            cubAI.StopAllCoroutines();
            cubAI.enabled = true;
            cubAI.ResetCubAI();
        }
    }

    public void OnDespawned()
    {
        if (cubAI)
        {
            cubAI.StopAllCoroutines();
            cubAI.enabled = false;
        }

        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }
}