using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    void Awake()
    {
        UtilityLibrary.ThrowIfNull(this, _tableTitle);
        UtilityLibrary.ThrowIfNull(this, _soccerTitle);
        UtilityLibrary.ThrowIfNull(this, _proTitle);
    }

    // Update is called once per frame
    void Update()
    {
       // use it to make animations / to make the info text floating.... 
    }

    // TODO: make the 3 title text appear, with animation (coming big & opacity)
    // then display info text floating...
}
