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

    // The number of player set to each field of the side. (for a 2 player match, field must have 1 player on each side. For a 4 player match, 2 players on each side)
    private int _blueSidePlayers = 0;
    public int BlueSidePlayers { get { return _blueSidePlayers; } }
    private int _redSidePlayers = 0;
    public int RedSidePlayers { get { return _redSidePlayers; } }

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
        switch (pFieldSide)
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
        return new Vector3[] { _blueCorner1.position, _blueCorner2.position };
    }

    public Vector3[] GetRedCornersPosition()
    {
        return new Vector3[] { _redCorner1.position, _redCorner2.position };
    }

    public FieldSide TakeTwoPlayerRandomSide()
    {
        if (_blueSidePlayers == 1)
        {
            if (_redSidePlayers < 1)
            {
                // Blue side is taken, return red side
                return TakeRedSide();
            }
            else
            {
                Debug.LogError("No side available on the field.");
                return FieldSide.None;
            }
        }

        if (_redSidePlayers == 1)
        {
            if (_blueSidePlayers < 1)
            {
                // Red side is taken, return blue side
                return TakeBlueSide();
            }
            else
            {
                Debug.LogError("No side available on the field.");
                return FieldSide.None;
            }
        }

        // Get random side of the field
        return GetRandomSide();
    }

    private FieldSide GetRandomSide()
    {
        int lRandomSide = Random.Range(0, 2);
        if (lRandomSide == 0)
        {
            return TakeRedSide();
        }
        else
        {
            return TakeBlueSide();
        }
    }

    private FieldSide TakeBlueSide()
    {
        _blueSidePlayers++;
        return FieldSide.Blue;
    }

    private FieldSide TakeRedSide()
    {
        _redSidePlayers++;
        return FieldSide.Red;
    }

    public FieldSide TakeFourPlayerRandomSide()
    {
        if (_blueSidePlayers == 2)
        {
            if (_redSidePlayers < 2)
            {
                return TakeRedSide();
            }
            else
            {
                Debug.LogError("No side available on the field.");
                return FieldSide.None;
            }
        }

        if (_redSidePlayers == 2)
        {
            if (_blueSidePlayers < 2)
            {
                return TakeBlueSide();
            }
            else
            {
                Debug.LogError("No side available on the field.");
                return FieldSide.None;
            }
        }

        if (_blueSidePlayers == 0 && _redSidePlayers == 1)
            return TakeBlueSide();

        if (_redSidePlayers == 0 && _blueSidePlayers == 1)
            return TakeRedSide();

        // If there is space on both sides, take randomly
        return GetRandomSide();
    }

    public Transform GetSideTransform(FieldSide pFieldSide)
    {
        if (pFieldSide == FieldSide.None)
            return null;

        return pFieldSide == FieldSide.Red ? _redSideTransform : _blueSideTransform;
    }
}
