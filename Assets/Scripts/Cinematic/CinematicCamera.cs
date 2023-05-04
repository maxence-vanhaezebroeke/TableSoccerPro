using System;
using UnityEngine;

[RequireComponent(typeof(FadeCamera))]
public class CinematicCamera : CinematicBehaviour
{
    public Action OnCameraFadeEnded;

    private FadeCamera _fadeCamera;

    protected override void Awake() 
    {
        base.Awake();

        _fadeCamera = GetComponent<FadeCamera>();    
    }

    protected void Start()
    {
        _fadeCamera.OnFadeEnded += FadeCamera_OnFadeEnded;
    }

    private void FadeCamera_OnFadeEnded()
    {
        _fadeCamera.OnFadeEnded -= FadeCamera_OnFadeEnded;
        OnCameraFadeEnded?.Invoke();
    }
}
