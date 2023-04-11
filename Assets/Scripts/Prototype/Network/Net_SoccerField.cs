using UnityEngine;
using FishNet.Object;
using TMPro;
using System.Collections.Generic;

public class Net_SoccerField : NetworkBehaviour
{
    public enum FieldSide
    {
        None,
        Red,
        Blue
    }
    
    [SerializeField]
    private Transform _blueSideTransform;

    [SerializeField]
    private Transform _redSideTransform;

    [SerializeField]
    private TextMeshPro _blueSideScoreText;
    [SerializeField]
    private TextMeshPro _redSideScoreText;

    [SerializeField]
    private List<ParticleSystem> _blueSideParticleSystems;
    [SerializeField]
    private List<ParticleSystem> _redSideParticleSystems;

    [Header("Corners :")]
    
    [SerializeField]
    private Transform _blueCorner1;
    
    [SerializeField]
    private Transform _blueCorner2;

    [SerializeField]
    private Transform _redCorner1;

    [SerializeField]
    private Transform _redCorner2;

    private bool _isBlueSideTaken = false;
    private bool _isRedSideTaken = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        UtilityLibrary.ThrowIfNull(this, _blueSideTransform);
        UtilityLibrary.ThrowIfNull(this, _redSideTransform);
        UtilityLibrary.ThrowIfNull(this, _blueSideScoreText);
        UtilityLibrary.ThrowIfNull(this, _redSideScoreText);
        
        UtilityLibrary.ThrowIfNull(this, _blueCorner1);
        UtilityLibrary.ThrowIfNull(this, _blueCorner2);
        UtilityLibrary.ThrowIfNull(this, _redCorner1);
        UtilityLibrary.ThrowIfNull(this, _redCorner2);
    }

    public void ResetGame()
    {
        _blueSideScoreText.text = 0.ToString();
        _redSideScoreText.text = 0.ToString();
    }

    public void Server_OnScore(FieldSide pFieldSide)
    {
        // change things for server
        OnScore(pFieldSide);
        // change things for clients
        All_OnScoreRpc(pFieldSide);
    }

    // Field side should be blue when ball get scored on blue net, same for red side
    private void OnScore(FieldSide pFieldSide)
    {
        switch(pFieldSide)
        {
            // If we score on red net
            case FieldSide.Red:
                // Blue gets the point
                _blueSideScoreText.text = (int.Parse(_blueSideScoreText.text) + 1).ToString();
                // We want to trigger particles for side who scored, i.e. blue here
                foreach (ParticleSystem lBlueSideParticleSystem in _blueSideParticleSystems)
                {
                    lBlueSideParticleSystem.Play();
                }
            break;
            case FieldSide.Blue:
                // Red gets the point
                _redSideScoreText.text = (int.Parse(_redSideScoreText.text) + 1).ToString();
                // We want to trigger particles for side who scored, i.e. red here
                foreach (ParticleSystem lRedSideParticleSystem in _redSideParticleSystems)
                {
                    lRedSideParticleSystem.Play();
                }
            break;
            case FieldSide.None:
                Debug.LogError("Cannot score on None side");
            break;
        }
    }

    [ObserversRpc]
    private void All_OnScoreRpc(FieldSide pFieldSide)
    {
        if (IsServer)
            return;

        OnScore(pFieldSide);
    }

    public Vector3[] GetBlueCornersPosition()
    {
        return new Vector3[] {_blueCorner1.position, _blueCorner2.position};
    }

    public Vector3[] GetRedCornersPosition()
    {
        return new Vector3[] {_redCorner1.position, _redCorner2.position};
    }

    // FIXME : for now, don't need to go further
    // But if tis function is used very often, change implementation.
    public bool IsRedSide(Vector3 pPosition)
    {
        return Vector3.Distance(_blueSideTransform.position, pPosition) > Vector3.Distance(_redSideTransform.position, pPosition);
    }

    // It is important here to return transform : that way we can know 
    // the rotation to apply to our player, depending on which side he is
    public Transform TakeRandomSide()
    {
        // First, the server will ask for his position, so we give him blue side
        if (!_isBlueSideTaken)
        {
            Debug.Log("Side: blue");
            _isBlueSideTaken = true;
            return _blueSideTransform;
        }
        // Then, client will as for his pos, so give him red side
        else
        {
            Debug.Log("Side: red");
            _isRedSideTaken = true;
            return _redSideTransform;
        }

        // FIXME : The code below works, and is supposed to be used !
        // BUT, it does not currently take into account soccer bar position
        // so fix later!
#pragma warning disable CS0162
        if (_isBlueSideTaken)
        {
            // Both side taken, no side can be return
            if (_isRedSideTaken)
            {
                Debug.Log("No place to take on this field !");
                return null;
            }
            else
            {
                // Blue side is taken, return red side
                _isRedSideTaken = true;
                return _redSideTransform;
            }
        }

        if (_isRedSideTaken)
        {
            // Case when both side are taken is already handled

            if (!_isBlueSideTaken)
            {
                // Red side is taken, return blue side
                _isBlueSideTaken = true;
                return _blueSideTransform;
            }
        }

        // Get random side of the field, since both are free

        // 0 = red, 1 = blue
        int lRandomSide = Random.Range(0, 2);
        if (lRandomSide == 0)
        {
            _isRedSideTaken = true;
            return _redSideTransform;
        }
        else
        {
            _isBlueSideTaken = true;
            return _blueSideTransform;
        }
#pragma warning restore CS0162

    }
}
 