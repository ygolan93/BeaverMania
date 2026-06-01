using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{

    public class GameMaster : MonoBehaviour
    {
        private static GameMaster instance;
        public Vector3 lastCheckPointPos;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;

                if (transform.parent != null)
                    transform.SetParent(null);

                DontDestroyOnLoad(gameObject);
                EnsureGameplayServices();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        static void EnsureGameplayServices()
        {
            if (instance == null)
                return;

            if (instance.GetComponent<ObjectiveSyncService>() == null)
                instance.gameObject.AddComponent<ObjectiveSyncService>();
        }
    }
}
