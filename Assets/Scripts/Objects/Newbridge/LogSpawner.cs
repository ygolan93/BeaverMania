using UnityEngine;
using UnityEngine.Serialization;
using Beavermania.Core.GameFlow;
using Beavermania.Display;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{
    public class LogSpawner : MonoBehaviour
    {
        public const int DefaultLogsPerTree = 4;

        [SerializeField] Transform SpawnPoint;
        [SerializeField] [Min(1)] [FormerlySerializedAs("_logsPerTree")]
        int logsPerTree = DefaultLogsPerTree;
        public Transform[] Prefab;
        public GameObject TreeDeath;
        [SerializeField] Material givingTreeBark;
        [SerializeField] Material givingTreeLeaves;

        bool _logsDropped;

        void Awake()
        {
            ApplyNatureTreeMaterials();
        }

        void ApplyNatureTreeMaterials()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null || givingTreeBark == null || givingTreeLeaves == null)
                return;

            renderer.sharedMaterials = new[] { givingTreeBark, givingTreeLeaves };
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (logsPerTree < 1)
                logsPerTree = 1;

            ApplyNatureTreeMaterials();
        }
#endif

        public void OnTriggerStay(Collider OBJ)
        {
            if (!OBJ.gameObject.CompareTag("Player"))
                return;

            var player = OBJ.gameObject.GetComponent<BeaverPlayer>();
            if (player == null)
                return;

            if (PlayerInputReader.IsPrimaryHeld() && player.grounded == false)
                DestroyTree(OBJ.transform);
        }

        public void OnCollisionStay(Collision OBJ)
        {
            if (!OBJ.gameObject.CompareTag("Player"))
                return;

            var player = OBJ.gameObject.GetComponent<BeaverPlayer>();
            if (player == null)
                return;

            if (PlayerInputReader.IsPrimaryHeld())
                DestroyTree(OBJ.transform);
        }

        public void DestroyTree(Transform OBJ)
        {
            if (_logsDropped)
                return;

            _logsDropped = true;

            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            if (TreeDeath != null && SpawnPoint != null)
                PooledOneShotVfx.Spawn(TreeDeath, new Vector3(SpawnPoint.position.x, OBJ.position.y, SpawnPoint.position.z), Quaternion.identity);

            if (Prefab != null && Prefab.Length > 0)
            {
                var origin = SpawnPoint != null ? SpawnPoint.position : transform.position;
                int stackIndex = 0;
                var primary = Prefab[0];
                if (primary != null)
                {
                    for (int i = 0; i < logsPerTree; i++)
                        Instantiate(primary, origin + new Vector3(0, stackIndex++, 0), Quaternion.Euler(0, 0, 90));
                }
                else
                    Debug.LogError($"LogSpawner on '{name}' has no primary prefab at Prefab[0]; skipping primary log spawn.", this);

                for (int r = 1; r < Prefab.Length; r++)
                {
                    var bonus = Prefab[r];
                    if (bonus == null)
                        continue;

                    Instantiate(bonus, origin + new Vector3(0, stackIndex++, 0), Quaternion.Euler(0, 0, 90));
                }
            }

            if (ObjectiveSyncService.Instance != null)
                ObjectiveSyncService.Instance.OnChopTreeDestroyed(transform);

            Destroy(gameObject);
        }
    }
}
