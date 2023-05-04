using System;
using System.Collections;
using UnityEngine;

public class CinematicManager : MonoBehaviour
{

    [Header("Field")]
    [SerializeField]
    private Cine_SoccerBar _attackers;
    [SerializeField]
    private Cine_SoccerBar _defenders;
    [SerializeField]
    private Cine_SoccerBar _goalkeeper;

    [SerializeField]
    private CinematicCamera _camera;

    [SerializeField]
    private CinematicBehaviour _ball;

    [Header("UI")]
    [SerializeField]
    private UI_Cinematic _UICinematicPrefab;

    private Rigidbody _ballRb;

    private bool _isCinematicEnded = false;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _attackers);
        UtilityLibrary.ThrowIfNull(this, _defenders);
        UtilityLibrary.ThrowIfNull(this, _goalkeeper);
        UtilityLibrary.ThrowIfNull(this, _camera);
        UtilityLibrary.ThrowIfNull(this, _UICinematicPrefab);

        _ballRb = _ball.GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Activate physics and wait for camera fade to end
        Physics.autoSimulation = true;
        _camera.OnCameraFadeEnded += CinematicCamera_OnCameraFadeEnded;
    }

    private void CinematicCamera_OnCameraFadeEnded()
    {
        // When camera fade ends, start cinematic !
        _camera.OnCameraFadeEnded -= CinematicCamera_OnCameraFadeEnded;
        CinematicSequence();
    }

    void Update()
    {
        if (_isCinematicEnded == false && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipCinematic();
        }
    }

    private void SkipCinematic()
    {
        StopCinematic();
        OnCinematicEnds(true);
        // This is the final position & rotation of the camera when the cinematic ends
        _camera.transform.localPosition = new Vector3(-6.24f, 3.02f, 1.98f);
        _camera.transform.rotation = Quaternion.Euler(new Vector3(16.987f, 94.35f, 0f));
    }

    private void StopCinematic()
    {
        StopAllCoroutines();
        StopBall();
        StopCinematicBehaviour(_attackers);
        StopCinematicBehaviour(_defenders);
        StopCinematicBehaviour(_goalkeeper);
        StopCinematicBehaviour(_camera);
        StopCinematicBehaviour(_ball);
    }

    private void StopCinematicBehaviour(CinematicBehaviour pCinematicBehaviour)
    {
        pCinematicBehaviour.StopMovement();
        pCinematicBehaviour.StopRotation();
    }

    private void CinematicSequence()
    {
        StartCoroutine(CameraSequence());
        StartCoroutine(BallSequence());
        StartCoroutine(GoalkeeperSequence());
        StartCoroutine(DefendersSequence());
        StartCoroutine(AttackersSequence());
    }

    private void OnCinematicEnds(bool pIsCinematicSkipped)
    {
        UI_Cinematic lUICinematic = Instantiate(_UICinematicPrefab);
        if (pIsCinematicSkipped)
            lUICinematic._skipAnimation = true;

        _isCinematicEnded = true;
    }

    private IEnumerator CameraSequence()
    {
        // Follow the ball
        _camera.Move(new Vector3(1.2f, .2f, .5f), .8f);
        _camera.Rotate(Vector3.up * 10f, .8f);
        yield return new WaitForSeconds(1.5f);

        // Aerial view for the shot
        _camera.Move(new Vector3(.1f, .8f, -.35f) * .45f, 2f);
        _camera.Rotate(new Vector3(10f, -3f, 0f) * .45f, 2f);
        yield return new WaitForSeconds(2f);
        _camera.StopMovement();
        _camera.StopRotation();
        yield return new WaitForSeconds(.65f);
        
        
        // Ending travelling...
        _camera.Move(new Vector3(0f, 2f, -.2f) * .75f, 3f, 1f);
        _camera.Rotate(new Vector3(-10f, -35f, 0f) * .75f, 3f);
        yield return new WaitForSeconds(1f);
        OnCinematicEnds(false);
    }

    private IEnumerator BallSequence()
    {
        // Ball comes to attacker
        _ballRb.AddForce(new Vector3(1f, .3f, .3f) * 2.4f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(.8f);
        // Attack controls the ball
        StopBall();
        yield return new WaitForSeconds(.9f);
        // After rotating around, attacker pushes the ball towards symmetrical attacker
        _ballRb.AddForce(new Vector3(.05f, 0f, -1f) * 2f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(1f);
        // Other attacker is passing ball at the middle player
        StopBall();
        _ballRb.AddForce(new Vector3(.05f, 0f, 1f) * 1.85f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(.6f);
        // When ball is at the middle, shoot !
        _ballRb.AddForce(new Vector3(1f, .15f, .1f) * 15f, ForceMode.VelocityChange);
    }

    private void StopBall()
    {
        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
    }

    private IEnumerator GoalkeeperSequence()
    {
        // When ball arrives, block the side where the ball is
        yield return new WaitForSeconds(.35f);
        _goalkeeper.MoveDown(.75f, .6f);
        _goalkeeper.RotateBackward(.5f, 10f);
        yield return new WaitForSeconds(1.5f);

        // Follow the ball
        _goalkeeper.MoveUp(.75f, 1.15f);
        yield return new WaitForSeconds(1f);

        // Be late trying to follow the ball...
        _goalkeeper.MoveDown(.5f, .3f);
    }

    private IEnumerator DefendersSequence()
    {
        // Move defenders to block side where the ball is
        yield return new WaitForSeconds(.7f);
        _defenders.MoveDown(.35f, .1f);
        yield return new WaitForSeconds(1.1f);

        // follow the ball
        _defenders.MoveUp(.6f, .5f);
        yield return new WaitForSeconds(.6f);

        // Switch with other defender !
        _defenders.MoveDown(.3f, .2f);
        yield return new WaitForSeconds(.5f);
        // Try to keep up with the ball, but no enough !
        _defenders.MoveDown(.5f, .35f);
    }

    private IEnumerator AttackersSequence()
    {
        // Catching the ball
        _attackers.MoveUp(.8f, .25f);
        yield return new WaitForSeconds(.4f);
        _attackers.RotateForward(.4f, 15f);
        yield return new WaitForSeconds(.8f);

        // Pushing the ball up
        _attackers.MoveDown(.25f, .2f);
        yield return new WaitForSeconds(.15f);
        _attackers.RotateBackward(.3f, 30f);
        yield return new WaitForSeconds(.2f);
        _attackers.MoveUp(.3f, .275f);
        yield return new WaitForSeconds(.3f);
        // Up-ing the middle attacker
        _attackers.RotateBackward(.2f, 35f);
        yield return new WaitForSeconds(.2f);
        _attackers.RotateForward(.25f, 32f);
        yield return new WaitForSeconds(.35f);
        _attackers.RotateForward(.1f, 15f);
        yield return new WaitForSeconds(.15f);

        // Passing to the middle player
        _attackers.MoveDown(.3f, .15f);
        yield return new WaitForSeconds(.5f);

        // Shoot
        _attackers.RotateBackward(.1f, 40f);
        yield return new WaitForSeconds(.1f);
        _attackers.RotateForward(.2f, 80f);
        yield return new WaitForSeconds(.05f);
        _attackers.Blast(1f, _ball.transform.position + new Vector3(0f, .075f, 0f));
    }
}
