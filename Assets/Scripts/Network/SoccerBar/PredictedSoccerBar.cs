using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PredictedSoccerBar : NetworkSoccerBar
{
    private readonly float MAX_BAR_FORCE = 1000f;

    #region Client-side prediction structs
    public struct MoveData : IReplicateData
    {
        // tells if we need to do powershot, or move the bar
        public bool IsPowerShot;
        public float PowerShotDirection;
        public bool StopAngularVelocity;

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
    #endregion

    #region Client-side Prediction variables

    // Default values for prediction. Changing them in CSP-way will cause CSP to activate
    // NOTE: Unity's not detecting movementQueud as used variable, even though it is for CSP, so remove warning
#pragma warning disable 0414
    private bool _movementQueued = false;
#pragma warning restore 0414
    private bool _isOutOfBounds = false;
    private bool _isPowerShotQueued = false;
    private bool _stopAngularVelocity = false;
    private float _powerShotDirection = 1f;
    private Vector3 _addedForceQueued;
    private Vector3 _addedTorqueQueued;

    // Used to know when coroutine for powershots is active
    private bool _isPowerShotActive;

    private double _powerShotTimer;

    #endregion

    #region Exposed variables
    [Header("Physics")]
    [SerializeField]
    [Tooltip("Z rotation speed, when turning the bar")]
    private float _scrollSpeed = 600f;

    [SerializeField]
    [Tooltip("Z position speed, when moving the bar")]
    private float _barSpeed = 40f;

    [SerializeField]
    [Tooltip("Z position bound, used to prevent bar going out of the field")]
    private float _zBound = 1f;

    [SerializeField]
    [Tooltip("When bar hits ball, give it a speed boost of this amount")]
    private float _speedHitBoost = 1.8f;

    [SerializeField]
    [Tooltip("Prefab of blast, spawned when bar hits ball with enough speed")]
    private PS_Blast _blastPrefab;
    #endregion

    // Simple rigidbody reference
    private Rigidbody _rb;

    // Stores the initial Z location of the bar, used to reset it and calculates Z bounds
    private float _initialZLocation;

    // Reset rotation timer, active when bar is not controlled. At 1s, it lowers every player down
    private float _resetRotationTimer = 0f;

    // When blast is spawned, enters in cooldown before this bar can blast again.
    // Cooldown is added to this value, and when variables is at 0, blast can be made
    private float _blastCooldown = 0f;

    #region Mouse movement

    // Instead of creating a variable in the update, I keep it in class
    // Because I need it all the time, so no memory allocation time
    private float _mouseScrollValue;
    // Same as above
    private float _mouseY;

    private float _lastMouseY;

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

        // Changing max angular velocity, to get powerful shots
        _rb.maxAngularVelocity = 16f;
        // Inertia Tensor is a rotational analog of mass : the larger the inertia component is, the more torque that is required to
        // achieve the same angular acceleration. Point is : every soccer bar should rotate at the EXACT same speed. So define it 
        // as the same for every soccer bar.
        _rb.inertiaTensor = new Vector3(0f, 0f, 35f);
        // Resetting center of mass, so that soccer bar turns (Z axis) always from its origin
        // (if not, added soccer player are lowering the center of mass, making rotation not bar centered)
        _rb.centerOfMass = new Vector3(0f, 0f, 0f);

        _lastMouseY = Input.GetAxis("Mouse Y");
        _initialZLocation = transform.position.z;
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

        // TODO: Don't change position, use rigidbody to MovePosition or MoveRotation !
        transform.position = pRD.Position;
        transform.rotation = pRD.Rotation;
        _rb.velocity = pRD.Velocity;
        _rb.angularVelocity = pRD.AngularVelocity;
    }

    private void BuildActions(out MoveData md)
    {
        md = default;
        md.IsPowerShot = _isPowerShotQueued;
        md.PowerShotDirection = _powerShotDirection;
        md.StopAngularVelocity = _stopAngularVelocity;
        md.AddedForce = _addedForceQueued;
        md.AddedTorque = _addedTorqueQueued;
        md.IsOutOfBounds = _isOutOfBounds;

        // Resetting infos for next tick
        _movementQueued = false;
        _isPowerShotQueued = false;
        _stopAngularVelocity = false;
        _powerShotDirection = 1f;
        _isOutOfBounds = false;
        _addedForceQueued = Vector3.zero;
        _addedTorqueQueued = Vector3.zero;
    }

    [Replicate]
    private void Move(MoveData pMoveData, bool pAsServer, Channel channel = Channel.Reliable, bool replaying = false)
    {
        // Powershot must be updated, even when player isn't controlling the bar
        UpdatePowerShot();

        // Rotation
        if (pMoveData.StopAngularVelocity)
            _rb.angularVelocity = Vector3.zero;

        if (pMoveData.IsPowerShot)
        {
            _rb.angularVelocity = Vector3.zero;
            _rb.AddTorque(0f, 0f, MAX_BAR_FORCE * FieldSideFactor * pMoveData.PowerShotDirection, ForceMode.Acceleration);
            //StartCoroutine(nameof(AfterPowerShot));
        }
        else if (pMoveData.AddedTorque != Vector3.zero)
        {
            _rb.AddTorque(pMoveData.AddedTorque, ForceMode.Acceleration);
        }

        // Movement
        if (pMoveData.IsOutOfBounds)
        {
            _rb.velocity = Vector3.zero;
            // TODO: Don't set position like this, use rigidbody MovePosition !
            if (transform.position.z > _initialZLocation + _zBound)
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation + _zBound);
            else if (transform.position.z < _initialZLocation - _zBound)
                transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation - _zBound);
        }
        // Move if not out of bounds
        else if (pMoveData.AddedForce != Vector3.zero)
        {
            _rb.AddForce(pMoveData.AddedForce, ForceMode.Acceleration);
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

    public override void ResetGame()
    {
        _rb.MovePosition(new Vector3(transform.position.x, transform.position.y, _initialZLocation));
        _rb.MoveRotation(Quaternion.Euler(Vector3.zero));
        _rb.angularVelocity = Vector3.zero;
        _rb.velocity = Vector3.zero;
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (_isControlledByPlayer)
        {
            _mouseScrollValue = Input.GetAxis("Mouse ScrollWheel") * FieldSideFactor;
            // NOTE: Known bug - when using touchpad, sometimes there is one second before user can move bar
            // (right after possessing it). Not visible with mouse, so i'll let this one pass
            _mouseY = Input.GetAxis("Mouse Y") * FieldSideFactor;

            UpdateMovement(_mouseScrollValue, _mouseY);

            // Lower blast cooldown if needed
            if (_blastCooldown > 0f)
                _blastCooldown -= Time.deltaTime;
        }
        else
        {
            UpdateResetRotationTimer();
        }
    }

    private void UpdatePowerShot()
    {
        if (_isPowerShotActive)
        {
            // power shot is active : handle it.
            _powerShotTimer += base.TimeManager.TickDelta;
            // If statement from end of the powershot to start of the powershot (_powerShotTimer will be first greater than 2, 
            // then greater than 4, then greater than 8)
            if (_powerShotTimer >= 13 * base.TimeManager.TickDelta)
            {
                // Stop velocity !
                _movementQueued = true;
                _stopAngularVelocity = true;

                // Ending powershot...
                _powerShotTimer = 0f;
                _isPowerShotActive = false;
            }
            else if (_powerShotTimer >= 6 * base.TimeManager.TickDelta)
            {
                // Reverse powershot !
                _movementQueued = true;
                _isPowerShotQueued = true;
                _powerShotDirection = -1f;
            }
            else if (_powerShotTimer >= 3 * base.TimeManager.TickDelta)
            {
                // Powershot again !
                _movementQueued = true;
                _isPowerShotQueued = true;
                _powerShotDirection = 1f;
            }
        }
    }

    private void UpdateMovement(float pScroll, float pMoveY)
    {
        _resetRotationTimer = 0f;

        // ----- Update click
        if (_isPowerShotActive == false && Input.GetMouseButtonDown(0))
        {
            _movementQueued = true;
            _powerShotDirection = 1f;
            _isPowerShotQueued = true;
            _isPowerShotActive = true;
        }

        // ----- Update Movement
        // If mouse changed
        if (pMoveY != 0)
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
            float lMouseScrollFactor = pScroll * 1.5f;
            float lZRotation = lMouseScrollFactor * _scrollSpeed;

            // if we scrolled in opposite direction as current velocity, make force count more 
            if (Mathf.Sign(lMouseScrollFactor) != Mathf.Sign(_rb.angularVelocity.z))
                lZRotation *= 3f;

            _addedTorqueQueued = new Vector3(0f, 0f, lZRotation);
        }
        // -----
    }

    private void UpdateResetRotationTimer()
    {
        // If one second passed WITHOUT any movement
        if (_resetRotationTimer > 1.0f)
        {
            // if Z rotation is NOT nearly zero
            if (Mathf.Abs(_rb.transform.rotation.z) > 0.125f)
            {
                // if rigidbody is not moving too fast
                if (_rb.angularVelocity.z < 6f)
                {
                    _movementQueued = true;
                    // Moving rigidbody towards 0° angle.
                    float lDirection = _rb.transform.rotation.eulerAngles.z > 180f ? 1 : -1;
                    _addedTorqueQueued = new Vector3(0f, 0f, 4f * lDirection);
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
        //Vector3 lReflectedBallDirection = Vector3.Reflect(lBallRigidbody.velocity, pCollision.GetContact(0).normal).normalized;

        // If ball is going forward, and bar is going forward, then ball needs to still go forward
        Vector3 lBallNewDirection = lBallRigidbody.velocity;
        // If bar and ball are in opposite direction
        if (Mathf.Sign(_rb.angularVelocity.z) != lBallRigidbody.velocity.z)
        {
            // Ball is going the normal direction of the collision
            lBallNewDirection = pCollision.GetContact(0).normal;
        }
        lBallNewDirection = lBallNewDirection.normalized;

        float _reducedForceFactor = 0.3f;
        // taking both angular and linear velocity. Scaling it down by _reducedForceFactor to fit the ball
        float lBarForce = (Mathf.Abs(_rb.angularVelocity.z) + Mathf.Abs(_rb.velocity.z)) * _reducedForceFactor;

        // TODO: Reduce force if force is going through the floor
        // float lReducedYForce = lBallNewDirection.y < 0 ? lBallNewDirection.y * lBarForce * 5f : 1;

        // Only consider X & Z speed boost, to avoid getting the ball flying
        Vector3 lForceValue;
        lForceValue.x = lBallNewDirection.x * _speedHitBoost * lBarForce; // - lReducedYForce;
        lForceValue.y = 0f;
        lForceValue.z = lBallNewDirection.z * _speedHitBoost * lBarForce; // - lReducedYForce;

        // FIXME: ball is going in crazy directions sometimes... Reflected vector is maybe not in the direction I expected
        Debug.Log("AngVelZ- " + _rb.angularVelocity.z + " -vel- " + _rb.velocity.z + " -speedForce- " + lBarForce);// + " -reducedYForce- " + lReducedYForce);
        // if force value is high enough in one of our directions
        if (lBarForce > 1.2f)
        {
            // If blast is not in cooldown
            if (_blastCooldown <= 0f)
            {
                // We blast !
                float lBlastRadius = lForceValue.magnitude * 0.25f;
                Server_Blast(lBlastRadius, pCollision.GetContact(0).point);
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
        All_BlastRpc(pBlastRadius, pLocation);
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
        _blastPrefab._speed = pBlastRadius * 1.2f;
        Instantiate(_blastPrefab, pLocation, _blastPrefab.transform.rotation);
    }
}
