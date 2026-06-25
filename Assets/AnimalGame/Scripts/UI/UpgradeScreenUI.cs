using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeScreenUI : MonoBehaviour
{
    private enum UpgradeTab
    {
        Player,
        Base,
        Animals
    }

    [System.Serializable]
    public class UpgradeButton
    {
        public Button button;
        public Image buttonImage;

        [Header("Sprites")]
        public Sprite closedSprite;
        public Sprite openSprite;
    }

    [Header("Top Buttons")]
    [SerializeField] private UpgradeButton playerUpgradeButton;
    [SerializeField] private UpgradeButton baseUpgradeButton;
    [SerializeField] private UpgradeButton animalUnlockButton;

    [Header("Panels")]
    [SerializeField] private GameObject playerUpgradePanel;
    [SerializeField] private GameObject baseUpgradePanel;
    [SerializeField] private GameObject animalUnlockPanel;

    [Header("Title Text")]
    [SerializeField] private TextMeshProUGUI selectedTabText;

    private void Awake()
    {
        if (playerUpgradeButton.button)
            playerUpgradeButton.button.onClick.AddListener(OpenPlayerUpgrades);

        if (baseUpgradeButton.button)
            baseUpgradeButton.button.onClick.AddListener(OpenBaseUpgrades);

        if (animalUnlockButton.button)
            animalUnlockButton.button.onClick.AddListener(OpenAnimalUnlocks);
    }

    private void OnEnable()
    {
        // Always open Player Upgrades first when this screen opens.
        SetTab(UpgradeTab.Player);
    }

    private void OnDestroy()
    {
        if (playerUpgradeButton.button)
            playerUpgradeButton.button.onClick.RemoveListener(OpenPlayerUpgrades);

        if (baseUpgradeButton.button)
            baseUpgradeButton.button.onClick.RemoveListener(OpenBaseUpgrades);

        if (animalUnlockButton.button)
            animalUnlockButton.button.onClick.RemoveListener(OpenAnimalUnlocks);
    }

    public void OpenPlayerUpgrades()
    {
        SetTab(UpgradeTab.Player);
    }

    public void OpenBaseUpgrades()
    {
        SetTab(UpgradeTab.Base);
    }

    public void OpenAnimalUnlocks()
    {
        SetTab(UpgradeTab.Animals);
    }

    private void SetTab(UpgradeTab tab)
    {
        bool isPlayer = tab == UpgradeTab.Player;
        bool isBase = tab == UpgradeTab.Base;
        bool isAnimals = tab == UpgradeTab.Animals;

        SetPanel(playerUpgradePanel, isPlayer);
        SetPanel(baseUpgradePanel, isBase);
        SetPanel(animalUnlockPanel, isAnimals);

        SetButtonVisual(playerUpgradeButton, isPlayer);
        SetButtonVisual(baseUpgradeButton, isBase);
        SetButtonVisual(animalUnlockButton, isAnimals);

        if (selectedTabText)
        {
            selectedTabText.text = tab switch
            {
                UpgradeTab.Player => "Player Upgrades",
                UpgradeTab.Base => "Base Upgrades",
                UpgradeTab.Animals => "Unlock Animals",
                _ => ""
            };
        }
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel)
            panel.SetActive(active);
    }

    private void SetButtonVisual(UpgradeButton upgradeButton, bool isOpen)
    {
        if (upgradeButton.buttonImage == null)
            return;

        upgradeButton.buttonImage.sprite = isOpen
            ? upgradeButton.openSprite
            : upgradeButton.closedSprite;
    }
}