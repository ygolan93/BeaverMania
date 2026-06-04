using Beavermania.Data.NPC;
using Beavermania.Display;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class ShadowRevenantChargeAttack : MonoBehaviour
    {
        const float DirectionEpsilon = 0.0001f;

        [SerializeField] Transform chargeOrigin;

        Vector3 chargeDirection = Vector3.forward;
        bool hitApplied;
        bool active;

        public bool IsActive => active;
        public Vector3 ChargeDirection => chargeDirection;

        public void BeginCharge(Vector3 horizontalDirection)
        {
            chargeDirection = horizontalDirection.sqrMagnitude > DirectionEpsilon
                ? horizontalDirection.normalized
                : transform.forward;
            hitApplied = false;
            active = true;
        }

        public void EndCharge()
        {
            active = false;
            hitApplied = false;
        }

        public bool TickMovement(
            ShadowRevenantConfig config,
            Rigidbody body,
            float deltaTime,
            System.Func<Vector3, Vector3> resolveHoverPosition)
        {
            if (!active || config == null)
                return false;

            float step = config.chargeSpeed * deltaTime;
            Vector3 origin = ResolveOrigin();
            Vector3 nextFlat = transform.position + chargeDirection * step;
            bool blocked = false;

            if (config.chargeObstructionMask.value != 0
                && Physics.SphereCast(
                    origin,
                    config.chargeHitRadius * 0.5f,
                    chargeDirection,
                    out RaycastHit obstructionHit,
                    step + config.chargeHitRadius,
                    config.chargeObstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                blocked = true;
                nextFlat = obstructionHit.point - chargeDirection * config.chargeHitRadius;
            }

            Vector3 resolved = resolveHoverPosition != null
                ? resolveHoverPosition(nextFlat)
                : nextFlat;

            if (body != null && body.isKinematic)
                body.MovePosition(resolved);
            else
                transform.position = resolved;

            return blocked;
        }

        public bool TryApplyHit(
            ShadowRevenantConfig config,
            IShadowRevenantTarget hitTarget,
            out Vector3 impactPoint)
        {
            impactPoint = ResolveOrigin();
            if (!active || hitApplied || config == null || hitTarget == null || hitTarget.TargetTransform == null)
                return false;

            if (!hitTarget.CanReceiveShadowDamage || hitTarget.IsAvoidingDamage)
                return false;

            Vector3 targetPosition = hitTarget.TargetTransform.position;
            Vector3 toTarget = targetPosition - transform.position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude > (config.chargeMaxRange + config.chargeHitRadius) * (config.chargeMaxRange + config.chargeHitRadius))
                return false;

            float alignment = Vector3.Dot(chargeDirection, horizontal.normalized);
            if (alignment < 0.35f)
                return false;

            Vector3 castOrigin = ResolveOrigin();
            float castDistance = horizontal.magnitude + config.chargeHitRadius;
            if (Physics.SphereCast(
                    castOrigin,
                    config.chargeHitRadius,
                    horizontal.normalized,
                    out RaycastHit hit,
                    castDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                IShadowRevenantTarget struck = hit.collider.GetComponentInParent<IShadowRevenantTarget>();
                if (struck == null || struck.TargetTransform != hitTarget.TargetTransform)
                    return false;

                impactPoint = hit.point;
            }
            else if (horizontal.magnitude > config.chargeHitRadius * 2f)
            {
                return false;
            }

            hitTarget.ReceiveShadowDamage(config.chargeDamage);
            hitApplied = true;
            impactPoint = targetPosition + Vector3.up * 0.5f;
            return true;
        }

        public void SpawnImpactVfx(ShadowRevenantConfig config, Vector3 position)
        {
            if (config == null || config.chargeImpactVfxPrefab == null)
                return;

            PooledOneShotVfx.Spawn(config.chargeImpactVfxPrefab, position, Quaternion.identity);
        }

        Vector3 ResolveOrigin()
        {
            if (chargeOrigin != null)
                return chargeOrigin.position;

            return transform.position + Vector3.up * 0.75f;
        }
    }
}
