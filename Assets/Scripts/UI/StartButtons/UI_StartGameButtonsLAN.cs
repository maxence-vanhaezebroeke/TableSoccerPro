using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;

// Dictates Start game buttons logic for creating LAN game 
public class UI_StartGameButtonsLAN : UI_StartGameButtonsNetwork
{
    protected override void SetTransport()
    {
        InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();    
    }

    protected override void ReturnButton_OnClick()
    {
        // In order to stop connection properly, I have to set client transport before. In case player only wants to click return,
        // I have to do this to handle every case
        SetTransport();
        InstanceFinder.ClientManager.StopConnection();
        
        base.ReturnButton_OnClick();
    }

    protected override void StartClient()
    {
        InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    protected override void StartServer()
    {
        if(!InstanceFinder.TransportManager.GetTransport<Multipass>().StartConnection(true, 0))
            OnServerConnectionError();
    }
}
