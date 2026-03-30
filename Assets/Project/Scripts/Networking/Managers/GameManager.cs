using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;

namespace Networking.Managers
{
    /// <summary>
    /// Exact replication of OtherGame GameManager.
    /// Manages game state and player data tracking.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnRunnerShutDownEvent;
        
        private Dictionary<PlayerRef, Networking.Models.PlayerSessionData> _playerData = new Dictionary<PlayerRef, Networking.Models.PlayerSessionData>();

        public enum GameState
        {
            Lobby,
            Playing,
            Loading
        }

        public GameState State { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this.transform.parent.gameObject);
            }
            DontDestroyOnLoad(transform.parent);
        }

        private void OnEnable()
        {
            OnPlayerLeftEvent.RegisterResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RegisterResponse(DisconnectedFromSession);
        }

        private void OnDisable()
        {
            OnPlayerLeftEvent.RemoveResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RemoveResponse(DisconnectedFromSession);
        }

        public void SetGameState(GameState state)
        {
            State = state;
        }

        public Networking.Models.PlayerSessionData GetPlayerData(PlayerRef player, NetworkRunner runner)
        {
            NetworkObject NO;
            if (runner.TryGetPlayerObject(player, out NO))
            {
                Networking.Models.PlayerSessionData data = NO.GetComponent<Networking.Models.PlayerSessionData>();
                return data;
            }
            else
            {
                Debug.LogWarning("Player not found");
                return null;
            }
        }

        public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
        {
            if (_playerData.ContainsKey(player))
            {
                if (_playerData[player].Instance != null)
                {
                    runner.Despawn(_playerData[player].Instance);
                }
                runner.Despawn(_playerData[player].Object);
                _playerData.Remove(player);
            }
        }

        public void LeaveRoom()
        {
            _ = LeaveRoomAsync();
        }

        private async Task LeaveRoomAsync()
        {
            await ShutdownRunner();
        }

        private async Task ShutdownRunner()
        {
            if (Networking.Services.FusionNetworkService.LocalRunner != null)
            {
                await Networking.Services.FusionNetworkService.LocalRunner.Shutdown();
            }
            SetGameState(GameState.Lobby);
            _playerData.Clear();
        }

        public void DisconnectedFromSession(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("Disconnected from the session");
            ExitSession();
        }

        public void ExitSession()
        {
            _ = ShutdownRunner();
            SceneManager.LoadScene(0);
        }

        public void ExitGame()
        {
            _ = ShutdownRunner();
            Application.Quit();
        }

        public void SetPlayerDataObject(PlayerRef objectInputAuthority, Networking.Models.PlayerSessionData playerData)
        {
            if (!_playerData.ContainsKey(objectInputAuthority))
            {
                _playerData.Add(objectInputAuthority, playerData);
            }
        }
    }
}
