using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StampedeModeUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject uiRoot;

    [Header("Texts")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI timerText;

    [Header("Lives - Hardcoded 3")]
    public Image lifeImage1;
    public Image lifeImage2;
    public Image lifeImage3;

    [Header("Settings")]
    public string headerMessage = "Stampede Run!";
    public float lifeFadeDuration = 0.25f;

    private Image[] lifeImages;
    private Coroutine[] fadeRoutines;
    private int currentLives;

    private void Awake()
    {
        lifeImages = new Image[3]
        {
            lifeImage1,
            lifeImage2,
            lifeImage3
        };

        fadeRoutines = new Coroutine[3];

        Hide();
    }

    public void Show(float duration)
    {
        if (uiRoot != null)
            uiRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (headerText != null)
            headerText.text = headerMessage;

        currentLives = 3;

        ResetLives();
        UpdateTimer(duration);
    }

    public void Hide()
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void UpdateTimer(float remainingTime)
    {
        if (timerText == null)
            return;

        remainingTime = Mathf.Max(0f, remainingTime);

        int seconds = Mathf.CeilToInt(remainingTime);
        timerText.text = seconds.ToString();
    }

    public void SetLives(int lives)
    {
        lives = Mathf.Clamp(lives, 0, 3);

        if (lives == currentLives)
            return;

        if (lives < currentLives)
        {
            for (int i = currentLives - 1; i >= lives; i--)
            {
                FadeAndDisableLife(i);
            }
        }
        else
        {
            for (int i = 0; i < lives; i++)
            {
                EnableLife(i);
            }
        }

        currentLives = lives;
    }

    private void ResetLives()
    {
        for (int i = 0; i < lifeImages.Length; i++)
        {
            EnableLife(i);
        }
    }

    private void EnableLife(int index)
    {
        if (!IsValidLifeIndex(index))
            return;

        if (fadeRoutines[index] != null)
        {
            StopCoroutine(fadeRoutines[index]);
            fadeRoutines[index] = null;
        }

        Image image = lifeImages[index];

        image.gameObject.SetActive(true);

        Color color = image.color;
        color.a = 1f;
        image.color = color;
    }

    private void FadeAndDisableLife(int index)
    {
        if (!IsValidLifeIndex(index))
            return;

        if (fadeRoutines[index] != null)
            StopCoroutine(fadeRoutines[index]);

        fadeRoutines[index] = StartCoroutine(FadeLifeRoutine(index));
    }

    private IEnumerator FadeLifeRoutine(int index)
    {
        Image image = lifeImages[index];

        if (image == null)
            yield break;

        image.gameObject.SetActive(true);

        Color startColor = image.color;
        startColor.a = 1f;

        Color endColor = startColor;
        endColor.a = 0f;

        float timer = 0f;

        while (timer < lifeFadeDuration)
        {
            timer += Time.deltaTime;

            float t = timer / lifeFadeDuration;
            image.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        image.color = endColor;
        image.gameObject.SetActive(false);

        fadeRoutines[index] = null;
    }

    private bool IsValidLifeIndex(int index)
    {
        if (lifeImages == null)
            return false;

        if (index < 0 || index >= lifeImages.Length)
            return false;

        return lifeImages[index] != null;
    }
}