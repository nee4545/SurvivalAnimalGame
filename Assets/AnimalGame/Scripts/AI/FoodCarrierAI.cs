using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class FoodCarrierAI : MonoBehaviour
{
    private enum CarrierState
    {
        FollowPlayer,
        MoveToMeat,
        ReturningToBase,
        DepositingFood,
        ReturningToPlayer
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public HomeFoodPoint foodPoint;
    public Transform carryPoint;
    public CuteAnimalAnimHandler animHandler;

    [Header("Follow Settings")]
    public float followDistance = 4f;
    public float followSideOffset = 1.5f;
    public float followRepathInterval = 0.25f;
    public float returnToPlayerDistance = 6f;

    [Header("Meat Search")]
    public float meatSearchRadius = 35f;
    public float meatPickupDistance = 1.4f;
    public float meatCheckInterval = 0.35f;

    [Header("Carry Settings")]
    public int maxCarryAmount = 3;
    public float verticalStackOffset = 0.25f;
    public float randomRotationAmount = 20f;

    [Header("Tween Settings")]
    public float pickupMoveDuration = 0.35f;
    public float pickupJumpPower = 0.75f;
    public Ease pickupEase = Ease.OutBack;

    public float depositMoveDuration = 0.25f;
    public float depositJumpPower = 0.5f;
    public Ease depositEase = Ease.OutQuad;

    [Header("Movement")]
    public float followSpeed = 4f;
    public float collectSpeed = 5f;
    public float returnBaseSpeed = 6f;

    [Header("Huge Map Safety")]
    public bool teleportBackToPlayerIfTooFar = true;
    public float teleportBackDistance = 120f;
    public Vector3 teleportBackOffset = new Vector3(-3f, 0f, -3f);

    [Header("Worker Emotion UI")]
    public GameObject emojiCloud;
    public GameObject huntEmoji;
    public GameObject deliverEmoji;
    public TextMeshProUGUI emotionText;

    [Header("Worker Emotion Timing")]
    public float emotionDisplayDuration = 2.5f;

    [Tooltip("Tag used to detect the player bumping into this worker.")]
    public string playerTag = "Player";

    private Coroutine emotionRoutine;
    private bool stateInitialized;

    private enum CarrierEmotionMode
    {
        None,
        Hunting,
        Delivering
    }

    private CarrierEmotionMode currentEmotionMode;

    [TextArea]
    public string huntText = "Let us find food for cubs";

    [TextArea]
    public string deliverText = "Dropping off meat";

    [Header("Debug")]
    public bool debugLogs;

    private CarrierState currentState;
    private readonly List<GameObject> carriedMeat = new();

    private GameObject targetMeat;
    private float nextMeatCheckTime;
    private float nextFollowRepathTime;

    public int CurrentCarryCount => carriedMeat.Count;
    public bool IsFull => carriedMeat.Count >= maxCarryAmount;
    public bool HasMeat => carriedMeat.Count > 0;

    private void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!animHandler)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        HideEmotionVisuals();
    }

    private void Start()
    {
        ChangeState(CarrierState.FollowPlayer);
    }

    private void Update()
    {
        switch (currentState)
        {
            case CarrierState.FollowPlayer:
                UpdateFollowPlayer();
                break;

            case CarrierState.MoveToMeat:
                UpdateMoveToMeat();
                break;

            case CarrierState.ReturningToBase:
                UpdateReturningToBase();
                break;

            case CarrierState.DepositingFood:
                break;

            case CarrierState.ReturningToPlayer:
                UpdateReturningToPlayer();
                break;
        }
        UpdateAnimation();
    }


    private CarrierEmotionMode GetEmotionMode(CarrierState state)
    {
        switch (state)
        {
            case CarrierState.ReturningToBase:
            case CarrierState.DepositingFood:
                return CarrierEmotionMode.Delivering;

            case CarrierState.FollowPlayer:
            case CarrierState.MoveToMeat:
            case CarrierState.ReturningToPlayer:
                return CarrierEmotionMode.Hunting;

            default:
                return CarrierEmotionMode.None;
        }
    }

    private void ShowCurrentEmotion()
    {
        CarrierEmotionMode mode = GetEmotionMode(currentState);

        if (mode == CarrierEmotionMode.None)
        {
            HideEmotionVisuals();
            return;
        }

        if (emotionRoutine != null)
            StopCoroutine(emotionRoutine);

        emotionRoutine = StartCoroutine(
            ShowEmotionRoutine(mode, emotionDisplayDuration)
        );
    }

    private IEnumerator ShowEmotionRoutine(
        CarrierEmotionMode mode,
        float duration
    )
    {
        if (emojiCloud)
            emojiCloud.SetActive(true);

        bool showHunt = mode == CarrierEmotionMode.Hunting;
        bool showDeliver = mode == CarrierEmotionMode.Delivering;

        if (huntEmoji)
            huntEmoji.SetActive(showHunt);

        if (deliverEmoji)
            deliverEmoji.SetActive(showDeliver);

        if (emotionText)
        {
            emotionText.gameObject.SetActive(true);

            switch (mode)
            {
                case CarrierEmotionMode.Hunting:
                    emotionText.text = huntText;
                    break;

                case CarrierEmotionMode.Delivering:
                    emotionText.text = deliverText;
                    break;

                default:
                    emotionText.text = "";
                    break;
            }
        }

        yield return new WaitForSeconds(duration);

        HideEmotionVisuals();
        emotionRoutine = null;
    }

    private void HideEmotionVisuals()
    {
        if (huntEmoji)
            huntEmoji.SetActive(false);

        if (deliverEmoji)
            deliverEmoji.SetActive(false);

        if (emotionText)
        {
            emotionText.text = "";
            emotionText.gameObject.SetActive(false);
        }

        if (emojiCloud)
            emojiCloud.SetActive(false);
    }


    public void ResetCarrierAI()
    {

        if (emotionRoutine != null)
        {
            StopCoroutine(emotionRoutine);
            emotionRoutine = null;
        }

        stateInitialized = false;
        currentEmotionMode = CarrierEmotionMode.None;
        HideEmotionVisuals();

        StopAllCoroutines();

        if (targetMeat != null && FoodCarrierDirector.Instance != null)
            FoodCarrierDirector.Instance.ReleaseClaim(targetMeat);

        targetMeat = null;
        nextMeatCheckTime = 0f;
        nextFollowRepathTime = 0f;

        ClearCarriedMeatForPool();

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
        }

        ChangeState(CarrierState.FollowPlayer);
    }

    public void PrepareCarrierForPoolDespawn()
    {
        if (emotionRoutine != null)
        {
            StopCoroutine(emotionRoutine);
            emotionRoutine = null;
        }

        stateInitialized = false;
        currentEmotionMode = CarrierEmotionMode.None;
        HideEmotionVisuals();

        StopAllCoroutines();

        if (targetMeat != null && FoodCarrierDirector.Instance != null)
            FoodCarrierDirector.Instance.ReleaseClaim(targetMeat);

        targetMeat = null;

        ClearCarriedMeatForPool();

        transform.DOKill();

        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    private void ClearCarriedMeatForPool()
    {
        for (int i = carriedMeat.Count - 1; i >= 0; i--)
        {
            GameObject meat = carriedMeat[i];

            if (!meat)
                continue;

            meat.transform.DOKill();
            meat.transform.SetParent(null, true);

            PooledObject pooled = meat.GetComponent<PooledObject>();

            if (pooled)
                pooled.Despawn();
            else
                Destroy(meat);
        }

        carriedMeat.Clear();
    }

    private void OnDisable()
    {
        if (targetMeat != null && FoodCarrierDirector.Instance != null)
            FoodCarrierDirector.Instance.ReleaseClaim(targetMeat);

        targetMeat = null;

        if (emotionRoutine != null)
        {
            StopCoroutine(emotionRoutine);
            emotionRoutine = null;
        }

        HideEmotionVisuals();
    }

    private void UpdateFollowPlayer()
    {
        TryFindMeat();

        if (targetMeat)
        {
            ChangeState(CarrierState.MoveToMeat);
            return;
        }

        FollowPlayer();
    }

    private void UpdateMoveToMeat()
    {
        if (!IsTargetMeatValid(targetMeat))
        {
            FoodCarrierDirector.Instance?.ReleaseClaim(targetMeat);

            targetMeat = null;

            TryFindMeat(true);

            if (targetMeat)
                ChangeState(CarrierState.MoveToMeat);
            else
                ChangeState(HasMeat ? CarrierState.ReturningToBase : CarrierState.FollowPlayer);

            return;
        }

        float distance = Vector3.Distance(transform.position, targetMeat.transform.position);

        if (distance <= meatPickupDistance)
        {
            TryPickupTargetMeat();
            return;
        }

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = collectSpeed;

            if (!agent.hasPath || agent.remainingDistance < 0.2f)
                agent.SetDestination(targetMeat.transform.position);
        }
    }

    private void UpdateReturningToBase()
    {
        if (!foodPoint)
        {
            ChangeState(CarrierState.ReturningToPlayer);
            return;
        }

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.hasPath)
                agent.SetDestination(foodPoint.transform.position);

            float distance = Vector3.Distance(transform.position, foodPoint.transform.position);

            if (distance <= 2f)
            {
                StartCoroutine(DepositFoodRoutine());
                ChangeState(CarrierState.DepositingFood);
            }
        }
    }

    private void UpdateReturningToPlayer()
    {
        if (!player)
            return;

        if (!IsFull)
        {
            TryFindMeat();

            if (targetMeat)
            {
                ChangeState(CarrierState.MoveToMeat);
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (teleportBackToPlayerIfTooFar && distanceToPlayer >= teleportBackDistance)
        {
            TeleportNearPlayer();
            ChangeState(CarrierState.FollowPlayer);
            return;
        }

        if (distanceToPlayer <= returnToPlayerDistance)
        {
            ChangeState(CarrierState.FollowPlayer);
            return;
        }

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = followSpeed;

            if (Time.time >= nextFollowRepathTime)
            {
                nextFollowRepathTime = Time.time + followRepathInterval;
                agent.SetDestination(GetFollowPosition());
            }
        }
    }

    private bool IsTargetMeatValid(GameObject meat)
    {
        if (meat == null)
            return false;

        if (!meat.activeInHierarchy)
            return false;

        MeatPickup pickup = meat.GetComponent<MeatPickup>();

        if (pickup == null)
            return false;

        if (!pickup.canBePickedUp)
            return false;

        // If meat is already parented to player/carrier/food point, ignore it.
        if (meat.transform.parent != null)
            return false;

        return true;
    }

    private void TryFindMeat(bool force = false)
    {
        if (IsFull)
        {
            ChangeState(CarrierState.ReturningToBase);
            return;
        }

        if (!force && Time.time < nextMeatCheckTime)
            return;

        nextMeatCheckTime = Time.time + meatCheckInterval;

        if (FoodCarrierDirector.Instance == null)
            return;

        Vector3 searchFrom = player ? player.position : transform.position;

        bool found = FoodCarrierDirector.Instance.TryClaimNearestMeat(
            searchFrom,
            meatSearchRadius,
            out GameObject meat
        );

        if (found)
        {
            if (!IsTargetMeatValid(meat))
            {
                FoodCarrierDirector.Instance.ReleaseClaim(meat);
                return;
            }

            targetMeat = meat;

            if (debugLogs)
                Debug.Log($"[FoodCarrierAI] Claimed meat: {targetMeat.name}");
        }
    }

    private void TryPickupTargetMeat()
    {
        if (targetMeat == null)
        {
            ChangeState(HasMeat ? CarrierState.ReturningToBase : CarrierState.FollowPlayer);
            return;
        }

        if (IsFull)
        {
            FoodCarrierDirector.Instance?.ReleaseClaim(targetMeat);
            targetMeat = null;
            ChangeState(CarrierState.ReturningToBase);
            return;
        }

        GameObject meat = targetMeat;
        targetMeat = null;

        FoodCarrierDirector.Instance?.UnregisterMeat(meat);

        CarryMeat(meat);

        if (IsFull)
        {
            ChangeState(CarrierState.ReturningToBase);
        }
        else
        {
            TryFindMeat(true);

            if (targetMeat)
                ChangeState(CarrierState.MoveToMeat);
            else
                ChangeState(CarrierState.ReturningToBase);
        }
    }

    private void CarryMeat(GameObject meat)
    {
        if (!meat || !carryPoint)
            return;

        MeatPickup pickup = meat.GetComponent<MeatPickup>();

        if (pickup)
            pickup.canBePickedUp = false;

        carriedMeat.Add(meat);

        int index = carriedMeat.Count - 1;

        Collider col = meat.GetComponent<Collider>();
        if (col)
            col.enabled = false;

        Transform meatTransform = meat.transform;
        meatTransform.DOKill();

        Vector3 targetLocalPos = new Vector3(
            0f,
            index * verticalStackOffset,
            0f
        );

        Quaternion targetLocalRot = Quaternion.Euler(
            Random.Range(-randomRotationAmount, randomRotationAmount),
            Random.Range(0f, 360f),
            Random.Range(-randomRotationAmount, randomRotationAmount)
        );

        meatTransform.SetParent(carryPoint, true);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            meatTransform.DOLocalJump(
                targetLocalPos,
                pickupJumpPower,
                1,
                pickupMoveDuration
            ).SetEase(pickupEase)
        );

        sequence.Join(
            meatTransform.DOLocalRotateQuaternion(
                targetLocalRot,
                pickupMoveDuration
            )
        );

        sequence.OnComplete(() =>
        {
            if (!meatTransform)
                return;

            meatTransform.localPosition = targetLocalPos;
            meatTransform.localRotation = targetLocalRot;
        });
    }

    private System.Collections.IEnumerator DepositFoodRoutine()
    {
        if (!foodPoint)
        {
            ChangeState(CarrierState.ReturningToPlayer);
            yield break;
        }

        while (carriedMeat.Count > 0)
        {
            if (foodPoint.CurrentFoodCount >= foodPoint.maxFoodCapacity)
                break;

            GameObject meat = RemoveTopMeat();

            if (!meat)
                continue;

            bool stored = foodPoint.TryStoreExternalMeat(meat);

            if (!stored)
            {
                CarryMeat(meat);
                break;
            }

            yield return new WaitForSeconds(0.12f);
        }

        ChangeState(CarrierState.ReturningToPlayer);
    }

    private GameObject RemoveTopMeat()
    {
        if (carriedMeat.Count == 0)
            return null;

        int lastIndex = carriedMeat.Count - 1;
        GameObject meat = carriedMeat[lastIndex];

        carriedMeat.RemoveAt(lastIndex);

        if (meat)
        {
            meat.transform.DOKill();
            meat.transform.SetParent(null, true);
        }

        return meat;
    }

    private void FollowPlayer()
    {
        if (!player || !agent || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.speed = followSpeed;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= followDistance)
        {
            if (agent.hasPath)
                agent.ResetPath();

            return;
        }

        if (Time.time >= nextFollowRepathTime)
        {
            nextFollowRepathTime = Time.time + followRepathInterval;
            agent.SetDestination(GetFollowPosition());
        }
    }

    private Vector3 GetFollowPosition()
    {
        if (!player)
            return transform.position;

        Vector3 behind = -player.forward * followDistance;
        Vector3 side = player.right * followSideOffset;

        Vector3 target = player.position + behind + side;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            return hit.position;

        return player.position;
    }

    private void TeleportNearPlayer()
    {
        if (!player || !agent)
            return;

        Vector3 target = player.position + player.TransformDirection(teleportBackOffset);

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            transform.position = hit.position;
        }
    }

    private void ChangeState(CarrierState newState)
    {
        CarrierEmotionMode previousEmotionMode =
            stateInitialized
                ? GetEmotionMode(currentState)
                : CarrierEmotionMode.None;

        CarrierEmotionMode newEmotionMode =
            GetEmotionMode(newState);

        bool emotionTaskChanged =
            stateInitialized &&
            previousEmotionMode != newEmotionMode;

        currentState = newState;
        currentEmotionMode = newEmotionMode;
        stateInitialized = true;

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();

            switch (currentState)
            {
                case CarrierState.FollowPlayer:
                    agent.speed = followSpeed;
                    break;

                case CarrierState.MoveToMeat:
                    agent.speed = collectSpeed;

                    if (targetMeat)
                        agent.SetDestination(targetMeat.transform.position);
                    break;

                case CarrierState.ReturningToBase:
                    agent.speed = returnBaseSpeed;

                    if (foodPoint)
                        agent.SetDestination(foodPoint.transform.position);
                    break;

                case CarrierState.DepositingFood:
                    agent.speed = 0f;
                    break;

                case CarrierState.ReturningToPlayer:
                    agent.speed = followSpeed;

                    if (player)
                        agent.SetDestination(GetFollowPosition());
                    break;
            }
        }

        // Only display when switching between hunting and delivering.
        if (emotionTaskChanged)
            ShowCurrentEmotion();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        ShowCurrentEmotion();
    }

    private void UpdateAnimation()
    {
        if (!animHandler || !agent || !agent.enabled)
            return;

        if (agent.velocity.magnitude > 0.1f)
            animHandler.SetAnimation(eCuteAnimalAnims.WALK);
        else
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }
}