using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : MonoBehaviour
{
    public float PlayerBaseHealth = 100;
    public float EnemyBaseHealth = 100;
    public bool GameStarted = false;
    public float DamagePerCharacter = 10;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void DamageBase(bool isPlayer)
    {
        if(isPlayer)
        {
            PlayerBaseHealth -= DamagePerCharacter;
        }
        else
        {
            EnemyBaseHealth -= DamagePerCharacter;
        }
    }
}
