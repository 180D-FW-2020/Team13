using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyStatus
{
    None = 0,
    Idle = 1,
    Moving = 2,
    Attacking = 3,
    Dead = 4
}


// EnemyController controls an individual enemy, allowing it to walk towards the player and 
// attack it. Animations and health are both maintained.
public class EnemyController : MonoBehaviour
{
    public Animator animator;
    public float attackDistance;
    public float attackInterval;
    public float dieDelay;

    private bool killCamReplay;

    private Transform target;
    private bool running;
    private int health;
    private GameManager gameManager;

    private EnemyStatus state;

    private void Start()
    {
        state = EnemyStatus.Idle;
    }

    public void StartGame()
    {
        animator.SetTrigger(running ? Constants.TRIGGER_RUN : Constants.TRIGGER_WALK);
        state = EnemyStatus.Moving;
    }

    public void FixedUpdate()
    {
        Vector3 dir = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
        if (state == EnemyStatus.Moving && Vector2.Distance(target.position.xz(), transform.position.xz()) < attackDistance)
            StartCoroutine(Attack());
    }

    public void SetProperties(Transform targetTransform, bool running, int health, bool killCamReplay, float? killTime)
    {
        this.killCamReplay = killCamReplay;
        target = targetTransform;
        this.running = running;
        this.health = health;

        Vector3 dir = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);

        if (killCamReplay)
        {
            transform.position += transform.forward * (running ? Constants.RUN_SPEED : Constants.WALK_SPEED) * (float)killTime;
        }
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public IEnumerator Attack()
    {
        state = EnemyStatus.Attacking;
        animator.SetTrigger(Constants.TRIGGER_ATTACK);
        var interval = new WaitForSeconds(attackInterval);
        while (state != EnemyStatus.Dead)
        {
            yield return interval;
            gameManager.AttackPlayer();
        }
    }

    public void RegisterHit(int damage, bool mainPlayer)
    {
        if (state != EnemyStatus.Dead)
        {
            health -= damage;
            //Debug.Log("Shot, " + health);
            bool killed = health <= 0;
            if (mainPlayer)
                gameManager.RegisterShot(gameObject, damage, killed);
        }
    }

    public IEnumerator Die()
    {
        if (state == EnemyStatus.Dead)
            yield break;
        state = EnemyStatus.Dead;
        animator.SetTrigger(Constants.TRIGGER_FALLDOWN);
        yield return new WaitForSeconds(dieDelay);
        Destroy(gameObject);
    }
}
