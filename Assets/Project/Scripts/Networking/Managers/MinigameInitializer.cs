using UnityEngine;
using Fusion;

namespace Networking.Managers
{
    /// <summary>
    /// Initializes the minigame by spawning the MinigameManager.
    /// Should be placed in the minigame scene.
    /// </summary>
    public class MinigameInitializer : MonoBehaviour
    {
        [SerializeField]
        private NetworkPrefabRef _minigameManagerPrefab;

        private bool _hasSpawned = false;

        private void Start()
        {
            // Only spawn once
            if (_hasSpawned)
            {
                Debug.LogWarning("[MinigameInitializer] Already spawned manager. Skipping duplicate spawn.");
                return;
            }

            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
            {
                Debug.LogError("[MinigameInitializer] NetworkRunner not found!");
                return;
            }

            if (!runner.IsServer)
            {
                Debug.Log("[MinigameInitializer] Non-host client - manager spawned by host.");
                _hasSpawned = true; // Mark as spawned so we don't try again
                return;
            }

            // Host spawns the minigame manager
            if (_minigameManagerPrefab == null)
            {
                Debug.LogError("[MinigameInitializer] MinigameManager prefab not assigned!");
                return;
            }

            Debug.Log("[MinigameInitializer] Host spawning MinigameManager prefab...");
            var spawnedObj = runner.Spawn(
                _minigameManagerPrefab,
                inputAuthority: runner.LocalPlayer
            );

            if (spawnedObj == null)
            {
                Debug.LogError("[MinigameInitializer] Failed to spawn minigame manager object.");
                return;
            }

            _hasSpawned = true;
            Debug.Log($"[MinigameInitializer] ✓ Minigame manager spawned successfully: {spawnedObj.name}");
        }
    }
}
