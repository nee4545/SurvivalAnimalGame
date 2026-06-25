using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WildFoodCarrierAI : MonoBehaviour
{
    private enum WildCarrierState
    {
        Roaming,
        Watching,
        EatingOffering,
        Recruited,
        Fleeing
    }

    [Header("References")]
    public NavMeshAgent agent;
    public CuteAnimalAnimHandler animHandler;
    public Transform roamCenter;

    [Header("Roaming")]
    public float roamRadius = 6f;
    public float minRoamWait = 1.5f;
    public float maxRoamWait = 4f;
    public float roamSpeed = 2f;
    public float destinationReachDistance = 0.75f;

    [Header("Reaction")]
    public float watchingSpeed = 1.5f;
    public float fleeSpeed = 6f;
    public float fleeDistance = 12f;

    [Header("Happy Reaction")]
    public float backflipJumpPower = 1.4f;
    public float backflipDuration = 0.65f;
    public Ease backflipEase = Ease.OutQuad;

    [Header("Recruitment Walk")]
    public float recruitedWalkSpeed = 3.5f;
    public float recruitedReachDistance = 1f;

    private bool isDoingBackflip;
    private Action onReachedRecruitmentPoint;

    [Header("Debug")]
    public bool debugLogs;

    private FoodCarrierRecruitmentSpot recruitmentSpot;
    private WildCarrierState currentState = WildCarrierState.Roaming;

    private float roamTimer;
    private Vector3 startPosition;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
    }

    private void Start()
    {
        startPosition = transform.position;

        if (roamCenter == null)
            roamCenter = transform;

        ResetWildCarrier();
    }

    private void Update()
    {
        switch (currentState)
        {
            case WildCarrierState.Roaming:
                UpdateRoaming();
                break;

            case WildCarrierState.Watching:
                UpdateWatching();
                break;

            case WildCarrierState.EatingOffering:
                break;

            case WildCarrierState.Recruited:
                break;

            case WildCarrierState.Fleeing:
                UpdateFleeing();
                break;
        }

        UpdateAnimation();
    }

    public void AssignRecruitmentSpot(FoodCarrierRecruitmentSpot spot)
    {
        recruitmentSpot = spot;

        if (roamCenter == null && spot != null)
            roamCenter = spot.transform;
    }

    public void OnRecruitmentStarted()
    {
        if (currentState == WildCarrierState.Recruited)
            return;

        ChangeState(WildCarrierState.Watching);
    }

    public void OnMeatDonated(int currentMeat, int requiredMeat)
    {
        if (currentState == WildCarrierState.Recruited)
            return;

        PlayHappyBackflip();

        if (debugLogs)
            Debug.Log($"[WildFoodCarrierAI] Happy meat reaction: {currentMeat}/{requiredMeat}", this);
    }

    private void PlayHappyBackflip()
    {
        if (isDoingBackflip)
            return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        isDoingBackflip = true;

        if (animHandler != null)
            animHandler.SetAnimation(eCuteAnimalAnims.JUMP);

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOJump(
                transform.position,
                backflipJumpPower,
                1,
                backflipDuration
            ).SetEase(backflipEase)
        );

        // Local X rotation usually gives a nice backflip.
        // If your animal flips sideways, change Vector3.right to Vector3.forward.
        sequence.Join(
            transform.DOLocalRotate(
                Vector3.right * -360f,
                backflipDuration,
                RotateMode.LocalAxisAdd
            )
        );

        sequence.OnComplete(() =>
        {
            isDoingBackflip = false;

            if (currentState == WildCarrierState.Recruited)
                return;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;

            ChangeState(WildCarrierState.Watching);
        });
    }


    public void MoveToRecruitmentPointAndDespawn(
    Transform recruitmentPoint,
    Action onReachedPoint
)
    {
        CancelInvoke();
        transform.DOKill();

        onReachedRecruitmentPoint = onReachedPoint;

        ChangeState(WildCarrierState.Recruited);

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            onReachedRecruitmentPoint?.Invoke();
            DespawnWildCarrier();
            return;
        }

        agent.isStopped = false;
        agent.speed = recruitedWalkSpeed;

        Vector3 targetPosition = recruitmentPoint != null
            ? recruitmentPoint.position
            : transform.position;

        agent.SetDestination(targetPosition);

        StartCoroutine(WaitUntilReachedRecruitmentPoint());
    }

    private IEnumerator WaitUntilReachedRecruitmentPoint()
    {
        while (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= recruitedReachDistance)
                break;

            yield return null;
        }

        onReachedRecruitmentPoint?.Invoke();
        onReachedRecruitmentPoint = null;

        DespawnWildCarrier();
    }

    private void DespawnWildCarrier()
    {
        PooledObject pooledObject = GetComponent<PooledObject>();

        if (pooledObject != null)
            pooledObject.Despawn();
        else
            gameObject.SetActive(false);
    }

    public void OnRecruitmentSuccess()
    {
        CancelInvoke();

        ChangeState(WildCarrierState.Recruited);

        if (debugLogs)
            Debug.Log("[WildFoodCarrierAI] Recruitment success.", this);

        PooledObject pooledObject = GetComponent<PooledObject>();

        if (pooledObject != null)
            pooledObject.Despawn(0.25f);
        else
            gameObject.SetActive(false);
    }

    public void OnRecruitmentFailed()
    {
        CancelInvoke();
        ChangeState(WildCarrierState.Fleeing);
    }

    public void ResetWildCarrier()
    {
        CancelInvoke();

        transform.position = startPosition;

        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = false;
            }

            agent.speed = roamSpeed;
        }

        ChangeState(WildCarrierState.Roaming);
    }

    private void UpdateRoaming()
    {
        roamTimer -= Time.deltaTime;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        bool reachedDestination =
            !agent.pathPending &&
            (!agent.hasPath || agent.remainingDistance <= destinationReachDistance);

        if (reachedDestination && roamTimer <= 0f)
        {
            SetRandomRoamDestination();
            roamTimer = UnityEngine.Random.Range(minRoamWait, maxRoamWait);
        }
    }

    private void UpdateWatching()
    {
        if (recruitmentSpot == null || recruitmentSpot.player == null)
            return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.speed = watchingSpeed;

        Vector3 playerPosition = recruitmentSpot.player.position;
        Vector3 directionAwayFromPlayer = transform.position - playerPosition;
        directionAwayFromPlayer.y = 0f;

        if (directionAwayFromPlayer.sqrMagnitude < 0.01f)
            directionAwayFromPlayer = -transform.forward;

        Vector3 watchPosition = playerPosition + directionAwayFromPlayer.normalized * 4f;

        if (NavMesh.SamplePosition(watchPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        RotateTowards(playerPosition);
    }

    private void UpdateFleeing()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        bool reachedDestination =
            !agent.pathPending &&
            (!agent.hasPath || agent.remainingDistance <= destinationReachDistance);

        if (reachedDestination)
        {
            ChangeState(WildCarrierState.Roaming);
        }
    }

    private void SetRandomRoamDestination()
    {
        if (roamCenter == null)
            return;

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * roamRadius;

        Vector3 target = roamCenter.position + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            agent.speed = roamSpeed;
            agent.SetDestination(hit.position);
        }
    }

    private void SetFleeDestination()
    {
        Vector3 fleeDirection = transform.forward;

        if (recruitmentSpot != null && recruitmentSpot.player != null)
        {
            fleeDirection = transform.position - recruitmentSpot.player.position;
            fleeDirection.y = 0f;

            if (fleeDirection.sqrMagnitude < 0.01f)
                fleeDirection = transform.forward;
        }

        Vector3 target = transform.position + fleeDirection.normalized * fleeDistance;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(hit.position);
        }
    }

    private void ReturnToWatching()
    {
        if (currentState == WildCarrierState.EatingOffering)
            ChangeState(WildCarrierState.Watching);
    }

    private void ChangeState(WildCarrierState newState)
    {
        currentState = newState;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (newState == WildCarrierState.EatingOffering)
                agent.ResetPath();

            if (newState == WildCarrierState.Fleeing)
                SetFleeDestination();
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * 5f
        );
    }

    private void UpdateAnimation()
    {
        if (animHandler == null)
            return;

        if (isDoingBackflip)
            return;

        if (currentState == WildCarrierState.EatingOffering)
            return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
            return;
        }

        float speed = agent.velocity.magnitude;

        if (speed > 0.15f)
            animHandler.SetAnimation(eCuteAnimalAnims.WALK);
        else
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }
}