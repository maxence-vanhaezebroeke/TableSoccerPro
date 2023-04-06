using System;
using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class FNet_Players : MonoBehaviour
{
    public static List<Net_Player> _spawnedPlayers;
    public List<Net_Player> Players 
    {
        get
        {
            return _spawnedPlayers;
        }
    }

    private void Awake()
    {
        _spawnedPlayers = new List<Net_Player>();

        GetComponent<PlayerSpawner>().OnSpawned += OnPlayerSpawn;
    }

    private void OnPlayerSpawn(NetworkObject pNetworkObject)
    {
        Debug.Log("Player spawned!");
        Net_Player lPlayer = pNetworkObject.GetComponent<Net_Player>();
        if (lPlayer)
        {
            lPlayer.OnPlayerDespawn += OnPlayerDespawn;
            _spawnedPlayers.Add(lPlayer);
        }
        else
        {
            Debug.LogError("Player spawner shouldn't be spawning anything else than Net_Player.");
        }
    }

    private void OnPlayerDespawn(Net_Player pPlayer)
    {
        pPlayer.OnPlayerDespawn -= OnPlayerDespawn;
        _spawnedPlayers.Remove(pPlayer);
    }

    private void OnDestroy()
    {
        GetComponent<PlayerSpawner>().OnSpawned -= OnPlayerSpawn;
    }
}
