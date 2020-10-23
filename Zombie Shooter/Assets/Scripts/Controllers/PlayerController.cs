using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public GameManager gameManager;
    public MouseControlType mouseControlType;

    // reticle
    public Transform reticle;

    // mouse controls camera
    public Camera playerCamera;
    public float cameraRotateSensitivity;
    private float rotationX = 0f;
    private float rotationY = 0f;
    Quaternion originalRotation;

    private Ray aimingRay;
    private RaycastHit hit;

    private void Start()
    {
        if (mouseControlType == MouseControlType.Camera)
        {
            Cursor.lockState = CursorLockMode.Locked;
            originalRotation = playerCamera.transform.parent.rotation;
        }
    }

    void Update()
    {
        Aim();

        if (Input.GetMouseButtonDown(0)) // click to shoot
        {
            Shoot();
        }
    }

    private void Aim()
    {
        if (mouseControlType == MouseControlType.Reticle)
        {
            reticle.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z);
        }
        else if (mouseControlType == MouseControlType.Camera)
        {
            rotationX += Input.GetAxis("Mouse X") * cameraRotateSensitivity;
            rotationY += Input.GetAxis("Mouse Y") * cameraRotateSensitivity;
            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

            playerCamera.transform.parent.rotation = originalRotation * xQuaternion * yQuaternion;
        }
        aimingRay = playerCamera.ScreenPointToRay(reticle.position);
    }

    private void Shoot()
    {
        if (Physics.Raycast(aimingRay, out hit))
        {
            GameObject hitObject = hit.collider.transform.gameObject;
            if (hitObject.tag == "Enemy")
            {
                Debug.Log("GOTTEM");
                Debug.Log(hitObject.name);
                gameManager.KillEnemy(hitObject);
            }
        }
    }
}
