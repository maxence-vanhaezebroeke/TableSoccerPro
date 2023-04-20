using System;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class GameState : Singleton<GameState>
{
    private string _joinCode = null;
    public string JoinCode
    {
        get { return _joinCode; }
        set
        {
            SetJoinCode(value);
        }
    }

    public bool HasJoinCode
    {
        get
        {
            return _joinCode != null;
        }
    }

    private NetworkManager _networkManager;

    protected override void Awake()
    {
        base.Awake();
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
    }

    private void SetJoinCode(string pNewJoinCodeValue)
    {
        _joinCode = pNewJoinCodeValue;
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs pClientConnectionState)
    {
        if (pClientConnectionState.ConnectionState == LocalConnectionState.Stopping)
        {
            // Stopping the network manager means that game state will be destroyed with it
            _isBeingDestroyed = true;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_networkManager.ClientManager)
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }
}
