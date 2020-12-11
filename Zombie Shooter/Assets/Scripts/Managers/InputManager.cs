using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum AimInputType
{
    Mouse = 1,
    CV = 2
}

public enum WeaponSelectInputType
{
    IMU = 1,
    ArrowKeys = 2,
}

public class InputManager : MonoBehaviour
{
    public AimInputType aimInputType;
    public WeaponSelectInputType weaponSelectInputType;
    public float reticleStopVelocityThreshold;
    internal Transform playerReticle;
    public WeaponController weaponController;

    [Header("Computer Vision Tracking")]
    public RawImage webcamPreview;
    public bool enablePreview;
    public float[] greenLowerHSV = new float[3];
    public float[] greenUpperHSV = new float[3];

    [Header("Raspberry Pi Input")]
    public string ipAddress;
    public int port;

    private ComputerVisionInput cvInput;
    private RaspberryPiInput rpiInput;

    private Vector3 previousPosition;
    internal float velocity = 0;

    public void Start()
    {
        webcamPreview.enabled = enablePreview;
        if (weaponSelectInputType == WeaponSelectInputType.IMU)
            rpiInput = new RaspberryPiInput(ipAddress, port);
        if (aimInputType == AimInputType.CV)
            cvInput = new ComputerVisionInput(WebCamTexture.devices[0], greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
    }

    public void UpdateInput()
    {
        //weapon switching
        weaponController.SwitchWeapon(GetGesture());

        //aim reticle
        previousPosition = playerReticle.position;
        playerReticle.position = GetReticleInput();
        weaponController.Aim(playerReticle.position);
        velocity = (playerReticle.position - previousPosition).magnitude / Time.fixedDeltaTime;
    }

    public Vector3 GetReticleInput()
    {
        if (aimInputType == AimInputType.Mouse)
            return new Vector3(Input.mousePosition.x, Input.mousePosition.y);
        else
            return cvInput.Update();
    }

    public GestureType GetGesture()
    {
        GestureType gestureType = GestureType.None;
        if (weaponSelectInputType == WeaponSelectInputType.ArrowKeys)
        {
            if (Input.GetKeyDown("left"))
                gestureType = GestureType.L;
            else if (Input.GetKeyDown("right"))
                gestureType = GestureType.R;
            else if (Input.GetKeyDown("up"))
                gestureType = GestureType.U;
            else if (Input.GetKeyDown("down"))
                gestureType = GestureType.D;
        }
        else
        {
            gestureType = rpiInput.GetGesture();
        }
        return gestureType;
    }

    public void ShootIfStopped()
    {
        if (velocity < reticleStopVelocityThreshold) //reticle stopped 
            weaponController.Shoot();
    }

    public void OnApplicationQuit()
    {
        rpiInput?.Close();
    }
}