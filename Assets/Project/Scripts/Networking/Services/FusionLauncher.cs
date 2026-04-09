using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System;
using Fusion.Sockets;

namespace Networking.Services
{
    public class FusionLauncher : MonoBehaviour
    {
        private NetworkRunner _runner;
        private ConnectionStatus _status;

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
            SetConnectionStatus(ConnectionStatus.Connecting, "");

            DontDestroyOnLoad(gameObject);

            if (_runner == null)
                _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.name = name;
            _runner.ProvideInput = mode != GameMode.Server;

            // Register FusionNetworkService as callbacks handler
            var fusionService = FindFirstObjectByType<FusionNetworkService>();
            if (fusionService != null)
            {
                _runner.AddCallbacks(fusionService);
                Debug.Log("[FusionLauncher] FusionNetworkService registered as callbacks handler");
            }

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = room,
                SceneManager = sceneLoader,
                PlayerCount = 6  // Max 6 players (SUMAK design: 2-6 players)
            });

            SetConnectionStatus(ConnectionStatus.Connected, "");
        }

        public void SetConnectionStatus(ConnectionStatus status, string message)
        {
            _status = status;
        }
    }
}
