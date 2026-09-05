using Beavermania.Player.Combat;
using UnityEngine;

namespace Beavermania.NPC
{
    public interface IPlayerAttackReceiver
    {
        bool ReceivePlayerAttack(
            int baseDamage,
            PlayerAttackKind attackKind,
            EnemyDamageType damageType,
            Transform source);
    }
}
