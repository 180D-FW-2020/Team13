using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WeaponController swaps the current weapon based on a specified gesture
public class WeaponController : MonoBehaviour
{
    public bool playerWeapon;
    public float referenceRadius;
    public Weapon leftWeapon;
    public Weapon rightWeapon;
    public Weapon upWeapon;
    public Weapon downWeapon;

    private Weapon currentWeapon;
    private GestureType currentWeaponType;

    public void Start()
    {
        SwitchWeapon(GestureType.L);
    }

    public void Aim(Vector3 reticlePosition)
    {
        reticlePosition.z = referenceRadius;
        var pos = Camera.main.ScreenToWorldPoint(reticlePosition);
        currentWeapon.transform.LookAt(pos, Vector3.up);
    }

    public void Shoot()
    {
        currentWeapon?.RemoteFire();
    }

    public int GetCurrentAmmo()
    {
        return currentWeapon.GetCurrentAmmo();
    }


    public void SwitchWeapon(GestureType direction)
    {
        if (direction == GestureType.None || direction == currentWeaponType)
            return;
        Debug.Log($"Switching weapon: {direction}");
        currentWeaponType = direction;
        switch (direction)
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
    }

    public void SwitchWeapon(Weapon newWeapon)
    {
        if (currentWeapon)
            Destroy(currentWeapon.gameObject);
        var weapon = Instantiate(newWeapon.gameObject, transform.position, transform.rotation);
        weapon.transform.SetParent(transform);
        currentWeapon = weapon.GetComponent<Weapon>();
    }

    public GestureType GetWeaponType()
    {
        return currentWeaponType;
    }
}
