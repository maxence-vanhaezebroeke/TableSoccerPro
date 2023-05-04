
using System.Collections.Generic;
using FishNet.Connection;

public class FourPlayerSoccerGameManager : SoccerGameManager
{
    public override Net_SoccerField.FieldSide GetRandomFieldSide()
    {
        return _soccerField.TakeFourPlayerRandomSide();
    }

    public override int NumberOfBarsRequired()
    {
        return 2;
    }

    public override List<PredictedSoccerBar> Server_InstantiateSoccerBars(Net_SoccerField.FieldSide pFieldSide, NetworkConnection pNetworkConnection)
    {
        if (pFieldSide == Net_SoccerField.FieldSide.Red)
        {
            // First player to join will be in attack
            if (_soccerField.RedSidePlayers == 1)
                return Server_InstantiateTwoRedAttackSoccerBars(pNetworkConnection);
            else
                return Server_InstantiateTwoRedDefenseSoccerBars(pNetworkConnection);
        }
        else if (pFieldSide == Net_SoccerField.FieldSide.Blue)
        {
            if (_soccerField.BlueSidePlayers == 1)
                return Server_InstantiateTwoBlueAttackSoccerBars(pNetworkConnection);
            else
                return Server_InstantiateTwoBlueDefenseSoccerBars(pNetworkConnection);
        }

        throw new System.NotSupportedException();
    }
}
