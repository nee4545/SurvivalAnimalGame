using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationEnum
{
    IDLE,
    WALKING,
    RUNNING,
    RUNN_SLASH,
    SLASH,
    THROW,
    KICK,
    DEATH,
    HURT,
}

[System.Serializable]
public struct AnimationDef
{
    public List<Sprite> sprites;
    public AnimationEnum animation;
}


public class AnimationHandler : MonoBehaviour
{

    public List<AnimationDef> AnimSprites;
    public float animTimePerframe = 0.1f;
    int currentIndex = 0;
    float currentTime = 0f;
    public AnimationEnum currentAnimation;
    public SpriteRenderer charSprite;
    AnimationDef currentAnimationDef;
    bool needToPlayDeath = false;
    private bool deathAnimationFinished = false;
    public float hurtAnimationDuration = 0.25f; // Duration for HURT animation
    private bool isAnimationLocked = false; // Flag to prevent animation state changes
    private float hurtAnimationTimeElapsed = 0f; // Time elapsed during hurt animation
    ChibiWarriors chibi = null;


    // Start is called before the first frame update
    void Start()
    {
        charSprite = GetComponent<SpriteRenderer>();
        chibi = GetComponent<ChibiWarriors>();

        if(AnimSprites.Count>0)
        {
            for(int i=0; i<AnimSprites.Count; i++)
            {
                if (AnimSprites[i].animation == currentAnimation)
                {
                    currentAnimationDef = AnimSprites[i];
                    break;
                }
            }
        }
    }

    public void ChangeAnimation(AnimationEnum animation, float animTime = 0.1f)
    {
        if (isAnimationLocked && animation!= AnimationEnum.DEATH)
            return;

        if (currentAnimation == animation)
            return;


        if (AnimSprites.Count > 0)
        {
            for (int i = 0; i < AnimSprites.Count; i++)
            {
                if (AnimSprites[i].animation == animation)
                {
                    currentAnimationDef = AnimSprites[i];
                    currentAnimation = animation;
                    break;
                }
            }
        }

        animTimePerframe = animTime;
    }

    public bool isInLastAnimFrame()
    {
        return currentIndex == currentAnimationDef.sprites.Count - 3;
    }

    public int GetTotalFrames()
    {
        return currentAnimationDef.sprites.Count;
    }

    // Update is called once per frame
    void Update()
    {

        if (AnimSprites == null || AnimSprites.Count <= 0)
        {
            return;
        }

        currentTime += Time.deltaTime;

        // Check if we should stop updating for death animation
        if (currentAnimation == AnimationEnum.DEATH)
        {
            // Only update if the death animation hasn't finished yet
            if (!deathAnimationFinished)
            {
                if (currentIndex == currentAnimationDef.sprites.Count - 1)
                {
                    // Set the sprite to the last frame and mark the animation as finished
                    GetComponent<SpriteRenderer>().sprite = currentAnimationDef.sprites[currentIndex];
                    deathAnimationFinished = true;  // Mark the death animation as finished
                    return;
                }
            }
        }


        if (currentAnimation == AnimationEnum.HURT)
        {
            hurtAnimationTimeElapsed += Time.deltaTime;
            if (hurtAnimationTimeElapsed >= hurtAnimationDuration)
            {
                // Reset the animation lock and elapsed time after HURT animation finishes
                hurtAnimationTimeElapsed = 0f;
                isAnimationLocked = false;
                //ChangeAnimation(AnimationEnum.IDLE); // Optional: switch back to IDLE or a default state
                return;
            }
        }

        if (currentTime >= animTimePerframe)
        {
            currentIndex++;

            if (currentIndex >= currentAnimationDef.sprites.Count)
            {
                // Reset index for non-death animations only
                if (currentAnimation != AnimationEnum.DEATH && currentAnimation!= AnimationEnum.HURT)
                {
                    currentIndex = 0; // Loop the animation
                }
                else
                {
                    currentIndex = currentAnimationDef.sprites.Count - 1;
                    // If working with death animation, don't reset
                    //return; // End the update loop for death animation
                }
            }


            charSprite.sprite = currentAnimationDef.sprites[currentIndex];
            currentTime = 0f;
        }

    }


    IEnumerator DestroyGameObjectAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);

        Destroy(this.gameObject);

        yield return null;
    }


    public void PlayDeathAnimation()
    {
        needToPlayDeath = true;
        deathAnimationFinished = false;
        ChangeAnimation(AnimationEnum.DEATH, 0.035f);
        StartCoroutine(DestroyGameObjectAfterDelay());
    }

    public void PlayHurtAnimation()
    {
        ChangeAnimation(AnimationEnum.HURT, 0.035f);
        isAnimationLocked = true; // Lock animations while playing HURT
        // You can also handle a call for PlayDeathAnimation if needed
    }


}
