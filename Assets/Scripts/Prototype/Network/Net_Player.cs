using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet;
using System;
using FishNet.Transporting.Multipass;
using System.Linq;
using FishNet.Discovery;

public class Net_Player : NetworkBehaviour
{
    public Action<Net_Player> OnPlayerDespawn;

    [SerializeField]
    private Camera _playerCamera;

    [SerializeField]
    private UI_PlayerControls _playerControlsPrefab;
    [SerializeField]
    private UI_JoinCode _joinCodePrefab;

    private UI_PlayerControls _playerControls;
    private UI_JoinCode _joinCode;

    // Becomes true when player gets enough bar required to play (ready to play)
    [SyncVar(OnChange = nameof(OnIsPlayerReadyChanged))]
    private bool _isPlayerReady;
    public bool IsPlayerReady
    {
        get { return _isPlayerReady; }
    }

    public new int OwnerId
    {
        get
        {
            return NetworkObject.OwnerId;
        }
    }

    // Reference to GameManager. This variable should be null for clients
    // (game manager is only relevant on server)
    private Net_GameManager _gameManager;

    // List of soccer bar we own (we will be able to control during gameplay)
    private List<Net_SoccerBar> _soccerBars;

    // Controlled soccer bar index. Default -1, will be set when game starts and StartingSoccerBar is chosen.
    private int _controlledSoccerBarIndex = -1;

    [SyncVar(OnChange = nameof(OnFieldSideChanged))]
    private Net_SoccerField.FieldSide _fieldSide = Net_SoccerField.FieldSide.None;

    protected void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _joinCodePrefab);
        UtilityLibrary.ThrowIfNull(this, _playerControlsPrefab);
        UtilityLibrary.ThrowIfNull(this, _playerCamera);
    }

    // Function called once, for every player spawned
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // If we are the owner
        if (base.Owner.IsLocalClient)
        {
            ShowControlsAndJoinCode();
            _playerCamera.gameObject.SetActive(true);
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (base.IsOwner)
            DestroyControls();
    }

    public override void OnDespawnServer(NetworkConnection connection)
    {
        base.OnDespawnServer(connection);
        // Used for FNet_PlayerManager, to reference every connected player (server-side)
        OnPlayerDespawn?.Invoke(this);
    }

    // Function called on owning client, as it spawns his net_player from the server
    // NOTE: for host, this will be called too ! (on the client instance)
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsServer)
        {
            Server_InitializeGameManager();
            // Owning server start
            if (base.IsOwner)
            {
                // Initialize ball, field and local bars
                Server_OnOwningStart();
            }
            // Non-owning server start
            else
            {
                // Initialize remote bars
                Server_OnNonOwningStart();
            }
        }
        // Client start
        else
        {
            if (_fieldSide != Net_SoccerField.FieldSide.None)
                // TODO: Ask for server to tell us correct field side info about this player!
                return;
        }
    }

    private void Server_OnOwningStart()
    {
        Debug.Log("<color=#00000><i>On server owning start</i></color>");

        // Retrieve game manager for this game, spawn field & ball.
        _gameManager.Server_InstantiateField();
        _gameManager.Server_InstantiateBall();

        // Place server random side of the field
        Server_SetRandomFieldSide();

        // Server spawns its own bars
        _gameManager.Server_InstantiateSoccerBars(_fieldSide, base.LocalConnection);
    }

    // Triggered server-side when the player is spawned, and the server is not the owner 
    private void Server_OnNonOwningStart()
    {
        // Place client random side of the field
        Server_SetRandomFieldSide();

        // Instantiate soccer bar for remote player
        _gameManager.Server_InstantiateSoccerBars(_fieldSide, base.Owner);
    }

    private void OnIsPlayerReadyChanged(bool pPrevious, bool pCurrent, System.Boolean pAsServer)
    {
        // if owner
        if (IsOwner)
        {
            // Order soccer bars, & possess the correct one !
            OrderSoccerBar(_soccerBars);
            PossessStartingBar();
            // We're in game "mode", so hide cursor
            GameMode.Instance.SetCursorVisibility(false);
        }

        if (IsServer)
        {
            // Set player side of every soccer bar that we spawned
            if (_fieldSide == Net_SoccerField.FieldSide.Red)
                Server_SetRedSide();
            else
                Server_SetBlueSide();

            // Checking for every player to be ready
            if (Server_CanEveryPlayerStartGame())
            {
                // At this point, server and clients should have the good number of bar spawned
                // We start !
                Debug.Log("Starting the game !");

                // Do starting game things
                Server_StartGame();
            }
            else
            {
                Debug.Log("Not starting the game yet : not fulfilling every condition.");
            }
        }
    }

    void Server_SetRandomFieldSide()
    {
        if (!IsServer)
            return;

        _fieldSide = _gameManager.GetRandomFieldSide();
    }

    private void OnFieldSideChanged(Net_SoccerField.FieldSide pOldFieldSide, Net_SoccerField.FieldSide pNewFieldSide, System.Boolean pAsServer)
    {
        if (!pAsServer)
            return;

        // TODO: check that this works??
        Transform lFieldSideTransform = _gameManager.GetSideTransform(_fieldSide);
        All_SetClientSide(lFieldSideTransform.position, lFieldSideTransform.rotation);
    }

    [ObserversRpc]
    private void All_SetClientSide(Vector3 pPosition, Quaternion pRotation)
    {
        transform.position = pPosition;
        transform.rotation = pRotation;
    }

    private void Server_InitializeGameManager()
    {
        if (!IsServer)
            return;

        if (!Net_GameManager.Instance)
        {
            Debug.LogError("Net Game Manager not instantiated - it should be as singleton. Is it on the scene?");
            return;
        }
        _gameManager = Net_GameManager.Instance;
    }

    private void ShowControlsAndJoinCode()
    {
        if (!_playerControls)
        {
            _playerControls = Instantiate(_playerControlsPrefab);
        }
        else
        {
            _playerControls.gameObject.SetActive(true);
        }

        if (PlayerState.Instance.HasJoinCode)
        {
            if (!_joinCode)
                _joinCode = Instantiate(_joinCodePrefab);
            else
                _joinCode.gameObject.SetActive(true);
        }
    }

    private void DestroyControls()
    {
        if (_playerControls)
            Destroy(_playerControls);
    }

    // Determines if a player can start the game or not
    // i.e. : a player has enough bars to play !
    bool CanStartGame(int pNumberOfBars)
    {
        return pNumberOfBars == _soccerBars.Count;
    }

    // Server checks every client if they can start the game (see above)
    bool Server_CanEveryPlayerStartGame()
    {
        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_PlayerManager>().Players;

        // First, we're checking if the number of player is good enough
        if (lPlayers.Count < GameMode.Instance.NumberOfPlayer)
        {
            Debug.Log("Not enough player connected - waiting for more...");
            return false;
        }

        // Then, we're checking if every connected client is ready to play

        int lIgnoredPlayer = 0;
        // Foreach connected clients
        foreach (Net_Player lPlayer in lPlayers)
        {
            if (!lPlayer)
            {
                Debug.Log("PlayerObject is not a lNetPlayer - he will not be taken into ReadyToStartGame account");
                lIgnoredPlayer++;
            }
            else if (!lPlayer.IsPlayerReady)
            {
                Debug.Log("Player " + lPlayer.OwnerId + " is not ready...");
                return false;
            }
        }

        if (lPlayers.Count - lIgnoredPlayer < GameMode.Instance.NumberOfPlayer)
        {
            Debug.Log("Not enough lNetPlayer connected - waiting for more...");
            return false;
        }

        Debug.Log("Game is ready !");
        // Every client has been checked - they all can start the game
        return true;
    }

    public void AddSoccerBar(Net_SoccerBar pSoccerBar)
    {
        if (_soccerBars == null)
        {
            _soccerBars = new List<Net_SoccerBar>();
        }

        Debug.Log("Yay ! Adding soccer bar : "
            + pSoccerBar.GetComponent<NetworkObject>().ObjectId
            + " of owner : "
            + GetComponent<NetworkObject>().OwnerId
            + " - Soccer bar has : " + pSoccerBar.NumberOfPlayers() + " players.");

        _soccerBars.Add(pSoccerBar);

        // If is only owner
        if (IsOwner)
        {
            // owning server
            if (IsServer)
            {
                // Server checks for HIS player to be ready
                Server_OnSoccerBarAdded();
            }
            // owning client
            else
            {
                // Client checks for HIS player to be ready
                OnSoccerBarAddedClient();
            }
        }
    }

    void OnSoccerBarAddedClient()
    {
        OnSoccerBarAddedServerRpc();
    }

    [ServerRpc]
    void OnSoccerBarAddedServerRpc()
    {
        OnSoccerBarAddedRpc(base.Owner, _gameManager.NumberOfBarsRequired());
    }

    [TargetRpc]
    void OnSoccerBarAddedRpc(NetworkConnection pNetworkConnection, int pNumberOfBars)
    {
        if (!IsOwner)
        {
            Debug.LogWarning("Function should only be triggered on owning client. Returning.");
            return;
        }

        if (CanStartGame(pNumberOfBars))
        {
            PlayerIsReadyServerRpc();
        }
        else
        {
            Debug.Log("Player is not ready for now...");
        }
    }

    [ServerRpc]
    private void PlayerIsReadyServerRpc()
    {
        _isPlayerReady = true;
    }

    void Server_OnSoccerBarAdded()
    {
        if (!IsServer)
            return;

        // Server checks if he's ready
        if (CanStartGame(_gameManager.NumberOfBarsRequired()))
        {
            _isPlayerReady = true;
        }
        else
        {
            Debug.Log("Player is not ready for now...");
            return;
        }
    }

    void Server_StartGame()
    {
        if (!IsServer)
            return;

        // Start game for every connected Net_Player !
        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_PlayerManager>().Players;
        foreach (Net_Player lPlayer in lPlayers)
        {
            if (lPlayer)
            {
                // Start match for this player
                lPlayer.Server_OnGameStart();
            }
        }
    }

    private void Server_OnGameStart()
    {
        if (!IsServer)
            return;

        // When game starts, no more players are expected to come.
        if (NetworkManager.GetComponent<NetworkDiscovery>() is NetworkDiscovery lNetDiscovery)
            lNetDiscovery.StopAdvertisingServer();

        // Starting a game means that maybe players were waiting for other players, so they played
        // We need to reset game for everyone to play from 0!
        Server_ResetGame();
    }

    private void Server_ResetGame()
    {
        if (!IsServer)
            return;

        _gameManager.ResetGame();

        foreach (Net_SoccerBar lSoccerBar in _soccerBars)
        {
            lSoccerBar.ResetGame();
        }
    }

    // Synchronizing player to be red side (from server)
    private void Server_SetRedSide()
    {
        if (!IsServer)
            return;

        foreach (Net_SoccerBar lSoccerBar in _soccerBars)
        {
            lSoccerBar.Server_SetRedSide();
        }
    }

    private void Server_SetBlueSide()
    {
        if (!IsServer)
            return;

        foreach (Net_SoccerBar lSoccerBar in _soccerBars)
        {
            lSoccerBar.Server_SetBlueSide();
        }
    }

    public void PossessStartingBar()
    {
        int lNumberOfBars = _soccerBars.Count;
        Debug.Log("Number of bars : " + lNumberOfBars);
        switch (lNumberOfBars)
        {
            case 1:
                _soccerBars[0].Possess();
                _controlledSoccerBarIndex = 0;
                break;
            case 2:
                // If one of our two bars are defense bar, it means that player is in defense!
                // Defense : possess two-player bar. Attack : possess five-player bar
                if (_soccerBars[0].NumberOfPlayers() == 1 || _soccerBars[0].NumberOfPlayers() == 2)
                {
                    for (int lSoccerBarIndex = 0; lSoccerBarIndex < _soccerBars.Count; lSoccerBarIndex++)
                    {
                        if (_soccerBars[lSoccerBarIndex].NumberOfPlayers() == 2)
                        {
                            _soccerBars[lSoccerBarIndex].Possess();
                            _controlledSoccerBarIndex = lSoccerBarIndex;
                            return;
                        }
                    }
                }
                else
                {
                    for (int lSoccerBarIndex = 0; lSoccerBarIndex < _soccerBars.Count; lSoccerBarIndex++)
                    {
                        if (_soccerBars[lSoccerBarIndex].NumberOfPlayers() == 5)
                        {
                            _soccerBars[lSoccerBarIndex].Possess();
                            _controlledSoccerBarIndex = lSoccerBarIndex;
                            return;
                        }
                    }
                }

                Debug.LogWarning("Possessing starting bar will be random, since player in defense doesn't have any 2 player bar, or player in attack doesn't have any 5 player bar.");
                _soccerBars[0].Possess();
                _controlledSoccerBarIndex = 0;
                break;
            case 4:
                // With 4 bars, player SHOULD have a 5 player bar
                for (int lSoccerBarIndex = 0; lSoccerBarIndex < _soccerBars.Count; lSoccerBarIndex++)
                {
                    if (_soccerBars[lSoccerBarIndex].NumberOfPlayers() == 5)
                    {
                        _soccerBars[lSoccerBarIndex].Possess();
                        _controlledSoccerBarIndex = lSoccerBarIndex;
                        return;
                    }
                }

                Debug.LogWarning("Player with 4 bars doesn't have a 5 players bar? Possessing the first one then. But this is not good for now.");
                _soccerBars[0].Possess();
                _controlledSoccerBarIndex = 0;
                break;
            default:
                Debug.LogWarning("Starting game possess is not implemented with that number of bars possessed yet. Do it !");
                _soccerBars[0].Possess();
                _controlledSoccerBarIndex = 0;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If we're not owner, don't go further
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Shutting down...");
            if (IsServer)
            {
                // Stopping Tugboat
                InstanceFinder.TransportManager.GetTransport<Multipass>().StopConnection(true, 0);
                // Stopping FishyUnityTransport
                InstanceFinder.TransportManager.GetTransport<Multipass>().StopConnection(true, 1);
            }
            else
            {
                InstanceFinder.ClientManager.StopConnection();
            }
        }

        // If player is not ready, nothing to do for now
        if (!_isPlayerReady)
        {
            return;
        }

        /*
                if (Input.GetKeyDown(KeyCode.A))
                {
                    TrySwitchLeftSoccerBar();
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    TrySwitchRightSoccerBar();
                }
        */

        if (Input.GetKeyDown(KeyCode.A))
        {
            TrySwitchSoccerBar(Net_SoccerBar.BarDisposition.One);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            TrySwitchSoccerBar(Net_SoccerBar.BarDisposition.Two);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TrySwitchSoccerBar(Net_SoccerBar.BarDisposition.Five);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            TrySwitchSoccerBar(Net_SoccerBar.BarDisposition.Three);
        }

        if (IsServer && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            // CTRL + R
            _gameManager?.ResetBallLocation();
        }
    }

    void TrySwitchLeftSoccerBar()
    {
        TrySwitchSoccerBar(true);
    }

    void TrySwitchRightSoccerBar()
    {
        TrySwitchSoccerBar(false);
    }

    private void TrySwitchSoccerBar(int pIndex)
    {
        // if we tried accessing a non-valid index, returning
        if (_soccerBars.Count <= pIndex || pIndex < 0)
            return;

        SwitchToSoccerBar(pIndex);
    }

    private void TrySwitchSoccerBar(Net_SoccerBar.BarDisposition pBarDisposition)
    {
        // If we have 4 bars, since bar disposition has to be [1,2,5,3], I already know the indexes of each bar
        if (_soccerBars.Count == 4)
        {
            switch (pBarDisposition)
            {
                case Net_SoccerBar.BarDisposition.One:
                    SwitchToSoccerBar(0);
                    break;
                case Net_SoccerBar.BarDisposition.Two:
                    SwitchToSoccerBar(1);
                    break;
                case Net_SoccerBar.BarDisposition.Five:
                    SwitchToSoccerBar(2);
                    break;
                case Net_SoccerBar.BarDisposition.Three:
                    SwitchToSoccerBar(3);
                    break;
            }
            return;
        }

        // If we have less than 4 bars, we have to find the bar with the correct bar disposition
        for (int lSoccerBarIndex = 0; lSoccerBarIndex < _soccerBars.Count; lSoccerBarIndex++)
        {
            if (_soccerBars[lSoccerBarIndex].GetBarDisposition == pBarDisposition)
            {
                SwitchToSoccerBar(lSoccerBarIndex);
                return;
            }
        }
        // If we didn't find any bar that has the same bar disposition, not switching (therefore the "try" in fct name)
    }

    // If player is not controlling the most left bar, he can control another more left bar
    // Same for most right (we're checking for this here)
    private void TrySwitchSoccerBar(bool pIsLeft)
    {
        int lDirection = pIsLeft ? -1 : 1;
        // Checking if soccerbar can change direction
        // Inclusively [0, _soccerBars.Count]
        if (_controlledSoccerBarIndex + lDirection >= 0 && _controlledSoccerBarIndex + lDirection < _soccerBars.Count)
        {
            SwitchToSoccerBar(_controlledSoccerBarIndex + lDirection);
        }
    }

    // Switching soccer bar
    private void SwitchToSoccerBar(int pNewControlledSoccerBarIndex)
    {
        _soccerBars[_controlledSoccerBarIndex].Unpossess();
        _soccerBars[pNewControlledSoccerBarIndex].Possess();
        _controlledSoccerBarIndex = pNewControlledSoccerBarIndex;
    }

    // ----- Ordering soccer bars

    // Return the given pSoccerBars ordered from left to right soccer bars
    public List<Net_SoccerBar> OrderSoccerBar(List<Net_SoccerBar> pSoccerBars)
    {
        int lNumberOfBars = pSoccerBars.Count;

        if (lNumberOfBars == 2)
        {
            return OrderSizeTwoSoccerBars(pSoccerBars);
        }

        if (lNumberOfBars == 4)
        {
            return OrderSizeFourSoccerBars(pSoccerBars);
        }

        Debug.LogError("Error : Tried to order soccer bar from an invalid number of bars. Number of bar is : " + lNumberOfBars + ". List will just be returned, not ordered.");
        return pSoccerBars;
    }

    private List<Net_SoccerBar> OrderSizeTwoSoccerBars(List<Net_SoccerBar> pSoccerBars)
    {
        // If player has a 5 player bar, he's attacking
        if (pSoccerBars[0].NumberOfPlayers() == 5 || pSoccerBars[1].NumberOfPlayers() == 5)
        {
            // If player is attacking and first bar isn't 5 player, it's not ordered
            if (pSoccerBars[0].NumberOfPlayers() != 5)
            {
                // Swap elements
                UtilityLibrary.Swap<Net_SoccerBar>(pSoccerBars, 0, 1);
            }
        }
        else
        {
            // If player is defending and first bar isn't goalkeeper, it's not ordered
            if (pSoccerBars[0].NumberOfPlayers() != 1)
            {
                UtilityLibrary.Swap<Net_SoccerBar>(pSoccerBars, 0, 1);
            }
        }

        // Ordered list
        return pSoccerBars;
    }

    private List<Net_SoccerBar> OrderSizeFourSoccerBars(List<Net_SoccerBar> pSoccerBars)
    {
        // Size four soccer bar disposition is [1, 2, 5, 3] (based on number of players) : 
        // - Goalkeeper (1 player)
        // - Defenders (2 players)
        // - Halves (5 players)
        // - Attackers (3 players)
        // So first, order list by ascending number of player [1, 2, 3, 5]
        List<Net_SoccerBar> lOrderedList = pSoccerBars.OrderBy(bar => bar.NumberOfPlayers()).ToList();
        // Then swap two lasts bars, to get [1, 2, 5, 3]
        UtilityLibrary.Swap<Net_SoccerBar>(lOrderedList, 3, 2);
        return lOrderedList;
    }

    // -----
}
