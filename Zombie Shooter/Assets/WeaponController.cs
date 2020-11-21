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

    public void Update()
    {
        if (Input.GetKeyDown("left"))
            SwitchWeapon(leftWeapon);
        else if (Input.GetKeyDown("right"))
            SwitchWeapon(rightWeapon);
        else if (Input.GetKeyDown("up"))
            SwitchWeapon(upWeapon);
        else if (Input.GetKeyDown("down"))
            SwitchWeapon(downWeapon);
    }

    public void Aim(Vector3 reticlePosition)
    {
        reticlePosition.z = referenceRadius;
        var pos = Camera.main.ScreenToWorldPoint(reticlePosition);
        Debug.Log(pos);
        currentWeapon.transform.LookAt(pos, Vector3.up);
    }

    public void SwitchWeapon(Weapon newWeapon)
    {
        if (currentWeapon)
            Destroy(currentWeapon.gameObject);
        var weapon = Instantiate(newWeapon.gameObject, weaponLocation, Quaternion.identity, transform);
        currentWeapon = weapon.GetComponent<Weapon>();
    }
}
