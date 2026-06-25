using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HomeFoodPoint : MonoBehaviour
{
    [Header("Food Storage")]
    public Transform foodStackPoint;
    public int maxFoodCapacity = 20;

    [Header("Ground Placement")]
    public float dropRadius = 2f;
    public int groundSlots = 10;
    public LayerMask groundLayer;
    public float terrainRayHeight = 5f;
    public float terrainRayDistance = 15f;

    [Header("Stack Visual Settings")]
    public float verticalStackOffset = 0.22f;
    public float randomRotationAmount = 25f;

    [Header("Drop Tween Settings")]
    public float depositDelay = 0.12f;
    public float dropMoveDuration = 0.35f;
    public float dropJumpPower = 0.6f;
    public Ease dropEase = Ease.OutQuad;

    private readonly List<GameObject> storedMeat = new();
    private readonly List<Vector3> groundLocalSlots = new();

    public int CurrentFoodCount => storedMeat.Count;
    public bool HasFood => storedMeat.Count > 0;

    private Coroutine depositRoutine;

    private void Awake()
    {
        GenerateGroundSlots();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMeatCarrier carrier = other.GetComponentInParent<PlayerMeatCarrier>();

        if (carrier == null)
            return;

        if (depositRoutine == null)
            depositRoutine = StartCoroutine(DepositMeatRoutine(carrier));
    }

    private IEnumerator DepositMeatRoutine(PlayerMeatCarrier carrier)
    {
        while (carrier.HasMeat && storedMeat.Count < maxFoodCapacity)
        {
            GameObject meat = carrier.RemoveTopMeat();

            if (meat == null)
                yield break;

            StoreMeat(meat);

            yield return new WaitForSeconds(depositDelay);
        }

        depositRoutine = null;
    }

    private void StoreMeat(GameObject meat)
    {
        storedMeat.Add(meat);

        int index = storedMeat.Count - 1;
        int slotIndex = index % groundSlots;
        int stackLayer = index / groundSlots;

        Vector3 targetWorldPos = GetSlotWorldPosition(slotIndex);
        targetWorldPos.y += stackLayer * verticalStackOffset;

        Quaternion targetWorldRot = Quaternion.Euler(
            Random.Range(-randomRotationAmount, randomRotationAmount),
            Random.Range(0f, 360f),
            Random.Range(-randomRotationAmount, randomRotationAmount)
        );

        Collider col = meat.GetComponent<Collider>();
        if (col)
            col.enabled = false;

        Transform meatTransform = meat.transform;
        meatTransform.DOKill();

        meatTransform.SetParent(null, true);

        Sequence dropSequence = DOTween.Sequence();

        dropSequence.Append(
            meatTransform.DOJump(
                targetWorldPos,
                dropJumpPower,
                1,
                dropMoveDuration
            ).SetEase(dropEase)
        );

        dropSequence.Join(
            meatTransform.DORotateQuaternion(
                targetWorldRot,
                dropMoveDuration
            )
        );

        dropSequence.OnComplete(() =>
        {
            if (meatTransform == null)
                return;

            meatTransform.position = targetWorldPos;
            meatTransform.rotation = targetWorldRot;
        });
    }

    private void GenerateGroundSlots()
    {
        groundLocalSlots.Clear();

        if (groundSlots <= 0)
            groundSlots = 1;

        for (int i = 0; i < groundSlots; i++)
        {
            float angle = i * Mathf.PI * 2f / groundSlots;

            float radius = dropRadius * Mathf.Sqrt((i + 0.5f) / groundSlots);

            Vector3 localOffset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            groundLocalSlots.Add(localOffset);
        }
    }

    public bool TryStoreExternalMeat(GameObject meat)
    {
        if (meat == null)
            return false;

        if (storedMeat.Count >= maxFoodCapacity)
            return false;

        StoreMeat(meat);
        return true;
    }

    private Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (foodStackPoint == null)
            return transform.position;

        if (groundLocalSlots.Count == 0)
            GenerateGroundSlots();

        slotIndex = Mathf.Clamp(slotIndex, 0, groundLocalSlots.Count - 1);

        Vector3 basePos = foodStackPoint.position + groundLocalSlots[slotIndex];

        Vector3 rayStart = basePos + Vector3.up * terrainRayHeight;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                terrainRayDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return basePos;
    }

    public bool TryConsumeFood()
    {
        if (storedMeat.Count == 0)
            return false;

        int lastIndex = storedMeat.Count - 1;
        GameObject meat = storedMeat[lastIndex];

        storedMeat.RemoveAt(lastIndex);

        if (meat)
        {
            meat.transform.DOKill();

            PooledObject pooledObject = meat.GetComponent<PooledObject>();

            if (pooledObject)
                pooledObject.Despawn();
            else
                Destroy(meat);
        }

        return true;
    }
}