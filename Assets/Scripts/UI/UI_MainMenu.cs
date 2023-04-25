using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField]
    private Button _startGameLanButton;

    [SerializeField]
    private Button _startGameOnlineButton;

    [SerializeField]
    private TMP_Dropdown _numberOfPlayerDropdown;

    [SerializeField]
    private Button _quitButton;

    [SerializeField]
    private UI_StartGameButtonsLAN _startGameButtonsLAN;

    [SerializeField]
    private UI_StartGameButtonsOnline _startGameButtonsOnline;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _startGameLanButton);
        UtilityLibrary.ThrowIfNull(this, _startGameOnlineButton);
        UtilityLibrary.ThrowIfNull(this, _numberOfPlayerDropdown);
        UtilityLibrary.ThrowIfNull(this, _quitButton);

        UtilityLibrary.ThrowIfNull(this, _startGameButtonsLAN);
        UtilityLibrary.ThrowIfNull(this, _startGameButtonsOnline);

        // Default : sub-menu disabled
        _startGameButtonsLAN.gameObject.SetActive(false);
        _startGameButtonsOnline.gameObject.SetActive(false);

        _startGameLanButton.onClick.AddListener(StartGameLanButton_OnClick);
        _startGameOnlineButton.onClick.AddListener(StartGameOnlineButton_OnClick);
        _numberOfPlayerDropdown.onValueChanged.AddListener(NumberOfPlayerDropdown_OnValueChanged);
        _quitButton.onClick.AddListener(QuitButton_OnClick);
    }

    void Start()
    {
        // When going back from game to menu, need to reset mouse cursor to be usable by players.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void StartGameLanButton_OnClick()
    {
        SwitchToSubMenu(_startGameButtonsLAN);
    }

    private void StartGameOnlineButton_OnClick()
    {
        SwitchToSubMenu(_startGameButtonsOnline);
    }

    private void NumberOfPlayerDropdown_OnValueChanged(int pValue)
    {
        GameMode.Instance.SetNumberOfPlayer(Int32.Parse(_numberOfPlayerDropdown.options[pValue].text));
    }

    private void QuitButton_OnClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartGameButtons_OnReturnClicked(UI_StartGameButtons pStartGameButtons)
    {
        pStartGameButtons.OnReturnClicked_Action -= StartGameButtons_OnReturnClicked;
        pStartGameButtons.gameObject.SetActive(false);
        SetMainMenuInteractable(true);
    }

    private void SwitchToSubMenu(UI_StartGameButtons pStartGameButtons)
    {
        SetMainMenuInteractable(false);
        pStartGameButtons.gameObject.SetActive(true);
        pStartGameButtons.OnReturnClicked_Action += StartGameButtons_OnReturnClicked;
    }

    private void SetMainMenuInteractable(bool pIsInteractable)
    {
        _startGameLanButton.gameObject.SetActive(pIsInteractable);
        _startGameOnlineButton.gameObject.SetActive(pIsInteractable);
        _numberOfPlayerDropdown.interactable = pIsInteractable;
    }
}
