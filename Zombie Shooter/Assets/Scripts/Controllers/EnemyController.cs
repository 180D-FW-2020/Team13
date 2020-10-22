using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public void Die()
    {
        //put some sort of animation here
        Destroy(gameObject);
    }
}
