
using UnityEngine;

[RequireComponent(typeof(TwoPlayerSoccerGameManager), typeof(FourPlayerSoccerGameManager))]
public class GameMode : Singleton<GameMode>
{
    // Number of player that will play. Default is 2, it will be updated when player chose to do so in MainMenu.
    private int _numberOfPlayer = 2;
    public int NumberOfPlayer { get { return _numberOfPlayer; } }

    public SoccerGameManager GameManager
    {
        get
        {
            switch (_numberOfPlayer)
            {
                case 2:
                    return GetComponent<TwoPlayerSoccerGameManager>();
                case 4:
                    return GetComponent<FourPlayerSoccerGameManager>();
            }

            throw new System.MemberAccessException("No GameManager with this number of player. Number is : " + _numberOfPlayer);
        }
    }

    internal void SetNumberOfPlayer(int pNewValue)
    {
        _numberOfPlayer = pNewValue;
    }

    public void SetCursorVisibility(bool pIsVisible)
    {
        if (pIsVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
