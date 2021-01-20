using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class WeaponData
{
    public Weapon weapon;
    public float aimSpeed;
}

// PlayerController controls autoshooting of the weapon
public class PlayerController : MonoBehaviour
{
    public bool mainPlayer;
    public float movementSpeed;

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
    private GestureType currentWeaponType = GestureType.None;

    private bool walking;
    private bool aiming;

    private Vector3 previousRotation;
    private float velocity = 0;
    private Vector2 rotation = Vector2.zero;
    private bool shootingEnabled;
    private InputManager inputManager;

    private Rigidbody rb;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentWeapon = new WeaponData();
        SwitchWeapon(GestureType.L);
    }

    public void SetInputManager(InputManager manager)
    {
        inputManager = manager;
    }

    public void UpdateInput()
    {
        if (shootingEnabled)
        {
            //switch weapon
            SwitchWeapon(inputManager.GetGesture());

            //aim 
            rotation += inputManager.GetAimInput() * (aiming ? aimingSensitivity : cameraSensitivity);
            rotation.x = Mathf.Clamp(rotation.x, -xLimit / 2, xLimit / 2);
            rotation.y = Mathf.Clamp(rotation.y, -yLimit / 2, yLimit / 2);
            transform.eulerAngles = rotation;
            velocity = (transform.eulerAngles - previousRotation).sqrMagnitude / Time.deltaTime;
            previousRotation = transform.eulerAngles;

            //shoot
            if (Input.GetKeyDown(KeyCode.A))
                StartCoroutine(AimAndShoot());
        }
    }

    public void ResetRotation()
    {
        rotation = Vector2.zero;
    }

    public void EnableShooting(bool enabled)
    {
        shootingEnabled = enabled;
        currentWeapon.weapon.showCrosshair = enabled;
    }
    
    public int GetCurrentAmmo()
    {
        return currentWeapon.weapon.GetCurrentAmmo();
    }

    public void SwitchWeapon(GestureType type)
    {
        if (currentWeaponType == type)
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
        currentWeapon.weapon.showCrosshair = shootingEnabled;
    }

    public IEnumerator AimAndShoot()
    {
        if (!aiming) {
            while (currentWeapon.weapon.transform.localPosition != aimOffset)
            {
                currentWeapon.weapon.transform.localPosition = Vector3.MoveTowards(currentWeapon.weapon.transform.localPosition, aimOffset, Time.deltaTime * currentWeapon.aimSpeed);
                yield return new WaitForEndOfFrame();
            }
            aiming = true;
        }

        while (Input.GetKey(KeyCode.A))
        {
            Shoot();
            yield return new WaitForSeconds(1/currentWeapon.weapon.rateOfFire);
        }

        if (aiming)
        {
            while (currentWeapon.weapon.transform.localPosition != weaponOffset)
            {
                currentWeapon.weapon.transform.localPosition = Vector3.MoveTowards(currentWeapon.weapon.transform.localPosition, weaponOffset, Time.deltaTime * currentWeapon.aimSpeed);
                yield return new WaitForEndOfFrame();
            }
            aiming = false;
        }
    }


    public void Shoot()
    {
        if (shootingEnabled && aiming)
            currentWeapon.weapon.RemoteFire();
    }

    private void SwitchWeapon(WeaponData weaponData)
    {
        if (currentWeapon.weapon)
            Destroy(currentWeapon.weapon.gameObject);
        var newGun = Instantiate(weaponData.weapon.gameObject, weaponOffset, Quaternion.identity);
        newGun.transform.SetParent(transform, false);
        currentWeapon.aimSpeed = weaponData.aimSpeed;
        currentWeapon.weapon = newGun.GetComponent<Weapon>();
    }

    public IEnumerator WalkToPad(Transform pad)
    {
        walking = true;
        transform.rotation = Quaternion.LookRotation(new Vector3(pad.position.x, transform.position.y, pad.position.z)); //look at pad
        while (transform.position != pad.position)
        {
            rb.MovePosition(transform.position + (pad.position - transform.position) * Time.deltaTime * movementSpeed);
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
