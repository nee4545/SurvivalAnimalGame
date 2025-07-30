using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public enum CharacterStateChibi
{
    WALKING,
    MOVING_ENEMY,
    ATTACKING_ENEMY,
    ATTACKING_ENEMY_MELEE,
    ATTACKING_ENEMY_RANGE,
}

[System.Serializable]
public struct CharacterData
{
    public float Damage;
    public bool isMelee;
    public float speed;
    public float health;
    public float animAttackPerFrame;
    public float attackRange;
    public float backAwayDistance;
    public Weapon weapon;
}

public class ChibiWarriors : MonoBehaviour
{
    public CharacterStateChibi currentState = CharacterStateChibi.WALKING;
    public Vector3 direction = Vector3.right;
    public CharacterData characterData = new();
    public bool isEnemyCharacter = false;
    public bool isSpecialCharacter = false;
    public float specialCharMeleeRange = 1.0f;
    GameObject enemy = null;
    private SpriteRenderer spriteRenderer;
    float attackTime = 0;
    public GameObject healthBar;
    bool attackPerofrmed = false;
    AnimationHandler handler = null;
    public float maxHealth = 100;
    float runningAttackRange;
    SortingGroup sortingGroup;
    public bool isDead = false;

    // Start is called before the first frame update
    void Start()
    {
        //characterData.speed = 10;
        //int randomNum = Random.Range(0, spawnPoints.Length);

        //direction = spawnPoints[randomNum].position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();
        characterData.health = maxHealth;
        characterData.Damage = 10;
        healthBar.gameObject.SetActive(false);
        handler = GetComponent<AnimationHandler>();
        runningAttackRange = characterData.attackRange * 1.5f;
    }

    // Update is called once per frame
    void Update()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100);
        if (sortingGroup != null)
            sortingGroup.sortingOrder = Mathf.RoundToInt(transform.position.y * -100);
        healthBar.GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(transform.position.y * -100);

        if (isDead)
            return;

        if (characterData.health <= 0)
        {
            isDead = true;
            handler.PlayDeathAnimation();
            return;
            //Destroy(this.gameObject);
        }

        CheckAndUpdateStates();

        if (currentState == CharacterStateChibi.MOVING_ENEMY)
        {
            MoveTowardsEnemy();
        }

        if (currentState == CharacterStateChibi.ATTACKING_ENEMY)
        {
            AttackEnemyIfClose();
        }

        UpdateHealth();

    }

    void CheckAndUpdateStates()
    {

        if (IsEnemyInScene())
        {
            currentState = CharacterStateChibi.MOVING_ENEMY;
        }
        if (currentState == CharacterStateChibi.MOVING_ENEMY)
        {

            if (enemy.IsDestroyed() || enemy == null)
            {
                currentState = CharacterStateChibi.WALKING;
                enemy = null;
            }

            else if (Vector3.Distance(transform.position, enemy.transform.position) <= characterData.attackRange)
            {
                currentState = CharacterStateChibi.ATTACKING_ENEMY;
            }
        }
        else
        {
            if (isEnemyCharacter)
            {
                direction = Vector3.left;
            }
            else
            {
                direction = Vector3.right;
            }
            currentState = CharacterStateChibi.WALKING;

        }
    }

    private void AttackEnemyIfClose()
    {

        if (!isSpecialCharacter)
        {
            if (characterData.isMelee)
                handler.ChangeAnimation(AnimationEnum.SLASH, characterData.animAttackPerFrame);
            else
                handler.ChangeAnimation(AnimationEnum.THROW, characterData.animAttackPerFrame);
        }
        else
        {

            handler.ChangeAnimation(AnimationEnum.THROW, characterData.animAttackPerFrame);
        }

        if (!isSpecialCharacter)
        {
            if (handler.isInLastAnimFrame() && attackPerofrmed == false)
            {
                if (characterData.isMelee)
                {
                    ChibiWarriors chibi = enemy.GetComponent<ChibiWarriors>();
                    chibi.TakeDamage(characterData.Damage);
                    if (!chibi.isSpecialCharacter)
                    {
                        if (isEnemyCharacter)
                        {
                            //chibi.Push(Vector3.left);
                            chibi.Push(Vector3.left, true);
                        }
                        else
                        {
                            //chibi.Push(Vector3.right);
                            chibi.Push(Vector3.right, true);
                        }
                    }
                }
                else
                {
                    Vector3 offset = new Vector3(0.3f, -0.25f, 0);
                    if (isEnemyCharacter)
                    {
                        offset.x *= -1;
                    }
                    Weapon weapon = Instantiate(characterData.weapon, transform.position + offset, Quaternion.identity);
                    weapon.enemy = enemy;
                    weapon.isEnemyWeapon = isEnemyCharacter;
                    if (isEnemyCharacter)
                    {
                        weapon.ChangeSpriteDir(true);
                    }
                }

                attackPerofrmed = true;
                StartCoroutine(ResetAttackPerformed(handler.GetTotalFrames() * characterData.animAttackPerFrame));
            }
        }
        else
        {

            if (handler.isInLastAnimFrame() && attackPerofrmed == false)
            {
                LayerMask mask = LayerMask.GetMask("Enemy");
                if (isEnemyCharacter)
                {
                    mask = LayerMask.GetMask("Player");
                }

                float range = characterData.attackRange;
                Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range, mask);

                if (enemies.Length > 0)
                {
                    foreach (Collider2D obj in enemies)
                    {
                        ChibiWarriors chibi = obj.gameObject.GetComponent<ChibiWarriors>();
                        if (!chibi.isSpecialCharacter)
                        {
                            if (isEnemyCharacter)
                            {
                                if(chibi.transform.position.x > GlobalVars.PushDistanceXEnemy)
                                    chibi.PushInArc(Vector3.left, false);
                            }
                            else
                            {
                                if (chibi.transform.position.x < GlobalVars.PushDistanceXPlayer)
                                    chibi.PushInArc(Vector3.right, false);
                            }
                        }
                        else
                        {
                            if (isEnemyCharacter)
                            {
                                //if (chibi.transform.position.x < GlobalVars.PushDistanceXEnemy)
                                    //chibi.PushInArc(Vector3.left, true);
                                    
                            }
                            else
                            {
                                //if (chibi.transform.position.x > GlobalVars.PushDistanceXPlayer)
                                    //chibi.PushInArc(Vector3.right, true);
                            }
                        }
                        chibi.PlayHurtAnimation();
                        chibi.TakeDamage(characterData.Damage);
                    }
                }

                attackPerofrmed = true;
                StartCoroutine(ResetAttackPerformed(handler.GetTotalFrames() * characterData.animAttackPerFrame));
            }

        }

    }

    IEnumerator ResetAttackPerformed(float delay = 0.8f)
    {
        yield return new WaitForSeconds(delay);
        attackPerofrmed = false;
    }


    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (currentState == CharacterStateChibi.WALKING)
        {
            handler.ChangeAnimation(AnimationEnum.RUNNING, 0.05f);

            transform.Translate(direction * characterData.speed * Time.deltaTime);
        }

    }

    public void PlayHurtAnimation()
    {
        handler.PlayHurtAnimation();
    }


    public void TakeDamage(float damage = 10)
    {
        if (isDead)
            return;
        characterData.health -= damage;

        if(characterData.health<=0)
        {
            if(isEnemyCharacter)
            {
                Push(Vector3.right);
            }
            else
            {
                Push(Vector3.left);
            }

            gameObject.GetComponent<CircleCollider2D>().enabled = false;
        }    
        healthBar.gameObject.SetActive(true);
    }

    public void Push(Vector3 direction, bool isMinor = false)
    {
        float maxDistance = 3;
        if(isMinor)
        {
            maxDistance = 0.2f;
        }
        direction *= maxDistance;
        Vector3 finalDistance = transform.position + direction;

        Vector2 clamped = ClampVector2(new Vector2(finalDistance.x, finalDistance.y), GlobalVars.MinX, GlobalVars.MaxX, GlobalVars.MinY, GlobalVars.MaxY);

        finalDistance.x = clamped.x;
        finalDistance.y = clamped.y;

        transform.DOMove(finalDistance, 0.5f);
    }

    public void PushInArc(Vector3 direction, bool isSpecial, bool isBeast = false)
    {
        if (isDead)
            return;

        float randomAngle = Random.Range(-60f, 60f);
        if(isSpecial)
        {
            randomAngle = Random.Range(-10, 10f);
        }
        
        if(isBeast)
        {
            randomAngle = Random.Range(-3f, 3f);
        }

        Quaternion rotation = Quaternion.Euler(0, 0, randomAngle); // Rotate around the Z-axis for 2D
        Vector3 arcDirection = rotation * direction;

        float maxDistance = GlobalVars.SpecialCharacterPushDistance;

        if(isSpecial)
        {
            maxDistance *= 0.2f;
        }

        if(isBeast)
        {
            maxDistance*=0.45f;
        }

        arcDirection *= maxDistance; // Scale the direction by maxDistance

        // Calculate the final position
        Vector3 finalDistance = transform.position + arcDirection;

        Vector2 clamped = ClampVector2(new Vector2(finalDistance.x, finalDistance.y), GlobalVars.MinX, GlobalVars.MaxX, GlobalVars.MinY, GlobalVars.MaxY);

        finalDistance.x = clamped.x;
        finalDistance.y = clamped.y;

        // Move the object to the calculated position over time
        transform.DOMove(finalDistance, 0.5f);
    }


    public Vector2 ClampVector2(Vector2 vector, float minX, float maxX, float minY, float maxY)
    {
        // Clamp the x component
        float clampedX = Mathf.Clamp(vector.x, minX, maxX);

        // Clamp the y component
        float clampedY = Mathf.Clamp(vector.y, minY, maxY);

        // Return the clamped vector
        return new Vector2(clampedX, clampedY);
    }

    private void BackAwayFromEnemies()
    {
        // Determine direction to back away (opposite the enemy)
        if (enemy != null)
        {
            Vector3 backAwayDirection = (transform.position - enemy.transform.position).normalized;
            Vector3 targetPosition = transform.position + backAwayDirection * characterData.backAwayDistance;

            // Move the character to the target position smoothly
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, characterData.speed * Time.deltaTime);

            // Optionally play a backing away animation.
            handler.ChangeAnimation(AnimationEnum.RUNNING, 0.05f);
        }
    }

    //private void ExecuteRegularAttack()
    //{
    //    if (characterData.isMelee)
    //    {
    //        handler.ChangeAnimation(AnimationEnum.SLASH, characterData.animAttackPerFrame);
    //        InflictDamageToEnemy();
    //    }
    //    else
    //    {
    //        handler.ChangeAnimation(AnimationEnum.THROW, characterData.animAttackPerFrame);
    //        FireWeapon();
    //    }
    //}

    //private void ExecuteSpecialAttack()
    //{
    //    handler.ChangeAnimation(AnimationEnum.THROW, characterData.animAttackPerFrame);
    //    // Special attack logic
    //}


    private bool IsEnemyInScene()
    {
        LayerMask mask = isEnemyCharacter ? LayerMask.GetMask("Player") : LayerMask.GetMask("Enemy");
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 100, mask);

        List<ChibiWarriors> aliveEnemies = new List<ChibiWarriors>();
        float closestDistance = Mathf.Infinity;
        ChibiWarriors closestEnemy = null;

        foreach (Collider2D collider in enemies)
        {
            ChibiWarriors chibi = collider.GetComponent<ChibiWarriors>();
            if (!chibi.isDead)
            {
                aliveEnemies.Add(chibi);

                // Prioritize by distance or health here
                float distance = Vector2.Distance(transform.position, chibi.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = chibi;
                }
                else if (distance == closestDistance && chibi.characterData.health < closestEnemy.characterData.health)
                {
                    closestEnemy = chibi; // If same distance, prioritize weaker enemy
                }
            }
        }

        if (closestEnemy != null)
        {
            this.enemy = closestEnemy.gameObject; // Update reference to the closest enemy
        }

        return aliveEnemies.Count > 0;
    }

    private void MoveTowardsEnemy()
    {

        if(enemy.IsDestroyed())
        {
            currentState = CharacterStateChibi.WALKING;
            return;
        }
        // Move towards the closest enemy
        if (enemy != null)
        {
            if(Vector3.Distance(enemy.transform.position,transform.position) <= runningAttackRange && characterData.isMelee)
            {
                handler.ChangeAnimation(AnimationEnum.RUNN_SLASH, 0.05f);
            }
            else
            {
                handler.ChangeAnimation(AnimationEnum.RUNNING, 0.05f);
            }
            transform.position = Vector3.MoveTowards(transform.position, enemy.transform.position, characterData.speed * Time.deltaTime);
        } 
    }

    void UpdateHealth()
    {
        float healthPercent = characterData.health / maxHealth;
        // Scale health bar width to max 16 units
        float healthBarWidth = 16 * healthPercent;
        healthBar.transform.localScale = new Vector3(healthBarWidth, healthBar.transform.localScale.y, healthBar.transform.localScale.z);
    }

}
