using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PT_Bar : MonoBehaviour
{
    //[SerializeField]
    private float _scrollSpeed = 700f;
    // [SerializeField]
    private float _moveSpeed = 50f;
    // [SerializeField]
    private float _zBound = 1f;
    // [SerializeField]
    private float _speedHitBoost = 1.5f;

    [SerializeField]
    private PT_PlayerFeet _playerFeet;

    private Rigidbody _rb;

    private float _mouseScroll;
    private float _mouseY;
    private float _initialZLocation;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _playerFeet);

        _rb = GetComponent<Rigidbody>();
        _rb.maxAngularVelocity = 15f;

        _initialZLocation = transform.position.z;
        _playerFeet.OnPlayerFeetEnterBallCollision += OnPlayerFeetEnterBallCollision;
    }

    void OnDestroy()
    {
        _playerFeet.OnPlayerFeetEnterBallCollision -= OnPlayerFeetEnterBallCollision;
    }

    void OnPlayerFeetEnterBallCollision(PT_PlayerFeet pPlayerFeet, PT_Ball pBall)
    {
        Rigidbody lBallRigidbody = pBall.gameObject.GetComponent<Rigidbody>();
        // Adding ball speed boost in bar rigidbody angular direction times boost
        float lFloorToPlayerAngle = Vector3.Angle(Vector3.back, _rb.angularVelocity);
        Debug.Log("Floor to player angle : " + lFloorToPlayerAngle);
        lBallRigidbody.AddForce(_rb.angularVelocity * lFloorToPlayerAngle * _speedHitBoost, ForceMode.VelocityChange);
    }

    public void OnCollisionWithBall(PT_Ball pBall, Collision pCollision)
    {
        Rigidbody lBallRigidbody = pBall.gameObject.GetComponent<Rigidbody>();
        Vector3 lReflectedBallDirection = Vector3.Reflect(lBallRigidbody.velocity, pCollision.GetContact(0).normal);
        // TODO : if player hits the ball while facing the floor, ball shouldn't be speed up !

        float _reducedForceFactor = 0.01f;
        // taking both angular and linear velocity. Scaling it down by _reducedForceFactor to fit the ball
        // NOTE: since shooting is the key, we scale by 2 the angular velocity
        float lBarForce = (Mathf.Abs(_rb.angularVelocity.z) * 3f + Mathf.Abs(_rb.velocity.z)) * _reducedForceFactor;

        Debug.Log("Angular velocity : " + _rb.angularVelocity.z + " - velocity : " + _rb.velocity.z);

        // Force will be applied on ball direction, with a speed boost, depending on bar power at the collision point (which should be the max speed of rotations)
        Vector3 lForceValue = lReflectedBallDirection * _speedHitBoost * Mathf.Max(_rb.angularVelocity.z, _rb.velocity.z);
        Debug.Log("Speed boost ! Force value : " + lForceValue);
        lBallRigidbody.AddForce(lForceValue, ForceMode.VelocityChange);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateMovements();
    }

    private void Update()
    {
        ZBound();
    }

    void ZBound()
    {
        if (transform.position.z > _initialZLocation + _zBound)
        {
            _rb.velocity = Vector3.zero;
            transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation + _zBound);
        }
        else if (transform.position.z < _initialZLocation - _zBound)
        {
            _rb.velocity = Vector3.zero;
            transform.position = new Vector3(transform.position.x, transform.position.y, _initialZLocation - _zBound);
        }
    }

    void UpdateMovements()
    {
        // ----- Update Rotation
        //_mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        float lMouseScrollFactor = 3f;
        float lAngularVelocityFactor = 0.2f;
        _mouseScroll = Input.GetAxis("Mouse ScrollWheel") * lMouseScrollFactor;

        float lAngularVelocityIncrease = Mathf.Abs(_rb.angularVelocity.z * lAngularVelocityFactor);
        /*
        // if we want to rotate the bar in opposite direction as it is turning
        if (_rb.angularVelocity.z > 0 && _mouseScroll < 0 || _rb.angularVelocity.z < 0 && _mouseScroll > 0)
        {
            // Don't accelerate the bar faster
            lAngularVelocityIncrease = 1f;
        }
        else
        {
            // if we want to bar to go faster in same direction, go even faster!
            lAngularVelocityIncrease = Mathf.Abs(_rb.angularVelocity.z * lAngularVelocityFactor);
        }
        */

        if (_mouseScroll != 0)
        {
            float lZTorqueForce = Mathf.Max(lAngularVelocityIncrease, 1) * _mouseScroll * _scrollSpeed;
            Debug.Log("T : " + Time.realtimeSinceStartup.ToString("0.00") + " -Scroll value- " + _mouseScroll + " -AngVel+- " + lAngularVelocityIncrease + " -Force- " + lZTorqueForce);
            // Add rotation force of a certain value
            _rb.AddTorque(0f, 0f, lZTorqueForce, ForceMode.Acceleration);
        }

        _mouseY = Input.GetAxis("Mouse Y");
        if (_mouseY != 0)
        {
            _rb.AddForce(0f, 0f, _mouseY * _moveSpeed, ForceMode.Acceleration);
        }
    }
}
