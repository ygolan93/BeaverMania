using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beavermania.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraAnchors : MonoBehaviour
    {
        const string ContainerName = "CameraAnchors";
        const string FollowAnchorName = "CameraFollowAnchor";
        const string GameplayLookAnchorName = "GameplayLookAnchor";
        const string ClosePovLookAnchorName = "ClosePovLookAnchor";

        public Transform rootReference;
        public Transform faceReference;
        public Transform cameraFollowAnchor;
        public Transform gameplayLookAnchor;
        public Transform closePovLookAnchor;

        public Transform GameplayFollowTarget => cameraFollowAnchor;
        public Transform GameplayLookTarget => gameplayLookAnchor;
        public Transform ClosePovLookTarget => closePovLookAnchor != null ? closePovLookAnchor : gameplayLookAnchor;

        void Reset()
        {
            rootReference = transform;
            ResolveExistingAnchors();
        }

        void OnValidate()
        {
            if (rootReference == null)
                rootReference = transform;

            ResolveExistingAnchors();
            UpdateAnchors();
        }

        void LateUpdate()
        {
            UpdateAnchors();
        }

        [ContextMenu("Create Missing Camera Anchor Hierarchy")]
        public void CreateMissingAnchorHierarchy()
        {
            var container = FindOrCreateChild(transform, ContainerName);
            cameraFollowAnchor = FindOrCreateChild(container, FollowAnchorName);
            gameplayLookAnchor = FindOrCreateChild(container, GameplayLookAnchorName);
            closePovLookAnchor = FindOrCreateChild(container, ClosePovLookAnchorName);
            UpdateAnchors();
        }

        public void EnsureRuntimeAnchorHierarchy()
        {
            if (rootReference == null)
                rootReference = transform;

            if (cameraFollowAnchor == null || gameplayLookAnchor == null || closePovLookAnchor == null)
                CreateMissingAnchorHierarchy();
            else
                UpdateAnchors();
        }

        public void UpdateAnchors()
        {
            var root = rootReference != null ? rootReference : transform;
            var face = faceReference != null ? faceReference : root;
            var yawRotation = ResolveYawRotation(root);

            SetAnchor(cameraFollowAnchor, root.position, yawRotation);
            SetAnchor(gameplayLookAnchor, face.position, yawRotation);
            SetAnchor(closePovLookAnchor, face.position, yawRotation);
        }

        void ResolveExistingAnchors()
        {
            var container = transform.Find(ContainerName);
            if (container == null)
                return;

            if (cameraFollowAnchor == null)
                cameraFollowAnchor = container.Find(FollowAnchorName);
            if (gameplayLookAnchor == null)
                gameplayLookAnchor = container.Find(GameplayLookAnchorName);
            if (closePovLookAnchor == null)
                closePovLookAnchor = container.Find(ClosePovLookAnchorName);
        }

        static void SetAnchor(Transform anchor, Vector3 position, Quaternion rotation)
        {
            if (anchor != null)
                anchor.SetPositionAndRotation(position, rotation);
        }

        Quaternion ResolveYawRotation(Transform root)
        {
            var forward = root != null ? root.forward : transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.000001f)
                forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.000001f)
                forward = Vector3.forward;

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        static Transform FindOrCreateChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                return child;

            var childObject = new GameObject(childName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
#endif
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }
    }
}
