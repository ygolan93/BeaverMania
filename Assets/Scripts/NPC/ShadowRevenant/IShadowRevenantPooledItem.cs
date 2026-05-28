namespace Beavermania.NPC
{
    public interface IShadowRevenantPooledItem
    {
        bool IsPoolActive { get; }

        void DeactivateToPool();
    }
}
