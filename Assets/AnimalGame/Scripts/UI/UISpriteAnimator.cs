using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Animation Frames")]
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();

    [Header("Playback Settings")]
    [SerializeField] private float frameRate = 0.1f; // seconds per frame
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;

    private int currentIndex;
    private float timer;
    private bool isPlaying;

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || sprites.Count == 0 || targetImage == null)
            return;

        timer += Time.deltaTime;

        if (timer >= frameRate)
        {
            timer = 0f;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
        targetImage.sprite = sprites[currentIndex];
        currentIndex++;

        if (currentIndex >= sprites.Count)
        {
            if (loop)
                currentIndex = 0;
            else
                Stop();
        }
    }

    // 🔹 Public Controls

    public void Play()
    {
        if (sprites.Count == 0 || targetImage == null)
            return;

        isPlaying = true;
        currentIndex = 0;
        timer = 0f;
        targetImage.sprite = sprites[currentIndex];
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void SetSprites(List<Sprite> newSprites)
    {
        sprites = newSprites;
        currentIndex = 0;
    }
}
