using UnityEngine;

public class GameMode : Singleton<GameMode>
{
    [SerializeField]
    [Tooltip("How many players expected to start the game. Currently supported : 2 players")]
    private int _numberOfPlayer = 2;

    public int NumberOfPlayer { get { return _numberOfPlayer; } }

    protected override void Awake() 
    {
        base.Awake();
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
