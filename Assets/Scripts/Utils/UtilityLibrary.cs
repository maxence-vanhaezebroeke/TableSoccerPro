using System.Collections.Generic;
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
            throw new System.ArgumentNullException("pVariable");
        }
    }

    // Returns pLocation1 if distance between pLocation1,position and pLocation2,position is the smallest. Return pLocation2 otherwise
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
}
 