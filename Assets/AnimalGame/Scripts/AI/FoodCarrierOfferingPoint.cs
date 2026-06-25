using DG.Tweening;
using UnityEngine;

public class FoodCarrierOfferingPoint : MonoBehaviour
{
    [Header("References")]
    public FoodCarrierRecruitmentSpot recruitmentSpot;
    public Transform meatStackPoint;

    [Header("Stack Visuals")]
    public float verticalStackOffset = 0.22f;
    public float randomSpreadRadius = 0.35f;
    public float moveDuration = 0.25f;
    public float jumpPower = 0.45f;

    [Header("Player Deposit")]
    public bool autoDepositFromPlayer = true;
    public float depositInterval = 0.2f;

    [Header("State")]
    public int currentMeatCount;

    private float nextDepositTime;



    private void Awake()
    {
        if (recruitmentSpot == null)
            recruitmentSpot = GetComponentInParent<FoodCarrierRecruitmentSpot>();

        if (meatStackPoint == null)
            meatStackPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAcceptFromPlayer(other);
        TryAcceptLooseMeat(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAcceptFromPlayer(other);
        TryAcceptLooseMeat(other);
    }

    private void TryAcceptFromPlayer(Collider other)
    {
        if (!autoDepositFromPlayer)
            return;

        if (recruitmentSpot == null || !recruitmentSpot.CanAcceptMeat)
            return;

        if (Time.time < nextDepositTime)
            return;

        PlayerMeatCarrier playerCarrier = other.GetComponentInParent<PlayerMeatCarrier>();

        if (playerCarrier == null)
            return;

        if (!playerCarrier.HasMeat)
            return;

        GameObject meatObject = playerCarrier.RemoveTopMeat();

        if (meatObject == null)
            return;

        nextDepositTime = Time.time + depositInterval;

        StoreOfferingMeat(meatObject);
    }

    private void TryAcceptLooseMeat(Collider other)
    {
        if (recruitmentSpot == null || !recruitmentSpot.CanAcceptMeat)
            return;

        MeatPickup meatPickup = other.GetComponent<MeatPickup>();

        if (meatPickup == null)
            meatPickup = other.GetComponentInParent<MeatPickup>();

        if (meatPickup == null)
            meatPickup = other.GetComponentInChildren<MeatPickup>();

        if (meatPickup == null)
            return;

        GameObject meatObject = meatPickup.gameObject;

        // Loose meat only. Carried meat is handled by TryAcceptFromPlayer.
        if (meatObject.transform.parent != null)
            return;

        if (!meatPickup.canBePickedUp)
            return;

        StoreOfferingMeat(meatObject);
    }

    private void StoreOfferingMeat(GameObject meatObject)
    {
        if (meatObject == null)
            return;

        currentMeatCount++;

        MeatPickup pickup = meatObject.GetComponent<MeatPickup>();
        if (pickup)
            pickup.canBePickedUp = false;

        Collider col = meatObject.GetComponent<Collider>();
        if (col)
            col.enabled = false;

        FoodCarrierDirector.Instance?.UnregisterMeat(meatObject);

        meatObject.transform.DOKill();
        meatObject.transform.SetParent(meatStackPoint, true);

        Vector2 randomCircle = Random.insideUnitCircle * randomSpreadRadius;

        Vector3 targetLocalPosition = new Vector3(
            randomCircle.x,
            (currentMeatCount - 1) * verticalStackOffset,
            randomCircle.y
        );

        Quaternion targetLocalRotation = Quaternion.Euler(
            Random.Range(-15f, 15f),
            Random.Range(0f, 360f),
            Random.Range(-15f, 15f)
        );

        meatObject.transform.DOLocalJump(
            targetLocalPosition,
            jumpPower,
            1,
            moveDuration
        );

        meatObject.transform.DOLocalRotateQuaternion(
            targetLocalRotation,
            moveDuration
        );

        recruitmentSpot.RegisterOfferingMeat(meatObject);
    }

    public void ClearOfferingMeat()
    {
        if (meatStackPoint == null)
            return;

        for (int i = meatStackPoint.childCount - 1; i >= 0; i--)
        {
            Transform child = meatStackPoint.GetChild(i);

            if (child == null)
                continue;

            child.DOKill();
            child.SetParent(null, true);

            PooledObject pooledObject = child.GetComponent<PooledObject>();

            if (pooledObject)
                pooledObject.Despawn();
            else
                Destroy(child.gameObject);
        }

        currentMeatCount = 0;
    }
}