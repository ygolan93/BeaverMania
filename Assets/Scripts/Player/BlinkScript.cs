using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkScript : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer Skin;
    float Blink;
    int BlinkCount;
    [SerializeField] int BlinkLimit;
    [SerializeField] float BlinkSpeed;
    float WaitBetweenBlinks;
    [SerializeField] float BlinkBreak;
    float WaitClock;
    bool BlinkSwitch;
    Behaviour Player;
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        WaitBetweenBlinks = 0.3f;
        WaitClock = 0;
        BlinkCount = 0;
    }
    // Update is called once per frame
    void Update()
    {
        if(Player.hurt==true)
        {
            Skin.SetBlendShapeWeight(0, 20);
        }

        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl))
        {
            Skin.SetBlendShapeWeight(0, 30);
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                Skin.SetBlendShapeWeight(0, 0);
            }
            if (BlinkCount < BlinkLimit)
            {
                WaitBetweenBlinks -= Time.deltaTime;
                if (WaitBetweenBlinks <= 0)
                {
                    Blinking();
                }
                WaitClock = 0;
            }
            if (BlinkCount >= BlinkLimit)
            {
                WaitClock += Time.deltaTime;
                if (WaitClock >= BlinkBreak)
                {
                    BlinkCount = 0;
                }
            }
        }
    }

    void Blinking()
    {
        if (Blink <= 0)
        {
            BlinkSwitch = true;
            BlinkCount++;
            WaitBetweenBlinks = 0.3f;
        }
        if (Blink >= 100)
        {
            BlinkSwitch = false;
        }

        if (BlinkSwitch == true)
        {
            Skin.SetBlendShapeWeight(0, Blink += BlinkSpeed);
        }
        if (BlinkSwitch == false)
        {
            Skin.SetBlendShapeWeight(0, Blink -= BlinkSpeed);
        }
    }

}
