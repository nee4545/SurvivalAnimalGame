using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eCuteAnimalAnims
{
    IDLE,
    WALK,
    RUN,
    JUMP,
    ATTACK,
    DIE,
    REST,
    EAT,
    DAMAGE,
    NONE,
}

public enum CuteAnimControllerType
{
    GenericInt,   // your current system: int "animation"
    SpiderBool,   // spider controller: isWalking / isScared / isAttacking / isDead1/2/3
    SnakeBool     // snake controller: isIdle / isWalking / isAttacking / isDead
}


public class CuteAnimalAnimHandler : MonoBehaviour
{
    public Animator animator;

    [Header("Controller Type (hack for spider/snake)")]
    public CuteAnimControllerType controllerType = CuteAnimControllerType.GenericInt;

    bool isLocked = false;
    eCuteAnimalAnims currentAnimState = eCuteAnimalAnims.NONE;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        SetAnimation(eCuteAnimalAnims.IDLE);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LockAnimation()
    {
        isLocked = true;
    }

    IEnumerator LockAnimationRoutine(float delay = 1f)
    {
        //LockAnimation();

        yield return new WaitForSeconds(delay);

        isLocked = false;
    }

    // --- SPIDER / SNAKE SPECIAL HANDLERS ---

    void ApplySpiderAnimation(eCuteAnimalAnims animation)
    {
        // Reset all relevant bools first
        animator.SetBool("isWalking", false);
        animator.SetBool("isScared", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isDead1", false);
        animator.SetBool("isDead2", false);
        animator.SetBool("isDead3", false);

        switch (animation)
        {
            case eCuteAnimalAnims.IDLE:
            case eCuteAnimalAnims.REST:
            case eCuteAnimalAnims.EAT:
            case eCuteAnimalAnims.NONE:
                // All bools false → Idle state in your first controller
                break;

            case eCuteAnimalAnims.WALK:
            case eCuteAnimalAnims.RUN:
                animator.SetBool("isWalking", true);
                break;

            case eCuteAnimalAnims.DAMAGE:
                animator.SetBool("isScared", true);
                StartCoroutine(LockAnimationRoutine(0.5f));
                break;

            case eCuteAnimalAnims.ATTACK:
                animator.SetBool("isAttacking", true);
                StartCoroutine(LockAnimationRoutine(0.5f));
                break;

            case eCuteAnimalAnims.DIE:
                // Pick one death variant; you can randomize between 1–3 later
                animator.SetBool("isDead1", true);
                break;

            case eCuteAnimalAnims.JUMP:
                // No real jump on spider → just treat as walk/idle
                animator.SetBool("isWalking", true);
                break;
        }
    }

    void ApplySnakeAnimation(eCuteAnimalAnims animation)
    {
        // Reset all relevant bools first
        animator.SetBool("isIdle", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isDead", false);

        switch (animation)
        {
            case eCuteAnimalAnims.IDLE:
            case eCuteAnimalAnims.REST:
            case eCuteAnimalAnims.EAT:
            case eCuteAnimalAnims.NONE:
                animator.SetBool("isIdle", true);
                break;

            case eCuteAnimalAnims.WALK:
            case eCuteAnimalAnims.RUN:
                animator.SetBool("isWalking", true);   // maps to Slither state
                break;

            case eCuteAnimalAnims.ATTACK:
                animator.SetBool("isAttacking", true);
                StartCoroutine(LockAnimationRoutine(0.5f));
                break;

            case eCuteAnimalAnims.DAMAGE:
                // Simple hack: flash idle again; or reuse attack if you have a hit reaction
                animator.SetBool("isIdle", true);
                StartCoroutine(LockAnimationRoutine(0.25f));
                break;

            case eCuteAnimalAnims.DIE:
                animator.SetBool("isDead", true);
                break;

            case eCuteAnimalAnims.JUMP:
                // No jump on snake; keep slithering
                animator.SetBool("isWalking", true);
                break;
        }
    }


    public void SetAnimation(eCuteAnimalAnims animation)
    {
        if (isLocked)
            return;

        if (currentAnimState == animation)
            return;

        currentAnimState = animation;

        if (controllerType == CuteAnimControllerType.SpiderBool)
        {
            ApplySpiderAnimation(animation);
            return;
        }

        if (controllerType == CuteAnimControllerType.SnakeBool)
        {
            ApplySnakeAnimation(animation);
            return;
        }

        switch (animation)
        {
            case eCuteAnimalAnims.IDLE:
                {
                    animator.SetInteger("animation", 0);
                    break;
                }

            case eCuteAnimalAnims.WALK:
                {
                    animator.SetInteger("animation", 1);
                    break;
                }

            case eCuteAnimalAnims.RUN:
                {
                    animator.SetInteger("animation", 2);
                    break;
                }

            case eCuteAnimalAnims.DAMAGE:
                {
                    animator.SetInteger("animation", 7);
                    StartCoroutine(LockAnimationRoutine(0.5f));
                    break;
                }

            case eCuteAnimalAnims.ATTACK:
                {
                    animator.SetInteger("animation", 6);
                    StartCoroutine(LockAnimationRoutine(0.5f));
                    break;
                }

            case eCuteAnimalAnims.DIE:
                {
                    animator.SetInteger("animation", 8);
                    break;
                }

            case eCuteAnimalAnims.EAT:
                {
                    animator.SetInteger("animation", 4);
                    break;
                }

            case eCuteAnimalAnims.REST:
                {
                    animator.SetInteger("animation", 5);
                    break;
                }

            case eCuteAnimalAnims.JUMP:
                {
                    animator.SetInteger("animation", 3);
                    StartCoroutine(LockAnimationRoutine());
                    break;
                }
        }

        
    }
}
