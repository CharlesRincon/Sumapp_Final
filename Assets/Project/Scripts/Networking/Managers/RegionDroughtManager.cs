using UnityEngine;
using Fusion;
using Networking.Models;
using System.Collections.Generic;

namespace Networking.Managers
{
    /// <summary>
    /// Manages a drought emergency minigame where players must refill regional water levels.
    /// Inherits from MinigameManager to reuse timer and leaderboard logic.
    /// </summary>
    public class RegionDroughtManager : MinigameManager
    {
        [Header("Drought Settings")]
        [Networked, Capacity(6)]
        public NetworkArray<float> RegionWaterLevels => default;

        [Networked]
        public int EmergencyRegionIndex { get; set; } = -1;

        [SerializeField] private float _baseDroughtSpeed = 3f;
        [SerializeField] private float _maxExtraSpeed = 10f; // Speed increases over time
        [SerializeField] private float _emergencyMultiplier = 3f;
        [SerializeField] private float _refillAmount = 6f;
        [SerializeField] private float _emergencyChangeInterval = 3f;

        private float _nextEmergencyTime;

        public override void Spawned()
        {
            base.Spawned();
            
            if (Object.HasStateAuthority)
            {
                // Force 20 seconds duration regardless of base prefab settings
                RemainingTime = 20f;

                // Start all regions at 90% water
                for (int i = 0; i < 6; i++)
                {
                    RegionWaterLevels.Set(i, 90f);
                }
                _nextEmergencyTime = (float)Runner.SimulationTime + _emergencyChangeInterval;
                EmergencyRegionIndex = Random.Range(0, 6);
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!Object.HasStateAuthority || !IsGameActive())
                return;

            // Calculate difficulty scaling: 0 at start, 1 at end
            // MinigameManager has RemainingTime.
            float progress = 1f - (GetRemainingTime() / 20f); 
            float currentDroughtSpeed = _baseDroughtSpeed + (progress * _maxExtraSpeed);

            // Apply drought to all regions
            for (int i = 0; i < 6; i++)
            {
                float currentLevel = RegionWaterLevels[i];
                if (currentLevel <= 0) continue; // Region is already "dead"

                float speed = currentDroughtSpeed;
                if (i == EmergencyRegionIndex)
                {
                    speed *= _emergencyMultiplier;
                }

                float nextLevel = Mathf.Max(0, currentLevel - (speed * Runner.DeltaTime));
                RegionWaterLevels.Set(i, nextLevel);
            }

            // Change the emergency region periodically
            if ((float)Runner.SimulationTime >= _nextEmergencyTime)
            {
                EmergencyRegionIndex = Random.Range(0, 6);
                _nextEmergencyTime = (float)Runner.SimulationTime + _emergencyChangeInterval;
            }
        }

        /// <summary>
        /// RPC called by clients to "refill" a region and gain points.
        /// </summary>
        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPC_RefillRegion(int regionIndex, PlayerRef player)
        {
            if (!IsGameActive() || regionIndex < 0 || regionIndex >= 6)
                return;

            float currentLevel = RegionWaterLevels[regionIndex];
            
            // Add water
            float newLevel = Mathf.Min(100f, currentLevel + _refillAmount);
            RegionWaterLevels.Set(regionIndex, newLevel);

            // Award points based on urgency
            int points = 1; // Base point
            
            if (regionIndex == EmergencyRegionIndex)
            {
                points = 10; // Critical bonus
            }
            else if (currentLevel < 30f)
            {
                points = 5; // Low water bonus
            }

            // Update player score in PlayerSessionData
            var playerData = GameManager.Instance.GetPlayerData(player, Runner);
            if (playerData != null)
            {
                playerData.MinigameClickCount += points;
                Debug.Log($"[RegionDroughtManager] Player {player.PlayerId} refilled Region {regionIndex}. Points +{points}");
            }
        }
    }
}
