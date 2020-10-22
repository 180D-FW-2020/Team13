using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static string TRIGGER_MOVE = "TriggerMove";
    public static string TRIGGER_FALLDOWN = "TriggerFallingDown";

    public Animator animator;
    public float secondsIdleUntilWalk;
    public float dieDelay;

    private Transform target;

    public void Start()
    {
        StartCoroutine(WaitForWalk());
    }

    public void Update()
    {
        Vector3 dir = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
    }

    public IEnumerator WaitForWalk()
    {
        yield return new WaitForSeconds(secondsIdleUntilWalk);
        animator.SetTrigger(TRIGGER_MOVE);
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }
    public IEnumerator Die()
    {
        animator.SetTrigger(TRIGGER_FALLDOWN);
        gameObject.tag = "DeadEnemy"; //prevent double shoot
        yield return new WaitForSeconds(dieDelay);
        Destroy(gameObject);
    }
}
