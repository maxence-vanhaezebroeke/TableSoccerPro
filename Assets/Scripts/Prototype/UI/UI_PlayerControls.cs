using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class UI_PlayerControls : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _controlsShortcutText;
    [SerializeField]
    private TextMeshProUGUI _controlsText;

    private bool _isControlsTextVisible = false;

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _controlsShortcutText);
        UtilityLibrary.ThrowIfNull(this, _controlsText);
    }

    private void DisplayControlsText()
    {
        // hide shortcut text
        _controlsShortcutText.gameObject.SetActive(false);
        // show control text
        _controlsText.gameObject.SetActive(true);
    }

    private void DisplayControlsShortcut()
    {
        // hide control text
        _controlsText.gameObject.SetActive(false);
        // show shortcut text 
        _controlsShortcutText.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (!_isControlsTextVisible)
            {
                DisplayControlsText();
                _isControlsTextVisible = true;
            }
            else
            {
                DisplayControlsShortcut();
                _isControlsTextVisible = false;
            }
        }
    }
}
