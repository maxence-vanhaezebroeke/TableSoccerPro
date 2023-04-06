using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerBar : MonoBehaviour
{
    public enum BarDisposition
    {
        One,
        Two,
        Three,
        Five
    }

    #region Exposed variables

    [SerializeField]
    private float _scrollSpeed = 1750f;

    [SerializeField]
    private float _barSpeed = 15f;

    [SerializeField]
    private float _zBound = 1f;

    [SerializeField]
    private BarDisposition _barDisposition;

    [SerializeField]
    private SoccerPlayer _soccerPlayerPrefab;

    #endregion

    private List<SoccerPlayer> _soccerPlayers;

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

    // Start is called before the first frame update
    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        _lastMouseY = Input.GetAxis("Mouse Y");
        _soccerPlayers = new List<SoccerPlayer>();
        _initialZLocation = transform.position.z;

        // Default spawn location : center of the bar
        Vector3 lPlayerSpawnPos = new Vector3(0f, 0f, 0f);
        switch(_barDisposition)
        {
            case BarDisposition.One:
                InstantiateSoccerPlayer(lPlayerSpawnPos);
            break;
            case BarDisposition.Two:
                // Two players, each from 50 cm from the middle of the bar (defense)
                lPlayerSpawnPos.z -= 5f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += 10f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
            break;
            case BarDisposition.Three:
                // Three players, one middle and two 1m from the middle of the bar (attack)
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= 10f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += 20f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
            break;
            case BarDisposition.Five:
                // Five players, same as three but 2 more
                // AND : we need to put less space between them ! (75cm instead of 1m)
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= 7.5f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z -= 7.5f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += 15f + 7.5f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
                lPlayerSpawnPos.z += 7.5f;
                InstantiateSoccerPlayer(lPlayerSpawnPos);
            break;
        }
    }

    void InstantiateSoccerPlayer(Vector3 pPosition)
    {
        SoccerPlayer lSoccerPlayer = Instantiate(_soccerPlayerPrefab, transform.position, _soccerPlayerPrefab.transform.rotation);
        lSoccerPlayer.transform.SetParent(transform);
        lSoccerPlayer.ChangeLocalPosition(pPosition);
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

    public int Players()
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
