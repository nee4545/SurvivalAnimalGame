using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICoinFlyEffect : MonoBehaviour
{
    public static UICoinFlyEffect Instance;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform coinPrefab;
    [SerializeField] private RectTransform coinTarget;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Camera")]
    [SerializeField] private Camera worldCamera;

    [Header("Coin Burst Settings")]
    [SerializeField] private int maxVisualCoins = 12;
    [SerializeField] private float burstRadius = 120f;
    [SerializeField] private float burstDuration = 0.25f;
    [SerializeField] private float flyDuration = 0.55f;
    [SerializeField] private float coinDelayStep = 0.035f;

    [Header("Animation")]
    [SerializeField] private float startScale = 0.25f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float endScale = 0.25f;

    private RectTransform canvasRect;
    private int currentCoins;

    private readonly Queue<RectTransform> pool = new Queue<RectTransform>();

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        canvasRect = canvas.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (coinPrefab != null)
            coinPrefab.gameObject.SetActive(false);
    }

    private void Start()
    {
       
    }

    public void PlayCoinReward(Vector3 worldPosition, int coinAmount)
    {
        if (coinAmount <= 0)
            return;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localSpawnPosition
        );

        PlayCoinRewardFromUIPosition(localSpawnPosition, coinAmount);
    }

    public void PlayCoinRewardFromUIPosition(Vector2 uiPosition, int coinAmount)
    {
        int visualCoinCount = Mathf.Clamp(coinAmount, 1, maxVisualCoins);

        for (int i = 0; i < visualCoinCount; i++)
        {
            RectTransform coin = GetCoin();
            coin.SetParent(canvasRect, false);
            coin.anchoredPosition = uiPosition;
            coin.localScale = Vector3.one * startScale;
            coin.gameObject.SetActive(true);

            Vector2 randomBurstPos = uiPosition + Random.insideUnitCircle * burstRadius;

            Sequence seq = DOTween.Sequence();

            seq.AppendInterval(i * coinDelayStep);

            seq.Append(coin.DOScale(popScale, burstDuration).SetEase(Ease.OutBack));
            seq.Join(coin.DOAnchorPos(randomBurstPos, burstDuration).SetEase(Ease.OutQuad));

            seq.Append(coin.DOAnchorPos(GetTargetLocalPosition(), flyDuration).SetEase(Ease.InBack));
            seq.Join(coin.DOScale(endScale, flyDuration).SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                ReturnCoin(coin);
                PulseCoinTarget();
            });
        }

        StartCoroutine(AddCoinsAfterDelay(coinAmount, 0.45f));
    }

    private Vector2 GetTargetLocalPosition()
    {
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, coinTarget.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localTargetPosition
        );

        return localTargetPosition;
    }

    private IEnumerator AddCoinsAfterDelay(int amount, float delay)
    {
        yield return new WaitForSeconds(delay);

        currentCoins += amount;

        if (coinText != null)
            coinText.text = currentCoins.ToString();
    }

    private void PulseCoinTarget()
    {
        if (coinTarget == null)
            return;

        coinTarget.DOKill();
        coinTarget.localScale = Vector3.one;

        coinTarget
            .DOScale(1.15f, 0.12f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                coinTarget.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad);
            });
    }

    private RectTransform GetCoin()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        RectTransform newCoin = Instantiate(coinPrefab);
        return newCoin;
    }

    private void ReturnCoin(RectTransform coin)
    {
        coin.DOKill();
        coin.gameObject.SetActive(false);
        pool.Enqueue(coin);
    }

    public void SetCurrentCoins(int amount)
    {
        currentCoins = amount;

        if (coinText != null)
            coinText.text = currentCoins.ToString();
    }
}