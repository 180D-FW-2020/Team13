using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// PlayerController controls autoshooting of the weapon
public class PlayerController : MonoBehaviour
{
    public bool mainPlayer;
    public InputManager inputManager;
    public Transform playerCamera;
    public WeaponController weaponController;
    public float movementSpeed;

    private bool walking = false;

    public Transform GetPlayerCameraPosition()
    {
        return playerCamera;
    }

    public int GetCurrentAmmo()
    {
        return weaponController.GetCurrentAmmo();
    }

    public IEnumerator WalkToPad(Transform pad)
    {
        walking = true;
        transform.rotation = Quaternion.LookRotation(new Vector3(pad.position.x, transform.position.y, pad.position.z)); //look at pad
        while (transform.position != pad.position)
        {
            transform.position = Vector3.MoveTowards(transform.position, pad.position, Time.deltaTime * movementSpeed);
            yield return transform.position;
        }
        transform.SetParent(pad); //set parent
        transform.localRotation = Quaternion.identity; //face forward
        walking = false;
    }

    public bool IsWalking()
    {
        return walking;
    }
}
