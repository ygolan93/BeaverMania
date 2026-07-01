using System;

namespace Beavermania.NPC
{
    public interface IBossVictorySource
    {
        float VictoryDelay { get; }
        event Action<IBossVictorySource> Defeated;
    }
}
