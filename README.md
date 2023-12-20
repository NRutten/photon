This repository contains the relevant code for the graduation project 'Photon' by Niek Rutten.
The repository contains 5 relevant files, 3 c# scripts implemented in Unity and 2 Arduino sketches running on 2 different ESP32 modules in the prototype.
The two Arduino sketches gather data from sensors connected to the ESP32 modules and send it to a host PC.
The two "Listener" scripts receive this data.
The "Main" script converts the received sensor readings and combines them with a vive tracker's position to calculate and send dimming values to a lightsketching luminaire.
Not the entire unity project is included but details on how the unity scene is set up are detailed below as well as the circuit diagram and details for the electronics.

Written by Niek Rutten for graduation project "Photon".
© Niek Rutten & TU/e 2023

**Unity Setup**
The unity project is running on a pc to which a vive headset and vive tracker 2.0 is connected. The project is adapted from the VR core template.
The SteamVR plugin (https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647) is required and Steam & SteamVR must be installed.
The project tracks which bulbs to affect by tracking collisions between a sphere representing the prototype and cylinders representing the bulb's beams.

The sphere has a tracker object parent which follows the vive tracker trough the SteamVR_TrackedObject script included in the SteamVR plug in (make sure the appopriate device is selected).

![Unity1](https://github.com/NRutten/photon/assets/34235736/6fc53ddb-fbb3-49dd-9c07-337b68d71bb6)

The Sphere itself contains the 3 scripts included in the repository (the Main script references the Listener scripts so make sure to link those by dragging the scripts to the appropriate fields).
It also contains a collider, make sure "Is Trigger" is checked.

![Unity2](https://github.com/NRutten/photon/assets/34235736/0beb6335-0244-45c8-b2aa-bab25d39b8e9)

Each cylinder represents one bulb. Each cylinder contains a collider and has a tag indicating which bulb it represents. For easy calibration all cylinders share an empty "fixtures" parent.

![Unity3](https://github.com/NRutten/photon/assets/34235736/ff0b9b90-fd43-44d2-99b8-d4539b8d89db)
