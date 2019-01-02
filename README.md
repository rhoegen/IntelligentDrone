# IntelligentDrone

This Proof of Concepts uses the DJI Drone SDK to fetch live video from supported DJI drones and analyzes it using Microsoft cognitive services.

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

# How it was done
<TBD>
