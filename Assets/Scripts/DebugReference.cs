using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugReference : MonoBehaviour
{
    public Behavior Player;

    public TextMeshProUGUI DisplayText;
    public TextMeshProUGUI LogCountText;
    public TextMeshProUGUI HealingDisplay;


    //// Text LegacyText;
    //private void Start()
    //{
    //}
    // Update is called once per frame
    void Update()
    {
        DisplayText.text = Player.DebugText;
        LogCountText.text = Player.LogCount;
        HealingDisplay.text = Player.HealingText;
    }
}
