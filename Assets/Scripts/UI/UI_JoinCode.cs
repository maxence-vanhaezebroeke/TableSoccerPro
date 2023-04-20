using UnityEngine;
using TMPro;

public class UI_JoinCode : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _infoText;

    [SerializeField]
    private TextMeshProUGUI _joinCodeText;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _infoText);
        UtilityLibrary.ThrowIfNull(this, _joinCodeText);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameState.Instance.HasJoinCode)
            Show();
        else
            // If we spawned a join code to display the game state join code, and there is none, no need to keep this alive
            Destroy(gameObject);
    }

    private void Show()
    {
        if (_joinCodeText.text != GameState.Instance.JoinCode)
            _joinCodeText.text = GameState.Instance.JoinCode;

        _infoText.alpha = 255f;
        _joinCodeText.alpha = 255f;
    }

    private void Hide()
    {
        _infoText.alpha = 0f;
        _joinCodeText.alpha = 0f;
    }

    private bool IsVisible()
    {
        return _infoText.alpha > 0f || _joinCodeText.alpha > 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (IsVisible())
                Hide();
            else
                Show();
        }
    }
}
