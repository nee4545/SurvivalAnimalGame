using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Spawnner
{
    public int NumberOfEnemiesPerSpawn;
    public List<GameObject> Characters;
    int currentIndex = 0;

    public void Spawn(Vector3 position)
    {
        for(int i=0; i<NumberOfEnemiesPerSpawn; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle *0.65f;

            // Calculate the enemy's spawn position
            Vector3 spawnPosition = position + new Vector3(randomDirection.x, randomDirection.y, 0);

            // Instantiate the enemy prefab at the calculated position
            GameObject.Instantiate(Characters[currentIndex], spawnPosition, Quaternion.identity);

            currentIndex++;

            if(currentIndex>= Characters.Count)
            {
                currentIndex = 0;
            }
        }
    }
}


public class SpawnHandler : MonoBehaviour
{
    public Transform[] PlayerSpawnPoints;
    public Transform[] EnemySpawnPoints;

    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;
    public GameObject RangePlayerPrefab;
    public GameObject RangeEnemyPrefab;

    public Transform PlayerSpawnnerSpawnPoint;
    public Transform EnemySpawnnerSpawnPoint;

    float currentlyrolledRandom = -1;


    public Spawnner PlayerSpawnner;
    public Spawnner EnemySpawnner;

    

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Spawn(true);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Spawn(false);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Spawn(false,true);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Spawn(true,true);
        }

        if(Input.GetKeyDown(KeyCode.Q))
        {
            SpawnFromSpawnner(true);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            SpawnFromSpawnner(false);
        }

    }

    public void SpawnFromSpawnner(bool isPlayer)
    {
        if(isPlayer)
        {
            PlayerSpawnner.Spawn(PlayerSpawnnerSpawnPoint.position);
        }
        else
        {
            EnemySpawnner.Spawn(EnemySpawnnerSpawnPoint.position);
        }
    }

    void Spawn(bool enemy, bool isRange = false)
    {
        if(!isRange)
        {
            if (enemy)
            {

                float additionalYOffset = 0.25f;

                int randomNum = Random.Range(0, PlayerSpawnPoints.Length);
                if(currentlyrolledRandom!= randomNum)
                {
                    currentlyrolledRandom = randomNum;

                    int randomMultipler = Random.Range(0, 2);

                    if(randomMultipler>0)
                    {
                        additionalYOffset *= -1;
                    }


                }

                Vector3 position = PlayerSpawnPoints[randomNum].transform.position;
                position.y += additionalYOffset;

                GameObject.Instantiate(EnemyPrefab, position, Quaternion.identity);
            }
            else
            {
                float additionalYOffset = 0.25f;


                int randomNum = Random.Range(0, EnemySpawnPoints.Length);

                if (currentlyrolledRandom != randomNum)
                {
                    currentlyrolledRandom = randomNum;

                    int randomMultipler = Random.Range(0, 2);

                    if (randomMultipler > 0)
                    {
                        additionalYOffset *= -1;
                    }


                }

                Vector3 position = EnemySpawnPoints[randomNum].transform.position;
                position.y += additionalYOffset;

                GameObject.Instantiate(PlayerPrefab, position, Quaternion.identity);
            }
        }
        else
        {
            if (enemy)
            {
                float additionalYOffset = 0.25f;

                int randomNum = Random.Range(0, PlayerSpawnPoints.Length);

                if (currentlyrolledRandom != randomNum)
                {
                    currentlyrolledRandom = randomNum;

                    int randomMultipler = Random.Range(0, 2);

                    if (randomMultipler > 0)
                    {
                        additionalYOffset *= -1;
                    }


                }

                Vector3 position = PlayerSpawnPoints[randomNum].transform.position;
                position.y += additionalYOffset;

                GameObject.Instantiate(RangeEnemyPrefab, position, Quaternion.identity);
            }
            else
            {
                float additionalYOffset = 0.25f;

                int randomNum = Random.Range(0, EnemySpawnPoints.Length);

                if (currentlyrolledRandom != randomNum)
                {
                    currentlyrolledRandom = randomNum;

                    int randomMultipler = Random.Range(0, 2);

                    if (randomMultipler > 0)
                    {
                        additionalYOffset *= -1;
                    }


                }


                Vector3 position = EnemySpawnPoints[randomNum].transform.position;
                position.y += additionalYOffset;

                GameObject.Instantiate(RangePlayerPrefab, position, Quaternion.identity);
            }
        }
       
    }
}
