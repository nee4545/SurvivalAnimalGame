using UnityEngine;

public class StampedeHazardAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float lifeTime = 8f;

    [Header("Collision")]
    public string playerTag = "Player";
    public bool canBeJumpedOver = true;
    public float jumpAvoidHeight = 1.0f;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;

    [Header("Debug")]
    public bool debugLogs;

    private StampedeMiniGameController controller;
    private StampedeLaneController laneController;

    private Vector3 moveDirection;
    private float timer;
    private bool active;
    private bool alreadyHitPlayer;

    public void Init(
        StampedeMiniGameController miniGameController,
        StampedeLaneController playerLaneController,
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        Vector3 runDirection,
        float speed
    )
    {
        controller = miniGameController;
        laneController = playerLaneController;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        moveDirection = runDirection.normalized;
        moveSpeed = speed;

        timer = lifeTime;
        active = true;
        alreadyHitPlayer = false;

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active)
            return;

        animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!active)
            return;

        if (alreadyHitPlayer)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (PlayerAvoidedByJump())
            return;

        alreadyHitPlayer = true;

        Vector3 hitDirection = other.transform.position - transform.position;
        hitDirection.y = 0f;

        if (controller != null)
            controller.RegisterPlayerHit(hitDirection);

        //Despawn();
    }

    private bool PlayerAvoidedByJump()
    {
        if (!canBeJumpedOver)
            return false;

        if (laneController == null)
            return false;

        if (!laneController.IsJumping)
            return false;

        // Simple timing rule:
        // if player is currently jumping, predator can pass under.
        // Later we can make this stricter using jump height/window.
        return true;
    }

    private void Despawn()
    {
        active = false;

        // For now just disable. Later we can connect this to your pool system.
        gameObject.SetActive(false);
    }
}