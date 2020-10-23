using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyManager))]
[RequireComponent(typeof(UIManager))]
public class GameManager : MonoBehaviour
{
    public int healthLossIncrement;

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

    private EnemyManager enemyManager;
    private UIManager uiManager;

    public void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        uiManager = GetComponent<UIManager>();
    }

    public void Start()
    {
        Health = 100;
        enemyManager.SpawnEnemies();
    }

    public void KillEnemy(GameObject enemy)
    {
        enemyManager.KillEnemy(enemy);
        //do something with the UI
    }

    public void AttackPlayer()
    {
        Health -= healthLossIncrement;
    }
}
