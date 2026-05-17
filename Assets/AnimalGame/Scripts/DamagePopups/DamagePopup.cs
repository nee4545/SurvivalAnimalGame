using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI textMesh;
    public Transform visualRoot;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public float lifetime = 0.8f;
    public float floatY = 1.2f;
    public float moveDuration = 0.8f;

    [Header("Scale Tween")]
    public float startScale = 0.8f;
    public float punchScale = 1.2f;
    public float settleScale = 1f;
    public float scaleUpTime = 0.12f;
    public float scaleDownTime = 0.15f;

    private Camera cam;

    public void Init(int damage)
    {
        cam = Camera.main;

        if (textMesh != null)
            textMesh.text = damage.ToString();

        if (visualRoot == null)
            visualRoot = transform;

        visualRoot.localScale = Vector3.one * startScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        PlayAnimation();
    }

    public void InitText(string popupText)
    {

        cam = Camera.main;

        if (textMesh != null)
            textMesh.text = popupText;

        if (visualRoot == null)
            visualRoot = transform;

        visualRoot.localScale = Vector3.one * startScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        PlayAnimation();
    }

    void PlayAnimation()
    {
        Sequence seq = DOTween.Sequence();

        // Slight pop
        seq.Append(visualRoot.DOScale(punchScale, scaleUpTime).SetEase(Ease.OutBack));
        seq.Append(visualRoot.DOScale(settleScale, scaleDownTime).SetEase(Ease.InOutSine));

        // Float upward
        transform.DOMoveY(transform.position.y + floatY, moveDuration)
            .SetEase(Ease.OutQuad);

        // Fade
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, lifetime)
                .SetEase(Ease.OutQuad)
                .SetDelay(0.05f);
        }

        Destroy(gameObject, lifetime);
    }

    void LateUpdate()
    {
        // Billboard to camera
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }
}