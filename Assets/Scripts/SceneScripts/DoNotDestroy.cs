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
                GameObject other = musicObjects[i];
                if (other == gameObject)
                    continue;

                if (other.scene.name == "DontDestroyOnLoad")
                {
                    Destroy(gameObject);
                    return;
                }
            }

            for (int i = 0; i < musicObjects.Length; i++)
            {
                if (musicObjects[i] != gameObject)
                    Destroy(musicObjects[i]);
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
