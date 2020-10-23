using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public GameObject target;

    public GameObject enemy;
    public float spawnDelay;
    public float dieDelay;
    public float enemySpawnRangeMinX;
    public float enemySpawnRangeMaxX;
    public int maxEnemies;

    private GameManager gameManager;
    private List<GameObject> enemies = new List<GameObject>();

    public void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public void SpawnEnemies()
    {
        StartCoroutine(Spawn());
    }
    private IEnumerator Spawn()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            if (enemies.Count < maxEnemies)
            {
                Vector3 position = enemy.transform.position;
                position.x = Random.Range(enemySpawnRangeMinX, enemySpawnRangeMaxX);
                var spawnedEnemy = Instantiate(enemy, position, Quaternion.identity, transform);
                var spawnedEnemyController = spawnedEnemy.GetComponent<EnemyController>();
                spawnedEnemyController.SetTarget(target.transform);
                spawnedEnemyController.SetGameManager(gameManager);
                enemies.Add(spawnedEnemy);
            }
        }
    }

    public void KillEnemy(GameObject enemy)
    {
        var enemyController = enemy.GetComponent<EnemyController>();
        StartCoroutine(enemyController.Die());
        enemies.Remove(enemy);
    }
}
