using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net.Sockets;
using System;
using System.Net;
using System.Text;

public enum GestureType
{
    L = 0,
    R = 1,
    U = 2,
    D = 3,
    None = 4,
}

// Performs all networking and data aquisition to receive gestures from the Pi
public class RaspberryPiInput
{
    private Socket client;

    // initialize to user-specified RPi IP address
    public RaspberryPiInput(string ip, int port)
    {
        IPAddress ipAddress = IPAddress.Parse(ip);
        client = new Socket(SocketType.Stream, ProtocolType.Tcp);
        client.Connect(ipAddress, port);
    }

    // receive a single character
    public GestureType GetGesture()
    {
        if (client.Available > 0)
        {
            byte[] buffer = new byte[1];
            client.Receive(buffer);
            string gesture = Encoding.UTF8.GetString(buffer);
            try
            {
                GestureType gestureType = (GestureType)Enum.Parse(typeof(GestureType), gesture, true);
                Debug.Log($"Gesture: {gesture}");
                return gestureType;
            }
            catch
            {
                Debug.Log($"Invalid Gesture: {gesture}");
                return GestureType.None;
            }
        }
        return GestureType.None;
    }
    
    public void Close()
    {
        client.Close();
    }
}
