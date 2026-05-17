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
            agent.enabled = true;

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