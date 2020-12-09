using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public float referenceRadius;
    public Vector3 weaponLocation = new Vector3(0.25f, -0.4f, 0.9f);
    public Weapon leftWeapon;
    public Weapon rightWeapon;
    public Weapon upWeapon;
    public Weapon downWeapon;

    private Weapon currentWeapon;

    public void Start()
    {
        SwitchWeapon(leftWeapon);
    }

    public void Aim(Vector3 reticlePosition)
    {
        reticlePosition.z = referenceRadius;
        var pos = Camera.main.ScreenToWorldPoint(reticlePosition);
        Debug.Log(pos);
        currentWeapon.transform.LookAt(pos, Vector3.up);
    }

    public void SwitchWeapon(GestureType direction)
    {
        switch (direction)
        {
            case GestureType.Left:
                SwitchWeapon(leftWeapon);
                break;
            case GestureType.Right:
                SwitchWeapon(rightWeapon);
                break;
            case GestureType.Up:
                SwitchWeapon(upWeapon);
                break;
            case GestureType.Down:
                SwitchWeapon(downWeapon);
                break;
        }
    }

    public void SwitchWeapon(Weapon newWeapon)
    {
        if (currentWeapon)
            Destroy(currentWeapon.gameObject);
        var weapon = Instantiate(newWeapon.gameObject, weaponLocation, Quaternion.identity, transform);
        currentWeapon = weapon.GetComponent<Weapon>();
    }
}
