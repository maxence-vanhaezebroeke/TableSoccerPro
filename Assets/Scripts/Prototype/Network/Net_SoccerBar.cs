using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet;
using FishNet.Object.Prediction;
using FishNet.Transporting;

[RequireComponent(typeof(Rigidbody))]
public class Net_SoccerBar : NetworkBehaviour
{
    private readonly float MAX_BAR_FORCE = 1000f;

    public enum BarDisposition
    {
        One, // GoalKeeper
        Two, // Defenders
        Three, // Attackers
        Five // Halves
    }

    public struct MoveData : IReplicateData
    {
        // tells if we need to do powershot, or move the bar
        public bool IsPowerShot;
        // if bar is out of bounds, we need to move it inside
        public bool IsOutOfBounds;
        // regular velocity / angularVelocity forces that we would want to apply
        public Vector3 AddedForce;
        public Vector3 AddedTorque;

        // Needed stuff for IReplicateData (prediction)
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #region Exposed variables

    [SerializeField]
    private float _scrollSpeed = 600f;

    [SerializeField]
    private float _barSpeed = 40f;

    [SerializeField]
    private float _zBound = 1f;
    [SerializeField]
    private float _speedHitBoost = 1.8f;

    [SerializeField]
    [Tooltip("Indicate which disposition (i.e. : how many players there will be on the bar).")]
    private BarDisposition _barDisposition;

    [SerializeField]
    [Tooltip("Every SoccerPlayer attached to the bar")]
    private List<Net_SoccerPlayer> _soccerPlayers;

    [SerializeField]
    [Tooltip("Material applied to bar when it is possessed by a player.")]
    private Material _possessedMaterial;

    [SerializeField]
    [Tooltip("Bar renderer")]
    private Renderer _renderer;

    [SerializeField]
    private PS_Blast _blastPrefab;

    #endregion

    // Simple rigidbody reference (since its on RequireComponent)
    private Rigidbody _rb;

    // Stores the initial Z location of the bar, used to reset it and calculates Z bounds
    private float _initialZLocation;

    // Stores initial material, to restore it once we want to revert the new material. (at this time : when we highlight bar)
    private Material _initialMaterial;

    // If bar is controlled by player, bar can respond to player input and move. Otherwise, bar won't move on player input
    private bool _isControlledByPlayer = false;

    // Reset rotation timer, active when bar is not controlled. At 1s, it lowers every player down
    private float _resetRotationTimer = 0f;

    // When blast is spawned, enters in cooldown before this bar can blast again.
    // Cooldown is added to this value, and when variables is at 0, blast can be made
    private float _blastCooldown = 0f;

    [SyncVar(OnChange = nameof(FieldSide_OnValueChanged))]
    private Net_SoccerField.FieldSide _fieldSide;

    #region Mouse movement

    // Instead of creating a variable in the update, I keep it in class
    // Because I need it all the time, so no memory allocation time
    private float _mouseScrollValue;
    // Same as above
    private float _mouseY;
    // Same as above
    private bool _leftClick;

    private float _lastMouseY;

    // When player is on blue side, factor is 1. When red side, factor is -1.
    // This is used to invert controls when a player is changing side.
    [SyncVar]
    private int _fieldSideFactor = 1;

    #endregion

    #region Client-side Prediction

    // Default values for prediction. Changing them in CSP-way will cause CSP to activate
    // NOTE: Unity's not detecting movementQueud as used variable, even though it is for CSP, so remove warning
#pragma warning disable 0414
    private bool _movementQueued = false;
#pragma warning restore 0414
    private bool _isOutOfBounds = false;
    private bool _isPowerShotQueued = false;
    private Vector3 _addedForceQueued;
    private Vector3 _addedTorqueQueued;

    // Used to know when coroutine for powershots is active
    private bool _isPowerShotActive;

    #endregion


    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (base.TimeManager)
        {
            base.TimeManager.OnTick += TimeManager_OnTick;
            base.TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        _rb = GetComponent<Rigidbody>();

        // Changing max angular velocity, to get powered shots !
        _rb.maxAngularVelocity = 16f;
        // Inertia Tensor is a rotational analog of mass : the larger the inertia component is, the more torque that is required to
        // achieve the same angular acceleration. Point is : every soccer bar should rotate at the EXACT same speed. So define it 
        // as the same for every soccer bar.
        _rb.inertiaTensor = new Vector3(0f, 0f, 35f);
        // Resetting center of mass, so that soccer bar turns (Z axis) always from its origin
        // (if not, added soccer player are lowering the center of mass, making rotation not bar centered)
        _rb.centerOfMass = new Vector3(0f, 0f, 0f);
        // If we're connecting late client, change correct side
        if (_fieldSide != Net_SoccerField.FieldSide.None)
        {
            SetSide(_fieldSide);
        }

        _lastMouseY = Input.GetAxis("Mouse Y");
        _initialZLocation = transform.position.z;
        _initialMaterial = _renderer.material;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (base.TimeManager)
        {
            base.TimeManager.OnTick -= TimeManager_OnTick;
            base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    private void TimeManager_OnTick()
    {
        if (base.IsOwner)
        {
            Reconcile(default, false);
            BuildActions(out MoveData md);
            Move(md, false);
        }

        if (base.IsServer)
        {
            Move(default, true);
        }
    }

    [Reconcile]
    private void Reconcile(ReconcileData pRD, bool pAsServer, Channel channel = Channel.Reliable)
    {
        // Power shot is a complex physics operation, where forces are applied in very small amount of time.
        // Reconcile cannot be done during this time, otherwise client will always be desync with servers, even though
        // his prediction will be correct. So, wait for powershot, then reconcile everything after.
        if (_isPowerShotActive)
            return;

        transform.position = pRD.Position;
        transform.rotation = pRD.Rotation;
        _rb.velocity = pRD.Velocity;
        _rb.angularVelocity = pRD.AngularVelocity;
    }

    private void BuildActions(out MoveData md)
    {
        md = default;
        md.IsPowerShot = _isPowerShotQueued;
        md.AddedForce = _addedForceQueued;
        md.AddedTorque = _addedTorqueQueued;
        md.IsOutOfBounds = _isOutOfBounds;

        // Resetting infos for next tick
        _movementQueued = false;
        _isPowerShotQueued = false;
        _isOutOfBounds = false;
        _addedForceQueued = Vector3.zero;
        _addedTorqueQueued = Vector3.zero;
    }

    [Replicate]
    private void Move(MoveData pMoveData, bool pAsServer, Channel channel = Channel.Reliable, bool replaying = false)
    {
        if (pMoveData.IsPowerShot)
        {
            _rb.angularVelocity = Vector3.zero;
            _rb.AddTorque(0f, 0f, MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Acceleration);
            StartCoroutine(nameof(AfterPowerShot));

            return;
        }

        if (pMoveData.IsOutOfBounds)
        {
            _rb.velocity = Vector3.zero;
            if (transform.position.z > _initialZLocation + _zBound)
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation + _zBound);
            else if (transform.position.z < _initialZLocation - _zBound)
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation - _zBound);
        }
        else if (pMoveData.AddedForce != Vector3.zero)
        {
            _rb.AddForce(pMoveData.AddedForce, ForceMode.Acceleration);
        }

        if (pMoveData.AddedTorque != Vector3.zero)
        {
            _rb.AddTorque(pMoveData.AddedTorque, ForceMode.Acceleration);
        }
    }

    private void TimeManager_OnPostTick()
    {
        if (base.IsServer)
        {
            // I haven't got tons of networking & physics implied, so in order to be accurate
            // with soccer bars, I reconcile every ticks
            ReconcileData rd = new ReconcileData()
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = _rb.velocity,
                AngularVelocity = _rb.angularVelocity
            };

            Reconcile(rd, true);
        }
    }

    private void FieldSide_OnValueChanged(Net_SoccerField.FieldSide pPreviousValue, Net_SoccerField.FieldSide pNewValue, System.Boolean pAsServer)
    {
        if (pNewValue != Net_SoccerField.FieldSide.None)
        {
            SetSide(pNewValue);
        }
    }

    // initializing bar on client side that own the bar
    [TargetRpc]
    public void InitializeClientRpc(NetworkConnection pClientConnection)
    {
        Debug.Log("Client - I own bar : " + base.ObjectId);

        IReadOnlyCollection<NetworkObject> lOwnObjects = pClientConnection.Objects;
        NetworkObject lPlayer = null;
        foreach (var lOwnObject in lOwnObjects)
        {
            if (lOwnObject.GetComponent<Net_Player>())
            {
                lPlayer = lOwnObject;
                break;
            }
        }

        // FIXME: in this section, returning is the only thing i do to prevent next of code to go in error
        // best thing to do would be to despawn the soccer bar, or wait and find him a new player owner
        if (lPlayer == null)
        {
            Debug.LogError("Error : no player found on player id : " + pClientConnection.ClientId + " which should have been owner of this bar...");
            return;
        }

        Net_Player lNetPlayer = lPlayer.GetComponent<Net_Player>();
        if (!lNetPlayer)
        {
            Debug.LogWarning("Error : PlayerObject found but no Net_Player.");
            return;
        }

        lNetPlayer.AddSoccerBar(this);
    }

    public void Server_Initialize(int? pClientId = null)
    {
        if (!IsServer)
            return;

        int lOwnerNetworkId;

        if (pClientId != null)
        {
            Debug.Log("Client : " + pClientId + " owns bar : " + base.ObjectId);
            lOwnerNetworkId = pClientId.Value;
        }
        else
        {
            Debug.Log("I own bar : " + base.ObjectId);
            lOwnerNetworkId = base.Owner.ClientId;
        }

        Net_Player lOwnerPlayer = InstanceFinder.NetworkManager.GetComponent<FNet_PlayerManager>().GetPlayerById(lOwnerNetworkId);

        // FIXME: in this section, returning is the only thing I do to prevent next of code to go in error
        // best thing to do would be to despawn the soccer bar, or wait and find him a new player owner
        if (!lOwnerPlayer)
        {
            Debug.LogError("Error : no player found on player id : " + lOwnerNetworkId + " which should have been owner of this bar...");
            return;
        }

        lOwnerPlayer.AddSoccerBar(this);
    }

    public void Possess()
    {
        _isControlledByPlayer = true;
        _renderer.material = _possessedMaterial;
    }

    public void Unpossess()
    {
        _isControlledByPlayer = false;
        _renderer.material = _initialMaterial;
    }

    public void ResetGame()
    {
        _rb.transform.rotation = Quaternion.Euler(Vector3.zero);
        _rb.angularVelocity = Vector3.zero;
        _rb.velocity = Vector3.zero;
        transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation);
    }

    void Update()
    {
        // Only owners can move bar
        if (!IsOwner)
            return;

        if (_isControlledByPlayer)
        {
            _mouseScrollValue = Input.GetAxis("Mouse ScrollWheel") * _fieldSideFactor;
            _mouseY = Input.GetAxis("Mouse Y") * _fieldSideFactor;
            _leftClick = Input.GetMouseButtonDown(0);

            UpdateMovement(_mouseScrollValue, _mouseY, _leftClick);

            // Lower blast cooldown if needed
            if (_blastCooldown > 0f)
                _blastCooldown -= Time.deltaTime;
        }
        else
        {
            UpdateResetRotationTimer();
        }
    }

    void UpdateMovement(float pScroll, float pMoveY, bool pLeftClickDown)
    {
        _resetRotationTimer = 0f;

        // ----- Update Movement
        // If mouse changed
        if (pMoveY != _lastMouseY)
        {
            _movementQueued = true;
            _addedForceQueued = new Vector3(0f, 0f, pMoveY * _barSpeed);

            _lastMouseY = pMoveY;
        }

        if (transform.position.z > _initialZLocation + _zBound)
        {
            _movementQueued = true;
            _isOutOfBounds = true;
        }
        else if (transform.position.z < _initialZLocation - _zBound)
        {
            _movementQueued = true;
            _isOutOfBounds = true;
        }

        // ----- Update Rotation
        if (pScroll != 0)
        {
            _movementQueued = true;
            float lMouseScrollFactor = pScroll * 2f;
            float lZRotation = lMouseScrollFactor * _scrollSpeed;

            // if we scrolled in opposite direction as current velocity, make force count more 
            if (Mathf.Sign(lMouseScrollFactor) != Mathf.Sign(_rb.angularVelocity.z))
                lZRotation *= 1.25f;

            _addedTorqueQueued = new Vector3(0f, 0f, lZRotation);
        }
        // -----

        // Left click = power shot, maximum power !
        if (pLeftClickDown)
        {
            _movementQueued = true;
            _isPowerShotQueued = true;
        }
        // -----
    }

    // Called right after player makes a powershot
    private IEnumerator AfterPowerShot()
    {
        // TODO: This is not dependent to physics tick, so it may be random sometimes
        // try to do a physical solution of this.

        _isPowerShotActive = true;
        // First, wait a bit
        yield return new WaitForSeconds(0.04f);
        // Apply force again, so that power is still at max speed !
        _rb.AddTorque(0f, 0f, MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Impulse);
        yield return new WaitForSeconds(0.035f);
        // After the full shot, send player back
        _rb.AddTorque(0f, 0f, -MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Impulse);
        yield return new WaitForSeconds(0.135f);
        // When player is at start location, reduce its speed
        _rb.angularVelocity *= 0.02f;
        _isPowerShotActive = false;
    }

    private void UpdateResetRotationTimer()
    {
        // If one second passed WITHOUT any movement
        if (_resetRotationTimer > 1.0f)
        {
            // if Z rotation is NOT nearly zero
            if (Mathf.Abs(_rb.transform.rotation.z) > 0.135f)
            {
                // if rigidbody is not moving too fast
                if (_rb.angularVelocity.z < 7f)
                {
                    _movementQueued = true;
                    // Moving rigidbody towards 0° angle.
                    float lDirection = _rb.transform.rotation.eulerAngles.z > 180f ? 1 : -1;
                    _addedTorqueQueued = new Vector3(0f, 0f, 5f * lDirection);
                }
            }
        }
        else
        {
            // Increase timer
            _resetRotationTimer += Time.deltaTime;
        }
    }

    public void OnCollisionWithBall(Net_Ball pBall, Collision pCollision)
    {
        Rigidbody lBallRigidbody = pBall.gameObject.GetComponent<Rigidbody>();
        Vector3 lReflectedBallDirection = Vector3.Reflect(lBallRigidbody.velocity, pCollision.GetContact(0).normal).normalized;
        // TODO : if player hits the ball while facing the floor, ball shouldn't be speed up !

        float _reducedForceFactor = 0.22f;
        // taking both angular and linear velocity. Scaling it down by _reducedForceFactor to fit the ball
        float lBarForce = (Mathf.Abs(_rb.angularVelocity.z) + Mathf.Abs(_rb.velocity.z)) * _reducedForceFactor;

        // FIXME: this is not working so well... Fix it or remove it 
        // if player tries to shoot the ball to the floor, ball must be stopped
        // (*-5 because : scale it a bit up, & its a negative value - i want my force to be positive)
        float lReducedYForce = lReflectedBallDirection.y < 0 ? lReflectedBallDirection.y * lBarForce * 5f : 1;

        // Only consider X & Z speed boost, to avoid getting the ball flying
        Vector3 lForceValue;
        lForceValue.x = lReflectedBallDirection.x * _speedHitBoost * lBarForce - lReducedYForce;
        lForceValue.y = 0f;
        lForceValue.z = lReflectedBallDirection.z * _speedHitBoost * lBarForce - lReducedYForce;

        // FIXME: ball is going in crazy directions sometimes... Reflected vector is maybe not in the direction I expected
        Debug.Log("AngVelZ- " + _rb.angularVelocity.z + " -vel- " + _rb.velocity.z + " -speedForce- " + lBarForce + " -reducedYForce- " + lReducedYForce);
        // if force value is high enough in one of our directions
        if (lBarForce > 1.2f)
        {
            Debug.DrawLine(pCollision.GetContact(0).point, pCollision.GetContact(0).point + lReflectedBallDirection, Color.red, 4f);

            // If blast is not in cooldown
            if (_blastCooldown <= 0f)
            {
                // We blast !
                float lBlastRadius = lForceValue.magnitude * 0.25f;
                Server_Blast(lBlastRadius, pCollision.GetContact(0).point);
                All_BlastRpc(lBlastRadius, pCollision.GetContact(0).point);
                // Setting blast timer to 1
                _blastCooldown = 1f;
            }
        }

        // Force will be applied on ball direction, with a speed boost, depending on bar power at the collision point (which should be the max speed of rotations)
        lBallRigidbody.AddForce(lForceValue, ForceMode.VelocityChange);
    }

    private void Server_Blast(float pBlastRadius, Vector3 pLocation)
    {
        Blast(pBlastRadius, pLocation);
    }

    [ObserversRpc]
    private void All_BlastRpc(float pBlastRadius, Vector3 pLocation)
    {
        if (IsServer)
            return;

        Blast(pBlastRadius, pLocation);
    }

    private void Blast(float pBlastRadius, Vector3 pLocation)
    {
        _blastPrefab._maxRadius = pBlastRadius;
        _blastPrefab._speed = pBlastRadius * 1.25f;
        Instantiate(_blastPrefab, pLocation, _blastPrefab.transform.rotation);
    }

    public int NumberOfPlayers()
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

    public void Server_SetRedSide()
    {
        // --- Server-side changes for set red side
        if (!IsServer)
        {
            Debug.LogWarning("Calling set function on a non-server instance. Returning...");
            return;
        }

        Debug.Log("Bar : " + base.ObjectId + " is red side !");
        _fieldSideFactor = -1;
        // ---
        // Setting field side
        _fieldSide = Net_SoccerField.FieldSide.Red;
    }

    public void Server_SetBlueSide()
    {
        if (!IsServer)
            return;

        if (_fieldSideFactor != 1)
            _fieldSideFactor = 1;

        _fieldSide = Net_SoccerField.FieldSide.Blue;
    }

    private void SetSide(Net_SoccerField.FieldSide pFieldSide)
    {
        foreach (Net_SoccerPlayer lSoccerPlayer in _soccerPlayers)
        {
            lSoccerPlayer.SetFieldSide(pFieldSide);
        }
    }
}