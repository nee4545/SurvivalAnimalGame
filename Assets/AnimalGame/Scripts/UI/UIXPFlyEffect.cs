using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIXPFlyEffect : MonoBehaviour
{
    public static UIXPFlyEffect Instance;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private RectTransform xpTextPrefab;
    [SerializeField] private RectTransform xpTargetPoint;
    [SerializeField] private Camera worldCamera;

    [Header("World Spawn Offset")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Animation")]
    [SerializeField] private float startScale = 1.6f;
    [SerializeField] private float endScale = 0.45f;
    [SerializeField] private float riseAmount = 90f;
    [SerializeField] private float riseDuration = 0.35f;
    [SerializeField] private float flyDuration = 0.55f;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Timing")]
    [SerializeField] private float holdBeforeFly = 0.15f;

    private RectTransform canvasRect;
    private readonly Queue<RectTransform> pool = new Queue<RectTransform>();

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (spawnParent == null)
            spawnParent = canvasRect;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (xpTextPrefab != null)
            xpTextPrefab.gameObject.SetActive(false);
    }

    private void Start()
    {
        
    }

    public void PlayXPReward(Vector3 worldPosition, int xpAmount)
    {
        if (xpAmount <= 0 || xpTextPrefab == null || canvas == null)
            return;

        Vector3 adjustedWorldPosition = worldPosition + worldOffset;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, adjustedWorldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParent,
            screenPosition,
            GetUICamera(),
            out Vector2 localSpawnPosition
        );

        PlayXPRewardFromUIPosition(localSpawnPosition, xpAmount);
    }



    public void PlayXPRewardFromUIPosition(Vector2 uiPosition, int xpAmount)
    {
        RectTransform xpText = GetXPText();

        xpText.SetParent(spawnParent, false);

        xpText.anchorMin = new Vector2(0.5f, 0.5f);
        xpText.anchorMax = new Vector2(0.5f, 0.5f);
        xpText.pivot = new Vector2(0.5f, 0.5f);

        xpText.anchoredPosition = uiPosition;
        xpText.localPosition = new Vector3(xpText.localPosition.x, xpText.localPosition.y, 0f);
        xpText.localRotation = Quaternion.identity;
        xpText.localScale = Vector3.one * startScale;

        xpText.gameObject.SetActive(true);

        TextMeshProUGUI tmp = xpText.GetComponentInChildren<TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = $"+{xpAmount} XP";
        }

        CanvasGroup canvasGroup = xpText.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = xpText.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        Vector2 risePosition = uiPosition + Vector2.up * riseAmount;
        Vector2 targetPosition = GetTargetLocalPosition();

        xpText.DOKill();
        canvasGroup.DOKill();

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            xpText.DOAnchorPos(risePosition, riseDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            xpText.DOScale(4.15f, riseDuration)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(holdBeforeFly);

        sequence.Append(
            xpText.DOAnchorPos(targetPosition, flyDuration)
                .SetEase(Ease.InOutQuad)
        );

        sequence.Join(
            xpText.DOScale(endScale, flyDuration)
                .SetEase(Ease.InQuad)
        );

        sequence.Append(
            canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.OnComplete(() =>
        {
            PulseXPTarget();
            ReturnXPText(xpText);
        });
    }

    private Vector2 GetTargetLocalPosition()
    {
        if (xpTargetPoint == null)
            return Vector2.zero;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetUICamera(), xpTargetPoint.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParent,
            screenPosition,
            GetUICamera(),
            out Vector2 localTargetPosition
        );

        return localTargetPosition;
    }

    private Camera GetUICamera()
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private void PulseXPTarget()
    {
        if (xpTargetPoint == null)
            return;

        xpTargetPoint.DOKill();
        xpTargetPoint.localScale = Vector3.one;

        xpTargetPoint
            .DOScale(1.12f, 0.12f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                xpTargetPoint.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad);
            });
    }

    private RectTransform GetXPText()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        return Instantiate(xpTextPrefab);
    }

    private void ReturnXPText(RectTransform xpText)
    {
        if (xpText == null)
            return;

        xpText.DOKill();

        CanvasGroup canvasGroup = xpText.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
        }

        xpText.gameObject.SetActive(false);
        pool.Enqueue(xpText);
    }
}