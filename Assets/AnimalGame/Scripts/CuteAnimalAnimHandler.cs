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


public class CuteAnimalAnimHandler : MonoBehaviour
{
    public Animator animator;
    bool isLocked = false;
    eCuteAnimalAnims currentAnimState = eCuteAnimalAnims.NONE;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
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

    public void SetAnimation(eCuteAnimalAnims animation)
    {
        if (isLocked)
            return;

        if (currentAnimState == animation)
            return;

        currentAnimState = animation;

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
