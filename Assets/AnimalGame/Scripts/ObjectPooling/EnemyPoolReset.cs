using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PooledObject))]
public class EnemyPoolReset : MonoBehaviour, IPoolable
{
    CuteAnimalAI ai;
    Health health;
    Animator animator;
    NavMeshAgent agent;

    void Awake()
    {
        ai = GetComponent<CuteAnimalAI>();
        health = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void OnSpawned()
    {
        if (health) health.ResetHealth();      // implement ResetHealth() below
        if (animator) { animator.Rebind(); animator.Update(0f); }

        if (ai)
        {
            ai.StopAllCoroutines();
            ai.jumpSessionActive = false;
            ai.wasProvoked = false;
            ai.currentChargeAttempts = 0;
            ai.isChargeCooldownActive = false;

            if (agent)
            {
                agent.isStopped = false;
                agent.ResetPath();
                agent.updatePosition = true;
                agent.updateRotation = true;
            }

            // pick a sensible starting state
            switch (ai.aiType)
            {
                case CuteAnimalAI.AIType.AggressiveJumping: ai.StateMachine.ChangeState(new AIPerchRestState(ai)); break;
                case CuteAnimalAI.AIType.Companion: ai.StateMachine.ChangeState(new AICompanionFollowState(ai)); break;
                default: ai.StateMachine.ChangeState(new AIWanderState(ai)); break;
            }

            ai.spawnPosition = transform.position; // treat spawn point as new home
        }
    }

    public void OnDespawned()
    {
        if (agent) { agent.isStopped = true; agent.ResetPath(); }
        if (ai) ai.StopAllCoroutines();
    }
}
