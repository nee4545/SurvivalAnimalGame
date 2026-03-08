using UnityEngine;

public class CompanionMenuUI : MonoBehaviour
{
    public CCActor player;
    public UICompanionCard[] companionCards;

    private void OnEnable()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (player == null) return;

        for (int i = 0; i < companionCards.Length; i++)
        {
            if (companionCards[i] != null)
                companionCards[i].Bind(player);
        }
    }
}