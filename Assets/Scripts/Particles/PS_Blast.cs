using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PS_Blast : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Number of point to draw circle blast")]
    private int _numberOfPoints;

    // ----- Public variables
    // Those variables are public because before instantiating a blast, we want to modify them
    // (as parameters) in code
    [SerializeField]
    [Tooltip("Maximum blast radius")]
    public float _maxRadius;

    [SerializeField]
    [Tooltip("Blast speed")]
    public float _speed;
    // ----- endof Public variables

    [SerializeField]
    [Tooltip("Blast line width")]
    private float _width;

    private LineRenderer _lineRenderer;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numberOfPoints + 1;
    }

    void Start()
    {
        StartCoroutine(nameof(Blast));
    }

    private IEnumerator Blast()
    {
        float lCurrentRadius = 0f;
        while (lCurrentRadius < _maxRadius)
        {
            lCurrentRadius += Time.deltaTime * _speed;
            Draw(lCurrentRadius);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Draw(float pCurrentRadius)
    {
        float lAngleBetweenPoints = 360f / _numberOfPoints;

        Vector3 lPointPosition;
        for (int lPointIndex = 0; lPointIndex <= _numberOfPoints; lPointIndex++)
        {
            float lAngle = lPointIndex * lAngleBetweenPoints * Mathf.Deg2Rad;
            // Direction * current radius
            lPointPosition = new Vector3(Mathf.Sin(lAngle), Mathf.Cos(lAngle), 0f) * pCurrentRadius;

            _lineRenderer.SetPosition(lPointIndex, lPointPosition);
        }

        _lineRenderer.widthMultiplier = Mathf.Lerp(0f, _width, 1f - pCurrentRadius / _maxRadius);
    }
}
