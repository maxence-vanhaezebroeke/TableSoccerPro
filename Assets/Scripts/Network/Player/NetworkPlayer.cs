using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting.Multipass;
using UnityEngine;

// Basic network player, with a camera and handle to despawn/player leaving (on escape keycode)
public class NetworkPlayer : NetworkBehaviour
{
    public Action<NetworkPlayer> OnPlayerDespawn;

    [SerializeField]
    private Camera _playerCamera;

    [SerializeField]
    private KeyCode _quitKey = KeyCode.Escape;

    [SerializeField]
    private UI_JoinCode _joinCodePrefab;

    private UI_JoinCode _joinCode;

    public new int OwnerId
    {
        get
        {
            return NetworkObject.OwnerId;
        }
    }

    protected virtual void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _joinCodePrefab);
        UtilityLibrary.ThrowIfNull(this, _playerCamera);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.Owner.IsLocalClient)
        {
            // Activate camera for local player
            _playerCamera.gameObject.SetActive(true);
            // Show join code if there is one
            TryShowJoinCode();
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (base.IsOwner)
            DestroyJoinCode();
    }

    public override void OnDespawnServer(NetworkConnection connection)
    {
        base.OnDespawnServer(connection);
        // Used for NetworkPlayerManager, to reference every connected player (server-side)
        OnPlayerDespawn?.Invoke(this);
    }

    private void TryShowJoinCode()
    {
        // If player state has a join code, show it, otherwise nothing to do.
        if (PlayerState.Instance.HasJoinCode)
        {
            if (!_joinCode)
            {
                _joinCode = Instantiate(_joinCodePrefab);
            }
            else
            {
                _joinCode.gameObject.SetActive(true);
            }
        }
    }

    private void DestroyJoinCode()
    {
        if (_joinCode)
            Destroy(_joinCode.gameObject);
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!base.IsOwner)
            return;

        if (Input.GetKeyDown(_quitKey))
        {
            Debug.Log("Shutting down...");
            if (base.IsServer)
            {
                // Stopping Tugboat
                InstanceFinder.TransportManager.GetTransport<Multipass>().StopConnection(true, 0);
                // Stopping FishyUnityTransport
                InstanceFinder.TransportManager.GetTransport<Multipass>().StopConnection(true, 1);
            }
            else
            {
                InstanceFinder.ClientManager.StopConnection();
            }
        }
    }
}
