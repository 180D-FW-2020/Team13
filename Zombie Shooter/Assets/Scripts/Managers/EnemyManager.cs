using OpenCvSharp;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// EnemyManager initialized enemy locations and maintains a map of enemy ids and positions.
// This data is updated as enemies are killed
public class EnemyManager : MonoBehaviour
{
    public GameObject enemy;
    public float dieDelay;

    private GameManager gameManager;
    private Dictionary<string, EnemyController> enemies = new Dictionary<string, EnemyController>();

    public void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }
    
    public void StartGame()
    {
        foreach (string enemyId in enemies.Keys)
        {
            enemies[enemyId].StartGame();
        }
    }

    public void Initialize(Dictionary<string, EnemyState> positions, Transform levelOffset, List<Transform> playerPads, bool killCamReplay = false, Dictionary<long, string> killTimes = null, long initialTime = 0)
    {
        transform.position = levelOffset.position;
        transform.rotation = levelOffset.rotation;
        foreach (KeyValuePair<string, EnemyState> pair in positions)
        {
            EnemyState state = pair.Value;
            var spawnedEnemy = Instantiate(enemy);
            spawnedEnemy.transform.SetParent(transform);
            spawnedEnemy.transform.localPosition = new Vector3(state.position[0], 0, state.position[1]);
            var spawnedEnemyController = spawnedEnemy.GetComponent<EnemyController>();
            long killTime = 0L;
            if (killCamReplay)
            {
                killTime = killTimes.First(x => x.Value.Contains($"{pair.Key}:")).Key;
            }
            spawnedEnemyController.SetProperties(playerPads[state.target], state.running == 1, state.health, killCamReplay, (killTime - Mathf.Max(initialTime, positions[pair.Key].attacking)) / 1000f);
            spawnedEnemyController.SetGameManager(gameManager);
            spawnedEnemy.name = pair.Key;
            enemies.Add(pair.Key, spawnedEnemyController);
        }
    }

    public void KillEnemy(string enemyId)
    {
        if (enemies.ContainsKey(enemyId)) {
            StartCoroutine(enemies[enemyId].Die());
            enemies.Remove(enemyId);
        }
    }

    public void KillAllEnemies()
    {
        foreach (string enemyId in enemies.Keys)
        {
            Debug.Log($"Enemy {enemyId} still alive");
            KillEnemy(enemyId);
        }
    }

    public void ShootEnemy(string enemyId, int damage)
    {
        if (enemies.ContainsKey(enemyId))
            enemies[enemyId].RegisterHit(damage);
    }

    public int GetEnemyCount()
    {
        return enemies.Count;
    }
}
