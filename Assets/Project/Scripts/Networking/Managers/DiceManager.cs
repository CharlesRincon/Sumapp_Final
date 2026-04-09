using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Managers
{
    /// <summary>
    /// Manages dice rolling mechanics for the table game.
    /// Handles validation, turn order, and roll tracking.
    /// </summary>
    public class DiceManager : MonoBehaviour
    {
        public static DiceManager Instance { get; private set; }

        private NetworkRunner _runner;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(transform.root.gameObject);
        }

        private void Start()
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[DiceManager] NetworkRunner not found!");
            }
            else
            {
                Debug.Log("[DiceManager] ✓ Initialized");
            }
        }

        /// <summary>
        /// Request a dice roll for the local player.
        /// The player's PlayerSessionData will handle the RPC to the host.
        /// </summary>
        public void RequestDiceRoll(PlayerRef player)
        {
            if (!EnsureActiveRunner())
            {
                Debug.LogError("[DiceManager.RequestDiceRoll] NetworkRunner not found or not running!");
                return;
            }

            if (!player.IsRealPlayer)
            {
                Debug.LogWarning("[DiceManager.RequestDiceRoll] Invalid local player reference.");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[DiceManager.RequestDiceRoll] GameManager instance not found!");
                return;
            }

            var playerData = GameManager.Instance.GetPlayerData(player, _runner);
            if (playerData == null)
            {
                Debug.LogError($"[DiceManager.RequestDiceRoll] PlayerSessionData not found for player {player.PlayerId}");
                return;
            }

            if (!playerData.IsActiveTurn)
            {
                Debug.LogWarning("[DiceManager] Roll request ignored: not this player's active turn.");
                return;
            }

            // Call the RPC to roll the dice
            playerData.RPC_RequestValidatedTurnRoll();
            Debug.Log($"[DiceManager] Dice roll requested for player {player.PlayerId}");
        }

        private bool EnsureActiveRunner()
        {
            if (_runner != null && _runner.IsRunning)
            {
                return true;
            }

            _runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (_runner != null && _runner.IsRunning)
            {
                return true;
            }

            _runner = FindFirstObjectByType<NetworkRunner>();
            return _runner != null && _runner.IsRunning;
        }

        /// <summary>
        /// Get the last dice roll result for a player.
        /// Returns 0 if no roll has been made yet.
        /// </summary>
        public int GetLastDiceRoll(PlayerRef player)
        {
            var playerData = GameManager.Instance.GetPlayerData(player, _runner);
            return playerData != null ? playerData.LastDiceRoll : 0;
        }

        /// <summary>
        /// Get the timestamp of the last dice roll.
        /// Used by UI to determine if the result should still be displayed.
        /// </summary>
        public float GetLastDiceRollTime(PlayerRef player)
        {
            var playerData = GameManager.Instance.GetPlayerData(player, _runner);
            return playerData != null ? playerData.LastDiceRollTime : 0f;
        }

        /// <summary>
        /// Get all current dice rolls, sorted by highest to lowest.
        /// Useful for determining turn order (highest rolls first).
        /// </summary>
        public List<(PlayerRef player, int roll)> GetAllDiceRolls()
        {
            var rolls = new List<(PlayerRef player, int roll)>();

            if (_runner == null)
                return rolls;

            foreach (var player in _runner.ActivePlayers)
            {
                int roll = GetLastDiceRoll(player);
                if (roll > 0)  // Only include players who have rolled
                {
                    rolls.Add((player, roll));
                }
            }

            // Sort by roll value descending (highest first)
            rolls = rolls.OrderByDescending(x => x.roll).ToList();
            return rolls;
        }

        /// <summary>
        /// Determine turn order based on current dice rolls.
        /// Returns list of players ordered from highest roll to lowest.
        /// FUTURE: Use this when implementing the initialization phase.
        /// </summary>
        public List<PlayerRef> GetTurnOrder()
        {
            return GetAllDiceRolls().Select(x => x.player).ToList();
        }

        /// <summary>
        /// Reset all dice rolls (useful when starting a new round).
        /// Only the host can do this.
        /// </summary>
        public void ResetAllDiceRolls()
        {
            if (_runner == null || !_runner.IsServer)
            {
                Debug.LogError("[DiceManager.ResetAllDiceRolls] Only the host can reset dice rolls!");
                return;
            }

            foreach (var player in _runner.ActivePlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData != null)
                {
                    playerData.LastDiceRoll = 0;
                    playerData.LastDiceRollTime = 0f;
                }
            }

            Debug.Log("[DiceManager] All dice rolls reset");
        }
    }
}
