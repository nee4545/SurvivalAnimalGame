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

    [Header("Feedback")]
    public UISpriteFeedbackAnimator affordabilityAnimator;

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
        bool canAfford = player.storedMeat >= cost;
        bool canUpgradeNow = stat.CanUpgrade && canAfford;

        if (nameText != null)
            nameText.text = GetDisplayName(statType);

        if (currentValueText != null)
            currentValueText.text = FormatStatValue(statType, stat.CurrentValue);

        if (nextValueText != null)
            nextValueText.text = stat.CanUpgrade ? FormatStatValue(statType, stat.NextValue) : "MAX";

        if (costText != null)
            costText.text = stat.CanUpgrade ? cost.ToString() : "-";

        // Always keep button clickable so disabled feedback can play on click
        if (upgradeButton != null)
            upgradeButton.interactable = true;

        // Only set idle sprite here
        if (affordabilityAnimator != null)
        {
            if (canUpgradeNow)
                affordabilityAnimator.SetEnabledIdle();
            else
                affordabilityAnimator.SetDisabledIdle();
        }
    }

    public void OnUpgradePressed()
    {
        if (player == null) return;

        PlayerStat stat = player.GetStat(statType);
        if (stat == null) return;

        int cost = player.GetUpgradeCost(statType);
        bool canAfford = player.storedMeat >= cost;
        bool canUpgradeNow = stat.CanUpgrade && canAfford;

        if (canUpgradeNow)
        {
            bool upgraded = player.UpgradeStat(statType);

            if (upgraded)
            {
                if (affordabilityAnimator != null)
                    affordabilityAnimator.PlayEnabledFeedback();

                Refresh();
            }
        }
        else
        {
            if (affordabilityAnimator != null)
                affordabilityAnimator.PlayDisabledFeedback();
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
            case PlayerStatType.CompnionLimit: return "Companion Limit";
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
            case PlayerStatType.CompnionLimit:
                return Mathf.RoundToInt(value).ToString();

            default:
                return value.ToString("0.##");
        }
    }
}