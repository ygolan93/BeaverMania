using System;

namespace Beavermania.Display
{
    public sealed class FrameBudgetGovernor
    {
        readonly float[] rollingFrameTimesMs;
        readonly int startingTier;
        readonly int minTier;
        readonly int maxTier;
        readonly float degradeAverageFrameTimeMs;
        readonly int degradeConsecutiveFrames;
        readonly float recoverAverageFrameTimeMs;
        readonly int recoverConsecutiveFrames;
        readonly float tierChangeCooldownSeconds;

        int rollingCount;
        int rollingIndex;
        int consecutiveOverBudgetFrames;
        int consecutiveUnderBudgetFrames;
        float rollingTotalFrameTimeMs;
        float cooldownRemainingSeconds;

        public FrameBudgetGovernor(
            int startingTier,
            int minTier,
            int maxTier,
            int rollingWindowSize,
            float degradeAverageFrameTimeMs,
            int degradeConsecutiveFrames,
            float recoverAverageFrameTimeMs,
            int recoverConsecutiveFrames,
            float tierChangeCooldownSeconds)
        {
            if (rollingWindowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(rollingWindowSize));

            if (minTier > maxTier)
                throw new ArgumentOutOfRangeException(nameof(minTier));

            this.startingTier = Clamp(startingTier, minTier, maxTier);
            this.minTier = minTier;
            this.maxTier = maxTier;
            this.degradeAverageFrameTimeMs = Max(0f, degradeAverageFrameTimeMs);
            this.degradeConsecutiveFrames = Math.Max(1, degradeConsecutiveFrames);
            this.recoverAverageFrameTimeMs = Max(0f, recoverAverageFrameTimeMs);
            this.recoverConsecutiveFrames = Math.Max(1, recoverConsecutiveFrames);
            this.tierChangeCooldownSeconds = Max(0f, tierChangeCooldownSeconds);
            rollingFrameTimesMs = new float[rollingWindowSize];

            Reset();
        }

        public int CurrentTier { get; private set; }

        public void Reset()
        {
            Array.Clear(rollingFrameTimesMs, 0, rollingFrameTimesMs.Length);
            rollingCount = 0;
            rollingIndex = 0;
            rollingTotalFrameTimeMs = 0f;
            consecutiveOverBudgetFrames = 0;
            consecutiveUnderBudgetFrames = 0;
            cooldownRemainingSeconds = 0f;
            CurrentTier = startingTier;
        }

        public void RecordFrame(float unscaledDeltaTime)
        {
            float sanitizedDeltaTime = Max(0f, unscaledDeltaTime);
            float frameTimeMs = sanitizedDeltaTime * 1000f;

            if (rollingCount == rollingFrameTimesMs.Length)
                rollingTotalFrameTimeMs -= rollingFrameTimesMs[rollingIndex];
            else
                rollingCount++;

            rollingFrameTimesMs[rollingIndex] = frameTimeMs;
            rollingTotalFrameTimeMs += frameTimeMs;
            rollingIndex = (rollingIndex + 1) % rollingFrameTimesMs.Length;

            cooldownRemainingSeconds = Max(0f, cooldownRemainingSeconds - sanitizedDeltaTime);

            if (rollingCount < rollingFrameTimesMs.Length)
            {
                consecutiveOverBudgetFrames = 0;
                consecutiveUnderBudgetFrames = 0;
                return;
            }

            float averageFrameTimeMs = rollingTotalFrameTimeMs / rollingCount;
            if (averageFrameTimeMs > degradeAverageFrameTimeMs)
            {
                consecutiveOverBudgetFrames++;
                consecutiveUnderBudgetFrames = 0;
                return;
            }

            if (averageFrameTimeMs < recoverAverageFrameTimeMs)
            {
                consecutiveUnderBudgetFrames++;
                consecutiveOverBudgetFrames = 0;
                return;
            }

            consecutiveOverBudgetFrames = 0;
            consecutiveUnderBudgetFrames = 0;
        }

        public bool TryStepTier(out int nextTier)
        {
            nextTier = CurrentTier;
            if (rollingCount < rollingFrameTimesMs.Length || cooldownRemainingSeconds > 0f)
                return false;

            if (consecutiveOverBudgetFrames >= degradeConsecutiveFrames && CurrentTier < maxTier)
            {
                CurrentTier++;
                nextTier = CurrentTier;
                BeginCooldown();
                return true;
            }

            if (consecutiveUnderBudgetFrames >= recoverConsecutiveFrames && CurrentTier > minTier)
            {
                CurrentTier--;
                nextTier = CurrentTier;
                BeginCooldown();
                return true;
            }

            return false;
        }

        void BeginCooldown()
        {
            cooldownRemainingSeconds = tierChangeCooldownSeconds;
            consecutiveOverBudgetFrames = 0;
            consecutiveUnderBudgetFrames = 0;
        }

        static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            return value > max ? max : value;
        }

        static float Max(float first, float second)
        {
            return first > second ? first : second;
        }
    }
}
