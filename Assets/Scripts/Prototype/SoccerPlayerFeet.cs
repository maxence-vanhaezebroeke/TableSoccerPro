using System;
using UnityEngine;

public class SoccerPlayerFeet : MonoBehaviour
{
    public Action<Collision> OnSoccerPlayerFitCollisionEnter;

    [SerializeField]
    private float _feetPower;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float GetFeetPower()
    {
        return _feetPower;
    }

    void OnCollisionEnter(Collision other)
    {
        OnSoccerPlayerFitCollisionEnter.Invoke(other);   
    }
}
