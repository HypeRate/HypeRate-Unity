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
        textBox = GetComponent<TMP_Text>();

        hypeRateSocket = HypeRate.HypeRate.GetInstance();
        // connect to the hyperate server
        await hypeRateSocket.ConnectToServer(websocketToken, hyperateURL);
        // join the heartbeat channel, to receive heartbeat events
        await hypeRateSocket.JoinHeartbeatChannel(hyperateID);

        // add function to the message received callback, to do something useful
        hypeRateSocket.onMessageReceivedCallback = this.ChangeText;
    }

    private void ChangeText(string message)
    {
        HypeRateDataPackage datapackage = JsonUtility.FromJson<HypeRateDataPackage>(message);
        GetComponent<TextMesh>().text = datapackage.payload.hr;

    }

    private async void OnApplicationQuit()
    {
        await hypeRateSocket.CloseConnection();
    }

}
