using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using FusionUtilsEvents;
using System.Text;

namespace Networking.Services
{
    /// <summary>
    /// Exact replication of OtherGame FusionHelper.
    /// Implements INetworkRunnerCallbacks for Fusion networking events.
    /// </summary>
    public class FusionNetworkService : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const int MaxPlayersPerRoom = 6;

        public static NetworkRunner LocalRunner;

        public NetworkPrefabRef PlayerDataNO;

        public FusionEvent OnPlayerJoinedEvent;
        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnShutdownEvent;
        public FusionEvent OnDisconnectEvent;

        [Serializable]
        private struct ConnectionTokenPayload
        {
            public int Version;
            public string Nickname;
            public string Password;
        }

        public static byte[] BuildConnectionToken(string nickname, string password)
        {
            var payload = new ConnectionTokenPayload
            {
                Version = 1,
                Nickname = (nickname ?? string.Empty).Trim(),
                Password = password ?? string.Empty
            };

            string json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }

        private static bool TryReadConnectionToken(byte[] token, out ConnectionTokenPayload payload)
        {
            payload = default;

            if (token == null || token.Length == 0)
            {
                return false;
            }

            try
            {
                string json = Encoding.UTF8.GetString(token);
                payload = JsonUtility.FromJson<ConnectionTokenPayload>(json);
                return payload.Version == 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionNetworkService] Failed to decode connection token: {ex.Message}");
                return false;
            }
        }

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
            Debug.Log($"[FusionNetworkService] Player {player.PlayerId} joined. Total: {playerCount}/{MaxPlayersPerRoom}");

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
            Debug.LogWarning($"[FusionNetworkService] Runner shutdown: {shutdownReason}");

            if (LocalRunner == runner)
            {
                LocalRunner = null;
            }

            if (FusionLauncher.IsRetrying)
            {
                Debug.Log("[FusionNetworkService] Shutdown event suppressed (Launcher is retrying).");
                return;
            }

            OnShutdownEvent?.Raise(runner: runner);
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log($"[FusionNetworkService] Connected. Region: {runner.SessionInfo?.Region}");
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"[FusionNetworkService] Disconnected from server: {reason}");

            if (LocalRunner == runner)
            {
                LocalRunner = null;
            }
            OnDisconnectEvent?.Raise(runner: runner);
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogWarning($"[FusionNetworkService] Connect failed to {remoteAddress}: {reason}");
        }

        /// <summary>
        /// Validate player join requests.
        /// Enforces 2-6 player limit per SUMAK game design.
        /// </summary>
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            // Enforce 2-6 player limit (SUMAK design requirement)
            int currentPlayerCount = runner.ActivePlayers.Count();
            string expectedPassword = PlayerPrefs.GetString("RoomPassword", string.Empty);
            bool hasToken = TryReadConnectionToken(token, out ConnectionTokenPayload payload);

            if (!hasToken)
            {
                Debug.LogWarning("[FusionNetworkService] Connection denied: missing/invalid connection token.");
                request.Refuse();
                return;
            }

            string presentedNick = string.IsNullOrWhiteSpace(payload.Nickname) ? "Unknown" : payload.Nickname;

            if (currentPlayerCount >= MaxPlayersPerRoom)
            {
                Debug.LogWarning($"[FusionNetworkService] Connection denied for {presentedNick}: room full ({MaxPlayersPerRoom}/{MaxPlayersPerRoom}).");
                request.Refuse();
                return;
            }

            if (!string.Equals(expectedPassword, payload.Password ?? string.Empty, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[FusionNetworkService] Connection denied for {presentedNick}: invalid room password.");
                request.Refuse();
                return;
            }

            request.Accept();
            Debug.Log($"[FusionNetworkService] ✓ Player '{presentedNick}' approved to join ({currentPlayerCount + 1}/{MaxPlayersPerRoom})");
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
