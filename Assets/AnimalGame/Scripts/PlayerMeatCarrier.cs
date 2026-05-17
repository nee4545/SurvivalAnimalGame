using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerMeatCarrier : MonoBehaviour
{
    [Header("Carry Settings")]
    public Transform meatPoint;
    public int maxCarryAmount = 3;

    [Header("Carry Conflict")]
    public PlayerCubCarrier cubCarrier;

    [Header("Stack Visual Settings")]
    public float verticalStackOffset = 0.25f;
    public float randomRotationAmount = 20f;

    [Header("Pickup Tween Settings")]
    public float pickupMoveDuration = 0.35f;
    public float pickupJumpPower = 0.75f;
    public Ease pickupEase = Ease.OutBack;
    public bool useScalePunch = true;
    public float punchScaleAmount = 0.15f;
    public float punchDuration = 0.25f;

    private readonly List<GameObject> carriedMeat = new();

    public int CurrentMeatCount => carriedMeat.Count;
    public bool IsFull => carriedMeat.Count >= maxCarryAmount;
    public bool HasMeat => carriedMeat.Count > 0;

    private void Awake()
    {
        if (cubCarrier == null)
            cubCarrier = GetComponent<PlayerCubCarrier>();
    }

    public bool TryCollectMeat(GameObject meatObject)
    {
        if (cubCarrier != null && cubCarrier.HasCub)
            return false;

        if (IsFull || meatObject == null || meatPoint == null)
            return false;

        carriedMeat.Add(meatObject);

        int index = carriedMeat.Count - 1;

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

        Collider col = meatObject.GetComponent<Collider>();
        if (col)
            col.enabled = false;

        Transform meatTransform = meatObject.transform;
        meatTransform.DOKill();

        // Important:
        // Parent first, but keep current world position.
        // This makes the meat follow the moving player during the tween.
        meatTransform.SetParent(meatPoint, true);

        Sequence pickupSequence = DOTween.Sequence();

        pickupSequence.Append(
            meatTransform.DOLocalJump(
                targetLocalPos,
                pickupJumpPower,
                1,
                pickupMoveDuration
            ).SetEase(pickupEase)
        );

        pickupSequence.Join(
            meatTransform.DOLocalRotateQuaternion(
                targetLocalRot,
                pickupMoveDuration
            )
        );

        if (useScalePunch)
        {
            pickupSequence.Join(
                meatTransform.DOPunchScale(
                    Vector3.one * punchScaleAmount,
                    punchDuration,
                    1,
                    0.5f
                )
            );
        }

        pickupSequence.OnComplete(() =>
        {
            if (meatObject == null)
                return;

            meatTransform.localPosition = targetLocalPos;
            meatTransform.localRotation = targetLocalRot;
        });

        return true;
    }

    public GameObject RemoveTopMeat()
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
}