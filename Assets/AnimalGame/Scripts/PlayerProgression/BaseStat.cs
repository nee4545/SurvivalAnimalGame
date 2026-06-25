using System;
using System.Collections.Generic;
using UnityEngine;

public enum BaseStatType
{
    MaxFoodAtBase,
    MaxCubs,
    MaxHunters,
    MaxFoodCarriers,
    MaxFoodPerCarrier,
    CubHungerDrainRate
}

[Serializable]
public class BaseStat
{
    public BaseStatType type;

    public float baseValue;
    public float perLevelIncrease;
    public int level;
    public int maxLevel = 10;

    public float CurrentValue =>
        baseValue + level * perLevelIncrease;

    public float NextValue =>
        level < maxLevel
            ? baseValue + (level + 1) * perLevelIncrease
            : CurrentValue;

    public bool CanUpgrade => level < maxLevel;

    public void Upgrade()
    {
        if (CanUpgrade)
            level++;
    }
}