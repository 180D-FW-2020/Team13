using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO.Ports;
using System;

public enum GestureType
{
    Left = 0,
    Right = 1,
    Up = 2,
    Down = 3
}

public class SerialInput
{
    public UnityEvent<GestureType> Gesture;
    private SerialPort serialPort;

    public SerialInput(string portName, UnityAction<GestureType> gestureHandler)
    {
        Gesture.AddListener(gestureHandler);
        serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
        serialPort.Open();
        serialPort.DataReceived += SerialDataReceived;
    }

    private void SerialDataReceived(object sender, SerialDataReceivedEventArgs args)
    {
        string gesture = serialPort.ReadExisting();
        try
        {
            GestureType gestureType = (GestureType)Enum.Parse(typeof(GestureType), gesture, true);
            Debug.Log($"Gesture: {gesture}");
            Gesture.Invoke(gestureType);
        } 
        catch
        {
            Debug.Log($"Invalid Gesture: {gesture}");
        }
    }
    
    public void Close()
    {
        serialPort.Close();
    }
}
