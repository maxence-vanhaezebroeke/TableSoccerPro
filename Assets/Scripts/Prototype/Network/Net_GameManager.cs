using FishNet;
using FishNet.Connection;
using UnityEngine;

public class Net_GameManager : Singleton<Net_GameManager>
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
    [Tooltip("Determines the middle point of the field, from which bars, players and ball will spawn & play.")]
    private Vector3 _fieldLocation;

    private Net_SoccerField _soccerField;
    private Net_Ball _soccerBall;

    // To initialize objects, I must use a Vector3 as location
    // To avoid doing new Vector3() every time, I store it in here.
    private Vector3 _initializeLocation;

    // Corners are meshes that avoid ball being stuck. We don't want to return this location as ball would be in the ground
    // So apply an offset to give proper corner location for ball
    private Vector3 lBallCornerOffset = new Vector3(.075f, .1f, .075f);

    // NOTE : Functions are in majority starting with Server_
    // because gamemanager only has meaning on the server ! (to deal with game)
    // GameManager is not a networked object, because it doesn't need to
    // It's just an internal state the server keeps to run gameplay logic !

    protected override void Awake()
    {
        base.Awake();

        UtilityLibrary.ThrowIfNull(this, _onePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _twoPlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _threePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _fivePlayerSoccerBarPrefab);

        UtilityLibrary.ThrowIfNull(this, _soccerBallPrefab);
        UtilityLibrary.ThrowIfNull(this, _soccerFieldPrefab);
    }

    public int NumberOfBarsRequired()
    {
        switch (GameMode.Instance.NumberOfPlayer)
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

    public Net_SoccerField.FieldSide GetRandomFieldSide()
    {
        if (GameMode.Instance.NumberOfPlayer == 2)
            return _soccerField.TakeTwoPlayerRandomSide();
        else if (GameMode.Instance.NumberOfPlayer == 4)
            return _soccerField.TakeFourPlayerRandomSide();
        else
        {
            Debug.Log("Non-valid number of player : field side cannot be set");
            return Net_SoccerField.FieldSide.None;
        } 
    }

    public Transform GetSideTransform(Net_SoccerField.FieldSide pFieldSide)
    {
        return _soccerField.GetSideTransform(pFieldSide);
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
    }

    private void SpawnBar(Net_SoccerBar pPrefab, NetworkConnection pNConnection, Vector3 pPosition)
    {
        Net_SoccerBar lSoccerBar = Instantiate(pPrefab, pPosition, pPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSoccerBar.gameObject, pNConnection);
        lSoccerBar.Server_Initialize(pNConnection.ClientId);

        // If bar is for remote client, initialize it
        if (!pNConnection.IsHost)
            lSoccerBar.InitializeClientRpc(pNConnection);
    }

    private void SpawnBar(bool pIsRedSide, Net_SoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        _initializeLocation = _fieldLocation;
        _initializeLocation.y += .5f;
        switch (pBarDisposition)
        {
            case Net_SoccerBar.BarDisposition.One:
                _initializeLocation.x += pIsRedSide ? 3.1f : -3.1f;
                SpawnBar(_onePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
                break;
            case Net_SoccerBar.BarDisposition.Two:
                _initializeLocation.x += pIsRedSide ? 2.2f : -2.2f;
                SpawnBar(_twoPlayerSoccerBarPrefab, pNConnection, _initializeLocation);
                break;
            case Net_SoccerBar.BarDisposition.Three:
                _initializeLocation.x += pIsRedSide ? -1.4f : 1.4f;
                SpawnBar(_threePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
                break;
            case Net_SoccerBar.BarDisposition.Five:
                _initializeLocation.x += pIsRedSide ? .4f : -.4f;
                SpawnBar(_fivePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
                break;
        }
    }

    private void SpawnBlueBar(Net_SoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        SpawnBar(false, pBarDisposition, pNConnection);
    }

    private void SpawnRedBar(Net_SoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        SpawnBar(true, pBarDisposition, pNConnection);
    }

    // Field side to know which side we must spawn bars (because it can be random), and network connection to owner
    // (can be server if host, or fully remote client)
    public void Server_InstantiateSoccerBars(Net_SoccerField.FieldSide pFieldSide, NetworkConnection pNetworkConnection)
    {
        switch (GameMode.Instance.NumberOfPlayer)
        {
            case 2:
                if (pFieldSide == Net_SoccerField.FieldSide.Red)
                    Server_InstantiateFourRedSoccerBars(pNetworkConnection);
                else if (pFieldSide == Net_SoccerField.FieldSide.Blue)
                    Server_InstantiateFourBlueSoccerBars(pNetworkConnection);
                else
                    Debug.LogError("Cannot instantiate soccer bar for undefined field side...");
                break;
            case 4:
                if (pFieldSide == Net_SoccerField.FieldSide.Red)
                {
                    // First player to join will be in attack
                    if (_soccerField.RedSidePlayers == 1)
                        Server_InstantiateTwoRedAttackSoccerBars(pNetworkConnection);
                    else
                        Server_InstantiateTwoRedDefenseSoccerBars(pNetworkConnection);
                }
                else if (pFieldSide == Net_SoccerField.FieldSide.Blue)
                {
                    if (_soccerField.BlueSidePlayers == 1)
                        Server_InstantiateTwoBlueAttackSoccerBars(pNetworkConnection);
                    else
                        Server_InstantiateTwoBlueDefenseSoccerBars(pNetworkConnection);
                }
                else
                {
                    throw new System.NotSupportedException();
                }

                break;
            default:
                Debug.LogError("Number of player given by game mode is not implemented. Value is : " + GameMode.Instance.NumberOfPlayer);
                break;
        }
    }

    private void Server_InstantiateFourBlueSoccerBars(NetworkConnection pNConnection)
    {
        Server_InstantiateTwoBlueDefenseSoccerBars(pNConnection);
        Server_InstantiateTwoBlueAttackSoccerBars(pNConnection);
    }

    private void Server_InstantiateFourRedSoccerBars(NetworkConnection pNConnection)
    {
        Server_InstantiateTwoRedDefenseSoccerBars(pNConnection);
        Server_InstantiateTwoRedAttackSoccerBars(pNConnection);
    }

    private void Server_InstantiateTwoRedAttackSoccerBars(NetworkConnection pNConnection)
    {
        SpawnRedBar(Net_SoccerBar.BarDisposition.Five, pNConnection);
        SpawnRedBar(Net_SoccerBar.BarDisposition.Three, pNConnection);
    }

    private void Server_InstantiateTwoRedDefenseSoccerBars(NetworkConnection pNConnection)
    {
        SpawnRedBar(Net_SoccerBar.BarDisposition.One, pNConnection);
        SpawnRedBar(Net_SoccerBar.BarDisposition.Two, pNConnection);
    }

        private void Server_InstantiateTwoBlueAttackSoccerBars(NetworkConnection pNConnection)
    {
        SpawnBlueBar(Net_SoccerBar.BarDisposition.Five, pNConnection);
        SpawnBlueBar(Net_SoccerBar.BarDisposition.Three, pNConnection);
    }

    private void Server_InstantiateTwoBlueDefenseSoccerBars(NetworkConnection pNConnection)
    {
        SpawnBlueBar(Net_SoccerBar.BarDisposition.One, pNConnection);
        SpawnBlueBar(Net_SoccerBar.BarDisposition.Two, pNConnection);
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
        pNetBall.transform.position = FindClosestBallCorner(pNetBall.transform.position);
    }

    // From the given position, will return the ball position of the closest corner (corner + ball offset)
    private Vector3 FindClosestBallCorner(Vector3 pPosition)
    {
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