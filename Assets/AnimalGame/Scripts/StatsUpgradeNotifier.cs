using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StatsUpgradeNotifier : MonoBehaviour
{
    public enum NotifyMode
    {
        AnyStatUpgradeable,
        MinimumMeat
    }

    [Header("References")]
    public CCActor player;
    public GameObject notificationRoot;
    public RectTransform exclamationTransform;
    public Image exclamationImage;

    [Header("Notify Rule")]
    public NotifyMode notifyMode = NotifyMode.AnyStatUpgradeable;
    public int requiredMeat = 50;

    [Header("Bob Animation")]
    public float bobAmount = 12f;
    public float bobDuration = 0.5f;
    public float scalePunch = 0.05f; // relative amount, e.g. 0.05 = 5%

    private Tween _moveTween;
    private Tween _scaleTween;
    private Vector2 _baseAnchoredPos;
    private Vector3 _baseScale;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        if (notificationRoot == null && exclamationImage != null)
            notificationRoot = exclamationImage.gameObject;

        if (exclamationTransform == null && notificationRoot != null)
            exclamationTransform = notificationRoot.GetComponent<RectTransform>();

        if (exclamationTransform != null)
        {
            _baseAnchoredPos = exclamationTransform.anchoredPosition;
            _baseScale = exclamationTransform.localScale;
        }
    }

    private void OnEnable()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        if (player != null)
            player.OnProgressChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnProgressChanged -= Refresh;

        StopTweens();
    }

    private void OnDestroy()
    {
        StopTweens();
    }

    public void Refresh()
    {
        if (player == null)
        {
            SetNotificationVisible(false);
            return;
        }

        bool shouldNotify = ShouldNotify();

        if (shouldNotify)
            ShowAndAnimate();
        else
            HideNotification();
    }

    private bool ShouldNotify()
    {
        switch (notifyMode)
        {
            case NotifyMode.MinimumMeat:
                return player.storedMeat >= requiredMeat;

            case NotifyMode.AnyStatUpgradeable:
            default:
                return HasAnyUpgradeableStat();
        }
    }

    private bool HasAnyUpgradeableStat()
    {
        if (player.stats == null || player.stats.Count == 0)
            return false;

        for (int i = 0; i < player.stats.Count; i++)
        {
            PlayerStat stat = player.stats[i];
            if (stat == null || !stat.CanUpgrade)
                continue;

            int cost = player.GetUpgradeCost(stat.type);
            if (player.storedMeat >= cost)
                return true;
        }

        return false;
    }

    private void ShowAndAnimate()
    {
        SetNotificationVisible(true);

        if (exclamationTransform == null)
            return;

        if (_moveTween == null || !_moveTween.IsActive())
        {
            exclamationTransform.anchoredPosition = _baseAnchoredPos;

            _moveTween = exclamationTransform
                .DOAnchorPosY(_baseAnchoredPos.y + bobAmount, bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (_scaleTween == null || !_scaleTween.IsActive())
        {
            exclamationTransform.localScale = _baseScale;

            _scaleTween = exclamationTransform
                .DOScale(Vector3.one * scalePunch, bobDuration)
                .SetRelative()
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void HideNotification()
    {
        StopTweens();
        SetNotificationVisible(false);
    }

    private void SetNotificationVisible(bool visible)
    {
        if (notificationRoot != null && notificationRoot.activeSelf != visible)
            notificationRoot.SetActive(visible);
    }

    private void StopTweens()
    {
        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }

        if (_scaleTween != null)
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }

        if (exclamationTransform != null)
        {
            exclamationTransform.anchoredPosition = _baseAnchoredPos;
            exclamationTransform.localScale = _baseScale;
        }
    }
}