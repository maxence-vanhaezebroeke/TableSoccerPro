using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerTable : MonoBehaviour
{
    // RedGoal / RedDef / BlueAttk / RedHalf / BlueHalf / RedAttk / BlueDef / BlueGoal
    private List<SoccerBar> _redSoccerBars;
    private List<SoccerBar> _blueSoccerBars;


    public List<SoccerBar> GetRedSoccerBars()
    {
        return _redSoccerBars;
    }

    public List<SoccerBar> GetBlueSoccerBars()
    {
        return _blueSoccerBars;
    }

    // The starting soccer bar is the soccer bar with the most player on it
    // (because we want to start the game with the most player bar)
    public SoccerBar GetStartingSoccerBar(List<SoccerBar> pSoccerBars)
    {
        SoccerBar lBestSoccerBarToStart = pSoccerBars[0];
        for (int lSoccerBarsIndex = 1; lSoccerBarsIndex < pSoccerBars.Count; lSoccerBarsIndex++)
        {
            if (pSoccerBars[lSoccerBarsIndex].Players() > lBestSoccerBarToStart.Players())
            {
                lBestSoccerBarToStart = pSoccerBars[lSoccerBarsIndex];
            }
        }
        return lBestSoccerBarToStart;
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
