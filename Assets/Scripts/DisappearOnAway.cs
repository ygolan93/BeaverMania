using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{

    public class DisappearOnAway : MonoBehaviour
    {
        [SerializeField] GameObject Item;

        public BeaverPlayer Player;
        public Vector3 DistanceVec;
        [SerializeField] float ActualDistance;
        [SerializeField] float PopUpDistance;
        private void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<BeaverPlayer>();
        }
        private void Update()
        {
            DistanceVec = Player.transform.position - Item.transform.position;
            ActualDistance = Mathf.Abs(DistanceVec.magnitude);
            if (ActualDistance < PopUpDistance)
            {
                Item.SetActive(true);
            }
            else
                Item.SetActive(false);
        }




    }
}
