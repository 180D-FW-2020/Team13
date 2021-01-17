using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EnemyManager initialized enemy locations and maintains a map of enemy ids and positions.
// This data is updated as enemies are killed
public class EnemyManager : MonoBehaviour
{
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

    public void Initialize(Dictionary<string, EnemyState> positions, Transform levelOffset, List<Transform> playerPads)
    {
        transform.position = levelOffset.position;
        transform.rotation = levelOffset.rotation;
        foreach (KeyValuePair<string, EnemyState> pair in positions)
        {
            EnemyState state = pair.Value;
            string[] xz = state.initialPosition.Split(',');
            var spawnedEnemy = Instantiate(enemy);
            spawnedEnemy.transform.SetParent(transform);
            spawnedEnemy.transform.localPosition = new Vector3(float.Parse(xz[0]), 0, float.Parse(xz[1]));
            var spawnedEnemyController = spawnedEnemy.GetComponent<EnemyController>();
            spawnedEnemyController.SetProperties(playerPads[state.target], state.running == 1, state.health);
            spawnedEnemyController.SetGameManager(gameManager);
            spawnedEnemy.name = pair.Key;
            enemies.Add(spawnedEnemyController);
        }
    }

    public void KillEnemy(string enemyId)
    {
        var enemy = transform.Find(enemyId);
        var enemyController = enemy.GetComponent<EnemyController>();
        enemies.Remove(enemyController);
        StartCoroutine(enemyController.Die());
    }

    public int GetEnemyCount()
    {
        return enemies.Count;
    }
}
