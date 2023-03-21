using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using JetBrains.Annotations;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR

[CustomEditor(typeof(WebSocketClient))]
public class WebSocketEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WebSocketClient t = (WebSocketClient)target;
        if (!t)
            return;

        // 접속되는 상황을 보고 아래를 진행한다.
        t.URL = EditorGUILayout.TextField(t.URL);
        EditorGUILayout.TextField(t.status);

        if (t.status == "Ready" || t.status == "Closed")
        {
            if (GUILayout.Button("Connect"))
                t.Connect();
        }
        else
        {
            if (t.status == "Connected")
            {
                t.room_id = EditorGUILayout.TextField(t.room_id);

                if (GUILayout.Button("Join Room"))
                {
                    var payload = new JObject();
                    payload.Add("room_id", t.room_id);

                    var json = new JObject();
                    json.Add("type", "join");
                    json.Add("payload", payload);

                    t.Send(json.ToString());
                }
            }

            if (t.status == "Joined")
            {
                if (GUILayout.Button("Send Offer"))
                {
                    // @note - sdp는 어떤거든 signaling server는 개의치 않습니다. client끼리 검증할 내용임.
                    var payload = new JObject();
                    payload.Add("sdp", "a=ice-ufrag:23Mj\r\n\r\na=ice-pwd:I5aYxsishC5pYTLiYmh+lh\r\n\r\na=ice-options:ice2,trickle");
                    payload.Add("client_id", t.client_id);

                    var json = new JObject();
                    json.Add("type", "offer");
                    json.Add("payload", payload);

                    t.Send(json.ToString());
                }
            }
        }
    }
}

#endif