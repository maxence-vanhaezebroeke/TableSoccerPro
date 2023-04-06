using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    // A followBar should follow the mouse
    // it'll interact with the falling balls, to deviate them
    // FollowBar can have powerups : bouncy one, bigger one, taller one, ...
    // We need a button that makes the follow bar intagible :
    // that way, if the user has the bar on top of a ball
    // He can use this to go under and still deviate the ball !
    // private FollowBar ... 
    // We can rotate the follow bar using a & e buttons (i guess)
    // (should be smth like 30°) - BETTER : rotate bar with scroll !
    // Bad powerups will sometimes get in the screen :
    // they can for example : make the followbar intangible a random
    // number of time, for a random time of seconds

    // At the botton, different bowls, smaller ones gives more points

    // We can make that if a ball goes at the border right of the screen
    // it comes back by the left of the screen

    // TODO : This is a 2D game experience. What can I do to make it a 3D one?
    // I know ! Sometimes view will change for different camera angle
    // At this point, player should have ability to flip at a certain amount
    // the bar (for example, 40° ONLY x or -40° ONLY x)
    // This way, player can put balls directly in the bowls, placed at perfect
    // locations away. There should also be bars that spawn WITH that 40° angle to 
    // help player if they are not ready to flip their bar on the x axis
    // There should be a combo to flip on X : spacebar = 0. Up + spacebar = 40. Down + spacebar = -40

    [SerializeField]
    private GameObject _followBar;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void StartGame()
    {
        // Spawn a bar that'll follow player's mouse
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFollowBarPosition();
    }

    void UpdateFollowBarPosition()
    {
        
    }
}
