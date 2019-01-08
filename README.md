# IntelligentDrone

This Proof of Concepts uses the DJI Drone SDK to fetch live video from supported DJI drones and analyzes it using Microsoft cognitive services.

This project uses code from the following projects/sites:

Quickstart Guide for Object Detection: https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/csharp-tutorial-od
Quickstart Guide for Computer Vision: https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/quickstarts/csharp-analyze
DJI SDK Tutorial: https://github.com/dji-sdk/Windows-SDK-Doc/blob/master/source/cn/tutorials/index.md
Encode WriteableBitmap to JPG: https://stackoverflow.com/questions/39613854/uwp-encode-writeablebitmap-to-jpeg-byte-array

Also used was the project https://github.com/vladkol/CustomVision.COCO to help train a Custom Vision Project using the CoCo Dataset (http://cocodataset.org/).

# Getting it to work

## Prerequisites
To get the App to work you will need a compatible DJI Drone. As of January 2019, these include only the Mavic Air as well as the Phantom Pro 4. Please refer to https://github.com/dji-sdk/Windows-SDK for more details and updates on supported drones.

## Register DJI App (Developer Portal)
The DJI SDK will have to be registered to a developer account for each App. You can create an app within the developer portal: https://developer.dji.com/user/apps/.

## Get your Cognitive Services keys & URLs
The cognitive services need a subscription key that is passed to the endpoint. To get the keys necessary for this demo, you have to create a subscription. A free subscription is sufficient for the demo.

Get a Free Azure subscription here: https://azure.microsoft.com/de-de/free/ai/

## Optional: Train a Custom Vision Model
If you want to dig into more advanced scenarios, a custom vision model will be needed. You will need the training and evaulation keys from https://customvision.ai/ to use object recognition.

## Connect your drone
A Mavic Air can be connected directly to your computer using WiFi. To do this just use the same procedure that you use to connect the drone directly to your smartphone (without controller).

Please note that you need to provide an internet connection with a LAN cable or seperate WiFi adapter to make the cognitive services request.

## Start the App
On the first start the app will not be able to connect to the drone. You'll have to go to the settings page and enter all IDs that you have collected through the paragraphs above. The Settings will be saved on your device so with the next startup, you should see the drone feed in the appropriate page.

# How it was done
<TBD>
