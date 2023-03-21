using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WebSocketSharp;

public class IncomingPacket
{
    public string type { get; set; }
    public string payload { get; set; }
}

public class ParseConnect
{
    public string client_id { get; set; }
}

public class ParseWithSdp
{
    public string client_id { get; set; }
    public string sdp { get; set; }
}

public class WebSocketClient : MonoBehaviour
{
    public string URL = "172.31.16.1"; // "127.0.0.1";
    public string room_id = "1";
    public string status { get; private set; }
    public string client_id { get; private set; }

    private WebSocketSharp.WebSocket m_socket = null;

    private void Awake()
    {
        status = "N/A";
        client_id = "N/A";
    }

    private void Start()
    {
        try {
            m_socket = new WebSocketSharp.WebSocket(URL);
            m_socket.OnOpen += OnOpen;
            m_socket.OnClose += OnClose;
            m_socket.OnMessage += ReceiveMsg;
            m_socket.OnError += OnError;

            status = "Ready";
        }
        catch {

        }
    }

    private void OnDestroy()
    {
        
    }

    public void Connect()
    {
        if (m_socket == null || !m_socket.IsAlive)
        {
            status = "Connecting";
            m_socket.Connect();
        }
    }

    public void CloseSocket()
    {
        if (m_socket == null)
            return;
        if (m_socket.IsAlive)
            m_socket.Close();

        m_socket = null;

        room_id = "0";
        client_id = "N/A";

        status = "Closed";
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        Debug.Log("# Connection is closed: " + e.Reason);

        CloseSocket();
    }

    private void OnOpen(object sender, System.EventArgs e)
    {
        Debug.Log("### Open");

        status = "Connected";
    }

    private void OnError(object sender, ErrorEventArgs args)
    {
        Debug.Log("### Error: " + args.Message);
    }

    public void Send(string msg)
    {
        if (!m_socket.IsAlive)
            return;

        m_socket.Send(Encoding.UTF8.GetBytes(msg));
    }

    public void ReceiveMsg(object sender, MessageEventArgs e)
    {
        Debug.Log("> " + e.Data);

        var packet = JsonConvert.DeserializeObject<IncomingPacket>(e.Data);

        //Debug.LogFormat("# {0}: {1}", packet.type, packet.payload);

        if (packet.type == "ping")
            return;

        if (packet.type == "notify-client-id")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseConnect>(payload_str);
            client_id = payload.client_id;

            Debug.LogFormat("My client id is : {0}", client_id);
        }

        if (packet.type == "new-client")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseConnect>(payload_str);
            // check payload.client_id is same with client_id
            // if not, there is new client just joined.

            if (payload.client_id == client_id)
                this.status = "Joined";

            Debug.LogFormat("New client is coming : {0}", payload.client_id);
        }

        if (packet.type == "offer")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseWithSdp>(payload_str);
            Debug.LogFormat("Got offer: {0}, {1}", payload.client_id, payload.sdp);
        }

        if (packet.type == "answer")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseWithSdp>(payload_str);
            Debug.LogFormat("Got answer: {0}, {1}", payload.client_id, payload.sdp);
        }

        if (packet.type == "ice-candidate")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseWithSdp>(payload_str);
            Debug.LogFormat("Got ice-candidate: {0}, {1}", payload.client_id, payload.sdp);
        }

        if (packet.type == "leave-client")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<ParseWithSdp>(payload_str);
            Debug.LogFormat("Client leaved: {0}, {1}", payload.client_id, payload.sdp);
        }
    }
}
