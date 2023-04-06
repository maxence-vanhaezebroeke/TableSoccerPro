using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet;

[RequireComponent(typeof(Rigidbody))]
public class Net_SoccerBar : NetworkBehaviour
{
    private float MAX_BAR_FORCE = 1000f;

    public enum BarDisposition
    {
        One, // GoalKeeper
        Two, // Defenders
        Three, // Attackers
        Five // Halves
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

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _rb = GetComponent<Rigidbody>();

        if (IsServer)
        {
            // Changing max angular velocity, to get powered shots !
            _rb.maxAngularVelocity = 16f;
            // Resetting center of mass, so that soccer bar turns (Z axis) always from its origin
            // (if not, added soccer player are lowering the center of mass, making rotation not bar centered)
            _rb.centerOfMass = new Vector3(0f, 0f, 0f);
        }
        else
        {
            // If we're connecting late client, change correct side
            if (_fieldSide != Net_SoccerField.FieldSide.None)
            {
                SetSide(_fieldSide);
            }
        }

        _lastMouseY = Input.GetAxis("Mouse Y");
        _initialZLocation = transform.position.z;
        _initialMaterial = _renderer.material;
    }

    private void FieldSide_OnValueChanged(Net_SoccerField.FieldSide pPreviousValue, Net_SoccerField.FieldSide pNewValue, System.Boolean pAsServer)
    {
        if (pNewValue != Net_SoccerField.FieldSide.None)
        {
            SetSide(pNewValue);
        }
    }

    // initializing bar on client side that should "own" the bar
    // NOTE: own is not about ownership, because ownership will always be server-side.
    // The problem with client-side is that ball server-side cannot interact properly with bar client-side
    // Physics are in play, so hard to do. Client-side prediction was an idea, but
    // too complicated to implement for me now, and not solving physics issues
    [TargetRpc]
    public void InitializeClientRpc(NetworkConnection pClientConnection)
    {
        Debug.Log("I own bar : " + base.ObjectId);
        
        return;
        /*
        NetworkObject lPlayer = NetworkManager.LocalClient.PlayerObject;

        // FIXME: in this section, returning is the only thing i do to prevent next of code to go in error
        // best thing to do would be to despawn the soccer bar, or wait and find him a new player owner
        if (!lPlayer)
        {
            Debug.LogError("Error : no player found on player id : " + NetworkManager.LocalClientId + " which should have been owner of this bar...");
            return;
        }

        Net_Player lNetPlayer = lPlayer.GetComponent<Net_Player>();
        if (!lNetPlayer)
        {
            Debug.LogWarning("Error : PlayerObject found but no Net_Player.");
            return;
        }

        lNetPlayer.AddSoccerBar(this);
        */
    }

    public void Server_Initialize(int? pClientId = null)
    {
        if (!IsServer)
        {
            return;
        }

        int lOwnerNetworkId;

        if (pClientId != null)
        {
            Debug.Log("Client : " + pClientId + " own bar : " + base.ObjectId);
            lOwnerNetworkId = pClientId.Value;
        }
        else
        {
            Debug.Log("I own bar : " + base.ObjectId);
            lOwnerNetworkId = ClientManager.Connection.ClientId;
        }

        return;
        /*
        NetworkObject lPlayer = NetworkManager.Singleton.ConnectedClients[lOwnerNetworkId].PlayerObject;

        // FIXME: in this section, returning is the only thing i do to prevent next of code to go in error
        // best thing to do would be to despawn the soccer bar, or wait and find him a new player owner
        if (!lPlayer)
        {
            Debug.LogError("Error : no player found on player id : " + lOwnerNetworkId + " which should have been owner of this bar...");
            return;
        }

        Net_Player lNetPlayer = lPlayer.GetComponent<Net_Player>();
        if (!lNetPlayer)
        {
            Debug.LogWarning("Error : PlayerObject found but no Net_Player.");
            return;
        }

        lNetPlayer.AddSoccerBar(this);
        */
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

    void FixedUpdate()
    {
        if (_isControlledByPlayer)
        {
            _mouseScrollValue = Input.GetAxis("Mouse ScrollWheel") * _fieldSideFactor;
            _mouseY = Input.GetAxis("Mouse Y") * _fieldSideFactor;
            _leftClick = Input.GetMouseButtonDown(0);

            // NOTE: I have to do this, because i'm dealing with ball physics. So i can't
            // own soccer bars on clients, because they do physics too. 
            // Non-owning physic obj means that rigidbody is kinematic, & you can't apply forces to kinematic objects
            if (IsServer)
            {
                Server_UpdateMovement(_mouseScrollValue, _mouseY, _leftClick);

                // Lower blast cooldown if needed
                if (_blastCooldown > 0f)
                    _blastCooldown -= Time.deltaTime;
            }
            else
            {
                Client_UpdateMovement(_mouseScrollValue, _mouseY, _leftClick);
            }
        }
        else
        {
            Server_UpdateResetRotationTimer();
        }
    }

    void Server_UpdateMovement(float pScroll, float pMoveY, bool pLeftClickDown)
    {
        _resetRotationTimer = 0f;

        // ----- Update Rotation
        if (pScroll != 0)
        {
            float lMouseScrollFactor = pScroll * 2f;

            // if we scrolled in opposite direction as current velocity, make it count more by lowering angular velocity 
            if (Mathf.Sign(lMouseScrollFactor) != Mathf.Sign(_rb.angularVelocity.z))
                _rb.angularVelocity *= .4f;

            // Add torque force to our bar to spin
            _rb.AddTorque(0f, 0f, lMouseScrollFactor * _scrollSpeed, ForceMode.Acceleration);
        }
        // -----

        // ----- Update Movement
        // If mouse changed
        if (pMoveY != _lastMouseY)
        {
            // in bounds, add normal force
            _rb.AddForce(0f, 0f, pMoveY * _barSpeed, ForceMode.Acceleration);

            _lastMouseY = pMoveY;
        }

        // Predict next z position, to see if it's gonna be out of bounds
        // NOTE: this prediction is not calculating WITH applied force that we would do at the end
        // so it should be inaccurate (but still do the job in our case)
        float lNextZValue = transform.position.z + _rb.velocity.z * Time.fixedDeltaTime;
        if (lNextZValue > _initialZLocation + _zBound)
        {
            // if Z+ bound, sets position just under it, and reset velocity
            transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation + _zBound);
            _rb.velocity = Vector3.zero;
        }
        else if (lNextZValue < _initialZLocation - _zBound)
        {
            // if Z- bound, sets position just above it, and reset velocity
            transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation - _zBound);
            _rb.velocity = Vector3.zero;
        }

        // Left click = power shot, maximum power !
        if (pLeftClickDown)
        {
            _rb.angularVelocity = Vector3.zero;
            _rb.AddTorque(0f, 0f, MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Impulse);

            StartCoroutine(nameof(AfterPowerShot));
        }
        // -----
    }

    // Called right after player makes a powershot
    private IEnumerator AfterPowerShot()
    {
        // First, wait a bit
        yield return new WaitForSeconds(0.04f);
        // Apply force again, so that power is still at max speed !
        _rb.AddTorque(0f, 0f, MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Impulse);
        yield return new WaitForSeconds(0.035f);
        // After the full shot, send player back
        _rb.AddTorque(0f, 0f, -MAX_BAR_FORCE * _fieldSideFactor, ForceMode.Impulse);
        yield return new WaitForSeconds(0.21f);
        // When player is at start location, reduce its speed
        _rb.angularVelocity *= 0.1f;
    }

    private void Server_UpdateResetRotationTimer()
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
                    float lDirection = _rb.transform.rotation.eulerAngles.z > 180f ? 1 : -1;
                    // Moving rigidbody towards 0° angle.
                    _rb.AddTorque(new Vector3(0f, 0f, 5f * lDirection), ForceMode.Acceleration);
                }
            }
        }
        else
        {
            // Increase timer
            _resetRotationTimer += Time.deltaTime;
        }
    }

    // NOTE: only thing I can do here is send scroll and mouse movement value to server
    // I'm using NetworkTransform, which doesn't allow me to move it before server does it and confirm move
    // (which will cause lags on player movement, I know, but I'm doing with what I can do for now)
    private void Client_UpdateMovement(float pScroll, float pMoveY, bool pLeftClickDown)
    {
        Server_UpdateMovementServerRpc(pScroll, pMoveY, pLeftClickDown);
    }

    [ServerRpc(RequireOwnership = false)]
    private void Server_UpdateMovementServerRpc(float pScroll, float pMoveY, bool pLeftClickDown)
    {
        Server_UpdateMovement(pScroll, pMoveY, pLeftClickDown);
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

        Debug.Log("AngVelZ- " + _rb.angularVelocity.z + " -vel- " + _rb.velocity.z + " -speedForce- " + lBarForce + " -reducedYForce- " + lReducedYForce);
        // if force value is high enough in one of our directions
        if (lBarForce > 1.2f)
        {
            Debug.DrawLine(pCollision.GetContact(0).point, pCollision.GetContact(0).point - lForceValue, Color.red, 4f);

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