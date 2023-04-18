using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Networking.Transport.Relay;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.FishyUnityTransport;
using FishNet.Transporting.Tugboat;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Button _startHostButton;

    [SerializeField]
    private Button _startClientButton;

    [SerializeField]
    private Button _startHostMultiplayerButton;
    [SerializeField]
    private Button _startClientMultiplayerButton;

    [SerializeField]
    private Button _exitGameButton;

    [SerializeField]
    private TextMeshProUGUI _joinCodeText;
    [SerializeField]
    private TMP_InputField _joinCodeInput;

    [SerializeField]
    private int _maximumOnlinePlayers = 2;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _startHostButton);
        UtilityLibrary.ThrowIfNull(this, _startClientButton);
        UtilityLibrary.ThrowIfNull(this, _startHostMultiplayerButton);
        UtilityLibrary.ThrowIfNull(this, _startClientMultiplayerButton);
        UtilityLibrary.ThrowIfNull(this, _exitGameButton);
        UtilityLibrary.ThrowIfNull(this, _joinCodeText);
    }

    // Start is called before the first frame update
    void Start()
    {
        _startHostButton.onClick.AddListener(StartHost);
        _startClientButton.onClick.AddListener(StartClient);

        _startHostMultiplayerButton.onClick.AddListener(StartMultiplayerHost);
        _startClientMultiplayerButton.onClick.AddListener(StartMultiplayerClient);

        _exitGameButton.onClick.AddListener(QuitGame);

        string[] lArguments = System.Environment.GetCommandLineArgs();
        for (int lArgumentIndex = 0; lArgumentIndex < lArguments.Length; lArgumentIndex++)
        {
            if (lArguments[lArgumentIndex] == "-hostip")
            {
                string lIp = lArguments[lArgumentIndex + 1];
                Debug.Log("Yay ! We have successfully retrieved host ip from argument. It is : " + lIp);

                InstanceFinder.TransportManager.GetTransport<Tugboat>().SetServerBindAddress(lIp, FishNet.Transporting.IPAddressType.IPv4);
                InstanceFinder.TransportManager.GetTransport<Tugboat>().SetClientAddress(lIp);
            }
        }
    }


    // ----- LAN
    private void StartHost()
    {
        InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
        if (!InstanceFinder.TransportManager.GetTransport<Multipass>().StartConnection(true, 0))
        {
            Debug.LogError("Error : server host couldn't be created !");
        }

        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    private void StartClient()
    {
        InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }
    // -----

    // ----- Multiplayer
    private async void StartMultiplayerHost()
    {
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always autheticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        List<Region> lAllRegions = await RelayService.Instance.ListRegionsAsync();

        string lChosenRegion = lAllRegions[0].Id;
        foreach (Region lRegion in lAllRegions)
        {
            if (lRegion.Id.StartsWith("europe-west"))
            {
                lChosenRegion = lRegion.Id;
                break;
            }
        }

        //Ask Unity Services to allocate a Relay server
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maximumOnlinePlayers, lChosenRegion);

        //Retrieve the Relay join code for our clients to join our party
        string JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        _joinCodeText.text = JoinCode;

        Debug.Log("Server join code : " + JoinCode);
        SettingNetworkManagerProperties(allocation, JoinCode);
    }

    private async void StartMultiplayerClient()
    {
        // No need to try if player didn't enter join code.
        if (_joinCodeInput.text.Length < 1)
        {
            return;
        }

        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always authenticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Joining server with code : " + _joinCodeInput.text);

        //Ask Unity Services for allocation data based on a join code
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(_joinCodeInput.text);

        SettingNetworkManagerProperties(allocation);
    }

    private async void SettingNetworkManagerProperties(Allocation pAllocation, string JoinCode)
    {
        InstanceFinder.NetworkManager.TransportManager.GetTransport<Multipass>().SetClientTransport<FishyUnityTransport>();
        FishyUnityTransport lTransport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishyUnityTransport>();
        lTransport.SetRelayServerData(new RelayServerData(pAllocation, "dtls"));

        // TODO: encapsulation
        if (!InstanceFinder.TransportManager.GetTransport<Multipass>().StartConnection(true, 1))
        {
            Debug.LogError("Error : server host couldn't be created !");
        }

        JoinAllocation lJoinAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCode);
        lTransport.SetRelayServerData(new RelayServerData(lJoinAllocation, "dtls"));

        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    private void SettingNetworkManagerProperties(JoinAllocation pAllocation)
    {
        InstanceFinder.NetworkManager.TransportManager.GetTransport<Multipass>().SetClientTransport<FishyUnityTransport>();
        FishyUnityTransport lTransport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishyUnityTransport>();
        lTransport.SetRelayServerData(new RelayServerData(pAllocation, "dtls"));

        // TODO: encapsulation
        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }


    // -----

    private void QuitGame()
    {
        Application.Quit();
    }

    public bool HasJoiningCode()
    {
        return _joinCodeText.text.Length > 0;
    }
}