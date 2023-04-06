/* using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NT_NetworkManager : MonoBehaviour
{
    private bool _hasMultiplayerStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_hasMultiplayerStarted)
            return;
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            _hasMultiplayerStarted = true;
            NetworkManager.Singleton.StartHost();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            _hasMultiplayerStarted = true;
            NetworkManager.Singleton.StartClient();
        }
    }
}
 */