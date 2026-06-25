using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerCubCarrier : MonoBehaviour
{
    [Header("Carry Settings")]
    public Transform cubCarryPoint;
    public int maxCarryAmount = 1;

    [Header("Visual Settings")]
    public float verticalStackOffset = 0.4f;

    [Header("Carry Conflict")]
    public PlayerMeatCarrier meatCarrier;

    [Header("Player Thought UI")]
    public PlayerThoughtUI thoughtUI;

    [Header("Pickup Tween")]
    public float pickupDuration = 0.45f;
    public float pickupJumpPower = 1.2f;
    public Ease pickupEase = Ease.OutBack;

    private readonly List<OrphanCubAI> carriedCubs = new();

    public int CurrentCarryCount => carriedCubs.Count;
    public bool IsFull => carriedCubs.Count >= maxCarryAmount;
    public bool HasCub => carriedCubs.Count > 0;

    private void Awake()
    {
        if (meatCarrier == null)
            meatCarrier = GetComponent<PlayerMeatCarrier>();

        if (thoughtUI == null)
            thoughtUI = GetComponentInChildren<PlayerThoughtUI>();
    }

    public bool TryCarryCub(OrphanCubAI cub)
    {
        if (meatCarrier != null && meatCarrier.HasMeat)
        {
            thoughtUI?.ShowCantCarryCub();
            return false;
        }

        if (IsFull || cub == null || cubCarryPoint == null)
            return false;

        carriedCubs.Add(cub);
        cub.OnPickedUpByPlayer();

        int index = carriedCubs.Count - 1;

        Transform cubTransform = cub.transform;
        cubTransform.DOKill();

        Collider col = cub.GetComponent<Collider>();
        if (col)
            col.enabled = false;

        Vector3 targetLocalPos = new Vector3(
            0f,
            index * verticalStackOffset,
            0f
        );

        Quaternion targetLocalRot = Quaternion.identity;

        cubTransform.SetParent(cubCarryPoint, true);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            cubTransform.DOLocalJump(
                targetLocalPos,
                pickupJumpPower,
                1,
                pickupDuration
            ).SetEase(pickupEase)
        );

        sequence.Join(
            cubTransform.DOLocalRotateQuaternion(
                targetLocalRot,
                pickupDuration
            )
        );

        sequence.OnComplete(() =>
        {
            if (!cubTransform)
                return;

            cubTransform.localPosition = targetLocalPos;
            cubTransform.localRotation = targetLocalRot;
        });

        return true;
    }

    public OrphanCubAI RemoveSpecificCub(OrphanCubAI cub)
    {
        if (cub == null)
            return null;

        if (!carriedCubs.Remove(cub))
            return null;

        cub.transform.DOKill();
        cub.transform.SetParent(null, true);

        return cub;
    }

    public OrphanCubAI RemoveTopCub()
    {
        if (carriedCubs.Count == 0)
            return null;

        int lastIndex = carriedCubs.Count - 1;
        OrphanCubAI cub = carriedCubs[lastIndex];

        carriedCubs.RemoveAt(lastIndex);

        if (cub)
        {
            cub.transform.DOKill();
            cub.transform.SetParent(null, true);
        }

        return cub;
    }
}