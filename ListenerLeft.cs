/*ListenerLeft.cs
Code to listen for UDP messages containing sensor values from an ESP32.
This script is attached to a sphere containing a rigid body collider set to trigger. 
This sphere is a child of an empty Tracker object containing the "Steam VR_tracked object" script from the SteamVR Unity plugin. The selected tracked object is a vive tracker attached to the prototype.
The scene also contains cylinders containing a rigid body collider and a tag matching the corresponding light bulb in the fixture (e.g. "12").
Written by Niek Rutten for graduation project "Photon".

© Niek Rutten & TU/e 2023
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class ListenerLeft : MonoBehaviour
{

    int portData = 4100; //port on which to receive the data
    public int receiveBufferSize = 1000; // buffer for received data

    //udp variables
    UdpClient clientData;
    IPEndPoint ipEndPointData;
    private object obj = null;
    private System.AsyncCallback AC;
    byte[] receivedBytes;

    //public variables to store sensor values
    public float saturationSensor;
    public float luminanceSensor;

    void Start() //called once on initialization of object
    {
        InitializeUDPListener(); //start udp listener
    }
    public void InitializeUDPListener() //function to start udp listener
    {
        //initialize udp listener
        ipEndPointData = new IPEndPoint(IPAddress.Any, portData);
        clientData = new UdpClient();
        clientData.Client.ReceiveBufferSize = receiveBufferSize;
        clientData.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
        clientData.ExclusiveAddressUse = false;
        clientData.EnableBroadcast = true;
        clientData.Client.Bind(ipEndPointData);
        clientData.DontFragment = true;
        //listen for udp messages and run receivedUDPPacket function when a message is received
        AC = new System.AsyncCallback(ReceivedUDPPacket);
        clientData.BeginReceive(AC, obj);
        Debug.Log("UDP - Left Hand Start Receiving..");
    }

    void ReceivedUDPPacket(System.IAsyncResult result)
    {
        receivedBytes = clientData.EndReceive(result, ref ipEndPointData);//save incoming data
        ParsePacket();//process the incoming data
        clientData.BeginReceive(AC, obj);//continue listening
    }

    void ParsePacket() //function to process incoming data
    {
        String sensorString = System.Text.Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length); //convert incoming data from byte array to string
        String[] stringArray = sensorString.Split(' ');//split String into the two seperate sensor values
        int sensorNumber = Int32.Parse(stringArray[1]);//convert first sensor value from string to int
        //this sensor measures the size of the prototype through measuring the rotation of one of the hinges
        //if rotation passes the maximum it resets to 0, we add the maximum value if this happens to get a consistent readout
        if (sensorNumber < 1000) 
        {
            sensorNumber += 4095;
        }
        //remap the measured difference between current sensor value and the fully extended reference value from 0 to 1
        float brightness = map(Math.Abs(3666 - sensorNumber), 11, 360, 1, 0);
        //trim sensor values outside of the range
        if (brightness < 0) { brightness = 0; }
        if (brightness > 1) { brightness = 1; }
        luminanceSensor = brightness; //store value in a public variable
        
        int sensorNumber2 = Int32.Parse(stringArray[0]);//convert second sensor value from string to int
        float saturation = map(CalculateAverage(sensorNumber2), 15, 26, 0, 1);//calculate rolling average of the distance value and then remap range to 0-1
        //trim sensor values outside of the range
        if (saturation < 0) { saturation = 0; }
        if (saturation > 1) { saturation = 1; }
        saturationSensor = saturation;//store value in a public variable
    }
    //close UDP on shutdown
    void OnDestroy()
    {
        if (clientData != null)
        {
            clientData.Close();
        }

    }

    //function to remap value from one range (a1-a2) to another (b1-b2)
    float map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }

    //variables & function to calculate a rolling average
    private Queue<float> samples = new Queue<float>();
    private int windowSize = 15;//number of values to sample
    private float sampleAccumulator;
 private float CalculateAverage(float newSample)
    {
        sampleAccumulator += newSample;
        samples.Enqueue(newSample);

        if (samples.Count > windowSize)
        {
            sampleAccumulator -= samples.Dequeue();
        }

        return (sampleAccumulator / samples.Count);
    }
}

