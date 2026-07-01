using System;
using Beavermania.Player;
using UnityEngine;

namespace Beavermania.NPC
{
    public sealed class WaspQueenChargeAttack : MonoBehaviour
    {
        const float DirectionEpsilon = 0.0001f;
        const float SteerLerpRate = 12f;

        [SerializeField] Transform chargeOrigin;
        [SerializeField] float hitRadius = 1.4f;

        Vector3 chargeDirection = Vector3.forward;
        bool active;
        bool hitApplied;

        public Vector3 ChargeDirection => chargeDirection;
        public bool IsActive => active;

        public void BeginCharge(Vector3 horizontalDirection)
        {
            chargeDirection = horizontalDirection.sqrMagnitude > DirectionEpsilon
                ? horizontalDirection.normalized
                : transform.forward;
            active = true;
            hitApplied = false;
        }

        public void EndCharge()
        {
            active = false;
            hitApplied = false;
        }

        /// <summary>
        /// Re-steers the active dash toward a moving target. homingStrength 0 = locked (no steer), 1 = strong tracking.
        /// </summary>
        public void SteerToward(Vector3 targetPosition, float homingStrength, float deltaTime)
        {
            if (!active)
                return;

            Vector3 toTarget = targetPosition - transform.position;
            Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontal.sqrMagnitude <= DirectionEpsilon)
                return;

            float blend = Mathf.Clamp01(Mathf.Clamp01(homingStrength) * SteerLerpRate * deltaTime);
            Vector3 steered = Vector3.Slerp(chargeDirection, horizontal.normalized, blend);
            chargeDirection = steered.sqrMagnitude > DirectionEpsilon ? steered.normalized : transform.forward;
        }

        public bool TickMovement(
            Rigidbody body,
            float speed,
            float deltaTime,
            Func<Vector3, Vector3> resolvePosition,
            LayerMask obstructionMask)
        {
            if (!active)
                return false;

            float step = Mathf.Max(0f, speed) * deltaTime;
            Vector3 nextPosition = transform.position + chargeDirection * step;
            bool blocked = false;
            Vector3 origin = ResolveOrigin();

            if (obstructionMask.value != 0
                && Physics.SphereCast(
                    origin,
                    hitRadius * 0.5f,
                    chargeDirection,
                    out RaycastHit hit,
                    step + hitRadius,
                    obstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                blocked = true;
                nextPosition = hit.point - chargeDirection * hitRadius;
            }

            Vector3 resolved = resolvePosition != null ? resolvePosition(nextPosition) : nextPosition;
            if (body != null && body.isKinematic)
                body.MovePosition(resolved);
            else if (body != null)
                body.position = resolved;
            else
                transform.position = resolved;

            return blocked;
        }

        public bool TryApplyHit(BeaverPlayerBehaviour player, float damage, out Vector3 impactPoint)
        {
            impactPoint = ResolveOrigin();
            if (!active || hitApplied || player == null)
                return false;

            Vector3 toPlayer = player.transform.position - transform.position;
            Vector3 horizontal = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (horizontal.sqrMagnitude <= DirectionEpsilon)
                return false;

            float alignment = Vector3.Dot(chargeDirection, horizontal.normalized);
            if (alignment < 0.35f)
                return false;

            if (horizontal.magnitude > hitRadius * 1.75f)
                return false;

            if (player.Rolling || player.isParried)
                return false;

            player.TakeDamage(damage);
            hitApplied = true;
            impactPoint = player.transform.position;
            return true;
        }

        Vector3 ResolveOrigin()
        {
            if (chargeOrigin != null)
                return chargeOrigin.position;

            return transform.position + Vector3.up * 0.8f;
        }
    }
}
