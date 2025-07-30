using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AI;

public class FlockingAnimalAI : MonoBehaviour
{
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float roamRadius = 10f;
    public float flockSeparationDistance = 2.5f;
    public float flockCohesionWeight = 1f;
    public float flockSeparationWeight = 1.5f;
    public float flockAlignmentWeight = 1f;

    private float attackTimer;
    private NavMeshAgent agent;
    private Transform player;
    public CuteAnimalAnimHandler animHandler;

    private Vector3 flockTarget;
    private float newWanderTimer;
    private float wanderInterval = 3f;

    private static List<FlockingAnimalAI> allAnimals = new List<FlockingAnimalAI>();

    private enum State { Roaming, Chasing, Attacking }
    private State currentState = State.Roaming;

    private void OnEnable() => allAnimals.Add(this);
    private void OnDisable() => allAnimals.Remove(this);

    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animHandler = GetComponent<CuteAnimalAnimHandler>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        PickNewWanderTarget();
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Roaming:
                FlockMovement();
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = State.Chasing;
                }
                break;

            case State.Chasing:
                agent.SetDestination(player.position);
                animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

                if (distanceToPlayer <= attackRange)
                {
                    currentState = State.Attacking;
                    agent.isStopped = true;
                    attackTimer = attackCooldown;
                    animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);
                }
                else if (distanceToPlayer > detectionRange * 1.5f)
                {
                    currentState = State.Roaming;
                    agent.isStopped = false;
                    PickNewWanderTarget();
                }
                break;

            case State.Attacking:
                transform.LookAt(player.position);

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);
                    // You can add damage logic here
                }

                if (distanceToPlayer > attackRange)
                {
                    currentState = State.Chasing;
                    agent.isStopped = false;
                }
                break;
        }
    }

    void PickNewWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, NavMesh.AllAreas))
        {
            flockTarget = hit.position;
            agent.SetDestination(flockTarget);
            animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
        }
    }

    void FlockMovement()
    {
        newWanderTimer -= Time.deltaTime;

        if (newWanderTimer <= 0f || Vector3.Distance(transform.position, flockTarget) < 1.5f)
        {
            PickNewWanderTarget();
            newWanderTimer = wanderInterval;
        }

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int neighborCount = 0;

        foreach (var other in allAnimals)
        {
            if (other == this) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < flockSeparationDistance * 2)
            {
                // Separation
                separation += (transform.position - other.transform.position) / dist;

                // Alignment
                alignment += other.agent.velocity;

                // Cohesion
                cohesion += other.transform.position;

                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            separation = (separation / neighborCount).normalized;
            alignment = (alignment / neighborCount).normalized;
            cohesion = ((cohesion / neighborCount) - transform.position).normalized;

            Vector3 flockDirection =
                separation * flockSeparationWeight +
                alignment * flockAlignmentWeight +
                cohesion * flockCohesionWeight;

            Vector3 finalTarget = flockTarget + flockDirection * 3f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(finalTarget, out hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            animHandler?.SetAnimation(eCuteAnimalAnims.WALK);
        }
    }

    
}
