using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpriteFeedbackAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Enabled State")]
    [SerializeField] private List<Sprite> enabledAnimation = new List<Sprite>();
    [SerializeField] private Sprite enabledIdleSprite;

    [Header("Disabled State")]
    [SerializeField] private List<Sprite> disabledAnimation = new List<Sprite>();
    [SerializeField] private Sprite disabledIdleSprite;

    [Header("Playback Settings")]
    [SerializeField] private float frameRate = 0.08f; // seconds per frame

    private Coroutine animationRoutine;

    // -------------------------
    // Public API (call these)
    // -------------------------

    public void PlayEnabledFeedback()
    {
        if (enabledAnimation.Count == 0 || targetImage == null)
            return;

        PlayOnce(enabledAnimation, enabledIdleSprite);
    }

    public void PlayDisabledFeedback()
    {
        if (disabledAnimation.Count == 0 || targetImage == null)
            return;

        PlayOnce(disabledAnimation, disabledIdleSprite);
    }

    // -------------------------
    // Core Logic
    // -------------------------

    private void PlayOnce(List<Sprite> animationFrames, Sprite finalSprite)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(PlayAnimationOnce(animationFrames, finalSprite));
    }

    private IEnumerator PlayAnimationOnce(List<Sprite> frames, Sprite finalSprite)
    {
        for (int i = 0; i < frames.Count; i++)
        {
            targetImage.sprite = frames[i];
            yield return new WaitForSeconds(frameRate);
        }

        // Restore final state
        targetImage.sprite = finalSprite;
        animationRoutine = null;
    }
}
