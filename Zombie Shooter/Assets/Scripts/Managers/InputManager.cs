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

    [Header("First Person Aim")]
    public float shootInterval;
    public float sensitivity;
    public float xLimit;
    public float yLimit;

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

    private Vector3 previousRotation;
    internal float velocity = 0;
    private Vector2 rotation = Vector2.zero;
    private PlayerController playerController;
    private bool shootingEnabled = false;

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

    public void ResetRotation()
    {
        rotation = Vector2.zero;
    }

    public void SetMainPlayer(PlayerController player)
    {
        playerController = player;
        StartCoroutine(AutoShoot());
    }

    public void UpdateInput()
    {
        //weapon switching
        playerController.weaponController.SwitchWeapon(GetGesture());

        //aim reticle
        rotation += GetAimInput() * sensitivity;
        rotation.x = Mathf.Clamp(rotation.x, -xLimit / 2, xLimit / 2);
        rotation.y = Mathf.Clamp(rotation.y, -yLimit / 2, yLimit / 2);
        playerController.transform.eulerAngles = rotation;
        velocity = (playerController.transform.eulerAngles - previousRotation).sqrMagnitude / Time.deltaTime;
        previousRotation = playerController.transform.eulerAngles;
    }

    public int GetCurrentAmmo()
    {
        return playerController.GetCurrentAmmo();
    }

    public void EnableShooting(bool enabled)
    {
        shootingEnabled = enabled;
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

    private IEnumerator AutoShoot()
    {
        while (true)
        {
            if (shootingEnabled)
            {
                ShootIfStopped();
                yield return new WaitForSeconds(shootInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void ShootIfStopped()
    {
        if (Mathf.Abs(velocity) < reticleStopVelocityThreshold) //aim stopped
        {
            playerController.weaponController.Shoot();
            gameManager.Shoot((int)playerController.weaponController.GetWeaponType());
        }
    }

    public void OnApplicationQuit()
    {
        rpiInput?.Close();
    }
}