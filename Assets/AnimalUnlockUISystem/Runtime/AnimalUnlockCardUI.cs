using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalUnlockCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image background;
    public Image animalIcon;
    public TextMeshProUGUI animalNameText;
    public TextMeshProUGUI requiredLevelText;
    public GameObject lockOverlay;
    public GameObject unlockedBadge;
    public Button selectButton;

    private AnimalUnlockData data;

    public AnimalUnlockData Data => data;

    public void Setup(AnimalUnlockData unlockData, int playerLevel)
    {
        data = unlockData;

        if (animalIcon)
            animalIcon.sprite = data ? data.icon : null;

        if (animalNameText)
            animalNameText.text = data ? data.displayName : "Animal";

        bool isUnlocked = data != null && playerLevel >= data.unlockLevel;

        if (lockOverlay)
            lockOverlay.SetActive(!isUnlocked);

        if (unlockedBadge)
            unlockedBadge.SetActive(isUnlocked);

        if (selectButton)
            selectButton.interactable = isUnlocked;

        if (requiredLevelText && data != null)
            requiredLevelText.text = $"Unlocks at Lv. {data.unlockLevel}";

        if (animalIcon)
        {
            Color c = animalIcon.color;
            c.a = isUnlocked ? 1f : 0.45f;
            animalIcon.color = c;
        }
    }
}
