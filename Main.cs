/*Main.cs
Code to convert sensor readings and object position to dimming values and sending these values to a lightsketching luminaire.
This script is attached to a sphere containing a rigid body collider set to trigger. 
This sphere is a child of an empty Tracker object containing the "Steam VR_tracked object" script from the SteamVR Unity plugin. The selected tracked object is a vive tracker attached to the prototype.
The scene also contains cylinders containing a rigid body collider and a tag matching the corresponding light bulb in the fixture (e.g. "12").
Two referenced scripts need to be assigned in the inspector: ListenerLeft.cs & ListenerRight.cs to retrieve sensor values

Written by Niek Rutten for graduation project "Photon".

© Niek Rutten & TU/e 2023
 */

using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;
using static Unity.VisualScripting.Metadata;
using Unity.VisualScripting;

public class Main : MonoBehaviour
{   

    public Color myColor; //for calculating the light's color 

    float _time;//time tracking value
    float _interval = 0.05f;//update interval of the fixture values in ms

    //we need to send udp messages to the ESP32 modules so they respond with sensor values
    //these variables are for those messsages and defined in the Init() function
    private string IPLeft;
    public int portLeft;  
    IPEndPoint remoteEndPointLeft;
    UdpClient left;
    private string IPRight;
    public int portRight;
    IPEndPoint remoteEndPointRight;
    UdpClient right;

    //these variables are for sending data to the fixture and defined in the Init() function
    private string IPLamp;
    public int portLamp;
    IPEndPoint remoteEndPointLamp;
    UdpClient lamp;

    public ushort[] brightness = new ushort[205];//array for dimming values in the form of Ushorts (2 byte integers)
    public byte[] package = new byte[410];//byte array for the actual data to be sent to the fixture in bytes

    public ListenerRight ListenerRight; //ListenerRight class referencing ListenerRight script
    public ListenerLeft ListenerLeft; //ListenerLeft class referencing ListenerRight script

    private int[] colliderLog = new int[38]; //array for tracking which beams the prototype is currently colliding with

    private void Awake() //called once during initialization of object
    {
        _time = 0f; 
        Init(); //run UDP initialization function

        for (int i = 0; i < samples.Length; i++) //initialize the array used for keeping a rolling average of all dimming values
        {
            samples[i] = new Queue<float>();
        }
    }


    private void Update() //runs every frame
    {
        _time += Time.deltaTime;//keep tracking time
        while (_time >= _interval)//if interval number of ms has passed
        {
            for (int i = 0; i < colliderLog.Length; i++)//for all the bulb's beams check if they are currently in collision with the sphere representing the prototype
            {
                if (colliderLog[i] == 1)//if they are in collision, calculate the dimming values based on the sensor values
                {
                    //for debugging we check if there is sensor data coming in
                    print(ListenerLeft.luminanceSensor);
                    print(ListenerLeft.saturationSensor);
                    print(ListenerRight.hueSensor);

                    //the index 'i' of the colliderLog array corresponds to the relevant bulb in the luminaire
                    //each bulb contains 5 different LED types (red, green, blue, warm white, cold white) and thus occupies 5 dimming channels in the array
                    int index = i * 5;

                    //we go from Hue, Saturation, Brightness (value) values to Red, Green, Blue dimming values by converting from the HSV color space to the RGB color space
                    //hue is controlled by the sensor measuring the rotation of two hands
                    //brightness is controlled by the sensor measuring the total extension of the prototype
                    myColor = Color.HSVToRGB(ListenerRight.hueSensor, 1, ListenerLeft.luminanceSensor); 

                    //the saturation sensor controls the ratio between the RGB leds and the white leds
                    //a value of 0 means only white LEDs, a value of 1 means only RGB LEDs
                    float red = myColor.r * ListenerLeft.saturationSensor; // the rgb values are fetched from the calculated color
                    float green = myColor.g * ListenerLeft.saturationSensor;
                    float blue = myColor.b * ListenerLeft.saturationSensor;
                    //the value of the white LEDs is also still scaled based on the value of the brightness sensor
                    float ww = ListenerLeft.luminanceSensor * (1-ListenerLeft.saturationSensor);
                    float cw = ListenerLeft.luminanceSensor * (1 - ListenerLeft.saturationSensor);

                    //for every dimming value of every bulb we keep a rolling average and update that for the actual dimming values
                    //this ensures no sudden transitions in brightness
                    //here we also remap the values from 0 to 1 to the range actually used by the luminaire protocol (2 byte numbers ranging from 0 to 4095)
                    ushort Red = (ushort)Math.Round(ComputeAverage(red, index) * 4095);
                    ushort Green = (ushort)Math.Round(ComputeAverage(green, index + 1) * 4095);
                    ushort Blue = (ushort)Math.Round(ComputeAverage(blue, index + 2) * 4095);
                    ushort Ww = (ushort)Math.Round(ComputeAverage(ww, index + 3) * 4095);
                    ushort Cw = (ushort)Math.Round(ComputeAverage(cw, index + 4) * 4095);

                    //save the final dimming values in the appopriate array
                    brightness[index] = Red;
                    brightness[index + 1] = Green;
                    brightness[index + 2] = Blue;
                    brightness[index + 3] = Cw;
                    brightness[index + 4] = Ww;
                }
            }


            for (int i = 0; i < package.Length; i += 2)//here we convert the ushort array into a byte array ready for sending by ordering and assigning the bytes
            {
                package[i] = BitConverter.GetBytes(brightness[i / 2])[1];
                package[i + 1] = BitConverter.GetBytes(brightness[i / 2])[0];
            }
            SendData(package); //send the updated bytes to the fixture
            _time -= _interval; //reset the timer
        }

    }

    public void Init() //initialize all the devices to send UDP messages to
    {
        print("UDPSend.init()");
        IPLamp = "192.168.4.203"; //IPaddress of the light fixture
        portLamp = 4210; //port through which to send the dimming values
        remoteEndPointLamp = new IPEndPoint(IPAddress.Parse(IPLamp), portLamp);
        lamp = new UdpClient();

        IPLeft = "192.168.4.241"; //IPaddress of the left ESP32 module
        portLeft = 4200; //port through which to send the call for new sensor values of the left ESP32 module
        remoteEndPointLeft = new IPEndPoint(IPAddress.Parse(IPLeft), portLeft);
        left = new UdpClient();

        IPRight = "192.168.4.210"; //IPaddress of the right ESP32 module
        portRight = 4201; //port through which to send the call for new sensor values of the right ESP32 module
        remoteEndPointRight = new IPEndPoint(IPAddress.Parse(IPRight), portRight);
        right = new UdpClient();
    }

    //variables and function to calculate average for all dimmming values
    Queue<float>[] samples = new Queue<float>[205];//one queue for each dimming value
    private float[] sampleAccumulator = new float[205];
    private float sampleCount = 0;
    private int windowSize = 15;//number of values to sample

    public float ComputeAverage(float newSample, int arrayIndex)
    {
        sampleAccumulator[arrayIndex] += newSample; //update the values at the given index
        samples[arrayIndex].Enqueue(newSample);

        if (samples[arrayIndex].Count > windowSize) //remove oldest values
        {
            sampleAccumulator[arrayIndex] -= samples[arrayIndex].Dequeue();
        }

        return sampleAccumulator[arrayIndex] / samples[arrayIndex].Count; //calculate and return new average
    }

    private void OnTriggerEnter(UnityEngine.Collider other) //this function is triggered when the sphere representing the prototype collides with a beam's cylinder
    {
        Int16 index;
        Int16.TryParse(other.gameObject.tag, out index); //check the tag of the given cylinder for which bulb it corresponds to and convert it into an integer
        colliderLog[index] = 1; //update the array to reflect the new collisions
    }

    private void OnTriggerExit(UnityEngine.Collider other) //this function is triggered when the sphere representing the prototype exits a beam's cylinder
    {
        Int16 index;
        Int16.TryParse(other.gameObject.tag, out index); // check the tag of the given cylinder for which bulb it corresponds to and convert it into an integer
        colliderLog[index] = 0;//update the array to reflect the ended collisions
    }

    private void SendData(byte[] data) //function to send data to both the light fixture and the two ESP32 modules
    {
        try
        {
            var trigger = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };//random data to send as a data request for the ESP32 modules
            lamp.Send(data, data.Length, remoteEndPointLamp); //send most recent dimming values to the light fixture
            left.Send(trigger, trigger.Length, remoteEndPointLeft); //send data request to left ESP32 modules
            right.Send(trigger, trigger.Length, remoteEndPointRight); //send data request to right ESP32 modules

        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

}




