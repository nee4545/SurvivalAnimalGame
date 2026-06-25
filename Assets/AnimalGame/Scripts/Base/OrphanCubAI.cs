using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class OrphanCubAI : MonoBehaviour
{
    private enum OrphanState
    {
        Roaming,
        Carried,
        GoingToAdoptionPoint,
        Dead
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Health health;
    public CuteAnimalAnimHandler animHandler;

    [Header("Roaming")]
    public float roamRadius = 6f;
    public float minRoamWaitTime = 1.5f;
    public float maxRoamWaitTime = 4f;
    public float destinationReachDistance = 0.75f;
    public float roamSpeed = 2f;

    [Header("Survival")]
    public float healthDrainPerSecond = 1f;

    [Header("Dismount Jump")]
    public float dismountJumpForwardDistance = 2.5f;
    public float dismountJumpHeight = 1.2f;
    public float dismountJumpDuration = 0.35f;

    [Header("Adoption")]
    public float adoptionDistance = 2f;
    public BaseCubManager baseCubManager;

    [Header("Emotion UI")]
    public GameObject adoptMeEmoji;
    public GameObject goingHomeEmoji;
    public GameObject loveEmoji;
    public TextMeshProUGUI emotionText;

    [Header("Player Thought Cooldown")]
    public float baseFullThoughtCooldown = 1.5f;
    private float nextBaseFullThoughtTime;

    [Header("UI")]
    public GameObject healthBarObject;

    public string adoptMeText = "Adopt me";
    public string goingHomeText = "Yay Going Home";
    public string thankYouText = "Thank you";

    private OrphanState currentState;
    private Transform player;
    private PlayerCubCarrier playerCarrier;
    private Vector3 spawnCenter;
    private float roamTimer;

    private void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!health)
            health = GetComponent<Health>();

        if (!animHandler)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        spawnCenter = transform.position;
    }

    private void Start()
    {
        ChangeState(OrphanState.Roaming);
    }

    private void Update()
    {

        if (currentState == OrphanState.Dead ||
     currentState == OrphanState.Carried ||
     currentState == OrphanState.GoingToAdoptionPoint)
            return;

        DrainHealth();
        TryFindPlayer();
        TryAdopt();
        UpdateRoaming();
        UpdateAnimation();
    }

    private void UpdateHealthBarState()
    {
        if (!healthBarObject)
            return;

        bool showHealthBar =
            currentState == OrphanState.Roaming;

        healthBarObject.SetActive(showHealthBar);
    }

    private void UpdateEmotionState()
    {
        bool showAdoptMe = currentState == OrphanState.Roaming;
        bool showGoingHome = currentState == OrphanState.Carried;
        bool showLove = currentState == OrphanState.GoingToAdoptionPoint;

        if (adoptMeEmoji)
            adoptMeEmoji.SetActive(showAdoptMe);

        if (goingHomeEmoji)
            goingHomeEmoji.SetActive(showGoingHome);

        if (loveEmoji)
            loveEmoji.SetActive(showLove);

        if (emotionText)
        {
            if (showAdoptMe)
                emotionText.text = adoptMeText;
            else if (showGoingHome)
                emotionText.text = goingHomeText;
            else if (showLove)
                emotionText.text = thankYouText;
            else
                emotionText.text = "";
        }
    }

    private void DrainHealth()
    {
        if (!health || health.IsDead)
            return;

        health.TakeDamage(healthDrainPerSecond * Time.deltaTime,null,false);

        if (health.IsDead)
            Die();
    }

    private void TryFindPlayer()
    {
        if (playerCarrier != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (!playerObj)
            return;

        player = playerObj.transform;
        playerCarrier = playerObj.GetComponentInChildren<PlayerCubCarrier>();
    }

    private void TryAdopt()
    {
        if (!player || !playerCarrier || !baseCubManager)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Important:
        // Do not show any adoption warning unless player is actually near this orphan.
        if (distance > adoptionDistance)
            return;

        if (!baseCubManager.HasSpace)
        {
            if (Time.time >= nextBaseFullThoughtTime)
            {
                PlayerThoughtUI thoughtUI = player.GetComponentInChildren<PlayerThoughtUI>();

                if (thoughtUI)
                    thoughtUI.ShowBaseFull();

                nextBaseFullThoughtTime = Time.time + baseFullThoughtCooldown;
            }

            return;
        }

        if (playerCarrier.IsFull)
            return;

        bool adopted = playerCarrier.TryCarryCub(this);

        if (adopted)
            ChangeState(OrphanState.Carried);
    }

    private void UpdateRoaming()
    {
        if (!agent || !agent.enabled)
            return;

        roamTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= destinationReachDistance)
        {
            if (roamTimer <= 0f)
            {
                SetRandomRoamDestination();
                roamTimer = Random.Range(minRoamWaitTime, maxRoamWaitTime);
            }
        }
    }

    public void StartMovingToAdoptionPoint(
    Transform adoptionPoint,
    Transform playerTransform,
    System.Action<OrphanCubAI> onReached)
    {
        StartCoroutine(MoveToAdoptionPointRoutine(adoptionPoint, playerTransform, onReached));
    }

    private IEnumerator MoveToAdoptionPointRoutine(
    Transform adoptionPoint,
    Transform playerTransform,
    System.Action<OrphanCubAI> onReached)
    {
        if (!adoptionPoint)
            yield break;

        ChangeState(OrphanState.GoingToAdoptionPoint);

        transform.SetParent(null, true);

        Collider col = GetComponent<Collider>();
        if (col)
            col.enabled = false;

        if (agent)
            agent.enabled = false;

        if (animHandler)
            animHandler.SetAnimation(eCuteAnimalAnims.JUMP);

        Vector3 start = transform.position;

        Vector3 jumpDir = playerTransform
            ? playerTransform.forward
            : transform.forward;

        jumpDir.y = 0f;

        if (jumpDir.sqrMagnitude < 0.01f)
            jumpDir = transform.forward;

        jumpDir.Normalize();

        Vector3 jumpEnd = start + jumpDir * dismountJumpForwardDistance;

        if (NavMesh.SamplePosition(jumpEnd, out NavMeshHit jumpHit, 3f, NavMesh.AllAreas))
            jumpEnd = jumpHit.position;

        float elapsed = 0f;

        while (elapsed < dismountJumpDuration)
        {
            float t = Mathf.Clamp01(elapsed / dismountJumpDuration);

            Vector3 pos = Vector3.Lerp(start, jumpEnd, t);
            pos.y = Mathf.Lerp(start.y, jumpEnd.y, t)
                    + dismountJumpHeight * Mathf.Sin(Mathf.PI * t);

            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = jumpEnd;

        if (agent)
        {
            agent.enabled = true;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
                agent.Warp(startHit.position);

            agent.isStopped = false;
            agent.speed = roamSpeed;
            agent.SetDestination(adoptionPoint.position);
        }

        if (col)
            col.enabled = true;

        if (animHandler)
            animHandler.SetAnimation(eCuteAnimalAnims.WALK);

        while (agent && agent.enabled)
        {
            if (!agent.pathPending && agent.remainingDistance <= destinationReachDistance)
                break;

            yield return null;
        }

        if (animHandler)
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);

        onReached?.Invoke(this);
    }

    private void SetRandomRoamDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * roamRadius;

        Vector3 target = spawnCenter + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
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

    private void ChangeState(OrphanState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case OrphanState.Roaming:
                if (agent)
                {
                    agent.enabled = true;
                    agent.speed = roamSpeed;
                    agent.isStopped = false;
                }

                roamTimer = 0f;
                break;

            case OrphanState.Carried:
                if (agent)
                {
                    if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                        agent.ResetPath();
                    agent.enabled = false;
                }

                if (animHandler)
                    animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
                break;

            case OrphanState.Dead:
                if (agent)
                {
                    agent.ResetPath();
                    agent.enabled = false;
                }

                if (animHandler)
                    animHandler.SetAnimation(eCuteAnimalAnims.DIE);
                break;
        }

        UpdateEmotionState();
        UpdateHealthBarState();
    }

    public void OnPickedUpByPlayer()
    {
        ChangeState(OrphanState.Carried);
    }

    public void OnAdoptedIntoBase()
    {
        // This object will usually be despawned by BaseAdoptionPoint.
    }

    private void Die()
    {
        ChangeState(OrphanState.Dead);

        PooledObject pooledObject = GetComponent<PooledObject>();

        if (pooledObject)
            pooledObject.Despawn(2f);
        else
            Destroy(gameObject, 2f);
    }

    public void ResetOrphanCub()
    {
        StopAllCoroutines();

        currentState = OrphanState.Roaming;

        spawnCenter = transform.position;
        roamTimer = 0f;

        if (baseCubManager == null)
            baseCubManager = FindFirstObjectByType<BaseCubManager>();

        if (health)
            health.ResetHealth();

        if (agent)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = false;
            }

            agent.speed = roamSpeed;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }



        Collider col = GetComponent<Collider>();
        if (col)
            col.enabled = true;

        transform.SetParent(null, true);

        ChangeState(OrphanState.Roaming);
    }
}