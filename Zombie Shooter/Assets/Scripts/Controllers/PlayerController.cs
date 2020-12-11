using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public InputManager inputManager;

    public bool autoShoot;
    public float shootInterval;

    public void StartGame()
    {
        if (autoShoot)
            StartCoroutine(AutoShoot());
    }

    private IEnumerator AutoShoot()
    {
        while (true)
        {
            inputManager.ShootIfStopped();
            yield return new WaitForSeconds(shootInterval);
        }
    }
}
