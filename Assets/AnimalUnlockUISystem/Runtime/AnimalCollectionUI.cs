using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnimalCollectionUI : MonoBehaviour
{
    [Header("Player")]
    public CCActor player;

    [Header("Animal Data")]
    public List<AnimalUnlockData> animals = new();

    [Header("UI")]
    public Transform contentParent;
    public AnimalUnlockCardUI cardPrefab;
    public TextMeshProUGUI levelText;

    private readonly List<AnimalUnlockCardUI> spawnedCards = new();

    private void OnEnable()
    {
        if (player != null)
            player.OnProgressChanged += Refresh;

        BuildCards();
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnProgressChanged -= Refresh;
    }

    public void Rebuild()
    {
        ClearCards();
        BuildCards();
        Refresh();
    }

    private void ClearCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            if (spawnedCards[i])
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();
    }

    private void BuildCards()
    {
        if (contentParent == null || cardPrefab == null)
            return;

        if (spawnedCards.Count > 0)
            return;

        foreach (AnimalUnlockData animal in animals)
        {
            AnimalUnlockCardUI card = Instantiate(cardPrefab, contentParent);
            card.gameObject.SetActive(true);
            spawnedCards.Add(card);
        }
    }

    public void Refresh()
    {
        if (player == null)
            return;

        if (levelText)
            levelText.text = $"Level {player.playerLevel}";

        int count = Mathf.Min(spawnedCards.Count, animals.Count);

        for (int i = 0; i < count; i++)
            spawnedCards[i].Setup(animals[i], player.playerLevel);
    }
}
