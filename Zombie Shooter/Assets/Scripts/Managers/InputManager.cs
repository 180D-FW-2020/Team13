using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public InputType inputType;
    public MouseControlType mouseControlType;

    [Header("UI Elements")]
    public RawImage webcamPreview;

    [Header("Camera Control")]
    public Camera playerCamera;
    public float cameraRotateSensitivity;
    private float rotationX = 0f;
    private float rotationY = 0f;
    Quaternion originalRotation;

    [Header("Reticle Control")]
    public Transform reticle;

    [Header("Computer Vision Tracking")]
    public bool enablePreview;
    public float[] greenLowerHSV = new float[3];
    public float[] greenUpperHSV = new float[3];

    private ComputerVisionInput cvInput;

    public void Start()
    {
        originalRotation = playerCamera.transform.parent.rotation;
        if (inputType == InputType.CV)
            cvInput = new ComputerVisionInput(WebCamTexture.devices[0], greenLowerHSV, greenUpperHSV, enablePreview, webcamPreview);
    }

    public void FixedUpdate()
    {
        if (mouseControlType == MouseControlType.Reticle)
        {
            reticle.position = GetReticleInput();
        }
        else if (mouseControlType == MouseControlType.Camera)
        {
            playerCamera.transform.parent.rotation = GetCameraInput();
        }
    }

    public Vector3 GetReticleInput()
    {
        if (inputType == InputType.Mouse)
            return new Vector3(Input.mousePosition.x, Input.mousePosition.y);
        else
            return cvInput.Update();
    }

    public Quaternion GetCameraInput()
    {
        Vector2 position = Vector2.zero;
        if (inputType == InputType.Mouse)
            position = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        else
        {
            Vector2 inputPosition = cvInput.Update();
            position = new Vector2((inputPosition.x - Screen.width / 2) / Screen.width, (inputPosition.y - Screen.height / 2) / Screen.height);
        }
        rotationX += position.x * cameraRotateSensitivity;
        rotationY += position.y * cameraRotateSensitivity;

        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

        return originalRotation * xQuaternion * yQuaternion;
    }

    public void OnApplicationQuit()
    {
    }
}