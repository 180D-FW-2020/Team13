using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EnemyManager initialized enemy locations and maintains a map of enemy ids and positions.
// This data is updated as enemies are killed
public class EnemyManager : MonoBehaviour
{
    public GameObject target;

    public GameObject enemy;
    public float dieDelay;

    private GameManager gameManager;
    private List<EnemyController> enemies = new List<EnemyController>();

    public void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }
    
    public void StartGame()
    {
        foreach (EnemyController enemyController in enemies)
        {
            enemyController.StartGame();
        }
    }

    public void Initialize(Dictionary<string, string> positions)
    {
        if (enemies.Count == 0) //check if already initialized
        {
            foreach (KeyValuePair<string, string> position in positions)
            {
                string[] xz = position.Value.Split(',');
                var spawnedEnemy = Instantiate(enemy, new Vector3(float.Parse(xz[0]), 0, float.Parse(xz[1])), Quaternion.identity, transform);
                var spawnedEnemyController = spawnedEnemy.GetComponent<EnemyController>();
                spawnedEnemyController.SetTarget(target.transform);
                spawnedEnemyController.SetGameManager(gameManager);
                spawnedEnemy.name = position.Key;
                enemies.Add(spawnedEnemyController);
            }
        }
    }

    public void KillEnemy(string enemyId)
    {
        var enemy = transform.Find(enemyId);
        var enemyController = enemy.GetComponent<EnemyController>();
        enemies.Remove(enemyController);
        StartCoroutine(enemyController.Die());
    }
}
