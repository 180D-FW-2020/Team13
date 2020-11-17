﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public GameObject target;

    public GameObject enemy;
    public float dieDelay;

    private GameManager gameManager;
    private List<GameObject> enemies = new List<GameObject>();

    public void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public void Initialize(Dictionary<string, float> positions)
    {
        foreach (KeyValuePair<string, float> position in positions)
        {
            var spawnedEnemy = Instantiate(enemy, new Vector3(position.Value, 0), Quaternion.identity, transform);
            var spawnedEnemyController = spawnedEnemy.GetComponent<EnemyController>();
            spawnedEnemyController.SetTarget(target.transform);
            spawnedEnemyController.SetGameManager(gameManager);
            spawnedEnemy.name = position.Key;
            enemies.Add(spawnedEnemy);
        }
    }

    public void KillEnemy(string enemyId)
    {
        var enemy = transform.Find(enemyId);
        var enemyController = enemy.GetComponent<EnemyController>();
        StartCoroutine(enemyController.Die());
        enemies.Remove(enemy.gameObject);
    }
}