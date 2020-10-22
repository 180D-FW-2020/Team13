﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public MouseControlType mouseControlType;

    // reticle
    public Transform reticle;

    //camera
    public Camera playerCamera;
    public float cameraRotateSensitivity;

    private Ray aimingRay;
    private RaycastHit hit;

    void Start()
    {
        
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
        if (mouseControlType == MouseControlType.Reticle)
        {
            reticle.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z);
        }
        else if (mouseControlType == MouseControlType.Camera)
        {
            playerCamera.transform.rotation *= Quaternion.Euler(-Input.GetAxis("Mouse Y") * cameraRotateSensitivity, Input.GetAxis("Mouse X") * cameraRotateSensitivity, 0);
        }
        aimingRay = playerCamera.ScreenPointToRay(reticle.position);
    }

    private void Shoot()
    {
        if (Physics.Raycast(aimingRay, out hit))
        {
            if (hit.transform.tag == "Enemy")
            {
                Debug.Log("GOTTEM");
                EnemyController enemy = hit.transform.gameObject.GetComponent<EnemyController>();
                enemy.Die();
            }
        }
    }
}
