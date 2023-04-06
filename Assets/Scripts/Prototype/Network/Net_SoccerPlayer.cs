using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Net_SoccerPlayer : MonoBehaviour
{
    #region Serialized variables

    [SerializeField]
    [Tooltip("Red side material, to color player's torso.")]
    private Material _redSideTorsoMaterial;
    [SerializeField]
    [Tooltip("Blue side material, to color player's torso.")]
    private Material _blueSideTorsoMaterial;

    [SerializeField]
    [Tooltip("Visual renderer of the player's torso.")]
    private Renderer _torsoRenderer;

    #endregion

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _redSideTorsoMaterial);
        UtilityLibrary.ThrowIfNull(this, _blueSideTorsoMaterial);
        UtilityLibrary.ThrowIfNull(this, _torsoRenderer);
    }

    public void SetFieldSide(Net_SoccerField.FieldSide pFieldSide)
    {
        switch (pFieldSide)
        {
            case Net_SoccerField.FieldSide.Red:
                _torsoRenderer.material = _redSideTorsoMaterial;
            break;
            case Net_SoccerField.FieldSide.Blue:
                _torsoRenderer.material = _blueSideTorsoMaterial;
            break;
        }
    }
}
