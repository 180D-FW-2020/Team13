using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyManager))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(InputManager))]
public class GameManager : MonoBehaviour
{
    public int healthLossIncrement;

    public int hitScore;
    public int killScore;

    private int health;
    public int Health
    {
        get { return health; }
        set
        {
            health = value;
            uiManager?.SetHealth(health);
        }
    }

    private int score;
    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            uiManager?.UpdateScore(score);
        }
    }


    private EnemyManager enemyManager;
    private UIManager uiManager;

    public void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        uiManager = GetComponent<UIManager>();
    }

    public void Start()
    {
        uiManager.ShowLoading();
        uiManager.startButton.onClick.AddListener(StartGame);
        uiManager.ShowStart();
    }

    public void Update()
    {

    }

    public void StartGame()
    {
        uiManager.StartGame();
        Health = 100;
        enemyManager.SpawnEnemies();
        Debug.Log("Game Started");
    }

    public void KillEnemy(GameObject enemy)
    {
        Score += killScore;
        enemyManager.KillEnemy(enemy);
    }

    public void AttackPlayer()
    {
        Health -= healthLossIncrement;
        if (Health <= 0) {
            Debug.Log("YOU DEAD AF");
        }
    }
}
