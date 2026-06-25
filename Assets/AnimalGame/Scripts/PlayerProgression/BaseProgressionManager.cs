using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseProgressionManager : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private CCActor player;

    [Header("Base References")]
    [SerializeField] private HomeFoodPoint homeFoodPoint;
    [SerializeField] private BaseCubManager baseCubManager;

    [Header("Food Carrier References")]
    [SerializeField] private List<FoodCarrierAI> activeFoodCarriers = new();

    [Header("Base Stats")]
    [SerializeField] private List<BaseStat> baseStats = new();

    public event Action OnBaseStatsChanged;

    public int MaxFoodAtBase { get; private set; }
    public int MaxCubs { get; private set; }
    public int MaxHunters { get; private set; }
    public int MaxFoodCarriers { get; private set; }
    public int MaxFoodPerCarrier { get; private set; }
    public float CubHungerDrainRate { get; private set; }

    private void Awake()
    {
        InitializeBaseStats();
        ApplyBaseStats();
    }

    private void InitializeBaseStats()
    {
        if (baseStats != null && baseStats.Count > 0)
            return;

        baseStats = new List<BaseStat>
        {
            new BaseStat
            {
                type = BaseStatType.MaxFoodAtBase,
                baseValue = 10,
                perLevelIncrease = 5,
                maxLevel = 10
            },
            new BaseStat
            {
                type = BaseStatType.MaxCubs,
                baseValue = 2,
                perLevelIncrease = 1,
                maxLevel = 8
            },
            new BaseStat
            {
                type = BaseStatType.MaxHunters,
                baseValue = 1,
                perLevelIncrease = 1,
                maxLevel = 5
            },
            new BaseStat
            {
                type = BaseStatType.MaxFoodCarriers,
                baseValue = 1,
                perLevelIncrease = 1,
                maxLevel = 5
            },
            new BaseStat
            {
                type = BaseStatType.MaxFoodPerCarrier,
                baseValue = 3,
                perLevelIncrease = 1,
                maxLevel = 7
            },
            new BaseStat
            {
                type = BaseStatType.CubHungerDrainRate,
                baseValue = 2f,
                perLevelIncrease = -0.2f,
                maxLevel = 8
            }
        };
    }

    public bool UpgradeBaseStat(BaseStatType type)
    {
        BaseStat stat = GetBaseStat(type);

        if (stat == null || !stat.CanUpgrade)
            return false;

        int cost = GetUpgradeCost(type);

        if (player != null && !player.TrySpendMeat(cost))
            return false;

        stat.Upgrade();
        ApplyBaseStats();

        OnBaseStatsChanged?.Invoke();
        return true;
    }

    public int GetUpgradeCost(BaseStatType type)
    {
        BaseStat stat = GetBaseStat(type);

        if (stat == null)
            return 0;

        return 15 + stat.level * 10;
    }

    public BaseStat GetBaseStat(BaseStatType type)
    {
        return baseStats.Find(s => s.type == type);
    }

    public void ApplyBaseStats()
    {
        foreach (BaseStat stat in baseStats)
        {
            switch (stat.type)
            {
                case BaseStatType.MaxFoodAtBase:
                    MaxFoodAtBase = Mathf.RoundToInt(stat.CurrentValue);
                    ApplyMaxFoodAtBase();
                    break;

                case BaseStatType.MaxCubs:
                    MaxCubs = Mathf.RoundToInt(stat.CurrentValue);
                    ApplyMaxCubs();
                    break;

                case BaseStatType.MaxHunters:
                    MaxHunters = Mathf.RoundToInt(stat.CurrentValue);
                    break;

                case BaseStatType.MaxFoodCarriers:
                    MaxFoodCarriers = Mathf.RoundToInt(stat.CurrentValue);
                    break;

                case BaseStatType.MaxFoodPerCarrier:
                    MaxFoodPerCarrier = Mathf.RoundToInt(stat.CurrentValue);
                    ApplyMaxFoodPerCarrier();
                    break;

                case BaseStatType.CubHungerDrainRate:
                    CubHungerDrainRate = Mathf.Max(0.1f, stat.CurrentValue);
                    ApplyCubHungerDrainRate();
                    break;
            }
        }
    }

    private void ApplyMaxFoodAtBase()
    {
        if (homeFoodPoint != null)
            homeFoodPoint.maxFoodCapacity = MaxFoodAtBase;
    }

    private void ApplyMaxCubs()
    {
        if (baseCubManager != null)
            baseCubManager.maxCubCount = MaxCubs;
    }

    private void ApplyMaxFoodPerCarrier()
    {
        for (int i = 0; i < activeFoodCarriers.Count; i++)
        {
            if (activeFoodCarriers[i] != null)
                activeFoodCarriers[i].maxCarryAmount = MaxFoodPerCarrier;
        }
    }

    private void ApplyCubHungerDrainRate()
    {
        AnimalCubAI[] cubs = FindObjectsByType<AnimalCubAI>(FindObjectsSortMode.None);

        for (int i = 0; i < cubs.Length; i++)
        {
            if (cubs[i] != null)
                cubs[i].healthDrainPerSecond = CubHungerDrainRate;
        }
    }

    public bool CanAddCub(int currentCubCount)
    {
        return currentCubCount < MaxCubs;
    }

    public bool CanAddHunter(int currentHunterCount)
    {
        return currentHunterCount < MaxHunters;
    }

    public bool CanAddFoodCarrier(int currentCarrierCount)
    {
        return currentCarrierCount < MaxFoodCarriers;
    }

    public void RegisterFoodCarrier(FoodCarrierAI carrier)
    {
        if (carrier == null)
            return;

        if (!activeFoodCarriers.Contains(carrier))
            activeFoodCarriers.Add(carrier);

        carrier.maxCarryAmount = MaxFoodPerCarrier;
    }

    public void UnregisterFoodCarrier(FoodCarrierAI carrier)
    {
        if (carrier == null)
            return;

        activeFoodCarriers.Remove(carrier);
    }
}