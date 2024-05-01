using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

using HypeRate;

public class Example : MonoBehaviour
{
    // Put your websocket Token ID here
    public string websocketToken = "<Request your Websocket Token>"; // If you don't have one, get it here https://www.hyperate.io/api
    public string hyperateURL = "wss://app.hyperate.io/socket/websocket";
    public string hyperateID = "internal-testing";


    [Serializable]
    public class HypeRateDataPackagePayload
    {
        public string hr;
        public string status;
    }

    [Serializable]
    public class HypeRateDataPackage
    {
        public string @event; // @ escapes the "event" keyword
        public HypeRateDataPackagePayload payload;
    }

    // Textbox to display your heart rate in
    TMP_Text textBox;

    HypeRate.HypeRate hypeRateSocket;

    async void Start()
    {
        textBox = GetComponent<TextMeshProUGUI>();

        hypeRateSocket = HypeRate.HypeRate.GetInstance();
        await hypeRateSocket.ConnectToServer(websocketToken, hyperateURL);

        Debug.Log("poop");
        await hypeRateSocket.JoinHeartbeatChannel(hyperateID);


        /*

        websocket.OnMessage += (bytes) =>
        {
        // getting the message as a string
            var message = System.Text.Encoding.UTF8.GetString(bytes);

            var dataPackage = JsonUtility.FromJson<HypeRateDataPackage>(message);
            Debug.Log(dataPackage);

            if (dataPackage.@event == "hr_update")
            {
                // Change textbox text into the newly received Heart Rate (integer like "86" which represents beats per minute)
                textBox.text = dataPackage.payload.hr;
            }
        };

        // Send heartbeat message every 10 seconds in order to not suspended the connection
        InvokeRepeating("SendHeartbeat", 1.0f, 10.0f);
        */
    }

    private async void OnApplicationQuit()
    {
        await hypeRateSocket.CloseConnection();
    }

}
