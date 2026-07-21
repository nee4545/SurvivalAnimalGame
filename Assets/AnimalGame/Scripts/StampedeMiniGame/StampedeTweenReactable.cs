using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class StampedeTweenReactable : MonoBehaviour
{
    public static readonly List<StampedeTweenReactable> All = new();

    [Header("Movement")]
    public float pushDistance = 0.35f;
    public float hopHeight = 0.18f;
    public float pushDuration = 0.16f;
    public float returnDuration = 0.35f;

    [Header("Rotation")]
    public float rotationAngle = 18f;
    public bool rotateOnReact = true;

    [Header("Reaction")]
    public float cooldown = 0.6f;
    public bool ignoreWhileAnimating = true;

    [Header("Randomness")]
    public float randomSideAmount = 0.25f;
    public float randomStrengthMin = 0.85f;
    public float randomStrengthMax = 1.15f;

    private bool isAnimating;
    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private Sequence reactionSequence;

    private void Awake()
    {
        CacheStartTransform();
    }

    private void OnEnable()
    {
        CacheStartTransform();

        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        KillTween();
    }

    private void OnDestroy()
    {
        All.Remove(this);
        KillTween();
    }

    private void CacheStartTransform()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
    }

    public void React(Vector3 sourceWorldPosition, float strength = 1f)
    {
        if (ignoreWhileAnimating && isAnimating)
            return;

        isAnimating = true;

        KillTween();

        Vector3 worldDir = transform.position - sourceWorldPosition;
        worldDir.y = 0f;

        if (worldDir.sqrMagnitude < 0.001f)
            worldDir = Random.insideUnitSphere;

        worldDir.y = 0f;
        worldDir.Normalize();

        if (randomSideAmount > 0f)
        {
            Vector3 random = Random.insideUnitSphere;
            random.y = 0f;

            if (random.sqrMagnitude > 0.001f)
            {
                random.Normalize();
                worldDir = Vector3.Lerp(worldDir, random, randomSideAmount).normalized;
            }
        }

        Vector3 localDir = transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDir)
            : worldDir;

        localDir.y = 0f;

        if (localDir.sqrMagnitude < 0.001f)
            localDir = Vector3.forward;

        localDir.Normalize();

        float finalStrength =
            strength *
            Random.Range(randomStrengthMin, randomStrengthMax);

        Vector3 pushedLocalPos =
            startLocalPos +
            localDir * pushDistance * finalStrength;

        pushedLocalPos.y += hopHeight * finalStrength;

        Quaternion pushedLocalRot = startLocalRot;

        if (rotateOnReact)
        {
            pushedLocalRot =
                startLocalRot *
                Quaternion.Euler(
                    Random.Range(-rotationAngle, rotationAngle),
                    Random.Range(-rotationAngle, rotationAngle),
                    Random.Range(-rotationAngle, rotationAngle)
                );
        }

        reactionSequence = DOTween.Sequence();

        reactionSequence.Append(
            transform
                .DOLocalMove(pushedLocalPos, pushDuration)
                .SetEase(Ease.OutQuad)
        );

        reactionSequence.Join(
            transform
                .DOLocalRotateQuaternion(pushedLocalRot, pushDuration)
                .SetEase(Ease.OutQuad)
        );

        reactionSequence.Append(
            transform
                .DOLocalMove(startLocalPos, returnDuration)
                .SetEase(Ease.OutBack)
        );

        reactionSequence.Join(
            transform
                .DOLocalRotateQuaternion(startLocalRot, returnDuration)
                .SetEase(Ease.OutQuad)
        );

        reactionSequence.OnComplete(() =>
        {
            DOVirtual.DelayedCall(cooldown, () =>
            {
                isAnimating = false;
            });
        });
    }

    private void KillTween()
    {
        if (reactionSequence != null)
        {
            reactionSequence.Kill();
            reactionSequence = null;
        }

        transform.DOKill();
    }
}