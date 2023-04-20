using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet;
using System;
using FishNet.Transporting.Multipass;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Linq;

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

    // Reference to GameManager. This variable should be null for clients
    // (game manager is only relevant on server)
    private Net_GameManager _gameManager;

    // Becomes true when player gets enough bar required to play
    //private NetworkVariable<bool> _isPlayerReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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

    // List of soccer bar we own (we will be able to control during gameplay)
    private List<Net_SoccerBar> _soccerBars;

    // Controlled soccer bar index. Default -1, will be set when game starts and StartingSoccerBar is chosen.
    private int _controlledSoccerBarIndex = -1;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // If we are the only owner of this object
        if (base.Owner.IsLocalClient)
        {
            OnPlayerFirstStart();
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (IsOwner)
        {
            DestroyControls();
        }
    }

    private void SetCursorVisibility(bool pIsVisible)
    {
        if (pIsVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
            SetCursorVisibility(false);
        }

        if (IsServer)
        {
            // Set player side
            if (_gameManager.IsRedFieldSide(transform.position))
                Server_SetRedSide();
            else
                Server_SetBlueSide();

            // Checking for every player to be ready
            if (Server_CanEveryPlayerStartGame())
            {
                // At this point, server and clients should have the good number of bar spawned
                // We start !
                Debug.Log("Starting the game ! Finally !");

                // Do starting game things
                Server_StartGame();
            }
            else
            {
                Debug.Log("Not starting the game yet : not fulfilling every condition.");
            }
        }
    }

    void Server_TeleportRandomFieldSide()
    {
        if (!IsServer)
            return;

        Transform lFieldSide = _gameManager.GetRandomFieldSide();
        All_SetClientSide(lFieldSide.position, lFieldSide.rotation);
    }

    [ObserversRpc]
    private void All_SetClientSide(Vector3 pPosition, Quaternion pRotation)
    {
        transform.position = pPosition;
        transform.rotation = pRotation;
    }

    // Function called when the server start, and a player spawns for him
    // Because he's the server and a player (as host)
    // Here, we want to load networked object for the game :  field, ball, bars, etc.
    // MAYBE MORE !
    private void Server_OnOwningStart()
    {
        Debug.Log("<color=#00000><i>On server owning start</i></color>");

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("Uninitialized");
        }

        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("User signed in for multiplayer game - as server, I should retrieve JoinCode!");
        }

        Server_InitializeGameManager();
        // Server spawns soccer field
        _gameManager.Server_InstantiateField();

        _gameManager.Server_InstantiateBall();
    }

    public override void OnDespawnServer(NetworkConnection connection)
    {
        base.OnDespawnServer(connection);
        OnPlayerDespawn?.Invoke(this);
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

    // Function called on owning client, as it spawns his net_player from the server
    // Here we can do client things as when we're joining, such as :
    // asking for bars ....  MAYBE MORE
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
        {
            if (IsServer)
                Server_OnNonOwningStart();

            return;
        }

        if (IsServer)
        {
            Server_InitializeGameManager();

            // Initialize ball and field
            Server_OnOwningStart();

            // Place server random side of the field
            Server_TeleportRandomFieldSide();

            // Server spawns its own bars
            Server_SpawnOwnSoccerBar();
        }
        else
        {
            // On spawn, client asks server to spawn him a soccer bar
            SpawnSoccerBarForClientServerRpc();
        }
    }

    // Function called ONE TIME only, when they typically join the game
    // Used to do similar server AND client things, before online game starts !
    // such as remove local thing and start for full network experience
    void OnPlayerFirstStart()
    {
        ShowControlsAndJoinCode();

        if (!_playerCamera)
        {
            Debug.LogWarning("Please reference the camera in the Net_Player !");
            return;
        }

        // TODO: what if it's already active in the prefab?
        _playerCamera.gameObject.SetActive(true);
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

        if (GameState.Instance.HasJoinCode)
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

    // Triggered server-side when the player is spawned, and the server is not the owner 
    private void Server_OnNonOwningStart()
    {
        Debug.Log("Server_OnNonOwningStart");
        Server_InitializeGameManager();

        Server_TeleportRandomFieldSide();
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
        if (lPlayers.Count < _gameManager.NumberOfPlayers)
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

        if (lPlayers.Count - lIgnoredPlayer < _gameManager.NumberOfPlayers)
        {
            Debug.Log("Not enough lNetPlayer connected - waiting for more...");
            return false;
        }

        Debug.Log("Game is ready !");
        // Every client has been checked - they all can start the game
        return true;
    }

    [ServerRpc]
    void SpawnSoccerBarForClientServerRpc()
    {
        // If owning client is connected
        if (InstanceFinder.ClientManager.Clients[base.OwnerId] != null)
        {
            Debug.Log("On server - spawning soccer bar for owner : " + base.Owner.ClientId);
            // Asks the game manager to spawn soccer bar (because it's its role)
            // We only need the client id to give him authority on soccer bar
            _gameManager.Server_InstantiateSoccerBar(base.Owner);
        }
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

        Debug.Log("target rpc working?");

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

    // Function called on a server instance of a player
    // The goal is to find server's owning player, and call PossessStartingBar on him
    void Server_PossessStartingBar()
    {
        if (!IsServer)
            return;

        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_PlayerManager>().Players;
        // Foreach connected clients (including server)
        foreach (Net_Player lPlayer in lPlayers)
        {
            if (lPlayer.IsOwner)
            {
                lPlayer.PossessStartingBar();
                return;
            }
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
                break;
        }
    }

    [TargetRpc]
    void PossessStartingBarRpc(NetworkConnection pNetworkConnection)
    {
        PossessStartingBar();
    }

    void Server_SpawnOwnSoccerBar()
    {
        if (!IsServer)
            return;

        _gameManager.Server_InstantiateSoccerBar();
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
            TrySwitchSoccerBar(0);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            TrySwitchSoccerBar(1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TrySwitchSoccerBar(2);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            TrySwitchSoccerBar(3);
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
