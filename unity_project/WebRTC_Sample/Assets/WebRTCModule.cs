using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using UnityEngine.Events;
using System.Net.Sockets;

namespace Signaling
{
    public class IncomingPacket
    {
        public string type { get; set; }
        public string payload { get; set; }
    }

    public class ParseConnect
    {
        public string client_id { get; set; }
    }

    public class ParseNewClient
    {
        public string client_id { get; set; }
        public int index { get; set; }
    }

    public class ParseWithSdp
    {
        public string client_id { get; set; }
        public int index { get; set; }
        public string sdp { get; set; }
    }

    public class ParseIceCandidate
    {
        public string client_id { get; set; }
        public int index { get; set; }
        public string candidate { get; set; }
        public string sdp_mid { get; set; }
        public int sdp_index { get; set; }
    }

}

public class WebRTCModule : MonoBehaviour
{
    public enum WebSocketStatus
    {
        Invalid,
        Ready,
        Connecting,
        Connected,
        Joined,
        Closed,
    }

    public string URL = "ws://127.0.0.1:5000/connect";
    public WebSocketStatus status { get; private set; }
    public string client_id { get; private set; }

    private WebSocketSharp.WebSocket m_socket = null;

    private Queue<string> _log_queue = new Queue<string>();
    private Queue<Action> _actions = new Queue<Action>();

    // event handler
    public System.Action<string> log_handler;
    public System.Action data_channel_open_handler;
    public System.Action<WebSocketStatus> socket_handler;

    private void Update()
    {
        // 다른 스레드에서 호출되면 예외가 발생하는 처리들 때문에 이렇게 했음
        if (_actions.Count > 0)
        {
            var action = _actions.Dequeue();
            action();
        }

        if (_log_queue.Count > 0)
        {
            var log = _log_queue.Dequeue();
            log_handler(log);
        }
    }


    #region [DataChannelSample]
    RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="candidate"></param>
    void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
    {
        var target_client_id = FindTargetID(pc);
        if (string.IsNullOrEmpty(target_client_id))
            return;

        Debug_Log($"peer:{client_id}-{GetTargetClientID(pc)} ICE candidate sending to:\n {candidate.Candidate}");
        SendICECandidate(candidate, target_client_id);
    }


    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Connected");

                // 완료됐습니다.
                var target_client_id = GetTargetClientID(pc);
                peer_conn_dic[target_client_id].is_avaiable = true;
                break;
            case RTCIceConnectionState.Disconnected:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug_Log($"ConnectionState> peer:{client_id}-{GetTargetClientID(pc)} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }

    // 로컬에 new-client가 들어오면 offer를 발송한다.
    private IEnumerator ActionSendOfferToTarget(string target_client_id)
    {
        Debug_Log("GetSelectedSdpSemantics");
        var configuration = GetSelectedSdpSemantics();

        // create new peer-connection (ie. agent)
        var pc1 = new RTCPeerConnection(ref configuration);
        var pc1_data = RegisterPeer(pc1, target_client_id);

        Debug_Log("Created local peer connection object pc1");
        pc1.OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        pc1.OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); };
        pc1.OnDataChannel = channel =>
        {
            pc1_data.remote_channel = channel;
            pc1_data.remote_channel.OnMessage = bytes =>
            {
                Debug_Log($"> msg from {target_client_id}: {System.Text.Encoding.UTF8.GetString(bytes)}");
            }; ;
        }; ;

        // 데이터용 채널 등록 for local
        RTCDataChannelInit conf = new RTCDataChannelInit();
        pc1_data.data_channel = pc1.CreateDataChannel("data", conf);
        pc1_data.data_channel.OnOpen = () =>
        {
            data_channel_open_handler();
        };

        // then create offer
        Debug_Log($"pc1:{client_id} create Offer then send it to {target_client_id}");
        var op = pc1.CreateOffer();
        yield return op;

        Debug_Log($"pc1:{client_id} createOffer ...");

        if (!op.IsError)
        {
            Debug_Log($"Offer from pc1:{client_id}, sdp is \n{op.Desc.sdp}");
            Debug_Log($"pc1:{client_id} setLocalDescription start");

            var desc = op.Desc;
            var op2 = pc1.SetLocalDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                //OnSetLocalSuccess(pc1);
                Debug_Log($"pc1:{client_id} SetLocalDescription complete");
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            // desc.sdp가 전송되어야 한다.
            SendOffer(op.Desc, target_client_id);
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    // 연결을 끊어버림
    public void Hangup()
    {
        RemoveAllConnection();
    }

    void OnSetLocalSuccess(RTCPeerConnection pc)
    {
        Debug_Log($"peer:{client_id}-{GetTargetClientID(pc)} SetLocalDescription complete");
    }

    void OnSetSessionDescriptionError(ref RTCError error)
    {
    }

    void OnSetRemoteSuccess(RTCPeerConnection pc)
    {
        Debug_Log($"peer:{client_id}-{GetTargetClientID(pc)} SetRemoteDescription complete");
    }

    void OnAddIceCandidateSuccess(RTCPeerConnection pc)
    {
        Debug_Log($"{GetTargetClientID(pc)} addIceCandidate success");
    }

    void OnAddIceCandidateError(RTCPeerConnection pc, RTCError error)
    {
        Debug_Log($"{GetTargetClientID(pc)} failed to add ICE Candidate: ${error}");
    }

    void OnCreateSessionDescriptionError(RTCError e)
    {

    }

    #endregion

    #region [PeerConnection]
    private class PeerConnData
    {
        public RTCPeerConnection peer_conn;
        public bool is_avaiable;                // connected까지 가면 true
        public RTCDataChannel data_channel;
        public RTCDataChannel remote_channel;
    }

    private Dictionary<string, PeerConnData> peer_conn_dic = new Dictionary<string, PeerConnData>();

    private PeerConnData RegisterPeer(RTCPeerConnection pc, string target_client_id)
    {
        // 항상 나(client_id) -> 상대방(target_client_id) 간의 이슈임
        var pcd = new PeerConnData()
        {
            peer_conn = pc,
            is_avaiable = false,
            data_channel = null,
            remote_channel = null,
        };

        peer_conn_dic.Add(target_client_id, pcd);
        return pcd;
    }

    private void RemoveAllConnection()
    {
        foreach (var kvp in peer_conn_dic)
        {
            var pcd = kvp.Value;
            if (pcd.peer_conn != null)
                pcd.peer_conn.Close();
            if (pcd.data_channel != null)
                pcd.data_channel = null;
            if (pcd.remote_channel != null)
                pcd.remote_channel = null;
        }

        peer_conn_dic.Clear();
    }

    string GetTargetClientID(RTCPeerConnection pc)
    {
        foreach (var kvp in peer_conn_dic)
        {
            if (kvp.Value.peer_conn == pc)
                return kvp.Key;
        }

        return "NOT_FOUND";
    }

    string FindTargetID(RTCPeerConnection pc)
    {
        foreach (var kvp in peer_conn_dic)
        {
            if (kvp.Value.peer_conn == pc)
                return kvp.Key;
        }

        return "NOT_FOUND";
    }

    // 사용하지 않은 함수
    //IEnumerator LoopGetStats()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(1f);

    //        if (!sendButton.interactable)
    //            continue;

    //        var op1 = pc1.GetStats();
    //        var op2 = pc2.GetStats();

    //        yield return op1;
    //        yield return op2;

    //        Debug_Log("pc1");
    //        foreach (var stat in op1.Value.Stats.Values)
    //        {
    //            Debug_Log(stat.Type.ToString());
    //        }
    //        Debug_Log("pc2");
    //        foreach (var stat in op2.Value.Stats.Values)
    //        {
    //            Debug_Log(stat.Type.ToString());
    //        }
    //    }
    //}

    #endregion

    #region [WebSocket & message parser from Signaling server]

    // 추가된 함수들
    public void Connect()
    {
        StartCoroutine(WebSocketCreate());
    }

    private IEnumerator WebSocketCreate()
    {
        try
        {
            m_socket = new WebSocketSharp.WebSocket(URL);
            m_socket.OnOpen += OnSocketOpen;
            m_socket.OnClose += OnSocketClose;
            m_socket.OnMessage += OnSocketReceiveMsg;
            m_socket.OnError += OnSocketError;
            m_socket.Log.Level = LogLevel.Error;

            status = WebSocketStatus.Ready;
            client_id = "N/A";

            if (m_socket == null || !m_socket.IsAlive)
            {
                _actions.Enqueue(() => { status = WebSocketStatus.Connecting; });
                m_socket.Connect();
            }

            yield break;
        }
        catch
        {
            Debug.LogError("# Error> fail to create websocket " + URL);
        }
    }

    public void Join(string room_id)
    {
        var payload = new JObject();
        payload.Add("room_id", room_id);

        var json = new JObject();
        json.Add("type", "join");
        json.Add("payload", payload);

        this.Send(json.ToString());
    }

    public void CloseSocket()
    {
        if (m_socket == null)
            return;
        if (m_socket.IsAlive)
            m_socket.Close();

        m_socket = null;
        client_id = "N/A";

        _actions.Enqueue(() =>
        {
            status = WebSocketStatus.Closed;
            socket_handler(status);
        });
    }

    private void OnSocketOpen(object sender, System.EventArgs e)
    {
        Debug_Log("### Open");

        _actions.Enqueue(() =>
        {
            status = WebSocketStatus.Connected;
            socket_handler(status);
        });
    }

    private void OnSocketClose(object sender, CloseEventArgs e)
    {
        Debug_Log("# Connection is closed: " + e.Reason);

        CloseSocket();
    }

    private void OnSocketError(object sender, ErrorEventArgs args)
    {
        Debug_Log("### Error: " + args.Message);

        // @todo
    }

    // 이 함수는 signaling server로 보내는겁니다
    private void Send(string msg)
    {
        m_socket.Send(Encoding.UTF8.GetBytes(msg));
    }

    // signaling server와 주고받는 메시지를 처리한다.
    private void OnSocketReceiveMsg(object sender, WebSocketSharp.MessageEventArgs e)
    {
        //Debug_Log("> " + e.Data);

        var packet = JsonConvert.DeserializeObject<Signaling.IncomingPacket>(e.Data);

        Debug_Log(string.Format("# packet: {0}, payload: {1}", packet.type, packet.payload));

        if (packet.type == "ping")
            return;

        if (packet.type == "notify-client-id")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseConnect>(payload_str);
            client_id = payload.client_id;

            Debug_Log(string.Format("My client id is - client_id:{0}", client_id));
        }

        if (packet.type == "new-client")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseNewClient>(payload_str);

            // check payload.client_id is same with client_id
            // if not, there is new client just joined.
            if (payload.client_id == client_id)
            {
                _actions.Enqueue(() =>
                {
                    // update ui
                    status = WebSocketStatus.Joined;
                    socket_handler(status);
                });
            }
            else
            {
                // send SDP offer to payload.client_id
                _actions.Enqueue(() =>
                {
                    StartCoroutine(ActionSendOfferToTarget(payload.client_id));
                });
            }

            Debug_Log(string.Format("New client is coming - client_id:{0}, index:{1}", payload.client_id, payload.index));
        }

        if (packet.type == "offer")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseWithSdp>(payload_str);
            Debug_Log(string.Format("Got offer from: client_id:{0}, index:{1}, sdp:{2}", payload.client_id, payload.index, payload.sdp));

            RTCSessionDescription desc = new RTCSessionDescription()
            {
                type = RTCSdpType.Offer,
                sdp = payload.sdp
            };

            _actions.Enqueue(() =>
            {
                StartCoroutine(ActionParseOffer(desc, payload.client_id));
            });
        }

        if (packet.type == "answer")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseWithSdp>(payload_str);
            Debug_Log(string.Format("Got answer: client_id:{0}, index:{1}, sdp:{2}", payload.client_id, payload.index, payload.sdp));

            RTCSessionDescription desc = new RTCSessionDescription()
            {
                type = RTCSdpType.Answer,
                sdp = payload.sdp
            };

            _actions.Enqueue(() =>
            {
                StartCoroutine(ActionParseAnswer(desc, payload.client_id));
            });
        }

        if (packet.type == "ice-candidate")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseIceCandidate>(payload_str);
            Debug_Log(string.Format("Got ice-candidate: {0}, {1}, {2}, {3}, {4}", payload.client_id, payload.index, payload.candidate, payload.sdp_mid, payload.sdp_index));

            _actions.Enqueue(() =>
            {
                StartCoroutine(ActionParseIceCandidate(payload.client_id, payload.candidate, payload.sdp_mid, payload.sdp_index));
            });
        }

        if (packet.type == "leave-client")
        {
            var bytes = Convert.FromBase64String(packet.payload);
            string payload_str = Encoding.Default.GetString(bytes);

            var payload = JsonConvert.DeserializeObject<Signaling.ParseWithSdp>(payload_str);
            Debug_Log(string.Format("Client leaved: {0}, {1}", payload.client_id, payload.sdp));
        }
    }

    // 무조건 나(client_id)가 보내는 것임
    private void SendOffer(RTCSessionDescription desc, string target_client_id)
    {
        Debug_Log($"pc1:{client_id} send offer to {target_client_id} with sdp:{desc.sdp}");

        SendSDP(desc, target_client_id);
    }

    // 무조건 나(client_id)가 보내는 것임
    private void SendAnswer(RTCSessionDescription desc, string target_client_id)
    {
        Debug_Log($"pc1:{client_id} send answer to {target_client_id} with sdp:{desc.sdp}");

        SendSDP(desc, target_client_id);
    }

    // 지정한 target_cliet_id에게 SDP를 보낸다.
    private void SendSDP(RTCSessionDescription desc, string target_client_id)
    {
        // @note - sdp는 어떤거든 signaling server는 개의치 않습니다. client끼리 검증할 내용임.
        var payload = new JObject();
        payload.Add("sdp", desc.sdp); // ex: "a=ice-ufrag:23Mj\r\n\r\na=ice-pwd:I5aYxsishC5pYTLiYmh+lh\r\n\r\na=ice-options:ice2,trickle");
        payload.Add("client_id", target_client_id);

        var json = new JObject();
        json.Add("type", (desc.type == RTCSdpType.Offer) ? "offer" : "answer");
        json.Add("payload", payload);

        this.Send(json.ToString());

        Debug_Log($"Ok, {client_id} send {desc.type.ToString()} to {target_client_id}");
    }

    private void SendICECandidate(RTCIceCandidate candidate, string target_client_id)
    {
        var payload = new JObject();

        payload.Add("client_id", target_client_id);
        // 이렇게 보내야 하나? sdp(candidate)만 보내도 충분할 것 같은데?
        payload.Add("candidate", candidate.Candidate);
        payload.Add("sdp_mid", candidate.SdpMid);
        payload.Add("sdp_index", candidate.SdpMLineIndex);

        var json = new JObject();
        json.Add("type", "ice-candidate");
        json.Add("payload", payload);

        this.Send(json.ToString());

        Debug_Log($"Ok, {client_id} send ice candidate to {target_client_id}");
    }


    // offer를 분석해서 answer를 대답한다
    private IEnumerator ActionParseOffer(RTCSessionDescription desc, string sender_client_id)
    {
        // desc.type으로 offer인지, answer인지 알 수 있다 
        var is_offer = desc.type == RTCSdpType.Offer;

        // 없던거면 새로 peer 등록
        if (!peer_conn_dic.ContainsKey(sender_client_id))
        {
            // new peer_conn!
            Debug_Log("GetSelectedSdpSemantics");
            var configuration = GetSelectedSdpSemantics();

            // create new peer-connection (ie. agent)
            var pc1 = new RTCPeerConnection(ref configuration);
            var pc1_data = RegisterPeer(pc1, sender_client_id);

            Debug_Log($"Created remote peer connection object {sender_client_id}");
            pc1.OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); }; // pc2OnIceCandidate;
            pc1.OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); }; // pc2OnIceConnectionChange;
            pc1.OnDataChannel = channel =>
            {
                pc1_data.remote_channel = channel;
                pc1_data.remote_channel.OnMessage = bytes =>
                {
                    Debug_Log($"> msg from {sender_client_id}: {System.Text.Encoding.UTF8.GetString(bytes)}");
                }; ;
            }; ;

            // 데이터용 채널 등록 for local
            RTCDataChannelInit conf = new RTCDataChannelInit();
            pc1_data.data_channel = pc1.CreateDataChannel("data", conf);
            pc1_data.data_channel.OnOpen = () =>
            {
                data_channel_open_handler();
            };
        }

        var pc2 = peer_conn_dic[sender_client_id];

        Debug_Log("pc2 setRemoteDescription start");
        var op2 = pc2.peer_conn.SetRemoteDescription(ref desc);
        Debug_Log("pc2 setRemoteDescription ...");
        yield return op2;
        Debug_Log("pc2 setRemoteDescription ... ...");
        if (!op2.IsError)
        {
            OnSetRemoteSuccess(pc2.peer_conn);
        }
        else
        {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }

        // 아직 LocalDescription 등록된 상황이 아니라면 answer를 만들어서 보낸다.
        if (is_offer)
        {
            Debug_Log("pc2 createAnswer start");
            // Since the 'remote' side has no media stream we need
            // to pass in the right constraints in order for it to
            // accept the incoming offer of audio and video.

            var op3 = pc2.peer_conn.CreateAnswer();
            yield return op3;
            if (!op3.IsError)
            {
                var desc2 = op3.Desc;

                Debug_Log($"Answer from pc2:\n{desc2.sdp}");
                Debug_Log("pc2 setLocalDescription start");
                var op = pc2.peer_conn.SetLocalDescription(ref desc2);
                yield return op;

                if (!op.IsError)
                {
                    OnSetLocalSuccess(pc2.peer_conn);
                }
                else
                {
                    var error = op.Error;
                    OnSetSessionDescriptionError(ref error);
                }

                SendAnswer(desc2, sender_client_id);
            }
            else
            {
                OnCreateSessionDescriptionError(op3.Error);
            }
        }
    }

    // answer을 분석해서 처리한다
    private IEnumerator ActionParseAnswer(RTCSessionDescription desc, string sender_client_id)
    {
        // desc.type으로 offer인지, answer인지 알 수 있다 
        var is_answer = desc.type == RTCSdpType.Answer;

        // 없던거면 새로 peer 등록
        if (!peer_conn_dic.ContainsKey(sender_client_id))
        {
            // new peer_conn!
            Debug_Log("GetSelectedSdpSemantics");
            var configuration = GetSelectedSdpSemantics();

            // create new peer-connection (ie. agent)
            var pc1 = new RTCPeerConnection(ref configuration);
            var pc1_data = RegisterPeer(pc1, sender_client_id);

            Debug_Log($"Created remote peer connection object {sender_client_id}");
            pc1.OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); }; // pc2OnIceCandidate;
            pc1.OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); }; // pc2OnIceConnectionChange;
            pc1.OnDataChannel = channel =>
            {
                pc1_data.remote_channel = channel;
                pc1_data.remote_channel.OnMessage = bytes =>
                {
                    Debug_Log($"> msg from {sender_client_id}: {System.Text.Encoding.UTF8.GetString(bytes)}");
                }; ;
            }; ;

            // 데이터용 채널 등록 for local
            RTCDataChannelInit conf = new RTCDataChannelInit();
            pc1_data.data_channel = pc1.CreateDataChannel("data", conf);
            pc1_data.data_channel.OnOpen = () =>
            {
                data_channel_open_handler();
            };
        }

        var pc2 = peer_conn_dic[sender_client_id];

        Debug_Log("pc2 setRemoteDescription start");
        var op2 = pc2.peer_conn.SetRemoteDescription(ref desc);
        Debug_Log("pc2 setRemoteDescription ...");
        yield return op2;
        Debug_Log("pc2 setRemoteDescription ... ...");
        if (!op2.IsError)
        {
            OnSetRemoteSuccess(pc2.peer_conn);
        }
        else
        {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    // ice-candidate 처리를 수행한다.
    private IEnumerator ActionParseIceCandidate(string sender_client_id, string candidate, string mid, int index)
    {
        var candidate_info = new RTCIceCandidateInit()
        {
            candidate = candidate,
            sdpMid = mid,
            sdpMLineIndex = index,
        };

        var candidate_inst = new RTCIceCandidate(candidate_info);

        // 없던거면 새로 peer 등록
        if (!peer_conn_dic.ContainsKey(sender_client_id))
        {
            // new peer_conn!
            Debug_Log("GetSelectedSdpSemantics");
            var configuration = GetSelectedSdpSemantics();

            // create new peer-connection (ie. agent)
            var pc1 = new RTCPeerConnection(ref configuration);
            var pc1_data = RegisterPeer(pc1, sender_client_id);

            Debug_Log($"Created remote peer connection object {sender_client_id}");
            pc1.OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); }; // pc2OnIceCandidate;
            pc1.OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); }; // pc2OnIceConnectionChange;
            pc1.OnDataChannel = channel =>
            {
                pc1_data.remote_channel = channel;
                pc1_data.remote_channel.OnMessage = bytes =>
                {
                    Debug_Log($"> msg from {sender_client_id}: {System.Text.Encoding.UTF8.GetString(bytes)}");
                };
            };

            // 데이터용 채널 등록 for local
            RTCDataChannelInit conf = new RTCDataChannelInit();
            pc1_data.data_channel = pc1.CreateDataChannel("data", conf);
            pc1_data.data_channel.OnOpen = () =>
            {
                data_channel_open_handler();
            };
        }

        var pc2 = peer_conn_dic[sender_client_id];
        if (!pc2.peer_conn.AddIceCandidate(candidate_inst))
        {
            Debug_Log($"fail to addIceCandidate from pc2:{GetTargetClientID(pc2.peer_conn)}");
            yield break;
        }
        else
        {
            Debug_Log($"ice-candidate: {candidate} is added");
        }
    }

    // data 채널로 메시지를 보낸다. byte[]등을 보내려면 아래 참고
    // - https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/datachannel.html
    public void SendMsg(string text_str)
    {
        // 모든 remote들에게 전달한다
        foreach (var kvp in peer_conn_dic)
        {
            if (!kvp.Value.is_avaiable)
                continue;

            var channel = kvp.Value.data_channel;
            channel.Send(text_str);
        }
    }

    // 디버그도 thread 이슈 때문에 queuing해야 한다.
    private void Debug_Log(string message)
    {
        _log_queue.Enqueue(message);
    }

    #endregion
}
