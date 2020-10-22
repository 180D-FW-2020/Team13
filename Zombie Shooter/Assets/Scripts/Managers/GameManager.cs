using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;

    public GameObject enemy;
    public float spawnDelay;
    public float dieDelay;
    public float enemySpawnRangeMinX;
    public float enemySpawnRangeMaxX;
    public int maxEnemies;

    private List<GameObject> enemies = new List<GameObject>();

    public void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    public IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            if (enemies.Count < maxEnemies)
            {
                Vector3 position = enemy.transform.position;
                position.x = Random.Range(enemySpawnRangeMinX, enemySpawnRangeMaxX);
                var spawnedEnemy = Instantiate(enemy, position, Quaternion.identity, transform);
                spawnedEnemy.GetComponent<EnemyController>().SetTarget(player.transform);
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
