using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicBehaviour : MonoBehaviour
{
    public struct MovingDistance
    {
        public Vector3 Distance; // in meters
        public float Time; // in seconds
        public float SlowingDownPercent;
    }

    public struct RotatingDegrees
    {
        public Vector3 Degrees;
        public float Time; // in seconds
        public float SlowingDownPercent;
    }

    private MovingDistance _movingDistance;

    private RotatingDegrees _rotatingDegrees;

    protected virtual void Awake()
    {
        _movingDistance = new MovingDistance();
        ResetMovingDistance();
        _rotatingDegrees = new RotatingDegrees();
        ResetRotatingDegrees();
    }

    private void ResetMovingDistance()
    {
        _movingDistance.Distance = Vector3.zero;
        _movingDistance.Time = 0f;
        _movingDistance.SlowingDownPercent = 0f;
    }

    private void ResetRotatingDegrees()
    {
        _rotatingDegrees.Degrees = Vector3.zero;
        _rotatingDegrees.Time = 0f;
        _rotatingDegrees.SlowingDownPercent = 0f;
    }

    // Moving object to go pDistance in meters, during ptime seconds (this can be slowed down over time 
    // /!\ Slowing down will reduce the distance done - it doesn't take in account the distance with slowing down activated)
    public void Move(Vector3 pDistance, float pTime, float pSlowingDownPercent = 0f)
    {
        if (_movingDistance.Time > 0f)
        {
            Debug.Log("Tried to move an already moving object - you must either stop it or wait for it to complete its movement.");
            return;
        }

        _movingDistance.Distance = pDistance / pTime;
        _movingDistance.Time = pTime;
        _movingDistance.SlowingDownPercent = pSlowingDownPercent;
    }

    public void StopMovement()
    {
        ResetMovingDistance();
    }

    public void Rotate(Vector3 pDegrees, float pTime, float pSlowingDownPercent = 0f)
    {
        if (_rotatingDegrees.Time > 0f)
        {
            Debug.Log("Tried to move an already moving object - you must either stop it or wait for it to complete its movement.");
            return;
        }

        _rotatingDegrees.Degrees = pDegrees / pTime;
        _rotatingDegrees.Time = pTime;
        _rotatingDegrees.SlowingDownPercent = pSlowingDownPercent;
    }

    public void StopRotation()
    {
        ResetRotatingDegrees();
    }

    protected virtual void Update()
    {
        if (_movingDistance.Time > 0f)
        {
            transform.localPosition += _movingDistance.Distance * Time.deltaTime;
            // transform.Translate(_movingDistance.Distance * Time.deltaTime, Space.Self);
            _movingDistance.Time -= Time.deltaTime;
            if (_movingDistance.Time <= 0f)
                OnMovementStop();
        }

        if (_rotatingDegrees.Time > 0f)
        {
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + _rotatingDegrees.Degrees * Time.deltaTime);
            // transform.Rotate(_rotatingDegrees.Degrees * Time.deltaTime, Space.Self);
            _rotatingDegrees.Time -= Time.deltaTime;
            if (_rotatingDegrees.Time <= 0f)
                OnRotationStop();
        }
    }

    private void OnMovementStop()
    {
        ResetMovingDistance();
    }

    private void OnRotationStop()
    {
        ResetRotatingDegrees();
    }
}
