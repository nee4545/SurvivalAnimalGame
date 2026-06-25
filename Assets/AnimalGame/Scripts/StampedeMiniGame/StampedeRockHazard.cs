using DG.Tweening;
using UnityEngine;

public class StampedeRockHazard : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDirection;
    public float moveSpeed = 15f;

    [Header("Collision")]
    public bool removeLifeWhenPlayerHits = true;
    public bool shatterOnAIHit = true;

    [Header("AI Shatter Distance Gate")]
    public bool onlyAllowAIShatterNearPlayer = true;

    [Tooltip("AI can shatter this rock only when the rock is this close to the player.")]
    public float aiShatterAllowedDistanceFromPlayer = 10f;

    [Header("Tween Shatter")]
    public GameObject shatterVfxPrefab;
    public GameObject[] smallRockPiecePrefabs;

    public int smallPieceCount = 6;
    public float pieceSpawnRadius = 0.35f;

    public float pieceFlyDistanceMin = 1.2f;
    public float pieceFlyDistanceMax = 2.6f;

    public float pieceJumpPowerMin = 0.6f;
    public float pieceJumpPowerMax = 1.3f;

    public float pieceFlyDuration = 0.45f;
    public float pieceStayDuration = 0.6f;
    public float pieceShrinkDuration = 0.3f;

    public float pieceRotateAmount = 360f;

    public Ease pieceFlyEase = Ease.OutQuad;
    public Ease pieceShrinkEase = Ease.InBack;

    public bool disablePieceColliders = true;

    [Header("Lifetime")]
    public float despawnDistanceFromPlayer = 50f;

    private StampedeMiniGameController miniGameController;
    private Transform player;
    private bool hasShattered;

    public void Init(
        StampedeMiniGameController controller,
        Transform playerTransform,
        Vector3 direction,
        float speed
    )
    {
        miniGameController = controller;
        player = playerTransform;

        moveDirection = direction;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = Vector3.back;

        moveDirection.Normalize();

        moveSpeed = Mathf.Abs(speed);
        hasShattered = false;
    }

    private void Update()
    {
        if (hasShattered)
            return;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance >= despawnDistanceFromPlayer)
                Destroy(gameObject);
        }
    }

    private bool CanAIShatterRock()
    {
        if (!onlyAllowAIShatterNearPlayer)
            return true;

        if (player == null)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        return distanceToPlayer <= aiShatterAllowedDistanceFromPlayer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasShattered)
            return;

        CCActor playerActor = other.GetComponentInParent<CCActor>();

        if (playerActor != null)
        {
            if (removeLifeWhenPlayerHits && miniGameController != null)
            {
                Vector3 hitDirection = playerActor.transform.position - transform.position;
                hitDirection.y = 0f;

                if (hitDirection.sqrMagnitude < 0.001f)
                    hitDirection = -moveDirection;

                miniGameController.RegisterRockHazardHit(hitDirection.normalized);
            }

            Shatter();
            return;
        }

        if (shatterOnAIHit)
        {
            StampedeHazardAI animalAI = other.GetComponent<StampedeHazardAI>();

            if (animalAI != null)
            {
                if(CanAIShatterRock())
                    Shatter();
                return;
            }
        }
    }

    private void Shatter()
    {
        if (hasShattered)
            return;

        hasShattered = true;

        if (shatterVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                shatterVfxPrefab,
                transform.position,
                transform.rotation
            );

            Destroy(vfx, pieceFlyDuration + pieceStayDuration + pieceShrinkDuration);
        }

        SpawnSmallRockPieces();

        Destroy(gameObject);
    }

    private void SpawnSmallRockPieces()
    {
        if (smallRockPiecePrefabs == null || smallRockPiecePrefabs.Length == 0)
            return;

        float totalLifetime =
            pieceFlyDuration +
            pieceStayDuration +
            pieceShrinkDuration +
            0.15f;

        GameObject anchor = new GameObject("StampedeRockShatterAnchor");
        anchor.transform.position = transform.position;
        anchor.transform.rotation = Quaternion.identity;

        Vector3 anchorEndPosition =
            anchor.transform.position +
            moveDirection.normalized * moveSpeed * totalLifetime;

        anchor.transform
            .DOMove(anchorEndPosition, totalLifetime)
            .SetEase(Ease.Linear)
            .SetLink(anchor)
            .OnComplete(() =>
            {
                if (anchor != null)
                    Destroy(anchor);
            });

        for (int i = 0; i < smallPieceCount; i++)
        {
            GameObject prefab = smallRockPiecePrefabs[
                Random.Range(0, smallRockPiecePrefabs.Length)
            ];

            if (prefab == null)
                continue;

            Vector2 randomCircle = Random.insideUnitCircle;

            if (randomCircle.sqrMagnitude < 0.001f)
                randomCircle = Vector2.right;

            randomCircle.Normalize();

            Vector3 burstDirection = new Vector3(
                randomCircle.x,
                0f,
                randomCircle.y
            );

            Vector3 startLocalPosition =
                burstDirection * Random.Range(0f, pieceSpawnRadius);

            startLocalPosition.y = Random.Range(0.1f, 0.35f);

            GameObject piece = Instantiate(prefab, anchor.transform);

            Transform pieceTransform = piece.transform;
            pieceTransform.localPosition = startLocalPosition;
            pieceTransform.localRotation = Random.rotation;

            if (disablePieceColliders)
            {
                Collider[] colliders = piece.GetComponentsInChildren<Collider>();

                for (int c = 0; c < colliders.Length; c++)
                    colliders[c].enabled = false;
            }

            pieceTransform.DOKill();

            Vector3 originalScale = pieceTransform.localScale;

            Vector3 endLocalPosition =
                startLocalPosition +
                burstDirection * Random.Range(pieceFlyDistanceMin, pieceFlyDistanceMax);

            endLocalPosition.y =
                startLocalPosition.y + Random.Range(-0.15f, 0.25f);

            Vector3 randomRotation = new Vector3(
                Random.Range(-pieceRotateAmount, pieceRotateAmount),
                Random.Range(-pieceRotateAmount, pieceRotateAmount),
                Random.Range(-pieceRotateAmount, pieceRotateAmount)
            );

            float jumpPower = Random.Range(
                pieceJumpPowerMin,
                pieceJumpPowerMax
            );

            Sequence seq = DOTween.Sequence();

            seq.Append(
                pieceTransform.DOLocalJump(
                    endLocalPosition,
                    jumpPower,
                    1,
                    pieceFlyDuration
                ).SetEase(pieceFlyEase)
            );

            seq.Join(
                pieceTransform.DOLocalRotate(
                    pieceTransform.localEulerAngles + randomRotation,
                    pieceFlyDuration,
                    RotateMode.FastBeyond360
                )
            );

            seq.AppendInterval(pieceStayDuration);

            seq.Append(
                pieceTransform.DOScale(
                    Vector3.zero,
                    pieceShrinkDuration
                ).SetEase(pieceShrinkEase)
            );

            seq.SetLink(anchor);

            seq.OnComplete(() =>
            {
                if (!pieceTransform)
                    return;

                pieceTransform.localScale = originalScale;
            });
        }
    }
}