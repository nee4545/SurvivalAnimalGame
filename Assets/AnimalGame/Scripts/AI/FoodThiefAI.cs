using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class FoodThiefAI : MonoBehaviour
{
    private enum ThiefState
    {
        SearchingForMeat,
        MovingToMeat,
        EatingMeat,
        FleeingFromPlayer
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public CuteAnimalAnimHandler animHandler;

    [Header("Meat Search")]
    public float meatSearchRadius = 40f;
    public float meatCheckInterval = 0.35f;
    public float meatReachDistance = 1.25f;

    [Header("Eating")]
    public float eatDuration = 1.2f;
    public bool despawnMeatAfterEating = true;

    [Header("Flee")]
    public float fleeDistance = 18f;
    public float fleeSpeed = 7f;
    public float fleeDuration = 2.5f;
    public float safeDistanceFromPlayer = 14f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float idleSpeed = 2f;

    [Header("Optional Despawn")]
    public bool despawnAfterLongNoMeat = false;
    public float noMeatDespawnTime = 20f;

    [Header("Debug")]
    public bool debugLogs;

    private ThiefState currentState;
    private GameObject targetMeat;
    private Coroutine stateRoutine;

    private float nextMeatCheckTime;
    private float noMeatTimer;

    private void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!animHandler)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
    }

    private void Start()
    {
        ChangeState(ThiefState.SearchingForMeat);
    }

    private void Update()
    {
        switch (currentState)
        {
            case ThiefState.SearchingForMeat:
                UpdateSearchingForMeat();
                break;

            case ThiefState.MovingToMeat:
                UpdateMovingToMeat();
                break;

            case ThiefState.EatingMeat:
                break;

            case ThiefState.FleeingFromPlayer:
                break;
        }

        UpdateAnimation();
    }

    private void UpdateSearchingForMeat()
    {
        if (despawnAfterLongNoMeat)
        {
            noMeatTimer += Time.deltaTime;

            if (noMeatTimer >= noMeatDespawnTime)
            {
                DespawnSelf();
                return;
            }
        }

        if (Time.time < nextMeatCheckTime)
            return;

        nextMeatCheckTime = Time.time + meatCheckInterval;

        TryFindMeat();
    }

    private void UpdateMovingToMeat()
    {
        if (!IsMeatValid(targetMeat))
        {
            ReleaseCurrentMeatClaim();
            targetMeat = null;
            ChangeState(ThiefState.SearchingForMeat);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetMeat.transform.position);

        if (distance <= meatReachDistance)
        {
            ChangeState(ThiefState.EatingMeat);
            return;
        }

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = moveSpeed;

            if (!agent.hasPath || agent.remainingDistance <= 0.2f)
                agent.SetDestination(targetMeat.transform.position);
        }
    }

    private void TryFindMeat()
    {
        if (FoodCarrierDirector.Instance == null)
            return;

        bool found = FoodCarrierDirector.Instance.TryClaimNearestMeat(
            transform.position,
            meatSearchRadius,
            out GameObject meat
        );

        if (!found)
            return;

        if (!IsMeatValid(meat))
        {
            FoodCarrierDirector.Instance.ReleaseClaim(meat);
            return;
        }

        targetMeat = meat;
        noMeatTimer = 0f;

        if (debugLogs)
            Debug.Log($"[FoodThiefAI] Claimed meat: {targetMeat.name}");

        ChangeState(ThiefState.MovingToMeat);
    }

    public void SetInitialMeatTarget(GameObject meat)
    {
        if (!IsMeatValid(meat))
            return;

        ReleaseCurrentMeatClaim();

        targetMeat = meat;

        if (FoodCarrierDirector.Instance != null)
            FoodCarrierDirector.Instance.UnregisterMeat(meat);

        ChangeState(ThiefState.MovingToMeat);
    }

    private bool IsMeatValid(GameObject meat)
    {
        if (meat == null)
            return false;

        if (!meat.activeInHierarchy)
            return false;

        if (meat.transform.parent != null)
            return false;

        MeatPickup pickup = meat.GetComponent<MeatPickup>();

        if (pickup == null)
            return false;

        if (!pickup.canBePickedUp)
            return false;

        return true;
    }

    private void ChangeState(ThiefState newState)
    {
        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        currentState = newState;

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();

            switch (currentState)
            {
                case ThiefState.SearchingForMeat:
                    agent.speed = idleSpeed;
                    break;

                case ThiefState.MovingToMeat:
                    agent.speed = moveSpeed;

                    if (targetMeat)
                        agent.SetDestination(targetMeat.transform.position);
                    break;

                case ThiefState.EatingMeat:
                    agent.speed = 0f;
                    agent.isStopped = true;
                    stateRoutine = StartCoroutine(EatMeatRoutine());
                    break;

                case ThiefState.FleeingFromPlayer:
                    agent.speed = fleeSpeed;
                    agent.isStopped = false;
                    stateRoutine = StartCoroutine(FleeRoutine());
                    break;
            }
        }
    }

    private IEnumerator EatMeatRoutine()
    {
        if (animHandler)
            animHandler.SetAnimation(eCuteAnimalAnims.EAT);

        yield return new WaitForSeconds(eatDuration);

        if (IsMeatValid(targetMeat))
        {
            GameObject meat = targetMeat;
            targetMeat = null;

            FoodCarrierDirector.Instance?.UnregisterMeat(meat);

            if (despawnMeatAfterEating)
                DespawnMeat(meat);
        }
        else
        {
            targetMeat = null;
        }

        if (agent && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        ChangeState(ThiefState.SearchingForMeat);
    }

    private IEnumerator FleeRoutine()
    {
        ReleaseCurrentMeatClaim();
        targetMeat = null;

        Vector3 fleeDir = GetFleeDirection();
        Vector3 fleeTarget = transform.position + fleeDir * fleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            fleeTarget = hit.position;

        if (agent && agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(fleeTarget);

        float timer = 0f;

        while (timer < fleeDuration)
        {
            timer += Time.deltaTime;

            if (player)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                if (distanceToPlayer >= safeDistanceFromPlayer)
                    break;
            }

            yield return null;
        }

        ChangeState(ThiefState.SearchingForMeat);
    }

    public void ResetFoodThiefAI()
    {
        StopAllCoroutines();

        ReleaseCurrentMeatClaim();

        targetMeat = null;
        stateRoutine = null;
        nextMeatCheckTime = 0f;
        noMeatTimer = 0f;

        transform.DOKill();

        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!animHandler)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        if (agent)
        {
            agent.enabled = true;

            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = false;
            }

            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.speed = idleSpeed;
        }

        ChangeState(ThiefState.SearchingForMeat);
    }

    public void PrepareFoodThiefForPoolDespawn()
    {
        StopAllCoroutines();

        ReleaseCurrentMeatClaim();

        targetMeat = null;
        stateRoutine = null;

        transform.DOKill();

        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    private Vector3 GetFleeDirection()
    {
        if (!player)
            return -transform.forward;

        Vector3 dir = transform.position - player.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            dir = -transform.forward;

        return dir.normalized;
    }

    public void NotifyAttackedByPlayer(Transform attacker)
    {
        if (attacker)
            player = attacker;

        if (currentState == ThiefState.FleeingFromPlayer)
            return;

        ChangeState(ThiefState.FleeingFromPlayer);
    }

    private void ReleaseCurrentMeatClaim()
    {
        if (targetMeat != null && FoodCarrierDirector.Instance != null)
            FoodCarrierDirector.Instance.ReleaseClaim(targetMeat);
    }

    private void DespawnMeat(GameObject meat)
    {
        if (!meat)
            return;

        meat.transform.DOKill();

        PooledObject pooled = meat.GetComponent<PooledObject>();

        if (pooled)
            pooled.Despawn();
        else
            Destroy(meat);
    }

    private void DespawnSelf()
    {
        ReleaseCurrentMeatClaim();

        PooledObject pooled = GetComponent<PooledObject>();

        if (pooled)
            pooled.Despawn();
        else
            Destroy(gameObject);
    }

    private void UpdateAnimation()
    {
        if (!animHandler)
            return;

        if (currentState == ThiefState.EatingMeat)
        {
            animHandler.SetAnimation(eCuteAnimalAnims.EAT);
            return;
        }

        float speed = agent && agent.enabled ? agent.velocity.magnitude : 0f;

        if (speed > 0.1f)
            animHandler.SetAnimation(eCuteAnimalAnims.RUN);
        else
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }

    private void OnDisable()
    {
        ReleaseCurrentMeatClaim();
        targetMeat = null;

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }
    }
}