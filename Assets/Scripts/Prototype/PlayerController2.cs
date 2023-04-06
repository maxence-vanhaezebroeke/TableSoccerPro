using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2 : MonoBehaviour
{
    private List<SoccerBar> _controlledSoccerBar;
    private SoccerBar _currentlyControlledSoccerBar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void StartGame(List <SoccerBar> pSoccerBars, SoccerBar pStartingSoccerBar)
    {
        _controlledSoccerBar = pSoccerBars;
        SetCurrentlyControlledSoccerBar(pStartingSoccerBar);
    }

    void SetCurrentlyControlledSoccerBar(SoccerBar pNewControlledSoccerBar)
    {
        _currentlyControlledSoccerBar.Unpossess();
        _currentlyControlledSoccerBar = pNewControlledSoccerBar;
        _currentlyControlledSoccerBar.Possess();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Change controlled soccer bar right side
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            // Change controlled soccer bar left side
        }
    }
}
