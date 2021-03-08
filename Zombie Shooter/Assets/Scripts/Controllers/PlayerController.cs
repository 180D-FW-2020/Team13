using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class WeaponData
{
    public string name;
    public Weapon weapon;
    public float aimSpeed;
}

// PlayerController controls autoshooting of the weapon
public class PlayerController : MonoBehaviour
{
    public float movementSpeed;
    public bool killed = false;

    [Header("Camera")]
    public Transform playerCamera;
    public float cameraSensitivity;
    public float aimingSensitivity;
    public float xLimit;
    public float yLimit;

    [Header("Weapons")]
    public WeaponData leftWeapon;
    public WeaponData rightWeapon;
    public WeaponData upWeapon;
    public WeaponData downWeapon;
    public Vector3 weaponOffset;
    public Vector3 aimOffset;

    private WeaponData currentWeapon;
    private GestureType currentWeaponType;

    private bool mainPlayer;
    private bool walking;
    private bool shooting;
    private bool aiming;

    private Vector3 previousRotation;
    private float velocity = 0;
    private Vector2 rotation = Vector2.zero;
    private bool shootingEnabled;
    private InputManager inputManager;

    private GestureType gesture;

    private int score;

    public void Initialize(bool main, InputManager manager)
    {
        inputManager = manager;
        mainPlayer = main;
        currentWeapon = new WeaponData();
        SwitchWeapon(GestureType.L);
        StartCoroutine(AimAndShoot());
    }

    public void UpdateInput()
    {
        if (shootingEnabled)
        {
            //switch weapon and shoot
            gesture = inputManager.GetGesture();
            if (gesture == GestureType.X && shootingEnabled)
                shooting = true;
            else if (gesture == GestureType.O)
                shooting = false;
            else
                SwitchWeapon(gesture);

            //aim 
            rotation += inputManager.GetAimInput() * (aiming ? aimingSensitivity : cameraSensitivity);
            rotation.x = Mathf.Clamp(rotation.x, -xLimit / 2, xLimit / 2);
            rotation.y = Mathf.Clamp(rotation.y, -yLimit / 2, yLimit / 2);
            transform.eulerAngles = rotation;
            velocity = (transform.eulerAngles - previousRotation).sqrMagnitude / Time.deltaTime;
            previousRotation = transform.eulerAngles;

        }
    }

    public void SetKilled()
    {
        killed = true;
        EnableShooting(false);
    }

    public void ResetRotation()
    {
        rotation = Vector2.zero;
    }

    public void EnableShooting(bool enabled)
    {
        if (killed) enabled = false;
        shootingEnabled = enabled;
        shooting = enabled ? shooting : false;
        currentWeapon.weapon.showCrosshair = enabled;
    }
    
    public int GetCurrentAmmo()
    {
        return currentWeapon.weapon.GetCurrentAmmo();
    }

    public void ReloadWeapon()
    {
        currentWeapon.weapon.Reload();
        // Debug.Log("Reloading Weapon...");
    }

    public void SwitchWeapon(GestureType type)
    {
        if (currentWeaponType == type || type == GestureType.None)
            return;

        switch (type)
        {
            case GestureType.L:
                SwitchWeapon(leftWeapon);
                break;
            case GestureType.R:
                SwitchWeapon(rightWeapon);
                break;
            case GestureType.U:
                SwitchWeapon(upWeapon);
                break;
            case GestureType.D:
                SwitchWeapon(downWeapon);
                break;
        }
        currentWeaponType = type;
        currentWeapon.weapon.playerWeapon = mainPlayer;
        currentWeapon.weapon.showCrosshair = shootingEnabled;
    }

    public IEnumerator AimAndShoot()
    {
        while (true)
        {
            if (shooting && !aiming)
            {
                while (currentWeapon.weapon.transform.localPosition != aimOffset)
                {
                    currentWeapon.weapon.transform.localPosition = Vector3.MoveTowards(currentWeapon.weapon.transform.localPosition, aimOffset, Time.deltaTime * currentWeapon.aimSpeed);
                    yield return currentWeapon.weapon.transform.localPosition;
                }
                aiming = true;
            }

            else if (shooting && aiming)
            {
                Shoot();
                yield return new WaitForSeconds(1 / currentWeapon.weapon.rateOfFire);
            }

            else if (!shooting && aiming)
            {
                while (currentWeapon.weapon.transform.localPosition != weaponOffset)
                {
                    currentWeapon.weapon.transform.localPosition = Vector3.MoveTowards(currentWeapon.weapon.transform.localPosition, weaponOffset, Time.deltaTime * currentWeapon.aimSpeed);
                    yield return currentWeapon.weapon.transform.localPosition;
                }
                aiming = false;
            }

            else
            {
                yield return null;
            }
        }
    }

    public void UpdateScore(int newScore)
    {
        score = newScore;
    }

    public int GetScore()
    {
        return score;
    }

    public void Shoot()
    {
        currentWeapon.weapon.RemoteFire();
    }

    private void SwitchWeapon(WeaponData weaponData)
    {
        shooting = false;
        if (currentWeapon.weapon)
            Destroy(currentWeapon.weapon.gameObject);
        var newGun = Instantiate(weaponData.weapon.gameObject, weaponOffset, Quaternion.identity);
        newGun.transform.SetParent(transform, false);
        currentWeapon.aimSpeed = weaponData.aimSpeed;
        currentWeapon.weapon = newGun.GetComponent<Weapon>();
    }

    public IEnumerator WalkToPad(Transform pad, bool toLevel)
    {
        walking = true;

        if (true)
        {
            Vector3 leaveTruck = new Vector3();
            if (toLevel)
                leaveTruck = new Vector3(transform.position.x - 15.0f, transform.position.y, transform.position.z);
            else
                leaveTruck = new Vector3(pad.position.x - 15.0f, pad.position.y, pad.position.z);
            transform.rotation = Quaternion.LookRotation(new Vector3(leaveTruck.x, leaveTruck.y, leaveTruck.z)); // look at waypoint
            while (transform.position != leaveTruck) // move towards waypoint
            {
                transform.position = Vector3.MoveTowards(transform.position, leaveTruck, Time.deltaTime * movementSpeed);
                yield return transform.position;
            }
        }

        // go to pad
        transform.rotation = Quaternion.LookRotation(new Vector3(pad.position.x, transform.position.y, pad.position.z)); //look at pad
        while (transform.position != pad.position)
        {
            transform.position = Vector3.MoveTowards(transform.position, pad.position, Time.deltaTime * movementSpeed);
            yield return transform.position;
        }
        transform.SetParent(pad); //set parent
        transform.localRotation = Quaternion.identity; //face forward

        // // return to truck
        // if (!toLevel)
        // {
        //     Vector3 leaveTruck = new Vector3(pad.position.x - 15.0f, pad.position.y, pad.position.z);
        //     transform.rotation = Quaternion.LookRotation(new Vector3(leaveTruck.x, leaveTruck.y, leaveTruck.z)); // look at waypoint
        //     while (transform.position != leaveTruck) // move towards waypoint
        //     {
        //         transform.position = Vector3.MoveTowards(transform.position, leaveTruck, Time.deltaTime * movementSpeed);
        //         yield return transform.position;
        //     }
        // }

        walking = false;
    }

    public bool IsWalking()
    {
        return walking;
    }

    public void SetShooting(bool isShooting)
    {
        shooting = isShooting;
    }

    public int GetShooting()
    {
        if (!shooting)
            return 0;
        return (int)currentWeaponType;
    }
}
