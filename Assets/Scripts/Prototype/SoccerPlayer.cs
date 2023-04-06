using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerPlayer : MonoBehaviour
{
    
    [SerializeField]
    [Tooltip("At which location the player should be hanged by the bar. Only local position will matter.")]
    private Transform _barPosition;

    [SerializeField]
    [Tooltip("Soccer player feet")]
    private SoccerPlayerFeet _soccerPlayerFeet;

    private float _torqueFactor = 100f;

    // Start is called before the first frame update
    void Start()
    {
        StartGame();
    }

    public void ChangeLocalPosition(Vector3 pNewLocalPos)
    {
        // Because SoccerPlayer should be on a rotated bar, y becomes z on the bar referential
        // TODO : this is implicit, and shouldn't be
        // FIXME : BAR POSITION IS NOT WORKING !
        //pNewLocalPos.z += _barPosition.localPosition.y;
        
        transform.localPosition = pNewLocalPos;
    }

    void StartGame()
    {
        // Move the player to the correct bar position
        if (_soccerPlayerFeet)
        {
            _soccerPlayerFeet.OnSoccerPlayerFitCollisionEnter += OnFeetCollisionEnter;
        }
        else
        {
            Debug.LogError("SoccerPlayer should have feet instantiated.");
        }
    }

    void OnDestroy()
    {
        if (_soccerPlayerFeet)
        {
            _soccerPlayerFeet.OnSoccerPlayerFitCollisionEnter -= OnFeetCollisionEnter;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnFeetCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ball"))
        {
            Debug.Log("We hit ball!");
            Debug.DrawRay(other.GetContact(0).point, 
            (other.gameObject.transform.position - other.GetContact(0).point) * 10f,
            Color.red,
            3.0f);

            Ball lBall = other.gameObject.GetComponent<Ball>();

            // Calculating when we pinch the ball with the floor, and not hitting it
            float lAngle = Vector3.Angle((other.gameObject.transform.position - other.GetContact(0).point), transform.right);
            float lTorqueForce = (180f - lAngle) * _torqueFactor;
            other.gameObject.GetComponent<Rigidbody>().AddTorque(transform.right * lTorqueForce, ForceMode.Impulse);

            other.gameObject.GetComponent<Rigidbody>().AddForce(
                (other.gameObject.transform.position - other.GetContact(0).point) 
                * _soccerPlayerFeet.GetFeetPower(), 
                ForceMode.Impulse);
        }
    }
}
