using Beavermania.Data.NPC;
using Beavermania.Display;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beavermania.NPC
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ShadowRevenantProjectileAimLine : MonoBehaviour
    {
        const float DirectionEpsilon = 0.0001f;

        static Material sharedAimLineMaterial;

        [SerializeField] LineRenderer lineRenderer;
        [SerializeField] Transform aimOrigin;
        [SerializeField] Material aimLineMaterial;

        LayerMask obstructionMask;
        float maxRange;
        float baseWidth;
        Color baseColor;
        bool windupActive;
        RaycastHit cachedHit;

        public bool IsWindupActive => windupActive;

        void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            ConfigureLineRendererDefaults();
            Hide();
        }

        void ConfigureLineRendererDefaults()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.sortingOrder = 10;

            Material material = aimLineMaterial != null ? aimLineMaterial : GetOrCreateSharedAimLineMaterial();
            if (material != null)
                lineRenderer.sharedMaterial = material;
        }

        static Material GetOrCreateSharedAimLineMaterial()
        {
            if (sharedAimLineMaterial != null)
                return sharedAimLineMaterial;

            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return null;

            sharedAimLineMaterial = new Material(shader);
            sharedAimLineMaterial.name = "ShadowRevenantAimLineRuntime";

            if (shader.name.Contains("Universal Render Pipeline"))
            {
                sharedAimLineMaterial.SetFloat("_Surface", 1f);
                sharedAimLineMaterial.SetFloat("_Blend", 0f);
                sharedAimLineMaterial.SetFloat("_AlphaClip", 0f);
                sharedAimLineMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                sharedAimLineMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                sharedAimLineMaterial.SetFloat("_ZWrite", 0f);
                sharedAimLineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedAimLineMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                sharedAimLineMaterial.renderQueue = (int)RenderQueue.Transparent;
            }

            sharedAimLineMaterial.color = Color.white;
            return sharedAimLineMaterial;
        }

        public void ApplyConfig(ShadowRevenantConfig config, Transform originOverride)
        {
            if (config == null)
                return;

            if (originOverride != null)
                aimOrigin = originOverride;

            obstructionMask = config.projectileObstructionMask;
            maxRange = config.projectileRange;
            baseWidth = config.projectileAimLineWidth;
            baseColor = config.projectileAimLineColor;

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = baseWidth;
                lineRenderer.endWidth = baseWidth * 0.65f;
                lineRenderer.startColor = baseColor;
                lineRenderer.endColor = baseColor;
            }
        }

        public void BeginWindup()
        {
            if (lineRenderer == null)
                return;

            windupActive = true;
            lineRenderer.enabled = true;
        }

        public void UpdateLine(Vector3 targetPoint, float windupProgress)
        {
            if (!windupActive || lineRenderer == null)
                return;

            Vector3 origin = aimOrigin != null ? aimOrigin.position : transform.position + Vector3.up;
            Vector3 toTarget = targetPoint - origin;
            if (toTarget.sqrMagnitude <= DirectionEpsilon)
                toTarget = transform.forward;

            Vector3 direction = toTarget.normalized;
            float range = maxRange > 0f ? maxRange : toTarget.magnitude;
            Vector3 endPoint = origin + direction * range;

            if (obstructionMask.value != 0
                && Physics.Raycast(origin, direction, out cachedHit, range, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                endPoint = cachedHit.point;
            }

            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, endPoint);

            float widthScale = Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(windupProgress));
            lineRenderer.startWidth = baseWidth * widthScale;
            lineRenderer.endWidth = baseWidth * 0.65f * widthScale;

            Color bright = baseColor;
            bright.a = Mathf.Lerp(baseColor.a * 0.45f, baseColor.a, Mathf.Clamp01(windupProgress));
            lineRenderer.startColor = bright;
            lineRenderer.endColor = bright;
        }

        public void OnFired(ShadowRevenantConfig config)
        {
            Hide();

            if (config == null || config.projectileTracerVfxPrefab == null || lineRenderer == null)
                return;

            Vector3 origin = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);
            Vector3 midpoint = (origin + end) * 0.5f;
            Vector3 direction = end - origin;
            Quaternion rotation = direction.sqrMagnitude > DirectionEpsilon
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : transform.rotation;

            PooledOneShotVfx.Spawn(config.projectileTracerVfxPrefab, midpoint, rotation);
        }

        public void Hide()
        {
            windupActive = false;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }
    }
}
