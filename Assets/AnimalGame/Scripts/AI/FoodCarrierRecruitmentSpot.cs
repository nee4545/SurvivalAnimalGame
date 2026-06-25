using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FoodCarrierRecruitmentSpot : MonoBehaviour
{
    private enum RecruitmentState
    {
        WaitingForPlayer,
        Active,
        Success,
        Failed,
        Cooldown
    }

    [Header("Challenge Settings")]
    public int requiredMeat = 3;
    public float timeLimit = 90f;
    public float retryCooldown = 30f;

    [Header("Recruitment")]
    public GameObject recruitedCarrierPrefab;
    public Transform recruitedSpawnPoint;
    public Transform player;
    public HomeFoodPoint homeFoodPoint;

    [Header("Wild Carriers")]
    public List<WildFoodCarrierAI> wildCarriers = new();

    [Header("Meat Detection")]
    public bool startWhenPlayerEnters = true;
    public string playerTag = "Player";

    [Header("UI Optional")]
    public GameObject recruitmentPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI meatText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI messageText;

    [Header("Offering Point")]
    public FoodCarrierOfferingPoint offeringPoint;
    public bool consumeOfferingMeatOnSuccess = true;

    public bool CanAcceptMeat =>
        currentState == RecruitmentState.Active;

    [Header("Debug")]
    public bool debugLogs;

    private RecruitmentState currentState = RecruitmentState.WaitingForPlayer;

    private int currentMeat;
    private float timer;
    private float cooldownTimer;

    private void Start()
    {
        timer = timeLimit;

        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null)
                wildCarriers[i].AssignRecruitmentSpot(this);
        }

        SetPanel(false);
        RefreshUI();
    }

    private void Update()
    {
        switch (currentState)
        {
            case RecruitmentState.Active:
                UpdateActiveChallenge();
                break;

            case RecruitmentState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void UpdateActiveChallenge()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            FailRecruitment();
            return;
        }

        RefreshUI();
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            ResetSpot();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (player == null)
                player = other.transform;

            if (startWhenPlayerEnters)
                StartRecruitment();

            return;
        }

    }

    public void RegisterOfferingMeat(GameObject meatObject)
    {
        if (currentState != RecruitmentState.Active)
            return;

        currentMeat++;

        if (debugLogs)
            Debug.Log($"[FoodCarrierRecruitmentSpot] Offering meat: {currentMeat}/{requiredMeat}", this);

        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null)
                wildCarriers[i].OnMeatDonated(currentMeat, requiredMeat);
        }

        RefreshUI();

        if (currentMeat >= requiredMeat)
            CompleteRecruitment();
    }

    public void StartRecruitment()
    {
        if (currentState != RecruitmentState.WaitingForPlayer)
            return;

        currentState = RecruitmentState.Active;
        currentMeat = 0;
        timer = timeLimit;

        SetPanel(true);
        SetMessage("Earn their trust. Drop meat here.");

        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null)
                wildCarriers[i].OnRecruitmentStarted();
        }

        RefreshUI();

        if (debugLogs)
            Debug.Log("[FoodCarrierRecruitmentSpot] Recruitment started.", this);
    }

    private void CompleteRecruitment()
    {
        if (currentState != RecruitmentState.Active)
            return;

        currentState = RecruitmentState.Success;

        SetMessage("Food Carrier joined your family!");

        SetPanel(false);

        WildFoodCarrierAI selectedWildCarrier = GetFirstAvailableWildCarrier();

        if (selectedWildCarrier != null)
        {
            selectedWildCarrier.MoveToRecruitmentPointAndDespawn(
                recruitedSpawnPoint,
                SpawnRecruitedCarrier
            );
        }
        else
        {
            SpawnRecruitedCarrier();
        }

        if (consumeOfferingMeatOnSuccess && offeringPoint != null)
            offeringPoint.ClearOfferingMeat();

        if (debugLogs)
            Debug.Log("[FoodCarrierRecruitmentSpot] Recruitment success.", this);
    }

    private WildFoodCarrierAI GetFirstAvailableWildCarrier()
    {
        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null && wildCarriers[i].gameObject.activeInHierarchy)
                return wildCarriers[i];
        }

        return null;
    }

    private void FailRecruitment()
    {
        if (currentState != RecruitmentState.Active)
            return;

        currentState = RecruitmentState.Failed;

        SetMessage("The carrier lost trust. Come back later.");

        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null)
                wildCarriers[i].OnRecruitmentFailed();
        }

        if (offeringPoint != null)
            offeringPoint.ClearOfferingMeat();

        cooldownTimer = retryCooldown;
        currentState = RecruitmentState.Cooldown;

        RefreshUI();

        if (debugLogs)
            Debug.Log("[FoodCarrierRecruitmentSpot] Recruitment failed.", this);
    }

    private void ResetSpot()
    {
        currentState = RecruitmentState.WaitingForPlayer;
        currentMeat = 0;
        timer = timeLimit;

        if (offeringPoint != null)
            offeringPoint.ClearOfferingMeat();

        for (int i = 0; i < wildCarriers.Count; i++)
        {
            if (wildCarriers[i] != null)
                wildCarriers[i].ResetWildCarrier();
        }

        SetPanel(false);
        RefreshUI();

        if (debugLogs)
            Debug.Log("[FoodCarrierRecruitmentSpot] Spot reset.", this);
    }

    private void SpawnRecruitedCarrier()
    {
        if (recruitedCarrierPrefab == null)
        {
            Debug.LogWarning("[FoodCarrierRecruitmentSpot] Missing recruitedCarrierPrefab.", this);
            return;
        }

        Vector3 spawnPos = recruitedSpawnPoint != null
            ? recruitedSpawnPoint.position
            : transform.position + transform.forward * 2f;

        Quaternion spawnRot = recruitedSpawnPoint != null
            ? recruitedSpawnPoint.rotation
            : Quaternion.identity;

        GameObject carrierObject = PoolManager.Spawn(
            recruitedCarrierPrefab,
            spawnPos,
            spawnRot
        );

        FoodCarrierAI carrierAI = carrierObject.GetComponent<FoodCarrierAI>();

        if (carrierAI != null)
        {
            carrierAI.player = player;
            carrierAI.foodPoint = homeFoodPoint;
        }
        else
        {
            Debug.LogWarning("[FoodCarrierRecruitmentSpot] Spawned prefab has no FoodCarrierAI.", carrierObject);
        }
    }

    private void RefreshUI()
    {
        if (titleText != null)
            titleText.text = "Earn Their Trust";

        if (meatText != null)
            meatText.text = $"Meat: {currentMeat}/{requiredMeat}";

        if (timerText != null)
            timerText.text = $"Time: {Mathf.CeilToInt(Mathf.Max(0f, timer))}s";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    private void SetPanel(bool active)
    {
        if (recruitmentPanel != null)
            recruitmentPanel.SetActive(active);
    }

    public float GetProgress01()
    {
        if (requiredMeat <= 0)
            return 1f;

        return Mathf.Clamp01((float)currentMeat / requiredMeat);
    }
}