using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

public class Net_GameManager : MonoBehaviour
{
    #region Soccer bars prefab

    [SerializeField]
    private Net_SoccerBar _onePlayerSoccerBarPrefab;

    [SerializeField]
    private Net_SoccerBar _twoPlayerSoccerBarPrefab;

    [SerializeField]
    private Net_SoccerBar _threePlayerSoccerBarPrefab;

    [SerializeField]
    private Net_SoccerBar _fivePlayerSoccerBarPrefab;

    #endregion

    [SerializeField]
    private Net_SoccerField _soccerFieldPrefab;
    [SerializeField]
    private Net_Ball _soccerBallPrefab;

    [SerializeField]
    [Tooltip("Determines the center point of the soccer, from which bars, players and ball will spawn & play.")]
    private Vector3 _fieldLocation;

    [SerializeField]
    private int _numberOfPlayers;

    private Net_SoccerField _soccerField;
    private Net_Ball _soccerBall;

    // To initialize objects, i must use a Vector3 as location
    // To avoid doing new Vector3() every time, i store it in here. (so this is a "garbage" variable)
    private Vector3 _initializeLocation;

    // Corners are meshes that avoid ball being stuck. We don't want to return this location as ball would be in the ground
    // So apply an offset to give proper corner location for ball
    private Vector3 lBallCornerOffset = new Vector3(.075f, .1f, .075f); 

    // NOTE : Functions are in majority starting with Server_
    // because gamemanager only has meaning on the server ! (to deal with game)
    // GameManager is not a networked object, because it doesn't need to
    // It's just an internal state the server keeps to run gameplay logic !

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _onePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _twoPlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _threePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _fivePlayerSoccerBarPrefab);

        UtilityLibrary.ThrowIfNull(this, _soccerBallPrefab);
        UtilityLibrary.ThrowIfNull(this, _soccerFieldPrefab);
    }

    public int NumberOfBarsRequired()
    {
        switch (_numberOfPlayers)
        {
            case 2:
                return 4;
            case 4:
                return 2;
            default:
                Debug.LogWarning("Game will never start - number of bars required is not good. Did you filled the number of player in Net_GameManager correctly? ");
                return -1;
        }
    }

    public int NumberOfPlayers()
    {
        return _numberOfPlayers;
    }

    public bool IsRedFieldSide(Vector3 pPosition)
    {
        return _soccerField.IsRedSide(pPosition);
    }

    public Transform GetRandomFieldSide()
    {
        return _soccerField.TakeRandomSide();
    }

    public void ResetGame()
    {
        // Reset field (goal counters)
        _soccerField.ResetGame();
        // Ball in the middle
        ResetBallLocation();
    }

    public void Server_InstantiateField()
    {
        if (_soccerField)
        {
            Debug.Log("Soccer field already spawned - returning...");
            return;
        }
        _soccerField = Instantiate(_soccerFieldPrefab, _fieldLocation, _soccerFieldPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(_soccerField.gameObject);
        //_soccerField.GetComponent<NetworkObject>().Spawn();
    }

    // Without args : server spawn
    // With args : give client id for client spawn
    public void Server_InstantiateSoccerBar(NetworkConnection pClient = null)
    {
        // If we get a client id, we instantiate client soccer bar
        if (pClient != null)
        {
            Server_InstantiateClientSoccerBars(pClient);
        }
        else
        {
            // else, server soccer bar
            Server_InstantiateServerSoccerBars();
        }
    }

    private void Server_InstantiateServerSoccerBars()
    {
        // TODO : this assumes server is blue side, and player is red side
        // but currently side is randomly set. Take this in account !
        NetworkConnection lPlayerConnection = InstanceFinder.NetworkManager.GetComponent<FNet_PlayerManager>().Players[0].LocalConnection;

        // First, left goalkeeper
        _initializeLocation = _fieldLocation;
        _initializeLocation.x -= 3.1f;
        _initializeLocation.y += .5f;
        Net_SoccerBar lSB = Instantiate(_onePlayerSoccerBarPrefab, _initializeLocation, _onePlayerSoccerBarPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, lPlayerConnection);
        lSB.Server_Initialize(lPlayerConnection.ClientId);
        // Server is client (host), so no need to initialize it for him
        //lSB.InitializeClientRpc(lPlayerConnection);

        // Second, left defenders
        _initializeLocation.x += .9f;
        lSB = Instantiate(_twoPlayerSoccerBarPrefab, _initializeLocation, _twoPlayerSoccerBarPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, lPlayerConnection);
        lSB.Server_Initialize(lPlayerConnection.ClientId);
        //lSB.InitializeClientRpc(lPlayerConnection);

        // Third, left halves
        _initializeLocation.x += 1.8f;
        lSB = Instantiate(_fivePlayerSoccerBarPrefab, _initializeLocation, _fivePlayerSoccerBarPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, lPlayerConnection);
        lSB.Server_Initialize(lPlayerConnection.ClientId);
        //lSB.InitializeClientRpc(lPlayerConnection);

        // Last (4th), left attackers
        _initializeLocation.x += 1.8f;
        lSB = Instantiate(_threePlayerSoccerBarPrefab, _initializeLocation, _threePlayerSoccerBarPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, lPlayerConnection);
        lSB.Server_Initialize(lPlayerConnection.ClientId);
        //lSB.InitializeClientRpc(lPlayerConnection);
    }

    private void SB_OnStartNetwork(Net_SoccerBar pSoccerBar)
    {
        pSoccerBar.Server_OnStartNetwork -= SB_OnStartNetwork;
        pSoccerBar.Server_Initialize();
    }

    private void Server_InstantiateClientSoccerBars(NetworkConnection pClient)
    {
        // First, right goalkeeper
        _initializeLocation = _fieldLocation;
        _initializeLocation.x += 3.1f;
        _initializeLocation.y += .5f;
        Net_SoccerBar lSB = Instantiate(_onePlayerSoccerBarPrefab, _initializeLocation, _onePlayerSoccerBarPrefab.transform.rotation);
        //lSB.GetComponent<NetworkObject>().SpawnWithOwnership(pClientId);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, pClient);
        // Initialize it on server - server will add soccer bar reference to the corresponding player
        lSB.Server_Initialize(pClient.ClientId);
        // Initialize it on client - adding it to owning player only, and returning server info if needed
        lSB.InitializeClientRpc(pClient);

        // Second, right defenders
        _initializeLocation.x += -.9f;
        lSB = Instantiate(_twoPlayerSoccerBarPrefab, _initializeLocation, _twoPlayerSoccerBarPrefab.transform.rotation);
        //lSB.GetComponent<NetworkObject>().SpawnWithOwnership(pClientId);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, pClient);
        // Initialize it on server - server will add soccer bar reference to the corresponding player
        lSB.Server_Initialize(pClient.ClientId);
        // Initialize it on client - adding it to owning player only, and returning server info if needed
        lSB.InitializeClientRpc(pClient);

        // Third, right halves
        _initializeLocation.x += -1.8f;
        lSB = Instantiate(_fivePlayerSoccerBarPrefab, _initializeLocation, _fivePlayerSoccerBarPrefab.transform.rotation);
        //lSB.GetComponent<NetworkObject>().SpawnWithOwnership(pClientId);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, pClient);
        // Initialize it on server - server will add soccer bar reference to the corresponding player
        lSB.Server_Initialize(pClient.ClientId);
        // Initialize it on client - adding it to owning player only, and returning server info if needed
        lSB.InitializeClientRpc(pClient);

        // Last (4th), right attackers
        _initializeLocation.x += -1.8f;
        lSB = Instantiate(_threePlayerSoccerBarPrefab, _initializeLocation, _threePlayerSoccerBarPrefab.transform.rotation);
        //lSB.GetComponent<NetworkObject>().SpawnWithOwnership(pClientId);
        InstanceFinder.ServerManager.Spawn(lSB.gameObject, pClient);
        // Initialize it on server - server will add soccer bar reference to the corresponding player
        lSB.Server_Initialize(pClient.ClientId);
        // Initialize it on client - adding it to owning player only, and returning server info if needed
        lSB.InitializeClientRpc(pClient);
    }

    public void Server_InstantiateBall()
    {
        if (_soccerBall)
        {
            Debug.Log("Soccer ball already spawned - returning...");
            return;
        }

        _initializeLocation = _fieldLocation;
        _initializeLocation.x -= .5f;
        _initializeLocation.y += .5f;
        _initializeLocation.z += .5f;
        _soccerBall = Instantiate(_soccerBallPrefab, _initializeLocation, _soccerBallPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(_soccerBall.gameObject);
        // TODO: unsubscribe this event somewhere later !
        _soccerBall.OnGoalEnter += Ball_OnGoalEnter;
        // TODO: same as above
        _soccerBall.OnBallExitBounds += Ball_OnBallExitBounds;
    }

    private void Ball_OnGoalEnter(Net_Ball pNetBall, Net_SoccerField.FieldSide pFieldSide)
    {
        _soccerField.Server_OnScore(pFieldSide);

        Server_ResetBallLocation(pNetBall, pFieldSide);
    }

    private void Ball_OnBallExitBounds(Net_Ball pNetBall, Collider pCollider)
    {
        if (!_soccerField)
        {
            Debug.LogError("/!\\ Ball is out of bounds but field is not valid? Shouldn't be happening !");
            return;
        }

        // We already checked for soccer field validity, so we can assume fct will always have a value
        pNetBall.transform.position = FindClosestBallCorner(pNetBall.transform.position).Value;
    }

    // From the given position, will return the ball position of the closest corner (corner + ball offset)
    private Vector3? FindClosestBallCorner(Vector3 pPosition)
    {
        if (!_soccerField)
            return null;

        // Get corners (blue & red)
        Vector3[] lBlueCorners = _soccerField.GetBlueCornersPosition();
        Vector3[] lRedCorners = _soccerField.GetRedCornersPosition();
        // Take the first blue as our default choice
        Vector3 lChosenCornerPosition = lBlueCorners[0];
        // Compare every blue to choose the nearest blue corner
        foreach (Vector3 lBlueCorner in lBlueCorners)
        {
            lChosenCornerPosition = UtilityLibrary.SmallestDistancePosition(pPosition, lChosenCornerPosition, lBlueCorner);
        }
        // Compare every red to choose the nearest red corner
        foreach (Vector3 lRedCorner in lRedCorners)
        {
            lChosenCornerPosition = UtilityLibrary.SmallestDistancePosition(pPosition, lChosenCornerPosition, lRedCorner);
        }
        // lChosenCornerPosition is now the nearest corner from the ball, between every blue & red corners
        
        // We need to add ball offset correctly : if a corner coordinate is negative, just add more negative
        // (as we don't want the ball to always go positive axis, because it will be good for one and just be mid-field for second)
        lChosenCornerPosition.x += lChosenCornerPosition.x < 0 ? -lBallCornerOffset.x : lBallCornerOffset.x;
        lChosenCornerPosition.y += lBallCornerOffset.y;
        lChosenCornerPosition.z += lChosenCornerPosition.z < 0 ? -lBallCornerOffset.z : lBallCornerOffset.z;
        return lChosenCornerPosition;
    }

    private void Server_ResetBallLocation(Net_Ball pNetBall, Net_SoccerField.FieldSide? pFieldSide = null)
    {
        // Resetting ball velocity
        // Since Net_Ball requires component Rigidbody, no need to check for it to be valid
        pNetBall.GetComponent<Rigidbody>().velocity = Vector3.zero;

        if (!_soccerField)
        {
            Debug.LogError("Tried to reset ball location but soccer field is not valid. Returning...");
            return;
        }

        if (pFieldSide.HasValue)
        {
            switch (pFieldSide.Value)
            {
                // Position ball to give advantage to player who got scored
                case Net_SoccerField.FieldSide.Red:
                    pNetBall.transform.position = _soccerField.transform.position + new Vector3(-.17f, .5f, 0f);
                    break;
                case Net_SoccerField.FieldSide.Blue:
                    pNetBall.transform.position = _soccerField.transform.position + new Vector3(.17f, .5f, 0f);
                    break;
            }
        }
        else
        {
            // give ball a random speed to avoid getting stuck in the middle of the field, unreachable
            int lRandomX = Random.Range(0, 2) == 0 ? 1 : -1;
            int lRandomZ = Random.Range(0, 2) == 0 ? 1 : -1;
            pNetBall.GetComponent<Rigidbody>().angularVelocity = new Vector3(Random.Range(1f, 1.7f) * lRandomX, 0f, Random.Range(1f, 1.7f) * lRandomZ);
            // center of the field
            pNetBall.transform.position = _soccerField.transform.position;
        }
    }

    // If ball is stuck, we can call this function to reset its location at the middle of the field.
    public void ResetBallLocation()
    {
        if (_soccerBall && _soccerField)
        {
            Server_ResetBallLocation(_soccerBall);
        }
        else
        {
            Debug.Log("Warning - Tried to reset ball location but either ball or soccer field is not valid. Not possible now...");
        }
    }
}