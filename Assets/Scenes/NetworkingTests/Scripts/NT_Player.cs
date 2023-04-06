using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NT_Player : NetworkBehaviour
{
    [SerializeField]
    private NT_Bean _beanPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && NetworkManager.LocalClientId != OwnerClientId)
        {
            NT_Bean lBean = Instantiate(_beanPrefab);
            lBean.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
