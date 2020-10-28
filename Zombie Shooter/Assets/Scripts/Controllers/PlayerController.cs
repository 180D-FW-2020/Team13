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

    private Ray aimingRay;
    private RaycastHit hit;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        Aim();

        if (Input.GetMouseButtonDown(0)) // click to shoot
        {
            Shoot();
        }
    }

    private void Aim()
    {
        aimingRay = playerCamera.ScreenPointToRay(inputManager.reticle.position);
    }

    private void Shoot()
    {
        if (Physics.Raycast(aimingRay, out hit))
        {
            GameObject hitObject = hit.collider.transform.gameObject;
            if (hitObject.tag == "Enemy")
            {
                Debug.Log("GOTTEM");
                Debug.Log(hitObject.name);
                gameManager.KillEnemy(hitObject);
            }
        }
    }
}
