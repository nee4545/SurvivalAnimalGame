using TMPro;
using UnityEngine;

public class PlayerThoughtUI : MonoBehaviour
{
    [Header("Root Object")]
    public GameObject thoughtRoot;

    [Header("Emoji Objects")]
    public GameObject foodLimitEmoji;
    public GameObject cantCarryCubEmoji;
    public GameObject baseFullEmoji;

    [Header("Text")]
    public TextMeshProUGUI thoughtText;

    [Header("Messages")]
    public string maxFoodText = "Max Limit Reached";
    public string cantCarryCubText = "Drop food and adopt this cub";
    public string baseFullText = "Base is full cannot adopt this Cub";

    [Header("Timing")]
    public float showDuration = 1f;

    private float hideTimer;

    private void Awake()
    {
        HideAll();
    }

    private void OnEnable()
    {
        HideAll();
    }

    private void Update()
    {
        if (hideTimer <= 0f)
            return;

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
            HideAll();
    }

    public void ShowFoodLimit()
    {
        ShowThought(foodLimitEmoji, maxFoodText);
    }

    public void ShowCantCarryCub()
    {
        ShowThought(cantCarryCubEmoji, cantCarryCubText);
    }

    public void ShowBaseFull()
    {
        ShowThought(baseFullEmoji, baseFullText);
    }

    private void ShowThought(GameObject emojiToShow, string message)
    {
        HideEmojiObjectsOnly();

        if (thoughtRoot)
            thoughtRoot.SetActive(true);

        if (emojiToShow)
            emojiToShow.SetActive(true);

        if (thoughtText)
            thoughtText.text = message;

        hideTimer = showDuration;
    }

    private void HideAll()
    {
        HideEmojiObjectsOnly();

        if (thoughtText)
            thoughtText.text = "";

        if (thoughtRoot)
            thoughtRoot.SetActive(false);

        hideTimer = 0f;
    }

    private void HideEmojiObjectsOnly()
    {
        if (foodLimitEmoji)
            foodLimitEmoji.SetActive(false);

        if (cantCarryCubEmoji)
            cantCarryCubEmoji.SetActive(false);

        if (baseFullEmoji)
            baseFullEmoji.SetActive(false);
    }
}