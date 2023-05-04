using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UtilityLibrary
{
    public static void ThrowIfNull(Object pObject, Object pVariable)
    {
        if (pVariable == null)
        {
            Debug.LogWarning("<color=#ED7014><b>"
                + "A variable has been serialized but is empty. "
                + "Please reference this variable in the corresponding prefab. Should be in script : "
                + pObject
                + "</b></color>");
            throw new System.ArgumentNullException("Serialized variable is null.");
        }
    }

    // Returns pLocation1 if distance between pLocation1,position and pLocation2,position is the smallest, else return pLocation2
    public static Vector3 SmallestDistancePosition(Vector3 pPosition, Vector3 pLocation1, Vector3 pLocation2)
    {
        return Vector3.Distance(pPosition, pLocation1) < Vector3.Distance(pPosition, pLocation2) ? pLocation1 : pLocation2;
    }

    public static void Swap<T>(IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
    }

    public static List<NetworkSoccerBar> OrderSizeTwoSoccerBars(List<NetworkSoccerBar> pSoccerBars)
    {
        // If player has a 5 player bar, he's attacking
        if (pSoccerBars[0].NumberOfPlayers() == 5 || pSoccerBars[1].NumberOfPlayers() == 5)
        {
            // If player is attacking and first bar isn't 5 player, it's not ordered
            if (pSoccerBars[0].NumberOfPlayers() != 5)
            {
                // Swap elements
                UtilityLibrary.Swap<NetworkSoccerBar>(pSoccerBars, 0, 1);
            }
        }
        else
        {
            // If player is defending and first bar isn't goalkeeper, it's not ordered
            if (pSoccerBars[0].NumberOfPlayers() != 1)
            {
                UtilityLibrary.Swap<NetworkSoccerBar>(pSoccerBars, 0, 1);
            }
        }

        // Ordered list
        return pSoccerBars;
    }

    public static List<NetworkSoccerBar> OrderSizeFourSoccerBars(List<NetworkSoccerBar> pSoccerBars)
    {
        // Size four soccer bar disposition is [1, 2, 5, 3] (based on number of players) : 
        // - Goalkeeper (1 player)
        // - Defenders (2 players)
        // - Halves (5 players)
        // - Attackers (3 players)
        // So first, order list by ascending number of player [1, 2, 3, 5]
        List<NetworkSoccerBar> lOrderedList = pSoccerBars.OrderBy(bar => bar.NumberOfPlayers()).ToList();
        // Then swap two lasts bars, to get [1, 2, 5, 3]
        UtilityLibrary.Swap<NetworkSoccerBar>(lOrderedList, 3, 2);
        return lOrderedList;
    }
}
