using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Cinematic : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _tableTitle;
    [SerializeField]
    private TextMeshProUGUI _soccerTitle;
    [SerializeField]
    private TextMeshProUGUI _proTitle;

    [SerializeField]
    private TextMeshProUGUI _infoText;

    private float _animationSpeed = 1.4f;

    private bool _isAnimationEnded = false;

    public bool _skipAnimation = false;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _tableTitle);
        UtilityLibrary.ThrowIfNull(this, _soccerTitle);
        UtilityLibrary.ThrowIfNull(this, _proTitle);
        UtilityLibrary.ThrowIfNull(this, _infoText);
        HideEverything();
    }

    void HideEverything()
    {
        SetAlphaEverything(0f);
    }

    private void SetAlphaEverything(float pNewAlpha)
    {
        _tableTitle.alpha = pNewAlpha;
        _soccerTitle.alpha = pNewAlpha;
        _proTitle.alpha = pNewAlpha;
        _infoText.alpha = pNewAlpha;
    }

    void ShowEverything()
    {
        SetAlphaEverything(1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (_skipAnimation)
        {
            ShowEverything();
            OnAnimationEnd();
        }

        if (_tableTitle.alpha < 1f)
        {
            _tableTitle.alpha += Time.deltaTime * _animationSpeed;
        }
        else if (_soccerTitle.alpha < 1f)
        {
            _soccerTitle.alpha += Time.deltaTime * _animationSpeed;
        }
        else if (_proTitle.alpha < 1f)
        {
            _proTitle.alpha += Time.deltaTime * _animationSpeed;
        }
        else if (_infoText.alpha < 1f)
        {
            _infoText.alpha += Time.deltaTime * _animationSpeed;
            // if our new value is the last one that we increment
            if (_infoText.alpha >= 1f)
                OnAnimationEnd();
        }
        else
        {
            if (_isAnimationEnded && Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        if (_infoText.alpha > 0f)
            UpdateInfoTextAnimation();
    }

    void OnAnimationEnd()
    {
        _isAnimationEnded = true;
    }

    void UpdateInfoTextAnimation()
    {

    }

    // TODO: make the 3 title text appear, with animation (coming big & opacity)
    // then display info text floating...
}
