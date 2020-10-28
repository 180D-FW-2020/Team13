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
    public UIManager uiManager;

    private Ray aimingRay;
    private RaycastHit hit;

    private Camera playerCamera;

    private int score;
    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            uiManager?.UpdateScore(score);
        }
    }

    public int hitScore;
    public int killScore;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        uiManager = GetComponent<UIManager>();
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
                Score += killScore;
            }
        }
    }
}
