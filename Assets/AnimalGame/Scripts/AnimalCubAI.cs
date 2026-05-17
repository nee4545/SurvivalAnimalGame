using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class AnimalCubAI : MonoBehaviour
{
    private enum CubState
    {
        RoamingBase,
        GoingToFood,
        Eating,
        HuddlingNearPlayer,
        Dead
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform baseCenter;
    public Transform player;
    public HomeFoodPoint foodPoint;

    [Header("Base Roaming")]
    public float baseRoamRadius = 8f;
    public float minRoamWaitTime = 1.5f;
    public float maxRoamWaitTime = 4f;
    public float destinationReachDistance = 0.75f;

    [Header("Hunger / Health")]
    public Health health;
    public float healthDrainPerSecond = 2f;
    public float hungryHealthThreshold = 45f;
    public float foodHealAmount = 75f;

    [Header("Starvation")]
    public float starvationDamagePerSecond = 5f;

    [Header("Eating")]
    public float eatDistance = 1.5f;
    public float eatDuration = 1.2f;

    [Header("Player Huddle")]
    public float playerBaseRadius = 10f;
    public float huddleDistanceBehindPlayer = 2f;
    public float huddleRoamRadius = 1.5f;
    public float huddleRefreshTime = 1f;

    [Header("Emoji Visuals")]
    public GameObject happyEmoji;
    public GameObject loveEmoji;
    public GameObject sadEmoji;
    public GameObject disappointedEmoji;

    [Header("Movement")]
    public float roamSpeed = 2f;
    public float hungrySpeed = 3f;
    public float huddleSpeed = 3.2f;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;
    public float walkAnimSpeedThreshold = 0.1f;

    [Header("Emotion Text")]
    public TextMeshProUGUI emotionText;

    [Header("Base Manager")]
    public BaseCubManager baseCubManager;
    private bool isRegisteredToBase;

    private CubState currentState;

    private float roamTimer;
    private float eatTimer;
    private float huddleTimer;
    private PlayerMeatCarrier playerMeatCarrier;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        if (health == null)
            health = GetComponent<Health>();

    }

    private void Start()
    {
        if (baseCubManager == null)
            baseCubManager = FindFirstObjectByType<BaseCubManager>();

        if (baseCubManager != null && !isRegisteredToBase)
        {
            isRegisteredToBase = baseCubManager.RegisterCub(this);
        }
        else if (baseCubManager != null)
        {
            AssignBaseManager(baseCubManager);
        }

        if (player != null)
            playerMeatCarrier = player.GetComponent<PlayerMeatCarrier>();

        if (health != null)
            health.onDeath.AddListener(Die);

        ChangeState(CubState.RoamingBase);
    }

    public void AssignBaseManager(BaseCubManager manager)
    {
        baseCubManager = manager;

        if (baseCubManager == null)
            return;

        if (baseCenter == null)
            baseCenter = baseCubManager.baseCenter;

        if (player == null)
            player = baseCubManager.player;

        if (foodPoint == null)
            foodPoint = baseCubManager.foodPoint;

        if (player != null)
            playerMeatCarrier = player.GetComponent<PlayerMeatCarrier>();
    }


    private void UpdateAnimation()
    {
        if (animHandler == null)
            return;

        if (currentState == CubState.Dead)
            return;

        if (currentState == CubState.Eating)
        {
            animHandler.SetAnimation(eCuteAnimalAnims.EAT);
            return;
        }

        float speed = 0f;

        if (agent != null && agent.enabled)
            speed = agent.velocity.magnitude;

        if (speed > walkAnimSpeedThreshold)
            animHandler.SetAnimation(eCuteAnimalAnims.WALK);
        else
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }


    private void UpdateEmojiState()
    {
        bool isDead = health != null && health.IsDead;
        bool isStarving = health != null && health.CurrentHealth <= 0f;
        bool isHungry = health != null && health.CurrentHealth <= hungryHealthThreshold;

        bool playerHasMeat =
            playerMeatCarrier != null &&
            playerMeatCarrier.HasMeat;

        bool foodPointHasMeat =
            foodPoint != null &&
            foodPoint.HasFood;

        bool shouldShowSad = isStarving || isDead;

        bool shouldShowLove =
            !shouldShowSad &&
            playerHasMeat;

        bool shouldShowDisappointed =
            !shouldShowSad &&
            !shouldShowLove &&
            isHungry &&
            !foodPointHasMeat;

        bool shouldShowHappy =
            !shouldShowSad &&
            !shouldShowLove &&
            !shouldShowDisappointed;

        if (happyEmoji)
            happyEmoji.SetActive(shouldShowHappy);

        if (loveEmoji)
            loveEmoji.SetActive(shouldShowLove);

        if (sadEmoji)
            sadEmoji.SetActive(shouldShowSad);

        if (disappointedEmoji)
            disappointedEmoji.SetActive(shouldShowDisappointed);

        // Emotion text
        if (emotionText)
        {
            bool isHuddling =
                currentState == CubState.HuddlingNearPlayer;

            bool isEating =
                currentState == CubState.Eating;

            if (isEating)
            {
                emotionText.text = "Yum Yum!";
            }
            else if (shouldShowSad)
            {
                emotionText.text = "I'm Hungry !";
            }
            else if (shouldShowLove)
            {
                emotionText.text = isHuddling
                    ? "Feed me Soon!"
                    : "Food On The Way";
            }
            else if (shouldShowDisappointed)
            {
                emotionText.text = "No Food Left...";
            }
            else if (shouldShowHappy)
            {
                emotionText.text = foodPointHasMeat
                    ? "Food Available"
                    : "Im full but no food in base!";
            }
        }
    }

    public void ResetCubAI()
    {
        currentState = CubState.RoamingBase;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<Health>();

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        if (player != null)
            playerMeatCarrier = player.GetComponent<PlayerMeatCarrier>();

        if (agent)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        ChangeState(CubState.RoamingBase);
        UpdateEmojiState();
    }

    private void Update()
    {
        if (currentState == CubState.Dead)
            return;

        UpdateHunger();

        switch (currentState)
        {
            case CubState.RoamingBase:
                UpdateRoamingBase();
                break;

            case CubState.GoingToFood:
                UpdateGoingToFood();
                break;

            case CubState.Eating:
                UpdateEating();
                break;

            case CubState.HuddlingNearPlayer:
                UpdateHuddlingNearPlayer();
                break;
        }

        UpdateEmojiState();
        UpdateAnimation();
    }

    private void UpdateHunger()
    {
        if (health == null || health.IsDead)
            return;

        health.TakeDamage(healthDrainPerSecond * Time.deltaTime,null,false);

        if (health.IsDead)
        {
            Die();
            return;
        }

        if (health.CurrentHealth <= hungryHealthThreshold)
        {
            if (foodPoint != null && foodPoint.HasFood)
            {
                if (currentState != CubState.GoingToFood && currentState != CubState.Eating)
                    ChangeState(CubState.GoingToFood);
            }
            else if (IsPlayerNearBase())
            {
                if (currentState != CubState.HuddlingNearPlayer)
                    ChangeState(CubState.HuddlingNearPlayer);
            }
        }
    }

    private void UpdateRoamingBase()
    {
        roamTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= destinationReachDistance)
        {
            if (roamTimer <= 0f)
            {
                SetRandomBaseDestination();
                roamTimer = Random.Range(minRoamWaitTime, maxRoamWaitTime);
            }
        }
    }

    private void UpdateGoingToFood()
    {
        if (foodPoint == null)
        {
            ChangeState(CubState.RoamingBase);
            return;
        }

        if (!foodPoint.HasFood)
        {
            if (IsPlayerNearBase())
                ChangeState(CubState.HuddlingNearPlayer);
            else
                ChangeState(CubState.RoamingBase);

            return;
        }

        agent.SetDestination(foodPoint.transform.position);

        float distanceToFood = Vector3.Distance(transform.position, foodPoint.transform.position);

        if (distanceToFood <= eatDistance)
        {
            ChangeState(CubState.Eating);
        }
    }

    private void UpdateEating()
    {
        eatTimer -= Time.deltaTime;

        if (eatTimer > 0f)
            return;

        if (foodPoint != null && foodPoint.TryConsumeFood())
        {
            if (health != null)
                health.Heal(foodHealAmount);
        }

        ChangeState(CubState.RoamingBase);
    }

    private void UpdateHuddlingNearPlayer()
    {
        if (foodPoint != null && foodPoint.HasFood)
        {
            ChangeState(CubState.GoingToFood);
            return;
        }

        if (!IsPlayerNearBase())
        {
            ChangeState(CubState.RoamingBase);
            return;
        }

        huddleTimer -= Time.deltaTime;

        if (huddleTimer <= 0f)
        {
            SetHuddleDestination();
            huddleTimer = huddleRefreshTime;
        }
    }

    private void ChangeState(CubState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case CubState.RoamingBase:
                agent.speed = roamSpeed;
                roamTimer = 0f;
                SetRandomBaseDestination();
                break;

            case CubState.GoingToFood:
                agent.speed = hungrySpeed;
                break;

            case CubState.Eating:
                agent.ResetPath();
                eatTimer = eatDuration;
                break;

            case CubState.HuddlingNearPlayer:
                agent.speed = huddleSpeed;
                huddleTimer = 0f;
                break;

            case CubState.Dead:
                if(agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.enabled = false;
                }
                break;
        }
    }

    private void SetRandomBaseDestination()
    {
        if (baseCenter == null || agent == null || !agent.enabled)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * baseRoamRadius;

        Vector3 targetPosition = baseCenter.position + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void SetHuddleDestination()
    {
        if (player == null || agent == null || !agent.enabled)
            return;

        Vector3 behindPlayer = player.position - player.forward * huddleDistanceBehindPlayer;

        Vector2 randomCircle = Random.insideUnitCircle * huddleRoamRadius;

        Vector3 targetPosition = behindPlayer + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private bool IsPlayerNearBase()
    {
        if (player == null || baseCenter == null)
            return false;

        float distance = Vector3.Distance(player.position, baseCenter.position);
        return distance <= playerBaseRadius;
    }

    private void Die()
    {
        if (currentState == CubState.Dead)
            return;

        if (baseCubManager != null)
            baseCubManager.UnregisterCub(this);

        isRegisteredToBase = false;

        ChangeState(CubState.Dead);

        if (animHandler != null)
            animHandler.SetAnimation(eCuteAnimalAnims.DIE);

        PooledObject pooledObject = GetComponent<PooledObject>();

        if (pooledObject)
            pooledObject.Despawn(2f);
        else
            Destroy(gameObject, 2f);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.onDeath.RemoveListener(Die);

        if (baseCubManager != null)
            baseCubManager.UnregisterCub(this);
    }
}