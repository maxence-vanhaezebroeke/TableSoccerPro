using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Discovery;
using FishNet.Managing;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class NetworkPlayerManager : MonoBehaviour
{
    [SerializeField]
    private NetworkPlayer _networkPlayerPrefab;

    protected List<NetworkPlayer> _spawnedPlayers;
    public List<NetworkPlayer> Players
    {
        get
        {
            return _spawnedPlayers;
        }
    }

    public bool IsServer
    {
        get
        {
            return _networkManager && _networkManager.IsServer;
        }
    }

    protected NetworkManager _networkManager;

    protected virtual void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _networkPlayerPrefab);
        _spawnedPlayers = new List<NetworkPlayer>();
        _networkManager = GetComponent<NetworkManager>();

        if (_networkManager && _networkManager.SceneManager)
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScene;
    }

    private void OnClientLoadedStartScene(NetworkConnection pNConnection, bool pAsServer)
    {
        if (!pAsServer)
            return;

        // As server
        NetworkPlayer lPlayer = Instantiate(_networkPlayerPrefab, Vector3.zero, _networkPlayerPrefab.transform.rotation);
        Server_OnPlayerSpawn(lPlayer);
        _networkManager.ServerManager.Spawn(lPlayer.gameObject, pNConnection);
    }

    protected virtual void Server_OnPlayerSpawn(NetworkPlayer pPlayer)
    {
        pPlayer.OnPlayerDespawn += Server_OnPlayerDespawn;
        _spawnedPlayers.Add(pPlayer);

        // If we've reached max player, stop listening to new clients
        if (_spawnedPlayers.Count >= GameMode.Instance.NumberOfPlayer)
        {
            InstanceFinder.NetworkManager.GetComponent<NetworkDiscovery>().StopAdvertisingServer();
        }
    }

    public NetworkPlayer GetPlayerById(int pId)
    {
        return _spawnedPlayers.Find(x => x.OwnerId == pId);
    }

    protected virtual void Server_OnPlayerDespawn(NetworkPlayer pPlayer)
    {
        pPlayer.OnPlayerDespawn -= Server_OnPlayerDespawn;
        _spawnedPlayers.Remove(pPlayer);
    }

    protected virtual void OnDestroy()
    {
        if (_networkManager && _networkManager.SceneManager)
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScene;
    }
}
