using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{

    public class CurrencySpin : MonoBehaviour
    {
         BeaverPlayer Player;
        private void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<BeaverPlayer>();
        }
        // Update is called once per frame
        private void Update()
        {
            transform.Rotate(0, 0, 170 * Time.deltaTime);

        }
        private void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Player"))
            {
                Player.Currency += 1;
                Destroy(transform.gameObject);
            }
        }
    }
}
