using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Idle = 0,
    Moving = 1,
    Attacking = 2,
    Dead = 3
}


// EnemyController controls an individual enemy, allowing it to walk towards the player and 
// attack it. Animations and health are both maintained.
public class EnemyController : MonoBehaviour
{
    public Animator animator;
    public float secondsIdleUntilWalk;
    public float attackDistance;
    public float attackInterval;
    public float dieDelay;

    private Transform target;
    private GameManager gameManager;

    private EnemyState state;

    public void Start()
    {
        state = EnemyState.Idle;
        //StartCoroutine(WaitForMove());
    }

    public void FixedUpdate()
    {
        Vector3 dir = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);

        //if (state == EnemyState.Moving && Vector2.Distance(target.position.xz(), transform.position.xz()) < attackDistance)
        //    StartCoroutine(Attack());
    }

    public IEnumerator WaitForMove()
    {
        yield return new WaitForSeconds(secondsIdleUntilWalk);
        animator.SetTrigger(Constants.TRIGGER_MOVE);
        state = EnemyState.Moving;
    }
    
    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public IEnumerator Attack()
    {
        state = EnemyState.Attacking;
        animator.SetTrigger(Constants.TRIGGER_ATTACK);
        var interval = new WaitForSeconds(attackInterval);
        while (state != EnemyState.Dead)
        {
            yield return interval;
            gameManager.AttackPlayer();
        }
    }

    public void ChangeHealth(float health)
    {
        gameManager.KillEnemy(gameObject);
    }

    public IEnumerator Die()
    {
        state = EnemyState.Dead;
        animator.SetTrigger(Constants.TRIGGER_FALLDOWN);
        gameObject.tag = "DeadEnemy"; //prevent double shoot
        yield return new WaitForSeconds(dieDelay);
        Destroy(gameObject);
    }
}
