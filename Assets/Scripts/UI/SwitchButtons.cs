using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.UI.Menus
{

    public class SwitchButtons : MonoBehaviour
    {
        public GameObject ContinueButton;
        public GameObject FinishButton;
        // Start is called before the first frame update
        void Start()
        {
            ContinueButton.SetActive(true);
            FinishButton.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (ContinueButton == null)
                FinishButton.SetActive(true);
        }
    }
}
