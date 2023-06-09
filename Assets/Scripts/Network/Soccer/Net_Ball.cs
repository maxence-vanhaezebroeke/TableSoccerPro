using System.Collections.Generic;
using System;
using UnityEngine;
using FishNet.Object;

[RequireComponent(typeof(Rigidbody))]
public class Net_Ball : NetworkBehaviour
{
    public Action<Net_Ball, Net_SoccerField.FieldSide> OnGoalEnter;

    public Action<Net_Ball, Collider> OnBallExitBounds;

    [SerializeField]
    private List<Material> _ballSkins;

    private Material[] _defaultMaterials;

    private Rigidbody _rb;
    public Rigidbody Rigidbody
    {
        get => _rb;
    }

    private bool _isSkinned = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (IsServer)
        {
            _rb = GetComponent<Rigidbody>();
        }

        // Saving initial materials
        _defaultMaterials = GetComponentInChildren<Renderer>().materials;
    }

    void Update()
    {
        if (!IsServer)
            return;

        // CTRL + B (server)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.B))
        {
            Server_ChangeSkin();
        }
    }

    private void Server_ChangeSkin()
    {
        if (!IsServer)
            return;

        All_ChangeSkinRpc();
        ChangeSkin();
    }

    [ObserversRpc]
    private void All_ChangeSkinRpc()
    {
        if (IsServer)
            return;
        ChangeSkin();
    }

    private void ChangeSkin()
    {
        if (_isSkinned)
        {
            RestoreDefaultSkin();
        }
        else
        {
            SkinBall();
        }
    }

    private void RestoreDefaultSkin()
    {
        GetComponentInChildren<Renderer>().materials = _defaultMaterials;
        _isSkinned = false;
    }

    private void SkinBall()
    {
        // For now, takes first skin in ball skins
        Material[] lSkinBallMaterials = new Material[_defaultMaterials.Length];
        for (int lMaterialIndex = 0; lMaterialIndex < lSkinBallMaterials.Length; lMaterialIndex++)
        {
            lSkinBallMaterials[lMaterialIndex] = _ballSkins[0];
        }

        GetComponentInChildren<Renderer>().materials = lSkinBallMaterials;

        _isSkinned = true;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!IsServer)
            return;

        if (other.gameObject.CompareTag("Bar"))
        {
            other.gameObject.GetComponent<PredictedSoccerBar>().OnCollisionWithBall(this, other);
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            Bounce(other.GetContact(0).normal);
        }
    }

    private void Bounce(Vector3 pNormal)
    {
        _rb.velocity = Vector3.Reflect(_rb.velocity, pNormal);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (other.CompareTag("BlueGoal"))
        {
            OnGoalEnter.Invoke(this, Net_SoccerField.FieldSide.Blue);
        }
        else if (other.CompareTag("RedGoal"))
        {
            OnGoalEnter.Invoke(this, Net_SoccerField.FieldSide.Red);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BallBound"))
        {
            BallOutOfBounds(other);
        }
    }

    private void BallOutOfBounds(Collider pBoundCollider)
    {
        // No speed, but a bit of angular speed to make it roll
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = new Vector3(UnityEngine.Random.Range(1f, 1.5f), 0f, UnityEngine.Random.Range(1f, 1.5f));

        // Raise event, so that manager will know what to do
        OnBallExitBounds.Invoke(this, pBoundCollider);
    }
}
