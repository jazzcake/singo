using AOT;
using Juice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static LibJuice;

public class Connectivity : MonoBehaviour
{
    private static LibJuice plugin;
    private static uint handle1;
    private static uint handle2;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        plugin = GetComponent<LibJuice>();

        // libjuice 내의 test_connectivity() 함수를 동일하게 구현, 해당 소스 찾아보면 주석 확인 가능

        plugin.SetLogLevel(Juice.LOG_LEVEL.VERBOSE);
        plugin.Init(5);

        handle1 = plugin.AssignAgent();
        plugin.SetConfig_Stun(handle1, "stun.l.google.com", 19302);
        plugin.SetConfig_LocalPortRange(handle1, 0, 0);
        plugin.SetConfig_Turn(handle1, 0, "openrelay.metered.ca", 80, "openrelayproject", "openrelayproject");
        plugin.SetConfig_Callback(handle1, OnStateChanged1, OnCandidate1, OnGatheringDone1, OnRecv1);
        if (plugin.CreateAgent(handle1) == false)
        {
            Debug.LogError("Fail to create agent1");
            yield break;
        }

        handle2 = plugin.AssignAgent();
        plugin.SetConfig_Stun(handle2, "stun.l.google.com", 19302);
        plugin.SetConfig_LocalPortRange(handle2, 60000, 61000);
        plugin.SetConfig_Callback(handle2, OnStateChanged2, OnCandidate2, OnGatheringDone2, OnRecv2);
        if (plugin.CreateAgent(handle2) == false)
        {
            Debug.LogError("Fail to create agent2");
            yield break;
        }

        string sdp1;
        if (!plugin.GetLocalDescription(handle1, out sdp1))
        {
            Debug.LogError("Fail to get local description for " + handle1);
        }
        else
        {
            Debug.LogFormat("Local description1: {0}", sdp1);
        }

        plugin.SetRemoteDescription(handle2, sdp1);

        string sdp2;
        if (!plugin.GetLocalDescription(handle2, out sdp2))
        {
            Debug.LogError("Fail to get local description for " + handle2);
        }
        else
        {
            Debug.LogFormat("Local description2: {0}", sdp2);
        }

        plugin.SetRemoteDescription(handle1, sdp2);

        Debug.Log("Ok, SDP setting done respectively");


        plugin.GatherCandidates(handle1);
        yield return new WaitForSeconds(2);

        plugin.GatherCandidates(handle2);
        yield return new WaitForSeconds(2);

        // connection should be finished

        var state1 = plugin.GetState(handle1);
        var state2 = plugin.GetState(handle2);
        bool success = (state1 == Juice.JUICE_STATE.COMPLETED && state2 == Juice.JUICE_STATE.COMPLETED);

        string local, remote;
        if (success &= plugin.GetSelectedCandidates(handle1, out local, out remote))
        {
            Debug.LogFormat("Local candidate 1: {0}", local);
            Debug.LogFormat("Remote candidate 1: {0}", remote);
            if ((local.IndexOf("typ host") == -1 && local.IndexOf("typ prflx") == -1) ||
                (remote.IndexOf("typ host") == -1 && remote.IndexOf("typ prflx") == -1))
                success = false;
        }

        if (success &= plugin.GetSelectedCandidates(handle2, out local, out remote))
        {
            Debug.LogFormat("Local candidate 2: {0}", local);
            Debug.LogFormat("Remote candidate 2: {0}", remote);
            if ((local.IndexOf("typ host") == -1 && local.IndexOf("typ prflx") == -1) ||
                (remote.IndexOf("typ host") == -1 && remote.IndexOf("typ prflx") == -1))
                success = false;
        }

        // get address
        string local_addr, remote_addr;
        if (success &= plugin.GetSelectedAddress(handle1, out local_addr, out remote_addr))
        {
            Debug.LogFormat("Local address 1: {0}", local_addr);
            Debug.LogFormat("Remote address 1: {0}", remote_addr);
        }
        if (success &= plugin.GetSelectedAddress(handle2, out local_addr, out remote_addr))
        {
            Debug.LogFormat("Local address 2: {0}", local_addr);
            Debug.LogFormat("Remote address 2: {0}", remote_addr);
        }

        plugin.Destroy(handle1);
        plugin.Destroy(handle2);

        yield return new WaitForSeconds(2);

        if (success)
        {
            Debug.Log("Success");
        }
        else
        {
            Debug.Log("Failure");
        }
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnStateChanged))]
    public static void OnStateChanged1(uint handle, JUICE_STATE state, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnStateChanged1 {0}, {1}", handle, state.ToString());
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnCandidate))]
    public static void OnCandidate1(uint handle, [MarshalAs(UnmanagedType.LPStr)] string sdp, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnCandidate1 {0}, {1}", handle, sdp);
        plugin.AddRemoteCandidate(handle2, sdp);
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnGatheringDone))]
    public static void OnGatheringDone1(uint handle, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnGatheringDone1 {0}", handle);
        plugin.SetRemoteGatheringDone(handle2);
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnRecv))]
    public static void OnRecv1(uint handle, [MarshalAs(UnmanagedType.LPStr)] string data, uint size, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnRecv1 {0}, {1}", handle, data);
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnStateChanged))]
    public static void OnStateChanged2(uint handle, JUICE_STATE state, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnStateChanged2 {0}, {1}", handle, state.ToString());
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnCandidate))]
    public static void OnCandidate2(uint handle, [MarshalAs(UnmanagedType.LPStr)] string sdp, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnCandidate2 {0}, {1}", handle, sdp);
        plugin.AddRemoteCandidate(handle1, sdp);
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnGatheringDone))]
    public static void OnGatheringDone2(uint handle, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnGatheringDone2 {0}", handle);
        plugin.SetRemoteGatheringDone(handle1);
    }

    [MonoPInvokeCallback(typeof(LibJuice.CallbackOnRecv))]
    public static void OnRecv2(uint handle, [MarshalAs(UnmanagedType.LPStr)] string data, uint size, IntPtr user_ptr)
    {
        Debug.LogFormat("> OnRecv2 {0}, {1}", handle, data);
    }

}
