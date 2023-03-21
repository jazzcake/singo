using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using System;
using System.Text;
using UnityEngine.Networking.PlayerConnection;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.XR;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine.Events;

[RequireComponent(typeof(WebRTCModule))]
class DataChannelSampleA : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Button connectButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hangupButton;
    [SerializeField] private Button sendButton;
    [SerializeField] private InputField textSend;
    [SerializeField] private InputField textReceive;
#pragma warning restore 0649

    private string room_id = "1";

    private WebRTCModule _web_rtc_mod = null;

    private void Awake()
    {
        _web_rtc_mod = GetComponent<WebRTCModule>();

        _web_rtc_mod.log_handler = (log) =>
        {
            textReceive.text += ">" + log + Environment.NewLine;
            Debug.Log(log);
        };

        _web_rtc_mod.data_channel_open_handler = () =>
        {
            sendButton.interactable = true;
            hangupButton.interactable = true;
        };

        _web_rtc_mod.socket_handler = (status) =>
        {
            switch (status)
            {
                case WebRTCModule.WebSocketStatus.Connected:
                    joinButton.interactable = true;
                    break;
            }
        };

        connectButton.onClick.AddListener(() =>
        {
            connectButton.interactable = false;

            _web_rtc_mod.Connect();
        });

        joinButton.onClick.AddListener(() =>
        {
            joinButton.interactable = false;

            _web_rtc_mod.Join(room_id);
        });

        hangupButton.onClick.AddListener(() =>
        {
            _web_rtc_mod.Hangup();

            textSend.text = string.Empty;
            textReceive.text = string.Empty;

            hangupButton.interactable = false;
            sendButton.interactable = false;
            //callButton.interactable = true;
        });

        sendButton.onClick.AddListener(() =>
        {
            _web_rtc_mod.SendMsg(textSend.text);
        });
    }

    private void Start()
    {
        connectButton.interactable = true;
        joinButton.interactable = false;
        hangupButton.interactable = false;
    }
}
