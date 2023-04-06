using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PT_PlayerFeet : MonoBehaviour
{
    public Action<PT_PlayerFeet, PT_Ball> OnPlayerFeetEnterBallCollision;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnCollisionWithBall(PT_Ball pBall)
    {
        Debug.Log("Hit the ball !");
        OnPlayerFeetEnterBallCollision.Invoke(this, pBall);
    }
}
