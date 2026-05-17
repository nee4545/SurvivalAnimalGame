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
    GenericInt,   // old system: int "animation"
    SpiderBool,   // spider controller: isWalking / isScared / isAttacking / isDead1/2/3
    SnakeBool     // snake controller: isIdle / isWalking / isAttacking / isDead
}

[System.Serializable]
public class AnimationBoolSet
{
    [Tooltip("List of bool parameter names on the Animator for this category. One will be randomly chosen.")]
    public string[] parameterNames;
}

[System.Serializable]
public class AnimationNameSet
{
    [Tooltip("Animator state names for this category. One will be randomly chosen. These are NOT parameters.")]
    public string[] stateNames;
}

public class CuteAnimalAnimHandler : MonoBehaviour
{
    public Animator animator;

    [Header("Controller Type (for old animals)")]
    public CuteAnimControllerType controllerType = CuteAnimControllerType.GenericInt;

    [Header("Human / Direct State Name Animations")]
    [Tooltip("Enable this for humanoid/human characters where you want to play Animator states directly by name instead of using parameters.")]
    public bool isHuman = false;

    [Tooltip("Use a small fade when switching states. Disable if you want animator.Play() to snap immediately.")]
    public bool humanUseCrossFade = true;

    [Tooltip("Fade duration used when humanUseCrossFade is enabled.")]
    public float humanCrossFadeTime = 0.1f;

    public AnimationNameSet humanIdleSet;
    public AnimationNameSet humanWalkSet;
    public AnimationNameSet humanRunSet;
    public AnimationNameSet humanJumpSet;
    public AnimationNameSet humanAttackSet;
    public AnimationNameSet humanDeathSet;
    public AnimationNameSet humanRestSet;
    public AnimationNameSet humanEatSet;
    public AnimationNameSet humanDamageSet;

    [Header("PolyPerfect Pack")]
    [Tooltip("Enable this for animals using the new PolyPerfect-style Animator (bool parameters).")]
    public bool isPolyPerfectAnimal = false;

    [Tooltip("Idle / stand / look-around variants.")]
    public AnimationBoolSet polyIdleSet;

    [Tooltip("Walk / slow locomotion variants.")]
    public AnimationBoolSet polyWalkSet;

    [Tooltip("Run / fast locomotion variants.")]
    public AnimationBoolSet polyRunSet;

    [Tooltip("Attack / bite / lunge variants.")]
    public AnimationBoolSet polyAttackSet;

    [Tooltip("Death / fall variants.")]
    public AnimationBoolSet polyDeathSet;

    [Tooltip("Rest / sleep variants.")]
    public AnimationBoolSet polyRestSet;

    [Tooltip("Eat / graze / drink variants.")]
    public AnimationBoolSet polyEatSet;

    // Cached bool parameter names for PolyPerfect-style controllers
    private HashSet<string> _polyBoolParams;

    bool isLocked = false;
    eCuteAnimalAnims currentAnimState = eCuteAnimalAnims.NONE;


    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {


        if (isPolyPerfectAnimal)
        {
            CachePolyBoolParameters();
        }

        SetAnimation(eCuteAnimalAnims.IDLE);

    }

    void Update()
    {
        // no-op, driven externally
    }

    // ---------------- HUMAN / DIRECT STATE NAME HELPERS ----------------

    bool TryPlayHumanNonEmpty(AnimationNameSet set)
    {
        if (set == null || set.stateNames == null || set.stateNames.Length == 0)
            return false;

        PlayHumanFromSet(set);
        return true;
    }

    void PlayHumanFromSet(AnimationNameSet set)
    {
        if (animator == null || set == null || set.stateNames == null || set.stateNames.Length == 0)
            return;

        string chosen = set.stateNames.Length == 1
            ? set.stateNames[0]
            : set.stateNames[Random.Range(0, set.stateNames.Length)];

        if (string.IsNullOrEmpty(chosen))
            return;

        // This plays an Animator STATE directly by name.
        // The name must match the state name inside your Animator Controller.
        if (humanUseCrossFade && humanCrossFadeTime > 0f)
            animator.CrossFadeInFixedTime(chosen, humanCrossFadeTime, 0);
        else
            animator.Play(chosen, 0, 0f);
    }

    void ApplyHumanAnimation(eCuteAnimalAnims animation)
    {
        switch (animation)
        {
            case eCuteAnimalAnims.IDLE:
            case eCuteAnimalAnims.NONE:
                TryPlayHumanNonEmpty(humanIdleSet);
                break;

            case eCuteAnimalAnims.WALK:
                if (!TryPlayHumanNonEmpty(humanWalkSet))
                    TryPlayHumanNonEmpty(humanIdleSet);
                break;

            case eCuteAnimalAnims.RUN:
                if (!TryPlayHumanNonEmpty(humanRunSet))
                {
                    if (!TryPlayHumanNonEmpty(humanWalkSet))
                        TryPlayHumanNonEmpty(humanIdleSet);
                }
                break;

            case eCuteAnimalAnims.JUMP:
                if (TryPlayHumanNonEmpty(humanJumpSet))
                    StartCoroutine(LockAnimationRoutine());
                break;

            case eCuteAnimalAnims.ATTACK:
                if (TryPlayHumanNonEmpty(humanAttackSet))
                    StartCoroutine(LockAnimationRoutine(0.5f));
                break;

            case eCuteAnimalAnims.DIE:
                TryPlayHumanNonEmpty(humanDeathSet);
                break;

            case eCuteAnimalAnims.REST:
                if (!TryPlayHumanNonEmpty(humanRestSet))
                    TryPlayHumanNonEmpty(humanIdleSet);
                break;

            case eCuteAnimalAnims.EAT:
                if (!TryPlayHumanNonEmpty(humanEatSet))
                    TryPlayHumanNonEmpty(humanIdleSet);
                break;

            case eCuteAnimalAnims.DAMAGE:
                if (TryPlayHumanNonEmpty(humanDamageSet))
                    StartCoroutine(LockAnimationRoutine(0.35f));
                break;
        }
    }

    // ---------------- POLYPERFECT HELPERS ----------------

    void CachePolyBoolParameters()
    {
        _polyBoolParams = new HashSet<string>();

        if (animator == null)
            return;

        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool)
            {
                _polyBoolParams.Add(p.name);
            }
        }
    }

    void ClearAllPolyBools()
    {
        if (animator == null || _polyBoolParams == null)
            return;

        foreach (var name in _polyBoolParams)
        {
            animator.SetBool(name, false);
        }
    }

    void PlayFromSet(AnimationBoolSet set)
    {
        if (animator == null || set == null || set.parameterNames == null || set.parameterNames.Length == 0)
            return;

        string chosen;

        if (set.parameterNames.Length == 1)
        {
            chosen = set.parameterNames[0];
        }
        else
        {
            int index = Random.Range(0, set.parameterNames.Length);
            chosen = set.parameterNames[index];
        }

        if (string.IsNullOrEmpty(chosen))
            return;

        // If we haven't cached bool params (unlikely if isPolyPerfectAnimal is true), just try anyway.
        if (_polyBoolParams != null && !_polyBoolParams.Contains(chosen))
        {
            // Silent fail to avoid spam, or log if you want:
            // Debug.LogWarning($"[CuteAnimalAnimHandler] Animator has no bool '{chosen}' on {name}.");
            return;
        }

        ClearAllPolyBools();
        animator.SetBool(chosen, true);
    }

    void ApplyPolyPerfectAnimation(eCuteAnimalAnims animation)
    {
        switch (animation)
        {
            case eCuteAnimalAnims.IDLE:
            case eCuteAnimalAnims.NONE:
                // If there are defined idle bools, use them.
                // Otherwise, clear all bools so the Animator's default state plays (idle clip).
                if (!TryPlayNonEmpty(polyIdleSet))
                    ClearAllPolyBools();
                break;

            case eCuteAnimalAnims.WALK:
                if (!TryPlayNonEmpty(polyWalkSet))
                {
                    // Fallbacks if walk is missing
                    if (!TryPlayNonEmpty(polyRunSet))
                        if (!TryPlayNonEmpty(polyIdleSet))
                            ClearAllPolyBools();
                }
                break;

            case eCuteAnimalAnims.RUN:
                // prefer run; fall back to walk; then idle; then default state
                if (!TryPlayNonEmpty(polyRunSet))
                {
                    if (!TryPlayNonEmpty(polyWalkSet))
                    {
                        if (!TryPlayNonEmpty(polyIdleSet))
                            ClearAllPolyBools();
                    }
                }
                break;

            case eCuteAnimalAnims.ATTACK:
                if (!TryPlayNonEmpty(polyAttackSet))
                {
                    // Worst case, just go back to idle/default
                    if (!TryPlayNonEmpty(polyIdleSet))
                        ClearAllPolyBools();
                }
                break;

            case eCuteAnimalAnims.DIE:
                if (!TryPlayNonEmpty(polyDeathSet))
                {
                    // If no death anim wired, just freeze in whatever it was doing
                    // (or you can ClearAllPolyBools() if you prefer)
                    ClearAllPolyBools();
                }
                break;

            case eCuteAnimalAnims.REST:
                if (!TryPlayNonEmpty(polyRestSet))
                {
                    if (!TryPlayNonEmpty(polyIdleSet))
                        ClearAllPolyBools();
                }
                break;

            case eCuteAnimalAnims.EAT:
                if (!TryPlayNonEmpty(polyEatSet))
                {
                    if (!TryPlayNonEmpty(polyIdleSet))
                        ClearAllPolyBools();
                }
                break;

            case eCuteAnimalAnims.DAMAGE:
                // Quick hack: small flinch → fall back to idle/default
                if (!TryPlayNonEmpty(polyIdleSet))
                    ClearAllPolyBools();
                break;

            case eCuteAnimalAnims.JUMP:
                // Many animals don't have a jump; treat as run/walk
                if (!TryPlayNonEmpty(polyRunSet))
                {
                    if (!TryPlayNonEmpty(polyWalkSet))
                    {
                        if (!TryPlayNonEmpty(polyIdleSet))
                            ClearAllPolyBools();
                    }
                }
                break;
        }
    }


    bool TryPlayNonEmpty(AnimationBoolSet set)
    {
        if (set == null || set.parameterNames == null || set.parameterNames.Length == 0)
            return false;

        PlayFromSet(set);
        return true;
    }

    // ---------------- LOCKING HELPERS ----------------

    void LockAnimation()
    {
        isLocked = true;
    }

    IEnumerator LockAnimationRoutine(float delay = 1f)
    {
        // LockAnimation(); // currently not locking before delay
        yield return new WaitForSeconds(delay);
        isLocked = false;
    }

    // ---------------- SPIDER / SNAKE SPECIAL HANDLERS (OLD) ----------------

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

    // ---------------- MAIN ENTRY POINT ----------------

    public void SetAnimation(eCuteAnimalAnims animation)
    {
        if (isLocked)
            return;

        if (currentAnimState == animation)
            return;

        currentAnimState = animation;

        // 1) Human path: play Animator states directly by name, no parameters
        if (isHuman)
        {
            ApplyHumanAnimation(animation);
            return;
        }

        // 2) PolyPerfect path: new pack animals
        if (isPolyPerfectAnimal)
        {
            ApplyPolyPerfectAnimation(animation);
            return;
        }

        // 3) Old bool-based specials
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

        // 4) Default old system: int "animation" parameter
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
