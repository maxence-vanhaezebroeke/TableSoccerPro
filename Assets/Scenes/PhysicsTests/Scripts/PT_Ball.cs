using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PT_Ball : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision other)
    {
        PT_Bar lBar = other.gameObject.GetComponent<PT_Bar>();
        if (lBar)
        {
            lBar.OnCollisionWithBall(this, other);
        }
    }
}
