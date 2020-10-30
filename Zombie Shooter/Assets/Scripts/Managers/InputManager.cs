using System;
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.UI;

public class CVInputState
{
    public const int bufferSize = 1024;
    public byte[] buffer = new byte[bufferSize];
    public string readString;
    public Socket clientSocket;
}

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

    [Header("UI Elements")]
    public Text cvConnectionStatus;
    public Text imuConnectionStatus;
    public Color disconnectedColor;
    public Color connectedColor;

    private Socket serverSocket;
    private Socket clientSocket;
    private bool connected = false;

    private int screenWidth;
    private int screenHeight;
    private Vector2 inputPosition;

    public void Start()
    {
        cvConnectionStatus.color = disconnectedColor;
        imuConnectionStatus.color = disconnectedColor;

        if (inputType == InputType.CV)
            StartCoroutine(ConnectSocketClient());
        else
            OnInputConnected.Invoke();
    }

    private IEnumerator ConnectSocketClient()
    {
        Debug.Log("CV Input Socket Connection State: Disconnected");
        IPAddress address = IPAddress.Parse(ipAddress);
        IPEndPoint endpoint = new IPEndPoint(address, port);
        serverSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(endpoint);
        serverSocket.Listen(1);
        yield return serverSocket;

        Task<Socket> acceptTask = serverSocket.AcceptAsync();
        Debug.Log("CV Input Socket Connection State: Listening");
        while (!acceptTask.IsCompleted)
        {
            yield return clientSocket;
        }
        clientSocket = acceptTask.Result;
        Debug.Log("CV Input Socket Connection State: Connected");
        OnInputConnected.Invoke();
        CVInputState state = new CVInputState();
        state.clientSocket = clientSocket;
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        cvConnectionStatus.text = "Connected";
        cvConnectionStatus.color = connectedColor;
        clientSocket.BeginReceive(state.buffer, 0, CVInputState.bufferSize, 0, ReceiveData, state);

        yield return clientSocket;
    }

    public void ReceiveData(IAsyncResult result)
    {
        CVInputState state = (CVInputState)result.AsyncState;
        if (connected)
        {
            int n = state.clientSocket.EndReceive(result);
            if (n > 0)
            {
                state.readString = Encoding.ASCII.GetString(state.buffer, 0, n);
                var split = state.readString.Trim().Split(' ').Select(s => float.Parse(s)).ToArray();
                split[0] *= screenWidth;
                split[1] *= screenHeight;
                var pos = new Vector2(split[0], split[1]);
                inputPosition = pos;
            }
            state.clientSocket.BeginReceive(state.buffer, 0, CVInputState.bufferSize, 0, ReceiveData, state);
        }
    }

    public void StartGame()
    {
        connected = true;
    }

    public void Update()
    {
        if (connected)
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

    public void OnApplicationQuit()
    {
        connected = false;
        serverSocket?.Shutdown(SocketShutdown.Both);
        clientSocket?.Close();
        serverSocket?.Close();
    }
}