using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static string TRIGGER_MOVE = "TriggerMove";
    public static string TRIGGER_FALLDOWN = "TriggerFallingDown";

    public Animator animator;
    public float secondsIdleUntilWalk;

    public void Start()
    {
        StartCoroutine(WaitForWalk());
    }

    public IEnumerator WaitForWalk()
    {
        yield return new WaitForSeconds(secondsIdleUntilWalk);
        animator.SetTrigger(TRIGGER_MOVE);
    }
    public void Die()
    {
        animator.SetTrigger(TRIGGER_FALLDOWN);
    }
}
