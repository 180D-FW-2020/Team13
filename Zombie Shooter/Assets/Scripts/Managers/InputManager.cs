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

    [Header("Serial Input from RPi")]
    public string serialPortName;

    private ComputerVisionInput cvInput;
    private SerialInput serialInput;

    private Vector3 previousPosition;
    internal float velocity = 0;

    public void Start()
    {
        webcamPreview.enabled = enablePreview;
        serialInput = new SerialInput(serialPortName, GestureReceived);
        if (inputType == InputType.CV)
            cvInput = new ComputerVisionInput(WebCamTexture.devices[0], greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
    }

    public void UpdateInput()
    {
        //manual weapon switching
        if (Input.GetKeyDown("left"))
            weapon.SwitchWeapon(GestureType.Left);
        else if (Input.GetKeyDown("right"))
            weapon.SwitchWeapon(GestureType.Right);
        else if (Input.GetKeyDown("up"))
            weapon.SwitchWeapon(GestureType.Up);
        else if (Input.GetKeyDown("down"))
            weapon.SwitchWeapon(GestureType.Down);

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

    public void GestureReceived(GestureType gesture)
    {
        Debug.Log("Shit works");
    }

    public bool IsReticleStopped()
    {
        return velocity < reticleStopVelocityThreshold;
    }

    public void OnApplicationQuit()
    {
        serialInput.Close();
    }
}