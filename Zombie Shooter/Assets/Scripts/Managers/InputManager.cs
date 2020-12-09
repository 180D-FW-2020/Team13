using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public InputType inputType;
    public float reticleStopVelocityThreshold;
    internal Transform playerReticle;
    public WeaponController weapon;

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
        rpiInput = new RaspberryPiInput(ipAddress, port);
        if (inputType == InputType.CV)
            cvInput = new ComputerVisionInput(WebCamTexture.devices[0], greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
    }

    public void UpdateInput()
    {
        //weapon switching
        GestureType gesture = rpiInput.GetGesture();
        if (Input.GetKeyDown("left") || gesture == GestureType.L)
            weapon.SwitchWeapon(GestureType.L);
        else if (Input.GetKeyDown("right") || gesture == GestureType.R)
            weapon.SwitchWeapon(GestureType.R);
        else if (Input.GetKeyDown("up") || gesture == GestureType.U)
            weapon.SwitchWeapon(GestureType.U);
        else if (Input.GetKeyDown("down") || gesture == GestureType.D)
            weapon.SwitchWeapon(GestureType.D);

        previousPosition = playerReticle.position;
        playerReticle.position = GetReticleInput();
        weapon.Aim(playerReticle.position);
        velocity = (playerReticle.position - previousPosition).magnitude / Time.fixedDeltaTime;
    }

    public Vector3 GetReticleInput()
    {
        if (inputType == InputType.Mouse)
            return new Vector3(Input.mousePosition.x, Input.mousePosition.y);
        else
            return cvInput.Update();
    }

    public bool IsReticleStopped()
    {
        return velocity < reticleStopVelocityThreshold;
    }

    public void OnApplicationQuit()
    {
        rpiInput?.Close();
    }
}