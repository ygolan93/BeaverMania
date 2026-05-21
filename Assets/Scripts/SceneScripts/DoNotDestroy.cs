using UnityEngine;

namespace Beavermania.Audio
{
    public class DoNotDestroy : MonoBehaviour
    {
        void Awake()
        {
            GameObject[] musicObjects = GameObject.FindGameObjectsWithTag("Music");
            for (int i = 0; i < musicObjects.Length; i++)
            {
                if (musicObjects[i] != gameObject)
                    Destroy(musicObjects[i]);
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
