
using System.Collections.Generic;
using FishNet.Connection;

public class TwoPlayerSoccerGameManager : SoccerGameManager
{
    public override Net_SoccerField.FieldSide GetRandomFieldSide()
    {
        return _soccerField.TakeTwoPlayerRandomSide();
    }

    public override int NumberOfBarsRequired()
    {
        return 4;
    }

    public override List<PredictedSoccerBar> Server_InstantiateSoccerBars(Net_SoccerField.FieldSide pFieldSide, NetworkConnection pNetworkConnection)
    {
        if (pFieldSide == Net_SoccerField.FieldSide.Red)
            return Server_InstantiateFourRedSoccerBars(pNetworkConnection);
        else if (pFieldSide == Net_SoccerField.FieldSide.Blue)
            return Server_InstantiateFourBlueSoccerBars(pNetworkConnection);
        
        throw new System.NotSupportedException();
    }
}
