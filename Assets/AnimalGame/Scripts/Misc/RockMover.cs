using UnityEngine;
using DG.Tweening;

public class RockMover : FoliageReactableBase
{
    [Header("Movement")]
    public float pushDistance = 0.35f;
    public float hopHeight = 0.2f;
    public float moveDuration = 0.25f;

    [Header("Rotation")]
    public float rotationAngle = 20f;

    [Header("Grounding")]
    public LayerMask groundLayer = ~0;
    public float groundRayHeight = 1.5f;
    public float groundRayDistance = 3f;
    public float groundOffset = 0.01f;

    [Header("Limits")]
    [Tooltip("-1 = unlimited moves")]
    public int maxMoves = 1;
    public float cooldown = 0.5f;

    private int moveCount;
    private bool isAnimating;

    // 🔹 Called by PlayerFoliageScanner
    public override void React(Vector3 playerPos)
    {
        if (isAnimating) return;
        if (!IsMoveAllowed()) return;

        Move(playerPos);
    }

    bool IsMoveAllowed()
    {
        if (maxMoves < 0)
            return true;

        return moveCount < maxMoves;
    }

    void Move(Vector3 playerPos)
    {
        isAnimating = true;
        moveCount++;

        transform.DOKill();

        Vector3 startPos = transform.position;

        Vector3 dir = (startPos - playerPos);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = Random.insideUnitSphere;

        dir.Normalize();

        Vector3 targetPos = startPos + dir * pushDistance;

        // Ground snap (same as your original)
        Vector3 rayOrigin = targetPos + Vector3.up * groundRayHeight;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer))
        {
            isAnimating = false;
            return;
        }

        targetPos.y = hit.point.y + groundOffset;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOMoveX(targetPos.x, moveDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveZ(targetPos.z, moveDuration).SetEase(Ease.OutQuad));

        if (hopHeight > 0f)
        {
            seq.Join(transform.DOMoveY(startPos.y + hopHeight, moveDuration * 0.5f)
                .SetEase(Ease.OutQuad));
        }

        seq.Join(transform.DORotate(
            new Vector3(0, Random.Range(-rotationAngle, rotationAngle), 0),
            moveDuration,
            RotateMode.WorldAxisAdd
        ));

        seq.Append(transform.DOMoveY(targetPos.y, moveDuration * 0.5f)
            .SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            Invoke(nameof(ResetCooldown), cooldown);
        });
    }

    void ResetCooldown()
    {
        isAnimating = false;
    }
}
