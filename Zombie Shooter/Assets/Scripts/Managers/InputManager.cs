using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class InputManager : MonoBehaviour
{
    public int port;
    public string ipAddress;

    public InputType inputType;
    public MouseControlType mouseControlType;

    public UnityEvent OnInputConnected;

    [Header("Camera Control")]
    public Camera playerCamera;
    public float cameraRotateSensitivity;
    private float rotationX = 0f;
    private float rotationY = 0f;
    Quaternion originalRotation;

    [Header("Reticle Control")]
    public Transform reticle;

    private bool running = false;
    private Socket serverSocket;
    private Socket clientSocket;

    private Vector2 inputPosition;
    private int screenWidth;
    private int screenHeight;

    private Process cvProcess;
    private Thread readThread;
    private bool inputConnected = false;
    private bool gameStarted;

    private void Start()
    {
        if (mouseControlType == MouseControlType.Camera)
        {
            Cursor.lockState = inputType == InputType.CV ? CursorLockMode.None : CursorLockMode.Locked;
            originalRotation = playerCamera.transform.parent.rotation;
        }

        StartCoroutine(ConnectCVInput());
    }

    private IEnumerator ConnectCVInput()
    {
        yield return null;
        if (inputType == InputType.CV)
        {
            cvProcess = new Process();
            cvProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = "python",
                Arguments = "-u ../../CV/linear_tracking.py",
                WorkingDirectory = Application.dataPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            cvProcess.EnableRaisingEvents = true;
            cvProcess.Exited += ProcessExited;
            cvProcess.Start();
            yield return cvProcess;

            var line = "";
            while ((line = cvProcess.StandardOutput.ReadLine()) != "Connected")
                yield return line;

            screenWidth = Screen.width;
            screenHeight = Screen.height;
            ThreadStart threadStart = new ThreadStart(ReadOutput);
            readThread = new Thread(threadStart);
            readThread.Start();
        }

        OnInputConnected.Invoke();
        inputConnected = true;
        yield return null;
    }

    private void ReadOutput()
    {
        while (inputConnected)
        {
            string data = cvProcess.StandardOutput.ReadLine().Trim();
            if (string.IsNullOrEmpty(data)) continue;

            var split = data.Split(' ').Select(s => float.Parse(s)).ToArray();
            split[0] *= screenWidth;
            split[1] *= screenHeight;
            var pos = new Vector2(split[0], split[1]);
            inputPosition = pos;
        }
    }

    private void ProcessExited(object sender, EventArgs e)
    {
        Application.Quit(1);
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    public void Update()
    {
        if (inputConnected && gameStarted)
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
    }

    public Vector3 GetReticleInput()
    {
        if (inputType == InputType.Mouse)
            return new Vector3(Input.mousePosition.x, Input.mousePosition.y);
        else
            return inputPosition;
    }

    public Quaternion GetCameraInput()
    {
        Vector2 position = Vector2.zero;
        if (inputType == InputType.Mouse)
            position = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        else
            position = new Vector2((inputPosition.x - Screen.width / 2) / Screen.width, (inputPosition.y - Screen.height / 2) / Screen.height);
        rotationX += position.x * cameraRotateSensitivity;
        rotationY += position.y * cameraRotateSensitivity;

        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

        return originalRotation * xQuaternion * yQuaternion;
    }

    public void OnDestroy()
    {
        readThread?.Abort();
        cvProcess?.Kill();
        clientSocket?.Shutdown(SocketShutdown.Both);
        clientSocket?.Close();
        serverSocket?.Close();
    }
}
