using System;
using System.Collections.Generic;
using Beavermania.Player.AI;
using UnityEngine;

namespace Beavermania.Data.Player
{
    [Serializable]
    public class CombatBucketData
    {
        public string threatTag = "NPC";
        public int distanceBucket;
        public int totalSamples;
        public int defendCount;
        public int bowCount;
        public int fireBreathCount;
        public int meleeHammersCount;
        public int meleeArmorCount;
        public int meleeBareCount;
        public int rollCount;

        public AutoCombatActionKind GetDominantAction(int minSamples)
        {
            if (totalSamples < minSamples)
                return AutoCombatActionKind.None;

            int best = defendCount;
            AutoCombatActionKind action = AutoCombatActionKind.Defend;
            if (bowCount > best) { best = bowCount; action = AutoCombatActionKind.Bow; }
            if (fireBreathCount > best) { best = fireBreathCount; action = AutoCombatActionKind.FireBreath; }
            int meleeTotal = meleeHammersCount + meleeArmorCount + meleeBareCount;
            if (meleeTotal > best) { best = meleeTotal; action = AutoCombatActionKind.Melee; }
            if (rollCount > best) { best = rollCount; action = AutoCombatActionKind.RollEvade; }
            return action;
        }

        public string GetDominantArsenal(int minSamples)
        {
            if (totalSamples < minSamples)
                return string.Empty;

            int best = meleeBareCount;
            string name = "Bare Hands";
            if (meleeHammersCount > best) { best = meleeHammersCount; name = "Hammers"; }
            if (meleeArmorCount > best) { best = meleeArmorCount; name = "ArmorSet"; }
            if (bowCount > best) { best = bowCount; name = "Bow"; }
            return name;
        }

        public void RecordAction(AutoCombatActionKind action, string arsenal)
        {
            totalSamples++;
            switch (action)
            {
                case AutoCombatActionKind.Defend: defendCount++; break;
                case AutoCombatActionKind.Bow: bowCount++; break;
                case AutoCombatActionKind.FireBreath: fireBreathCount++; break;
                case AutoCombatActionKind.RollEvade: rollCount++; break;
                case AutoCombatActionKind.Melee:
                    if (arsenal == "Hammers") meleeHammersCount++;
                    else if (arsenal == "ArmorSet") meleeArmorCount++;
                    else meleeBareCount++;
                    break;
            }
        }
    }

    [CreateAssetMenu(fileName = "AutoPlayerPlaystyleProfile", menuName = "Beavermania/Player/AutoPlayer Playstyle Profile")]
    public class AutoPlayerPlaystyleProfile : ScriptableObject
    {
        public string sourceSceneName;
        public int sessionCount;
        public int totalSamples;
        public float recordedDurationSeconds;

        [Header("Movement (learned averages)")]
        public float learnedEngageRadius = 10f;
        public float learnedSprintDistance = 6f;
        public float learnedStuckTimeThreshold = 2.5f;
        public float learnedObstacleAvoidDistance = 1.2f;
        [Range(0f, 1f)] public float learnedJumpRate = 0.15f;
        [Range(0f, 1f)] public float learnedSprintRate = 0.5f;

        [Header("Objectives")]
        [Range(0f, 1f)] public float learnedBridgeUrgency = 0.85f;
        public int combatSampleCount;
        public int bridgeActivityCount;
        public int exploreActivityCount;

        public List<CombatBucketData> combatBuckets = new List<CombatBucketData>();

        public static int DistanceToBucket(float distance)
        {
            if (distance < 4f) return 0;
            if (distance < 8f) return 1;
            if (distance < 15f) return 2;
            return 3;
        }

        public CombatBucketData GetOrCreateBucket(string threatTag, int distanceBucket)
        {
            if (string.IsNullOrEmpty(threatTag))
                threatTag = "Unknown";

            for (int i = 0; i < combatBuckets.Count; i++)
            {
                CombatBucketData bucket = combatBuckets[i];
                if (bucket.threatTag == threatTag && bucket.distanceBucket == distanceBucket)
                    return bucket;
            }

            var created = new CombatBucketData
            {
                threatTag = threatTag,
                distanceBucket = distanceBucket
            };
            combatBuckets.Add(created);
            return created;
        }

        public CombatBucketData FindBucket(string threatTag, float distance)
        {
            if (string.IsNullOrEmpty(threatTag))
                threatTag = "Unknown";

            int bucketIndex = DistanceToBucket(distance);
            for (int i = 0; i < combatBuckets.Count; i++)
            {
                CombatBucketData bucket = combatBuckets[i];
                if (bucket.threatTag == threatTag && bucket.distanceBucket == bucketIndex)
                    return bucket;
            }

            return null;
        }

        public bool TryGetLearnedCombat(string threatTag, float distance, int minSamples,
            out AutoCombatActionKind action, out string arsenal, out float confidence)
        {
            action = AutoCombatActionKind.None;
            arsenal = string.Empty;
            confidence = 0f;

            CombatBucketData bucket = FindBucket(threatTag, distance);
            if (bucket == null || bucket.totalSamples < minSamples)
                return false;

            action = bucket.GetDominantAction(minSamples);
            arsenal = bucket.GetDominantArsenal(minSamples);
            if (action == AutoCombatActionKind.None)
                return false;

            int dominant = 0;
            dominant = Mathf.Max(dominant, bucket.defendCount);
            dominant = Mathf.Max(dominant, bucket.bowCount);
            dominant = Mathf.Max(dominant, bucket.fireBreathCount);
            dominant = Mathf.Max(dominant, bucket.meleeHammersCount + bucket.meleeArmorCount + bucket.meleeBareCount);
            dominant = Mathf.Max(dominant, bucket.rollCount);
            confidence = bucket.totalSamples > 0 ? (float)dominant / bucket.totalSamples : 0f;
            return true;
        }

        public void BakeFromSamples(IReadOnlyList<AutoPlayerPlaystyleSample> samples, string sceneName, bool mergeSession)
        {
            if (samples == null || samples.Count == 0)
                return;

            if (!mergeSession)
            {
                combatBuckets.Clear();
                combatSampleCount = 0;
                bridgeActivityCount = 0;
                exploreActivityCount = 0;
                totalSamples = 0;
                recordedDurationSeconds = 0f;
            }

            sessionCount++;
            sourceSceneName = sceneName;

            float engageSum = 0f;
            int engageCount = 0;
            float sprintSum = 0f;
            float obstacleDistSum = 0f;
            int obstacleCount = 0;
            int jumpAttempts = 0;
            int moveSamples = 0;
            float minTime = float.MaxValue;
            float maxTime = 0f;

            for (int i = 0; i < samples.Count; i++)
            {
                AutoPlayerPlaystyleSample sample = samples[i];
                totalSamples++;
                if (sample.Time < minTime) minTime = sample.Time;
                if (sample.Time > maxTime) maxTime = sample.Time;

                switch (sample.Activity)
                {
                    case PlaystyleActivityKind.Combat: combatSampleCount++; break;
                    case PlaystyleActivityKind.BuildBridge: bridgeActivityCount++; break;
                    case PlaystyleActivityKind.Explore: exploreActivityCount++; break;
                }

                if (sample.ThreatDistance > 0.5f && sample.Activity == PlaystyleActivityKind.Combat)
                {
                    engageSum += sample.ThreatDistance;
                    engageCount++;
                    CombatBucketData bucket = GetOrCreateBucket(sample.ThreatTag, DistanceToBucket(sample.ThreatDistance));
                    bucket.RecordAction(sample.InferredAction, sample.ActiveArsenal);
                }

                if (sample.MoveDirection.sqrMagnitude > 0.01f)
                {
                    moveSamples++;
                    if (sample.SprintHeld)
                        sprintSum += 1f;
                }

                if (sample.JumpPressed)
                    jumpAttempts++;

                if (sample.ObstacleBlockedAhead)
                {
                    obstacleCount++;
                    obstacleDistSum += sample.ObstacleAheadDistance;
                }
            }

            recordedDurationSeconds += Mathf.Max(0f, maxTime - minTime);

            if (engageCount > 0)
                learnedEngageRadius = Mathf.Clamp(engageSum / engageCount, 5f, 18f);

            if (moveSamples > 0)
            {
                learnedSprintRate = sprintSum / moveSamples;
                learnedSprintDistance = Mathf.Lerp(4f, 10f, 1f - learnedSprintRate);
            }

            if (obstacleCount > 0)
                learnedObstacleAvoidDistance = Mathf.Clamp(obstacleDistSum / obstacleCount, 0.8f, 2.5f);

            if (moveSamples > 0)
                learnedJumpRate = Mathf.Clamp01((float)jumpAttempts / moveSamples);

            int activityTotal = combatSampleCount + bridgeActivityCount + exploreActivityCount;
            if (activityTotal > 0)
                learnedBridgeUrgency = Mathf.Clamp01((float)bridgeActivityCount / activityTotal + 0.5f);

            learnedStuckTimeThreshold = Mathf.Clamp(learnedStuckTimeThreshold, 1.5f, 4f);
        }
    }
}
