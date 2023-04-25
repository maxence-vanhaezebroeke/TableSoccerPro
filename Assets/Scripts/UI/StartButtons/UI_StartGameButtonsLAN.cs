using System;
using System.Collections;
using System.Net;
using FishNet;
using FishNet.Discovery;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

// Dictates Start game buttons logic for creating LAN game 
public class UI_StartGameButtonsLAN : UI_StartGameButtonsNetwork
{
    // The amount of time client will search for server
    private float _networkDiscoveryTime = 10.0f;

    protected override void SetTransport()
    {
        InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
    }

    protected override void ReturnButton_OnClick()
    {
        // In order to stop connection properly, I have to set client transport before. In case player only wants to click return,
        // I have to do this to handle every case
        SetTransport();
        InstanceFinder.ClientManager.StopConnection();

        base.ReturnButton_OnClick();
    }

    protected override void StartRemoteClient()
    {
        // As fully client, we need to use NetworkDiscovery to find server on LAN
        NetworkDiscovery lNetDiscovery = InstanceFinder.NetworkManager.GetComponent<NetworkDiscovery>();
        // Binding response to server found callback
        lNetDiscovery.ServerFoundCallback += NetworkDiscovery_ServerFound;
        // Execute research
        lNetDiscovery.StartSearchingForServers();
        // If research is not working after coroutine, stop it.
        StartCoroutine(nameof(StopSearchingForServers));
    }

    protected override void StartHostClient()
    {
        // Join the already set server in local
        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    protected override void StartServer()
    {
        if (!InstanceFinder.TransportManager.GetTransport<Multipass>().StartConnection(true, 0))
            OnServerConnectionError();

        // Waiting for server to be started
        InstanceFinder.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
    }

    private IEnumerator StopSearchingForServers()
    {
        yield return new WaitForSeconds(_networkDiscoveryTime);
        InstanceFinder.NetworkManager.GetComponent<NetworkDiscovery>().StopSearchingForServers();
        OnClientConnectionError();
    }

    private void NetworkDiscovery_ServerFound(IPEndPoint pIPEndPoint)
    {
        Tugboat lTugboat = InstanceFinder.TransportManager.GetTransport<Tugboat>();
        lTugboat.SetClientAddress(pIPEndPoint.Address.ToString());

        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }


    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs pServerConnectionState)
    {
        if (pServerConnectionState.ConnectionState == LocalConnectionState.Started)
        {
            InstanceFinder.NetworkManager.GetComponent<NetworkDiscovery>().StartAdvertisingServer();
            OnServerStartedAction.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager)
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        StopAllCoroutines();
    }
}
