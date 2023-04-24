using FishNet;
using FishNet.Transporting.FishyUnityTransport;
using FishNet.Transporting.Multipass;
using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

// Dictates Start game buttons logic for creating Online game
public class UI_StartGameButtonsOnline : UI_StartGameButtonsNetwork
{
    [SerializeField]
    private UI_Loader _loader;

    // Joining input field
    [SerializeField]
    private TMP_InputField _joinCodeInput;

    private Button _lastClickedButton;

    protected override void Awake()
    {
        base.Awake();

        UtilityLibrary.ThrowIfNull(this, _loader);
        UtilityLibrary.ThrowIfNull(this, _joinCodeInput);

        _loader.Hide();
    }

    protected override void SetTransport()
    {
        InstanceFinder.NetworkManager.TransportManager.GetTransport<Multipass>().SetClientTransport<FishyUnityTransport>();
    }

    protected override void StartHostClient()
    {
        StartHostClientAsync();
    }

    protected override void StartRemoteClient()
    {
        StartRemoteClientAsync();
    }

    private async void StartHostClientAsync()
    {
        if (_loader)
            _loader.Display();

        await TryStartClientAsync(_joinCodeInput.text);
        if (_loader)
            _loader.Hide();
    }

    private async void StartRemoteClientAsync()
    {
        _loader.Display();

        await StartUnityServices();
        await TryStartClientAsync(_joinCodeInput.text);

        if (_loader)
            _loader.Hide();
    }

    protected override void StartServer()
    {
        StartServerAsync();
    }

    protected override void StartClientButton_OnClick()
    {
        _lastClickedButton = _startClientButton;

        if (_joinCodeInput.text.Length < 1)
        {
            OnClientConnectionError();
            return;
        }

        base.StartClientButton_OnClick();
    }

    protected override void StartHostButton_OnClick()
    {
        _lastClickedButton = _startHostButton;
        base.StartHostButton_OnClick();
    }

    protected async void StartServerAsync()
    {
        _loader.Display();

        await StartUnityServices();
        // Region lWestEuropeRegion = await GetEUWRegion();
        await TryStartServerAsync();

        OnServerStartedAction.Invoke();
        if (_loader)
            _loader.Hide();
    }

    // When asking for online, as this is an asynchronous process, prevent to user from leaving buttons.
    // If server/client leads to an error, it will reactivate buttons, otherwise we go into game.
    protected override void SetButtonsInteractable(bool pIsInteractable)
    {
        _returnButton.interactable = pIsInteractable;
        base.SetButtonsInteractable(pIsInteractable);
    }

    protected override void OnHostStartError()
    {
        _returnButton.interactable = true;
        base.OnHostStartError();
    }

    protected override void OnClientStartError()
    {
        _returnButton.interactable = true;
        base.OnClientStartError();
    }

    protected override void OnServerConnectionError()
    {
        _loader.Hide();
        base.OnServerConnectionError();
    }

    protected override void OnClientConnectionError()
    {
        _loader.Hide();
        base.OnClientConnectionError();
    }

    private async System.Threading.Tasks.Task StartUnityServices()
    {
        // Initialize the Unity Services engine
        await UnityServices.InitializeAsync();

        // TODO: setup a real authentification system in the future.
        // Always authenticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Signing in...");
            // (bind to the SignInFailed callback before signing in to catch errors)
            AuthenticationService.Instance.SignInFailed += AuthenticationService_SignInFailed;
            // If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void AuthenticationService_SignInFailed(RequestFailedException pRequestFailedException)
    {
        Debug.Log(pRequestFailedException.Message);
        AuthenticationService.Instance.SignInFailed -= AuthenticationService_SignInFailed;

        if (_lastClickedButton == _startHostButton)
            OnServerConnectionError();
        else if (_lastClickedButton == _startClientButton)
            OnClientConnectionError();
    }

    private async System.Threading.Tasks.Task TryStartServerAsync(Region pChosenRegion = null)
    {
        // Ask Unity Services for allocation data based on a join code
        Allocation lAllocation;
        try
        {
            // TODO: make max connection variable
            // If no Region chosen, Unity Relay's QoS will take the best server
            lAllocation = await RelayService.Instance.CreateAllocationAsync(4, pChosenRegion != null ? pChosenRegion.Id : null);
            SetRelayServerData(lAllocation);
        }
        catch
        {
            Debug.LogWarning("Relay service allocation creation failed");
            OnServerConnectionError();
            _loader.Hide();
            return;
        }

        // Retrieve the Relay join code for our clients to join our party
        _joinCodeInput.text = await RelayService.Instance.GetJoinCodeAsync(lAllocation.AllocationId);
        GameState.Instance.JoinCode = _joinCodeInput.text;

        if (!InstanceFinder.TransportManager.GetTransport<Multipass>().StartConnection(true, 1))
            OnServerConnectionError();
    }

    private async System.Threading.Tasks.Task TryStartClientAsync(string pJoinCodeInput)
    {
        // Ask Unity Services for allocation data based on a join code
        JoinAllocation lAllocation;
        try
        {
            lAllocation = await RelayService.Instance.JoinAllocationAsync(pJoinCodeInput);
            Debug.Log("Setting allocation server data from joining code : " + pJoinCodeInput);
            SetRelayServerData(lAllocation);
        }
        catch
        {
            Debug.LogWarning("Relay create join code request failed");
            OnClientConnectionError();
            _loader.Hide();
            return;
        }

        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    protected async System.Threading.Tasks.Task<Region> GetEUWRegion()
    {
        List<Region> lAllRegions = await RelayService.Instance.ListRegionsAsync();

        Region lChosenRegion = lAllRegions[0];
        foreach (Region lRegion in lAllRegions)
        {
            if (lRegion.Id.StartsWith("europe-west"))
            {
                lChosenRegion = lRegion;
                break;
            }
        }

        return lChosenRegion;
    }

    private void SetRelayServerData(JoinAllocation pJoinAllocation)
    {
        FishyUnityTransport lTransport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishyUnityTransport>();
        lTransport.SetRelayServerData(new RelayServerData(pJoinAllocation, "dtls"));
    }

    private void SetRelayServerData(Allocation pAllocation)
    {
        FishyUnityTransport lTransport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishyUnityTransport>();
        lTransport.SetRelayServerData(new RelayServerData(pAllocation, "dtls"));
    }
}
