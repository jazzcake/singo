using Juice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

namespace Juice
{
    // define handle
    using JUICE_AGENT_HANDLE = System.UInt32;

    // define enums same like in juice.h
    public enum JUICE_ERR
    {
        SUCCESS = 0,
        INVALID = -1,       // invalid argument
        FAILED = -2,        // runtime error
        NOT_AVAIL = -3,     // element not available
    }

    public enum JUICE_STATE
    {
        DISCONNECTED = 0,
        GATHERING,
        CONNECTING,
        CONNECTED,
        COMPLETED,
        FAILED
    }

    public enum LOG_LEVEL
    {
        VERBOSE = 0,
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL,
        NONE
    }
}

public class LibJuice : MonoBehaviour
{
    // constants same in juice.h, c
    public const int JUICE_MAX_ADDRESS_STRING_LEN = 64;
    public const int JUICE_MAX_CANDIDATE_SDP_STRING_LEN = 256;
    public const int JUICE_MAX_SDP_STRING_LEN = 4096;

    public uint INVALID_HANDLE { get { unchecked { return (uint)-1; }  } }

    // in plugin_wrapper.h
    #region [Plugin extern]

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallbackOnStateChanged(uint handle, JUICE_STATE state, IntPtr user_ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallbackOnCandidate(uint handle, [MarshalAs(UnmanagedType.LPStr)] string sdp, IntPtr user_ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallbackOnGatheringDone(uint handle, IntPtr user_ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallbackOnRecv(uint handle, [MarshalAs(UnmanagedType.LPStr)] string data, uint size, IntPtr user_ptr);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_init(int agent_count);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern uint juice_wrapper_assign_agent();

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_set_stun(uint handle, [MarshalAs(UnmanagedType.LPStr)] string buffer, int port);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_set_turn(uint handle, int turn_server_index, 
        [MarshalAs(UnmanagedType.LPStr)] string host, int port, 
        [MarshalAs(UnmanagedType.LPStr)] string username, 
        [MarshalAs(UnmanagedType.LPStr)] string password
        );

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_local_port_range(uint handle, int begin_port, int end_port);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_set_callbacks(uint handle, 
        CallbackOnStateChanged fn_on_state_changed, 
        CallbackOnCandidate fn_on_candidate, 
        CallbackOnGatheringDone fn_on_gathering_done,
        CallbackOnRecv fn_on_recv);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_create_agent(uint handle);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_get_local_description(uint handle, [In, Out] byte[] buffer, uint size);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_set_remote_description(uint handle, [MarshalAs(UnmanagedType.LPStr)] string buffer);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_gather_candidates(uint handle);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_add_remote_candidate(uint handle, [MarshalAs(UnmanagedType.LPStr)] string sdp);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_set_remote_gathering_done(uint handle);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_get_selected_candidates(uint handle, 
        [In, Out] byte[] local, uint local_size, 
        [In, Out] byte[] remote, uint remote_size
        );

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_get_selected_addresses(uint handle, 
        [In, Out] byte[] local, uint local_size, 
        [In, Out] byte[] remote, uint remote_size
        );

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern int juice_wrapper_get_state(uint handle);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_destroy(uint handle);

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern void juice_wrapper_set_log_level(int level);

    // in random.h

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern uint juice_rand32();

#if UNITY_EDITOR
    [DllImport("juice", CharSet = CharSet.Ansi)]
#else
    [DllImport("__Internal", CharSet = CharSet.Ansi)]
#endif
    private static extern ulong juice_rand64();

#endregion

    // methods
#region [Methods]

    // Plugin 대응되는 함수들
    public void Init(int agent_count)
    {
        juice_wrapper_init(agent_count);
    }

    public uint AssignAgent()
    {
        var handle = juice_wrapper_assign_agent();
        if (handle == INVALID_HANDLE)
        {
            Debug.Log("Fail to assign agent. Check the number of agent or total agent count when initialized.");
            return INVALID_HANDLE;
        }

        return handle;
    }

    public void SetConfig_Stun(uint handle, string host, int port)
    {
        juice_wrapper_set_stun(handle, host, port);
    }

    public void SetConfig_Turn(uint handle, int turn_server_index, string host, int port, string username, string password)
    {
        juice_wrapper_set_turn(handle, turn_server_index, host, port, username, password);
    }

    public void SetConfig_LocalPortRange(uint handle, int begin_port, int end_port)
    {
        juice_wrapper_local_port_range(handle, begin_port, end_port);
    }

    public void SetConfig_Callback(uint handle, CallbackOnStateChanged fn_on_state_chaned, CallbackOnCandidate fn_on_candidate, CallbackOnGatheringDone fn_on_gathering_done, CallbackOnRecv fn_on_recv)
    {
        juice_wrapper_set_callbacks(handle, fn_on_state_chaned, fn_on_candidate, fn_on_gathering_done, fn_on_recv);
    }

    // Assign 이후 configuration이 종료되면 생성한다
    public bool CreateAgent(uint handle)
    {
        return (JUICE_ERR)juice_wrapper_create_agent(handle) == JUICE_ERR.SUCCESS;
    }

    public bool GetLocalDescription(uint handle, out string description)
    {
        var buffer = new byte[JUICE_MAX_SDP_STRING_LEN];
        var ret = (JUICE_ERR)juice_wrapper_get_local_description(handle, buffer, JUICE_MAX_SDP_STRING_LEN);

        if (ret != JUICE_ERR.SUCCESS)
        {
            description = string.Empty;
            return false;
        }

        description = Encoding.Default.GetString(buffer);
        return true;
    }

    public bool SetRemoteDescription(uint handle, string description)
    {
        var ret = (JUICE_ERR)juice_wrapper_set_remote_description(handle, description);
        if (ret != JUICE_ERR.SUCCESS)
            return false;

        return true;
    }

    public bool GatherCandidates(uint handle)
    {
        var ret = (JUICE_ERR)juice_wrapper_gather_candidates(handle);
        if (ret != JUICE_ERR.SUCCESS)
            return false;

        return true;
    }

    public bool AddRemoteCandidate(uint handle, string sdp)
    {
        var ret = (JUICE_ERR)juice_wrapper_add_remote_candidate(handle, sdp);
        if (ret != JUICE_ERR.SUCCESS)
            return false;

        return true;
    }

    public bool SetRemoteGatheringDone(uint handle)
    {
        var ret = (JUICE_ERR)juice_wrapper_set_remote_gathering_done(handle);
        if (ret != JUICE_ERR.SUCCESS)
            return false;

        return true;
    }

    public bool GetSelectedCandidates(uint handle, out string local, out string remote)
    {
        var buffer_local = new byte[JUICE_MAX_CANDIDATE_SDP_STRING_LEN];
        var buffer_remote = new byte[JUICE_MAX_CANDIDATE_SDP_STRING_LEN];

        var ret = (JUICE_ERR)juice_wrapper_get_selected_candidates(handle, buffer_local, JUICE_MAX_CANDIDATE_SDP_STRING_LEN, buffer_remote, JUICE_MAX_CANDIDATE_SDP_STRING_LEN);

        local = Encoding.Default.GetString(buffer_local);
        remote = Encoding.Default.GetString(buffer_remote);

        return (ret == JUICE_ERR.SUCCESS);
    }

    public bool GetSelectedAddress(uint handle, out string local, out string remote)
    {
        var buffer_local = new byte[JUICE_MAX_CANDIDATE_SDP_STRING_LEN];
        var buffer_remote = new byte[JUICE_MAX_CANDIDATE_SDP_STRING_LEN];

        var ret = (JUICE_ERR)juice_wrapper_get_selected_addresses(handle, buffer_local, JUICE_MAX_CANDIDATE_SDP_STRING_LEN, buffer_remote, JUICE_MAX_CANDIDATE_SDP_STRING_LEN);

        local = Encoding.Default.GetString(buffer_local);
        remote = Encoding.Default.GetString(buffer_remote);

        return (ret == JUICE_ERR.SUCCESS);
    }

    public JUICE_STATE GetState(uint handle)
    {
        var ret = juice_wrapper_get_state(handle);
        return (JUICE_STATE)ret;
    }

    public void Destroy(uint handle)
    {
        juice_wrapper_destroy(handle);
    }

    public void SetLogLevel(LOG_LEVEL level)
    {
        juice_wrapper_set_log_level((int)level);
    }

#endregion

    // reference
#region [References - Marshalling]

    // @note - https://bravenewmethod.com/2017/10/30/unity-c-native-plugin-examples/ 참고

    // Marshal "char*" in C#
    // https://stackoverflow.com/questions/162897/marshal-char-in-c-sharp

    // How to get IntPtr from byte[] in C#
    // https://stackoverflow.com/questions/537573/how-to-get-intptr-from-byte-in-c-sharp

    // How to transfer function pointer in C# to in native DLL
    // https://stackoverflow.com/questions/43226928/how-to-pass-function-pointer-from-c-sharp-to-a-c-dll
    // https://forum.unity.com/threads/making-calls-from-c-to-c-with-il2cpp-instead-of-mono_runtime_invoke.295697/

    // @note - 아래는 위 방식이 아니라 IntPtr형태로 string buffer를 전달받는 방식
    //public JUICE_ERR GetLocalDescription(juice_agent_t* agent, out string descripion_str)
    //{
    //    var buffer = new byte[JUICE_MAX_SDP_STRING_LEN];
    //    var pinned_buffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
    //    IntPtr pointer = pinned_buffer.AddrOfPinnedObject();
    //    var ret = (JUICE_ERR)juice_get_local_description(agent, pointer, JUICE_MAX_SDP_STRING_LEN);

    //    if (ret == JUICE_ERR.SUCCESS)
    //        descripion_str = Encoding.ASCII.GetString(buffer);

    //    pinned_buffer.Free();
    //    return ret;
    //}

#endregion
}
