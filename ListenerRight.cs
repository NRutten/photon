/*ListenerRight.cs
Code to listen for UDP messages containing sensor values from an ESP32.
This script is attached to a sphere containing a rigid body collider set to trigger. 
This sphere is a child of an empty Tracker object containing the "Steam VR_tracked object" script from the SteamVR Unity plugin. The selected tracked object is a vive tracker attached to the prototype.
The scene also contains cylinders containing a rigid body collider and a tag matching the corresponding light bulb in the fixture (e.g. "12").
Written by Niek Rutten for graduation project "Photon".

© Niek Rutten & TU/e 2023
 */

using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

public class ListenerRight : MonoBehaviour
{
    int portData = 4101; //port on which to receive the data
    public int receiveBufferSize = 1000; // buffer for received data

    public float hueSensor; //public variable to store sensor value

    //udp variables
    UdpClient clientData;
    IPEndPoint ipEndPointData;
    private object obj = null;
    private System.AsyncCallback AC;
    byte[] receivedBytes;

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
        Debug.Log("UDP - Right Hand Start Receiving..");
    }

    void ReceivedUDPPacket(System.IAsyncResult result)
    {
        receivedBytes = clientData.EndReceive(result, ref ipEndPointData); //save incoming data
        ParsePacket();//process the incoming data
        clientData.BeginReceive(AC, obj);//continue listening
    }

    void ParsePacket() //function to process incoming data
    {
        String sensorString = System.Text.Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length); //convert incoming data from byte array to string
        int sensorNumber = Int32.Parse(sensorString); //convert incoming data from string to int
        if (sensorNumber > 2048)//this sensor is linked to the hue, we are only using half the range so we remap the value to reset after passing the 180 degree point
        {
            sensorNumber -= 2048;
        }
        hueSensor = (float)sensorNumber / 2048; //remap to value from 0 to 1 and store in public variable
    }

    void OnDestroy() //close UDP on shutdown
    {
        if (clientData != null)
        {
            clientData.Close();
        }

    }
}

