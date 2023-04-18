using UnityEngine;
using TMPro;
using System.Collections;

// Logic class for Start game buttons - handles network logic on buttons clicked
public abstract class UI_StartGameButtonsNetwork : UI_StartGameButtons
{
    [SerializeField]
    protected TextMeshProUGUI _hostErrorText;

    [SerializeField]
    protected TextMeshProUGUI _clientErrorText;

    protected override void Awake()
    {
        base.Awake();

        UtilityLibrary.ThrowIfNull(this, _hostErrorText);
        UtilityLibrary.ThrowIfNull(this, _clientErrorText);
    }

    protected override void StartHostButton_OnClick()
    {
        base.StartHostButton_OnClick();

        SetTransport();
        StartHost();
    }

    protected override void StartClientButton_OnClick()
    {
        base.StartClientButton_OnClick();

        SetTransport();
        StartClient();
    }

    // Call this method in children if server creation couldn't be done
    protected virtual void OnServerConnectionError()
    {
        DisplayHostTextError();
    }

    // Call this method in children if server connection / client creation couldn't be done
    protected virtual void OnClientConnectionError()
    {
        DisplayClientTextError();
    }

    private void DisplayHostTextError()
    {
        StartCoroutine(nameof(DisplayTextError), true);
    }

    private void DisplayClientTextError()
    {
        StartCoroutine(nameof(DisplayTextError), false);
    }

    private IEnumerator DisplayTextError(bool pIsHost)
    {
        TextMeshProUGUI lText = pIsHost ? _hostErrorText : _clientErrorText;
        lText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (pIsHost)
            OnHostStartError();
        else
            OnClientStartError();

        yield return new WaitForSeconds(1f);
        lText.gameObject.SetActive(false);
    }

    private void HideHostTextError()
    {
        _hostErrorText.gameObject.SetActive(false);
    }

    private void HideClientTextError()
    {
        _clientErrorText.gameObject.SetActive(false);
    }

    // this class should be overriden by LAN/Online transport
    protected abstract void SetTransport();

    protected virtual void StartHost()
    {
        StartServer();
        StartClient();
    }

    protected abstract void StartServer();

    protected abstract void StartClient();
}
