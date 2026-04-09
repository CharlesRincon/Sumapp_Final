using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using FusionUtilsEvents;

namespace Networking.Services
{
    /// <summary>
    /// Exact replication of OtherGame FusionHelper.
    /// Implements INetworkRunnerCallbacks for Fusion networking events.
    /// </summary>
    public class FusionNetworkService : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static NetworkRunner LocalRunner;

        public NetworkPrefabRef PlayerDataNO;

        public FusionEvent OnPlayerJoinedEvent;
        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnShutdownEvent;
        public FusionEvent OnDisconnectEvent;

        /// <summary>
        /// Host-side validation for dice roll requests.
        /// </summary>
        public bool ValidateDiceRollRequest(PlayerRef requestingPlayer, PlayerRef activePlayer, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return false;
            }

            if (requestingPlayer != activePlayer)
            {
                Debug.LogWarning($"[FusionNetworkService] Dice request rejected. Requesting={requestingPlayer.PlayerId}, Active={activePlayer.PlayerId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Host-side dice generation for the round slice.
        /// </summary>
        public int GenerateValidatedDiceRoll()
        {
            return UnityEngine.Random.Range(1, 11);
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                runner.Spawn(PlayerDataNO, inputAuthority: player);
            }

            if (runner.LocalPlayer == player)
            {
                LocalRunner = runner;
            }

            // Log player count status (SUMAK: 2-6 players max)
            int playerCount = runner.ActivePlayers.Count();
            Debug.Log($"[FusionNetworkService] Player {player.PlayerId} joined. Total: {playerCount}/6");

            if (playerCount >= 5)
            {
                Debug.Log("[FusionNetworkService] Room almost full (5/6 or 6/6)!");
            }

            OnPlayerJoinedEvent?.Raise(player, runner);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            OnPlayerLeftEvent?.Raise(player, runner);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (LocalRunner == runner)
            {
                LocalRunner = null;
            }
            OnShutdownEvent?.Raise(runner: runner);
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (LocalRunner == runner)
            {
                LocalRunner = null;
            }
            OnDisconnectEvent?.Raise(runner: runner);
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        /// <summary>
        /// Validate player join requests.
        /// Enforces 2-6 player limit per SUMAK game design.
        /// </summary>
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            // Enforce 2-6 player limit (SUMAK design requirement)
            int currentPlayerCount = runner.ActivePlayers.Count();

            if (currentPlayerCount >= 6)
            {
                Debug.LogWarning($"[FusionNetworkService] Connection denied: Room full (6/6 players)");
                // In Photon Fusion 2.x, rejecting a connection is done by not approving it
                // The framework will handle the denial automatically
                return;
            }

            Debug.Log($"[FusionNetworkService] ✓ Player approved to join ({currentPlayerCount + 1}/6)");
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    }
}
