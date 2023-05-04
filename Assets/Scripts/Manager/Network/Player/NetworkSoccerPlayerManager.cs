using FishNet.Connection;
using FishNet.Discovery;
using UnityEngine;

// Handle soccer player list, and interaction with game flow
public class NetworkSoccerPlayerManager : NetworkPlayerManager
{
    private SoccerGameManager _gameManager;

    private void InitializeGameManager()
    {
        // If GameManager, it is already initialized, nothing to do here.
        if (_gameManager)
            return;

        // Retrieving the correct GameManager (two or four player)
        _gameManager = GameMode.Instance.GameManager;

        // Asking him to spawn field and ball
        // TODO: I think I could set a scene with an already existing field, and ball,
        // But since I'm not spawning the GameManager with the scene, I cannot serialize them. (maybe by finding them?)
        // I'm not fully satisfied with this, I'll rework it later (because I'm sure I can)
        _gameManager.InstantiateField();
        _gameManager.InstantiateBall();
    }

    protected void Update()
    {
        if (IsServer && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            // CTRL + R
            _gameManager?.ResetBallLocation();
        }
    }

    // Server function : logic will only be run server side.
    protected override void Server_OnPlayerSpawn(NetworkPlayer pPlayer)
    {
        base.Server_OnPlayerSpawn(pPlayer);
        if (pPlayer is NetworkSoccerPlayer lSoccerPlayer)
        {
            lSoccerPlayer.OnPlayerIsReady += NetworkSoccerPlayer_OnPlayerIsReady;
            lSoccerPlayer.OnPlayerStarted += NetworkSoccerPlayer_OnPlayerStarted;
            lSoccerPlayer.OnPlayerFieldSideChanged += NetworkSoccerPlayer_OnPlayerFieldSideChanged;
            lSoccerPlayer.OnPlayerSoccerBarAdded += NetworkSoccerPlayer_OnPlayerSoccerBarAdded;
        }
    }

    private void NetworkSoccerPlayer_OnPlayerSoccerBarAdded(NetworkSoccerPlayer pSoccerPlayer)
    {
        if (CanPlayerStartGame(pSoccerPlayer))
        {
            pSoccerPlayer.SetPlayerReady();
        }
    }

    private void NetworkSoccerPlayer_OnPlayerFieldSideChanged(NetworkSoccerPlayer pSoccerPlayer, Net_SoccerField.FieldSide pNewFieldSide)
    {
        // When field side changes, server needs to put player at the correct location & rotation.
        Transform lFieldSideTransform = _gameManager.GetSideTransform(pNewFieldSide);
        pSoccerPlayer.All_SetPlayerPositionAndRotation(lFieldSideTransform.position, lFieldSideTransform.rotation);
    }

    private void NetworkSoccerPlayer_OnPlayerIsReady(NetworkSoccerPlayer pSoccerPlayer)
    {
        if (IsEveryPlayerReady())
        {
            StartGame();
        }
    }

    private void NetworkSoccerPlayer_OnPlayerStarted(NetworkSoccerPlayer pSoccerPlayer)
    {
        bool lIsHost = pSoccerPlayer.Owner.IsHost;

        // On host, instantiate game logic once!
        if (lIsHost)
        {
            InitializeGameManager();
        }

        // For every player, we need to set his field side, and spawn him soccer bars
        pSoccerPlayer.Server_SetFieldSide(_gameManager.GetRandomFieldSide());
        foreach (NetworkSoccerBar lSoccerBar in _gameManager.Server_InstantiateSoccerBars(pSoccerPlayer.FieldSide, pSoccerPlayer.Owner))
        {
            pSoccerPlayer.AddSoccerBar(lSoccerBar);

            // If we're spaning soccer bars for a remote player, send him the soccer bar !
            if (lIsHost == false)
                pSoccerPlayer.AddSoccerBarTargetRpc(pSoccerPlayer.Owner, lSoccerBar);
        }
    }

    // Server function : logic will only be run server side.
    protected override void Server_OnPlayerDespawn(NetworkPlayer pPlayer)
    {
        if (pPlayer is NetworkSoccerPlayer lSoccerPlayer)
        {
            lSoccerPlayer.OnPlayerIsReady -= NetworkSoccerPlayer_OnPlayerIsReady;
            lSoccerPlayer.OnPlayerStarted -= NetworkSoccerPlayer_OnPlayerStarted;
            lSoccerPlayer.OnPlayerFieldSideChanged -= NetworkSoccerPlayer_OnPlayerFieldSideChanged;
            lSoccerPlayer.OnPlayerSoccerBarAdded -= NetworkSoccerPlayer_OnPlayerSoccerBarAdded;
        }

        base.Server_OnPlayerDespawn(pPlayer);
    }

    private bool CanPlayerStartGame(NetworkSoccerPlayer pPlayer)
    {
        return _gameManager.NumberOfBarsRequired() == pPlayer.SoccerBarCount;
    }

    private bool IsEveryPlayerReady()
    {
        // First, we're checking if the number of player is good enough
        if (_spawnedPlayers.Count < GameMode.Instance.NumberOfPlayer)
        {
            Debug.Log("Not enough player connected - waiting for more.");
            return false;
        }

        // Then, we're checking if there is enough soccer player ready to play

        int lIgnoredPlayer = 0;
        foreach (NetworkPlayer lPlayer in _spawnedPlayers)
        {
            if (lPlayer is NetworkSoccerPlayer lSoccerPlayer)
            {
                if (lSoccerPlayer.IsPlayerReady == false)
                {
                    Debug.Log("Player " + lSoccerPlayer.name + " is not ready.");
                    return false;
                }
            }
            else
            {
                Debug.Log("Player is not a soccer player - add it as an ignored player for player ready count.");
                lIgnoredPlayer++;
            }
        }

        if (_spawnedPlayers.Count - lIgnoredPlayer < GameMode.Instance.NumberOfPlayer)
        {
            Debug.Log("Not enough NetworkSoccerPlayer connected - waiting for more...");
            return false;
        }

        Debug.Log("Game is ready !");
        // Every client has been checked - they all can start the game
        return true;
    }

    private void StartGame()
    {
        // When game starts, no more players are expected to come.
        if (_networkManager.GetComponent<NetworkDiscovery>() is NetworkDiscovery lNetDiscovery)
            lNetDiscovery.StopAdvertisingServer();

        // Starting a game means that maybe players were waiting for other players, so they played
        // We need to reset game for everyone to play from 0!
        ResetGame();
    }

    private void ResetGame()
    {
        _gameManager.ResetGame();

        foreach (NetworkPlayer lPlayer in _spawnedPlayers)
        {
            if (lPlayer is NetworkSoccerPlayer lSoccerPlayer)
            {
                lSoccerPlayer.Server_ResetGame();
            }
        }
    }
}
