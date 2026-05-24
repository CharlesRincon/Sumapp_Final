using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System;
using Fusion.Sockets;
using System.Threading.Tasks;

namespace Networking.Services
{
    public class FusionLauncher : MonoBehaviour
    {
        private const int MaxClientJoinAttempts = 8;

        private NetworkRunner _runner;
        private ConnectionStatus _status;
        private string _statusMessage;

        public enum ConnectionStatus
        {
            Disconnected,
            Connecting,
            Failed,
            Connected,
            Loading,
            Loaded
        }

        public async void Launch(GameMode mode, string room, INetworkSceneManager sceneLoader)
        {
            string normalizedRoom = (room ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedRoom))
            {
                SetConnectionStatus(ConnectionStatus.Failed, "Room name is required.");
                Debug.LogError("[FusionLauncher] Launch aborted: room name is empty.");
                return;
            }

            SetConnectionStatus(ConnectionStatus.Connecting, "Connecting...");

            DontDestroyOnLoad(gameObject);

            int maxAttempts = mode == GameMode.Client ? MaxClientJoinAttempts : 1;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await EnsureRunnerReady(mode);

                try
                {
                    var args = new StartGameArgs()
                    {
                        GameMode = mode,
                        SessionName = normalizedRoom,
                        SceneManager = sceneLoader,
                        PlayerCount = 6,  // Max 6 players (SUMAK design: 2-6 players)
                        ConnectionToken = FusionNetworkService.BuildConnectionToken(
                            PlayerPrefs.GetString("Nick", string.Empty),
                            PlayerPrefs.GetString("RoomPassword", string.Empty))
                    };

                    Debug.Log($"[FusionLauncher] Attempt {attempt}/{maxAttempts} — Room: '{normalizedRoom}', Mode: {mode}");

                    StartGameResult result = await _runner.StartGame(args);

                    if (result.Ok)
                    {
                        SetConnectionStatus(ConnectionStatus.Connected, "Connected.");
                        Debug.Log($"[FusionLauncher] Connected to room '{normalizedRoom}' as {mode} (attempt {attempt}/{maxAttempts}).");
                        return;
                    }

                    string reason = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? result.ShutdownReason.ToString()
                        : result.ErrorMessage;

                    bool shouldRetry = ShouldRetryJoin(mode, reason, attempt, maxAttempts);
                    if (!shouldRetry)
                    {
                        SetConnectionStatus(ConnectionStatus.Failed, reason);
                        Debug.LogError($"[FusionLauncher] StartGame failed for room '{normalizedRoom}'. Reason: {reason}");
                        await SafeShutdownRunner();
                        return;
                    }

                    int delayMs = GetRetryDelayMs(attempt);
                    SetConnectionStatus(ConnectionStatus.Connecting, $"Retrying join ({attempt}/{maxAttempts})...");
                    Debug.LogWarning($"[FusionLauncher] Join retry scheduled in {delayMs}ms for room '{normalizedRoom}'. Reason: {reason}");

                    await SafeShutdownRunner();
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    bool shouldRetry = ShouldRetryJoin(mode, ex.Message, attempt, maxAttempts);
                    if (!shouldRetry)
                    {
                        SetConnectionStatus(ConnectionStatus.Failed, ex.Message);
                        Debug.LogError($"[FusionLauncher] Exception during Launch: {ex.Message}");
                        await SafeShutdownRunner();
                        return;
                    }

                    int delayMs = GetRetryDelayMs(attempt);
                    SetConnectionStatus(ConnectionStatus.Connecting, $"Retrying join ({attempt}/{maxAttempts})...");
                    Debug.LogWarning($"[FusionLauncher] Transient launch exception, retrying in {delayMs}ms: {ex.Message}");
                    await SafeShutdownRunner();
                    await Task.Delay(delayMs);
                }
            }

            SetConnectionStatus(ConnectionStatus.Failed, "Join failed after retries.");
        }

        public void SetConnectionStatus(ConnectionStatus status, string message)
        {
            _status = status;
            _statusMessage = message;
        }

        private async Task SafeShutdownRunner()
        {
            if (_runner == null) return;

            try
            {
                await _runner.Shutdown();
            }
            catch (Exception shutdownEx)
            {
                Debug.LogWarning($"[FusionLauncher] Shutdown error: {shutdownEx.Message}");
            }
            finally
            {
                if (_runner != null)
                {
                    Destroy(_runner);
                }
                _runner = null;

                await Task.Yield();
            }
        }

        private async Task EnsureRunnerReady(GameMode mode)
        {
            if (_runner != null)
            {
                await SafeShutdownRunner();
            }

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.name = name;
            _runner.ProvideInput = mode != GameMode.Server;

            await Task.Yield();

            var fusionService = FindFirstObjectByType<FusionNetworkService>();
            if (fusionService != null)
            {
                _runner.AddCallbacks(fusionService);
            }
        }

        private static bool ShouldRetryJoin(GameMode mode, string reason, int attempt, int maxAttempts)
        {
            if (mode != GameMode.Client || attempt >= maxAttempts)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            // Fusion/Photon 32758: room name not found in the selected region yet.
            return reason.IndexOf("32758", StringComparison.OrdinalIgnoreCase) >= 0
                || reason.IndexOf("game does not exist", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int GetRetryDelayMs(int attempt)
        {
            // 250, 500, 1000... short backoff to absorb host room registration delay.
            int baseDelay = 500;
            int shift = Mathf.Clamp(attempt - 1, 0, 2);
            return Mathf.Min(baseDelay * (1 << shift), 3000);
        }
    }
}
