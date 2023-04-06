using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet;
using FishNet.Component.Spawning;
using System;

public class Net_Player : NetworkBehaviour
{
    public Action<Net_Player> OnPlayerDespawn;

    [SerializeField]
    private Camera _playerCamera;

    [SerializeField]
    private UI_PlayerControls _playerControlsPrefab;

    private UI_PlayerControls _playerControls;

    // Reference to GameManager. This variable should be null for clients
    // (game manager is only relevant on server)
    private Net_GameManager _gameManager;

    // Reference to main menu - so we can set it active as soon as network disconnects
    // TODO: maybe main menu should be in another scene? would be easier i think
    private MainMenu _mainMenu;
    private Camera _mainMenuCamera;

    // Becomes true when player gets enough bar required to play
    //private NetworkVariable<bool> _isPlayerReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SyncVar(OnChange = nameof(OnIsPlayerReadyChanged))]
    private bool _isPlayerReady;
    public bool IsPlayerReady
    {
        get { return _isPlayerReady; }
    }

    // List of soccer bar we own (we will be able to control during gameplay)
    private List<Net_SoccerBar> _soccerBars;

    // Controlled soccer bar index. Default -1, will be set when game starts and StartingSoccerBar is chosen.
    private int _controlledSoccerBarIndex = -1;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        GameObject lGO = GameObject.Find("Net_GameManagerPrefab");
        if (lGO)
        {
            if (IsServer)
            {
                _gameManager = lGO.GetComponent<Net_GameManager>();
            }
            else
            {
                // Destroy(lGO);
            }
        }
        else
        {
            Debug.LogError("ERROR : At player spawn, game manager should be found. It is not !");
        }

        // If we are the only owner of this object
        if (base.Owner.IsLocalClient)
        {
            All_OnFirstStart();
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (IsOwner)
        {
            DisplayMenu();
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

            // if owner & server (should be done only ONCE !, when the server is ready to play !)
            if (IsServer)
            {
                //_gameManager.Server_InstantiateBall();
            }
        }

        if (IsServer)
        {
            // If player is red side, give him proper changements
            if (_gameManager.IsRedFieldSide(transform.position))
            {
                // Server will set him red side
                Server_SetRedSide();
            }
            else
            {
                Server_SetBlueSide();
            }
        }

        if (IsServer)
        {
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
        Transform lFieldSide = _gameManager.GetRandomFieldSide();
        transform.position = lFieldSide.position;
        transform.rotation = lFieldSide.rotation;
    }

    // Function called when the server start, and a player spawns for him
    // Because he's the server and a player (as host)
    // Here, we want to load networked object for the game :  field, ball, bars, etc.
    // MAYBE MORE !
    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log("<color=#00000><i>On server start from owning net player</i></color>");
        // Server spawns soccer field
        _gameManager.Server_InstantiateField();

        // Place server random side of the field
        Server_TeleportRandomFieldSide();

        // Server spawns its own bar
        Server_SpawnOwnSoccerBar();

        _gameManager.Server_InstantiateBall();
    }

    public override void OnDespawnServer(NetworkConnection connection)
    {
        base.OnDespawnServer(connection);
        OnPlayerDespawn.Invoke(this);
    }

    // Function called on owning client, as it spawns his net_player from the server
    // Here we can do client things as when we're joining, such as :
    // asking for bars ....  MAYBE MORE
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsServer)
            return;
        // On spawn, client asks server to spawn him a soccer bar
        SpawnSoccerBarForClientServerRpc();
    }

    // Function called ONE TIME only, when they typically join the game
    // Used to do similar server AND client things, before online game starts !
    // such as remove local thing and start for full network experience
    void All_OnFirstStart()
    {
        HideMenu();
        ShowControls();

        // Switching from menu to player camera
        if (!_playerCamera)
        {
            Debug.LogWarning("Please reference the camera in the Net_Player !");
            return;
        }

        // Disable main menu camera, and activate the player one
        Camera lMainMenuCamera = Camera.main;
        if (lMainMenuCamera)
        {
            // reference it, if we want to come back to menu later
            _mainMenuCamera = lMainMenuCamera;

            lMainMenuCamera.gameObject.SetActive(false);
            _playerCamera.gameObject.SetActive(true);
        }
    }

    // hide main menu & main menu background
    // Hide only if field "Joining code" in main menu is empty (i.e. everything but when we started as multiplayer host)
    // or if we fill the boolean parameter to true
    private void HideMenu(bool pForceHideEverything = false)
    {
        GameObject lMainMenuGO = GameObject.Find("MainMenu");
        if (lMainMenuGO)
        {
            MainMenu lMainMenu = lMainMenuGO.GetComponent<MainMenu>();
            // if we find the menu
            if (lMainMenu)
            {
                // reference it - if we want to come back to it later
                _mainMenu = lMainMenu;
                // If main menu contain joining code
                if (lMainMenu.HasJoiningCode())
                {
                    // Only hide if we force it !
                    if (pForceHideEverything)
                    {
                        lMainMenuGO.SetActive(false);
                    }
                    // else, keep menu active, to show joining code!
                }
                else
                {
                    // No joining code - hide everything
                    lMainMenuGO.SetActive(false);
                }
            }
        }
    }

    private void ShowControls()
    {
        if (!_playerControls)
        {
            _playerControls = Instantiate(_playerControlsPrefab);
        }
        else
        {
            _playerControls.gameObject.SetActive(true);
        }
    }

    private void DestroyControls()
    {
        if (_playerControls)
            Destroy(_playerControls);
    }

    private void DisplayMenu()
    {
        if (_mainMenu && _mainMenuCamera)
        {
            _mainMenu.gameObject.SetActive(true);
            _mainMenuCamera.gameObject.SetActive(true);

            // Sets cursor visible again
            SetCursorVisibility(true);
        }
    }

    // Triggered server-side when the player is spawned, and the server is not the owner 
    private void Server_OnNonOwningStart()
    {
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
        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_Players>().Players;

        // First, we're checking if the number of player is good enough
        if (lPlayers.Count < _gameManager.NumberOfPlayers())
        {
            Debug.Log("Not enough player connected - waiting for more...");
            return false;
        }

        // Then, we're checking if every connected client is ready to play

        int lIgnoredPlayer = 0;
        // Foreach connected clients
        foreach (Net_Player lPlayer in lPlayers)
        {
            InstanceFinder.NetworkManager.GetComponent<PlayerSpawner>();

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

        if (lPlayers.Count - lIgnoredPlayer < _gameManager.NumberOfPlayers())
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
            // Asks the game manager to spawn soccer bar (because it's its role)
            // We only need the client id to give him authority on soccer bar
            _gameManager.Server_InstantiateSoccerBar(base.OwnerId);
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
            + " of player : "
            + GetComponent<NetworkObject>().ObjectId
            + " - Soccer bar has : " + pSoccerBar.NumberOfPlayer() + " players.");

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
        OnSoccerBarAddedRpc(InstanceFinder.ClientManager.Clients[base.OwnerId], _gameManager.NumberOfBarsRequired());
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
            _isPlayerReady = true;
        }
        else
        {
            Debug.Log("Player is not ready for now...");
        }
    }

    void Server_OnSoccerBarAdded()
    {
        if (!IsServer)
        {
            Debug.LogError("Is not server - shouldn't be here.");
            return;
        }

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
        // FORCE hidding menu, if it's not fully done
        HideMenu(true);

        // Start game for every connected Net_Player !
        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_Players>().Players;
        foreach (Net_Player lPlayer in lPlayers)
        {
            if (lPlayer)
            {
                // Start match for this player
                lPlayer.Server_GameHasStarted();
            }
        }
    }

    private void Server_GameHasStarted()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Calling server function on a non-server instance. Returning...");
            return;
        }

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
        {
            Debug.LogWarning("Calling server function on a non-server instance. Returning...");
            return;
        }

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
        {
            Debug.LogWarning("Server function called on a non-server instance.");
            return;
        }

        List<Net_Player> lPlayers = InstanceFinder.NetworkManager.GetComponent<FNet_Players>().Players;
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
                    if (_soccerBars[lSoccerBarIndex].NumberOfPlayer() == 5)
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
        {
            Debug.LogError("Is not server - shouldn't be here.");
            return;
        }

        _gameManager.Server_InstantiateSoccerBar();
    }

    // Update is called once per frame
    void Update()
    {
        // If we're not owner, don't go further
        if (!IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Shutting down...");
            if (IsServer)
                NetworkManager.ServerManager.StopConnection(true);
            else
                NetworkManager.ClientManager.StopConnection();
            DisplayMenu();
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
        if (_soccerBars.Count <= pIndex)
        {
            return;
        }

        SwitchSoccerBar(_controlledSoccerBarIndex, pIndex);
        _controlledSoccerBarIndex = pIndex;
    }

    // If player is not controlling the most left bar, he can control another more left bar
    // Same for most right (we're checking for this here)
    private void TrySwitchSoccerBar(bool pIsLeft)
    {
        int lDirection = pIsLeft ? -1 : 1;
        int lOldControlledSoccerBarIndex = _controlledSoccerBarIndex;
        // Checking if soccerbar can change direction
        // Inclusively [0, _soccerBars.Count]
        if (_controlledSoccerBarIndex + lDirection >= 0 && _controlledSoccerBarIndex + lDirection < _soccerBars.Count)
        {
            // Yes we can ! do it
            _controlledSoccerBarIndex += lDirection;
            SwitchSoccerBar(lOldControlledSoccerBarIndex, _controlledSoccerBarIndex);
        }
    }

    // Switching soccer bar
    private void SwitchSoccerBar(int pOldControlledSoccerBarIndex, int pNewControlledSoccerBarIndex)
    {
        _soccerBars[pOldControlledSoccerBarIndex].Unpossess();
        _soccerBars[pNewControlledSoccerBarIndex].Possess();
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
        if (pSoccerBars[0].NumberOfPlayer() == 5 || pSoccerBars[1].NumberOfPlayer() == 5)
        {
            // If player is attacking and first bar isn't 5 player, it's not ordered
            if (pSoccerBars[0].NumberOfPlayer() != 5)
            {
                // Swap elements
                UtilityLibrary.Swap<Net_SoccerBar>(pSoccerBars, 0, 1);
            }
        }
        else
        {
            // If player is defending and first bar isn't goalkeeper, it's not ordered
            if (pSoccerBars[0].NumberOfPlayer() != 1)
            {
                UtilityLibrary.Swap<Net_SoccerBar>(pSoccerBars, 0, 1);
            }
        }

        // List should be ordered
        return pSoccerBars;
    }

    private List<Net_SoccerBar> OrderSizeFourSoccerBars(List<Net_SoccerBar> pSoccerBars)
    {
        int[] lOrderedIndexes = new int[4];
        for (int lBarIndex = 0; lBarIndex < pSoccerBars.Count; lBarIndex++)
        {
            Net_SoccerBar lSoccerBar = pSoccerBars[lBarIndex];
            int lSoccerBarNumberOfPlayer = lSoccerBar.NumberOfPlayer();

            switch (lSoccerBarNumberOfPlayer)
            {
                case 1:
                    lOrderedIndexes[0] = lBarIndex;
                    break;
                case 2:
                    lOrderedIndexes[1] = lBarIndex;
                    break;
                case 3:
                    lOrderedIndexes[3] = lBarIndex;
                    break;
                case 5:
                    lOrderedIndexes[2] = lBarIndex;
                    break;
            }
        }

        // [0 : 3]
        for (int i = 0; i < 4; i++)
        {
            // If lOrderedIndexes is not [0, 1, 2, 3] (same as this loop)
            if (lOrderedIndexes[i] != i)
            {
                // Swap is needed
                UtilityLibrary.Swap<Net_SoccerBar>(pSoccerBars, i, lOrderedIndexes[i]);
            }
        }

        return pSoccerBars;
    }

    // -----
}
