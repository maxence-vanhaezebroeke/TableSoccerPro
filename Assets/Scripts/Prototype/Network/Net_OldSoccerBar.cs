using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Net_OldSoccerBar : NetworkBehaviour
{
    public enum BarDisposition
    {
        One, // GoalKeeper
        Two, // Defenders
        Three, // Attackers
        Five // Halves
    }

    #region Exposed variables

    [SerializeField]
    private float _scrollSpeed = 1750f;

    [SerializeField]
    private float _barSpeed = 15f;

    [SerializeField]
    private float _zBound = 1f;

    [SerializeField]
    [Tooltip("Indicate which disposition (i.e. : how many players there will be)." +
    "This value can be modified in code, before being Instantiated")]
    public BarDisposition _barDisposition;

    [SerializeField]
    private Net_SoccerPlayer _soccerPlayerPrefab;

    #endregion

    private List<Net_SoccerPlayer> _soccerPlayers;

    // TODO : implement scroll acceleration - the more the user scrolls on a small amount of time,
    // the more speed it'll get. And acceleration over time if he keeps scrolling

    private float _initialZLocation;

    private bool _isControlledByPlayer = false;

    #region Mouse movement

    // Instead of creating a variable in the update, I keep it in class
    // Because I need it all the time, so no memory allocation time
    private float _mouseScrollValue;
    // Same as above
    private float _mouseY;
    private float _lastMouseY;

    #endregion

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _lastMouseY = Input.GetAxis("Mouse Y");
        _initialZLocation = transform.position.z;

        NetworkObject lPlayer;

        // First, server instantiates soccer player associated to soccer bar
        if (IsServer)
        {
            Debug.Log("Server spawning soccer players...");
            Server_InstantiateSoccerPlayers();
        }

        // At this point, we only need the owner of the object
        if (NetworkManager.LocalClientId != OwnerClientId)
        {
            return;
        }

        // If server is onwer
        if (IsServer)
        {
            // Locate the owning player
            lPlayer = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject;
            Debug.Log("Owning player id is : " + OwnerClientId + " and server id is : " + NetworkManager.LocalClientId);
        }
        else
        {
            // As owner, locate our local player
            lPlayer = NetworkManager.LocalClient.PlayerObject;
        }

        Net_Player lNetPlayer = lPlayer.GetComponent<Net_Player>();
        if (!lNetPlayer)
        {
            Debug.LogError("Error : PlayerObject found but no Net_Player.");
            return;
        }

        // TODO : this is old, this only work on new version. Keep it here for now
        // if we want to revert
        // lNetPlayer.AddSoccerBar(this);
    }
    
    // Start is called before the first frame update
    void Start()
    {

    }

    void StartGame()
    {

    }

    void Server_InstantiateSoccerPlayers()
    {
        _soccerPlayers = new List<Net_SoccerPlayer>();

        // Default spawn location : center of the bar
        Vector3 lPlayerSpawnPos = new Vector3(0f, 0f, 0f);
        Debug.Log("Server will spawn : " + _barDisposition + " players.");
        switch (_barDisposition)
        {
            case BarDisposition.One:
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                break;
            case BarDisposition.Two:
                // Two players, each from 40 cm from the middle of the bar (defense)
                lPlayerSpawnPos.z -= .45f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += .9f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                break;
            case BarDisposition.Three:
                // Three players, one middle and two 33cm from the middle of the bar (attack)
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= .4f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += .8f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                break;
            case BarDisposition.Five:
                // Five players, same as three but 2 more
                // (with less space than 3)
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= .35f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= .35f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += .7f + .35f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += .35f;
                Server_InstantiateSoccerPlayer(lPlayerSpawnPos);
                break;
        }
    }

    void Server_InstantiateSoccerPlayer(Vector3 pPosition)
    {
        Net_SoccerPlayer lSoccerPlayer = Instantiate(_soccerPlayerPrefab, transform.position, _soccerPlayerPrefab.transform.rotation);

        // Spawn players with same authority as soccerbar
        //lSoccerPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        lSoccerPlayer.GetComponent<NetworkObject>().Spawn();
        lSoccerPlayer.transform.SetParent(transform);
        lSoccerPlayer.transform.localPosition = pPosition;

        _soccerPlayers.Add(lSoccerPlayer);
    }

    // Update is called once per frame
    void Update()
    {
        if (_isControlledByPlayer)
        {
            UpdateMovement();
        }
    }

    public void Possess()
    {
        _isControlledByPlayer = true;
    }

    public void Unpossess()
    {
        _isControlledByPlayer = false;
    }

    void UpdateMovement()
    {
        // ----- Update Rotation
        _mouseScrollValue = Input.GetAxis("Mouse ScrollWheel");
        // TODO : this will imply physics so think about FixedUpdate ?
        if (_mouseScrollValue != 0)
        {
            // Rotate over time depending on mouse scroll
            transform.Rotate(0f, 0f, _mouseScrollValue * _scrollSpeed * Time.deltaTime);
        }

        // ----- Update Movement
        _mouseY = Input.GetAxis("Mouse Y");
        if (_mouseY != _lastMouseY)
        {
            float lTranslatingValue = (_mouseY - _lastMouseY) * Time.deltaTime * _barSpeed;

            // Z+ boundary
            if (transform.position.z + lTranslatingValue > _initialZLocation + _zBound)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation + _zBound);
            }
            // Z- boundary
            else if (transform.position.z + lTranslatingValue < _initialZLocation - _zBound)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation - _zBound);
            }
            // Move freely between Z+ & Z-
            else
            {
                transform.Translate(0f, 0f, lTranslatingValue);
            }
        }
    }

    public int NumberOfPlayer()
    {
        switch (_barDisposition)
        {
            case BarDisposition.One:
                return 1;
            case BarDisposition.Two:
                return 2;
            case BarDisposition.Three:
                return 3;
            case BarDisposition.Five:
                return 5;
            default:
                return 0;
        }
    }
}
