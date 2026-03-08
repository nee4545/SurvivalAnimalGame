using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICompanionCard : MonoBehaviour
{
    [Header("Companion")]
    public string companionName;
    public GameObject companionPrefab;
    public int meatCost = 50;

    [Header("UI")]
    public TextMeshProUGUI companionNameText;
    public TextMeshProUGUI costText;
    public Button buyButton;

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
        if (companionNameText != null)
            companionNameText.text = companionName;

        if (costText != null)
            costText.text = meatCost.ToString();

        if (buyButton != null)
            buyButton.interactable = true;
    }

    public void OnBuyPressed()
    {
        if (player == null)
            return;

        bool bought = player.TryBuyAndSpawnCompanion(companionPrefab, meatCost);

        if (bought)
        {
            Refresh();
        }
        else
        {
            // Later you can add disabled feedback here
            Debug.Log("Not enough meat or companion prefab missing.");
        }
    }
}