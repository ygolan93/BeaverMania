using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class TriggerVolumeVisualHider : MonoBehaviour
    {
        [SerializeField] bool hideMeshRenderer = true;
        [SerializeField] bool hideSpriteRenderer = true;
        [SerializeField] bool requireTriggerCollider = true;

        void Awake()
        {
            if (requireTriggerCollider)
            {
                var collider = GetComponent<Collider>();
                if (collider == null || !collider.isTrigger)
                    return;
            }

            if (hideMeshRenderer)
            {
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.enabled = false;
            }

            if (hideSpriteRenderer)
            {
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
            }
        }

        void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider == null)
                return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            if (collider is BoxCollider box)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
            }
        }
    }
}
