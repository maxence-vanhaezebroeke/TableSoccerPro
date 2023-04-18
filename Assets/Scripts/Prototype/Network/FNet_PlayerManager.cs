using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class FNet_PlayerManager : MonoBehaviour
{
    [SerializeField]
    private Net_Player _playerPrefab;

    private static List<Net_Player> _spawnedPlayers;
    public List<Net_Player> Players
    {
        get
        {
            return _spawnedPlayers;
        }
    }

    private NetworkManager _networkManager;

    private void Awake()
    {
        _spawnedPlayers = new List<Net_Player>();
        _networkManager = GetComponent<NetworkManager>();
    
        if (_networkManager && _networkManager.SceneManager)
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScene;
    }

    private void OnClientLoadedStartScene(NetworkConnection pNConnection, bool pAsServer)
    {
        if (!pAsServer)
            return;

        Net_Player lPlayer = Instantiate(_playerPrefab, Vector3.zero, _playerPrefab.transform.rotation);
        Server_OnPlayerSpawn(lPlayer);
        _networkManager.ServerManager.Spawn(lPlayer.gameObject, pNConnection);
    }

    private void Server_OnPlayerSpawn(Net_Player pPlayer)
    {
        if (pPlayer)
        {
            pPlayer.OnPlayerDespawn += OnPlayerDespawn;
            _spawnedPlayers.Add(pPlayer);
        }
        else
        {
            Debug.LogError("Player spawned but is already null... His existence lasted less than a frame...");
        }
    }

    public Net_Player GetPlayerById(int pId)
    {
        return _spawnedPlayers.Find(x => x.OwnerId == pId);
    }

    private void OnPlayerDespawn(Net_Player pPlayer)
    {
        pPlayer.OnPlayerDespawn -= OnPlayerDespawn;
        _spawnedPlayers.Remove(pPlayer);
    }

    private void OnDestroy()
    {
        if (_networkManager && _networkManager.SceneManager)
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScene;
    }
}
