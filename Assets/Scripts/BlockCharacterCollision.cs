using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Player
{

    public class BlockCharacterCollision : MonoBehaviour
    {
        public CapsuleCollider characterCollider;
        public CapsuleCollider characterBlockerCollider;
        void Start()
        {
            Physics.IgnoreCollision(characterCollider, characterBlockerCollider, true);
        }

    }
}
