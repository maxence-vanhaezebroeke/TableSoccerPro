using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkSoccerPlayer : NetworkPlayer
{
    public Action<NetworkSoccerPlayer> OnPlayerIsReady;
    public Action<NetworkSoccerPlayer> OnPlayerStarted;
    // Action<this, new field side>
    public Action<NetworkSoccerPlayer, Net_SoccerField.FieldSide> OnPlayerFieldSideChanged;
    public Action<NetworkSoccerPlayer> OnPlayerSoccerBarAdded;

    [SerializeField]
    private UI_PlayerControls _playerControlsPrefab;

    private UI_PlayerControls _playerControls;

    [Header("SyncVars")]
    // Becomes true when player gets enough bar required to play (ready to play)
    [SyncVar(OnChange = nameof(OnIsPlayerReadyChanged))]
    private bool _isPlayerReady;
    public bool IsPlayerReady
    {
        get { return _isPlayerReady; }
    }

    private List<NetworkSoccerBar> _soccerBars;
    public int SoccerBarCount
    {
        get
        {
            return _soccerBars.Count;
        }
    }

    // Controlled soccer bar index. Default -1, will be set when game starts and StartingSoccerBar is chosen.
    private int _controlledSoccerBarIndex = -1;

    [SyncVar(OnChange = nameof(OnFieldSideChanged))]
    private Net_SoccerField.FieldSide _fieldSide = Net_SoccerField.FieldSide.None;
    public Net_SoccerField.FieldSide FieldSide
    {
        get
        {
            return _fieldSide;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        UtilityLibrary.ThrowIfNull(this, _playerControlsPrefab);
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (base.IsOwner)
            DestroyControls();
    }

    // Player start server side
    public override void OnStartServer()
    {
        base.OnStartServer();

        OnPlayerStarted?.Invoke(this);
    }

    // Player start client side
    public override void OnStartClient()
    {
        base.OnStartClient();

        ShowControls();

        if (_fieldSide != Net_SoccerField.FieldSide.None)
            // TODO: Ask for server to tell us correct field side info about this player!
            return;
    }

    private void OnIsPlayerReadyChanged(bool pPrevious, bool pCurrent, System.Boolean pAsServer)
    {
        if (IsOwner)
        {
            StartPlayerGameplay();
        }

        OnPlayerIsReady?.Invoke(this);
    }

    private void StartPlayerGameplay()
    {
        // Order soccer bars, & possess the correct one !
        OrderSoccerBar(_soccerBars);
        PossessStartingBar();
        // We're in game "mode", so hide cursor
        GameMode.Instance.SetCursorVisibility(false);
    }

    public void Server_SetFieldSide(Net_SoccerField.FieldSide pFieldSide)
    {
        if (!IsServer)
            return;

        _fieldSide = pFieldSide;
    }

    private void OnFieldSideChanged(Net_SoccerField.FieldSide pOldFieldSide, Net_SoccerField.FieldSide pNewFieldSide, System.Boolean pAsServer)
    {
        OnPlayerFieldSideChanged?.Invoke(this, pNewFieldSide);
    }

    // Public but observers rpc, so only server can call this to run logic on clients
    [ObserversRpc]
    public void All_SetPlayerPositionAndRotation(Vector3 pPosition, Quaternion pRotation)
    {
        transform.SetPositionAndRotation(pPosition, pRotation);
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
            Destroy(_playerControls.gameObject);
    }

    // FIXME : this was previously called by Net_soccerbar on bar spawn
    // BUT, for client, bar was searching for player to call it ! this is ridiculous
    // SoccerPlayerManager should ask game manager to spawn bar, and on bar spawn, give it to the correct player! (call server & owning player function)
    public void AddSoccerBar(NetworkSoccerBar pSoccerBar)
    {
        if (_soccerBars == null)
            _soccerBars = new List<NetworkSoccerBar>();

        Debug.Log("Adding soccer bar : "
            + pSoccerBar.GetComponent<NetworkObject>().ObjectId
            + " - owner is : "
            + GetComponent<NetworkObject>().OwnerId
            + " - Soccer bar has : " + pSoccerBar.NumberOfPlayers() + " players.");

        if (base.IsServer)
            pSoccerBar.SetSide(_fieldSide);
        
        _soccerBars.Add(pSoccerBar);

        // NOTE: here, i don't want my player to self assign "_isPlayerReady", because the logic
        // stands for GameManager. So, raising an event, listened by the player manager, which checks
        // with the GameManager to set player ready if so !
        OnPlayerSoccerBarAdded?.Invoke(this);
    }

    [TargetRpc]
    public void AddSoccerBarTargetRpc(NetworkConnection pOwnerConnection, NetworkSoccerBar pSoccerBar)
    {
        AddSoccerBar(pSoccerBar);
    }
    
    public void SetPlayerReady()
    {
        if (IsServer)
        {
            SetIsPlayerReady(true);
        }
        else
        {
            SetPlayerReadyServerRpc();
        }
    }

    private void SetIsPlayerReady(bool pIsPlayerReady)
    {
        if (_isPlayerReady != pIsPlayerReady)
            _isPlayerReady = pIsPlayerReady;
    }

    [ServerRpc]
    private void SetPlayerReadyServerRpc()
    {
        SetIsPlayerReady(true);
    }

    public void Server_ResetGame()
    {
        if (!IsServer)
            return;

        foreach (NetworkSoccerBar lSoccerBar in _soccerBars)
        {
            lSoccerBar.ResetGame();
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

    protected override void Update()
    {
        if (!base.IsOwner)
            return;

        base.Update();

        if (!_isPlayerReady)
            return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            TrySwitchSoccerBar(NetworkSoccerBar.BarDisposition.One);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            TrySwitchSoccerBar(NetworkSoccerBar.BarDisposition.Two);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TrySwitchSoccerBar(NetworkSoccerBar.BarDisposition.Five);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            TrySwitchSoccerBar(NetworkSoccerBar.BarDisposition.Three);
        }
    }

    private void TrySwitchSoccerBar(NetworkSoccerBar.BarDisposition pBarDisposition)
    {
        // If we have 4 bars, since bar disposition has to be [1,2,5,3], I already know the indexes of each bar
        if (_soccerBars.Count == 4)
        {
            switch (pBarDisposition)
            {
                case NetworkSoccerBar.BarDisposition.One:
                    SwitchToSoccerBar(0);
                    break;
                case NetworkSoccerBar.BarDisposition.Two:
                    SwitchToSoccerBar(1);
                    break;
                case NetworkSoccerBar.BarDisposition.Five:
                    SwitchToSoccerBar(2);
                    break;
                case NetworkSoccerBar.BarDisposition.Three:
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

    // Switching soccer bar
    private void SwitchToSoccerBar(int pNewControlledSoccerBarIndex)
    {
        _soccerBars[_controlledSoccerBarIndex].Unpossess();
        _soccerBars[pNewControlledSoccerBarIndex].Possess();
        _controlledSoccerBarIndex = pNewControlledSoccerBarIndex;
    }

    // ----- Ordering soccer bars

    // Return the given pSoccerBars ordered from left to right soccer bars
    public void OrderSoccerBar(List<NetworkSoccerBar> pSoccerBars)
    {
        int lNumberOfBars = pSoccerBars.Count;

        if (lNumberOfBars == 2)
        {
            _soccerBars = UtilityLibrary.OrderSizeTwoSoccerBars(pSoccerBars);
            return;
        }

        if (lNumberOfBars == 4)
        {
            _soccerBars = UtilityLibrary.OrderSizeFourSoccerBars(pSoccerBars);
            return;
        }

        Debug.LogWarning("Tried to order soccer bar from an invalid number of bars. Number of bar is : " + lNumberOfBars + ". List will just be returned, not ordered.");
    }
}
