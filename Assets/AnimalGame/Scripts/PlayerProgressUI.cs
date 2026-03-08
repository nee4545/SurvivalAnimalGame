using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProgressUI : MonoBehaviour
{
    [Header("Player")]
    public CCActor player;

    [Header("UI")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI meatText;
    public Slider xpSlider;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();
    }

    private void OnEnable()
    {
        BindPlayer();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindPlayer();
    }

    private void OnDestroy()
    {
        UnbindPlayer();
    }

    private void BindPlayer()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        if (player != null)
            player.OnProgressChanged += Refresh;
    }

    private void UnbindPlayer()
    {
        if (player != null)
            player.OnProgressChanged -= Refresh;
    }

    public void Refresh()
    {
        if (player == null) return;

        if (levelText != null)
            levelText.text = player.playerLevel.ToString();

        if (xpText != null)
            xpText.text = player.currentXP + " / " + player.XPToNextLevel;

        if (meatText != null)
            meatText.text = "Meat :" + player.storedMeat.ToString();

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
            xpSlider.value = player.XPProgress01;
        }
    }
}