using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AimInputType
{
    Mouse = 0,
    CV = 1,
    Finger = 2
}

public enum WeaponSelectInputType
{
    ArrowKeys = 0,
    IMU = 1
}

// InputManager is responsible for receiving both Computer Vision and IMU inputs, and 
// triggering the corresponding game events. 
public class InputManager : MonoBehaviour
{
    public float reticleStopVelocityThreshold;
    
    [Header("Controls/UI Options")]
    private AimInputType aimInputType;
    private WeaponSelectInputType weaponSelectInputType;
    public Text controlsText;
    public Text webcamText;
    public Toggle webcamToggle;

    [Header("Computer Vision Options")]
    public RawImage webcamPreview;
    public RawImage calibrationPreview;
    private bool enablePreview;
    public float[] greenLowerHSV = new float[3];
    public float[] greenUpperHSV = new float[3];

    [Header("Raspberry Pi Input")]
    public string ipAddress;
    public int port;

    private ComputerVisionInput cvInput;
    public FingerTracking ftInput;
    private RaspberryPiInput rpiInput;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public void Start()
    {
        SetDefaultControls();
        UpdateControlsText();
        Debug.Log(webcamToggle.isOn);
    }

    public void SetDefaultControls()
    {
        aimInputType = AimInputType.Mouse;
        weaponSelectInputType = WeaponSelectInputType.ArrowKeys;
        enablePreview = false;
    }

    public void UpdateAimingControls(int val) // mapped to dropdown menu in Unity Editor
    {
        aimInputType = (val == 0) ? AimInputType.Mouse : AimInputType.CV;
        UpdateControlsText();
        UpdatePreviewInteractable();
        UpdateWebcamText(false);
    }

    public void UpdateWeaponControls(int val) // mapped to dropdown menu in Unity Editor
    {
        weaponSelectInputType = (val == 0) ? WeaponSelectInputType.ArrowKeys : WeaponSelectInputType.IMU;
        UpdateControlsText();
    }

    public void UpdateControlsText()
    {
        string aimText = "Aiming: " + aimInputType;
        string weaponText = "Weapons: " + weaponSelectInputType;
        controlsText.text =  aimText + ", " + weaponText;
    }

    public void UpdateWebcamText(bool val)
    {
        if (aimInputType == AimInputType.CV) {
            webcamToggle.isOn = val;
            webcamText.text = "Webcam Preview: " + ((webcamToggle.isOn) ? "ON" : "OFF");
        } else {
            webcamToggle.isOn = false;
            webcamText.text = "";
        }
        enablePreview = webcamToggle.isOn;
    }

    public void UpdatePreviewInteractable()
    {
        bool isCV = (aimInputType == AimInputType.CV);
        if (!isCV) webcamToggle.isOn = false;
        webcamToggle.interactable = isCV;
    }

    public void InitInputs()
    {
        webcamPreview.enabled = enablePreview && (aimInputType != AimInputType.Mouse);
        if (weaponSelectInputType == WeaponSelectInputType.IMU)
            rpiInput = new RaspberryPiInput(ipAddress, port);
        if (aimInputType == AimInputType.CV)
            cvInput = new ComputerVisionInput(greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
        else if (aimInputType == AimInputType.Finger) {
            ftInput = new FingerTracking(enablePreview, webcamPreview, calibrationPreview);
        }
        Debug.Log("Aiming: " + aimInputType + ", Weapons: " + weaponSelectInputType);
    }

    public void UpdateCalibration()
    {
        ftInput.Update();
        if (Input.GetKeyDown("c"))
            ftInput.AnalyzeFrame();
    }

    public Vector2 GetAimInput()
    {
        if (aimInputType == AimInputType.CV)
            return cvInput.Update();
        else if (aimInputType == AimInputType.Finger)
            return ftInput.getPosition();
        else //mouse
            return new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));
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

    public void OnApplicationQuit()
    {
        rpiInput?.Close();
    }
}