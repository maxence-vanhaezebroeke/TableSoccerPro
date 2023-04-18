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

    // Joining code displayer & input field
    [SerializeField]
    private TextMeshProUGUI _joinCodeText;
    [SerializeField]
    private TMP_InputField _joinCodeInput;

    private Button _lastClickedButton;

    protected override void Awake()
    {
        base.Awake();

        UtilityLibrary.ThrowIfNull(this, _loader);
        UtilityLibrary.ThrowIfNull(this, _joinCodeText);
        UtilityLibrary.ThrowIfNull(this, _joinCodeInput);

        _loader.Hide();
    }

    protected override void SetTransport()
    {
        InstanceFinder.NetworkManager.TransportManager.GetTransport<Multipass>().SetClientTransport<FishyUnityTransport>();
    }

    protected override void StartClient()
    {
        StartClientAsync();
    }

    protected override void StartServer()
    {
        StartServerAsync();
    }

    protected override void StartHost()
    {
        StartHostAsync();
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

    // NOTE: I tried to avoid code duplicate, but with asynchronous methods, it's really easier for me to seperate
    // host, server and client asynchronous methods.
    private async void StartHostAsync()
    {
        _loader.Display();

        await StartUnityServices();
        Region lChosenRegion = await GetBestRegion();
        await TryStartServerAsync(lChosenRegion);
        await TryStartClientAsync(_joinCodeInput.text);

        _loader.Hide();
    }

    private async void StartClientAsync()
    {
        _loader.Display();

        await StartUnityServices();
        await TryStartClientAsync(_joinCodeInput.text);

        _loader.Hide();
    }

    protected async void StartServerAsync()
    {
        _loader.Display();

        await StartUnityServices();
        Region lChosenRegion = await GetBestRegion();
        await TryStartServerAsync(lChosenRegion);

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
            Debug.Log("Trying to sign in...");
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

    private async System.Threading.Tasks.Task TryStartServerAsync(Region pChosenRegion)
    {
        // Ask Unity Services for allocation data based on a join code
        Allocation lAllocation;
        try
        {
            lAllocation = await RelayService.Instance.CreateAllocationAsync(2, pChosenRegion.Id);
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
        _joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(lAllocation.AllocationId);

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

    protected async System.Threading.Tasks.Task<Region> GetBestRegion()
    {
        List<Region> lAllRegions = await RelayService.Instance.ListRegionsAsync();

        Region lChosenRegion = lAllRegions[0];
        foreach (Region lRegion in lAllRegions)
        {
            // TODO: don't return europe-west, but the lowest latency available region
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
