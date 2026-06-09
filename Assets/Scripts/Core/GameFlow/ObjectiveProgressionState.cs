namespace Beavermania.Core.GameFlow
{
    public sealed class ObjectiveProgressionState
    {
        public int CurrentObjectiveIndex { get; private set; }

        public bool IsInitialized { get; private set; }

        public void Initialize(int objectiveIndex)
        {
            CurrentObjectiveIndex = objectiveIndex;
            IsInitialized = true;
        }

        public bool TrySetCurrentObjectiveIndex(int objectiveIndex)
        {
            if (IsInitialized && objectiveIndex == CurrentObjectiveIndex)
                return false;

            CurrentObjectiveIndex = objectiveIndex;
            IsInitialized = true;
            return true;
        }
    }
}
