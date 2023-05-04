using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Button))]
public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    [Tooltip("Font highlighted color")]
    private Color _fontHighlightedColor;

    [SerializeField]
    private TextMeshProUGUI _buttonText;

    private Button _button;

    private Color _initialFontColor;

    void Awake()
    {
        _button = GetComponent<Button>();
        _initialFontColor = _buttonText.color; 
    }

    public void OnPointerExit(PointerEventData pEventData)
    {
        _buttonText.color = _initialFontColor;
    }

    public void OnPointerEnter(PointerEventData pEventData)
    {
        _buttonText.color = _fontHighlightedColor;
    }
}
