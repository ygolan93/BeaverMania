using UnityEngine;

namespace Beavermania.NPC
{
    public interface IEnemyDamageReceiver
    {
        bool ReceiveDamage(int damage, EnemyDamageType damageType, Transform source);
    }
}
