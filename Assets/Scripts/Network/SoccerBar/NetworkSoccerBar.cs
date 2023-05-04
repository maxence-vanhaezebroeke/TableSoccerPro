using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Contains network logic for a soccer bar (field side, players, possessing, etc.)
public class NetworkSoccerBar : NetworkBehaviour
{
    public enum BarDisposition
    {
        One, // GoalKeeper
        Two, // Defenders
        Three, // Attackers
        Five // Halves
    }

    [Header("Info")]
    [SerializeField]
    [Tooltip("Indicate which disposition (i.e. : how many players there will be on the bar).")]
    private BarDisposition _barDisposition;

    [SerializeField]
    [Tooltip("Every SoccerPlayer attached to the bar")]
    private List<Net_SoccerPlayer> _soccerPlayers;

    [SerializeField]
    [Tooltip("Material applied to bar when it is possessed by a player.")]
    private Material _possessedMaterial;

    [SerializeField]
    [Tooltip("Bar renderer")]
    private Renderer _renderer;

    public BarDisposition GetBarDisposition { get { return _barDisposition; } }

    // Stores initial material, to restore it once we want to revert the new material. (at this time : when we highlight bar)
    private Material _initialMaterial;

    // If bar is controlled by player, bar can respond to player input and move. Otherwise, bar won't move on player input
    protected bool _isControlledByPlayer = false;

    [SyncVar(OnChange = nameof(FieldSide_OnValueChanged))]
    private Net_SoccerField.FieldSide _fieldSide;

    // This is used to invert controls when a player is changing side.
    protected int FieldSideFactor
    {
        get
        {
            return _fieldSide == Net_SoccerField.FieldSide.Blue ? 1 : _fieldSide == Net_SoccerField.FieldSide.Red ? -1 : 0;
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

/* No need to do this, since it's a sync var, client should trigger OnChange
        if (_fieldSide != Net_SoccerField.FieldSide.None)
        {
            SetSide(_fieldSide);
        }
*/
        _initialMaterial = _renderer.material;
    }

    public int NumberOfPlayers()
    {
        switch (_barDisposition)
        {
            case BarDisposition.One:
                return 1;
            case BarDisposition.Two:
                return 2;
            case BarDisposition.Three:
                return 3;
            case BarDisposition.Five:
                return 5;
            default:
                return 0;
        }
    }

    public void Possess()
    {
        _isControlledByPlayer = true;
        _renderer.material = _possessedMaterial;
    }

    public void Unpossess()
    {
        _isControlledByPlayer = false;
        _renderer.material = _initialMaterial;
    }

    public virtual void ResetGame()
    {
        throw new System.NotImplementedException("Implement Reset Game function");
    }

    public void SetSide(Net_SoccerField.FieldSide pFieldSide)
    {
        if (pFieldSide == Net_SoccerField.FieldSide.None)
            return;

        _fieldSide = pFieldSide;
    }

    private void FieldSide_OnValueChanged(Net_SoccerField.FieldSide pPreviousValue, Net_SoccerField.FieldSide pNewValue, System.Boolean pAsServer)
    {
        SetFieldSide(pNewValue);
    }

    private void SetFieldSide(Net_SoccerField.FieldSide pFieldSide)
    {
        foreach (Net_SoccerPlayer lSoccerPlayer in _soccerPlayers)
        {
            lSoccerPlayer.SetFieldSide(pFieldSide);
        }
    }
}
