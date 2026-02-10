using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStatCard : MonoBehaviour
{
    public PlayerStatType statType;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI nextValueText;
    public Button upgradeButton;

    private CCActor player;

    public void Bind(CCActor actor)
    {
        player = actor;
        Refresh();
    }

    public void Refresh()
    {
        var stat = player.GetStat(statType);

        //nameText.text = statType.ToString();
        currentValueText.text = stat.CurrentValue.ToString("0.##");

        if (stat.CanUpgrade)
        {
            nextValueText.text = stat.NextValue.ToString("0.##");
            upgradeButton.interactable = true;
        }
        else
        {
            nextValueText.text = "MAX";
            upgradeButton.interactable = false;
        }
    }

    public void OnUpgradePressed()
    {
        if (player.UpgradeStat(statType))
            Refresh();
    }
}