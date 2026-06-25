using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStatCard : MonoBehaviour
{
    [Header("Stat")]
    public PlayerStatType statType;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI nextValueText;
    public TextMeshProUGUI costText;
    public Button upgradeButton;
    public Image UpgradeIndicator;

    public Sprite CanUpgrade;
    public Sprite CannotUpgrade;

    private CCActor player;

    public void Bind(CCActor actor)
    {
        if (player != null)
            player.OnProgressChanged -= Refresh;

        player = actor;

        if (player != null)
            player.OnProgressChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnProgressChanged -= Refresh;
    }

    public void Refresh()
    {
        if (player == null) return;

        PlayerStat stat = player.GetStat(statType);
        if (stat == null) return;

        int cost = player.GetUpgradeCost(statType);
        bool canAfford = player.Coins >= cost;
        bool canUpgradeNow = stat.CanUpgrade && canAfford;

        if (nameText != null)
            nameText.text = GetDisplayName(statType);

        if (currentValueText != null)
            currentValueText.text = FormatStatValue(statType, stat.CurrentValue);

        if (nextValueText != null)
            nextValueText.text = stat.CanUpgrade ? FormatStatValue(statType, stat.NextValue) : "MAX";

        if (costText != null)
            costText.text = stat.CanUpgrade ? cost.ToString() : "-";

        if(canUpgradeNow)
        {
            UpgradeIndicator.sprite = CanUpgrade;
        }
        else
        {
            UpgradeIndicator.sprite = CannotUpgrade;
        }

        // Always keep button clickable so disabled feedback can play on click
        if (upgradeButton != null)
            upgradeButton.interactable = true;
    }

    public void OnUpgradePressed()
    {
        if (player == null) return;

        PlayerStat stat = player.GetStat(statType);
        if (stat == null) return;

        int cost = player.GetUpgradeCost(statType);
        bool canAfford = player.Coins >= cost;
        bool canUpgradeNow = stat.CanUpgrade && canAfford;

        if (canUpgradeNow)
        {
            bool upgraded = player.UpgradeStat(statType);

            if (upgraded)
            {

                Refresh();
            }
        }
    }

    private string GetDisplayName(PlayerStatType type)
    {
        switch (type)
        {
            case PlayerStatType.MoveSpeed: return "Move Speed";
            case PlayerStatType.StaminaDrainRate: return "Stamina Drain";
            case PlayerStatType.HungerDrainRate: return "Hunger Drain";
            case PlayerStatType.MaxHealth: return "Max Health";
            case PlayerStatType.AttackDamage: return "Attack Damage";
            case PlayerStatType.FoodCarryLimit: return "Food Carry Limit";
            default: return type.ToString();
        }
    }

    private string FormatStatValue(PlayerStatType type, float value)
    {
        switch (type)
        {
            case PlayerStatType.MoveSpeed:
            case PlayerStatType.StaminaDrainRate:
            case PlayerStatType.HungerDrainRate:
                return value.ToString("0.0");

            case PlayerStatType.MaxHealth:
            case PlayerStatType.AttackDamage:
            case PlayerStatType.FoodCarryLimit:
                return Mathf.RoundToInt(value).ToString();

            default:
                return value.ToString("0.##");
        }
    }
}