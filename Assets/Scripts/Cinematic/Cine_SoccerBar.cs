using UnityEngine;

public class Cine_SoccerBar : CinematicBehaviour
{
    [SerializeField]
    private PS_Blast _blastPrefab;

    // 1% of slow down over time
    private float _slowingDownPercent = 1f;

    // TODO: movedown has arguments in the opposite way of parent Move (time, distance vs distance, time) - KEEP IT CLEAR !
    // Time to move in seconds, distance expected to travel per second, if slowing down - distance will be much slower than expected (to slow down)
    public void MoveDown(float pTime, float pDistance, bool pIsSlowingDown = false)
    {
        Move(new Vector3(0f, 0f, pDistance), pTime, pIsSlowingDown ? _slowingDownPercent : 0f);
    }

    // Ask bar to rotate during pTime seconds, to rotate for pDegrees° 
    public void RotateForward(float pTime, float pDegrees)
    {
        Rotate(new Vector3(0f, 0f, pDegrees), pTime);
    }

    public void RotateBackward(float pTime, float pDegrees)
    {
        RotateForward(pTime, -pDegrees);
    }

    public void MoveUp(float pTime, float pDistance, bool pIsSlowingDown = false)
    {
        MoveDown(pTime, -pDistance, pIsSlowingDown);
    }

    public void Blast(float pBlastRadius, Vector3 pLocation)
    {
        _blastPrefab._maxRadius = pBlastRadius;
        _blastPrefab._speed = pBlastRadius * 1.2f;
        Instantiate(_blastPrefab, pLocation, _blastPrefab.transform.rotation);
    }
}
