using UnityEngine;
using DG.Tweening;

public class RockTweenReaction : FoliageReactableBase
{
    [Header("Movement")]
    public float pushDistance = 0.4f;
    public float hopHeight = 0.25f;
    public float pushDuration = 0.25f;
    public float returnDuration = 0.4f;

    [Header("Rotation")]
    public float rotationAngle = 25f;

    [Header("Cooldown")]
    public float cooldown = 1.2f;

    private bool isAnimating;
    private Vector3 startPos;
    private Quaternion startRot;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            React(other.transform.position);
        }
    }

    // 🔹 Called by PlayerFoliageScanner
    public override void React(Vector3 playerPos)
    {
        if (isAnimating) return;

        isAnimating = true;

        Vector3 dir = (transform.position - playerPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = Random.insideUnitSphere;

        dir.Normalize();

        Vector3 pushPos = startPos + dir * pushDistance;

        transform.DOKill();

        Sequence seq = DOTween.Sequence();

        // Push + hop
        seq.Append(transform.DOMoveX(pushPos.x, pushDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveZ(pushPos.z, pushDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveY(startPos.y + hopHeight, pushDuration * 0.5f)
            .SetEase(Ease.OutQuad));

        // Rotate slightly
        seq.Join(transform.DORotate(
            new Vector3(0, Random.Range(-rotationAngle, rotationAngle), 0),
            pushDuration,
            RotateMode.WorldAxisAdd
        ));

        // Settle back
        seq.Append(transform.DOMoveY(startPos.y, returnDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOMove(startPos, returnDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DORotateQuaternion(startRot, returnDuration));

        seq.OnComplete(() =>
        {
            Invoke(nameof(ResetState), cooldown);
        });
    }

    void ResetState()
    {
        isAnimating = false;
    }
}
