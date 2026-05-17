using DG.Tweening;
using UnityEngine;

public class TweenScaleBob : MonoBehaviour
{
    [Header("Scale Bob")]
    public Vector3 targetScale = new Vector3(1.15f, 1.15f, 1.15f);
    public float duration = 0.5f;
    public Ease easeType = Ease.InOutSine;

    [Header("Loop")]
    public int loops = -1;
    public LoopType loopType = LoopType.Yoyo;

    private Vector3 originalScale;
    private Tween scaleTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayBob();
    }

    private void OnDisable()
    {
        KillTween();
        transform.localScale = originalScale;
    }

    public void PlayBob()
    {
        KillTween();

        transform.localScale = originalScale;

        scaleTween = transform.DOScale(targetScale, duration)
            .SetEase(easeType)
            .SetLoops(loops, loopType)
            .SetUpdate(true);
    }

    public void StopBob()
    {
        KillTween();
        transform.localScale = originalScale;
    }

    private void KillTween()
    {
        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill();
            scaleTween = null;
        }

        transform.DOKill();
    }
}