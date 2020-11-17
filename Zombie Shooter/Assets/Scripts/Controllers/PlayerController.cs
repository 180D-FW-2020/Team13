using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public GameManager gameManager;
    public InputManager inputManager;

    public bool autoShoot;
    public float autoShootInterval;

    private Ray aimingRay;
    private RaycastHit hit;

    private Camera playerCamera;
    private bool gameStarted = false;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        if (autoShoot)
            StartCoroutine(AutoShoot());
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    private IEnumerator AutoShoot()
    {
        while (true)
        {
            if (inputManager.IsReticleStopped()) Shoot();
            yield return new WaitForSeconds(autoShootInterval);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // click to shoot
            Shoot();
    }

    private void Shoot()
    {
        if (gameManager.GameStarted)
        {
            aimingRay = playerCamera.ScreenPointToRay(inputManager.playerReticle.position);
            if (Physics.Raycast(aimingRay, out hit))
            {
                GameObject hitObject = hit.collider.transform.gameObject;
                if (hitObject.tag == "Enemy")
                {
                    Debug.Log("GOTTEM");
                    gameManager.KillEnemy(hitObject);
                }
            }
        }
    }
}
