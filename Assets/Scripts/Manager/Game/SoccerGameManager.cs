using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using UnityEngine;

public abstract class SoccerGameManager : MonoBehaviour
{
    #region Soccer bars prefab

    [Header("Bars")]
    [SerializeField]
    private PredictedSoccerBar _onePlayerSoccerBarPrefab;

    [SerializeField]
    private PredictedSoccerBar _twoPlayerSoccerBarPrefab;

    [SerializeField]
    private PredictedSoccerBar _threePlayerSoccerBarPrefab;

    [SerializeField]
    private PredictedSoccerBar _fivePlayerSoccerBarPrefab;

    #endregion

    [Header("Soccer")]
    [SerializeField]
    private Net_SoccerField _soccerFieldPrefab;

    [SerializeField]
    private Net_Ball _soccerBallPrefab;

    protected Net_SoccerField _soccerField;
    private Net_Ball _soccerBall;

    [SerializeField]
    private Vector3 _fieldLocation = Vector3.zero;

    // To initialize bars, I must use a Vector3 as location
    // To avoid doing new Vector3() every time, I store it in here.
    private Vector3 _initializeLocation;

    // Corners are meshes that avoid ball being stuck. We don't want to return this location as ball would be inside the corner mesh
    // So apply an offset to give proper corner location for ball
    private Vector3 lBallCornerOffset = new Vector3(.075f, .1f, .075f);

    protected virtual void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _onePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _twoPlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _threePlayerSoccerBarPrefab);
        UtilityLibrary.ThrowIfNull(this, _fivePlayerSoccerBarPrefab);

        UtilityLibrary.ThrowIfNull(this, _soccerBallPrefab);
        UtilityLibrary.ThrowIfNull(this, _soccerFieldPrefab);
    }

    protected virtual void OnDestroy()
    {
        if (_soccerBall)
        {
            _soccerBall.OnGoalEnter -= Ball_OnGoalEnter;
            _soccerBall.OnBallExitBounds -= Ball_OnBallExitBounds;
        }
    }

    public abstract int NumberOfBarsRequired();

    public abstract Net_SoccerField.FieldSide GetRandomFieldSide();

    // Field side to know which side we must spawn bars (because it is random), and network connection to owner (host or remote client)
    public abstract List<PredictedSoccerBar> Server_InstantiateSoccerBars(Net_SoccerField.FieldSide pFieldSide, NetworkConnection pNetworkConnection);

    public void InstantiateField()
    {
        if (_soccerField)
        {
            Debug.Log("Soccer field already spawned - returning...");
            return;
        }

        _soccerField = Instantiate(_soccerFieldPrefab, _fieldLocation, _soccerFieldPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(_soccerField.gameObject);
    }

    public void InstantiateBall()
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
        FirstSpawnBall();

        _soccerBall.OnGoalEnter += Ball_OnGoalEnter;
        _soccerBall.OnBallExitBounds += Ball_OnBallExitBounds;
    }

    private void FirstSpawnBall()
    {
        _soccerBall.Rigidbody.AddForce(Vector3.right * .35f, ForceMode.VelocityChange);
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
        ResetBallLocation(_soccerBall);
    }

    private PredictedSoccerBar SpawnBar(PredictedSoccerBar pPrefab, NetworkConnection pNConnection, Vector3 pPosition)
    {
        PredictedSoccerBar lSoccerBar = Instantiate(pPrefab, pPosition, pPrefab.transform.rotation);
        InstanceFinder.ServerManager.Spawn(lSoccerBar.gameObject, pNConnection);
        return lSoccerBar;
    }

    private PredictedSoccerBar SpawnBar(bool pIsRedSide, PredictedSoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        _initializeLocation = _soccerField.transform.position;
        _initializeLocation.y += .5f;

        switch (pBarDisposition)
        {
            case PredictedSoccerBar.BarDisposition.One:
                _initializeLocation.x += pIsRedSide ? 3.1f : -3.1f;
                return SpawnBar(_onePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
            case PredictedSoccerBar.BarDisposition.Two:
                _initializeLocation.x += pIsRedSide ? 2.2f : -2.2f;
                return SpawnBar(_twoPlayerSoccerBarPrefab, pNConnection, _initializeLocation);
            case PredictedSoccerBar.BarDisposition.Three:
                _initializeLocation.x += pIsRedSide ? -1.4f : 1.4f;
                return SpawnBar(_threePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
            case PredictedSoccerBar.BarDisposition.Five:
                _initializeLocation.x += pIsRedSide ? .4f : -.4f;
                return SpawnBar(_fivePlayerSoccerBarPrefab, pNConnection, _initializeLocation);
        }

        throw new System.MethodAccessException("Soccer bar disposition is not valid : it is not a value inside the enumeration !");
    }

    private PredictedSoccerBar SpawnBlueBar(PredictedSoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        return SpawnBar(false, pBarDisposition, pNConnection);
    }

    private PredictedSoccerBar SpawnRedBar(PredictedSoccerBar.BarDisposition pBarDisposition, NetworkConnection pNConnection)
    {
        return SpawnBar(true, pBarDisposition, pNConnection);
    }

    protected List<PredictedSoccerBar> Server_InstantiateFourBlueSoccerBars(NetworkConnection pNConnection)
    {
        return Server_InstantiateTwoBlueDefenseSoccerBars(pNConnection).Concat(
            Server_InstantiateTwoBlueAttackSoccerBars(pNConnection)).ToList();
    }

    protected List<PredictedSoccerBar> Server_InstantiateFourRedSoccerBars(NetworkConnection pNConnection)
    {
        return Server_InstantiateTwoRedAttackSoccerBars(pNConnection).Concat(
            Server_InstantiateTwoRedDefenseSoccerBars(pNConnection)).ToList();
    }

    protected List<PredictedSoccerBar> Server_InstantiateTwoRedAttackSoccerBars(NetworkConnection pNConnection)
    {
        return new List<PredictedSoccerBar>()
        {
            SpawnRedBar(PredictedSoccerBar.BarDisposition.Five, pNConnection),
            SpawnRedBar(PredictedSoccerBar.BarDisposition.Three, pNConnection)
        };
    }

    protected List<PredictedSoccerBar> Server_InstantiateTwoRedDefenseSoccerBars(NetworkConnection pNConnection)
    {
        return new List<PredictedSoccerBar>()
        {
            SpawnRedBar(PredictedSoccerBar.BarDisposition.One, pNConnection),
            SpawnRedBar(PredictedSoccerBar.BarDisposition.Two, pNConnection)
        };
    }

    protected List<PredictedSoccerBar> Server_InstantiateTwoBlueAttackSoccerBars(NetworkConnection pNConnection)
    {
        return new List<PredictedSoccerBar>()
        {
            SpawnBlueBar(PredictedSoccerBar.BarDisposition.Five, pNConnection),
            SpawnBlueBar(PredictedSoccerBar.BarDisposition.Three, pNConnection)
        };
    }

    protected List<PredictedSoccerBar> Server_InstantiateTwoBlueDefenseSoccerBars(NetworkConnection pNConnection)
    {
        return new List<PredictedSoccerBar>()
        {
            SpawnBlueBar(PredictedSoccerBar.BarDisposition.One, pNConnection),
            SpawnBlueBar(PredictedSoccerBar.BarDisposition.Two, pNConnection)
        };
    }

    private void Ball_OnGoalEnter(Net_Ball pNetBall, Net_SoccerField.FieldSide pFieldSide)
    {
        _soccerField.Server_OnScore(pFieldSide);
        ResetBallLocation(pNetBall, pFieldSide);
    }

    private void Ball_OnBallExitBounds(Net_Ball pNetBall, Collider pCollider)
    {
        pNetBall.Rigidbody.MovePosition(FindClosestBallCorner(pNetBall.transform.position));
    }

    // From the given position, will return the ball position of the closest corner (corner + ball offset)
    private Vector3 FindClosestBallCorner(Vector3 pPosition)
    {
        Vector3[] lBlueCorners = _soccerField.GetBlueCornersPosition();
        // Take the first blue corner as default corner
        Vector3 lChosenCornerPosition = lBlueCorners[0];

        // Compare every blue to choose the nearest blue corner (ignoring the first one, since it's the default one)
        for (int lBlueCornerIndex = 1; lBlueCornerIndex < lBlueCorners.Length; lBlueCornerIndex++)
        {
            lChosenCornerPosition = UtilityLibrary.SmallestDistancePosition(pPosition, lChosenCornerPosition, lBlueCorners[lBlueCornerIndex]);
        }

        // Compare every red to choose the nearest red corner
        foreach (Vector3 lRedCorner in _soccerField.GetRedCornersPosition())
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

    // Will reset given ball to the center of the field, with a small speed depending on the given field side.
    // If no ball, game manager will take the reference to the only ball it has (_soccerBall), and if no field side, give it to red side
    public void ResetBallLocation(Net_Ball pNetBall = null, Net_SoccerField.FieldSide? pFieldSide = null)
    {
        if (pNetBall == null) pNetBall = _soccerBall;
        Rigidbody lBallRigidbody = pNetBall.Rigidbody;

        // Giving ball to blue side if given side is blue, else give it to red side (avoiding ball being stuck)
        int lRandomX = pFieldSide.HasValue && pFieldSide.Value == Net_SoccerField.FieldSide.Blue ? 1 : -1;
        // Random up/down direction
        int lRandomZ = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;

        // Reset velocity, but add extra angular velocity, for a slow reset
        lBallRigidbody.velocity = Vector3.zero;
        lBallRigidbody.angularVelocity = new Vector3(UnityEngine.Random.Range(1f, 1.7f) * lRandomX, 0f, UnityEngine.Random.Range(1f, 1.7f) * lRandomZ);

        // center of the field
        lBallRigidbody.MovePosition(_soccerField.transform.position);
    }
}