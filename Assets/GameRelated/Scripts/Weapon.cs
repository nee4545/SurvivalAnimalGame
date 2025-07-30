using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public bool isEnemyWeapon;
    public float damage;
    public float speed;
    public Vector3 direction;
    public GameObject enemy;
    ChibiWarriors chibi;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, -2);
    }

    // Update is called once per frame
    void Update()
    {
       

    }

    private void FixedUpdate()
    {

        if(enemy!=null)
        {
            if(chibi == null)
            {
                chibi = enemy.GetComponent<ChibiWarriors>();
            }    

            if(chibi.isDead)
            {
                Destroy(this.gameObject);
            }
        }

        if (enemy == null || enemy.IsDestroyed())
        {
            Destroy(this.gameObject);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, enemy.transform.position, speed * Time.deltaTime);
        }
    }

    public void ChangeSpriteDir(bool flip)
    {
        GetComponent<SpriteRenderer>().flipX = flip;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        ChibiWarriors warrior = collision.gameObject.GetComponent<ChibiWarriors>();

        if (warrior != null)
        {
            if (isEnemyWeapon != warrior.isEnemyCharacter)
            {
                warrior.TakeDamage(damage);
                Destroy(this.gameObject);
            }
        }
    }
}
