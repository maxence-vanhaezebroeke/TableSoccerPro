using System;
using UnityEngine;
using UnityEngine.UI;

// Visual class for implementing Start game buttons - dictates how buttons will interact
public abstract class UI_StartGameButtons : MonoBehaviour
{
    public Action<UI_StartGameButtons> OnReturnClicked_Action;

    [SerializeField]
    protected Button _startHostButton;

    [SerializeField]
    protected Button _startClientButton;

    [SerializeField]
    protected Button _returnButton;

    protected virtual void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _startHostButton);
        UtilityLibrary.ThrowIfNull(this, _startClientButton);
        UtilityLibrary.ThrowIfNull(this, _returnButton);

        _startHostButton.onClick.AddListener(StartHostButton_OnClick);
        _startClientButton.onClick.AddListener(StartClientButton_OnClick);
        _returnButton.onClick.AddListener(ReturnButton_OnClick);
    }

    protected virtual void StartHostButton_OnClick()
    {
        SetButtonsInteractable(false);
    }

    protected virtual void StartClientButton_OnClick()
    {
        SetButtonsInteractable(false);
    }

    protected virtual void ReturnButton_OnClick()
    {
        SetButtonsInteractable(true);
        OnReturnClicked_Action.Invoke(this);
    }

    protected virtual void OnHostStartError()
    {
        SetButtonsInteractable(true);
    }

    protected virtual void OnClientStartError()
    {
        SetButtonsInteractable(true);
    }

    protected virtual void SetButtonsInteractable(bool pIsInteractable)
    {
        _startHostButton.interactable = pIsInteractable;
        _startClientButton.interactable = pIsInteractable;
    }
}
