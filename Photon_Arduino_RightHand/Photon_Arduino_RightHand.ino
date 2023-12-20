/*
Photon_Arduino_RightHand.ino
Code to measure rotation and send the values over UDP to a host PC.
Written by Niek Rutten for graduation project "Photon".
Based on reference code:

Tillaart, R (2023) A5600_position (version 0.5.0) [Arduino sketch]. https://github.com/RobTillaart/AS5600/

© Niek Rutten & TU/e 2023
*/

#include "AS5600.h" //Magnetic Rotary Encoder
#include "Wire.h" //I2C library for communication with the sensors

AS5600 as5600;  //objects for sensor

//libraries for UDP communication over wifi
#include <WiFi.h>
#include <WiFiUdp.h>

// Network credentials
const char* ssid     = "XXX";
const char* password = "XXX";

IPAddress local_IP(192, 168, 4, X); // Static IP of ESP module
IPAddress gateway(192, 168, 4, 1); // gateway IP (router)
IPAddress subnet(255, 255, 255, 0); //subnet

unsigned int localPort = 4201;  //UDP port on which to receive messages
unsigned int sendPort = 4101; //UDP port on which to send messages

// buffers for receiving and sending data
char packetBuffer[24]; //buffer to hold incoming packet,
char  ReplyBuffer[36];     // a string to send back

int rotation=0; // variable for measuring rotation

int frequency=20; //measuring frequency in Hz
static uint32_t lastTime = 0; //variable for time tracking

WiFiUDP udp; //udp object for communication

void setup()
{
  Serial.begin(115200);
  // Configures static IP address
  if (!WiFi.config(local_IP, gateway, subnet)) {
    Serial.println("STA Failed to configure");
  }

  // Connect to Wi-Fi network with SSID and password
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  // Print local IP address
  Serial.println("");
  Serial.println("WiFi connected.");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
  //start UDP listener
  Serial.printf("udp server on port %d\n", localPort);
  udp.begin(localPort);
  
  Wire.begin();
  as5600.begin(4);  //initialize rotation sensor
  delay(100);
}


void loop()
{
  
if (millis() - lastTime >= 1000/frequency) //retrieve sensor values in intervals specified in the frequency value
  {
    lastTime = millis();
    rotation=as5600.readAngle();
  }

  int packetSize = udp.parsePacket(); //check for new incoming messages
  udp.read(packetBuffer, 24);
  
  if (packetSize) { //if a packet was received, respond with a packet containing the current sensor reading
      Serial.println("received");
      String sensorValues = String(rotation);
      sensorValues.toCharArray(ReplyBuffer,36);
      int packetSize = udp.parsePacket();
      udp.beginPacket(udp.remoteIP(), sendPort);
      udp.print(ReplyBuffer);
      udp.endPacket();
  }
}
