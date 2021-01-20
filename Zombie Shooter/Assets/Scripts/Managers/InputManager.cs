using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AimInputType
{
    Mouse = 1,
    CV = 2,
    Finger = 3
}

public enum WeaponSelectInputType
{
    IMU = 1,
    ArrowKeys = 2,
}

// InputManager is responsible for receiving both Computer Vision and IMU inputs, and 
// triggering the corresponding game events. 
public class InputManager : MonoBehaviour
{
    public AimInputType aimInputType;
    public WeaponSelectInputType weaponSelectInputType;
    public float reticleStopVelocityThreshold;

    [Header("Computer Vision Options")]
    public RawImage webcamPreview;
    public RawImage calibrationPreview;
    public bool enablePreview;
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
        webcamPreview.enabled = enablePreview && (aimInputType != AimInputType.Mouse);
        if (weaponSelectInputType == WeaponSelectInputType.IMU)
            rpiInput = new RaspberryPiInput(ipAddress, port);
        if (aimInputType == AimInputType.CV)
            cvInput = new ComputerVisionInput(greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
        else if (aimInputType == AimInputType.Finger) {
            ftInput = new FingerTracking(enablePreview, webcamPreview, calibrationPreview);
        }
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