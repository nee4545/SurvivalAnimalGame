using UnityEngine;
using TMPro;
using DG.Tweening;

public class LevelUpPopupUI : MonoBehaviour
{
    [Header("References")]
    public CCActor player;
    public GameObject popupRoot;
    public TextMeshProUGUI levelUpText;
    public Transform animatedTarget;

    [Header("Animation")]
    public float startScale = 0.8f;
    public float peakScale = 1.15f;
    public float settleScale = 1f;
    public float scaleUpDuration = 0.15f;
    public float scaleDownDuration = 0.12f;
    public float visibleDuration = 0.8f;

    private int _lastKnownLevel;
    private Sequence _sequence;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        if (popupRoot == null)
            popupRoot = gameObject;

        if (animatedTarget == null && levelUpText != null)
            animatedTarget = levelUpText.transform;

        popupRoot.SetActive(false);

        if (player != null)
            _lastKnownLevel = player.playerLevel;
    }

    private void OnEnable()
    {
        if (player == null)
            player = FindObjectOfType<CCActor>();

        if (player != null)
            player.OnProgressChanged += HandleProgressChanged;
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnProgressChanged -= HandleProgressChanged;
    }

    private void OnDestroy()
    {
        if (_sequence != null)
            _sequence.Kill();
    }

    private void HandleProgressChanged()
    {
        if (player == null) return;

        if (player.playerLevel > _lastKnownLevel)
        {
            _lastKnownLevel = player.playerLevel;
            ShowLevelUp(player.playerLevel);
        }
    }

    public void ShowLevelUp(int newLevel)
    {
        if (_sequence != null)
            _sequence.Kill();

        popupRoot.SetActive(true);

        if (animatedTarget != null)
            animatedTarget.localScale = Vector3.one * startScale;

        _sequence = DOTween.Sequence();

        if (animatedTarget != null)
        {
            _sequence.Append(animatedTarget.DOScale(peakScale, scaleUpDuration).SetEase(Ease.OutBack));
            _sequence.Append(animatedTarget.DOScale(settleScale, scaleDownDuration).SetEase(Ease.InOutSine));
        }

        _sequence.AppendInterval(visibleDuration);

        _sequence.OnComplete(() =>
        {
            popupRoot.SetActive(false);
        });
    }
}